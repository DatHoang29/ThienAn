namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Profile thông số màn hình và quy tắc nghiệp vụ cho tường Video Wall.
    /// Toàn bộ hằng số đóng vai trò giá trị mặc định, có thể override qua section "VideoWall" trong
    /// appsettings.json của Worker (ITS.VideoWall) hoặc khi BE được bổ sung file cấu hình tương ứng.
    /// </summary>
    public static class VwWallProfile
    {
        /// <summary>
        /// Chiều rộng một panel trên canvas tường (pixel logic).
        ///
        /// Thống nhất 3840×2160 với FE (canvas kịch bản), DB (VwWallTopology.ScreenWidth/ScreenHeight,
        /// toạ độ VwWindowScene) và app WPF — tường 8 cột × 4 hàng ⇒ canvas tuyệt đối 30720×8640.
        ///
        /// Đây là ĐƠN VỊ LOGIC, không phải độ phân giải thiết bị: khi đẩy xuống bộ điều khiển,
        /// toạ độ được quy đổi sang ô ảo uniformCoordinate bằng
        /// <c>local * BaseOutputSize / PanelWidthPx</c> (xem VwGeometryHelper.ToIsapiLocalRect),
        /// nên 1 panel luôn thành đúng 1 ô ảo dù đơn vị là 1920 hay 3840. Điều bắt buộc duy nhất là
        /// FE, DB và BE dùng CÙNG một giá trị.
        ///
        /// LỊCH SỬ: 09/09/2026 từng đổi sang 1920×1080 (theo KienTruc_VideoWall_DS-C66S-Cascade.md §1)
        /// làm lệch với FE/DB đang dùng 3840 ⇒ cửa sổ bị hiểu gấp đôi kích thước và lệch vị trí.
        /// 24/09/2026 thống nhất lại 3840×2160.
        ///
        /// Có thể override qua VideoWall:Device:Profile:PanelWidthPx; bản ghi VwWallTopology
        /// (ScreenWidth) nếu có cũng phải cùng giá trị này.
        /// </summary>
        public const int PanelWidthPx = 3840;

        /// <summary>
        /// Chiều cao một panel trên canvas tường (pixel logic) — xem ghi chú ở PanelWidthPx.
        /// </summary>
        public const int PanelHeightPx = 2160;

        /// <summary>
        /// Tỉ lệ ô màn hình (rộng:cao) cho phép chọn ở bố cục tường (VwWallTopology.CellRatio).
        /// Chỉ là hình dạng ô khi hiển thị — đơn vị toạ độ vẫn là PanelWidthPx × PanelHeightPx.
        /// </summary>
        public static readonly string[] CellRatios = ["16:9", "3:4"];

        /// <summary>
        /// Tỉ lệ ô mặc định khi bố cục chưa chọn.
        /// </summary>
        public const string DefaultCellRatio = "16:9";

        /// <summary>
        /// Số cột panel mặc định của tường (1 khối) khi chưa có bản ghi VwWallTopology.
        /// Nguồn: KienTruc_VideoWall_DS-C66S-Cascade.md §1 — tường 8 cột.
        /// </summary>
        public const int DefaultCols = 8;

        /// <summary>
        /// Số hàng panel mặc định của tường (1 khối) khi chưa có bản ghi VwWallTopology.
        /// Nguồn: KienTruc_VideoWall_DS-C66S-Cascade.md §1 — tường 4 hàng.
        /// </summary>
        public const int DefaultRows = 4;

        /// <summary>
        /// Số khối tối đa của một tường (VwWallBlock).
        /// </summary>
        public const int MaxBlocks = 8;

        /// <summary>
        /// Số cột tối đa của một khối. Chặn trên để canvas không vọt lên hàng triệu pixel.
        /// </summary>
        public const int MaxBlockCols = 32;

        /// <summary>
        /// Số hàng tối đa của một khối.
        /// </summary>
        public const int MaxBlockRows = 16;

        /// <summary>
        /// Số ô tối đa của một bố cục (VwWallTopology.CellCount: 1..100).
        /// </summary>
        public const int MaxCellCount = 100;

        /// <summary>
        /// Tiền tố mã màn hình do hệ thống tự sinh khi tạo màn từ ô trống: SCR-01, SCR-02...
        /// </summary>
        public const string ScreenCodePrefix = "SCR-";

        /// <summary>
        /// Kích thước ô ảo dự phòng của hệ toạ độ uniformCoordinate.
        /// Nguồn sự thật ưu tiên từ capabilities của thiết bị (baseOutputSize);
        /// giá trị này dùng khi capabilities không trả về.
        /// Xác nhận bằng log thiết bị thật: baseOutputSize = 1920 (LogsAPI/).
        /// </summary>
        public const int BaseOutputSize = 1920;

        /// <summary>
        /// Ngưỡng số lần lỗi xác thực liên tiếp trước khi ngắt mạch bảo vệ.
        /// Thiết bị cho tối thiểu 3 lần (maxIllegalLoginTimes min=3, §9 09-api-reference.md),
        /// nên dừng ở 2 là còn dư 1 lần an toàn.
        /// </summary>
        public const int MaxConsecutiveFailures = 2;

        /// <summary>
        /// Thời gian tạm khoá ngắt mạch bảo vệ chống khoá IP (phút).
        /// Thiết bị khoá mặc định 30 phút (maxIllegalLoginLockTime default=30,
        /// §9 Security/capabilities, 09-api-reference.md). Đặt trùng để breaker
        /// không mở lại sớm hơn thiết bị — mở sớm là gõ vào, gia hạn khoá.
        /// </summary>
        public const int BlockMinutes = 30;
    }
}
