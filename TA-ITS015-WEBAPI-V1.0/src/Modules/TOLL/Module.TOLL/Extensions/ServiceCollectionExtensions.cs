using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.TOLL.Core.Dto.ManualFetch;
using Module.TOLL.Infrastructure.Service.Cache;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using System.Diagnostics;

namespace Modules.TOLL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTollCore(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddTollInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Kích hoạt IHttpClientFactory cho các service gọi API ngoài (TollImageService gọi API ảnh TCS)
            services.AddHttpClient();

            // Nhận kết quả lấy dữ liệu thủ công từ dịch vụ ITS.Toll.Worker qua kênh ta.its.control.toll
            // để xoá cache danh mục SAU KHI dịch vụ đã lưu DB xong (lần xoá lúc bấm nút có thể bị
            // một request GetList chen vào nạp lại dữ liệu cũ trong lúc dịch vụ còn đang kéo).
            // CHỈ ĐƯỢC GỌI ĐÚNG 1 LẦN: BaseDataTransporter.ConsumeData cộng dồn handler vào OnDataReceived.
            // Author: ChuongLP - 28/09/2026
            ItsDataTransporter.ConsumeData(ItsDataTransporterChannel.ControlToll, data =>
            {
                try
                {
                    var json = data as string ?? JsonConvert.SerializeObject(data);
                    if (string.IsNullOrWhiteSpace(json)) return;

                    var result = JsonConvert.DeserializeObject<TollControlResultMessage>(json);
                    if (result == null) return;

                    // Kênh dùng chung 2 chiều: bỏ qua chính gói request do BE vừa phát đi.
                    if (!string.Equals(result.Type?.Trim(), "result", StringComparison.OrdinalIgnoreCase)) return;

                    // Chỉ xoá cache của loại dữ liệu lấy thành công (loại lỗi thì DB không đổi).
                    var dataTypes = (result.Items ?? new List<TollControlResultMessageItem>())
                        .Where(i => i.Ok && !string.IsNullOrWhiteSpace(i.DataType))
                        .Select(i => i.DataType!)
                        .ToList();
                    if (dataTypes.Count == 0) return;

                    // Callback chạy trên thread nền NATS, không có scope HTTP -> tự tạo scope DI.
                    var scopeFactory = App.GetService<IServiceScopeFactory>();
                    using var scope = scopeFactory.CreateScope();
                    scope.ServiceProvider.GetService<TollCacheService>()?.ClearByDataTypes(dataTypes);
                }
                catch (Exception ex)
                {
                    // Nuốt lỗi: không để exception làm rớt vòng subscribe NATS.
                    Debug.WriteLine($"[TollCache] Xử lý gói kết quả lỗi: {ex.Message}");
                }
            });

            return services;
        }

        public static IServiceCollection AddTollValidation(this IServiceCollection services)
        {
            services.AddControllers().AddTollValidation();
            return services;
        }
    }
}
