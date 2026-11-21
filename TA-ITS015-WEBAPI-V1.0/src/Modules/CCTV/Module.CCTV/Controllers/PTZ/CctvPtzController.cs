using Module.CCTV.Controllers.CctvPTZ.Dto;
using Modules.CCTV.Controllers;

namespace Module.CCTV.Controllers.CctvPTZ;


[ApiDescriptionSettings(GroupName, Order = 471)]
public class CctvPtzController : BaseController
{
    [DisplayName("CCTV PTZ di chuyển đến vị trí mong muốn")]
    [ApiDescriptionSettings(Name = "MovePtzPosition"), HttpPost]
    public async Task<CctvActionOutput> MovePtzPosition(CctvPtzPositionInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }


    [DisplayName("CCTV PTZ nhích từng bước")]
    [ApiDescriptionSettings(Name = "NudgePtz"), HttpPost]
    public async Task<CctvActionOutput> NudgePtz(CctvPtzNudgeInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }

    [DisplayName("CCTV PTZ dừng")]
    [ApiDescriptionSettings(Name = "StopPtz"), HttpPost]
    public async Task<CctvActionOutput> StopPtz(CctvPtzStopInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }

    [DisplayName("CCTV Kích hoạt PTZ preset")]
    [ApiDescriptionSettings(Name = "ActivatePreset"), HttpPost]
    public async Task<CctvActionOutput> ActivatePreset(CctvActivatePresetInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }

    //[DisplayName("CCTV Kích hoạt PTZ tour")]
    //[ApiDescriptionSettings(Name = "ActivateTour"), HttpPost]
    //public async Task<CctvActionOutput> ActivateTour(CctvActivateTourInput input)
    //{
    //    return await MessBus.InvokeAsync<CctvActionOutput>(input);
    //}

    [DisplayName("CCTV Lấy vị trí PTZ hiện tại")]
    [ApiDescriptionSettings(Name = "GetPosition")]
    public async Task<CctvPtzPositionOutput> GetPosition([FromQuery] CctvGetPtzPositionInput input)
    {
        return await MessBus.InvokeAsync<CctvPtzPositionOutput>(input);
    }

    [DisplayName("CCTV Thêm Preset PTZ tại vị trí hiện tại")]
    [ApiDescriptionSettings(Name = "AddPreset"), HttpPost]
    public async Task<CctvActionOutput> AddPreset(CctvAddPresetInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }

    [DisplayName("CCTV Đổi tên Preset PTZ")]
    [ApiDescriptionSettings(Name = "UpdatePreset"), HttpPost]
    public async Task<CctvActionOutput> UpdatePreset(CctvUpdatePresetInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }

    [DisplayName("CCTV Lấy giới hạn PTZ")]
    public async Task<CctvPtzLimitsOutput> GetLimit([FromQuery] CctvGetPtzLimitsInput input)
    {
        return await MessBus.InvokeAsync<CctvPtzLimitsOutput>(input);
    }

    [DisplayName("CCTV Lấy danh sách Preset")]
    [ApiDescriptionSettings(Name = "GetPresets")]
    public async Task<List<CctvPtzPresetOutput>> GetPreset([FromQuery] CctvGetPresetInput input)
    {
        return await MessBus.InvokeAsync<List<CctvPtzPresetOutput>>(input);
    }

    [DisplayName("CCTV Xóa Preset"), HttpPost]
    [ApiDescriptionSettings(Name = "DeletePreset")]
    public async Task<CctvActionOutput> DeletePreset(CctvDeletePresetInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }

    //[DisplayName("CCTV Lấy danh sách Tour")]
    //public async Task<List<CctvPtzTourOutput>> GetTours([FromQuery] CctvGetToursInput input)
    //{
    //    return await MessBus.InvokeAsync<List<CctvPtzTourOutput>>(input);
    //}


    [DisplayName("CCTV PTZ zoom liên tục")]
    [ApiDescriptionSettings(Name = "ZoomPtz"), HttpPost]
    public async Task<CctvActionOutput> ZoomPtz(CctvPtzZoomInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }

    [DisplayName("CCTV PTZ focus vào/ra")]
    [ApiDescriptionSettings(Name = "FocusPtz"), HttpPost]
    public async Task<CctvActionOutput> FocusPtz(CctvPtzFocusInput input)
    {
        return await MessBus.InvokeAsync<CctvActionOutput>(input);
    }

}

