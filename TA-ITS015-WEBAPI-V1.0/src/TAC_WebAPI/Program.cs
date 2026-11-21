using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace TAC_WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseLamar()
                .ConfigureLogging(l =>
                {
                    l.ClearProviders();
                    l.AddNLog();

                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        // Tự động nhận các URL/Port từ launchSettings.json (5001 & 5000)
                        // và thiết lập giao thức Http1AndHttp2 cho tất cả endpoint
                        options.ConfigureEndpointDefaults(listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                        });
                    });
                    webBuilder.Inject().UseStartup<Startup>();


                });
    }
}
