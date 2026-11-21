using System.Diagnostics;
using System.Text.Json;
using Modules.DataTransporter.Base.Core.Abstractions;
using Modules.DataTransporter.Natsio.Infrastructure.Configuration;
using Modules.DataTransporter.Natsio.Infrastructure.Handlers;

namespace Modules.DataTransporter.Base.Infrastructure.Handlers
{
    public delegate void NatsMessageReceived(string subject, object data);

    public class NatsEventHandler : IBaseEventHandler
    {
        public event NatsMessageReceived OnMessageReceived;
        public event DataReceivedHandler OnDataReceived;

        private readonly Task _initTask;
        private readonly Dictionary<string, NatsSubjectConfig> _subjectConfigs = new();
        private readonly Dictionary<string, string> _subjectToStream = new();
        private readonly HashSet<string> _registeredChannels = new();
        private readonly string _clientName;

        public NatsEventHandler(NatsRuntimeConfig? config = null, List<NatsStreamConfig>? streams = null)
        {
            _clientName = config?.ClientName ?? "WebApi";
            if (config != null)
                _initTask = InitializeAsync(config, streams);
            else
                _initTask = Task.CompletedTask;
        }

        private async Task InitializeAsync(NatsRuntimeConfig config, List<NatsStreamConfig>? streams)
        {
            try
            {
                await NatsService.Instance.ConnectAsync(config);

                if (streams != null)
                {
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Natsio] Init error: {ex.Message}");
            }
        }

        public Task WaitForInitializationAsync() => _initTask;

        public void RegisterSubject(NatsSubjectConfig subjectConfig)
        {
            _subjectConfigs[subjectConfig.Subject] = subjectConfig;
            if (!string.IsNullOrWhiteSpace(subjectConfig.StreamName))
                _subjectToStream[subjectConfig.Subject] = subjectConfig.StreamName;
        }

        public bool RegisterChannel(string keyName)
        {
            _registeredChannels.Add(keyName);
            return true;
        }

        /// <summary>
        /// Lấy resolved config cho subject: ưu tiên config từ file, fallback cho channel đã đăng ký.
        /// </summary>
        private ResolvedSubject ResolveSubject(string subject)
        {
            var safeConsumerName = $"{_clientName}-{subject}".Replace('.', '_');

            // Có config từ file → dùng config đó
            if (_subjectConfigs.TryGetValue(subject, out var cfg))
            {
                return new ResolvedSubject
                {
                    Mode = cfg.Mode?.Trim().ToLower() ?? "pubsub",
                    StreamName = _subjectToStream.TryGetValue(subject, out var s) ? s : null,
                    ConsumerName = !string.IsNullOrWhiteSpace(cfg.ConsumerName) ? cfg.ConsumerName : safeConsumerName
                };
            }

            // Không có config → pubsub mặc định
            return new ResolvedSubject
            {
                Mode = "pubsub",
                StreamName = null,
                ConsumerName = safeConsumerName
            };
        }

        public bool PublishData(string keyName, object data)
        {
            if (!NatsService.Instance.IsConnected) return false;

            var resolved = ResolveSubject(keyName);
            var json = data is string s ? s : JsonSerializer.Serialize(data);

            if (resolved.Mode == "jetstream" && resolved.StreamName != null)
                _ = NatsService.Instance.JsPublishAsync(keyName, json);
            else
                _ = NatsService.Instance.PublishAsync(keyName, json);

            return true;
        }

        public bool SubscribeData(string keyName)
        {
            if (!NatsService.Instance.IsConnected) return false;

            var resolved = ResolveSubject(keyName);
            var handler = new Action<string>(data =>
            {
                OnMessageReceived?.Invoke(keyName, data);
                OnDataReceived?.Invoke(keyName, data);
            });

            if (resolved.Mode == "jetstream" && resolved.StreamName != null)
                NatsService.Instance.ConsumeStream<string>(resolved.StreamName, resolved.ConsumerName, handler, filterSubject: keyName);
            else
                NatsService.Instance.Subscribe<string>(keyName, handler);

            return true;
        }

        private class ResolvedSubject
        {
            public string Mode { get; set; } = "pubsub";
            public string? StreamName { get; set; }
            public string ConsumerName { get; set; } = string.Empty;
        }
    }
}
