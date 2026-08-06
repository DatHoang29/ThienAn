namespace Modules.ShareDataWorker.Extensions
{
    /// <summary>
    /// Extension chứa cấu hình Dependency Injection cho Worker Service (Rule 2.1.3)
    /// Author: Đạt
    /// Created date: 31/07/2026
    /// </summary>
    public static class ShareDataWorkerExtensions
    {
        public static IServiceCollection AddWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? "Server=localhost;Database=dev_its10;Trusted_Connection=True;TrustServerCertificate=True;";

            services.AddScoped<ISqlSugarClient>(sp => new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = SqlSugar.DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            }));

            services.AddHostedService<ShareDataExportService>();

            return services;
        }
    }
}
