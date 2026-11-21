using Microsoft.Extensions.Logging;
using Services.Shared.Events;
using System.Collections.Concurrent;

namespace Services.Vds.Wpf.Logging;

/// <summary>
/// Author: KienLT
/// Description: Cầu nối <c>ILogger</c> → feed hoạt động của UI: mọi log của pipeline hiện lên cửa sổ
/// test. Thay <c>UiLog</c> static + <c>UiLoggerProvider</c> trước đây.
///
/// Vì sao vẫn cần cầu nối này thay vì chỉ dùng <c>ActivityPublisher</c> ở nghiệp vụ: log của
/// <c>ITS.VDS.Core</c> LÀ thứ chính để quan sát harness (camera nạp bao nhiêu, bản tin bị loại vì lý do
/// gì, job chạy ra sao). Nếu chỉ hiện activity do ViewModel/simulator tự phát thì cửa sổ mất gần hết
/// thông tin.
///
/// <b>KHÔNG đi qua MediatR/DI (bài học đã trả giá):</b> bản đầu tiên đẩy log vào
/// <c>ActivityPublisher</c>; MediatR khi đó phải resolve <c>INotificationHandler</c> là
/// <c>MainViewModel</c>, mà <c>MainViewModel</c> lại phụ thuộc <c>VdsConfigManager</c> — nếu log phát ra
/// ĐANG TRONG lúc container dựng <c>VdsConfigManager</c> thì DI dựng thêm một instance nữa, instance đó
/// lại log, thành vòng lặp vô hạn (quan sát được: file log đầy một dòng lặp lại hàng nghìn lần).
/// Vì vậy cầu nối chỉ phát một <see cref="ActivityLogged"/> event thuần, không chạm container.
/// Created date: 04/08/2026
/// </summary>
public sealed class ActivityLoggerProvider : ILoggerProvider
{
    /// <summary>Trần số log đệm trước khi UI đăng ký — chặn phình RAM nếu khởi động lỗi kéo dài.</summary>
    private const int PendingCapacity = 500;

    private readonly ConcurrentQueue<Activity> _pending = new();
    private readonly object _sync = new();
    private Action<Activity>? _sink;

    /// <summary>Mức tối thiểu đưa lên UI; Debug/Trace của pipeline quá nhiều nên bỏ.</summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    /// <summary>Phát mỗi khi có log mới. UI dùng <see cref="Subscribe"/> để nhận cả phần đã đệm.</summary>
    public event Action<Activity>? ActivityLogged;

    public ILogger CreateLogger(string categoryName) => new ActivityLogger(this, categoryName);

    public void Dispose() { }

    /// <summary>
    /// UI gọi một lần: nhận toàn bộ log đã đệm từ lúc khởi động rồi nhận tiếp các log sau đó.
    /// </summary>
    public void Subscribe(Action<Activity> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (_sync)
        {
            _sink = sink;
            while (_pending.TryDequeue(out var activity)) sink(activity);
        }
    }

    /// <summary>
    /// Đưa một activity không đến từ <c>ILogger</c> (simulator qua MediatR — xem
    /// <see cref="ActivityNotificationForwarder"/>) vào cùng feed; chưa có UI đăng ký thì đệm như log.
    /// </summary>
    public void Publish(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        Emit(activity);
    }

    private void Emit(Activity activity)
    {
        var sink = _sink;
        if (sink is not null)
        {
            sink(activity);
        }
        else
        {
            _pending.Enqueue(activity);
            while (_pending.Count > PendingCapacity) _pending.TryDequeue(out _);
        }

        ActivityLogged?.Invoke(activity);
    }

    private static ActivityLevel ToLevel(LogLevel level) => level switch
    {
        LogLevel.Error or LogLevel.Critical => ActivityLevel.Error,
        LogLevel.Warning => ActivityLevel.Warning,
        _ => ActivityLevel.Info
    };

    private sealed class ActivityLogger(ActivityLoggerProvider owner, string category) : ILogger
    {
        /// <summary>Chỉ giữ đoạn cuối của category cho gọn: <c>Services.Vds.Manager.VdsServiceManager</c> → <c>VdsServiceManager</c>.</summary>
        private readonly string _stage = category.Split('.').Last();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= owner.MinimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var detail = formatter(state, exception);
            if (exception is not null) detail += " — " + exception.Message;

            owner.Emit(new Activity(
                // global:: là BẮT BUỘC ở đây: project này có namespace Services.Vds.Shared, nên trong
                // Services.Vds.Wpf.* thì "Shared" được phân giải thành Services.Vds.Shared và mất đường
                // tới project Shared.Utility của repo.
                global::Shared.Utility.Utilities.BaseUtil.TimeServer.GetTimeServer(),
                _stage, detail, ToLevel(logLevel)));
        }
    }
}
