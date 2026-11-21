using System.Globalization;
using Modules.TMS.Controllers.TrafficData.Dto;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Core.Constants;
using Modules.VDS.Core.Constants;
using Shared.DTO.Dtos.System;

namespace Modules.TMS.Infrastructure.Services.TrafficData
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Vận tốc trung bình của vùng theo hướng.
    /// Created date: 12/11/2024
    ///
    /// Cutover 04/09/2026 (KienLT): trước đây hàm này trả <c>new Random().Next(60, 120)</c> cho cả
    /// <c>AverageSpeed</c> và <c>SpeedLimit</c> — nghĩa là endpoint
    /// <c>POST api/tms/TmsTrafficData/TrafficDataAverage</c> trả SỐ BỊA, mỗi lần gọi một giá trị khác.
    ///
    /// Nay lấy số THẬT, không truy vấn thêm bảng nào:
    /// <list type="bullet">
    /// <item><b>AverageSpeed</b>: từ <c>TmsZoneStatus.AverageSpeed</c> — do worker VDS ghi mỗi lần trạng thái
    /// vùng đổi (<c>ZoneStatusRepository.UpsertStatusAsync</c>), đúng nghĩa "vận tốc trung bình của vùng".
    /// Quá hạn theo <c>ZoneSpeedFreshnessConst</c> (08/10/2026) ⇒ 0.</item>
    /// <item><b>SpeedLimit</b>: từ <c>VdsConfig</c> khoá <c>vds_route_{hướng}_max_speed</c> — cùng nguồn mà
    /// service VDS dùng để sinh sự kiện quá tốc độ, nên hai bên không lệch ngưỡng.</item>
    /// </list>
    ///
    /// Thiếu dữ liệu thì trả <b>0</b> chứ không đoán: 0 nói được "chưa có số", còn số bịa thì không.
    /// </summary>
    public class TrafficDataSpeedChartService
    {
        /// <summary>Tiền tố khoá ngưỡng tốc độ theo hướng trong <c>VdsConfig</c> (nhóm <c>vds_config</c>).</summary>
        private const string SpeedLimitKeyPrefix = "vds_route_";

        /// <summary>Hậu tố khoá ngưỡng tốc độ tối đa.</summary>
        private const string SpeedLimitKeySuffix = "_max_speed";

        /// <summary>
        /// Vận tốc trung bình của một vùng theo hướng.
        /// </summary>
        /// <param name="zoneList">Danh sách vùng kèm <c>ZoneStatus</c> (worker VDS ghi).</param>
        /// <param name="opConfigList">Cấu hình vận hành để tra ngưỡng tốc độ; null ⇒ SpeedLimit = 0.</param>
        /// <param name="zone">Vùng cần lấy; rỗng ⇒ không xác định được vùng nào ⇒ trả 0.</param>
        /// <param name="direction">Hướng tuyến, dùng để tra ngưỡng.</param>
        /// <param name="serverNow">Giờ server — worker ghi <c>UpdateTime</c> theo giờ server-local.</param>
        public TmsTrafficDataAverage BuildTrafficDataAverage(
            List<ZoneOutput>? zoneList,
            List<SysOpConfigResponse>? opConfigList,
            string? zone,
            string? direction,
            DateTime serverNow)
        {
            var result = new TmsTrafficDataAverage()
            {
                Direction = direction,
                Zone = zone,
                AverageSpeed = 0,
                SpeedLimit = ResolveSpeedLimit(opConfigList, direction),
            };

            if (zoneList is null || string.IsNullOrWhiteSpace(zone)) return result;

            // Khớp theo ID vùng. KHÔNG lọc thêm theo direction: TmsZoneStatus là trạng thái CỦA VÙNG, không
            // tách theo hướng — lọc thêm sẽ luôn ra rỗng và số về 0 (đúng cái lỗi của bản cũ khi
            // Direction = LaneId không bao giờ khớp tham số hướng).
            var status = zoneList
                .FirstOrDefault(z => string.Equals(z.ID, zone, StringComparison.OrdinalIgnoreCase))
                ?.ZoneStatus;

            // Cùng hạn với snapshot/SSE của bản đồ (ZoneSpeedFreshnessConst): worker ngừng cập nhật quá cửa sổ
            // tính TB + biên ⇒ 0 ("chưa có số"), không vẽ số cũ như số đang chạy.
            var maxAge = ZoneSpeedFreshnessConst.MaxAgeSeconds(opConfigList?
                .FirstOrDefault(c => string.Equals(c.Code, VdsConfigConst.Code.ZoneSlowWindow, StringComparison.OrdinalIgnoreCase))
                ?.Value);
            if (!ZoneSpeedFreshnessConst.IsFresh(status?.UpdateTime, serverNow, maxAge)) return result;

            result.AverageSpeed = ParseSpeed(status?.AverageSpeed);
            return result;
        }

        /// <summary>
        /// Ngưỡng tốc độ tối đa của hướng, đọc từ <c>VdsConfig</c>. Không khai hoặc khai sai ⇒ 0
        /// (fail-closed: thà báo "chưa có ngưỡng" còn hơn vẽ đường giới hạn bịa lên biểu đồ).
        /// </summary>
        private static int ResolveSpeedLimit(List<SysOpConfigResponse>? opConfigList, string? direction)
        {
            if (opConfigList is null || string.IsNullOrWhiteSpace(direction)) return 0;

            var key = $"{SpeedLimitKeyPrefix}{direction.Trim()}{SpeedLimitKeySuffix}";
            var value = opConfigList
                .FirstOrDefault(c => string.Equals(c.Code, key, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            return ParseSpeed(value);
        }

        /// <summary>
        /// Đọc tốc độ từ chuỗi rồi LÀM TRÒN về số nguyên (DTO của FE khai <c>int?</c>).
        /// <c>TmsZoneStatus.AverageSpeed</c> và <c>VdsConfig.Value</c> đều là <c>string</c> nên phải parse;
        /// dùng <see cref="CultureInfo.InvariantCulture"/> để dấu thập phân không phụ thuộc locale của máy
        /// chạy (giá trị "62.5" trên máy locale VN sẽ parse sai nếu dùng culture hiện hành).
        /// Không đọc được hoặc âm ⇒ 0.
        /// </summary>
        private static int ParseSpeed(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;

            if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return 0;

            return value >= 0 ? (int)Math.Round(value, MidpointRounding.AwayFromZero) : 0;
        }
    }
}
