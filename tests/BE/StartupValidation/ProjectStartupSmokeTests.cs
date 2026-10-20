#if HAS_SHAREDATAWORKER
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using ShareDataWorker.Extensions;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
#endif
#if HAS_VIDEOWALL_WPF
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Extensions;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
#endif
using Microsoft.Extensions.Hosting;
#if HAS_VIDEOWALL_WORKER
using ITS.VideoWall.Core.Interfaces;
using ITS.VideoWall.Extensions;
using Module.VideoWall.Core.Interfaces;
#endif

namespace Tests.StartupValidation
{
    /// <summary>
    /// Description: Bộ kiểm thử xác thực tính toàn vẹn DI Container và khả năng build Host của từng project độc lập
    /// Created date: 14/09/2026
    /// </summary>
    [Collection("api")]
    public class ProjectStartupSmokeTests
    {
#if HAS_VIDEOWALL_WORKER
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
#endif

#if HAS_SHAREDATAWORKER
        /// <summary>
        /// Description: Khởi động tiến trình ShareDataWorker thật để kiểm chứng DI container không có lỗi và host khởi động thành công
        /// Created date: 19/10/2026
        /// </summary>
        [Fact]
        public async Task ShareDataWorker_WhenProcessStarts_HostStartsWithoutDiErrors_Test()
        {
            var workerDll = ResolveShareDataWorkerDllPath();
            var workerDir = Path.GetDirectoryName(workerDll)!;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
                .Build();

            var defaultConnection = config.GetConnectionString("DefaultConnection")
                ?? "Server=127.0.0.1;Database=test;MultipleActiveResultSets=true;User Id=sa;Password=Tac@@1234;Encrypt=False;TrustServerCertificate=true;Connect Timeout=30;Pooling=False;";

            // Khẳng định chuỗi CSDL test là localhost/127.0.0.1
            Assert.True(
                defaultConnection.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                defaultConnection.Contains("localhost", StringComparison.OrdinalIgnoreCase),
                "Chuỗi CSDL truyền vào Worker bắt buộc phải trỏ tới localhost/127.0.0.1");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{workerDll}\" --ConnectionStrings:DefaultConnection=\"{defaultConnection}\" --Logging:LogLevel:Microsoft.Hosting.Lifetime=Information",
                WorkingDirectory = workerDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

            using var process = new Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var startedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    if (e.Data.Contains("Application started", StringComparison.OrdinalIgnoreCase))
                    {
                        startedTcs.TrySetResult(true);
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                var completedTask = await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(60)));
                var isStarted = completedTask == startedTcs.Task;

                var allOutput = outputBuilder.ToString();
                var allError = errorBuilder.ToString();

                Assert.DoesNotContain("Unhandled exception", allOutput, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Unhandled exception", allError, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Error while validating the service descriptor", allOutput, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Error while validating the service descriptor", allError, StringComparison.OrdinalIgnoreCase);

                Assert.True(isStarted, $"ShareDataWorker không khởi động thành công trong 60 giây. Output:\n{allOutput}\nError:\n{allError}");
            }
            finally
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync();
                    }
                }
                catch
                {
                    // Ignore kill cleanup errors
                }
            }
        }

        private static string ResolveShareDataWorkerDllPath()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "TA-ITS015-WEBAPI-V1.0", "src", "Services", "ShareData", "ShareDataWorker", "bin", "Debug", "net10.0", "ShareDataWorker.dll");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            var fallback = Path.Combine(AppContext.BaseDirectory, "ShareDataWorker.dll");
            if (File.Exists(fallback))
            {
                return fallback;
            }

            throw new FileNotFoundException("Cannot locate ShareDataWorker.dll");
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

