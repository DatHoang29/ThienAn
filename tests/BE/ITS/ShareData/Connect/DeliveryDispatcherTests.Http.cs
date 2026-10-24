using System.Text;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Senders;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;

namespace Tests.ShareData.Connect;

public partial class DeliveryDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_WhenPartnerConnectionTypeIsApi_RoutesToApiChannel_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var dispatcher = CreateDispatcher(http: httpSender);
        var ctx = CreateDummyContext("101", "PARTNER_API", ConnectionType.Api);
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = 18092;
        ctx.Partner!.EndPointApiUrl = "/api/v1/data";

        var jsonPayload = "{\"data\": \"ok\"}";
        var request = new DeliveryRequest
        {
            JsonPayload = jsonPayload,
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["HttpTimeoutSec"] = "10"
            }
        };

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("API", result.Channel);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(Encoding.UTF8.GetByteCount(jsonPayload), result.SentByteSize);
    }

    /// <summary>
    /// Description: Kiểm thử Dispatcher lấy Address/Port từ cột đối tác và fallback hoặc nạp cấu hình bổ sung từ ConnectionConfig.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData(null, "/api/v1/legacy-data")]
    [InlineData("{\"TimeoutSeconds\":\"30\",\"AuthType\":\"Bearer\",\"Token\":\"test-token\"}", "/api/v1/override-endpoint")]
    public async Task DispatchAsync_WhenPartnerConfigProvidedOrNull_TakesAddressAndPortFromPartnerColumns_Test(string? connectionConfig, string endpointUrl)
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var dispatcher = CreateDispatcher(http: httpSender);
        var ctx = CreateDummyContext("101", "PARTNER_COLS", ConnectionType.Api);
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = 18092;
        ctx.Partner!.EndPointApiUrl = endpointUrl;
        ctx.Partner!.ConnectionConfig = connectionConfig;

        var mappingBytes = Encoding.UTF8.GetBytes("{\"test\": true}");
        var mapping = new DataMappingResult(true, mappingBytes, 1);
        var request = DataOutboundTransport.BuildRequest(mapping, ctx);

        // Act
        var result = await dispatcher.Dispatch(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("API", result.Channel);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(server.LastRequest);
        Assert.Equal(18092, server.LastRequest!.Port);
        Assert.Equal(endpointUrl, server.LastRequest.Path);
    }

    [Theory]
    [InlineData(70000)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HttpSender_WhenPortInvalid_ReturnsInvalidConfigurationError_Test(int invalidPort)
    {
        // Arrange
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BAD_PORT", "API");
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = invalidPort;
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>()
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains("Port", result.ErrorMessage);
    }

    /// <summary>
    /// Description: Kiểm thử HttpDeliverySender từ chối cấu hình TimeoutSeconds hoặc BypassSsl không hợp lệ.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("TimeoutSeconds", "-5", "Timeout")]
    [InlineData("TimeoutSeconds", "0", "Timeout")]
    [InlineData("TimeoutSeconds", "not_a_number", "Timeout")]
    [InlineData("BypassSsl", "not_a_boolean", "BypassSsl")]
    public async Task HttpSender_WhenConfigValueInvalid_ReturnsInvalidConfigurationError_Test(string configKey, string invalidValue, string expectedErrorKeyword)
    {
        // Arrange
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BAD_CONFIG", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                [configKey] = invalidValue
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains(expectedErrorKeyword, result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenBearerAuthProvided_SendsAuthorizationBearerHeader_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BEARER", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["AuthType"] = "Bearer",
                ["Token"] = "token_jwt_secret_xyz"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(server.LastRequest);
        Assert.True(server.LastRequest!.Headers.ContainsKey("Authorization"));
        Assert.Equal("Bearer token_jwt_secret_xyz", server.LastRequest.Headers["Authorization"]);
    }

    [Fact]
    public async Task HttpSender_WhenApiKeyAuthProvided_SendsApiKeyHeader_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_APIKEY", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["AuthType"] = "ApiKey",
                ["ApiKey"] = "demo-api-key-999",
                ["ApiKeyHeader"] = "X-Integration-Key"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(server.LastRequest);
        Assert.True(server.LastRequest!.Headers.ContainsKey("X-Integration-Key"));
        Assert.Equal("demo-api-key-999", server.LastRequest.Headers["X-Integration-Key"]);
    }

    [Fact]
    public async Task HttpSender_WhenBasicAuthProvided_SendsAuthorizationBasicHeader_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BASIC", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["AuthType"] = "Basic",
                ["Username"] = "itsadmin",
                ["Password"] = "P@ssw0rd!"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(server.LastRequest);
        Assert.True(server.LastRequest!.Headers.ContainsKey("Authorization"));
        var expectedBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("itsadmin:P@ssw0rd!"));
        Assert.Equal("Basic " + expectedBase64, server.LastRequest.Headers["Authorization"]);
    }

    [Fact]
    public async Task HttpSender_WhenHttp200ReturnsBusinessError_ReturnsFailedWithBusinessError_Test()
    {
        // Arrange: Đối tác trả HTTP 200 nhưng body chứa lỗi nghiệp vụ (isSuccess = false)
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = "{\"isSuccess\": false, \"errorCode\": \"ERR_PAYLOAD_INVALID\", \"message\": \"Payload schema mismatch\"}";

        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BIZ_ERR", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["SuccessField"] = "isSuccess",
                ["ErrorCodeField"] = "errorCode",
                ["ErrorMessageField"] = "message"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("BusinessError", result.ErrorCode);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("Payload schema mismatch", result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenServerDropsConnection_ReturnsProtocolMismatchHint_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.AbortOnCall.Add(1);

        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_ABORT", "API");
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = ShareDataPartnerServerMock.DefaultPort;
        ctx.Partner!.EndPointApiUrl = "/api/v1/data";

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = ShareDataPartnerServerMock.DefaultPort.ToString(),
                ["HttpTimeoutSec"] = "5"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("đóng kết nối", result.ErrorMessage);
        Assert.Contains("cổng", result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenPayloadBytesProvided_SendsExactBytesAndContentType_Test()
    {
        var server = _host.PartnerServer;
        server.ResetDefaults();

        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BYTES", "API");
        var expectedBytes = Encoding.UTF8.GetBytes("{\"cipher\":\"encrypted_payload_123\"}");
        var request = new DeliveryRequest
        {
            PayloadBytes = expectedBytes,
            ContentType = "application/json",
            JsonPayload = "dummy_should_not_be_used",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = ShareDataPartnerServerMock.DefaultHost,
                ["Port"] = ShareDataPartnerServerMock.DefaultPort.ToString()
            }
        };

        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(server.LastRequest);
        Assert.Equal(expectedBytes, Encoding.UTF8.GetBytes(server.LastRequest.Body));
    }

    /// <summary>
    /// Description: Kiểm thử HttpDeliverySender phát hiện và ghi cảnh báo khi cấu hình xác thực chưa đủ trường hoặc không hỗ trợ.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("", "stray-token", null, "stray-user", null, "Cấu hình phương thức xác thực rỗng hoặc None")]
    [InlineData("None", "stray-token", null, "stray-user", null, "Cấu hình phương thức xác thực rỗng hoặc None")]
    [InlineData("OAuth3Custom", "token-123", null, null, null, "AuthType [OAuth3Custom] không được hỗ trợ")]
    [InlineData("Bearer", "", null, null, null, "Bearer yêu cầu Token")]
    [InlineData("ApiKey", null, null, null, null, "ApiKey yêu cầu ApiKey")]
    [InlineData("Basic", null, null, "admin", null, "Basic yêu cầu Username và Password")]
    public async Task HttpSender_WhenAuthConfigIncompleteOrUnsupported_EmitsExpectedWarning_Test(
        string authType,
        string? token,
        string? apiKey,
        string? username,
        string? password,
        string expectedWarningSubstring)
    {
        // Arrange
        _host.PartnerServer.ResetDefaults();
        var handler = new HttpDeliverySender();
        var ctx = CreateSendContext();
        var config = new Dictionary<string, string> { [ConnectKeys.AuthType] = authType };
        if (token != null) config[ConnectKeys.Token] = token;
        if (apiKey != null) config[ConnectKeys.ApiKey] = apiKey;
        if (username != null) config[ConnectKeys.Username] = username;
        if (password != null) config[ConnectKeys.Password] = password;

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = config
        };

        // Act
        var result = await handler.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains(expectedWarningSubstring));
        if (authType == "" || authType == "None")
        {
            Assert.False(_host.PartnerServer.LastRequest!.Headers.ContainsKey("Authorization"));
        }
    }

    /// <summary>
    /// Description: Kiểm thử HttpDeliverySender xử lý ResponseContract (Auto, HttpOnly, Furion, Default) theo body phản hồi của đối tác.
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("HttpOnly", "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}", true, null, null)]
    [InlineData("Auto", "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}", false, "BusinessError", "lz.exception.base.dataNotExist")]
    [InlineData(null, "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}", false, "BusinessError", "lz.exception.base.dataNotExist")]
    [InlineData(null, "{\"succeeded\":true}", true, null, null)]
    [InlineData("Furion", "{\"succeeded\": false, \"statusCode\": 400, \"errors\": \"Dữ liệu không đúng định dạng\"}", false, "BusinessError", "Dữ liệu không đúng định dạng")]
    public async Task HttpSender_WhenResponseContractConfigured_ProcessesResultAsExpected_Test(
        string? contract,
        string responseBody,
        bool expectedSuccess,
        string? expectedErrorCode,
        string? expectedMessageSubstring)
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = responseBody;

        var httpSender = new HttpDeliverySender();
        var ctx = CreateSendContext("PARTNER_CONTRACT_TEST");
        var config = new Dictionary<string, string>();
        if (contract != null)
        {
            config[ConnectKeys.ResponseContract] = contract;
        }

        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = config
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        Assert.Equal(200, result.StatusCode);
        if (expectedErrorCode != null)
        {
            Assert.Equal(expectedErrorCode, result.ErrorCode);
        }
        if (expectedMessageSubstring != null)
        {
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains(expectedMessageSubstring, result.ErrorMessage);
        }
    }
}
