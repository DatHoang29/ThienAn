using Modules.Media.Controllers.ItsMedia.Incident.Dto;
using Modules.Media.Infrastructure.Config;
using Modules.TMS.Core.Dto.Incident;

namespace Modules.Media.Controllers.ItsMedia.Incident.Queries;

/// <summary>
/// Description: [ITS] Lấy ảnh camera của sự cố. Dùng lại nguyên IncidentImageQueryHandler (module TMS)
/// để giữ đúng logic VDS/camera/slot, chỉ đổi sang DTO của Media.
/// Created date: 08/10/2026
/// </summary>
public class ItsMediaIncidentQueryHandler
{
    private readonly IMessageBus _messBus;
    private readonly MediaConfigService _configService;

    public ItsMediaIncidentQueryHandler(IMessageBus messBus, MediaConfigService configService)
    {
        _messBus = messBus;
        _configService = configService;
    }

    public async Task<List<ItsMediaIncidentOutput>> HandleAsync(ItsMediaIncidentInput input, CancellationToken ct)
    {
        var config = await _configService.GetAsync(ct);
        if (!config.Enabled(MediaConst.Subsystem.Incident)) return new();

        var images = await _messBus.InvokeAsync<List<TmsIncidentImageOutput>>(
            new TmsIncidentImageInput { ID = input.ID, PictureType = input.PictureType }, ct) ?? new();

        return images.Select(x => new ItsMediaIncidentOutput
        {
            IncidentId = x.IncidentId,
            EquipmentId = x.EquipmentId,
            EquipmentName = x.EquipmentName,
            KmNumber = x.KmNumber,
            MetNumber = x.MetNumber,
            PictureType = x.PictureType,
            CaptureTime = x.CaptureTime,
            ImageCapturedAt = x.ImageCapturedAt,
            ImageSource = x.ImageSource,
            WaitUntilUtc = x.WaitUntilUtc,
            Exists = x.Exists && !string.IsNullOrEmpty(x.Base64),
            Status = string.IsNullOrWhiteSpace(x.Status) ? MediaConst.Status.Ready : x.Status,
            ContentType = string.IsNullOrWhiteSpace(x.ContentType) ? "image/jpeg" : x.ContentType,
            Base64 = x.Base64 ?? string.Empty,
            // Url của ITS là đường dẫn nội bộ site ITS, không dùng được ở WP -> bỏ.
            Url = string.Empty,
            Version = x.CaptureTime.Ticks.ToString()
        }).ToList();
    }
}
