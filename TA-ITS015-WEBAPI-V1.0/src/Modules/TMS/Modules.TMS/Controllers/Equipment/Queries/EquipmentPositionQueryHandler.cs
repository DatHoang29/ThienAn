using Modules.VDS.Core.Helper;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using static Shared.DTO.Enums.BaseEnums;
using Modules.TMS.Core.Helper;
using Modules.VDS.Core.Dto;
using Modules.CfgSystem.Core.Dto.OpConfig;

namespace Modules.TMS.Controllers.Equipment.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Lớp nghiệp vụ xử lý các truy vấn thiết bị theo vị trí (Km/Met), hướng hoạt động
    /// và zone (thông qua bảng TmsZoneEquipment hoặc bắc cầu theo phạm vi zone).
    /// Note: HIỆN TẠI: DirectionCode (SysConfigData) = InfluenceScope (Incident) = LaneId (Zone) = DirectionId (Equipment)
    /// Tách ra từ EquipmentQueryHandler để tách riêng mối quan tâm vị trí/zone khỏi truy vấn CRUD cơ bản.
    /// Created date: 19/08/2024
    /// </summary>
    public class EquipmentPositionQueryHandler
    {
        private BaseRepository<TmsEquipment> _tmsEquipRep;
        private BaseRepository<TmsZoneEquipment> _zoneEquipRep;
        private BaseRepository<TmsEquipmentEventRule> _eventRuleRep;
        private BaseRepository<VdsConfig> _vdsConfigRep;

        public EquipmentPositionQueryHandler(BaseRepository<TmsEquipment> tmsEquipRep,
            BaseRepository<TmsZoneEquipment> zoneEquipRep,
            BaseRepository<TmsEquipmentEventRule> eventRuleRep,
            BaseRepository<VdsConfig> vdsConfigRep)
        {
            _tmsEquipRep = tmsEquipRep;
            _zoneEquipRep = zoneEquipRep;
            _eventRuleRep = eventRuleRep;
            _vdsConfigRep = vdsConfigRep;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo vị trí và hướng
        /// Note: HIỆN TẠI: DirectionCode (SysConfigData) = InfluenceScope (Incident) = LaneId (Zone) = DirectionId (Equipment)
        /// Created date: 19/08/2024
        /// </summary>
        public async Task<List<TmsEquipmentDataOutput>> HandleAsync(TmsEquipmentPositionInput command, IMessageBus messageBus)
        {
            var eList = new List<TmsEquipmentDataOutput>();

            // Lấy phạm vi theo zone; tuyến của thiết bị được lọc theo Location của từng zone.
            List<ZoneOutput> zList = await messageBus.InvokeAsync<List<ZoneOutput>>(new ZoneWithChildInput()
            {
                ID = command.ZoneId,
                IsExcludeParent = true
            });


            zList = zList.Where(z => z.IsDelete == null && z.Status == StatusEnum.Enable
                && (string.IsNullOrWhiteSpace(command.DirectionId) || command.DirectionId == SpecialConfigEnum.AllDirection
                    || z.LaneId == command.DirectionId || z.LaneId == SpecialConfigEnum.AllDirection)).ToList();
            if (string.IsNullOrWhiteSpace(command.ZoneId) && command.KmNumber.HasValue)
            {
                var position = command.KmNumber.Value * 1000 + (command.MetNumber ?? 0);
                zList = zList.Where(z => {
                    var a = (z.FromKmNumber ?? 0) * 1000 + (z.FromMetNumber ?? 0);
                    var b = (z.ToKmNumber ?? 0) * 1000 + (z.ToMetNumber ?? 0);
                    return Math.Min(a, b) <= position && position < Math.Max(a, b);
                }).ToList();
            }
            var coverage = await LoadPtzCoverageAsync(zList);

            // TODO: LƯU Ý LaneId ??? TẠM THỜI LaneId LÀ DirectionId (Ex: Left, Right, All...)
            zList?.ForEach(z =>
            {
                // Retrive the list of equipment in each zone
                int fromPosition = (z.FromKmNumber ?? 0) * 1000 + (z.FromMetNumber ?? 0);
                int toPosition = (z.ToKmNumber ?? 0) * 1000 + (z.ToMetNumber ?? 0);
                var mappedIds = coverage.Where(key => key.EndsWith("|" + z.ID, StringComparison.OrdinalIgnoreCase))
                    .Select(key => key[..key.IndexOf('|')]).ToList();

                var list = this._tmsEquipRep.AsQueryable()
                    .Where(e =>
                        mappedIds.Contains(e.ID) || (
                            (SqlFunc.IsNull(e.KmNumber, 0) * 1000 + SqlFunc.IsNull(e.MetNumber, 0) >= fromPosition)
                            &&
                            (SqlFunc.IsNull(e.KmNumber, 0) * 1000 + SqlFunc.IsNull(e.MetNumber, 0) < toPosition)
                        )
                        || // LẤY CẢ HAI CHIỀU
                        (
                            (SqlFunc.IsNull(e.KmNumber, 0) * 1000 + SqlFunc.IsNull(e.MetNumber, 0) >= toPosition)
                            &&
                            (SqlFunc.IsNull(e.KmNumber, 0) * 1000 + SqlFunc.IsNull(e.MetNumber, 0) < fromPosition)
                        )
                    )
                    .Where(e => e.Status == StatusEnum.Enable && e.IsDelete == null)
                    .WhereIF(!string.IsNullOrWhiteSpace(z.Location), e => e.Location == z.Location)
                    .OrderBy(e => e.KmNumber)
                    .OrderBy(e => e.MetNumber)
                    .Select((e) => new TmsEquipmentDataOutput() { ZoneId = z.ID, ZoneName = z.Name }, true)
                    .ToList();

                // IMPORTANCE: LỌC HƯỚNG HOẠT ĐỘNG PHÙ HỢP VỚI HƯỚNG CỦA SỰ CỐ (KHÔNG PHẢI HƯỚNG CỦA ZONE)
                // SAU KHI LẤY ĐƯỢC CÁC THIẾT BỊ THÔNG QUA BẮC CẦU PHẠM VỊ CỦA ZONE DỰA VÀO VỊ TRÍ SỰ CỐ
                // THỰC HIỆN LỌC CÁC THIẾT BỊ CÓ HƯỚNG HOẠT ĐỘNG CÙNG VỚI HƯỚNG CỦA SỰ CỐ
                list = list?.Where(equip => this.CheckOperationDirectionEquipment(command.DirectionId, equip.DirectionId, equip.ActiveDirection, z, coverage.Contains(CoverageKey(equip.ID, z.ID)))).ToList();

                // Danh sách toàn bộ thiết bị (bao gồm camera)
                if (list?.Count() > 0)
                    eList.AddRange(list);
            });

            return eList.DistinctBy(e => e.ID).OrderBy(item => item.ZoneName).ToList();
        }


        /// <summary>
        /// Author: ThangNN
        /// Description: List theo danh sách zone và thiết bị
        /// Note: HIỆN TẠI: DirectionCode (SysConfigData) = InfluenceScope (Incident) = LaneId (Zone) = DirectionId (Equipment)
        /// Created date: 19/08/2024
        /// </summary>
        public async Task<List<TmsEquipmentDataOutput>> HandleAsync(TmsEquipmentZoneListInput command, IMessageBus messageBus)
        {
            var eList = new List<TmsEquipmentDataOutput>();
            if ((command.ZoneIdList?.Count() ?? 0) == 0 || (command.EquipmentIdList?.Count() ?? 0) == 0)
                return eList;

            var zList = await messageBus.InvokeAsync<List<ZoneOutput>>(new TmsZoneIdListInput() { ZoneIdList = string.Join(",", command.ZoneIdList) });

            if (!string.IsNullOrWhiteSpace(command.ZoneType))
                zList = zList?.Where(item => item.Type != null && item.Type.Equals(command.ZoneType, StringComparison.OrdinalIgnoreCase)).ToList();

            if (zList == null || zList.Count == 0)
                return eList;

            var coverage = await LoadPtzCoverageAsync(zList);

            // Báo cáo toàn tuyến có nhiều zone: chỉ đọc danh sách thiết bị một lần.
            var equipmentList = await this._tmsEquipRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentType), e => e.EquipmentTypeId == command.EquipmentType)
                .Where(e => command.EquipmentIdList.Contains(e.ID))
                .Where(e => e.Status == StatusEnum.Enable && e.IsDelete == null)
                .OrderBy(e => e.KmNumber)
                .OrderBy(e => e.MetNumber)
                .ToListAsync();

            // TODO: LƯU Ý LaneId ??? TẠM THỜI LaneId LÀ DirectionId (Ex: Left, Right, All...)
            zList.ForEach(z =>
            {
                // Giữ nguyên quy tắc khoảng [đầu, cuối) cho cả hai chiều và lọc hướng theo zone.
                int fromPosition = (z.FromKmNumber ?? 0) * 1000 + (z.FromMetNumber ?? 0);
                int toPosition = (z.ToKmNumber ?? 0) * 1000 + (z.ToMetNumber ?? 0);

                foreach (var equipment in equipmentList)
                {
                    int position = (equipment.KmNumber ?? 0) * 1000 + (equipment.MetNumber ?? 0);
                    bool inZone = (position >= fromPosition && position < toPosition)
                        || (position >= toPosition && position < fromPosition);
                    if ((!string.IsNullOrWhiteSpace(z.Location) && equipment.Location != z.Location)
                        || (!inZone && !coverage.Contains(CoverageKey(equipment.ID, z.ID)))
                        || !this.CheckOperationDirectionEquipment(z.LaneId, equipment.DirectionId, equipment.ActiveDirection, z, coverage.Contains(CoverageKey(equipment.ID, z.ID))))
                        continue;

                    var item = equipment.Adapt<TmsEquipmentDataOutput>();
                    item.ZoneId = z.ID;
                    item.ZoneName = z.Name;
                    eList.Add(item);
                }
            });

            return eList;
        }

        /// <summary>
        /// Author: ThangNN, ChuongLP
        /// Description: List GỢI Ý THIẾT BỊ (VMS/LCS) quanh vị trí sự cố. Đây là điểm vào DUY NHẤT của
        ///              nghiệp vụ gợi ý thiết bị (dùng chung cho FE thủ công và luồng tự động).
        /// Created date: 09/08/2024 (gộp logic: 19/08/2026)
        /// </summary>
        public async Task<List<TmsEquipmentDataOutput>> HandleAsync(TmsEquipmentSuggestionInput command, IMessageBus messageBus)
        {
            // ============ 1. Phạm vi ============
            // small: phạm vi tích sẵn. large: phạm vi gợi ý thêm (chỉ hiển thị).
            // Cả hai do FE/automation đọc từ SysOpConfig rồi truyền xuống; thiếu thì fallback dùng chung.
            var smallRange = command.SuggestionSmallRange.GetValueOrDefault() > 0
                ? command.SuggestionSmallRange.Value
                : EquipmentSuggestionConst.DefaultSmallRangeMeters;
            var largeRange = command.SuggestionLongRange.GetValueOrDefault();

            var position = (command.KmNumber ?? 0) * 1000 + (command.MetNumber ?? 0);

            var effectiveRange = (command.IsExpand && largeRange > smallRange) ? largeRange : smallRange;
            var fromPosition = position - effectiveRange;
            var toPosition = position + effectiveRange;

            // Phạm vi toàn tuyến (chỉ khi tick + có cấu hình đầu/cuối tuyến).
            var useWholeRoute = command.IsWholeRoute && command.RouteStartPosition.HasValue && command.RouteEndPosition.HasValue;
            var routeLo = useWholeRoute ? Math.Min(command.RouteStartPosition.Value, command.RouteEndPosition.Value) : 0;
            var routeHi = useWholeRoute ? Math.Max(command.RouteStartPosition.Value, command.RouteEndPosition.Value) : 0;

            // ============ 2. Lấy ứng viên ============
            var equipmentList = await this._tmsEquipRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), e => e.EquipmentTypeId == command.EquipmentTypeId)
                .Where(e => e.Status == StatusEnum.Enable && e.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Location), e => e.Location == command.Location)
                .Where(e =>
                    (
                        SqlFunc.IsNull(e.KmNumber, 0) * 1000 + SqlFunc.IsNull(e.MetNumber, 0) >= fromPosition
                        && SqlFunc.IsNull(e.KmNumber, 0) * 1000 + SqlFunc.IsNull(e.MetNumber, 0) <= toPosition
                    )
                    ||
                    (
                        useWholeRoute
                        && SqlFunc.IsNull(e.KmNumber, 0) * 1000 + SqlFunc.IsNull(e.MetNumber, 0) >= routeLo
                        && SqlFunc.IsNull(e.KmNumber, 0) * 1000 + SqlFunc.IsNull(e.MetNumber, 0) <= routeHi
                    )
                )
                .Select((e) => new TmsEquipmentDataOutput() { IsRecommended = false }, true)
                .ToListAsync();

            if (equipmentList == null || equipmentList.Count == 0)
                return new List<TmsEquipmentDataOutput>();

            // ============ 3. Ngữ cảnh để quyết định TÍCH SẴN ============
            // Zone chứa vị trí sự cố, đã lọc theo TUYẾN (Location) và hướng -> dùng làm phạm vi "thuộc tuyến"
            // và để suy ra chiều di chuyển của phương tiện.
            var zList = await messageBus.InvokeAsync<List<ZoneOutput>>(new TmsZonePositionInput()
            {
                KmNumber = command.KmNumber,
                MetNumber = command.MetNumber,
                DirectionId = command.DirectionId, // Zone.LaneId == command.DirectionId
                Location = command.Location,       // Tuyến của sự cố (tms_zone_location)
                IsExcludeParent = true             // Chỉ lấy zone cấp bé nhất
            });

            // Before/After: automation truyền ProcessType của đúng rule; FE để trống -> tra theo cặp EventType+EquipmentType.
            var processTypes = !string.IsNullOrWhiteSpace(command.ProcessType)
                ? new List<string> { command.ProcessType.Trim() }
                : await this.ResolveProcessTypes(command.EventTypeId, command.EquipmentTypeId);

            // Chiều di chuyển (+1 tăng lý trình / -1 giảm lý trình) để biết thiết bị nằm trước hay sau sự cố.
            var travelSign = await ResolveTravelSignAsync(zList, command.DirectionId, command.Location);
            var coverage = await LoadPtzCoverageAsync(zList);

            // Sự cố ảnh hưởng CẢ HAI chiều -> không ràng buộc trước/sau (xem IsPositionValidByProcessType).
            var isAllDirectionIncident = !string.IsNullOrWhiteSpace(command.DirectionId)
                && command.DirectionId.Equals(SpecialConfigEnum.AllDirection, StringComparison.OrdinalIgnoreCase);

            // Cờ SysOpConfig: bật -> bỏ ràng buộc zone, tắt/chưa cấu hình -> giữ luồng cũ (xem EquipmentSuggestionConst).
            var ignoreZone = await this.IsIgnoreZoneEnabledAsync(messageBus);

            // ============ 4. Đánh dấu tích sẵn ============
            foreach (var equip in equipmentList)
            {
                var ePosition = (equip.KmNumber ?? 0) * 1000 + (equip.MetNumber ?? 0);
                var distance = Math.Abs(ePosition - position);
                equip.DistanceMeters = distance;

                // Ghi lại loại xử lý thỏa (kể cả thiết bị ngoài small) để FE gắn nhãn Trước/Sau và dễ chẩn đoán.
                equip.ProcessTypeMatched = processTypes.FirstOrDefault(pt =>
                    this.IsPositionValidByProcessType(pt, ePosition, position, travelSign, isAllDirectionIncident));

                equip.IsRecommended =
                    distance <= smallRange
                    && (this.CheckOperationDirectionEquipment(command.DirectionId, equip.DirectionId, equip.ActiveDirection, null)
                        || zList.Any(z => CheckOperationDirectionEquipment(command.DirectionId, equip.DirectionId, equip.ActiveDirection, z,
                            coverage.Contains(CoverageKey(equip.ID, z.ID)))))
                    && (ignoreZone || this.IsEquipmentInRouteZones(ePosition, zList))
                    && equip.ProcessTypeMatched != null;
            }

            // Gần -> xa. Luồng tự động dựa vào thứ tự này để cắt theo MaxTargetCount của rule.
            return equipmentList
                .OrderBy(u => u.DistanceMeters)
                .ThenBy(u => u.Name)
                .ToList();
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Đọc cờ incident_equipment_suggestion_ignore_zone trong SysOpConfig (nhóm operation_config).
        ///              Chỉ "1"/"true" mới bật; thiếu cấu hình hoặc lỗi đọc -> false để giữ nguyên luồng cũ.
        /// Created date: 11/11/2026
        /// </summary>
        private async Task<bool> IsIgnoreZoneEnabledAsync(IMessageBus messageBus)
        {
            try
            {
                var configs = await messageBus.InvokeAsync<List<SysOpConfigOutput>>(
                    new SysOpConfigInput { GroupCode = EquipmentSuggestionConst.OpConfig.GroupCode });
                var value = configs?.FirstOrDefault(c => string.Equals(c.Code,
                    EquipmentSuggestionConst.OpConfig.IgnoreZoneCode, StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
                return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Kiểm tra thiết bị có nằm trong phạm vi các zone thuộc TUYẾN (zList đã lọc theo Location) hay không.
        ///              Dùng để chỉ tích chọn sẵn thiết bị đúng tuyến của sự cố.
        /// Note: zList RỖNG nghĩa là vị trí sự cố không rơi vào zone nào đang Enable (hoặc chưa cấu hình zone).
        ///       Khi đó KHÔNG dùng điều kiện này để chặn (trả về true), nếu không toàn bộ gợi ý sẽ trống
        ///       và người vận hành không hiểu vì sao. Điều kiện khoảng cách + hướng + Before/After vẫn áp.
        /// </summary>
        private bool IsEquipmentInRouteZones(int equipPosition, List<ZoneOutput> zList)
        {
            if ((zList?.Count() ?? 0) == 0)
                return true;

            return zList.Any(z =>
            {
                var a = (z.FromKmNumber ?? 0) * 1000 + (z.FromMetNumber ?? 0);
                var b = (z.ToKmNumber ?? 0) * 1000 + (z.ToMetNumber ?? 0);
                var lo = Math.Min(a, b);
                var hi = Math.Max(a, b);
                return lo <= equipPosition && equipPosition <= hi;
            });
        }

        /// <summary>
        /// Author: ChuongLP
        /// Lấy DANH SÁCH ProcessType (Before/After) theo loại sự kiện + loại thiết bị từ TmsEquipmentEventRule.
        /// 1 cặp (EventType + EquipmentType) có thể cấu hình ĐỒNG THỜI cả Before lẫn After (2 bản ghi)
        /// -> phải lấy TẤT CẢ rule, không chỉ 1 (GetFirst sẽ bỏ sót 1 hướng).
        /// Không có cấu hình -> mặc định [Before].
        /// </summary>
        private async Task<List<string>> ResolveProcessTypes(string eventTypeId, string equipmentTypeId)
        {
            if (string.IsNullOrWhiteSpace(eventTypeId) || string.IsNullOrWhiteSpace(equipmentTypeId))
                return new List<string> { EquipmentEventRuleConst.ProcessType.Before };

            // Lấy tất cả rule đang bật của cặp này (có thể gồm cả Before và After)
            var rules = await this._eventRuleRep.AsQueryable()
                .Where(r => r.EventTypeId == eventTypeId
                    && r.EquipmentTypeId == equipmentTypeId
                    && r.Status == StatusEnum.Enable
                    && r.IsDelete == null)
                .Select(r => r.ProcessType)
                .ToListAsync();

            // Chuẩn hóa + loại rỗng/trùng
            var processTypes = rules
                .Where(pt => !string.IsNullOrWhiteSpace(pt))
                .Select(pt => pt.Trim())
                .Distinct()
                .ToList();

            // Chưa cấu hình -> mặc định Before
            return processTypes.Count > 0
                ? processTypes
                : new List<string> { EquipmentEventRuleConst.ProcessType.Before };
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Xác định CHIỀU DI CHUYỂN của phương tiện tại vị trí sự cố.
        ///              Trả về +1 nếu phương tiện đi theo chiều TĂNG lý trình, -1 nếu theo chiều GIẢM.
        /// Date: 19-08-2026
        /// </summary>
        private async Task<int> ResolveTravelSignAsync(List<ZoneOutput> zones, string? directionId, string? location)
        {
            var signs = zones.Where(z => z.LaneId == directionId
                    && (string.IsNullOrWhiteSpace(location) || z.Location == location))
                .Select(z => Math.Sign(((z.ToKmNumber ?? 0) * 1000 + (z.ToMetNumber ?? 0))
                    - ((z.FromKmNumber ?? 0) * 1000 + (z.FromMetNumber ?? 0))))
                .Where(sign => sign != 0).Distinct().ToList();
            if (signs.Count == 1) return signs[0];
            if (signs.Count > 1 && string.IsNullOrWhiteSpace(location)) return 0;
            var rows = await _vdsConfigRep.AsQueryable().Where(x => x.IsDelete == null && x.Status == StatusEnum.Enable
                && x.GroupCode == VdsConfigConst.Group.Segment && x.Code.StartsWith(VdsConfigConst.Pattern.KmDirPrefix)).ToListAsync();
            var errors = new List<VdsSegmentTopologyMessageOutput>();
            var orientation = VdsBaseHelper.RouteOrientationHp.Create(rows.Select(r => new VdsBaseHelper.SegmentTopologyHp.TopoConfigRow(r.GroupCode, r.Code, r.Value)), errors);
            if (errors.Count > 0) throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);
            return orientation(location, int.TryParse(directionId, out var dir) ? dir : 0);
        }

        private static string CoverageKey(string? equipmentId, string? zoneId)
            => equipmentId + "|" + zoneId;

        private async Task<HashSet<string>> LoadPtzCoverageAsync(List<ZoneOutput> zones)
        {
            var ids = zones.Select(z => z.ID).Distinct().ToList();
            if (ids.Count == 0) return new(StringComparer.OrdinalIgnoreCase);
            var rows = await _zoneEquipRep.AsQueryable()
                .InnerJoin<TmsEquipment>((ze, e) => ze.EquipmentId == e.ID)
                .InnerJoin<TmsEquipmentType>((ze, e, t) => e.EquipmentTypeId == t.ID)
                .InnerJoin<TmsZone>((ze, e, t, z) => ze.ZoneId == z.ID)
                .Where((ze, e, t, z) => ids.Contains(ze.ZoneId) && ze.IsDelete == null && e.IsDelete == null
                    && t.IsDelete == null && z.IsDelete == null && t.Code == "PTZ"
                    && (ze.Status == null || ze.Status == StatusEnum.Enable)
                    && e.Status == StatusEnum.Enable && z.Status == StatusEnum.Enable
                    && ze.TenantId == e.TenantId && e.TenantId == z.TenantId && e.Location == z.Location)
                .Select((ze, e, t, z) => new { ze.ZoneId, ze.EquipmentId }).ToListAsync();
            return rows.Select(r => CoverageKey(r.EquipmentId, r.ZoneId)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Kiểm tra thiết bị nằm đúng phía TRƯỚC (Before) hoặc SAU (After) vị trí sự cố
        ///              theo chiều di chuyển của phương tiện.
        /// Date: 19-08-2026
        /// </summary>
        private bool IsPositionValidByProcessType(string processType, int equipPosition, int incidentPosition, int travelSign, bool isAllDirectionIncident)
        {
            if (isAllDirectionIncident)
                return true;
            if (travelSign == 0) return false;

            var delta = (equipPosition - incidentPosition) * travelSign;

            return processType switch
            {
                EquipmentEventRuleConst.ProcessType.After => delta >= 0,
                _ => delta <= 0, // Before là mặc định
            };
        }

        /// <summary>
        /// Description: Lấy danh sách thiết bị theo danh sách ZoneId thông qua bảng TmsZoneEquipment
        /// [FIX] Ép ID/Code lấy từ TmsEquipment (e). Trước đây Select(..., true) auto-fill khiến
        ///       ID bị map nhầm từ bảng chính TmsZoneEquipment (ze.ID) => FE lưu nhầm id zoneEquipment
        ///       vào incident.equipmentId, dẫn tới không tra ngược ra được thiết bị/camera.
        /// </summary>
        public async Task<List<TmsEquipmentDataOutput>> HandleAsync(TmsEquipmentByZoneListInput command)
        {
            if (command.ZoneIds == null || command.ZoneIds.Count == 0)
                return new List<TmsEquipmentDataOutput>();

            var list = await _zoneEquipRep.AsQueryable()
                .InnerJoin<TmsEquipment>((ze, e) => ze.EquipmentId == e.ID)
                .LeftJoin<TmsZone>((ze, e, z) => ze.ZoneId == z.ID)
                .Where((ze, e, z) => command.ZoneIds.Contains(ze.ZoneId))
                .Where(IncidentPositionHelper.MappedEquipment)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), (ze, e, z) => e.EquipmentTypeId == command.EquipmentTypeId)
                .OrderBy((ze, e, z) => e.KmNumber)
                .OrderBy((ze, e, z) => e.MetNumber)
                .Select((ze, e, z) => new TmsEquipmentDataOutput()
                {
                    // QUAN TRỌNG: lấy ID/Code từ thiết bị (e), tránh auto-fill lấy nhầm từ ze (TmsZoneEquipment)
                    ID = e.ID,
                    Code = e.Code,
                    ZoneId = ze.ZoneId,
                    ZoneName = z.Name
                }, true)
                .ToListAsync();

            return list.DistinctBy(e => e.ID).ToList();
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xác định hướng hoạt động và vị trí phù hợp của thiết bị
        /// Note: zoneDirId (LaneId), equipDirId, equipActiveDirId chưa thống nhất kiểu dữ liệu
        /// Date: 19-08-2026
        /// </summary>
        private bool CheckOperationDirectionEquipment(object? targetDir, object? equipmentDir, object? equipActiveDir,
            ZoneOutput? zone, bool mappedPtz = false)
            => EquipmentDirectionHelper.CanServe(targetDir?.ToString(), equipmentDir?.ToString(), zone?.LaneId, mappedPtz);
    }
}
