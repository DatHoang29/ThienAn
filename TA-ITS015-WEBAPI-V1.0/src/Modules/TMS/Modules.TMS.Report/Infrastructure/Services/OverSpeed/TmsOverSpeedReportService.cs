using System.Globalization;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Extensions;
using Modules.TMS.Report.Controllers.OverSpeed.Dto;
using Modules.TMS.Report.Core.Exceptions;
using Modules.TMS.Report.Core.Helpers;
using Shared.DTO.Dtos.System;
using Shared.DTO.Dtos.Vds;
using Shared.Infrastructure.Services;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Shared.Utility.Utilities;
using Wolverine;
using static Shared.DTO.Enums.BaseEnums;
using TmsMsg = Modules.TMS.Core.Exceptions.BaseMsg;

namespace Modules.TMS.Report.Infrastructure.Services.OverSpeed
{
    /// <summary>
    /// Author: KienLT
    /// Description: Dựng dữ liệu báo cáo phương tiện vượt quá tốc độ.
    ///
    /// <para><b>Nguồn vi phạm</b>: các lượt camera AID gắn cờ <c>Speeding</c> (QuestDB, đọc in-process qua handler VDS).
    /// <b>Ngưỡng</b>: từ 19/10/2026 ưu tiên ngưỡng camera đã áp LÚC VI PHẠM (cột <c>speedMax</c> worker lưu khi ghi bản
    /// tin). Bản ghi cũ chưa có cột thì lùi về ngưỡng HIỆN HÀNH, phân giải đúng thứ tự của <c>SpeedRangeResolver</c> —
    /// đoạn chính của camera, thiếu thì mặc định theo hướng; không đủ cặp hoặc min &gt; max thì coi như chưa cấu hình.
    /// Ngưỡng đổi trong kỳ thì tiêu đề nhóm liệt kê đủ các ngưỡng đã áp.</para>
    /// Created date: 16/10/2026
    /// </summary>
    public class TmsOverSpeedReportService : IScoped
    {
        /// <summary>Trần khoảng thời gian của báo cáo (ngày) — chặt hơn trần 60 ngày của QuestDB vì PDF dạng danh sách.</summary>
        private const int MaxRangeDays = 31;

        private readonly IStringLocalizer _localizer;
        private readonly IMessageBus _messBus;
        private readonly UserManager _user;

        public TmsOverSpeedReportService(IStringLocalizer localizer, IMessageBus messBus, UserManager user)
        {
            _localizer = localizer;
            _messBus = messBus;
            _user = user;
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Truy xuất và xử lý dữ liệu báo cáo vượt tốc độ.
        /// Created date: 16/10/2026
        /// </summary>
        public async Task<OverSpeedReport> BuildOverSpeedReport(OverSpeedRenderInput command)
        {
            CheckValidInput(command);

            var fromUtc = BaseUtil.TimeServer.ToUtc(command.FromDate!.Value);
            var toUtc = BaseUtil.TimeServer.ToUtc(command.ToDate!.Value);

            var speedConfig = await _messBus.InvokeAsync<ZoneSpeedConfigListOutput>(new SpeedConfigListInput());
            var catalog = CameraCatalog.Build(speedConfig);
            var configNames = await LoadConfigNames();

            var reportData = new OverSpeedReport();
            LocalizeReportColumns(reportData);
            SetReportParameters(reportData, command, catalog, configNames);

            // Lọc vùng quy về danh sách camera: quan hệ camera-vùng là n-n, cột ZoneId của bản ghi không đủ tin cậy.
            var equipmentIds = ResolveEquipmentFilter(command, catalog);
            if (equipmentIds is { Length: 0 })
                return reportData;

            var rows = await _messBus.InvokeVdsAsync<List<VdsOverSpeedRow>>(new VdsOverSpeedRequest
            {
                FromDate = fromUtc,
                ToDate = toUtc,
                EquipmentIds = equipmentIds ?? [],
                Direction = string.IsNullOrWhiteSpace(command.Direction) ? null : command.Direction.Trim(),
                VehicleSize = string.IsNullOrWhiteSpace(command.VehicleSize) ? null : command.VehicleSize.Trim(),
                LicensePlate = string.IsNullOrWhiteSpace(command.LicensePlate) ? null : command.LicensePlate.Trim(),
            }, TimeSpan.FromSeconds(30), TmsMsg.VdsQuery.Exception.ReportDataUnavailable);

            var notConfigured = _localizer[BaseMsg.RptOverSpeed.Label.NotConfigured].Value;
            var noZone = _localizer[BaseMsg.RptOverSpeed.Label.NoZone].Value;

            var violations = rows
                .Select(row =>
                {
                    var camera = catalog.Find(row.EquipmentId);
                    // Ưu tiên ngưỡng camera đã áp LÚC VI PHẠM (VdsTrafficData.speedMax). Bản ghi cũ
                    // chưa lưu cột thì lùi về ngưỡng hiện hành; camera không thuộc đoạn nào thì chỉ còn mặc định theo hướng.
                    var limit = row.SpeedMax is > 0
                        ? row.SpeedMax
                        : camera != null ? camera.MaxSpeed : catalog.DirectionMaxSpeed(row.Direction);
                    double? exceed = limit is int l && row.Speed is double s ? s - l : null;
                    return (Row: row, Camera: camera, Limit: limit, Exceed: exceed);
                })
                .Where(x => command.MinExceedKmh is not double min || (x.Exceed is double e && e >= min))
                .ToList();

            reportData.DataList = violations
                .GroupBy(x => x.Row.EquipmentId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var first = g.First();
                    var camera = first.Camera;
                    var maxSpeed = g.Max(x => x.Row.Speed);
                    var items = g.OrderByDescending(x => x.Row.DetectTime).ThenByDescending(x => x.Row.VendorEventId)
                        .Select((x, idx) => MapItem(x.Row, x.Limit, x.Exceed, idx + 1, camera, notConfigured, configNames))
                        .ToList();

                    var zoneName = camera?.ZoneName ?? noZone;
                    var cameraName = camera?.Name ?? first.Row.EquipmentId ?? string.Empty;
                    var direction = DirectionName(camera?.DirectionId ?? first.Row.Direction, configNames.Directions);
                    return new OverSpeedGroupOutput
                    {
                        Title = $"{zoneName} - {cameraName}"
                                + (direction.Length > 0 ? $" ({reportData.Columns.Direction}: {direction})" : string.Empty),
                        Zone = zoneName,
                        Camera = cameraName,
                        Direction = direction,
                        // Ngưỡng có thể đổi trong kỳ báo cáo ⇒ liệt kê đủ các ngưỡng đã áp (vd "90, 100").
                        SpeedLimit = GroupSpeedLimits(g.Select(x => x.Limit), notConfigured),
                        Count = items.Count,
                        MaxSpeed = FormatSpeed(maxSpeed),
                        Items = items,
                    };
                })
                .OrderBy(x => x.Zone, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.Camera, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var exceeds = violations.Where(x => x.Exceed.HasValue).Select(x => x.Exceed!.Value).ToList();
            reportData.Summary = new OverSpeedSummary
            {
                TotalViolation = violations.Count,
                DistinctPlate = violations.Select(x => x.Row.LicensePlate?.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                MaxSpeed = FormatSpeed(violations.Max(x => x.Row.Speed)),
                AvgExceed = exceeds.Count > 0 ? FormatSpeed(exceeds.Average()) : string.Empty,
            };

            return reportData;
        }

        private OverSpeedItemOutput MapItem(VdsOverSpeedRow row, int? limit, double? exceed, int idx,
            CameraInfo? camera, string notConfigured, ConfigNames names)
        {
            var percent = exceed is double e && limit is int l && l > 0 ? e * 100 / l : (double?)null;
            return new OverSpeedItemOutput
            {
                Idx = idx,
                Time = BaseUtil.TimeServer.ToServerTime(row.DetectTime).ToString(DateTimeFormatEnum.DateTimeDefault),
                LicensePlate = row.LicensePlate?.Trim() ?? string.Empty,
                VehicleType = row.VehicleType ?? string.Empty,
                VehicleSize = VehicleSizeName(row.VehicleSize, names) ?? string.Empty,
                VehicleColor = row.VehicleColor ?? string.Empty,
                Location = FormatLocation(row.KmNumber ?? camera?.KmNumber, row.MetNumber ?? camera?.MetNumber),
                Speed = FormatSpeed(row.Speed),
                SpeedLimit = limit?.ToString(CultureInfo.InvariantCulture) ?? notConfigured,
                Exceed = FormatExceed(exceed, percent),
                ExceedPercentValue = percent ?? 0,
            };
        }

        /// <summary>
        /// Các ngưỡng khác nhau đã áp cho một camera trong kỳ, tăng dần (vd "90, 100"); dòng chưa có ngưỡng không tính.
        /// Không còn ngưỡng nào ⇒ <paramref name="notConfigured"/>.
        /// </summary>
        private static string GroupSpeedLimits(IEnumerable<int?> limits, string notConfigured)
        {
            var distinct = limits.Where(l => l is > 0).Select(l => l!.Value).Distinct().OrderBy(l => l).ToList();
            return distinct.Count == 0
                ? notConfigured
                : string.Join(", ", distinct.Select(l => l.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>Gộp mức vượt km/h và % vào một ô: <c>+10(11%)</c>. Thiếu ngưỡng thì để trống.</summary>
        private static string FormatExceed(double? exceed, double? percent)
        {
            if (exceed is not double v) return string.Empty;
            var kmh = (v > 0 ? "+" : string.Empty) + FormatSpeed(v);
            return percent is double p ? $"{kmh}({p.ToString("0", CultureInfo.InvariantCulture)}%)" : kmh;
        }

        /// <summary>
        /// Danh sách camera gửi xuống VDS: <c>null</c> = không lọc; mảng rỗng = bộ lọc không khớp camera nào (trả báo
        /// cáo rỗng, KHÔNG gửi mảng rỗng xuống vì phía VDS hiểu rỗng là mọi camera).
        /// </summary>
        private static string[]? ResolveEquipmentFilter(OverSpeedRenderInput command, CameraCatalog catalog)
        {
            var zoneIds = Clean(command.ZoneIdList);
            var equipmentIds = Clean(command.EquipmentIdList);
            if (zoneIds.Count == 0 && equipmentIds.Count == 0) return null;

            IEnumerable<string>? result = null;
            if (zoneIds.Count > 0) result = catalog.CamerasInZones(zoneIds);
            if (equipmentIds.Count > 0)
                result = result == null ? equipmentIds : result.Intersect(equipmentIds, StringComparer.OrdinalIgnoreCase);

            return result!.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static List<string> Clean(string[]? values)
            => values?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList() ?? [];

        private void CheckValidInput(OverSpeedRenderInput command)
        {
            if (command.FromDate == null || command.ToDate == null || command.FromDate >= command.ToDate)
                throw Oops.Oh(_localizer[BaseMsg.RptOverSpeed.Validation.TimeRangeInvalid].Value);

            if ((command.ToDate.Value - command.FromDate.Value).TotalDays > MaxRangeDays)
                throw Oops.Oh(string.Format(_localizer[BaseMsg.RptOverSpeed.Validation.LimitDateRange].Value, MaxRangeDays));

            if (command.MinExceedKmh is < 0)
                throw Oops.Oh(_localizer[BaseMsg.RptOverSpeed.Validation.MinExceedInvalid].Value);
        }

        /// <summary>Tên hiển thị lấy từ SysConfigData: hướng theo mã, cỡ xe theo short/medium/long.</summary>
        private sealed record ConfigNames(Dictionary<string, string> Directions, Dictionary<string, string> VehicleSizes);

        /// <summary>
        /// Đọc danh mục cấu hình MỘT lần: tên hướng (<c>direction_config_type</c>) và tên cỡ xe (<c>vehicle_type</c>) —
        /// cùng nguồn với dropdown của FE nên báo cáo không lệch tên khi cấu hình đổi.
        /// </summary>
        private static async Task<ConfigNames> LoadConfigNames()
        {
            var response = await BaseApiClient<IConfigDataApi>.Client().List(new SysConfigDataListRequest());
            var configs = (response.Content?.Result ?? new List<SysConfigDataResponse>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Code) && !string.IsNullOrWhiteSpace(c.Name)).ToList();

            Dictionary<string, string> ByType(string typeCode, Func<SysConfigDataResponse, string?> key) => configs
                .Where(c => string.Equals(c.ConfigTypeCode, typeCode, StringComparison.OrdinalIgnoreCase))
                .Select(c => (Key: key(c), c.Name))
                .Where(x => x.Key != null)
                .GroupBy(x => x.Key!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Name!, StringComparer.OrdinalIgnoreCase);

            return new ConfigNames(
                ByType(SysConfigTypeCodeEnum.TmsDirectionConfigCode, c => c.Code.Trim()),
                ByType(SysConfigTypeCodeEnum.TmsVehicleTypeConfigCode, c => VehicleSizeOf(c.Value, c.Code)));
        }

        /// <summary>
        /// Quy một mục cấu hình loại xe về cỡ <c>short/medium/long</c> — cùng quy tắc với <c>vehicleCountField</c> của FE:
        /// ưu tiên ảnh (<c>S/M/L.png</c>), sau đó đến mã (<c>s/short/1</c>, <c>m/medium/3</c>, <c>l/long/4</c>).
        /// </summary>
        private static string? VehicleSizeOf(string? value, string? code)
        {
            var bySize = (value?.Trim().ToLowerInvariant()) switch { "s.png" => "short", "m.png" => "medium", "l.png" => "long", _ => null };
            return bySize ?? (code?.Trim().ToLowerInvariant()) switch
            {
                "s" or "short" or "1" => "short",
                "m" or "medium" or "3" => "medium",
                "l" or "long" or "4" => "long",
                _ => null,
            };
        }

        private static string DirectionName(string? code, Dictionary<string, string> names)
            => string.IsNullOrWhiteSpace(code) ? string.Empty
                : names.TryGetValue(code.Trim(), out var name) ? name : code.Trim();

        private static string FormatSpeed(double? value)
            => value is double v ? Math.Round(v, 1).ToString("0.#", CultureInfo.InvariantCulture) : string.Empty;

        private static string FormatLocation(int? km, int? met)
            => km == null ? string.Empty : met == null ? $"Km{km}" : $"Km{km}+{met}";

        /// <summary>
        /// Author: KienLT
        /// Description: Set dữ liệu cho tham số ra
        /// Created date: 16/10/2026
        /// </summary>
        private void SetReportParameters(OverSpeedReport reportData, OverSpeedRenderInput command, CameraCatalog catalog,
            ConfigNames names)
        {
            var from = BaseUtil.TimeServer.ToServerTime(BaseUtil.TimeServer.ToUtc(command.FromDate!.Value));
            var to = BaseUtil.TimeServer.ToServerTime(BaseUtil.TimeServer.ToUtc(command.ToDate!.Value));

            reportData.Parameters.DataDate = from.ToString(DateTimeFormatEnum.DateTimeDefault) + " - " + to.ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportDate = BaseUtil.TimeServer.GetTimeServer().ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = "wwwroot/upload/sign.png";
            reportData.Parameters.ReportUser = _user.Account;

            reportData.Parameters.ReportTitle = _localizer[BaseMsg.RptOverSpeed.Label.RptTitle].Value.ToUpper();
            reportData.Parameters.PageLabel = _localizer[BaseMsg.RptBase.Label.Page].Value;
            reportData.Parameters.SpeedLimitNote = _localizer[BaseMsg.RptOverSpeed.Label.SpeedLimitNote].Value;
            reportData.Parameters.FilterText = BuildFilterText(command, catalog, names);
        }

        private string BuildFilterText(OverSpeedRenderInput command, CameraCatalog catalog, ConfigNames names)
        {
            var all = _localizer[BaseMsg.RptOverSpeed.Label.All].Value;
            string Join(IEnumerable<string> values) { var list = values.ToList(); return list.Count == 0 ? all : string.Join(", ", list); }

            var parts = new List<string>
            {
                $"{_localizer[BaseMsg.RptOverSpeed.Entity.Zone].Value}: {Join(Clean(command.ZoneIdList).Select(catalog.ZoneName))}",
                $"{_localizer[BaseMsg.RptOverSpeed.Entity.Camera].Value}: {Join(Clean(command.EquipmentIdList).Select(id => catalog.Find(id)?.Name ?? id))}",
                $"{_localizer[BaseMsg.RptOverSpeed.Entity.Direction].Value}: {(string.IsNullOrWhiteSpace(command.Direction) ? all : DirectionName(command.Direction, names.Directions))}",
                $"{_localizer[BaseMsg.RptOverSpeed.Entity.VehicleSize].Value}: {VehicleSizeName(command.VehicleSize, names) ?? all}",
            };
            if (!string.IsNullOrWhiteSpace(command.LicensePlate))
                parts.Add($"{_localizer[BaseMsg.RptOverSpeed.Entity.LicensePlate].Value}: {command.LicensePlate.Trim()}");
            if (command.MinExceedKmh is double min)
                parts.Add($"{_localizer[BaseMsg.RptOverSpeed.Entity.Exceed].Value} ≥ {FormatSpeed(min)}");

            return $"{_localizer[BaseMsg.RptOverSpeed.Label.Filter].Value}: {string.Join("; ", parts)}";
        }

        /// <summary>Tên cỡ xe theo cấu hình <c>vehicle_type</c>; cấu hình thiếu cỡ nào thì lùi về bản dịch của báo cáo lưu lượng.</summary>
        private string? VehicleSizeName(string? size, ConfigNames names)
        {
            var code = size?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(code)) return null;
            if (names.VehicleSizes.TryGetValue(code, out var name)) return name;
            return code switch
            {
                "short" => _localizer[BaseMsg.RptTrafficVolumeDay.Entity.ShortLength].Value,
                "medium" => _localizer[BaseMsg.RptTrafficVolumeDay.Entity.MediumLength].Value,
                "long" => _localizer[BaseMsg.RptTrafficVolumeDay.Entity.LongLength].Value,
                _ => size!.Trim(),
            };
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Dịch thuật tiêu đề cột
        /// Created date: 16/10/2026
        /// </summary>
        private void LocalizeReportColumns(OverSpeedReport reportData)
        {
            var c = reportData.Columns;
            c.Idx = _localizer[BaseMsg.RptBase.Label.Idx].Value.ToUpper();
            c.Time = _localizer[BaseMsg.RptOverSpeed.Entity.Time].Value.ToUpper();
            c.LicensePlate = _localizer[BaseMsg.RptOverSpeed.Entity.LicensePlate].Value.ToUpper();
            c.VehicleType = _localizer[BaseMsg.RptOverSpeed.Entity.VehicleType].Value.ToUpper();
            c.VehicleSize = _localizer[BaseMsg.RptOverSpeed.Entity.VehicleSize].Value.ToUpper();
            c.VehicleColor = _localizer[BaseMsg.RptOverSpeed.Entity.VehicleColor].Value.ToUpper();
            c.Location = _localizer[BaseMsg.RptOverSpeed.Entity.Location].Value.ToUpper();
            c.Speed = _localizer[BaseMsg.RptOverSpeed.Entity.Speed].Value.ToUpper();
            c.SpeedLimit = _localizer[BaseMsg.RptOverSpeed.Entity.SpeedLimit].Value.ToUpper();
            c.SpeedLimitLabel = _localizer[BaseMsg.RptOverSpeed.Entity.SpeedLimit].Value;
            c.Exceed = _localizer[BaseMsg.RptOverSpeed.Entity.Exceed].Value.ToUpper();
            c.Zone = _localizer[BaseMsg.RptOverSpeed.Entity.Zone].Value;
            c.Camera = _localizer[BaseMsg.RptOverSpeed.Entity.Camera].Value;
            c.Direction = _localizer[BaseMsg.RptOverSpeed.Entity.Direction].Value;
            c.TotalViolation = _localizer[BaseMsg.RptOverSpeed.Label.TotalViolation].Value;
            c.DistinctPlate = _localizer[BaseMsg.RptOverSpeed.Label.DistinctPlate].Value;
            c.MaxSpeed = _localizer[BaseMsg.RptOverSpeed.Label.MaxSpeed].Value;
            c.AvgExceed = _localizer[BaseMsg.RptOverSpeed.Label.AvgExceed].Value;
            c.Violation = _localizer[BaseMsg.RptOverSpeed.Label.Violation].Value;
        }

        /// <summary>Camera VDS kèm đoạn chính và ngưỡng tối đa hiện hành đã phân giải.</summary>
        private sealed record CameraInfo(string Id, string Name, string? ZoneName, string? DirectionId,
            int? KmNumber, int? MetNumber, int? MaxSpeed);

        /// <summary>
        /// Danh mục camera dựng từ <see cref="ZoneSpeedConfigListOutput"/> — cùng nguồn với màn cấu hình ngưỡng tốc độ,
        /// nên camera, đoạn chính và ngưỡng khớp đúng thứ worker đẩy xuống AID.
        /// </summary>
        private sealed class CameraCatalog
        {
            private readonly Dictionary<string, CameraInfo> _cameras = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, List<string>> _camerasByZone = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> _zoneNames = new(StringComparer.OrdinalIgnoreCase);
            private Dictionary<string, ZoneDirectionDefaultOutput> _directionDefaults = new(StringComparer.OrdinalIgnoreCase);

            public static CameraCatalog Build(ZoneSpeedConfigListOutput config)
            {
                var catalog = new CameraCatalog();
                var directionDefaults = config.DirectionDefaults
                    .GroupBy(d => d.Direction.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                catalog._directionDefaults = directionDefaults;

                foreach (var zone in config.Zones)
                {
                    var zoneId = zone.Id.Trim();
                    catalog._zoneNames[zoneId] = zone.Name ?? zone.Code ?? zoneId;
                    catalog._camerasByZone[zoneId] = zone.VdsCameras.Select(c => c.Id.Trim()).ToList();
                }

                // Camera chỉ lấy ngưỡng theo ĐOẠN CHÍNH (IsPrimary) như SpeedRangeResolver.ForCamera.
                var cameraLinks = config.Zones
                    .SelectMany(z => z.VdsCameras.Select(c => (Zone: z, Camera: c)))
                    .GroupBy(x => x.Camera.Id.Trim(), StringComparer.OrdinalIgnoreCase);
                foreach (var links in cameraLinks)
                {
                    var primary = links.FirstOrDefault(x => x.Camera.IsPrimary);
                    var (zone, camera) = primary.Camera != null ? primary : links.First();
                    var direction = camera.DirectionId?.Trim();
                    directionDefaults.TryGetValue(direction ?? string.Empty, out var directionDefault);

                    catalog._cameras[links.Key] = new CameraInfo(
                        camera.Id, camera.Name ?? camera.Code ?? camera.Id, zone.Name ?? zone.Code, direction,
                        camera.KmNumber, camera.MetNumber,
                        primary.Camera != null ? ResolveMaxSpeed(zone, directionDefault) : ResolveMaxSpeed(null, directionDefault));
                }

                return catalog;
            }

            public CameraInfo? Find(string? equipmentId)
                => !string.IsNullOrWhiteSpace(equipmentId) && _cameras.TryGetValue(equipmentId.Trim(), out var c) ? c : null;

            public IEnumerable<string> CamerasInZones(IEnumerable<string> zoneIds)
                => zoneIds.SelectMany(z => _camerasByZone.TryGetValue(z, out var list) ? list : []);

            public string ZoneName(string zoneId) => _zoneNames.TryGetValue(zoneId, out var name) ? name : zoneId;

            /// <summary>Ngưỡng tối đa chỉ từ mặc định theo hướng — cho camera không gắn đoạn nào.</summary>
            public int? DirectionMaxSpeed(string? direction)
                => ResolveMaxSpeed(null, !string.IsNullOrWhiteSpace(direction)
                    && _directionDefaults.TryGetValue(direction.Trim(), out var d) ? d : null);

            /// <summary>
            /// Ghép ngưỡng đoạn với mặc định hướng theo TỪNG giá trị, đúng <c>SpeedRangeResolver.Combine</c>: thiếu một vế
            /// hoặc min &gt; max ⇒ null (camera không nhận ngưỡng nào).
            /// </summary>
            private static int? ResolveMaxSpeed(ZoneSpeedConfigOutput? zone, ZoneDirectionDefaultOutput? direction)
            {
                var min = ToKmh(zone?.MinSpeed) ?? Positive(direction?.MinSpeed);
                var max = ToKmh(zone?.MaxSpeed) ?? Positive(direction?.MaxSpeed);
                return min is int lo && max is int hi && lo <= hi ? hi : null;
            }

            private static int? Positive(int? value) => value is > 0 ? value : null;

            private static int? ToKmh(float? value)
            {
                if (value is not float v || v <= 0) return null;
                var rounded = (int)Math.Round(v, MidpointRounding.AwayFromZero);
                return rounded > 0 ? rounded : null;
            }
        }
    }
}
