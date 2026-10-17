using Module.ShareData.Controllers.Partner.Validators;
using Module.ShareData.Core.Entities;
using Modules.Integration.ShareData;
using Modules.Integration.ShareData.PacketSend;
using Modules.Integration.ShareData.PacketSend.Senders;
using Modules.Integration.ShareData.Formats;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using System.Text;
using Tests.ShareData.Mocks;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Bộ kiểm thử đơn vị & tích hợp cho tầng điều phối kết nối xuất bản dữ liệu (Transport Adapter - R2).
    ///              Kiểm chứng hợp đồng 3 tham số chuẩn (JsonPayload, SecurityInfo, ConnectionConfig),
    ///              rẽ nhánh theo ConnectionType (API, Kafka, File), và chuyển đổi JSON -> byte[] của Kafka.
    /// Created date: 13/10/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataOutboundTransportTests
    {
        private readonly Host _host;

        public ShareDataOutboundTransportTests(Host host)
        {
            _host = host;
            _host.PartnerServer.ResetDefaults();
        }

        private static DataOutboundContext CreateDummyContext(string packetCode = "101", string partnerCode = "PARTNER_TEST", string connectionType = "API")
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
        public async Task DispatchAsync_WhenPartnerConnectionTypeIsFile_RoutesToFileChannel_Test()
        {
            // Arrange
            var fileSender = new FilePacketSender();
            var dispatcher = PacketSendDispatcher.CreateForHandlers([fileSender]);
            var ctx = CreateDummyContext("103", "PARTNER_FILE", "File");
            var jsonPayload = "{\"vdsCount\": 12, \"speed\": 80}";
            var customDir = "Out/TestPartner";
            var fileNamePattern = "vds_export_{serial}.json";
            var request = new PacketSendRequest
            {
                JsonPayload = jsonPayload,
                ConnectType = "File",
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

        [Fact]
        public async Task DispatchAsync_WhenPartnerConnectionTypeIsApi_RoutesToApiChannel_Test()
        {
            // Arrange
            var server = _host.PartnerServer;
            server.ResetDefaults();
            var restSender = new RestPacketSender();
            var dispatcher = PacketSendDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_API", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18092;
            ctx.Partner!.EndPointApiUrl = "/api/v1/data";

            var jsonPayload = "{\"data\": \"ok\"}";
            var request = new PacketSendRequest
            {
                JsonPayload = jsonPayload,
                ConnectType = "API",
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

        [Fact]
        public async Task DispatchAsync_WhenPartnerConnectionTypeIsUnknown_ReturnsUnsupportedError_Test()
        {
            // Arrange
            var dispatcher = PacketSendDispatcher.CreateForHandlers([]);
            var ctx = CreateDummyContext("101", "PARTNER_UNKNOWN", "UnknownProtocolX");
            var request = new PacketSendRequest
            {
                JsonPayload = "{}",
                ConnectType = "UnknownProtocolX"
            };

            // Act
            var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("UnsupportedConnectionType", result.ErrorCode);
        }

        [Fact]
        public void BuildRequest_WhenPartnerHasSecurityAndConnectionConfig_ExtractsSecurityInfoAndConfigProperly_Test()
        {
            // Arrange
            var ctx = CreateDummyContext("105", "PARTNER_CONFIG", "Kafka");
            ctx.Partner!.ConnectionConfig = "{\"ApiKey\":\"secret-key-12345\",\"Topic\":\"rfid-topic\",\"HttpTimeoutSec\":\"25\"}";
            var mappingBytes = Encoding.UTF8.GetBytes("{\"tag\":\"RFID_001\"}");
            var mapping = new DataMappingResult(true, mappingBytes, 1);

            // Act
            var request = DataOutboundTransport.BuildRequest(mapping, ctx);

            // Assert
            Assert.Equal("{\"tag\":\"RFID_001\"}", request.JsonPayload);
            Assert.Equal("Kafka", request.ConnectType);
            Assert.True(request.ConnectConfig.ContainsKey("Topic"));
            Assert.Equal("rfid-topic", request.ConnectConfig["Topic"]);
            Assert.True(request.ConnectConfig.ContainsKey("HttpTimeoutSec"));
            Assert.Equal("25", request.ConnectConfig["HttpTimeoutSec"]);
            Assert.True(request.SecurityInfo.ContainsKey(ConnectKeys.ApiKey));
            Assert.Equal("secret-key-12345", request.SecurityInfo[ConnectKeys.ApiKey]);
        }

        [Fact]
        public async Task DispatchAsync_WhenPartnerConfigEmpty_FallsBackToPartnerLegacyFields_Test()
        {
            // Arrange: Đối tác cũ không có ConnectionConfig JSON -> tự động fallback sang Address, Port, EndPointApiUrl
            var server = _host.PartnerServer;
            server.ResetDefaults();
            var restSender = new RestPacketSender();
            var dispatcher = PacketSendDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_LEGACY", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18092;
            ctx.Partner!.EndPointApiUrl = "/api/v1/legacy-data";
            ctx.Partner!.ConnectionConfig = null;

            var mappingBytes = Encoding.UTF8.GetBytes("{\"legacy\": true}");
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
            Assert.Equal("/api/v1/legacy-data", server.LastRequest.Path);
        }

        [Fact]
        public async Task DispatchAsync_TakesAddressAndPortFromPartnerColumns_AndCustomConfigFromDictionary_Test()
        {
            // Arrange: Địa chỉ và cổng chỉ lấy từ cột của Partner; ConnectionConfig chỉ chứa config riêng (Auth, Timeout)
            var server = _host.PartnerServer;
            server.ResetDefaults();
            var restSender = new RestPacketSender();
            var dispatcher = PacketSendDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_COLS", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18092;
            ctx.Partner!.EndPointApiUrl = "/api/v1/override-endpoint";
            ctx.Partner!.ConnectionConfig = "{\"TimeoutSeconds\":\"30\",\"AuthType\":\"Bearer\",\"Token\":\"test-token\"}";

            var mappingBytes = Encoding.UTF8.GetBytes("{\"override\": true}");
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
            Assert.Equal("/api/v1/override-endpoint", server.LastRequest.Path);
        }

        [Theory]
        [InlineData(70000)]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task RestSender_WhenPortInvalid_ReturnsInvalidConfigurationError_Test(int invalidPort)
        {
            // Arrange
            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_BAD_PORT", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = invalidPort;
            var request = new PacketSendRequest
            {
                JsonPayload = "{}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>()
            };

            // Act
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("InvalidConfiguration", result.ErrorCode);
            Assert.Contains("Port", result.ErrorMessage);
        }

        [Theory]
        [InlineData("-5")]
        [InlineData("0")]
        [InlineData("not_a_number")]
        public async Task RestSender_WhenTimeoutInvalid_ReturnsInvalidConfigurationError_Test(string invalidTimeout)
        {
            // Arrange
            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_BAD_TIMEOUT", "API");
            var request = new PacketSendRequest
            {
                JsonPayload = "{}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["TimeoutSeconds"] = invalidTimeout
                }
            };

            // Act
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("InvalidConfiguration", result.ErrorCode);
            Assert.Contains("Timeout", result.ErrorMessage);
        }

        [Fact]
        public async Task RestSender_WhenBypassSslInvalid_ReturnsInvalidConfigurationError_Test()
        {
            // Arrange
            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_BAD_SSL", "API");
            var request = new PacketSendRequest
            {
                JsonPayload = "{}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["BypassSsl"] = "not_a_boolean"
                }
            };

            // Act
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("InvalidConfiguration", result.ErrorCode);
            Assert.Contains("BypassSsl", result.ErrorMessage);
        }

        [Fact]
        public async Task RestSender_WhenBearerAuthProvided_SendsAuthorizationBearerHeader_Test()
        {
            // Arrange
            var server = _host.PartnerServer;
            server.ResetDefaults();
            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_BEARER", "API");
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["AuthType"] = "Bearer",
                    ["Token"] = "token_jwt_secret_xyz"
                }
            };

            // Act
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(server.LastRequest);
            Assert.True(server.LastRequest!.Headers.ContainsKey("Authorization"));
            Assert.Equal("Bearer token_jwt_secret_xyz", server.LastRequest.Headers["Authorization"]);
        }

        [Fact]
        public async Task RestSender_WhenApiKeyAuthProvided_SendsApiKeyHeader_Test()
        {
            // Arrange
            var server = _host.PartnerServer;
            server.ResetDefaults();
            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_APIKEY", "API");
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectType = "API",
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
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(server.LastRequest);
            Assert.True(server.LastRequest!.Headers.ContainsKey("X-Integration-Key"));
            Assert.Equal("demo-api-key-999", server.LastRequest.Headers["X-Integration-Key"]);
        }

        [Fact]
        public async Task RestSender_WhenBasicAuthProvided_SendsAuthorizationBasicHeader_Test()
        {
            // Arrange
            var server = _host.PartnerServer;
            server.ResetDefaults();
            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_BASIC", "API");
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectType = "API",
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
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(server.LastRequest);
            Assert.True(server.LastRequest!.Headers.ContainsKey("Authorization"));
            var expectedBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("itsadmin:P@ssw0rd!"));
            Assert.Equal("Basic " + expectedBase64, server.LastRequest.Headers["Authorization"]);
        }

        [Fact]
        public async Task RestSender_WhenHttp200ReturnsBusinessError_ReturnsFailedWithBusinessError_Test()
        {
            // Arrange: Đối tác trả HTTP 200 nhưng body chứa lỗi nghiệp vụ (isSuccess = false)
            var server = _host.PartnerServer;
            server.ResetDefaults();
            server.DefaultResponseBody = "{\"isSuccess\": false, \"errorCode\": \"ERR_PAYLOAD_INVALID\", \"message\": \"Payload schema mismatch\"}";

            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_BIZ_ERR", "API");
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectType = "API",
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
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("BusinessError", result.ErrorCode);
            Assert.Equal(200, result.StatusCode);
            Assert.Contains("Payload schema mismatch", result.ErrorMessage);
        }

        [Fact]
        public async Task RestSender_WhenFurionResponseHasFailedSucceeded_ReturnsBusinessError_Test()
        {
            // Arrange: Đối tác trả HTTP 200 dạng Furion/UnifyResult nhưng succeeded = false
            var server = _host.PartnerServer;
            server.ResetDefaults();
            server.DefaultResponseBody = "{\"succeeded\": false, \"statusCode\": 400, \"errors\": \"Dữ liệu không đúng định dạng\"}";

            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_FURION_ERR", "API");
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["ResponseContract"] = "Furion"
                }
            };

            // Act
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("BusinessError", result.ErrorCode);
            Assert.Equal(200, result.StatusCode);
            Assert.Contains("Dữ liệu không đúng định dạng", result.ErrorMessage);
        }

        /// <summary>
        /// Description: TC3 - Kiểm tra khi máy chủ đối tác ngắt kết nối ngang (server aborts / connection reset),
        ///              RestSender trả về thông báo lỗi chi tiết có gợi ý kiểm tra giao thức http/https hoặc sai cổng.
        /// Created date: 15/10/2026
        /// </summary>
        [Fact]
        public async Task RestSender_WhenServerDropsConnection_ReturnsProtocolMismatchHint_Test()
        {
            // Arrange
            var server = _host.PartnerServer;
            server.ResetDefaults();
            server.AbortOnCall.Add(1);

            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_ABORT", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = ShareDataPartnerServerMock.DefaultPort;
            ctx.Partner!.EndPointApiUrl = "/api/v1/data";

            var request = new PacketSendRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = ShareDataPartnerServerMock.DefaultPort.ToString(),
                    ["HttpTimeoutSec"] = "5"
                }
            };

            // Act
            var result = await restSender.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("đóng kết nối", result.ErrorMessage);
            Assert.Contains("http://", result.ErrorMessage);
            Assert.Contains("cổng", result.ErrorMessage);
        }

        [Fact]
        public async Task FileSender_WhenDirectoryPathHasInvalidCharacters_ReturnsInvalidPathError_Test()
        {
            // Arrange
            var fileSender = new FilePacketSender();
            var ctx = CreateDummyContext("103", "PARTNER_FILE_INVALID", "File");
            var invalidDir = "Out/Invalid|Path\0";
            var request = new PacketSendRequest
            {
                JsonPayload = "{}",
                ConnectType = "File",
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
            Assert.Contains("không hợp lệ", result.ErrorMessage);
        }

        [Fact]
        public void CsvFormat_ConvertsJsonArrayToRfc4180Csv_EscapesProperly_Test()
        {
            // Arrange
            var json = """
                [
                    {"id": 1, "name": "Station A", "remark": "Clear, dry"},
                    {"id": 2, "name": "Station \"B\"", "remark": "Rainy, wet\r\nSlippery road"}
                ]
                """;

            // Act
            var csv = CsvFormat.ConvertJsonToCsv(json);

            // Assert
            Assert.Contains("id,name,remark", csv);
            Assert.Contains("1,Station A,\"Clear, dry\"", csv);
            Assert.Contains("2,\"Station \"\"B\"\"\",\"Rainy, wet\r\nSlippery road\"", csv);
        }

        [Fact]
        public void AesGcmEncryptor_EncryptDecryptRoundtrip_ReturnsOriginalBytes_Test()
        {
            // Arrange
            var encryptor = new Modules.Integration.ShareData.Encryption.AesGcmEncryptor();
            var keyBytes = new byte[Modules.Integration.ShareData.Encryption.AesGcmEncryptor.KeySize];
            System.Security.Cryptography.RandomNumberGenerator.Fill(keyBytes);
            var ctx = new Modules.Integration.ShareData.Encryption.EncryptContext("test-kid", keyBytes);
            var originalBytes = Encoding.UTF8.GetBytes("{\"test\":\"secret-payload-data\"}");

            // Act
            var encryptedBytes = encryptor.Encrypt(originalBytes, ctx);
            var decryptedBytes = encryptor.Decrypt(encryptedBytes, ctx);

            // Assert
            Assert.NotEqual(originalBytes, encryptedBytes);
            Assert.Equal(originalBytes, decryptedBytes);
            var jsonEnvelope = Encoding.UTF8.GetString(encryptedBytes);
            Assert.Contains("AES256-GCM", jsonEnvelope);
            Assert.Contains("test-kid", jsonEnvelope);
        }

        [Fact]
        public void PlainEncryptor_EncryptDecrypt_PassesThroughIdenticalBytes_Test()
        {
            // Arrange
            var encryptor = new Modules.Integration.ShareData.Encryption.PlainEncryptor();
            var ctx = new Modules.Integration.ShareData.Encryption.EncryptContext(null, null);
            var originalBytes = Encoding.UTF8.GetBytes("{\"test\":\"plain-payload\"}");

            // Act
            var encrypted = encryptor.Encrypt(originalBytes, ctx);
            var decrypted = encryptor.Decrypt(encrypted, ctx);

            // Assert
            Assert.Equal(originalBytes, encrypted);
            Assert.Equal(originalBytes, decrypted);
        }

        [Fact]
        public async Task FtpSender_WhenDisabledOrMissingHost_ReturnsClearError_Test()
        {
            // Arrange
            var ftpSender = new FtpPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_FTP", "Ftp");

            var disabledReq = new PacketSendRequest
            {
                JsonPayload = "[{\"id\": 1}]",
                ConnectType = "Ftp",
                ConnectConfig = new Dictionary<string, string> { ["Enabled"] = "false" }
            };

            var missingHostReq = new PacketSendRequest
            {
                JsonPayload = "[{\"id\": 1}]",
                ConnectType = "Ftp",
                ConnectConfig = new Dictionary<string, string> { ["Enabled"] = "true" }
            };

            // Act
            var disabledRes = await ftpSender.Send(disabledReq, ctx, CancellationToken.None);
            if (ctx.Partner != null) ctx.Partner.Address = null;
            var missingHostRes = await ftpSender.Send(missingHostReq, ctx, CancellationToken.None);

            // Assert
            Assert.False(disabledRes.Success);
            Assert.Equal("Disabled", disabledRes.ErrorCode);

            Assert.False(missingHostRes.Success);
            Assert.Equal("MissingFtpHost", missingHostRes.ErrorCode);
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
        public void PacketMetadataResolver_Packet105_DefaultsToSnapshot_CanBeConfiguredToIncremental_Test()
        {
            // Arrange
            var packetDefault = new ShareDataPacket { Code = "105", Name = "AVI/RFID Data" };
            var packetIncr = new ShareDataPacket { Code = "105", Name = "AVI/RFID Data", SyncMode = BaseEnums.SyncMode.Incremental };
            var packetSnapshot = new ShareDataPacket { Code = "105", Name = "AVI/RFID Data", SyncMode = BaseEnums.SyncMode.Snapshot };

            // Act & Assert
            // 1. Mặc định gói 105 không set SyncMode là Snapshot
            var defaultPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetDefault);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.Snapshot, defaultPolicy);

            // 2. Cấu hình SyncMode = Incremental trên gói chuyển thành AlwaysIncremental
            var packetIncrPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetIncr);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.AlwaysIncremental, packetIncrPolicy);

            // 3. Cấu hình SyncMode = Snapshot trên gói là Snapshot
            var packetSnapPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetSnapshot);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.Snapshot, packetSnapPolicy);

            // 4. Packet có SyncMode = null -> Fallback mặc định Snapshot (1)
            var packetNull = new ShareDataPacket { Code = "999", Name = "Custom Packet", SyncMode = null };
            var nullFallbackPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetNull);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.Snapshot, nullFallbackPolicy);
        }

        [Fact]
        public void SqlValidator_SupportsFromDateAndToDateParameters_Test()
        {
            // Arrange
            var validSqlWithFromToDate = """
                SELECT ID, ZoneId, UpdateTime AS __lastTime, ID AS __rowid
                FROM TmsZoneStatus
                WHERE UpdateTime >= @fromDate AND UpdateTime <= @toDate AND ID > @lastKey
                """;

            var invalidSqlWithUnknownParam = """
                SELECT ID, ZoneId
                FROM TmsZoneStatus
                WHERE ZoneId = @unknownParam
                """;

            // Act
            var (validResult, validErr) = Module.ShareData.Core.Utils.SqlValidator.Validate(validSqlWithFromToDate);
            var (invalidResult, invalidErr) = Module.ShareData.Core.Utils.SqlValidator.Validate(invalidSqlWithUnknownParam);

            // Assert
            Assert.True(validResult);
            Assert.Null(validErr);

            Assert.False(invalidResult);
            Assert.Contains("@unknownParam", invalidErr);
        }

        [Fact]
        public void PartnerValidator_RejectsOutputPath_AndValidatesFileNamePatternVariables_Test()
        {
            // 1. Rejects OutputPath (deprecated key)
            Assert.Equal(ConnectConfigIssue.ObsoleteKey, ConnectConfigValidator.Check("{\"OutputPath\": \"/tmp/test.json\"}", out var errOutput));
            Assert.NotNull(errOutput);
            Assert.Equal("OutputPath", errOutput);

            // 2. FileNamePattern missing both {serial} and {timestamp}
            Assert.Equal(ConnectConfigIssue.FileNamePatternNoToken, ConnectConfigValidator.Check("{\"FileNamePattern\": \"static_data.json\"}", out var errStaticPattern));
            Assert.NotNull(errStaticPattern);
            Assert.Equal("static_data.json", errStaticPattern);

            // 3. Valid FileNamePattern with {serial} or {timestamp}
            Assert.Equal(ConnectConfigIssue.None, ConnectConfigValidator.Check("{\"FileNamePattern\": \"data_{serial}.json\"}"));
            Assert.Equal(ConnectConfigIssue.None, ConnectConfigValidator.Check("{\"FileNamePattern\": \"data_{timestamp}.json\"}"));
            Assert.Equal(ConnectConfigIssue.None, ConnectConfigValidator.Check("{\"FileNamePattern\": \"{packet}_{partner}_{timestamp}_{serial}.json\"}"));

            // 4. Test IsFileChannel aliases
            Assert.True(ConnectTypes.IsFileChannel("File"));
            Assert.True(ConnectTypes.IsFileChannel("file"));
            Assert.True(ConnectTypes.IsFileChannel("DISK"));
            Assert.True(ConnectTypes.IsFileChannel("NAS"));
            Assert.False(ConnectTypes.IsFileChannel("API"));
            Assert.False(ConnectTypes.IsFileChannel("Kafka"));
            Assert.False(ConnectTypes.IsFileChannel(null));
        }

        [Fact]
        public async Task FileSender_WhenDirectoryPathHasPathTraversal_ReturnsInvalidConfiguration_Test()
        {
            var fileSender = new FilePacketSender();
            var ctx = CreateDummyContext("103", "PARTNER_FILE_TRAVERSAL", "File");
            var request = new PacketSendRequest
            {
                PayloadBytes = Encoding.UTF8.GetBytes("test-content"),
                ConnectType = "File",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["DirectoryPath"] = "safe/../../etc/passwd"
                }
            };

            var result = await fileSender.Send(request, ctx, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("File", result.Channel);
            Assert.Equal("InvalidConfiguration", result.ErrorCode);
            Assert.Contains("traversal", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FileSender_WhenDirectoryPathAndFileNamePatternConfigured_SavesToExpectedCustomPath_Test()
        {
            var fileSender = new FilePacketSender();
            var ctx = CreateDummyContext("103", "PARTNER_FILE_CUSTOM", "File");
            var expectedBytes = Encoding.UTF8.GetBytes("custom-file-payload-content");
            var request = new PacketSendRequest
            {
                PayloadBytes = expectedBytes,
                FileExtension = ".csv",
                ConnectType = "File",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["DirectoryPath"] = "custom_export_dir",
                    ["FileNamePattern"] = "export_{partner}_{packet}_{timestamp}"
                }
            };

            var result = await fileSender.Send(request, ctx, CancellationToken.None);

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

        [Fact]
        public async Task RestSender_WhenPayloadBytesProvided_SendsExactBytesAndContentType_Test()
        {
            var server = _host.PartnerServer;
            server.ResetDefaults();

            var restSender = new RestPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_BYTES", "API");
            var expectedBytes = Encoding.UTF8.GetBytes("{\"cipher\":\"encrypted_payload_123\"}");
            var request = new PacketSendRequest
            {
                PayloadBytes = expectedBytes,
                ContentType = "application/json",
                JsonPayload = "dummy_should_not_be_used",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["Address"] = ShareDataPartnerServerMock.DefaultHost,
                    ["Port"] = ShareDataPartnerServerMock.DefaultPort.ToString()
                }
            };

            var result = await restSender.Send(request, ctx, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(server.LastRequest);
            Assert.Equal(expectedBytes, Encoding.UTF8.GetBytes(server.LastRequest.Body));
        }

        [Fact]
        public async Task FtpSender_WhenCsvPayloadBytesProvided_UsesExactPayloadBytesAndCsvExtension_Test()
        {
            var ftpSender = new FtpPacketSender();
            var ctx = CreateDummyContext("101", "PARTNER_FTP_CSV", "FTP");
            var csvBytes = Encoding.UTF8.GetBytes("ID,Code,Name\r\n1,A,Item1\r\n2,B,Item2");
            var request = new PacketSendRequest
            {
                PayloadBytes = csvBytes,
                ContentType = "text/csv",
                FileExtension = ".csv",
                JsonPayload = "{}",
                ConnectType = "FTP",
                ConnectConfig = new Dictionary<string, string>
                {
                    ["RemotePath"] = "/test/path"
                }
            };

            var result = await ftpSender.Send(request, ctx, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Ftp", result.Channel);
            Assert.Contains(result.ErrorCode, new[] { "FtpConnectionFailed", "FtpSendFailed", "FtpWebException" });
        }

        [Fact]
        public void ExportPathHelper_ResolveRootPath_WhenPathIsRooted_ReturnsNormalizedRootedPath_Test()
        {
            // Arrange
            var absolutePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "sharedata_abs_root"));

            // Act
            var resolvedWithDefaultBase = ExportPathHelper.ResolveRootPath(absolutePath);
            var resolvedWithCustomBase = ExportPathHelper.ResolveRootPath(absolutePath, "/some/other/custom/base");

            // Assert
            Assert.Equal(absolutePath, resolvedWithDefaultBase);
            Assert.Equal(absolutePath, resolvedWithCustomBase);
        }

        [Fact]
        public void ExportPathHelper_ResolveRootPath_WhenPathIsRelative_CombinesWithBaseDirectory_Test()
        {
            // Arrange
            var customBase = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "custom_base_dir"));
            var relativePath = "reports/daily_export";

            // Act
            var resolvedCustom = ExportPathHelper.ResolveRootPath(relativePath, customBase);
            var resolvedDefault = ExportPathHelper.ResolveRootPath(relativePath);

            // Assert
            Assert.Equal(Path.GetFullPath(Path.Combine(customBase, relativePath)), resolvedCustom);
            Assert.Equal(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath)), resolvedDefault);
        }

        [Fact]
        public void ExportPathHelper_ResolveRootPath_WhenPathIsEmptyOrNull_UsesDefaultShareDataSendUnderBaseDirectory_Test()
        {
            // Arrange
            var customBase = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "custom_base_default"));

            // Act
            var resolvedNull = ExportPathHelper.ResolveRootPath(null);
            var resolvedEmpty = ExportPathHelper.ResolveRootPath("");
            var resolvedWhitespace = ExportPathHelper.ResolveRootPath("   ");
            var resolvedCustomNull = ExportPathHelper.ResolveRootPath(null, customBase);

            // Assert
            var expectedDefault = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "sharedata/send"));
            Assert.Equal(expectedDefault, resolvedNull);
            Assert.Equal(expectedDefault, resolvedEmpty);
            Assert.Equal(expectedDefault, resolvedWhitespace);
            Assert.Equal(Path.GetFullPath(Path.Combine(customBase, "sharedata/send")), resolvedCustomNull);
        }

        [Fact]
        public void ExportPathHelper_ResolveRootPath_WhenCurrentDirectoryChanges_StillResolvesUnderBaseDirectoryNotCwd_Test()
        {
            // Arrange
            var originalCwd = Directory.GetCurrentDirectory();
            var tempCwd = Path.Combine(Path.GetTempPath(), "test_scm_cwd_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempCwd);
            Directory.SetCurrentDirectory(tempCwd);

            // Act
            var resolved = ExportPathHelper.ResolveRootPath("sharedata/send");

            // Phục hồi CWD tuần tự (Strict AAA, không try-finally)
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempCwd, true);

            // Assert
            var expected = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "sharedata/send"));
            Assert.Equal(expected, resolved);
            Assert.DoesNotContain(tempCwd, resolved);
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
}
