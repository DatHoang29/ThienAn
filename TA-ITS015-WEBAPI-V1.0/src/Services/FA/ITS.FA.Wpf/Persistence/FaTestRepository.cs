using ITS.FA.Core.Configuration;
using ITS.FA.Core.Models;
using Microsoft.Extensions.Options;
using Modules.FA.Core.Entities;
using Modules.TMS.Core.Entities;
using Services.Shared.Persistence;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;
using System.Globalization;

namespace ITS.FA.Wpf.Persistence;

/// <summary>
/// Truy vấn phục vụ riêng form test. Tách khỏi FaRepository để dịch vụ thật không mang các lệnh ghi của công cụ test.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public sealed class FaTestRepository : SqlSugarAccess
{
    /// <summary>Tiền tố Code của dữ liệu mẫu, dùng để nhận ra và không tạo trùng.</summary>
    public const string SampleCodePrefix = "FA-TEST-";

    private readonly FaOptions _options;

    public FaTestRepository(DatabaseManager databaseManager, IOptions<FaOptions> options)
        : base(databaseManager, () => options.Value.DataConnectionId)
    {
        _options = options.Value;
    }

    public Task<List<FaObjectData>> GetPointsAsync() =>
        Client.Queryable<FaObjectData>().Where(x => x.IsDelete == null).OrderBy(x => x.PointAddress).ToListAsync();

    public Task<List<FaFireAlarmConfig>> GetDoorsAsync() =>
        Client.Queryable<FaFireAlarmConfig>().Where(x => x.IsDelete == null).OrderBy(x => x.ModbusAddress).ToListAsync();

    public Task<List<TmsIncident>> GetRecentIncidentsAsync(int take = 50) =>
        Client.Queryable<TmsIncident>()
            .Where(x => x.IsDelete == null && x.Source == _options.Incident.Source)
            .OrderByDescending(x => x.StartDate)
            .Take(take)
            .ToListAsync();

    /// <summary>Kết thúc sự cố để thử trường hợp báo lại tại cùng vị trí phải tạo sự cố mới.</summary>
    public Task<int> FinishIncidentAsync(string id)
    {
        var state = ((int)BaseEnums.TmsIncidentStateEnum.Finished).ToString(CultureInfo.InvariantCulture);
        var now = BaseUtil.TimeServer.GetTimeServer();
        var user = _options.ServiceUserId;
        return Client.Updateable<TmsIncident>()
            .SetColumns(x => new TmsIncident { State = state, FinishedBy = user, FinishedDate = now })
            .Where(x => x.ID == id)
            .ExecuteCommandAsync();
    }

    /// <summary>Mã vị trí của dữ liệu mẫu (SysConfigData nhóm tms_zone_location), phải trùng TmsZone.Location.</summary>
    public const string SampleLocation = "main_route";

    /// <summary>
    /// Thêm 1 đầu báo, 1 nút ấn, 1 cửa nếu chưa có. Chỉ thêm dòng có mã FA-TEST-.
    /// Lý trình Km2+100..500 nằm trong vùng main_route có camera CCTV trên DB dev, để sự cố gắn được vùng và camera.
    /// </summary>
    public async Task<int> SeedSampleAsync()
    {
        var now = BaseUtil.TimeServer.GetTimeServer();
        var added = 0;

        FaObjectData Point(string code, int address, FaDeviceType type, string name, int met) => new()
        {
            ID = Guid.NewGuid().ToString(),
            Code = SampleCodePrefix + code,
            TenantId = _options.TenantId,
            CreateTime = now,
            CreateUId = _options.ServiceUserId,
            PointAddress = address,
            TypeObject = (int)type,
            Description = name,
            KmNumber = 2,
            MetNumber = met,
            Location = SampleLocation,
            Lane = 1,
            IsActive = false,
        };

        foreach (var point in new[]
                 {
                     Point("DETECTOR", 2001, FaDeviceType.Detector, "Đầu báo test", 100),
                     Point("BUTTON", 2002, FaDeviceType.Button, "Nút ấn test", 300),
                 })
        {
            if (await Client.Queryable<FaObjectData>().AnyAsync(x => x.Code == point.Code)) continue;
            added += await Client.Insertable(point).ExecuteCommandAsync();
        }

        var doorCode = SampleCodePrefix + "DOOR";
        if (!await Client.Queryable<FaFireAlarmConfig>().AnyAsync(x => x.Code == doorCode))
        {
            added += await Client.Insertable(new FaFireAlarmConfig
            {
                ID = Guid.NewGuid().ToString(),
                Code = doorCode,
                TenantId = _options.TenantId,
                CreateTime = now,
                CreateUId = _options.ServiceUserId,
                ModbusAddress = 2101,
                Description = "Cửa test",
                KmNumber = 2,
                MetNumber = 500,
                Location = SampleLocation,
                IsAlarm = false,
            }).ExecuteCommandAsync();
        }

        return added;
    }
}
