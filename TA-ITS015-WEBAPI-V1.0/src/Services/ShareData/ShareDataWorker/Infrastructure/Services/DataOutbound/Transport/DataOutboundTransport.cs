using System.Text;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery.Base.Enums;
using Modules.Integration.Delivery.Base.Helpers;
using Modules.Integration.Delivery.Base.Models;
using Modules.Integration.Delivery.Base.Models.Formats;
using Modules.Integration.Delivery.Formats;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Logging;

namespace ShareDataWorker.Infrastructure.Services.DataOutbound.Transport
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Dịch vụ chuyển giao gói tin kết xuất (Chặng 3: Data Transport): định dạng payload, lưu bằng chứng và gửi tới đối tác qua bộ điều phối kết nối.
    /// Created date: 16/10/2026
    /// Modified date: 13/11/2026
    /// </summary>
    public class DataOutboundTransport(
        DeliveryDispatcher deliveryDispatcher,
        IConfiguration? configuration = null)
    {
        private readonly DeliveryDispatcher _deliveryDispatcher = deliveryDispatcher ?? throw new ArgumentNullException(nameof(deliveryDispatcher));
        private readonly IConfiguration? _configuration = configuration;

        /// <summary>
        /// Author: DatHQ
        /// Description: Dựng cặp yêu cầu gửi và ngữ cảnh gửi cho một trang dữ liệu đã ánh xạ.
        /// Created date: 16/10/2026
        /// </summary>
        public static (DeliveryRequest Request, DeliveryContext Context) Build(
            DataMappingResult mapResult,
            DataOutboundContext ctx)
        {
            var request = BuildRequest(mapResult, ctx);
            var context = BuildContext(ctx);
            return (request, context);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chép thông tin đối tác, gói tin, đăng ký từ ngữ cảnh kết xuất sang ngữ cảnh gửi, không có nghiệp vụ.
        /// Created date: 16/10/2026
        /// </summary>
        public static DeliveryContext BuildContext(DataOutboundContext ctx)
        {
            return new DeliveryContext(
                PartnerCode: ctx.Partner?.Code,
                PacketCode: ctx.PacketCode,
                SubscriptionId: ctx.Subscription?.ID,
                SerialNbr: ctx.Subscription?.SerialNbr,
                DatatypeId: ctx.Subscription?.DatatypeId,
                ExportedAt: ctx.ExportedAt,
                Address: ctx.Partner?.Address,
                Port: ctx.Partner?.Port,
                EndPointApiUrl: ctx.Partner?.EndPointApiUrl
            );
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dựng yêu cầu gửi từ ConnectionConfig của đối tác.
        ///              Cấu hình được đổi thành dictionary không phân biệt hoa thường; JSON hỏng thì giữ nguyên chuỗi ở khóa RawConfig.
        ///              Khóa có chữ token, key, auth, pass, user được chép sang SecurityInfo. Lọc bằng Contains nên khóa như EncryptionKeyRef cũng vào đây.
        ///              Loại kết nối ưu tiên khóa trong JSON, rồi đến cột ConnectionType, mặc định là API.
        /// Created date: 16/10/2026
        /// </summary>
        public static DeliveryRequest BuildRequest(
            DataMappingResult mapResult,
            DataOutboundContext ctx)
        {
            var jsonPayload = mapResult.FinalBytes != null && mapResult.FinalBytes.Length > 0
                ? Encoding.UTF8.GetString(mapResult.FinalBytes)
                : string.Empty;

            var connectionConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(ctx.Partner?.ConnectionConfig))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(ctx.Partner.ConnectionConfig);
                    if (parsed != null)
                    {
                        foreach (var kv in parsed)
                        {
                            connectionConfig[kv.Key] = kv.Value?.ToString() ?? string.Empty;
                        }
                    }
                }
                catch
                {
                    connectionConfig[ConnectKeys.RawConfig] = ctx.Partner.ConnectionConfig;
                }
            }

            if (ctx.Partner != null)
            {
                if (!string.IsNullOrWhiteSpace(ctx.Partner.Code) && !connectionConfig.ContainsKey(ConnectKeys.PartnerCode))
                {
                    connectionConfig[ConnectKeys.PartnerCode] = ctx.Partner.Code;
                }
            }

            var securityInfo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in connectionConfig)
            {
                if (kv.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Contains("key", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Contains("pass", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Contains("user", StringComparison.OrdinalIgnoreCase))
                {
                    securityInfo[kv.Key] = kv.Value;
                }
            }

            var reader = new ConnectConfigReader(connectionConfig);
            var connectionType = reader.GetOptionalEnum<ConnectionType>(ConnectKeys.ConnectType)
                ?? ctx.Partner?.ConnectionType
                ?? ConnectionType.Api;

            return new DeliveryRequest
            {
                JsonPayload = jsonPayload,
                SecurityInfo = securityInfo,
                ConnectConfig = connectionConfig,
                ConnectType = connectionType
            };
        }

        /// <summary>
        /// Description: Chuẩn bị payload, lưu bằng chứng và chuyển giao dữ liệu tới đối tác qua kênh truyền tương ứng.
        /// </summary>
        public async Task<DataOutboundSendResult> Send(
            DataMappingResult mapping,
            DataOutboundContext ctx,
            CancellationToken ct)
        {
            // 1. Dựng DeliveryRequest/DeliveryContext
            var (baseRequest, connectionContext) = Build(mapping, ctx);
            var connCfg = baseRequest.ConnectConfig;
            var connectionType = baseRequest.ConnectType;

            // 2. Định dạng payload
            var formatName = connCfg.TryGetValue(ConnectKeys.Format, out var fmt) ? fmt : null;
            var formatCtx = new FormatContext(
                ctx.Partner?.Code,
                ctx.Packet?.Code,
                ctx.Subscription.SerialNbr,
                ctx.ExportedAt);
            var jsonString = baseRequest.JsonPayload;

            var transportPayload = PayloadFormatRegistry.ToTransport(formatName, connectionType, jsonString, formatCtx);
            var finalBytes = transportPayload.Bytes;
            var finalContentType = transportPayload.ContentType;
            var finalExtension = transportPayload.FileExtension;

            var connectionRequest = new DeliveryRequest
            {
                JsonPayload = finalBytes != null ? Encoding.UTF8.GetString(finalBytes) : string.Empty,
                PayloadBytes = finalBytes,
                ContentType = finalContentType,
                FileExtension = finalExtension,
                Headers = transportPayload.Headers,
                ConnectConfig = connCfg,
                ConnectType = connectionType,
                SecurityInfo = baseRequest.SecurityInfo
            };

            // 3. Lưu bằng chứng LUÔN LUÔN
            var vendorCode = ctx.Partner?.Code ?? ctx.Partner?.Name ?? ctx.Subscription.PartnerId ?? ShareDataEnum.SystemConstants.Unknown;
            var dataTypeId = ExportPathHelper.ResolveDatatypeFolderName(ctx.Subscription.DatatypeId);

            var relativePath = ExportPathHelper.GenerateExportRelativePath(
                vendorCode,
                dataTypeId,
                ctx.ExportedAt,
                ExportPathHelper.ResolveFileDiscriminator(ctx.Subscription.SerialNbr, ctx.Subscription.ID),
                finalExtension);

            var bytesLength = finalBytes?.Length ?? 0;

            var warnings = new List<string>();
            var transportReader = new ConnectConfigReader(connCfg);

            if (connCfg.ContainsKey(ConnectKeys.OutputPath))
            {
                warnings.Add("Khóa [OutputPath] là khóa đã bãi bỏ, bị bỏ qua.");
            }

            var encVal = transportReader.GetString(ConnectKeys.Encryptor);
            if (!string.IsNullOrWhiteSpace(encVal) && !EncryptNames.IsSupported(encVal))
            {
                warnings.Add($"Thuật toán mã hóa Encryptor [{encVal}] không được hỗ trợ.");
            }

            var inEncVal = transportReader.GetString(ConnectKeys.InboundEncryptor);
            if (!string.IsNullOrWhiteSpace(inEncVal) && !EncryptNames.IsSupported(inEncVal))
            {
                warnings.Add($"Thuật toán mã hóa InboundEncryptor [{inEncVal}] không được hỗ trợ.");
            }

            try
            {
                var basePath = _configuration?.GetValue<string>(ExportPathHelper.BasePathConfigKey) ?? ExportPathHelper.DefaultBasePath;
                await ExportPathHelper.SaveFile(
                    basePath,
                    relativePath,
                    finalBytes ?? [],
                    ct);
            }
            catch (Exception ex)
            {
                return new DataOutboundSendResult(
                    false,
                    bytesLength,
                    relativePath,
                    $"Lưu đệm payload thất bại, hủy gửi và giữ nguyên mốc: {ex.Message}",
                    AlertCode: ShareDataAlertCode.Outbound.ArchiveFailed,
                    Warnings: warnings);
            }

            // 4. Gửi
            var isFileChannel = connectionType.IsFileChannel();
            DeliveryResult connectionResult;
            if (isFileChannel)
            {
                var hasCustomDirectory = connCfg.TryGetValue(ConnectKeys.DirectoryPath, out var dp) && !string.IsNullOrWhiteSpace(dp);
                if (!hasCustomDirectory)
                {
                    connectionResult = new DeliveryResult
                    {
                        Success = true,
                        Channel = "File",
                        SentByteSize = bytesLength,
                        TargetAddressOrPath = relativePath
                    };
                }
                else
                {
                    connectionResult = await _deliveryDispatcher.Dispatch(
                        connectionRequest,
                        connectionContext,
                        ct);
                }
            }
            else
            {
                connectionResult = await _deliveryDispatcher.Dispatch(
                    connectionRequest,
                    connectionContext,
                    ct);
            }

            if (connectionResult.Warnings != null && connectionResult.Warnings.Count > 0)
            {
                warnings.AddRange(connectionResult.Warnings);
            }

            // 5. Kết quả
            var isSuccess = connectionResult.Success;
            var sentByteSize = connectionResult.SentByteSize > 0 ? connectionResult.SentByteSize : bytesLength;
            if (!isSuccess)
            {
                var sendFailedMsg = connectionResult.ErrorMessage ?? $"Gửi dữ liệu tới đối tác [{vendorCode}] thất bại.";
                return new DataOutboundSendResult(
                    false,
                    sentByteSize,
                    relativePath,
                    sendFailedMsg,
                    connectionResult.DetailJson,
                    AlertCode: ShareDataAlertCode.Outbound.HttpSendFailed,
                    Warnings: warnings,
                    IsPossiblyDelivered: connectionResult.IsPossiblyDelivered);
            }

            return new DataOutboundSendResult(
                true,
                sentByteSize,
                relativePath,
                Warnings: warnings);
        }
    }
}
