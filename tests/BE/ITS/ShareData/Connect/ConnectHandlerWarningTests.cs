using System.Text;
using Confluent.Kafka;
using Modules.Integration.ShareData;
using Modules.Integration.ShareData.PacketSend;
using Modules.Integration.ShareData.PacketSend.Senders;
using Tests.ShareData.Mocks;
using Xunit;

namespace Tests.ShareData.Connect
{
    /// <summary>
    /// Description: Bộ kiểm thử đơn vị cho việc ghi nhận cảnh báo cấu hình (Warnings)
    ///              khi gửi dữ liệu qua các handler REST, FTP, Kafka và File (P13-BE).
    /// Created date: 17/10/2026
    /// </summary>
    [Collection("api")]
    public class ConnectHandlerWarningTests
    {
        private readonly Host _host;

        public ConnectHandlerWarningTests(Host host)
        {
            _host = host;
            _host.PartnerServer.ResetDefaults();
        }

        private static PacketSendContext CreateContext(string partnerCode = "PARTNER_WARN", string connectionType = "API")
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

        #region 1. RestPacketSender Warnings

        [Theory]
        [InlineData("")]
        [InlineData("None")]
        public async Task RestSender_WhenAuthTypeEmptyOrNoneWithCredentials_EmitsWarningAndSendsWithoutAuth_Test(string authType)
        {
            // Arrange
            _host.PartnerServer.ResetDefaults();
            var handler = new RestPacketSender();
            var ctx = CreateContext();
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    [ConnectKeys.AuthType] = authType,
                    [ConnectKeys.Token] = "stray-token-123",
                    [ConnectKeys.Username] = "stray-user"
                }
            };

            // Act
            var result = await handler.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("Cấu hình phương thức xác thực rỗng hoặc None nhưng tồn tại thông tin xác thực"));
            Assert.False(_host.PartnerServer.LastRequest!.Headers.ContainsKey("Authorization"));
        }

        [Fact]
        public async Task RestSender_WhenUnsupportedAuthType_EmitsWarning_Test()
        {
            // Arrange
            _host.PartnerServer.ResetDefaults();
            var handler = new RestPacketSender();
            var ctx = CreateContext();
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    [ConnectKeys.AuthType] = "OAuth3Custom",
                    [ConnectKeys.Token] = "token-123"
                }
            };

            // Act
            var result = await handler.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("AuthType [OAuth3Custom] không được hỗ trợ"));
        }

        [Fact]
        public async Task RestSender_WhenBearerAuthMissingToken_EmitsWarning_Test()
        {
            // Arrange
            _host.PartnerServer.ResetDefaults();
            var handler = new RestPacketSender();
            var ctx = CreateContext();
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    [ConnectKeys.AuthType] = "Bearer",
                    [ConnectKeys.Token] = ""
                }
            };

            // Act
            var result = await handler.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("Bearer yêu cầu Token"));
        }

        [Fact]
        public async Task RestSender_WhenApiKeyAuthMissingKey_EmitsWarning_Test()
        {
            // Arrange
            _host.PartnerServer.ResetDefaults();
            var handler = new RestPacketSender();
            var ctx = CreateContext();
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    [ConnectKeys.AuthType] = "ApiKey"
                }
            };

            // Act
            var result = await handler.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("ApiKey yêu cầu ApiKey"));
        }

        [Fact]
        public async Task RestSender_WhenBasicAuthMissingCredentials_EmitsWarning_Test()
        {
            // Arrange
            _host.PartnerServer.ResetDefaults();
            var handler = new RestPacketSender();
            var ctx = CreateContext();
            var request = new PacketSendRequest
            {
                JsonPayload = "{\"test\": 1}",
                ConnectType = "API",
                ConnectConfig = new Dictionary<string, string>
                {
                    [ConnectKeys.AuthType] = "Basic",
                    [ConnectKeys.Username] = "admin"
                    // Missing Password
                }
            };

            // Act
            var result = await handler.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("Basic yêu cầu Username và Password"));
        }

        #endregion

        #region 2. FtpPacketSender Warnings

        [Fact]
        public void Ftp_ReadFtpConfig_WhenTimeoutNotInt_EmitsWarningAndDefaultsTo15_Test()
        {
            // Arrange
            var config = new Dictionary<string, string> { [ConnectKeys.TimeoutSeconds] = "invalid_timeout" };
            var warnings = new List<string>();

            // Act
            var ftpConfig = FtpPacketSender.ReadFtpConfig(config, null, warnings);

            // Assert
            Assert.Equal(15, ftpConfig.TimeoutSeconds);
            Assert.Single(warnings);
            Assert.Contains("không phải số nguyên hợp lệ, dùng mặc định 15 giây", warnings[0]);
        }

        [Theory]
        [InlineData("0", 1)]
        [InlineData("-10", 1)]
        [InlineData("150", 120)]
        public void Ftp_ReadFtpConfig_WhenTimeoutOutOfRange_EmitsWarningAndClamps_Test(string inputVal, int expectedClamped)
        {
            // Arrange
            var config = new Dictionary<string, string> { [ConnectKeys.TimeoutSeconds] = inputVal };
            var warnings = new List<string>();

            // Act
            var ftpConfig = FtpPacketSender.ReadFtpConfig(config, null, warnings);

            // Assert
            Assert.Equal(expectedClamped, ftpConfig.TimeoutSeconds);
            Assert.Single(warnings);
            Assert.Contains("vượt ngưỡng cho phép (1-120 giây)", warnings[0]);
        }

        [Fact]
        public void Ftp_ReadFtpConfig_WhenBooleanFieldsNotBool_EmitsWarningsAndUsesDefaults_Test()
        {
            // Arrange
            var config = new Dictionary<string, string>
            {
                [ConnectKeys.Enabled] = "not_bool_1",
                [ConnectKeys.UseSsl] = "not_bool_2",
                [ConnectKeys.CsvIncludeBom] = "not_bool_3"
            };
            var warnings = new List<string>();

            // Act
            var ftpConfig = FtpPacketSender.ReadFtpConfig(config, null, warnings);

            // Assert
            Assert.True(ftpConfig.IsEnabled);
            Assert.False(ftpConfig.UseSsl);
            Assert.False(ftpConfig.IncludeBom);
            Assert.Equal(3, warnings.Count);
            Assert.Contains(warnings, w => w.Contains("Enabled"));
            Assert.Contains(warnings, w => w.Contains("UseSsl"));
            Assert.Contains(warnings, w => w.Contains("CsvIncludeBom"));
        }

        #endregion

        #region 3. KafkaPacketSender Warnings

        [Fact]
        public void Kafka_BuildProducerConfig_WhenTimeoutMsNotInt_EmitsWarningAndDefaultsTo5000_Test()
        {
            // Arrange
            var config = new Dictionary<string, string> { [ConnectKeys.TimeoutMs] = "bad_timeout" };
            var warnings = new List<string>();

            // Act
            var pConfig = KafkaPacketSender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

            // Assert
            Assert.Equal(5000, pConfig.MessageTimeoutMs);
            Assert.Single(warnings);
            Assert.Contains("không phải số nguyên hợp lệ, dùng mặc định 5000ms", warnings[0]);
        }

        [Theory]
        [InlineData("100", 500)]
        [InlineData("80000", 60000)]
        public void Kafka_BuildProducerConfig_WhenTimeoutMsOutOfRange_EmitsWarningAndClamps_Test(string inputVal, int expectedClamped)
        {
            // Arrange
            var config = new Dictionary<string, string> { [ConnectKeys.TimeoutMs] = inputVal };
            var warnings = new List<string>();

            // Act
            var pConfig = KafkaPacketSender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

            // Assert
            Assert.Equal(expectedClamped, pConfig.MessageTimeoutMs);
            Assert.Single(warnings);
            Assert.Contains("vượt ngưỡng cho phép (500-60000ms)", warnings[0]);
        }

        [Fact]
        public void Kafka_BuildProducerConfig_WhenInvalidSecurityProtocol_EmitsWarning_Test()
        {
            // Arrange
            var config = new Dictionary<string, string> { [ConnectKeys.SecurityProtocol] = "BadProtoX" };
            var warnings = new List<string>();

            // Act
            var pConfig = KafkaPacketSender.BuildProducerConfig(config, "127.0.0.1:9092", warnings);

            // Assert
            Assert.Null(pConfig.SecurityProtocol);
            Assert.Single(warnings);
            Assert.Contains("SecurityProtocol [BadProtoX] không hợp lệ", warnings[0]);
        }

        [Fact]
        public void Kafka_BuildProducerConfig_SaslMechanismScramHyphenFails_ScramSha256Succeeds_Test()
        {
            // 1. "SCRAM-SHA-256" chứa dấu gạch ngang nên không khớp enum Confluent.Kafka.SaslMechanism.ScramSha256
            var badConfig = new Dictionary<string, string> { [ConnectKeys.SaslMechanism] = "SCRAM-SHA-256" };
            var badWarnings = new List<string>();

            var badPConfig = KafkaPacketSender.BuildProducerConfig(badConfig, "127.0.0.1:9092", badWarnings);

            Assert.Null(badPConfig.SaslMechanism);
            Assert.Single(badWarnings);
            Assert.Contains("SaslMechanism [SCRAM-SHA-256] không hợp lệ", badWarnings[0]);

            // 2. "ScramSha256" khớp enum Confluent.Kafka.SaslMechanism.ScramSha256
            var okConfig = new Dictionary<string, string> { [ConnectKeys.SaslMechanism] = "ScramSha256" };
            var okWarnings = new List<string>();

            var okPConfig = KafkaPacketSender.BuildProducerConfig(okConfig, "127.0.0.1:9092", okWarnings);

            Assert.Equal(SaslMechanism.ScramSha256, okPConfig.SaslMechanism);
            Assert.Empty(okWarnings);
        }

        #endregion

        #region 4. FilePacketSender Warnings

        [Fact]
        public async Task FileSender_WhenFileNamePatternMissingRequiredTokens_EmitsWarning_Test()
        {
            // Arrange
            var handler = new FilePacketSender();
            var ctx = CreateContext(connectionType: "File");
            var request = new PacketSendRequest
            {
                PayloadBytes = Encoding.UTF8.GetBytes("{\"file\": 1}"),
                ConnectType = "File",
                ConnectConfig = new Dictionary<string, string>
                {
                    [ConnectKeys.DirectoryPath] = "test_warn_out",
                    [ConnectKeys.FileNamePattern] = "static_export_file.json"
                }
            };

            // Act
            var result = await handler.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("thiếu tham số biến đổi {serial} và {timestamp}"));

            // Dọn dẹp tệp sinh ra
            if (!string.IsNullOrWhiteSpace(result.TargetAddressOrPath))
            {
                var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
        }

        [Fact]
        public async Task FileSender_WhenFileNamePatternHasSerialToken_DoesNotEmitMissingTokenWarning_Test()
        {
            // Arrange
            var handler = new FilePacketSender();
            var ctx = CreateContext(connectionType: "File");
            var request = new PacketSendRequest
            {
                PayloadBytes = Encoding.UTF8.GetBytes("{\"file\": 2}"),
                ConnectType = "File",
                ConnectConfig = new Dictionary<string, string>
                {
                    [ConnectKeys.DirectoryPath] = "test_warn_out",
                    [ConnectKeys.FileNamePattern] = "export_{serial}.json"
                }
            };

            // Act
            var result = await handler.Send(request, ctx, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Warnings);
            Assert.DoesNotContain(result.Warnings, w => w.Contains("thiếu tham số biến đổi {serial} và {timestamp}"));

            // Dọn dẹp tệp sinh ra
            if (!string.IsNullOrWhiteSpace(result.TargetAddressOrPath))
            {
                var fullPath = Path.Combine(ExportPathHelper.ResolveRootPath(ExportPathHelper.DefaultBasePath), result.TargetAddressOrPath);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
        }

        #endregion
    }
}
