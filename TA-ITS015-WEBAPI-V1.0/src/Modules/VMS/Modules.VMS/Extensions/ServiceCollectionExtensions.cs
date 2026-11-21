using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.VMS.Controllers.EquipmentTelemetry.Dto;
using Modules.VMS.Controllers.Process.Commands;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Infrastructure.Services.BackgroundService;
using Modules.VMS.Infrastructure.Services.EquipmentTelemetry;
using Modules.VMS.Infrastructure.Services.Process;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using NewLife.Reflection;
using System.Diagnostics;
using System.Threading;
using System;
using Wolverine;
using Wolverine.Runtime;
using Shared.Infrastructure.Extensions;
using Modules.VMS.Controllers.Current.Dto;
using Spectre.Console;

namespace Modules.VMS.Extensions
{
	/// <summary>
	/// Author: ThangNN
	/// Created date: 09/08/2024
	/// </summary>

	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddVMSCore(this IServiceCollection services)
		{
			return services;
		}

        public static IServiceCollection AddVMSValidation(this IServiceCollection services)
        {
            services.AddControllers().AddVMSValidation();

            return services;
        }

		public static IServiceCollection AddVMSInfrastructure(this IServiceCollection services, IConfiguration configuration)
		{
			// Khởi tạo lại thread lập lịch sau khi BE restart.
			// KHÔNG dùng ResendToDeviceBackgroundService vì hàm đó dùng Parallel.Invoke(async Action[])
			// — async void trap, exception bị nuốt im. RestartScheduleBackgroundService dùng await Task.Run(...)
			// nên exception propagate đúng cách lên hosting framework.
			// LƯU Ý: BE sẽ gửi NATS ngay khi startup → WPF form / ITS.VMS.Worker phải đang chạy trước.
			services.AddHostedService<RestartScheduleBackgroundService>();

			// Thu thập telemetry KHÔNG còn do BE lập lịch (đã bỏ job Hangfire 23/09/2026).
			// Dịch vụ ITS.VMS.Worker tự giữ nhịp, BE chỉ bị động trả danh sách thiết bị và lưu số liệu
			// nhận được — xem consumer kênh VmsData bên dưới.

			// Nhận kết quả điều khiển thiết bị từ dịch vụ ITS.VMS.Worker qua kênh ta.its.control.vms.
			// CHỈ ĐƯỢC GỌI ĐÚNG 1 LẦN: BaseDataTransporter.ConsumeData cộng dồn handler vào OnDataReceived,
			// gọi lặp lại sẽ khiến mỗi gói tin bị xử lý nhiều lần.
			// Author: ChuongLP - 22/09/2026
			ItsDataTransporter.ConsumeData(ItsDataTransporterChannel.ControlVms, data =>
			{
				try
				{
					var json = data as string ?? JsonConvert.SerializeObject(data);
					if (string.IsNullOrWhiteSpace(json)) return;

					var result = JsonConvert.DeserializeObject<VmsControlResult>(json);
					if (result == null) return;

					// Kênh dùng chung 2 chiều: bỏ qua chính gói request do BE vừa phát đi.
					if (!string.Equals(result.Type?.Trim(), VmsControlType.Result, StringComparison.OrdinalIgnoreCase)) return;

					// Chỉ đánh thức task đang chờ theo RequestId. KHÔNG resolve service/repository ở đây:
					// callback chạy trên thread nền, không có scope của request HTTP.
					VmsControlBridge.Complete(result);
				}
				catch
				{
					// Nuốt lỗi: không để exception làm rớt vòng subscribe NATS.
				}
			});

			// Luồng TELEMETRY do dịch vụ tự giữ nhịp, đi trên kênh DỮ LIỆU ta.its.data.vms.
			// Kênh này chở nhiều loại gói (kể cả vms-success/vms-fail BE bắn cho FE) nên phải lọc theo "Type".
			// Author: ChuongLP - 23/09/2026
			ItsDataTransporter.ConsumeData(ItsDataTransporterChannel.VmsData, data =>
			{
				try
				{
					var json = data as string ?? JsonConvert.SerializeObject(data);
					if (string.IsNullOrWhiteSpace(json)) return;

					// Đọc nhẹ để lấy Type trước. Gói vms-success/vms-fail không có trường này -> bỏ qua sớm.
					var type = JObject.Parse(json).Value<string>("type");
					if (string.IsNullOrWhiteSpace(type)) return;

					// Chạy rời khỏi thread nhận NATS: 2 việc dưới đây đều chạm DB, không được chặn vòng subscribe.
					// Callback không có scope HTTP nên phải tự tạo scope DI.
					_ = Task.Run(async () =>
					{
						try
						{
							var scopeFactory = App.GetService<IServiceScopeFactory>();
							using var scope = scopeFactory.CreateScope();
							var service = scope.ServiceProvider.GetService<VmsTelemetryDataService>();
							if (service == null) return;

							if (string.Equals(type, VmsDataType.EquipmentRequest, StringComparison.OrdinalIgnoreCase))
							{
								// Dịch vụ hỏi danh sách thiết bị -> đọc DB và trả lại trên cùng kênh.
								var request = JsonConvert.DeserializeObject<VmsEquipmentSyncRequest>(json);
								if (request == null) return;

								var result = await service.BuildEquipmentListAsync(request);
								ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.VmsData, result);
							}
							else if (string.Equals(type, VmsDataType.Telemetry, StringComparison.OrdinalIgnoreCase))
							{
								// Dịch vụ đẩy số liệu -> lưu bảng VmsEquipmentTelemetry.
								var push = JsonConvert.DeserializeObject<VmsTelemetryPush>(json);
								if (push == null) return;

								var saved = await service.SaveTelemetryAsync(push);
								Debug.WriteLine($"[VmsTelemetry] Đã lưu {saved} bản ghi telemetry (source={push.Source}).");
							}
							// equipment-result là gói do CHÍNH BE phát -> không xử lý lại.
						}
						catch (Exception ex)
						{
							// Nuốt lỗi: một gói lỗi không được làm rớt vòng subscribe NATS.
							Debug.WriteLine($"[VmsTelemetry] Xử lý gói tin lỗi: {ex.Message}");
						}
					});
				}
				catch
				{
					// Nuốt lỗi: không để exception làm rớt vòng subscribe NATS.
				}
			});

			return services;
		}

	}
}
