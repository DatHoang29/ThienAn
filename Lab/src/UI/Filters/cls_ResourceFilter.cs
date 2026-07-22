/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace UI.Filters
{
    /// <summary>
    /// Description: 5.3.7 Resource Filter (Bộ lọc tài nguyên - Chạy ngay sau Auth Filter để kiểm soát Caching & đo hiệu năng sớm)
    /// Author: Antigravity
    /// Create Date: 21/07/2026
    /// </summary>
    public class cls_ResourceFilter : IAsyncResourceFilter
    {
        /// <summary>
        /// Description: Xử lý trước và sau khi toàn bộ giai đoạn nạp tài nguyên / Model binding diễn ra
        /// </summary>
        /// <param name="context">Ngữ cảnh nạp tài nguyên</param>
        /// <param name="next">Delegate gọi tới giai đoạn tiếp theo</param>
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine("[AOP Resource Filter]: Bắt đầu kiểm soát tài nguyên Request...");

            // Ví dụ thực tế: Ngắt luồng sớm nếu User Agent bị cấm (Anti-Bot / Crawler Block)
            var strUserAgent = context.HttpContext.Request.Headers.UserAgent.ToString();
            if (strUserAgent.Contains("MaliciousBot", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[AOP Resource Filter]: Ngắt luồng sớm (Short-circuit) do Bot độc hại!");
                context.Result = new ContentResult
                {
                    Content = "Access Denied by Resource Filter Policy.",
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // Thực thi giai đoạn tiếp theo (Model Binding -> Action Filter -> Action Execution -> Result Filter)
            var resourceExecutedContext = await next();

            stopwatch.Stop();
            Console.WriteLine($"[AOP Resource Filter]: Hoàn tất xử lý tài nguyên trong {stopwatch.ElapsedMilliseconds} ms.");
        }
    }
}
