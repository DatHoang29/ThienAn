using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Core.Entities;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Interfaces;
using ShareDataWorker.Infrastructure.Services.DataExport;
using ShareDataWorker.Infrastructure.Services.DataExport.Transports;
using SqlSugar;
using Xunit;

namespace Tests.Modules.ShareData.Infrastructure.Services.DataExport.Transports
{
    [Collection("api")]
    public class HttpTransportTests(Host host) : IDisposable
    {
        private readonly Host _host = host;
        private MockHttpServer? _mockServer;

        public void Dispose()
        {
            _mockServer?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task HttpTransport_Send_WhenServerReturns200_SucceedsWithHeadersAndPayload_Test()
        {
            // 1. Khởi động Mock HTTP Server
            _mockServer = new MockHttpServer(18091);
            _mockServer.Start();

            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<HttpTransport>>();

            var partnerCode = "TEST_HTTP_PARTNER_" + Guid.NewGuid().ToString("N")[..8];
            var partner = new ShareDataPartner
            {
                ID = partnerCode,
                Code = partnerCode,
                Name = "Test HTTP Partner",
                Address = "127.0.0.1",
                Port = 18091,
                UseTls = false,
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var configDict = new Dictionary<string, string?>
            {
                [$"ShareDataTransport:Partners:{partnerCode}:Channel"] = "HTTP",
                [$"ShareDataTransport:Partners:{partnerCode}:Path"] = "/api/sharedata/v1/inbound",
                [$"ShareDataTransport:Partners:{partnerCode}:AuthType"] = "ApiKey",
                [$"ShareDataTransport:Partners:{partnerCode}:ApiKey"] = "secret-token-12345"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var transport = new HttpTransport(httpClientFactory, config, logger);
            var testPayload = Encoding.UTF8.GetBytes("{\"test\":\"data\"}");
            var hash = Convert.ToHexString(SHA256.HashData(testPayload));

            var request = new TransportRequest
            {
                PartnerCode = partnerCode,
                RelativePath = "Out/test.json",
                Payload = testPayload,
                Hash = hash,
                DatatypeId = "101",
                Format = "JSON",
                Address = partner.Address,
                Port = partner.Port,
                UseTls = partner.UseTls
            };

            // 2. Gửi qua HttpTransport
            var result = await transport.Send(request);

            // 3. Khẳng định
            Assert.True(result.Success);
            Assert.Equal("OK", result.StatusText);
            Assert.Single(_mockServer.ReceivedRequests);

            var req = _mockServer.ReceivedRequests[0];
            Assert.Equal("/api/sharedata/v1/inbound", req.Path);
            Assert.Equal("POST", req.Method);
            Assert.Equal(hash, req.Headers["X-Content-SHA256"]);
            Assert.Equal("secret-token-12345", req.Headers["X-API-Key"]);
            Assert.Equal(testPayload, req.Body);
        }

        [Fact]
        public async Task HttpTransport_Send_WhenServerReturns500_ReturnsFailure_Test()
        {
            _mockServer = new MockHttpServer(18092)
            {
                StatusCodeToReturn = 500
            };
            _mockServer.Start();

            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<HttpTransport>>();

            var partnerCode = "TEST_500_PARTNER_" + Guid.NewGuid().ToString("N")[..8];
            var partner = new ShareDataPartner
            {
                ID = partnerCode,
                Code = partnerCode,
                Name = "Test 500 Partner",
                Address = "127.0.0.1",
                Port = 18092,
                UseTls = false,
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var configDict = new Dictionary<string, string?>
            {
                [$"ShareDataTransport:Partners:{partnerCode}:Channel"] = "HTTP",
                [$"ShareDataTransport:Partners:{partnerCode}:AuthType"] = "ApiKey",
                [$"ShareDataTransport:Partners:{partnerCode}:ApiKey"] = "secret-token"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var transport = new HttpTransport(httpClientFactory, config, logger);
            var request = new TransportRequest
            {
                PartnerCode = partnerCode,
                Payload = Encoding.UTF8.GetBytes("{}"),
                Hash = "DUMMY_HASH",
                Address = partner.Address,
                Port = partner.Port,
                UseTls = partner.UseTls
            };

            var result = await transport.Send(request);

            Assert.False(result.Success);
            Assert.Contains("500", result.ErrorMessage);
        }

        [Fact]
        public async Task HttpTransport_Send_WhenMissingAuthCredentials_ThrowsInvalidOperationException_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<HttpTransport>>();

            var partnerCode = "TEST_NOAUTH_PARTNER";
            var configDict = new Dictionary<string, string?>
            {
                [$"ShareDataTransport:Partners:{partnerCode}:Channel"] = "HTTP"
                // Thiếu AuthType và ApiKey
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var transport = new HttpTransport(httpClientFactory, config, logger);
            var request = new TransportRequest
            {
                PartnerCode = partnerCode,
                Payload = Encoding.UTF8.GetBytes("{}"),
                Hash = "DUMMY_HASH"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => transport.Send(request));
        }

        [Fact]
        public async Task ExecuteExportForSubscription_WhenHttpTransportFails_LogsEsh1401_AndDoesNotAdvanceWatermark_Test()
        {
            // Mock server trả 500
            _mockServer = new MockHttpServer(18093)
            {
                StatusCodeToReturn = 500
            };
            _mockServer.Start();

            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var partnerId = "P_HTTP_FAIL_" + Guid.NewGuid().ToString("N")[..8];
            var partner = new ShareDataPartner
            {
                ID = partnerId,
                Code = partnerId,
                Name = "Partner HTTP Fail Test",
                Address = "127.0.0.1",
                Port = 18093,
                UseTls = false,
                Status = BaseEnums.StatusEnum.Enable
            };
            await db.Insertable(partner).ExecuteCommandAsync();

            var packet = new ShareDataPacket
            {
                ID = "PK_HTTP_FAIL_" + Guid.NewGuid().ToString("N")[..8],
                Code = "998",
                Name = "Packet Test HTTP Fail",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Incremental,
                PacketVersion = "1.0",
                IsActive = true
            };
            await db.Insertable(packet).ExecuteCommandAsync();

            var table = new ShareDataTable
            {
                ID = "TB_HTTP_FAIL_" + Guid.NewGuid().ToString("N")[..8],
                PacketCode = packet.Code,
                TableName = "ShareDataActivityLog",
                OrderNo = 1,
                IsRoot = true,
                IsActive = true,
                FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                {
                    new() { FieldKey = "logId", Column = "ID", DataType = "string", Required = true }
                })
            };
            await db.Insertable(table).ExecuteCommandAsync();

            // Insert a row in data table so query finds data
            await db.Insertable(new ShareDataActivityLog
            {
                ID = "SYS_LOG_FAIL_TEST",
                OccurredAt = DateTime.Now,
                LogType = ShareDataEnum.LogType.Transfer,
                Action = "TEST",
                Status = "Success"
            }).ExecuteCommandAsync();

            var initialLastTime = new DateTime(2026, 1, 1, 0, 0, 0);
            var sub = new ShareDataSubscription
            {
                ID = "SUB_HTTP_FAIL_" + Guid.NewGuid().ToString("N")[..8],
                PartnerId = partner.ID,
                DatatypeId = packet.Code,
                State = BaseEnums.SubSubscriptionState.Active,
                Mode = ShareDataEnum.SubMode.Periodic,
                Direction = ShareDataEnum.SubDirection.Outbound,
                ScheduleJson = "{\"intervalSeconds\":60}",
                LastTimeRun = initialLastTime,
                LastId = "INIT_ID"
            };
            await db.Insertable(sub).ExecuteCommandAsync();

            var configDict = new Dictionary<string, string?>
            {
                [$"ShareDataTransport:Partners:{partner.Code}:Channel"] = "HTTP",
                [$"ShareDataTransport:Partners:{partner.Code}:AuthType"] = "ApiKey",
                [$"ShareDataTransport:Partners:{partner.Code}:ApiKey"] = "test-key"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var service = new DataExportService(
                scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                scope.ServiceProvider.GetRequiredService<ILogger<DataExportService>>(),
                config);

            var (newWatermark, newLastId) = await service.ExecuteExportForSubscription(db, sub, partner, DateTime.Now, CancellationToken.None);

            // BẤT BIẾN 13 & 31: Truyền thất bại => kết quả trả về null, mốc KHÔNG NHÍCH
            Assert.Null(newWatermark);
            Assert.Null(newLastId);

            // AlertLog ESH-1401 được ghi nhận
            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.SubscriptionId == sub.ID && a.AlertCode == "ESH-1401")
                .ToListAsync();
            Assert.NotEmpty(alerts);

            // ActivityLog ghi nhận trạng thái Failed
            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.PartnerId == partner.ID && l.DatatypeId == packet.Code)
                .OrderByDescending(l => l.OccurredAt)
                .ToListAsync();
            Assert.NotEmpty(logs);
            Assert.Equal(ShareDataEnum.ExportStatus.Failed, logs[0].Status);
        }

        private class MockHttpServer : IDisposable
        {
            private readonly HttpListener _listener = new();
            private CancellationTokenSource? _cts;
            private Task? _listenTask;

            public int Port { get; }
            public int StatusCodeToReturn { get; set; } = 200;
            public List<(string Path, string Method, Dictionary<string, string> Headers, byte[] Body)> ReceivedRequests { get; } = [];

            public MockHttpServer(int port)
            {
                Port = port;
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            }

            public void Start()
            {
                _cts = new CancellationTokenSource();
                _listener.Start();
                _listenTask = Task.Run(async () =>
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var ctx = await _listener.GetContextAsync();
                            var req = ctx.Request;

                            using var ms = new MemoryStream();
                            await req.InputStream.CopyToAsync(ms);
                            var body = ms.ToArray();

                            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (string key in req.Headers.AllKeys)
                            {
                                if (key != null)
                                    headers[key] = req.Headers[key] ?? string.Empty;
                            }

                            lock (ReceivedRequests)
                            {
                                ReceivedRequests.Add((req.Url?.AbsolutePath ?? "/", req.HttpMethod, headers, body));
                            }

                            ctx.Response.StatusCode = StatusCodeToReturn;
                            var respBytes = Encoding.UTF8.GetBytes(StatusCodeToReturn == 200 ? "{\"status\":\"ok\"}" : "{\"error\":\"server_error\"}");
                            ctx.Response.ContentType = "application/json";
                            ctx.Response.ContentLength64 = respBytes.Length;
                            await ctx.Response.OutputStream.WriteAsync(respBytes);
                            ctx.Response.Close();
                        }
                        catch when (_cts.Token.IsCancellationRequested) { break; }
                        catch { }
                    }
                });
            }

            public void Dispose()
            {
                _cts?.Cancel();
                try { _listener.Stop(); } catch { }
                try { _listener.Close(); } catch { }
            }
        }
    }
}
