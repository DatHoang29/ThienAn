using Microsoft.Extensions.Logging;
using Modules.VDS.Core.Constants;
using Modules.VDS.Infrastructure.Services.TrafficSummary;
using Modules.VDS.Infrastructure.Services.TrafficSummary.Folding;
using Shared.DTO.Dtos.Vds;
using Shared.Utility.Counting;

namespace Modules.VDS.Infrastructure.Services.TrafficData
{
    /// <summary>
    /// Author: KienLT
    /// Description: Đọc số đếm xe theo <c>(giờ × direction × vehicleSize)</c> kèm CHÍNH SÁCH DỰ PHÒNG:
    /// ưu tiên gộp trực tiếp trên QuestDB, chỉ lùi về <c>TmsTrafficSummary</c> (SQL Server) khi QuestDB
    /// không kết nối được.
    ///
    /// <para><b>Vì sao chính sách nằm ở đây.</b> Trước 25/09/2026 nó nằm trong
    /// <c>Controllers/Report/Queries/ReportQueryHandler.cs</c> phục vụ endpoint <c>vdsreport/Volume</c> —
    /// endpoint KHÔNG có bên nào gọi: FE chỉ sinh client từ swagger rồi bỏ đó, còn báo cáo lưu lượng
    /// ngày/tháng thật (<c>Modules.TMS.Report</c>, trang <c>report/tms/trafficVolume</c>) gọi
    /// <c>vdstrafficdata/volumeBreakdown</c> qua Refit. Nghĩa là khả năng degrade DUY NHẤT của module VDS
    /// nằm ở nhánh chết, còn nhánh đang chạy thì QuestDB xuống là 503 và báo cáo trắng. Nên chính sách
    /// được dời sang đúng đường đang được gọi, còn feature Report bị xoá.</para>
    ///
    /// <para><b>Chỉ dự phòng khi <c>countAllEventTypes = false</c>.</b> <c>TmsTrafficSummary</c> do worker
    /// chốt theo bộ lọc sự kiện bình thường; gấp nó lại để trả lời một yêu cầu "đếm mọi loại sự kiện" sẽ
    /// ra con số KHÁC câu hỏi mà bên gọi không có cách nào biết. Bản cũ ở Report không gặp chuyện này vì
    /// nó truyền cứng <c>false</c>. Với <c>true</c>, giữ nguyên hành vi hiện tại: ném tiếp để
    /// <c>VdsExceptionFilter</c> đổi thành 503 + <c>Retry-After</c> (đường HTTP; đường in-process từ
    /// TMS.Report nhận nguyên ngoại lệ).</para>
    ///
    /// <para><b>Chỉ dự phòng khi lỗi KẾT NỐI.</b> Lỗi câu lệnh (thiếu bảng, sai kiểu) phải nổi lên như cũ —
    /// che nó bằng số liệu từ nguồn khác là biến một lỗi cần sửa thành báo cáo trông có vẻ đúng.
    /// <c>VdsRepository.GuardAsync</c> đã dịch lỗi kết nối thành <see cref="QuestDbUnavailableException"/>;
    /// vẫn kiểm thêm <see cref="QuestAvailabilityGate.IsConnectivityFailure"/> cho trường hợp ngoại lệ
    /// thoát ra nguyên dạng.</para>
    ///
    /// <para><b>Kết quả không mang cờ nguồn.</b> Hợp đồng <c>volumeBreakdown</c> là
    /// <c>List&lt;VdsTrafficVolumeBreakdownRow&gt;</c> — dùng bởi report ngày/tháng của TMS.Report (gọi
    /// in-process qua <c>IMessageBus</c> với <c>VdsTrafficVolumeBreakdownRequest</c> từ 13/10/2026, thay Refit
    /// <c>IVdsTrafficDataApi</c> đã xoá) và client FE đã sinh, đổi hình dạng là phá cả hai. Vì vậy việc đã phải dùng nguồn dự phòng được ghi bằng log
    /// WARNING thay vì thêm trường; số liệu từ summary là số đã chốt nên không tự suy ra được là đủ phủ
    /// cửa sổ hay chưa.</para>
    ///
    /// <para><c>IScoped</c> của Furion: vòng đời theo request, không cần đăng ký tay.</para>
    /// Created date: 25/09/2026
    /// </summary>
    public sealed class VdsVolumeBreakdownReader(
        VdsTrafficDataService traffic,
        VdsTrafficSummaryService summary,
        ILogger<VdsVolumeBreakdownReader> logger) : IScoped
    {
        /// <summary>
        /// Đếm xe theo <c>(giờ, direction, vehicleSize)</c> trong <c>[fromUtc, toUtc)</c>.
        /// </summary>
        /// <param name="fromUtc">Mốc đầu cửa sổ, đã quy về UTC ở handler.</param>
        /// <param name="toUtc">Mốc cuối cửa sổ (không bao gồm), đã quy về UTC ở handler.</param>
        /// <param name="countAllEventTypes">true = đếm cả sự kiện bất thường; khi đó KHÔNG dự phòng.</param>
        public async Task<List<VdsTrafficVolumeBreakdownRow>> ReadAsync(
            DateTime fromUtc, DateTime toUtc, bool countAllEventTypes)
        {
            try
            {
                return await traffic.VolumeBreakdownAsync(fromUtc, toUtc, countAllEventTypes);
            }
            catch (Exception ex) when (!countAllEventTypes
                && (ex is QuestDbUnavailableException || QuestAvailabilityGate.IsConnectivityFailure(ex)))
            {
                var rows = await summary.ReadAsync(
                    fromUtc, toUtc, VdsSummaryPeriodConst.Hour, TrafficCountScopes.Equipment);

                logger.LogWarning(ex,
                    "[VdsVolumeBreakdown] QuestDB không dùng được — lấy số đã chốt từ TmsTrafficSummary " +
                    "cho cửa sổ {From:o}..{To:o} ({Rows} dòng tổng hợp). Số này KHÔNG có watermark phủ dữ " +
                    "liệu nên có thể thiếu những giờ worker chưa chốt.",
                    fromUtc, toUtc, rows.Count);

                return VdsSummaryVolumeFolder.FoldSummary(rows);
            }
        }
    }
}
