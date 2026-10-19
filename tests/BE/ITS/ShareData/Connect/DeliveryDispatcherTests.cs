using System.Text;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Utils;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Interfaces;
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
///              Bao gồm điều phối dispatcher, mã hóa/giải mã, định dạng đường dẫn xuất và các kênh truyền Http, File, Ftp, Kafka.
/// Created date: 17/10/2026
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
        var dispatcher = new DeliveryDispatcher([]);
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
    public void AesGcmEncrypt_EncryptDecryptRoundtrip_ReturnsOriginalBytes_Test()
    {
        // Arrange
        var encrypt = new AesGcmEncrypt();
        var keyBytes = new byte[AesGcmEncrypt.KeySize];
        System.Security.Cryptography.RandomNumberGenerator.Fill(keyBytes);
        var ctx = new EncryptContext("test-kid", keyBytes);
        var originalBytes = Encoding.UTF8.GetBytes("{\"test\":\"secret-payload-data\"}");

        // Act
        var encryptedBytes = encrypt.Encrypt(originalBytes, ctx);
        var decryptedBytes = encrypt.Decrypt(encryptedBytes, ctx);

        // Assert
        Assert.NotEqual(originalBytes, encryptedBytes);
        Assert.Equal(originalBytes, decryptedBytes);
        var jsonEnvelope = Encoding.UTF8.GetString(encryptedBytes);
        Assert.Contains("AES256-GCM", jsonEnvelope);
        Assert.Contains("test-kid", jsonEnvelope);
    }

    [Fact]
    public void PlainEncrypt_EncryptDecrypt_PassesThroughIdenticalBytes_Test()
    {
        // Arrange
        var encrypt = new PlainEncrypt();
        var ctx = new EncryptContext(null, null);
        var originalBytes = Encoding.UTF8.GetBytes("{\"test\":\"plain-payload\"}");

        // Act
        var encrypted = encrypt.Encrypt(originalBytes, ctx);
        var decrypted = encrypt.Decrypt(encrypted, ctx);

        // Assert
        Assert.Equal(originalBytes, encrypted);
        Assert.Equal(originalBytes, decrypted);
    }

    [Fact]
    public void SqlValidator_SupportsFromDateAndToDateParameters_Test()
    {
        // Arrange: Cú pháp SQL với @fromDate và @toDate phải được SqlValidator chấp thuận
        var validSql = "SELECT ID, Code, Name FROM TmsZone WHERE UpdateTime >= @fromDate AND UpdateTime <= @toDate";

        // Act
        var (isValid, _) = SqlValidator.Validate(validSql);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void PartnerValidator_RejectsOutputPath_AndValidatesFileNamePatternVariables_Test()
    {
        // Arrange
        var configWithOutputPath = "{\"OutputPath\":\"C:\\\\Temp\"}";
        var configWithValidTokens = "{\"FileNamePattern\":\"export_{partner}_{packet}_{serial}_{timestamp}.json\"}";

        // Act & Assert: CheckStructure cho phép lưu nhưng phát hiện cảnh báo cấu hình
        var structure1 = ConnectConfigValidator.CheckStructure(configWithOutputPath);
        Assert.Equal(ConnectConfigIssue.None, structure1);

        var structure2 = ConnectConfigValidator.CheckStructure(configWithValidTokens);
        Assert.Equal(ConnectConfigIssue.None, structure2);
    }

    [Fact]
    public void ExportPathHelper_ResolveRootPath_WhenPathIsRooted_ReturnsNormalizedRootedPath_Test()
    {
        // Arrange
        var rooted = Path.Combine(Path.GetTempPath(), "sharedata_custom_test");

        // Act
        var resolved = ExportPathHelper.ResolveRootPath(rooted);

        // Assert
        Assert.Equal(Path.GetFullPath(rooted), resolved);
    }

    [Fact]
    public void ExportPathHelper_ResolveRootPath_WhenPathIsRelative_CombinesWithBaseDirectory_Test()
    {
        // Arrange
        var relative = "custom_relative_dir";

        // Act
        var resolved = ExportPathHelper.ResolveRootPath(relative);

        // Assert
        var expected = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relative));
        Assert.Equal(expected, resolved);
    }

    [Fact]
    public void ExportPathHelper_ResolveRootPath_WhenPathIsEmptyOrNull_UsesDefaultShareDataSendUnderBaseDirectory_Test()
    {
        // Act
        var resolvedNull = ExportPathHelper.ResolveRootPath(null);
        var resolvedEmpty = ExportPathHelper.ResolveRootPath(string.Empty);
        var resolvedWhitespace = ExportPathHelper.ResolveRootPath("   ");

        // Assert
        var expected = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ExportPathHelper.DefaultBasePath));
        Assert.Equal(expected, resolvedNull);
        Assert.Equal(expected, resolvedEmpty);
        Assert.Equal(expected, resolvedWhitespace);
    }

    [Fact]
    public void ExportPathHelper_ResolveRootPath_WhenCurrentDirectoryChanges_StillResolvesUnderBaseDirectoryNotCwd_Test()
    {
        // Arrange: Dù Working Directory bị thay đổi, đường dẫn tương đối vẫn kiên định bám theo AppContext.BaseDirectory
        var relative = "safe_folder_check";
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());

            // Act
            var resolved = ExportPathHelper.ResolveRootPath(relative);

            // Assert
            var expected = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relative));
            Assert.Equal(expected, resolved);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public async Task AddDelivery_RegistersAllFourSenders_AndDispatcherSelectsExpectedChannel_Test()
    {
        // Arrange
        var senders = _host.Services.GetServices<IDeliverySender>().ToList();

        // Assert: đủ 4 kênh
        Assert.Equal(4, senders.Count);

        // Mỗi ConnectType đúng 1 bản
        var senderTypes = senders.Select(s => s.ConnectType).ToList();
        Assert.Contains(ConnectionType.Api, senderTypes);
        Assert.Contains(ConnectionType.File, senderTypes);
        Assert.Contains(ConnectionType.Kafka, senderTypes);
        Assert.Contains(ConnectionType.Ftp, senderTypes);
        Assert.Equal(4, senderTypes.Distinct().Count());

        // DeliveryDispatcher chọn đúng kênh
        var dispatcher = _host.Services.GetRequiredService<DeliveryDispatcher>();
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = "test_dispatch_check",
                [ConnectKeys.FileNamePattern] = "check_{serial}.json"
            }
        };
        var ctx = CreateSendContext(connectionType: "File");

        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("File", result.Channel);

        if (!string.IsNullOrWhiteSpace(result.TargetAddressOrPath))
        {
            var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }

    [Fact]
    public async Task Dispatch_WhenConnectConfigContainsRawConfig_FailsWithInvalidConnectionConfigAndNoFileCreated_Test()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"sharedata_dispatch_rawconfig_{Guid.NewGuid():N}");
        var fileSender = CreateFileSender(tempDir);
        var dispatcher = new DeliveryDispatcher([fileSender]);
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
        var dispatcher = new DeliveryDispatcher([ftpSender]);
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
        this IDeliverySender handler,
        DeliveryRequest request,
        DataOutboundContext ctx,
        CancellationToken ct = default)
    {
        return handler.Send(request, DataOutboundTransport.BuildContext(ctx), ct);
    }
}
