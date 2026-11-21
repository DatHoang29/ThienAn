using Modules.VDS.Infrastructure.Services.Filters.Mvc;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Shared.QuestHa.Enums;
using Shared.QuestHa.Extensions;
using Modules.VDS.Core.Helper;

namespace Modules.VDS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVDSCore(this IServiceCollection services)
        {


            return services;
        }

        public static IServiceCollection AddVDSInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // KienLT - 27/10/2026: WebAPI chỉ đọc; replicator thuộc Worker, không được chạy hai chủ cursor.
            // KienLT - 31/10/2026: kiểm cấu hình + đăng ký gom về AddQuestHa vai Reader; ReadMode khác Router ⇒ không đăng ký gì,
            // VdsRepository nhận QuestHaManager null và giữ đường cầu dao + probe cũ.
            services.AddQuestHa(configuration, VdsBaseHelper.QuestTableHp.All, QuestHaHostRoleEnum.Reader);
            // KienLT - 31/10/2026: áp tham số router (vds_questha_router_*) từ VdsConfig lúc khởi động; HA tắt thì worker thoát ngay.
            services.AddHostedService<Modules.VDS.Infrastructure.Services.QuestHa.Workers.VdsQuestHaSettingsWorker>();
            // Cầu dao QuestDB dùng CHUNG cho mọi VdsRepository<T>: singleton vì mô tả một sự thật
            // duy nhất — máy chủ QuestDB có tới được hay không. Thiếu nó thì QuestDB xuống làm mỗi request
            // api/vds/* treo ~5 phút và cạn thread pool (đo 26/08/2026). Xem VdsRepository.
            services.AddSingleton<QuestAvailabilityGate>();
            // Hub SSE là SINGLETON vì nó giữ các nhóm kết nối và vòng lấy dữ liệu dùng chung; scoped thì mỗi
            // request lại có một hub riêng và việc gộp nhóm trở thành vô nghĩa.
            Shared.Utility.Sse.SnapshotSseAuthorization.AddSnapshotSse(services);
            services.AddScoped(typeof(VdsRepository<>));
            services.AddControllers(options => options.Filters.Add<VdsRefreshAfterSaveFilter>());

            return services;
        }

        public static IServiceCollection AddVDSValidation(this IServiceCollection services)
        {
            services.AddControllers().AddVDSValidation();
            return services;
        }
    }
}
