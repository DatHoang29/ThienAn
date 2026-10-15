using Module.ShareData.Controllers.Partner.Validators;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Core.Models.DataOutbound;
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
            var kafkaSender = new DataOutboundKafkaSender();
            var dispatcher = ConnectionDispatcher.CreateForHandlers([kafkaSender]);
            var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");
            var jsonPayload = "{\"temperature\": 28.5, \"humidity\": 65}";
            var request = new ConnectionRequest
            {
                JsonPayload = jsonPayload,
                ConnectionType = "Kafka",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Topic"] = "traffic-weather-topic",
                    ["BootstrapServers"] = "127.0.0.1:9092"
                }
            };

            // Act
            var result = await dispatcher.DispatchAsync(request, ctx, CancellationToken.None);

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
            var kafkaSender = new DataOutboundKafkaSender();
            var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");
            var request = new ConnectionRequest
            {
                JsonPayload = string.Empty,
                ConnectionType = "Kafka"
            };

            // Act
            var result = await kafkaSender.SendAsync(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Kafka", result.Channel);
            Assert.Equal("EmptyPayload", result.ErrorCode);
        }

        [Fact]
        public async Task DispatchAsync_WhenPartnerConnectionTypeIsFile_RoutesToFileChannel_Test()
        {
            // Arrange
            var fileSender = new DataOutboundFileSender();
            var dispatcher = ConnectionDispatcher.CreateForHandlers([fileSender]);
            var ctx = CreateDummyContext("103", "PARTNER_FILE", "File");
            var jsonPayload = "{\"vdsCount\": 12, \"speed\": 80}";
            var customDir = "Out/TestPartner";
            var fileNamePattern = "vds_export_{serial}.json";
            var request = new ConnectionRequest
            {
                JsonPayload = jsonPayload,
                ConnectionType = "File",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["DirectoryPath"] = customDir,
                    ["FileNamePattern"] = fileNamePattern
                }
            };

            // Act
            var result = await dispatcher.DispatchAsync(request, ctx, CancellationToken.None);

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
            var restSender = new DataOutboundRestSender();
            var dispatcher = ConnectionDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_API", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18092;
            ctx.Partner!.EndPointApiUrl = "/api/v1/data";

            var jsonPayload = "{\"data\": \"ok\"}";
            var request = new ConnectionRequest
            {
                JsonPayload = jsonPayload,
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["HttpTimeoutSec"] = "10"
                }
            };

            // Act
            var result = await dispatcher.DispatchAsync(request, ctx, CancellationToken.None);

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
            var dispatcher = ConnectionDispatcher.CreateForHandlers([]);
            var ctx = CreateDummyContext("101", "PARTNER_UNKNOWN", "UnknownProtocolX");
            var request = new ConnectionRequest
            {
                JsonPayload = "{}",
                ConnectionType = "UnknownProtocolX"
            };

            // Act
            var result = await dispatcher.DispatchAsync(request, ctx, CancellationToken.None);

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
            var request = ConnectionDispatcher.BuildRequest(mapping, ctx);

            // Assert
            Assert.Equal("{\"tag\":\"RFID_001\"}", request.JsonPayload);
            Assert.Equal("Kafka", request.ConnectionType);
            Assert.True(request.ConnectionConfig.ContainsKey("Topic"));
            Assert.Equal("rfid-topic", request.ConnectionConfig["Topic"]);
            Assert.True(request.ConnectionConfig.ContainsKey("HttpTimeoutSec"));
            Assert.Equal("25", request.ConnectionConfig["HttpTimeoutSec"]);
            Assert.True(request.SecurityInfo.ContainsKey("ApiKey"));
            Assert.Equal("secret-key-12345", request.SecurityInfo["ApiKey"]);
        }

        [Fact]
        public async Task DispatchAsync_WhenPartnerConfigEmpty_FallsBackToPartnerLegacyFields_Test()
        {
            // Arrange: Đối tác cũ không có ConnectionConfig JSON -> tự động fallback sang Address, Port, EndPointApiUrl
            var server = _host.PartnerServer;
            server.ResetDefaults();
            var restSender = new DataOutboundRestSender();
            var dispatcher = ConnectionDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_LEGACY", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18092;
            ctx.Partner!.EndPointApiUrl = "/api/v1/legacy-data";
            ctx.Partner!.ConnectionConfig = null;

            var mappingBytes = Encoding.UTF8.GetBytes("{\"legacy\": true}");
            var mapping = new DataMappingResult(true, mappingBytes, 1);
            var request = ConnectionDispatcher.BuildRequest(mapping, ctx);

            // Act
            var result = await dispatcher.DispatchAsync(request, ctx, CancellationToken.None);

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
            var restSender = new DataOutboundRestSender();
            var dispatcher = ConnectionDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_COLS", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18092;
            ctx.Partner!.EndPointApiUrl = "/api/v1/override-endpoint";
            ctx.Partner!.ConnectionConfig = "{\"TimeoutSeconds\":\"30\",\"AuthType\":\"Bearer\",\"Token\":\"test-token\"}";

            var mappingBytes = Encoding.UTF8.GetBytes("{\"override\": true}");
            var mapping = new DataMappingResult(true, mappingBytes, 1);
            var request = ConnectionDispatcher.BuildRequest(mapping, ctx);

            // Act
            var result = await dispatcher.DispatchAsync(request, ctx, CancellationToken.None);

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
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BAD_PORT", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = invalidPort;
            var request = new ConnectionRequest
            {
                JsonPayload = "{}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>()
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

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
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BAD_TIMEOUT", "API");
            var request = new ConnectionRequest
            {
                JsonPayload = "{}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["TimeoutSeconds"] = invalidTimeout
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("InvalidConfiguration", result.ErrorCode);
            Assert.Contains("Timeout", result.ErrorMessage);
        }

        [Fact]
        public async Task RestSender_WhenBypassSslInvalid_ReturnsInvalidConfigurationError_Test()
        {
            // Arrange
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BAD_SSL", "API");
            var request = new ConnectionRequest
            {
                JsonPayload = "{}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["BypassSsl"] = "not_a_boolean"
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

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
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BEARER", "API");
            var request = new ConnectionRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["AuthType"] = "Bearer",
                    ["Token"] = "token_jwt_secret_xyz"
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

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
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_APIKEY", "API");
            var request = new ConnectionRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["AuthType"] = "ApiKey",
                    ["ApiKey"] = "demo-api-key-999",
                    ["ApiKeyHeader"] = "X-Integration-Key"
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

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
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BASIC", "API");
            var request = new ConnectionRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["AuthType"] = "Basic",
                    ["Username"] = "itsadmin",
                    ["Password"] = "P@ssw0rd!"
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

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

            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BIZ_ERR", "API");
            var request = new ConnectionRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["SuccessField"] = "isSuccess",
                    ["ErrorCodeField"] = "errorCode",
                    ["ErrorMessageField"] = "message"
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

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

            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_FURION_ERR", "API");
            var request = new ConnectionRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = "18092",
                    ["ResponseContract"] = "Furion"
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

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

            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_ABORT", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = ShareDataPartnerServerMock.DefaultPort;
            ctx.Partner!.EndPointApiUrl = "/api/v1/data";

            var request = new ConnectionRequest
            {
                JsonPayload = "{\"data\":\"test\"}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = ShareDataPartnerServerMock.DefaultPort.ToString(),
                    ["HttpTimeoutSec"] = "5"
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

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
            var fileSender = new DataOutboundFileSender();
            var ctx = CreateDummyContext("103", "PARTNER_FILE_INVALID", "File");
            var invalidDir = "Out/Invalid|Path\0";
            var request = new ConnectionRequest
            {
                JsonPayload = "{}",
                ConnectionType = "File",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["DirectoryPath"] = invalidDir
                }
            };

            // Act
            var result = await fileSender.SendAsync(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("File", result.Channel);
            Assert.Equal("InvalidConfiguration", result.ErrorCode);
            Assert.Contains("không hợp lệ", result.ErrorMessage);
        }

        [Fact]
        public void PartnerValidator_ValidateConnectionTypeAndConfig_ValidatesCorrectly_Test()
        {
            // Arrange & Act & Assert: ConnectionType
            Assert.True(PartnerValidator.IsValidConnectionType("API"));
            Assert.True(PartnerValidator.IsValidConnectionType("REST"));
            Assert.True(PartnerValidator.IsValidConnectionType("HTTP"));
            Assert.True(PartnerValidator.IsValidConnectionType("File"));
            Assert.True(PartnerValidator.IsValidConnectionType("Kafka"));
            Assert.True(PartnerValidator.IsValidConnectionType("FTP"));
            Assert.True(PartnerValidator.IsValidConnectionType("api"));
            Assert.True(PartnerValidator.IsValidConnectionType("kafka"));

            Assert.False(PartnerValidator.IsValidConnectionType("RabbitMQ"));
            Assert.False(PartnerValidator.IsValidConnectionType("gRPC"));
            Assert.False(PartnerValidator.IsValidConnectionType("MQTT"));

            // Arrange & Act & Assert: ConnectionConfig
            Assert.True(PartnerValidator.IsValidConnectionConfig(null!, out _));
            Assert.True(PartnerValidator.IsValidConnectionConfig("", out _));
            Assert.True(PartnerValidator.IsValidConnectionConfig("   ", out _));
            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"TimeoutSeconds\": 30, \"BypassSsl\": true, \"AuthType\": \"Bearer\", \"Token\": \"sec123\"}", out var validErr));
            Assert.Null(validErr);

            Assert.False(PartnerValidator.IsValidConnectionConfig("not_json", out var err1));
            Assert.NotNull(err1);

            Assert.False(PartnerValidator.IsValidConnectionConfig("[1, 2, 3]", out var err2));
            Assert.NotNull(err2);

            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"nested\": {\"key\": 1}}", out var err3));
            Assert.NotNull(err3);

            // Các key dành riêng cho cột (BaseUrl, Url, Host, Server, Address, IP, Port, Endpoint, Path, EndPointApiUrl) bị từ chối
            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"BaseUrl\": \"http://localhost\"}", out var errBaseUrl));
            Assert.NotNull(errBaseUrl);
            Assert.Contains("BaseUrl", errBaseUrl);

            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"Port\": 18090}", out var errPort));
            Assert.NotNull(errPort);
            Assert.Contains("Port", errPort);

            // Key trùng nhau (không phân biệt hoa thường) bị từ chối
            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"Token\": \"1\", \"token\": \"2\"}", out var errDup));
            Assert.NotNull(errDup);
            Assert.Contains("trùng lặp", errDup);

            // Key rỗng bị từ chối
            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"\": \"val\"}", out var errEmptyKey));
            Assert.NotNull(errEmptyKey);
            Assert.Contains("không được để trống", errEmptyKey);

            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"TimeoutSeconds\": -1}", out var err5));
            Assert.NotNull(err5);
            Assert.Contains("dương", err5);

            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"BypassSsl\": \"xyz\"}", out var err6));
            Assert.NotNull(err6);
            Assert.Contains("boolean", err6);

            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"AuthType\": \"InvalidAuth\"}", out var err7));
            Assert.NotNull(err7);
            Assert.Contains("AuthType", err7);
        }

        [Fact]
        public void CsvFormatter_ConvertsJsonArrayToRfc4180Csv_EscapesProperly_Test()
        {
            // Arrange
            var json = """
                [
                    {"id": 1, "name": "Station A", "remark": "Clear, dry"},
                    {"id": 2, "name": "Station \"B\"", "remark": "Rainy, wet\r\nSlippery road"}
                ]
                """;

            // Act
            var csv = ShareDataWorker.Core.Utils.Formatters.CsvFormatter.ConvertJsonToCsv(json);

            // Assert
            Assert.Contains("id,name,remark", csv);
            Assert.Contains("1,Station A,\"Clear, dry\"", csv);
            Assert.Contains("2,\"Station \"\"B\"\"\",\"Rainy, wet\r\nSlippery road\"", csv);
        }

        [Fact]
        public void AesGcmCodec_EncodeDecodeRoundtrip_ReturnsOriginalBytes_Test()
        {
            // Arrange
            var codec = new Modules.Integration.ShareData.Codecs.AesGcmCodec();
            var keyBytes = new byte[Modules.Integration.ShareData.Codecs.AesGcmCodec.KeySize];
            System.Security.Cryptography.RandomNumberGenerator.Fill(keyBytes);
            var ctx = new Modules.Integration.ShareData.Codecs.PayloadCodecContext("test-kid", keyBytes);
            var originalBytes = Encoding.UTF8.GetBytes("{\"test\":\"secret-payload-data\"}");

            // Act
            var encodedBytes = codec.Encode(originalBytes, ctx);
            var decodedBytes = codec.Decode(encodedBytes, ctx);

            // Assert
            Assert.NotEqual(originalBytes, encodedBytes);
            Assert.Equal(originalBytes, decodedBytes);
            var jsonEnvelope = Encoding.UTF8.GetString(encodedBytes);
            Assert.Contains("AES256-GCM", jsonEnvelope);
            Assert.Contains("test-kid", jsonEnvelope);
        }

        [Fact]
        public void NoneCodec_EncodeDecode_PassesThroughIdenticalBytes_Test()
        {
            // Arrange
            var codec = new Modules.Integration.ShareData.Codecs.NoneCodec();
            var ctx = new Modules.Integration.ShareData.Codecs.PayloadCodecContext(null, null);
            var originalBytes = Encoding.UTF8.GetBytes("{\"test\":\"plain-payload\"}");

            // Act
            var encoded = codec.Encode(originalBytes, ctx);
            var decoded = codec.Decode(encoded, ctx);

            // Assert
            Assert.Equal(originalBytes, encoded);
            Assert.Equal(originalBytes, decoded);
        }

        [Fact]
        public async Task FtpSender_WhenDisabledOrMissingHost_ReturnsClearError_Test()
        {
            // Arrange
            var ftpSender = new DataOutboundFtpSender();
            var ctx = CreateDummyContext("101", "PARTNER_FTP", "Ftp");

            var disabledReq = new ConnectionRequest
            {
                JsonPayload = "[{\"id\": 1}]",
                ConnectionType = "Ftp",
                ConnectionConfig = new Dictionary<string, string> { ["Enabled"] = "false" }
            };

            var missingHostReq = new ConnectionRequest
            {
                JsonPayload = "[{\"id\": 1}]",
                ConnectionType = "Ftp",
                ConnectionConfig = new Dictionary<string, string> { ["Enabled"] = "true" }
            };

            // Act
            var disabledRes = await ftpSender.SendAsync(disabledReq, ctx, CancellationToken.None);
            if (ctx.Partner != null) ctx.Partner.Address = null;
            var missingHostRes = await ftpSender.SendAsync(missingHostReq, ctx, CancellationToken.None);

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
            var kafkaSender = new DataOutboundKafkaSender();
            var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");

            var disabledReq = new ConnectionRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectionType = "Kafka",
                ConnectionConfig = new Dictionary<string, string> { ["Enabled"] = "false" }
            };

            var missingServersReq = new ConnectionRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectionType = "Kafka",
                ConnectionConfig = new Dictionary<string, string> { ["Enabled"] = "true", ["Topic"] = "my-topic" }
            };

            // Act
            var disabledRes = await kafkaSender.SendAsync(disabledReq, ctx, CancellationToken.None);
            if (ctx.Partner != null) ctx.Partner.Address = null;
            var missingServersRes = await kafkaSender.SendAsync(missingServersReq, ctx, CancellationToken.None);

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

            var reqWithBootstrap = new ConnectionRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectionType = "Kafka",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["BootstrapServers"] = "kafka-cluster:9092,kafka-cluster:9093"
                }
            };

            var reqFallback = new ConnectionRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectionType = "Kafka",
                ConnectionConfig = new Dictionary<string, string>()
            };

            // Act: Phân giải bootstrapServers theo đúng logic DataOutboundKafkaSender
            var serversExplicit = reqWithBootstrap.ConnectionConfig.TryGetValue("BootstrapServers", out var s1) && !string.IsNullOrWhiteSpace(s1)
                ? s1.Trim()
                : (!string.IsNullOrWhiteSpace(ctx.Partner?.Address)
                    ? (ctx.Partner.Address.Contains(':') || !ctx.Partner.Port.HasValue ? ctx.Partner.Address.Trim() : $"{ctx.Partner.Address.Trim()}:{ctx.Partner.Port.Value}")
                    : null);

            var serversFallback = reqFallback.ConnectionConfig.TryGetValue("BootstrapServers", out var s2) && !string.IsNullOrWhiteSpace(s2)
                ? s2.Trim()
                : (!string.IsNullOrWhiteSpace(ctx.Partner?.Address)
                    ? (ctx.Partner.Address.Contains(':') || !ctx.Partner.Port.HasValue ? ctx.Partner.Address.Trim() : $"{ctx.Partner.Address.Trim()}:{ctx.Partner.Port.Value}")
                    : null);

            // Assert
            Assert.Equal("kafka-cluster:9092,kafka-cluster:9093", serversExplicit);
            Assert.Equal("10.0.0.1:9092", serversFallback);
        }


        [Fact]
        public void PacketMetadataResolver_Packet105_DefaultsToSnapshot_CanBeOverriddenToIncremental_Test()
        {
            // Arrange
            var packetDefault = new ShareDataPacket { Code = "105", Name = "AVI/RFID Data" };
            var packetIncr = new ShareDataPacket { Code = "105", Name = "AVI/RFID Data", SyncKind = 2 };
            var mappingSnapshot = new ShareDataMapping { SyncKind = 1 };
            var mappingIncremental = new ShareDataMapping { SyncKind = 2 };

            // Act & Assert
            // 1. Mặc định gói 105 là Snapshot
            var defaultPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetDefault);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.Snapshot, defaultPolicy);

            // 2. Cấu hình SyncKind = 2 (Incremental) trên gói chuyển thành AlwaysIncremental
            var packetIncrPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetIncr);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.AlwaysIncremental, packetIncrPolicy);

            // 3. Cấu hình SyncKind = 2 trên Mapping của đối tác ghi đè gói tin
            var mappingIncrPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetDefault, mappingIncremental);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.AlwaysIncremental, mappingIncrPolicy);

            // 4. Cấu hình SyncKind = 1 trên Mapping của đối tác ghi đè gói tin Incremental thành Snapshot
            var mappingSnapPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetIncr, mappingSnapshot);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.Snapshot, mappingSnapPolicy);

            // 5. Cả packet và mapping đều không có SyncKind (null) -> Fallback mặc định Snapshot (1)
            var packetNull = new ShareDataPacket { Code = "999", Name = "Custom Packet", SyncKind = null };
            var mappingNull = new ShareDataMapping { SyncKind = null };
            var nullFallbackPolicy = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetNull, mappingNull);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.Snapshot, nullFallbackPolicy);

            var packetOnlyNull = ShareDataWorker.Core.Utils.Resolvers.PacketMetadataResolver.ResolvePacketPolicy(packetNull);
            Assert.Equal(ShareDataWorker.Core.Enums.ShareDataEnum.PacketPolicy.Snapshot, packetOnlyNull);
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
        public void PartnerValidator_ArchiveEnabled_ValidatesBoolean_Test()
        {
            // Valid boolean values
            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"ArchiveEnabled\": true}", out var err1));
            Assert.Null(err1);
            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"ArchiveEnabled\": false}", out var err2));
            Assert.Null(err2);
            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"ArchiveEnabled\": \"true\"}", out var err3));
            Assert.Null(err3);
            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"ArchiveEnabled\": \"false\"}", out var err4));
            Assert.Null(err4);

            // Invalid boolean value
            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"ArchiveEnabled\": \"abc\"}", out var errInvalid));
            Assert.NotNull(errInvalid);
            Assert.Contains("ArchiveEnabled", errInvalid);
            Assert.Contains("boolean", errInvalid);
        }

        [Fact]
        public void PartnerValidator_RejectsOutputPath_AndValidatesFileNamePatternVariables_Test()
        {
            // 1. Rejects OutputPath (deprecated key)
            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"OutputPath\": \"/tmp/test.json\"}", out var errOutput));
            Assert.NotNull(errOutput);
            Assert.Contains("OutputPath", errOutput);

            // 2. FileNamePattern missing both {serial} and {timestamp}
            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"FileNamePattern\": \"static_data.json\"}", out var errStaticPattern));
            Assert.NotNull(errStaticPattern);
            Assert.Contains("FileNamePattern", errStaticPattern);
            Assert.Contains("{serial}", errStaticPattern);

            // 3. Valid FileNamePattern with {serial} or {timestamp}
            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"FileNamePattern\": \"data_{serial}.json\"}", out var errSerial));
            Assert.Null(errSerial);

            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"FileNamePattern\": \"data_{timestamp}.json\"}", out var errTime));
            Assert.Null(errTime);

            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"FileNamePattern\": \"{packet}_{partner}_{timestamp}_{serial}.json\"}", out var errFull));
            Assert.Null(errFull);

            // 4. Test IsFileChannel aliases
            Assert.True(ConnectionDispatcher.IsFileChannel("File"));
            Assert.True(ConnectionDispatcher.IsFileChannel("file"));
            Assert.True(ConnectionDispatcher.IsFileChannel("DISK"));
            Assert.True(ConnectionDispatcher.IsFileChannel("NAS"));
            Assert.False(ConnectionDispatcher.IsFileChannel("API"));
            Assert.False(ConnectionDispatcher.IsFileChannel("Kafka"));
            Assert.False(ConnectionDispatcher.IsFileChannel(null));
        }

        [Fact]
        public async Task FileSender_WhenDirectoryPathHasPathTraversal_ReturnsInvalidConfiguration_Test()
        {
            var fileSender = new DataOutboundFileSender();
            var ctx = CreateDummyContext("103", "PARTNER_FILE_TRAVERSAL", "File");
            var request = new ConnectionRequest
            {
                PayloadBytes = Encoding.UTF8.GetBytes("test-content"),
                ConnectionType = "File",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["DirectoryPath"] = "safe/../../etc/passwd"
                }
            };

            var result = await fileSender.SendAsync(request, ctx, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("File", result.Channel);
            Assert.Equal("InvalidConfiguration", result.ErrorCode);
            Assert.Contains("traversal", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FileSender_WhenDirectoryPathAndFileNamePatternConfigured_SavesToExpectedCustomPath_Test()
        {
            var fileSender = new DataOutboundFileSender();
            var ctx = CreateDummyContext("103", "PARTNER_FILE_CUSTOM", "File");
            var expectedBytes = Encoding.UTF8.GetBytes("custom-file-payload-content");
            var request = new ConnectionRequest
            {
                PayloadBytes = expectedBytes,
                FileExtension = ".csv",
                ConnectionType = "File",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["DirectoryPath"] = "custom_export_dir",
                    ["FileNamePattern"] = "export_{partner}_{packet}_{timestamp}"
                }
            };

            var result = await fileSender.SendAsync(request, ctx, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("File", result.Channel);
            Assert.NotNull(result.TargetAddressOrPath);
            Assert.Contains("custom_export_dir", result.TargetAddressOrPath);
            Assert.Contains("PARTNER_FILE_CUSTOM", result.TargetAddressOrPath);
            Assert.EndsWith(".csv", result.TargetAddressOrPath);

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "sharedata/send", result.TargetAddressOrPath);
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

            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BYTES", "API");
            var expectedBytes = Encoding.UTF8.GetBytes("{\"cipher\":\"encrypted_payload_123\"}");
            var request = new ConnectionRequest
            {
                PayloadBytes = expectedBytes,
                ContentType = "application/json",
                JsonPayload = "dummy_should_not_be_used",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = ShareDataPartnerServerMock.DefaultHost,
                    ["Port"] = ShareDataPartnerServerMock.DefaultPort.ToString()
                }
            };

            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(server.LastRequest);
            Assert.Equal(expectedBytes, Encoding.UTF8.GetBytes(server.LastRequest.Body));
        }

        [Fact]
        public async Task FtpSender_WhenCsvPayloadBytesProvided_UsesExactPayloadBytesAndCsvExtension_Test()
        {
            var ftpSender = new DataOutboundFtpSender();
            var ctx = CreateDummyContext("101", "PARTNER_FTP_CSV", "FTP");
            var csvBytes = Encoding.UTF8.GetBytes("ID,Code,Name\r\n1,A,Item1\r\n2,B,Item2");
            var request = new ConnectionRequest
            {
                PayloadBytes = csvBytes,
                ContentType = "text/csv",
                FileExtension = ".csv",
                JsonPayload = "{}",
                ConnectionType = "FTP",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["RemotePath"] = "/test/path"
                }
            };

            var result = await ftpSender.SendAsync(request, ctx, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Ftp", result.Channel);
            Assert.Contains(result.ErrorCode, new[] { "FtpConnectionFailed", "FtpSendFailed", "FtpWebException" });
        }
    }
}
