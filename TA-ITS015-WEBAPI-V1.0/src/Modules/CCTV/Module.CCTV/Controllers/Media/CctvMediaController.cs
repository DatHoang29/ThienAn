using Module.CCTV.Controllers.CctvMedia.Dto;
using Modules.CCTV.Controllers;

namespace Module.CCTV.Controllers.CctvMedia;

[ApiDescriptionSettings(GroupName, Order = 460)]
public class CctvMediaController : BaseController
{
    [DisplayName("CCTV - Lấy WSS URL WebRTC (Live hoặc Playback)")]
    public async Task<CctvWebRtcStreamOutput> GetWebRtcStreamUrl([FromQuery] CctvWebRtcUrlInput input)
    {
        return await MessBus.InvokeAsync<CctvWebRtcStreamOutput>(input);
    }


    [DisplayName("CCTV - Lấy URL tải file video archive")]
    public async Task<CctvMediaDownloadOutput> GetMediaDownloadUrl([FromQuery] CctvMediaDownloadInput input)
    {
        return await MessBus.InvokeAsync<CctvMediaDownloadOutput>(input);
    }

    [DisplayName("CCTV - Chụp ảnh snapshot từ camera")]
    public async Task<IActionResult> GetSnapshot([FromQuery] CctvSnapshotInput input)
    {
        var result = await MessBus.InvokeAsync<CctvSnapshotOutput>(input);
        return new FileContentResult(result.Data, result.ContentType)
        {
            FileDownloadName = result.FileName
        };
    }

    [DisplayName("CCTV - Lấy URL ảnh snapshot qua Cctv Proxy (xem dạng ảnh ở mạng public)")]
    public async Task<CctvSnapshotUrlOutput> GetSnapshotUrl([FromQuery] CctvSnapshotUrlInput input)
    {
        return await MessBus.InvokeAsync<CctvSnapshotUrlOutput>(input);
    }

    [DisplayName("CCTV - Lấy danh sách footage đã ghi của camera")]
    public async Task<CctvFootageOutput> GetFootage([FromQuery] CctvFootageInput input)
    {
        return await MessBus.InvokeAsync<CctvFootageOutput>(input);
    }
}
