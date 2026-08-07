using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Đăng ký chia sẻ dữ liệu hai chiều.
    ///              OUTBOUND = mình cấp dữ liệu cho đối tác; INBOUND = đối tác cấp dữ liệu cho mình.
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataSubscription")]
    public class ShareDataSubscription : EntityTenant
    {
        /// <summary>Đối tác (ShareDataPartner.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }

        /// <summary>Phiên kết nối đã đăng ký thành công (ShareDataSession.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SessionId { get; set; }

        /// <summary>Chiều đăng ký: OUTBOUND | INBOUND.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "OUTBOUND | INBOUND")]
        public string? Direction { get; set; }

        /// <summary>Serial number tăng dần theo đối tác (datexPublish-Serial-nbr).</summary>
        [SugarColumn(IsNullable = true)]
        public int? SerialNbr { get; set; }

        /// <summary>Mã loại dữ liệu chia sẻ — lấy từ bảng cấu hình, nhóm shareData_type.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? DatatypeId { get; set; }

        /// <summary>Chế độ: SINGLE | EVENT | PERIODIC.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "SINGLE | EVENT | PERIODIC")]
        public string? Mode { get; set; }

        /// <summary>
        /// Lịch gửi dạng JSON (chỉ dùng khi Mode = PERIODIC).
        /// { kind, updateDelaySec, startTime, endTime, daysOfWeek[], startDate, endDate, durationMinutes }
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Lịch gửi dạng JSON")]
        public string? ScheduleJson { get; set; }

        /// <summary>Định dạng: DATA | FILE.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length8, ColumnDescription = "DATA | FILE")]
        public string? Format { get; set; }

        /// <summary>Ưu tiên 0..10.</summary>
        [SugarColumn(IsNullable = true)]
        public int? Priority { get; set; }

        /// <summary>Đảm bảo gửi — gửi lại đến khi đối tác xác nhận.</summary>
        [SugarColumn(IsNullable = true)]
        public bool? Guaranteed { get; set; }

        /// <summary>Tự khôi phục — tự đăng ký lại sau khi nối lại kết nối.</summary>
        [SugarColumn(IsNullable = true)]
        public bool? Persistent { get; set; }

        /// <summary>Trạng thái: PENDING | ACTIVE | PAUSED | REJECTED | CANCELLED | EXPIRED.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? State { get; set; }

        /// <summary>Lý do đối tác từ chối (reject reason-cd).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? RejectReason { get; set; }

        /// <summary>Lý do hủy đăng ký (cancel reason-cd).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? CancelReason { get; set; }

        /// <summary>Thời điểm gửi yêu cầu đăng ký.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? RequestedAt { get; set; }

        /// <summary>Thời điểm được duyệt/từ chối.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? ResolvedAt { get; set; }

        /// <summary>Người duyệt đăng ký INBOUND (userId).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ResolvedBy { get; set; }

        /// <summary>Nguồn dữ liệu cấp cho đăng ký này (ShareDataDataSource.ID) — suy từ mapping.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? DataSourceId { get; set; }

        /// <summary>Hồ sơ ánh xạ áp dụng (ShareDataMappingProfile.ID) — suy từ (đối tác × gói tin × chiều).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? MappingProfileId { get; set; }

        /// <summary>Nguồn sự kiện kích hoạt khi Mode = EVENT (ShareDataEventSource.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EventSourceId { get; set; }

        /// <summary>Giãn cách tối thiểu giữa 2 lần gửi khi Mode = EVENT (giây).</summary>
        [SugarColumn(IsNullable = true)]
        public int? DebounceSec { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? LastTimeRun { get; set; }

    }
}
