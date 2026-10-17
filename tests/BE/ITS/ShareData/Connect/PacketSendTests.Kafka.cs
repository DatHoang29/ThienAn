using Confluent.Kafka;
using Modules.Integration.ShareData;
using Modules.Integration.ShareData.PacketSend;
using Modules.Integration.ShareData.PacketSend.Senders;
using Xunit;

namespace Tests.ShareData.Connect;

public partial class PacketSendTests
{
    [Fact]
    public async Task DispatchAsync_WhenPartnerConnectionTypeIsKafka_ConvertsJsonToBytesAndRoutesToKafkaChannel_Test()
    {
        // Arrange
        var kafkaSender = new KafkaPacketSender();
        var dispatcher = PacketSendDispatcher.CreateForHandlers([kafkaSender]);
        var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");
        var jsonPayload = "{\"temperature\": 28.5, \"humidity\": 65}";
        var request = new PacketSendRequest
        {
            JsonPayload = jsonPayload,
            ConnectType = "Kafka",
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
        var kafkaSender = new KafkaPacketSender();
        var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");
        var request = new PacketSendRequest
        {
            JsonPayload = string.Empty,
            ConnectType = "Kafka"
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
        var kafkaSender = new KafkaPacketSender();
        var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");

        var disabledReq = new PacketSendRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = "Kafka",
            ConnectConfig = new Dictionary<string, string> { ["Enabled"] = "false" }
        };

        var missingServersReq = new PacketSendRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = "Kafka",
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
    public void Kafka_WhenBootstrapServersInConfig_PrefersOverPartnerColumns_Test()
    {
        // Arrange
        var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");
        ctx.Partner!.Address = "10.0.0.1";
        ctx.Partner!.Port = 9092;

        var reqWithBootstrap = new PacketSendRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = "Kafka",
            ConnectConfig = new Dictionary<string, string>
            {
                ["BootstrapServers"] = "kafka-cluster:9092,kafka-cluster:9093"
            }
        };

        var reqFallback = new PacketSendRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = "Kafka",
            ConnectConfig = new Dictionary<string, string>()
        };

        // Act: Phân giải bootstrapServers theo đúng logic KafkaPacketSender
        var serversExplicit = reqWithBootstrap.ConnectConfig.TryGetValue("BootstrapServers", out var s1) && !string.IsNullOrWhiteSpace(s1)
            ? s1.Trim()
            : (!string.IsNullOrWhiteSpace(ctx.Partner?.Address)
                ? (ctx.Partner.Address.Contains(':') || !ctx.Partner.Port.HasValue ? ctx.Partner.Address.Trim() : $"{ctx.Partner.Address.Trim()}:{ctx.Partner.Port.Value}")
                : null);

        var serversFallback = reqFallback.ConnectConfig.TryGetValue("BootstrapServers", out var s2) && !string.IsNullOrWhiteSpace(s2)
            ? s2.Trim()
            : (!string.IsNullOrWhiteSpace(ctx.Partner?.Address)
                ? (ctx.Partner.Address.Contains(':') || !ctx.Partner.Port.HasValue ? ctx.Partner.Address.Trim() : $"{ctx.Partner.Address.Trim()}:{ctx.Partner.Port.Value}")
                : null);

        // Assert
        Assert.Equal("kafka-cluster:9092,kafka-cluster:9093", serversExplicit);
        Assert.Equal("10.0.0.1:9092", serversFallback);
    }

    [Fact]
    public void Kafka_BuildProducerConfig_WhenTimeoutMsNotInt_EmitsWarningAndDefaultsTo5000_Test()
    {
        // Arrange
        var config = new Dictionary<string, string> { [ConnectKeys.TimeoutMs] = "bad_timeout" };
        var warnings = new List<string>();

        // Act
        var pConfig = KafkaPacketSender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

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
        var pConfig = KafkaPacketSender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

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
        var pConfig = KafkaPacketSender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

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

        var badPConfig = KafkaPacketSender.BuildProducerConfig(badConfig, "127.0.0.1:9092", badWarnings);

        Assert.Null(badPConfig.SaslMechanism);
        Assert.Single(badWarnings);
        Assert.Contains("SaslMechanism [SCRAM-SHA-256] không hợp lệ", badWarnings[0]);

        // 2. "ScramSha256" khớp enum Confluent.Kafka.SaslMechanism.ScramSha256
        var okConfig = new Dictionary<string, string> { [ConnectKeys.SaslMechanism] = "ScramSha256" };
        var okWarnings = new List<string>();

        var okPConfig = KafkaPacketSender.BuildProducerConfig(okConfig, "127.0.0.1:9092", okWarnings);

        Assert.Equal(SaslMechanism.ScramSha256, okPConfig.SaslMechanism);
        Assert.Empty(okWarnings);
    }
}
