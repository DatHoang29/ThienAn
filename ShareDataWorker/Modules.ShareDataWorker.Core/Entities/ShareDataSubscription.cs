using SqlSugar;
using Modules.ShareDataWorker.Core.Constants;

namespace Modules.ShareDataWorker.Core.Entities
{
    /// <summary>
    /// Đăng ký chia sẻ dữ liệu hai chiều (Outbound/Inbound).
    /// Author: Đạt
    /// Created date: 05/08/2026
    /// </summary>
    [SugarTable("ShareDataSubscription")]
    [SugarIndex("index_{table}_Scheduler", nameof(State), OrderByType.Asc, nameof(Direction), OrderByType.Asc, nameof(NextTimeRun), OrderByType.Asc)]
    public class ShareDataSubscription : EntityTenant
    {
        /// <summary>Mã chuỗi đăng ký (Serial Number text).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SerialNbr { get; set; }

        /// <summary>Đối tác (ShareDataPartner.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }

        /// <summary>Phiên kết nối đã đăng ký thành công (ShareDataSession.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SessionId { get; set; }

        /// <summary>Chiều đăng ký: OUTBOUND | INBOUND.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "OUTBOUND | INBOUND")]
        public string? Direction { get; set; }

        /// <summary>Loại dữ liệu chia sẻ (101–111).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? DatatypeId { get; set; }

        /// <summary>Chế độ: SINGLE | EVENT | PERIODIC | BATCH.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "SINGLE | EVENT | PERIODIC | BATCH")]
        public string? Mode { get; set; }

        /// <summary>Chu kỳ chạy (giây).</summary>
        [SugarColumn(IsNullable = true)]
        public int IntervalSeconds { get; set; } = 60;

        /// <summary>Lịch gửi dạng JSON.</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)", ColumnDescription = "Lịch gửi dạng JSON")]
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

        /// <summary>Trạng thái thực thi Worker: Idle | Running | Error.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? RunStatus { get; set; }

        /// <summary>Thời điểm thực thi tiếp theo.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? NextTimeRun { get; set; }

        /// <summary>Lý do đối tác từ chối.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? RejectReason { get; set; }

        /// <summary>Lý do hủy đăng ký.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? CancelReason { get; set; }

        /// <summary>Thời điểm gửi yêu cầu đăng ký.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? RequestedAt { get; set; }

        /// <summary>Thời điểm được duyệt/từ chối.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? ResolvedAt { get; set; }

        /// <summary>Người duyệt đăng ký INBOUND.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ResolvedBy { get; set; }

        /// <summary>Nguồn dữ liệu cấp cho đăng ký này (ShareDataDataSource.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? DataSourceId { get; set; }

        /// <summary>Hồ sơ ánh xạ áp dụng (ShareDataMappingProfile.ID).</summary>
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
    }
}
