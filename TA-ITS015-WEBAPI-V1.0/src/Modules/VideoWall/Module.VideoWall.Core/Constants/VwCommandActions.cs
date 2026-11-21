namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Description: Các hằng số định danh hành động điều khiển Video Wall gửi qua kênh NATS
    /// Created date: 10/09/2026
    /// </summary>
    public static class VwCommandActions
    {
        /// <summary>Đồng bộ toàn bộ danh sách cửa sổ của một scene xuống thiết bị</summary>
        public const string SyncSceneWindows = "SYNC_SCENE_WINDOWS";

        /// <summary>Kích hoạt một kịch bản (scene) trên bộ điều khiển Video Wall</summary>
        public const string ActivateScene = "ACTIVATE_SCENE";

        /// <summary>Nâng hoặc hạ thứ tự xếp lớp (Z-Order Top/Bottom) của một cửa sổ</summary>
        public const string SetWindowLayer = "SET_WINDOW_LAYER";

        /// <summary>Xóa trạng thái lỗi xác thực (Device Auth Failure) bảo vệ thiết bị</summary>
        public const string ResetDeviceAuthFailure = "RESET_DEVICE_AUTH_FAILURE";

        // ═══════════════════════════════════════════════════════════════════
        // Nhóm Device Setup (6) — Kịch bản đấu nối bộ điều khiển
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Ping kiểm tra kết nối tới bộ điều khiển</summary>
        public const string DeviceSetupPing = "DEVICE_SETUP_PING";

        /// <summary>Dò quét thông tin phần cứng bộ điều khiển</summary>
        public const string DeviceSetupProbe = "DEVICE_SETUP_PROBE";

        /// <summary>Đồng bộ danh sách nguồn đầu vào/ra từ thiết bị</summary>
        public const string DeviceSetupSyncSources = "DEVICE_SETUP_SYNC_SOURCES";

        /// <summary>Đồng bộ cổng ra + bảng gán cổng → ô từ thiết bị (VwControllerOutput)</summary>
        public const string DeviceSetupSyncOutputs = "DEVICE_SETUP_SYNC_OUTPUTS";

        /// <summary>Đồng bộ màn đang cắm trên các bộ con (VwScreen), ghép vào cổng ra đã lưu</summary>
        public const string DeviceSetupSyncScreens = "DEVICE_SETUP_SYNC_SCREENS";

        /// <summary>Ghi bảng cổng ra → vị trí (WallOutputList) xuống bộ con theo Bố cục gán cổng; DryRun ⇒ chỉ so sánh, không ghi</summary>
        public const string DeviceSetupApplyWallOutputs = "DEVICE_SETUP_APPLY_WALL_OUTPUTS";

        /// <summary>Bật / tắt toàn bộ màn của bộ hiển thị (ScreenCtrl openAll / closeAll)</summary>
        public const string DeviceSetupScreenPower = "DEVICE_SETUP_SCREEN_POWER";

        /// <summary>Đặt độ sáng (đèn nền 0-100) cho từng màn (ScreenCtrl/control effect backlight)</summary>
        public const string DeviceSetupScreenBacklight = "DEVICE_SETUP_SCREEN_BACKLIGHT";

        /// <summary>Đọc trạng thái 1 màn (bật/tắt, độ sáng) từ bộ hiển thị (ScreenCtrl/screenStatus)</summary>
        public const string DeviceSetupScreenStatus = "DEVICE_SETUP_SCREEN_STATUS";

        /// <summary>Dựng kịch bản hiển thị hoàn chỉnh lên thiết bị</summary>
        public const string DeviceSetupScene = "DEVICE_SETUP_SCENE";

        /// <summary>Gửi lệnh ISAPI tùy ý (passthrough) tới thiết bị</summary>
        public const string DeviceSetupSendPassthrough = "DEVICE_SETUP_SEND_PASSTHROUGH";

        /// <summary>Xóa trạng thái lỗi xác thực và xóa cache tường theo ControllerId</summary>
        public const string DeviceSetupResetDeviceAuthFailure = "DEVICE_SETUP_RESET_DEVICE_AUTH_FAILURE";

        /// <summary>Đối chiếu/đồng bộ ngược kịch bản đang chạy thật từ thiết bị về DB</summary>
        public const string DeviceSetupSyncActiveScene = "DEVICE_SETUP_SYNC_ACTIVE_SCENE";

        // ═══════════════════════════════════════════════════════════════════
        // Nhóm Proxy (1) — Truy vấn kênh đầu vào thiết bị
        // ═══════════════════════════════════════════════════════════════════

        public const string DeviceProxyInputChannels = "DEVICE_PROXY_INPUT_CHANNELS";
    }
}
