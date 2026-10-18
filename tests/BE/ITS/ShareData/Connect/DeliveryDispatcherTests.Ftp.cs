using System.Text;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Interfaces;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Senders;
using Shared.DTO.Enums;
using Xunit;

namespace Tests.ShareData.Connect;

public partial class DeliveryDispatcherTests
{
    [Fact]
    public async Task FtpSender_WhenDisabledOrMissingHost_ReturnsClearError_Test()
    {
        // Arrange
        var ftpSender = new FtpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_FTP", ConnectionType.Ftp);

        var disabledReq = new DeliveryRequest
        {
            JsonPayload = "[{\"id\": 1}]",
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string> { ["Enabled"] = "false" }
        };

        var missingHostReq = new DeliveryRequest
        {
            JsonPayload = "[{\"id\": 1}]",
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string> { ["Enabled"] = "true" }
        };

        // Act
        var disabledRes = await ftpSender.Send(disabledReq, ctx, CancellationToken.None);
        if (ctx.Partner != null) ctx.Partner.Address = null;
        var missingHostRes = await ftpSender.Send(missingHostReq, ctx, CancellationToken.None);

        // Assert
        Assert.False(disabledRes.Success);
        Assert.Equal("Disabled", disabledRes.ErrorCode);

        Assert.False(missingHostRes.Success);
        Assert.Equal("MissingFtpHost", missingHostRes.ErrorCode);
    }

    [Fact]
    public async Task FtpSender_WhenCsvPayloadBytesProvided_UsesExactPayloadBytesAndCsvExtension_Test()
    {
        var ftpSender = new FtpDeliverySender();
        var ctx = CreateDummyContext("101", "PARTNER_FTP_CSV", ConnectionType.Ftp);
        var csvBytes = Encoding.UTF8.GetBytes("ID,Code,Name\r\n1,A,Item1\r\n2,B,Item2");
        var request = new DeliveryRequest
        {
            PayloadBytes = csvBytes,
            ContentType = "text/csv",
            FileExtension = ".csv",
            JsonPayload = "{}",
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string>
            {
                ["RemotePath"] = "/test/path"
            }
        };

        var result = await ftpSender.Send(request, ctx, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Ftp", result.Channel);
        Assert.Contains(result.ErrorCode, new[] { "FtpConnectionFailed", "FtpSendFailed", "FtpWebException" });
    }

    [Fact]
    public void Ftp_ReadFtpConfig_WhenTimeoutNotInt_EmitsWarningAndDefaultsTo15_Test()
    {
        // Arrange
        var config = new Dictionary<string, string> { [ConnectKeys.TimeoutSeconds] = "invalid_timeout" };
        var warnings = new List<string>();

        // Act
        var ftpConfig = FtpDeliverySender.ReadFtpConfig(config, null, warnings);

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
        var ftpConfig = FtpDeliverySender.ReadFtpConfig(config, null, warnings);

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
        var ftpConfig = FtpDeliverySender.ReadFtpConfig(config, null, warnings);

        // Assert
        Assert.True(ftpConfig.IsEnabled);
        Assert.False(ftpConfig.UseSsl);
        Assert.False(ftpConfig.IncludeBom);
        Assert.Equal(3, warnings.Count);
        Assert.Contains(warnings, w => w.Contains("Enabled"));
        Assert.Contains(warnings, w => w.Contains("UseSsl"));
        Assert.Contains(warnings, w => w.Contains("CsvIncludeBom"));
    }
}
