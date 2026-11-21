namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Author: Đạt
    /// Description: Giá trị cột VwEventTriggerLog.Action.
    ///
    ///              Gom vào một chỗ vì cột này được ghi từ hai nơi (VwDeviceSetupCommandHandler và
    ///              VwSceneWorkflowCommandHandler) rồi lại được LỌC từ nơi thứ ba (tab Log của công
    ///              cụ WPF). Gõ chuỗi rời rạc ở ba nơi là kiểu sai âm thầm: lọc không ra bản ghi nào
    ///              mà không có lỗi nào để lần.
    ///
    ///              Độ dài phải nằm trong EntityConst.Length32.
    /// Created date: 26/08/2026
    /// </summary>
    public static class VwEventTriggerAction
    {
        /// <summary>Xác nhận Digest Auth vào bộ điều khiển.</summary>
        public const string Ping = "Ping";

        /// <summary>Khảo sát thiết bị bằng các lệnh chỉ-đọc.</summary>
        public const string Probe = "Probe";

        /// <summary>Nạp videoInputChannelID thật của thiết bị vào VwSource.SignalNo.</summary>
        public const string SyncSources = "SyncSources";

        /// <summary>Dựng kịch bản hiển thị xuống thiết bị.</summary>
        public const string SetupScene = "SetupScene";

        /// <summary>Chuyển tiếp ISAPI thô do người vận hành tự gõ Method/Path/Body.</summary>
        public const string Isapi = "Isapi";

        /// <summary>Kích hoạt kịch bản lên tường qua luồng nghiệp vụ VwScene/Active.</summary>
        public const string ActiveScene = "ActiveScene";

        /// <summary>Đối chiếu/đồng bộ ngược kịch bản đang chạy thật từ thiết bị về CSDL.</summary>
        public const string SyncActiveScene = "SyncActiveScene";

        /// <summary>Đồng bộ cổng ra từ thiết bị về CSDL.</summary>
        public const string SyncOutputs = "SyncOutputs";

        /// <summary>Đồng bộ màn đang cắm (bộ con) từ thiết bị về CSDL.</summary>
        public const string SyncScreens = "SyncScreens";

        /// <summary>Ghi bảng cổng ra → vị trí (WallOutputList) xuống bộ con theo Bố cục gán cổng (07/11/2026).</summary>
        public const string ApplyWallOutputs = "ApplyWallOutputs";

        /// <summary>Bật / tắt toàn bộ màn của bộ hiển thị (11/11/2026).</summary>
        public const string ScreenPower = "ScreenPower";

        /// <summary>Đặt độ sáng (đèn nền) cho màn của bộ hiển thị (11/11/2026).</summary>
        public const string ScreenBacklight = "ScreenBacklight";

        /// <summary>Tên bước tổng hợp khi lệnh ném ngoại lệ nên không có bước ISAPI nào để ghi.</summary>
        public const string FailedStepName = "Lệnh không hoàn thành";
    }
}
