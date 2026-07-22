/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace UI.Filters
{
    /// <summary>
    /// Description: 5.3.8 Action Filter / Attribute (Bộ lọc hành động Action - Audit Logging & đo thời gian thực thi Action)
    /// Author: Antigravity
    /// Create Date: 21/07/2026
    /// </summary>
    public class cls_AuditLogActionFilter : ActionFilterAttribute
    {
        /// <summary>
        /// Description: Phương thức chạy trước và sau khi Controller Action được gọi
        /// </summary>
        /// <param name="context">Ngữ cảnh thực thi Action</param>
        /// <param name="next">Delegate gọi Action và các Filter tiếp theo</param>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // ===== GIAI ĐOẠN 1: TRƯỚC KHI VÀO ACTION =====
            var objActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var strControllerName = objActionDescriptor?.ControllerName;
            var strActionName = objActionDescriptor?.ActionName;
            var strIpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            Console.WriteLine($"[AOP Action Filter - BEFORE]: [{strIpAddress}] Đang chuẩn bị gọi Action {strControllerName}/{strActionName}");

            // Bạn có thể soi hoặc sửa các tham số truyền vào Action (context.ActionArguments)
            foreach (var kvp in context.ActionArguments)
            {
                Console.WriteLine($"  -> Parameter: {kvp.Key} = {JsonSerializer.Serialize(kvp.Value)}");
            }

            var stopwatch = Stopwatch.StartNew();

            // ===== THỰC THI ACTION =====
            var objExecutedContext = await next();

            stopwatch.Stop();

            // ===== GIAI ĐOẠN 2: SAU KHI ACTION THỰC THI XONG =====
            var isSuccess = objExecutedContext.Exception == null;
            Console.WriteLine($"[AOP Action Filter - AFTER]: Action {strControllerName}/{strActionName} chạy thành công = {isSuccess} (Thời gian: {stopwatch.ElapsedMilliseconds} ms)");
        }
    }
}
