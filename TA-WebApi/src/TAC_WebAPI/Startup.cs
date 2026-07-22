using FluentValidation.AspNetCore;
using Furion;
using Hangfire;
using JasperFx.CodeGeneration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Extensions;
using Modules.FSystem.Extensions;
using Modules.Report.Base.Extensions;
using Modules.Report.Demo.Extensions;
using Modules.Sample.Report.Extensions;
using Modules.Samplev2.Extensions;
using Shared.Core.Extensions;
using Shared.Core.Settings;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Localization;
using Shared.Infrastructure.Middlewares;
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWolverine(opts =>
            {
                opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic;
                opts.Policies.AutoApplyTransactions();
                opts.Durability.Mode = DurabilityMode.MediatorOnly;

            });
            //Cấu hình hangfire server (load trong Database.json)
            if (App.Configuration["Hangfire:Enable"]?.ToLower() == "true")
            {
                services.AddHangfire(x => x.UseSqlServerStorage(App.Configuration["Hangfire:ConnectionString"]));
                services.AddHangfireServer();
            }


            services
                .AddDistributedMemoryCache()
                .AddSerialization(_config)
                .AddSharedInfrastructure(_config)
                .AddFSystemModule(_config)
                .AddCfgSystemModule(_config)
                .AddReportBaseModule(_config) // Lưu ý: Đăng ký ReportBase khi sử dụng report bất kỳ
                .AddSamplev2Module(_config)
                .AddReportDemoModule(_config)
                .AddSampleReportModule(_config)
                .AddSharedApplication(_config);
            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            services.AddSingleton<LocalizationMiddleware>();
            services.Configure<CacheConfiguration>(_config.GetSection("CacheConfiguration"));

            services.AddSpecificationDocuments();

            services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
            services.AddControllers().AddInject().AddFriendlyException().AddAppLocalization(settings =>
            {
                services.AddJsonLocalization(options => options.ResourcesPath = "Resources");
            }); ;
            services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblies(App.Assemblies));

            services.AddDynamicApiControllers();
            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseInject();
            app.UseUnifyResultStatusCodes();
            if (App.Configuration["Hangfire:Enable"]?.ToLower() == "true")
            {
                app.UseHangfireDashboard("/jobs"); //hangfire endpoints

            }

            app.UseSharedInfrastructure();
            app.UseUnifyResultStatusCodes();
            app.UseStaticFiles();

        }


    }


}
