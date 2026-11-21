using System.Globalization;
using System.Text.Json;
using ITS.FA.Core.Configuration;
using ITS.FA.Core.Models;
using ITS.FA.Core.Persistence;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Helper;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;

namespace ITS.FA.Core.Processing;

/// <summary>
/// Tạo sự cố khi có điểm báo cháy. Port SqlDataEventHandler.OnAlarmActived của ITS008:
/// bỏ qua điểm chưa có vị trí (Km = 0 và Mét = 0), không tạo nếu vị trí đã có sự cố FA chưa đóng,
/// gom vùng và camera theo vị trí rồi ghi TmsIncident.
/// Mọi giá trị nghiệp vụ (loại sự kiện, nguồn, tiền tố, trạng thái đóng) lấy từ <see cref="FaIncidentOptions"/>.
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
public sealed class FaIncidentService
{
    private readonly FaRepository _repository; 
    private readonly FaZoneLocator _locator;
    private readonly IOptionsMonitor<FaOptions> _options;
    private readonly ILogger<FaIncidentService> _logger;

    public FaIncidentService(
        FaRepository repository,
        FaZoneLocator locator,
        IOptionsMonitor<FaOptions> options,
        ILogger<FaIncidentService> logger)
    {
        _repository = repository;
        _locator = locator;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Trả sự cố mới, hoặc null khi bỏ qua (tắt, thiếu vị trí, đã có sự cố).
    /// Cấu hình Service:Incident đọc lại MỖI lần tạo (IOptionsMonitor): sửa EventTypeId trong appsettings.json
    /// thì lần báo cháy kế tiếp dùng ngay, không cần khởi động lại dịch vụ.
    /// </summary>
    public async Task<FaCreatedIncident?> CreateForPointAsync(FaObjectData point, CancellationToken ct = default)
    {
        var options = _options.CurrentValue.Incident;
        if (!options.Enabled) return null;

        var km = point.KmNumber ?? 0;
        var met = point.MetNumber ?? 0;
        if (km == 0 && met == 0) return null;
        // Mã tuyến/hầm như TmsZone.Location. Trống = không lọc vùng (cũ: Tunnel = 0).
        var location = string.IsNullOrWhiteSpace(point.Location) ? null : point.Location.Trim();

        if (await _repository.IsLocationHasIncidentAsync(km, met, ct).ConfigureAwait(false))
        {
            _logger.LogInformation("Km{Km}+{Met} đã có sự cố báo cháy chưa đóng, không tạo mới.", km, met);
            return null;
        }

        // Loại sự kiện cháy do người dùng tạo trên web (Danh mục loại sự kiện), lấy ID khai vào
        // Service:Incident:EventTypeId. Không code cứng ID vì mỗi DB một ID.
        TmsEventType? eventType = null;
        var eventTypeId = options.EventTypeId?.Trim();
        if (string.IsNullOrEmpty(eventTypeId))
        {
            _logger.LogWarning("Service:Incident:EventTypeId chưa khai — sự cố báo cháy được tạo với EventTypeId = null.");
        }
        else
        {
            eventType = await _repository.GetEventTypeAsync(eventTypeId, ct).ConfigureAwait(false);
            if (eventType is null)
                _logger.LogWarning("Không có TmsEventType.ID = {EventTypeId} (sai ID hoặc đã xoá) — sự cố được tạo với EventTypeId = null. Kiểm tra Service:Incident:EventTypeId.", eventTypeId);
        }

        var zones = await _locator.GetZonesAsync(km, met, location, ct).ConfigureAwait(false);
        var cameras = await _locator.GetCamerasAsync(km, met, zones, ct).ConfigureAwait(false);

        var now = BaseUtil.TimeServer.GetTimeServer();
        const string influenceScope = "0"; // Báo cháy ảnh hưởng cả hai hướng, như bản cũ.

        // Đặt tên/mã bằng helper chung của TMS như VDS và web:
        //   Mã : ICC_{mã loại}_{mã tuyến}_{hướng}_{km}_{mét}_{ddMMyyyyHHmmss}
        //   Tên: {tên loại} {tên tuyến} {tên hướng} Km{km}+{mét} {dd-MM-yyyy HH:mm:ss}
        // Chưa khai loại sự kiện thì dùng mã/tên dự phòng trong cấu hình.
        var locationName = await _repository.GetConfigNameAsync(ZoneLocationConfigType, location, ct).ConfigureAwait(false) ?? location;
        var directionName = await _repository.GetConfigNameAsync(DirectionConfigType, influenceScope, ct).ConfigureAwait(false) ?? influenceScope;
        var code = IncidentNamingHelper.Code(eventType?.Code ?? options.FallbackEventCode, location, influenceScope, km, met, now);
        var name = IncidentNamingHelper.Name(eventType?.Name ?? options.FallbackEventName, locationName, directionName, km, met, now);

        var incident = new TmsIncident
        {
            Name = name,
            Code = code,
            // Chỉ ghi ID đã tra thấy trong TmsEventType: ID sai thì sự cố không trỏ tới loại không tồn tại.
            EventTypeId = eventType?.ID,
            // Trống thì lấy theo loại sự kiện, cùng cách IncidentBroadcaster chuẩn hóa nhóm.
            EventGroup = string.IsNullOrWhiteSpace(options.EventGroup)
                ? IncidentEventGroupConst.Normalize(eventType?.Type)
                : options.EventGroup,
            StartDate = now,
            Source = options.Source,
            // Giữ mặc định "0" của TmsIncident khi điểm chưa khai vị trí.
            Location = location ?? "0",
            ZoneId = string.Join(",", zones.Select(z => z.ID)),
            EquipmentId = string.Join(",", cameras.Select(c => c.ID)),
            KmNumber = km,
            MetNumber = met,
            InfluenceScope = influenceScope,
            CreatedBy = options.Source,
            CreatedDate = now,
            Description = BuildDescription(point, km, met),
            Metadata = BuildMetadata(point),
            State = ((int)BaseEnums.TmsIncidentStateEnum.Registration).ToString(CultureInfo.InvariantCulture),
        };
        incident.CreateTime = now;

        await InsertWithRetryAsync(incident, options, ct).ConfigureAwait(false);
        _logger.LogInformation("Đã tạo sự cố báo cháy {Code} tại Km{Km}+{Met}", incident.Code, km, met);
        return new FaCreatedIncident(incident, eventType);
    }

    /// <summary>Loại danh mục tên tuyến/hầm và hướng, cùng mã ConfigDataManager của VDS dùng.</summary>
    private const string ZoneLocationConfigType = "tms_zone_location";
    private const string DirectionConfigType = "direction_config_type";

    /// <summary>Tên loại điểm theo TypeObject (1 đầu báo, 2 nút ấn).</summary>
    private static string PointTypeName(int? typeObject) => typeObject switch
    {
        (int)FaDeviceType.Detector => "Đầu báo",
        (int)FaDeviceType.Button => "Nút ấn",
        _ => $"Điểm loại {typeObject}",
    };

    /// <summary>Ví dụ: "Đầu báo 'Đầu báo hầm 1' (địa chỉ 2001) báo cháy tại Km2+100". Cột Description dài 512.</summary>
    private static string BuildDescription(FaObjectData point, int km, int met)
    {
        var name = string.IsNullOrWhiteSpace(point.Description) ? null : $" '{point.Description.Trim()}'";
        var text = $"{PointTypeName(point.TypeObject)}{name} (địa chỉ {point.PointAddress}) báo cháy tại Km{km}+{met}";
        return text.Length <= 512 ? text : text[..512];
    }

    /// <summary>
    /// Điểm sinh ra sự cố, để truy lại thiết bị (như VDS lưu thông tin xe). Không dùng EventId vì cột đó là
    /// khoá UNIQUE theo bản tin: cùng một điểm báo lại sau khi sự cố kết thúc sẽ đụng khoá. Cột Metadata dài 512.
    /// </summary>
    private static string? BuildMetadata(FaObjectData point)
    {
        var json = JsonSerializer.Serialize(new
        {
            FaPointId = point.ID,
            FaPointAddress = point.PointAddress,
            FaPointType = point.TypeObject,
            FaPointName = point.Description,
            Lane = point.Lane,
        });
        return json.Length <= 512 ? json : null;
    }

    /// <summary>Cũ: RetryHelper.RetryOnException quanh db.SaveChanges. Hết số lần thử thì ném lỗi cho người gọi.</summary>
    private async Task InsertWithRetryAsync(TmsIncident incident, FaIncidentOptions options, CancellationToken ct)
    {
        var attempts = Math.Max(1, options.SaveRetryAttempts);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _repository.InsertIncidentAsync(incident, ct).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < attempts)
            {
                _logger.LogError(ex, "Ghi sự cố lỗi lần {Attempt}/{Attempts}, thử lại sau {Delay} giây.",
                    attempt, attempts, options.SaveRetryDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(options.SaveRetryDelaySeconds), ct).ConfigureAwait(false);
            }
        }
    }
}
