using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Nhật ký hoạt động của phân hệ Chia sẻ dữ liệu
    /// Created date: 2026-08-05
    /// </summary>
    [SugarTable("ShareDataActivityLog")]
    [SugarIndex("index_{table}_OA", nameof(CreateTime), OrderByType.Desc)]
    [SugarIndex("index_{table}_ParentId", nameof(ParentId), OrderByType.Asc, nameof(StepNbr), OrderByType.Asc)]
    public class ShareDataActivityLog : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "CONFIG | TRANSFER")]
        public BaseEnums.LogTypeEnum? LogType { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "0 CREATE | 1 UPDATE | 2 DELETE | 3 CONNECT | 4 DISCONNECT | 5 PAUSE | 6 RESUME | 7 CANCEL | 8 APPROVE | 9 REJECT | 10 SEND | 11 RECEIVE | 12 EXPORT")]
        public BaseEnums.ActivityAction? Action { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? OccurredAt { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.SuccessEnums? Success { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Description { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ErrorMessage { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "0 Partner | 1 Subscription | 2 Session | 3 EventSource | 4 Packet | 5 Mapping | 6 CodeSet")]
        public BaseEnums.ShareDataTargetType? TargetType { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? TargetId { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? TargetName { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? PartnerName { get; set; }
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Giá trị trước thay đổi dạng JSON")]
        public string? BeforeJson { get; set; }
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Giá trị sau thay đổi dạng JSON")]
        public string? AfterJson { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ChangedFields { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SubscriptionId { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SessionId { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "SND | RCV")]
        public BaseEnums.TransferDirection? TransferDirection { get; set; }
        [SugarColumn(IsNullable = true)]
        public string? DatatypeId { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? MappingId { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? PacketVersion { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? SerialNbr { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? PacketNbr { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? PduType { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "0 = DATA | 1 = FILE")]
        public BaseEnums.PublishFormat? Format { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ByteSize { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? RecordCount { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? FilePath { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Hash { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OperatorName { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OperatorIp { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID dòng log cha. null = chính nó là dòng cha")]
        public string? ParentId { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "Thứ tự bước thực thi của dòng con trong một lượt gửi/nhận (xếp tăng dần theo số này). Gửi: 1 Trích xuất | 2 Ánh xạ & Gửi · Nhận: 1 Tiếp nhận | 2 Ánh xạ & Lưu · null = dòng cha")]
        public int? StepNbr { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<ShareDataActivityLog>? Children { get; set; }
    }
}
