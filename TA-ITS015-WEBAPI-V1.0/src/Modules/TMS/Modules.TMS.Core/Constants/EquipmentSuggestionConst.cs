namespace Modules.TMS.Core.Constants
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Hằng số dùng chung cho nghiệp vụ GỢI Ý THIẾT BỊ theo vị trí sự cố (VMS/LCS).
    ///
    ///              NGUỒN CẤU HÌNH DUY NHẤT của phạm vi gợi ý là SysOpConfig (nhóm operation_config).
    ///              Cả luồng thủ công (FE đọc rồi truyền xuống) và luồng tự động (backend tự đọc) đều
    ///              lấy từ đây, nên khi thiếu cấu hình thì hai luồng phải fallback về CÙNG một giá trị
    ///              (DefaultSmallRangeMeters). Trước đây backend fallback 15000 còn FE fallback 4000,
    ///              dẫn tới tự động và thủ công chọn ra hai tập thiết bị khác nhau.
    ///
    ///              TmsEquipmentEventRule KHÔNG còn giữ phạm vi; rule chỉ quyết định Before/After và
    ///              MaxTargetCount.
    /// Created date: 19/08/2026
    /// </summary>
    public static class EquipmentSuggestionConst
    {
        /// <summary>
        /// Phạm vi gần (mét) dùng khi SysOpConfig chưa cấu hình. Thiết bị trong phạm vi này được TÍCH SẴN.
        /// </summary>
        public const int DefaultSmallRangeMeters = 4000;

        /// <summary>
        /// Mã cấu hình phạm vi gợi ý trong SysOpConfig.
        /// </summary>
        public static class OpConfig
        {
            public const string GroupCode = "operation_config";

            /// <summary>Phạm vi gần (mét): thiết bị trong phạm vi này được tích chọn sẵn.</summary>
            public const string SmallRangeCode = "incident_equipment_suggestion_small_range";

            /// <summary>Phạm vi xa (mét): thiết bị chỉ được GỢI Ý (hiển thị, không tích sẵn).</summary>
            public const string LargeRangeCode = "incident_equipment_suggestion_large_range";

            /// <summary>
            /// Bật/tắt quét thiết bị NGOÀI ZONE khi tích chọn sẵn (Value = "1"/"true" là bật).
            /// - Tắt / chưa cấu hình (mặc định): luồng cũ, thiết bị phải nằm trong zone cấp bé nhất chứa sự cố.
            ///   Phù hợp dự án Tuyến (zone là cả đoạn giữa các nút giao, dài vài km).
            /// - Bật: bỏ ràng buộc zone, tích sẵn mọi VMS/LCS trong bán kính small (vẫn lọc tuyến/hướng/Trước-Sau).
            ///   Dùng cho dự án Hầm (zone ~120m) và thiết bị lắp ngoài hầm / thiết bị test nằm ngoài zone.
            /// </summary>
            public const string IgnoreZoneCode = "incident_equipment_suggestion_ignore_zone";
        }

        /// <summary>
        /// Mã chiều di chuyển của phương tiện, dùng để xác định thiết bị nằm TRƯỚC hay SAU vị trí sự cố.
        /// Cùng miền giá trị: TmsZone.LaneId = TmsEquipment.DirectionId = TmsIncident.InfluenceScope.
        ///
        /// QUY ƯỚC LẤY TỪ DỮ LIỆU ZONE (kiểm chứng trên TmsZone/main_route, đúng 11/11 bản ghi):
        ///   LaneId = "1" -> FromKmNumber &gt; ToKmNumber  => chiều GIẢM lý trình (vd CL -&gt; HN)
        ///   LaneId = "2" -> FromKmNumber &lt; ToKmNumber  => chiều TĂNG lý trình (vd HN -&gt; CL)
        ///
        /// CẢNH BÁO: comment/logic cũ trong module giả định NGƯỢC LẠI ("1" là chiều tăng). Nếu về sau
        /// dữ liệu zone đổi quy ước thì phải sửa đúng ở đây, đừng rải giá trị "1"/"2" ra nhiều nơi.
        /// Nguồn chính xác nhất vẫn là hình học zone (From/To); hai hằng số này chỉ dùng để suy chiều
        /// khi vị trí sự cố KHÔNG rơi vào zone nào.
        /// </summary>
        public static class TravelDirection
        {
            /// <summary>Chiều giảm lý trình (Km lớn -> Km nhỏ).</summary>
            public const string Decreasing = "1";

            /// <summary>Chiều tăng lý trình (Km nhỏ -> Km lớn).</summary>
            public const string Increasing = "2";
        }
    }
}
