using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đăng ký chia sẻ dữ liệu hai chiều.
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataSubscription")]
    public class ShareDataSubscription : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SessionId { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "1 = Inbound | 0 = Outbound")]
        public BaseEnums.Direction? Direction { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? SerialNbr { get; set; }
        [SugarColumn(IsNullable = true)]
        public string? DatatypeId { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.SubMode? Mode { get; set; }
        /// <summary>Định dạng xuất bản (DATA | FILE).</summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.PublishFormat? Format { get; set; }
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Lịch gửi dạng JSON")]
        public string? ScheduleJson { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? Priority { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.SubSubscriptionState? State { get; set; }
        ///// <summary>Lý do đối tác từ chối (reject reason-cd).</summary>
        //[SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        //public string? RejectReason { get; set; }
        /// <summary>Lý do hủy đăng ký (cancel reason-cd).</summary>
        //[SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        //public string? CancelReason { get; set; }
        /// <summary>Thời điểm gửi yêu cầu đăng ký.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? RequestedAt { get; set; }
        /// <summary>Thời điểm được duyệt/từ chối.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? ResolvedAt { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EventSourceId { get; set; }

        /// <summary>Giãn cách tối thiểu giữa 2 lần gửi theo sự kiện khi Mode = EVENT (giây).</summary>
        [SugarColumn(IsNullable = true)]
        public int? DebounceSec { get; set; }

        /// <summary>
        /// Gửi ngay khi có dữ liệu mới
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Gửi ngay khi có dữ liệu mới")]
        public bool? SendOnNewData { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? NextTimeRun { get; set; }

        /// <summary>Mốc mà đăng ký đang được một worker xử lý tới. Null = đang rảnh. Vì sao tách khỏi NextTimeRun: xem MasterPlan §5.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? ProcessingUntil { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? LastTimeRun { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? IntervalSeconds { get; set; }

        // Đăng ký KHÔNG lưu liên kết tới hồ sơ ánh xạ.
        //
        // Cột MappingId cũ đã bỏ: luồng nghiệp vụ là đăng ký TRƯỚC — hồ sơ ánh xạ SAU, nên lúc tạo
        // đăng ký thì hồ sơ chưa tồn tại; sau đó hồ sơ còn bị bật, tắt, sửa, xoá ở màn Ánh xạ mà
        // không chạm vào đăng ký, nên mọi bản chụp đều sẽ lệch. Worker cũng chưa bao giờ đọc cột đó.
        //
        // Thay vào đó, tình trạng hồ sơ được SUY SỐNG theo (đối tác × gói tin × chiều):
        //   - worker suy lúc chạy,
        //   - WebAPI suy lúc truy vấn (ShareDataMappingResolver) và trả ra DTO dưới dạng
        //     MappingState / MappingCode / MappingName.
        // Muốn tra lịch sử hồ sơ đã dùng thật thì đọc ShareDataActivityLog.MappingId do worker ghi.
    }
}
