using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Phiên kết nối đối tác
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataSession")]
    public class ShareDataSession : EntityTenant
    {
        /// <summary>Đối tác của phiên (ShareDataPartner.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }
        /// <summary>Chiều phiên — lưu số, xem BaseEnums.SessionDirection.</summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "0 = OUT | 1 = IN")]
        public BaseEnums.SessionDirection? Direction { get; set; }
        /// <summary>Trạng thái FSM — lưu số, xem BaseEnums.SessionFsmState.</summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "0 IDLE | 1 CONNECTING | 2 LOGIN_WAIT | 3 ACTIVE | 4 CLOSING | 5 CLOSED | 6 RECONNECT_WAIT | 7 REJECTED | 8 TIMEOUT")]
        public BaseEnums.SessionFsmState? State { get; set; }
        /// <summary>Thời điểm bắt đầu phiên.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? StartedAt { get; set; }
        /// <summary>Thời điểm đối tác Accept.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? AcceptedAt { get; set; }
        /// <summary>Thời điểm kết thúc phiên.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? EndedAt { get; set; }
        /// <summary>Lý do kết thúc (logout/terminate reason-cd).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? EndReason { get; set; }
        /// <summary>Số gói đã gửi.</summary>
        [SugarColumn(IsNullable = true)]
        public long? PacketsSent { get; set; }
        /// <summary>Số gói đã nhận.</summary>
        [SugarColumn(IsNullable = true)]
        public long? PacketsRecv { get; set; }
        /// <summary>Heartbeat gần nhất.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? LastHeartbeatAt { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
