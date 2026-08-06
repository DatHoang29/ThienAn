using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Phiên kết nối C2C với đối tác. Trạng thái + bộ đếm gói được cập nhật realtime.
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataSession")]
    public class ShareDataSession : EntityTenant
    {
        /// <summary>Đối tác của phiên (ShareDataPartner.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }

        /// <summary>Chiều phiên: OUT | IN.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length8, ColumnDescription = "OUT | IN")]
        public string? Direction { get; set; }

        /// <summary>Trạng thái FSM: IDLE | CONNECTING | LOGIN_WAIT | ACTIVE | CLOSING | CLOSED | RECONNECT_WAIT | REJECTED | TIMEOUT.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? State { get; set; }

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

        /// <summary>RTT heartbeat (ms).</summary>
        [SugarColumn(IsNullable = true)]
        public int? HeartbeatRttMs { get; set; }

        /// <summary>Datagram size đã đàm phán khi Login.</summary>
        [SugarColumn(IsNullable = true)]
        public int? NegotiatedDatagramSize { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
