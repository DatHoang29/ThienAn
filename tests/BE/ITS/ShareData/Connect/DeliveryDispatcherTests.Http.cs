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

    [Fact]
    public async Task DispatchAsync_WhenPartnerConfigEmpty_FallsBackToPartnerLegacyFields_Test()
    {
        // Arrange: Đối tác cũ không có ConnectionConfig JSON -> tự động fallback sang Address, Port, EndPointApiUrl
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var dispatcher = CreateDispatcher(http: httpSender);
        var ctx = CreateDummyContext("101", "PARTNER_LEGACY", ConnectionType.Api);
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = 18092;
        ctx.Partner!.EndPointApiUrl = "/api/v1/legacy-data";
        ctx.Partner!.ConnectionConfig = null;

        var mappingBytes = Encoding.UTF8.GetBytes("{\"legacy\": true}");
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
        Assert.Equal("/api/v1/legacy-data", server.LastRequest.Path);
    }

    [Fact]
    public async Task DispatchAsync_TakesAddressAndPortFromPartnerColumns_AndCustomConfigFromDictionary_Test()
    {
        // Arrange: Địa chỉ và cổng chỉ lấy từ cột của Partner; ConnectionConfig chỉ chứa config riêng (Auth, Timeout)
        var server = _host.PartnerServer;
        server.ResetDefaults();
        var httpSender = new HttpDeliverySender();
        var dispatcher = CreateDispatcher(http: httpSender);
        var ctx = CreateDummyContext("101", "PARTNER_COLS", "API");
        ctx.Partner!.Address = "127.0.0.1";
        ctx.Partner!.Port = 18092;
        ctx.Partner!.EndPointApiUrl = "/api/v1/override-endpoint";
        ctx.Partner!.ConnectionConfig = "{\"TimeoutSeconds\":\"30\",\"AuthType\":\"Bearer\",\"Token\":\"test-token\"}";

        var mappingBytes = Encoding.UTF8.GetBytes("{\"override\": true}");
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
        Assert.Equal("/api/v1/override-endpoint", server.LastRequest.Path);
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

    [Theory]
    [InlineData("-5")]
    [InlineData("0")]
    [InlineData("not_a_number")]
    public async Task HttpSender_WhenTimeoutInvalid_ReturnsInvalidConfigurationError_Test(string invalidTimeout)
    {
        // Arrange
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BAD_TIMEOUT", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["TimeoutSeconds"] = invalidTimeout
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains("Timeout", result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenBypassSslInvalid_ReturnsInvalidConfigurationError_Test()
    {
        // Arrange
        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_BAD_SSL", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["BypassSsl"] = "not_a_boolean"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("InvalidConfiguration", result.ErrorCode);
        Assert.Contains("BypassSsl", result.ErrorMessage);
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
    public async Task HttpSender_WhenFurionResponseHasFailedSucceeded_ReturnsBusinessError_Test()
    {
        // Arrange: Đối tác trả HTTP 200 dạng Furion/UnifyResult nhưng succeeded = false
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = "{\"succeeded\": false, \"statusCode\": 400, \"errors\": \"Dữ liệu không đúng định dạng\"}";

        var httpSender = new HttpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_FURION_ERR", "API");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                ["Address"] = "127.0.0.1",
                ["Port"] = "18092",
                ["ResponseContract"] = "Furion"
            }
        };

        // Act
        var result = await httpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("BusinessError", result.ErrorCode);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("Dữ liệu không đúng định dạng", result.ErrorMessage);
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

    [Theory]
    [InlineData("")]
    [InlineData("None")]
    public async Task HttpSender_WhenAuthTypeEmptyOrNoneWithCredentials_EmitsWarningAndSendsWithoutAuth_Test(string authType)
    {
        // Arrange
        _host.PartnerServer.ResetDefaults();
        var handler = new HttpDeliverySender();
        var ctx = CreateSendContext();
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Api,
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
    public async Task HttpSender_WhenUnsupportedAuthType_EmitsWarning_Test()
    {
        // Arrange
        _host.PartnerServer.ResetDefaults();
        var handler = new HttpDeliverySender();
        var ctx = CreateSendContext();
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Api,
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
    public async Task HttpSender_WhenBearerAuthMissingToken_EmitsWarning_Test()
    {
        // Arrange
        _host.PartnerServer.ResetDefaults();
        var handler = new HttpDeliverySender();
        var ctx = CreateSendContext();
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Api,
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
    public async Task HttpSender_WhenApiKeyAuthMissingKey_EmitsWarning_Test()
    {
        // Arrange
        _host.PartnerServer.ResetDefaults();
        var handler = new HttpDeliverySender();
        var ctx = CreateSendContext();
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Api,
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
    public async Task HttpSender_WhenBasicAuthMissingCredentials_EmitsWarning_Test()
    {
        // Arrange
        _host.PartnerServer.ResetDefaults();
        var handler = new HttpDeliverySender();
        var ctx = CreateSendContext();
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"test\": 1}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.AuthType] = "Basic",
                [ConnectKeys.Username] = "admin"
            }
        };

        // Act
        var result = await handler.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("Basic yêu cầu Username và Password"));
    }

    [Fact]
    public async Task HttpSender_WhenNoResponseContractAndSucceededFalse_ReturnsBusinessError_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}";

        var httpSender = new HttpDeliverySender();
        var ctx = CreateSendContext("PARTNER_DEFAULT_ERR");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>()
        };

        // Act
        var result = await httpSender.Send(
            request,
            ctx,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Equal("BusinessError", result.ErrorCode);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("[400] lz.exception.base.dataNotExist", result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenResponseContractHttpOnlyAndSucceededFalse_ReturnsSuccess_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}";

        var httpSender = new HttpDeliverySender();
        var ctx = CreateSendContext("PARTNER_HTTP_ONLY");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.ResponseContract] = "HttpOnly"
            }
        };

        // Act
        var result = await httpSender.Send(
            request,
            ctx,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task HttpSender_WhenResponseContractAutoAndSucceededFalse_ReturnsBusinessError_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = "{\"succeeded\":false,\"code\":400,\"message\":\"lz.exception.base.dataNotExist\"}";

        var httpSender = new HttpDeliverySender();
        var ctx = CreateSendContext("PARTNER_AUTO_CONTRACT");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.ResponseContract] = "Auto"
            }
        };

        // Act
        var result = await httpSender.Send(
            request,
            ctx,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Equal("BusinessError", result.ErrorCode);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("[400] lz.exception.base.dataNotExist", result.ErrorMessage);
    }

    [Fact]
    public async Task HttpSender_WhenNoResponseContractAndSucceededTrue_ReturnsSuccess_Test()
    {
        // Arrange
        var server = _host.PartnerServer;
        server.ResetDefaults();
        server.DefaultResponseBody = "{\"succeeded\":true}";

        var httpSender = new HttpDeliverySender();
        var ctx = CreateSendContext("PARTNER_SUCCEEDED_TRUE");
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"data\":\"test\"}",
            ConnectType = ConnectionType.Api,
            ConnectConfig = new Dictionary<string, string>()
        };

        // Act
        var result = await httpSender.Send(
            request,
            ctx,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
    }
}
