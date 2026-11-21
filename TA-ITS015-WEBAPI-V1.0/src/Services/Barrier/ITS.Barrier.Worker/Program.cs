using ITS.Barrier.Core.Integration;
using ITS.Barrier.Core.Logging;
using ITS.Barrier.Core.Messaging;
using ITS.Barrier.Worker.Messaging;

// Author: ChuongLP
// Date: 05/10/2026
// Worker Service lắng nghe lệnh NATS ta.its.control.barrier để kích hoạt mở/đóng Barrier qua Siemens S7-1200

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Host.UseWindowsService(options => options.ServiceName = "ITS Barrier Control");

var configuration = builder.Configuration;
var services = builder.Services;

// Logger ghi file độc lập
var logDir = configuration.GetValue<string>("Logging:FileDirectory");
services.AddSingleton(new BarrierLogger(logDir));

// Cấu hình NATS
var natsOptions = new BarrierControlNatsOption();
configuration.GetSection(BarrierControlNatsOption.SectionName).Bind(natsOptions);
services.AddSingleton(natsOptions);

// Đăng ký tầng thực thi thiết bị Barrier
services.AddSingleton<BarrierDeviceExecutor>();

// Đăng ký NATS Listener & Hosted Service
services.AddSingleton<BarrierControlNatsListener>();
services.AddHostedService<BarrierControlHostedService>();

var app = builder.Build();

app.MapGet("/health", (BarrierControlNatsListener listener, BarrierControlNatsOption options) => Results.Ok(new
{
    ok = true,
    service = "ITS.Barrier.Worker",
    control = new
    {
        listening = listener.IsListening,
        subject = options.ControlSubject,
        natsUrl = options.Url
    },
    timestamp = DateTime.Now
}));

app.Run();
