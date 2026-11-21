using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Infrastructure.Services.Process;
using Shared.Utility.Resilience;
using Wolverine;

namespace Modules.VMS.Infrastructure.Services.BackgroundService
{
	/// <summary>
	/// Description: Khởi động lại application, service sẽ chạy 1 lần để kiểm tra trạng thái tiến trình còn đang chạy thì khởi tạo lại thread để chạy lập lịch
	///Author: TuanHA
	///Create Date: 19/03/2025
	///Update 06/10/2026: chuyển từ IHostedService sang BackgroundService + chờ DbReadyGate.
	///    - Trước đây StartAsync await Task.Run(... Task.Delay 15s ...) nên app đứng 15s trước khi nhận request.
	///    - Nay chạy trong ExecuteAsync (không chặn khởi động) và chỉ chạm DB sau khi cổng CSDL mở.
	/// </summary>
	public class RestartScheduleBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly DbReadyGate _dbReadyGate;

		public RestartScheduleBackgroundService(IServiceProvider serviceProvider, DbReadyGate dbReadyGate)
		{
			_serviceProvider = serviceProvider;
			_dbReadyGate = dbReadyGate;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				Debug.WriteLine("RestartScheduleBackgroundService is starting.");

				// Chờ app khởi động xong + CSDL sẵn sàng
				await _dbReadyGate.WaitAsync(stoppingToken);

				// Chờ WPF form / ITS.VMS.Worker kịp kết nối NATS trước khi BE gửi lệnh đầu tiên.
				// Nếu bỏ delay, mỗi biển lập lịch sẽ timeout (VmsTimeoutSeconds) ngay khi startup.
				// Thời gian chờ đọc từ cấu hình "VmsRestartScheduleDelaySec", mặc định 15 giây.
				int delaySec;
				try { delaySec = App.GetConfig<int?>("DeviceIntegration:VmsRestartScheduleDelaySec") ?? 15; }
				catch { delaySec = 15; }

				if (delaySec > 0)
				{
					Debug.WriteLine($"RestartScheduleBackgroundService: chờ {delaySec}s cho Worker/WPF sẵn sàng...");
					await Task.Delay(TimeSpan.FromSeconds(delaySec), stoppingToken);
				}

				if (stoppingToken.IsCancellationRequested) return;

				using (var scope = _serviceProvider.CreateScope())
				{
					Debug.WriteLine("RestartScheduleBackgroundService create scope.");

					// Create service message bus
					var messBuss = scope.ServiceProvider.GetRequiredService<IMessageBus>();

					// Create service vmsProcess
					var processService = scope.ServiceProvider.GetRequiredService<VmsProcessService>();

					// Create object request handler
					RestartProcessScheduleInput input = new RestartProcessScheduleInput();

					// Lịch kẹt ở pause-incident dù biển đã hết sự cố -> đưa về run để được nạp lại ngay bên dưới
						try
						{
							await scope.ServiceProvider.GetRequiredService<Modules.VMS.Infrastructure.Services.Schedule.ScheduleService>()
								.RecoverStalePausedSchedulesAsync();
						}
						catch (Exception ex)
						{
							Debug.WriteLine($"RestartScheduleBackgroundService: không khôi phục được lịch pause-incident: {ex.Message}");
						}

						// Nạp các tiến trình cần khôi phục (cả schedule và schedule-incident đang 'run', bỏ qua 'pause-incident')
					// theo DEC-07 và Section 5.4
					input.ListProcess = await processService.GetActiveProcessesForRestartAsync();

					// Nếu danh sách tiến trình có thì khởi tạo lại thread chạy lập lịch
					if (input.ListProcess.Count > 0)
					{
						var listProcessIds = input.ListProcess.Select(q => q.ID).ToList();
						input.ListProcessImg = await processService.GetListProcessImg(listProcessIds);
						await messBuss.InvokeAsync(input);
					}
				}
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				// app đang tắt
			}
			catch (Exception ex)
			{
				// TOTO: log
				Debug.WriteLine($"RestartScheduleBackgroundService error: {ex}");
			}
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			Debug.WriteLine("RestartScheduleBackgroundService is stop.");
			return base.StopAsync(cancellationToken);
		}
	}
}
