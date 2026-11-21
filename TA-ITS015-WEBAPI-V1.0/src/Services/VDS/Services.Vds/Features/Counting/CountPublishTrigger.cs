using Services.Vds.Configuration;
using Services.Vds.Shared.Messaging;

namespace Services.Vds.Features.Counting;

/// <summary>
/// Author: KienLT
/// Description: Cầu nối giữa đường ghi dữ liệu và <see cref="CountPublishWorker"/>: đường ghi báo "camera
/// này vừa có dữ liệu mới", worker tự quyết định khi nào phát.
///
/// Tách ra để đường ghi KHÔNG phải chờ truy vấn đếm: nếu ghi xong rồi phát ngay tại đó thì mỗi lượt xe
/// phải chờ một truy vấn gộp 24h, và giờ cao điểm sẽ nghẽn queue tuần tự.
///
/// Phần đánh thức nằm ở <see cref="PublishSignal"/>; class này chỉ thêm CHÍNH SÁCH PHẠM VI:
/// <b>chỉ CAMERA ĐẾM đánh thức được lượt phát</b> (xem <see cref="IsCountingCamera"/>). Bản tin đếm là số
/// tổng quan theo hướng, nên xe qua một camera giữa tuyến không làm con số của hướng đổi; để nó đánh thức
/// thì mỗi lượt xe ở mọi camera đều kéo theo một truy vấn gộp 24h mà kết quả không khác lượt trước.
/// Số của các camera còn lại vẫn tiến theo lượt phát snapshot định kỳ
/// (<c>Publish:CountRefreshIntervalSeconds</c>).
/// Created date: 26/08/2026
/// </summary>
public sealed class CountPublishTrigger : IDisposable
{
    private readonly PublishSignal _signal = new();
    private readonly IVdsConfigManager _config;
    private readonly ILogger<CountPublishTrigger> _logger;

    private int _warnedNoScope;

    public CountPublishTrigger(IVdsConfigManager config, ILogger<CountPublishTrigger> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Đánh dấu một camera vừa có dữ liệu mới. Gọi từ đường ghi nên phải rẻ và không ném.
    /// Camera không thuộc phạm vi đếm ⇒ bỏ qua im lặng.
    /// </summary>
    public void Notify(string? equipmentId)
    {
        if (string.IsNullOrWhiteSpace(equipmentId)) return;

        var id = equipmentId.Trim();
        if (!IsCountingCamera(id)) return;

        _signal.Mark(id);
    }

    /// <summary>
    /// Camera có nằm trong phạm vi đếm hay không: thuộc một nhóm hướng
    /// (<see cref="PublishOptions.CountDirectionGroups"/>) hoặc có trong
    /// <see cref="PublishOptions.CountCameras"/> — tức camera CÓ MẶT trong payload theo cấu hình.
    ///
    /// Khai RỖNG cả hai ⇒ nhận MỌI camera (hành vi trước đây) và cảnh báo một lần: cấu hình rỗng nghĩa là
    /// chưa ai chọn phạm vi, chặn hết ở đây sẽ làm bản tin đếm chỉ còn phát theo chu kỳ mà không có dấu
    /// hiệu gì để lần ra.
    ///
    /// Đọc cấu hình ở MỖI lần gọi (không chốt trong constructor) vì nhóm hướng đổi được lúc đang chạy. Phép
    /// so sánh chạy trên vài phần tử nên rẻ hơn nhiều so với lượt ghi QuestDB vừa xong ngay trước đó.
    /// </summary>
    private bool IsCountingCamera(string equipmentId)
    {
        var publish = _config.Current.Publish;
        var groups = publish.CountDirectionGroups;
        var cameras = publish.CountCameras;

        if (groups.Count == 0 && cameras.Count == 0)
        {
            if (Interlocked.Exchange(ref _warnedNoScope, 1) == 0)
            {
                _logger.LogWarning(
                    "[Count] Chưa khai Publish:CountDirectionGroups lẫn Publish:CountCameras ⇒ nhận MỌI " +
                    "camera làm tín hiệu phát. Khai điểm vào của từng hướng để chỉ camera đếm đánh thức " +
                    "lượt phát.");
            }

            return true;
        }

        foreach (var id in cameras)
        {
            if (string.Equals(id, equipmentId, StringComparison.OrdinalIgnoreCase)) return true;
        }

        foreach (var group in groups)
        {
            foreach (var id in group.EquipmentIds)
            {
                if (string.Equals(id, equipmentId, StringComparison.OrdinalIgnoreCase)) return true;
            }
        }

        return false;
    }

    /// <inheritdoc cref="PublishSignal.WaitAsync"/>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) => _signal.WaitAsync(timeout, ct);

    /// <inheritdoc cref="PublishSignal.Drain"/>
    public List<string> DrainChanged() => _signal.Drain();

    public void Dispose() => _signal.Dispose();
}
