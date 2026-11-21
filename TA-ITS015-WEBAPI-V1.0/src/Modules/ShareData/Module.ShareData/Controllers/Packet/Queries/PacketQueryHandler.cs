using Furion.Localization;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Metadata;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Core.Helpers;
using Module.ShareData.Infrastructure.Services.ShareDataConfig;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Module.ShareData.Infrastructure.Services.ShareDataWrite.Analysis;
using Module.ShareData.Infrastructure.Services.ShareDataWrite;
using Module.ShareData.Infrastructure;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.ShareData.Controllers.Packet.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn Gói tin 
    /// Created date: 2026-08-21
    /// Modified date: 20/10/2026 (Ném lỗi theo BaseMsg, lấy giờ bằng BaseUtil.TimeServer)
    /// </summary>
    public class PacketQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataPacket> _packetRep;
        private readonly ConfigService _configService;
        private readonly UserManager _userManager;
        private readonly ISqlSugarClient _db;
        private readonly PacketSqlService _packetSqlService;
        private readonly WriteSqlService _writeSqlService;

        private sealed record PartnerPacketScope(List<string> PacketIds, HashSet<string> CustomPacketIds);

        public PacketQueryHandler(
            BaseRepository<ShareDataPacket> packetRep,
            ConfigService configService,
            UserManager userManager,
            ISqlSugarClient db,
            PacketSqlService packetSqlService,
            WriteSqlService writeSqlService)
        {
            _packetRep = packetRep;
            _configService = configService;
            _userManager = userManager;
            _db = db;
            _packetSqlService = packetSqlService;
            _writeSqlService = writeSqlService;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách gói tin theo phân trang
        ///              Khi lọc theo đối tác (PartnerId), chỉ trả các gói đối tác đang đăng ký gửi hoặc đã có SQL riêng,
        ///              kèm cờ IsPartnerCustomSql (chỉ SuperAdmin).
        /// Created date: 2026-08-21
        /// Modified date: 23/10/2026 (Lọc theo đối tác)
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPagePacketOutput>> HandleAsync(ShareDataPagePacketInput command)
        {
            var partnerId = command.PartnerId?.Trim();
            var scope = string.IsNullOrWhiteSpace(partnerId) ? null : await ResolvePartnerScopeAsync(partnerId);
            var scopedPacketIds = scope?.PacketIds ?? [];

            var pagedList = await _packetRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                .WhereIF(scope != null, u => SqlFunc.ContainsArray(scopedPacketIds, u.ID))
                .Select(u => new ShareDataPagePacketOutput
                {
                }, true)
                .OrderBuilder(command, "", "OrderNo", false)
                .OrderByIF(string.IsNullOrWhiteSpace(command.Field) || string.Equals(command.Field, "OrderNo", StringComparison.OrdinalIgnoreCase), u => u.Code)
                .ToPagedListAsync(command.Page, command.PageSize);

            if (scope != null)
            {
                foreach (var row in pagedList.Records)
                {
                    row.IsPartnerCustomSql = row.ID != null && scope.CustomPacketIds.Contains(row.ID);
                }
            }

            // Số bảng bị ảnh hưởng ở chiều nhận của từng gói để danh sách hiện "{n} bảng" / "Chỉ gửi"
            var codes = pagedList.Records.Where(r => !string.IsNullOrWhiteSpace(r.Code)).Select(r => r.Code!).ToList();
            // Khối câu ghi nằm ở dòng mẫu chung và dòng riêng đối tác của ShareDataSql: đếm gộp bảng đích của mọi câu ghi
            var writePacketIds = pagedList.Records.Where(r => !string.IsNullOrWhiteSpace(r.ID)).Select(r => r.ID!).ToList();
            if (writePacketIds.Count > 0)
            {
                var writeRows = await _packetRep.Context.Queryable<ShareDataSql>()
                    .Where(s => s.IsDelete == null && SqlFunc.ContainsArray(writePacketIds, s.PacketId))
                    .Where(s => s.WriteSql != null && s.WriteSql != "")
                    .Select(s => new { s.PacketId, s.AffectedTables })
                    .ToListAsync();
                foreach (var row in pagedList.Records)
                {
                    row.WriteSqlCount = writeRows
                        .Where(w => w.PacketId == row.ID)
                        .SelectMany(w => WriteSqlAnalyzer.ParseTableList(w.AffectedTables, null))
                        .DistinctBy(WriteSqlAnalyzer.NormalizeTable, StringComparer.OrdinalIgnoreCase)
                        .Count();
                }
            }

            // Thống kê đối tác đang sử dụng từng gói tin (qua đăng ký hiệu lực hoặc cấu hình SQL riêng)
            var packetIds = pagedList.Records.Where(r => !string.IsNullOrWhiteSpace(r.ID)).Select(r => r.ID!).ToList();
            if (packetIds.Count > 0)
            {
                var db = _packetRep.Context;
                var subscribedPartnerRows = await db.Queryable<ShareDataSubscription>()
                    .Where(s => s.IsDelete == null && s.PartnerId != null)
                    .Where(s => s.State != BaseEnums.SubSubscriptionState.Cancelled
                        && s.State != BaseEnums.SubSubscriptionState.Rejected
                        && s.State != BaseEnums.SubSubscriptionState.Expired)
                    .Where(s => SqlFunc.ContainsArray(packetIds, s.DatatypeId) || (codes.Count > 0 && SqlFunc.ContainsArray(codes, s.DatatypeId)))
                    .Select(s => new { s.DatatypeId, s.PartnerId })
                    .ToListAsync();

                var partnerSqlRows = await db.Queryable<ShareDataSql>()
                    .Where(s => s.IsDelete == null && s.PartnerId != null && SqlFunc.ContainsArray(packetIds, s.PacketId))
                    .Select(s => new { s.PacketId, s.PartnerId, s.ExtractSql })
                    .ToListAsync();

                var allPartnerIds = subscribedPartnerRows.Select(s => s.PartnerId!)
                    .Concat(partnerSqlRows.Select(s => s.PartnerId!))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var partnerLookup = allPartnerIds.Count > 0
                    ? (await db.Queryable<ShareDataPartner>()
                        .Where(p => p.IsDelete == null && SqlFunc.ContainsArray(allPartnerIds, p.ID))
                        .Select(p => new { p.ID, p.Code, p.Name })
                        .ToListAsync())
                        .ToDictionary(p => p.ID, p => !string.IsNullOrWhiteSpace(p.Code) ? p.Code! : (p.Name ?? p.ID), StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in pagedList.Records)
                {
                    var pId = row.ID;
                    var pCode = row.Code;

                    var subPartners = subscribedPartnerRows
                        .Where(s => (pId != null && string.Equals(s.DatatypeId, pId, StringComparison.OrdinalIgnoreCase))
                                 || (pCode != null && string.Equals(s.DatatypeId, pCode, StringComparison.OrdinalIgnoreCase)))
                        .Select(s => s.PartnerId!)
                        .ToList();

                    var sqlPartners = partnerSqlRows
                        .Where(s => pId != null && string.Equals(s.PacketId, pId, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var distinctPids = subPartners
                        .Concat(sqlPartners.Select(s => s.PartnerId!))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    row.PartnerCount = distinctPids.Count;

                    if (distinctPids.Count > 0)
                    {
                        var customPids = sqlPartners
                            .Where(s => !string.IsNullOrWhiteSpace(s.ExtractSql))
                            .Select(s => s.PartnerId!)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        row.PartnerNames = distinctPids.Select(pid => partnerLookup.TryGetValue(pid, out var n) ? n : pid).ToList();
                    }
                }
            }

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách gói tin theo tiêu chí lọc (không phân trang)
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<List<ShareDataPagePacketOutput>> HandleAsync(ShareDataPacketInput command)
        {
            var list = await _packetRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code == command.Code)
                .Select(u => new ShareDataPagePacketOutput() { }, true)
                .OrderBy(u => u.OrderNo)
                .OrderBy(u => u.Code)
                .ToListAsync();
            return list;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 gói tin theo ID
        /// Created date: 2026-09-17
        /// </summary>
        public async Task<ShareDataPacketOutput> HandleAsync(ShareDataIdPacketInput command)
        {
            return await _packetRep.AsQueryable()
                .Where(u => u.ID == command.ID)
                .Select(u => new ShareDataPacketOutput() { }, true)
                .FirstAsync()
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lấy dữ liệu mẫu THẬT của một gói tin theo câu truy vấn ExtractSql hiệu lực.
        /// Created date: 2026-09-22
        /// Modified date: 09/11/2026
        /// </summary>
        public async Task<ShareDataPacketSampleDataOutput> HandleAsync(ShareDataPacketSampleDataInput command)
        {
            if (string.IsNullOrWhiteSpace(command.DatatypeId) && string.IsNullOrWhiteSpace(command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var packet = await _packetRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.DatatypeId), u => u.ID == command.DatatypeId)
                .WhereIF(string.IsNullOrWhiteSpace(command.DatatypeId), u => u.Code == command.Code)
                .FirstAsync()
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            string? partnerId = null;
            if (!string.IsNullOrWhiteSpace(command.PartnerCode))
            {
                var partnerCode = command.PartnerCode.Trim();
                var partner = await _packetRep.Context.Queryable<ShareDataPartner>()
                    .Where(p => p.IsDelete == null && p.Code == partnerCode)
                    .FirstAsync();
                partnerId = partner?.ID;
            }

            var sqlConfig = await SqlTextHelper.ResolveSqlConfig(_packetRep.Context, packet, partnerId);
            var sql = sqlConfig.ExtractSql;

            if (string.IsNullOrWhiteSpace(sql))
                throw Oops.Oh(BaseMsg.ShareData.Exception.PacketExtractSqlMissing, packet.Code ?? string.Empty);

            var variables = await SqlTextHelper.ResolveVariables(_packetRep.Context, packet.ID, partnerId);
            var validation = SqlValidatorHelper.Validate(sql, variables);
            if (!validation.IsValid)
            {
                throw SqlValidatorHelper.ToException(validation.ErrorKey, validation.ErrorArgs);
            }

            var limit = await _configService.Get();
            var requestedTop = command.Top is > 0 ? command.Top.Value : limit.MappingSampleTop.Default;
            var safeTop = Math.Clamp(requestedTop, limit.MappingSampleTop.Min, limit.MappingSampleTop.Max);
            var cleanSql = sql.TrimEnd(';');
            var isSnapshot = !SqlValidatorHelper.IsIncrementalSql(sql, variables.CursorNames);

            var cursorContext = new SqlVariableContext(SqlParameterHelper.ResolveCursor(isSnapshot, null));
            var parameters = SqlParameterHelper.BuildParameters(variables, cursorContext);

            // Màn ánh xạ chỉ cần vài dòng: biến cỡ trang (@pageSize) lấy đúng số dòng mẫu để CSDL không trả thừa,
            // và đọc tối đa safeTop dòng phòng câu SQL không dùng biến cỡ trang.
            foreach (var parameter in parameters)
            {
                if (string.Equals(parameter.ParameterName.TrimStart('@'), ShareDataConst.SqlVariablePageSize, StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Value = safeTop;
                }
            }

            var columns = new List<string>();
            var rows = new List<Dictionary<string, object?>>();

            using (var reader = await _packetRep.Context.Ado.GetDataReaderAsync(cleanSql, parameters))
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    columns.Add(reader.GetName(i));
                }

                while (rows.Count < safeTop && reader.Read())
                {
                    var item = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        item[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    rows.Add(item);
                }
            }

            return new ShareDataPacketSampleDataOutput
            {
                DatatypeId = packet.ID,
                PacketCode = packet.Code,
                Columns = columns,
                Rows = rows,
                RowCount = rows.Count,
                Sql = cleanSql
            };
        }

        /// <summary>
        /// Các gói tin thuộc phạm vi của một đối tác: gói đối tác đăng ký gửi còn hiệu lực (khớp theo ID hoặc mã gói)
        /// cộng các gói đã có SQL riêng, cùng tập gói đang dùng SQL riêng. Cùng điều kiện với danh sách đối tác của SQL Center.
        /// </summary>
        private async Task<PartnerPacketScope> ResolvePartnerScopeAsync(string partnerId)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var db = _packetRep.Context;
            var subscribedTypes = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.IsDelete == null && s.PartnerId == partnerId)
                .Where(s => s.Direction == BaseEnums.Direction.Outbound || s.Direction == BaseEnums.Direction.Both)
                .Where(s => s.State != BaseEnums.SubSubscriptionState.Cancelled
                    && s.State != BaseEnums.SubSubscriptionState.Rejected
                    && s.State != BaseEnums.SubSubscriptionState.Expired)
                .Select(s => s.DatatypeId)
                .ToListAsync();

            var partnerSqls = await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PartnerId == partnerId)
                .ToListAsync();

            var customPacketIds = partnerSqls
                .Where(s => SqlTextHelper.IsCustom(s) && !string.IsNullOrWhiteSpace(s.PacketId))
                .Select(s => s.PacketId!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var types = subscribedTypes
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var packetIds = partnerSqls
                .Where(s => !string.IsNullOrWhiteSpace(s.PacketId))
                .Select(s => s.PacketId!)
                .ToList();

            if (types.Count > 0)
            {
                // DatatypeId của đăng ký có thể là ID hoặc mã gói tin.
                var matched = await _packetRep.AsQueryable()
                    .Where(p => p.IsDelete == null && (SqlFunc.ContainsArray(types, p.ID) || SqlFunc.ContainsArray(types, p.Code)))
                    .Select(p => p.ID)
                    .ToListAsync();
                packetIds.AddRange(matched.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!));
            }

            var distinctIds = packetIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinctIds.Count == 0)
            {
                // Danh sách rỗng khiến điều kiện IN bị bỏ; thêm giá trị không bao giờ trùng ID để kết quả rỗng.
                distinctIds.Add(string.Empty);
            }

            return new PartnerPacketScope(distinctIds, customPacketIds);
        }

        #region Câu truy vấn SQL trích xuất, dò cột, dữ liệu mẫu
        /// <summary>
        /// Biến đã khai của gói tin (theo mã) cho đối tác (nếu có); không truyền mã gói hoặc gói không tồn tại thì không có biến nào.
        /// </summary>
        private async Task<SqlVariableSet> LoadVariablesAsync(string? packetCode, string? partnerId)
        {
            var code = packetCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                return SqlVariableSet.Empty;
            }

            var packetId = await _packetRep.AsQueryable()
                .Where(x => x.IsDelete == null && x.Code == code)
                .Select(x => x.ID)
                .FirstAsync();

            // Đang soạn SQL riêng: bộ biến của đối tác, chưa có (sắp tạo SQL riêng) thì bộ biến mẫu chung sẽ được chép sang
            return SqlVariableSet.From(await SqlTextHelper.LoadEditingVariableRows(_db, packetId, partnerId?.Trim()));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc câu SQL mẫu chung của gói tin. Chỉ SuperAdmin.
        /// Created date: 19/10/2026
        /// </summary>
        public async Task<PacketExtractSqlOutput> HandleAsync(GetPacketExtractSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var packetCode = command.PacketCode?.Trim() ?? string.Empty;

            var packet = await _packetRep.AsQueryable()
                .Where(x => x.IsDelete == null && x.Code == packetCode)
                .FirstAsync();

            if (packet == null)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, packetCode);
            }

            // Câu SQL mẫu chung chỉ có một nguồn là ShareDataSql (đã bỏ TemplateSql của gói)
            var commonSql = await SqlTextHelper.SelectCommonSql(_db, packet.ID);
            return PacketSqlService.BuildExtractOutput(packet, commonSql);
        }

        /// <summary>
        /// Kiểm tra cú pháp SQL và lấy danh sách cột nguồn phục vụ giao diện cấu hình ánh xạ.
        /// </summary>
        public async Task<ParsePacketColumnsOutput> HandleAsync(ParsePacketColumnsInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlValidate);
            }

            var variables = await LoadVariablesAsync(command.PacketCode, command.PartnerId);
            var validation = await _packetSqlService.ValidateSql(_db, command.ExtractSql, variables);
            string? parseError = null;
            int? errorLine = null;
            int? errorColumn = null;
            if (!validation.IsValid)
            {
                if (SqlValidatorHelper.TryLocateError(command.ExtractSql, validation.ErrorKey, validation.ErrorArgs, out var line, out var column))
                {
                    errorLine = line;
                    errorColumn = column;
                }
                var localized = L.Text[validation.ErrorKey ?? BaseMsg.ShareData.Validation.SqlScriptInvalid, validation.ErrorArgs ?? []].Value;
                parseError = (string.IsNullOrWhiteSpace(localized) || string.Equals(localized, validation.ErrorKey, StringComparison.OrdinalIgnoreCase))
                    ? (validation.ErrorMessage ?? validation.ErrorKey)
                    : localized;
            }
            return new ParsePacketColumnsOutput
            {
                IsValid = validation.IsValid,
                ErrorMessage = parseError,
                ErrorLine = errorLine,
                ErrorColumn = errorColumn,
                Columns = validation.Columns
            };
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dò cột trả về của câu SQL đang soạn (chưa lưu) và đối chiếu với danh sách trường đang lưu: khoá mới, khoá mất, khoá đổi kiểu,
        ///              cùng các hồ sơ ánh xạ chiều gửi đang dùng khoá mất. PartnerId trống = câu mẫu chung; có giá trị = câu SQL riêng của đối tác
        ///              (đối chiếu với trường đối tác đang dùng, khoá mới lấy nhãn theo trường mẫu chung). Không ghi dữ liệu.
        /// Created date: 24/10/2026
        /// </summary>
        public async Task<DescribeColumnsOutput> HandleAsync(DescribeColumnsInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlValidate);
            }

            var packetCode = command.PacketCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(packetCode))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            var packet = await _packetRep.AsQueryable()
                .Where(x => x.IsDelete == null && x.Code == packetCode)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, packetCode);

            var partnerId = command.PartnerId?.Trim();
            var hasPartner = !string.IsNullOrWhiteSpace(partnerId);
            if (hasPartner && !await _db.Queryable<ShareDataPartner>().AnyAsync(p => p.IsDelete == null && p.ID == partnerId))
            {
                throw Oops.Oh(BaseMsg.BaseException.NotExist, L.Text[BaseMsg.ShareData.Entity.PartnerId]);
            }

            var sqlConfig = await SqlTextHelper.ResolveSqlConfig(_db, packet, hasPartner ? partnerId : null);
            var commonFields = SqlFieldHelper.Read(sqlConfig.Common?.FieldsJson);
            var currentFields = !hasPartner
                ? commonFields
                : sqlConfig.IsCustom
                    ? SqlFieldHelper.Read(sqlConfig.Partner!.FieldsJson)
                    : commonFields.Where(f => !f.Lost).ToList();

            // Dò theo bộ biến của câu đang soạn (SQL riêng của đối tác hoặc mẫu chung)
            var variables = await LoadVariablesAsync(packetCode, hasPartner ? partnerId : null);
            var validation = await _packetSqlService.ValidateSql(_db, command.ExtractSql, variables);
            if (!validation.IsValid)
            {
                var (errorMessage, errorLine, errorColumn) = DescribeValidationError(command.ExtractSql, validation);
                return new DescribeColumnsOutput
                {
                    IsValid = false,
                    ErrorMessage = errorMessage,
                    ErrorLine = errorLine,
                    ErrorColumn = errorColumn,
                    Fields = currentFields
                };
            }

            var merge = SqlFieldHelper.Merge(currentFields, validation.ColumnInfos, hasPartner ? commonFields : null);
            return new DescribeColumnsOutput
            {
                IsValid = true,
                Fields = merge.Fields,
                Added = merge.Added,
                Lost = merge.Lost,
                TypeChanged = merge.TypeChanged,
                AffectedMappings = await _packetSqlService.FindMappingUsage(packet, hasPartner ? partnerId : null, merge.Lost)
            };
        }

        /// <summary>
        /// Câu báo lỗi (đã dịch, chưa có bản dịch thì dùng bản tiếng Việt chuẩn) và vị trí lỗi trong câu SQL để giao diện đánh dấu.
        /// </summary>
        private static (string? Message, int? Line, int? Column) DescribeValidationError(string sql, SqlExtractionResult validation)
        {
            if (validation.IsValid) return (null, null, null);

            int? errorLine = null;
            int? errorColumn = null;
            if (SqlValidatorHelper.TryLocateError(sql, validation.ErrorKey, validation.ErrorArgs, out var line, out var column))
            {
                errorLine = line;
                errorColumn = column;
            }

            var localized = L.Text[validation.ErrorKey ?? BaseMsg.ShareData.Validation.SqlScriptInvalid, validation.ErrorArgs ?? []].Value;
            var message = (string.IsNullOrWhiteSpace(localized) || string.Equals(localized, validation.ErrorKey, StringComparison.OrdinalIgnoreCase))
                ? (validation.ErrorMessage ?? validation.ErrorKey)
                : localized;
            return (message, errorLine, errorColumn);
        }

        /// <summary>
        /// Chạy thử câu truy vấn SQL trích xuất và trả về danh sách dữ liệu mẫu theo giới hạn cấu hình sharedata_config.
        /// </summary>
        public async Task<ExecutePacketSqlSampleOutput> HandleAsync(ExecutePacketSqlSampleInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlPreview);
            }

            var variables = await LoadVariablesAsync(command.PacketCode, command.PartnerId);
            var validation = await _packetSqlService.ValidateSql(_db, command.ExtractSql, variables);
            if (!validation.IsValid)
            {
                var localized = L.Text[validation.ErrorKey ?? BaseMsg.ShareData.Validation.SqlScriptInvalid, validation.ErrorArgs ?? []].Value;
                var sampleError = (string.IsNullOrWhiteSpace(localized) || string.Equals(localized, validation.ErrorKey, StringComparison.OrdinalIgnoreCase))
                    ? (validation.ErrorMessage ?? validation.ErrorKey)
                    : localized;
                return new ExecutePacketSqlSampleOutput
                {
                    IsSuccess = false,
                    ErrorMessage = sampleError
                };
            }

            try
            {
                var limit = await _configService.Get();
                var requestedTop = command.Top > 0 ? command.Top : limit.SqlSampleTop.Default;
                var safeTop = Math.Clamp(requestedTop, limit.SqlSampleTop.Min, limit.SqlSampleTop.Max);
                var cleanSql = command.ExtractSql.Trim().TrimEnd(';');

                // Chạy thử như lần gửi đầu tiên: chưa có mốc lần trước
                var cursorContext = new SqlVariableContext(SqlParameterHelper.ResolveCursor(false, null));
                var parameters = SqlParameterHelper.BuildParameters(variables, cursorContext);

                using var reader = await _db.Ado.GetDataReaderAsync(cleanSql, parameters);
                var fieldCount = reader.FieldCount;
                var resolvedColumns = new List<string>(fieldCount);
                var colCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < fieldCount; i++)
                {
                    var colName = reader.GetName(i);
                    if (string.IsNullOrWhiteSpace(colName))
                    {
                        colName = $"Column_{i + 1}";
                    }

                    if (colCountMap.TryGetValue(colName, out var count))
                    {
                        count++;
                        colCountMap[colName] = count;
                        resolvedColumns.Add($"{colName}_{count}");
                    }
                    else
                    {
                        colCountMap[colName] = 1;
                        resolvedColumns.Add(colName);
                    }
                }

                var rows = new List<Dictionary<string, object?>>();
                var readCount = 0;

                if (reader is System.Data.Common.DbDataReader dbReader)
                {
                    while (readCount < safeTop && await dbReader.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(fieldCount);
                        for (var i = 0; i < fieldCount; i++)
                        {
                            row[resolvedColumns[i]] = dbReader.IsDBNull(i) ? null : dbReader.GetValue(i);
                        }
                        rows.Add(row);
                        readCount++;
                    }
                }
                else
                {
                    while (readCount < safeTop && reader.Read())
                    {
                        var row = new Dictionary<string, object?>(fieldCount);
                        for (var i = 0; i < fieldCount; i++)
                        {
                            row[resolvedColumns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        rows.Add(row);
                        readCount++;
                    }
                }

                return new ExecutePacketSqlSampleOutput
                {
                    IsSuccess = true,
                    Columns = resolvedColumns.Count > 0 ? resolvedColumns : validation.Columns,
                    Rows = rows
                };
            }
            catch (Exception ex)
            {
                return new ExecutePacketSqlSampleOutput
                {
                    IsSuccess = false,
                    ErrorMessage = L.Text[BaseMsg.ShareData.Exception.SqlSampleExecutionFailed, ex.Message].Value
                };
            }
        }

        /// <summary>
        /// Xử lý lấy danh mục bảng Whitelist an toàn.
        /// </summary>
        public async Task<List<MetadataTableDto>> HandleAsync(GetMetadataTablesInput input)
        {
            return await _packetSqlService.GetAllowedTables();
        }

        /// <summary>
        /// Xử lý lấy danh sách cột của bảng nguồn cụ thể.
        /// </summary>
        public async Task<List<MetadataFieldDto>> HandleAsync(GetMetadataFieldsInput input)
        {
            return await _packetSqlService.GetTableFields(input.Id);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Danh sách trường dùng được của gói tin cho màn ánh xạ: trường của câu SQL riêng nếu đối tác có câu riêng,
        ///              không thì trường của câu mẫu chung; bỏ trường đã mất khỏi câu SQL và trường người dùng đánh Bỏ qua.
        /// Created date: 26/10/2026
        /// </summary>
        public async Task<List<SqlFieldItem>> HandleAsync(GetPacketFieldsInput command)
        {
            var datatypeId = command.DatatypeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(datatypeId))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            var packet = await _packetRep.AsQueryable()
                .Where(x => x.IsDelete == null && (x.ID == datatypeId || x.Code == datatypeId))
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, datatypeId);

            var partnerId = command.PartnerId?.Trim();
            var sqlConfig = await SqlTextHelper.ResolveSqlConfig(_db, packet, string.IsNullOrWhiteSpace(partnerId) ? null : partnerId);
            var fields = SqlFieldHelper.Read(sqlConfig.IsCustom ? sqlConfig.Partner!.FieldsJson : sqlConfig.Common?.FieldsJson);
            return fields.Where(f => !f.Lost && !f.Skip).ToList();
        }

        /// <summary>
        /// Lấy danh sách đối tác có thể cấu hình câu truy vấn riêng trên một gói tin (1 dòng/đối tác) kèm trạng thái câu riêng:
        /// đối tác có đăng ký gửi gói này còn hiệu lực, hoặc đã có câu riêng. Không đòi hồ sơ ánh xạ.
        /// </summary>
        public async Task<List<PartnerSqlListItemOutput>> HandleAsync(GetPartnerSqlListInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var packetCode = command.PacketCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(packetCode))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            var packet = await _packetRep.AsQueryable()
                .Where(x => x.IsDelete == null && x.Code == packetCode)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, packetCode);

            var packetId = packet.ID;
            var subscribedPartnerIds = await _db.Queryable<ShareDataSubscription>()
                .Where(s => s.IsDelete == null)
                .Where(s => s.Direction == BaseEnums.Direction.Outbound || s.Direction == BaseEnums.Direction.Both)
                .Where(s => s.DatatypeId == packetId || s.DatatypeId == packetCode)
                .Where(s => s.State != BaseEnums.SubSubscriptionState.Cancelled
                    && s.State != BaseEnums.SubSubscriptionState.Rejected
                    && s.State != BaseEnums.SubSubscriptionState.Expired)
                .Select(s => s.PartnerId)
                .ToListAsync();

            var partnerSqls = await _db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId && s.PartnerId != null)
                .ToListAsync();

            var partnerIds = subscribedPartnerIds
                .Concat(partnerSqls.Select(s => s.PartnerId))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (partnerIds.Count == 0)
            {
                return [];
            }

            var partners = await _db.Queryable<ShareDataPartner>()
                .Where(p => p.IsDelete == null && partnerIds.Contains(p.ID))
                .ToListAsync();

            var sqlByPartner = partnerSqls
                .Where(s => !string.IsNullOrWhiteSpace(s.PartnerId))
                .ToDictionary(s => s.PartnerId!, s => s, StringComparer.OrdinalIgnoreCase);

            var result = new List<PartnerSqlListItemOutput>();
            foreach (var partner in partners)
            {
                if (string.IsNullOrWhiteSpace(partner.ID))
                {
                    continue;
                }

                sqlByPartner.TryGetValue(partner.ID, out var partnerSql);

                result.Add(new PartnerSqlListItemOutput
                {
                    PartnerId = partner.ID,
                    PartnerCode = partner.Code ?? string.Empty,
                    PartnerName = partner.Name ?? string.Empty,
                    IsCustom = SqlTextHelper.IsCustom(partnerSql),
                    UpdateTime = partnerSql?.UpdateTime ?? partnerSql?.CreateTime
                });
            }

            return result
                .OrderBy(x => x.IsCustom ? (x.UpdateTime ?? DateTime.MaxValue) : DateTime.MaxValue)
                .ThenBy(x => x.PartnerName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.PartnerCode, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Lấy chi tiết câu truy vấn trích xuất riêng của đối tác kèm kết quả đối chiếu với gói tin chuẩn.
        /// </summary>
        public async Task<PartnerExtractSqlOutput> HandleAsync(GetPartnerExtractSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var (partner, packet) = await _packetSqlService.ResolvePartnerAndPacket(command.PartnerId, command.PacketCode);
            var sqlConfig = await SqlTextHelper.ResolveSqlConfig(_db, packet, partner.ID);
            var partnerSql = sqlConfig.Partner;
            var isCustom = SqlTextHelper.IsCustom(partnerSql);

            return new PartnerExtractSqlOutput
            {
                PartnerId = partner.ID ?? string.Empty,
                PartnerCode = partner.Code ?? string.Empty,
                PartnerName = partner.Name ?? string.Empty,
                PacketCode = packet.Code ?? string.Empty,
                StandardSql = sqlConfig.Common?.ExtractSql ?? string.Empty,
                CustomSql = partnerSql?.ExtractSql,
                IsCustom = isCustom,
                HasDifferences = SqlTextHelper.HasDifferences(partnerSql, sqlConfig.Common?.ExtractSql),
                Fields = isCustom ? SqlFieldHelper.Read(partnerSql!.FieldsJson) : [],
                CommonFields = SqlFieldHelper.Read(sqlConfig.Common?.FieldsJson),
                DescribedTime = isCustom ? partnerSql!.DescribedTime : null,
            };
        }

        #endregion

        #region Tham số câu truy vấn (VariablesJson)
        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc bảng tham số của câu SQL theo mẫu chung hoặc đối tác. Chỉ SuperAdmin.
        /// Created date: 26/10/2026
        /// </summary>
        public async Task<PacketSqlVariablesOutput> HandleAsync(GetPacketSqlVariablesInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var packetCode = command.PacketCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(packetCode))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            var packet = await _db.Queryable<ShareDataPacket>()
                .Where(x => x.IsDelete == null && x.Code == packetCode)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, packetCode);

            var partnerId = string.IsNullOrWhiteSpace(command.PartnerId) ? null : command.PartnerId.Trim();
            return new PacketSqlVariablesOutput
            {
                PacketCode = packet.Code ?? packetCode,
                PartnerId = partnerId,
                Items = await _packetSqlService.LoadItems(packet.ID, partnerId)
            };
        }

        #endregion

        #region Khối câu ghi chiều nhận (WriteSql)
        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc câu ghi mẫu chung của gói tin. Chỉ SuperAdmin.
        /// Created date: 27/10/2026
        /// </summary>
        public async Task<PacketWriteSqlsOutput> HandleAsync(GetPacketWriteSqlsInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var packet = await _writeSqlService.FindPacket(command.PacketCode);
            return await _writeSqlService.LoadOutput(packet);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy câu ghi chiều nhận riêng của đối tác kèm câu ghi mẫu chung (giống GetPartnerExtractSql).
        /// Created date: 30/10/2026
        /// </summary>
        public async Task<List<PartnerSqlListItemOutput>> HandleAsync(GetPartnerWriteSqlListInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var packet = await _writeSqlService.FindPacket(command.PacketCode);
            return await _writeSqlService.LoadPartnerList(packet);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đọc câu ghi của đối tác kèm câu mẫu chung để đối chiếu. Chỉ SuperAdmin.
        /// Created date: 30/10/2026
        /// </summary>
        public async Task<PartnerWriteSqlOutput> HandleAsync(GetPartnerWriteSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var (partner, packet) = await _packetSqlService.ResolvePartnerAndPacket(command.PartnerId, command.PacketCode);
            return await _writeSqlService.LoadPartnerOutput(packet, partner);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm khối câu ghi chiều nhận, không lưu. Chỉ SuperAdmin.
        /// Created date: 27/10/2026
        /// </summary>
        public async Task<ValidateWriteSqlOutput> HandleAsync(ValidateWriteSqlInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlValidate);
            }

            var packet = await _writeSqlService.FindPacket(command.PacketCode);
            var partner = await _writeSqlService.FindPartner(command.PartnerId);
            return await _writeSqlService.Validate(packet, partner?.ID, command.AffectedTables, command.WriteSql);
        }

        #endregion

        #region Chi tiết SQL mẫu chung của gói (một lần gọi)

        /// <summary>
        /// Lấy gọn câu SQL mẫu chung (kèm trường), bộ biến mẫu chung và khối câu ghi của gói: đọc gói và dòng mẫu chung ShareDataSql một lần
        /// thay cho ba lần gọi GetExtractSql, GetSqlVariables và GetWriteSqls khi mở gói.
        /// </summary>
        public async Task<PacketSqlDetailOutput> HandleAsync(GetPacketSqlDetailInput command)
        {
            if (!_userManager.SuperAdmin)
            {
                throw Oops.Oh(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView);
            }

            var packet = await _writeSqlService.FindPacket(command.PacketCode);
            var commonSql = await SqlTextHelper.SelectCommonSql(_db, packet.ID);
            var packetCode = packet.Code ?? string.Empty;

            return new PacketSqlDetailOutput
            {
                Extract = PacketSqlService.BuildExtractOutput(packet, commonSql),
                Variables = new PacketSqlVariablesOutput
                {
                    PacketCode = packetCode,
                    Items = SqlTextHelper.ReadVariables(commonSql?.VariablesJson)
                        .Select(v => PacketSqlService.ToVariableItem(v, ShareDataConst.SqlVariableScopeCommon, null))
                        .ToList()
                },
                Write = WriteSqlService.BuildWriteOutput(packetCode, commonSql?.WriteSql, commonSql?.AffectedTables, commonSql?.WriteRemark)
            };
        }

        #endregion
    }
}
