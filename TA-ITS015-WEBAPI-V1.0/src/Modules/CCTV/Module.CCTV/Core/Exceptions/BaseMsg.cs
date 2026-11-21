using Shared.DTO.Constants.Localization;

namespace Modules.CCTV.Core.Exceptions
{
    /// <summary>
    /// Author: ChuongLP
    /// Created date: 2026-06-08
    /// </summary>
    internal class BaseMsg : BaseLocaleManager
    {
        // ─────────────────────────────────────────────────────────────────────
        // CCTV — thông báo chung toàn module CCTV
        // ─────────────────────────────────────────────────────────────────────
        internal class Cctv
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.cctv.";
                internal const string Validation = "lz.validation.cctv.";
                internal const string Exception = "lz.exception.cctv.";
                internal const string Entity = "lz.entity.cctv.";
            }

            /// <summary>
            /// Tên hiển thị các trường thực thể dùng trong message lỗi.
            /// Ví dụ: "DeviceId", "Ip", "Tên dashboard"...
            /// </summary>
            internal class Entity
            {
                /// <summary>DeviceId (GUID do NxVMS cấp)</summary>
                internal const string DeviceId = Prefix.Entity + "deviceId";

                /// <summary>Địa chỉ IP của thiết bị</summary>
                internal const string Ip = Prefix.Entity + "ip";

                /// <summary>Tên Dashboard</summary>
                internal const string DashboardName = Prefix.Entity + "dashboardName";

                /// <summary>DashboardId (khoá ngoại trong DashboardItem)</summary>
                internal const string DashboardId = Prefix.Entity + "dashboardId";

                /// <summary>Tên Template</summary>
                internal const string TemplateName = Prefix.Entity + "templateName";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CctvDevice — thông báo riêng cho chức năng quản lý thiết bị
        // ─────────────────────────────────────────────────────────────────────
        internal class CctvDevice
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.cctvDevice.";
                internal const string Validation = "lz.validation.cctvDevice.";
                internal const string Exception = "lz.exception.cctvDevice.";
                internal const string Entity = "lz.entity.cctvDevice.";
            }

            /// <summary>
            /// Exception message khi không tìm thấy thiết bị hoặc thiết bị chưa đủ dữ liệu.
            /// Dùng thay cho các string hardcode trong CctvMediaQueryHandler / CctvPtzQueryHandler.
            /// </summary>
            internal class Exception
            {
                /// <summary>
                /// "Không tìm thấy camera với DeviceId [{0}]. Vui lòng đồng bộ thiết bị trước."
                /// </summary>
                internal const string NotFoundByDeviceId = Prefix.Exception + "notFoundByDeviceId";

                /// <summary>
                /// "Camera DeviceId [{0}] chưa có DeviceId từ hệ thống."
                /// </summary>
                internal const string MissingVmsDeviceId = Prefix.Exception + "missingVmsDeviceId";

                /// <summary>
                /// "Không tìm thấy camera với IP [{0}]. Vui lòng đồng bộ thiết bị trước."
                /// </summary>
                internal const string NotFoundByIp = Prefix.Exception + "notFoundByIp";

                /// <summary>
                /// "Cần truyền DeviceId hoặc IP để xác định camera."
                /// </summary>
                internal const string MissingDeviceIdOrIp = Prefix.Exception + "missingDeviceIdOrIp";

                /// <summary>
                /// "Có nhiều camera cùng IP [{0}]. Vui lòng truyền DeviceId để xác định chính xác."
                /// </summary>
                internal const string MultipleByIp = Prefix.Exception + "multipleByIp";

                /// <summary>
                /// "Đồng bộ thất bại cho thiết bị DeviceId [{0}]."
                /// </summary>
                internal const string SyncFailed = Prefix.Exception + "syncFailed";
            }

            internal class Entity
            {
                /// <summary>DeviceId (GUID NxVMS)</summary>
                internal const string DeviceId = Prefix.Entity + "deviceId";

                /// <summary>Địa chỉ IP</summary>
                internal const string Ip = Prefix.Entity + "ip";

                /// <summary>Tên thiết bị</summary>
                internal const string Name = Prefix.Entity + "name";

                /// <summary>Trạng thái kết nối (DeviceState)</summary>
                internal const string DeviceState = Prefix.Entity + "deviceState";
            }

            internal class Validation
            {
                /// <summary>
                /// "Cần truyền DeviceId hoặc IP để xác định camera."
                /// </summary>
                internal const string DeviceIdOrIpRequired = Prefix.Validation + "deviceIdOrIpRequired";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CctvDashboard — thông báo riêng cho chức năng Dashboard
        // ─────────────────────────────────────────────────────────────────────
        internal class CctvDashboard
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.cctvDashboard.";
                internal const string Validation = "lz.validation.cctvDashboard.";
                internal const string Exception = "lz.exception.cctvDashboard.";
                internal const string Entity = "lz.entity.cctvDashboard.";
            }

            internal class Entity
            {
                /// <summary>Tên Dashboard</summary>
                internal const string Name = Prefix.Entity + "name";

                /// <summary>Tag phân loại Dashboard</summary>
                internal const string Tag = Prefix.Entity + "tag";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CctvDashboardItem — thông báo riêng cho chức năng Dashboard Item
        // ─────────────────────────────────────────────────────────────────────
        internal class CctvDashboardItem
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.cctvDashboardItem.";
                internal const string Validation = "lz.validation.cctvDashboardItem.";
                internal const string Exception = "lz.exception.cctvDashboardItem.";
                internal const string Entity = "lz.entity.cctvDashboardItem.";
            }

            internal class Entity
            {
                /// <summary>DashboardId (khoá ngoại)</summary>
                internal const string DashboardId = Prefix.Entity + "dashboardId";

                /// <summary>EquipmentId (khoá ngoại sang TmsEquipment)</summary>
                internal const string EquipmentId = Prefix.Entity + "equipmentId";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CctvTemplate — thông báo riêng cho chức năng Template layout
        // ─────────────────────────────────────────────────────────────────────
        internal class CctvTemplate
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.cctvTemplate.";
                internal const string Validation = "lz.validation.cctvTemplate.";
                internal const string Exception = "lz.exception.cctvTemplate.";
                internal const string Entity = "lz.entity.cctvTemplate.";
            }

            internal class Entity
            {
                /// <summary>Tên Template</summary>
                internal const string Name = Prefix.Entity + "name";

                /// <summary>Ghi chú Template</summary>
                internal const string Remark = Prefix.Entity + "remark";

                /// <summary>Danh sách slot trong Template (JSON)</summary>
                internal const string Items = Prefix.Entity + "items";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CctvMedia — thông báo riêng cho chức năng Media (stream, download, snapshot)
        // ─────────────────────────────────────────────────────────────────────
        internal class CctvMedia
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.cctvMedia.";
                internal const string Validation = "lz.validation.cctvMedia.";
                internal const string Exception = "lz.exception.cctvMedia.";
                internal const string Entity = "lz.entity.cctvMedia.";
            }

            internal class Exception
            {
                /// <summary>
                /// "Không thể lấy token xác thực từ NxVMS."
                /// </summary>
                internal const string TokenFailed = Prefix.Exception + "tokenFailed";

                /// <summary>
                /// "Không thể lấy URL stream cho DeviceId [{0}]."
                /// </summary>
                internal const string StreamUrlFailed = Prefix.Exception + "streamUrlFailed";

                /// <summary>
                /// "Không thể tạo URL tải video cho DeviceId [{0}]."
                /// </summary>
                internal const string DownloadUrlFailed = Prefix.Exception + "downloadUrlFailed";

                /// <summary>
                /// "Snapshot thất bại cho DeviceId [{0}]."
                /// </summary>
                internal const string SnapshotFailed = Prefix.Exception + "snapshotFailed";

                /// <summary>
                /// "Chưa cấu hình [{0}] trong SysOpConfig (nhóm cctv_config) cho Cctv Proxy."
                /// Author: kien.le - Date: 2026-10-06
                /// </summary>
                internal const string ProxyNotConfigured = Prefix.Exception + "proxyNotConfigured";
            }

            internal class Entity
            {
                /// <summary>Mốc thời gian bắt đầu (Unix ms)</summary>
                internal const string PositionMs = Prefix.Entity + "positionMs";

                /// <summary>Thời lượng xuất video (ms)</summary>
                internal const string DurationMs = Prefix.Entity + "durationMs";

                /// <summary>Độ phân giải</summary>
                internal const string Resolution = Prefix.Entity + "resolution";

                /// <summary>Thời điểm bắt đầu</summary>
                internal const string FromDate = Prefix.Entity + "fromDate";

                /// <summary>Thời điểm kết thúc</summary>
                internal const string ToDate = Prefix.Entity + "toDate";

                /// <summary>Codec video khi tải</summary>
                internal const string VideoCodec = Prefix.Entity + "videoCodec";

                /// <summary>Loại đoạn ghi (recording/motion/analytics)</summary>
                internal const string PeriodType = Prefix.Entity + "periodType";

                /// <summary>Vùng cắt ảnh</summary>
                internal const string Crop = Prefix.Entity + "crop";

                /// <summary>Góc xoay ảnh</summary>
                internal const string Rotation = Prefix.Entity + "rotation";
            }

            /// <summary>Date: 21/10/2026</summary>
            internal class Validation
            {
                /// <summary>"Thời điểm kết thúc phải sau thời điểm bắt đầu."</summary>
                internal const string ToDateAfterFromDate = Prefix.Validation + "toDateAfterFromDate";

                /// <summary>"Thời lượng tải video tối đa 60 phút mỗi lần."</summary>
                internal const string DownloadDurationExceeded = Prefix.Validation + "downloadDurationExceeded";

                /// <summary>"Vùng cắt ảnh không đúng định dạng x,y,wxh (giá trị từ 0 đến 1)."</summary>
                internal const string InvalidCrop = Prefix.Validation + "invalidCrop";
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CctvPtz — thông báo riêng cho chức năng điều khiển PTZ
        // ─────────────────────────────────────────────────────────────────────
        internal class CctvPtz
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.cctvPtz.";
                internal const string Validation = "lz.validation.cctvPtz.";
                internal const string Exception = "lz.exception.cctvPtz.";
                internal const string Entity = "lz.entity.cctvPtz.";
            }

            internal class Exception
            {
                /// <summary>
                /// "Điều khiển PTZ thất bại cho DeviceId [{0}]."
                /// </summary>
                internal const string MoveFailed = Prefix.Exception + "moveFailed";

                /// <summary>
                /// "Không lấy được vị trí PTZ của thiết bị [{0}]."
                /// </summary>
                internal const string GetPositionFailed = Prefix.Exception + "getPositionFailed";

                /// <summary>
                /// "Không lấy được giới hạn PTZ của thiết bị [{0}]."
                /// </summary>
                internal const string GetLimitsFailed = Prefix.Exception + "getLimitsFailed";

                /// <summary>
                /// "Kích hoạt preset [{0}] thất bại."
                /// </summary>
                internal const string ActivatePresetFailed = Prefix.Exception + "activatePresetFailed";

                /// <summary>
                /// "Xóa preset [{0}] thất bại."
                /// </summary>
                internal const string DeletePresetFailed = Prefix.Exception + "deletePresetFailed";

                /// <summary>
                /// "Kích hoạt tour [{0}] thất bại."
                /// </summary>
                internal const string ActivateTourFailed = Prefix.Exception + "activateTourFailed";


                /// <summary>
                /// "Lưu preset [{0}] thất bại."
                /// </summary>
                internal const string SavePresetFailed = Prefix.Exception + "savePresetFailed";

                /// <summary>
                /// "Không xác định được preset vừa tạo. Vui lòng thử lại hoặc đồng bộ preset."
                /// (dùng cho case chênh lệch danh sách ≠ 1 khi Add)
                /// </summary>
                internal const string PresetIdNotResolved = Prefix.Exception + "presetIdNotResolved";

                /// <summary>
                /// "Đổi tên preset [{0}] thất bại."
                /// </summary>
                internal const string UpdatePresetFailed = Prefix.Exception + "updatePresetFailed";

                /// <summary>
                /// "Không xác định được preset cần kích hoạt (thiếu PresetId hoặc tên không khớp)."
                /// </summary>
                internal const string PresetNotSpecified = Prefix.Exception + "presetNotSpecified";

                /// <summary>
                /// "Camera không phản hồi. Vui lòng kiểm tra kết nối thiết bị."
                /// (dùng cho timeout / camera offline — bước 3)
                /// </summary>
                internal const string DeviceNotResponding = Prefix.Exception + "deviceNotResponding";

                /// <summary>
                /// "Không kết nối được tới hệ thống NxVMS."
                /// </summary>
                internal const string VmsConnectionFailed = Prefix.Exception + "vmsConnectionFailed";
            }

            internal class Entity
            {
                /// <summary>PresetId</summary>
                internal const string PresetId = Prefix.Entity + "presetId";

                /// <summary>TourId</summary>
                internal const string TourId = Prefix.Entity + "tourId";

                /// <summary>Tốc độ Pan</summary>
                internal const string PanSpeed = Prefix.Entity + "panSpeed";

                /// <summary>Tốc độ Tilt</summary>
                internal const string TiltSpeed = Prefix.Entity + "tiltSpeed";

                /// <summary>Tốc độ Zoom</summary>
                internal const string ZoomSpeed = Prefix.Entity + "zoomSpeed";
            }
        }
    }
}