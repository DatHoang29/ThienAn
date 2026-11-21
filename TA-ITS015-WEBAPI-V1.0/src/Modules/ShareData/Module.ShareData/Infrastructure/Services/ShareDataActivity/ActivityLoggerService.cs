using Furion;
using Furion.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Entities;
using Newtonsoft.Json;
using Shared.Core.Extensions;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;

namespace Module.ShareData.Infrastructure.Services.ShareDataActivityService
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Ghi nhật ký hoạt động phân hệ Chia sẻ dữ liệu.
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityLogger : IScoped
    {
        /// <summary>Các trường không bao giờ được đưa vào BeforeJson / AfterJson.</summary>
        private static readonly string[] SensitiveFields = { "PasswordHash", "Password" };

        private readonly BaseRepository<ShareDataActivityLog> _activityLogRep;
        private readonly BaseRepository<ShareDataPartner> _partnerRep;
        private readonly BaseRepository<ShareDataPacket>? _packetRep;
        private readonly ILogger<ShareDataActivityLogger> _logger;

        /// <summary>Tên đối tác đã tra trong scope hiện tại — tránh query lại khi ghi nhiều dòng log.</summary>
        private readonly Dictionary<string, string?> _partnerNameCache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (string? Code, string? Name)> _packetCache = new(StringComparer.Ordinal);

        public ShareDataActivityLogger(
            BaseRepository<ShareDataActivityLog> activityLogRep,
            BaseRepository<ShareDataPartner> partnerRep,
            ILogger<ShareDataActivityLogger> logger)
            : this(activityLogRep, partnerRep, null, logger)
        {
        }

        public ShareDataActivityLogger(
            BaseRepository<ShareDataActivityLog> activityLogRep,
            BaseRepository<ShareDataPartner> partnerRep,
            BaseRepository<ShareDataPacket>? packetRep,
            ILogger<ShareDataActivityLogger> logger)
        {
            _activityLogRep = activityLogRep;
            _partnerRep = partnerRep;
            _packetRep = packetRep;
            _logger = logger;
        }

        // ── Nhận dạng đối tượng nghiệp vụ ────────────────────────────────

        /// <summary>
        /// Author: HieuNV
        /// Description: Thông tin nhận dạng suy trực tiếp từ entity, để call site chỉ cần truyền
        ///              đúng đối tượng nghiệp vụ thay vì liệt kê từng trường.
        /// Created date: 2026-09-10
        /// </summary>
        private readonly record struct AuditTarget(
            BaseEnums.ShareDataTargetType? Type,
            string? Id,
            string? Name,
            string? PartnerId,
            string? PartnerName,
            string? SubscriptionId,
            string? PacketId = null);

        /// <summary>
        /// Author: HieuNV
        /// Description: Suy loại / ID / nhãn / đối tác từ entity. Kiểu chưa khai thì ghi log với
        ///              TargetType rỗng chứ không ném lỗi — ghi log không được làm hỏng nghiệp vụ.
        /// Created date: 2026-09-10
        /// </summary>
        private static AuditTarget Describe(object? target) => target switch
        {
            ShareDataSubscription s => new(BaseEnums.ShareDataTargetType.Subscription,
                s.ID, SubscriptionLabel(s), s.PartnerId, null, s.ID),
            ShareDataPartner p => new(BaseEnums.ShareDataTargetType.Partner,
                p.ID, PartnerLabel(p), p.ID, p.Name, null),
            ShareDataMapping m => new(BaseEnums.ShareDataTargetType.Mapping,
                m.ID, m.Code ?? m.Name, m.PartnerId, null, null),
            ShareDataSql ps when !string.IsNullOrWhiteSpace(ps.PartnerId) => new(BaseEnums.ShareDataTargetType.Partner,
                ps.PartnerId, "SQL riêng", ps.PartnerId, null, null, ps.PacketId),
            ShareDataSql cs => new(BaseEnums.ShareDataTargetType.Packet,
                cs.PacketId, "SQL mẫu chung", null, null, null, cs.PacketId),
            ShareDataLastSend ls => new(BaseEnums.ShareDataTargetType.Packet,
                ls.ID,
                !string.IsNullOrWhiteSpace(ls.CursorKey)
                    ? $"Mốc gửi {ls.PartnerCode} · {ls.PacketCode} · nhóm {ls.CursorKey}"
                    : $"Mốc gửi {ls.PartnerCode} · {ls.PacketCode}",
                null, ls.PartnerCode, null),
            ShareDataCodeSet c => new(BaseEnums.ShareDataTargetType.CodeSet,
                c.ID, c.Code ?? c.Name, null, null, null),
            ShareDataPacket pk => new(BaseEnums.ShareDataTargetType.Packet,
                pk.ID, pk.Name ?? pk.Code, null, null, null),
            _ => new(null, null, null, null, null, null, null)
        };

        /// <summary>Nhãn đăng ký — dạng "đăng ký gửi #12 loại 101".</summary>
        private static string SubscriptionLabel(ShareDataSubscription s) =>
            $"đăng ký {(s.Direction == BaseEnums.Direction.Inbound ? "nhận" : "gửi")} #{s.SerialNbr} loại {s.DatatypeId}";

        /// <summary>Nhãn đối tác — ưu tiên "mã - tên", thiếu thì lấy phần còn lại.</summary>
        private static string PartnerLabel(ShareDataPartner p)
        {
            if (string.IsNullOrWhiteSpace(p.Code))
                return p.Name ?? p.ID ?? string.Empty;

            return string.IsNullOrWhiteSpace(p.Name) ? p.Code! : $"{p.Code} - {p.Name}";
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Tên đối tác để ghi kèm nhật ký. 
        /// Created date: 2026-09-10
        /// </summary>
        private async Task<string?> ResolvePartnerNameAsync(AuditTarget target)
        {
            if (!string.IsNullOrWhiteSpace(target.PartnerName)) return target.PartnerName;
            if (string.IsNullOrWhiteSpace(target.PartnerId)) return null;

            if (_partnerNameCache.TryGetValue(target.PartnerId!, out var cached)) return cached;

            string? name = null;
            try
            {
                name = (await _partnerRep.GetFirstAsync(u => u.ID == target.PartnerId))?.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShareDataActivityLogger] Không tra được tên đối tác {PartnerId}", target.PartnerId);
            }

            _partnerNameCache[target.PartnerId!] = name;
            return name;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tra mã và tên gói tin theo ID, kết quả giữ trong _packetCache.
        /// Created date: 29/10/2026
        /// </summary>
        private async Task<(string? Code, string? Name)> ResolvePacketInfoAsync(string? packetId)
        {
            if (string.IsNullOrWhiteSpace(packetId)) return (null, null);
            if (_packetCache.TryGetValue(packetId, out var cached)) return cached;

            string? code = null;
            string? name = null;
            try
            {
                var rep = _packetRep ?? App.GetService<BaseRepository<ShareDataPacket>>();
                if (rep != null)
                {
                    var packet = await rep.GetFirstAsync(p => p.ID == packetId || p.Code == packetId);
                    code = packet?.Code;
                    name = packet?.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ShareDataActivityLogger] Không tra được thông tin gói tin {PacketId}", packetId);
            }

            var info = (code, name);
            _packetCache[packetId] = info;
            return info;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Dựng tên đối tượng, tên đối tác, mã gói và tiền tố mô tả của nhật ký: dòng ShareDataSql phân biệt SQL riêng và mẫu chung, đối tượng khác lấy theo tên.
        /// Created date: 29/10/2026
        /// </summary>
        private async Task<(string? TargetName, string? PartnerName, string? DatatypeId, string? CustomPrefix)> ResolveSqlTargetDetailsAsync(AuditTarget target, object entity)
        {
            if (entity is not ShareDataSql sql)
            {
                var partnerName = await ResolvePartnerNameAsync(target);
                return (target.Name, partnerName, null, null);
            }

            var (packetCode, packetName) = await ResolvePacketInfoAsync(sql.PacketId);
            var packetCodeStr = !string.IsNullOrWhiteSpace(packetCode) ? packetCode : "chưa rõ";
            var packetLabel = !string.IsNullOrWhiteSpace(packetCode)
                ? (!string.IsNullOrWhiteSpace(packetName) ? $"[{packetCode}] {packetName}" : $"[{packetCode}]")
                : null;

            var datatypeId = !string.IsNullOrWhiteSpace(sql.PacketId) ? sql.PacketId : packetCode;

            if (!string.IsNullOrWhiteSpace(sql.PartnerId))
            {
                var partnerName = await ResolvePartnerNameAsync(target);
                var targetName = packetLabel != null ? $"{packetLabel} (SQL riêng)" : "SQL riêng";
                var prefix = $"SQL riêng gói tin [{packetCodeStr}] cho đối tác \"{partnerName ?? sql.PartnerId}\"";
                return (targetName, partnerName, datatypeId, prefix);
            }
            else
            {
                var targetName = packetLabel != null ? $"{packetLabel} (SQL mẫu chung)" : "SQL mẫu chung";
                var partnerName = "Mẫu chung (tất cả đối tác)";
                var prefix = $"SQL mẫu chung gói tin [{packetCodeStr}]";
                return (targetName, partnerName, datatypeId, prefix);
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi log thêm mới một bản ghi cấu hình.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogCreateAsync(object after)
        {
            var target = Describe(after);
            var (targetName, partnerName, datatypeId, customPrefix) = await ResolveSqlTargetDetailsAsync(target, after);

            var desc = customPrefix != null
                ? $"Thêm mới {customPrefix}"
                : $"Thêm mới {TargetLabel(target.Type)} \"{target.Name}\"";

            await WriteAsync(new ShareDataActivityLog
            {
                LogType = BaseEnums.LogTypeEnum.CONFIG,
                Action = BaseEnums.ActivityAction.Create,
                TargetType = target.Type,
                TargetId = target.Id,
                TargetName = targetName,
                PartnerId = target.PartnerId,
                PartnerName = partnerName,
                DatatypeId = datatypeId,
                SubscriptionId = target.SubscriptionId,
                AfterJson = Serialize(after),
                Description = desc
            });
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi log cập nhật một bản ghi cấu hình, tự so sánh để lấy danh sách trường đã đổi.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogUpdateAsync(object before, object after)
        {
            var target = Describe(after);
            var (targetName, partnerName, datatypeId, customPrefix) = await ResolveSqlTargetDetailsAsync(target, after);
            var changed = DiffFields(before, after);

            var desc = customPrefix != null
                ? (changed.Count == 0
                    ? $"Cập nhật {customPrefix} (không có trường nào thay đổi)"
                    : $"Cập nhật {customPrefix}: {string.Join(", ", changed)}")
                : (changed.Count == 0
                    ? $"Cập nhật {TargetLabel(target.Type)} \"{target.Name}\" (không có trường nào thay đổi)"
                    : $"Cập nhật {TargetLabel(target.Type)} \"{target.Name}\": {string.Join(", ", changed)}");

            await WriteAsync(new ShareDataActivityLog
            {
                LogType = BaseEnums.LogTypeEnum.CONFIG,
                Action = BaseEnums.ActivityAction.Update,
                TargetType = target.Type,
                TargetId = target.Id,
                TargetName = targetName,
                PartnerId = target.PartnerId,
                PartnerName = partnerName,
                DatatypeId = datatypeId,
                SubscriptionId = target.SubscriptionId,
                BeforeJson = Serialize(before),
                AfterJson = Serialize(after),
                ChangedFields = changed.Count == 0 ? null : string.Join(", ", changed),
                Description = desc
            });
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi log xóa một bản ghi cấu hình.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogDeleteAsync(object before)
        {
            var target = Describe(before);
            var (targetName, partnerName, datatypeId, customPrefix) = await ResolveSqlTargetDetailsAsync(target, before);

            var desc = customPrefix != null
                ? $"Xóa {customPrefix}"
                : $"Xóa {TargetLabel(target.Type)} \"{target.Name}\"";

            await WriteAsync(new ShareDataActivityLog
            {
                LogType = BaseEnums.LogTypeEnum.CONFIG,
                Action = BaseEnums.ActivityAction.Delete,
                TargetType = target.Type,
                TargetId = target.Id,
                TargetName = targetName,
                PartnerId = target.PartnerId,
                PartnerName = partnerName,
                DatatypeId = datatypeId,
                SubscriptionId = target.SubscriptionId,
                BeforeJson = Serialize(before),
                Description = desc
            });
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi log một hành động vận hành trên cấu hình
        ///              (kết nối, ngắt kết nối, tắt/bật, hủy, duyệt, từ chối).
        ///              Bỏ trống description thì tự dựng từ hành động và nhãn của đối tượng.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogActionAsync(BaseEnums.ActivityAction action, object target,
            string? description = null, string? reason = null, string? sessionId = null)
        {
            var info = Describe(target);
            var text = string.IsNullOrWhiteSpace(description)
                ? $"{ActionLabel(action)} {TargetLabel(info.Type)} \"{info.Name}\""
                : description!;

            await WriteAsync(new ShareDataActivityLog
            {
                LogType = BaseEnums.LogTypeEnum.CONFIG,
                Action = action,
                TargetType = info.Type,
                TargetId = info.Id,
                TargetName = info.Name,
                PartnerId = info.PartnerId,
                PartnerName = await ResolvePartnerNameAsync(info),
                SubscriptionId = info.SubscriptionId,
                SessionId = sessionId,
                Description = string.IsNullOrWhiteSpace(reason) ? text : $"{text} (lý do: {reason})"
            });
        }

        /// <summary>Động từ tiếng Việt của hành động, dùng dựng câu mô tả mặc định.</summary>
        private static string ActionLabel(BaseEnums.ActivityAction action) => action switch
        {
            BaseEnums.ActivityAction.Pause => "Tắt",
            BaseEnums.ActivityAction.Resume => "Bật lại",
            BaseEnums.ActivityAction.Approve => "Duyệt",
            BaseEnums.ActivityAction.Reject => "Từ chối",
            BaseEnums.ActivityAction.Cancel => "Hủy",
            BaseEnums.ActivityAction.Connect => "Kết nối",
            BaseEnums.ActivityAction.Disconnect => "Ngắt kết nối",
            _ => "Thao tác trên"
        };

        // ── Nhóm TRANSFER: gửi / nhận gói tin ────────────────────────────

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi log một lượt truyền gói tin (gửi đi hoặc nhận về).
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<string> LogTransferAsync(BaseEnums.TransferDirection transferDirection, string? partnerId, string? partnerName,
            string? subscriptionId, string? sessionId, string? datatypeId, string? pduType,
            int? serialNbr = null, int? packetNbr = null, BaseEnums.PublishFormat? format = null,
            long? byteSize = null, int? recordCount = null,
            bool success = true, string? errorMessage = null, string? description = null,
            string? parentId = null, int? stepNbr = null, string? id = null)
        {
            var isSend = transferDirection == BaseEnums.TransferDirection.SND;
            var directionLabel = isSend ? "Gửi" : "Nhận";
            var logId = id ?? Guid.NewGuid().ToString("N");

            await WriteAsync(new ShareDataActivityLog
            {
                ID = logId,
                LogType = BaseEnums.LogTypeEnum.Transfer,
                Action = isSend ? BaseEnums.ActivityAction.Send : BaseEnums.ActivityAction.Receive,
                TargetType = BaseEnums.ShareDataTargetType.Subscription,
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
                Success = success ? BaseEnums.SuccessEnums.Success : BaseEnums.SuccessEnums.Fail,
                ErrorMessage = errorMessage,
                Description = description
                    ?? $"{directionLabel} gói tin loại {datatypeId} với đối tác \"{partnerName}\"",
                ParentId = parentId,
                StepNbr = stepNbr
            });

            return logId;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi log một lượt xuất file bằng chứng.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task LogExportAsync(string? partnerId, string? partnerName, string? subscriptionId,
            string? datatypeId, string? filePath, string? hash, int? recordCount, long? byteSize,
            bool success = true, string? errorMessage = null)
        {
            await WriteAsync(new ShareDataActivityLog
            {
                LogType = BaseEnums.LogTypeEnum.Transfer,
                Action = BaseEnums.ActivityAction.Export,
                TargetType = BaseEnums.ShareDataTargetType.Subscription,
                TargetId = subscriptionId,
                PartnerId = partnerId,
                PartnerName = partnerName,
                SubscriptionId = subscriptionId,
                TransferDirection = BaseEnums.TransferDirection.SND,
                DatatypeId = datatypeId,
                Format = BaseEnums.PublishFormat.File,
                FilePath = filePath,
                Hash = hash,
                RecordCount = recordCount,
                ByteSize = byteSize,
                Success = success ? BaseEnums.SuccessEnums.Success : BaseEnums.SuccessEnums.Fail,
                ErrorMessage = errorMessage,
                Description = $"Xuất file dữ liệu loại {datatypeId} cho đối tác \"{partnerName}\""
            });
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghi log cập nhật bắt buộc trong transaction. Không nuốt ngoại lệ: nếu ghi DB thất bại sẽ ném lỗi để caller rollback transaction.
        /// Created date: 13/10/2026
        /// Modified date: 20/10/2026 (Lấy giờ qua BaseUtil.TimeServer)
        /// </summary>
        public async Task LogUpdate(object before, object after)
        {
            var target = Describe(after);
            var (targetName, partnerName, datatypeId, customPrefix) = await ResolveSqlTargetDetailsAsync(target, after);
            var changed = DiffFields(before, after);

            var desc = customPrefix != null
                ? (changed.Count == 0
                    ? $"Cập nhật {customPrefix} (không có trường nào thay đổi)"
                    : $"Cập nhật {customPrefix}: {string.Join(", ", changed)}")
                : (changed.Count == 0
                    ? $"Cập nhật {TargetLabel(target.Type)} \"{target.Name}\" (không có trường nào thay đổi)"
                    : $"Cập nhật {TargetLabel(target.Type)} \"{target.Name}\": {string.Join(", ", changed)}");

            var log = new ShareDataActivityLog
            {
                LogType = BaseEnums.LogTypeEnum.CONFIG,
                Action = BaseEnums.ActivityAction.Update,
                TargetType = target.Type,
                TargetId = target.Id,
                TargetName = targetName,
                PartnerId = target.PartnerId,
                PartnerName = partnerName,
                DatatypeId = datatypeId,
                SubscriptionId = target.SubscriptionId,
                BeforeJson = Serialize(before),
                AfterJson = Serialize(after),
                ChangedFields = changed.Count == 0 ? null : string.Join(", ", changed),
                Description = desc,
                OccurredAt = BaseUtil.TimeServer.GetTimeServer(),
                Success = BaseEnums.SuccessEnums.Success,
                OperatorName = CurrentOperator(),
                OperatorIp = CurrentIp()
            };

            await _activityLogRep.InsertAsync(log);
        }

        // ── Hạ tầng ─────────────────────────────────────────────────────

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi bản ghi nhật ký xuống DB. Nuốt mọi lỗi để không làm hỏng nghiệp vụ chính.
        /// Created date: 2026-08-05
        /// </summary>
        private async Task WriteAsync(ShareDataActivityLog log)
        {
            try
            {
                log.OccurredAt ??= DateTime.Now;
                log.Success ??= BaseEnums.SuccessEnums.Success;
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
        /// Author: HieuNV
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
        /// Author: HieuNV
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
        /// Author: HieuNV
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
        /// Author: HieuNV
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
        /// Author: HieuNV
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
        /// Author: HieuNV
        /// Description: Nhãn tiếng Việt của loại đối tượng, dùng dựng câu mô tả.
        /// Created date: 2026-08-05
        /// </summary>
        private static string TargetLabel(BaseEnums.ShareDataTargetType? targetType) => targetType switch
        {
            BaseEnums.ShareDataTargetType.Partner => "đối tác chia sẻ",
            BaseEnums.ShareDataTargetType.Subscription => "đăng ký chia sẻ",
            BaseEnums.ShareDataTargetType.Session => "phiên kết nối",
            BaseEnums.ShareDataTargetType.Packet => "gói tin",
            BaseEnums.ShareDataTargetType.Mapping => "phễu lọc",
            BaseEnums.ShareDataTargetType.CodeSet => "bộ mã quy đổi",
            BaseEnums.ShareDataTargetType.EventSource => "nguồn sự kiện",
            BaseEnums.ShareDataTargetType.PacketField => "field gói tin",
            _ => "bản ghi"
        };
    }
}
