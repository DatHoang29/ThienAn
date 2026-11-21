using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Base.Enums;

namespace Modules.Integration.Delivery.Senders
{
    public sealed record FtpConfig(
        string Host,
        int Port,
        string? RemotePath,
        bool UseSsl,
        int TimeoutSeconds,
        string? Username,
        string? Password,
        bool IsEnabled,
        string Format,
        bool IncludeBom);

    /// <summary>
    /// Description: Kênh xuất bản dữ liệu qua giao thức FTP / FTPS.
    ///              Hỗ trợ xuất file JSON hoặc CSV (chuẩn RFC 4180 CRLF) lên máy chủ FTP đối tác.
    /// Created date: 14/10/2026
    /// Modified date: 19/10/2026 (Gỡ IDeliverySender)
    /// </summary>
    public class FtpDeliverySender(ILogger<FtpDeliverySender>? logger = null)
    {
        public ConnectionType ConnectType => ConnectionType.Ftp;

        /// <summary>
        /// Hàm thuần đọc và chuẩn hoá cấu hình kết nối FTP từ Dictionary và DeliveryContext.
        /// Bảo đảm không Trim() mật khẩu và hỗ trợ đầy đủ UseSsl, TimeoutSeconds, CsvIncludeBom.
        /// </summary>
        public static FtpConfig ReadFtpConfig(
            IDictionary<string, string> config,
            DeliveryContext? ctx,
            List<string>? warnings = null)
        {
            var reader = new ConnectConfigReader(config);

            var isEnabled = true;
            var enabledStr = reader.GetString(ConnectKeys.Enabled);
            if (!string.IsNullOrWhiteSpace(enabledStr))
            {
                if (bool.TryParse(enabledStr, out var parsedEnabled))
                {
                    isEnabled = parsedEnabled;
                }
                else
                {
                    warnings?.Add($"Cấu hình Enabled [{enabledStr}] không phải boolean hợp lệ, dùng mặc định true.");
                }
            }

            var host = ctx?.Address?.Trim() ?? string.Empty;
            var port = ctx?.Port ?? 21;

            var remotePath = reader.GetString(ConnectKeys.RemotePath)?.TrimEnd('/') ?? string.Empty;

            var useSsl = false;
            var useSslStr = reader.GetString(ConnectKeys.UseSsl, ConnectKeys.UseSslAliases);
            if (!string.IsNullOrWhiteSpace(useSslStr))
            {
                if (bool.TryParse(useSslStr, out var parsedUseSsl))
                {
                    useSsl = parsedUseSsl;
                }
                else
                {
                    warnings?.Add($"Cấu hình UseSsl [{useSslStr}] không phải boolean hợp lệ, dùng mặc định false.");
                }
            }

            var timeoutSeconds = 15;
            var timeoutStr = reader.GetString(ConnectKeys.TimeoutSeconds, ConnectKeys.TimeoutSecondsAliases);
            if (!string.IsNullOrWhiteSpace(timeoutStr))
            {
                if (!int.TryParse(timeoutStr, out var parsedTimeout))
                {
                    warnings?.Add($"Cấu hình Timeout [{timeoutStr}] không phải số nguyên hợp lệ, dùng mặc định 15 giây.");
                }
                else if (parsedTimeout < 1 || parsedTimeout > 120)
                {
                    timeoutSeconds = Math.Clamp(parsedTimeout, 1, 120);
                    warnings?.Add($"Cấu hình Timeout [{timeoutStr}] vượt ngưỡng cho phép (1-120 giây), tự động điều chỉnh về {timeoutSeconds} giây.");
                }
                else
                {
                    timeoutSeconds = parsedTimeout;
                }
            }

            var username = reader.GetRawString(
                ConnectKeys.Username,
                defaultValue: string.Empty,
                trim: true,
                ConnectKeys.UsernameAliases)!;
            // Tuyệt đối không Trim() password
            var password = reader.GetRawString(
                ConnectKeys.Password,
                defaultValue: string.Empty,
                trim: false,
                ConnectKeys.PasswordAliases)!;

            var format = reader.GetString(ConnectKeys.Format) ?? "JSON";

            var includeBom = false;
            var includeBomStr = reader.GetString(ConnectKeys.CsvIncludeBom);
            if (!string.IsNullOrWhiteSpace(includeBomStr))
            {
                if (bool.TryParse(includeBomStr, out var parsedBom))
                {
                    includeBom = parsedBom;
                }
                else
                {
                    warnings?.Add($"Cấu hình CsvIncludeBom [{includeBomStr}] không phải boolean hợp lệ, dùng mặc định false.");
                }
            }

            return new FtpConfig(
                host,
                port,
                remotePath,
                useSsl,
                timeoutSeconds,
                username,
                password,
                isEnabled,
                format,
                includeBom);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tải gói tin lên máy chủ FTP hoặc FTPS bằng FtpWebRequest, API .NET đã lỗi thời nên có tắt cảnh báo SYSLIB0014.
        ///              Kênh FTPS tự bật SSL. Tên tệp lấy theo khóa FileName, không có thì tự sinh theo mã gói, thời gian, số thứ tự. Enabled = false trả lỗi Disabled.
        /// Created date: 14/10/2026
        /// </summary>
        public virtual async Task<DeliveryResult> Send(DeliveryRequest request, DeliveryContext ctx, CancellationToken ct)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(request.JsonPayload))
            {
                logger?.LogWarning("❌ [FTP] Payload dữ liệu rỗng, không thể tải lên.");
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Ftp",
                    ErrorCode = "EmptyPayload",
                    ErrorMessage = "Payload dữ liệu rỗng.",
                    Warnings = warnings
                };
            }

            var ftpConfig = ReadFtpConfig(request.ConnectConfig, ctx, warnings);
            if (request.ConnectType == ConnectionType.Ftps && !ftpConfig.UseSsl)
            {
                ftpConfig = ftpConfig with { UseSsl = true };
            }

            if (!ftpConfig.IsEnabled)
            {
                logger?.LogInformation("ℹ️ [FTP] Kênh FTP của đối tác [{Partner}] đang bị vô hiệu hóa (Enabled = false).", ctx.PartnerCode ?? "Unknown");
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Ftp",
                    ErrorCode = "Disabled",
                    ErrorMessage = $"Kênh FTP của đối tác [{ctx.PartnerCode}] đang bị vô hiệu hóa (Enabled = false).",
                    Warnings = warnings
                };
            }

            if (string.IsNullOrWhiteSpace(ftpConfig.Host))
            {
                logger?.LogWarning("❌ [FTP] Thiếu thông tin máy chủ Host cho kết nối FTP của đối tác [{Partner}].", ctx.PartnerCode ?? "Unknown");
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Ftp",
                    ErrorCode = "MissingFtpHost",
                    ErrorMessage = "Thiếu thông tin máy chủ Host cho kết nối FTP.",
                    Warnings = warnings
                };
            }

            var isCsv = ftpConfig.Format.Equals("CSV", StringComparison.OrdinalIgnoreCase);
            var contentBytes = request.PayloadBytes ?? Encoding.UTF8.GetBytes(request.JsonPayload);

            var ext = !string.IsNullOrWhiteSpace(request.FileExtension)
                ? request.FileExtension.TrimStart('.')
                : (isCsv ? "csv" : "json");
            var reader = new ConnectConfigReader(request.ConnectConfig);
            var fileName = reader.GetString(ConnectKeys.FileName)
                ?? $"{ctx.PacketCode ?? "packet"}_{DateTime.Now:yyyyMMddHHmmss}_{ctx.SerialNbr?.ToString() ?? Guid.NewGuid().ToString("N")[..8]}.{ext}";

            var scheme = "ftp";
            var ftpUriString = string.IsNullOrWhiteSpace(ftpConfig.RemotePath)
                ? $"{scheme}://{ftpConfig.Host}:{ftpConfig.Port}/{fileName}"
                : $"{scheme}://{ftpConfig.Host}:{ftpConfig.Port}/{ftpConfig.RemotePath}/{fileName}";

            logger?.LogInformation("➡️ [FTP] Đang tải lên tập tin [{FileName}] ({ByteSize} bytes, SSL: {UseSsl}) tới [{FtpUri}]",
                fileName, contentBytes.Length, ftpConfig.UseSsl, ftpUriString);

            try
            {
#pragma warning disable SYSLIB0014 // FtpWebRequest is obsolete
                var ftpRequest = (FtpWebRequest)WebRequest.Create(new Uri(ftpUriString));
                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                if (!string.IsNullOrWhiteSpace(ftpConfig.Username))
                {
                    ftpRequest.Credentials = new NetworkCredential(ftpConfig.Username, ftpConfig.Password);
                }
                ftpRequest.EnableSsl = ftpConfig.UseSsl;
                ftpRequest.Timeout = ftpConfig.TimeoutSeconds * 1000;
                ftpRequest.UseBinary = true;
                ftpRequest.KeepAlive = false;

                using (var reqStream = await ftpRequest.GetRequestStreamAsync())
                {
                    await reqStream.WriteAsync(contentBytes, ct);
                }

                using var response = (FtpWebResponse)await ftpRequest.GetResponseAsync();
                logger?.LogInformation("✅ [FTP] Tải lên thành công: {StatusDescription}", response.StatusDescription);
#pragma warning restore SYSLIB0014

                return new DeliveryResult
                {
                    Success = true,
                    Channel = "Ftp",
                    SentByteSize = contentBytes.Length,
                    TargetAddressOrPath = ftpUriString,
                    DetailJson = $"{{\"uri\":\"{ftpUriString}\",\"format\":\"{ext.ToUpper()}\",\"bytes\":{contentBytes.Length},\"ssl\":{ftpConfig.UseSsl.ToString().ToLower()}}}",
                    Warnings = warnings
                };
            }
            catch (WebException wEx)
            {
                logger?.LogError(wEx, "❌ [FTP] Lỗi WebException khi tải lên FTP: {Message}", wEx.Message);
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Ftp",
                    ErrorCode = "FtpWebException",
                    ErrorMessage = $"Lỗi kết nối FTP: {wEx.Message}",
                    TargetAddressOrPath = ftpUriString,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "❌ [FTP] Ngoại lệ khi tải lên FTP: {Message}", ex.Message);
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "Ftp",
                    ErrorCode = "FtpUploadError",
                    ErrorMessage = $"Ngoại lệ tải lên FTP: {ex.Message}",
                    TargetAddressOrPath = ftpUriString,
                    Warnings = warnings
                };
            }
        }
    }
}
