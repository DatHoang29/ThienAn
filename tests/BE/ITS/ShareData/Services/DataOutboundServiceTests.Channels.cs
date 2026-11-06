using System.Net;
using System.Text;
using Confluent.Kafka;
using Module.ShareData.Core.Entities;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums;
using Microsoft.Extensions.DependencyInjection;
using ShareDataWorker.Infrastructure.Logging;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Lượt gửi trọn vẹn qua kênh File, FTP và Kafka tới máy chủ thật trên Docker local: gói tin tới nơi thì mốc tiến; kênh từ chối hoặc không kết nối được thì mốc giữ nguyên và có cảnh báo.
    /// Created date: 05/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Gói một dòng và đối tác dùng kênh do configurePartner khai; trả mã gói, đối tác, đăng ký và mã dòng.
        /// Created date: 05/11/2026
        /// </summary>
        private async Task<(string PacketCode, ShareDataPartner Partner, ShareDataSubscription Subscription, string RowCode)> SeedChannelFlowAsync(
            ISqlSugarClient db, string unique, Action<ShareDataPartner> configurePartner)
        {
            var packetId = $"pkt-chn-{unique}";
            var packetCode = $"PKT_CHN_{unique}";
            var rowPrefix = $"ZC_CHN_{unique}_";
            await SeedZoneRowAsync(db, rowPrefix, unique, "R1", new DateTime(2026, 10, 27, 9, 0, 0));
            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var variables = ZoneVariables(10, rowPrefix, null);
            var sql = ZoneCursorSql();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_CHN_{unique}",
                SubCode = $"SUB_CHN_{unique}",
                DatatypeId = packetId,
                MappingShape = RowCodeShape,
                ConfigurePartner = configurePartner
            });
            return (packetCode, partner, sub, $"{rowPrefix}R1");
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đọc dòng mốc của đối tác và gói.
        /// Created date: 05/11/2026
        /// </summary>
        private static Task<ShareDataLastSend> ReadCursorAsync(ISqlSugarClient db, ShareDataPartner partner, string packetCode) =>
            db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode).FirstAsync();

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo thư mục trên máy chủ FTP thử nghiệm; đã có thì bỏ qua.
        /// Created date: 05/11/2026
        /// </summary>
        private static void EnsureFtpDirectory(string host, int port, string username, string password, string remotePath)
        {
#pragma warning disable SYSLIB0014
            var current = string.Empty;
            foreach (var part in remotePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                current += "/" + part;
                try
                {
                    var request = (FtpWebRequest)WebRequest.Create(new Uri($"ftp://{host}:{port}{current}"));
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = new NetworkCredential(username, password);
                    request.Timeout = 5000;
                    using var response = (FtpWebResponse)request.GetResponse();
                }
                catch (WebException ex) when (ex.Response is FtpWebResponse { StatusCode: FtpStatusCode.ActionNotTakenFileUnavailable })
                {
                }
            }
#pragma warning restore SYSLIB0014
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tải một tệp từ máy chủ FTP thử nghiệm về dạng chuỗi.
        /// Created date: 05/11/2026
        /// </summary>
        private static string DownloadFtpFile(string uri, string username, string password)
        {
#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(new Uri(uri));
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(username, password);
            request.UseBinary = true;
            request.KeepAlive = false;
            using var response = (FtpWebResponse)request.GetResponse();
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
#pragma warning restore SYSLIB0014
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kênh File có thư mục tùy chỉnh: nằm trong thư mục gốc cho phép thì ghi tệp ở đó và tiến mốc, khóa cấu hình không phân biệt hoa thường;
        ///              ngoài thư mục gốc hoặc có ".." thì không ghi, mốc giữ nguyên, ghi ESH-1401.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("trongGoc", ConnectKeys.DirectoryPath)]
        [InlineData("trongGoc", "directorypath")]
        [InlineData("ngoaiGoc", ConnectKeys.DirectoryPath)]
        [InlineData("vuotThuMuc", ConnectKeys.DirectoryPath)]
        public async Task ProcessSubscriptions_WhenFileChannelHasDirectory_WritesOnlyInsideAllowedRoot_Test(string directoryKind, string configKey)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var tempRoot = Path.GetTempPath();
            var outside = Path.Combine(tempRoot, $"sd-outside-{unique}");
            var directory = directoryKind switch
            {
                "trongGoc" => $"partner-files-{unique}",
                "ngoaiGoc" => outside,
                _ => $"../sd-outside-{unique}"
            };
            var (packetCode, partner, sub, rowCode) = await SeedChannelFlowAsync(db, unique, p =>
            {
                p.ConnectionType = ConnectionType.File;
                p.ConnectionConfig = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { [configKey] = directory });
            });
            var workingDirectory = Directory.GetCurrentDirectory();
            var insideRoot = Path.Combine(workingDirectory, "sharedata/send", directory);

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var cursor = await ReadCursorAsync(db, partner, packetCode);
            if (directoryKind == "trongGoc")
            {
                var files = Directory.GetFiles(insideRoot);
                var file = Assert.Single(files);
                var content = await File.ReadAllTextAsync(file);
                Assert.Contains(rowCode, content);
                Assert.EndsWith($"_R1_{unique}", cursor.LastKey);
                Directory.Delete(insideRoot, recursive: true);
            }
            else
            {
                var outsideCreated = Directory.Exists(outside);
                var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
                Assert.False(outsideCreated);
                Assert.True(string.IsNullOrEmpty(cursor?.LastKey));
                Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác nhận qua FTP: gói tin được tải lên đúng thư mục và tên tệp đã khai, tải về thấy đủ dòng, mốc tiến.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerUsesFtp_UploadsPageAndAdvancesCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var settings = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var host = settings["Ftp:Host"]!;
            var port = int.Parse(settings["Ftp:Port"]!);
            var username = settings["Ftp:Username"]!;
            var password = settings["Ftp:Password"]!;
            var unique = Guid.NewGuid().ToString("N")[..8];
            var remotePath = $"sharedata-test/{unique}";
            EnsureFtpDirectory(host, port, username, password, remotePath);
            var fileName = $"goi-{unique}.json";
            var (packetCode, partner, _, rowCode) = await SeedChannelFlowAsync(db, unique, p =>
            {
                p.ConnectionType = ConnectionType.Ftp;
                p.Address = host;
                p.Port = port;
                p.ConnectionConfig = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    [ConnectKeys.Username] = username,
                    [ConnectKeys.Password] = password,
                    [ConnectKeys.RemotePath] = remotePath,
                    [ConnectKeys.FileName] = fileName
                });
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var uploaded = DownloadFtpFile($"ftp://{host}:{port}/{remotePath}/{fileName}", username, password);
            var cursor = await ReadCursorAsync(db, partner, packetCode);
            Assert.Contains(rowCode, uploaded);
            Assert.EndsWith($"_R1_{unique}", cursor.LastKey);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đối tác nhận qua Kafka: gói tin vào đúng topic đã khai, đọc lại thấy đủ dòng, mốc tiến.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenPartnerUsesKafka_ProducesPageAndAdvancesCursor_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var bootstrapServers = scope.ServiceProvider.GetRequiredService<IConfiguration>()["Kafka:BootstrapServers"]!;
            var unique = Guid.NewGuid().ToString("N")[..8];
            var topic = $"sharedata.test.{unique}";
            var (packetCode, partner, _, rowCode) = await SeedChannelFlowAsync(db, unique, p =>
            {
                p.ConnectionType = ConnectionType.Kafka;
                p.ConnectionConfig = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    [ConnectKeys.Topic] = topic,
                    [ConnectKeys.BootstrapServers] = bootstrapServers,
                    [ConnectKeys.TimeoutMs] = "20000"
                });
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            using var consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = $"group.test.{unique}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            }).Build();
            consumer.Subscribe(topic);
            var pollInterval = TimeSpan.FromMilliseconds(300);
            var deadline = DateTime.UtcNow.AddSeconds(30);
            ConsumeResult<string, byte[]>? received = null;
            while (received?.Message == null && DateTime.UtcNow < deadline)
            {
                received = consumer.Consume(pollInterval);
            }

            Assert.NotNull(received?.Message);
            var payload = Encoding.UTF8.GetString(received.Message.Value);
            var cursor = await ReadCursorAsync(db, partner, packetCode);
            Assert.Contains(rowCode, payload);
            Assert.EndsWith($"_R1_{unique}", cursor.LastKey);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kênh FTPS luôn bật mã hóa, kể cả khi cấu hình ghi UseSsl = false: máy chủ chỉ có FTP thường thì không lùi về gửi không mã hóa, tệp không có trên máy chủ, mốc giữ nguyên, ghi ESH-1401.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("false")]
        public async Task ProcessSubscriptions_WhenFtpsServerHasNoTls_DoesNotFallBackToPlainFtp_Test(string? useSsl)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var settings = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var host = settings["Ftp:Host"]!;
            var port = int.Parse(settings["Ftp:Port"]!);
            var username = settings["Ftp:Username"]!;
            var password = settings["Ftp:Password"]!;
            var unique = Guid.NewGuid().ToString("N")[..8];
            var remotePath = $"sharedata-test/{unique}";
            EnsureFtpDirectory(host, port, username, password, remotePath);
            var fileName = $"goi-{unique}.json";
            var connectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.Username] = username,
                [ConnectKeys.Password] = password,
                [ConnectKeys.RemotePath] = remotePath,
                [ConnectKeys.FileName] = fileName,
                [ConnectKeys.TimeoutSeconds] = "5"
            };
            if (useSsl != null)
            {
                connectConfig[ConnectKeys.UseSsl] = useSsl;
            }
            var (packetCode, partner, sub, _) = await SeedChannelFlowAsync(db, unique, p =>
            {
                p.ConnectionType = ConnectionType.Ftps;
                p.Address = host;
                p.Port = port;
                p.ConnectionConfig = System.Text.Json.JsonSerializer.Serialize(connectConfig);
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var download = Record.Exception(() => DownloadFtpFile($"ftp://{host}:{port}/{remotePath}/{fileName}", username, password));
            var cursor = await ReadCursorAsync(db, partner, packetCode);
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            Assert.IsType<WebException>(download);
            Assert.True(string.IsNullOrEmpty(cursor?.LastKey));
            Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kênh FTP, Kafka bị tắt (Enabled = false), thiếu địa chỉ hoặc không kết nối được: không tính là đã gửi, mốc giữ nguyên, ghi ESH-1401 để lượt sau gửi lại.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("ftpTat")]
        [InlineData("ftpKhongKetNoi")]
        [InlineData("ftpThieuDiaChi")]
        [InlineData("kafkaTat")]
        [InlineData("kafkaThieuDiaChi")]
        public async Task ProcessSubscriptions_WhenFtpOrKafkaCannotDeliver_KeepsCursorAndLogsEsh1401_Test(string caseName)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var (packetCode, partner, sub, _) = await SeedChannelFlowAsync(db, unique, p =>
            {
                p.ConnectionType = caseName.StartsWith("ftp", StringComparison.Ordinal) ? ConnectionType.Ftp : ConnectionType.Kafka;
                switch (caseName)
                {
                    case "ftpTat":
                    case "kafkaTat":
                        p.Address = "127.0.0.1";
                        p.Port = 1;
                        p.ConnectionConfig = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { [ConnectKeys.Enabled] = "false" });
                        break;
                    case "ftpKhongKetNoi":
                        p.Address = "127.0.0.1";
                        p.Port = 1;
                        p.ConnectionConfig = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { [ConnectKeys.TimeoutSeconds] = "3" });
                        break;
                    default:
                        p.Address = null;
                        p.Port = null;
                        break;
                }
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var cursor = await ReadCursorAsync(db, partner, packetCode);
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            Assert.True(string.IsNullOrEmpty(cursor?.LastKey));
            Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.HttpSendFailed);
        }
    }
}
