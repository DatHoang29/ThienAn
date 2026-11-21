using Microsoft.Extensions.Configuration;
using Services.CctvProxy.Configuration;
using Services.Shared.Logging;
using Services.Shared.Persistence;
using SqlSugar;

namespace Services.CctvProxy.Features.PublicSnapshot;

/// <summary>
/// Author: kien.le
/// Description: Danh bạ camera công khai cho web người dân:
/// - Danh sách equipment được công khai: SysOpConfig GroupCode = <see cref="GroupCode"/>, mỗi dòng Value = equipmentId
///   (Value trống thì lấy Code). Dòng Status = Disable hoặc đã xoá thì bỏ qua.
/// - Ánh xạ equipment → camera: TmsEquipment.Ip = CctvDevice.Ip (giống màn hình thiết bị CCTV).
/// Cache toàn bộ trong DirectoryCacheSeconds; DB lỗi thì giữ danh bạ cũ.
/// Created date: 06/10/2026
/// </summary>
public sealed class PublicCameraDirectory(
    IConfiguration configuration,
    DatabaseManager databaseManager,
    IConfigManager configManager)
    : SqlSugarAccess(databaseManager, () => configuration["Service:DataConnectionId"] ?? "Default")
{
    public const string GroupCode = "cctv_public_equipment_ids";

    /// <summary>Status = Disable trong BaseEnums.StatusEnum.</summary>
    private const int StatusDisable = 0;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private Snapshot _snapshot = new(new Dictionary<string, PublicCamera>(StringComparer.OrdinalIgnoreCase), DateTimeOffset.MinValue);

    /// <summary>Tìm camera của một equipment công khai (dùng danh bạ cache).</summary>
    /// <param name="equipmentId">TmsEquipment.ID.</param>
    /// <param name="cancellationToken">Huỷ khi người xem ngắt kết nối.</param>
    /// <returns>Camera; null nếu equipment không công khai hoặc chưa ánh xạ được camera.</returns>
    public async Task<PublicCamera?> FindAsync(string equipmentId, CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(cancellationToken);
        return snapshot.Cameras.GetValueOrDefault(equipmentId);
    }

    private async Task<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromSeconds(Math.Max(1, configManager.Current.Public.DirectoryCacheSeconds));
        var current = Volatile.Read(ref _snapshot);
        if (DateTimeOffset.UtcNow - current.LoadedAt < ttl)
            return current;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            current = Volatile.Read(ref _snapshot);
            if (DateTimeOffset.UtcNow - current.LoadedAt < ttl)
                return current;

            try
            {
                var next = new Snapshot(await LoadAsync(), DateTimeOffset.UtcNow);
                Volatile.Write(ref _snapshot, next);
                return next;
            }
            catch (Exception exception)
            {
                // DB lỗi: giữ danh bạ cũ, thử lại ở chu kỳ sau (không để mỗi request đều dội DB).
                LogManager.Error(exception, "Không đọc được danh sách camera công khai; giữ danh sách cũ", kind: LogKind.Config);
                var kept = current with { LoadedAt = DateTimeOffset.UtcNow };
                Volatile.Write(ref _snapshot, kept);
                return kept;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<Dictionary<string, PublicCamera>> LoadAsync()
    {
        var rows = await Client.Ado.SqlQueryAsync<PublicCameraRow>(
            """
            SELECT e.ID AS EquipmentId, c.DeviceId, c.DeviceState, c.CreateTime
            FROM SysOpConfig o
            JOIN TmsEquipment e
                ON e.ID = COALESCE(NULLIF(LTRIM(RTRIM(o.Value)), ''), LTRIM(RTRIM(o.Code)))
                AND e.IsDelete IS NULL
            JOIN CctvDevice c
                ON c.Ip = e.Ip
                AND c.IsDelete IS NULL
            WHERE o.GroupCode = @GroupCode
                AND o.IsDelete IS NULL
                AND (o.Status IS NULL OR o.Status <> @StatusDisable)
            """,
            new SugarParameter[]
            {
                new("@GroupCode", GroupCode),
                new("@StatusDisable", StatusDisable),
            });

        // Một IP có thể trùng nhiều dòng CctvDevice (đồng bộ cũ) — lấy bản mới nhất.
        var cameras = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.EquipmentId) && !string.IsNullOrWhiteSpace(x.DeviceId))
            .GroupBy(x => x.EquipmentId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(x => x.CreateTime).First();
                    return new PublicCamera(g.Key, latest.DeviceId!, latest.DeviceState == 1);
                },
                StringComparer.OrdinalIgnoreCase);

        LogManager.Info($"Nạp {cameras.Count} camera công khai (SysOpConfig {GroupCode})", kind: LogKind.Config);
        return cameras;
    }

    private sealed record Snapshot(Dictionary<string, PublicCamera> Cameras, DateTimeOffset LoadedAt);

    private sealed class PublicCameraRow
    {
        public string? EquipmentId { get; set; }
        public string? DeviceId { get; set; }
        public int? DeviceState { get; set; }
        public DateTime? CreateTime { get; set; }
    }
}

/// <param name="IsOnline">CctvDevice.DeviceState = 1 (Hoạt động) theo lần đồng bộ gần nhất.</param>
public sealed record PublicCamera(string EquipmentId, string DeviceId, bool IsOnline);
