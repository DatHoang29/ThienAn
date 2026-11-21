namespace Module.VideoWall.Core.Dto.Command
{
    /// <summary>
    /// Description: Dữ liệu chi tiết cho lệnh đồng bộ cửa sổ scene (SYNC_SCENE_WINDOWS)
    /// Created date: 10/09/2026
    /// </summary>
    public class VwSyncWindowsPayload
    {
        /// <summary>ID của kịch bản cần đồng bộ lại toàn bộ cửa sổ</summary>
        public string SceneId { get; set; } = string.Empty;

        /// <summary>ID cửa sổ bị loại trừ (nếu là thao tác xoá cửa sổ)</summary>
        public string? ExcludeWindowId { get; set; }

        /// <summary>Danh sách ID các cửa sổ bị loại trừ (nếu xoá hàng loạt)</summary>
        public List<string>? ExcludeWindowIds { get; set; }
    }

    /// <summary>
    /// Description: Dữ liệu chi tiết cho lệnh kích hoạt kịch bản (ACTIVATE_SCENE)
    /// Created date: 10/09/2026
    /// </summary>
    public class VwActivateScenePayload
    {
        /// <summary>ID kịch bản</summary>
        public string SceneId { get; set; } = string.Empty;

        /// <summary>Mã số kịch bản trên thiết bị Hikvision (Scene ID 1..N)</summary>
        public string? OutputId { get; set; }

        /// <summary>Danh sách controllerId cần gửi lệnh kích hoạt</summary>
        public List<string> TargetControllerIds { get; set; } = [];
    }

    /// <summary>
    /// Description: Dữ liệu chi tiết cho lệnh đổi layer Z-Order cửa sổ (SET_WINDOW_LAYER)
    /// Created date: 10/09/2026
    /// </summary>
    public class VwSetWindowLayerPayload
    {
        /// <summary>ID bộ điều khiển quản lý cửa sổ</summary>
        public string ControllerId { get; set; } = string.Empty;

        /// <summary>Mã số cửa sổ phía phần cứng Hikvision (DeviceWindowId 1..N)</summary>
        public string DeviceWindowId { get; set; } = string.Empty;

        /// <summary>Hành động xếp lớp: Top (nâng lên trên cùng) hoặc Bottom (hạ xuống dưới cùng)</summary>
        public string Action { get; set; } = string.Empty;
    }

    /// <summary>
    /// Description: Dữ liệu chi tiết cho lệnh xoá trạng thái lỗi xác thực (RESET_DEVICE_AUTH_FAILURE)
    /// Created date: 10/09/2026
    /// </summary>
    public class VwResetDeviceAuthFailurePayload
    {
        /// <summary>Địa chỉ IP hoặc khoá định danh bộ điều khiển cần xoá trạng thái lỗi xác thực</summary>
        public string TargetIpOrKey { get; set; } = string.Empty;
    }
}
