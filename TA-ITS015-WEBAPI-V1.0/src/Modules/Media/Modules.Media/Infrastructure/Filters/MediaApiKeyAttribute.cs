using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Modules.Media.Infrastructure.Config;

namespace Modules.Media.Infrastructure.Filters;

/// <summary>
/// Description: Kiểm tra header X-Media-Key khớp media_api_key trong SysOpConfig.
/// Dùng cho ITS-Media (chỉ site WP được gọi). Chưa cấu hình key -> từ chối toàn bộ (fail-closed).
/// Created date: 08/10/2026
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MediaApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configService = context.HttpContext.RequestServices.GetRequiredService<MediaConfigService>();
        var config = await configService.GetAsync(context.HttpContext.RequestAborted);

        var expected = config.ApiKey;
        var actual = context.HttpContext.Request.Headers[MediaConst.ApiKeyHeader].FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrEmpty(expected) || !FixedEquals(expected, actual))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }

    private static bool FixedEquals(string a, string b)
    {
        var x = Encoding.UTF8.GetBytes(a);
        var y = Encoding.UTF8.GetBytes(b);
        return x.Length == y.Length && CryptographicOperations.FixedTimeEquals(x, y);
    }
}
