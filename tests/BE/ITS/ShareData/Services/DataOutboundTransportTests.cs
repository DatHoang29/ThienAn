using System.Text;
using Microsoft.Extensions.Configuration;
using Module.ShareData.Core.Entities;
using Modules.Integration.ShareData;
using Modules.Integration.ShareData.Connect;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Interfaces.DataOutbound;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
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
            string connectionType = "API",
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
            var dispatcher = new FakeConnectDispatcher
            {
                ResultToReturn = new ConnectResult
                {
                    Success = true,
                    Channel = ConnectTypes.Api,
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
            var dispatcher = new FakeConnectDispatcher();
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
            var dispatcher = new FakeConnectDispatcher();
            var transport = new DataOutboundTransport(dispatcher, config);
            var ctx = CreateContext(connectionType: ConnectTypes.File);
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
            var dispatcher = new FakeConnectDispatcher();
            var transport = new DataOutboundTransport(dispatcher, config);
            var customTargetDir = Path.Combine(_tempDirectory, "custom_target");
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = customTargetDir
            };
            var ctx = CreateContext(connectionType: ConnectTypes.File, config: connCfg);
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
            var dispatcher = new FakeConnectDispatcher();
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
            var dispatcher = new FakeConnectDispatcher
            {
                ResultToReturn = new ConnectResult
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
            var dispatcher = new FakeConnectDispatcher();
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

        private sealed class FakeConnectDispatcher : IConnectDispatcher
        {
            public int DispatchCallCount { get; private set; }
            public ConnectRequest? LastRequest { get; private set; }
            public ConnectContext? LastContext { get; private set; }
            public ConnectResult ResultToReturn { get; set; } = new ConnectResult
            {
                Success = true,
                Channel = ConnectTypes.Api,
                SentByteSize = 100,
                TargetAddressOrPath = "http://127.0.0.1:18090/api/receive"
            };

            public Task<ConnectResult> Dispatch(
                ConnectRequest request,
                ConnectContext context,
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
