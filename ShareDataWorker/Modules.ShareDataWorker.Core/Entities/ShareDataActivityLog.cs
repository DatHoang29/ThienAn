using SqlSugar;
using Modules.ShareDataWorker.Core.Constants;

namespace Modules.ShareDataWorker.Core.Entities
{
    /// <summary>
    /// Nhật ký hoạt động của phân hệ Chia sẻ dữ liệu.
    /// Author: Đạt
    /// Created date: 05/08/2026
    /// </summary>
    [SugarTable("ShareDataActivityLog")]
    [SugarIndex("index_{table}_LT", nameof(LogType), OrderByType.Asc)]
    [SugarIndex("index_{table}_OA", nameof(OccurredAt), OrderByType.Desc)]
    [SugarIndex("index_{table}_PI", nameof(PartnerId), OrderByType.Asc)]
    public class ShareDataActivityLog : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "CONFIG | TRANSFER")]
        public string? LogType { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Action { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? OccurredAt { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "SUCCESS | FAILED")]
        public string? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Description { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ErrorMessage { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? TargetType { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? TargetId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? TargetName { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PartnerId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? PartnerName { get; set; }

        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
        public string? BeforeJson { get; set; }

        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
        public string? AfterJson { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ChangedFields { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SubscriptionId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? SessionId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length8, ColumnDescription = "SND | RCV")]
        public string? TransferDirection { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? DatatypeId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? SerialNbr { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? PacketNbr { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? PduType { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length8)]
        public string? Format { get; set; }

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
    }
}
