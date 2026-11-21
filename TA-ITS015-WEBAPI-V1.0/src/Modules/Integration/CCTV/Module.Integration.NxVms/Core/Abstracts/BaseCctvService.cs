using Module.Integration.CCTV.Core.Dto;
using Module.Integration.NxVms.Infrastructure.Dto;

namespace Module.Integration.CCTV.Core.Abstracts;

/// <summary>
/// Abstract base class định nghĩa contract cho bất kỳ hệ thống VMS nào.
/// Tầng CCTV (handler/controller) chỉ phụ thuộc vào class này,
/// không import trực tiếp NxVmsService hay bất kỳ hãng cụ thể nào.
///
/// Quy tắc mở rộng: khi tích hợp thêm hãng mới (Hikvision, Milestone…),
/// chỉ cần tạo class kế thừa BaseCctvService và đăng ký DI —
/// toàn bộ handler không cần sửa.
/// </summary>
public abstract class BaseCctvService
{
    // ── Device ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách thiết bị từ VMS.
    /// onlyOnline=true → chỉ trả thiết bị đang Online.
    /// </summary>
    public abstract Task<List<VmsDeviceOutput>> GetDevicesAsync(bool onlyOnline = false,CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy token xác thực hiện tại (có cache, tự refresh khi gần hết hạn).
    /// Trả về null nếu không kết nối được tới VMS (host down/timeout) — không throw.
    /// </summary>
    public abstract Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);


    // ── Media ─────────────────────────────────────────────────────────────────

    /// <summary>Build URL stream/media thông thường (không phải WebRTC).</summary>
    public abstract Task<string> BuildMediaUrlAsync(VmsMediaUrlInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Build WSS URL WebRTC có kèm token trong URL.
    /// Dùng khi FE kết nối thẳng tới Vms (không qua proxy).
    /// </summary>
    public abstract Task<string> BuildWebRtcStreamUrlAsync(VmsWebRtcUrlInput input);

    ///// <summary>
    ///// Build WSS URL WebRTC KHÔNG kèm token.
    ///// Dùng khi FE kết nối qua backend proxy — proxy tự gắn Bearer header.
    ///// </summary>
    //public abstract Task<string> BuildWebRtcStreamUrlNoAuth(VmsWebRtcUrlInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Build HTTPS URL tải file video archive (async vì cần lấy token tươi gắn vào URL).
    /// </summary>
    public abstract Task<string> BuildMediaDownloadUrlAsync(VmsMediaDownloadInput input,CancellationToken cancellationToken = default);

    /// <summary>Lấy ảnh snapshot từ camera. Trả về bytes + content-type.</summary>
    public abstract Task<(byte[] Data, string ContentType)> GetSnapshotAsync(VmsSnapshotInput input,CancellationToken cancellationToken = default);

    /// <summary>
    /// Build URL ảnh snapshot phía VMS (không kèm token) — WebAPI đổi sang URL Cctv Proxy kèm ticket.
    /// Author: kien.le - Date: 2026-10-06
    /// </summary>
    public abstract Task<string> BuildSnapshotUrlAsync(VmsSnapshotInput input);

    /// <summary>Lấy danh sách footage (các chunk đã ghi) của thiết bị.</summary>
    public abstract Task<List<VmsFootageOutput>> GetFootageAsync(VmsFootageInput input, CancellationToken cancellationToken = default);

    #region PTZ

    /// <summary>Di chuyển PTZ đến vị trí tuyệt đối (pan, tilt, fov).</summary>
    public abstract Task<VmsActionOutput> MovePtzPositionAsync(VmsPtzPositionInput input,CancellationToken cancellationToken = default);

    /// <summary>Di chuyển PTZ liên tục theo tốc độ (panSpeed, tiltSpeed, zoomSpeed).</summary>
    public abstract Task<VmsActionOutput> MovePtzAsync(VmsPtzMoveInput input,CancellationToken cancellationToken = default);

    /// <summary>Dừng di chuyển PTZ.</summary>
    public abstract Task<VmsActionOutput> StopPtzAsync(string deviceId,CancellationToken cancellationToken = default);

    /// <summary>Lấy vị trí PTZ hiện tại.</summary>
    public abstract Task<VmsPtzPositionOutput> GetPtzPositionAsync(string deviceId,CancellationToken cancellationToken = default);

    /// <summary>Lấy giới hạn PTZ (min/max pan, tilt, zoom, focus).</summary>
    public abstract Task<VmsPtzLimitsOutput> GetPtzLimitsAsync(string deviceId,CancellationToken cancellationToken = default);
    

    // ── PTZ Preset ────────────────────────────────────────────────────────────

    /// <summary>Lấy danh sách preset PTZ đã đăng ký trên camera.</summary>
    public abstract Task<List<VmsPtzPresetOutput>> GetPresetsAsync(string deviceId,CancellationToken cancellationToken = default);

    /// <summary>Kích hoạt (load) một preset PTZ theo presetId.</summary>
    public abstract Task<VmsActionOutput> ActivatePresetAsync(VmsActivatePresetInput input,CancellationToken cancellationToken = default);

    /// <summary>Xóa một preset PTZ.</summary>
    public abstract Task<VmsActionOutput> DeletePresetAsync(VmsDeletePresetInput input,CancellationToken cancellationToken = default);

    /// <summary>Đổi tên một preset PTZ đã tồn tại.</summary>
    public abstract Task<VmsActionOutput> UpdatePresetAsync(NxVmsUpdatePresetInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo preset PTZ mới tại vị trí hiện tại của camera, rồi trả về presetId THẬT.
    /// Vì id trong response add không đáng tin, mỗi vendor tự chịu trách nhiệm
    /// xác định đúng presetId vừa tạo (NxVms: so sánh danh sách trước/sau khi add).
    /// Tầng trên chỉ nhận về presetId chính xác để lưu DB.
    /// </summary>
    public abstract Task<VmsPresetAddOutput> AddPresetAndResolveIdAsync(NxVmsAddPresetInput input, CancellationToken cancellationToken = default);
    // ── PTZ Tour ──────────────────────────────────────────────────────────────

    /// <summary>Lấy danh sách tour PTZ.</summary>
    public abstract Task<List<VmsPtzTourOutput>> GetToursAsync(string deviceId,CancellationToken cancellationToken = default);

    /// <summary>Kích hoạt hành trình tuần tra tự động (tour) theo tourId.</summary>
    public abstract Task<VmsActionOutput> ActivateTourAsync(VmsActivateTourInput input,CancellationToken cancellationToken = default);

    #endregion
}
