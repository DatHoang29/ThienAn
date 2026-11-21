namespace Shared.QuestHa.Infrastructure.Services.Timeline;

/// <summary>
/// Author: KienLT
/// Description: Timeline dùng chung (ring buffer) — mọi thành phần Core đẩy sự kiện vào đây; dashboard đọc
/// <see cref="GetRecent"/> hoặc nghe <see cref="Published"/>. Handler của event không được ném lỗi/chặn lâu.
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestHaTimelineService
{
    private readonly object _sync = new();
    private readonly Queue<QuestHaEventOutput> _items = new();
    private readonly int _capacity;
    private readonly TimeProvider _clock;

    /// <summary>Khởi tạo timeline vòng có sức chứa cố định và đồng hồ.</summary>
    public QuestHaTimelineService(int capacity = 500, TimeProvider? clock = null)
    {
        _capacity = Math.Max(10, capacity);
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>Phát khi có sự kiện timeline mới (dashboard/SSE đăng ký).</summary>
    public event EventHandler<QuestHaEventOutput>? Published;

    /// <summary>Thêm một sự kiện vào timeline (vòng đệm có trần) và phát <see cref="Published"/>.</summary>
    public void Publish(string source, string? node, QuestHaEventKindEnum kind, string message)
    {
        var e = new QuestHaEventOutput(_clock.GetUtcNow().UtcDateTime, source, node, kind, message);
        lock (_sync)
        {
            _items.Enqueue(e);
            while (_items.Count > _capacity) _items.Dequeue();
        }

        try
        {
            Published?.Invoke(this, e);
        }
        catch
        {
            // Không để lỗi của listener (dashboard) làm hỏng replicator/router.
        }
    }

    /// <summary>Mới nhất trước.</summary>
    public IReadOnlyList<QuestHaEventOutput> GetRecent(int take = 100)
    {
        lock (_sync) return _items.Reverse().Take(Math.Max(1, take)).ToList();
    }
}
