using ITS.VideoWall.Core.Dto.DeviceSetup;
using ITS.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Dto.WindowScene;
using Module.VideoWall.Core.Entities;

namespace ITS.VideoWall.Core.Interfaces
{
    /// <summary>
    /// Author: Đạt
    /// Description: Dịch vụ nghiệp vụ điều khiển và đồng bộ thiết bị Video Wall ISAPI cấp cao:
    ///              - Đóng gói toàn bộ logic tương tác phần cứng, kiểm tra năng lực thiết bị (capabilities).
    ///              - Kích hoạt kịch bản hiển thị và xử lý đồng bộ đa bộ điều khiển (Multi-controller).
    ///              - Đồng bộ danh sách cửa sổ kịch bản (resync toàn bộ) và ánh xạ toạ độ cục bộ.
    ///              - Điều khiển chuyển nguồn tín hiệu và sắp xếp thứ tự lớp hiển thị (Z-Order Top/Bottom).
    ///              - Quản lý trạng thái lỗi xác thực thiết bị (Device Auth Failure).
    ///              - Kịch bản đấu nối thiết bị (Device Setup) khảo sát, đối chiếu và dựng kịch bản hiển thị.
    /// Created date: 18/08/2026
    /// </summary>
    public interface IVwISAPIDeviceService
    {
        /// <summary>
        /// Description: Truy vấn danh sách các kênh tín hiệu đầu vào video từ thiết bị controller vật lý.
        /// </summary>
        Task<VwISAPIInputChannelsResponse> GetInputChannels(VwController controller, CancellationToken ct = default);

        /// <summary>
        /// Description: Kích hoạt kịch bản hiển thị trên thiết bị phần cứng (kiểm tra capabilities, gửi lệnh Activate, cảnh báo lệch trạng thái).
        /// </summary>
        Task ActivateScene(VwScene scene, List<VwController> controllers, List<string> targetControllerIds, CancellationToken ct = default);

        /// <summary>
        /// Description: Đồng bộ toàn bộ danh sách cửa sổ của 1 kịch bản lên thiết bị vật lý (xoá hết rồi tạo lại, ánh xạ toạ độ, cập nhật DeviceWindowId).
        /// </summary>
        Task SyncSceneWindowsToDevice(
            string? sceneId,
            VwWindowScene? extraWindow = null,
            string? excludeWindowId = null,
            HashSet<string>? excludeWindowIds = null,
            CancellationToken ct = default);

        /// <summary>
        /// Description: Điều chỉnh thứ tự lớp hiển thị của cửa sổ (Top / Bottom) trên thiết bị.
        /// </summary>
        Task SetWindowLayer(VwController controller, string deviceWindowId, VwWindowLayerAction action, CancellationToken ct = default);

        /// <summary>
        /// Description: Kiểm tra IP hoặc Controller có đang trong trạng thái chặn do lỗi xác thực (Device Auth Failure) hay không.
        /// </summary>
        bool IsDeviceAuthFailureBlocked(string? ipOrKey, out TimeSpan remaining);

        /// <summary>
        /// Description: Ghi nhận lỗi xác thực phần cứng (401/403) để tích lũy trạng thái chặn.
        /// </summary>
        void RecordDeviceAuthFailure(string? ipOrKey, int statusCode);

        /// <summary>
        /// Description: Ghi nhận giao tiếp thành công, xóa bộ đếm lỗi xác thực trong Cache phân tán.
        /// </summary>
        void RecordAuthSuccess(string? ipOrKey);

        /// <summary>
        /// Description: Xóa trạng thái lỗi xác thực (Device Auth Failure) cho bộ điều khiển cụ thể.
        /// </summary>
        void ResetDeviceAuthFailure(string? ipOrKey);

        /// <summary>
        /// Description: Xóa toàn bộ trạng thái lỗi xác thực của tất cả bộ điều khiển trong Cache phân tán.
        /// </summary>
        void ResetAllDeviceAuthFailures();

        #region Kịch bản đấu nối thiết bị (Device Setup)

        /// <summary>Xác nhận Digest Auth vào thiết bị.</summary>
        Task<VwSetupSceneStep> Ping(string controllerId, CancellationToken ct = default);

        /// <summary>Khảo sát thiết bị bằng lệnh chỉ-đọc và đối chiếu với dữ liệu đang lưu trong DB.</summary>
        Task<VwProbeDeviceOutput> Probe(VwProbeDeviceInput input, CancellationToken ct = default);

        /// <summary>Nạp videoInputChannelID thật vào VwSource.SignalNo.</summary>
        Task<VwSyncSourcesOutput> SyncSources(VwSyncSourcesInput input, CancellationToken ct = default);

        /// <summary>
        /// Đồng bộ cổng ra (bảng gán cổng → ô) từ thiết bị về VwControllerOutput,
        /// rồi tính lại vị trí cổng ra theo bố cục đang áp dụng. input.ID trống ⇒ mọi bộ điều khiển.
        /// </summary>
        Task<VwSyncOutputsOutput> SyncOutputs(VwSyncOutputsInput input, CancellationToken ct = default);

        /// <summary>Đồng bộ màn đang cắm (bộ con) về VwScreen, ghép vào cổng ra đã lưu. input.ID trống ⇒ mọi bộ con.</summary>
        Task<VwSyncOutputsOutput> SyncScreens(VwSyncScreensInput input, CancellationToken ct = default);

        /// <summary>
        /// Ghi bảng cổng ra → vị trí (WallOutputList) xuống bộ con theo Bố cục gán cổng.
        /// DryRun ⇒ chỉ đọc thiết bị và trả bảng so sánh, không ghi.
        /// </summary>
        Task<VwApplyWallOutputsOutput> ApplyWallOutputs(VwApplyWallOutputsInput input, CancellationToken ct = default);

        /// <summary>Bật / tắt toàn bộ màn của các bộ hiển thị (ControllerIds trống ⇒ toàn tường).</summary>
        Task<VwScreenControlOutput> ScreenPower(VwScreenPowerInput input, CancellationToken ct = default);

        /// <summary>Đặt độ sáng (đèn nền 0-100) cho từng màn (ScreenIds).</summary>
        Task<VwScreenControlOutput> ScreenBacklight(VwScreenBacklightInput input, CancellationToken ct = default);

        /// <summary>Đọc trạng thái 1 màn (bật/tắt, độ sáng) từ bộ hiển thị; ghi độ sáng vào VwScreen.Brightness.</summary>
        Task<VwScreenStatusOutput> ReadScreenStatus(VwScreenStatusInput input, CancellationToken ct = default);

        /// <summary>Đối chiếu/đồng bộ ngược kịch bản đang chạy thật trên thiết bị về CSDL.</summary>
        Task<VwSyncActiveSceneOutput> SyncActiveScene(VwSyncActiveSceneInput input, CancellationToken ct = default);

        /// <summary>Dựng kịch bản hiển thị xuống thiết bị: khảo sát → đẩy cửa sổ → lưu → kích hoạt.</summary>
        Task<VwSetupSceneOutput> SetupScene(VwSetupSceneInput input, CancellationToken ct = default);

        /// <summary>Chuyển tiếp ISAPI thô: gửi HTTP request tùy ý xuống thiết bị, trả về raw request/response.</summary>
        Task<VwSetupSceneStep> SendPassthrough(VwISAPIPassthroughInput input, CancellationToken ct = default);

        #endregion

        #region Wall Resolution & Cache

        /// <summary>
        /// Xác định số thứ tự tường (wallNo) đang điều khiển trên bộ điều khiển.
        /// </summary>
        Task<int> ResolveWall(VwController controller, int? requestedWallNo = null, CancellationToken ct = default);

        /// <summary>
        /// Xác định danh sách wall logic cần thao tác cho bộ điều khiển.
        /// </summary>
        Task<List<int>> ResolveWalls(VwController controller, CancellationToken ct = default);

        /// <summary>
        /// Xóa số thứ tự tường đã ghi nhớ khỏi cache.
        /// </summary>
        void ForgetWall(string? controllerId);

        /// <summary>
        /// Yêu cầu bộ điều khiển trung tâm duy nhất (bộ có bộ con — xem VwControllerTopology, có IP), ném lỗi nếu thiếu, thừa hoặc chưa khai IP.
        /// </summary>
        Task<VwController> RequireCenterController(CancellationToken ct = default);

        /// <summary>
        /// Lấy bộ điều khiển trung tâm (bộ có bộ con — xem VwControllerTopology).
        /// </summary>
        Task<VwController> GetCenterController(CancellationToken ct = default);

        /// <summary>
        /// Alias tương thích: Lấy bộ điều khiển trung tâm.
        /// </summary>
        Task<VwController> GetCenterControllerAsync(CancellationToken ct = default);

        #endregion
    }
}
