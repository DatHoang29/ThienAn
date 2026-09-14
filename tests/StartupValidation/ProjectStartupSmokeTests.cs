#if HAS_SHAREDATAWORKER
using ShareDataWorker.Core.Interfaces;
using ShareDataWorker.Extensions;
#endif
#if HAS_VIDEOWALL_WPF
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Extensions;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ITS.VideoWall.Extensions;
using Module.VideoWall.Core.Interfaces;

namespace Tests.StartupValidation
{
    /// <summary>
    /// Description: Bộ kiểm thử xác thực tính toàn vẹn DI Container và khả năng build Host của từng project độc lập
    /// Created date: 14/09/2026
    /// </summary>
    public class ProjectStartupSmokeTests
    {
        /// <summary>
        /// Description: Kiểm tra ITS.VideoWall (Worker) build Host thành công và resolve các service cốt lõi
        /// Created date: 14/09/2026
        /// </summary>
        [Fact]
        public void ITS_VideoWall_Worker_Host_BuildsSuccessfully_Test()
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                EnvironmentName = "Test",
                ContentRootPath = AppContext.BaseDirectory
            });

            builder.Configuration.Sources.Clear();
            builder.Configuration.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false);

            builder.Services.Configure<ServiceProviderOptions>(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            });

            builder.Services.AddVideoWallWorkerHost(builder.Configuration);

            using var host = builder.Build();
            Assert.NotNull(host);

            var publisher = host.Services.GetService<IVwNatsPublisher>();
            Assert.NotNull(publisher);

            using var scope = host.Services.CreateScope();
            var deviceService = scope.ServiceProvider.GetService<IVwISAPIDeviceService>();
            Assert.NotNull(deviceService);

            var sceneRegionService = scope.ServiceProvider.GetService<IVwSceneRegionService>();
            Assert.NotNull(sceneRegionService);

            var eventTriggerLogService = scope.ServiceProvider.GetService<IVwEventTriggerLogService>();
            Assert.NotNull(eventTriggerLogService);
        }

#if HAS_SHAREDATAWORKER
        /// <summary>
        /// Description: Kiểm tra ShareDataWorker build DI container thành công và resolve các service cốt lõi
        /// Created date: 14/09/2026
        /// </summary>
        [Fact]
        public void ShareDataWorker_Host_BuildsSuccessfully_Test()
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                EnvironmentName = "Test",
                ContentRootPath = AppContext.BaseDirectory
            });

            builder.Configuration.Sources.Clear();
            builder.Configuration.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false);

            builder.Services.AddShareDataWorkerCoreServices();
            builder.Services.AddWorkerInfrastructure(builder.Configuration);

            builder.Services.Configure<ServiceProviderOptions>(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            });

            using var host = builder.Build();
            Assert.NotNull(host);
            Assert.NotNull(host.Services.GetService<IDataPublicationService>());
            Assert.NotNull(host.Services.GetService<IDataInboundService>());
        }
#endif

#if HAS_VIDEOWALL_WPF
        /// <summary>
        /// Description: Kiểm tra ITS.VideoWall.WPF nạp đủ ViewModel và API Invoker
        /// Created date: 14/09/2026
        /// </summary>
        [Fact]
        public void ITS_VideoWall_WPF_Host_BuildsSuccessfully_Test()
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                EnvironmentName = "Test",
                ContentRootPath = AppContext.BaseDirectory
            });

            builder.Configuration.Sources.Clear();
            builder.Configuration.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false);

            builder.Services.AddVideoWallWpf(builder.Configuration);

            using var host = builder.Build();
            Assert.NotNull(host.Services.GetService<MainViewModel>());
            Assert.NotNull(host.Services.GetService<ApiInvoker>());
            Assert.NotNull(host.Services.GetService<VideoWallApiClient>());
        }
#endif
    }
}
