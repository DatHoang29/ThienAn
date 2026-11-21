using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Metadata;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Core.Helpers;
using Module.ShareData.Infrastructure.Services.ShareDataMappingReader;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;

namespace Module.ShareData.Infrastructure.Services.ShareDataExtraction
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Dịch vụ SQL Center cho gói tin (siêu dữ liệu bảng/cột, danh mục bảng nguồn và kiểm tra an toàn câu truy vấn SQL bất đồng bộ) dành cho SuperAdmin.
    /// Created date: 18/10/2026
    /// </summary>
    public class PacketSqlService : IScoped
    {
        private readonly ISqlSugarClient _db;
        private readonly UserManager _userManager;
        private readonly ILogger<PacketSqlService> _logger;
        private static int _emptyCatalogWarned;

        private static readonly Regex SafeTableNameRegex = new(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

        /// <summary>Kiểu người dùng được chọn cho trường: 4 kiểu bộ dò sinh + các mã trong cấu hình sharedata_value_type.</summary>
        private static readonly string[] AllowedFieldTypes =
        [
            SqlFieldType.String, SqlFieldType.Number, SqlFieldType.DateTime, SqlFieldType.Bool,
            "int", "long", "float", "double", "decimal", "dateTime", "guid"
        ];

        public PacketSqlService(
            ISqlSugarClient db,
            UserManager userManager,
            ILogger<PacketSqlService> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Bộ biến của một câu SQL (biến đi theo câu SQL): partnerId trống = mẫu chung; có partnerId = bộ biến của SQL riêng đối tác,
        /// đối tác chưa có dòng nào (chưa tạo SQL riêng) thì trả bộ biến mẫu chung làm điểm xuất phát.
        /// Dùng chung cho đọc và cho kết quả sau khi lưu.
        /// </summary>
        public async Task<List<PacketSqlVariableItem>> LoadItems(
            string? packetId,
            string? partnerId,
            IReadOnlyList<SqlVariableItem>? savedRows = null,
            string? packetCode = null,
            string? partnerCode = null)
        {
            // Nơi gọi vừa ghi bộ biến (và đã có mã gói, mã đối tác) thì truyền vào, không đọc lại từ CSDL
            var rows = savedRows ?? await SqlTextHelper.LoadEditingVariableRows(_db, packetId, partnerId);
            var scope = string.IsNullOrWhiteSpace(partnerId) ? ShareDataConst.SqlVariableScopeCommon : ShareDataConst.SqlVariableScopePartner;
            ShareDataLastSend? lastSend = null;
            List<ShareDataLastSend> groupCursors = [];
            if (!string.IsNullOrWhiteSpace(partnerId) && !string.IsNullOrWhiteSpace(packetId))
            {
                packetCode ??= await _db.Queryable<ShareDataPacket>()
                    .Where(p => p.IsDelete == null && p.ID == packetId)
                    .Select(p => p.Code)
                    .FirstAsync();
                partnerCode ??= await _db.Queryable<ShareDataPartner>()
                    .Where(p => p.IsDelete == null && p.ID == partnerId)
                    .Select(p => p.Code)
                    .FirstAsync();
                if (!string.IsNullOrWhiteSpace(packetCode) && !string.IsNullOrWhiteSpace(partnerCode))
                {
                    var cursors = await _db.Queryable<ShareDataLastSend>()
                        .Where(ls => ls.IsDelete == null && ls.PartnerCode == partnerCode && ls.PacketCode == packetCode)
                        .ToListAsync();
                    lastSend = cursors.FirstOrDefault(ls => string.IsNullOrEmpty(ls.CursorKey));
                    groupCursors = cursors.Where(ls => !string.IsNullOrEmpty(ls.CursorKey)).ToList();
                }
            }
            return rows.Select(v => ToVariableItem(v, scope, lastSend, groupCursors)).ToList();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đổi dòng cấu hình biến SQL sang DTO trả về giao diện; biến lastKey_&lt;nhóm&gt; lấy mốc đã lưu từ groupCursors.
        /// Created date: 29/10/2026
        /// Modified date: 02/11/2026
        /// </summary>
        public static PacketSqlVariableItem ToVariableItem(SqlVariableItem v, string scope, ShareDataLastSend? lastSend, IReadOnlyList<ShareDataLastSend>? groupCursors = null) => new()
        {
            Code = v.Code ?? string.Empty,
            Name = v.Name,
            DataType = v.DataType ?? string.Empty,
            ValueKind = v.ValueKind ?? string.Empty,
            Value = v.Value,
            DefaultValue = v.DefaultValue,
            SavedValue = ResolveSavedValue(v, lastSend, groupCursors),
            Remark = v.Remark,
            OrderNo = v.OrderNo,
            Scope = scope
        };

        /// <summary>
        /// Author: DatHQ
        /// Description: Mốc khóa đã lưu của biến lấy từ hệ thống, biến của nhóm có tên lấy mốc của nhóm đó; nguồn khác và biến giá trị cố định trả null.
        /// Created date: 29/10/2026
        /// Modified date: 03/11/2026
        /// </summary>
        private static string? ResolveSavedValue(SqlVariableItem v, ShareDataLastSend? lastSend, IReadOnlyList<ShareDataLastSend>? groupCursors)
        {
            if (!string.Equals(v.ValueKind, ShareDataConst.SqlVariableKindBuiltin, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var group = SqlVariableSet.CursorGroupOf(v.Code);
            var groupCursor = group == null ? null : groupCursors?.FirstOrDefault(k => string.Equals(k.CursorKey, group, StringComparison.OrdinalIgnoreCase));
            return string.Equals(v.Value, ShareDataConst.SqlVariableSourceLastKey, StringComparison.OrdinalIgnoreCase)
                ? (group == null ? lastSend?.LastKey : groupCursor?.LastKey)
                : null;
        }

        /// <summary>
        /// Tra đối tác theo ID và gói tin theo mã; thiếu một trong hai thì ném lỗi.
        /// </summary>
        public async Task<(ShareDataPartner Partner, ShareDataPacket Packet)> ResolvePartnerAndPacket(string? partnerId, string? packetCode)
        {
            var id = partnerId?.Trim() ?? string.Empty;
            var code = packetCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            var partner = await _db.Queryable<ShareDataPartner>()
                .Where(p => p.IsDelete == null && p.ID == id)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PartnerId]);

            var packet = await _db.Queryable<ShareDataPacket>()
                .Where(p => p.IsDelete == null && p.Code == code)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, code);

            return (partner, packet);
        }

        /// <summary>
        /// Dựng cấu hình câu SQL mẫu chung (kèm danh sách trường) từ gói tin và dòng mẫu chung ShareDataSql.
        /// Modified date: 03/11/2026
        /// </summary>
        public static PacketExtractSqlOutput BuildExtractOutput(ShareDataPacket packet, ShareDataSql? commonSql, List<SqlFieldItem>? fields = null) => new()
        {
            ID = packet.ID ?? string.Empty,
            PacketCode = packet.Code ?? string.Empty,
            ExtractSql = commonSql?.ExtractSql ?? string.Empty,
            Status = packet.Status ?? BaseEnums.StatusEnum.Enable,
            Remark = packet.Remark,
            Fields = fields ?? SqlFieldHelper.Read(commonSql?.FieldsJson),
            DescribedTime = commonSql?.DescribedTime,
            CreateTime = packet.CreateTime,
            UpdateTime = packet.UpdateTime
        };

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm tra tính an toàn của câu lệnh SQL bất đồng bộ, đối chiếu danh mục bảng nguồn (IsQueryable = true) và trích xuất danh sách tên cột.
        ///              variables: bộ biến đã khai ở bảng tham số câu truy vấn của câu SQL; câu SQL chỉ được dùng các biến này.
        /// Created date: 18/10/2026
        /// Modified date: 25/10/2026 (Nhận bộ biến đã khai, bỏ biến hệ thống)
        /// </summary>
        public async Task<SqlExtractionResult> ValidateSql(ISqlSugarClient? db, string sql, SqlVariableSet? variables = null)
        {
            var allowedTables = await GetQueryableTableSet();
            return SqlValidatorHelper.ValidateAndExtractColumns(db, sql, allowedTables, variables);
        }

        /// <summary>
        /// Lấy danh sách bảng nguồn CSDL cho phép trích xuất (Whitelist) từ bảng ShareDataSourceTable (IsQueryable = true).
        /// </summary>
        public async Task<List<MetadataTableDto>> GetAllowedTables()
        {
            ValidateSuperAdmin();
            var rows = await LoadSourceTables();

            return rows
                .Where(x => x.IsQueryable == true && !string.IsNullOrWhiteSpace(x.TableName))
                .Select(MapToMetadataTableDto)
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách cột của bảng nguồn CSDL kèm kiểu dữ liệu và mô tả cột trên CSDL.
        /// </summary>
        public async Task<List<MetadataFieldDto>> GetTableFields(string tableId)
        {
            ValidateSuperAdmin();

            if (string.IsNullOrWhiteSpace(tableId))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.TableNameRequired);
            }

            var cleanId = tableId.Trim();

            // Tra bảng theo mã định danh trong danh mục bảng nguồn được phép truy vấn (IsQueryable = true); tên bảng không đi qua tham số
            var sourceTables = await LoadSourceTables();
            var matchedTable = sourceTables.FirstOrDefault(t =>
                t.IsQueryable == true &&
                !string.IsNullOrWhiteSpace(t.TableName) &&
                t.ID == cleanId);
            if (matchedTable == null)
            {
                return new List<MetadataFieldDto>();
            }

            var canonicalTableName = matchedTable.TableName!.Trim();

            // Kiểm tra an toàn tên bảng trong danh mục: chỉ cho phép chữ, số và dấu gạch dưới, cấm bảng hệ thống
            if (!SafeTableNameRegex.IsMatch(canonicalTableName) ||
                canonicalTableName.StartsWith("sys", StringComparison.OrdinalIgnoreCase) ||
                canonicalTableName.StartsWith("information_schema", StringComparison.OrdinalIgnoreCase))
            {
                return new List<MetadataFieldDto>();
            }

            var result = new List<MetadataFieldDto>();
            var seenColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Đọc metadata cột thật từ CSDL
            if (_db != null)
            {
                var dbColumns = _db.DbMaintenance.GetColumnInfosByTableName(canonicalTableName, false);
                if (dbColumns != null && dbColumns.Count > 0)
                {
                    foreach (var col in dbColumns)
                    {
                        var colName = col.DbColumnName;
                        if (string.IsNullOrWhiteSpace(colName) || seenColumns.Contains(colName))
                            continue;

                        seenColumns.Add(colName);

                        result.Add(new MetadataFieldDto
                        {
                            ColumnName = colName,
                            DataType = col.DataType ?? string.Empty,
                            DisplayName = colName,
                            Description = col.ColumnDescription,
                            SuggestedAlias = null,
                            IsNullable = col.IsNullable,
                            IsPrimaryKey = col.IsPrimarykey
                        });
                    }
                }
            }

            if (result.Count == 0)
            {
                _logger?.LogWarning(
                    "⚠️ Bảng nguồn {TableName} có trong ShareDataSourceTable nhưng không đọc được cột trên CSDL, kiểm tra lại cấu hình bảng nguồn.",
                    canonicalTableName);
            }

            return result;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Không phải SuperAdmin thì ném lỗi, dùng cho các API đọc siêu dữ liệu bảng và cột.
        /// Created date: 18/10/2026
        /// </summary>
        private void ValidateSuperAdmin()
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForMetadata);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Áp phần người dùng sửa (nhãn, bắt buộc, bỏ qua, nhóm, ghi chú) lên danh sách trường do bộ dò sinh ra.
        ///              Khoá phải có sẵn trong danh sách; không thêm khoá mới. Trả về các khoá vừa được bật bỏ qua.
        /// Created date: 24/10/2026
        /// </summary>
        public static List<string> ApplyFieldEdits(List<SqlFieldItem> fields, IReadOnlyList<SqlFieldEditInput>? edits)
        {
            var newlySkipped = new List<string>();
            if (edits == null || edits.Count == 0) return newlySkipped;

            var map = fields.ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var edit in edits)
            {
                var key = edit.Key?.Trim() ?? string.Empty;
                if (!map.TryGetValue(key, out var field))
                {
                    continue;
                }

                var label = edit.Label?.Trim();
                var group = edit.Group?.Trim();
                var remark = edit.Remark?.Trim();
                EnsureMaxLength(key, nameof(SqlFieldItem.Label), label, SqlFieldHelper.LabelMaxLength);
                EnsureMaxLength(key, nameof(SqlFieldItem.Group), group, SqlFieldHelper.LabelMaxLength);
                EnsureMaxLength(key, nameof(SqlFieldItem.Remark), remark, SqlFieldHelper.RemarkMaxLength);

                if (edit.Skip && !field.Skip) newlySkipped.Add(field.Key);

                field.Label = string.IsNullOrEmpty(label) ? null : label;
                field.Group = string.IsNullOrEmpty(group) ? null : group;
                field.Remark = string.IsNullOrEmpty(remark) ? null : remark;
                field.Required = edit.Required;
                field.Skip = edit.Skip;

                // Kiểu người dùng chọn: trùng kiểu dò được thì bỏ chọn tay, khác thì giữ và bộ dò không ghi đè nữa
                // Nhận kiểu của bộ dò (string/number/datetime/bool) và kiểu trong cấu hình sharedata_value_type (int, long, float, double, decimal, dateTime, guid...)
                var type = edit.Type?.Trim();
                if (!string.IsNullOrEmpty(type))
                {
                    var allowed = AllowedFieldTypes.FirstOrDefault(t => string.Equals(t, type, StringComparison.OrdinalIgnoreCase))
                        ?? throw Oops.Oh(BaseMsg.ShareData.Validation.SqlFieldTypeInvalid, key, type);
                    var detected = string.IsNullOrWhiteSpace(field.DbType) ? field.Type : SqlFieldHelper.MapDbType(field.DbType);
                    field.TypeOverride = string.Equals(allowed, detected, StringComparison.OrdinalIgnoreCase) ? null : allowed;
                    field.Type = allowed;
                }
            }

            return newlySkipped;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Giá trị dài quá giới hạn thì ném lỗi, câu lỗi nêu khóa trường và tên thuộc tính.
        /// Created date: 26/10/2026
        /// </summary>
        private static void EnsureMaxLength(string key, string name, string? value, int maxLength)
        {
            if (value != null && value.Length > maxLength)
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.SqlFieldValueTooLong, key, name, maxLength);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tìm hồ sơ ánh xạ chiều gửi (Outbound/Both) của gói tin đang dùng các khoá trường cho trước (đọc $field trong TargetShapeJson).
        ///              partnerId có giá trị: chỉ hồ sơ của đối tác đó (dòng SQL riêng). partnerId trống: hồ sơ của các đối tác đang dùng câu mẫu chung
        ///              (bỏ đối tác đã có câu SQL riêng).
        /// Created date: 24/10/2026
        /// </summary>
        public async Task<List<SqlFieldMappingUsage>> FindMappingUsage(ShareDataPacket packet, string? partnerId, IReadOnlyCollection<string> keys)
        {
            if (keys.Count == 0 || string.IsNullOrWhiteSpace(packet.ID)) return [];

            var packetId = packet.ID;
            var packetCode = packet.Code ?? string.Empty;
            var hasPartner = !string.IsNullOrWhiteSpace(partnerId);

            var mappings = await _db.Queryable<ShareDataMapping>()
                .Where(m => m.IsDelete == null
                    && (m.DatatypeId == packetId || m.DatatypeId == packetCode)
                    && (m.Direction == BaseEnums.Direction.Outbound || m.Direction == BaseEnums.Direction.Both))
                .WhereIF(hasPartner, m => m.PartnerId == partnerId)
                .ToListAsync();
            if (mappings.Count == 0) return [];

            if (!hasPartner)
            {
                var customPartnerIds = await LoadCustomPartnerIds(packetId);
                mappings = mappings.Where(m => m.PartnerId == null || !customPartnerIds.Contains(m.PartnerId)).ToList();
            }

            var keySet = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
            var result = new List<SqlFieldMappingUsage>();
            foreach (var mapping in mappings)
            {
                var usedKeys = ShapeReader.ReadShape(mapping.TargetShapeJson).FieldKeys
                    .Where(keySet.Contains)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (usedKeys.Count == 0) continue;

                result.Add(new SqlFieldMappingUsage
                {
                    MappingId = mapping.ID ?? string.Empty,
                    MappingCode = mapping.Code,
                    MappingName = mapping.Name,
                    PartnerId = mapping.PartnerId,
                    Keys = usedKeys
                });
            }

            return result;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chặn bật bỏ qua cho khoá đang được hồ sơ ánh xạ dùng: cột vẫn còn trong câu SQL nên dữ liệu vẫn được gửi,
        ///              bỏ qua lúc này chỉ làm giao diện và dữ liệu gửi đi lệch nhau.
        /// Created date: 24/10/2026
        /// </summary>
        public async Task EnsureSkippedKeysNotInUse(ShareDataPacket packet, string? partnerId, IReadOnlyCollection<string> newlySkipped)
        {
            var usages = await FindMappingUsage(packet, partnerId, newlySkipped);
            var first = usages.FirstOrDefault();
            if (first != null)
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.SqlFieldSkipInUse, first.Keys[0], first.MappingCode ?? first.MappingName ?? first.MappingId);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác đang có mốc nối đuôi (ShareDataLastSend có LastKey) sẽ không dùng được mốc nữa khi câu SQL
        ///              đổi cột mốc (công thức __lastKey, kể cả viết trong câu con, hoặc bảng nguồn của công thức khác giữa oldSql và newSql): mốc cũ không so được với câu mới.
        ///              Dòng chưa có LastKey (Worker tạo ở lượt đầu, chưa gửi được dòng nào) không có mốc để đặt lại nên không tính.
        ///              partnerId có giá trị: chỉ đối tác đó; trống: các đối tác đang dùng câu mẫu chung (bỏ đối tác đã có câu SQL riêng).
        ///              Câu mới không nối đuôi hoặc công thức giữ nguyên thì trả rỗng.
        /// Created date: 29/10/2026
        /// Modified date: 06/11/2026
        /// </summary>
        public async Task<List<CursorResetPartner>> FindCursorResets(ShareDataPacket packet, string? partnerId, string? oldSql, string? newSql)
        {
            if (!CursorFormulaChanged(oldSql, newSql) || string.IsNullOrWhiteSpace(packet.Code))
            {
                return [];
            }

            var packetCode = packet.Code;
            var partnerCodes = await _db.Queryable<ShareDataLastSend>()
                .Where(ls => ls.PacketCode == packetCode && ls.LastKey != null && ls.LastKey != "")
                .Select(ls => ls.PartnerCode)
                .Distinct()
                .ToListAsync();
            if (partnerCodes.Count == 0) return [];

            var hasPartner = !string.IsNullOrWhiteSpace(partnerId);
            var partners = await _db.Queryable<ShareDataPartner>()
                .Where(p => p.IsDelete == null && partnerCodes.Contains(p.Code))
                .WhereIF(hasPartner, p => p.ID == partnerId)
                .ToListAsync();
            if (!hasPartner)
            {
                var customPartnerIds = await LoadCustomPartnerIds(packet.ID);
                partners = partners.Where(p => !customPartnerIds.Contains(p.ID)).ToList();
            }

            return partners
                .Select(p => new CursorResetPartner { PartnerId = p.ID, PartnerCode = p.Code, PartnerName = p.Name })
                .ToList();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Cột mốc của câu mới khác câu cũ ở nhóm mốc mặc định hoặc ở một nhóm mốc có tên của câu mới (nhóm không có ở câu mới thì không tính):
        ///              chữ ký công thức khác, hoặc công thức gốc (kể cả viết trong câu con) hay bảng nguồn của công thức khác.
        /// Created date: 30/10/2026
        /// Modified date: 06/11/2026
        /// </summary>
        private static bool CursorFormulaChanged(string? oldSql, string? newSql)
        {
            var cursorGroups = SqlValidatorHelper.GetCursorGroups(newSql);
            return CursorChanged(null) || cursorGroups.Any(group => CursorChanged(group));

            bool CursorChanged(string? group)
            {
                var newSignature = SqlValidatorHelper.GetCursorSignature(newSql, group);
                var oldSignature = SqlValidatorHelper.GetCursorSignature(oldSql, group);
                if (newSignature != null && newSignature != oldSignature)
                {
                    return true;
                }

                var newIdentity = SqlValidatorHelper.GetCursorIdentity(newSql, group);
                var oldIdentity = SqlValidatorHelper.GetCursorIdentity(oldSql, group);
                return newIdentity != null && newIdentity != oldIdentity;
            }
        }

        /// <summary>
        /// Lấy tập tên các bảng nguồn đang được phép truy vấn (IsQueryable = true), không phân biệt hoa thường.
        /// </summary>
        public async Task<HashSet<string>> GetQueryableTableSet()
        {
            var rows = await LoadSourceTables();
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (row.IsQueryable == true && !string.IsNullOrWhiteSpace(row.TableName))
                {
                    set.Add(row.TableName.Trim());
                }
            }

            return set;
        }

        /// <summary>
        /// Đọc danh mục bảng nguồn từ bảng ShareDataSourceTable (chưa xóa mềm), sắp theo OrderNo.
        /// Nếu bảng chưa tồn tại, 0 dòng hoặc gặp lỗi đọc DB thì trả về danh sách rỗng và log cảnh báo chạy script 03.
        /// </summary>
        public async Task<List<ShareDataSourceTable>> LoadSourceTables()
        {
            try
            {
                if (_db != null && _db.DbMaintenance.IsAnyTable("ShareDataSourceTable", false))
                {
                    var rows = await _db.Queryable<ShareDataSourceTable>()
                        .Where(x => x.IsDelete == null)
                        .OrderBy(x => x.OrderNo)
                        .OrderBy(x => x.ID)
                        .ToListAsync();

                    if (rows != null && rows.Count > 0)
                    {
                        Interlocked.Exchange(ref _emptyCatalogWarned, 0);
                        return rows;
                    }
                }

                WarnEmptyCatalogOnce();
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
            {
                WarnEmptyCatalogOnce();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "⚠️ Không thể đọc danh mục bảng nguồn từ ShareDataSourceTable: {Message}",
                    ex.Message);
            }

            return [];
        }

        /// <summary>Đối tác đã có câu SQL riêng cho gói tin (không còn dùng câu mẫu chung).</summary>
        private async Task<HashSet<string>> LoadCustomPartnerIds(string? packetId) =>
            (await _db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId && s.PartnerId != null && s.ExtractSql != null && s.ExtractSql != "")
                .Select(s => s.PartnerId)
                .ToListAsync())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Author: DatHQ
        /// Description: Cảnh báo bảng ShareDataSourceTable chưa có dữ liệu, chỉ một lần trong vòng đời tiến trình.
        /// Created date: 18/10/2026
        /// </summary>
        private void WarnEmptyCatalogOnce()
        {
            if (Interlocked.Exchange(ref _emptyCatalogWarned, 1) == 0)
            {
                _logger?.LogWarning(
                    "⚠️ Chưa có dữ liệu danh mục bảng nguồn ShareDataSourceTable.");
            }
        }

        /// <summary>
        /// Ánh xạ dòng ShareDataSourceTable sang MetadataTableDto bao gồm danh sách PacketCodes được chuẩn hóa.
        /// </summary>
        private static MetadataTableDto MapToMetadataTableDto(ShareDataSourceTable row)
        {
            var tableName = row.TableName?.Trim() ?? string.Empty;
            var packetCodes = !string.IsNullOrWhiteSpace(row.PacketCodes)
                ? row.PacketCodes
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList()
                : [];

            return new MetadataTableDto
            {
                Id = row.ID ?? string.Empty,
                TableName = tableName,
                DisplayName = !string.IsNullOrWhiteSpace(row.Name) ? row.Name.Trim() : tableName,
                Description = row.Remark,
                DefaultAlias = !string.IsNullOrWhiteSpace(row.DefaultAlias) ? row.DefaultAlias.Trim() : "t",
                PacketCodes = packetCodes
            };
        }
    }
}
