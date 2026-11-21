using System.Globalization;
using System.Text.Json;
using ITS.Toll.Core.Configuration;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Models;
using ITS.Toll.Core.Persistence;
using Microsoft.Extensions.Options;
using Modules.TOLL.Core.Entities;
using Services.Shared.Configuration;
using Services.Shared.Persistence;

namespace ITS.Toll.Core.Services;

/// <summary>
/// Lưu dữ liệu lấy từ API ETC xuống DB ITS theo từng DataType của endpoint:
///   - Master        → TollMasterData (VehicleType/TicketType/PriceType/Reason/Payment/Status/TransactionStatus)
///                      + TollRoad (tcsRoads) + TollPriceList (tcsPriceLists) + TollPriceListDetail (tcsPriceListDetails).
///   - Station        → TollStation (API mdstram/allstation, khóa: id).
///   - Lane           → TollLane    (API mdslane/alllane, khóa: laneId).
///   - Shift          → TollShift   (API acs13quanly/mdsca/list, khóa: id).
///   - TransactionIn  → TollTransactionIn, TransactionOut → TollTransactionOut (khóa: transaction_ID).
///   - OutSummary     → TollTicketOutSummary (khóa: id).
///   - AccessLog      → TollAccessHistory (API appaccesslog/accesshistorytcs, result.items;
///                      TCS không trả ID nên khóa chống trùng = EmployeeCode+AccessTime+Status).
/// Quy tắc chung:
///   - Dữ liệu danh mục/snapshot (Master, Road, PriceList, PriceListDetail, Station, Lane, Shift):
///     Insert nếu mới / CHỈ Update khi dữ liệu nghiệp vụ thay đổi (tính Unchanged) / XÓA bản ghi trong DB nếu thừa so với TCS.
///   - Dữ liệu giao dịch/tổng hợp/nhật ký theo thời gian (TransactionIn, TransactionOut, OutSummary, AccessLog):
///     Insert/Update theo khoảng thời gian, không xóa snapshot (xóa lẻ qua luồng push Delete).
/// Author: ChuongLP
/// Created date: 24/08/2026
/// </summary>
public sealed class TollDataStore : TollRepositoryBase
{
    private readonly TollLogger _logger;

    public TollDataStore(DatabaseManager databaseManager, IOptions<DbConnectionOptions> dbOptions, TollServiceOptions service, TollLogger logger)
        : base(databaseManager, dbOptions, service)
    {
        _logger = logger;
    }

    /// <summary>
    /// Lưu dữ liệu theo DataType của endpoint. Các trường ảnh được lưu NGUYÊN VĂN như API trả
    /// (đường dẫn tương đối đầy đủ, vd "HAUKIEM\HNCL\...\xxx.jpg") — không rút gọn, không ghép thêm.
    /// Author: ChuongLP
    /// Created date: 24/08/2026 
    /// </summary>
    public async Task<TollSaveResult> SaveAsync(TollApiCallResult result, CancellationToken cancellationToken = default)
    {
        if (!result.Success)
            return new TollSaveResult(false, "Bỏ qua lưu: lần gọi API không thành công.");

        _logger.Info(LogCategory.Sync, $"Lưu DB [{result.DataType}]…");

        var save = result.DataType switch
        {
            "Master" => await ImportMasterAsync(result.RawContent, cancellationToken),
            "Station" => await ImportStationAsync(result.RawContent, cancellationToken),
            "Lane" => await ImportLaneAsync(result.RawContent, cancellationToken),
            "Shift" => await ImportShiftAsync(result.RawContent, cancellationToken),
            "TransactionIn" => await ImportTransactionInAsync(result.RawContent, cancellationToken),
            "TransactionOut" => await ImportTransactionOutAsync(result.RawContent, cancellationToken),
            "OutSummary" => await ImportOutSummaryAsync(result.RawContent, cancellationToken),
            "AccessLog" => await ImportAccessLogAsync(result.RawContent, cancellationToken),
            _ => new TollSaveResult(false, $"Chưa hỗ trợ lưu DataType='{result.DataType}'."),
        };

        if (save.Ok)
            _logger.Success(LogCategory.Sync, $"   Kết quả lưu DB: {save.Message}");
        else
            _logger.Error(LogCategory.Sync, $"   Lưu DB thất bại: {save.Message}");

        return save;
    }

    // ================== PUSH 1 BẢN GHI (TCS đẩy realtime) ==================
    // Chỉ hỗ trợ dữ liệu giao dịch/tổng hợp (có thay đổi/hậu kiểm): TransactionIn, TransactionOut, OutSummary.
    // Master/danh mục không nhận qua push (đã có luồng pull tự động).

    /// <summary>
    /// Upsert MỘT bản ghi TCS đẩy sang (Insert/Update gộp chung để idempotent): có theo khóa nghiệp vụ thì
    /// cập nhật (chỉ khi dữ liệu đổi), chưa có thì thêm mới. Dùng lại các hàm map (FillIn/FillOut/FillSummary).
    /// Author: ChuongLP
    /// Created date: 12/09/2026
    /// </summary>
    public async Task<TollSaveResult> UpsertRecordAsync(string dataType, JsonElement item, CancellationToken ct = default)
    {
        var client = Client;
        var now = DateTime.Now;
        try
        {
            switch (dataType)
            {
                case "TransactionIn":
                {
                    var tid = GetStr(item, "transaction_ID");
                    if (string.IsNullOrWhiteSpace(tid))
                        return new TollSaveResult(false, "Thiếu 'transaction_ID' trong dữ liệu TransactionIn.");
                    var exist = await client.Queryable<TollTransactionIn>().Where(x => x.TransactionId == tid).FirstAsync();
                    if (exist != null)
                    {
                        if (TollEntityDiff.ApplyAndCheckChanged(exist, FillIn, item))
                        {
                            exist.SyncTime = now;
                            await client.Updateable(exist).ExecuteCommandAsync();
                            return new TollSaveResult(true, $"Đã cập nhật TransactionIn '{tid}'.", Received: 1, Updated: 1);
                        }
                        return new TollSaveResult(true, $"TransactionIn '{tid}' không đổi.", Received: 1, Unchanged: 1);
                    }
                    var entity = new TollTransactionIn { TransactionId = tid, SyncTime = now };
                    FillIn(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    return new TollSaveResult(true, $"Đã thêm mới TransactionIn '{tid}'.", Received: 1, Inserted: 1);
                }

                case "TransactionOut":
                {
                    var tid = GetStr(item, "transaction_ID");
                    if (string.IsNullOrWhiteSpace(tid))
                        return new TollSaveResult(false, "Thiếu 'transaction_ID' trong dữ liệu TransactionOut.");
                    var exist = await client.Queryable<TollTransactionOut>().Where(x => x.TransactionId == tid).FirstAsync();
                    if (exist != null)
                    {
                        if (TollEntityDiff.ApplyAndCheckChanged(exist, FillOut, item))
                        {
                            exist.SyncTime = now;
                            await client.Updateable(exist).ExecuteCommandAsync();
                            return new TollSaveResult(true, $"Đã cập nhật TransactionOut '{tid}'.", Received: 1, Updated: 1);
                        }
                        return new TollSaveResult(true, $"TransactionOut '{tid}' không đổi.", Received: 1, Unchanged: 1);
                    }
                    var entity = new TollTransactionOut { TransactionId = tid, SyncTime = now };
                    FillOut(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    return new TollSaveResult(true, $"Đã thêm mới TransactionOut '{tid}'.", Received: 1, Inserted: 1);
                }

                case "OutSummary":
                {
                    var sourceId = GetStr(item, "id");
                    if (string.IsNullOrWhiteSpace(sourceId))
                        return new TollSaveResult(false, "Thiếu 'id' trong dữ liệu OutSummary.");
                    var exist = await client.Queryable<TollTicketOutSummary>().Where(x => x.SourceId == sourceId).FirstAsync();
                    if (exist != null)
                    {
                        if (TollEntityDiff.ApplyAndCheckChanged(exist, FillSummary, item))
                        {
                            exist.LastSyncTime = now;
                            await client.Updateable(exist).ExecuteCommandAsync();
                            return new TollSaveResult(true, $"Đã cập nhật OutSummary '{sourceId}'.", Received: 1, Updated: 1);
                        }
                        return new TollSaveResult(true, $"OutSummary '{sourceId}' không đổi.", Received: 1, Unchanged: 1);
                    }
                    var entity = new TollTicketOutSummary { SourceId = sourceId, LastSyncTime = now };
                    FillSummary(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    return new TollSaveResult(true, $"Đã thêm mới OutSummary '{sourceId}'.", Received: 1, Inserted: 1);
                }

                default:
                    return new TollSaveResult(false, $"Chưa hỗ trợ push DataType='{dataType}'.");
            }
        }
        catch (Exception ex)
        {
            return new TollSaveResult(false, $"Upsert [{dataType}] thất bại: {ex.Message}");
        }
    }

    /// <summary>
    /// Xóa MỘT bản ghi theo id bên TCS: TransactionIn/Out xóa theo Transaction_ID; OutSummary xóa theo ConfirmedId (ticket_Confirmed_ID).
    /// Không tìm thấy ⇒ xóa 0 dòng (vẫn coi là thành công, idempotent).
    /// Author: ChuongLP
    /// Created date: 12/09/2026
    /// </summary>
    public async Task<TollSaveResult> DeleteRecordAsync(string dataType, string sourceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return new TollSaveResult(false, "Thiếu id (id bên TCS) để xóa.");

        var client = Client;
        try
        {
            int deleted = dataType switch
            {
                "TransactionIn" => await client.Deleteable<TollTransactionIn>().Where(x => x.TransactionId == sourceId).ExecuteCommandAsync(),
                "TransactionOut" => await client.Deleteable<TollTransactionOut>().Where(x => x.TransactionId == sourceId).ExecuteCommandAsync(),
                "OutSummary" => await client.Deleteable<TollTicketOutSummary>().Where(x => x.ConfirmedId == sourceId).ExecuteCommandAsync(),
                _ => -1,
            };

            if (deleted < 0)
                return new TollSaveResult(false, $"Chưa hỗ trợ xóa DataType='{dataType}'.");

            return new TollSaveResult(true, $"Đã xóa {deleted} bản ghi [{dataType}] theo id '{sourceId}'.", Received: deleted, Deleted: deleted);
        }
        catch (Exception ex)
        {
            return new TollSaveResult(false, $"Xóa [{dataType}] thất bại: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse payload masterdata và upsert vào 4 nhóm bảng (MasterData, Road, PriceList, PriceListDetail)
    /// Author: ChuongLP
    /// Created date: 24/08/2026
    /// .</summary>
    private async Task<TollSaveResult> ImportMasterAsync(string json, CancellationToken ct)
    {
        JsonElement result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            // Response bọc trong "result"; nếu không có thì coi root là result.
            result = TryGetProp(doc.RootElement, "result", out var r) ? r.Clone() : doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            return new TollSaveResult(false, $"Không đọc được JSON masterdata: {ex.Message}");
        }

        var now = DateTime.Now;
        var client = Client;

        try
        {
            client.Ado.BeginTran();

            var counts = new List<string>();

            // ===== 1) Các danh mục gộp vào TollMasterData (phân biệt bằng DataType) =====
            var groups = new (string Json, string DataType, string RemarkProp)[]
            {
                ("vehicleTypes", "VehicleType", "note"),
                ("ticketTypes", "TicketType", "note"),
                ("priceTypes", "PriceType", "note"),
                ("reasons", "Reason", "description"),
                ("payments", "Payment", "description"),
                ("statuses", "Status", "note"),
                ("transactionStatuses", "TransactionStatus", "note"),
            };

            // Nạp sẵn danh mục hiện có để dò trùng theo (DataType, SourceId).
            var existingMaster = await client.Queryable<TollMasterData>().ToListAsync();
            var masterByKey = existingMaster
                .Where(x => !string.IsNullOrEmpty(x.SourceId))
                .GroupBy(x => $"{x.DataType}|{x.SourceId}")
                .ToDictionary(g => g.Key, g => g.First());

            var masterIns = 0;
            var masterUpd = 0;
            var masterUnchanged = 0;
            var masterDel = 0;
            foreach (var (arrName, dataType, remarkProp) in groups)
            {
                // Chỉ xử lý và xóa thừa khi thuộc tính arrName có trong JSON response và là MẢNG
                if (!TryGetProp(result, arrName, out var arrElem) || arrElem.ValueKind != JsonValueKind.Array)
                    continue;

                var incomingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in arrElem.EnumerateArray())
                {
                    var sourceId = GetStr(item, "id");
                    if (string.IsNullOrWhiteSpace(sourceId)) continue;
                    incomingIds.Add(sourceId);

                    var name = GetStr(item, "name");
                    var group = GetStr(item, "group");
                    var remark = GetStr(item, remarkProp);
                    var raw = item.GetRawText();
                    var key = $"{dataType}|{sourceId}";

                    if (masterByKey.TryGetValue(key, out var exist))
                    {
                        var before = TollEntityDiff.CloneDeclared(exist);
                        exist.Name = name;
                        exist.GroupCode = group;
                        exist.Remark = remark;
                        exist.RawJson = raw;
                        if (TollEntityDiff.ChangedDeclared(before, exist))
                        {
                            exist.LastSyncTime = now;
                            await client.Updateable(exist).ExecuteCommandAsync();
                            masterUpd++;
                        }
                        else masterUnchanged++;
                    }
                    else
                    {
                        var entity = new TollMasterData
                        {
                            DataType = dataType,
                            SourceId = sourceId,
                            Name = name,
                            GroupCode = group,
                            Remark = remark,
                            RawJson = raw,
                            LastSyncTime = now,
                        };
                        Stamp(entity, now);
                        await client.Insertable(entity).ExecuteCommandAsync();
                        masterByKey[key] = entity;
                        masterIns++;
                    }
                }

                // Xóa các bản ghi thuộc DataType này có trong DB nhưng TCS không còn trả về
                var toDelete = existingMaster
                    .Where(x => string.Equals(x.DataType, dataType, StringComparison.OrdinalIgnoreCase)
                                && !string.IsNullOrEmpty(x.SourceId)
                                && !incomingIds.Contains(x.SourceId))
                    .ToList();
                if (toDelete.Count > 0)
                {
                    var deleteIds = toDelete.Select(x => x.ID).ToList();
                    await client.Deleteable<TollMasterData>().In(x => x.ID, deleteIds).ExecuteCommandAsync();
                    foreach (var d in toDelete)
                        masterByKey.Remove($"{d.DataType}|{d.SourceId}");
                    masterDel += toDelete.Count;
                }
            }
            counts.Add($"MasterData +{masterIns} ~{masterUpd} ={masterUnchanged} -{masterDel}");

            // ===== 2) Tuyến đường -> TollRoad (khóa SourceId) =====
            var existingRoads = await client.Queryable<TollRoad>().ToListAsync();
            var roadById = existingRoads
                .Where(x => !string.IsNullOrEmpty(x.SourceId))
                .GroupBy(x => x.SourceId!)
                .ToDictionary(g => g.Key, g => g.First());

            var roadIns = 0;
            var roadUpd = 0;
            var roadUnchanged = 0;
            var roadDel = 0;
            if (TryGetProp(result, "tcsRoads", out var roadArrElem) && roadArrElem.ValueKind == JsonValueKind.Array)
            {
                var incomingRoadIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in roadArrElem.EnumerateArray())
                {
                    var sourceId = GetStr(item, "id");
                    if (string.IsNullOrWhiteSpace(sourceId)) continue;
                    incomingRoadIds.Add(sourceId);

                    if (roadById.TryGetValue(sourceId, out var exist))
                    {
                        var before = TollEntityDiff.CloneDeclared(exist);
                        exist.Name = GetStr(item, "name");
                        exist.StationIdIn = GetStr(item, "station_ID_In");
                        exist.StationIdOut = GetStr(item, "station_ID_Out");
                        exist.Km = GetStr(item, "km");
                        exist.Remark = GetStr(item, "note");
                        exist.Active = GetBool(item, "active");
                        if (TollEntityDiff.ChangedDeclared(before, exist))
                        {
                            exist.SyncTime = now;
                            await client.Updateable(exist).ExecuteCommandAsync();
                            roadUpd++;
                        }
                        else roadUnchanged++;
                    }
                    else
                    {
                        var entity = new TollRoad
                        {
                            SourceId = sourceId,
                            Name = GetStr(item, "name"),
                            StationIdIn = GetStr(item, "station_ID_In"),
                            StationIdOut = GetStr(item, "station_ID_Out"),
                            Km = GetStr(item, "km"),
                            Remark = GetStr(item, "note"),
                            Active = GetBool(item, "active"),
                            SyncTime = now,
                        };
                        Stamp(entity, now);
                        await client.Insertable(entity).ExecuteCommandAsync();
                        roadById[sourceId] = entity;
                        roadIns++;
                    }
                }

                // Xóa các tuyến đường thừa so với TCS
                var roadsToDelete = existingRoads
                    .Where(x => !string.IsNullOrEmpty(x.SourceId) && !incomingRoadIds.Contains(x.SourceId))
                    .ToList();
                if (roadsToDelete.Count > 0)
                {
                    var deleteIds = roadsToDelete.Select(x => x.ID).ToList();
                    await client.Deleteable<TollRoad>().In(x => x.ID, deleteIds).ExecuteCommandAsync();
                    roadDel += roadsToDelete.Count;
                }
            }
            counts.Add($"Road +{roadIns} ~{roadUpd} ={roadUnchanged} -{roadDel}");

            // ===== 3) Bảng giá (header) -> TollPriceList (khóa SourceId) =====
            // Chạy TRƯỚC chi tiết: TollPriceListDetail.PriceListSourceId tham chiếu TollPriceList.SourceId.
            var existingPriceLists = await client.Queryable<TollPriceList>().ToListAsync();
            var priceListById = existingPriceLists
                .Where(x => !string.IsNullOrEmpty(x.SourceId))
                .GroupBy(x => x.SourceId!)
                .ToDictionary(g => g.Key, g => g.First());

            var priceListIns = 0;
            var priceListUpd = 0;
            var priceListUnchanged = 0;
            var priceListDel = 0;
            var incomingPriceListIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hasPriceListArr = TryGetProp(result, "tcsPriceLists", out var priceListArrElem) && priceListArrElem.ValueKind == JsonValueKind.Array;
            if (hasPriceListArr)
            {
                foreach (var item in priceListArrElem.EnumerateArray())
                {
                    var sourceId = GetStr(item, "id");
                    if (string.IsNullOrWhiteSpace(sourceId)) continue;
                    incomingPriceListIds.Add(sourceId);

                    if (priceListById.TryGetValue(sourceId, out var exist))
                    {
                        if (TollEntityDiff.ApplyAndCheckChanged(exist, FillPriceList, item))
                        {
                            exist.SyncTime = now;
                            await client.Updateable(exist).ExecuteCommandAsync();
                            priceListUpd++;
                        }
                        else priceListUnchanged++;
                    }
                    else
                    {
                        var entity = new TollPriceList { SourceId = sourceId, SyncTime = now };
                        FillPriceList(entity, item);
                        Stamp(entity, now);
                        await client.Insertable(entity).ExecuteCommandAsync();
                        priceListById[sourceId] = entity;
                        priceListIns++;
                    }
                }
            }

            // ===== 4) Chi tiết bảng giá -> TollPriceListDetail (khóa SourceId) =====
            var existingDetails = await client.Queryable<TollPriceListDetail>().ToListAsync();
            var detailById = existingDetails
                .Where(x => !string.IsNullOrEmpty(x.SourceId))
                .GroupBy(x => x.SourceId!)
                .ToDictionary(g => g.Key, g => g.First());

            var detailIns = 0;
            var detailUpd = 0;
            var detailUnchanged = 0;
            var detailDel = 0;
            var incomingDetailIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hasDetailArr = TryGetProp(result, "tcsPriceListDetails", out var detailArrElem) && detailArrElem.ValueKind == JsonValueKind.Array;
            if (hasDetailArr)
            {
                foreach (var item in detailArrElem.EnumerateArray())
                {
                    var sourceId = GetStr(item, "id");
                    if (string.IsNullOrWhiteSpace(sourceId)) continue;
                    incomingDetailIds.Add(sourceId);

                    if (detailById.TryGetValue(sourceId, out var exist))
                    {
                        if (TollEntityDiff.ApplyAndCheckChanged(exist, FillDetail, item))
                        {
                            exist.SyncTime = now;
                            await client.Updateable(exist).ExecuteCommandAsync();
                            detailUpd++;
                        }
                        else detailUnchanged++;
                    }
                    else
                    {
                        var entity = new TollPriceListDetail { SourceId = sourceId, SyncTime = now };
                        FillDetail(entity, item);
                        Stamp(entity, now);
                        await client.Insertable(entity).ExecuteCommandAsync();
                        detailById[sourceId] = entity;
                        detailIns++;
                    }
                }

                // Xóa chi tiết bảng giá thừa TRƯỚC (để tránh lỗi ràng buộc khóa ngoại với bảng giá header)
                var detailsToDelete = existingDetails
                    .Where(x => !string.IsNullOrEmpty(x.SourceId) && !incomingDetailIds.Contains(x.SourceId))
                    .ToList();
                if (detailsToDelete.Count > 0)
                {
                    var deleteIds = detailsToDelete.Select(x => x.ID).ToList();
                    await client.Deleteable<TollPriceListDetail>().In(x => x.ID, deleteIds).ExecuteCommandAsync();
                    detailDel += detailsToDelete.Count;
                }
            }

            // Xóa bảng giá header thừa SAU KHI đã xóa chi tiết
            if (hasPriceListArr)
            {
                var priceListsToDelete = existingPriceLists
                    .Where(x => !string.IsNullOrEmpty(x.SourceId) && !incomingPriceListIds.Contains(x.SourceId))
                    .ToList();
                if (priceListsToDelete.Count > 0)
                {
                    var deleteIds = priceListsToDelete.Select(x => x.ID).ToList();
                    await client.Deleteable<TollPriceList>().In(x => x.ID, deleteIds).ExecuteCommandAsync();
                    priceListDel += priceListsToDelete.Count;
                }
            }

            counts.Add($"PriceList +{priceListIns} ~{priceListUpd} ={priceListUnchanged} -{priceListDel}");
            counts.Add($"PriceListDetail +{detailIns} ~{detailUpd} ={detailUnchanged} -{detailDel}");

            client.Ado.CommitTran();
            var mIns = masterIns + roadIns + priceListIns + detailIns;
            var mUpd = masterUpd + roadUpd + priceListUpd + detailUpd;
            var mUnch = masterUnchanged + roadUnchanged + priceListUnchanged + detailUnchanged;
            var mDel = masterDel + roadDel + priceListDel + detailDel;
            return new TollSaveResult(true, "Đã lưu masterdata: " + string.Join(" | ", counts),
                Received: mIns + mUpd + mUnch, Inserted: mIns, Updated: mUpd, Unchanged: mUnch, Deleted: mDel);
        }
        catch (Exception ex)
        {
            try { client.Ado.RollbackTran(); } catch { /* bỏ qua */ }
            return new TollSaveResult(false, $"Lưu masterdata thất bại: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse payload danh mục TRẠM (API mdstram/allstation — result là MẢNG) và upsert vào TollStation (khóa: id).
    /// Author: ChuongLP
    /// Created date: 26/08/2026
    /// </summary>
    private async Task<TollSaveResult> ImportStationAsync(string json, CancellationToken ct)
    {
        if (!TryGetResultArray(json, out var result, out var parseError))
            return new TollSaveResult(false, parseError!);

        var client = Client;
        var now = DateTime.Now;
        try
        {
            client.Ado.BeginTran();

            var existing = await client.Queryable<TollStation>().ToListAsync();
            var byId = existing
                .Where(x => !string.IsNullOrEmpty(x.StationId))
                .GroupBy(x => x.StationId!)
                .ToDictionary(g => g.Key, g => g.First());

            var ins = 0;
            var upd = 0;
            var unchanged = 0;
            var incomingStationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in result.EnumerateArray())
            {
                var sid = GetStr(item, "id");
                if (string.IsNullOrWhiteSpace(sid)) continue;
                incomingStationIds.Add(sid);

                if (byId.TryGetValue(sid, out var exist))
                {
                    if (TollEntityDiff.ApplyAndCheckChanged(exist, FillStation, item))
                    {
                        exist.LastSyncTime = now;
                        // RefId do người dùng map trên ITS (TmsWorkUnit) → không ghi đè khi đồng bộ TCS.
                        await client.Updateable(exist).IgnoreColumns(x => x.RefId).ExecuteCommandAsync();
                        upd++;
                    }
                    else unchanged++;
                }
                else
                {
                    var entity = new TollStation { StationId = sid, LastSyncTime = now };
                    FillStation(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    byId[sid] = entity;
                    ins++;
                }
            }

            // Xóa các trạm thừa trong DB mà TCS không còn trả về
            var stationsToDelete = existing
                .Where(x => !string.IsNullOrEmpty(x.StationId) && !incomingStationIds.Contains(x.StationId))
                .ToList();
            var del = 0;
            if (stationsToDelete.Count > 0)
            {
                var deleteIds = stationsToDelete.Select(x => x.ID).ToList();
                await client.Deleteable<TollStation>().In(x => x.ID, deleteIds).ExecuteCommandAsync();
                del = stationsToDelete.Count;
            }

            client.Ado.CommitTran();
            return new TollSaveResult(true, $"Đã lưu Station: +{ins} ~{upd} ={unchanged} -{del}",
                Received: ins + upd + unchanged, Inserted: ins, Updated: upd, Unchanged: unchanged, Deleted: del);
        }
        catch (Exception ex)
        {
            try { client.Ado.RollbackTran(); } catch { /* bỏ qua */ }
            return new TollSaveResult(false, $"Lưu Station thất bại: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse payload danh mục LÀN (API mdslane/alllane — result là MẢNG) và upsert vào TollLane (khóa: laneId).
    /// Author: ChuongLP
    /// Created date: 26/08/2026
    /// </summary>
    private async Task<TollSaveResult> ImportLaneAsync(string json, CancellationToken ct)
    {
        if (!TryGetResultArray(json, out var result, out var parseError))
            return new TollSaveResult(false, parseError!);

        var client = Client;
        var now = DateTime.Now;
        try
        {
            client.Ado.BeginTran();

            var existing = await client.Queryable<TollLane>().ToListAsync();
            var byId = existing
                .Where(x => !string.IsNullOrEmpty(x.LaneId))
                .GroupBy(x => x.LaneId!)
                .ToDictionary(g => g.Key, g => g.First());

            var ins = 0;
            var upd = 0;
            var unchanged = 0;
            var incomingLaneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in result.EnumerateArray())
            {
                var lid = GetStr(item, "laneId");
                if (string.IsNullOrWhiteSpace(lid)) continue;
                incomingLaneIds.Add(lid);

                if (byId.TryGetValue(lid, out var exist))
                {
                    if (TollEntityDiff.ApplyAndCheckChanged(exist, FillLane, item))
                    {
                        exist.LastSyncTime = now;
                        await client.Updateable(exist).ExecuteCommandAsync();
                        upd++;
                    }
                    else unchanged++;
                }
                else
                {
                    var entity = new TollLane { LaneId = lid, LastSyncTime = now };
                    FillLane(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    byId[lid] = entity;
                    ins++;
                }
            }

            // Xóa các làn thừa trong DB mà TCS không còn trả về
            var lanesToDelete = existing
                .Where(x => !string.IsNullOrEmpty(x.LaneId) && !incomingLaneIds.Contains(x.LaneId))
                .ToList();
            var del = 0;
            if (lanesToDelete.Count > 0)
            {
                var deleteIds = lanesToDelete.Select(x => x.ID).ToList();
                await client.Deleteable<TollLane>().In(x => x.ID, deleteIds).ExecuteCommandAsync();
                del = lanesToDelete.Count;
            }

            client.Ado.CommitTran();
            return new TollSaveResult(true, $"Đã lưu Lane: +{ins} ~{upd} ={unchanged} -{del}",
                Received: ins + upd + unchanged, Inserted: ins, Updated: upd, Unchanged: unchanged, Deleted: del);
        }
        catch (Exception ex)
        {
            try { client.Ado.RollbackTran(); } catch { /* bỏ qua */ }
            return new TollSaveResult(false, $"Lưu Lane thất bại: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse payload danh mục CA LÀM VIỆC (API acs13quanly/mdsca/list — result là MẢNG) và upsert vào TollShift (khóa: id).
    /// Author: ChuongLP
    /// Created date: 26/08/2026
    /// </summary>
    private async Task<TollSaveResult> ImportShiftAsync(string json, CancellationToken ct)
    {
        if (!TryGetResultArray(json, out var result, out var parseError))
            return new TollSaveResult(false, parseError!);

        var client = Client;
        var now = DateTime.Now;
        try
        {
            client.Ado.BeginTran();

            var existing = await client.Queryable<TollShift>().ToListAsync();
            var byId = existing
                .Where(x => !string.IsNullOrEmpty(x.ShiftId))
                .GroupBy(x => x.ShiftId!)
                .ToDictionary(g => g.Key, g => g.First());

            var ins = 0;
            var upd = 0;
            var unchanged = 0;
            var incomingShiftIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in result.EnumerateArray())
            {
                var sid = GetStr(item, "id");
                if (string.IsNullOrWhiteSpace(sid)) continue;
                incomingShiftIds.Add(sid);

                if (byId.TryGetValue(sid, out var exist))
                {
                    if (TollEntityDiff.ApplyAndCheckChanged(exist, FillShift, item))
                    {
                        exist.LastSyncTime = now;
                        await client.Updateable(exist).ExecuteCommandAsync();
                        upd++;
                    }
                    else unchanged++;
                }
                else
                {
                    var entity = new TollShift { ShiftId = sid, LastSyncTime = now };
                    FillShift(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    byId[sid] = entity;
                    ins++;
                }
            }

            // Xóa các ca làm việc thừa trong DB mà TCS không còn trả về
            var shiftsToDelete = existing
                .Where(x => !string.IsNullOrEmpty(x.ShiftId) && !incomingShiftIds.Contains(x.ShiftId))
                .ToList();
            var del = 0;
            if (shiftsToDelete.Count > 0)
            {
                var deleteIds = shiftsToDelete.Select(x => x.ID).ToList();
                await client.Deleteable<TollShift>().In(x => x.ID, deleteIds).ExecuteCommandAsync();
                del = shiftsToDelete.Count;
            }

            client.Ado.CommitTran();
            return new TollSaveResult(true, $"Đã lưu Shift: +{ins} ~{upd} ={unchanged} -{del}",
                Received: ins + upd + unchanged, Inserted: ins, Updated: upd, Unchanged: unchanged, Deleted: del);
        }
        catch (Exception ex)
        {
            try { client.Ado.RollbackTran(); } catch { /* bỏ qua */ }
            return new TollSaveResult(false, $"Lưu Shift thất bại: {ex.Message}");
        }
    }

    /// <summary>Map trường JSON (mdsca/list) → TollShift.</summary>
    private static void FillShift(TollShift e, JsonElement item)
    {
        e.Name = GetStr(item, "name");
        e.StartTime = GetDateTime(item, "startTime");
        e.EndTime = GetDateTime(item, "endTime");
        e.Note = GetStr(item, "note");
        e.ShiftBE = GetStr(item, "code");
    }

    /// <summary>Map trường JSON (mdstram/allstation) → TollStation.</summary>
    private static void FillStation(TollStation e, JsonElement item)
    {
        e.Name = GetStr(item, "name");
        e.Note = GetStr(item, "note");
        e.Company = GetStr(item, "company");
        e.StationType = GetInt(item, "station_Type");
        e.StationBE = GetStr(item, "station_BE");
        e.Type = GetStr(item, "type");
        e.Active = GetInt(item, "active");   // active dạng bool -> 1/0
        // Không map: address/phone/taxCode/imageFolder/group (entity không có cột).
    }

    /// <summary>Map trường JSON (mdslane/alllane) → TollLane.</summary>
    private static void FillLane(TollLane e, JsonElement item)
    {
        e.Name = GetStr(item, "name");
        e.Note = GetStr(item, "note");
        e.StationId = GetStr(item, "station_ID");
        e.Direction = GetStr(item, "direction");
        e.LaneBE = GetStr(item, "laneID_BE");
        // Không map: camera_Name/lpr (entity không có cột).
    }

    /// <summary>
    /// Gán dữ liệu từ JSON vào entity TollPriceListDetail
    /// Author: ChuongLP
    /// Created date: 24/08/2026
    /// </summary>
    /// <param name="e"></param>
    /// <param name="item"></param>
    /// <summary>
    /// Parse payload giao dịch XE VÀO và upsert vào TollTransactionIn (khóa: transaction_ID).
    /// Giả định endpoint alltransactionin trả về cùng tên trường như alltransactionout (đối chiếu lại khi có sample In).
    /// Author: ChuongLP
    /// Created date: 25/08/2026
    /// </summary>
    private async Task<TollSaveResult> ImportTransactionInAsync(string json, CancellationToken ct)
    {
        if (!TryGetResultArray(json, out var result, out var parseError))
            return new TollSaveResult(false, parseError!);

        var client = Client;
        var now = DateTime.Now;
        try
        {
            client.Ado.BeginTran();

            var existing = await client.Queryable<TollTransactionIn>().ToListAsync();
            var byId = existing
                .Where(x => !string.IsNullOrEmpty(x.TransactionId))
                .GroupBy(x => x.TransactionId!)
                .ToDictionary(g => g.Key, g => g.First());

            var ins = 0;
            var upd = 0;
            var unchanged = 0;
            foreach (var item in result.EnumerateArray())
            {
                var tid = GetStr(item, "transaction_ID");
                if (string.IsNullOrWhiteSpace(tid)) continue;

                if (byId.TryGetValue(tid, out var exist))
                {
                    if (TollEntityDiff.ApplyAndCheckChanged(exist, FillIn, item))
                    {
                        exist.SyncTime = now;
                        await client.Updateable(exist).ExecuteCommandAsync();
                        upd++;
                    }
                    else unchanged++;
                }
                else
                {
                    var entity = new TollTransactionIn { TransactionId = tid, SyncTime = now };
                    FillIn(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    byId[tid] = entity;
                    ins++;
                }
            }

            client.Ado.CommitTran();
            return new TollSaveResult(true, $"Đã lưu TransactionIn: +{ins} ~{upd} ={unchanged}",
                Received: ins + upd + unchanged, Inserted: ins, Updated: upd, Unchanged: unchanged);
        }
        catch (Exception ex)
        {
            try { client.Ado.RollbackTran(); } catch { /* bỏ qua */ }
            return new TollSaveResult(false, $"Lưu TransactionIn thất bại: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse payload giao dịch XE RA và upsert vào TollTransactionOut (khóa: transaction_ID).
    /// Author: ChuongLP
    /// Created date: 25/08/2026
    /// </summary>
    private async Task<TollSaveResult> ImportTransactionOutAsync(string json, CancellationToken ct)
    {
        if (!TryGetResultArray(json, out var result, out var parseError))
            return new TollSaveResult(false, parseError!);

        var client = Client;
        var now = DateTime.Now;
        try
        {
            client.Ado.BeginTran();

            var existing = await client.Queryable<TollTransactionOut>().ToListAsync();
            var byId = existing
                .Where(x => !string.IsNullOrEmpty(x.TransactionId))
                .GroupBy(x => x.TransactionId!)
                .ToDictionary(g => g.Key, g => g.First());

            var ins = 0;
            var upd = 0;
            var unchanged = 0;
            foreach (var item in result.EnumerateArray())
            {
                var tid = GetStr(item, "transaction_ID");
                if (string.IsNullOrWhiteSpace(tid)) continue;

                if (byId.TryGetValue(tid, out var exist))
                {
                    if (TollEntityDiff.ApplyAndCheckChanged(exist, FillOut, item))
                    {
                        exist.SyncTime = now;
                        await client.Updateable(exist).ExecuteCommandAsync();
                        upd++;
                    }
                    else unchanged++;
                }
                else
                {
                    var entity = new TollTransactionOut { TransactionId = tid, SyncTime = now };
                    FillOut(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    byId[tid] = entity;
                    ins++;
                }
            }

            client.Ado.CommitTran();
            return new TollSaveResult(true, $"Đã lưu TransactionOut: +{ins} ~{upd} ={unchanged}",
                Received: ins + upd + unchanged, Inserted: ins, Updated: upd, Unchanged: unchanged);
        }
        catch (Exception ex)
        {
            try { client.Ado.RollbackTran(); } catch { /* bỏ qua */ }
            return new TollSaveResult(false, $"Lưu TransactionOut thất bại: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse payload tổng hợp vé ra và upsert vào TollTicketOutSummary (khóa: id).
    /// Author: ChuongLP
    /// Created date: 25/08/2026
    /// </summary>
    private async Task<TollSaveResult> ImportOutSummaryAsync(string json, CancellationToken ct)
    {
        if (!TryGetResultArray(json, out var result, out var parseError))
            return new TollSaveResult(false, parseError!);

        var client = Client;
        var now = DateTime.Now;
        try
        {
            client.Ado.BeginTran();

            var existing = await client.Queryable<TollTicketOutSummary>().ToListAsync();
            var byId = existing
                .Where(x => !string.IsNullOrEmpty(x.SourceId))
                .GroupBy(x => x.SourceId!)
                .ToDictionary(g => g.Key, g => g.First());

            var ins = 0;
            var upd = 0;
            var unchanged = 0;
            foreach (var item in result.EnumerateArray())
            {
                var sourceId = GetStr(item, "id");
                if (string.IsNullOrWhiteSpace(sourceId)) continue;

                if (byId.TryGetValue(sourceId, out var exist))
                {
                    if (TollEntityDiff.ApplyAndCheckChanged(exist, FillSummary, item))
                    {
                        exist.LastSyncTime = now;
                        await client.Updateable(exist).ExecuteCommandAsync();
                        upd++;
                    }
                    else unchanged++;
                }
                else
                {
                    var entity = new TollTicketOutSummary { SourceId = sourceId, LastSyncTime = now };
                    FillSummary(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    byId[sourceId] = entity;
                    ins++;
                }
            }

            client.Ado.CommitTran();
            return new TollSaveResult(true, $"Đã lưu OutSummary: +{ins} ~{upd} ={unchanged}",
                Received: ins + upd + unchanged, Inserted: ins, Updated: upd, Unchanged: unchanged);
        }
        catch (Exception ex)
        {
            try { client.Ado.RollbackTran(); } catch { /* bỏ qua */ }
            return new TollSaveResult(false, $"Lưu OutSummary thất bại: {ex.Message}");
        }
    }

    /// <summary>Map trường JSON → TollTicketOutSummary.</summary>
    private static void FillSummary(TollTicketOutSummary e, JsonElement item)
    {
        e.ProcessDate = GetDateTime(item, "processDate");
        e.StartTime = GetDateTime(item, "startDate");
        e.EndTime = GetDateTime(item, "endDate");
        e.StationId = GetStr(item, "station_ID");
        e.EntryStationId = GetStr(item, "station_ID_In");
        e.Direction = GetStr(item, "direction");
        e.VehicleTypeId = GetStr(item, "vehicleType_ID");
        e.TicketTypeId = GetStr(item, "ticketType_ID");
        e.PriceTypeId = GetStr(item, "priceType_ID");
        e.PriceListId = GetStr(item, "priceList_ID");
        e.Price = GetDecimal(item, "price");
        e.AmountTotal = GetLong(item, "amount_Total");
        e.TotalAmount = GetDecimal(item, "total");
        e.AmountEdit = GetLong(item, "amount_Edit");
        e.TotalEdit = GetDecimal(item, "total_Edit");
        e.ReasonId = GetStr(item, "reason_ID");
        e.ConfirmedId = GetStr(item, "ticket_Confirmed_ID");
        // Không có trong sample: Amount_Sum/Total_Sum (để null); note/dataJson (entity không có cột).
    }

    /// <summary>
    /// Parse payload lịch sử truy cập nhân viên (API appaccesslog/accesshistorytcs — mảng nằm ở result.items)
    /// và upsert vào TollAccessHistory. TCS không trả ID nên khóa chống trùng là bộ
    /// (EmployeeCode + AccessTime + Status): Insert nếu mới / Update thời điểm đồng bộ nếu đã có.
    /// Author: ChuongLP
    /// Created date: 09/09/2026
    /// </summary>
    private async Task<TollSaveResult> ImportAccessLogAsync(string json, CancellationToken ct)
    {
        if (!TryGetResultItemsArray(json, out var items, out var parseError))
            return new TollSaveResult(false, parseError!);

        var client = Client;
        var now = DateTime.Now;
        try
        {
            client.Ado.BeginTran();

            // TCS không có ID → dò trùng bằng khóa nghiệp vụ tổng hợp (chuẩn hóa ở BuildAccessLogKey).
            var existing = await client.Queryable<TollAccessHistory>().ToListAsync();
            var byKey = new Dictionary<string, TollAccessHistory>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in existing)
            {
                var k = BuildAccessLogKey(row.EmployeeCode, row.AccessTime, row.Status);
                byKey[k] = row; // trùng khóa trong DB (nếu có) → giữ bản gặp sau, đủ để chống chèn lặp
            }

            var ins = 0;
            var upd = 0;
            var unchanged = 0;
            foreach (var item in items.EnumerateArray())
            {
                var employeeCode = GetStr(item, "employeeCode") ?? GetStr(item, "userName") ?? GetStr(item, "account");
                var accessTime = GetDateTime(item, "accessTime") ?? GetDateTime(item, "logTime") ?? GetDateTime(item, "actionTime") ?? GetDateTime(item, "logDateTime");
                var status = GetStr(item, "status") ?? GetStr(item, "actionName");

                // Bỏ qua bản ghi rỗng hoàn toàn (không đủ dữ liệu để định danh).
                if (string.IsNullOrWhiteSpace(employeeCode) && accessTime is null && string.IsNullOrWhiteSpace(status))
                    continue;

                var key = BuildAccessLogKey(employeeCode, accessTime, status);

                if (byKey.TryGetValue(key, out var exist))
                {
                    if (TollEntityDiff.ApplyAndCheckChanged(exist, FillAccessLog, item))
                    {
                        exist.LastSyncTime = now;
                        await client.Updateable(exist).ExecuteCommandAsync();
                        upd++;
                    }
                    else unchanged++;
                }
                else
                {
                    var entity = new TollAccessHistory { LastSyncTime = now };
                    FillAccessLog(entity, item);
                    Stamp(entity, now);
                    await client.Insertable(entity).ExecuteCommandAsync();
                    byKey[key] = entity;
                    ins++;
                }
            }

            client.Ado.CommitTran();
            return new TollSaveResult(true, $"Đã lưu AccessLog: +{ins} ~{upd} ={unchanged}",
                Received: ins + upd + unchanged, Inserted: ins, Updated: upd, Unchanged: unchanged);
        }
        catch (Exception ex)
        {
            try { client.Ado.RollbackTran(); } catch { /* bỏ qua */ }
            return new TollSaveResult(false, $"Lưu AccessLog thất bại: {ex.Message}");
        }
    }

    /// <summary>Map trường JSON (accesshistorytcs) → TollAccessHistory.</summary>
    private static void FillAccessLog(TollAccessHistory e, JsonElement item)
    {
        e.EmployeeCode = GetStr(item, "employeeCode") ?? GetStr(item, "userName") ?? GetStr(item, "account");
        e.EmployeeName = GetStr(item, "employeeName") ?? GetStr(item, "fullName") ?? GetStr(item, "name");
        e.AccessTime = GetDateTime(item, "accessTime") ?? GetDateTime(item, "logTime") ?? GetDateTime(item, "actionTime") ?? GetDateTime(item, "logDateTime");
        e.Status = GetStr(item, "status") ?? GetStr(item, "actionName");
    }

    /// <summary>Dựng khóa nghiệp vụ chống trùng cho lịch sử truy cập (EmployeeCode|AccessTime(yyyy-MM-dd HH:mm:ss)|Status).</summary>
    private static string BuildAccessLogKey(string? employeeCode, DateTime? accessTime, string? status)
    {
        var code = (employeeCode ?? string.Empty).Trim();
        var time = accessTime?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;
        var st = (status ?? string.Empty).Trim();
        return $"{code}|{time}|{st}";
    }

    /// <summary>Map trường JSON → TollTransactionIn (chỉ các cột entity In có).</summary>
    private static void FillIn(TollTransactionIn e, JsonElement item)
    {
        e.StationId = GetStr(item, "station_ID");
        e.LaneId = GetStr(item, "lane_ID");
        e.TransactionDateTime = GetDateTime(item, "transaction_Datetime");
        e.StatusChecked = GetStr(item, "status_Checked");
        e.VehicleTypeId = GetStr(item, "vehicleType_ID");
        e.ReasonId = GetStr(item, "reason_ID");
        e.TicketId = GetStr(item, "ticket_ID");
        e.TicketTypeId = GetStr(item, "ticketType_ID");
        e.TagId = GetStr(item, "tagID");
        e.Plate = GetStr(item, "plate");
        e.PlateLpr = GetStr(item, "plate_LPR");
        e.PlateEdit = GetStr(item, "plate_Edit");
        e.TransactionDateTimeIn = GetDateTime(item, "transaction_Datetime_In");
        e.StatusId = GetStr(item, "status_ID");
        e.ImageLane = GetStr(item, "imageLane");
        e.ImagePlate = GetStr(item, "imagePlate");
        e.UserId = GetStr(item, "user_ID");
        e.ShiftCode = GetStr(item, "shift_ID");
        e.ProcessDate = GetDateTime(item, "proccess_Date");
        e.Serial = GetStr(item, "serial");
        // Không có trong JSON: OperatorName (ghép SysUser sau).
    }

    /// <summary>Map trường JSON → TollTransactionOut (đầy đủ vào/ra + giá).</summary>
    private static void FillOut(TollTransactionOut e, JsonElement item)
    {
        e.StationIdIn = GetStr(item, "station_ID_In");
        e.StationId = GetStr(item, "station_ID");
        e.LaneIdIn = GetStr(item, "lane_ID_In");
        e.LaneId = GetStr(item, "lane_ID");
        e.StatusChecked = GetStr(item, "status_Checked");
        e.VehicleTypeId = GetStr(item, "vehicleType_ID");
        e.ReasonId = GetStr(item, "reason_ID");
        e.TicketId = GetStr(item, "ticket_ID");
        e.TicketTypeId = GetStr(item, "ticketType_ID");
        e.TagId = GetStr(item, "tagID");
        e.Price = GetDecimal(item, "price");
        e.Plate = GetStr(item, "plate");
        e.PlateLpr = GetStr(item, "plate_LPR");
        e.PlateEdit = GetStr(item, "plate_Edit");
        e.TransactionDateTime = GetDateTime(item, "transaction_Datetime");
        e.TransactionDateTimeIn = GetDateTime(item, "transaction_Datetime_In");
        e.StatusId = GetStr(item, "status_ID");
        e.ImageLane = GetStr(item, "imageLane");
        e.ImagePlate = GetStr(item, "imagePlate");
        e.ImageLaneIn = GetStr(item, "imageLane_In");
        e.ImagePlateIn = GetStr(item, "imagePlate_In");
        e.UserId = GetStr(item, "user_ID");
        e.ShiftCode = GetStr(item, "shift_ID");
        e.ProcessDate = GetDateTime(item, "proccess_Date");
        e.Serial = GetStr(item, "serial");
        // Không có trong JSON: OperatorName (ghép SysUser sau).
    }

    /// <summary>Lấy mảng "result" từ payload (transaction trả về mảng ở "result").</summary>
    private static bool TryGetResultArray(string json, out JsonElement array, out string? error)
    {
        array = default;
        error = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var target = TryGetProp(root, "result", out var r) ? r : root;
            if (target.ValueKind != JsonValueKind.Array)
            {
                error = "Dữ liệu 'result' không phải mảng giao dịch.";
                return false;
            }
            array = target.Clone();
            return true;
        }
        catch (Exception ex)
        {
            error = $"Không đọc được JSON giao dịch: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Lấy mảng "result.items" từ payload access log (result là OBJECT bọc mảng items,
    /// khác transaction có result là mảng trực tiếp). Không có items → coi như mảng rỗng (thành công, 0 bản ghi).
    /// </summary>
    private static bool TryGetResultItemsArray(string json, out JsonElement array, out string? error)
    {
        array = default;
        error = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var result = TryGetProp(root, "result", out var r) ? r : root;

            if (result.ValueKind == JsonValueKind.Array)
            {
                // Dự phòng: một số phiên bản API có thể trả result là mảng trực tiếp.
                array = result.Clone();
                return true;
            }

            if (TryGetProp(result, "items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                array = items.Clone();
                return true;
            }

            error = "Dữ liệu 'result.items' không phải mảng lịch sử truy cập.";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Không đọc được JSON lịch sử truy cập: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Gán dữ liệu từ JSON (phần tử tcsPriceLists) vào entity TollPriceList
    /// Author: HieuNV
    /// Created date: 06/10/2026
    /// </summary>
    private static void FillPriceList(TollPriceList e, JsonElement item)
    {
        e.Name = GetStr(item, "name");
        e.StartDate = GetDateTime(item, "startDate");
        e.EndDate = GetDateTime(item, "endDate");
        e.Active = GetBool(item, "active");
        e.Note = GetStr(item, "note");
        e.SourceCreateDate = GetDateTime(item, "create_Date");
        e.SourceWriteDate = GetDateTime(item, "write_Date");
    }

    private static void FillDetail(TollPriceListDetail e, JsonElement item)
    {
        e.Code = GetStr(item, "code");
        e.Name = GetStr(item, "name");
        e.Price = GetDecimal(item, "price");
        e.VehicleTypeSourceId = GetStr(item, "vehicleType_ID");
        e.TicketTypeSourceId = GetStr(item, "ticketType_ID");
        e.PriceTypeSourceId = GetStr(item, "priceType_ID");
        e.PriceListSourceId = GetStr(item, "priceList_ID");
        e.RoadSourceId = GetStr(item, "road_ID");
        e.Active = GetBool(item, "active");
        e.Note = GetStr(item, "note");
    }

    // ================== JSON helpers (không phân biệt hoa/thường) ==================

    private static bool TryGetProp(JsonElement obj, string name, out JsonElement value)
    {
        value = default;
        if (obj.ValueKind != JsonValueKind.Object) return false;
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<JsonElement> GetArray(JsonElement obj, string name)
    {
        if (TryGetProp(obj, name, out var arr) && arr.ValueKind == JsonValueKind.Array)
            return arr.EnumerateArray();
        return Enumerable.Empty<JsonElement>();
    }

    private static string? GetStr(JsonElement obj, string name)
    {
        if (!TryGetProp(obj, name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => v.GetRawText(),
        };
    }

    private static decimal? GetDecimal(JsonElement obj, string name)
    {
        if (!TryGetProp(obj, name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
        if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var s)) return s;
        return null;
    }

    private static long? GetLong(JsonElement obj, string name)
    {
        if (!TryGetProp(obj, name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var l)) return l;
        if (v.ValueKind == JsonValueKind.String && long.TryParse(v.GetString(), out var s)) return s;
        return null;
    }

    /// <summary>Lấy int? — chấp nhận number, chuỗi số, hoặc bool (true=1/false=0, dùng cho cột Active kiểu int).</summary>
    private static int? GetInt(JsonElement obj, string name)
    {
        if (!TryGetProp(obj, name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.TryGetInt32(out var i) ? i : (v.TryGetInt64(out var l) ? (int)l : (int?)null),
            JsonValueKind.String => int.TryParse(v.GetString(), out var s) ? s : (int?)null,
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => null,
        };
    }

    private static bool? GetBool(JsonElement obj, string name)
    {
        if (!TryGetProp(obj, name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(v.GetString(), out var b) ? b : null,
            _ => null,
        };
    }

    private static readonly string[] CustomDateTimeFormats = new[]
    {
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "yyyy-MM-ddTHH:mm:ss.FFFZ",
        "yyyy-MM-ddTHH:mm:ss.FFFK",
        "yyyy-MM-ddTHH:mm:ss.FFF",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm:ss.FFF",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy",
        "dd-MM-yyyy HH:mm:ss",
        "dd-MM-yyyy HH:mm",
        "dd-MM-yyyy",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy/MM/dd",
    };

    private static DateTime? GetDateTime(JsonElement obj, string name)
    {
        if (!TryGetProp(obj, name, out var v)) return null;
        if (v.ValueKind == JsonValueKind.String)
        {
            var s = v.GetString();
            if (string.IsNullOrWhiteSpace(s)) return null;

            if (DateTime.TryParseExact(s, CustomDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
                return dtExact;

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtInv))
                return dtInv;

            if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var dtVi))
                return dtVi;
        }
        else if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var epoch))
        {
            return epoch > 1000000000000
                ? DateTimeOffset.FromUnixTimeMilliseconds(epoch).LocalDateTime
                : DateTimeOffset.FromUnixTimeSeconds(epoch).LocalDateTime;
        }
        return null;
    }
}
