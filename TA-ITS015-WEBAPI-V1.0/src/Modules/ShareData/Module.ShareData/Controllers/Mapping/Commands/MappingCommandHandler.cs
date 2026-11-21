using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataActivityService;
using Module.ShareData.Infrastructure.Services.ShareDataMappingReader;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.ShareData.Controllers.Mapping.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa, bật tắt và sao chép Phễu lọc (ShareDataMapping)
    /// Created date: 2026-08-21
    /// </summary>
    public class MappingCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataMapping> _mappingRep;
        private readonly BaseRepository<ShareDataPartner> _partnerRep;
        private readonly BaseRepository<ShareDataCodeSet> _codeSetRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseCacheService _cache;
        private readonly ShareDataActivityLogger _activityLogger;

        /// <summary>Giá trị hệ thống bắt buộc của hồ sơ có chiều gửi — khớp BUILTIN_FIELDS bên FE.</summary>
        private static readonly string[] RequiredMetaKeys = ["Now", "Serial", "PacketCode", "PartnerCode"];

        public MappingCommandHandler(
            BaseRepository<ShareDataMapping> mappingRep,
            BaseRepository<ShareDataPartner> partnerRep,
            BaseRepository<ShareDataCodeSet> codeSetRep,
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseCacheService cache,
            ShareDataActivityLogger activityLogger)
        {
            _mappingRep = mappingRep;
            _partnerRep = partnerRep;
            _codeSetRep = codeSetRep;
            _subscriptionRep = subscriptionRep;
            _cache = cache;
            _activityLogger = activityLogger;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 phễu lọc. 
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataMappingOutput> HandleAsync(ShareDataAddMappingInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.PacketVersion = command.PacketVersion?.Trim();
            var partnerCheck = await _partnerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.PartnerId)
                ?? throw Oops.Oh(BaseMsg.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PartnerId]);

            if (partnerCheck.Status == BaseEnums.StatusEnum.Disable)
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerDisabled);

            EnsureMetaFields(command.Direction, command.TargetShapeJson);

            var (fieldKeys, codeSets) = ShapeReader.ReadShape(command.TargetShapeJson);
            if (fieldKeys.Count > 0 || codeSets.Count > 0)
            {
                foreach (var fieldKey in fieldKeys)
                {
                    if (string.IsNullOrWhiteSpace(fieldKey))
                        throw Oops.Oh(BaseMsg.ShareData.Validation.MappingFieldKeyRequired);
                }

                var wantedCodeSets = codeSets.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (wantedCodeSets.Count > 0)
                {
                    var existingCodeSets = await _codeSetRep.AsQueryable()
                        .Where(u => u.IsDelete == null && u.Code != null && wantedCodeSets.Contains(u.Code))
                        .Select(u => u.Code)
                        .ToListAsync();

                    var missingCodeSet = wantedCodeSets.FirstOrDefault(c => !existingCodeSets.Contains(c, StringComparer.OrdinalIgnoreCase));
                    if (missingCodeSet != null)
                        throw Oops.Oh(BaseMsg.ShareData.Exception.MappingCodeSetNotExist, missingCodeSet);
                }
            }

            if (!string.IsNullOrWhiteSpace(command.Code)
                && await _mappingRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            var entity = command.Adapt<ShareDataMapping>();
            entity.CreateUId = null;

            var db = _mappingRep.Context;
            await db.AsTenant().BeginTranAsync();
            try
            {
                await EnsureNoActiveConflictAsync(entity, null);
                await _mappingRep.InsertWithDiffLogAsync(entity);
                await db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataMapping);

            var output = entity.Adapt<ShareDataMappingOutput>();
            var partner = await _partnerRep.GetFirstAsync(u => u.ID == entity.PartnerId);
            output.PartnerName = partner?.Name;
            output.PartnerCode = partner?.Code;
            output.MappedFieldCount = ShapeReader.CountMappedFields(entity.TargetShapeJson);

            await _activityLogger.LogCreateAsync(entity);

            return output;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 phễu lọc
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataMappingOutput> HandleAsync(ShareDataUpdateMappingInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.PacketVersion = command.PacketVersion?.Trim();
            var entity = await _mappingRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await HasActiveSubscriptionAsync(entity))
                throw Oops.Oh(BaseMsg.ShareData.Exception.MappingInUse);

            // Giữ nguyên bộ ba định danh, chỉ nhận phần cấu hình quy đổi.
            command.PartnerId = entity.PartnerId;
            command.DatatypeId = entity.DatatypeId;
            command.Direction = entity.Direction;

            var partnerCheck = await _partnerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.PartnerId)
                ?? throw Oops.Oh(BaseMsg.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.ShareData.Entity.PartnerId]);

            if (partnerCheck.Status == BaseEnums.StatusEnum.Disable)
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerDisabled);

            EnsureMetaFields(command.Direction, command.TargetShapeJson);

            var (fieldKeys, codeSets) = ShapeReader.ReadShape(command.TargetShapeJson);
            if (fieldKeys.Count > 0 || codeSets.Count > 0)
            {
                foreach (var fieldKey in fieldKeys)
                {
                    if (string.IsNullOrWhiteSpace(fieldKey))
                        throw Oops.Oh(BaseMsg.ShareData.Validation.MappingFieldKeyRequired);
                }

                var wantedCodeSets = codeSets.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (wantedCodeSets.Count > 0)
                {
                    var existingCodeSets = await _codeSetRep.AsQueryable()
                        .Where(u => u.IsDelete == null && u.Code != null && wantedCodeSets.Contains(u.Code))
                        .Select(u => u.Code)
                        .ToListAsync();

                    var missingCodeSet = wantedCodeSets.FirstOrDefault(c => !existingCodeSets.Contains(c, StringComparer.OrdinalIgnoreCase));
                    if (missingCodeSet != null)
                        throw Oops.Oh(BaseMsg.ShareData.Exception.MappingCodeSetNotExist, missingCodeSet);
                }
            }

            if (!string.IsNullOrWhiteSpace(command.Code)
                && await _mappingRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            var before = entity.Adapt<ShareDataMapping>();

            entity.Code = command.Code;
            entity.Name = command.Name;
            entity.PacketVersion = command.PacketVersion;
            entity.PartnerSchemaJson = command.PartnerSchemaJson;
            entity.TargetShapeJson = command.TargetShapeJson;
            entity.IsActive = command.IsActive;
            entity.Remark = command.Remark;

            var db = _mappingRep.Context;
            await db.AsTenant().BeginTranAsync();
            try
            {
                await EnsureNoActiveConflictAsync(entity, entity.ID);
                await _mappingRep.UpdateWithDiffLogAsync(entity);
                await db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataMapping);

            var output = entity.Adapt<ShareDataMappingOutput>();
            var partner = await _partnerRep.GetFirstAsync(u => u.ID == entity.PartnerId);
            output.PartnerName = partner?.Name;
            output.PartnerCode = partner?.Code;
            output.MappedFieldCount = ShapeReader.CountMappedFields(entity.TargetShapeJson);

            await _activityLogger.LogUpdateAsync(before, entity);

            return output;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 phễu lọc (xóa mềm)
        /// Created date: 2026-08-21
        /// </summary>
        public async Task HandleAsync(ShareDataDeleteMappingInput command)
        {
            var entity = await _mappingRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await HasActiveSubscriptionAsync(entity))
                throw Oops.Oh(BaseMsg.ShareData.Exception.MappingInUse);

            var before = entity.Adapt<ShareDataMapping>();

            await _mappingRep.SoftDeleteAsync<ShareDataMapping>(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataMapping);

            await _activityLogger.LogDeleteAsync(before);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều phễu lọc
        /// Created date: 2026-08-21
        /// </summary>
        public async Task HandleAsync(List<ShareDataDeleteMappingInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _mappingRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            foreach (var item in entities)
                if (await HasActiveSubscriptionAsync(item))
                    throw Oops.Oh(BaseMsg.ShareData.Exception.MappingInUse);

            await _mappingRep.SoftDeleteAsync<ShareDataMapping>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataMapping);

            foreach (var item in entities)
            {
                await _activityLogger.LogDeleteAsync(item);
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Bật / tắt 1 phễu lọc.
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataMappingOutput> HandleAsync(ShareDataSetActiveMappingInput command)
        {
            var entity = await _mappingRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await HasActiveSubscriptionAsync(entity))
                throw Oops.Oh(BaseMsg.ShareData.Exception.MappingInUse);

            var before = entity.Adapt<ShareDataMapping>();
            var db = _mappingRep.Context;

            await db.AsTenant().BeginTranAsync();
            try
            {
                entity.IsActive = command.IsActive;
                await EnsureNoActiveConflictAsync(entity, entity.ID);
                await _mappingRep.UpdateWithDiffLogAsync(entity);

                await db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataMapping);

            var output = entity.Adapt<ShareDataMappingOutput>();
            var partner = await _partnerRep.GetFirstAsync(u => u.ID == entity.PartnerId);
            output.PartnerName = partner?.Name;
            output.PartnerCode = partner?.Code;
            //output.PacketName = await _packetRep.AsQueryable()
                //.Where(u => u.Code == entity.DatatypeId).Select(u => u.Name).FirstAsync();
            output.MappedFieldCount = ShapeReader.CountMappedFields(entity.TargetShapeJson);

            await _activityLogger.LogUpdateAsync(before, entity);

            return output;
        }

        #region privatefunction
        /// <summary>
        /// Author: HieuNV
        /// Description: Hồ sơ có gửi đi (Outbound hoặc Both) phải khai đủ 4 giá trị hệ thống
        ///              Now, Serial, PacketCode, PartnerCode bằng ký hiệu $meta trong bộ khung.
        ///              Hồ sơ chỉ nhận thì không bắt buộc.
        /// Created date: 2026-09-18
        /// </summary>
        private static void EnsureMetaFields(BaseEnums.Direction? direction, string? targetShapeJson)
        {
            if (direction == BaseEnums.Direction.Inbound) return;

            var declared = ShapeReader.ReadMetaKeys(targetShapeJson);
            var missing = RequiredMetaKeys
                .Where(k => !declared.Contains(k, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (missing.Count > 0)
                throw Oops.Oh(BaseMsg.ShareData.Exception.MappingMetaFieldsRequired, string.Join(", ", missing));
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Mỗi (đối tác × gói tin × chiều) chỉ được 1 phễu lọc bật.
        /// Created date: 2026-08-21
        /// </summary>
        private async Task EnsureNoActiveConflictAsync(ShareDataMapping candidate, string? excludeId)
        {
            if (candidate.IsActive != true) return;

            var query = _mappingRep.AsQueryable()
                .Where(u => u.IsDelete == null
                    && u.IsActive == true
                    && u.PartnerId == candidate.PartnerId
                    && u.DatatypeId == candidate.DatatypeId);

            if (candidate.Direction != BaseEnums.Direction.Both)
                query = query.Where(u => u.Direction == candidate.Direction || u.Direction == BaseEnums.Direction.Both);

            if (!string.IsNullOrWhiteSpace(excludeId))
                query = query.Where(u => u.ID != excludeId);

            if (await query.AnyAsync())
                throw Oops.Oh(BaseMsg.ShareData.Exception.MappingConflictInUse);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Hồ sơ này có đang được đăng ký nào còn sống dùng hay không.
        ///
        ///              KHÔNG so theo Subscription.MappingId nữa: cột đó đã bỏ khỏi đăng ký.
        ///              Suy sống đúng luật worker: cùng đối tác, cùng gói tin, chiều của đăng ký trùng
        ///              chiều hồ sơ hoặc hồ sơ khai Both. Hồ sơ đang tắt thì worker không dùng tới nên
        ///              sửa thoải mái — chỉ chặn khi hồ sơ đang bật.
        /// Created date: 17/09/2026
        /// </summary>
        private async Task<bool> HasActiveSubscriptionAsync(ShareDataMapping mapping)
        {
            if (mapping.IsActive != true) return false;
            if (string.IsNullOrWhiteSpace(mapping.PartnerId) || string.IsNullOrWhiteSpace(mapping.DatatypeId))
                return false;

            return await _subscriptionRep.IsAnyAsync(u => u.IsDelete == null
                && u.PartnerId == mapping.PartnerId
                && u.DatatypeId == mapping.DatatypeId
                && (mapping.Direction == BaseEnums.Direction.Both || u.Direction == mapping.Direction)
                && ShareDataConst.SubStateGroup.Alive.Contains(u.State));
        }
        #endregion
    }
}
