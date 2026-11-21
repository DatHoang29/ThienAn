using Module.VideoWall.Core.Dto.DeviceSetup;

namespace Module.VideoWall.Core.Dto.Device
{
    /// <summary>
    /// Description: Định danh thiết bị đích: dùng ControllerId trong DB, hoặc Device adhoc khi chưa lưu.
    /// Created date: 07/09/2026
    /// </summary>
    public abstract class VwDeviceTargetInput
    {
        public string? ControllerId { get; set; }

        /// <summary>Thông tin kết nối adhoc khi chưa lưu controller vào DB. Tái dùng DTO đã có.</summary>
        public VwISAPIPassthroughDevice? Device { get; set; }
    }

    /// <summary>
    /// Description: Input cho truy vấn InputChannels. Phục vụ VwControllerController.GetInputChannels.
    /// Created date: 07/09/2026
    /// </summary>
    public class VwDeviceInputChannelsInput : VwDeviceTargetInput { }
}
