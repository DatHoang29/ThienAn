using ITS.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Dto.DeviceSetup;

namespace ITS.VideoWall.Core.Dto.DeviceSetup
{
    /// <summary>
    /// Description: Kết quả khảo sát năng lực thiết bị VideoWall qua ISAPI (VwProbeDeviceOutput).
    /// Created date: 14/09/2026
    /// </summary>
    public class VwProbeDeviceOutput
    {
        public string? ControllerId { get; set; }

        public string? ControllerName { get; set; }

        public string? Ip { get; set; }

        public bool Reachable { get; set; }

        public bool? IsSupportScene { get; set; }

        public int? MaxWindowNums { get; set; }

        /// <summary>Trần số SID kịch bản — VwScene.OutputId phải nằm trong 1..giá trị này.</summary>
        public int? MaxSceneNums { get; set; }

        public int? BaseOutputSize { get; set; }

        /// <summary>Model thiết bị backend đang giả định để áp dụng profile (mặc định DS-C66S-H88-CL hoặc override qua JSON).</summary>
        public string? AssumedModel { get; set; }

        public int? WallNo { get; set; }

        public List<VwWallSummary> Walls { get; set; } = [];

        public List<VwISAPIOutputItem> Outputs { get; set; } = [];

        public List<VwISAPIInputChannel> InputChannels { get; set; } = [];

        /// <summary>VwScreen.OutputId không khớp cổng ra nào của thiết bị.</summary>
        public List<VwDeviceMismatch> ScreenMismatches { get; set; } = [];

        /// <summary>VwSource.SignalNo không khớp videoInputChannelID nào của thiết bị.</summary>
        public List<VwDeviceMismatch> SourceMismatches { get; set; } = [];

        public List<VwSetupSceneStep> Steps { get; set; } = [];
    }

    /// <summary>
    /// Description: Kết quả đồng bộ nguồn tín hiệu đầu vào từ thiết bị về CSDL (VwSyncSourcesOutput).
    /// Created date: 14/09/2026
    /// </summary>
    public class VwSyncSourcesOutput
    {
        public string? ControllerId { get; set; }

        public bool Applied { get; set; }

        public int UpdatedCount { get; set; }

        public List<VwDeviceMismatch> Changes { get; set; } = [];

        public List<VwISAPIInputChannel> InputChannels { get; set; } = [];

        public List<VwSetupSceneStep> Steps { get; set; } = [];
    }

    /// <summary>
    /// Description: Kết quả đồng bộ cổng ra + màn đang cắm từ thiết bị (VwSyncOutputsOutput).
    /// Created date: 06/11/2026
    /// </summary>
    /// <summary>
    /// Description: Kết quả ghi bảng cổng ra → vị trí xuống bộ con (DEVICE_SETUP_APPLY_WALL_OUTPUTS).
    ///              DryRun ⇒ chỉ có bảng so sánh, không ghi. BackupXml = cấu hình tường đọc được TRƯỚC khi ghi (để khôi phục).
    /// Created date: 07/11/2026
    /// </summary>
    public class VwApplyWallOutputsOutput
    {
        public bool DryRun { get; set; }
        public List<VwApplyWallOutputsControllerResult> Controllers { get; set; } = [];
        public List<VwSetupSceneStep> Steps { get; set; } = [];
    }

    public class VwApplyWallOutputsControllerResult
    {
        public string? ControllerId { get; set; }
        public string? ControllerName { get; set; }
        public int? WallNo { get; set; }
        /// <summary>Đọc / tính / (ghi + đọc lại khớp) thành công.</summary>
        public bool Success { get; set; }
        /// <summary>Đã ghi xuống thiết bị (false khi DryRun hoặc không có thay đổi).</summary>
        public bool Applied { get; set; }
        public string? Message { get; set; }
        public List<VwWallOutputChange> Changes { get; set; } = [];
        public string? BackupXml { get; set; }
    }

    /// <summary>Một cổng: vị trí trên thiết bị hiện tại → vị trí theo Bố cục gán cổng. Change: unchanged | moved | added | removed.</summary>
    public class VwWallOutputChange
    {
        public string? OutputId { get; set; }
        public int DeviceOutputId { get; set; }
        public string? Name { get; set; }
        public string Change { get; set; } = "unchanged";
        public int[]? OldRect { get; set; }
        public int[]? NewRect { get; set; }
    }

    /// <summary>
    /// Description: Kết quả lệnh điều khiển màn (bật / tắt, độ sáng) theo từng bộ hiển thị.
    /// Created date: 11/11/2026
    /// </summary>
    public class VwScreenControlOutput
    {
        public List<VwScreenControlControllerResult> Controllers { get; set; } = [];
        public List<VwSetupSceneStep> Steps { get; set; } = [];
    }

    public class VwScreenControlControllerResult
    {
        public string? ControllerId { get; set; }
        public string? ControllerName { get; set; }
        public int? WallNo { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>Trạng thái 1 màn đọc từ thiết bị (DEVICE_SETUP_SCREEN_STATUS). Backlight = độ sáng 0-100.</summary>
    public class VwScreenStatusOutput
    {
        public string? ScreenId { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        /// <summary>on / off.</summary>
        public string? SwitchStatus { get; set; }
        public int? Backlight { get; set; }
    }

    public class VwSyncOutputsOutput
    {
        /// <summary>Kết quả theo từng bộ điều khiển đã đồng bộ.</summary>
        public List<VwSyncOutputsControllerResult> Controllers { get; set; } = [];

        public List<VwSetupSceneStep> Steps { get; set; } = [];
    }

    public class VwSyncOutputsControllerResult
    {
        public string? ControllerId { get; set; }
        public string? ControllerName { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? WallNo { get; set; }
        public int OutputCount { get; set; }
        public int AddedOutputs { get; set; }
        public int MissingOutputs { get; set; }
        public int ScreenCount { get; set; }
    }

    /// <summary>
    /// Description: Kết quả thiết lập/dựng kịch bản hiển thị xuống thiết bị VideoWall (VwSetupSceneOutput).
    /// Created date: 14/09/2026
    /// </summary>
    public class VwSetupSceneOutput
    {
        public bool Success { get; set; }

        public bool DryRun { get; set; }

        public string? ControllerId { get; set; }

        public string? ControllerName { get; set; }

        public int? WallNo { get; set; }

        public string? SceneId { get; set; }

        public string? SceneCode { get; set; }

        /// <summary>SID của kịch bản trên thiết bị — lấy từ VwScene.OutputId.</summary>
        public string? SceneSid { get; set; }

        /// <summary>Trần số SID thiết bị nhận, đọc từ capabilities — dùng để kiểm SceneSid hợp lệ.</summary>
        public int? MaxSceneNums { get; set; }

        public string? Message { get; set; }

        /// <summary>Danh sách tường đọc được, đánh dấu tường mà kịch bản đã chọn.</summary>
        public List<VwWallSummary> Walls { get; set; } = [];

        public List<VwSetupSceneStep> Steps { get; set; } = [];

        public List<VwSetupWindowResult> Windows { get; set; } = [];

        /// <summary>
        /// Ảnh chụp cửa sổ đang có trên tường TRƯỚC khi kịch bản ghi. Giữ lại để dựng ngược
        /// khi cần hoàn tác — thiết bị không có API undo.
        /// </summary>
        public List<VwISAPIWallWindowItem> WindowSnapshot { get; set; } = [];
    }
}
