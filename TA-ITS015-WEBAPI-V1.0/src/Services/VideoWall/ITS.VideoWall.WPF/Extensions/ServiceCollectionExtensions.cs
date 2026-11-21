using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Services.Shared.Events;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.ViewModels;

namespace Module.VideoWall.WPF.Extensions
{
    /// <summary>
    /// Description: Extension methods đăng ký toàn bộ DI services cho VideoWall WPF client
    /// Created date: 14/09/2026
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVideoWallWpf(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));

            services.AddMediatR(options =>
                options.RegisterServicesFromAssembly(typeof(ActivityPublisher).Assembly));

            services.AddSingleton<ActivityPublisher>();
            services.AddSingleton<SessionState>();
            services.AddSingleton<IUserConfirmation, MessageBoxUserConfirmation>();
            services.AddSingleton<ApiInvoker>();
            services.AddSingleton<VideoWallApiClient>();

            services.AddHttpClient(ApiInvoker.VideoWallClient, (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
                }
                client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", options.Language);
            });

            services.AddSingleton<ConnectionViewModel>();
            services.AddSingleton<ParametersViewModel>();
            services.AddSingleton<SceneSetupViewModel>();
            services.AddSingleton<ScheduleViewModel>();
            services.AddSingleton<ScenarioViewModel>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<INotificationHandler<ActivityNotification>>(
                sp => sp.GetRequiredService<MainViewModel>());
            services.AddSingleton<INotificationHandler<DeviceStepNotification>>(
                sp => sp.GetRequiredService<MainViewModel>());

            return services;
        }
    }
}
