using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Bản chụp gói tin ESHARE — sinh từ mã nguồn ([EshPacket] trên lớp PacketNNN),
    ///              Câu SQL trích xuất và danh sách trường của gói tin nằm ở ShareDataSql (FieldsJson).
    /// Created date: 2026-08-21
    /// Modified date: 27/10/2026 (DatHQ: bỏ TemplateSql, câu SQL chỉ còn một nguồn ShareDataSql)
    /// </summary>
    [SugarTable("ShareDataPacket")]
    [SugarIndex("index_{table}_CT", nameof(CreateTime), OrderByType.Asc)]
    public class ShareDataPacket : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Mã gói tin")]
        public new string? Code { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? PacketVersion { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Description { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
