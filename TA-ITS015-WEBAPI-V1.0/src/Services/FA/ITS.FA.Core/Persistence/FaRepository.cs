using ITS.FA.Core.Configuration;
using Services.Shared.Persistence;
using Shared.Utility.Utilities;

namespace ITS.FA.Core.Persistence;

/// <summary>
/// Truy cập DB của dịch vụ FA. Thay cho 7 stored procedure FaObjectData_* của ITS008:
///   GetDetectorAndButton    → <see cref="GetPointsAsync"/>
///   GetDoor                 → <see cref="GetDoorsAsync"/>
///   UpdatePointState        → <see cref="UpdatePointStateAsync"/>
///   UpdateDoorState         → <see cref="UpdateDoorStateAsync"/>
///   UpdateFlagState         → <see cref="UpdateFlagAsync"/>
///   ResetAllPoint           → <see cref="ResetAllPointsAsync"/>
///   IsLocationHasFireAlarm  → <see cref="IsLocationHasIncidentAsync"/>
/// Giữ đúng hành vi cũ: các truy vấn đọc KHÔNG lọc tenant (chỉ loại bản ghi xoá mềm).
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
public sealed class FaRepository : SqlSugarAccess
{
    private readonly FaOptions _options;

    public FaRepository(DatabaseManager databaseManager, IOptions<FaOptions> options)
        : base(databaseManager, () => options.Value.DataConnectionId)
    {
        _options = options.Value;
    }

    /// <summary>Cũ: SELECT * FROM FA_OBJECT_DATA — toàn bộ đầu báo và nút ấn, không lọc.</summary>
    public Task<List<FaObjectData>> GetPointsAsync(CancellationToken ct = default) =>
        Client.Queryable<FaObjectData>().Where(x => x.IsDelete == null).ToListAsync(ct);

    /// <summary>
    /// Cũ: SELECT * FROM FIRE_ALARM_CONFIG — KHÔNG lọc loại: mọi dòng đều bị coi là cửa và bị quét Modbus.
    /// </summary>
    public Task<List<FaFireAlarmConfig>> GetDoorsAsync(CancellationToken ct = default) =>
        Client.Queryable<FaFireAlarmConfig>().Where(x => x.IsDelete == null).ToListAsync(ct);

    /// <summary>Cũ: UPDATE FA_OBJECT_DATA SET IsActived, LastModified = UTC, LastModifiedBy WHERE Id.</summary>
    public Task<int> UpdatePointStateAsync(int pointAddress, bool isActive, string modifiedBy, CancellationToken ct = default)
    {
        var now = BaseUtil.TimeServer.GetTimeServer();
        return Client.Updateable<FaObjectData>()
            .SetColumns(x => new FaObjectData { IsActive = isActive, UpdateTime = now, UpdateUId = modifiedBy })
            .Where(x => x.PointAddress == pointAddress)
            .ExecuteCommandAsync(ct);
    }

    /// <summary>Cũ: UPDATE FIRE_ALARM_CONFIG SET IS_ALARM, LAST_ALARM_TIMESTAMP = GETUTCDATE() WHERE ID.</summary>
    public Task<int> UpdateDoorStateAsync(string id, bool isAlarm, CancellationToken ct = default)
    {
        var utcNow = DateTime.UtcNow;
        return Client.Updateable<FaFireAlarmConfig>()
            .SetColumns(x => new FaFireAlarmConfig { IsAlarm = isAlarm, LastAlarmTime = utcNow })
            .Where(x => x.ID == id)
            .ExecuteCommandAsync(ct);
    }

    /// <summary>Cũ: chỉ UPDATE theo ObjectKey, KHÔNG insert — không có cờ thì không làm gì.</summary>
    public Task<int> UpdateFlagAsync(string key, string value, string modifiedBy, CancellationToken ct = default) =>
        Client.Updateable<FaObjectFlag>()
            .SetColumns(x => new FaObjectFlag { FlagValue = value, UpdateUId = modifiedBy })
            .Where(x => x.FlagKey == key)
            .ExecuteCommandAsync(ct);

    /// <summary>Cũ: UPDATE FA_OBJECT_DATA SET IsActived = 0 (toàn bảng, không ghi LastModified).</summary>
    public Task<int> ResetAllPointsAsync(CancellationToken ct = default) =>
        Client.Updateable<FaObjectData>()
            .SetColumns(x => new FaObjectData { IsActive = false })
            .Where(x => true)
            .ExecuteCommandAsync(ct);

    /// <summary>
    /// Cũ: có INCIDENTS cùng Km/Mét, SOURCE = 'FA', STATUS &gt; 0 và không thuộc (4, 5).
    /// Trạng thái đóng lấy từ <see cref="FaIncidentOptions.ClosedStates"/>.
    /// </summary>
    public async Task<bool> IsLocationHasIncidentAsync(int kmNumber, int metNumber, CancellationToken ct = default)
    {
        var closed = _options.Incident.ClosedStates;
        return await Client.Queryable<TmsIncident>()
            .Where(x => x.IsDelete == null
                        && x.Source == _options.Incident.Source
                        && x.KmNumber == kmNumber
                        && x.MetNumber == metNumber
                        && x.State != null
                        && !closed.Contains(x.State))
            .AnyAsync();
    }

    /// <summary>Loại sự kiện báo cháy để điền nhóm, mức cảnh báo, icon vào sự cố và bản tin.</summary>
    public async Task<TmsEventType?> GetEventTypeAsync(string id, CancellationToken ct = default) =>
        await Client.Queryable<TmsEventType>().Where(x => x.ID == id && x.IsDelete == null).FirstAsync(ct);

    /// <summary>
    /// Tên hiển thị của một mã danh mục (SysConfigData theo SysConfigType.Code), như ConfigDataManager của VDS.
    /// Không lọc tenant, chỉ loại xoá mềm. Không có thì trả null để người gọi dùng lại mã.
    /// </summary>
    public async Task<string?> GetConfigNameAsync(string configTypeCode, string? code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var key = code.Trim();
        var name = await Client.Queryable<Modules.CfgSystem.Core.Entities.SysConfigData>()
            .InnerJoin<Modules.CfgSystem.Core.Entities.SysConfigType>((data, type) => type.ID == data.ConfigTypeId)
            .Where((data, type) => type.Code == configTypeCode && type.IsDelete == null && data.IsDelete == null && data.Code == key)
            .OrderBy((data, type) => data.OrderNo)
            .Select((data, type) => data.Name)
            .FirstAsync(ct);
        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    // ----- Dữ liệu vùng / camera cho FaZoneLocator (cũ: ZONES, vw_CAMERA_EQUIPMENTS) -----

    public Task<List<TmsZone>> GetZonesAsync(CancellationToken ct = default) =>
        Client.Queryable<TmsZone>().Where(x => x.IsDelete == null).ToListAsync(ct);

    public Task<List<TmsZoneEquipment>> GetZoneEquipmentsAsync(IReadOnlyCollection<string> zoneIds, CancellationToken ct = default)
    {
        var ids = zoneIds.ToList();
        return Client.Queryable<TmsZoneEquipment>()
            .Where(x => x.IsDelete == null && ids.Contains(x.ZoneId))
            .ToListAsync(ct);
    }

    public Task<List<TmsEquipment>> GetCamerasAsync(IReadOnlyCollection<string> equipmentIds, string cameraTypeId, CancellationToken ct = default)
    {
        var ids = equipmentIds.ToList();
        return Client.Queryable<TmsEquipment>()
            .Where(x => x.IsDelete == null && x.EquipmentTypeId == cameraTypeId && ids.Contains(x.ID))
            .ToListAsync(ct);
    }

    public async Task InsertIncidentAsync(TmsIncident incident, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(incident.ID)) incident.ID = Guid.NewGuid().ToString();
        incident.TenantId = _options.TenantId;
        incident.CreateUId = _options.ServiceUserId;
        await Client.Insertable(incident).ExecuteCommandAsync(ct);
    }
}
