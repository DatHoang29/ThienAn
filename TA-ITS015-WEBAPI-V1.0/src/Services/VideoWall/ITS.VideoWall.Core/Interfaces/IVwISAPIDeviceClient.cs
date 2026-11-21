using ITS.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Entities;

namespace ITS.VideoWall.Core.Interfaces
{
    /// <summary>
    /// Client giao tiếp với bộ điều khiển Hikvision Video Wall qua ISAPI.
    ///
    /// Hai quy ước xuyên suốt:
    ///   - Never-throw: mọi lỗi mạng/thiết bị/tham số trả về trong VwISAPIResult, không ném ngoại lệ.
    ///   - Việc chọn tường thuộc về IVwISAPIDeviceService.ResolveWall. Client KHÔNG tự đoán số tường;
    ///     thiết bị thật không trả wallBindOutputStatus (opt theo đặc tả hãng), xác định bound
    ///     bằng GetOutputsAsync (tường rỗng = unbound). Xem WallResolution.cs.
    /// </summary>
    public interface IVwISAPIDeviceClient
    {
        #region Device Auth Failure & Credential Management

        /// <summary>Đăng ký thông tin kết nối cho 1 Controller và trả về Base Uri.</summary>
        Uri EnsureRegistered(VwController controller);

        /// <summary>Làm mới thông tin kết nối khi controller đổi mật khẩu hoặc IP.</summary>
        Uri Refresh(VwController controller);

        bool IsDeviceAuthFailureBlocked(string? ipOrKey, out TimeSpan remaining);

        void RecordDeviceAuthFailure(string? ipOrKey, int statusCode);

        void ResetDeviceAuthFailure(string? ipOrKey);

        void ResetAllDeviceAuthFailures();

        #endregion

        #region ISAPI Business Operations

        /// <summary>Xác nhận kết nối và đăng nhập Digest (GET /ISAPI/Security/userCheck).</summary>
        Task<VwISAPIResult<VwISAPIUserCheckResponse>> UserCheckAsync(VwController controller, CancellationToken ct = default);

        Task<VwISAPIResult<VwISAPICapabilitiesResponse>> GetCapabilitiesAsync(VwController controller, CancellationToken ct = default);

        /// <summary>Danh sách tường logic — dùng để chọn tường đang gắn màn hình (bound).</summary>
        Task<VwISAPIResult<VwISAPIVideoWallList>> GetVideoWallsAsync(VwController controller, CancellationToken ct = default);

        Task<VwISAPIResult<VwISAPISceneStatusResponse>> GetActiveSceneAsync(VwController controller, int? wallNo = null, CancellationToken ct = default);

        Task<VwISAPIResult> ActivateSceneAsync(VwController controller, string isapiSceneId, int? wallNo = null, CancellationToken ct = default);

        Task<VwISAPIResult> SaveSceneDataAsync(VwController controller, string isapiSceneId, int? wallNo = null, CancellationToken ct = default);

        Task<VwISAPIResult<VwISAPIOutputsResponse>> GetOutputsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default);

        /// <summary>
        /// Cửa sổ đang mở trên tường. Dùng để chụp trạng thái trước khi ghi và để tra VWMWID sau
        /// khi Add — firmware không đảm bảo trả ID cửa sổ vừa tạo trong ResponseStatus.
        /// </summary>
        Task<VwISAPIResult<VwISAPIWallWindowList>> GetWindowsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default);

        /// <summary>Trả về VwISAPIResponseStatus.Id — ID cửa sổ (VWMWID) vừa tạo, có thể là 0.</summary>
        Task<VwISAPIResult<VwISAPIResponseStatus>> AddWindowAsync(VwController controller, VwISAPIWindowRequest window, int? wallNo = null, CancellationToken ct = default);

        Task<VwISAPIResult> UpdateWindowAsync(VwController controller, string windowId, VwISAPIWindowRequest window, int? wallNo = null, CancellationToken ct = default);

        Task<VwISAPIResult> DeleteWindowAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default);

        /// <summary>
        /// Xoá TẤT CẢ cửa sổ trên 1 tường — lệnh xoá trắng, kể cả cửa sổ do người khác tạo.
        /// Dùng cho chiến lược resync toàn bộ vì VwWindowScene không giữ VWMWID ổn định.
        /// </summary>
        Task<VwISAPIResult> DeleteAllWindowsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default);

        /// <summary>Danh sách kênh tín hiệu đầu vào (GET /ISAPI/DisplayDev/Video/inputs/channels).</summary>
        Task<VwISAPIResult<VwISAPIInputChannelsResponse>> GetInputChannelsAsync(VwController controller, CancellationToken ct = default);

        /// <summary>Trạng thái cắm cáp từng cổng ra (GET /ISAPI/DisplayDev/Video/outputs/channels).</summary>
        Task<VwISAPIResult<VwISAPIOutputChannelsResponse>> GetOutputChannelsAsync(VwController controller, CancellationToken ct = default);

        /// <summary>
        /// Màn LCD đang cắm trên từng ô của tường: serial, nhiệt độ, thời gian chạy
        /// (POST /ISAPI/DisplayDev/ScreenCtrl/GetWallScreenInfo?format=json, body = danh sách rect cần hỏi — đo 11/11/2026;
        /// GET trả 400, danh sách rỗng trả deviceError).
        /// </summary>
        Task<VwISAPIResult<VwISAPIWallScreenInfoResponse>> GetWallScreenInfoAsync(VwController controller, int wallNo,
            IEnumerable<VwISAPIWallScreenRect> rects, CancellationToken ct = default);

        /// <summary>
        /// Bật / tắt toàn bộ màn của tường trên bộ con: PUT /ISAPI/DisplayDev/ScreenCtrl/openAll | closeAll,
        /// body &lt;ScreenCtrl&gt;&lt;VideoWallID/&gt;&lt;/ScreenCtrl&gt; (lấy từ web hãng 11/11/2026; openAll không có trong tài liệu).
        /// </summary>
        Task<VwISAPIResult> SetScreenPowerAsync(VwController controller, int wallNo, bool on, CancellationToken ct = default);

        /// <summary>
        /// Trạng thái 1 màn (bật/tắt, đèn nền): POST /ISAPI/DisplayDev/ScreenCtrl/screenStatus?format=json,
        /// body {"ScreenStatusCond":{"statusType":"all","ScreenPos":{wallNo, Rect}}} (đo 11/11/2026).
        /// </summary>
        Task<VwISAPIResult<VwISAPIScreenStatusResponse>> GetScreenStatusAsync(VwController controller, int wallNo,
            VwISAPIWallScreenRect rect, CancellationToken ct = default);

        /// <summary>
        /// Đặt đèn nền (độ sáng 0-100) cho các màn: PUT /ISAPI/DisplayDev/ScreenCtrl/control,
        /// &lt;ScreenCtrlEx&gt; command=effect, backlight Number (khung lấy từ web hãng, thử OK trên .20 ngày 11/11/2026).
        /// </summary>
        Task<VwISAPIResult> SetScreenBacklightAsync(VwController controller, int wallNo,
            IEnumerable<VwISAPIWallScreenRect> rects, int value, CancellationToken ct = default);

        Task<VwISAPIResult> BringWindowToTopAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default);

        Task<VwISAPIResult> SendWindowToBottomAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default);

        /// <summary>
        /// 🔒 Danh sách kênh stream IP — response chứa credential thiết bị mã hoá.
        /// DTO output CỐ Ý không map password. KHÔNG log RawResponse của endpoint này.
        /// </summary>
        Task<VwISAPIResult<VwISAPIStreamChannelList>> GetStreamChannelsAsync(VwController controller, CancellationToken ct = default);

        /// <summary>Danh sách kịch bản trên một tường (GET .../{wallNo}/scene).</summary>
        Task<VwISAPIResult<VwISAPIWallSceneList>> GetScenesAsync(VwController controller, int? wallNo = null, CancellationToken ct = default);

        /// <summary>
        /// Layout kịch bản — endpoint duy nhất trả JSON, không phải XML.
        /// Query string ?format=json PHẢI nằm trong path khi tính HA2 của Digest.
        /// </summary>
        Task<VwISAPIResult<VwISAPISceneInfoResponse>> GetSceneInfoAsync(VwController controller, string sceneId, int? wallNo = null, CancellationToken ct = default);

        /// <summary>Tạo kịch bản — body không có id; SID mới ở ResponseStatus.ID.</summary>
        Task<VwISAPIResult<VwISAPIResponseStatus>> CreateSceneAsync(VwController controller, string sceneName, int? wallNo = null, CancellationToken ct = default);

        /// <summary>Đổi tên kịch bản — body có id phải khớp {sid} trên URL.</summary>
        Task<VwISAPIResult> RenameSceneAsync(VwController controller, string sceneId, string sceneName, int? wallNo = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy một cửa sổ theo ID.
        /// ⚠️ Firmware hiểu {winId} là số thứ tự 1-based (ordinal), KHÔNG phải VWMWID.
        /// Caller nên đối chiếu qua GetWindowsAsync để xác nhận.
        /// </summary>
        Task<VwISAPIResult<VwISAPIWallWindowItem>> GetWindowAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default);

        /// <summary>Bắt đầu giải mã động cho sub-window — bodyless PUT, 60/60 OK trong log.</summary>
        Task<VwISAPIResult> StartSubWindowDecodeAsync(VwController controller, string windowId, int subId, int? wallNo = null, CancellationToken ct = default);

        /// <summary>Chuyển tiếp ISAPI thô: gửi HTTP request tùy ý xuống thiết bị, không serialize/deserialize XML.</summary>
        Task<VwISAPIResult> SendRawAsync(VwController controller, HttpMethod method, string relativeUri,
            string? body, string? contentType, CancellationToken ct = default);

        /// <summary>Tuần tự hoá đối tượng sang chuỗi XML theo namespace mặc định của ISAPI.</summary>
        string SerializeToXml(object payload);

        #endregion
    }
}
