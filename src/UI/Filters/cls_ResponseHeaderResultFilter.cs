/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace UI.Filters
{
    /// <summary>
    /// Description: 5.3.10 Result Filter (Bộ lọc kết quả - Can thiệp & Bổ sung thông tin vào HTTP Response Header trước khi gửi về Client)
    /// Author: Antigravity
    /// Create Date: 21/07/2026
    /// </summary>
    public class cls_ResponseHeaderResultFilter : IAsyncResultFilter
    {
        /// <summary>
        /// Description: Phương thức thực thi trước và sau khi Result (View/JsonResult) được trả về
        /// </summary>
        /// <param name="context">Ngữ cảnh kết quả trả về</param>
        /// <param name="next">Delegate thực thi tiếp</param>
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            Console.WriteLine("[AOP Result Filter]: Đang bổ sung Custom Response Headers...");

            // Bổ sung các Header phản hồi tùy chỉnh trước khi stream được ghi ra response body
            context.HttpContext.Response.Headers.Append("X-Powered-By", "TA-Corp DDD Framework");
            context.HttpContext.Response.Headers.Append("X-Server-Timestamp", DateTime.Now.Ticks.ToString());

            // Chuyển sang giai đoạn hoàn tất trả về kết quả
            var resultExecutedContext = await next();

            Console.WriteLine("[AOP Result Filter]: Đã hoàn tất gửi Response Header về Client!");
        }
    }
}
