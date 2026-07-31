using ITS.Sync.Core.Abstractions;
using ITS.Sync.Infrastructure.Messaging;
using ITS.Sync.Infrastructure.Persistence;
using ITS.Sync.Application.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace ITS.Sync.Application;

/// <summary>
/// Đăng ký toàn bộ dịch vụ đồng bộ (đọc cấu hình DB, SqlSugar, các bảng, bộ chạy) vào DI.
/// Host (WPF/Worker) chỉ cần gọi services.AddSync(configuration).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSync(this IServiceCollection services, IConfiguration configuration)
    {
        // 1) Đọc section "DbConnection" (Database.json) -> options.
        var dbOptions = new DbConnectionOptions();
        configuration.GetSection(DbConnectionOptions.SectionName).Bind(dbOptions);
        services.AddSingleton(dbOptions);

        // 2) SqlSugar (2 DB) + bộ chọn nguồn/đích.
        services.AddSingleton<SqlSugarFactory>();
        services.AddSingleton<ISqlSugarClient>(sp => sp.GetRequiredService<SqlSugarFactory>().Create());
        services.AddSingleton<SyncDbAccessor>();

        // 2b) Bộ bước dùng chung (đọc/so sánh/ghi) — mỗi bảng gọi tường minh trong SyncAsync.
        services.AddSingleton<SyncSteps>();

        // 3) Các bảng đồng bộ (mỗi bảng một dòng ITableSync).
        //    Thứ tự: bảng danh mục/cha trước, bảng chi tiết/tham chiếu sau.

        // Nhóm cấu hình hệ thống (chuẩn hóa ID trước). SysConfigType là cha của SysConfigData.
        services.AddSingleton<ITableSync, SysConfigTypeSync>();
        services.AddSingleton<ITableSync, SysConfigDataSync>();
        services.AddSingleton<ITableSync, SysOpConfigSync>();

        services.AddSingleton<ITableSync, TmsEquipmentTypeSync>();
        services.AddSingleton<ITableSync, TmsEventTypeSync>();
        services.AddSingleton<ITableSync, TmsWorkUnitSync>();
        services.AddSingleton<ITableSync, TmsZoneSync>();
        services.AddSingleton<ITableSync, TmsMapSync>();
        services.AddSingleton<ITableSync, TmsEquipmentSync>();
        services.AddSingleton<ITableSync, TmsMapDetailSync>();
        services.AddSingleton<ITableSync, TmsZoneEquipmentSync>();
        services.AddSingleton<ITableSync, TmsIncidentSync>();
        services.AddSingleton<ITableSync, TmsZoneStatusSync>();

        // 4) Bộ chạy tổng.
        services.AddSingleton<ISyncManager, SyncManager>();

        // 5) Bộ điều khiển dùng chung (sở hữu vòng lặp chạy định kỳ) cho mọi host.
        services.AddSingleton<ISyncController, SyncController>();

        // 6) Tầng NATS riêng của WP_Sync: lắng nghe tín hiệu để đồng bộ MỘT bảng lẻ.
        var natsOptions = new NatsSyncOptions();
        configuration.GetSection(NatsSyncOptions.SectionName).Bind(natsOptions);
        services.AddSingleton(natsOptions);
        services.AddSingleton<NatsSyncListener>();

        return services;
    }
}
