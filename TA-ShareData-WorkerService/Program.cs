using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using TA_ShareData_WorkerService.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection")
                               ?? "Server=localhost;Database=dev_its10;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddScoped<ISqlSugarClient>(sp =>
        {
            var sqlSugar = new SqlSugarScope(new ConnectionConfig()
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
            return sqlSugar;
        });

        services.AddHostedService<ShareDataExportWorker>();
    })
    .Build();

await host.RunAsync();
