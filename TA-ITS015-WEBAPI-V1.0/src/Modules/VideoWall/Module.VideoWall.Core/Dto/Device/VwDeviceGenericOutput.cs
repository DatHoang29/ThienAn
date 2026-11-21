using Module.VideoWall.Core.Dto.DeviceSetup;

namespace Module.VideoWall.Core.Dto.Device
{
    /// <summary>
    /// Description: Kết quả bọc chung cho các thao tác thiết bị VideoWall, bao gồm dữ liệu phản hồi và bước thực thi timeline
    /// Created date: 11/09/2026
    /// </summary>
    public class VwDeviceGenericOutput<T>
    {
        public T? Data { get; set; }
        public VwSetupSceneStep? Step { get; set; }
    }
}
