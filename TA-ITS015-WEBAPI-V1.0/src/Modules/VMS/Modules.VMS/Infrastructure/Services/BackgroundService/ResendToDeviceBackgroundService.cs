using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Infrastructure.Services.Current;
using Modules.VMS.Infrastructure.Services.Process;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace Modules.VMS.Infrastructure.Services.BackgroundService
{
	public class ResendToDeviceBackgroundService : IHostedService, IDisposable
	{

		private readonly IServiceProvider _serviceProvider;

		public ResendToDeviceBackgroundService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}	

		public Task StartAsync(CancellationToken cancellationToken)
		{
			// Ensure run at once
			Task.Run(async () =>
			{
				try
				{
					Debug.WriteLine("Resend To Device Background Service is starting.");

					using (var scope = _serviceProvider.CreateScope())
					{
						Debug.WriteLine("Resend To Device Background Service create scope.");

						// Create service message bus
						var messBuss = scope.ServiceProvider.GetRequiredService<IMessageBus>();

						// Create service vmsProcess
						var processService = scope.ServiceProvider.GetRequiredService<VmsProcessService>();

						// Create service vmsCurrent
						var vmsCurrentService = scope.ServiceProvider.GetRequiredService<VmsCurrentService>();

						// Get list VmsCurrent
						var listVmsCurrent = await vmsCurrentService.GetListVmsCurrent();

						// Get list process not in schedule
						var listProcess = await processService.GetListProcess();
					
						var listProcessScheduleStateRun = listProcess.Where(p => p.Type == "schedule" && p.State == "run").ToList();						
						var listProcessNotInSchedule = listProcess.Except(listProcessScheduleStateRun).ToList();

						Debug.WriteLine("In the scope - listProcessNotInSchedule with: " + listProcessNotInSchedule.Count);
						Debug.WriteLine("In the scope - listProcessSchedule with: " + listProcessScheduleStateRun.Count);					

						Action[] actions = new Action[]
						{
							async () =>
							{
								Debug.WriteLine("Task Action 1");
								ResendToDeviceInput resendToDeviceInput = new ResendToDeviceInput();
								resendToDeviceInput.ListProcess = listProcessNotInSchedule;
								resendToDeviceInput.ListCurrent = listVmsCurrent;
								if (resendToDeviceInput.ListProcess.Count > 0)
								{
									// Handle in ProcessCommandHandler
									await messBuss.InvokeAsync(resendToDeviceInput);
								}
							},
							async () =>
							{
								Debug.WriteLine("Task Action 2");

								// Create object request handler
								RestartProcessScheduleInput input = new RestartProcessScheduleInput();

								// Get list Vms process with type schedule and state is run
								input.ListProcess =listProcessScheduleStateRun;
						
								// Nêu danh sách tiến trình có thì khởi tạo lại thread chạy lập lịch
								if(input.ListProcess.Count > 0)
								{
									var listProcessIds = input.ListProcess.Select(q => q.ID).ToList();
									input.ListProcessImg = await processService.GetListProcessImg(listProcessIds);

									// Handle in ScheduleCommandHandler
									await messBuss.InvokeAsync(input);
								}
							}
						};
						Parallel.Invoke(actions);
					}
				}
				catch (Exception ex)
				{
					// TODO: log
					throw ex;
				}

			}, cancellationToken);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			Debug.WriteLine("Resend To Device Background Service is stop.");
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			Debug.WriteLine("Resend To Device Background Service is Dispose.");

		}
	}
}
