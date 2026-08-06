using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Đối tác chia sẻ dữ liệu (Trung tâm QG, TMC khác, CSGT...).
    ///              Chứa tham số kết nối C2C đàm phán khi Login (heartbeat/datagram/timeout/encoding).
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataPartner")]
    public class ShareDataPartner : EntityTenant
    {
        /// <summary>Tên đối tác — VD "Trung tâm QLĐHGT Quốc gia".</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>Địa chỉ IP/host của Exterior Connection Server.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Address { get; set; }

        /// <summary>Cổng TCP (1..65535).</summary>
        [SugarColumn(IsNullable = true)]
        public int? Port { get; set; }

        /// <summary>Hồ sơ mã hóa: ASN | XML_A.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "ASN | XML_A")]
        public string? ProtocolProfile { get; set; }

        /// <summary>Tên đăng nhập gửi trong gói Login.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Username { get; set; }

        /// <summary>
        /// Mật khẩu đã băm. Đánh dấu JsonIgnore ngay tại entity để mọi DTO kế thừa
        /// đều không lộ giá trị này ra FE (và FE cũng không gửi ngược lên được).
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? PasswordHash { get; set; }

        /// <summary>Bên khởi tạo kết nối: clientInitiated | serverInitiated.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "clientInitiated | serverInitiated")]
        public string? InitiatorMode { get; set; }

        /// <summary>Heartbeat tối đa (giây) — [5..300].</summary>
        [SugarColumn(IsNullable = true)]
        public int? HeartbeatMaxSec { get; set; }

        /// <summary>Kích thước datagram tối đa (byte) — [512..65535].</summary>
        [SugarColumn(IsNullable = true)]
        public int? DatagramSize { get; set; }

        /// <summary>Response timeout (giây) — [3..60].</summary>
        [SugarColumn(IsNullable = true)]
        public int? ResponseTimeoutSec { get; set; }

        /// <summary>Bật TLS cho kênh Internet.</summary>
        [SugarColumn(IsNullable = true)]
        public bool? UseTls { get; set; }

        /// <summary>Trạng thái cấu hình: CONFIGURED | ENABLED | DISABLED.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "CONFIGURED | ENABLED | DISABLED")]
        public string? Status { get; set; }

        /// <summary>Thứ tự hiển thị (mặc định 100).</summary>
        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
