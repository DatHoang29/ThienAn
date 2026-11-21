using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Shared.Configuration;
using Services.Shared.Extensions;
using Services.Shared.Messaging;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Shared.Persistence;
using Services.Vds.Shared.State;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Features.Counting;
using Services.Vds.Diagnostics;
using Services.Vds.Features.Events.Automation;
using Services.Vds.Features.Events;
using Services.Vds.Features.Journey;
using Services.Vds.Scheduling;
using Services.Vds.Features.Media;
using Services.Vds.Features.Statistic;
using Services.Vds.Features.Summary;
using Services.Vds.Pipeline.Persistence;
using Services.Vds.Features.VehicleFilter;
using Services.Vds.Features.ZoneStatus;
using Services.Vds.Features.ZoneTraffic;
using Services.Vds.Pipeline;
using Services.Vds.Pipeline.Mapping;
using Services.Vds.Manager;
// KienLT - 27/10/2026: Namespace Services.Vds.Shared che mất gốc Shared.* nên cần alias qua global::.
using QuestHaHostRoleEnum = global::Shared.QuestHa.Enums.QuestHaHostRoleEnum;
using static global::Shared.QuestHa.Extensions.QuestHaServiceCollectionExtensions; // services.AddQuestHa

namespace Services.Vds;

/// <summary>
/// Author: KienLT
/// Description: Một chỗ duy nhất đăng ký DI cho service VDS. Host (Worker/Wpf) chỉ gọi
/// <see cref="AddVdsServices"/>, không cần biết bên trong có gì.
///
/// Mỗi phase thêm một khối vào đây kèm ghi chú khối đó phục vụ tính năng nào — đọc file này là biết
/// service hiện có những gì.
/// Created date: 24/08/2026
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVdsServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // KienLT - 27/10/2026: kiểm cấu hình trước khi tạo host; Direct không đăng ký QuestHa.
        // KienLT - 31/10/2026: kiểm cấu hình + đăng ký QuestDB HA gom về AddQuestHa (vai Writer); repository nhận QuestHaManager
        // (null khi WriteMode=Direct và ReadMode=Single ⇒ đường ghi/đọc cũ giữ nguyên).
        services.AddQuestHa(configuration, Modules.VDS.Core.Helper.VdsBaseHelper.QuestTableHp.All, QuestHaHostRoleEnum.Writer);

        // ===== Nền dùng chung mọi service =====
        // DatabaseManager (SqlSugarScope nhiều kết nối, đọc Database.json section "DbConnection"),
        // TransportManager (NATS, đọc DataTransporter.json), ConfigLoaderRegistry.
        services.AddServiceFoundation(configuration);
        services.AddSingleton<Services.Vds.Features.Media.VdsImageShareSettingsRepository>();
        services.AddSingleton(sp => new Modules.VDS.Core.Services.Media.VdsImageShareAccess(
            ct => sp.GetRequiredService<Services.Vds.Features.Media.VdsImageShareSettingsRepository>().ReadAsync(ct)));
        services.AddSingleton(sp => new Modules.VDS.Core.Services.Media.VdsRegistrationImageStore(
            configuration.GetSection(Modules.VDS.Core.Options.VdsRegistrationImageOptions.Section)
                .Get<Modules.VDS.Core.Options.VdsRegistrationImageOptions>() ?? new(),
            sp.GetRequiredService<Modules.VDS.Core.Services.Media.VdsImageShareAccess>()));

        // ===== Cấu hình =====
        // ConfigOverrideStore cần StateDirectory, mà giá trị đó lại nằm trong section "Service".
        // Đọc trực tiếp từ IConfiguration ở đây (không qua VdsConfigManager) để tránh phụ thuộc vòng:
        // VdsConfigManager cần store trong constructor để nạp override ngay lúc khởi tạo.
        var stateDirectory =
            configuration[$"{ServiceConfig.SectionName}:{nameof(ServiceConfig.StateDirectory)}"] ?? "data";

        services.AddSingleton(sp => new ConfigOverrideStore(
            stateDirectory,
            sp.GetRequiredService<ILogger<ConfigOverrideStore>>()));

        services.AddSingleton<VdsConfigManager>();
        // Cùng MỘT instance cho cả hai kiểu: đăng ký riêng lẻ sẽ tạo hai bộ snapshot độc lập, đổi tham số
        // ở bộ này thì bộ kia không thấy.
        services.AddSingleton<IVdsConfigManager>(sp => sp.GetRequiredService<VdsConfigManager>());

        // Nạp lại cấu hình từ file theo chu kỳ. Tự tắt nếu ConfigReloadSeconds = 0 (mặc định).
        services.AddHostedService<ConfigReloadWorker>();

        // Nghe subject dùng chung ta.its.refresh: nhận MÃ refresh → nạp lại mảng dữ liệu tương ứng từ DB
        // (camera / SysConfigData / VdsConfig / … ; mã 0 = tất cả) → ghi file cache, dùng ngay không
        // restart. Mã VdsConfig ghi đè ngưỡng tốc độ + chu kỳ thống kê xuống snapshot.
        // Không kết nối được NATS thì tự bỏ qua, service vẫn chạy.
        services.AddHostedService<VdsRefreshSubscriber>();

        // ===== Persistence: MỘT repository cho MỘT bảng (hoặc một nhóm bảng danh mục) =====
        // Cầu dao QuestDB dùng CHUNG cho mọi repository bên dưới: nó mô tả một sự thật duy nhất — máy chủ
        // QuestDB có tới được hay không. Thiếu nó thì QuestDB xuống sẽ làm mỗi lượt gọi giữ luồng vài phút.
        services.AddSingleton<QuestAvailabilityGate>();

        services.AddSingleton<VdsCatalogRepository>();       // SQL Server: TmsEquipment, SysOpConfig
        services.AddSingleton<QuestTrafficDataRepository>(); // QuestDB: SqlSugar/PgWire đọc + ghi
        services.AddSingleton<QuestTrafficStatisticRepository>(); // QuestDB: số liệu chu kỳ thiết bị gửi
        services.AddSingleton<QuestCountReader>();                // QuestDB: đếm xe bằng SAMPLE BY

        // ===== Bản tin thống kê chu kỳ =====
        services.AddSingleton<StatisticHandler>();

        // ===== Đếm xe =====
        // Không có bộ đếm RAM và không có bảng đếm riêng: số tính lại từ bảng thô mỗi lần phát, nên
        // restart không làm nhảy số. Trigger để đường ghi không phải chờ truy vấn.
        services.AddSingleton<CountPublishTrigger>();
        services.AddSingleton<CountPayloadBuilder>();
        // Count snapshots are served by module SSE; no NATS count worker.

        // ===== Lưu lượng theo vùng (subject traffic) =====
        // Dùng LẠI QuestCountReader và CountChartBuilder của phần đếm: cùng một câu truy vấn, chỉ khác khoá
        // gộp (zoneId thay vì equipmentId). Nhân bản engine ra đây là mở đường cho hai subject ra số lệch.
        services.AddSingleton<ZoneTrafficPublishTrigger>();
        services.AddSingleton<ZoneTrafficPayloadBuilder>();
        services.AddHostedService<ZoneTrafficPublishWorker>();

        // ===== Sự kiện và sự cố =====
        // Sự cố ở lại SQL Server vì có vòng đời trạng thái và người dùng sửa trên web.
        services.AddSingleton<IncidentRepository>();
        services.AddSingleton<IncidentTypeCache>();
        services.AddSingleton<IncidentPromoter>();

        // Nhờ TMS xử lý tiếp sự cố vừa ghi (lập lịch kịch bản tự động). Tách hàng đợi + worker riêng để một
        // lượt API chậm không chặn queue tuần tự của pipeline. Tắt bằng Service:TmsApi:Enabled — worker tự
        // thoát, hàng đợi vẫn nhận việc và bỏ êm.
        // HttpClient qua IHttpClientFactory: handler được dùng lại nên chạy dài ngày không cạn socket.
        services.AddSingleton<IncidentAutomationQueue>();
        // Danh sách chờ bàn giao lệnh lập lịch, lưu ra file JSON để sống qua restart (hàng đợi trên là RAM).
        services.AddSingleton<IncidentDispatchStore>();
        services.AddHttpClient(IncidentAutomationApiClient.HttpClientName);
        // KienLT - 12/11/2026: client thứ hai bỏ kiểm tra chứng chỉ, chỉ được chọn khi Service:TmsApi:AllowSelfSignedCert = true.
        services.AddHttpClient(IncidentAutomationApiClient.InsecureHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });
        services.AddSingleton<IncidentAutomationApiClient>();
        services.AddHostedService<IncidentAutomationScheduler>();

        // ===== Trạng thái vùng =====
        // Máy trạng thái thuần RAM + repository ghi 3 cột của bảng dùng chung với TMS + worker quét theo giờ.
        // Worker là BẮT BUỘC: hai điều kiện hạ trạng thái đều theo thời gian nên không có bản tin nào tới để
        // kích hoạt. Tắt bằng Service:ZoneStatus:Enabled (mặc định tắt vì bảng dùng chung với worker cũ).
        services.AddSingleton<ZoneStatusTracker>();
        services.AddSingleton<ZoneStatusRepository>();
        services.AddSingleton<ZoneStatusUpdater>();

        // ===== Cảnh báo biển số (danh sách đen/trắng) =====
        // Danh sách giữ trong RAM + nạp lại theo chu kỳ vì người vận hành khai trên web. Cửa chống dội theo
        // biển nằm trong RAM chứ không ở DB: bảng lịch sử phải nhận mọi lượt xe đi qua.
        // Tắt bằng Service:VehicleFilter:Enabled (mặc định tắt vì ghi bảng dùng chung với worker cũ).
        services.AddSingleton<VehicleFilterRepository>();
        services.AddSingleton<VehicleFilterManager>();
        services.AddSingleton<PlateTripTracker>();
        services.AddSingleton<VehicleFilterNotifier>();

        // ===== Media (ảnh/clip kèm bản tin) =====
        // Hàng đợi có trần + worker nền: đường ghi chỉ đẩy việc, không tải, nên tốc độ nhận bản tin không phụ
        // thuộc tốc độ tải ảnh. Đường dẫn file suy tất định từ thời điểm + loại nên không cần bảng media.
        // Handler riêng: media dùng cùng khoá và cùng chứng thư với luồng SSE.
        services.AddSingleton<VdsMediaQueue>();
        services.AddSingleton<VdsMediaStorage>();
        services.AddHostedService<VdsMediaDownloader>();
        services.AddHttpClient(VdsMediaStorage.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var runtime = sp.GetRequiredService<IVdsConfigManager>().Current;
                var handler = new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    // Vẫn nhận thay đổi DNS dù handler sống lâu.
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = runtime.Media.EffectiveWorkerCount * 2,
                };

                if (runtime.Ingestion.AllowSelfSignedCert)
                    handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;

                return handler;
            });

        // ===== Hành trình A→B =====
        // Từ 29/09/2026 ghép THEO BẢN TIN: xe có biển tới camera cuối B ⇒ pipeline đẩy tín hiệu sang hàng đợi
        // riêng ⇒ JourneyArrivalWorker chọn đoạn (đoạn khai, không có thì đoạn tự sinh), tra QuestDB lượt cùng
        // biển ở camera đầu, ghi hành trình (luỹ đẳng) và nâng sự cố quá tốc độ TRUNG BÌNH. Job chu kỳ JourneyJob
        // còn lại làm lượt quét ĐỐI SOÁT. Tắt bằng Service:Journey:Enabled; Journey:Trigger=interval để quay về
        // chỉ chạy chu kỳ.
        services.AddSingleton<SegmentManager>();
        // Đoạn TỰ SINH từ km/tuyến/hướng + nhóm vds_segment; cache ra state/catalog.
        services.AddSingleton<AutoSegmentManager>();
        // Hai hàm chọn đoạn cho camera cuối: đoạn khai trước, không có thì đoạn tự sinh.
        services.AddSingleton<JourneySegmentResolver>();
        services.AddSingleton<QuestJourneySightingReader>();
        services.AddSingleton<JourneyRepository>();
        services.AddSingleton<JourneyProgressStore>();
        services.AddSingleton<JourneyWriter>();
        // Lượt quét đối soát theo chu kỳ (Journey:IntervalSeconds) — bù chuyến đường bản tin bỏ sót.
        services.AddSingleton<JourneyJob>();
        // Ghép THEO BẢN TIN (Journey:Trigger = arrival): pipeline đẩy tín hiệu, worker riêng tính ngay.
        services.AddSingleton<JourneyArrivalQueue>();
        services.AddSingleton<JourneyArrivalProcessor>();
        services.AddHostedService<JourneyArrivalWorker>();

        // ===== Job định kỳ =====
        // MỘT chỗ chạy mọi việc theo chu kỳ: quét trạng thái vùng, nạp lại danh sách biển, ghép hành trình,
        // nạp lại danh mục. Mỗi job một vòng lặp riêng nên job chậm không đẩy lùi job khác.
        // Những việc theo SỰ KIỆN (phát số đếm, tải media, gọi API lập lịch) KHÔNG nằm ở đây.
        services.AddSingleton<CatalogReloadJob>();
        services.AddHostedService<VdsJobScheduler>();

        // ===== Tổng hợp số liệu lâu dài (fallback báo cáo khi QuestDB lỗi) =====
        // Đọc số đếm từ QuestDB theo giờ vừa xong, gộp, ghi TmsTrafficSummary trên SQL Server. Ngày/tháng
        // gộp từ chính bảng summary (chỉ SQL Server) nên vẫn chạy khi QuestDB chết. Chạy trong VdsJobScheduler.
        services.AddSingleton<SqlTrafficSummaryRepository>();
        services.AddSingleton<TrafficSummaryJob>();
        services.AddSingleton<SummaryProgressStore>();

        // ===== Quy tắc nghiệp vụ thuần =====
        // Nơi duy nhất quy đổi đơn vị chiều dài và phân cỡ xe; đọc ngưỡng từ cấu hình mỗi lần gọi.
        services.AddSingleton<VehicleSizeResolver>();

        // ===== Chống trùng khi nâng sự cố: KHÔNG có store nào ở đây =====
        // Trước 10/09/2026 có EventDedupStore (RAM + {StateDirectory}/recent/{loại}.json) giữ mốc "sự việc
        // này vừa nâng sự cố". Đã BỎ: cache chỉ biết thời gian, không biết TRẠNG THÁI sự cố (TMS đổi trạng
        // thái, VDS không được thông báo), nên sự cố đã kết thúc vẫn chặn bản tin mới tới hết cửa sổ. Nay
        // IncidentPromoter truy vấn thẳng bảng sự cố qua IncidentRepository.FindOpenByConditionAsync, dấu vết
        // chống trùng là cột TmsIncident.ConditionKey. Đừng dựng lại cache ở đây.

        // ===== Publish NATS =====
        // Lớp chính sách quanh TransportManager: chưa cấu hình NATS thì bỏ qua êm, publish lỗi không
        // được làm đứt pipeline vì dữ liệu đã ghi DB xong.
        services.AddSingleton<VdsPublisher>();

        // Heartbeat ta.its.heartbeat.vds ({ data = "ok" } mỗi 30s) cho bên giám sát tính VDS
        // Online/Offline. Mặc định BẬT, không cần khai cấu hình; mục "Heartbeat" trong DataTransporter.json chỉ để ghi đè.
        // Dùng chung TransportManager với VdsPublisher (cùng kết nối NATS), chạy cho cả Worker lẫn WPF.
        services.AddHostedService(sp => new HeartbeatPublisher(
            sp.GetRequiredService<TransportManager>(), configuration,
            new HeartbeatOptions { Enabled = true, Subject = VdsSubjects.Heartbeat }));

        // ===== Danh mục giữ trong RAM =====
        // Singleton vì giữ trạng thái dùng chung: mọi bản tin đều tra cache camera này.
        // CatalogSnapshotStore ghi last-known-good ra file để DB chết lúc khởi động không chặn service —
        // chỉ cho các danh mục giữ THUẦN trong RAM (camera, vùng, đoạn); ngưỡng tốc độ và mapping đã sống
        // qua restart nhờ config-overrides.json.
        services.AddSingleton<CatalogSnapshotStore>();
        services.AddSingleton<CameraManager>();
        services.AddSingleton<ZoneManager>();          // camera → vùng, cho zoneId và trạng thái vùng
        services.AddSingleton<SpeedRangeResolver>();   // ngưỡng min/max: vùng chính → mặc định theo hướng
        services.AddSingleton<ConfigDataManager>();    // tên hiển thị vị trí/hướng, cho tên sự cố
        // KienLT - 31/10/2026: factory để truyền tenant sở hữu tham số QuestDB HA (QuestHa:SettingsTenantId) — giống nhau ở MS DI và Lamar.
        services.AddSingleton(sp => new OpConfigManager(sp.GetRequiredService<VdsCatalogRepository>(), sp.GetRequiredService<IVdsConfigManager>(),
            sp.GetRequiredService<ILogger<OpConfigManager>>(), sp.GetService<global::Shared.QuestHa.QuestHaManager>(),
            configuration[global::Modules.VDS.Core.Constants.VdsConfigConst.QuestHa.SettingsTenantConfigKey]));
        // Mapping cấu hình từ SysConfigData (cha-con): sự kiện VDS↔ITS, cỡ xe. Đẩy vào snapshot; fallback appsettings.
        services.AddSingleton<MappingManager>();

        // NƠI DUY NHẤT biết mảng nào nạp bằng bộ nạp nào. Ba đường refresh (khởi động, chu kỳ, NATS) đều
        // gọi qua đây — trước kia mỗi đường tự liệt kê lại chuỗi LoadAsync nên thêm mảng mới phải sửa 3 chỗ.
        services.AddSingleton<CatalogRefresher>();

        // ===== Đường dữ liệu thời gian thực + nguồn bản tin =====
        // Wolverine queue bounded/tuần tự → parse/normalize → camera cache → SqlSugar QuestDB → checkpoint.
        // Không có hosted service nào ở đây: vòng đời do VdsServiceManager điều phối.
        services.AddVdsPipeline(configuration);

        // ===== Hợp đồng với host =====
        // MỘT type (partial 2 file: vòng đời + giả lập) hiện thực cả hai nhóm contract. Host chỉ resolve
        // IServiceManager; UI/test resolve thêm IVdsFrameInjector khi cần bơm đúng một bản tin.
        // Đăng ký cùng MỘT instance cho ba kiểu: tách ra sẽ có ba manager với ba State độc lập.
        services.AddSingleton<VdsServiceManager>();
        services.AddSingleton<IVdsServiceManager>(sp => sp.GetRequiredService<VdsServiceManager>());
        services.AddSingleton<IVdsFrameInjector>(sp => sp.GetRequiredService<VdsServiceManager>());

        // ===== Chẩn đoán =====
        // Chỉ còn phép kiểm KẾT NỐI, chạy nền không chặn khởi động. Phép thử GHI end-to-end
        // (QuestDbWriteProbe) đã hoàn thành nhiệm vụ migration ngày 26/08/2026 và được xoá.
        services.AddHostedService<QuestDbHealthCheck>();

        // Doc so lieu quan sat cho harness WPF. KHONG nam trong duong du lieu.
        services.AddSingleton<VdsDiagnosticsRepository>();

        return services;
    }
}
