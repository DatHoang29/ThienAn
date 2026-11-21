using Modules.VDS.Core.Constants;

namespace Modules.VDS.Controllers.Media.Dto;

/// <summary>
/// Author: KienLT
/// Description: Khóa tra file media của một bản tin VDS.
///
/// <para>DTO này KHÔNG lộ ra swagger: action <c>GetFile</c> giữ nguyên ba tham số
/// <c>eventId</c>/<c>detectTime</c>/<c>kind</c> trên query string để thẻ ảnh của FE không phải đổi,
/// controller tự dựng DTO rồi dispatch qua message bus.</para>
/// <para><b>Không còn <c>[Required]</c> trên khóa kép (bỏ 23/09/2026):</b> value type không nullable luôn có
/// giá trị nên attribute không chặn được <c>0</c> hay <c>0001-01-01</c>. Miền hợp lệ do
/// <c>VdsEventKeyRules</c> định nghĩa; handler kiểm và trả <c>MediaFileStatus.KeyInvalid</c> để endpoint này
/// giữ đúng hình dạng <c>ProblemDetails</c> 400.</para>
/// Created date: 21/09/2026
/// </summary>
public class MediaFileInput
{
    public long EventId { get; set; }
    public DateTime DetectTime { get; set; }

    /// <summary>Nhánh media: full / vehicle / plate / violation / snapshot / clip.</summary>
    public string Kind { get; set; } = VdsMediaKindConst.Full;
}
