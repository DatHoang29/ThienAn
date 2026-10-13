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
            Assert.True(result.Success);
            Assert.Equal("Kafka", result.Channel);
            Assert.Equal("traffic-weather-topic", result.TargetAddressOrPath);
            Assert.Equal(Encoding.UTF8.GetByteCount(jsonPayload), result.SentByteSize);
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
    }
}
