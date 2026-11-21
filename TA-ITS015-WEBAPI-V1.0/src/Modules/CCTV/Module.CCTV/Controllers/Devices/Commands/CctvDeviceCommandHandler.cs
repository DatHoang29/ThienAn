using Module.CCTV.Controllers.CctvDevices.Dto;
using Module.CCTV.Infrastructure.Services.Device;
using Modules.CCTV.Core.Entities;
using Modules.CCTV.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Shared.Core.Extensions;
using Shared.DTO.Enums;
using Wolverine;

namespace Module.CCTV.Controllers.Devices.Commands
{
    public class CctvDeviceCommand : IWolverineHandler
    {
        private readonly BaseRepository<CctvDevice> _cctvDeviceRep;
        private readonly CctvDeviceSyncService _syncService;

        public CctvDeviceCommand(BaseRepository<CctvDevice> cctvDeviceRep, CctvDeviceSyncService syncService)
        {
            _cctvDeviceRep = cctvDeviceRep;
            _syncService = syncService;
        }

        /// <summary>
        /// Đồng bộ thiết bị NxVMS → ITS theo yêu cầu (nút "Đồng bộ" trên màn hình thiết bị CCTV).
        /// Chuyển từ CctvDeviceQueryHandler sang (21/10/2026) — đây là thao tác ghi; dùng chung logic với job Hangfire.
        /// Author: ChuongLP
        /// </summary>
        public Task<CctvDeviceSyncOutput> HandleAsync(CctvGetDeviceInput input, CancellationToken cancellationToken = default)
            => _syncService.SyncAsync(cancellationToken);

        /// <summary>
        /// Xử lý kích hoạt/ vô hiệu hóa thiết bị
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task HandleAsync(CctvDeviceActiveInput command)
        {
            var entity = await ResolveDeviceAsync(command.DeviceId, command.Ip);

            entity.Status = entity.Status == BaseEnums.StatusEnum.Enable
                                    ? BaseEnums.StatusEnum.Disable
                                    : BaseEnums.StatusEnum.Enable;
            entity.UpdateUId = App.User.GetUsername();
            entity.UpdateTime = DateTime.Now;

            await _cctvDeviceRep.UpdateAsync(entity);
        }

        /// <summary>
        /// Hàm xử lý xóa thiết bị
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task HandleAsync(CctvDeleteDeviceInput command)
        {
            var entity = await ResolveDeviceAsync(command.DeviceId, command.Ip);

            await _cctvDeviceRep.DeleteAsync(entity);
        }

        /// <summary>
        /// Tra bản ghi thiết bị theo Ip (ưu tiên nếu có) hoặc theo ID bản ghi (khóa chính).
        /// Nhiều bản ghi trùng Ip → throw để tránh thao tác nhầm thiết bị.
        /// Author: ChuongLP
        /// Date: 2026-07-28
        /// </summary>
        private async Task<CctvDevice> ResolveDeviceAsync(string? id, string? ip)
        {
            if (!string.IsNullOrWhiteSpace(ip))
            {
                var matches = await _cctvDeviceRep.AsQueryable()
                    .Where(x => x.IsDelete == null && x.Ip == ip)
                    .ToListAsync();

                if (matches.Count == 0)
                    throw Oops.Oh(BaseMsg.CctvDevice.Exception.NotFoundByIp, ip);

                if (matches.Count > 1)
                    throw Oops.Oh(BaseMsg.CctvDevice.Exception.MultipleByIp, ip);

                return matches[0];
            }

            return await _cctvDeviceRep.GetByIdAsync(id)
                ?? throw Oops.Oh(BaseMsg.BaseException.NotExist);
        }
    }
}
