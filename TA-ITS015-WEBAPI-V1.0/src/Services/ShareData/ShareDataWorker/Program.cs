/// <summary>
/// Program Entry Point cho ShareDataWorker
/// Author: Đạt
/// Created date: 31/07/2026
/// Modified date: 10/11/2026
/// </summary>

var host = Host.CreateDefaultBuilder(args)
    // Cho phép chạy dưới dạng Windows Service (báo trạng thái Running cho SCM,
    // đặt ContentRoot = thư mục chứa exe để đọc đúng appsettings.json)
    .UseWindowsService()
    .UseConfiguredServiceName()
    .UseCustomEnvironment()
    .UseContentRootStoragePath()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddWorkerInfrastructure(hostContext.Configuration);
    })
    .Build();

await host.RunAsync();
