using ITS.TrafficLight.Core.Integration;
using ITS.TrafficLight.Core.Logging;
using ITS.TrafficLight.Core.Messaging;
using ITS.TrafficLight.Worker.Messaging;
using Modules.Integration.TrafficLight;

// Author: ChuongLP
// Date: 05/10/2026
// Worker Service lắng nghe lệnh NATS ta.its.control.trafficlight điều khiển Đèn tín hiệu giao thông (S7-1200 / Modbus)

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Host.UseWindowsService(options => options.ServiceName = "ITS TrafficLight Control");

var configuration = builder.Configuration;
var services = builder.Services;

// Logger độc lập
var logDir = configuration.GetValue<string>("Logging:FileDirectory");
services.AddSingleton(new TrafficLightLogger(logDir));

// Cấu hình NATS
var natsOptions = new TrafficLightControlNatsOption();
configuration.GetSection(TrafficLightControlNatsOption.SectionName).Bind(natsOptions);
services.AddSingleton(natsOptions);

// Tầng Integration điều phối đa hãng
services.AddSingleton<IntgTrafficLightManager>();

// Tầng thực thi thiết bị Đèn
services.AddSingleton<TrafficLightDeviceExecutor>();

// NATS Listener & Hosted Service
services.AddSingleton<TrafficLightControlNatsListener>();
services.AddHostedService<TrafficLightControlHostedService>();

var app = builder.Build();

app.MapGet("/health", (TrafficLightControlNatsListener listener, TrafficLightControlNatsOption options) => Results.Ok(new
{
    ok = true,
    service = "ITS.TrafficLight.Worker",
    control = new
    {
        listening = listener.IsListening,
        subject = options.ControlSubject,
        natsUrl = options.Url
    },
    timestamp = DateTime.Now
}));

app.Run();
