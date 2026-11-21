using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.CfgSystem.Core.Refresh;

namespace Modules.VDS.Infrastructure.Services.Filters.Mvc;

/// <summary>
/// Author: KienLT
/// Description: Nối CRUD danh mục sẵn có vào luồng refresh V7 mà không phải copy các controller đó vào VDS.
///
/// <para>Filter KHÔNG còn giữ mã refresh cứng. Nó chỉ khai báo "source:scope" của thứ vừa đổi;
/// <see cref="RefreshModifyService"/> tra rule trong SysConfigData để ra mã. Muốn đổi mã refresh
/// của một nhóm thì sửa rule trong DB, không phải build lại.</para>
///
/// <para><b>Vì sao vẫn cần filter dù handler đã tự notify:</b> filter phủ những action KHÔNG đi qua
/// handler có notify — ví dụ <c>UpdateTmsEquipmentVisible</c>/<c>Invisible</c> do
/// EquipmentVisibilityCommandHandler xử lý và không bắn refresh.</para>
///
/// <para><b>Sửa 21/09/2026 — bỏ phát trùng.</b> Trước đây filter route cả <c>TmsEquipment</c>,
/// <c>TmsZone</c>, <c>TmsZoneEquipment</c> theo TIỀN TỐ tên action, nên 9 action đã được handler notify bị
/// bắn thêm bản tin thứ hai. Đã đối chiếu từng action trước khi thu hẹp:</para>
/// <list type="bullet">
/// <item><c>TmsZone</c>: ZoneCommandHandler notify đủ 4 action Add/Update/Delete/BatchDelete — đúng bằng
/// toàn bộ action ghi của controller ⇒ bỏ khỏi filter.</item>
/// <item><c>TmsZoneEquipment</c>: ZoneEquipmentCommandHandler notify đủ 5 action kể cả ConfirmMapping;
/// AutoMap/AutoMapAll chỉ là xem trước, không ghi ⇒ bỏ khỏi filter.</item>
/// <item><c>TmsEquipment</c>: EquipmentCommandHandler notify 4 action CRUD nhưng KHÔNG phủ
/// Visible/Invisible ⇒ filter chỉ giữ đúng hai action đó.</item>
/// <item><c>TmsEventType</c>, <c>TmsVehicleFilter</c>, <c>SysConfigType</c>: không có handler nào tự
/// notify ⇒ filter là đường DUY NHẤT, phải giữ.</item>
/// </list>
///
/// <para><c>SysConfigData</c> và <c>VdsConfig</c> cũng KHÔNG nằm trong bảng dưới: handler của chúng
/// (SysConfigDataCommandHandler, ConfigCommandHandler) đã tự notify với scope chính xác là
/// ConfigTypeCode / GroupCode của bản ghi, còn filter chỉ biết tên controller nên sẽ cho ra scope kém
/// chính xác hơn.</para>
/// Created date: 16/09/2026
/// </summary>
public sealed class VdsRefreshAfterSaveFilter(RefreshModifyService refreshModify, Microsoft.Extensions.Logging.ILogger<VdsRefreshAfterSaveFilter> logger) : IAsyncActionFilter
{
    /// <summary>Thứ vừa đổi thuộc source:scope nào; null = action này không sinh refresh qua filter.</summary>
    public static (string Source, string Scope)? RouteFor(string controller, string action)
    {
        // TmsEquipment: chỉ hai action ẩn/hiện thiết bị là không được handler notify. Khớp CHÍNH XÁC tên
        // action chứ không theo tiền tố, vì "UpdateTmsEquipment" là tiền tố của "UpdateTmsEquipmentVisible".
        if (controller == "TmsEquipment")
            return action is "UpdateTmsEquipmentVisible" or "UpdateTmsEquipmentInvisible"
                ? ("catalog", "equipment")
                : null;

        if (!new[] { "Add", "Update", "Delete", "BatchDelete", "Save" }.Any(x => action.StartsWith(x, StringComparison.Ordinal))) return null;
        return controller switch
        {
            "TmsEventType" => ("catalog", "eventtype"),
            "TmsVehicleFilter" => ("catalog", "vehiclefilter"),
            "SysConfigType" => ("sysconfig", "configtype"),
            _ => null
        };
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Exception != null || executed.Canceled || !context.ModelState.IsValid) return;
        var status = (executed.Result as IStatusCodeActionResult)?.StatusCode ?? context.HttpContext.Response.StatusCode;
        if (status is < 200 or >= 300 || executed.Result is Microsoft.AspNetCore.Mvc.ObjectResult { Value: false }) return;
        if (context.ActionDescriptor is not ControllerActionDescriptor action) return;
        var route = RouteFor(action.ControllerName, action.MethodInfo?.Name ?? action.ActionName);
        if (route is null) return;

        var dispatch = await refreshModify.NotifyAsync(new RefreshModifyRequest(route.Value.Source, route.Value.Scope, null, action.ActionName));
        context.HttpContext.Response.Headers["X-Vds-Refresh"] = dispatch.Published ? "sent" : "pending";
        if (dispatch.Code is int code)
            context.HttpContext.Response.Headers["X-Vds-Refresh-Code"] = code.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (dispatch.Published)
            logger.LogInformation("[VdsRefresh] {Controller}/{Action}: {Source}:{Scope} => code={Code}, subject={Subject}, sent (worker apply not acknowledged).",
                action.ControllerName, action.ActionName, route.Value.Source, route.Value.Scope, dispatch.Code, ItsRefresh.Subject);
        else
            logger.LogWarning("[VdsRefresh] {Controller}/{Action}: DB action succeeded but refresh for {Source}:{Scope} was not published (thieu rule hoac transport loi); dung endpoint Refresh de thu lai.",
                action.ControllerName, action.ActionName, route.Value.Source, route.Value.Scope);
    }
}
