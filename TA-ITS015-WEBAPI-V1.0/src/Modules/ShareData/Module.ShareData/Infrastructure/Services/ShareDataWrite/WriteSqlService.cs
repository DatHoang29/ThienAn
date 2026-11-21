using System.Data;
using System.Text.Json;
using Furion.Localization;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Core.Helpers;
using Module.ShareData.Infrastructure.Services.ShareDataWrite.Analysis;
using Shared.DTO.Constants.Its015;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;
using JsonException = System.Text.Json.JsonException;

namespace Module.ShareData.Infrastructure.Services.ShareDataWrite
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Kiểm và chạy thử khối câu ghi chiều nhận (ShareDataSql.WriteSql) của gói tin:
    ///              - Kiểm: phân tích tĩnh (WriteSqlAnalyzer), rồi kiểm các bảng bị ảnh hưởng có thật và khối câu biên dịch được trên CSDL nhận
    ///                (sys.dm_exec_describe_first_result_set, không chạy dữ liệu).
    ///              - Chạy thử: chạy thật khối câu ghi trên CSDL nhận trong một giao dịch rồi luôn quay lui, CSDL không đổi.
    /// Created date: 26/10/2026
    /// </summary>
    public class WriteSqlService : IScoped
    {
        /// <summary>Giá trị @operator khi chạy thử (Worker dùng SHAREDATA_INBOUND).</summary>
        private const string TryOperator = "SHAREDATA_TRY";

        private const string DescribeErrorSql = @"SELECT TOP 1 error_message
FROM sys.dm_exec_describe_first_result_set(@tsql, @params, 0)
WHERE error_number IS NOT NULL";

        private readonly ISqlSugarClient _db;

        public WriteSqlService(ISqlSugarClient db)
        {
            _db = db;
        }

        /// <summary>Tra gói tin theo mã; không có thì ném lỗi.</summary>
        public async Task<ShareDataPacket> FindPacket(string? packetCode)
        {
            var code = packetCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code))
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.PacketCodeRequired);
            }

            return await _db.Queryable<ShareDataPacket>()
                .Where(p => p.IsDelete == null && p.Code == code)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.ShareData.Exception.PacketCodeNotFound, code);
        }

        /// <summary>Tra đối tác theo ID (câu ghi riêng đối tác); trống trả null = mẫu chung, không có thì ném lỗi.</summary>
        public async Task<ShareDataPartner?> FindPartner(string? partnerId)
        {
            if (string.IsNullOrWhiteSpace(partnerId)) return null;

            return await _db.Queryable<ShareDataPartner>()
                .Where(p => p.IsDelete == null && p.ID == partnerId)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
        }

        /// <summary>Đọc khối câu ghi ở dòng mẫu chung ShareDataSql của gói.</summary>
        public async Task<PacketWriteSqlsOutput> LoadOutput(ShareDataPacket packet)
        {
            var row = await SqlTextHelper.SelectCommonSql(_db, packet.ID);
            return BuildWriteOutput(packet.Code ?? string.Empty, row?.WriteSql, row?.AffectedTables, row?.WriteRemark);
        }

        /// <summary>
        /// Đối tác có thể cấu hình câu ghi riêng trên gói (giống danh sách đối tác của chiều gửi, nhưng theo chiều nhận):
        /// đối tác có đăng ký nhận (Inbound/Both) còn hiệu lực, có hồ sơ ánh xạ chiều nhận (Inbound/Both), hoặc đã có câu ghi riêng.
        /// IsCustom = đã có câu ghi riêng. Thứ tự: đối tác có câu ghi riêng theo thời điểm tạo/sửa, rồi theo tên.
        /// </summary>
        public async Task<List<PartnerSqlListItemOutput>> LoadPartnerList(ShareDataPacket packet)
        {
            var packetId = packet.ID;
            var packetCode = packet.Code;

            var subscribedPartnerIds = await _db.Queryable<ShareDataSubscription>()
                .Where(s => s.IsDelete == null)
                .Where(s => s.Direction == BaseEnums.Direction.Inbound || s.Direction == BaseEnums.Direction.Both)
                .Where(s => s.DatatypeId == packetId || s.DatatypeId == packetCode)
                .Where(s => s.State != BaseEnums.SubSubscriptionState.Cancelled
                    && s.State != BaseEnums.SubSubscriptionState.Rejected
                    && s.State != BaseEnums.SubSubscriptionState.Expired)
                .Select(s => s.PartnerId)
                .ToListAsync();

            var mappedPartnerIds = await _db.Queryable<ShareDataMapping>()
                .Where(m => m.IsDelete == null && m.DatatypeId == packetId)
                .Where(m => m.Direction == BaseEnums.Direction.Inbound || m.Direction == BaseEnums.Direction.Both)
                .Select(m => m.PartnerId)
                .ToListAsync();

            var partnerSqls = await _db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId && s.PartnerId != null)
                .ToListAsync();
            var customWriteByPartner = partnerSqls
                .Where(s => !string.IsNullOrWhiteSpace(s.PartnerId) && !string.IsNullOrWhiteSpace(s.WriteSql))
                .ToDictionary(s => s.PartnerId!, s => s, StringComparer.OrdinalIgnoreCase);

            var partnerIds = subscribedPartnerIds
                .Concat(mappedPartnerIds)
                .Concat(customWriteByPartner.Keys)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (partnerIds.Count == 0) return [];

            var partners = await _db.Queryable<ShareDataPartner>()
                .Where(p => p.IsDelete == null && partnerIds.Contains(p.ID))
                .ToListAsync();

            return partners
                .Where(p => !string.IsNullOrWhiteSpace(p.ID))
                .Select(p =>
                {
                    customWriteByPartner.TryGetValue(p.ID, out var row);
                    return new PartnerSqlListItemOutput
                    {
                        PartnerId = p.ID,
                        PartnerCode = p.Code ?? string.Empty,
                        PartnerName = p.Name ?? string.Empty,
                        IsCustom = row != null,
                        UpdateTime = row?.UpdateTime ?? row?.CreateTime
                    };
                })
                .OrderBy(x => x.IsCustom ? (x.UpdateTime ?? DateTime.MaxValue) : DateTime.MaxValue)
                .ThenBy(x => x.PartnerName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.PartnerCode, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Đọc câu ghi của đối tác: câu ghi riêng (dòng ShareDataSql PartnerId = đối tác) và câu ghi mẫu chung để đối chiếu.
        /// Bảng / khoá / ghi chú mô tả câu ghi hiệu lực — riêng nếu có, không thì mẫu chung (đúng câu Worker sẽ chạy).
        /// </summary>
        public async Task<PartnerWriteSqlOutput> LoadPartnerOutput(ShareDataPacket packet, ShareDataPartner partner)
        {
            var (common, partnerSql) = await SqlTextHelper.ResolveSqlConfig(_db, packet, partner.ID);
            var isCustom = !string.IsNullOrWhiteSpace(partnerSql?.WriteSql);
            var effectiveRow = isCustom ? partnerSql : common;
            var effective = BuildWriteOutput(packet.Code ?? string.Empty, effectiveRow?.WriteSql, effectiveRow?.AffectedTables, effectiveRow?.WriteRemark);

            return new PartnerWriteSqlOutput
            {
                PartnerId = partner.ID,
                PartnerCode = partner.Code ?? string.Empty,
                PartnerName = partner.Name ?? string.Empty,
                PacketCode = packet.Code ?? string.Empty,
                StandardWriteSql = common?.WriteSql,
                CustomWriteSql = isCustom ? partnerSql!.WriteSql : null,
                IsCustom = isCustom,
                AffectedTables = effective.AffectedTables,
                DetectedTables = effective.DetectedTables,
                KeyFields = effective.KeyFields,
                Remark = effective.Remark
            };
        }

        /// <summary>Dựng cấu hình khối câu ghi từ các cột câu ghi của dòng mẫu chung: tự dò bảng đích và khoá bản ghi.</summary>
        public static PacketWriteSqlsOutput BuildWriteOutput(string packetCode, string? savedWriteSql, string? savedAffectedTables, string? remark)
        {
            var writeSql = savedWriteSql?.Trim();
            if (string.IsNullOrWhiteSpace(writeSql))
            {
                return new PacketWriteSqlsOutput { PacketCode = packetCode };
            }

            var affectedTables = WriteSqlAnalyzer.ParseTableList(savedAffectedTables, null)
                .DistinctBy(WriteSqlAnalyzer.NormalizeTable, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var analysis = WriteSqlAnalyzer.Analyze(writeSql, affectedTables, null);

            if (affectedTables.Count == 0)
            {
                affectedTables = [.. analysis.DetectedTables];
            }

            return new PacketWriteSqlsOutput
            {
                PacketCode = packetCode,
                WriteSql = writeSql,
                AffectedTables = affectedTables,
                DetectedTables = analysis.DetectedTables,
                KeyFields = analysis.KeyFields,
                Remark = remark
            };
        }

        /// <summary>
        /// Description: Kiểm khối câu ghi của gói. Phân tích tĩnh có lỗi thì không kiểm tiếp trên CSDL.
        ///              Chưa cấu hình CSDL nhận thì chỉ cảnh báo (không chặn lưu).
        /// </summary>
        public async Task<ValidateWriteSqlOutput> Validate(ShareDataPacket packet, string? partnerId, IEnumerable<string>? affectedTables, string? writeSql)
        {
            // Trường của gói theo đúng câu trích xuất đối tác dùng: có SQL riêng thì lấy trường của SQL riêng, không thì mẫu chung.
            var (common, partner) = await SqlTextHelper.ResolveSqlConfig(_db, packet, partnerId);
            var fieldsJson = SqlTextHelper.IsCustom(partner) ? partner!.FieldsJson : common?.FieldsJson;
            var fieldKeys = SqlFieldHelper.Read(fieldsJson).Where(f => !f.Lost).Select(f => f.Key).ToList();
            var analysis = WriteSqlAnalyzer.Analyze(writeSql, affectedTables, fieldKeys);
            var checks = analysis.Findings.Select(ToCheck).ToList();

            if (!analysis.HasError)
            {
                var inbound = GetInboundDb();
                if (inbound == null)
                {
                    checks.Add(Check(ShareDataConst.WriteSqlCheckWarning, BaseMsg.ShareData.Validation.WriteDbNotConfigured));
                }
                else
                {
                    var tablesToCheck = (affectedTables ?? [])
                        .Concat(analysis.DetectedTables)
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => t.Trim())
                        .DistinctBy(WriteSqlAnalyzer.NormalizeTable, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var missingTable = false;
                    foreach (var table in tablesToCheck)
                    {
                        var exists = await inbound.Ado.GetIntAsync(
                            "SELECT CASE WHEN OBJECT_ID(@t, 'U') IS NULL THEN 0 ELSE 1 END",
                            new SugarParameter("@t", table)
                        );
                        if (exists == 0)
                        {
                            checks.Add(Check(ShareDataConst.WriteSqlCheckError, BaseMsg.ShareData.Validation.WriteTableNotFound, table));
                            missingTable = true;
                        }
                    }

                    if (!missingTable)
                    {
                        var compileError = await inbound.Ado.GetStringAsync(
                            DescribeErrorSql,
                            new SugarParameter("@tsql", writeSql, System.Data.DbType.String) { Size = -1 },
                            new SugarParameter("@params", ParameterDeclaration(), System.Data.DbType.String) { Size = -1 }
                        );
                        if (!string.IsNullOrWhiteSpace(compileError))
                        {
                            checks.Add(Check(ShareDataConst.WriteSqlCheckError, BaseMsg.ShareData.Validation.WriteSqlCompileError, compileError));
                        }
                    }
                }
            }

            return new ValidateWriteSqlOutput
            {
                IsValid = checks.All(c => c.Level != ShareDataConst.WriteSqlCheckError),
                Checks = checks,
                DetectedTables = analysis.DetectedTables,
                KeyFields = analysis.KeyFields
            };
        }

        /// <summary>
        /// Description: Chạy thử khối câu ghi với một mảng bản ghi mẫu trên CSDL nhận, luôn quay lui.
        ///              Khối còn lỗi khi phân tích tĩnh thì từ chối, không chạy.
        /// </summary>
        public async Task<TryWriteSqlOutput> TryRun(ShareDataPacket packet, ShareDataPartner? partner, string? writeSql, IEnumerable<string>? affectedTables, string? recordsJson)
        {
            int recordCount;
            try
            {
                using var doc = JsonDocument.Parse(recordsJson ?? string.Empty);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw Oops.Oh(BaseMsg.ShareData.Validation.WriteSqlRecordsInvalid);
                }
                recordCount = doc.RootElement.GetArrayLength();
            }
            catch (JsonException)
            {
                throw Oops.Oh(BaseMsg.ShareData.Validation.WriteSqlRecordsInvalid);
            }

            var cleanTables = (affectedTables ?? []).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
            var analysis = WriteSqlAnalyzer.Analyze(writeSql, cleanTables, null);
            var firstError = analysis.Findings.FirstOrDefault(f => f.Level == ShareDataConst.WriteSqlCheckError);
            if (firstError != null)
            {
                throw Oops.Oh(
                    BaseMsg.ShareData.Validation.WriteSqlItemInvalid,
                    1,
                    string.Join(", ", cleanTables),
                    Localize(firstError.MessageKey, firstError.Args)
                );
            }

            var inbound = GetInboundDb() ?? throw Oops.Oh(BaseMsg.ShareData.Validation.WriteDbNotConfigured);
            using var db = inbound.CopyNew();
            var parameters = BuildTryParameters(packet, partner, recordsJson!);
            var output = new TryWriteSqlOutput { RecordCount = recordCount, IsSuccess = true };

            await db.Ado.BeginTranAsync();
            try
            {
                var ds = await db.Ado.GetDataSetAllAsync(writeSql!, parameters);
                var inserted = 0;
                var updated = 0;
                foreach (DataTable table in ds.Tables)
                {
                    if (table.Columns.Count == 0) continue;
                    foreach (DataRow row in table.Rows)
                    {
                        var action = row[0]?.ToString();
                        if (string.Equals(action, "INSERT", StringComparison.OrdinalIgnoreCase)) inserted++;
                        else if (string.Equals(action, "UPDATE", StringComparison.OrdinalIgnoreCase)) updated++;
                    }
                }
                output.Inserted = inserted;
                output.Updated = updated;
            }
            catch (Exception ex) when (ex is not AppFriendlyException)
            {
                output.ErrorMessage = ex.Message;
                output.IsSuccess = false;
            }
            finally
            {
                await RollbackQuietly(db);
            }

            return output;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lấy kết nối CSDL nhận; chưa cấu hình thì trả null.
        /// Created date: 26/10/2026
        /// </summary>
        private ISqlSugarClient? GetInboundDb()
        {
            var tenant = _db.AsTenant();
            return tenant.IsAnyConnection(Its015Const.ConnectionConst.ShareData)
                ? tenant.GetConnectionScope(Its015Const.ConnectionConst.ShareData)
                : null;
        }

        /// <summary>Khai kiểu tham số hệ thống cho sys.dm_exec_describe_first_result_set.</summary>
        private static string ParameterDeclaration()
            => string.Join(", ", ShareDataConst.WriteSqlSystemParameters.Select(p => $"@{p.Key} {p.Value}"));

        /// <summary>Tham số hệ thống như Worker truyền (DataInboundService.BuildWriteParameters); chạy thử mẫu chung thì đối tác để trống.</summary>
        private static SugarParameter[] BuildTryParameters(ShareDataPacket packet, ShareDataPartner? partner, string recordsJson) =>
        [
            new SugarParameter("@records", recordsJson, System.Data.DbType.String) { Size = -1 },
            new SugarParameter("@now", BaseUtil.TimeServer.GetTimeServer(), System.Data.DbType.DateTime),
            new SugarParameter("@tenantId", packet.TenantId?.ToString() ?? "Default", System.Data.DbType.String),
            new SugarParameter("@operator", TryOperator, System.Data.DbType.String),
            new SugarParameter("@partnerId", (object?)partner?.ID ?? DBNull.Value, System.Data.DbType.String),
            new SugarParameter("@partnerCode", (object?)partner?.Code ?? DBNull.Value, System.Data.DbType.String),
            new SugarParameter("@datatypeId", packet.Code, System.Data.DbType.String),
            new SugarParameter("@serialNbr", 0, System.Data.DbType.Int32),
            new SugarParameter("@packetId", packet.ID, System.Data.DbType.String)
        ];

        /// <summary>Lỗi SQL có thể đã làm máy chủ huỷ giao dịch: quay lui lần hai thì bỏ qua.</summary>
        private static async Task RollbackQuietly(ISqlSugarClient db)
        {
            try
            {
                await db.Ado.RollbackTranAsync();
            }
            catch (InvalidOperationException)
            {
                db.Ado.Transaction = null;
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đổi kết quả phân tích tĩnh thành dòng kết quả kiểm, đã dịch câu thông báo.
        /// Created date: 26/10/2026
        /// </summary>
        private static WriteSqlCheckItem ToCheck(WriteSqlAnalyzer.Finding f) => new()
        {
            Level = f.Level,
            Message = Localize(f.MessageKey, f.Args),
            Line = f.Line,
            Column = f.Column
        };

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo một dòng kết quả kiểm từ khóa thông báo.
        /// Created date: 26/10/2026
        /// </summary>
        private static WriteSqlCheckItem Check(string level, string key, params object[] args)
            => new() { Level = level, Message = Localize(key, args) };

        /// <summary>
        /// Author: DatHQ
        /// Description: Dịch khóa thông báo theo ngôn ngữ hiện tại.
        /// Created date: 26/10/2026
        /// </summary>
        private static string Localize(string key, object[] args) => L.Text[key, args].Value;
    }
}
