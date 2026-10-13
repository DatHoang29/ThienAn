using Services.Shared.Messaging;

namespace Tests.RealServer
{
    /// <summary>
    /// Description: Bộ kiểm thử kết nối trực tiếp tới NATS Server thật của ShareDataWorker.
    ///              Độc lập hoàn toàn với luồng nghiệp vụ và CSDL local, thuộc Collection "real-server".
    /// Created date: 13/10/2026
    /// </summary>
    [Collection("real-server")]
    public class ShareDataNatsServerConnectionTests
    {
        /// <summary>
        /// Description: Kiểm thử kết nối trực tiếp lên NATS Server thật (tls://115.78.1.139:12211, InsecureSkipVerify = true)
        ///              theo cấu hình của ShareDataWorker/appsettings.json và xác nhận luồng Publish/Subscribe hoạt động thông suốt.
        /// Created date: 13/10/2026
        /// </summary>
        [Fact(Skip = "Test kết nối trực tiếp lên NATS Server thật (tls://115.78.1.139:12211) — bỏ Skip khi cần kiểm tra kết nối server.")]
        public async Task NatsServerTlsConnection_WhenUsingShareDataWorkerConfig_ConnectsAndPublishesRoundTrip_Test()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N")[..8];
            var testSubject = $"ta.its.event.sharedata.newdata.test.{testId}";
            var expectedPayload = new { TestId = testId, PacketCode = "103" };

            var workerDir = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "TA-ITS015-WEBAPI-V1.0", "src", "Services", "ShareData", "ShareDataWorker"));
            var appSettingsPath = Path.Combine(workerDir, "appsettings.json");
            var credsPath = Path.Combine(workerDir, "Configuration", "nats.creds");

            Assert.True(File.Exists(appSettingsPath), $"Không tìm thấy file cấu hình: {appSettingsPath}");
            Assert.True(File.Exists(credsPath), $"Không tìm thấy file chứng chỉ NATS: {credsPath}");

            var workerConfig = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Nats:JwtFile"] = credsPath,
                    ["Nats:UserSeedFile"] = credsPath,
                    ["Nats:NewDataSubject"] = testSubject
                })
                .Build();

            Assert.Equal("tls://115.78.1.139:12211", workerConfig["Nats:Url"]);
            Assert.True(workerConfig.GetValue<bool>("Nats:InsecureSkipVerify"));

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await Modules.DataTransporter.Natsio.Infrastructure.Handlers.NatsService.Instance.DisconnectAsync();

            await using var remoteTransport = new TransportManager(workerConfig);
            var receivedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Act 1: Kết nối trực tiếp lên NATS Server TLS thật và đăng ký lắng nghe trên subject kiểm thử
            await remoteTransport.ConnectAsync(cts.Token);
            await remoteTransport.SubscribeAsync(
                testSubject,
                msg =>
                {
                    receivedTcs.TrySetResult(msg);
                    return Task.CompletedTask;
                },
                cts.Token);

            // Assert 1: Xác nhận kết nối TLS tới server 115.78.1.139:12211 thành công
            Assert.True(remoteTransport.IsConnected,
                "Không thể kết nối trực tiếp tới NATS Server TLS tại tls://115.78.1.139:12211.");

            // Act 2: Publish thông điệp kiểm thử qua NATS Server thật và chờ nhận phản hồi round-trip
            var published = await remoteTransport.PublishAsync(testSubject, expectedPayload, cts.Token);
            var receivedMessage = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10), cts.Token);

            // Assert 2: Xác nhận thông điệp đi qua NATS Server thật về tới subscriber nguyên vẹn
            Assert.True(published, "Publish thông điệp lên NATS Server thật thất bại.");
            Assert.Contains(testId, receivedMessage);

            await remoteTransport.DisconnectAsync(cts.Token);
        }
    }
}
