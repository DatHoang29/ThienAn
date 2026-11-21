using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Modules.CCTV.Core.Entities
{
    [SugarTable("CctvTemplate")]
    public class CctvTemplate : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>
        /// Danh sách slot layout lưu dạng JSON string.
        /// Deserialize tại QueryHandler khi trả về Output.
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString)]
        public string? Item { get; set; }
    }
}