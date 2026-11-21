using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đối tác chia sẻ dữ liệu 
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataPartner")]
    public class ShareDataPartner : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Address { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? Port { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Endpoint URL")]
        public string? EndPointApiUrl { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.SessionState? SessionState { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
        //[SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        //public string? Username { get; set; }
        //[Newtonsoft.Json.JsonIgnore]
        //[System.Text.Json.Serialization.JsonIgnore]
        //[SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        //public string? PasswordHash { get; set; }
        //[SugarColumn(IsNullable = true)]
        //public int? HeartbeatMaxSec { get; set; }
        /// <summary>Kích thước datagram tối đa (byte) — [512..65535].</summary>
        //[SugarColumn(IsNullable = true)]
        //public int? DatagramSize { get; set; }
        /// <summary>Response timeout (giây) — [3..60].</summary>
        //[SugarColumn(IsNullable = true)]
        //public int? ResponseTimeoutSec { get; set; }
        /// <summary>
        /// URL API lấy gói tin của đối tác — dùng cho LUỒNG NHẬN (Direction = Inbound).
        /// Worker sẽ POST tới địa chỉ này để liệt kê và tải gói tin về.
        /// Để trống nghĩa là đối tác chưa cấp API, luồng nhận bỏ qua đối tác đó.
        /// </summary>
        ///<summary>Bật TLS cho kênh Internet.</summary>
        //[SugarColumn(IsNullable = true)]
        //public bool? UseTls { get; set; }
        /// <summary>
        /// Loại kênh kết nối đối tác (1: API, 2: File, 3: FTP, 4: FTPS, 5: Kafka).
        /// Trống (null) thì mặc định là ConnectionType.Api.
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Loại kênh kết nối (1: API, 2: File, 3: FTP, 4: FTPS, 5: Kafka)")]
        public ConnectionType? ConnectionType { get; set; }
        /// <summary>
        /// Cấu hình kết nối dạng JSON phẳng key-value theo ConnectKeys (xác thực, Format, TimeoutSeconds, mã hóa, DirectoryPath/FileNamePattern, Topic...).
        /// Không chứa Address/Port/EndPointApiUrl (đã là cột riêng).
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Cấu hình kết nối JSON động")]
        public string? ConnectionConfig { get; set; }
    }
}
