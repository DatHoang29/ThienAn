using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Bộ mã quy đổi giá trị giữa CSDL nội bộ và giá trị chuẩn theo đặc tả.
    /// Created date: 2026-08-21
    /// </summary>
    [SugarTable("ShareDataCodeSet")]
    [SugarIndex("index_{table}_CT", nameof(CreateTime), OrderByType.Desc)]
    public class ShareDataCodeSet : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status{ get; set; }
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Bảng quy đổi giá trị + giá trị mặc định, dạng JSON")]
        public string? ValuesJson { get; set; }

    }
}
