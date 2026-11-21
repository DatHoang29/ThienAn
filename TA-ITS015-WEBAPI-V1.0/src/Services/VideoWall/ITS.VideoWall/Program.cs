using ITS.VideoWall.Extensions;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "TA-ITS015-VideoWall";
});

// DI Registrations
builder.Services.AddVideoWallWorkerHost(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
