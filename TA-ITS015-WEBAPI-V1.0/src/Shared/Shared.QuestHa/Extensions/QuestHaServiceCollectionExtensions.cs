using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.QuestHa.Extensions;

/// <summary>
/// Author: KienLT
/// Description: Đăng ký Shared.QuestHa một cửa cho host (Worker/WPF, WebAPI, CLI). Thư viện chạy cả host không Furion
/// ⇒ đăng ký tay (CODING_RULES §8); cluster, router, reconciler, backup là singleton dùng chung trạng thái.
/// Created date: 09/10/2026
/// </summary>
public static class QuestHaServiceCollectionExtensions
{
    /// <summary>
    /// Đọc và kiểm section <c>QuestHa</c> của <paramref name="configuration"/>, đăng ký thành phần theo <paramref name="role"/> và
    /// <see cref="QuestHaManager"/>. Cấu hình sai ⇒ <see cref="InvalidOperationException"/> ngay lúc đăng ký (trước khi tạo host).
    /// Writer: WriteMode=Direct và ReadMode=Single ⇒ không đăng ký gì (không có Manager, host giữ đường cũ).
    /// Reader: ReadMode khác Router ⇒ không đăng ký gì. Admin: luôn đăng ký Manager vận hành (không replicator/worker nền).
    /// <paramref name="tables"/> = các bảng QuestDB được nhân bản (bắt buộc ≥ 1 khi có đăng ký).
    /// </summary>
    public static IServiceCollection AddQuestHa(this IServiceCollection services, IConfiguration configuration,
        IReadOnlyList<QuestTableSpec> tables, QuestHaHostRoleEnum role)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(tables);
        var section = configuration.GetSection(QuestHaOptions.SectionName);
        var options = section.Get<QuestHaOptions>() ?? new();
        return role switch
        {
            QuestHaHostRoleEnum.Writer => services.AddWriter(section, options, tables),
            QuestHaHostRoleEnum.Reader => services.AddReader(section, options, tables),
            QuestHaHostRoleEnum.Admin => services.AddAdmin(options, tables),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Vai QuestHa không hợp lệ."),
        };
    }

    // Kiểm theo đúng thứ tự Services.Vds đã dùng để thông báo lỗi khởi động không đổi.
    private static IServiceCollection AddWriter(this IServiceCollection services, IConfiguration section, QuestHaOptions quest,
        IReadOnlyList<QuestTableSpec> tables)
    {
        if (!Enum.IsDefined(quest.WriteMode))
            throw new InvalidOperationException("QuestHa:WriteMode không hợp lệ.");
        if (quest.WriteMode != QuestWriteModeEnum.Direct)
        {
            if (string.IsNullOrWhiteSpace(quest.JournalConnectionString))
                throw new InvalidOperationException("QuestHa:JournalConnectionString bắt buộc khi WriteMode=Shadow/Journal (DB journal riêng).");
            if (quest.Nodes is null || quest.Nodes.Count == 0)
                throw new InvalidOperationException("QuestHa:Nodes phải có ít nhất một node PHỤ khi WriteMode=Shadow.");
            if (quest.Journal.AppendQueueCapacity <= 0)
                throw new InvalidOperationException("QuestHa:Journal:AppendQueueCapacity phải lớn hơn 0.");
            // báo đúng khoá cấu hình ngay khi khởi động, không nổ muộn lúc resolve.
            if (quest.Journal.AppendMaxBatch <= 0)
                throw new InvalidOperationException("QuestHa:Journal:AppendMaxBatch phải lớn hơn 0.");
            // chỉ Shadow được bỏ qua lỗi bản sao; Journal phải chờ commit.
            if (quest.WriteMode == QuestWriteModeEnum.Journal)
            {
                if (quest.Nodes.Count < 2)
                    throw new InvalidOperationException("QuestHa:Nodes phải khai cả node chính và node phụ khi WriteMode=Journal.");
                if (quest.ReadMode != QuestReadModeEnum.Router)
                    throw new InvalidOperationException("QuestHa:WriteMode=Journal yêu cầu ReadMode=Router để job không đọc node chưa nhận đủ dữ liệu.");
            }
        }
        // Router chỉ hợp lệ khi cả hai node được journal cấp dữ liệu.
        if (!Enum.IsDefined(quest.ReadMode)) throw new InvalidOperationException("QuestHa:ReadMode không hợp lệ.");
        if (quest.ReadMode == QuestReadModeEnum.Router && quest.WriteMode != QuestWriteModeEnum.Journal)
            throw new InvalidOperationException("Services.Vds ReadMode=Router yêu cầu WriteMode=Journal và cả hai node.");
        if (quest.WriteMode == QuestWriteModeEnum.Direct) return services;
        ValidateExport(quest);

        services.Configure<QuestHaOptions>(section);
        services.AddQuestHaCoreServices(tables, runReplication: true);
        if (quest.WriteMode == QuestWriteModeEnum.Shadow)
        {
            // Đăng ký sau worker nền của core để thứ tự khởi động/dừng như trước.
            services.TryAddSingleton(sp => new QuestShadowCopyWorker(sp.GetRequiredService<IJournalStore>(),
                sp.GetRequiredService<IOptions<QuestHaOptions>>(), sp.GetRequiredService<ILogger<QuestShadowCopyWorker>>()));
            services.AddHostedService(sp => sp.GetRequiredService<QuestShadowCopyWorker>());
        }
        return services.AddManager(QuestHaHostRoleEnum.Writer);
    }

    // Export bật ⇒ cấu hình hạ tầng phải đủ ngay lúc khởi động (không nổ muộn lúc 02:00).
    private static void ValidateExport(QuestHaOptions quest)
    {
        try { quest.Export.ValidateInfrastructure(); }
        catch (ArgumentException ex) { throw new InvalidOperationException(ex.Message); }
        if (quest.Export.Enabled && !quest.Nodes.Any(n => !string.IsNullOrWhiteSpace(n.HttpUrl)))
            throw new InvalidOperationException("QuestHa:Export:Enabled=true cần ít nhất một node có HttpUrl.");
    }

    // Một tài nguyên applock cho mọi lượt ghi file/catalog bản xuất (xuất và retention; Worker và CLI). Trả false khi khoá đang bị giữ.
    private const string ExportLockResource = "QuestHa:Export";

    private static Func<Func<CancellationToken, Task>, CancellationToken, Task<bool>> ExportExclusive(QuestMaintenanceLockService lockService) =>
        (work, ct) => lockService.RunAsync(async token => { await work(token).ConfigureAwait(false); return true; }, ct, () => false);

    private static IServiceCollection AddReader(this IServiceCollection services, IConfiguration section, QuestHaOptions quest,
        IReadOnlyList<QuestTableSpec> tables)
    {
        // WebAPI chỉ đọc; replicator thuộc Worker, không được chạy hai chủ cursor.
        if (!Enum.IsDefined(quest.ReadMode)) throw new InvalidOperationException("QuestHa:ReadMode không hợp lệ.");
        if (quest.ReadMode != QuestReadModeEnum.Router) return services;
        if (string.IsNullOrWhiteSpace(quest.JournalConnectionString) || quest.Nodes.Count < 2)
            throw new InvalidOperationException("QuestHa Router cần JournalConnectionString và cả hai node trong Nodes.");
        services.Configure<QuestHaOptions>(section);
        services.AddQuestHaCoreServices(tables, runReplication: false);
        return services.AddManager(QuestHaHostRoleEnum.Reader);
    }

    // CLI dựng đúng các service mà lệnh cần như trước (không logger, đồng hồ hệ thống), không worker nền.
    private static IServiceCollection AddAdmin(this IServiceCollection services, QuestHaOptions o, IReadOnlyList<QuestTableSpec> tables)
    {
        if (string.IsNullOrWhiteSpace(o.JournalConnectionString))
            throw new InvalidOperationException("QuestHa:JournalConnectionString bắt buộc cho QuestHa.Admin.");
        if (tables.Count == 0) throw new ArgumentException("QuestHa cần ít nhất 1 bảng (QuestTableSpec).", nameof(tables));
        o.JournalMaintenance.ExportCountsAsBackup = o.Export.Enabled;
        services.TryAddSingleton(_ => new QuestHaSettingsService(o));
        services.TryAddSingleton(sp => new QuestJournalService(o.JournalConnectionString, o.CommandTimeoutSeconds, maintenance: o.JournalMaintenance,
            maintenanceSource: () => sp.GetRequiredService<QuestHaSettingsService>().Current.JournalMaintenance));
        services.TryAddSingleton<IJournalStore>(sp => sp.GetRequiredService<QuestJournalService>());
        services.TryAddSingleton(sp => new QuestJournalMaintenanceService(sp.GetRequiredService<IJournalStore>(), o,
            settings: sp.GetRequiredService<QuestHaSettingsService>()));
        services.TryAddSingleton(sp => new QuestHaHealthService(sp.GetRequiredService<IJournalStore>(), o, TimeProvider.System,
            settings: sp.GetRequiredService<QuestHaSettingsService>(), tables: tables));
        // Xuất/nạp Parquet chạy ngay trong CLI (kéo HTTP từ node, không dừng node); khoá applock chung với Worker.
        services.TryAddSingleton(sp =>
        {
            var clients = o.Nodes.Select(n => (IQuestNodeClient)new QuestNodeClientService(n, o.CommandTimeoutSeconds)).ToList();
            var nodes = o.Nodes.Select((n, i) => new QuestExportNodeService(n, clients[i], o.Export.HttpTimeout)).ToList();
            var settings = sp.GetRequiredService<QuestHaSettingsService>();
            return new QuestExportService(sp.GetRequiredService<IJournalStore>(), o, tables, nodes, new QuestNodeCoverageService(clients, tables),
                () => settings.Current, exclusive: ExportExclusive(new QuestMaintenanceLockService(o.JournalConnectionString, ExportLockResource)));
        });
        services.TryAddSingleton(sp => new QuestNodeCompareService(sp.GetRequiredService<IJournalStore>()));
        services.TryAddSingleton(_ => new QuestBackupAdminClientService(o));
        services.TryAddSingleton(_ => new QuestMaintenanceLockService(o.JournalConnectionString));
        services.TryAddSingleton(sp => new QuestHaManager(QuestHaHostRoleEnum.Admin, o,
            journal: () => sp.GetRequiredService<IJournalStore>(),
            maintenance: () => sp.GetRequiredService<QuestJournalMaintenanceService>(),
            health: () => sp.GetRequiredService<QuestHaHealthService>(),
            compare: () => sp.GetRequiredService<QuestNodeCompareService>(),
            nodeClient: node => new QuestNodeClientService(node, o.CommandTimeoutSeconds),
            adminPipe: () => sp.GetRequiredService<QuestBackupAdminClientService>(),
            maintenanceLock: () => sp.GetRequiredService<QuestMaintenanceLockService>(),
            settings: () => sp.GetRequiredService<QuestHaSettingsService>(),
            export: () => sp.GetRequiredService<QuestExportService>()));
        return services;
    }

    private static IServiceCollection AddManager(this IServiceCollection services, QuestHaHostRoleEnum role)
    {
        services.TryAddSingleton(sp =>
        {
            var o = sp.GetRequiredService<IOptions<QuestHaOptions>>().Value;
            return new QuestHaManager(role, o,
                journal: () => sp.GetRequiredService<IJournalStore>(),
                shadow: role == QuestHaHostRoleEnum.Writer && o.WriteMode == QuestWriteModeEnum.Shadow
                    ? () => sp.GetRequiredService<QuestShadowCopyWorker>() : null,
                coveredRead: o.ReadMode == QuestReadModeEnum.Router ? () => sp.GetRequiredService<QuestCoveredReadService>() : null,
                maintenance: () => sp.GetRequiredService<QuestJournalMaintenanceService>(),
                health: () => sp.GetRequiredService<QuestHaHealthService>(),
                settings: () => sp.GetRequiredService<QuestHaSettingsService>(),
                export: role == QuestHaHostRoleEnum.Writer ? () => sp.GetRequiredService<QuestExportService>() : null);
        });
        return services;
    }

    /// <summary>
    /// Đăng ký thành phần core dùng chung Writer/Reader (options đã Configure trước). Host (dashboard) có thể tự đăng ký
    /// <see cref="INodeControl"/> cho từng node (<c>services.AddSingleton&lt;INodeControl&gt;(...)</c>); thiếu thì backup/restore báo lỗi rõ ràng.
    /// </summary>
    internal static IServiceCollection AddQuestHaCoreServices(this IServiceCollection services, IReadOnlyList<QuestTableSpec> tables, bool runReplication = true)
    {
        if (tables.Count == 0) throw new ArgumentException("QuestHa cần ít nhất 1 bảng (QuestTableSpec).", nameof(tables));
        var specs = tables.ToArray();

        // Export bật ⇒ bản xuất Verified được tính như "có backup" khi dọn journal (cờ nội bộ, không bind từ cấu hình).
        services.PostConfigure<QuestHaOptions>(o => o.JournalMaintenance.ExportCountsAsBackup = o.Export.Enabled);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(sp => new QuestHaTimelineService(500, sp.GetRequiredService<TimeProvider>()));
        // Snapshot tham số vận hành dùng chung; mọi service dưới đây đọc nó mỗi vòng thay vì chụp options lúc dựng.
        services.TryAddSingleton(sp => new QuestHaSettingsService(sp.GetRequiredService<IOptions<QuestHaOptions>>().Value,
            sp.GetRequiredService<ILogger<QuestHaSettingsService>>()));
        services.TryAddSingleton(sp =>
        {
            var o = sp.GetRequiredService<IOptions<QuestHaOptions>>().Value;
            // chi tiết provider chỉ chuyển tới logger ở Debug.
            return new QuestJournalService(o.JournalConnectionString, o.CommandTimeoutSeconds, sp.GetRequiredService<TimeProvider>(), o.Journal, o.JournalMaintenance,
                sp.GetRequiredService<ILogger<QuestJournalService>>(),
                () => sp.GetRequiredService<QuestHaSettingsService>().Current.JournalMaintenance);
        });
        services.TryAddSingleton<IJournalStore>(sp => sp.GetRequiredService<QuestJournalService>());
        // service cho giám sát/reader; lịch chỉ ở chủ sở hữu replicator.
        services.TryAddSingleton(sp => new QuestJournalMaintenanceService(sp.GetRequiredService<IJournalStore>(),
            sp.GetRequiredService<IOptions<QuestHaOptions>>().Value, sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<QuestHaTimelineService>(), sp.GetRequiredService<ILogger<QuestJournalMaintenanceService>>(),
            sp.GetRequiredService<QuestHaSettingsService>()));

        services.TryAddSingleton(sp =>
        {
            var o = sp.GetRequiredService<IOptions<QuestHaOptions>>().Value;
            if (o.Nodes.Count == 0) throw new InvalidOperationException("QuestHa:Nodes phải có ít nhất 1 node.");
            var dup = o.Nodes.GroupBy(n => n.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (dup is not null) throw new InvalidOperationException($"Trùng tên node QuestHa: {dup.Key}");

            var clock = sp.GetRequiredService<TimeProvider>();
            var timeline = sp.GetRequiredService<QuestHaTimelineService>();
            var store = sp.GetRequiredService<IJournalStore>();
            var lf = sp.GetRequiredService<ILoggerFactory>();
            var settings = sp.GetRequiredService<QuestHaSettingsService>();

            // log reset pool dùng logger/client đúng node, chung đồng hồ với replicator/router.
            var clients = o.Nodes.Select(n => (IQuestNodeClient)new QuestNodeClientService(n, o.CommandTimeoutSeconds,
                lf.CreateLogger<QuestNodeClientService>(), clock)).ToList();
            var replicators = clients.Select(c => new QuestReplicatorService(store, c, specs, o.Replicator, clock, timeline,
                lf.CreateLogger<QuestReplicatorService>())).ToList();
            var byName = clients.ToDictionary(c => c.Node, StringComparer.OrdinalIgnoreCase);
            var statusSource = new JournalFallbackStatusSource(
                new SqlNodeStatusSource(store, o.Router.StatusCacheTtl, clock, o.Router.StatusRefreshTimeout),
                // host chỉ đọc không có heartbeat cục bộ để thay cho SQL.
                () => runReplication ? replicators.Select(r => r.Snapshot).ToList() : Array.Empty<NodeStatusOutput>(),
                clients, specs[0], o.Router, o.Replicator.ReadyMaxLag, clock, readerOnly: !runReplication);
            var router = new QuestNodeRouterService(o.Nodes,
                (node, ct) => byName[node].OpenLeaseAsync(ct),
                statusSource,
                // router và replicator cùng host vô hiệu đúng pool qua cùng client/node.
                o.Router, clock, timeline, lf.CreateLogger<QuestNodeRouterService>(),
                resetConnectionPoolWithReason: (node, reason) => byName[node].ResetConnectionPool(reason),
                runtime: () => settings.Current.Router);
            // chứng nhận per-node cần đủ mọi bảng của cluster.
            var reconciler = new QuestReconcilerService(store, clients, o.Reconciler, clock, timeline, lf.CreateLogger<QuestReconcilerService>(), specs);
            var controls = sp.GetServices<INodeControl>().ToList();
            // adapter backup local là bổ sung, ưu tiên adapter host đã đăng ký.
            foreach (var (node, control) in o.Backup.NodeControls)
                if (!controls.Any(c => c.Node.Equals(node, StringComparison.OrdinalIgnoreCase))) controls.Add(new QuestBackupNodeControlService(node, control));
            var backup = new QuestBackupService(store, replicators, controls, clients, specs,
                o.Backup, timeline, lf.CreateLogger<QuestBackupService>(), () => settings.Current.Backup);

            return new QuestHaClusterService(store, replicators, clients, specs,
                router, reconciler, backup, timeline, o, statusSource);
        });
        services.TryAddSingleton(sp => sp.GetRequiredService<QuestHaClusterService>().Router);
        // đường đọc có chứng nhận coverage dùng chung (Manager resolve khi ReadMode=Router);
        // dựng từ router/journal/client/bảng của CÙNG cluster singleton, chỉ tạo khi có nơi resolve.
        services.TryAddSingleton(sp =>
        {
            var cluster = sp.GetRequiredService<QuestHaClusterService>();
            var settings = sp.GetRequiredService<QuestHaSettingsService>();
            return new QuestCoveredReadService(cluster.Router, cluster.Store, cluster.NodeClients, cluster.Tables, cluster.Options.Router,
                () => settings.Current.Router);
        });
        services.TryAddSingleton(sp => sp.GetRequiredService<QuestHaClusterService>().Reconciler);
        services.TryAddSingleton(sp => sp.GetRequiredService<QuestHaClusterService>().Backup);
        // đối soát/vá giữa HAI node (vd node chính ↔ node phụ ở Shadow), khác Reconciler (journal ↔ node).
        services.TryAddSingleton(sp => new QuestNodeCompareService(sp.GetRequiredService<IJournalStore>(),
            sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILoggerFactory>().CreateLogger<QuestNodeCompareService>()));
        // giám sát (chỉ đọc journal) — dùng được ở cả reader-only.
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<QuestHaOptions>>().Value.Health);
        services.TryAddSingleton(sp => new QuestHaHealthService(sp.GetRequiredService<IJournalStore>(),
            sp.GetRequiredService<IOptions<QuestHaOptions>>().Value, sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<QuestHaHealthService>>(), sp.GetRequiredService<QuestHaSettingsService>(), specs));
        // lịch dùng duy nhất backup/replicator thuộc host sở hữu.
        services.TryAddSingleton(sp => new QuestBackupScheduleService(sp.GetRequiredService<IJournalStore>(),
            sp.GetRequiredService<QuestBackupService>(), sp.GetRequiredService<IOptions<QuestHaOptions>>().Value,
            sp.GetRequiredService<QuestHaTimelineService>(), sp.GetRequiredService<ILogger<QuestBackupScheduleService>>(),
            sp.GetRequiredService<QuestHaSettingsService>()));
        if (runReplication)
        {
            // phục hồi dấu stop trước replicator; shutdown đủ ngân sách cleanup độc lập.
            services.AddOptions<HostOptions>().PostConfigure<IOptions<QuestHaOptions>>((host, config) =>
            {
                if (!config.Value.Backup.Enabled && string.IsNullOrWhiteSpace(config.Value.Backup.AdminPipeName)) return;
                var minimum = config.Value.Backup.NodeStopTimeout + config.Value.Backup.NodeStartTimeout + TimeSpan.FromSeconds(30);
                if (host.ShutdownTimeout < minimum) host.ShutdownTimeout = minimum;
            });
            services.AddHostedService<QuestBackupRecoveryWorker>();
            services.AddHostedService<QuestHaHostedWorker>();
            services.AddHostedService<Infrastructure.Services.Health.Workers.QuestHaHealthWorker>();
            services.AddHostedService<QuestJournalMaintenanceWorker>();
            // lịch và IPC chỉ ở core, không đăng ký reader-only.
            services.AddHostedService<QuestBackupScheduleWorker>();
            services.AddHostedService<QuestBackupAdminWorker>();
            // Lịch xuất Parquet chỉ ở host sở hữu replicator (Writer); worker tự thoát khi Export:Enabled=false.
            services.TryAddSingleton(sp =>
            {
                var cluster = sp.GetRequiredService<QuestHaClusterService>();
                var settings = sp.GetRequiredService<QuestHaSettingsService>();
                var o = cluster.Options;
                var nodes = o.Nodes.Select(n => new QuestExportNodeService(n, cluster.NodeClient(n.Name), o.Export.HttpTimeout)).ToList();
                return new QuestExportService(cluster.Store, o, cluster.Tables, nodes, new QuestNodeCoverageService(cluster.NodeClients, cluster.Tables),
                    () => settings.Current, () => cluster.Router.GetSnapshot().CurrentNode,
                    ExportExclusive(new QuestMaintenanceLockService(o.JournalConnectionString, ExportLockResource)),
                    sp.GetRequiredService<TimeProvider>(), timeline: cluster.Timeline,
                    logger: sp.GetRequiredService<ILoggerFactory>().CreateLogger<QuestExportService>());
            });
            services.TryAddSingleton(sp => new QuestExportScheduleService(sp.GetRequiredService<QuestExportService>(),
                () => sp.GetRequiredService<QuestHaSettingsService>().Current, sp.GetRequiredService<QuestHaTimelineService>(),
                sp.GetRequiredService<ILogger<QuestExportScheduleService>>()));
            services.AddHostedService<Infrastructure.Services.Export.Workers.QuestExportScheduleWorker>();
        }
        return services;
    }
}
