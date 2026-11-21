using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Source
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("VwSource")]
    public class VwSource : EntityTenant
    {
        /// <summary>
        /// Description: Tên nguồn tín hiệu đầu vào.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        /// <summary>
        /// Description: Mã định danh ControllerId của bộ điều khiển quản lý nguồn tín hiệu này.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ControllerId { get; set; }

        /// <summary>
        /// Description: Loại nguồn tín hiệu SourceType (Camera, IPC, PC, HDMI...).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SourceType { get; set; }

        /** ISAPI OutputID (metadata) */
        /// <summary>
        /// Description: Kiểu tín hiệu SignalType theo giao thức ISAPI.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SignalType { get; set; }

        /// <summary>
        /// Description: Đường dẫn URL luồng phát tín hiệu (RTSP/HTTP stream).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Url { get; set; }

        /// <summary>
        /// Description: Cổng kết nối đầu vào InputPort trên thiết bị.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public int? InputPort { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? SignalNo { get; set; }

        /// <summary>
        /// Description: ID ảnh chụp thu nhỏ Thumbnail của nguồn tín hiệu.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Thumbnail { get; set; }

        /// <summary>
        /// Description: Độ phân giải tối đa MaxResolution được nguồn hỗ trợ.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? MaxResolution { get; set; }

        /// <summary>
        /// Description: Tỷ lệ khung hình AspectRatio (16:9, 4:3...).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? AspectRatio { get; set; }

        /// <summary>
        /// Description: Trạng thái kích hoạt (Enable / Disable) của nguồn tín hiệu.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>
        /// Description: Trạng thái tín hiệu SignalStatus thực tế (Có tín hiệu / Mất tín hiệu).
        ///              Giá trị là hằng chuỗi trong BaseEnums.SignalStatus ("Signal" / "NoSignal" / "Abnormal").
        ///              BaseEnums.SignalStatus là class hằng (không phải enum) ⇒ kiểu thuộc tính phải là string
        ///              (khai kiểu class thì SqlSugar không map được cột nvarchar ⇒ luôn null) — sửa 10/11/2026.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SignalStatus { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        /// <summary>
        /// Description: Ghi chú Remark bổ sung cho nguồn tín hiệu.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        /// <summary>
        /// Description: Thời điểm gần nhất Heartbeat cập nhật SignalStatus cho nguồn tín hiệu này.
        /// Created date: 12/09/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? LastCheckedAt { get; set; }
    }
}