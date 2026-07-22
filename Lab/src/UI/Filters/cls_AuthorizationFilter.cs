/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace UI.Filters
{
    /// <summary>
    /// Description: 5.3.6 Authorization Filter (Bộ lọc ủy quyền & xác thực API Key trước khi Request chạm vào Controller - Kế thừa IScoped của Furion)
    /// Author: Antigravity
    /// Create Date: 21/07/2026
    /// </summary>
    public class cls_AuthorizationFilter : IAsyncAuthorizationFilter, IScoped
    {
        /// <summary>
        /// Description: Phương thức kiểm tra quyền truy cập bất đồng bộ
        /// </summary>
        /// <param name="context">Ngữ cảnh xác thực của Request</param>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            Console.WriteLine("[AOP Auth Filter]: Đang kiểm tra quyền truy cập...");

            // 1. Kiểm tra xem Action hoặc Controller có gắn thuộc tính cho phép truy cập vô danh [AllowAnonymous] hay không
            var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null) return;

            var isAllowAnonymous = context.Filters.Any(f => f is IAllowAnonymousFilter)
                || actionDescriptor.ControllerTypeInfo.IsDefined(typeof(AllowAnonymousAttribute), true)
                || actionDescriptor.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute), true);

            // Nếu cho phép truy cập vô danh thì bỏ qua kiểm tra
            if (isAllowAnonymous)
            {
                await Task.CompletedTask;
                return;
            }

            // 2. Thực tế sản xuất: Kiểm tra Header "X-Api-Key" gửi lên từ Client
            if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var strApiKey) || strApiKey != "TA-SECRET-KEY-123")
            {
                Console.WriteLine("[AOP Auth Filter]: Bị từ chối truy cập (Thiếu hoặc sai X-Api-Key)!");

                // Ngắt luồng sớm (Short-circuit pipeline) và trả về HTTP 401 Unauthorized
                context.Result = new JsonResult(new
                {
                    isSuccess = false,
                    intStatusCode = StatusCodes.Status401Unauthorized,
                    strMessage = "Yêu cầu bị từ chối! Thiếu hoặc sai Header 'X-Api-Key' hợp lệ."
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
        }
    }
}
