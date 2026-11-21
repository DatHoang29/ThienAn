using System.Text.Json;
using Modules.DataTransporter.Natsio.Infrastructure.Configuration;

namespace Modules.DataTransporter.Natsio.Infrastructure.Handlers;

/// <summary>
/// High-level NATS client cho các service ngoài. Tự detect JetStream/PubSub theo subject config.
/// Hoàn toàn độc lập, không phụ thuộc Base hay ITS module.
/// </summary>
public class NatsClient
{
    private readonly Dictionary<string, NatsSubjectConfig> _subjectConfigs = new();
    private readonly Dictionary<string, string> _subjectToStream = new();
    private readonly string _clientName;

    public NatsClient(NatsRuntimeConfig? config = null, List<NatsStreamConfig>? streams = null)
    {
        _clientName = config?.ClientName ?? "NatsClient";
        if (config != null)
            InitializeAsync(config, streams).GetAwaiter().GetResult();
    }

    #region Initialization

    public async Task InitializeAsync(NatsRuntimeConfig config, List<NatsStreamConfig>? streams = null)
    {
        await NatsService.Instance.ConnectAsync(config);

        if (streams == null) return;

        foreach (var stream in streams)
        {
            if (string.IsNullOrWhiteSpace(stream.Name)) continue;

            string resolvedName = stream.Name;

            if (stream.InitStream && !string.IsNullOrWhiteSpace(stream.Subjects))
            {
                var subjects = stream.Subjects.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                //var maxAge = NatsConfigParser.ResolveMaxAge(stream.MaxAge);

                var actualStreamName = await NatsService.Instance.CreateStreamAsync(stream.Name, subjects, stream.Storage ?? "file");
                resolvedName = actualStreamName ?? stream.Name;
            }

            if (stream.SubjectsList != null)
            {
                foreach (var s in stream.SubjectsList)
                {
                    _subjectConfigs[s.Subject] = s;
                    _subjectToStream[s.Subject] = resolvedName;
                }
            }
        }
    }

    /// <summary>
    /// Đăng ký subject thủ công (dùng khi không có stream config)
    /// </summary>
    public void RegisterSubject(string subject, string? mode = null, string? streamName = null, string? consumerName = null)
    {
        _subjectConfigs[subject] = new NatsSubjectConfig
        {
            Subject = subject,
            Mode = mode,
            StreamName = streamName,
            ConsumerName = consumerName
        };
        if (!string.IsNullOrWhiteSpace(streamName))
            _subjectToStream[subject] = streamName;
    }

    #endregion

    #region Publish

    public bool Publish(string subject, object data)
    {
        var json = data is string s ? s : JsonSerializer.Serialize(data);
        var resolved = ResolveSubject(subject);

        return resolved.Mode == "jetstream" && resolved.StreamName != null
            ? NatsService.Instance.JsPublishAsync(subject, json).GetAwaiter().GetResult()
            : NatsService.Instance.PublishAsync(subject, json).GetAwaiter().GetResult();
    }

    public async Task<bool> PublishAsync(string subject, object data)
    {
        var json = data is string s ? s : JsonSerializer.Serialize(data);
        var resolved = ResolveSubject(subject);

        return resolved.Mode == "jetstream" && resolved.StreamName != null
            ? await NatsService.Instance.JsPublishAsync(subject, json)
            : await NatsService.Instance.PublishAsync(subject, json);
    }

    #endregion

    #region Subscribe / Consume

    public void Subscribe(string subject, Action<string> onData)
    {
        var resolved = ResolveSubject(subject);

        if (resolved.Mode == "jetstream" && resolved.StreamName != null)
            NatsService.Instance.ConsumeStream<string>(resolved.StreamName, resolved.ConsumerName, onData, filterSubject: subject);
        else
            NatsService.Instance.Subscribe<string>(subject, onData);
    }

    public void Subscribe(string subject, Func<string, Task> onData)
    {
        var resolved = ResolveSubject(subject);

        if (resolved.Mode == "jetstream" && resolved.StreamName != null)
            NatsService.Instance.ConsumeStream<string>(resolved.StreamName, resolved.ConsumerName, onData, filterSubject: subject);
        else
            NatsService.Instance.Subscribe<string>(subject, onData);
    }

    public void Unsubscribe(string subject)
    {
        var resolved = ResolveSubject(subject);
        if (resolved.Mode == "jetstream" && resolved.StreamName != null)
            NatsService.Instance.StopConsume(resolved.ConsumerName);
        else
            NatsService.Instance.Unsubscribe(subject);
    }

    #endregion

    #region Connection

    public bool IsConnected => NatsService.Instance.IsConnected;

    public async Task DisconnectAsync()
    {
        await NatsService.Instance.DisconnectAsync();
    }

    #endregion

    #region Private

    private ResolvedSubject ResolveSubject(string subject)
    {
        var safeConsumerName = $"{_clientName}-{subject}".Replace('.', '_');

        if (_subjectConfigs.TryGetValue(subject, out var cfg))
        {
            return new ResolvedSubject
            {
                Mode = cfg.Mode?.Trim().ToLower() ?? "pubsub",
                StreamName = _subjectToStream.TryGetValue(subject, out var s) ? s : null,
                ConsumerName = !string.IsNullOrWhiteSpace(cfg.ConsumerName) ? cfg.ConsumerName : safeConsumerName
            };
        }

        return new ResolvedSubject
        {
            Mode = "pubsub",
            StreamName = null,
            ConsumerName = safeConsumerName
        };
    }

    private class ResolvedSubject
    {
        public string Mode { get; set; } = "pubsub";
        public string? StreamName { get; set; }
        public string ConsumerName { get; set; } = string.Empty;
    }

    #endregion
}
