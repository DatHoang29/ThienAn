using Modules.VDS.Infrastructure.Services.Filters.Mvc;
using Microsoft.Extensions.Localization;
using Modules.VDS.Controllers.Media.Dto;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Exceptions;

namespace Modules.VDS.Controllers.Media;

/// <summary>
/// Author: KienLT
/// Description: API trả file media của bản tin VDS (ảnh toàn cảnh / phương tiện / biển số / vi phạm / clip).
///
/// <para>Ba nhánh lỗi giữ NGUYÊN mã HTTP 400/503/404 thay vì ném <c>Oops.Oh</c>: đây là endpoint trả file
/// cho thẻ ảnh của FE, đổi mã trạng thái là đổi cách FE phân biệt "sai tham số" với "ảnh chưa có". Vì vậy
/// handler trả về <see cref="MediaFileStatus"/> để controller dựng đúng mã.</para>
///
/// <para><b>Cả ba nhánh dùng CÙNG một hình dạng <see cref="ProblemDetails"/></b>, khớp với
/// <c>VdsExceptionFilter</c>. Trước 21/09/2026 mỗi nhánh một kiểu: 400 trả chuỗi trần, 503 và 404 trả
/// anonymous <c>{ message }</c> — ba hình dạng lỗi trên cùng một endpoint.</para>
/// Created date: 21/09/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 591)]
[ApiController]
public class VdsMediaController(IStringLocalizer lz) : BaseController
{
    /// <summary>
    /// Get VDS media file
    /// </summary>
    /// <param name="eventId">Mã bản tin của vendor.</param>
    /// <param name="detectTime">Mốc phát hiện (UTC) — cùng eventId tạo thành khóa kép trên QuestDB.</param>
    /// <param name="kind">Nhánh media: full / vehicle / plate / violation / snapshot / clip.</param>
    /// <returns>Nội dung file, hoặc ProblemDetails với mã 400 / 503 / 404.</returns>
    /// <remarks>
    /// <c>[Required]</c> ở đây CHỈ để OpenAPI đánh dấu tham số bắt buộc; nó không chặn được <c>0</c> hay
    /// <c>0001-01-01</c>. Khóa ngoài miền hợp lệ trả về 400 qua nhánh <c>KeyInvalid</c>.
    /// </remarks>
    [HttpGet("File")]
    [ProducesResponseType(typeof(FileResult), 200, "application/octet-stream")]
    [DisplayName("Media file")]
    public async Task<IActionResult> GetFile([Required] long eventId, [Required] DateTime detectTime, string kind = VdsMediaKindConst.Full)
    {
        var media = await MessBus.InvokeAsync<MediaFileOutput>(
            new MediaFileInput { EventId = eventId, DetectTime = detectTime, Kind = kind });

        return media.Status switch
        {
            MediaFileStatus.KeyInvalid => Problem(400, media.MessageKey!),
            MediaFileStatus.KindInvalid => Problem(400, BaseMsg.Vds.Exception.MediaKindInvalid),
            MediaFileStatus.StorageNotConfigured => Problem(503, BaseMsg.Vds.Exception.MediaStorageNotConfigured),
            MediaFileStatus.NotFound => Problem(404, BaseMsg.Vds.Exception.MediaNotFound),
            _ => new FileStreamResult(media.Content!, media.ContentType!) { EnableRangeProcessing = true }
        };
    }

    private ObjectResult Problem(int status, string messageKey)
        => new(new ProblemDetails { Status = status, Title = lz[messageKey].Value }) { StatusCode = status };
}
