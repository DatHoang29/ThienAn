using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Its015;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Gói tin THÔ nhận về (luồng NHẬN)
    /// Created date: 15/09/2026
    /// Modified date: 27/10/2026 (thêm chỉ mục cho truy vấn của Worker: gói chờ xử lý theo đối tác × gói, gói kẹt, dọn gói cũ)
    /// </summary>
    [Tenant(Its015Const.ConnectionConst.ShareData)]
    [SugarTable("ShareDataInboundPacket")]
    [SugarIndex("index_{table}_State_Partner_Packet", nameof(ProcessState), OrderByType.Asc, nameof(PartnerCode), OrderByType.Asc, nameof(PacketCode), OrderByType.Asc, nameof(SerialNbr), OrderByType.Asc, nameof(CreateTime), OrderByType.Asc)]
    [SugarIndex("index_{table}_State_ProcessedAt", nameof(ProcessState), OrderByType.Asc, nameof(ProcessedAt), OrderByType.Asc)]
    [SugarIndex("index_{table}_CT", nameof(CreateTime), OrderByType.Asc)]
    public class ShareDataInboundPacket : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? PartnerCode { get; set; }

        [SugarColumn(IsNullable = true)]
        public string? PacketCode { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? PacketVersion { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? SerialNbr { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? PduType { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = DATA | 1 = FILE")]
        public BaseEnums.PublishFormat? Format { get; set; }

        //[SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        //public string? ContentType { get; set; }

        //[SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        //public string? Hash { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? ByteSize { get; set; }

        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Nội dung thô nhận được")]
        public string? RawContent { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 PENDING | 1 PROCESSING | 2 DONE | 3 FAILED | 4 NO_SUBSCRIPTION")]
        public BaseEnums.InboundProcessState? ProcessState { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ProcessedAt { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// ID của bản ghi nhật ký cha (ShareDataActivityLog.ID) bên CSDL chính.
        /// Cầu nối duy nhất mang định danh dòng cha từ WebAPI sang Worker qua ranh giới 2 CSDL.
        /// null với các gói tin cũ được tạo trước khi nâng cấp SV-8b.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ReceiveLogId { get; set; }
    }
}
