using Microsoft.AspNetCore.Mvc.Filters;
using Modules.Media.Infrastructure.Config;

namespace Modules.Media.Infrastructure.Filters;

/// <summary>
/// Description: Chỉ cho phép controller/action chạy khi bản deploy bật đúng vai trò (Media:Role).
/// Sai vai trò -> 404, như thể endpoint không tồn tại.
/// Created date: 08/10/2026
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MediaRoleAttribute : Attribute, IActionFilter
{
    private readonly string _role;

    public MediaRoleAttribute(string role) => _role = role;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!MediaRole.Allows(_role)) context.Result = new NotFoundResult();
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
