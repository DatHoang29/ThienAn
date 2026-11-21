using Module.Integration.CCTV.Core.Dto;

namespace Module.CCTV.Infrastructure.Services.Media;

/// <summary>
/// Author: ChuongLP
/// Description: Cache ngắn hạn (20 giây) danh sách thiết bị lấy từ VMS cho luồng chọn nguồn live
/// (<see cref="CctvLiveSourceService"/>): nhiều lượt mở camera liên tiếp / nhiều người xem cùng lúc
/// chỉ gọi NxVMS 1 lần thay vì tải toàn bộ danh sách thiết bị cho mỗi lượt.
/// - Single-flight: request đồng thời chờ cùng 1 lần tải.
/// - Lỗi khi tải → không lưu cache, ném lỗi cho caller xử lý như trước.
/// Danh sách trả về dùng chung giữa các request → caller chỉ được đọc, không sửa.
/// Created date: 21/10/2026
/// </summary>
public sealed class CctvVmsDeviceCache : ISingleton
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(20);

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    private sealed record Entry(IReadOnlyList<VmsDeviceOutput> Devices, DateTime ExpiresAtUtc);

    /// <param name="vendorKey">Khóa vendor (null/rỗng = vendor mặc định theo cấu hình).</param>
    /// <param name="loader">Hàm tải danh sách từ VMS (chạy trong scope của request đang gọi).</param>
    public async Task<IReadOnlyList<VmsDeviceOutput>> GetOrLoadAsync(
        string? vendorKey, Func<Task<List<VmsDeviceOutput>>> loader, CancellationToken cancellationToken = default)
    {
        var key = vendorKey?.Trim() ?? string.Empty;
        if (TryGetFresh(key, out var cached))
            return cached;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            // Request khác có thể vừa tải xong trong lúc chờ lock.
            if (TryGetFresh(key, out cached))
                return cached;

            var devices = await loader();
            lock (_entries)
                _entries[key] = new Entry(devices, DateTime.UtcNow.Add(Lifetime));
            return devices;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private bool TryGetFresh(string key, out IReadOnlyList<VmsDeviceOutput> devices)
    {
        lock (_entries)
        {
            if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAtUtc > DateTime.UtcNow)
            {
                devices = entry.Devices;
                return true;
            }
        }

        devices = Array.Empty<VmsDeviceOutput>();
        return false;
    }
}
