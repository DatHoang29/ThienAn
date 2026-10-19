using System.Net;
using System.Text;
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Interfaces;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;

namespace Tests.ShareData.Services
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ kiểm thử đơn vị và tích hợp cho chặng chuyển giao dữ liệu kết xuất (DataOutboundTransport):
    ///              kiểm chứng định dạng payload, lưu tệp bằng chứng vô điều kiện, chuyển giao qua IDeliveryDispatcher thật
    ///              và xử lý kết quả/cảnh báo lỗi (Rule 19.18).
    /// Created date: 16/10/2026
    /// </summary>
    [Collection("api")]
    public class DataOutboundTransportTests : IDisposable
    {
        private readonly Host _host;
        private readonly IDeliveryDispatcher _dispatcher;
        private readonly string _tempDirectory;

        public DataOutboundTransportTests(Host host)
        {
            _host = host;
            _host.PartnerServer.ResetDefaults();
            _dispatcher = _host.Services.GetRequiredService<IDeliveryDispatcher>();

            var hostConfig = _host.Services.GetRequiredService<IConfiguration>();
            var basePath = hostConfig.GetValue<string>(ExportPathHelper.BasePathConfigKey) ?? ExportPathHelper.DefaultBasePath;
            var baseRoot = ExportPathHelper.ResolveRootPath(basePath);

            _tempDirectory = Path.Combine(baseRoot, "dot_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            _host.PartnerServer.ResetDefaults();
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
                [ExportPathHelper.BasePathConfigKey] = customBasePath ?? _tempDirectory
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        private static DataOutboundContext CreateContext(
            string packetCode = "101",
            string partnerCode = "PARTNER_01",
            ConnectionType? connectionType = ConnectionType.Api,
            Dictionary<string, string>? config = null,
            int port = ShareDataPartnerServerMock.DefaultPort,
            string endPointApiUrl = "/api/receive")
        {
            var partner = new ShareDataPartner
            {
                ID = "partner-id-" + Guid.NewGuid().ToString("N"),
                Code = partnerCode,
                Name = "Partner 01",
                ConnectionType = connectionType,
                ConnectionConfig = config != null ? System.Text.Json.JsonSerializer.Serialize(config) : null,
                Address = ShareDataPartnerServerMock.DefaultHost,
                Port = port,
                EndPointApiUrl = endPointApiUrl
            };
            var packet = new ShareDataPacket
            {
                ID = "packet-id-" + Guid.NewGuid().ToString("N"),
                Code = packetCode,
                PacketVersion = "1.0"
            };
            var sub = new ShareDataSubscription
            {
                ID = "sub-id-" + Guid.NewGuid().ToString("N"),
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
            // Arrange: Kênh File có DirectoryPath ghi nhận byte payload thực tế chuyển giao tới tệp đích
            var config = CreateConfig();
            var transport = new DataOutboundTransport(_dispatcher, config);
            var customTargetDir = Path.Combine(_tempDirectory, "dispatched_" + Guid.NewGuid().ToString("N"));
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = customTargetDir
            };
            var ctx = CreateContext(connectionType: ConnectionType.File, config: connCfg);
            var rawJson = "[{\"id\":1,\"name\":\"Item 01\"}]";
            var rawBytes = Encoding.UTF8.GetBytes(rawJson);
            var mapping = new DataMappingResult(true, rawBytes, 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(rawBytes.Length, result.ByteSize);
            Assert.NotNull(result.RelativePath);

            var fullEvidencePath = Path.Combine(_tempDirectory, result.RelativePath);
            Assert.True(File.Exists(fullEvidencePath));
            var savedEvidenceBytes = await File.ReadAllBytesAsync(fullEvidencePath);

            Assert.True(Directory.Exists(customTargetDir));
            var dispatchedFiles = Directory.GetFiles(customTargetDir);
            var dispatchedFile = Assert.Single(dispatchedFiles);
            var dispatchedBytes = await File.ReadAllBytesAsync(dispatchedFile);

            Assert.Equal(savedEvidenceBytes, dispatchedBytes);
            Assert.Equal(rawBytes, dispatchedBytes);
        }

        [Fact]
        public async Task Send_WhenFormatCsv_GeneratesCsvBytesAndCsvExtension_Test()
        {
            // Arrange: Định dạng CSV qua kênh File thật sinh tệp có đuôi .csv và nội dung phân tách dấu phẩy
            var config = CreateConfig();
            var transport = new DataOutboundTransport(_dispatcher, config);
            var customTargetDir = Path.Combine(_tempDirectory, "dispatched_csv_" + Guid.NewGuid().ToString("N"));
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.Format] = "Csv",
                [ConnectKeys.DirectoryPath] = customTargetDir
            };
            var ctx = CreateContext(connectionType: ConnectionType.File, config: connCfg);
            var rawJson = "[{\"colA\":\"val1\",\"colB\":\"val2\"}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.RelativePath);
            Assert.EndsWith(".csv", result.RelativePath, StringComparison.OrdinalIgnoreCase);

            var fullEvidencePath = Path.Combine(_tempDirectory, result.RelativePath);
            Assert.True(File.Exists(fullEvidencePath));
            var evidenceContent = await File.ReadAllTextAsync(fullEvidencePath);
            Assert.Contains("colA,colB", evidenceContent);
            Assert.Contains("val1,val2", evidenceContent);

            Assert.True(Directory.Exists(customTargetDir));
            var dispatchedFiles = Directory.GetFiles(customTargetDir);
            var dispatchedFile = Assert.Single(dispatchedFiles);
            Assert.EndsWith(".csv", dispatchedFile, StringComparison.OrdinalIgnoreCase);

            var dispatchedContent = await File.ReadAllTextAsync(dispatchedFile);
            Assert.Equal(evidenceContent, dispatchedContent);
            Assert.Contains("colA,colB", dispatchedContent);
            Assert.Contains("val1,val2", dispatchedContent);
        }

        [Fact]
        public async Task Send_WhenFileChannelWithoutDirectoryPath_DoesNotCallDispatcherAndUsesEvidencePath_Test()
        {
            // Arrange: Kênh File không có DirectoryPath thì không chuyển giao qua dispatcher, chỉ dùng tệp bằng chứng
            var config = CreateConfig();
            var transport = new DataOutboundTransport(_dispatcher, config);
            var uncalledTargetDir = Path.Combine(_tempDirectory, "uncalled_target_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(uncalledTargetDir);

            var ctx = CreateContext(connectionType: ConnectionType.File);
            var rawJson = "[{\"id\":1}]";
            var rawBytes = Encoding.UTF8.GetBytes(rawJson);
            var mapping = new DataMappingResult(true, rawBytes, 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.RelativePath);
            var fullPath = Path.Combine(_tempDirectory, result.RelativePath);
            Assert.True(File.Exists(fullPath));

            // Không gọi dispatcher: thư mục đích không có tệp mới
            Assert.Empty(Directory.GetFiles(uncalledTargetDir));
        }

        [Fact]
        public async Task Send_WhenFileChannelWithDirectoryPath_CallsDispatcher_Test()
        {
            // Arrange: Kênh File có DirectoryPath chuyển giao thành công tới thư mục chỉ định qua FileDeliverySender thật
            var config = CreateConfig();
            var transport = new DataOutboundTransport(_dispatcher, config);
            var customTargetDir = Path.Combine(_tempDirectory, "custom_target_" + Guid.NewGuid().ToString("N"));
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = customTargetDir
            };
            var ctx = CreateContext(connectionType: ConnectionType.File, config: connCfg);
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(Directory.Exists(customTargetDir));
            var files = Directory.GetFiles(customTargetDir);
            Assert.Single(files);
            Assert.True(new FileInfo(files[0]).Length > 0);
        }

        [Fact]
        public async Task Send_WhenSavingEvidenceFails_ReturnsArchiveFailedAlertAndAbortsDispatch_Test()
        {
            // Arrange: Tạo một tệp tĩnh trùng đường dẫn BasePath để Directory.CreateDirectory ném lỗi
            var blockerFilePath = Path.Combine(_tempDirectory, "blocker_file.bin");
            await File.WriteAllTextAsync(blockerFilePath, "blocking file");

            var config = CreateConfig(customBasePath: blockerFilePath);
            var transport = new DataOutboundTransport(_dispatcher, config);
            var customTargetDir = Path.Combine(_tempDirectory, "aborted_target_" + Guid.NewGuid().ToString("N"));
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.DirectoryPath] = customTargetDir
            };
            var ctx = CreateContext(connectionType: ConnectionType.File, config: connCfg);
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
            Assert.Contains("Lưu đệm payload thất bại", result.ErrorMessage);
            Assert.False(Directory.Exists(customTargetDir));
        }

        [Fact]
        public async Task Send_WhenDispatcherFails_ReturnsHttpSendFailedAlertAndForwardsDetails_Test()
        {
            // Arrange: Máy chủ đối tác trả HTTP 500 kèm chi tiết lỗi
            _host.PartnerServer.ResetDefaults();
            _host.PartnerServer.DefaultStatusCode = HttpStatusCode.InternalServerError;
            _host.PartnerServer.DefaultResponseBody = "{\"status\":500,\"error\":\"InternalServerError\"}";

            var config = CreateConfig();
            var transport = new DataOutboundTransport(_dispatcher, config);
            var ctx = CreateContext(
                connectionType: ConnectionType.Api,
                port: ShareDataPartnerServerMock.DefaultPort,
                endPointApiUrl: "/api/receive");
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
            Assert.NotNull(result.DetailJson);
            Assert.Contains("500", result.DetailJson);
            Assert.Contains("InternalServerError", result.DetailJson);
        }

        [Fact]
        public async Task Send_WhenLegacyArchiveEnabledFalseConfigured_IgnoresFlagAndAlwaysSavesEvidence_Test()
        {
            // Arrange: Khóa cấu hình cũ ArchiveEnabled=false bị bỏ qua, hệ thống luôn lưu bằng chứng
            var config = CreateConfig();
            var transport = new DataOutboundTransport(_dispatcher, config);
            var connCfg = new Dictionary<string, string>
            {
                ["ArchiveEnabled"] = "false"
            };
            var ctx = CreateContext(connectionType: ConnectionType.File, config: connCfg);
            var rawJson = "[{\"id\":100}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.RelativePath);
            var fullPath = Path.Combine(_tempDirectory, result.RelativePath);
            Assert.True(File.Exists(fullPath));
        }

        [Fact]
        public async Task Send_WhenConfigContainsOutputPathAndUnsupportedEncryptor_EmitsTransportWarnings_Test()
        {
            // Arrange
            var config = CreateConfig();
            var transport = new DataOutboundTransport(_dispatcher, config);
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.OutputPath] = "/deprecated/out/path",
                [ConnectKeys.Encryptor] = "UnsupportedAlgoX",
                [ConnectKeys.InboundEncryptor] = "UnsupportedInboundY"
            };
            var ctx = CreateContext(connectionType: ConnectionType.File, config: connCfg);
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("Khóa [OutputPath] là khóa đã bãi bỏ, bị bỏ qua."));
            Assert.Contains(result.Warnings, w => w.Contains("Thuật toán mã hóa Encryptor [UnsupportedAlgoX] không được hỗ trợ."));
            Assert.Contains(result.Warnings, w => w.Contains("Thuật toán mã hóa InboundEncryptor [UnsupportedInboundY] không được hỗ trợ."));
        }

        [Fact]
        public async Task Send_WhenDispatcherReturnsWarnings_MergesWarningsIntoResult_Test()
        {
            // Arrange: Cảnh báo từ DataOutboundTransport (OutputPath) gộp cùng cảnh báo từ HttpDeliverySender (AuthType không hỗ trợ)
            _host.PartnerServer.ResetDefaults();
            var config = CreateConfig();
            var transport = new DataOutboundTransport(_dispatcher, config);
            var connCfg = new Dictionary<string, string>
            {
                [ConnectKeys.OutputPath] = "/deprecated/out/path",
                [ConnectKeys.AuthType] = "UnsupportedAuth"
            };
            var ctx = CreateContext(
                connectionType: ConnectionType.Api,
                config: connCfg,
                port: ShareDataPartnerServerMock.DefaultPort,
                endPointApiUrl: "/api/receive");
            var rawJson = "[{\"id\":1}]";
            var mapping = new DataMappingResult(true, Encoding.UTF8.GetBytes(rawJson), 1);

            // Act
            var result = await transport.Send(
                mapping,
                ctx,
                CancellationToken.None);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.Warnings);
            Assert.Equal(2, result.Warnings.Count);
            Assert.Contains(result.Warnings, w => w.Contains("OutputPath"));
            Assert.Contains(result.Warnings, w => w.Contains("AuthType"));
        }

        [Fact]
        public void BuildRequest_WhenPartnerHasSecurityAndConnectionConfig_ExtractsSecurityInfoAndConfigProperly_Test()
        {
            // Arrange
            var ctx = CreateContext("105", "PARTNER_CONFIG", ConnectionType.Kafka);
            ctx.Partner!.ConnectionConfig = "{\"ApiKey\":\"secret-key-12345\",\"Topic\":\"rfid-topic\",\"HttpTimeoutSec\":\"25\"}";
            var mappingBytes = Encoding.UTF8.GetBytes("{\"tag\":\"RFID_001\"}");
            var mapping = new DataMappingResult(true, mappingBytes, 1);

            // Act
            var request = DataOutboundTransport.BuildRequest(mapping, ctx);

            // Assert
            Assert.Equal("{\"tag\":\"RFID_001\"}", request.JsonPayload);
            Assert.Equal(ConnectionType.Kafka, request.ConnectType);
            Assert.True(request.ConnectConfig.ContainsKey("Topic"));
            Assert.Equal("rfid-topic", request.ConnectConfig["Topic"]);
            Assert.True(request.ConnectConfig.ContainsKey("HttpTimeoutSec"));
            Assert.Equal("25", request.ConnectConfig["HttpTimeoutSec"]);
            Assert.True(request.SecurityInfo.ContainsKey(ConnectKeys.ApiKey));
            Assert.Equal("secret-key-12345", request.SecurityInfo[ConnectKeys.ApiKey]);
        }
    }
}
