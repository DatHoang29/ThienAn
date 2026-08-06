using Modules.ShareDataWorker.Core.Entities.Source;

namespace Modules.ShareDataWorker.Infrastructure.Services
{
    /// <summary>
    /// Background Worker Service xử lý kết xuất & truyền dữ liệu định kỳ theo CronJob (ESHARE V1)
    /// Author: Đạt
    /// Created date: 31/07/2026
    /// </summary>
    public class ShareDataExportService(IServiceScopeFactory scopeFactory, ILogger<ShareDataExportService> logger) : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);
        private const int DefaultIntervalSeconds = 60;

        public static readonly Dictionary<EshEnums.DatatypeIdEnum, Func<ISqlSugarClient, DateTime?, Task<List<object>>>> PacketQueryRegistry = new()
        {
            [EshEnums.DatatypeIdEnum.TrafficFlow] = async (db, _) =>
                (await db.Queryable<TmsZoneStatus>()
                    .LeftJoin<TmsZone>((zs, z) => zs.ZoneId == z.ID)
                    .LeftJoin<TmsTrafficStatistic>((zs, z, ts) => zs.ZoneId == ts.ZoneId)
                    .Select((zs, z, ts) => new
                    {
                        zoneId = zs.ZoneId,
                        zoneName = z.Name,
                        fromLocationKm = z.FromKmNumber,
                        fromLocationMet = z.FromMetNumber,
                        toLocationKm = z.ToKmNumber,
                        toLocationMet = z.ToMetNumber,
                        laneId = z.LaneId,
                        averageSpeed = (decimal?)SqlFunc.ToDecimal(zs.AverageSpeed),
                        trafficCondition = zs.Condition,
                        dataTime = zs.UpdateTime,
                        speedLimit = z.MaxSpeed,
                        vehicleCount = ts.TotalVehicleNumber
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.CctvImage] = async (db, _) =>
                (await db.Queryable<CctvDevice>()
                    .LeftJoin<TmsEquipment>((c, e) => c.Ip == e.Ip)
                    .Select((c, e) => new
                    {
                        cameraCode = e.Code,
                        cameraName = c.Name,
                        snapshot = c.SnapshotUrl,
                        snapshotTime = c.SnapshotTime,
                        deviceState = c.DeviceState,
                        locationKm = e.KmNumber,
                        locationMet = e.MetNumber,
                        direction = e.DirectionId
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.VehicleDetection] = async (db, lastTime) =>
                (await db.Queryable<TmsTrafficData>()
                    .LeftJoin<TmsEquipment>((td, e) => td.EquipmentId == e.ID)
                    .WhereIF(lastTime.HasValue, td => td.DetectTime > lastTime!.Value)
                    .Select((td, e) => new
                    {
                        detectionId = td.ID,
                        detectTime = td.DetectTime,
                        vehicleType = td.Type,
                        licensePlate = td.LicensePlate,
                        speed = td.Speed,
                        lane = td.Lane,
                        direction = td.Direction,
                        locationRoute = td.Location,
                        equipmentId = td.EquipmentId,
                        locationKm = e.KmNumber,
                        locationMet = e.MetNumber
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.Weather] = async (db, lastTime) =>
                (await db.Queryable<TmsWeather>()
                    .WhereIF(lastTime.HasValue, w => w.TimeDetect > lastTime!.Value)
                    .Select(w => new
                    {
                        weatherStationId = w.RefId,
                        locationDetail = w.LocationDetail,
                        temperature = w.Temperature,
                        humidity = w.Hudmidity,
                        windSpeed = w.WindSpeed,
                        windDirection = w.WindDirection,
                        rainfall = w.Rain,
                        rainfallHour = w.RainHour,
                        visibility = w.Foresight,
                        weatherDescription = w.Description,
                        weatherCode = w.ShortDescription,
                        detectTime = w.TimeDetect
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.VehicleIdentification] = async (db, lastTime) =>
                (await db.Queryable<TollTransactionOut>()
                    .LeftJoin<TmsVehicleRegistration>((t, vr) => SqlFunc.IsNull(t.PlateEdit, t.PlateLpr) == vr.Plate)
                    .WhereIF(lastTime.HasValue, t => t.TransactionDateTime > lastTime!.Value)
                    .Select((t, vr) => new
                    {
                        transactionId = t.TransactionId,
                        tagId = t.TagId,
                        licensePlate = SqlFunc.IsNull(t.PlateEdit, t.PlateLpr),
                        vehicleTypeId = t.VehicleTypeId,
                        entryTime = t.TransactionDateTimeIn,
                        exitTime = t.TransactionDateTime,
                        laneId = t.LaneId,
                        stationId = t.StationId,
                        vehicleBrand = vr.Brand,
                        vehicleOwner = vr.Owner
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.WeighInMotion] = async (db, lastTime) =>
                (await db.Queryable<TmsTrafficData>()
                    .WhereIF(lastTime.HasValue, td => td.DetectTime > lastTime!.Value)
                    .Select(td => new
                    {
                        detectTime = td.DetectTime,
                        lane = td.Lane,
                        locationCode = td.Location,
                        speed = td.Speed,
                        height = td.Height,
                        width = td.Width,
                        length = td.Length
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.TrafficIncident] = async (db, lastTime) =>
                (await db.Queryable<TmsIncident>()
                    .LeftJoin<TmsEventType>((i, et) => i.EventTypeId == et.ID)
                    .WhereIF(lastTime.HasValue, i => SqlFunc.IsNull(i.UpdateTime, i.StartDate) > lastTime!.Value)
                    .Select((i, et) => new
                    {
                        incidentCode = i.Code,
                        incidentName = i.Name,
                        eventTypeId = i.EventTypeId,
                        eventTypeName = et.Name,
                        occurredTime = i.StartDate,
                        locationKm = i.KmNumber,
                        locationMet = i.MetNumber,
                        locationRoute = i.Location,
                        direction = i.InfluenceScope,
                        injuredCount = i.InjuredNumber,
                        vehicleCount = i.VehicleNumber,
                        incidentState = i.State,
                        description = i.Description,
                        source = i.Source
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.VmsDisplay] = async (db, _) =>
                (await db.Queryable<VmsCurrent>()
                    .LeftJoin<TmsEquipment>((v, e) => v.EquipmentId == e.ID)
                    .Select((v, e) => new
                    {
                        equipmentCode = e.Code,
                        vmsName = v.Name,
                        locationKm = e.KmNumber,
                        locationMet = e.MetNumber,
                        direction = e.DirectionId,
                        laneId = e.LaneId,
                        displayContent = v.RowData,
                        displayImageUrl = v.Url,
                        displaySize = v.Size,
                        priority = v.Priority,
                        executedTime = v.ExecutedDate
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.TollCollection] = async (db, lastTime) =>
                (await db.Queryable<TollTransactionOut>()
                    .LeftJoin<TollLane>((t, l) => t.LaneId == l.LaneId)
                    .LeftJoin<TollStation>((t, l, s) => t.StationId == s.StationId)
                    .WhereIF(lastTime.HasValue, t => t.TransactionDateTime > lastTime!.Value)
                    .Select((t, l, s) => new
                    {
                        transactionId = t.TransactionId,
                        entryTime = t.TransactionDateTimeIn,
                        exitTime = t.TransactionDateTime,
                        vehicleTypeId = t.VehicleTypeId,
                        licensePlate = SqlFunc.IsNull(t.PlateEdit, t.PlateLpr),
                        tagId = t.TagId,
                        laneId = t.LaneId,
                        laneName = l.Name,
                        stationId = t.StationId,
                        stationName = s.Name,
                        tollPrice = (decimal?)null,
                        syncTime = t.SyncTime
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.PublicMessaging] = async (db, lastTime) =>
                (await db.Queryable<TmsIncident>()
                    .LeftJoin<VmsCurrent>((i, v) => true)
                    .WhereIF(lastTime.HasValue, i => i.StartDate > lastTime!.Value)
                    .Select((i, v) => new
                    {
                        incidentMessage = SqlFunc.MergeString(i.Name, " - ", i.Description),
                        guidanceContent = v.RowData,
                        locationKm = i.KmNumber,
                        locationMet = i.MetNumber,
                        publishedTime = i.StartDate
                    })
                    .ToListAsync()).Cast<object>().ToList(),

            [EshEnums.DatatypeIdEnum.InterCenterExchange] = async (db, lastTime) =>
                (await db.Queryable<TmsSignalLog>()
                    .WhereIF(lastTime.HasValue, sl => sl.CreateTime > lastTime!.Value)
                    .Select(sl => new
                    {
                        packetType = ((int)EshEnums.DatatypeIdEnum.VehicleDetection).ToString(),
                        controlCommand = sl.NewData,
                        controlState = sl.State,
                        createdTime = sl.CreateTime
                    })
                    .ToListAsync()).Cast<object>().ToList()
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogInformationMsg(BaseMsg.Worker.Started, DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchSubscriptions(stoppingToken);
                }
                catch (Exception ex)
                {
                    LogInformationMsg($"❌ Lỗi trong quá trình xử lý kết xuất dữ liệu: {ex.Message}");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            LogInformationMsg(BaseMsg.Worker.Stopping, DateTimeOffset.Now);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Xử lý lô các Subscriptions đến hạn kết xuất dữ liệu
        /// Created date: 04/08/2026
        /// </summary>
        public async Task ProcessBatchSubscriptions(CancellationToken stoppingToken = default)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var now = DateTime.Now;
            var lockTimeoutDate = now.Subtract(LockTimeout);
            List<ShareDataSubscription> subs = [];

            await db.Ado.UseTranAsync(async () =>
            {
                subs = await db.Queryable<ShareDataSubscription>()
                    .With(SqlWith.UpdLock)
                    .Where(s => s.State == EshEnums.SubState.Active
                                && (s.Mode == EshEnums.SubMode.Batch || s.Mode == EshEnums.SubMode.Periodic)
                                && s.Direction == EshEnums.SubDirection.Outbound
                                && (s.RunStatus == null || s.RunStatus != EshEnums.RunStatus.Running || s.UpdateTime <= lockTimeoutDate)
                                && (s.NextTimeRun == null || s.NextTimeRun <= now))
                    .ToListAsync(stoppingToken);

                if (subs.Count == 0)
                    return;

                var subIds = subs.Select(s => s.ID).ToList();
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => new ShareDataSubscription
                    {
                        RunStatus = EshEnums.RunStatus.Running,
                        UpdateTime = now
                    })
                    .Where(s => subIds.Contains(s.ID))
                    .ExecuteCommandAsync(stoppingToken);
            });

            if (subs.Count == 0)
            {
                LogInformationMsg("ℹ️ Không có Subscription nào đến hạn cần xử lý.");
                return;
            }

            foreach (var sub in subs)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                await ExecuteExport(sub);
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Truy vấn dữ liệu cho Subscription theo cấu hình Entity mới ShareDataDataSource, với Fallback về PacketQueryRegistry
        /// Created date: 05/08/2026
        /// </summary>
        private async Task<List<object>> FetchDataForSubscription(SqlSugarClient db, ShareDataSubscription sub)
        {
            List<object>? data = null;
            ShareDataMappingProfile? mp = null;

            try
            {
                if (!string.IsNullOrEmpty(sub.MappingProfileId))
                {
                    mp = await db.Queryable<ShareDataMappingProfile>().InSingleAsync(sub.MappingProfileId);
                    if (string.IsNullOrEmpty(sub.DatatypeId) && !string.IsNullOrEmpty(mp?.DatatypeId))
                        sub.DatatypeId = mp.DatatypeId;
                }

                // Nếu có MappingsJson cấu hình -> Ưu tiên dùng ShareDataDataSource và map field
                if (mp != null && !string.IsNullOrWhiteSpace(mp.MappingsJson))
                {
                    string? dsId = sub.DataSourceId ?? mp.DataSourceId;
                    if (!string.IsNullOrEmpty(dsId))
                    {
                        var ds = await db.Queryable<ShareDataDataSource>().InSingleAsync(dsId);
                        if (ds != null)
                        {
                            if (string.Equals(ds.Kind, "SAVED_QUERY", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ds.QueryText))
                            {
                                var rawList = await db.Ado.SqlQueryAsync<dynamic>(ds.QueryText);
                                data = rawList.Cast<object>().ToList();
                            }
                            else if (string.Equals(ds.Kind, "FIELD_PICKER", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ds.TableOrView))
                            {
                                var topN = ds.TopN.HasValue && ds.TopN.Value > 0 ? ds.TopN.Value : 50;
                                var rawList = await db.Queryable(ds.TableOrView, "t").Take(topN).ToListAsync();
                                data = rawList.Cast<object>().ToList();
                            }
                        }
                    }

                    if (data != null && data.Count > 0)
                        data = ApplyFieldMappings(data, mp.MappingsJson);
                }
            }
            catch (Exception ex)
            {
                LogInformationMsg($"⚠️ Truy vấn theo ShareDataDataSource/MappingsJson gặp sự cố, chuyển sang dùng PacketQueryRegistry. Chi tiết: {ex.Message}");
            }

            // Nếu không có MappingsJson (hoặc DataSource chưa lấy được dữ liệu) -> Dùng PacketQueryRegistry làm mặc định
            if ((data == null || data.Count == 0) && TryResolveDatatypeEnum(sub.DatatypeId!, out var datatypeEnum))
            {
                if (PacketQueryRegistry.TryGetValue(datatypeEnum, out var queryFunc))
                {
                    data = await queryFunc(db, sub.UpdateTime);
                }
            }

            return data ?? [];
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Thực thi quy trình xuất dữ liệu JSON cho 1 đăng ký nhận tin (Subscription)
        /// Created date: 04/08/2026
        /// </summary>
        private async Task ExecuteExport(ShareDataSubscription sub)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            using var db = baseClient.CopyNew();
            var now = DateTime.Now;
            var partner = !string.IsNullOrEmpty(sub.PartnerId) ? await db.Queryable<ShareDataPartner>().InSingleAsync(sub.PartnerId) : null;

            try
            {
                // 1. Kiểm tra tính hợp lệ của cấu hình Subscription trước khi truy vấn dữ liệu
                var hasValidDatatypeEnum = TryResolveDatatypeEnum(sub.DatatypeId!, out _);
                if (!hasValidDatatypeEnum && string.IsNullOrEmpty(sub.DataSourceId) && string.IsNullOrEmpty(sub.MappingProfileId))
                {
                    var queryError = $"Không tìm thấy hàm truy vấn dữ liệu cho Subscription ID {sub.ID} (Gói tin {sub.DatatypeId}).";
                    LogInformationMsg($"❌ {queryError}");
                    await LogExportResult(db, sub, partner, 0, 0, null, EshEnums.ExportStatus.Failed, queryError);
                    return;
                }

                // 2. Thực hiện truy vấn dữ liệu sau khi đã xác nhận Subscription hợp lệ
                var data = await FetchDataForSubscription(db, sub);
                if (data == null || data.Count == 0)
                {
                    LogInformationMsg($"ℹ️ Không có dữ liệu mới cho Subscription ID {sub.ID}. Bỏ qua xuất file JSON.");
                    await LogExportResult(db, sub, partner, 0, 0, null, EshEnums.ExportStatus.Success, "Không có dữ liệu mới");
                    return;
                }

                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var byteSize = (long)jsonBytes.Length;
                var vendorCode = partner?.Code ?? partner?.Name ?? sub.PartnerId ?? EshEnums.SystemConstants.Unknown;
                var dataTypeId = sub.DatatypeId ?? EshEnums.SystemConstants.Unknown;
                var relativePath = GenerateExportRelativePath(vendorCode, dataTypeId, now, sub.SerialNbr);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
                var directoryPath = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                await File.WriteAllBytesAsync(fullPath, jsonBytes);
                await LogExportResult(db, sub, partner, data.Count, byteSize, relativePath, EshEnums.ExportStatus.Success);
                LogInformationMsg($"✅ Kết xuất dữ liệu THÀNH CÔNG cho Subscription ID {sub.ID} ({data.Count} bản ghi, {byteSize} bytes) -> File: {relativePath}");
            }
            catch (Exception ex)
            {
                LogInformationMsg($"❌ Lỗi kết xuất dữ liệu cho Subscription ID {sub.ID}: {ex.Message}");
                await LogExportResult(db, sub, partner, 0, 0, null, EshEnums.ExportStatus.Failed, ex.Message);
            }
            finally
            {
                var finishedTime = DateTime.Now;
                var intervalSec = sub.IntervalSeconds > 0 ? sub.IntervalSeconds : DefaultIntervalSeconds;
                var nextTimeRun = finishedTime.AddSeconds(intervalSec);

                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(x => new ShareDataSubscription
                    {
                        RunStatus = EshEnums.RunStatus.Idle,
                        NextTimeRun = nextTimeRun,
                        UpdateTime = finishedTime
                    })
                    .Where(x => x.ID == sub.ID)
                    .ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Chuyển đổi mã chuỗi hoặc số int sang DatatypeIdEnum hợp lệ
        /// Created date: 04/08/2026
        /// </summary>
        private static bool TryResolveDatatypeEnum(string datatypeIdStr, out EshEnums.DatatypeIdEnum datatypeEnum)
        {
            datatypeEnum = default;
            if (string.IsNullOrWhiteSpace(datatypeIdStr))
                return false;

            if (Enum.TryParse(datatypeIdStr, true, out datatypeEnum) && (Enum.IsDefined(datatypeEnum) || PacketQueryRegistry.ContainsKey(datatypeEnum)))
                return true;

            return false;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi nhận nhật ký kết xuất dữ liệu vào bảng ShareDataActivityLog với đầy đủ trường chuẩn truyền nhận
        /// Created date: 04/08/2026
        /// </summary>
        private static async Task LogExportResult(SqlSugarClient db, ShareDataSubscription sub, ShareDataPartner? partner,
            long recordCount, long byteSize, string? filePath, string? status, string? errorMessage = null)
        {
            var isSuccess = status == EshEnums.ExportStatus.Success;
            var directionLabel = "Gửi";
            var partnerName = partner?.Name ?? partner?.Code ?? sub.PartnerId;
            int? serialNbr = int.TryParse(sub.SerialNbr, out var parsedSn) ? parsedSn : null;

            await db.Insertable(new ShareDataActivityLog
            {
                LogType = "TRANSFER",
                Action = "SEND",
                Status = isSuccess ? "SUCCESS" : "FAILED",
                TargetType = "Subscription",
                TargetId = sub.ID,
                TargetName = sub.SerialNbr,
                SubscriptionId = sub.ID,
                PartnerId = sub.PartnerId,
                PartnerName = partnerName,
                SessionId = sub.SessionId,
                TransferDirection = "SND",
                DatatypeId = sub.DatatypeId,
                SerialNbr = serialNbr,
                PduType = sub.Format == "FILE" ? "FILE_PACKET" : "DATA_PACKET",
                Format = sub.Format ?? "DATA",
                OccurredAt = DateTime.Now,
                RecordCount = (int)recordCount,
                ByteSize = byteSize,
                FilePath = filePath,
                ErrorMessage = errorMessage,
                OperatorName = EshEnums.Operator.System,
                Description = isSuccess
                    ? $"{directionLabel} gói tin loại {sub.DatatypeId} với đối tác \"{partnerName}\""
                    : $"Lỗi {directionLabel.ToLower()} gói tin loại {sub.DatatypeId} với đối tác \"{partnerName}\": {errorMessage}"
            }).ExecuteCommandAsync();
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Phát sinh đường dẫn tương đối lưu tập tin JSON kết xuất
        /// Created date: 04/08/2026
        /// </summary>
        public static string GenerateExportRelativePath(string partnerCode, string datatypeId, DateTime time,
            string? subSerialNbr = null, string extension = "json")
        {
            var ext = extension.TrimStart('.').ToLower();
            var serialSuffix = string.IsNullOrWhiteSpace(subSerialNbr) ? "" : $"_{subSerialNbr}";
            return
                $"Out/{partnerCode}/{time:yyyyMM}/{time:ddHH}/{datatypeId}/{datatypeId}{serialSuffix}_{time:yyyyMMddHHmmss}.{ext}";
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ánh xạ đổi tên các trường dữ liệu theo MappingsJson cấu hình trong ShareDataMappingProfile
        /// Created date: 05/08/2026
        /// </summary>
        private static List<object> ApplyFieldMappings(List<object> rawData, string? mappingsJson)
        {
            if (rawData == null || rawData.Count == 0 || string.IsNullOrWhiteSpace(mappingsJson))
                return rawData ?? [];

            try
            {
                var mappings = ParseMappingsJson(mappingsJson);
                if (mappings.Count == 0)
                    return rawData;

                var result = new List<object>();
                foreach (var item in rawData)
                {
                    IDictionary<string, object?>? dict = null;
                    if (item is IDictionary<string, object?> d)
                    {
                        dict = d;
                    }
                    else if (item is JsonElement je && je.ValueKind == JsonValueKind.Object)
                    {
                        dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText());
                    }
                    else
                    {
                        var json = JsonSerializer.Serialize(item);
                        dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                    }

                    if (dict == null)
                    {
                        result.Add(item);
                        continue;
                    }

                    var mappedRecord = new Dictionary<string, object?>();
                    foreach (var (sourceField, targetField) in mappings)
                    {
                        var matchedKey = dict.Keys.FirstOrDefault(k => string.Equals(k, sourceField, StringComparison.OrdinalIgnoreCase));
                        mappedRecord[targetField] = matchedKey != null ? dict[matchedKey] : null;
                    }

                    result.Add(mappedRecord);
                }
                return result;
            }
            catch
            {
                return rawData;
            }
        }

        private static List<(string sourceField, string targetField)> ParseMappingsJson(string mappingsJson)
        {
            var list = new List<(string sourceField, string targetField)>();
            try
            {
                using var doc = JsonDocument.Parse(mappingsJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in doc.RootElement.EnumerateArray())
                    {
                        string? src = null;
                        string? tgt = null;

                        if (elem.TryGetProperty("sourceField", out var pSrc) || elem.TryGetProperty("source", out pSrc) || elem.TryGetProperty("SourceField", out pSrc))
                            src = pSrc.GetString();

                        if (elem.TryGetProperty("targetField", out var pTgt) || elem.TryGetProperty("target", out pTgt) || elem.TryGetProperty("TargetField", out pTgt))
                            tgt = pTgt.GetString();

                        if (!string.IsNullOrWhiteSpace(src) && !string.IsNullOrWhiteSpace(tgt))
                            list.Add((src, tgt));
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var tgt = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(prop.Name) && !string.IsNullOrWhiteSpace(tgt))
                            list.Add((prop.Name, tgt));
                    }
                }
            }
            catch
            {
                // ignore parse errors
            }
            return list;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi log thông tin nội bộ cho dịch vụ Background Worker
        /// Created date: 04/08/2026
        /// </summary>
        private void LogInformationMsg(string message, params object?[] args)
        {
#if DEBUG
            logger.LogInformation(message, args);
#endif
        }
    }
}