using Modules.TMS.Controllers.ZoneEquipment.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.ZoneEquipment.Queries
{
    public class ZoneEquipmentQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsZoneEquipment> _zoneEquipmentRep;
        private BaseRepository<TmsZone> _zoneRep;
        private BaseRepository<TmsEquipment> _equipmentRep;
        private BaseCacheService _cache;

        public ZoneEquipmentQueryHandler(
            BaseRepository<TmsZoneEquipment> zoneEquipmentRep,
            BaseRepository<TmsZone> zoneRep,
            BaseRepository<TmsEquipment> equipmentRep,
            BaseCacheService cache)
        {
            _zoneEquipmentRep = zoneEquipmentRep;
            _zoneRep = zoneRep;
            _equipmentRep = equipmentRep;
            _cache = cache;
        }

        /// <summary>
        /// Description: Lấy danh sách ZoneEquipment theo bộ lọc
        /// </summary>
        public async Task<List<ZoneEquipmentOutput>> HandleAsync(ZoneEquipmentInput command)
        {
            var query = _zoneEquipmentRep.AsQueryable()
                .LeftJoin<TmsZone>((ze, z) => ze.ZoneId == z.ID)
                .LeftJoin<TmsEquipment>((ze, z, e) => ze.EquipmentId == e.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), (ze, z, e) => ze.ZoneId == command.ZoneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), (ze, z, e) => ze.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Type), (ze, z, e) => ze.Type == command.Type)
                .WhereIF(command.Status != null, (ze, z, e) => ze.Status == command.Status)
                .WhereIF(!string.IsNullOrWhiteSpace(command.LaneId), (ze, z, e) => ze.LaneId == command.LaneId)
                .Where((ze, z, e) => ze.IsDelete == null)
                .OrderByDescending((ze, z, e) => ze.CreateTime)
                .Select((ze, z, e) => new ZoneEquipmentOutput
                {
                    ID = ze.ID,
                    Code = ze.Code,
                    ZoneId = ze.ZoneId,
                    EquipmentId = ze.EquipmentId,
                    Type = ze.Type,
                    Status = ze.Status,
                    LaneId = ze.LaneId,
                    Remark = ze.Remark,
                    CreateTime = ze.CreateTime,
                    ZoneName = z.Name,
                    ZoneCode = z.Code,
                    EquipmentName = e.Name,
                    EquipmentCode = e.Code,
                    EquipmentKmNumber = e.KmNumber,
                    EquipmentMetNumber = e.MetNumber,
                });

            return await query.ToListAsync();
        }

        /// <summary>
        /// Description: Lấy danh sách phân trang ZoneEquipment
        /// </summary>
        public async Task<SqlSugarPagedList<ZoneEquipmentOutput>> HandleAsync(PageZoneEquipmentInput command)
        {
            var query = _zoneEquipmentRep.AsQueryable()
                .LeftJoin<TmsZone>((ze, z) => ze.ZoneId == z.ID)
                .LeftJoin<TmsEquipment>((ze, z, e) => ze.EquipmentId == e.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), (ze, z, e) => ze.ZoneId == command.ZoneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), (ze, z, e) => ze.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Type), (ze, z, e) => ze.Type == command.Type)
                .WhereIF(!string.IsNullOrWhiteSpace(command.LaneId), (ze, z, e) => ze.LaneId == command.LaneId)
                .Where((ze, z, e) => ze.IsDelete == null)
                .OrderByDescending((ze, z, e) => ze.CreateTime)
                .Select((ze, z, e) => new ZoneEquipmentOutput
                {
                    ID = ze.ID,
                    Code = ze.Code,
                    ZoneId = ze.ZoneId,
                    EquipmentId = ze.EquipmentId,
                    Type = ze.Type,
                    Status = ze.Status,
                    LaneId = ze.LaneId,
                    Remark = ze.Remark,
                    CreateTime = ze.CreateTime,
                    ZoneName = z.Name,
                    ZoneCode = z.Code,
                    EquipmentName = e.Name,
                    EquipmentCode = e.Code,
                    EquipmentKmNumber = e.KmNumber,
                    EquipmentMetNumber = e.MetNumber,
                });

            return await query.ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Description: Auto-map equipment cho một zone dựa trên km+met và LaneId
        /// Trả về danh sách preview, KHÔNG lưu DB
        /// </summary>
        public async Task<List<AutoMapPreviewOutput>> HandleAsync(AutoMapZoneEquipmentInput command)
        {
            var zone = await _zoneRep.GetFirstAsync(z => z.ID == command.ZoneId && z.IsDelete == null)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            return await GenerateAutoMapPreview(zone);
        }

        /// <summary>
        /// Description: Auto-map equipment cho tất cả zones
        /// Trả về danh sách preview, KHÔNG lưu DB
        /// </summary>
        public async Task<List<AutoMapPreviewOutput>> HandleAsync(AutoMapAllZoneEquipmentInput command)
        {
            var zones = await _zoneRep.AsQueryable()
                .Where(z => z.IsDelete == null && z.Status == BaseEnums.StatusEnum.Enable)
                .ToListAsync();

            var result = new List<AutoMapPreviewOutput>();
            foreach (var zone in zones)
            {
                var preview = await GenerateAutoMapPreview(zone);
                result.AddRange(preview);
            }
            return result;
        }

        /// <summary>
        /// Description: Sinh danh sách preview auto-map cho một zone
        /// Logic: So sánh vị trí km+met của equipment với ranh giới zone và khớp hướng
        /// (Zone.LaneId với Equipment.DirectionId). Thiết bị có DirectionId = 0 được áp dụng cho cả hai hướng.
        /// </summary>
        private async Task<List<AutoMapPreviewOutput>> GenerateAutoMapPreview(TmsZone zone)
        {
            var result = new List<AutoMapPreviewOutput>();

            var zoneFromPos = (zone.FromKmNumber ?? 0) * 1000 + (zone.FromMetNumber ?? 0);
            var zoneToPos = (zone.ToKmNumber ?? 0) * 1000 + (zone.ToMetNumber ?? 0);

            if (zoneFromPos == 0 && zoneToPos == 0) return result;

            bool isGreater = zoneToPos >= zoneFromPos;

            var allEquipment = await _equipmentRep.AsQueryable()
                .Where(e => e.IsDelete == null && e.Status == BaseEnums.StatusEnum.Enable)
                .ToListAsync();

            var existingMappedIds = await _zoneEquipmentRep.AsQueryable()
                .Where(ze => ze.ZoneId == zone.ID && ze.IsDelete == null
                    && (ze.Status == null || ze.Status == BaseEnums.StatusEnum.Enable))
                .Select(ze => ze.EquipmentId)
                .ToListAsync();

            foreach (var equip in allEquipment)
            {
                if (existingMappedIds.Contains(equip.ID)) continue;

                var equipPos = (equip.KmNumber ?? 0) * 1000 + (equip.MetNumber ?? 0);

                bool positionMatch;
                if (isGreater)
                    positionMatch = equipPos >= zoneFromPos && equipPos < zoneToPos;
                else
                    positionMatch = equipPos <= zoneFromPos && equipPos > zoneToPos;

                if (!positionMatch) continue;

                // Zone và equipment phải cùng mã Location để được gợi ý.
                var isLocationMatch = !string.IsNullOrWhiteSpace(equip.Location)
                    && !string.IsNullOrWhiteSpace(zone.Location)
                    && string.Equals(equip.Location, zone.Location, StringComparison.OrdinalIgnoreCase);

                if (!isLocationMatch) continue;

                // Zone.LaneId lưu mã hướng, còn Equipment.LaneId là làn vật lý.
                // Thiết bị hướng 0 dùng chung cho cả hai hướng.
                var isBothDirectionEquipment = equip.DirectionId == 0;
                var isZoneDirectionUnspecified = string.IsNullOrWhiteSpace(zone.LaneId);
                // PTZ khác hướng chỉ được dùng khi người vận hành đã khai liên kết zone;
                // auto-map không tự mở phạm vi mọi PTZ sang hướng đối diện.
                var isDirectionMatch = Modules.TMS.Core.Helper.EquipmentDirectionHelper.CanServe(
                    zone.LaneId, equip.DirectionId?.ToString(), zone.LaneId, mappedPtz: false);

                if (!isBothDirectionEquipment && !isZoneDirectionUnspecified && !isDirectionMatch) continue;

                var reasons = new List<string> { "Vị trí km+met", "Location trùng khớp" };
                if (isBothDirectionEquipment)
                    reasons.Add("Thiết bị dùng chung cả hai hướng");
                else if (isDirectionMatch)
                    reasons.Add("Hướng trùng khớp");

                result.Add(new AutoMapPreviewOutput
                {
                    ZoneId = zone.ID,
                    ZoneName = zone.Name,
                    ZoneCode = zone.Code,
                    EquipmentId = equip.ID,
                    EquipmentName = equip.Name,
                    EquipmentCode = equip.Code,
                    KmNumber = equip.KmNumber,
                    MetNumber = equip.MetNumber,
                    LaneId = equip.LaneId,
                    MatchReason = string.Join(" + ", reasons)
                });
            }

            return result;
        }
    }
}
