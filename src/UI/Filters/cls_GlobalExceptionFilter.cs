/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace UI.Filters
{
    /// <summary>
    /// Description: 5.3.9 Exception Filter (Bộ lọc ngoại lệ toàn cục - Catch-all Unhandled Exceptions & Chuẩn hóa JSON Báo Lỗi)
    /// Author: Antigravity
    /// Create Date: 21/07/2026
    /// </summary>
    public class cls_GlobalExceptionFilter : IAsyncExceptionFilter
    {
        /// <summary>
        /// Description: Bắt và xử lý ngoại lệ bất đồng bộ
        /// </summary>
        /// <param name="context">Ngữ cảnh lỗi ngoại lệ</param>
        public async Task OnExceptionAsync(ExceptionContext context)
        {
            // Nếu lỗi đã được đánh dấu xử lý ở chỗ khác thì bỏ qua
            if (context.ExceptionHandled) return;

            var objException = context.Exception;
            var objActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var strActionName = $"{objActionDescriptor?.ControllerName}/{objActionDescriptor?.ActionName}";

            Console.WriteLine($"[AOP Exception Filter]: Xảy ra ngoại lệ chưa xử lý tại {strActionName}: {objException.Message}");

            // Tạo đối tượng JSON phản hồi lỗi chuẩn hóa cho phía Client / Mobile App
            var objErrorResponse = new
            {
                isSuccess = false,
                intStatusCode = StatusCodes.Status500InternalServerError,
                strErrorMessage = "Hệ thống gặp sự cố không mong muốn! Vui lòng thử lại sau.",
                strDetailDetail = objException.Message, // Khi chạy môi trường Dev
                dtTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            // Ghi đè Result trả về HTTP 500
            context.Result = new JsonResult(objErrorResponse)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            // Đánh dấu lỗi đã được xử lý xong (Tránh ứng dụng bị crash văng ra ngoài)
            context.ExceptionHandled = true;

            await Task.CompletedTask;
        }
    }
}
