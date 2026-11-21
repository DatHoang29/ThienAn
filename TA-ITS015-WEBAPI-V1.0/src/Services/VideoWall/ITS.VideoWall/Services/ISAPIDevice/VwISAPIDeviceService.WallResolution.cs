using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Entities;
using Shared.DTO.Constants.Application;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    public partial class VwISAPIDeviceService
    {
        private static readonly TimeSpan WallCacheDuration = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Chọn tường ISAPI sẽ thao tác cho một bộ điều khiển.
        ///
        /// Chuỗi ưu tiên:
        ///   1. requestedWallNo (caller chỉ định)
        ///   2. Cấu hình VideoWall:Device:WallNo
        ///   3. Cache (TTL 30 phút)
        ///   4. Gọi thiết bị: GET /ISAPI/DisplayDev/VideoWall → dò tường có cổng ra (bound)
        ///
        /// Xác định bound: ưu tiên wallBindOutputStatus nếu thiết bị trả (mock trả);
        /// thiết bị thật (DS-C30S-S11) KHÔNG trả trường này (0/2113 bản ghi trong LogsAPI/)
        /// → fallback sang GET .../{wallId}/outputs: WallOutputList rỗng ⇒ unbound.
        /// </summary>
        public async Task<int> ResolveWall(VwController controller, int? requestedWallNo = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(controller);

            if (requestedWallNo is > 0)
                return requestedWallNo.Value;

            var configured = _deviceOptions.Value.Device.WallNo;
            if (configured is > 0)
                return configured.Value;

            var key = GetWallCacheKey(controller.ID);
            var cached = _cache?.Get<int>(key) ?? 0;
            if (cached > 0)
                return cached;

            var result = await _deviceClient.GetVideoWallsAsync(controller, ct);
            if (!result.Success || result.Data == null || result.Data.VideoWall.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Không xác định được tường đang gắn màn hình trên thiết bị [{controller.Name ?? controller.ID}]: "
                    + $"{result.ErrorMessage ?? "danh sách tường rỗng"}");
            }

            // Bước 1: ưu tiên wallBindOutputStatus nếu thiết bị có trả (mock, firmware khác)
            var boundWalls = result.Data.VideoWall
                .Where(w => w.IsBoundByStatus == true)
                .ToList();

            // Bước 2: thiết bị thật không trả wallBindOutputStatus → tất cả IsBoundByStatus == null
            // Fallback: gọi GetOutputsAsync cho từng tường, tường nào có cổng ra là bound
            if (boundWalls.Count == 0
                && result.Data.VideoWall.Any(w => w.IsBoundByStatus == null))
            {
                _logger.LogInformation(
                    "Thiết bị [{Controller}] không trả wallBindOutputStatus — fallback sang kiểm tra outputs cho {Count} tường",
                    controller.Name ?? controller.ID,
                    result.Data.VideoWall.Count);

                foreach (var wall in result.Data.VideoWall)
                {
                    var outputResult = await _deviceClient.GetOutputsAsync(controller, wall.Id, ct);
                    if (outputResult.Success
                        && outputResult.Data?.WallOutput is { Count: > 0 })
                    {
                        boundWalls.Add(wall);
                    }
                }
            }

            if (boundWalls.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Không xác định được tường đang gắn màn hình trên thiết bị [{controller.Name ?? controller.ID}]: "
                    + "không có tường nào có cổng ra (bound)");
            }

            if (boundWalls.Count > 1)
            {
                _logger.LogWarning(
                    "Bộ điều khiển [{Controller}] có {Count} tường có cổng ra ({Walls}), tự động chọn tường đầu tiên (Wall {WallNo})",
                    controller.Name ?? controller.ID,
                    boundWalls.Count,
                    string.Join(", ", boundWalls.Select(w => w.Id)),
                    boundWalls[0].Id);
            }

            var selectedWall = boundWalls[0];

            _cache?.Set(key, selectedWall.Id, WallCacheDuration);
            _logger.LogInformation(
                "VwISAPIDeviceService: chọn tường {WallNo} ({WallName}) cho bộ điều khiển {Controller}",
                selectedWall.Id, selectedWall.Name, controller.Name ?? controller.ID);

            return selectedWall.Id;
        }

        public void ForgetWall(string? controllerId)
        {
            if (string.IsNullOrWhiteSpace(controllerId))
                return;

            _cache?.Remove(GetWallCacheKey(controllerId));
        }

        /// <summary>
        /// Xác định danh sách wall logic cần thao tác cho bộ trung tâm cascade.
        /// Bộ trung tâm có thể lái nhiều wall logic (VD 4 wall cho 4 khung), nên
        /// trả về List thay vì single int.
        /// Created date: 08/09/2026
        /// </summary>
        public async Task<List<int>> ResolveWalls(VwController controller, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(controller);

            var configured = _deviceOptions.Value.Device.WallNo;
            if (configured is > 0)
                return [configured.Value];

            var result = await _deviceClient.GetVideoWallsAsync(controller, ct);
            if (!result.Success || result.Data == null || result.Data.VideoWall.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Không xác định được tường trên thiết bị [{controller.Name ?? controller.ID}]: "
                    + $"{result.ErrorMessage ?? "danh sách tường rỗng"}");
            }

            var boundWalls = result.Data.VideoWall
                .Where(w => w.IsBoundByStatus == true)
                .ToList();

            if (boundWalls.Count == 0
                && result.Data.VideoWall.Any(w => w.IsBoundByStatus == null))
            {
                foreach (var wall in result.Data.VideoWall)
                {
                    var outputResult = await _deviceClient.GetOutputsAsync(controller, wall.Id, ct);
                    if (outputResult.Success
                        && outputResult.Data?.WallOutput is { Count: > 0 })
                    {
                        boundWalls.Add(wall);
                    }
                }
            }

            if (boundWalls.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Không xác định được tường đang gắn màn hình trên thiết bị [{controller.Name ?? controller.ID}]: "
                    + "không có tường nào có cổng ra (bound)");
            }

            return boundWalls.Select(w => w.Id).ToList();
        }

        /// <summary>
        /// Dùng chung tiền tố với cache bộ điều khiển để mỗi lần sửa VwController là tường ghi nhớ
        /// cũng bị xoá theo — đổi IP hay đổi cấu hình tường không để lại giá trị cũ.
        /// </summary>
        private static string GetWallCacheKey(string? controllerId) => $"{CacheConst.Vw.VwController}_wall:{controllerId}";
    }
}
