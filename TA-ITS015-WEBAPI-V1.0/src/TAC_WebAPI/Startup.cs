using Furion;
using Hangfire;
using JasperFx;
using JasperFx.CodeGeneration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Module.CCTV.Extensions;
using Module.ShareData.Extensions;
using Module.VideoWall.Extensions;


//using Module.Integration.NxVms.Extensions;
using Modules.CfgSystem.Extensions;
using Modules.DataTransporter.ITS.Extensions;
using Modules.FMS.Extensions;
using Modules.FMS.Report.Extensions;
using Modules.Integration.VMS.Extensions;
using Modules.Mail.Extensions;
using Modules.Media.Extensions;

using Modules.Mail.Infrastructure.Services.Mail.Schedule;
using Modules.Notification.Core.Constants;
using Modules.Notification.Extensions;
using Modules.Notification.Infrastructure.Services;
using Modules.Report.Base.Extensions;
using Modules.Report.Extensions;
using Modules.Samplev2.Extensions;

//using Modules.Samplev2.Extensions;
using Modules.System.Extensions;
using Modules.TMS.Extensions;
using Modules.TMS.Report.Extensions;
using Modules.TOLL.Extensions;
using Modules.TOLL.Report.Extensions;
using Modules.VDS.Extensions;
using Modules.VMS.Extensions;
using Modules.VMS.Report.Extensions;
using Modules.WP.Report.Extensions;
using Shared.Core.Extensions;

using Shared.Core.Settings;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Localization;
using Shared.Infrastructure.Middlewares;
using System;
using System.Globalization;
using System.Threading;
using Wolverine;

namespace TAC_WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration config)
        {
            _config = config;

        }

        private readonly IConfiguration _config;


        private void RegisterModules(IServiceCollection services)
        {
            services
                .AddDistributedMemoryCache()
                .AddSharedInfrastructure(_config)
                .AddSerialization(_config)
                .AddDataTransporterItsModule(_config)
                .AddNotificationModule(_config) // Hạ tầng thông báo dùng chung (user/org/toàn hệ thống) - 17/09/2026
                .AddSystemModule(_config)
                //Đăng ký các module sử dụng
                .AddReportBaseModule(_config) // Lưu ý: Đăng ký ReportBase khi sử dụng report bất kỳ
                .AddReportModule(_config)     // Quản lý cấu hình thông tin báo cáo - 27/07/2026
                .AddMailModule(_config)       // Hạ tầng gửi mail dùng chung - 13/08/2026
                //.AddSamplev2Module(_config)
                .AddCfgSystemModule(_config)
                .AddTMSModule(_config)
                .AddTMSReportModule(_config)
                .AddWPModule(_config)
                .AddWPReportModule(_config)
                .AddMediaModule(_config)      // ITS-Media + WP-MediaPublic (bật theo Media:Role) - 08/10/2026
                .AddTollModule(_config)       // Bổ sung cho luồng ETC -> ITS - 23/07/2026
                .AddTollReportModule(_config) // Bổ sung cho luồng ETC -> ITS - 23/07/2026
                .AddVMSModule(_config)
                .AddVMSReportModule(_config)
                .AddVDSModule(_config)
                .AddVWModule(_config)
                .AddShareDataModule(_config)
                .AddIntegrationVmsModule(_config)
                
                //.AddWPReportModule(_config)
                .AddFMSModule(_config) // Add By Ngọc Thắng - 14/12/2024
                .AddFMSReportModule(_config) // Add By Ngọc Thắng - 14/12/2024
                                             //Đăng ký các module sử dụng
                .AddSharedApplication(_config)
                .AddCctvModule(_config)
                

                .AddSharedApplication(_config);
        }




        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWolverine(opts =>
            {
                //#if DEBUG
                //                opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic;
                //#endif
                //#if RELEASE
                //                opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
                //#endif
                opts.Policies.AutoApplyTransactions();
                opts.Durability.Mode = DurabilityMode.MediatorOnly;


                // Use "Auto" type load mode at development time, but
                // "Static" any other time
                opts.Services.CritterStackDefaults(x =>
                {
                    x.Production.GeneratedCodeMode = TypeLoadMode.Auto;
                    x.Production.ResourceAutoCreate = AutoCreate.None;

                    // Little draconian, but this might be helpful
                    x.Production.AssertAllPreGeneratedTypesExist = true;

                    // These are defaults, but showing for completeness
                    x.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
                    x.Development.ResourceAutoCreate = AutoCreate.CreateOrUpdate;
                });

            });
            //Cấu hình hangfire server (load trong Database.json)
            if (App.Configuration["Hangfire:Enable"]?.ToLower() == "true")
            {
                services.AddHangfire(x => x.UseSqlServerStorage(App.Configuration["Hangfire:ConnectionString"]));
                services.AddHangfireServer();
            }

            RegisterModules(services);
            services.AddDatabaseAccessor();

            //services.AddLocalization(options =>
            //{
            //    options.ResourcesPath = "Resources";
            //});
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
                options.EnableForHttps = true; // Optional: Enable compression for HTTPS
            });
            services.AddSingleton<LocalizationMiddleware>();
            services.Configure<CacheConfiguration>(_config.GetSection("CacheConfiguration"));
            services.AddSpecificationDocuments();

            services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
            services.AddControllers().AddInject().AddFriendlyException().AddAppLocalization(settings =>
            {
                services.AddJsonLocalization(options => options.ResourcesPath = ["Resources"]);
            });


            services.AddDynamicApiControllers();
            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
                options.EnableForHttps = true; // Optional: Enable compression for HTTPS
            });



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

                        app.UseHttpsRedirection();
            app.UseInject();
            app.UseUnifyResultStatusCodes();
            if (App.Configuration["Hangfire:Enable"]?.ToLower() == "true")
            {
                app.UseHangfireDashboard("/jobs");

                // Job đồng bộ thiết bị CCTV (sync-cctv-devices) đã chuyển về Module.CCTV
                // (AddCctvInfrastructure -> CctvSyncDeviceJobRegistrar). Không đăng ký ở đây nữa.

                // Job gửi báo cáo qua email (mỗi lịch một job "rpt-email-{ID}") đã chuyển về Module.Mail
                // (AddMailInfrastructure -> RptEmailJobRegistrar) — 02/10/2026. Không đăng ký ở đây nữa.
            }
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("vi-VN");
            //app.UseAppLocalization();
            //app.UseUnifyResultStatusCodes();
            //app.UseStaticFiles();
            app.UseSharedInfrastructure();

        }


    }


}
