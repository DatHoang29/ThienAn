/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Microsoft.AspNetCore.Mvc.Controllers;

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
        /// Description: Bắt và xử lý ngoại lệ bất đồng bộ, tự động định dạng lỗi ValidationException của FluentValidation
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

            object objErrorResponse;
            int intStatusCode;

            // Xử lý riêng biệt nếu là lỗi xác thực dữ liệu từ FluentValidation (Tránh trùng với System.ComponentModel.DataAnnotations)
            if (objException is FluentValidation.ValidationException objValException)
            {
                intStatusCode = StatusCodes.Status400BadRequest;
                
                // Gom nhóm các lỗi theo tên thuộc tính/trường dữ liệu bị lỗi
                var dictErrors = objValException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                objErrorResponse = new
                {
                    isSuccess = false,
                    intStatusCode = intStatusCode,
                    strErrorMessage = "Dữ liệu đầu vào không hợp lệ!",
                    dictErrors = dictErrors, // Danh sách lỗi chi tiết từng trường
                    dtTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                };
            }
            else
            {
                intStatusCode = StatusCodes.Status500InternalServerError;
                objErrorResponse = new
                {
                    isSuccess = false,
                    intStatusCode = intStatusCode,
                    strErrorMessage = "Hệ thống gặp sự cố không mong muốn! Vui lòng thử lại sau.",
                    strDetailDetail = objException.Message, // Khi chạy môi trường Dev
                    dtTimestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                };
            }

            // Ghi đè Result trả về Client
            context.Result = new JsonResult(objErrorResponse)
            {
                StatusCode = intStatusCode
            };

            // Đánh dấu lỗi đã được xử lý xong (Tránh ứng dụng bị crash văng ra ngoài)
            context.ExceptionHandled = true;

            await Task.CompletedTask;
        }
    }
}
