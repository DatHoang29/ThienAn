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
    public class ShareDataOutboundTransportTests
    {
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
            var dispatcher = ShareDataConnectionDispatcher.CreateForHandlers([kafkaSender]);
            var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");
            var jsonPayload = "{\"temperature\": 28.5, \"humidity\": 65}";
            var request = new ShareDataConnectionRequest
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
            Assert.Equal("NotSupported", result.ErrorCode);
            Assert.Equal("traffic-weather-topic", result.TargetAddressOrPath);
        }

        [Fact]
        public async Task KafkaSender_WhenPayloadIsEmpty_ReturnsFailedResult_Test()
        {
            // Arrange
            var kafkaSender = new DataOutboundKafkaSender();
            var ctx = CreateDummyContext("101", "PARTNER_KAFKA", "Kafka");
            var request = new ShareDataConnectionRequest
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
            var dispatcher = ShareDataConnectionDispatcher.CreateForHandlers([fileSender]);
            var ctx = CreateDummyContext("103", "PARTNER_FILE", "File");
            var jsonPayload = "{\"vdsCount\": 12, \"speed\": 80}";
            var customPath = "Out/TestPartner/vds_export.json";
            var request = new ShareDataConnectionRequest
            {
                JsonPayload = jsonPayload,
                ConnectionType = "File",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["OutputPath"] = customPath
                }
            };

            // Act
            var result = await dispatcher.DispatchAsync(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("File", result.Channel);
            Assert.Equal(customPath, result.TargetAddressOrPath);
            Assert.Equal(Encoding.UTF8.GetByteCount(jsonPayload), result.SentByteSize);
        }

        [Fact]
        public async Task DispatchAsync_WhenPartnerConnectionTypeIsApi_RoutesToApiChannel_Test()
        {
            // Arrange
            using var server = new ShareDataPartnerServerMock();
            server.Start(18092);
            var restSender = new DataOutboundRestSender();
            var dispatcher = ShareDataConnectionDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_API", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18092;
            ctx.Partner!.EndPointApiUrl = "/api/v1/data";

            var jsonPayload = "{\"data\": \"ok\"}";
            var request = new ShareDataConnectionRequest
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
            var dispatcher = ShareDataConnectionDispatcher.CreateForHandlers([]);
            var ctx = CreateDummyContext("101", "PARTNER_UNKNOWN", "UnknownProtocolX");
            var request = new ShareDataConnectionRequest
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
            var request = ShareDataConnectionDispatcher.BuildRequest(mapping, ctx);

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
            using var server = new ShareDataPartnerServerMock();
            server.Start(18092);
            var restSender = new DataOutboundRestSender();
            var dispatcher = ShareDataConnectionDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_LEGACY", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18092;
            ctx.Partner!.EndPointApiUrl = "/api/v1/legacy-data";
            ctx.Partner!.ConnectionConfig = null;

            var mappingBytes = Encoding.UTF8.GetBytes("{\"legacy\": true}");
            var mapping = new DataMappingResult(true, mappingBytes, 1);
            var request = ShareDataConnectionDispatcher.BuildRequest(mapping, ctx);

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
        public async Task DispatchAsync_WhenConnectionConfigOverridesLegacyFields_PrefersConnectionConfig_Test()
        {
            // Arrange: ConnectionConfig có Port và EndPointApiUrl khác với trường cũ -> Dictionary phải ưu tiên tuyệt đối
            using var server = new ShareDataPartnerServerMock();
            server.Start(18092);
            var restSender = new DataOutboundRestSender();
            var dispatcher = ShareDataConnectionDispatcher.CreateForHandlers([restSender]);
            var ctx = CreateDummyContext("101", "PARTNER_OVERRIDE", "API");
            ctx.Partner!.Address = "127.0.0.1";
            ctx.Partner!.Port = 18090; // Cũ
            ctx.Partner!.EndPointApiUrl = "/api/v1/old-endpoint"; // Cũ
            ctx.Partner!.ConnectionConfig = "{\"Port\":\"18092\",\"EndPointApiUrl\":\"/api/v1/override-endpoint\"}";

            var mappingBytes = Encoding.UTF8.GetBytes("{\"override\": true}");
            var mapping = new DataMappingResult(true, mappingBytes, 1);
            var request = ShareDataConnectionDispatcher.BuildRequest(mapping, ctx);

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
        [InlineData("70000")]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("invalid_port")]
        public async Task RestSender_WhenPortInvalid_ReturnsInvalidConfigurationError_Test(string invalidPort)
        {
            // Arrange
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BAD_PORT", "API");
            var request = new ShareDataConnectionRequest
            {
                JsonPayload = "{}",
                ConnectionType = "API",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["Address"] = "127.0.0.1",
                    ["Port"] = invalidPort
                }
            };

            // Act
            var result = await restSender.SendAsync(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("InvalidConfiguration", result.ErrorCode);
            Assert.Contains("Port", result.Message);
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
            var request = new ShareDataConnectionRequest
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
            Assert.Contains("Timeout", result.Message);
        }

        [Fact]
        public async Task RestSender_WhenBypassSslInvalid_ReturnsInvalidConfigurationError_Test()
        {
            // Arrange
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BAD_SSL", "API");
            var request = new ShareDataConnectionRequest
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
            Assert.Contains("BypassSsl", result.Message);
        }

        [Fact]
        public async Task RestSender_WhenBearerAuthProvided_SendsAuthorizationBearerHeader_Test()
        {
            // Arrange
            using var server = new ShareDataPartnerServerMock();
            server.Start(18092);
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BEARER", "API");
            var request = new ShareDataConnectionRequest
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
            using var server = new ShareDataPartnerServerMock();
            server.Start(18092);
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_APIKEY", "API");
            var request = new ShareDataConnectionRequest
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
            using var server = new ShareDataPartnerServerMock();
            server.Start(18092);
            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BASIC", "API");
            var request = new ShareDataConnectionRequest
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
            using var server = new ShareDataPartnerServerMock();
            server.Start(18092);
            server.DefaultResponseBody = "{\"isSuccess\": false, \"errorCode\": \"ERR_PAYLOAD_INVALID\", \"message\": \"Payload schema mismatch\"}";

            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_BIZ_ERR", "API");
            var request = new ShareDataConnectionRequest
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
            Assert.Contains("Payload schema mismatch", result.Message);
        }

        [Fact]
        public async Task RestSender_WhenFurionResponseHasFailedSucceeded_ReturnsBusinessError_Test()
        {
            // Arrange: Đối tác trả HTTP 200 dạng Furion/UnifyResult nhưng succeeded = false
            using var server = new ShareDataPartnerServerMock();
            server.Start(18092);
            server.DefaultResponseBody = "{\"succeeded\": false, \"statusCode\": 400, \"errors\": \"Dữ liệu không đúng định dạng\"}";

            var restSender = new DataOutboundRestSender();
            var ctx = CreateDummyContext("101", "PARTNER_FURION_ERR", "API");
            var request = new ShareDataConnectionRequest
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
            Assert.Contains("Dữ liệu không đúng định dạng", result.Message);
        }

        [Fact]
        public async Task FileSender_WhenOutputPathHasInvalidCharacters_ReturnsInvalidPathError_Test()
        {
            // Arrange
            var fileSender = new DataOutboundFileSender();
            var ctx = CreateDummyContext("103", "PARTNER_FILE_INVALID", "File");
            var invalidPath = "Out/Invalid|Path\0/test.json";
            var request = new ShareDataConnectionRequest
            {
                JsonPayload = "{}",
                ConnectionType = "File",
                ConnectionConfig = new Dictionary<string, string>
                {
                    ["OutputPath"] = invalidPath
                }
            };

            // Act
            var result = await fileSender.SendAsync(request, ctx, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("File", result.Channel);
            Assert.Equal("InvalidPath", result.ErrorCode);
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
            Assert.True(PartnerValidator.IsValidConnectionConfig("{\"Port\": 18090, \"TimeoutSeconds\": 30, \"BypassSsl\": true, \"AuthType\": \"Bearer\"}", out var validErr));
            Assert.Null(validErr);

            Assert.False(PartnerValidator.IsValidConnectionConfig("not_json", out var err1));
            Assert.NotNull(err1);

            Assert.False(PartnerValidator.IsValidConnectionConfig("[1, 2, 3]", out var err2));
            Assert.NotNull(err2);

            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"nested\": {\"key\": 1}}", out var err3));
            Assert.NotNull(err3);

            Assert.False(PartnerValidator.IsValidConnectionConfig("{\"Port\": 70000}", out var err4));
            Assert.NotNull(err4);
            Assert.Contains("65535", err4);

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
    }
}
