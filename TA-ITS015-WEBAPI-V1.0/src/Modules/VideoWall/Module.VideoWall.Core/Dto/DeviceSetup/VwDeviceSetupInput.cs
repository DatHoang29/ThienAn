using Shared.Core.Utilities;

namespace Module.VideoWall.Core.Dto.DeviceSetup
{
    /// <summary>Xác nhận Digest Auth vào bộ điều khiển. ID = VwController.ID.</summary>
    public class VwPingDeviceInput : BaseIdInput { }

    /// <summary>
    /// Đọc danh sách cổng vào vật lý (GET inputs/channels) của bộ điều khiển để chọn khi khai báo nguồn.
    /// ID = VwController.ID. Chỉ đọc thiết bị, không ghi DB; kết quả về FE qua NATS (theo MessageId).
    /// </summary>
    public class VwInputChannelsInput : BaseIdInput { }

    /// <summary>
    /// Khảo sát thiết bị bằng các lệnh chỉ-đọc: capabilities, danh sách tường, cổng ra, kênh vào.
    /// Không phát bất kỳ lệnh ghi nào nên gọi lúc nào cũng an toàn.
    /// </summary>
    public class VwProbeDeviceInput : BaseIdInput
    {
        /// <summary>Bỏ trống để tự dò tường đang gắn màn hình.</summary>
        public int? WallNo { get; set; }
    }

    /// <summary>Nạp videoInputChannelID thật của thiết bị vào VwSource.SignalNo.</summary>
    public class VwSyncSourcesInput : BaseIdInput
    {
        /// <summary>Tắt (mặc định): chỉ trả về bảng đối chiếu, không ghi DB.</summary>
        public bool Apply { get; set; }
    }

    /// <summary>
    /// Đồng bộ cổng ra (bảng gán cổng → ô) từ thiết bị về VwControllerOutput.
    /// ID = VwController.ID; bỏ trống ⇒ BE gửi lệnh cho mọi bộ điều khiển (bộ trung tâm + bộ con).
    /// </summary>
    public class VwSyncOutputsInput
    {
        public string? ID { get; set; }

        /// <summary>Bỏ trống để tự dò tường đang gắn màn hình.</summary>
        public int? WallNo { get; set; }
    }

    /// <summary>
    /// Đồng bộ màn đang cắm trên bộ con (GetWallScreenInfo) về VwScreen, ghép vào cổng ra đã lưu.
    /// ID = VwController.ID của bộ con; bỏ trống ⇒ mọi bộ con.
    /// </summary>
    public class VwSyncScreensInput
    {
        public string? ID { get; set; }

        /// <summary>Bỏ trống ⇒ tường của cổng ra đã lưu.</summary>
        public int? WallNo { get; set; }
    }

    /// <summary>
    /// Description: Ghi bảng cổng ra → vị trí (WallOutputList) xuống bộ con theo Bố cục gán cổng.
    ///              Vị trí cổng = ô của màn đang giữ cổng, tính trong vùng ô của bộ đó (ô góc trên-trái của vùng = (0,0)), 1 ô = BaseOutputSize.
    ///              Cổng không gán màn ⇒ bỏ khỏi tường. DryRun = true ⇒ chỉ đọc thiết bị và trả bảng so sánh, không ghi.
    /// Created date: 07/11/2026
    /// </summary>
    public class VwApplyWallOutputsInput
    {
        /// <summary>Bộ con cần áp dụng; trống ⇒ mọi bộ hiển thị / độc lập có IP.</summary>
        public List<string>? ControllerIds { get; set; }

        public bool DryRun { get; set; } = true;
    }

    /// <summary>
    /// Description: Bật / tắt toàn bộ màn của bộ hiển thị (PUT ScreenCtrl/openAll | closeAll trên bộ con).
    /// Created date: 11/11/2026
    /// </summary>
    public class VwScreenPowerInput
    {
        /// <summary>Bộ hiển thị cần bật/tắt; trống ⇒ mọi bộ hiển thị / độc lập có IP (toàn tường).</summary>
        public List<string>? ControllerIds { get; set; }

        /// <summary>true = bật (openAll), false = tắt (closeAll).</summary>
        public bool On { get; set; }
    }

    /// <summary>
    /// Description: Đặt độ sáng (đèn nền 0-100) cho từng màn (PUT ScreenCtrl/control, effect backlight, rect cổng ra của màn).
    /// Created date: 11/11/2026
    /// </summary>
    public class VwScreenBacklightInput
    {
        /// <summary>VwScreen.ID cần chỉnh (màn phải đang gán cổng ra).</summary>
        public List<string> ScreenIds { get; set; } = [];

        /// <summary>Đèn nền 0-100 (Worker kiểm tra khoảng).</summary>
        public int Value { get; set; }
    }

    /// <summary>
    /// Đọc trạng thái 1 màn từ thiết bị (bật/tắt, độ sáng) — ID = VwScreen.ID, màn phải đang gán cổng ra (11/11/2026).
    /// Chậm (~3,5 s / màn) nên đọc khi chọn màn, không đọc trong Đồng bộ màn.
    /// </summary>
    public class VwScreenStatusInput : BaseIdInput { }

    /// <summary>Đối chiếu/đồng bộ ngược kịch bản đang chạy thật trên thiết bị về CSDL. ID = VwController.ID.</summary>
    public class VwSyncActiveSceneInput : BaseIdInput
    {
        /// <summary>Bỏ trống để tự dò tường đang gắn màn hình.</summary>
        public int? WallNo { get; set; }
    }

    /// <summary>
    /// Kịch bản dựng một kịch bản hiển thị xuống thiết bị: khảo sát → đẩy cửa sổ → lưu → kích hoạt.
    /// Ba cờ DryRun / ResetWindows / Activate quyết định mức tác động lên tường đang chiếu.
    /// </summary>
    public class VwSetupSceneInput
    {
        public string? ControllerId { get; set; }

        /// <summary>Ưu tiên SceneId; bỏ trống thì tra theo SceneCode.</summary>
        public string? SceneId { get; set; }

        public string? SceneCode { get; set; }

        public int? WallNo { get; set; }

        /// <summary>
        /// Mặc định bật: chạy trọn phần khảo sát và dựng sẵn payload XML nhưng KHÔNG phát lệnh ghi.
        /// </summary>
        public bool DryRun { get; set; } = true;

        /// <summary>
        /// Xoá toàn bộ cửa sổ đang có trên tường trước khi dựng lại. Mặc định tắt vì
        /// DELETE .../windows xoá trắng cả những cửa sổ do người khác tạo.
        /// </summary>
        public bool ResetWindows { get; set; }

        /// <summary>Nạp lại VwSource.SignalNo từ thiết bị trước khi dựng cửa sổ.</summary>
        public bool SyncSources { get; set; }

        /// <summary>Kích hoạt kịch bản sau khi lưu — bước duy nhất làm đổi hình đang chiếu.</summary>
        public bool Activate { get; set; }
    }

    /// <summary>Xoá trạng thái lỗi xác thực thiết bị (Device Auth Failure). Bỏ trống ControllerId để xoá toàn bộ.</summary>
    public class VwResetDeviceAuthFailureInput
    {
        public string? ControllerId { get; set; }
    }

    /// <summary>Thông tin kết nối adhoc khi không dùng ControllerId có sẵn trong DB.</summary>
    public class VwISAPIPassthroughDevice
    {
        public string? Ip { get; set; }

        public int? Port { get; set; }

        public string? Account { get; set; }

        public string? Password { get; set; }

        public bool SkipPasswordCheck { get; set; }
    }

    /// <summary>
    /// Chuyển tiếp ISAPI thô: gửi HTTP request tùy ý xuống thiết bị, trả về raw request/response.
    /// Bắt buộc cung cấp đúng 1 trong ControllerId hoặc Device — validate ở tầng service.
    /// </summary>
    public class VwISAPIPassthroughInput
    {
        public string? ControllerId { get; set; }

        public VwISAPIPassthroughDevice? Device { get; set; }

        /// <summary>HTTP method: GET, POST, PUT, DELETE.</summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn tương đối trên thiết bị, không bắt đầu bằng "/".
        /// Ví dụ: "ISAPI/DisplayDev/VideoWall/capabilities".
        /// </summary>
        public string Path { get; set; } = string.Empty;

        public string? Body { get; set; }

        public string? ContentType { get; set; }
    }
}
