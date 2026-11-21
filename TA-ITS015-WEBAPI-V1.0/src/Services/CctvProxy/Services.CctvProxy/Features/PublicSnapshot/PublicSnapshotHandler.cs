using System.Globalization;
using Microsoft.AspNetCore.Http;
using Services.CctvProxy.Configuration;
using Services.CctvProxy.Features.Proxy;
using Services.Shared.Logging;

namespace Services.CctvProxy.Features.PublicSnapshot;

/// <summary>
/// Author: kien.le
/// Description: Ảnh camera công khai cho web người dân — KHÔNG cần đăng nhập/ticket, gọi theo equipmentId (TmsEquipment.ID):
/// 1. Chỉ equipment có trong SysOpConfig cctv_public_equipment_ids mới xem được (còn lại 404).
/// 2. Camera online: lấy ảnh live NxVMS (cache CacheSeconds, mọi người xem dùng chung) rồi ghi đè thumbnail WebAPI.
/// 3. NxVMS lỗi / camera offline: trả thumbnail WebAPI (ảnh cuối cùng Hangfire hoặc proxy đã lưu).
/// Header X-Snapshot-Time (ISO 8601) = thời điểm chụp ảnh, X-Snapshot-Source = live | thumbnail.
/// Không nhận tham số nào (kích thước cố định) để không ai vượt được cache bắn vào NxVMS.
/// Created date: 06/10/2026
/// </summary>
public sealed class PublicSnapshotHandler(
    PublicCameraDirectory directory,
    SnapshotHttpProxy snapshotProxy,
    WebApiThumbnailStore thumbnailStore,
    IConfigManager configManager)
{
    /// <summary>
    /// Trả ảnh camera của một equipment công khai: ảnh live (cache chung) hoặc thumbnail WebAPI khi NxVMS lỗi/camera offline.
    /// Equipment không công khai trả 404; không có ảnh nào trả 503.
    /// </summary>
    /// <param name="context">Request từ web người dân (không cần đăng nhập).</param>
    /// <param name="equipmentId">TmsEquipment.ID.</param>
    public async Task HandleAsync(HttpContext context, string equipmentId)
    {
        var options = configManager.Current.Public;
        var aborted = context.RequestAborted;

        if (!options.Enabled || equipmentId.Length > 64)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var camera = await directory.FindAsync(equipmentId, aborted);
        if (camera is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var cacheSeconds = Math.Max(0, options.CacheSeconds);

        if (camera.IsOnline)
        {
            var live = await snapshotProxy.GetLiveAsync(camera.DeviceId, options.SnapshotSize, TimeSpan.FromSeconds(cacheSeconds), aborted);
            if (live.Data is not null)
            {
                // Ghi đè thumbnail WebAPI chạy nền — không bắt người xem chờ ghi file/DB.
                _ = thumbnailStore.SaveAsync(camera.DeviceId, live.Data, live.FetchedAt);
                await WriteImageAsync(context, live.Data, live.ContentType, live.FetchedAt, "live", cacheSeconds);
                return;
            }
        }

        var thumbnail = await thumbnailStore.ReadAsync(camera.DeviceId, aborted);
        if (thumbnail is { } fallback)
        {
            await WriteImageAsync(context, fallback.Data, "image/jpeg", new DateTimeOffset(fallback.ModifiedAt), "thumbnail", cacheSeconds);
            return;
        }

        LogManager.Log(Microsoft.Extensions.Logging.LogLevel.Warning,
            $"Không có ảnh cho equipment {equipmentId} (camera {camera.DeviceId}, online={camera.IsOnline})", kind: LogKind.Data);
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.CacheControl = "no-store";
    }

    private static async Task WriteImageAsync(
        HttpContext context, byte[] data, string contentType, DateTimeOffset takenAt, string source, int cacheSeconds)
    {
        var response = context.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = contentType;
        response.ContentLength = data.Length;
        // public + max-age: CDN/reverse proxy đặt phía trước có thể cache hộ, giảm băng thông của proxy.
        response.Headers.CacheControl = cacheSeconds > 0 ? $"public, max-age={cacheSeconds}" : "no-store";
        response.Headers["X-Snapshot-Time"] = takenAt.ToString("o", CultureInfo.InvariantCulture);
        response.Headers["X-Snapshot-Source"] = source;
        await response.Body.WriteAsync(data, context.RequestAborted);
    }
}
