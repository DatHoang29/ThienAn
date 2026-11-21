using Hangfire;
using Microsoft.Extensions.Logging;
using Module.CCTV.Controllers.CctvDevices.Dto;
using Module.CCTV.Controllers.CctvGetDevice;
using Module.CCTV.Infrastructure.Services.Media;
using Module.CCTV.Infrastructure.Services.Vendor;
using Module.Integration.CCTV.Core.Abstracts;
using Module.Integration.CCTV.Core.Dto;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Enums;
using Shared.Utility.Helpers;

namespace Module.CCTV.Infrastructure.Services.Device
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Đồng bộ thiết bị NxVMS → bảng CctvDevice (tách từ CctvDeviceQueryHandler 21/10/2026).
    /// Dùng chung cho:
    ///   - Job Hangfire "sync-cctv-devices" (<see cref="RunScheduledAsync"/>, có DisableConcurrentExecution).
    ///   - Nút "Đồng bộ" trên màn hình thiết bị CCTV (CctvDeviceCommandHandler → <see cref="SyncAsync"/>).
    /// Khóa trong process: không cho 2 lượt đồng bộ chạy chồng nhau trên cùng instance
    /// (trước đây job + nút bấm chạy song song sinh bản ghi CctvDevice trùng).
    /// Created date: 2026-06-09 — Updated: 21/10/2026
    /// </summary>
    public class CctvDeviceSyncService : IScoped
    {
        private static readonly SemaphoreSlim _syncLock = new(1, 1);

        private readonly BaseRepository<CctvDevice> _cctvDeviceRep;
        private readonly VendorService _vendorService;
        private readonly MediaSerivce _mediaSerivce;
        private readonly ILogger<CctvDeviceSyncService> _logger;

        public CctvDeviceSyncService(BaseRepository<CctvDevice> cctvDeviceRep, VendorService vendorService,
            MediaSerivce mediaSerivce, ILogger<CctvDeviceSyncService> logger)
        {
            _cctvDeviceRep = cctvDeviceRep;
            _vendorService = vendorService;
            _mediaSerivce = mediaSerivce;
            _logger = logger;
        }

        /// <summary>
        /// Đồng bộ theo yêu cầu người dùng. Đang có lượt đồng bộ khác chạy → báo lỗi, không chạy song song.
        /// </summary>
        public async Task<CctvDeviceSyncOutput> SyncAsync(CancellationToken cancellationToken = default)
        {
            if (!await _syncLock.WaitAsync(0, cancellationToken))
                throw Oops.Oh("Đang đồng bộ thiết bị CCTV, vui lòng thử lại sau.");

            try
            {
                return await SyncCoreAsync(cancellationToken);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Điểm vào của job Hangfire. Đang có lượt đồng bộ (vd người dùng vừa bấm) → bỏ qua lượt này.
        /// DisableConcurrentExecution: chặn chạy chồng giữa các instance WebAPI dùng chung Hangfire storage.
        /// </summary>
        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task RunScheduledAsync()
        {
            if (!await _syncLock.WaitAsync(0))
            {
                _logger.LogInformation("Đồng bộ CCTV theo lịch: bỏ qua vì đang có lượt đồng bộ khác chạy.");
                return;
            }

            try
            {
                var result = await SyncCoreAsync(CancellationToken.None);
                _logger.LogInformation(
                    "Đồng bộ CCTV theo lịch: VMS={Total}, thêm={Inserted}, sửa={Updated}, xóa={Deleted}, lỗi={Failed}, bỏ qua={Skipped}",
                    result.TotalFromVms, result.TotalInserted, result.TotalUpdated, result.TotalDeleted, result.TotalFailed, result.TotalSkipped);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>
        /// Xử lý đồng bộ thiết bị vào hệ thống ITS (logic giữ nguyên như bản trong CctvDeviceQueryHandler).
        /// Author : ChuongLP
        /// Date : 2026-06-09
        /// </summary>
        private async Task<CctvDeviceSyncOutput> SyncCoreAsync(CancellationToken cancellationToken)
        {
            var nxvms = await _vendorService.GetAsync();
            var result = new CctvDeviceSyncOutput();
            var now = DateTime.Now;

            var vmsDevices = await nxvms.GetDevicesAsync(onlyOnline: false, cancellationToken);
            var validVmsDevices = vmsDevices
                .Where(x => !string.IsNullOrWhiteSpace(x.DeviceId))
                .ToList();

            result.TotalFromVms = vmsDevices.Count;
            result.TotalSkipped = vmsDevices.Count - validVmsDevices.Count;

            var dbDevices = await _cctvDeviceRep.AsQueryable()
                .Where(x => x.IsDelete == null)
                .ToListAsync(cancellationToken);

            var dbMap = dbDevices
                .GroupBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    // Giữ bản mới nhất (tie-break theo ID để nhất quán với bước dọn trùng bên dưới)
                    g => g.OrderByDescending(x => x.CreateTime).ThenByDescending(x => x.ID).First(),
                    StringComparer.OrdinalIgnoreCase);

            var vmsIdSet = validVmsDevices
                .Select(x => x.DeviceId!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var device in validVmsDevices)
            {
                try
                {
                    var itsStatus = DeviceEnum.MapToItsStatus(device.DeviceState);

                    // Chỉ lưu URL stream khi thiết bị Online (HoatDong = 1).
                    // Thiết bị KHÔNG online mà vẫn có URL sẽ khiến FE cố mở stream không tồn tại
                    // → treo giao diện. Vì vậy khác Online thì để Url = null.
                    string? wssUrl = null;
                    if (itsStatus == (int)CctvItsStatus.HoatDong)
                    {
                        wssUrl = await nxvms.BuildWebRtcStreamUrlAsync(new VmsWebRtcUrlInput
                        {
                            DeviceId = device.DeviceId!,
                            Stream = "secondary",
                            Resolution = "640x360",
                            FromDate = null
                        });
                    }

                    if (dbMap.TryGetValue(device.DeviceId!, out var existed))
                    {
                        existed.Name = device.Name;
                        existed.Ip = device.Ip;
                        existed.Url = wssUrl;
                        existed.DeviceState = itsStatus;
                        existed.LastSyncTime = now;
                        existed.UpdateTime = now;

                        var snapshotUpdated = await TryUpdateSnapshotAsync(nxvms, existed, cancellationToken);

                        // (21/10/2026) Chỉ update cột do sync quản lý. Cctv Proxy (WebApiThumbnailStore) cũng ghi
                        // SnapshotTime/SnapshotUrl trực tiếp → chỉ ghi 2 cột này khi lượt sync chụp được ảnh mới,
                        // tránh đè giá trị mới hơn bằng dữ liệu đọc từ đầu lượt sync.
                        var updateable = _cctvDeviceRep.AsUpdateable(existed);
                        await (snapshotUpdated
                                ? updateable.UpdateColumns(x => new { x.Name, x.Ip, x.Url, x.DeviceState, x.LastSyncTime, x.UpdateTime, x.SnapshotUrl, x.SnapshotTime })
                                : updateable.UpdateColumns(x => new { x.Name, x.Ip, x.Url, x.DeviceState, x.LastSyncTime, x.UpdateTime }))
                            .ExecuteCommandAsync();
                        result.TotalUpdated++;
                    }
                    else
                    {
                        var newDevice = new CctvDevice
                        {
                            DeviceId = device.DeviceId!,
                            Name = device.Name,
                            Ip = device.Ip,
                            Url = wssUrl,
                            DeviceState = itsStatus,
                            Status = BaseEnums.StatusEnum.Enable,
                            LastSyncTime = now,
                            CreateTime = now,
                        };

                        await TryUpdateSnapshotAsync(nxvms, newDevice, cancellationToken);

                        await _cctvDeviceRep.InsertAsync(newDevice);
                        result.TotalInserted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Sync thất bại cho DeviceId={DeviceId}, Name={Name}",
                        device.DeviceId,
                        device.Name
                    );

                    result.TotalFailed++;
                }
            }

            if (validVmsDevices.Count == 0)
            {
                return result;
            }

            // (b) Dọn trùng: mỗi DeviceId chỉ giữ 1 bản ghi (bản mới nhất - đã được cập nhật ở vòng lặp trên),
            //     các bản ghi trùng DeviceId còn lại sẽ bị xóa để bảng chỉ có 1 data / DeviceId.
            var duplicateExtras = dbDevices
                .Where(x => !string.IsNullOrWhiteSpace(x.DeviceId))
                .GroupBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderByDescending(x => x.CreateTime).ThenByDescending(x => x.ID).Skip(1));

            // (a) NxVMS là nguồn chuẩn: thiết bị có trong DB nhưng không còn ở NxVMS → xóa cho khớp.
            // Gộp (a) + (b), loại trùng theo ID để không xóa lặp.
            var toDelete = dbDevices
                .Where(x => !string.IsNullOrWhiteSpace(x.DeviceId) && !vmsIdSet.Contains(x.DeviceId))
                .Concat(duplicateExtras)
                .GroupBy(x => x.ID)
                .Select(g => g.First())
                .ToList();

            foreach (var item in toDelete)
            {
                try
                {
                    _mediaSerivce.DeleteSnapshot(item.SnapshotUrl);  // Xóa ảnh snapshot cũ nếu có
                    await _cctvDeviceRep.DeleteAsync(item);
                    result.TotalDeleted++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Đồng bộ CCTV: Xóa thiết bị dư thất bại DeviceId={DeviceId}, Name={Name}",
                        item.DeviceId,
                        item.Name);
                }
            }

            return result;
        }

        /// <summary>
        /// Thử chụp snapshot và cập nhật SnapshotUrl/SnapshotTime.
        /// Nếu thiết bị offline hoặc chụp lỗi thì giữ nguyên ảnh cũ.
        /// Author: ChuongLP
        /// Date: 2026-06-21
        /// </summary>
        /// <returns>true nếu đã chụp + lưu ảnh mới (SnapshotUrl/SnapshotTime vừa được gán).</returns>
        private async Task<bool> TryUpdateSnapshotAsync(BaseCctvService nxvms, CctvDevice device, CancellationToken cancellationToken = default)
        {
            if (device == null)
                return false;

            if (string.IsNullOrWhiteSpace(device.DeviceId))
                return false;

            // Quy ước hiện tại: Online = 1.
            // Nếu DeviceEnum.MapToItsStatus của bạn map Online ra giá trị khác
            // thì chỉnh lại điều kiện này.
            if (device.DeviceState != 1)
                return false;

            try
            {
                var snapshotTime = DateTime.Now;

                var (bytes, _) = await nxvms.GetSnapshotAsync(
                    new VmsSnapshotInput
                    {
                        DeviceId = device.DeviceId,
                        Format = "jpg",
                        Size = "640x360",
                        FromDate = null
                    },
                    cancellationToken
                );

                var snapshotUrl = await _mediaSerivce.SaveSnapshotAsync(bytes, device.DeviceId, cancellationToken);

                if (string.IsNullOrWhiteSpace(snapshotUrl))
                    return false;

                device.SnapshotUrl = snapshotUrl;
                device.SnapshotTime = snapshotTime;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
