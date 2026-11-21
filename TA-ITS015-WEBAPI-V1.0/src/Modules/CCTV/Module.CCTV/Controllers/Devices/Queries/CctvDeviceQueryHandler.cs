using Microsoft.Extensions.Logging;
using Module.CCTV.Controllers.CctvDevices.Dto;
using Module.CCTV.Controllers.CctvGetDevice;
using Module.CCTV.Infrastructure.Services.Device;
using Module.CCTV.Infrastructure.Services.Media;
using Module.CCTV.Infrastructure.Services.Proxy;
using Module.CCTV.Infrastructure.Services.Vendor;
using Module.Integration.CCTV.Core.Abstracts;
using Module.Integration.CCTV.Core.Dto;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Enums;
using Shared.Utility.Helpers;
using Wolverine;

namespace Module.CCTV.Controllers.Cctvevices.Queries
{
    public class CctvDeviceQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<CctvDevice> _cctvDeviceRep;
        private readonly BaseRepository<TmsEquipment> _equipmentRep;
        private readonly BaseRepository<TmsEquipmentType> _equipmentTypeRep;
        private readonly BaseRepository<TmsModule> _modulesRep;
        private readonly DeviceService _configService;
        private readonly ILogger<CctvDeviceQueryHandler> _logger;
        private readonly VendorService _vendorService;
        private readonly CctvProxyUrlService _proxyUrlService;
        public CctvDeviceQueryHandler(BaseRepository<CctvDevice> cctvDeviceRep,BaseRepository<TmsEquipment> equipmentRep,BaseRepository<TmsEquipmentType> equipmentTypeRep,
            BaseRepository<TmsModule> modulesRep,DeviceService configService,ILogger<CctvDeviceQueryHandler> logger, VendorService vendorService,
            CctvProxyUrlService proxyUrlService)
        {
            _cctvDeviceRep = cctvDeviceRep;
            _equipmentRep = equipmentRep;
            _equipmentTypeRep = equipmentTypeRep;
            _modulesRep = modulesRep;
            _configService = configService;
            _logger = logger;
            _vendorService = vendorService;
            _proxyUrlService = proxyUrlService;
        }

        // Đồng bộ thiết bị (CctvGetDeviceInput) đã chuyển sang CctvDeviceSyncService + CctvDeviceCommandHandler (21/10/2026).

        /// <summary>
        /// Lấy danh sách thiết bị đang có trong hệ thống ITS (đã include cả thiết bị sau khi sync)
        /// Author: ChuongLP
        /// Date : 2026-06-09
        /// </summary>
        public async Task<List<CctvDeviceOutput>> HandleAsync(CctvGetDevicesListInput input, CancellationToken cancellationToken = default)
        {
            return await _cctvDeviceRep.AsQueryable()
                .Where(x => x.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(input.DeviceId), x => x.DeviceId == input.DeviceId)
                .WhereIF(!string.IsNullOrWhiteSpace(input.Ip), x => x.Ip!.Contains(input.Ip!))
                .WhereIF(!string.IsNullOrWhiteSpace(input.Name), x => x.Name!.Contains(input.Name!))
                .Select(x => new CctvDeviceOutput
                {
                    ID = x.ID,
                    DeviceId = x.DeviceId,
                    Name = x.Name,
                    Ip = x.Ip,
                    DeviceState = x.DeviceState,
                    Status = x.Status,
                    LastSyncTime = x.LastSyncTime,
                    CreateTime = x.CreateTime,
                    UpdateTime = x.UpdateTime,
                    SnapshotUrl = x.SnapshotUrl,
                    SnapshotTime = x.SnapshotTime
                })
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Lấy danh sách thiết bị tổng hợp.
        /// Tự động lọc theo ModuleCode được cấu hình trong SysOpConfig
        /// (Code: cctv_device_type, GroupCode: cctv_config, Value: "CCTV,VDS").
        /// FE không cần truyền EquipmentTypeId hay ModuleCode — BE tự lọc.
        /// Author: ChuongLP
        /// Date: 2026-06-09
        /// </summary>
        public async Task<List<CctvDeviceViewOutput>> HandleAsync(CctvGetDeviceViewInput input, CancellationToken cancellationToken = default)
        {
            var nxvms = await _vendorService.GetAsync();

            // ── Bước 1: Đọc danh sách ModuleCode được phép từ SysOpConfig ─────────
            // VD: "CCTV,VDS" → ["CCTV", "VDS"]
            var deviceTypeRaw = await _configService.GetDeviceTypeAsync();
            var allowedModules = deviceTypeRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            // ── Bước 2: Lấy ModuleId tương ứng từ TmsModules ─────────────────────
            // Tránh JOIN 4 bảng phức tạp — tách ra 2 query nhỏ, rõ ràng hơn
            var moduleIds = await _modulesRep.AsQueryable()
                .Where(m => allowedModules.Contains(m.KeyName) && m.IsDelete == null)
                .Select(m => m.ID)
                .ToListAsync(cancellationToken);

            if (!moduleIds.Any())
                return new List<CctvDeviceViewOutput>();

            // ── Bước 3: Lấy EquipmentTypeId thuộc các Module trên ────────────────
            var equipmentTypeIds = await _equipmentTypeRep.AsQueryable()
                .Where(et => moduleIds.Contains(et.ModuleId))
                .Select(et => et.ID)
                .ToListAsync(cancellationToken);

            if (!equipmentTypeIds.Any())
                return new List<CctvDeviceViewOutput>();

            // ── Bước 4: Query TmsEquipment JOIN CctvDevice, lọc theo EquipmentTypeId ──
            var list = await _equipmentRep.AsQueryable()
                .LeftJoin<CctvDevice>((eq, cam) => eq.Ip == cam.Ip)
                .LeftJoin<TmsEquipmentType>((eq, cam, et) => eq.EquipmentTypeId == et.ID)
                .Where((eq, cam, et) => equipmentTypeIds.Contains(eq.EquipmentTypeId))
                .WhereIF(!string.IsNullOrWhiteSpace(input.Ip), (eq, cam, et) => eq.Ip!.Contains(input.Ip!))
                .WhereIF(!string.IsNullOrWhiteSpace(input.DeviceId), (eq, cam, et) => cam.DeviceId == input.DeviceId)
                .Select((eq, cam, et) => new CctvDeviceViewOutput
                {
                    ID = eq.ID,
                    Ip = eq.Ip,
                    Name = eq.Name,
                    Port = eq.Port,
                    DeviceId = cam.DeviceId,
                    Url = cam.Url,
                    Remark = eq.Remark,
                    EquipmentTypeId = eq.EquipmentTypeId,
                    EquipmentTypeName = et.Name,
                    EquipmentState = eq.EquipmentState,
                    DeviceState = cam.DeviceState,
                    CreateTime = cam.CreateTime,
                    LastSyncTime = cam.LastSyncTime,
                    SnapshotUrl = cam.SnapshotUrl,
                    SnapshotTime = cam.SnapshotTime
                })
                .ToListAsync(cancellationToken);

            // ── Bước 5: Build WSS URL tươi CHỈ cho thiết bị Online ────────────────
            // Thiết bị khác Online: để Url = null, tránh FE mở stream không tồn tại gây treo.
            // Không trả URL NxVMS trực tiếp: đổi sang URL Cctv Proxy kèm ticket (kien.le 2026-10-07).
            // Không build được (vd chưa cấu hình proxy) → Url = null, không để lộ địa chỉ NxVMS cho FE.
            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item.DeviceId) || item.DeviceState != (int)CctvItsStatus.HoatDong)
                {
                    item.Url = null;
                    continue;
                }

                try
                {
                    var wssUrl = await nxvms.BuildWebRtcStreamUrlAsync(new VmsWebRtcUrlInput
                    {
                        DeviceId = item.DeviceId!,
                        Stream = "secondary",
                        Resolution = "640x360",
                        FromDate = null
                    });
                    item.Url = (await _proxyUrlService.BuildAsync(wssUrl, item.DeviceId!, BaseHelper.CctvStreamTicketHp.KindWebRtc)).Url;
                }
                catch (Exception ex)
                {
                    item.Url = null;
                    _logger.LogWarning(ex, "CCTV deviceview: không build được URL proxy cho DeviceId={DeviceId}", item.DeviceId);
                }
            }

            return list;
        }


    }
}