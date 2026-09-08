using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure.Services.ISAPIDevice;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Author: Đạt
    /// Description: 10 test case cho tầng xác thực Digest (VwISAPIDigestHandler) — không dùng Moq, dùng StubHttpMessageHandler
    /// Created date: 08/09/2026
    /// </summary>
    public class VwISAPIDigestHandlerTests
    {
        private static (HttpClient Client, StubHttpMessageHandler Stub) CreateTestPipeline(
            VwDeviceOptions options,
            Func<HttpRequestMessage, int, HttpResponseMessage> handlerFunc)
        {
            var stub = new StubHttpMessageHandler(handlerFunc);
            var credResolver = new VwISAPICredentialResolver(Options.Create(options));
            var digestHandler = new VwISAPIDigestHandler(credResolver, NullLogger<VwISAPIDigestHandler>.Instance)
            {
                InnerHandler = stub
            };
            var client = new HttpClient(digestHandler);
            return (client, stub);
        }

        private static VwDeviceOptions DefaultOptions => new()
        {
            Device = new VwDeviceConnectionOptions
            {
                UseMockDevice = false,
                Account = "admin",
                Password = "123"
            }
        };

        private static VwController DefaultController => new()
        {
            ID = "ctrl-1",
            Name = "Wall Controller",
            IP = "127.0.0.1",
            Account = "admin",
            PassWord = "123"
        };

        private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, VwController? controller = null)
        {
            var request = new HttpRequestMessage(method, uri);
            if (controller != null)
                request.Options.Set(VwISAPIDeviceClient.ControllerOptionKey, controller);

            return request;
        }

        #region 10 Test Cases

        [Fact]
        public async Task Test01_DualChallengeHeader_SplitsByDigestKeyword_DoesNotMixNonceAndAlgorithm()
        {
            // Case 1: Header có 2 challenge nối nhau (Digest ..., Digest ...)
            // Tách theo từ khoá Digest, không trộn nonce của cái này với algorithm của cái kia
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate",
                        "Digest realm=\"IP Camera\", nonce=\"sha256_nonce_111\", algorithm=\"SHA-256\", " +
                        "Digest realm=\"IP Camera\", nonce=\"md5_nonce_222\", algorithm=\"MD5\"");
                    return res;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (client)
            {
                var request = CreateRequest(HttpMethod.Get, "http://127.0.0.1:8080/ISAPI/Security/userCheck", DefaultController);
                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(2, stub.Requests.Count);

                var authHeader = stub.Requests[1].Headers.Authorization?.Parameter;
                Assert.NotNull(authHeader);

                // Phải chọn MD5 và dùng đúng nonce của MD5 ("md5_nonce_222"), không được dính "sha256_nonce_111"
                Assert.Contains("algorithm=MD5", authHeader);
                Assert.Contains("nonce=\"md5_nonce_222\"", authHeader);
                Assert.DoesNotContain("sha256_nonce_111", authHeader);
            }
        }

        [Fact]
        public async Task Test02_ChallengeWithoutAlgorithm_DefaultsToMD5()
        {
            // Case 2: Challenge thiếu algorithm -> Mặc định MD5 theo RFC 7616
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hikvision\", nonce=\"nonce_no_algo\", qop=\"auth\"");
                    return res;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (client)
            {
                var request = CreateRequest(HttpMethod.Get, "http://127.0.0.1:8080/ISAPI/Security/userCheck", DefaultController);
                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(2, stub.Requests.Count);

                var authHeader = stub.Requests[1].Headers.Authorization?.Parameter;
                Assert.NotNull(authHeader);
                Assert.Contains("algorithm=MD5", authHeader);
                Assert.Contains("nonce=\"nonce_no_algo\"", authHeader);
            }
        }

        [Fact]
        public async Task Test03_BothMD5AndSHA256Present_SelectsMD5()
        {
            // Case 3: Có cả MD5 và SHA-256 -> Chọn MD5
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate",
                        "Digest realm=\"Hik\", nonce=\"sha_nonce\", algorithm=\"SHA-256\", " +
                        "Digest realm=\"Hik\", nonce=\"md5_nonce\", algorithm=\"MD5\"");
                    return res;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (client)
            {
                var request = CreateRequest(HttpMethod.Get, "http://127.0.0.1:8080/ISAPI/Security/userCheck", DefaultController);
                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var authHeader = stub.Requests[1].Headers.Authorization?.Parameter;
                Assert.NotNull(authHeader);
                Assert.Contains("algorithm=MD5", authHeader);
                Assert.Contains("nonce=\"md5_nonce\"", authHeader);
            }
        }

        [Fact]
        public async Task Test04_OnlyMD5SessPresent_DoesNotFallbackToCandidate_Returns401Immediately()
        {
            // Case 4: CHỈ có MD5-sess -> KHÔNG fallback candidates[0]; trả 401 thay vì hash sai rồi bị khoá IP
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hik\", nonce=\"sess_nonce\", algorithm=\"MD5-sess\"");
                return res;
            });

            using (client)
            {
                var request = CreateRequest(HttpMethod.Get, "http://127.0.0.1:8080/ISAPI/Security/userCheck", DefaultController);
                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                // Không gửi lại request thứ 2 vì challenge không được hỗ trợ
                Assert.Single(stub.Requests);
            }
        }

        [Fact]
        public async Task Test05_StaleIsTrue_RetriesExactlyOnce_NoInfiniteLoop()
        {
            // Case 5: stale=TRUE -> Retry ĐÚNG 1 lần, không lặp vô hạn
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hik\", nonce=\"expired_nonce\", qop=\"auth\"");
                    return res;
                }
                if (count == 2)
                {
                    // Chặng 2: nonce bị hết hạn, trả stale=true
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hik\", nonce=\"fresh_nonce\", qop=\"auth\", stale=true");
                    return res;
                }
                if (count == 3)
                {
                    // Chặng 3: thiết bị vẫn tiếp tục trả stale=true để thử xem handler có lặp vô hạn không
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hik\", nonce=\"another_nonce\", qop=\"auth\", stale=true");
                    return res;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (client)
            {
                var request = CreateRequest(HttpMethod.Get, "http://127.0.0.1:8080/ISAPI/Security/userCheck", DefaultController);
                var response = await client.SendAsync(request);

                // Tổng số request gửi xuống chỉ đúng 3 lần (1 ban đầu + 1 auth + 1 stale retry), không lặp vô hạn
                Assert.Equal(3, stub.Requests.Count);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

                // Request thứ 3 dùng nonce mới "fresh_nonce"
                var thirdAuth = stub.Requests[2].Headers.Authorization?.Parameter;
                Assert.NotNull(thirdAuth);
                Assert.Contains("nonce=\"fresh_nonce\"", thirdAuth);
            }
        }

        [Fact]
        public async Task Test06_StaleIsFalse_DoesNotRetry_Returns401Immediately()
        {
            // Case 6: stale=FALSE -> KHÔNG retry, đẩy 401 lên ngay cho circuit breaker
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hik\", nonce=\"nonce_1\", qop=\"auth\"");
                    return res;
                }

                // Chặng 2: sai mật khẩu, stale=false
                var resFail = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                resFail.Headers.Add("WWW-Authenticate", "Digest realm=\"Hik\", nonce=\"nonce_1\", qop=\"auth\", stale=false");
                return resFail;
            });

            using (client)
            {
                var request = CreateRequest(HttpMethod.Get, "http://127.0.0.1:8080/ISAPI/Security/userCheck", DefaultController);
                var response = await client.SendAsync(request);

                // Chỉ gửi 2 request (chặng 1 và chặng 2), không retry thêm
                Assert.Equal(2, stub.Requests.Count);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task Test07_UriWithQueryParams_HA2CalculatedOnPathAndQuery()
        {
            // Case 7: URI có ?format=json -> HA2 tính trên PathAndQuery KÈM query
            var uri = "http://127.0.0.1:8080/ISAPI/DisplayDev/VideoWall/1/scene/1/sceneInfo?format=json";
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hikvision\", nonce=\"test_nonce\", qop=\"auth\"");
                    return res;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (client)
            {
                var request = CreateRequest(HttpMethod.Get, uri, DefaultController);
                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var authHeader = stub.Requests[1].Headers.Authorization?.Parameter;
                Assert.NotNull(authHeader);

                // Kiểm tra uri trong header Authorization phải giữ nguyên ?format=json
                Assert.Contains("uri=\"/ISAPI/DisplayDev/VideoWall/1/scene/1/sceneInfo?format=json\"", authHeader);

                // Tính độc lập response hash để xác minh HA2 có chứa query string
                // HA1 = MD5(admin:Hikvision:123)
                var ha1 = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("admin:Hikvision:123"))).ToLowerInvariant();
                // HA2 = MD5(GET:/ISAPI/DisplayDev/VideoWall/1/scene/1/sceneInfo?format=json)
                var ha2 = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("GET:/ISAPI/DisplayDev/VideoWall/1/scene/1/sceneInfo?format=json"))).ToLowerInvariant();

                var cnonceMatch = Regex.Match(authHeader, @"cnonce=""([^""]+)""");
                Assert.True(cnonceMatch.Success);
                var cnonce = cnonceMatch.Groups[1].Value;

                var expectedResponse = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes($"{ha1}:test_nonce:00000001:{cnonce}:auth:{ha2}"))).ToLowerInvariant();
                Assert.Contains($"response=\"{expectedResponse}\"", authHeader);
            }
        }

        [Fact]
        public async Task Test08_QopAuthIntAuth_SelectsAuthNotFirstItem()
        {
            // Case 8: qop="auth-int,auth" -> SelectQop chọn auth, không lấy phần tử đầu
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hikvision\", nonce=\"test_nonce\", qop=\"auth-int,auth\"");
                    return res;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (client)
            {
                var request = CreateRequest(HttpMethod.Get, "http://127.0.0.1:8080/ISAPI/Security/userCheck", DefaultController);
                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var authHeader = stub.Requests[1].Headers.Authorization?.Parameter;
                Assert.NotNull(authHeader);

                Assert.Contains("qop=auth", authHeader);
                Assert.DoesNotContain("qop=auth-int", authHeader);
            }
        }

        [Fact]
        public async Task Test09_RequestWithBody_BuffersAndResendsBodyIntactOnSecondLeg()
        {
            // Case 9: Request có body (POST/PUT) -> Body buffer và gửi lại NGUYÊN VẸN ở chặng 2
            var (client, stub) = CreateTestPipeline(DefaultOptions, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hikvision\", nonce=\"test_nonce\", qop=\"auth\"");
                    return res;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (client)
            {
                var xmlPayload = "<WallScene><id>1</id><name>Test Scene</name></WallScene>";
                var request = CreateRequest(HttpMethod.Post, "http://127.0.0.1:8080/ISAPI/DisplayDev/VideoWall/1/scene", DefaultController);
                request.Content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(2, stub.Requests.Count);

                // Cả 2 chặng đều nhận được body nguyên vẹn và content type chính xác
                Assert.Equal(xmlPayload, stub.RequestBodies[0]);
                Assert.Equal(xmlPayload, stub.RequestBodies[1]);
                Assert.Equal("application/xml", stub.Requests[1].Content?.Headers.ContentType?.MediaType);
            }
        }

        [Fact]
        public async Task Test10_NoControllerInRequestOptions_FallsBackToConfigAccount_DoesNotThrow()
        {
            // Case 10: Không gắn VwController vào request.Options -> Ghi LogWarning, rơi về tài khoản config, không ném
            var options = new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions
                {
                    UseMockDevice = false,
                    Account = "fallback_config_user",
                    Password = "fallback_config_password"
                }
            };

            var (client, stub) = CreateTestPipeline(options, (req, count) =>
            {
                if (count == 1)
                {
                    var res = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    res.Headers.Add("WWW-Authenticate", "Digest realm=\"Hikvision\", nonce=\"test_nonce\", qop=\"auth\"");
                    return res;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            using (client)
            {
                // Request KHÔNG gắn VwController vào request.Options
                var request = CreateRequest(HttpMethod.Get, "http://127.0.0.1:8080/ISAPI/Security/userCheck", controller: null);
                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(2, stub.Requests.Count);

                var authHeader = stub.Requests[1].Headers.Authorization?.Parameter;
                Assert.NotNull(authHeader);
                Assert.Contains("username=\"fallback_config_user\"", authHeader);
            }
        }

        #endregion
    }

    /// <summary>
    /// Test double cho HttpMessageHandler để mô phỏng HTTP responses và thu thập các requests đã gửi
    /// </summary>
    internal sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _handler;
        private int _callCount;

        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string?> RequestBodies { get; } = [];

        public StubHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _callCount++;
            Requests.Add(request);

            string? body = null;
            if (request.Content != null)
                body = await request.Content.ReadAsStringAsync(cancellationToken);

            RequestBodies.Add(body);

            return _handler(request, _callCount);
        }
    }
}
