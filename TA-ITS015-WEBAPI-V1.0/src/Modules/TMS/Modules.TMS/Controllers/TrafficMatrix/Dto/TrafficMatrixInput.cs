using Modules.TMS.Core.Constants;

namespace Modules.TMS.Controllers.TrafficMatrix.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: Tham số của bảng ma trận vận tốc / lưu lượng theo giờ trong MỘT ngày.
    /// Created date: 11/08/2026
    /// </summary>
    public class TrafficMatrixInput
    {
        /// <summary>
        /// Ngày cần xem. FE gửi theo UTC+0 nên handler phải quy về giờ server bằng
        /// <c>BaseUtil.TimeServer.GetTimeServer</c> trước khi cắt mốc ngày. Null ⇒ hôm nay.
        /// </summary>
        public DateTime? ValueDate { get; set; }

        /// <summary>
        /// Giới hạn trong một nhánh zone (lấy cả zone con). Null/rỗng ⇒ toàn tuyến.
        /// </summary>
        public string? ZoneId { get; set; }

        /// <summary>
        /// Loại zone dùng làm HÀNG của ma trận (danh mục <c>tms_zone_type</c>).
        /// Null/rỗng ⇒ <see cref="TrafficMatrixConst.DefaultRowZoneType"/>.
        /// </summary>
        public string? ZoneType { get; set; }

        /// <summary>
        /// Hướng tuyến (<c>TmsEquipment.DirectionId</c>: <c>"1"</c> Phải tuyến, <c>"2"</c> Trái tuyến).
        /// Null/rỗng hoặc <c>"0"</c> (<c>SpecialConfigEnum.AllDirection</c>) ⇒ trả về TẤT CẢ hướng,
        /// mỗi hướng một nhóm.
        /// </summary>
        public string? Direction { get; set; }

        /// <summary>
        /// Lọc theo cỡ xe (<c>TmsTrafficData.Type</c>: short / medium / long). Null ⇒ mọi cỡ xe.
        /// </summary>
        public string? VehicleType { get; set; }

        /// <summary>
        /// true ⇒ đếm mọi loại bản tin thay vì chỉ <c>Normal</c>. Mặc định false.
        /// <para>
        /// Bản tin sự kiện (Speeding / Congestion / ...) sinh THÊM trên cùng một lượt xe, nên bật cờ này
        /// là chấp nhận đếm trùng lượt xe và vận tốc bị kéo lệch. Chỉ dùng khi đối chiếu số liệu thô.
        /// </para>
        /// </summary>
        public bool CountAllEventTypes { get; set; }
    }
}
