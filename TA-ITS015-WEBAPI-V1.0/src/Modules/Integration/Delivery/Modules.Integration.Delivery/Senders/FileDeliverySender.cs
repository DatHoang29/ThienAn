using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Base.Enums;

namespace Modules.Integration.Delivery.Senders
{
    /// <summary>
    /// Kênh vận chuyển xuất bản dữ liệu xuống tập tin trên đĩa / NAS.
    /// Created date: 16/09/2026
    /// Modified date: 19/10/2026 (Gỡ IDeliverySender)
    /// </summary>
    public class FileDeliverySender
    {
        public ConnectionType ConnectType => ConnectionType.File;

        private readonly string _defaultBasePath;
        private readonly ILogger<FileDeliverySender> _logger;

        public FileDeliverySender(
            IConfiguration config,
            ILogger<FileDeliverySender> logger)
        {
            var basePath = config?.GetValue<string>(ExportPathHelper.BasePathConfigKey);
            _defaultBasePath = !string.IsNullOrWhiteSpace(basePath) ? basePath : ExportPathHelper.DefaultBasePath;
            _logger = logger;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghi gói tin thành tệp trên đĩa hoặc NAS. Thư mục tùy chỉnh phải nằm trong thư mục gốc cho phép, chặn đường dẫn có "..".
        ///              Mẫu tên FileNamePattern không được chứa đường dẫn; thiếu {serial} và {timestamp} thì chỉ cảnh báo. Có khóa RawConfig (cấu hình JSON hỏng) thì trả lỗi InvalidConnectionConfig.
        /// Created date: 16/09/2026
        /// </summary>
        public virtual async Task<DeliveryResult> Send(DeliveryRequest request, DeliveryContext ctx, CancellationToken ct)
        {
            var warnings = new List<string>();

            if (request.ConnectConfig != null && request.ConnectConfig.ContainsKey(ConnectKeys.RawConfig))
            {
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "File",
                    ErrorCode = "InvalidConnectionConfig",
                    ErrorMessage = "ConnectionConfig của đối tác không phải JSON Dictionary hợp lệ.",
                    Warnings = warnings
                };
            }

            var payloadBytes = request.PayloadBytes ?? (string.IsNullOrWhiteSpace(request.JsonPayload) ? [] : Encoding.UTF8.GetBytes(request.JsonPayload));
            if (payloadBytes.Length == 0)
            {
                return new DeliveryResult
                {
                    Success = true,
                    Channel = "File",
                    SentByteSize = 0,
                    Warnings = warnings
                };
            }

            var vendorCode = ctx.PartnerCode ?? ExportPathHelper.UnknownConstant;
            var dataTypeId = ExportPathHelper.ResolveDatatypeFolderName(ctx.DatatypeId);
            var extension = !string.IsNullOrWhiteSpace(request.FileExtension) ? request.FileExtension : ".json";

            var reader = new ConnectConfigReader(request.ConnectConfig);
            var customDirectory = reader.GetString(ConnectKeys.DirectoryPath, ConnectKeys.FileDirectoryAliases);
            var fileNamePattern = reader.GetString(ConnectKeys.FileNamePattern);

            var defaultRoot = ExportPathHelper.ResolveRootPath(_defaultBasePath);

            if (!string.IsNullOrWhiteSpace(customDirectory))
            {
                if (customDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || customDirectory.Contains(".."))
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "File",
                        ErrorCode = "InvalidConfiguration",
                        ErrorMessage = $"Cấu hình đường dẫn thư mục [{customDirectory}] chứa ký tự không hợp lệ hoặc vượt ra ngoài thư mục cho phép (path traversal '..').",
                        Warnings = warnings
                    };
                }

                var fullCustomDir = Path.GetFullPath(Path.IsPathRooted(customDirectory)
                    ? customDirectory
                    : Path.Combine(defaultRoot, customDirectory));

                var normalizedRoot = defaultRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var isUnderAllowedRoot = fullCustomDir.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                                         fullCustomDir.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

                if (!isUnderAllowedRoot)
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "File",
                        ErrorCode = "InvalidConfiguration",
                        ErrorMessage = $"Cấu hình đường dẫn thư mục [{customDirectory}] không nằm trong thư mục gốc cho phép của hệ thống [{_defaultBasePath}].",
                        Warnings = warnings
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(fileNamePattern))
            {
                if (fileNamePattern.Contains('/') || fileNamePattern.Contains('\\') || fileNamePattern.Contains("..") || fileNamePattern.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "File",
                        ErrorCode = "InvalidConfiguration",
                        ErrorMessage = $"Cấu hình mẫu tên tệp [{fileNamePattern}] chứa ký tự không hợp lệ hoặc đường dẫn (path traversal).",
                        Warnings = warnings
                    };
                }

                if (!FileNameTokens.HasRequiredToken(fileNamePattern))
                {
                    warnings.Add($"Cấu hình FileNamePattern [{fileNamePattern}] thiếu tham số biến đổi {{serial}} và {{timestamp}}.");
                }
            }

            string targetPath;
            if (!string.IsNullOrWhiteSpace(customDirectory))
            {
                var cleanExt = extension.StartsWith('.') ? extension : $".{extension}";
                string fileName;
                if (!string.IsNullOrWhiteSpace(fileNamePattern))
                {
                    var resolvedPattern = fileNamePattern
                        .Replace(FileNameTokens.Packet, ctx.PacketCode ?? "packet", StringComparison.OrdinalIgnoreCase)
                        .Replace(FileNameTokens.Partner, vendorCode, StringComparison.OrdinalIgnoreCase)
                        .Replace(FileNameTokens.Timestamp, ctx.ExportedAt.ToString("yyyyMMddHHmmss"), StringComparison.OrdinalIgnoreCase)
                        .Replace(FileNameTokens.Serial, (ctx.SerialNbr?.ToString() ?? "0"), StringComparison.OrdinalIgnoreCase);

                    fileName = resolvedPattern.EndsWith(cleanExt, StringComparison.OrdinalIgnoreCase)
                        ? resolvedPattern
                        : $"{resolvedPattern}{cleanExt}";
                }
                else
                {
                    fileName = $"{dataTypeId}_{ctx.ExportedAt:yyyyMMddHHmmss}_{ExportPathHelper.ResolveFileDiscriminator(ctx.SerialNbr, ctx.SubscriptionId)}{cleanExt}";
                }

                if (string.IsNullOrWhiteSpace(fileName) ||
                    Path.GetFileName(fileName) != fileName ||
                    fileName.Contains('/') ||
                    fileName.Contains('\\') ||
                    fileName.Contains("..") ||
                    fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "File",
                        ErrorCode = "InvalidConfiguration",
                        ErrorMessage = $"Tên tệp sau khi phân giải [{fileName}] không phải tên tệp thuần hợp lệ hoặc chứa ký tự phân tách đường dẫn.",
                        Warnings = warnings
                    };
                }

                targetPath = Path.Combine(customDirectory, fileName);

                var fullCustomDir = Path.GetFullPath(Path.IsPathRooted(customDirectory)
                    ? customDirectory
                    : Path.Combine(defaultRoot, customDirectory));
                var fullTargetPath = Path.GetFullPath(Path.IsPathRooted(targetPath)
                    ? targetPath
                    : Path.Combine(defaultRoot, targetPath));

                var normalizedCustomDir = fullCustomDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!fullTargetPath.StartsWith(normalizedCustomDir, StringComparison.OrdinalIgnoreCase) && !fullTargetPath.Equals(fullCustomDir, StringComparison.OrdinalIgnoreCase))
                {
                    return new DeliveryResult
                    {
                        Success = false,
                        Channel = "File",
                        ErrorCode = "InvalidConfiguration",
                        ErrorMessage = $"Đường dẫn tệp kết xuất [{targetPath}] vượt ra ngoài thư mục cấu hình [{customDirectory}].",
                        Warnings = warnings
                    };
                }
            }
            else
            {
                targetPath = ExportPathHelper.GenerateExportRelativePath(
                    vendorCode,
                    dataTypeId,
                    ctx.ExportedAt,
                    ExportPathHelper.ResolveFileDiscriminator(ctx.SerialNbr, ctx.SubscriptionId),
                    extension);
            }

            try
            {
                await ExportPathHelper.SaveFile(
                    _defaultBasePath,
                    targetPath,
                    payloadBytes,
                    ct);
                return new DeliveryResult
                {
                    Success = true,
                    Channel = "File",
                    SentByteSize = payloadBytes.Length,
                    TargetAddressOrPath = targetPath,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                return new DeliveryResult
                {
                    Success = false,
                    Channel = "File",
                    SentByteSize = payloadBytes.Length,
                    TargetAddressOrPath = targetPath,
                    ErrorCode = "FileWriteError",
                    ErrorMessage = ex.Message,
                    Warnings = warnings
                };
            }
        }
    }
}
