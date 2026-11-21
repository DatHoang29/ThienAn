using Shared.DTO.Constants.Application;

namespace ShareDataWorker.Infrastructure.Logging
{
    /// <summary>
    /// Nơi DUY NHẤT ghi hai bảng nhật ký của ShareData Worker: ShareDataActivityLog và ShareDataAlertLog.
    /// Author: HieuNV
    /// Created date: 07/09/2026
    /// </summary>
    public static class ShareDataTransferLog
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> PacketLabelCache = new();

        /// <summary>
        /// Description: Nhãn gói tin dễ đọc cho nhật ký hiển thị trên giao diện: mã gói thay cho ID nội bộ.
        ///              Không tra được thì dùng nhãn "không xác định", không bao giờ đưa chuỗi GUID ra.
        /// Created date: 22/10/2026
        /// </summary>
        private static async Task<string> ResolvePacketLabelAsync(ISqlSugarClient db, string? datatypeId)
        {
            if (string.IsNullOrWhiteSpace(datatypeId))
                return ShareDataEnum.SystemConstants.Unknown;

            var key = datatypeId.Trim();
            if (PacketLabelCache.TryGetValue(key, out var cached))
                return cached;

            try
            {
                var code = await db.Queryable<ShareDataPacket>()
                    .Where(p => p.ID == key || p.Code == key)
                    .Select(p => p.Code)
                    .FirstAsync();
                if (!string.IsNullOrWhiteSpace(code))
                {
                    PacketLabelCache[key] = code;
                    return code;
                }
            }
            catch (Exception)
            {
                // Không tra được mã thì rơi xuống nhãn mặc định, không làm hỏng việc ghi nhật ký
            }

            return Guid.TryParse(key, out _) ? ShareDataEnum.SystemConstants.Unknown : key;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi một dòng nhật ký truyền nhận. Chiều truyền quyết định Action và câu mô tả,
        ///              các trường còn lại giống hệt nhau ở cả hai luồng.
        /// Created date: 07/09/2026
        /// </summary>
        public static async Task<string> WriteActivityAsync(
            ISqlSugarClient db,
            ShareDataSubscription? sub,
            ShareDataPartner? partner,
            BaseEnums.TransferDirection direction,
            long recordCount = 0,
            long byteSize = 0,
            string? filePath = null,
            string? status = ShareDataEnum.ExportStatus.Success,
            string? errorMessage = null,
            string? hash = null,
            string? mappingId = null,
            string? packetVersion = null,
            int? serialNbr = null,
            string? pduType = null,
            string? description = null,
            string? logId = null,
            string? parentId = null,
            int? stepNbr = null,
            bool isUpdate = false)
        {
            var isSuccess = status == ShareDataEnum.ExportStatus.Success
                            || status == ShareDataEnum.ExportStatus.Queued;

            var isSend = direction == BaseEnums.TransferDirection.SND;
            var partnerName = partner?.Name ?? partner?.Code ?? ShareDataEnum.SystemConstants.Unknown;
            var packetLabel = await ResolvePacketLabelAsync(db, sub?.DatatypeId);
            var serial = serialNbr ?? sub?.SerialNbr;
            var targetName = sub?.Code ?? (!string.IsNullOrEmpty(sub?.DatatypeId) ? $"Gói {packetLabel}" : (sub != null ? "Đăng ký" : null));
            var partnerIp = partner?.Address;
            logId ??= Guid.NewGuid().ToString("N");

            // Chiều gửi: "với đối tác". Chiều nhận: "từ đối tác".
            description ??= isSuccess
                ? isSend
                    ? $"Gửi gói tin loại {packetLabel} với đối tác \"{partnerName}\""
                    : $"Nhận gói tin loại {packetLabel} từ đối tác \"{partnerName}\""
                : isSend
                    ? $"Lỗi gửi gói tin loại {packetLabel} với đối tác \"{partnerName}\""
                    : $"Lỗi nhận gói tin loại {packetLabel} từ đối tác \"{partnerName}\"";

            try
            {
                if (isUpdate && !string.IsNullOrWhiteSpace(logId))
                {
                    await db.Updateable<ShareDataActivityLog>()
                        .SetColumns(x => new ShareDataActivityLog
                        {
                            Success = isSuccess ? BaseEnums.SuccessEnums.Success : BaseEnums.SuccessEnums.Fail,
                            RecordCount = (int)recordCount,
                            ByteSize = byteSize > 0 ? byteSize : x.ByteSize,
                            FilePath = filePath != null ? Cut(filePath, 512) : x.FilePath,
                            ErrorMessage = Cut(errorMessage, 512),
                            Description = description != null ? Cut(description, 512) : x.Description,
                            UpdateTime = DateTime.Now
                        })
                        .Where(x => x.ID == logId)
                        .ExecuteCommandAsync();

                    return logId;
                }

                await db.Insertable(new ShareDataActivityLog
                {
                    ID = logId,
                    LogType = BaseEnums.LogTypeEnum.Transfer,
                    Action = isSend ? BaseEnums.ActivityAction.Send : BaseEnums.ActivityAction.Receive,
                    Success = isSuccess ? BaseEnums.SuccessEnums.Success : BaseEnums.SuccessEnums.Fail,
                    TargetType = BaseEnums.ShareDataTargetType.Subscription,
                    TargetId = sub?.ID,
                    TargetName = Cut(targetName, 128),
                    SubscriptionId = sub?.ID,
                    PartnerId = sub?.PartnerId,
                    PartnerName = Cut(partnerName, 128),
                    SessionId = sub?.SessionId,
                    TransferDirection = direction,
                    DatatypeId = sub?.DatatypeId,
                    SerialNbr = serial,
                    PacketNbr = serial ?? 1,
                    PduType = pduType ?? (sub != null ? ResolvePduType(sub) : ShareDataEnum.PduType.DataPacket),
                    Format = sub?.Format ?? BaseEnums.PublishFormat.Data,
                    OccurredAt = DateTime.Now,
                    RecordCount = (int)recordCount,
                    ByteSize = byteSize,
                    FilePath = Cut(filePath, 512),
                    Hash = Cut(hash, 128),
                    MappingId = mappingId,
                    PacketVersion = packetVersion,
                    ErrorMessage = Cut(errorMessage, 512),
                    OperatorName = ShareDataEnum.Operator.System,
                    OperatorIp = Cut(partnerIp, 64),
                    Remark = ShareDataEnum.FlowType.External,
                    Description = Cut(description, 512),
                    ParentId = parentId,
                    StepNbr = stepNbr
                }).ExecuteCommandAsync();
            }
            catch (Exception)
            {
                // Nuốt lỗi ghi nhật ký để không làm sập luồng nghiệp vụ chính
            }

            return logId;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cắt chuỗi cho vừa độ dài cột.
        /// Created date: 07/09/2026
        /// </summary>
        private static string? Cut(string? value, int maxLength)
            => string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];

        /// <summary>
        /// Description: Xác định PduType (Gói dữ liệu / Gói tập tin) dùng cho ghi nhật ký, theo cấu hình Format của Subscription.
        /// Created date: 25/09/2026
        /// </summary>
        private static string ResolvePduType(ShareDataSubscription sub) =>
            sub.Format == BaseEnums.PublishFormat.File ? ShareDataEnum.PduType.FilePacket : ShareDataEnum.PduType.DataPacket;

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi một cảnh báo hệ thống. Mã cảnh báo lấy từ ShareDataAlertCode.
        /// Created date: 07/09/2026
        /// </summary>
        public static async Task<string> WriteAlertAsync(
            ISqlSugarClient db,
            ShareDataSubscription? sub,
            string alertCode,
            BaseEnums.AlertSeverity severity,
            BaseEnums.AlertSource alertSource,
            string message,
            string? detailJson = null,
            ShareDataPartner? partner = null)
        {
            var logId = Guid.NewGuid().ToString("N");

            try
            {
                await db.Insertable(new ShareDataAlertLog
                {
                    ID = logId,
                    OccurredAt = DateTime.Now,
                    Severity = severity,
                    AlertSource = alertSource,
                    AlertCode = alertCode,
                    PartnerId = sub?.PartnerId ?? partner?.ID,
                    SessionId = sub?.SessionId,
                    SubscriptionId = sub?.ID,
                    DatatypeId = sub?.DatatypeId,
                    Message = Cut(message, 1000),
                    DetailJson = detailJson,
                    Acknowledged = false
                }).ExecuteCommandAsync();
            }
            catch (Exception)
            {
                // Nuốt lỗi ghi cảnh báo để không làm sập luồng nghiệp vụ chính
            }

            return logId;
        }

        /// <summary>
        /// Description: Ghi Alert + ghi ActivityLog trạng thái Failed cho lần xuất bản thất bại.
        ///              Hỗ trợ ghi log bước con (stepNbr) và chốt dòng cha (parentId) khi có sự cố.
        /// Created date: 25/09/2026
        /// </summary>
        public static async Task WriteFailureLogs(
            ISqlSugarClient db,
            ShareDataSubscription sub,
            ShareDataPartner? partner,
            string alertCode,
            BaseEnums.AlertSeverity severity,
            BaseEnums.AlertSource alertSource,
            string message,
            string? detailJson = null,
            long recordCount = 0,
            long byteSize = 0,
            string? filePath = null,
            string? mappingId = null,
            string? packetVersion = null,
            string? parentId = null,
            int? stepNbr = null)
        {
            await WriteAlertAsync(db, sub, alertCode, severity, alertSource, message, detailJson, partner);

            if (stepNbr.HasValue && !string.IsNullOrEmpty(parentId))
            {
                var stepDesc = stepNbr.Value switch
                {
                    1 => "Trích xuất dữ liệu thất bại",
                    2 => "Ánh xạ dữ liệu thất bại",
                    3 => "Gửi dữ liệu tới đối tác thất bại",
                    _ => $"Bước {stepNbr.Value} thất bại"
                };

                await WriteActivityAsync(db, sub, partner, BaseEnums.TransferDirection.SND,
                    recordCount: recordCount, byteSize: byteSize, filePath: filePath,
                    status: ShareDataEnum.ExportStatus.Failed, errorMessage: message,
                    mappingId: mappingId, packetVersion: packetVersion,
                    serialNbr: sub.SerialNbr,
                    description: stepDesc,
                    parentId: parentId, stepNbr: stepNbr);
            }

            await WriteActivityAsync(db, sub, partner, BaseEnums.TransferDirection.SND,
                recordCount: recordCount, byteSize: byteSize, filePath: filePath,
                status: ShareDataEnum.ExportStatus.Failed, errorMessage: message,
                mappingId: mappingId, packetVersion: packetVersion,
                serialNbr: sub?.SerialNbr,
                logId: parentId,
                isUpdate: !string.IsNullOrEmpty(parentId));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghi rủi ro của câu truy vấn nối đuôi vào nhật ký cấu hình (LogType CONFIG): không phải một lần gửi nên không lẫn vào nhật ký truyền nhận và số liệu gửi.
        /// Created date: 01/11/2026
        /// </summary>
        public static async Task WriteCursorRiskAsync(
            ISqlSugarClient db,
            ShareDataSubscription sub,
            ShareDataPartner? partner,
            string message,
            CancellationToken cancelToken = default)
        {
            var packetLabel = await ResolvePacketLabelAsync(db, sub.DatatypeId);
            await db.Insertable(new ShareDataActivityLog
            {
                ID = Guid.NewGuid().ToString("N"),
                LogType = BaseEnums.LogTypeEnum.CONFIG,
                Action = BaseEnums.ActivityAction.Export,
                Success = BaseEnums.SuccessEnums.Success,
                OccurredAt = DateTime.Now,
                TargetType = BaseEnums.ShareDataTargetType.Subscription,
                TargetId = sub.ID,
                TargetName = Cut(sub.Code ?? $"Gói {packetLabel}", EntityConst.Length128),
                PartnerId = sub.PartnerId,
                PartnerName = Cut(partner?.Name ?? partner?.Code, EntityConst.Length128),
                DatatypeId = sub.DatatypeId,
                Description = ShareDataEnum.ExportMessage.CursorSqlRisk,
                ErrorMessage = Cut(message, EntityConst.Length512),
                OperatorName = "ShareDataWorker",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            }).ExecuteCommandAsync(cancelToken);
        }

        /// <summary>
        /// Description: Ghi một sự cố HẠ TẦNG Change Tracking vào ShareDataActivityLog. Mã ESH-16xx đặt ở cột
        ///              Remark để tác vụ dọn quá hạn nhận diện chính xác nhóm này mà không đụng nhật ký nghiệp vụ.
        ///              Mọi lỗi khi ghi đều bị nuốt có chủ đích — mất 1 dòng nhật ký không đáng để sập chu kỳ polling.
        /// Created date: 27/09/2026
        /// </summary>
        public static async Task WriteTrackingAsync(
            ISqlSugarClient db,
            string alertCode,
            string description,
            string? errorMessage = null,
            string? detailJson = null,
            CancellationToken cancelToken = default)
        {
            try
            {
                await db.Insertable(new ShareDataActivityLog
                {
                    ID = Guid.NewGuid().ToString("N"),
                    LogType = BaseEnums.LogTypeEnum.Transfer,
                    OccurredAt = DateTime.Now,
                    Success = BaseEnums.SuccessEnums.Fail,
                    Description = Cut(description, EntityConst.Length512),
                    ErrorMessage = Cut(errorMessage, EntityConst.Length512),
                    TargetName = Cut($"DataChangeTrackingWorker@{Environment.MachineName}:{Environment.ProcessId}", EntityConst.Length128),
                    AfterJson = detailJson,
                    Remark = alertCode,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                }).ExecuteCommandAsync(cancelToken);
            }
            catch (Exception)
            {
                // Nuốt lỗi ghi nhật ký hạ tầng để không làm sập chu kỳ polling
            }
        }
    }
}
