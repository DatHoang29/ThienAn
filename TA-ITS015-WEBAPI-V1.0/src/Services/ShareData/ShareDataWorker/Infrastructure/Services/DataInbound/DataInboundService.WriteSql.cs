using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using ShareDataWorker.Infrastructure.Logging;

namespace ShareDataWorker.Infrastructure.Services.DataInbound
{
    /// <summary>
    /// Ghi dữ liệu chiều nhận bằng khối câu ghi khai sẵn (ShareDataSql.WriteSql ở dòng mẫu chung của gói).
    ///
    /// Cách chạy (ghi theo tập):
    ///  - Toàn bộ bản ghi đã chuẩn hoá của một gói được đóng thành MỘT chuỗi JSON, truyền vào @records.
    ///  - Mỗi câu ghi (thường là MERGE ... USING (SELECT ... FROM OPENJSON(@records) WITH (...)) ... OUTPUT $action)
    ///    chạy đúng một lần cho cả gói, theo thứ tự OrderNo.
    ///  - Tất cả câu ghi chạy chung một transaction trên CSDL Inbound: một câu lỗi → rollback cả gói.
    ///
    /// Tham số hệ thống truyền vào mọi câu ghi (câu nào không dùng thì bỏ qua):
    ///  @records · @now · @tenantId · @operator · @partnerId · @partnerCode · @datatypeId · @serialNbr · @packetId
    ///
    /// Author: HieuNV
    /// Created date: 15/09/2026
    /// </summary>
    public partial class DataInboundService
    {
        /// <summary>Giá trị @operator — ghi vào CreateUId / UpdateUId của bảng đích.</summary>
        private const string InboundOperator = "SHAREDATA_INBOUND";

        /// <summary>Giá trị @tenantId cho bản ghi thêm mới.</summary>
        private const string DefaultTenantId = "Default";

        private static readonly JsonSerializerOptions RecordsJsonOptions = new()
        {
            // Giữ nguyên fieldKey như trong gói tin để câu ghi đọc bằng OPENJSON ... WITH (fieldKey kiểu '$.fieldKey').
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        /// <summary>Số lỗi SQL Server khi câu lệnh bị chọn làm nạn nhân deadlock.</summary>
        private const int DeadlockVictimError = 1205;

        /// <summary>Số lỗi SqlClient khi câu lệnh chạy quá thời gian chờ.</summary>
        private const int CommandTimeoutError = -2;

        /// <summary>Mức lỗi từ đây trở lên làm đứt phiên (phiên bị cắt, mất kết nối).</summary>
        private const byte SessionBrokenErrorClass = 20;

        /// <summary>
        /// Author: DatHQ
        /// Description: Lỗi chạy câu ghi của gói, kèm cờ lỗi CSDL tạm thời để gói được ghi lại ở lượt sau.
        ///              Không gắn lỗi gốc vào InnerException: câu báo an toàn (SafeErrorMessage) gặp lỗi CSDL lồng bên trong sẽ thay cả câu bằng câu chung.
        /// Created date: 16/11/2026
        /// </summary>
        private sealed class InboundWriteException(string message, bool isTransient) : InvalidOperationException(message)
        {
            public bool IsTransient { get; } = isTransient;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chạy lần lượt các câu ghi của gói trong một transaction.
        ///              Đếm kết quả từ OUTPUT $action (INSERT / UPDATE) theo từng bảng.
        ///              Lỗi bất kỳ câu nào: rollback và ném lỗi (kèm số thứ tự câu ghi, không kèm tên bảng) để gói chuyển Failed.
        ///              Lỗi CSDL tạm thời (deadlock, hết giờ, phiên bị cắt) đánh dấu trên lỗi ném ra để gói được ghi lại ở lượt sau.
        /// Created date: 15/09/2026
        /// Modified date: 16/11/2026
        /// </summary>
        private async Task<SqlWriteResult> WriteRecordsBySqlAsync(
            ISqlSugarClient inboundDb,
            InboundContext ctx,
            ShareDataInboundPacket packet,
            List<Dictionary<string, object?>> records,
            CancellationToken cancellationToken,
            int writeCommandTimeoutSeconds)
        {
            var result = new SqlWriteResult();
            if (records.Count == 0)
                return result;

            var recordsJson = JsonSerializer.Serialize(records, RecordsJsonOptions);
            var parameters = BuildWriteParameters(ctx, packet, recordsJson);

            var oldTimeout = inboundDb.Ado.CommandTimeOut;
            inboundDb.Ado.CommandTimeOut = writeCommandTimeoutSeconds;

            await inboundDb.Ado.BeginTranAsync();
            int? currentNo = null;
            try
            {
                for (var i = 0; i < ctx.Writes.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentNo = i + 1;

                    var ds = await inboundDb.Ado.GetDataSetAllAsync(ctx.Writes[i], parameters);
                    var actions = new List<string>();
                    foreach (DataTable table in ds.Tables)
                    {
                        if (table.Columns.Count == 0) continue;
                        foreach (DataRow row in table.Rows)
                        {
                            if (row[0] is not null and not DBNull)
                            {
                                actions.Add(row[0].ToString()!);
                            }
                        }
                    }
                    result.Add(DescribeWriteTarget(currentNo.Value), actions);
                }

                await inboundDb.Ado.CommitTranAsync();
                return result;
            }
            catch (Exception ex)
            {
                await RollbackQuietly(inboundDb);

                var target = currentNo == null ? "câu ghi" : DescribeWriteTarget(currentNo.Value);
                // Lỗi gốc của máy chủ CSDL thường nêu tên bảng, cột, ràng buộc: chỉ ghi ở nhật ký máy chủ, không đưa vào nhật ký hiển thị trên giao diện.
                logger.LogError(ex, "❌ [Nhận] Lỗi chạy {Target} của {Tag}: {Message}", target, ctx.Tag, ex.Message);
                // Lỗi CSDL tạm thời (deadlock, hết giờ, phiên bị cắt) đánh dấu trên lỗi ném ra để gói được ghi lại ở lượt sau, câu báo giữ nguyên
                var message = $"Lỗi chạy {target}: {SafeErrorMessage.ForLog(ex)}";
                var isTransient = IsTransientSqlError(ex);
                throw new InboundWriteException(message, isTransient);
            }
            finally
            {
                inboundDb.Ado.CommandTimeOut = oldTimeout;
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Dựng bộ tham số hệ thống dùng chung cho mọi câu ghi của một gói.
        /// Created date: 15/09/2026
        /// </summary>
        private static SugarParameter[] BuildWriteParameters(
            InboundContext ctx, ShareDataInboundPacket packet, string recordsJson)
        {
            return
            [
                // nvarchar(max) — gói lớn vượt 4000 ký tự.
                new SugarParameter("@records", recordsJson, System.Data.DbType.String) { Size = -1 },
                new SugarParameter("@now", DateTime.Now, System.Data.DbType.DateTime),
                new SugarParameter("@tenantId", DefaultTenantId, System.Data.DbType.String),
                new SugarParameter("@operator", InboundOperator, System.Data.DbType.String),
                new SugarParameter("@partnerId", ctx.Partner?.ID, System.Data.DbType.String),
                new SugarParameter("@partnerCode", packet.PartnerCode, System.Data.DbType.String),
                new SugarParameter("@datatypeId", packet.PacketCode, System.Data.DbType.String),
                new SugarParameter("@serialNbr", packet.SerialNbr, System.Data.DbType.Int32),
                new SugarParameter("@packetId", packet.ID, System.Data.DbType.String)
            ];
        }

        /// <summary>Nhãn của một câu ghi trong log: số thứ tự, tuyệt đối không đưa tên bảng vào nhật ký hiển thị trên giao diện.</summary>
        private static string DescribeWriteTarget(int writeNo)
            => $"câu ghi #{writeNo}";

        /// <summary>
        /// Author: HieuNV
        /// Description: Kết quả chạy câu ghi của một gói — đếm theo từng bảng.
        /// Created date: 15/09/2026
        /// </summary>
        private sealed class SqlWriteResult
        {
            private readonly List<(string Target, int Inserted, int Updated)> _tables = [];

            public int Inserted => _tables.Sum(t => t.Inserted);
            public int Updated => _tables.Sum(t => t.Updated);

            /// <summary>
            /// Author: HieuNV
            /// Description: Cộng số dòng thêm mới và cập nhật của một bảng đích, đếm từ các hành động INSERT, UPDATE mà câu ghi trả về.
            /// Created date: 15/09/2026
            /// </summary>
            public void Add(string target, List<string>? actions)
            {
                var inserted = 0;
                var updated = 0;
                foreach (var action in actions ?? [])
                {
                    if (string.Equals(action, "INSERT", StringComparison.OrdinalIgnoreCase)) inserted++;
                    else if (string.Equals(action, "UPDATE", StringComparison.OrdinalIgnoreCase)) updated++;
                }

                _tables.Add((target, inserted, updated));
            }

            /// <summary>VD: "12 bản ghi · câu ghi #1 thêm 0, sửa 5 · câu ghi #2 thêm 2, sửa 10".</summary>
            public string Describe(int recordCount)
            {
                var sb = new StringBuilder($"{recordCount} bản ghi");
                foreach (var (target, inserted, updated) in _tables)
                    sb.Append($" · {target} thêm {inserted}, sửa {updated}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Description: Rollback transaction ghi gói mà không để lỗi rollback che mất lỗi SQL gốc.
        ///              Sau lỗi SQL, transaction có thể đã bị máy chủ hủy (rollback ném InvalidOperationException);
        ///              khi đó bỏ tham chiếu transaction để Dispose của client không rollback lần hai.
        /// Created date: 12/10/2026
        /// </summary>
        private static async Task RollbackQuietly(ISqlSugarClient inboundDb)
        {
            try
            {
                await inboundDb.Ado.RollbackTranAsync();
            }
            catch (InvalidOperationException)
            {
                inboundDb.Ado.Transaction = null;
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lỗi CSDL tạm thời, chạy lại sau là qua: bị chọn làm nạn nhân deadlock (1205), hết giờ chờ câu lệnh (-2),
        ///              lỗi nặng làm đứt phiên (mức lỗi từ 20, như phiên bị cắt 596). Duyệt cả chuỗi lỗi lồng.
        /// Created date: 16/11/2026
        /// </summary>
        private static bool IsTransientSqlError(Exception ex)
        {
            for (Exception? current = ex; current != null; current = current.InnerException)
            {
                if (current is SqlException sqlError
                    && (sqlError.Number == DeadlockVictimError || sqlError.Number == CommandTimeoutError || sqlError.Class >= SessionBrokenErrorClass))
                    return true;
            }

            return false;
        }
    }
}
