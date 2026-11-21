using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.DTO.Constants.Module
{
    /// <summary>
    /// Description: Các loại điều khiển nội dung VMS (ControlType) — dùng cho switch ở ControlVmsCommandHandler
    /// Author: ChuongLP
    /// Created date: 25/06/2026
    /// </summary>
    public static class VmsControlTypeConst
    {
        /// <summary> Hẹn giờ </summary>
        public const string Timer = "timer";

        /// <summary> Lập lịch </summary>
        public const string Schedule = "schedule";

        /// <summary> Hình ảnh </summary>
        public const string Image = "image";

        /// <summary> Mẫu dựng sẵn (template) </summary>
        public const string Template = "template";

        /// <summary> Văn bản </summary>
        public const string Text = "text";

        /// <summary> Thông điệp </summary>
        public const string Message = "message";

        /// <summary> Thư viện cụm từ </summary>
        public const string Phrase = "phrase";
    }

    /// <summary>
    /// Author: <bạn>
    /// Description: Hằng số phân loại thiết bị cho luồng điều khiển (FE gửi lên qua AddControlInput.DeviceType)
    /// Created date: 2026-07-10
    /// </summary>
    public static class VmsDeviceTypeConst
    {
        public const string VMS = "vms";
        public const string LCS = "lcs";
    }

    /// <summary>
    /// Chế độ phát khi có sự cố trên biển VMS
    /// </summary>
    public static class VmsPlayModeConst
    {
        /// <summary> Ghi đè: chiếm quyền độc quyền trên biển, dọn sạch sự cố cũ </summary>
        public const string Override = "override";

        /// <summary> Nối tiếp: ghép vào vòng lặp phát luân phiên cùng các ảnh sự cố khác </summary>
        public const string Append = "append";
    }

    /// <summary>
    /// Phân loại tiến trình VmsProcess
    /// </summary>
    public static class VmsProcessTypeConst
    {
        /// <summary> Lập lịch thông thường </summary>
        public const string Schedule = "schedule";

        /// <summary> Vòng lặp phát nối tiếp các hình ảnh sự cố </summary>
        public const string ScheduleIncident = "schedule-incident";

        /// <summary> Hẹn giờ </summary>
        public const string Timer = "timer";
    }

    /// <summary>
    /// Trạng thái tiến trình VmsProcess
    /// </summary>
    public static class VmsProcessStateConst
    {
        /// <summary> Đang chạy </summary>
        public const string Run = "run";

        /// <summary> Đã dừng </summary>
        public const string Stop = "stop";

        /// <summary> Tạm dừng do bị sự cố chiếm giữ </summary>
        public const string PauseIncident = "pause-incident";
    }

    /// <summary>
    /// Nguồn gốc ảnh trong bảng VmsProcessImg
    /// </summary>
    public static class VmsProcessImgSourceTypeConst
    {
        /// <summary> Ảnh lập lịch thường </summary>
        public const string Schedule = "schedule";

        /// <summary> Ảnh chèn tự động từ kịch bản automation sự cố </summary>
        public const string IncidentAuto = "incident_auto";

        /// <summary> Ảnh chèn thủ công từ form xử lý sự cố </summary>
        public const string IncidentManual = "incident_manual";

        /// <summary> Ảnh chèn nối tiếp từ bản đồ </summary>
        public const string ManualAppend = "manual_append";

        /// <summary> Ảnh của sự cố đang Ghi đè được đưa vào playlist khi có lệnh Nối tiếp </summary>
        public const string IncidentOverrideSeed = "incident_override";
    }

    /// <summary>
    /// Các tham số cấu hình hệ thống VMS trong SysOpConfig
    /// </summary>
    public static class VmsSysOpConfigConst
    {
        public const string GroupCode = "vms_config";

        /// <summary> Số lượng tối đa ảnh sự cố trong vòng lặp playlist (mặc định 5) </summary>
        public const string MaxPlaylistItems = "vms_incident_max_playlist_items";

        /// <summary> Chế độ phát sự cố mặc định của toàn hệ thống (override / append) </summary>
        public const string DefaultPlayMode = "vms_incident_default_play_mode";

        /// <summary> Thời gian hiển thị mặc định mỗi ảnh khi lặp (giây) </summary>
        public const string DefaultTimeToLive = "vms_incident_default_ttl";

        public const int DefaultMaxPlaylistItemsValue = 5;
        public const int DefaultTimeToLiveValue = 10;
        public const string DefaultPlayModeValue = "override";
    }
}
