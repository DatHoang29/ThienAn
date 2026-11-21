using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Dto.Controller;
using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.CfgSystem.Core.Refresh;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.TmsEquipments;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.Equipment.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh CRUD cơ bản (Add/Update/Delete) của đối tượng tmsEquipment.
    /// Các nghiệp vụ liên quan đến lịch sử trạng thái/broadcast NATS được tách ra TmsEquipmentHistoryService.
    /// Các nghiệp vụ bật/ẩn hiển thị hàng loạt được tách ra EquipmentVisibilityCommandHandler.
    /// Created date: 09/08/2024
    /// </summary>
    public class EquipmentCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsEquipment> _tmsEquipRep;
        private BaseCacheService _cache;
        private TmsEquipmentHistoryService _historyService;
        private RefreshModifyService _refreshModify;
        private IMessageBus _messageBus;
        private ILogger<EquipmentCommandHandler> _logger;

        public EquipmentCommandHandler(BaseRepository<TmsEquipment> tmsEquipRep, BaseCacheService cache,
            TmsEquipmentHistoryService historyService, RefreshModifyService refreshModify,
            IMessageBus messageBus, ILogger<EquipmentCommandHandler> logger)
        {
            _tmsEquipRep = tmsEquipRep;
            _cache = cache;
            _historyService = historyService;
            _refreshModify = refreshModify;
            _messageBus = messageBus;
            _logger = logger;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsEquipment
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddEquipmentInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã thiết bị trước khi kiểm tra và lưu vào cơ sở dữ liệu
            if (await _tmsEquipRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            // BỔ SUNG: Ràng buộc nếu FE gửi null thì fallback về Enable (1) hoặc throw lỗi - // ChuongLP - 12/10/2026
            command.Status ??= BaseEnums.StatusEnum.Enable;
            command.CreateUId = null;
            command.EquipmentVisible = (int)BaseEnums.TmsEquipmentVisibleStateEnum.Invisible;
            await _tmsEquipRep.InsertWithDiffLogAsync(command.Adapt<TmsEquipment>());

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:equipment' trong SysConfigData quyet dinh.
            await NotifyAsync(command.ID, "add");
            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.DeviceStatus, new { PackageType = "EquipmentStatus", Result = new[] { command } });
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng tmsEquipment
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateEquipmentInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã thiết bị trước khi kiểm tra và lưu vào cơ sở dữ liệu  

            if (await _tmsEquipRep.IsAnyAsync(u => u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            var sysPos = await _tmsEquipRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Lưu trạng thái cũ trước khi Adapt, vì Adapt(sysPos) có thể thay đổi entity hiện tại
            var oldEquipmentState = TmsEquipmentHistoryService.ToNullableInt(sysPos.EquipmentState);
            var oldCode = sysPos.Code;

            var updateEntity = command.Adapt(sysPos);

            // Lấy trạng thái mới sau khi map dữ liệu từ command vào entity
            var newEquipmentState = TmsEquipmentHistoryService.ToNullableInt(updateEntity.EquipmentState);
            var newCode = updateEntity.Code ?? command.Code ?? oldCode;

            await _tmsEquipRep.UpdateWithDiffLogAsync(updateEntity, false);

            // Xử lý thêm lịch sử trạng thái thiết bị sau khi update thiết bị
            await _historyService.HandleEquipmentHistoryAfterUpdateAsync(
                equipmentId: updateEntity.ID,
                code: newCode,
                oldState: oldEquipmentState,
                newState: newEquipmentState
            );

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:equipment' trong SysConfigData quyet dinh.
            await NotifyAsync(updateEntity.ID, "update");

            // Gửi message NATS về trạng thái thiết bị sau khi cập nhật
            await _historyService.PublishMessageNatsAsync(updateEntity, oldEquipmentState, newEquipmentState);

            // HieuNV - 23/10/2026 - Bộ điều khiển Video Wall giữ bản sao IP / tài khoản / mật khẩu của thiết bị ⇒ đồng bộ lại.
            // Lỗi đồng bộ không làm hỏng thao tác cập nhật thiết bị, chỉ ghi log (lưu lại bộ điều khiển để đồng bộ tay).
            try
            {
                await _messageBus.InvokeAsync(new VwSyncControllerFromEquipmentInput { EquipmentId = updateEntity.ID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đồng bộ thông tin kết nối thiết bị {EquipmentId} sang bộ điều khiển Video Wall thất bại.", updateEntity.ID);
            }
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsEquipment
        /// Created date: 09/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteEquipmentInput command)
        {
            var entity = await _tmsEquipRep.GetFirstAsync(u => u.ID == command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // HieuNV - 23/10/2026 - Chặn xoá thiết bị đang gắn với bộ điều khiển Video Wall (xoá bộ điều khiển trước)
            await _messageBus.InvokeAsync(new VwEnsureEquipmentNotUsedInput { EquipmentIds = new List<string> { entity.ID } });

            await _tmsEquipRep.SoftDeleteAsync<TmsEquipment>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:equipment' trong SysConfigData quyet dinh.
            await NotifyAsync(entity.ID, "delete");
            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.DeviceStatus, command);


        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsEquipment
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteEquipmentInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsEquipRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // HieuNV - 23/10/2026 - Chặn xoá thiết bị đang gắn với bộ điều khiển Video Wall (xoá bộ điều khiển trước)
            await _messageBus.InvokeAsync(new VwEnsureEquipmentNotUsedInput { EquipmentIds = ids });

            await _tmsEquipRep.SoftDeleteAsync<TmsEquipment>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:equipment' trong SysConfigData quyet dinh.
            await NotifyAsync(null, "batch-delete");
            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.DeviceStatus, command);
        }

        /// <summary>Handler chi khai bao cai gi vua doi; RefreshModifyService tra route trong DB roi bat NATS.</summary>
        private Task NotifyAsync(string? key, string action)
            => this._refreshModify.NotifyAsync(new RefreshModifyRequest("catalog", "equipment", key, action));
    }
}
