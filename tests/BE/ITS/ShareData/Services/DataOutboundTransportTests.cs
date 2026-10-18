using System.Text;
using Microsoft.Extensions.Configuration;
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Interfaces;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Interfaces.DataOutbound;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using Shared.DTO.Enums;
using Xunit;

namespace Tests.ShareData.Services
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ kiểm thử đơn vị cho chặng chuyển giao dữ liệu kết xuất (DataOutboundTransport):
    ///              kiểm chứng định dạng payload, lưu tệp bằng chứng vô điều kiện, chuyển giao qua dispatcher
    ///              và xử lý kết quả/cảnh báo lỗi.
    /// Created date: 16/10/2026
    /// </summary>
    public class DataOutboundTransportTests : IDisposable
    {
        private readonly string _tempDirectory;

        public DataOutboundTransportTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "dot_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Bỏ qua lỗi dọn dẹp thư mục tạm
            }
        }

        private IConfiguration CreateConfig(string? customBasePath = null)
        {
            var dict = new Dictionary<string, string?>
            {
                ["NasStorage:BasePath"] = customBasePath ?? _tempDirectory
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        private static DataOutboundContext CreateContext(
            string packetCode = "101",
            string partnerCode = "PARTNER_01",
            BaseEnums.ConnectionType? connectionType = BaseEnums.ConnectionType.Api,
            Dictionary<string, string>? config = null)
        {
            var partner = new ShareDataPartner
            {
                ID = "partner-id-1",
                Code = partnerCode,
                Name = "Partner 01",
                ConnectionType = connectionType,
                ConnectionConfig = config != null ? System.Text.Json.JsonSerializer.Serialize(config) : null,
                Address = "http://127.0.0.1",
                Port = 18090,
                EndPointApiUrl = "/api/receive"
            };
            var packet = new ShareDataPacket
            {
                ID = "packet-id-1",
                Code = packetCode,
                PacketVersion = "1.0"
            };
            var sub = new ShareDataSubscription
            {
                ID = "sub-id-1",
                PartnerId = partner.ID,
                DatatypeId = packetCode,
                SerialNbr = 10
            };
            return new DataOutboundContext(
                Subscription: sub,
                Partner: partner,
                Packet: packet,
                Mapping: null,
                ExportedAt: new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc),
                CancelToken: CancellationToken.None);
        }

        [Fact]
        public async Task Send_WhenNormalPayload_SucceedsAndSavesEvidenceMatchingDispatchedBytes_Test()
        {
            // Arrange
            var config = CreateConfig();
            var dispatcher = new FakeDeliveryDispatcher
            {
                ResultToReturn = new DeliveryResult
                {
                    Success = true,
                    Channel = "API",
                    SentByteSize = 35,
                    TargetAddressOrPath = "http://127.0.0.1:18090/api/receive"
                }
            };
            var transport = new DataOutboundTransport(dispatcher, config);
            var ctx = CreateContext();
            var rawJson = "[{\"id\":1,\"name\":\"Item 01\"}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(35, result.ByteSize);
            Assert.NotNull(result.RelativePath);
            Assert.Equal(1, dispatcher.DispatchCallCount);
            Assert.NotNull(dispatcher.LastRequest);

            var fullEvidencePath = Path.Combine(_tempDirectory, result.RelativePath);
            Assert.True(File.Exists(fullEvidencePath));
            var savedBytes = await File.ReadAllBytesAsync(fullEvidencePath);
            Assert.Equal(dispatcher.LastRequest!.PayloadBytes, savedBytes);
        }

        [Fact]
        public async Task Send_WhenFormatCsv_GeneratesCsvBytesAndCsvExtension_Test()
        {
            // Arrange
            var config = CreateConfig();
            var dispatcher = new FakeDeliveryDispatcher();
            var transport = new DataOutboundTransport(dispatcher, config);
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.Format] = "Csv"
            };
            var ctx = CreateContext(config: connCfg);
            var rawJson = "[{\"colA\":\"val1\",\"colB\":\"val2\"}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.RelativePath);
            Assert.EndsWith(".csv", result.RelativePath, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(dispatcher.LastRequest?.PayloadBytes);

            var payloadString = Encoding.UTF8.GetString(dispatcher.LastRequest!.PayloadBytes);
            Assert.Contains("colA,colB", payloadString);
            Assert.Contains("val1,val2", payloadString);
        }

        [Fact]
        public async Task Send_WhenFileChannelWithoutDirectoryPath_DoesNotCallDispatcherAndUsesEvidencePath_Test()
        {
            // Arrange
            var config = CreateConfig();
            var dispatcher = new FakeDeliveryDispatcher();
            var transport = new DataOutboundTransport(dispatcher, config);
            var ctx = CreateContext(connectionType: BaseEnums.ConnectionType.File);
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(0, dispatcher.DispatchCallCount);
            Assert.NotNull(result.RelativePath);
            var fullPath = Path.Combine(_tempDirectory, result.RelativePath);
            Assert.True(File.Exists(fullPath));
        }

        [Fact]
        public async Task Send_WhenFileChannelWithDirectoryPath_CallsDispatcher_Test()
        {
            // Arrange
            var config = CreateConfig();
            var dispatcher = new FakeDeliveryDispatcher();
            var transport = new DataOutboundTransport(dispatcher, config);
            var customTargetDir = Path.Combine(_tempDirectory, "custom_target");
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = customTargetDir
            };
            var ctx = CreateContext(connectionType: BaseEnums.ConnectionType.File, config: connCfg);
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, dispatcher.DispatchCallCount);
        }

        [Fact]
        public async Task Send_WhenSavingEvidenceFails_ReturnsArchiveFailedAlertAndAbortsDispatch_Test()
        {
            // Arrange: Tạo một tệp tĩnh trùng đường dẫn BasePath để Directory.CreateDirectory ném lỗi
            var blockerFilePath = Path.Combine(_tempDirectory, "blocker_file.bin");
            await File.WriteAllTextAsync(blockerFilePath, "blocking file");

            var config = CreateConfig(customBasePath: blockerFilePath);
            var dispatcher = new FakeDeliveryDispatcher();
            var transport = new DataOutboundTransport(dispatcher, config);
            var ctx = CreateContext();
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ShareDataAlertCode.Outbound.ArchiveFailed, result.AlertCode);
            Assert.Equal(0, dispatcher.DispatchCallCount);
            Assert.Contains("Lưu đệm payload thất bại", result.ErrorMessage);
        }

        [Fact]
        public async Task Send_WhenDispatcherFails_ReturnsHttpSendFailedAlertAndForwardsDetails_Test()
        {
            // Arrange
            var config = CreateConfig();
            var dispatcher = new FakeDeliveryDispatcher
            {
                ResultToReturn = new DeliveryResult
                {
                    Success = false,
                    ErrorMessage = null,
                    DetailJson = "{\"status\":500,\"error\":\"InternalServerError\"}"
                }
            };
            var transport = new DataOutboundTransport(dispatcher, config);
            var ctx = CreateContext();
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ShareDataAlertCode.Outbound.HttpSendFailed, result.AlertCode);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("thất bại", result.ErrorMessage);
            Assert.Equal("{\"status\":500,\"error\":\"InternalServerError\"}", result.DetailJson);
        }

        [Fact]
        public async Task Send_WhenLegacyArchiveEnabledFalseConfigured_IgnoresFlagAndAlwaysSavesEvidence_Test()
        {
            // Arrange: Khóa cấu hình cũ ArchiveEnabled=false bị bỏ qua, hệ thống luôn lưu bằng chứng
            var config = CreateConfig();
            var dispatcher = new FakeDeliveryDispatcher();
            var transport = new DataOutboundTransport(dispatcher, config);
            var connCfg = new Dictionary<string, string>
            {
                ["ArchiveEnabled"] = "false"
            };
            var ctx = CreateContext(config: connCfg);
            var rawJson = "[{\"id\":100}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.RelativePath);
            var fullPath = Path.Combine(_tempDirectory, result.RelativePath);
            Assert.True(File.Exists(fullPath));
        }

        [Fact]
        public async Task Send_WhenConfigContainsOutputPathAndUnsupportedEncryptor_EmitsTransportWarnings_Test()
        {
            // Arrange
            var config = CreateConfig();
            var dispatcher = new FakeDeliveryDispatcher();
            var transport = new DataOutboundTransport(dispatcher, config);
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.OutputPath] = "/deprecated/out/path",
                [ConnectKeys.Encryptor] = "UnsupportedAlgoX",
                [ConnectKeys.InboundEncryptor] = "UnsupportedInboundY"
            };
            var ctx = CreateContext(config: connCfg);
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("Khóa [OutputPath] là khóa đã bãi bỏ, bị bỏ qua."));
            Assert.Contains(result.Warnings, w => w.Contains("Thuật toán mã hóa Encryptor [UnsupportedAlgoX] không được hỗ trợ."));
            Assert.Contains(result.Warnings, w => w.Contains("Thuật toán mã hóa InboundEncryptor [UnsupportedInboundY] không được hỗ trợ."));
        }

        [Fact]
        public async Task Send_WhenDispatcherReturnsWarnings_MergesWarningsIntoResult_Test()
        {
            // Arrange
            var config = CreateConfig();
            var dispatcher = new FakeDeliveryDispatcher
            {
                ResultToReturn = new DeliveryResult
                {
                    Success = true,
                    Channel = "API",
                    SentByteSize = 50,
                    TargetAddressOrPath = "http://127.0.0.1:18090/api/receive",
                    Warnings = ["Cảnh báo từ Dispatcher kênh API."]
                }
            };
            var transport = new DataOutboundTransport(dispatcher, config);
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.OutputPath] = "/deprecated/out/path"
            };
            var ctx = CreateContext(config: connCfg);
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.Equal(2, result.Warnings.Count);
            Assert.Contains(result.Warnings, w => w.Contains("OutputPath"));
            Assert.Contains(result.Warnings, w => w.Contains("Cảnh báo từ Dispatcher"));
        }

        [Fact]
        public void BuildRequest_WhenPartnerHasSecurityAndConnectionConfig_ExtractsSecurityInfoAndConfigProperly_Test()
        {
            // Arrange
            var ctx = CreateContext("105", "PARTNER_CONFIG", BaseEnums.ConnectionType.Kafka);
            ctx.Partner!.ConnectionConfig = "{\"ApiKey\":\"secret-key-12345\",\"Topic\":\"rfid-topic\",\"HttpTimeoutSec\":\"25\"}";
            var mappingBytes = Encoding.UTF8.GetBytes("{\"tag\":\"RFID_001\"}");
            var mapping = new DataMappingResult(true, mappingBytes, 1);

            // Act
            var request = DataOutboundTransport.BuildRequest(mapping, ctx);

            // Assert
            Assert.Equal("{\"tag\":\"RFID_001\"}", request.JsonPayload);
            Assert.Equal(BaseEnums.ConnectionType.Kafka, request.ConnectType);
            Assert.True(request.ConnectConfig.ContainsKey("Topic"));
            Assert.Equal("rfid-topic", request.ConnectConfig["Topic"]);
            Assert.True(request.ConnectConfig.ContainsKey("HttpTimeoutSec"));
            Assert.Equal("25", request.ConnectConfig["HttpTimeoutSec"]);
            Assert.True(request.SecurityInfo.ContainsKey(ConnectKeys.ApiKey));
            Assert.Equal("secret-key-12345", request.SecurityInfo[ConnectKeys.ApiKey]);
        }

        private sealed class FakeDeliveryDispatcher : IDeliveryDispatcher
        {
            public int DispatchCallCount { get; private set; }
            public DeliveryRequest? LastRequest { get; private set; }
            public DeliveryContext? LastContext { get; private set; }
            public DeliveryResult ResultToReturn { get; set; } = new DeliveryResult
            {
                Success = true,
                Channel = "API",
                SentByteSize = 100,
                TargetAddressOrPath = "http://127.0.0.1:18090/api/receive"
            };

            public Task<DeliveryResult> Dispatch(
                DeliveryRequest request,
                DeliveryContext context,
                CancellationToken cancellationToken = default)
            {
                DispatchCallCount++;
                LastRequest = request;
                LastContext = context;
                return Task.FromResult(ResultToReturn);
            }
        }
    }
}
