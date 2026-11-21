using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Modules.VDS.Core.Exceptions;

namespace Modules.VDS.Infrastructure.Services.Filters.Mvc;

/// <summary>
/// Author: KienLT
/// Description: Đổi <see cref="QuestDbUnavailableException"/> thành HTTP 503 kèm <c>Retry-After</c>.
///
/// <para><b>Vì sao 503 chứ không để lọt lên global exception:</b> QuestDB không khả dụng là lỗi TẠM THỜI của
/// một nguồn dữ liệu, không phải lỗi request. 503 + <c>Retry-After</c> cho client biết nên thử lại sau bao
/// lâu thay vì coi như hỏng hẳn.</para>
///
/// <para><b>Không trả <c>ex.Message</c> ra ngoài nữa</b> (trước 21/09/2026 có trả): thông điệp nội bộ của
/// tầng persistence có thể chứa chuỗi kết nối, tên host, tên bảng. Chi tiết chẩn đoán thuộc log, không thuộc
/// response. Phần chữ hiển thị đi qua <see cref="IStringLocalizer"/> nên đổi được theo ngôn ngữ, thay vì
/// tiêu đề tiếng Anh cứng như trước.</para>
/// Created date: 08/09/2026
/// </summary>
public sealed class VdsExceptionFilter(IStringLocalizer localizer) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not QuestDbUnavailableException ex) return;

        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(ex.RetryAfter.TotalSeconds));
        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = 503,
            Title = localizer[BaseMsg.Vds.Exception.DataTemporarilyUnavailable].Value,
            Detail = localizer[BaseMsg.Vds.Exception.RetryAfterSeconds].Value
        })
        { StatusCode = 503 };
        context.ExceptionHandled = true;
    }
}
