using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Cảnh báo và lỗi của phân hệ Chia sẻ dữ liệu
    /// Created date: 2026-08-21
    /// </summary>
    [SugarTable("ShareDataAlertLog")]
    [SugarIndex("index_{table}_OA", nameof(OccurredAt), OrderByType.Desc)]
    public class ShareDataAlertLog : EntityTenant
    {
        /// <summary>Thời điểm phát sinh cảnh báo.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? OccurredAt { get; set; }
        /// <summary>Mức độ cảnh báo — lưu số, xem BaseEnums.AlertSeverity.</summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "0 = warning | 1 = error")]
        public BaseEnums.AlertSeverity? Severity { get; set; }
        /// <summary>Nguồn phát cảnh báo — lưu số, xem BaseEnums.AlertSource.</summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "0 Session | 1 Packet | 2 Subscription | 3 Protocol | 4 Funnel")]
        public BaseEnums.AlertSource? AlertSource { get; set; }

        /// <summary>Mã cảnh báo dạng ESH-xxxx — tra cứu nguyên nhân và cách xử lý.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "ESH-xxxx")]
        public string? AlertCode { get; set; }

        /// <summary>Đối tác liên quan (ShareDataPartner.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }

        /// <summary>Phiên kết nối liên quan (ShareDataSession.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SessionId { get; set; }

        /// <summary>Đăng ký chia sẻ liên quan (ShareDataSubscription.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SubscriptionId { get; set; }

        /// <summary>Gói tin liên quan (= ShareDataPacket.Code) — Length32 để khớp kiểu khi join.</summary>
        [SugarColumn(IsNullable = true)]
        public string? DatatypeId { get; set; }

        /// <summary>Nội dung cảnh báo tiếng Việt, dựng sẵn để hiển thị thẳng lên lưới.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Message { get; set; }

        /// <summary>Chi tiết bổ sung dạng JSON — ngữ cảnh để dò lỗi (field thiếu, phản hồi đối tác...).</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Chi tiết cảnh báo dạng JSON")]
        public string? DetailJson { get; set; }

        /// <summary>Đã xác nhận xử lý.</summary>
        [SugarColumn(IsNullable = true)]
        public bool? Acknowledged { get; set; }

        /// <summary>Người xác nhận (userId).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? AckBy { get; set; }

        /// <summary>Thời điểm xác nhận.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? AckAt { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
