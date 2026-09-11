using MediatR;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Interaction;
using Services.Shared.Events;

namespace Tests.Modules.VideoWall.Wpf;

/// <summary>
/// Author: Đạt
/// Description: IPublisher của MediatR chỉ ghi lại notification thay vì phát đi. Đứng thay MainViewModel
///              trong test: MainViewModel là WPF (System.Windows.Threading.Dispatcher) nên không dựng
///              được ở đây, nhưng nó tiêu thụ ĐÚNG hai loại notification này, nên đếm ở đây tương đương
///              đếm số dòng người vận hành thấy trên bảng log.
///              Trả Task.CompletedTask nên ActivityPublisher (fire-and-forget) ghi xong ngay trong lời
///              gọi — test không phải chờ hay Sleep.
/// Created date: 25/08/2026
/// </summary>
public sealed class RecordingPublisherTest : IPublisher
{
    private readonly List<INotification> _notifications = [];
    private readonly object _gate = new();

    public IReadOnlyList<INotification> Notifications
    {
        get
        {
            lock (_gate)
                return [.. _notifications];
        }
    }

    /// <summary>Dòng log chung ApiInvoker phát cho mỗi lượt gọi HTTP.</summary>
    public IReadOnlyList<ActivityNotification> ActivityRows =>
        [.. Notifications.OfType<ActivityNotification>()];

    /// <summary>Dòng log từng bước ISAPI (có kèm request/response) do VideoWallApiClient phát.</summary>
    public IReadOnlyList<DeviceStepNotification> DeviceStepRows =>
        [.. Notifications.OfType<DeviceStepNotification>()];

    /// <summary>Tổng số dòng hiện trên bảng log — gộp cả hai loại.</summary>
    public int TotalLogRows => ActivityRows.Count + DeviceStepRows.Count;

    public void Clear()
    {
        lock (_gate)
            _notifications.Clear();
    }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        if (notification is INotification typed)
            Record(typed);

        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        Record(notification);
        return Task.CompletedTask;
    }

    private void Record(INotification notification)
    {
        lock (_gate)
            _notifications.Add(notification);
    }
}

/// <summary>
/// Author: Đạt
/// Description: Trả sẵn Yes/No thay hộp thoại WPF. Luồng DryRun không hỏi xác nhận nên mặc định
///              trả false — nếu code lỡ hỏi thì test thấy CallCount > 0 và fail, đúng ý muốn.
/// Created date: 25/08/2026
/// </summary>
public sealed class UserConfirmationTest(bool answer) : IUserConfirmation
{
    public int CallCount { get; private set; }

    public string? LastMessage { get; private set; }

    public bool Confirm(string title, string message)
    {
        CallCount++;
        LastMessage = message;
        return answer;
    }
}

/// <summary>
/// Author: Đạt
/// Description: IHttpClientFactory trả về HttpClient của TAC_WebAPI in-memory cho MỌI tên client,
///              thay cho client dựng từ Configuration/Api.json. Nhờ đó ApiInvoker của client WPF
///              gọi thẳng vào backend thật trong tiến trình test.
/// Created date: 25/08/2026
/// </summary>
public sealed class InMemoryApiClientFactoryTest(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}

/// <summary>
/// Author: Đạt
/// Description: Gom sẵn cả cụm client WPF đã trỏ vào backend in-memory, để mỗi test khỏi lắp lại.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwWpfClientStackTest
{
    public required RecordingPublisherTest Publisher { get; init; }

    public required ActivityPublisher ActivityPublisher { get; init; }

    public required VideoWallApiClient ApiClient { get; init; }

    public required SessionState Session { get; init; }
}
