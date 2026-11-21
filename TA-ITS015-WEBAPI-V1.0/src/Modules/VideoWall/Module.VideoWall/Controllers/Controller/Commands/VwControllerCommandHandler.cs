using Mapster;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Dto.Controller;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Layout;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;
using Module.VideoWall.Core.Exceptions;
using Modules.TMS.Core.Entities;

namespace Module.VideoWall.Controllers.Controller.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwControllerCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseRepository<TmsEquipment> _equipmentRep;
        private readonly BaseCacheService _cache;
        private readonly IVwPublisher _publisher;
        private readonly VwWallLayoutService _layout;

        public VwControllerCommandHandler(
            BaseRepository<VwController> vwControllerRep,
            BaseRepository<TmsEquipment> equipmentRep,
            BaseCacheService cache,
            IVwPublisher publisher,
            VwWallLayoutService layout)
        {
            _vwControllerRep = vwControllerRep;
            _equipmentRep = equipmentRep;
            _cache = cache;
            _publisher = publisher;
            _layout = layout;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 VwController
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwAddControllerInput command)
        {
            // 1. Kiểm tra trùng tên
            command.Code = command.Code?.Trim();
            if (await _vwControllerRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            command.CreateUId = null;

            // 1b. Kiểm tra bộ điều khiển cha (tường phân cấp 2 tầng)
            command.Role = command.Role?.Trim();
            command.ParentControllerId = string.IsNullOrWhiteSpace(command.ParentControllerId) ? null : command.ParentControllerId.Trim();
            await EnsureValidRoleAsync(null, command.Role, command.ParentControllerId);

            // 1c. Kiểm tra thiết bị tham chiếu (RefId = TmsEquipment.ID) chưa gắn với bộ điều khiển khác
            command.RefId = string.IsNullOrWhiteSpace(command.RefId) ? null : command.RefId.Trim();
            await EnsureUniqueRefIdAsync(null, command.RefId);

            // 1d. Thông tin kết nối chỉ lấy từ thiết bị, bỏ qua giá trị FE gửi lên
            CopyConnection(await GetDeclaredEquipmentAsync(command.RefId), command);

            // 1e. Model là thông tin định danh hệ thống đọc về từ thiết bị (biên bản 22/10 mục 1.4) ⇒ không nhận từ form
            command.Model = null;

            // 2. Ghi DB + xóa cache
            await _vwControllerRep.InsertWithDiffLogAsync(command.Adapt<VwController>());
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 VwController
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwUpdateControllerInput command)
        {
            // 1. Kiểm tra trùng tên với bản ghi khác
            if (await _vwControllerRep.IsAnyAsync(u => u.IsDelete == null && u.Name == command.Name && u.ID != command.ID))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Name]);

            // 2. Lấy bản ghi gốc
            var entity = await _vwControllerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 2b. Kiểm tra bộ điều khiển cha (tường phân cấp 2 tầng)
            command.Role = command.Role?.Trim();
            command.ParentControllerId = string.IsNullOrWhiteSpace(command.ParentControllerId) ? null : command.ParentControllerId.Trim();
            await EnsureValidRoleAsync(command.ID, command.Role, command.ParentControllerId);
            // Đang gán vào ô của bố cục đang áp dụng ⇒ không được thành bộ phân phối / tắt
            if ((command.Role == VwControllerRole.Distribution || command.Status == BaseEnums.StatusEnum.Disable)
                && await _layout.IsControllerAssignedAsync(command.ID))
                throw Oops.Oh(BaseMsg.Vw.Exception.ControllerAssignedInLayout);

            // 2c. Kiểm tra thiết bị tham chiếu (RefId = TmsEquipment.ID) chưa gắn với bộ điều khiển khác
            command.RefId = string.IsNullOrWhiteSpace(command.RefId) ? null : command.RefId.Trim();
            await EnsureUniqueRefIdAsync(command.ID, command.RefId);
            var equipment = await GetDeclaredEquipmentAsync(command.RefId);

            // 3. Map dữ liệu từ command vào bản ghi gốc (Adapt ghi thẳng lên entity và trả về chính nó).
            //    Model chỉ đọc (hệ thống đọc từ thiết bị) ⇒ giữ giá trị đang lưu, bỏ qua giá trị FE gửi lên.
            var originalModel = entity.Model;
            command.Adapt(entity);
            entity.Model = originalModel;

            // 3b. Giá trị đã chuẩn hoá ở bước 2b / 2c (chuỗi rỗng ⇒ null)
            entity.ParentControllerId = command.ParentControllerId;
            entity.RefId = command.RefId;

            // 3c. Lưu lại = đồng bộ lại thông tin kết nối từ thiết bị (đè giá trị FE gửi lên)
            CopyConnection(equipment, entity);

            // 4. Ghi DB + xóa cache + reset trạng thái lỗi xác thực qua NATS (Luồng A - Fire-and-forget)
            //    false = ghi cả cột null (giống TmsEquipment) ⇒ xoá được bộ cha khi đổi sang phân phối / độc lập
            await _vwControllerRep.UpdateWithDiffLogAsync(entity, false);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
            ResetDeviceAuthFailure(entity);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: TMS gọi qua IMessageBus sau khi cập nhật thiết bị ⇒ chép IP, Port / tài khoản / mật khẩu mới
        ///              sang các VwController liên kết. Thiết bị là nguồn dữ liệu duy nhất nên chép nguyên trạng
        ///              (kể cả khi bị xoá trống), chỉ ghi khi có thay đổi.
        /// Created date: 23/10/2026
        /// </summary>
        public async Task HandleAsync(VwSyncControllerFromEquipmentInput command)
        {
            if (string.IsNullOrWhiteSpace(command.EquipmentId))
                return;

            var controllers = await _vwControllerRep.GetListAsync(u => u.IsDelete == null && u.RefId == command.EquipmentId);
            if (controllers.Count == 0)
                return;

            var equipment = await _equipmentRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.EquipmentId);
            if (equipment == null)
                return;

            foreach (var controller in controllers)
            {
                var (ip, port, account, password) = (controller.IP, controller.Port, controller.Account, controller.PassWord);
                CopyConnection(equipment, controller);
                if (ip == controller.IP && port == controller.Port && account == controller.Account && password == controller.PassWord)
                    continue;

                // false = ghi cả cột null (thiết bị bị xoá trống thông tin)
                await _vwControllerRep.UpdateWithDiffLogAsync(controller, false);
                ResetDeviceAuthFailure(controller);
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: TMS gọi qua IMessageBus trước khi xoá thiết bị ⇒ chặn nếu thiết bị đang gắn với bộ điều khiển
        ///              (muốn xoá thiết bị thì xoá bộ điều khiển trước).
        /// Created date: 23/10/2026
        /// </summary>
        public async Task HandleAsync(VwEnsureEquipmentNotUsedInput command)
        {
            if (command.EquipmentIds.Count == 0)
                return;

            var names = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.RefId != null && command.EquipmentIds.Contains(u.RefId))
                .Select(u => u.Name)
                .ToListAsync();

            if (names.Count > 0)
                throw Oops.Oh(BaseMsg.Vw.Exception.EquipmentUsedByController, string.Join(", ", names));
        }

        /// <summary>
        /// Description: Thiết bị tham chiếu phải tồn tại và khai báo đủ IP / tài khoản / mật khẩu.
        /// Created date: 23/10/2026
        /// </summary>
        private async Task<TmsEquipment> GetDeclaredEquipmentAsync(string? refId)
        {
            var equipment = await _equipmentRep.GetFirstAsync(u => u.IsDelete == null && u.ID == refId)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            if (string.IsNullOrWhiteSpace(equipment.Ip) || string.IsNullOrWhiteSpace(equipment.Username) || string.IsNullOrWhiteSpace(equipment.Password))
                throw Oops.Oh(BaseMsg.Vw.Exception.EquipmentConnectionIncomplete, equipment.Name);

            return equipment;
        }

        /// <summary>
        /// Description: Chép thông tin kết nối thiết bị sang bộ điều khiển: IP và cổng lưu 2 cột riêng (IP / Port).
        ///              Cổng không phải số ⇒ để trống (Worker dùng cổng mặc định).
        /// Created date: 23/10/2026 — cập nhật 26/10/2026 (tách cổng sang cột Port)
        /// </summary>
        private static void CopyConnection(TmsEquipment equipment, VwController controller)
        {
            controller.IP = equipment.Ip?.Trim();
            controller.Port = int.TryParse(equipment.Port?.Trim(), out var port) && port > 0 ? port : null;
            controller.Account = equipment.Username;
            controller.PassWord = equipment.Password;
        }

        /// <summary>
        /// Description: Thông tin kết nối vừa đổi ⇒ gỡ khoá do sai mật khẩu ở BE và Worker (qua NATS).
        /// Created date: 23/10/2026
        /// </summary>
        private void ResetDeviceAuthFailure(VwController controller)
        {
            if (string.IsNullOrWhiteSpace(controller.IP))
                return;

            _cache.Remove($"{CacheConst.Vw.VwDeviceAuthFailure}:{controller.IP}");

            _publisher.PublishCommand(
                VwCommandActions.ResetDeviceAuthFailure,
                new VwResetDeviceAuthFailurePayload { TargetIpOrKey = controller.IP },
                controllerId: controller.ID);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 VwController (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwDeleteControllerInput command)
        {
            var entity = await _vwControllerRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            await EnsureNoChildrenAsync(entity.ID);
            if (await _layout.IsControllerAssignedAsync(entity.ID))
                throw Oops.Oh(BaseMsg.Vw.Exception.ControllerAssignedInLayout);
            await _vwControllerRep.SoftDeleteAsync<VwController>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều VwController (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(List<VwDeleteControllerInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _vwControllerRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            // Bộ phân phối còn bộ con nằm ngoài danh sách xoá ⇒ chặn
            var orphans = await _vwControllerRep.CountAsync(u => u.IsDelete == null && u.ParentControllerId != null
                    && ids.Contains(u.ParentControllerId) && !ids.Contains(u.ID));
            if (orphans > 0)
                throw Oops.Oh(BaseMsg.Vw.Exception.DistributionControllerHasChildren, orphans);
            foreach (var id in ids)
                if (await _layout.IsControllerAssignedAsync(id))
                    throw Oops.Oh(BaseMsg.Vw.Exception.ControllerAssignedInLayout);

            await _vwControllerRep.SoftDeleteAsync<VwController>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        }

        /// <summary>
        /// Description: Một thiết bị (TmsEquipment) chỉ được liên kết với một VwController qua RefId.
        /// Created date: 09/10/2026
        /// </summary>
        private async Task EnsureUniqueRefIdAsync(string? controllerId, string? refId)
        {
            if (string.IsNullOrWhiteSpace(refId))
                return;

            if (await _vwControllerRep.IsAnyAsync(u => u.IsDelete == null && u.RefId == refId && u.ID != controllerId))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.EquipmentId]);
        }

        /// <summary>
        /// Description: Kiểm tra vai trò + bộ cha (VwControllerRole):
        ///                - Bộ phân phối / độc lập: không có cha. Bộ hiển thị: bắt buộc có cha là bộ phân phối.
        ///                - Bộ đang có bộ con (đang là bộ phân phối) thì chỉ giữ được vai trò bộ phân phối.
        /// Created date: 26/09/2026 — cập nhật 07/11/2026 (theo Role)
        /// </summary>
        private async Task EnsureValidRoleAsync(string? controllerId, string? role, string? parentControllerId)
        {
            if (string.IsNullOrWhiteSpace(role) || !VwControllerRole.All.Contains(role))
                throw Oops.Oh(BaseMsg.Vw.Exception.ControllerRoleInvalid);

            if (role != VwControllerRole.Distribution)
                await EnsureNoChildrenAsync(controllerId);

            // Hệ chỉ hỗ trợ 1 bộ phân phối: canvas master gộp cổng ra của mọi bộ phân phối (VwSceneRegionService)
            //    (Thêm mới: controllerId null ⇒ so với "" để SQL không thành "ID <> NULL" luôn sai)
            var selfId = controllerId ?? string.Empty;
            if (role == VwControllerRole.Distribution
                && await _vwControllerRep.IsAnyAsync(u => u.IsDelete == null && u.Role == VwControllerRole.Distribution && u.ID != selfId))
                throw Oops.Oh(BaseMsg.Vw.Exception.DistributionControllerExists);

            if (role != VwControllerRole.Display)
            {
                if (!string.IsNullOrWhiteSpace(parentControllerId))
                    throw Oops.Oh(BaseMsg.Vw.Exception.ControllerRoleCannotHaveParent);
                return;
            }

            if (string.IsNullOrWhiteSpace(parentControllerId))
                throw Oops.Oh(BaseMsg.Vw.Exception.DisplayControllerRequiresParent);

            if (!string.IsNullOrWhiteSpace(controllerId) && parentControllerId == controllerId)
                throw Oops.Oh(BaseMsg.Vw.Exception.ControllerCannotSelfParent);

            var parent = await _vwControllerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == parentControllerId)
                ?? throw Oops.Oh(BaseMsg.Vw.Exception.ParentControllerNotExist);

            if (parent.Role != VwControllerRole.Distribution)
                throw Oops.Oh(BaseMsg.Vw.Exception.ParentControllerMustBeDistribution);
        }

        /// <summary>
        /// Description: Bộ đang cấp tín hiệu cho bộ hiển thị (có con chưa xoá) ⇒ không được đổi vai trò / xoá.
        /// Created date: 07/11/2026
        /// </summary>
        private async Task EnsureNoChildrenAsync(string? controllerId)
        {
            if (string.IsNullOrWhiteSpace(controllerId))
                return;

            var children = await _vwControllerRep.CountAsync(u => u.IsDelete == null && u.ParentControllerId == controllerId);
            if (children > 0)
                throw Oops.Oh(BaseMsg.Vw.Exception.DistributionControllerHasChildren, children);
        }
    }
}
