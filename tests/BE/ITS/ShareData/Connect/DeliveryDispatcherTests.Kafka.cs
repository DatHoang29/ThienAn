using System.Text;
using Confluent.Kafka;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Base.Models.Formats;
using Modules.Integration.Delivery.Formats;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Senders;

namespace Tests.ShareData.Connect;

public partial class DeliveryDispatcherTests
{
    private string GetKafkaBootstrapServers()
    {
        var config = _host.Services.GetRequiredService<IConfiguration>();
        var servers = config["Kafka:BootstrapServers"];
        Assert.False(string.IsNullOrWhiteSpace(servers), "Chưa cấu hình Kafka:BootstrapServers trong appsettings.Test.json");
        return servers!;
    }

    private static IConsumer<string, byte[]> CreateKafkaConsumer(string bootstrapServers, string topic, string groupId)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
        consumer.Subscribe(topic);
        return consumer;
    }

    private static ConsumeResult<string, byte[]> ConsumeSingleMessage(IConsumer<string, byte[]> consumer, int timeoutSeconds = 15)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        ConsumeResult<string, byte[]>? cr = null;
        while (DateTime.UtcNow < deadline)
        {
            var res = consumer.Consume(TimeSpan.FromMilliseconds(300));
            if (res?.Message != null)
            {
                cr = res;
                break;
            }
        }

        Assert.True(cr != null && cr.Message != null, $"Không nhận được message từ Kafka topic trong vòng {timeoutSeconds}s.");
        return cr!;
    }

    [Fact]
    public async Task Kafka_Dispatch_WhenBrokerAvailable_ProducesFormattedMessageWithHeadersAndKey_Test()
    {
        // Arrange
        var bootstrapServers = GetKafkaBootstrapServers();
        var topic = $"sharedata.test.{Guid.NewGuid():N}";
        var groupId = $"group.test.{Guid.NewGuid():N}";
        using var kafkaSender = new KafkaDeliverySender();
        var dispatcher = new DeliveryDispatcher([kafkaSender]);

        var ctx = CreateSendContext(partnerCode: "PARTNER_KAFKA_REAL", connectionType: "Kafka");
        var jsonPayload = "{\"temperature\": 29.2, \"humidity\": 70}";
        var request = new DeliveryRequest
        {
            JsonPayload = jsonPayload,
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Topic"] = topic,
                ["BootstrapServers"] = bootstrapServers
            }
        };

        var formatCtx = new FormatContext(ctx.PartnerCode, ctx.PacketCode, ctx.SerialNbr, ctx.ExportedAt);
        var expectedTransport = new KafkaJsonFormat().ToTransport(jsonPayload, formatCtx);

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Gửi tới Kafka thất bại (Error: {result.ErrorCode} - {result.ErrorMessage}). Hãy đảm bảo Docker service tac-kafka đang chạy bằng lệnh: docker compose -f Docker/Compose/docker-compose.yml up -d tac-kafka");
        Assert.Equal("Kafka", result.Channel);
        Assert.Equal($"{topic}#p0@0", result.TargetAddressOrPath);
        Assert.Equal(expectedTransport.Bytes.Length, result.SentByteSize);

        using var consumer = CreateKafkaConsumer(bootstrapServers, topic, groupId);
        var cr = ConsumeSingleMessage(consumer, 15);

        Assert.Equal(ctx.SerialNbr.ToString(), cr.Message.Key);
        Assert.Equal(expectedTransport.Bytes, cr.Message.Value);
        Assert.NotNull(cr.Message.Headers);
        foreach (var (k, v) in expectedTransport.Headers)
        {
            var header = cr.Message.Headers.FirstOrDefault(h => h.Key == k);
            Assert.NotNull(header);
            Assert.Equal(v, Encoding.UTF8.GetString(header.GetValueBytes()));
        }
    }

    [Fact]
    public async Task Kafka_Send_WhenPayloadBytesProvided_PreservesExactBytes_Test()
    {
        // Arrange
        var bootstrapServers = GetKafkaBootstrapServers();
        var topic = $"sharedata.test.{Guid.NewGuid():N}";
        var groupId = $"group.test.{Guid.NewGuid():N}";
        using var kafkaSender = new KafkaDeliverySender();

        var ctx = CreateSendContext(partnerCode: "PARTNER_KAFKA_BYTES", connectionType: "Kafka");
        var rawBytes = Encoding.UTF8.GetBytes("CUSTOM_PRE_FORMATTED_KAFKA_PAYLOAD_12345");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"placeholder\": true}",
            PayloadBytes = rawBytes,
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Topic"] = topic,
                ["BootstrapServers"] = bootstrapServers
            }
        };

        // Act
        var result = await kafkaSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Gửi tới Kafka thất bại (Error: {result.ErrorCode} - {result.ErrorMessage}). Hãy đảm bảo Docker service tac-kafka đang chạy bằng lệnh: docker compose -f Docker/Compose/docker-compose.yml up -d tac-kafka");
        Assert.Equal(rawBytes.Length, result.SentByteSize);

        using var consumer = CreateKafkaConsumer(bootstrapServers, topic, groupId);
        var cr = ConsumeSingleMessage(consumer, 15);

        Assert.Equal(rawBytes, cr.Message.Value);
    }

    [Fact]
    public async Task Kafka_WhenBootstrapServersInConfig_PrefersOverPartnerColumns_Test()
    {
        // Arrange
        var bootstrapServers = GetKafkaBootstrapServers();
        var topic1 = $"sharedata.test.{Guid.NewGuid():N}";
        using var kafkaSender = new KafkaDeliverySender();

        // Trường hợp 1: Có BootstrapServers trong cấu hình, Address/Port của đối tác sai cổng -> ưu tiên cấu hình, gửi thành công
        var ctx1 = CreateSendContext(partnerCode: "PARTNER_PREF_CFG", connectionType: "Kafka");
        ctx1 = ctx1 with { Address = "127.0.0.1", Port = 1 };

        var req1 = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["BootstrapServers"] = bootstrapServers,
                ["Topic"] = topic1
            }
        };

        // Act 1
        var res1 = await kafkaSender.Send(req1, ctx1, CancellationToken.None);

        // Assert 1
        Assert.True(res1.Success, $"Ưu tiên BootstrapServers thất bại: {res1.ErrorCode} - {res1.ErrorMessage}");

        // Trường hợp 2: Không có BootstrapServers trong cấu hình, lấy Address và Port từ cột đối tác -> gửi thành công
        var topic2 = $"sharedata.test.{Guid.NewGuid():N}";
        var parts = bootstrapServers.Split(':');
        var host = parts[0];
        var port = int.Parse(parts[1]);

        var ctx2 = CreateSendContext(partnerCode: "PARTNER_FROM_COLUMNS", connectionType: "Kafka");
        ctx2 = ctx2 with { Address = host, Port = port };

        var req2 = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 2}",
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Topic"] = topic2
            }
        };

        // Act 2
        var res2 = await kafkaSender.Send(req2, ctx2, CancellationToken.None);

        // Assert 2
        Assert.True(res2.Success, $"Lấy địa chỉ từ cột đối tác thất bại: {res2.ErrorCode} - {res2.ErrorMessage}");
    }

    [Fact]
    public async Task Kafka_Send_WhenTopicNotSpecified_DefaultsToPacketCode_Test()
    {
        // Arrange
        var bootstrapServers = GetKafkaBootstrapServers();
        var uniquePacketCode = $"sharedata.test.{Guid.NewGuid():N}";
        var groupId = $"group.test.{Guid.NewGuid():N}";
        using var kafkaSender = new KafkaDeliverySender();

        var ctx = CreateSendContext(partnerCode: "PARTNER_DEFAULT_TOPIC", connectionType: "Kafka");
        ctx = ctx with { PacketCode = uniquePacketCode };

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"message\": \"default-topic-test\"}",
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["BootstrapServers"] = bootstrapServers
            }
        };

        // Act
        var result = await kafkaSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Gửi tới Kafka thất bại: {result.ErrorCode} - {result.ErrorMessage}");
        Assert.StartsWith($"{uniquePacketCode}#p", result.TargetAddressOrPath);

        using var consumer = CreateKafkaConsumer(bootstrapServers, uniquePacketCode, groupId);
        var cr = ConsumeSingleMessage(consumer, 15);

        Assert.NotNull(cr.Message);
        Assert.Contains("default-topic-test", Encoding.UTF8.GetString(cr.Message.Value));
    }

    [Fact]
    public async Task Kafka_Send_TwoConsecutiveMessagesToSameTopic_BothSucceedWithIncrementingOffset_Test()
    {
        // Arrange
        var bootstrapServers = GetKafkaBootstrapServers();
        var topic = $"sharedata.test.{Guid.NewGuid():N}";
        var groupId = $"group.test.{Guid.NewGuid():N}";
        using var kafkaSender = new KafkaDeliverySender();

        var ctx1 = CreateSendContext(partnerCode: "PARTNER_CONSECUTIVE", connectionType: "Kafka");
        ctx1 = ctx1 with { SerialNbr = 1 };
        var req1 = new DeliveryRequest
        {
            JsonPayload = "{\"seq\": 1}",
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Topic"] = topic,
                ["BootstrapServers"] = bootstrapServers
            }
        };

        var ctx2 = CreateSendContext(partnerCode: "PARTNER_CONSECUTIVE", connectionType: "Kafka");
        ctx2 = ctx2 with { SerialNbr = 2 };
        var req2 = new DeliveryRequest
        {
            JsonPayload = "{\"seq\": 2}",
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Topic"] = topic,
                ["BootstrapServers"] = bootstrapServers
            }
        };

        // Act
        var res1 = await kafkaSender.Send(req1, ctx1, CancellationToken.None);
        var res2 = await kafkaSender.Send(req2, ctx2, CancellationToken.None);

        // Assert
        Assert.True(res1.Success, $"Gửi lần 1 thất bại: {res1.ErrorCode}");
        Assert.True(res2.Success, $"Gửi lần 2 thất bại: {res2.ErrorCode}");

        Assert.Equal($"{topic}#p0@0", res1.TargetAddressOrPath);
        Assert.Equal($"{topic}#p0@1", res2.TargetAddressOrPath);

        using var consumer = CreateKafkaConsumer(bootstrapServers, topic, groupId);
        var cr1 = ConsumeSingleMessage(consumer, 15);
        var cr2 = ConsumeSingleMessage(consumer, 15);

        Assert.Equal("1", cr1.Message.Key);
        Assert.Equal("2", cr2.Message.Key);
    }
    [Fact]
    public async Task DispatchAsync_WhenPartnerConnectionTypeIsKafka_ConvertsJsonToBytesAndRoutesToKafkaChannel_Test()
    {
        // Arrange
        var kafkaSender = new KafkaDeliverySender();
        var dispatcher = new DeliveryDispatcher([kafkaSender]);
        var ctx = CreateDummyContext("101", "PARTNER_KAFKA", ConnectionType.Kafka);
        var jsonPayload = "{\"temperature\": 28.5, \"humidity\": 65}";
        var request = new DeliveryRequest
        {
            JsonPayload = jsonPayload,
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Topic"] = "traffic-weather-topic",
                ["BootstrapServers"] = "127.0.0.1:9092"
            }
        };

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Kafka", result.Channel);
        Assert.NotNull(result.ErrorCode);
        Assert.Equal("traffic-weather-topic", result.TargetAddressOrPath);
    }

    [Fact]
    public async Task KafkaSender_WhenPayloadIsEmpty_ReturnsFailedResult_Test()
    {
        // Arrange
        var kafkaSender = new KafkaDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_KAFKA", ConnectionType.Kafka);
        var request = new DeliveryRequest
        {
            JsonPayload = string.Empty,
            ConnectType = ConnectionType.Kafka
        };

        // Act
        var result = await kafkaSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Kafka", result.Channel);
        Assert.Equal("EmptyPayload", result.ErrorCode);
    }

    [Fact]
    public async Task KafkaSender_WhenDisabledOrMissingServers_ReturnsClearError_Test()
    {
        // Arrange
        var kafkaSender = new KafkaDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_KAFKA", ConnectionType.Kafka);

        var disabledReq = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string> { ["Enabled"] = "false" }
        };

        var missingServersReq = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string> { ["Enabled"] = "true", ["Topic"] = "my-topic" }
        };

        // Act
        var disabledRes = await kafkaSender.Send(disabledReq, ctx, CancellationToken.None);
        if (ctx.Partner != null) ctx.Partner.Address = null;
        var missingServersRes = await kafkaSender.Send(missingServersReq, ctx, CancellationToken.None);

        // Assert
        Assert.False(disabledRes.Success);
        Assert.Equal("Disabled", disabledRes.ErrorCode);

        Assert.False(missingServersRes.Success);
        Assert.Equal("MissingBootstrapServers", missingServersRes.ErrorCode);
    }



    [Fact]
    public void Kafka_BuildProducerConfig_WhenTimeoutMsNotInt_EmitsWarningAndDefaultsTo5000_Test()
    {
        // Arrange
        var config = new Dictionary<string, string> { [ConnectKeys.TimeoutMs] = "bad_timeout" };
        var warnings = new List<string>();

        // Act
        var pConfig = KafkaDeliverySender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

        // Assert
        Assert.Equal(5000, pConfig.MessageTimeoutMs);
        Assert.Single(warnings);
        Assert.Contains("không phải số nguyên hợp lệ, dùng mặc định 5000ms", warnings[0]);
    }

    [Theory]
    [InlineData("100", 500)]
    [InlineData("80000", 60000)]
    public void Kafka_BuildProducerConfig_WhenTimeoutMsOutOfRange_EmitsWarningAndClamps_Test(string inputVal, int expectedClamped)
    {
        // Arrange
        var config = new Dictionary<string, string> { [ConnectKeys.TimeoutMs] = inputVal };
        var warnings = new List<string>();

        // Act
        var pConfig = KafkaDeliverySender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

        // Assert
        Assert.Equal(expectedClamped, pConfig.MessageTimeoutMs);
        Assert.Single(warnings);
        Assert.Contains("vượt ngưỡng cho phép (500-60000ms)", warnings[0]);
    }

    [Fact]
    public void Kafka_BuildProducerConfig_WhenInvalidSecurityProtocol_EmitsWarning_Test()
    {
        // Arrange
        var config = new Dictionary<string, string> { [ConnectKeys.SecurityProtocol] = "BadProtoX" };
        var warnings = new List<string>();

        // Act
        var pConfig = KafkaDeliverySender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

        // Assert
        Assert.Null(pConfig.SecurityProtocol);
        Assert.Single(warnings);
        Assert.Contains("SecurityProtocol [BadProtoX] không hợp lệ", warnings[0]);
    }

    [Fact]
    public void Kafka_BuildProducerConfig_SaslMechanismScramHyphenFails_ScramSha256Succeeds_Test()
    {
        // 1. "SCRAM-SHA-256" chứa dấu gạch ngang nên không khớp enum Confluent.Kafka.SaslMechanism.ScramSha256
        var badConfig = new Dictionary<string, string> { [ConnectKeys.SaslMechanism] = "SCRAM-SHA-256" };
        var badWarnings = new List<string>();

        var badPConfig = KafkaDeliverySender.BuildProducerConfig(badConfig, "127.0.0.1:9092", badWarnings);

        Assert.Null(badPConfig.SaslMechanism);
        Assert.Single(badWarnings);
        Assert.Contains("SaslMechanism [SCRAM-SHA-256] không hợp lệ", badWarnings[0]);

        // 2. "ScramSha256" khớp enum Confluent.Kafka.SaslMechanism.ScramSha256
        var okConfig = new Dictionary<string, string> { [ConnectKeys.SaslMechanism] = "ScramSha256" };
        var okWarnings = new List<string>();

        var okPConfig = KafkaDeliverySender.BuildProducerConfig(okConfig, "127.0.0.1:9092", okWarnings);

        Assert.Equal(SaslMechanism.ScramSha256, okPConfig.SaslMechanism);
        Assert.Empty(okWarnings);
    }
}
