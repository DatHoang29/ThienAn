using Module.CCTV.Controllers.CctvDevices.Dto;
using Modules.CCTV.Controllers;

namespace Module.CCTV.Controllers.Cctvevices;

/// <summary>
/// Cctv Device - Cctv integration
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 470)]
public class CctvDeviceController : BaseController
{
    [DisplayName("CCTV Đồng bộ thiết bị")]
    [ApiDescriptionSettings(Name = "SyncDevice"), HttpPost]
    public async Task<CctvDeviceSyncOutput> SyncDevice(CctvGetDeviceInput input)
    {
        return await MessBus.InvokeAsync<CctvDeviceSyncOutput>(input);
    }
    
    [DisplayName("CCTV Lấy danh sách thiết bị")]
    public async Task<List<CctvDeviceOutput>> GetDeviceList([FromQuery] CctvGetDevicesListInput input)
    {
        return await MessBus.InvokeAsync<List<CctvDeviceOutput>>(input);
    }

    [DisplayName("CCTV Lấy danh sách camera tổng hợp")]
    public async Task<List<CctvDeviceViewOutput>> GetDeviceView([FromQuery] CctvGetDeviceViewInput input)
    {
        return await MessBus.InvokeAsync<List<CctvDeviceViewOutput>>(input);
    }

    [DisplayName("CCTV Bật/tắt trạng thái sử dụng thiết bị")]
    [ApiDescriptionSettings(Name = "Active"), HttpPost]
    public async Task ToggleDeviceStatus(CctvDeviceActiveInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    [DisplayName("CCTV Xóa thiết bị")]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    public async Task DeleteDevice(CctvDeleteDeviceInput input)
    {
        await MessBus.InvokeAsync(input);
    }
} 