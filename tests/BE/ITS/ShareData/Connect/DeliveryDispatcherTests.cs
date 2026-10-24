using System.Text;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Helpers;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Base.Models.Encrypt;
using Modules.Integration.Delivery.Encrypt;
using Modules.Integration.Delivery.Senders;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;

namespace Tests.ShareData.Connect;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử tích hợp cho tầng chuyển giao (Delivery) và DeliveryDispatcher (Rule 19.84).
///              Bao gồm điều phối dispatcher và các kênh truyền Http, File, Ftp, Kafka.
/// Created date: 17/10/2026
/// Modified date: 24/10/2026
/// </summary>
[Collection("api")]
public partial class DeliveryDispatcherTests
{
    private readonly Host _host;

    public DeliveryDispatcherTests(Host host)
    {
        _host = host;
        _host.PartnerServer.ResetDefaults();
    }

    private FileDeliverySender CreateFileSender(string? basePath = null)
    {
        var path = basePath ?? ExportPathHelper.DefaultBasePath;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ExportPathHelper.BasePathConfigKey] = path
        }).Build();

        return new FileDeliverySender(config, _host.Services.GetRequiredService<ILogger<FileDeliverySender>>());
    }

    private DeliveryDispatcher CreateDispatcher(
        HttpDeliverySender? http = null,
        FileDeliverySender? file = null,
        FtpDeliverySender? ftp = null,
        KafkaDeliverySender? kafka = null)
    {
        return new DeliveryDispatcher(
            http ?? _host.Services.GetRequiredService<HttpDeliverySender>(),
            file ?? _host.Services.GetRequiredService<FileDeliverySender>(),
            ftp ?? _host.Services.GetRequiredService<FtpDeliverySender>(),
            kafka ?? _host.Services.GetRequiredService<KafkaDeliverySender>(),
            _host.Services.GetService<ILogger<DeliveryDispatcher>>());
    }

    private static DataOutboundContext CreateDummyContext(string packetCode = "101", string partnerCode = "PARTNER_TEST", ConnectionType? connectionType = ConnectionType.Api)
    {
        var packet = new ShareDataPacket { Code = packetCode, Name = "Packet " + packetCode };
        var partner = new ShareDataPartner
        {
            Code = partnerCode,
            Name = "Partner " + partnerCode,
            ConnectionType = connectionType,
            Address = "127.0.0.1",
            Port = 18090,
            EndPointApiUrl = "/api/v1/share"
        };
        var sub = new ShareDataSubscription { ID = "sub-test-1", PartnerId = "partner-1", DatatypeId = packetCode };
        return new DataOutboundContext(sub, partner, packet, null, DateTime.UtcNow, CancellationToken.None);
    }

    private static DataOutboundContext CreateDummyContext(string packetCode, string partnerCode, string connectionTypeStr)
    {
        ConnectionType? type = Enum.TryParse<ConnectionType>(connectionTypeStr, true, out var parsed) ? parsed : null;
        return CreateDummyContext(packetCode, partnerCode, type);
    }

    private static DeliveryContext CreateSendContext(string partnerCode = "PARTNER_WARN", string connectionType = "API")
    {
        return new DeliveryContext(
            PartnerCode: partnerCode,
            PacketCode: "101",
            SubscriptionId: "sub-warn-1",
            SerialNbr: 1,
            DatatypeId: "101",
            ExportedAt: DateTime.UtcNow,
            Address: "127.0.0.1",
            Port: ShareDataPartnerServerMock.DefaultPort,
            EndPointApiUrl: "/api/v1/warn"
        );
    }

    [Fact]
    public async Task DispatchAsync_WhenPartnerConnectionTypeIsUnknown_ReturnsUnsupportedError_Test()
    {
        // Arrange
        var dispatcher = CreateDispatcher();
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
            ConnectType = (ConnectionType)999,
            ConnectConfig = new Dictionary<string, string>()
        };

        // Act
        var result = await dispatcher.Dispatch(request, CreateSendContext(connectionType: "SatelliteMesh"), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("UnsupportedConnectionType", result.ErrorCode);
    }

    [Fact]
    public async Task Dispatch_WhenConnectConfigContainsRawConfig_FailsWithInvalidConnectionConfigAndNoFileCreated_Test()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"sharedata_dispatch_rawconfig_{Guid.NewGuid():N}");
        var fileSender = CreateFileSender(tempDir);
        var dispatcher = CreateDispatcher(file: fileSender);
        var ctx = CreateSendContext(partnerCode: "PARTNER_RAWCONFIG", connectionType: "File");

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.RawConfig] = "INVALID_RAW_CONFIG_PAYLOAD",
                [ConnectKeys.DirectoryPath] = tempDir,
                [ConnectKeys.FileNamePattern] = "raw_{serial}.json"
            }
        };

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("InvalidConnectionConfig", result.ErrorCode);

        if (Directory.Exists(tempDir))
        {
            Assert.Empty(Directory.GetFiles(tempDir));
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Dispatch_WhenConnectTypeIsFtpsAndConfigMissingUseSsl_DefaultsUseSslToTrueAndRoutesToFtpChannel_Test()
    {
        // Arrange
        var ftpSender = new FtpDeliverySender();
        var dispatcher = CreateDispatcher(ftp: ftpSender);
        var ctx = CreateSendContext(partnerCode: "PARTNER_FTPS_AUTOSSL", connectionType: "Ftps");

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Ftps,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.Enabled] = "false"
            }
        };

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.Equal("Ftp", result.Channel);
        Assert.True(request.ConnectConfig.ContainsKey(ConnectKeys.UseSsl));
        Assert.Equal("true", request.ConnectConfig[ConnectKeys.UseSsl]);
    }
}

internal static class TestTransportExtensions
{
    public static Task<DeliveryResult> Dispatch(
        this DeliveryDispatcher dispatcher,
        DeliveryRequest request,
        DataOutboundContext ctx,
        CancellationToken ct = default)
    {
        return dispatcher.Dispatch(request, DataOutboundTransport.BuildContext(ctx), ct);
    }

    public static Task<DeliveryResult> Send(
        this HttpDeliverySender handler,
        DeliveryRequest request,
        DataOutboundContext ctx,
        CancellationToken ct = default)
    {
        return handler.Send(request, DataOutboundTransport.BuildContext(ctx), ct);
    }

    public static Task<DeliveryResult> Send(
        this FileDeliverySender handler,
        DeliveryRequest request,
        DataOutboundContext ctx,
        CancellationToken ct = default)
    {
        return handler.Send(request, DataOutboundTransport.BuildContext(ctx), ct);
    }

    public static Task<DeliveryResult> Send(
        this FtpDeliverySender handler,
        DeliveryRequest request,
        DataOutboundContext ctx,
        CancellationToken ct = default)
    {
        return handler.Send(request, DataOutboundTransport.BuildContext(ctx), ct);
    }

    public static Task<DeliveryResult> Send(
        this KafkaDeliverySender handler,
        DeliveryRequest request,
        DataOutboundContext ctx,
        CancellationToken ct = default)
    {
        return handler.Send(request, DataOutboundTransport.BuildContext(ctx), ct);
    }
}
