using Services.Vds.Shared.Messaging;

namespace Services.Vds.Features.ZoneTraffic;

/// <summary>
/// Author: KienLT
/// Description: Cầu nối giữa đường ghi dữ liệu và <see cref="ZoneTrafficPublishWorker"/>: đường ghi báo
/// "vùng này vừa có xe đi qua", worker tự quyết định khi nào phát.
///
/// Khoá là <c>TmsZone.ID</c>, khác <see cref="Counting.CountPublishTrigger"/> (khoá là camera). Ở đây
/// KHÔNG lọc phạm vi: yêu cầu là xe đi qua vùng nào thì cập nhật vùng đó, nên mọi vùng đều đánh thức được.
/// Camera bị loại khỏi phạm vi đếm (<c>Counting:CountingEquipmentIds</c>) vẫn đánh thức nhưng số của vùng
/// sẽ không đổi vì bộ lọc áp ở tầng gộp — chấp nhận một lượt phát thừa thay vì nhân đôi chỗ khai bộ lọc.
///
/// Vùng rỗng (camera chưa gán vùng) tự bị bỏ ở <see cref="PublishSignal.Mark"/>.
/// Created date: 27/08/2026
/// </summary>
public sealed class ZoneTrafficPublishTrigger : IDisposable
{
    private readonly PublishSignal _signal = new();

    /// <summary>Đánh dấu một vùng vừa có xe đi qua. Gọi từ đường ghi nên phải rẻ và không ném.</summary>
    public void Notify(string? zoneId) => _signal.Mark(zoneId);

    /// <inheritdoc cref="PublishSignal.WaitAsync"/>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) => _signal.WaitAsync(timeout, ct);

    /// <inheritdoc cref="PublishSignal.Drain"/>
    public List<string> DrainChanged() => _signal.Drain();

    public void Dispose() => _signal.Dispose();
}
