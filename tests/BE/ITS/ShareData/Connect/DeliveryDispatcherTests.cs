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
using System.Net;
using Modules.Integration.Delivery.Base.Models.Formats;
using Modules.Integration.Delivery.Formats;
using Confluent.Kafka;

namespace Tests.ShareData.Connect;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử tích hợp cho tầng chuyển giao (Delivery) và DeliveryDispatcher (Rule 19.84).
///              Bao gồm điều phối dispatcher và các kênh truyền Http, File, Ftp, Kafka.
/// Created date: 17/10/2026
/// Modified date: 24/10/2026
/// </summary>
[Collection("api")]
public class DeliveryDispatcherTests
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

    #region File

    [Fact]
    public async Task DispatchAsync_WhenPartnerConnectionTypeIsFile_RoutesToFileChannel_Test()
    {
        // Arrange
        var fileSender = CreateFileSender();
        var dispatcher = CreateDispatcher(file: fileSender);
        var ctx = CreateDummyContext("103", "PARTNER_FILE", ConnectionType.File);
        var jsonPayload = "{\"vdsCount\": 12, \"speed\": 80}";
        var customDir = "Out/TestPartner";
        var fileNamePattern = "vds_export_{serial}.json";
        var request = new DeliveryRequest
        {
            JsonPayload = jsonPayload,
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                ["DirectoryPath"] = customDir,
                ["FileNamePattern"] = fileNamePattern
            }
        };

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("File", result.Channel);
        Assert.Equal(Path.Combine(customDir, "vds_export_0.json"), result.TargetAddressOrPath);
        Assert.Equal(Encoding.UTF8.GetByteCount(jsonPayload), result.SentByteSize);
    }

    /// <summary>
    /// Description: Kiểm thử FileDeliverySender từ chối đường dẫn thư mục không hợp lệ hoặc chứa path traversal.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("Out/Invalid|Path\0", "không hợp lệ")]
    [InlineData("safe/../../etc/passwd", "traversal")]
    public async Task FileSender_WhenDirectoryPathInvalidOrTraversal_ReturnsInvalidConfiguration_Test(string invalidDir, string expectedErrorKeyword)
    {
        // Arrange
        var fileSender = CreateFileSender();
        var ctx = CreateDummyContext("103", "PARTNER_FILE_INVALID", ConnectionType.File);
        var request = new DeliveryRequest
        {
            PayloadBytes = Encoding.UTF8.GetBytes("test-content"),
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                ["DirectoryPath"] = invalidDir
            }
        };

        // Act
        var result = await fileSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("File", result.Channel);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains(expectedErrorKeyword, result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FileSender_WhenDirectoryPathAndFileNamePatternConfigured_SavesToExpectedCustomPath_Test()
    {
        // Arrange
        var fileSender = CreateFileSender();
        var ctx = CreateDummyContext("103", "PARTNER_FILE_CUSTOM", ConnectionType.File);
        var expectedBytes = Encoding.UTF8.GetBytes("custom-file-payload-content");
        var request = new DeliveryRequest
        {
            PayloadBytes = expectedBytes,
            FileExtension = ".csv",
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                ["DirectoryPath"] = "custom_export_dir",
                ["FileNamePattern"] = "export_{partner}_{packet}_{timestamp}"
            }
        };

        // Act
        var result = await fileSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("File", result.Channel);
        Assert.NotNull(result.TargetAddressOrPath);
        Assert.Contains("custom_export_dir", result.TargetAddressOrPath);
        Assert.Contains("PARTNER_FILE_CUSTOM", result.TargetAddressOrPath);
        Assert.EndsWith(".csv", result.TargetAddressOrPath);

        var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
        Assert.True(File.Exists(fullPath));
        var savedBytes = await File.ReadAllBytesAsync(fullPath);
        Assert.Equal(expectedBytes, savedBytes);

        // Clean up
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    /// <summary>
    /// Description: Kiểm thử FileDeliverySender phát hiện cảnh báo khi FileNamePattern thiếu tham số biến đổi {serial} hoặc {timestamp}.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("static_export_file.json", true)]
    [InlineData("export_{serial}.json", false)]
    public async Task FileSender_WhenFileNamePatternTokensChecked_EmitsWarningAsExpected_Test(string fileNamePattern, bool shouldWarn)
    {
        // Arrange
        var handler = CreateFileSender();
        var ctx = CreateSendContext(connectionType: "File");
        var request = new DeliveryRequest
        {
            PayloadBytes = Encoding.UTF8.GetBytes("{\"file\": 1}"),
            ConnectType = ConnectionType.File,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = "test_warn_out",
                [ConnectKeys.FileNamePattern] = fileNamePattern
            }
        };

        // Act
        var result = await handler.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        if (shouldWarn)
        {
            Assert.Contains(result.Warnings, w => w.Contains("thiếu tham số biến đổi {serial} và {timestamp}"));
        }
        else
        {
            Assert.DoesNotContain(result.Warnings, w => w.Contains("thiếu tham số biến đổi {serial} và {timestamp}"));
        }

        // Dọn dẹp tệp sinh ra
        if (!string.IsNullOrWhiteSpace(result.TargetAddressOrPath))
        {
            var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }

    #endregion

    #region Ftp

    private (string Host, int Port, string Username, string Password) GetFtpSettings()
    {
        var config = _host.Services.GetRequiredService<IConfiguration>();
        var host = config["Ftp:Host"] ?? "127.0.0.1";
        var port = int.TryParse(config["Ftp:Port"], out var p) ? p : 2121;
        var username = config["Ftp:Username"] ?? "tactest";
        var password = config["Ftp:Password"] ?? "Tac@@1234";
        return (host, port, username, password);
    }

    private static void EnsureFtpDirectoryExists(string host, int port, string username, string password, string remotePath)
    {
#pragma warning disable SYSLIB0014
        var parts = remotePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = "";
        foreach (var part in parts)
        {
            current += "/" + part;
            try
            {
                var req = (FtpWebRequest)WebRequest.Create(new Uri($"ftp://{host}:{port}{current}"));
                req.Method = WebRequestMethods.Ftp.MakeDirectory;
                req.Credentials = new NetworkCredential(username, password);
                req.Timeout = 5000;
                using var resp = (FtpWebResponse)req.GetResponse();
            }
            catch (WebException ex) when (ex.Response is FtpWebResponse ftpResp &&
                (ftpResp.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable ||
                 ftpResp.StatusCode == FtpStatusCode.FileActionAborted ||
                 (int)ftpResp.StatusCode == 550))
            {
                // Thư mục đã tồn tại, tiếp tục
            }
        }
#pragma warning restore SYSLIB0014
    }

    private static byte[] DownloadFtpFile(string ftpUri, string username, string password)
    {
#pragma warning disable SYSLIB0014
        var req = (FtpWebRequest)WebRequest.Create(new Uri(ftpUri));
        req.Method = WebRequestMethods.Ftp.DownloadFile;
        req.Credentials = new NetworkCredential(username, password);
        req.UseBinary = true;
        req.KeepAlive = false;
        using var resp = (FtpWebResponse)req.GetResponse();
        using var stream = resp.GetResponseStream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
#pragma warning restore SYSLIB0014
    }

    [Fact]
    public async Task Ftp_Send_WhenCsvPayloadBytesProvided_UploadsSuccessfullyAndCanBeDownloaded_Test()
    {
        // Arrange
        var (host, port, user, pass) = GetFtpSettings();
        var remotePath = $"/sharedata-test/{Guid.NewGuid():N}";
        EnsureFtpDirectoryExists(host, port, user, pass, remotePath);

        var ftpSender = new FtpDeliverySender();
        var ctx = CreateSendContext(partnerCode: "PARTNER_FTP_REAL", connectionType: "FTP");
        ctx = ctx with { Address = host, Port = port };

        var csvContent = "ID,Code,Name,Status\r\n101,A1,Device1,Active\r\n102,B2,Device2,Inactive\r\n";
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"ignored\": true}",
            PayloadBytes = csvBytes,
            ContentType = "text/csv",
            FileExtension = ".csv",
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.Username] = user,
                [ConnectKeys.Password] = pass,
                [ConnectKeys.RemotePath] = remotePath
            }
        };

        // Act
        var result = await ftpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Gửi tới FTP thất bại (Error: {result.ErrorCode} - {result.ErrorMessage}). Hãy đảm bảo Docker service tac-ftp đang chạy bằng lệnh: docker compose -f Docker/Compose/docker-compose.yml up -d tac-ftp");
        Assert.Equal("Ftp", result.Channel);
        Assert.Equal(csvBytes.Length, result.SentByteSize);
        Assert.NotNull(result.TargetAddressOrPath);
        Assert.EndsWith(".csv", result.TargetAddressOrPath);
        Assert.Contains(remotePath, result.TargetAddressOrPath);

        var downloadedBytes = DownloadFtpFile(result.TargetAddressOrPath, user, pass);
        Assert.Equal(csvBytes, downloadedBytes);
    }

    [Fact]
    public async Task Ftp_Send_WhenWrongPassword_ReturnsFtpWebException_Test()
    {
        // Arrange
        var (host, port, user, _) = GetFtpSettings();
        var remotePath = $"/sharedata-test/{Guid.NewGuid():N}";
        var wrongPassword = "TotallyWrongPassword@@@";

        var ftpSender = new FtpDeliverySender();
        var ctx = CreateSendContext(partnerCode: "PARTNER_FTP_BAD_PASS", connectionType: "FTP");
        ctx = ctx with { Address = host, Port = port };

        var request = new DeliveryRequest
        {
            JsonPayload = "[{\"id\": 1}]",
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.Username] = user,
                [ConnectKeys.Password] = wrongPassword,
                [ConnectKeys.RemotePath] = remotePath
            }
        };

        // Act
        var result = await ftpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Ftp", result.Channel);
        Assert.Equal("FtpWebException", result.ErrorCode);
    }

    [Fact]
    public async Task Ftp_Send_WhenCsvIncludeBomIsTrue_UploadedFileStartsWithUtf8Bom_Test()
    {
        // Arrange
        var (host, port, user, pass) = GetFtpSettings();
        var remotePath = $"/sharedata-test/{Guid.NewGuid():N}";
        EnsureFtpDirectoryExists(host, port, user, pass, remotePath);

        var ftpSender = new FtpDeliverySender();
        var ctx = CreateSendContext(partnerCode: "PARTNER_FTP_BOM", connectionType: "FTP");
        ctx = ctx with { Address = host, Port = port };

        var jsonPayload = "[{\"ID\": 1, \"Name\": \"Thiết bị kiểm thử\"}]";
        var formatCtx = new FormatContext(
            ctx.PartnerCode,
            ctx.PacketCode,
            ctx.SerialNbr,
            ctx.ExportedAt,
            new Dictionary<string, string> { [ConnectKeys.CsvIncludeBom] = "true" });

        var transport = new CsvFormat().ToTransport(jsonPayload, formatCtx);

        var request = new DeliveryRequest
        {
            JsonPayload = jsonPayload,
            PayloadBytes = transport.Bytes,
            ContentType = transport.ContentType,
            FileExtension = transport.FileExtension,
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.Username] = user,
                [ConnectKeys.Password] = pass,
                [ConnectKeys.RemotePath] = remotePath,
                [ConnectKeys.CsvIncludeBom] = "true"
            }
        };

        // Act
        var result = await ftpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Gửi tới FTP thất bại (Error: {result.ErrorCode} - {result.ErrorMessage}). Hãy đảm bảo Docker service tac-ftp đang chạy bằng lệnh: docker compose -f Docker/Compose/docker-compose.yml up -d tac-ftp");
        Assert.NotNull(result.TargetAddressOrPath);

        var downloadedBytes = DownloadFtpFile(result.TargetAddressOrPath, user, pass);
        Assert.True(downloadedBytes.Length >= 3, "Tệp tải về quá ngắn để chứa UTF-8 BOM");
        Assert.Equal(0xEF, downloadedBytes[0]);
        Assert.Equal(0xBB, downloadedBytes[1]);
        Assert.Equal(0xBF, downloadedBytes[2]);
    }

    #endregion

    #region Http

    [Fact]
    public async Task DispatchAsync_WhenPartnerConnectionTypeIsApi_RoutesToApiChannel_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var dispatcher = CreateDispatcher(http: httpSender);
        var ctx = CreateDummyContext("101", "PARTNER_API", ConnectionType.Api);
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = 18092;
        ctx.Partner!.EndPointApiUrl = "/api/v1/data";

        var jsonPayload = "{\"data\": \"ok\"}";
        var request = new DeliveryRequest
        {
            JsonPayload = jsonPayload,
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["HttpTimeoutSec"] = "10"
            }
        };

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("API", result.Channel);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(Encoding.UTF8.GetByteCount(jsonPayload), result.SentByteSize);
    }

    /// <summary>
    /// Description: Kiểm thử Dispatcher lấy Address/Port từ cột đối tác và fallback hoặc nạp cấu hình bổ sung từ ConnectionConfig.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData(null, "/api/v1/legacy-data")]
    [InlineData("{\"TimeoutSeconds\":\"30\",\"AuthType\":\"Bearer\",\"Token\":\"test-token\"}", "/api/v1/override-endpoint")]
    public async Task DispatchAsync_WhenPartnerConfigProvidedOrNull_TakesAddressAndPortFromPartnerColumns_Test(string? connectionConfig, string endpointUrl)
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var dispatcher = CreateDispatcher(http: httpSender);
        var ctx = CreateDummyContext("101", "PARTNER_COLS", ConnectionType.Api);
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = 18092;
        ctx.Partner!.EndPointApiUrl = endpointUrl;
        ctx.Partner!.ConnectionConfig = connectionConfig;

        var mappingBytes = Encoding.UTF8.GetBytes("{\"test\": true}");
        var mapping = new DataMappingResult(true, mappingBytes, 1);
        var request = DataOutboundTransport.BuildRequest(mapping, ctx);

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("API", result.Channel);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(server.LastRequest);
        Assert.Equal(18092, server.LastRequest!.Port);
        Assert.Equal(endpointUrl, server.LastRequest.Path);
    }

    [Theory]
    [InlineData(70000)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HttpSender_WhenPortInvalid_ReturnsInvalidConfigurationError_Test(int invalidPort)
    {
        // Arrange
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BAD_PORT", "API");
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = invalidPort;
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>()
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains("Port", result.ErrorMessage);
    }

    /// <summary>
    /// Description: Kiểm thử HttpDeliverySender từ chối cấu hình TimeoutSeconds hoặc BypassSsl không hợp lệ.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("TimeoutSeconds", "-5", "Timeout")]
    [InlineData("TimeoutSeconds", "0", "Timeout")]
    [InlineData("TimeoutSeconds", "not_a_number", "Timeout")]
    [InlineData("BypassSsl", "not_a_boolean", "BypassSsl")]
    public async Task HttpSender_WhenConfigValueInvalid_ReturnsInvalidConfigurationError_Test(string configKey, string invalidValue, string expectedErrorKeyword)
    {
        // Arrange
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BAD_CONFIG", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                [configKey] = invalidValue
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains(expectedErrorKeyword, result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenBearerAuthProvided_SendsAuthorizationBearerHeader_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BEARER", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["AuthType"] = "Bearer",
                ["Token"] = "token_jwt_secret_xyz"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(server.LastRequest);
        Assert.True(server.LastRequest!.Headers.ContainsKey("Authorization"));
        Assert.Equal("Bearer token_jwt_secret_xyz", server.LastRequest.Headers["Authorization"]);
    }

    [Fact]
    public async Task HttpSender_WhenApiKeyAuthProvided_SendsApiKeyHeader_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_APIKEY", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["AuthType"] = "ApiKey",
                ["ApiKey"] = "demo-api-key-999",
                ["ApiKeyHeader"] = "X-Integration-Key"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(server.LastRequest);
        Assert.True(server.LastRequest!.Headers.ContainsKey("X-Integration-Key"));
        Assert.Equal("demo-api-key-999", server.LastRequest.Headers["X-Integration-Key"]);
    }

    [Fact]
    public async Task HttpSender_WhenBasicAuthProvided_SendsAuthorizationBasicHeader_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BASIC", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["AuthType"] = "Basic",
                ["Username"] = "itsadmin",
                ["Password"] = "P@ssw0rd!"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(server.LastRequest);
        Assert.True(server.LastRequest!.Headers.ContainsKey("Authorization"));
        var expectedBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("itsadmin:P@ssw0rd!"));
        Assert.Equal("Basic " + expectedBase64, server.LastRequest.Headers["Authorization"]);
    }

    [Fact]
    public async Task HttpSender_WhenHttp200ReturnsBusinessError_ReturnsFailedWithBusinessError_Test()
    {
        // Arrange: Đối tác trả HTTP 200 nhưng body chứa lỗi nghiệp vụ (isSuccess = false)
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = "{\"isSuccess\": false, \"errorCode\": \"ERR_PAYLOAD_INVALID\", \"message\": \"Payload schema mismatch\"}";

        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BIZ_ERR", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["SuccessField"] = "isSuccess",
                ["ErrorCodeField"] = "errorCode",
                ["ErrorMessageField"] = "message"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("BusinessError", result.ErrorCode);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("Payload schema mismatch", result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenServerDropsConnection_ReturnsProtocolMismatchHint_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.AbortOnCall.Add(1);

        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_ABORT", "API");
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = ShareDataPartnerServerMock.DefaultPort;
        ctx.Partner!.EndPointApiUrl = "/api/v1/data";

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = ShareDataPartnerServerMock.DefaultPort.ToString(),
                ["HttpTimeoutSec"] = "5"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("đóng kết nối", result.ErrorMessage);
        Assert.Contains("cổng", result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenPayloadBytesProvided_SendsExactBytesAndContentType_Test()
    {
        var server = _host.PartnerServer;
        server.ResetDefaults();

        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BYTES", "API");
        var expectedBytes = Encoding.UTF8.GetBytes("{\"cipher\":\"encrypted_payload_123\"}");
        var request = new DeliveryRequest
        {
            PayloadBytes = expectedBytes,
            ContentType = "application/json",
            JsonPayload = "dummy_should_not_be_used",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = ShareDataPartnerServerMock.DefaultHost,
                ["Port"] = ShareDataPartnerServerMock.DefaultPort.ToString()
            }
        };

        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(server.LastRequest);
        Assert.Equal(expectedBytes, Encoding.UTF8.GetBytes(server.LastRequest.Body));
    }

    /// <summary>
    /// Description: Kiểm thử HttpDeliverySender phát hiện và ghi cảnh báo khi cấu hình xác thực chưa đủ trường hoặc không hỗ trợ.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("", "stray-token", null, "stray-user", null, "Cấu hình phương thức xác thực rỗng hoặc None")]
    [InlineData("None", "stray-token", null, "stray-user", null, "Cấu hình phương thức xác thực rỗng hoặc None")]
    [InlineData("OAuth3Custom", "token-123", null, null, null, "AuthType [OAuth3Custom] không được hỗ trợ")]
    [InlineData("Bearer", "", null, null, null, "Bearer yêu cầu Token")]
    [InlineData("ApiKey", null, null, null, null, "ApiKey yêu cầu ApiKey")]
    [InlineData("Basic", null, null, "admin", null, "Basic yêu cầu Username và Password")]
    public async Task HttpSender_WhenAuthConfigIncompleteOrUnsupported_EmitsExpectedWarning_Test(
        string authType,
        string? token,
        string? apiKey,
        string? username,
        string? password,
        string expectedWarningSubstring)
    {
        // Arrange
        _host.PartnerServer.ResetDefaults();
        var handler = new HttpDeliverySender();
        var ctx = CreateSendContext();
        var config = new Dictionary<string, string> { [ConnectKeys.AuthType] = authType };
        if (token != null) config[ConnectKeys.Token] = token;
        if (apiKey != null) config[ConnectKeys.ApiKey] = apiKey;
        if (username != null) config[ConnectKeys.Username] = username;
        if (password != null) config[ConnectKeys.Password] = password;

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = config
        };

        // Act
        var result = await handler.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains(expectedWarningSubstring));
        if (authType == "" || authType == "None")
        {
            Assert.False(_host.PartnerServer.LastRequest!.Headers.ContainsKey("Authorization"));
        }
    }

    /// <summary>
    /// Description: Kiểm thử HttpDeliverySender xử lý ResponseContract (Auto, HttpOnly, Furion, Default) theo body phản hồi của đối tác.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("HttpOnly", "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}", true, null, null)]
    [InlineData("Auto", "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}", false, "BusinessError", "lz.exception.base.dataNotExist")]
    [InlineData(null, "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}", false, "BusinessError", "lz.exception.base.dataNotExist")]
    [InlineData(null, "{\"succeeded\":true}", true, null, null)]
    [InlineData("Furion", "{\"succeeded\": false, \"statusCode\": 400, \"errors\": \"Dữ liệu không đúng định dạng\"}", false, "BusinessError", "Dữ liệu không đúng định dạng")]
    public async Task HttpSender_WhenResponseContractConfigured_ProcessesResultAsExpected_Test(
        string? contract,
        string responseBody,
        bool expectedSuccess,
        string? expectedErrorCode,
        string? expectedMessageSubstring)
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = responseBody;

        var httpSender = new HttpDeliverySender();
        var ctx = CreateSendContext("PARTNER_CONTRACT_TEST");
        var config = new Dictionary<string, string>();
        if (contract != null)
        {
            config[ConnectKeys.ResponseContract] = contract;
        }

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = config
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        Assert.Equal(200, result.StatusCode);
        if (expectedErrorCode != null)
        {
            Assert.Equal(expectedErrorCode, result.ErrorCode);
        }
        if (expectedMessageSubstring != null)
        {
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains(expectedMessageSubstring, result.ErrorMessage);
        }
    }

    #endregion

    #region Kafka

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
        var dispatcher = CreateDispatcher(kafka: kafkaSender);

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

    /// <summary>
    /// Description: Kiểm thử KafkaDeliverySender từ chối gửi khi kênh bị Disabled hoặc thiếu BootstrapServers.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("false", "some-topic", true, "Disabled")]
    [InlineData("true", "my-topic", false, "MissingBootstrapServers")]
    public async Task KafkaSender_WhenDisabledOrMissingServers_ReturnsExpectedError_Test(
        string enabled,
        string topic,
        bool hasPartnerAddress,
        string expectedErrorCode)
    {
        // Arrange
        var kafkaSender = new KafkaDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_KAFKA", ConnectionType.Kafka);
        if (!hasPartnerAddress && ctx.Partner != null)
        {
            ctx.Partner.Address = null;
        }

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Kafka,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Enabled"] = enabled,
                ["Topic"] = topic
            }
        };

        // Act
        var result = await kafkaSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Kafka", result.Channel);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
    }

    #endregion
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
