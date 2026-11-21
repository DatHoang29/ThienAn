using System.Collections.Concurrent;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Base.Models.Formats;
using Modules.Integration.Delivery.Formats;
using Modules.Integration.Delivery.Base.Enums;

namespace Modules.Integration.Delivery.Senders
{
    /// <summary>
    /// Description: Kênh vận chuyển xuất bản dữ liệu qua Kafka / Message Broker.
    ///              Sử dụng thư viện Confluent.Kafka chuẩn hóa kết nối cluster, hỗ trợ SASL/SSL và quản lý vòng đời producer tái dùng.
    ///              Truyền tải mảng byte[] = UTF-8(json) và đóng gói 4 header chuẩn hóa qua KafkaJsonFormat.
    /// Created date: 13/10/2026
    /// Modified date: 19/10/2026 (Gỡ IDeliverySender)
    /// </summary>
    public class KafkaDeliverySender(ILogger<KafkaDeliverySender>? logger = null) : IDisposable
    {
        public ConnectionType ConnectType => ConnectionType.Kafka;

        private static readonly ConcurrentDictionary<string, IProducer<string, byte[]>> _producers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Hàm thuần đọc cấu hình từ Dictionary và dựng ProducerConfig đầy đủ tham số SASL, SSL, ClientId.
        /// </summary>
        public static ProducerConfig BuildProducerConfig(
            IDictionary<string, string> config,
            string bootstrapServers,
            List<string>? warnings = null)
        {
            var reader = new ConnectConfigReader(config);
            var timeoutMs = 5000;
            var timeoutStr = reader.GetString(ConnectKeys.TimeoutMs);
            if (!string.IsNullOrWhiteSpace(timeoutStr))
            {
                if (!int.TryParse(timeoutStr, out var parsedTimeout))
                {
                    warnings?.Add($"Cấu hình TimeoutMs [{timeoutStr}] không phải số nguyên hợp lệ, dùng mặc định 5000ms.");
                }
                else if (parsedTimeout < 500 || parsedTimeout > 60000)
                {
                    timeoutMs = Math.Clamp(parsedTimeout, 500, 60000);
                    warnings?.Add($"Cấu hình TimeoutMs [{timeoutStr}] vượt ngưỡng cho phép (500-60000ms), tự động điều chỉnh về {timeoutMs}ms.");
                }
                else
                {
                    timeoutMs = parsedTimeout;
                }
            }

            var pConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                MessageTimeoutMs = timeoutMs,
                SocketTimeoutMs = timeoutMs,
                EnableIdempotence = true
            };

            var clientId = reader.GetString(ConnectKeys.ClientId);
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                pConfig.ClientId = clientId;
            }

            var secProtoStr = reader.GetString(ConnectKeys.SecurityProtocol);
            if (!string.IsNullOrWhiteSpace(secProtoStr))
            {
                if (Enum.TryParse<SecurityProtocol>(secProtoStr, ignoreCase: true, out var parsedSecProto))
                {
                    pConfig.SecurityProtocol = parsedSecProto;
                }
                else
                {
                    warnings?.Add($"Cấu hình SecurityProtocol [{secProtoStr}] không hợp lệ (hợp lệ: {string.Join(", ", Enum.GetNames<SecurityProtocol>())}), bỏ qua.");
                }
            }

            var saslMechStr = reader.GetString(ConnectKeys.SaslMechanism);
            if (!string.IsNullOrWhiteSpace(saslMechStr))
            {
                if (Enum.TryParse<SaslMechanism>(saslMechStr, ignoreCase: true, out var parsedSaslMech))
                {
                    pConfig.SaslMechanism = parsedSaslMech;
                }
                else
                {
                    warnings?.Add($"Cấu hình SaslMechanism [{saslMechStr}] không hợp lệ (hợp lệ: {string.Join(", ", Enum.GetNames<SaslMechanism>())}), bỏ qua.");
                }
            }

            var saslUser = reader.GetString(ConnectKeys.SaslUsername);
            if (!string.IsNullOrWhiteSpace(saslUser))
            {
                pConfig.SaslUsername = saslUser;
            }

            var saslPass = reader.GetRawString(ConnectKeys.SaslPassword, defaultValue: null, trim: false);
            if (!string.IsNullOrWhiteSpace(saslPass))
            {
                pConfig.SaslPassword = saslPass;
            }

            var caLoc = reader.GetString(ConnectKeys.SslCaLocation);
            if (!string.IsNullOrWhiteSpace(caLoc))
            {
                pConfig.SslCaLocation = caLoc;
            }

            return pConfig;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Xuất bản gói tin lên Kafka. Topic lấy từ cấu hình, không có thì dùng mã gói; BootstrapServers lấy từ cấu hình, không có thì ghép từ Address, Port.
        ///              Chỉ trạng thái Persisted mới tính thành công, PossiblyPersisted tính là lỗi. Enabled = false trả lỗi Disabled nên mốc không tiến.
        ///              Producer được dùng lại theo cấu hình, giữ trong _producers.
        /// Created date: 13/10/2026
        /// </summary>
        public virtual async Task<DeliveryResult> Send(DeliveryRequest request, DeliveryContext ctx, CancellationToken ct)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(request.JsonPayload))
            {
                logger?.LogWarning("❌ [Kafka] Payload dữ liệu rỗng, không thể xuất bản.");
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Kafka",
                    ErrorCode = "EmptyPayload",
                    ErrorMessage = "Payload dữ liệu rỗng.",
                    Warnings = warnings
                };
            }

            byte[] payloadBytes;
            IDictionary<string, string> headers;

            if (request.PayloadBytes != null && request.PayloadBytes.Length > 0)
            {
                payloadBytes = request.PayloadBytes;
                headers = request.Headers != null && request.Headers.Count > 0
                    ? request.Headers
                    : new KafkaJsonFormat().ToTransport(request.JsonPayload, new FormatContext(ctx.PartnerCode, ctx.PacketCode, ctx.SerialNbr, ctx.ExportedAt)).Headers;
            }
            else
            {
                var format = new KafkaJsonFormat();
                var formatCtx = new FormatContext(
                    ctx.PartnerCode,
                    ctx.PacketCode,
                    ctx.SerialNbr,
                    ctx.ExportedAt);

                var transportPayload = format.ToTransport(request.JsonPayload, formatCtx);
                payloadBytes = transportPayload.Bytes;
                headers = transportPayload.Headers;
            }

            var reader = new ConnectConfigReader(request.ConnectConfig);

            var topic = reader.GetString(ConnectKeys.Topic, ConnectKeys.TopicAliases)
                ?? (ctx.PacketCode ?? "default-topic");

            var bootstrapServers = reader.GetString(ConnectKeys.BootstrapServers)
                ?? (!string.IsNullOrWhiteSpace(ctx.Address)
                    ? (ctx.Address.Contains(':') || !ctx.Port.HasValue
                        ? ctx.Address.Trim()
                        : $"{ctx.Address.Trim()}:{ctx.Port.Value}")
                    : null);

            var isEnabled = reader.GetBool(ConnectKeys.Enabled, true);

            if (!isEnabled)
            {
                logger?.LogInformation("ℹ️ [Kafka] Kênh Kafka của đối tác [{Partner}] đang bị vô hiệu hóa (Enabled = false).", ctx.PartnerCode ?? "Unknown");
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Kafka",
                    ErrorCode = "Disabled",
                    ErrorMessage = $"Kênh Kafka của đối tác [{ctx.PartnerCode}] đang bị vô hiệu hóa (Enabled = false).",
                    TargetAddressOrPath = topic,
                    Warnings = warnings
                };
            }

            if (string.IsNullOrWhiteSpace(bootstrapServers) || bootstrapServers == ":")
            {
                logger?.LogWarning("❌ [Kafka] Thiếu cấu hình BootstrapServers cho kết nối Kafka của đối tác [{Partner}].", ctx.PartnerCode ?? "Unknown");
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Kafka",
                    ErrorCode = "MissingBootstrapServers",
                    ErrorMessage = "Thiếu thông tin BootstrapServers cho kết nối Kafka.",
                    TargetAddressOrPath = topic,
                    Warnings = warnings
                };
            }

            logger?.LogInformation("➡️ [Kafka] Đang xuất bản {ByteSize} bytes vào topic [{Topic}] tại cluster [{Servers}] cho đối tác [{Partner}]",
                payloadBytes.Length, topic, bootstrapServers, ctx.PartnerCode ?? "Unknown");

            try
            {
                var config = BuildProducerConfig(request.ConnectConfig, bootstrapServers, warnings);
                var producer = GetOrCreateProducer(config);

                var kafkaHeaders = new Headers();
                foreach (var (hk, hv) in headers)
                {
                    kafkaHeaders.Add(hk, Encoding.UTF8.GetBytes(hv));
                }

                var message = new Message<string, byte[]>
                {
                    Key = ctx.SerialNbr?.ToString() ?? ctx.SubscriptionId ?? Guid.NewGuid().ToString("N"),
                    Value = payloadBytes,
                    Headers = kafkaHeaders
                };

                var kafkaResult = await producer.ProduceAsync(topic, message, ct);

                // Chỉ tính là thành công khi Persisted tuyệt đối; PossiblyPersisted được tính là thất bại để bảo đảm độ tin cậy
                if (kafkaResult.Status == PersistenceStatus.Persisted)
                {
                    logger?.LogInformation("✅ [Kafka] Xuất bản thành công vào topic [{Topic}] offset [{Offset}] partition [{Partition}]",
                        topic, kafkaResult.Offset.Value, kafkaResult.Partition.Value);

                    return new DeliveryResult
                    {
                        Success = true,
                        Channel = "Kafka",
                        SentByteSize = payloadBytes.Length,
                        TargetAddressOrPath = $"{topic}#p{kafkaResult.Partition.Value}@{kafkaResult.Offset.Value}",
                        DetailJson = $"{{\"topic\":\"{topic}\",\"partition\":{kafkaResult.Partition.Value},\"offset\":{kafkaResult.Offset.Value}}}",
                        Warnings = warnings
                    };
                }

                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Kafka",
                    ErrorCode = kafkaResult.Status == PersistenceStatus.PossiblyPersisted ? "PossiblyPersisted" : "NotPersisted",
                    ErrorMessage = $"Gói tin chưa được ghi nhận vào Kafka cluster (Status: {kafkaResult.Status}).",
                    TargetAddressOrPath = topic,
                    Warnings = warnings
                };
            }
            catch (ProduceException<string, byte[]> pEx)
            {
                logger?.LogError(pEx, "❌ [Kafka] Lỗi ProduceException khi gửi vào topic [{Topic}]: {Error}", topic, pEx.Error.Reason);
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Kafka",
                    ErrorCode = pEx.Error.Code.ToString(),
                    ErrorMessage = $"Lỗi Kafka produce: {pEx.Error.Reason}",
                    TargetAddressOrPath = topic,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "❌ [Kafka] Ngoại lệ khi kết nối Kafka cluster: {Message}", ex.Message);
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Kafka",
                    ErrorCode = "KafkaConnectionError",
                    ErrorMessage = $"Ngoại lệ kết nối Kafka: {ex.Message}",
                    TargetAddressOrPath = topic,
                    Warnings = warnings
                };
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đẩy hết tin còn chờ, tối đa 2 giây mỗi producer, rồi giải phóng các producer đang giữ.
        /// Created date: 13/10/2026
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _producers)
            {
                try
                {
                    kvp.Value.Flush(TimeSpan.FromSeconds(2));
                    kvp.Value.Dispose();
                }
                catch
                {
                    // Ignore on shutdown
                }
            }
            _producers.Clear();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dùng lại producer theo cụm máy chủ, giao thức bảo mật, tài khoản và ClientId; chưa có thì tạo mới.
        /// Created date: 13/10/2026
        /// </summary>
        protected virtual IProducer<string, byte[]> GetOrCreateProducer(ProducerConfig config)
        {
            var cacheKey = $"{config.BootstrapServers}_{config.SecurityProtocol}_{config.SaslUsername}_{config.ClientId}";
            return _producers.GetOrAdd(cacheKey, _ => new ProducerBuilder<string, byte[]>(config).Build());
        }
    }
}
