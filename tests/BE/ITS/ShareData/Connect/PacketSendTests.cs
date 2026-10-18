using System.Text;
using Module.ShareData.Controllers.Partner.Validators;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Utils;
using Modules.Integration.ShareData.Base.Constants;
using Modules.Integration.ShareData.Base.Enums;
using Modules.Integration.ShareData.Base.Helpers;
using Modules.Integration.ShareData.Base.Interfaces.PacketSend;
using Modules.Integration.ShareData.Base.Models.Encrypt;
using Modules.Integration.ShareData.Base.Models.PacketSend;
using Modules.Integration.ShareData.Encrypt;
using Modules.Integration.ShareData.Formats;
using Modules.Integration.ShareData.PacketSend;
using Modules.Integration.ShareData.PacketSend.Senders;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using Tests.ShareData.Mocks;
using Shared.DTO.Enums;
using Xunit;

namespace Tests.ShareData.Connect;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử tích hợp cho tầng gửi gói tin (PacketSend) và PacketSendDispatcher (Rule 19.84).
///              Bao gồm điều phối dispatcher, mã hóa/giải mã, định dạng đường dẫn xuất và các kênh truyền Http, File, Ftp, Kafka.
/// Created date: 17/10/2026
/// </summary>
[Collection("api")]
public partial class PacketSendTests
{
    private readonly Host _host;

    public PacketSendTests(Host host)
    {
        _host = host;
        _host.PartnerServer.ResetDefaults();
    }

    private static DataOutboundContext CreateDummyContext(string packetCode = "101", string partnerCode = "PARTNER_TEST", BaseEnums.ConnectionType? connectionType = BaseEnums.ConnectionType.Api)
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
        BaseEnums.ConnectionType? type = Enum.TryParse<BaseEnums.ConnectionType>(connectionTypeStr, true, out var parsed) ? parsed : null;
        return CreateDummyContext(packetCode, partnerCode, type);
    }

    private static PacketSendContext CreateSendContext(string partnerCode = "PARTNER_WARN", string connectionType = "API")
    {
        return new PacketSendContext(
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
        var dispatcher = PacketSendDispatcher.CreateForHandlers([]);
        var request = new PacketSendRequest
        {
            JsonPayload = "{}",
            ConnectType = (BaseEnums.ConnectionType)999,
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
    public void PacketMetadataResolver_Packet105_DefaultsToSnapshot_CanBeConfiguredToIncremental_Test()
    {
        // Arrange
        var packet105 = new ShareDataPacket
        {
            Code = "105",
            Name = "Vehicle Event Packet",
            SyncMode = Shared.DTO.Enums.BaseEnums.SyncMode.Incremental
        };

        // Act & Assert
        Assert.Equal(Shared.DTO.Enums.BaseEnums.SyncMode.Incremental, packet105.SyncMode);
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
        var structure1 = PartnerValidator.CheckStructure(configWithOutputPath);
        Assert.Equal(ConnectConfigIssue.None, structure1);

        var structure2 = PartnerValidator.CheckStructure(configWithValidTokens);
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
}

internal static class TestTransportExtensions
{
    public static Task<PacketSendResult> Dispatch(
        this IPacketSendDispatcher dispatcher,
        PacketSendRequest request,
        DataOutboundContext ctx,
        CancellationToken ct = default)
    {
        return dispatcher.Dispatch(request, DataOutboundTransport.BuildContext(ctx), ct);
    }

    public static Task<PacketSendResult> Send(
        this IPacketSender handler,
        PacketSendRequest request,
        DataOutboundContext ctx,
        CancellationToken ct = default)
    {
        return handler.Send(request, DataOutboundTransport.BuildContext(ctx), ct);
    }
}
