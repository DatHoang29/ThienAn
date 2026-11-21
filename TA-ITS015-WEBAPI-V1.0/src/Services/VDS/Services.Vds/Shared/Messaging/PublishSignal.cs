using System.Collections.Concurrent;

namespace Services.Vds.Shared.Messaging;

/// <summary>
/// Author: KienLT
/// Description: Nguyên thuỷ "có gì đổi thì đánh thức" dùng chung cho các worker phát dữ liệu gộp
/// (số đếm theo hướng, lưu lượng theo vùng).
///
/// <b>Vì sao không để mỗi worker tự giữ semaphore</b>: hai bản logic đồng thời y hệt nhau là chỗ dễ ruỗng
/// lệch nhau nhất — sửa trần semaphore hay thứ tự drain ở một bên rồi quên bên kia thì bản tin mất mà
/// không có lỗi nào.
///
/// Hai tính chất phải giữ:
/// <list type="number">
/// <item><b>Trần tín hiệu là 1</b>: nhiều lượt xe trong cùng cửa sổ chỉ tạo MỘT lần đánh thức; phần còn
/// lại đã nằm trong tập khoá nên không mất thông tin.</item>
/// <item><b>Chỉ giữ TẬP khoá, không giữ hàng đợi từng lượt</b>: worker gộp mọi thay đổi trong cửa sổ thành
/// một lần phát nên số lượt không quan trọng, chỉ cần biết phạm vi nào cần cập nhật.</item>
/// </list>
/// Created date: 27/08/2026
/// </summary>
public sealed class PublishSignal : IDisposable
{
    private readonly ConcurrentDictionary<string, byte> _changed = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _signal = new(0, 1);

    /// <summary>
    /// Đánh dấu một phạm vi vừa có dữ liệu mới rồi đánh thức worker. Gọi từ đường ghi nên phải rẻ và
    /// KHÔNG ném. Khoá rỗng ⇒ bỏ qua.
    /// </summary>
    public void Mark(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        _changed[key.Trim()] = 0;

        try
        {
            if (_signal.CurrentCount == 0) _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Có luồng khác vừa release; đã có tín hiệu chờ xử lý nên bỏ qua.
        }
    }

    /// <summary>
    /// Chờ tới khi có dữ liệu mới hoặc hết <paramref name="timeout"/>.
    /// Trả true nếu được đánh thức bởi dữ liệu mới, false nếu hết thời gian chờ.
    /// </summary>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) =>
        _signal.WaitAsync(timeout, ct);

    /// <summary>
    /// Lấy và XOÁ tập khoá đã đổi. Lấy TRƯỚC khi truy vấn: phạm vi đổi trong lúc đang truy vấn thuộc lượt
    /// phát sau, không bị mất.
    /// </summary>
    public List<string> Drain()
    {
        var result = new List<string>(_changed.Count);
        foreach (var key in _changed.Keys)
        {
            if (_changed.TryRemove(key, out _)) result.Add(key);
        }

        return result;
    }

    public void Dispose() => _signal.Dispose();
}
