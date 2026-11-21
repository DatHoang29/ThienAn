using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Services.CctvProxy.Configuration;
using Services.Shared.Logging;
using Services.Shared.Persistence;
using SqlSugar;

namespace Services.CctvProxy.Features.PublicSnapshot;

/// <summary>
/// Author: kien.le
/// Description: Dùng chung thumbnail camera với WebAPI (file wwwroot/CCTV/snapshot/{deviceId}.jpg + CctvDevice.SnapshotUrl/SnapshotTime
/// do job Hangfire đồng bộ thiết bị tạo, xem MediaSerivce.SaveSnapshotAsync):
/// - <see cref="SaveAsync"/>: ảnh mới proxy vừa lấy được ghi ĐÈ file thumbnail + cập nhật SnapshotTime → FE nội bộ thấy ảnh mới nhất.
///   Ghi file kiểu tạm-rồi-đổi-tên nên người đọc không bao giờ thấy file dở; trùng lúc Hangfire đang ghi thì bỏ qua lượt này.
///   Không đụng gì tới job Hangfire (job vẫn tự chụp/ghi theo lịch của nó).
/// - <see cref="ReadAsync"/>: đọc thumbnail làm ảnh dự phòng khi NxVMS lỗi / camera offline.
/// Created date: 06/10/2026
/// </summary>
public sealed partial class WebApiThumbnailStore(
    IConfiguration configuration,
    DatabaseManager databaseManager,
    IConfigManager configManager)
    : SqlSugarAccess(databaseManager, () => configuration["Service:DataConnectionId"] ?? "Default")
{
    /// <summary>Thư mục tương đối trong wwwroot WebAPI — khớp giá trị SnapshotUrl Hangfire ghi (vd CCTV\snapshot\{id}.jpg).</summary>
    private const string RelativeFolder = @"CCTV\snapshot";

    /// <summary>Thời điểm ảnh gần nhất đã ghi cho từng camera — mỗi ảnh mới chỉ ghi 1 lần dù nhiều người xem.</summary>
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSaved = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEnabled => !string.IsNullOrWhiteSpace(configManager.Current.Public.WebApiSnapshotDirectory);

    /// <summary>Ghi đè thumbnail nếu <paramref name="fetchedAt"/> mới hơn lần ghi trước. Không ném lỗi.</summary>
    /// <param name="deviceId">DeviceId NxVMS (tên file thumbnail).</param>
    /// <param name="data">Ảnh jpg vừa lấy từ NxVMS.</param>
    /// <param name="fetchedAt">Thời điểm chụp (giờ server) — ghi vào CctvDevice.SnapshotTime.</param>
    public async Task SaveAsync(string deviceId, byte[] data, DateTimeOffset fetchedAt)
    {
        if (!IsEnabled || data.Length == 0)
            return;

        var previous = _lastSaved.GetOrAdd(deviceId, DateTimeOffset.MinValue);
        if (fetchedAt <= previous || !_lastSaved.TryUpdate(deviceId, fetchedAt, previous))
            return;

        var fileName = FileName(deviceId);
        var path = Path.Combine(configManager.Current.Public.WebApiSnapshotDirectory, fileName);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(tempPath, data);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Thường do Hangfire đang ghi đúng file đó — bỏ qua lượt này, ảnh sau sẽ ghi.
            TryDelete(tempPath);
            LogManager.Log(Microsoft.Extensions.Logging.LogLevel.Warning,
                $"Không ghi được thumbnail {path}: {exception.Message}", kind: LogKind.Data);
            return;
        }

        try
        {
            // SnapshotUrl chỉ điền khi đang trống (camera chưa từng có thumbnail) — có rồi thì giữ nguyên giá trị Hangfire ghi.
            await Client.Ado.ExecuteCommandAsync(
                """
                UPDATE CctvDevice
                SET SnapshotTime = @SnapshotTime,
                    SnapshotUrl = CASE WHEN SnapshotUrl IS NULL OR SnapshotUrl = '' THEN @SnapshotUrl ELSE SnapshotUrl END
                WHERE DeviceId = @DeviceId AND IsDelete IS NULL
                """,
                new SugarParameter[]
                {
                    new("@SnapshotTime", fetchedAt.LocalDateTime),
                    new("@SnapshotUrl", $@"{RelativeFolder}\{fileName}"),
                    new("@DeviceId", deviceId),
                });
        }
        catch (Exception exception)
        {
            LogManager.Error(exception, $"Không cập nhật được CctvDevice.SnapshotTime ({deviceId})", kind: LogKind.Data);
        }
    }

    /// <summary>Đọc thumbnail hiện có làm ảnh dự phòng.</summary>
    /// <param name="deviceId">DeviceId NxVMS.</param>
    /// <param name="cancellationToken">Huỷ khi người xem ngắt kết nối.</param>
    /// <returns>Ảnh + thời điểm ghi file; null nếu chưa cấu hình thư mục / chưa có file / đọc lỗi.</returns>
    public async Task<(byte[] Data, DateTime ModifiedAt)?> ReadAsync(string deviceId, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return null;

        var path = Path.Combine(configManager.Current.Public.WebApiSnapshotDirectory, FileName(deviceId));
        try
        {
            if (!File.Exists(path))
                return null;

            var data = await File.ReadAllBytesAsync(path, cancellationToken);
            return data.Length == 0 ? null : (data, File.GetLastWriteTime(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Cùng quy tắc đặt tên với MediaSerivce.SafeFileName của WebAPI.</summary>
    private static string FileName(string deviceId) => $"{UnsafeFileChars().Replace(deviceId, "_")}.jpg";

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_\-]")]
    private static partial Regex UnsafeFileChars();
}
