using Furion;
using Furion.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Entities;
using Newtonsoft.Json;
using Shared.Core.Extensions;

namespace Module.ShareData.Infrastructure.Services
{
    /// <summary>
    /// Description: Ghi nhật ký hoạt động phân hệ Chia sẻ dữ liệu.
    ///              Gọi tường minh từ các CommandHandler để kiểm soát được nội dung mô tả tiếng Việt.
    ///
    ///              NGUYÊN TẮC: ghi log KHÔNG ĐƯỢC làm hỏng nghiệp vụ chính.
    ///              Mọi lỗi phát sinh khi ghi log đều bị nuốt và chỉ đẩy ra log hệ thống.
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityLogger : IScoped
    {
        /// <summary>Các trường không bao giờ được đưa vào BeforeJson / AfterJson.</summary>
        private static readonly string[] SensitiveFields = { "PasswordHash", "Password" };

        private readonly BaseRepository<ShareDataActivityLog> _activityLogRep;
        private readonly ILogger<ShareDataActivityLogger> _logger;

        public ShareDataActivityLogger(
            BaseRepository<ShareDataActivityLog> activityLogRep,
            ILogger<ShareDataActivityLogger> logger)
        {
            _activityLogRep = activityLogRep;
            _logger = logger;
        }

        // ── Nhóm CONFIG: thêm / sửa / xóa cấu hình ───────────────────────

        /// <summary>
        /// Description: Ghi log thêm mới một bản ghi cấu hình.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogCreateAsync(string targetType, string? targetId, string? targetName,
            object? after, string? partnerId = null, string? partnerName = null)
        {
            await WriteAsync(new ShareDataActivityLog
            {
                LogType = ShareDataConst.LogType.Config,
                Action = ShareDataConst.ActivityAction.Create,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                PartnerId = partnerId,
                PartnerName = partnerName,
                AfterJson = Serialize(after),
                Description = $"Thêm mới {TargetLabel(targetType)} \"{targetName}\""
            });
        }

        /// <summary>
        /// Description: Ghi log cập nhật một bản ghi cấu hình, tự so sánh để lấy danh sách trường đã đổi.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogUpdateAsync(string targetType, string? targetId, string? targetName,
            object? before, object? after, string? partnerId = null, string? partnerName = null)
        {
            var changed = DiffFields(before, after);

            await WriteAsync(new ShareDataActivityLog
            {
                LogType = ShareDataConst.LogType.Config,
                Action = ShareDataConst.ActivityAction.Update,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                PartnerId = partnerId,
                PartnerName = partnerName,
                BeforeJson = Serialize(before),
                AfterJson = Serialize(after),
                ChangedFields = changed.Count == 0 ? null : string.Join(", ", changed),
                Description = changed.Count == 0
                    ? $"Cập nhật {TargetLabel(targetType)} \"{targetName}\" (không có trường nào thay đổi)"
                    : $"Cập nhật {TargetLabel(targetType)} \"{targetName}\": {string.Join(", ", changed)}"
            });
        }

        /// <summary>
        /// Description: Ghi log xóa một bản ghi cấu hình.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogDeleteAsync(string targetType, string? targetId, string? targetName,
            object? before, string? partnerId = null, string? partnerName = null)
        {
            await WriteAsync(new ShareDataActivityLog
            {
                LogType = ShareDataConst.LogType.Config,
                Action = ShareDataConst.ActivityAction.Delete,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                PartnerId = partnerId,
                PartnerName = partnerName,
                BeforeJson = Serialize(before),
                Description = $"Xóa {TargetLabel(targetType)} \"{targetName}\""
            });
        }

        /// <summary>
        /// Description: Ghi log một hành động vận hành trên cấu hình
        ///              (kết nối, ngắt kết nối, tắt/bật, hủy, duyệt, từ chối).
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogActionAsync(string action, string targetType, string? targetId, string? targetName,
            string? description, string? partnerId = null, string? partnerName = null,
            string? subscriptionId = null, string? sessionId = null, string? reason = null)
        {
            await WriteAsync(new ShareDataActivityLog
            {
                LogType = ShareDataConst.LogType.Config,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                PartnerId = partnerId,
                PartnerName = partnerName,
                SubscriptionId = subscriptionId,
                SessionId = sessionId,
                Description = string.IsNullOrWhiteSpace(reason) ? description : $"{description} (lý do: {reason})"
            });
        }

        // ── Nhóm TRANSFER: gửi / nhận gói tin ────────────────────────────

        /// <summary>
        /// Description: Ghi log một lượt truyền gói tin (gửi đi hoặc nhận về).
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogTransferAsync(string transferDirection, string? partnerId, string? partnerName,
            string? subscriptionId, string? sessionId, int? datatypeId, string? pduType,
            int? serialNbr = null, int? packetNbr = null, string? format = null,
            long? byteSize = null, int? recordCount = null,
            bool success = true, string? errorMessage = null, string? description = null)
        {
            var isSend = transferDirection == ShareDataConst.TransferDirection.Snd;
            var directionLabel = isSend ? "Gửi" : "Nhận";

            await WriteAsync(new ShareDataActivityLog
            {
                LogType = ShareDataConst.LogType.Transfer,
                Action = isSend ? ShareDataConst.ActivityAction.Send : ShareDataConst.ActivityAction.Receive,
                TargetType = ShareDataConst.TargetType.Subscription,
                TargetId = subscriptionId,
                PartnerId = partnerId,
                PartnerName = partnerName,
                SubscriptionId = subscriptionId,
                SessionId = sessionId,
                TransferDirection = transferDirection,
                DatatypeId = datatypeId,
                SerialNbr = serialNbr,
                PacketNbr = packetNbr,
                PduType = pduType,
                Format = format,
                ByteSize = byteSize,
                RecordCount = recordCount,
                Status = success ? ShareDataConst.ActivityStatus.Success : ShareDataConst.ActivityStatus.Failed,
                ErrorMessage = errorMessage,
                Description = description
                    ?? $"{directionLabel} gói tin loại {datatypeId} với đối tác \"{partnerName}\""
            });
        }

        /// <summary>
        /// Description: Ghi log một lượt xuất file bằng chứng.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogExportAsync(string? partnerId, string? partnerName, string? subscriptionId,
            int? datatypeId, string? filePath, string? hash, int? recordCount, long? byteSize,
            bool success = true, string? errorMessage = null)
        {
            await WriteAsync(new ShareDataActivityLog
            {
                LogType = ShareDataConst.LogType.Transfer,
                Action = ShareDataConst.ActivityAction.Export,
                TargetType = ShareDataConst.TargetType.Subscription,
                TargetId = subscriptionId,
                PartnerId = partnerId,
                PartnerName = partnerName,
                SubscriptionId = subscriptionId,
                TransferDirection = ShareDataConst.TransferDirection.Snd,
                DatatypeId = datatypeId,
                Format = ShareDataConst.PublishFormat.File,
                FilePath = filePath,
                Hash = hash,
                RecordCount = recordCount,
                ByteSize = byteSize,
                Status = success ? ShareDataConst.ActivityStatus.Success : ShareDataConst.ActivityStatus.Failed,
                ErrorMessage = errorMessage,
                Description = $"Xuất file dữ liệu loại {datatypeId} cho đối tác \"{partnerName}\""
            });
        }

        // ── Hạ tầng ─────────────────────────────────────────────────────

        /// <summary>
        /// Description: Ghi bản ghi nhật ký xuống DB. Nuốt mọi lỗi để không làm hỏng nghiệp vụ chính.
        /// Created date: 2026-08-05
        /// </summary>
        private async Task WriteAsync(ShareDataActivityLog log)
        {
            try
            {
                log.OccurredAt ??= DateTime.Now;
                log.Status ??= ShareDataConst.ActivityStatus.Success;
                log.OperatorName = CurrentOperator();
                log.OperatorIp = CurrentIp();

                await _activityLogRep.InsertAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShareDataActivityLogger] Ghi nhật ký hoạt động thất bại: {Action} {TargetType} {TargetId}",
                    log.Action, log.TargetType, log.TargetId);
            }
        }

        /// <summary>
        /// Description: Tài khoản đang thao tác. Luồng nền không có HttpContext thì ghi 'system'.
        /// Created date: 2026-08-05
        /// </summary>
        private static string CurrentOperator()
        {
            try
            {
                return App.User == null ? "system" : App.User.GetUsername();
            }
            catch
            {
                return "system";
            }
        }

        /// <summary>
        /// Description: Địa chỉ IP của người thao tác, rỗng khi chạy nền.
        /// Created date: 2026-08-05
        /// </summary>
        private static string? CurrentIp()
        {
            try
            {
                return App.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Description: Serialize đối tượng, loại bỏ các trường nhạy cảm và giá trị null.
        /// Created date: 2026-08-05
        /// </summary>
        private static string? Serialize(object? value)
        {
            if (value == null) return null;

            try
            {
                var dict = ToDictionary(value);
                return dict.Count == 0 ? null : JsonConvert.SerializeObject(dict);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Description: So sánh 2 đối tượng, trả về danh sách tên trường có giá trị khác nhau.
        /// Created date: 2026-08-05
        /// </summary>
        private static List<string> DiffFields(object? before, object? after)
        {
            var result = new List<string>();
            if (before == null || after == null) return result;

            try
            {
                var b = ToDictionary(before);
                var a = ToDictionary(after);

                foreach (var key in a.Keys)
                {
                    b.TryGetValue(key, out var oldValue);
                    var newValue = a[key];

                    if (!string.Equals(ToText(oldValue), ToText(newValue), StringComparison.Ordinal))
                        result.Add(key);
                }
            }
            catch
            {
                // So sánh hỏng thì bỏ qua, vẫn giữ được Before/After JSON.
            }

            return result;
        }

        /// <summary>
        /// Description: Đọc các property public đơn giản của đối tượng, bỏ trường nhạy cảm và trường điều hướng.
        /// Created date: 2026-08-05
        /// </summary>
        private static Dictionary<string, object?> ToDictionary(object value)
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var prop in value.GetType().GetProperties())
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                if (SensitiveFields.Contains(prop.Name)) continue;

                var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var isSimple = type.IsPrimitive || type.IsEnum
                    || type == typeof(string) || type == typeof(decimal)
                    || type == typeof(DateTime) || type == typeof(Guid);

                if (!isSimple) continue;

                result[prop.Name] = prop.GetValue(value);
            }

            return result;
        }

        /// <summary>
        /// Description: Quy giá trị về chuỗi để so sánh, null và chuỗi rỗng coi như bằng nhau.
        /// Created date: 2026-08-05
        /// </summary>
        private static string ToText(object? value)
        {
            if (value == null) return string.Empty;
            if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Description: Nhãn tiếng Việt của loại đối tượng, dùng dựng câu mô tả.
        /// Created date: 2026-08-05
        /// </summary>
        private static string TargetLabel(string targetType) => targetType switch
        {
            ShareDataConst.TargetType.Partner => "đối tác chia sẻ",
            ShareDataConst.TargetType.Subscription => "đăng ký chia sẻ",
            ShareDataConst.TargetType.Session => "phiên kết nối",
            ShareDataConst.TargetType.DataSource => "nguồn dữ liệu",
            ShareDataConst.TargetType.MappingProfile => "hồ sơ ánh xạ",
            ShareDataConst.TargetType.EventSource => "nguồn sự kiện",
            _ => "bản ghi"
        };
    }
}
