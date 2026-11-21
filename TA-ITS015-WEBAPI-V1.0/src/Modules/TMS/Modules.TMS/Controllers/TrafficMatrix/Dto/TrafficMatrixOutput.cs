using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.TrafficMatrix;

namespace Modules.TMS.Controllers.TrafficMatrix.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: Bảng ma trận theo giờ. HÀNG = đoạn tuyến, CỘT = 24 giờ, nhóm theo hướng tuyến.
    ///
    /// Trả về CẢ HAI số liệu (vận tốc + lưu lượng) trong một lần gọi để FE đổi tab
    /// Vận tốc ↔ Lưu lượng mà không phải gọi lại API — payload chỉ thêm 2 số mỗi ô.
    ///
    /// Created date: 11/08/2026
    /// </summary>
    public class TrafficMatrixOutput
    {
        /// <summary>Ngày của ma trận (00:00:00 giờ server).</summary>
        public DateTime ValueDate { get; set; }

        /// <summary>Danh sách giờ làm tiêu đề cột: 0..23.</summary>
        public List<int> Hours { get; set; } = new();

        /// <summary>Loại zone đã dùng làm hàng (echo lại để FE biết BE hiểu thế nào).</summary>
        public string? ZoneType { get; set; }

        /// <summary>
        /// Nhánh zone đang được giới hạn (thường là tuyến). Null ⇒ không giới hạn, lấy toàn bộ.
        /// Echo lại để FE đồng bộ ô chọn và biết danh sách đoạn tuyến nên gói trong phạm vi nào.
        /// </summary>
        public string? RootZoneId { get; set; }

        /// <summary>Loại bản tin đã được tính; rỗng ⇒ tính mọi loại.</summary>
        public List<string> CountedEventTypes { get; set; } = new();

        /// <summary>Dải màu của số liệu vận tốc, theo thứ tự chỉ số dải.</summary>
        public List<TrafficMatrixBand> SpeedBands { get; set; } = new();

        /// <summary>Dải màu của số liệu lưu lượng, theo thứ tự chỉ số dải.</summary>
        public List<TrafficMatrixBand> VolumeBands { get; set; } = new();

        /// <summary>Mỗi hướng tuyến một nhóm (FE render dạng collapse).</summary>
        public List<TrafficMatrixGroupOutput> Groups { get; set; } = new();
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Một nhóm hướng tuyến của ma trận.
    /// Created date: 11/08/2026
    /// </summary>
    public class TrafficMatrixGroupOutput
    {
        /// <summary>Mã hướng (<c>"1"</c> / <c>"2"</c>); null ⇒ nhóm dữ liệu chưa khai hướng.</summary>
        public string? DirectionCode { get; set; }

        /// <summary>Tên hướng lấy từ <c>SysConfigData</c> loại <c>direction_config_type</c>.</summary>
        public string? DirectionName { get; set; }

        /// <summary>Các đoạn tuyến, đã sắp theo lý trình tăng dần.</summary>
        public List<TrafficMatrixRowOutput> Rows { get; set; } = new();
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Một hàng của ma trận = một đoạn tuyến.
    /// Created date: 11/08/2026
    /// </summary>
    public class TrafficMatrixRowOutput
    {
        public string? ZoneId { get; set; }
        public string? ZoneCode { get; set; }
        public string? ZoneName { get; set; }

        /// <summary>Lý trình đầu đoạn — FE dùng để hiển thị và để sắp thứ tự hàng.</summary>
        public int? FromKmNumber { get; set; }
        public int? FromMetNumber { get; set; }
        public int? ToKmNumber { get; set; }
        public int? ToMetNumber { get; set; }

        /// <summary>
        /// Tốc độ tối đa khai báo cho đoạn (<c>TmsZone.MaxSpeed</c>); null nếu chưa khai.
        /// Chỉ để tham chiếu — việc tô màu vẫn theo dải chung để cả bảng so sánh được với nhau.
        /// </summary>
        public float? MaxSpeed { get; set; }

        /// <summary>24 ô theo giờ, luôn đủ 24 phần tử kể cả giờ không có xe.</summary>
        public List<TrafficMatrixCellOutput> Cells { get; set; } = new();

        /// <summary>Vận tốc trung bình CÓ TRỌNG SỐ của cả ngày; null khi không có mẫu vận tốc.</summary>
        public double? DayAvgSpeed { get; set; }

        /// <summary>Tổng lượt xe cả ngày của đoạn.</summary>
        public int DayVehicleCount { get; set; }

        /// <summary>Chỉ số dải màu của <see cref="DayAvgSpeed"/>.</summary>
        public int DaySpeedBandIndex { get; set; } = TrafficMatrixConst.NoDataBandIndex;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Một ô của ma trận = (đoạn tuyến, hướng, giờ).
    /// Created date: 11/08/2026
    /// </summary>
    public class TrafficMatrixCellOutput
    {
        /// <summary>Giờ trong ngày, 0..23.</summary>
        public int Hour { get; set; }

        /// <summary>
        /// Vận tốc trung bình có trọng số của giờ (km/h); <b>null = không có mẫu vận tốc</b>, khác hẳn
        /// với 0 km/h. FE hiển thị "-" chứ không hiển thị 0.
        /// </summary>
        public double? AvgSpeed { get; set; }

        /// <summary>Số lượt xe trong giờ; 0 nghĩa là thật sự không có xe.</summary>
        public int VehicleCount { get; set; }

        /// <summary>Số bản ghi có giá trị vận tốc — trọng số khi gộp và để soi độ tin cậy của ô.</summary>
        public int SpeedSampleCount { get; set; }

        /// <summary>Chỉ số dải màu theo vận tốc; -1 khi không có dữ liệu.</summary>
        public int SpeedBandIndex { get; set; } = TrafficMatrixConst.NoDataBandIndex;

        /// <summary>Chỉ số dải màu theo lưu lượng.</summary>
        public int VolumeBandIndex { get; set; } = TrafficMatrixConst.NoDataBandIndex;
    }
}
