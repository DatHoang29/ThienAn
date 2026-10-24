using System.Net;
using System.Text;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Base.Models.Formats;
using Modules.Integration.Delivery.Formats;
using Modules.Integration.Delivery.Senders;

namespace Tests.ShareData.Connect;

public partial class DeliveryDispatcherTests
{
    private (string Host, int Port, string Username, string Password) GetFtpSettings()
    {
        var config = _host.Services.GetRequiredService<IConfiguration>();
        var host = config["Ftp:Host"] ?? "127.0.0.1";
        var port = int.TryParse(config["Ftp:Port"], out var p) ? p : 2121;
        var username = config["Ftp:Username"] ?? "tactest";
        var password = config["Ftp:Password"] ?? "Tac@@1234";
        return (host, port, username, password);
    }

    private static void EnsureFtpDirectoryExists(string host, int port, string username, string password, string remotePath)
    {
#pragma warning disable SYSLIB0014
        var parts = remotePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = "";
        foreach (var part in parts)
        {
            current += "/" + part;
            try
            {
                var req = (FtpWebRequest)WebRequest.Create(new Uri($"ftp://{host}:{port}{current}"));
                req.Method = WebRequestMethods.Ftp.MakeDirectory;
                req.Credentials = new NetworkCredential(username, password);
                req.Timeout = 5000;
                using var resp = (FtpWebResponse)req.GetResponse();
            }
            catch (WebException ex) when (ex.Response is FtpWebResponse ftpResp &&
                (ftpResp.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable ||
                 ftpResp.StatusCode == FtpStatusCode.FileActionAborted ||
                 (int)ftpResp.StatusCode == 550))
            {
                // Thư mục đã tồn tại, tiếp tục
            }
        }
#pragma warning restore SYSLIB0014
    }

    private static byte[] DownloadFtpFile(string ftpUri, string username, string password)
    {
#pragma warning disable SYSLIB0014
        var req = (FtpWebRequest)WebRequest.Create(new Uri(ftpUri));
        req.Method = WebRequestMethods.Ftp.DownloadFile;
        req.Credentials = new NetworkCredential(username, password);
        req.UseBinary = true;
        req.KeepAlive = false;
        using var resp = (FtpWebResponse)req.GetResponse();
        using var stream = resp.GetResponseStream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
#pragma warning restore SYSLIB0014
    }

    [Fact]
    public async Task Ftp_Send_WhenCsvPayloadBytesProvided_UploadsSuccessfullyAndCanBeDownloaded_Test()
    {
        // Arrange
        var (host, port, user, pass) = GetFtpSettings();
        var remotePath = $"/sharedata-test/{Guid.NewGuid():N}";
        EnsureFtpDirectoryExists(host, port, user, pass, remotePath);

        var ftpSender = new FtpDeliverySender();
        var ctx = CreateSendContext(partnerCode: "PARTNER_FTP_REAL", connectionType: "FTP");
        ctx = ctx with { Address = host, Port = port };

        var csvContent = "ID,Code,Name,Status\r\n101,A1,Device1,Active\r\n102,B2,Device2,Inactive\r\n";
        var csvBytes = Encoding.UTF8.GetBytes(csvContent);
        var request = new DeliveryRequest
        {
            JsonPayload = "{\"ignored\": true}",
            PayloadBytes = csvBytes,
            ContentType = "text/csv",
            FileExtension = ".csv",
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.Username] = user,
                [ConnectKeys.Password] = pass,
                [ConnectKeys.RemotePath] = remotePath
            }
        };

        // Act
        var result = await ftpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Gửi tới FTP thất bại (Error: {result.ErrorCode} - {result.ErrorMessage}). Hãy đảm bảo Docker service tac-ftp đang chạy bằng lệnh: docker compose -f Docker/Compose/docker-compose.yml up -d tac-ftp");
        Assert.Equal("Ftp", result.Channel);
        Assert.Equal(csvBytes.Length, result.SentByteSize);
        Assert.NotNull(result.TargetAddressOrPath);
        Assert.EndsWith(".csv", result.TargetAddressOrPath);
        Assert.Contains(remotePath, result.TargetAddressOrPath);

        var downloadedBytes = DownloadFtpFile(result.TargetAddressOrPath, user, pass);
        Assert.Equal(csvBytes, downloadedBytes);
    }

    [Fact]
    public async Task Ftp_Send_WhenWrongPassword_ReturnsFtpWebException_Test()
    {
        // Arrange
        var (host, port, user, _) = GetFtpSettings();
        var remotePath = $"/sharedata-test/{Guid.NewGuid():N}";
        var wrongPassword = "TotallyWrongPassword@@@";

        var ftpSender = new FtpDeliverySender();
        var ctx = CreateSendContext(partnerCode: "PARTNER_FTP_BAD_PASS", connectionType: "FTP");
        ctx = ctx with { Address = host, Port = port };

        var request = new DeliveryRequest
        {
            JsonPayload = "[{\"id\": 1}]",
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.Username] = user,
                [ConnectKeys.Password] = wrongPassword,
                [ConnectKeys.RemotePath] = remotePath
            }
        };

        // Act
        var result = await ftpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Ftp", result.Channel);
        Assert.Equal("FtpWebException", result.ErrorCode);
    }

    [Fact]
    public async Task Ftp_Send_WhenCsvIncludeBomIsTrue_UploadedFileStartsWithUtf8Bom_Test()
    {
        // Arrange
        var (host, port, user, pass) = GetFtpSettings();
        var remotePath = $"/sharedata-test/{Guid.NewGuid():N}";
        EnsureFtpDirectoryExists(host, port, user, pass, remotePath);

        var ftpSender = new FtpDeliverySender();
        var ctx = CreateSendContext(partnerCode: "PARTNER_FTP_BOM", connectionType: "FTP");
        ctx = ctx with { Address = host, Port = port };

        var jsonPayload = "[{\"ID\": 1, \"Name\": \"Thiết bị kiểm thử\"}]";
        var formatCtx = new FormatContext(
            ctx.PartnerCode,
            ctx.PacketCode,
            ctx.SerialNbr,
            ctx.ExportedAt,
            new Dictionary<string, string> { [ConnectKeys.CsvIncludeBom] = "true" });

        var transport = new CsvFormat().ToTransport(jsonPayload, formatCtx);

        var request = new DeliveryRequest
        {
            JsonPayload = jsonPayload,
            PayloadBytes = transport.Bytes,
            ContentType = transport.ContentType,
            FileExtension = transport.FileExtension,
            ConnectType = ConnectionType.Ftp,
            ConnectConfig = new Dictionary<string, string>
            {
                [ConnectKeys.Username] = user,
                [ConnectKeys.Password] = pass,
                [ConnectKeys.RemotePath] = remotePath,
                [ConnectKeys.CsvIncludeBom] = "true"
            }
        };

        // Act
        var result = await ftpSender.Send(request, ctx, CancellationToken.None);

        // Assert
        Assert.True(result.Success, $"Gửi tới FTP thất bại (Error: {result.ErrorCode} - {result.ErrorMessage}). Hãy đảm bảo Docker service tac-ftp đang chạy bằng lệnh: docker compose -f Docker/Compose/docker-compose.yml up -d tac-ftp");
        Assert.NotNull(result.TargetAddressOrPath);

        var downloadedBytes = DownloadFtpFile(result.TargetAddressOrPath, user, pass);
        Assert.True(downloadedBytes.Length >= 3, "Tệp tải về quá ngắn để chứa UTF-8 BOM");
        Assert.Equal(0xEF, downloadedBytes[0]);
        Assert.Equal(0xBB, downloadedBytes[1]);
        Assert.Equal(0xBF, downloadedBytes[2]);
    }
}
