using Modules.TMS.Core.Dto.EquipmentEventRule;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.EquipmentEventRule.Commands
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Lớp nghiệp vụ xử lý lệnh ghi cho cấu hình hiển thị VMS/LCS theo loại sự kiện (TmsEquipmentEventRule)
    /// Created date: 16/07/2026
    /// </summary>
    public class EquipmentEventRuleCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsEquipmentEventRule> _ruleRep;
        private BaseCacheService _cache;

        public EquipmentEventRuleCommandHandler(BaseRepository<TmsEquipmentEventRule> ruleRep, BaseCacheService cache)
        {
            _ruleRep = ruleRep;
            _cache = cache;
        }

        /// <summary>
        /// Thêm mới rule. Chống trùng theo (EventTypeId + EquipmentTypeId + ProcessType).
        /// Author: ChuongLP
        /// Date: 2027-07-16
        /// </summary>
        public async Task HandleAsync(AddEquipmentEventRuleInput command)
        {
            command.ProcessType = command.ProcessType?.Trim();

            if (await IsDuplicateAsync(command.EventTypeId, command.EquipmentTypeId, command.ProcessType, null))
                throw Oops.Oh(BaseMsg.TmsEquipmentEventRule.Exception.Duplicate);

            command.CreateUId = null;
            await _ruleRep.InsertWithDiffLogAsync(command.Adapt<TmsEquipmentEventRule>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipmentEventRule);
        }

        /// <summary>
        /// Cập nhật rule. Chống trùng theo (EventTypeId + EquipmentTypeId + ProcessType), loại trừ chính nó.
        /// Author: ChuongLP
        /// Date: 2027-07-16
        /// </summary>
        public async Task HandleAsync(UpdateEquipmentEventRuleInput command)
        {
            command.ProcessType = command.ProcessType?.Trim();

            var entity = await _ruleRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await IsDuplicateAsync(command.EventTypeId, command.EquipmentTypeId, command.ProcessType, command.ID))
                throw Oops.Oh(BaseMsg.TmsEquipmentEventRule.Exception.Duplicate);

            // Chỉ cập nhật các trường nghiệp vụ, GIỮ NGUYÊN audit + soft-delete (IsDelete) của bản ghi DB.
            // Không Adapt trọn command vì payload FE mang sẵn IsDelete/CreateTime -> dễ ghi sai (vd IsDelete = now).
            entity.EventTypeId = command.EventTypeId;
            entity.EquipmentTypeId = command.EquipmentTypeId;
            entity.ProcessType = command.ProcessType;
            // MaxTargetCount là chính sách giới hạn số thiết bị của luồng tự động; trước đây bị bỏ sót
            // ở đây nên sửa rule là mất giá trị đã cấu hình.
            entity.MaxTargetCount = command.MaxTargetCount;
            entity.Status = command.Status;
            entity.Remark = command.Remark;

            await _ruleRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipmentEventRule);
        }

        /// <summary>
        /// Xóa 1 rule.
        /// Author: ChuongLP
        /// Date: 2027-07-16
        /// </summary>
        public async Task HandleAsync(DeleteEquipmentEventRuleInput command)
        {
            var entity = await _ruleRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Xóa CỨNG (hard delete): bảng mapping cấu hình, không lưu vết soft-delete.
            // Tránh bản ghi soft-delete còn sót gây hiển thị nhầm ở view + chặn tạo lại rule trùng.
            await _ruleRep.DeleteAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipmentEventRule);
        }

        /// <summary>
        /// Xóa nhiều rule (xóa cứng).
        /// Author: ChuongLP
        /// Date: 2027-07-16
        /// </summary>
        public async Task HandleAsync(List<DeleteEquipmentEventRuleInput> command)
        {
            var ids = command.Select(u => u.ID).Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            if (ids.Count == 0) return;

            // Xóa CỨNG theo danh sách ID
            await _ruleRep.DeleteAsync(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipmentEventRule);
        }

        /// <summary>
        /// Kiểm tra trùng rule theo tổ hợp (EventTypeId + EquipmentTypeId + ProcessType).
        /// excludeId != null: bỏ qua chính bản ghi đang cập nhật.
        /// Author: ChuongLP
        /// Date: 2027-07-16
        /// </summary>
        private async Task<bool> IsDuplicateAsync(string? eventTypeId, string? equipmentTypeId, string? processType, string? excludeId)
        {
            return await _ruleRep.IsAnyAsync(u =>
                u.EventTypeId == eventTypeId
                && u.EquipmentTypeId == equipmentTypeId
                && u.ProcessType == processType
                && (excludeId == null || u.ID != excludeId));
        }
    }
}
