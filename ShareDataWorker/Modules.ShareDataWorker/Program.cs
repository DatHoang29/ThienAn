/// <summary>
/// Program Entry Point cho Modules.ShareDataWorker
/// Author: Đạt
/// Created date: 31/07/2026
/// </summary>

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddWorkerInfrastructure(hostContext.Configuration);
    })
    .Build();

await host.RunAsync();
