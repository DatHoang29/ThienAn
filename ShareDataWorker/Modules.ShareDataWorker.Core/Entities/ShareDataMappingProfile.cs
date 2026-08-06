using SqlSugar;
using Modules.ShareDataWorker.Core.Constants;

namespace Modules.ShareDataWorker.Core.Entities
{
    /// <summary>
    /// Hồ sơ ánh xạ trường dữ liệu.
    /// Author: Đạt
    /// Created date: 05/08/2026
    /// </summary>
    [SugarTable("ShareDataMappingProfile")]
    public class ShareDataMappingProfile : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Code { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>Đối tác áp dụng (ShareDataPartner.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? VendorId { get; set; }

        /// <summary>Loại dữ liệu chia sẻ (101–111).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? DatatypeId { get; set; }

        /// <summary>Chiều ánh xạ: OUT | IN.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length8, ColumnDescription = "OUT | IN")]
        public string? Direction { get; set; }

        /// <summary>Nguồn dữ liệu nhúng (ShareDataDataSource.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? DataSourceId { get; set; }

        /// <summary>Danh sách dòng ánh xạ dạng JSON.</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)", ColumnDescription = "Danh sách ánh xạ field dạng JSON")]
        public string? MappingsJson { get; set; }

        /// <summary>Đang áp dụng.</summary>
        [SugarColumn(IsNullable = true)]
        public bool? IsActive { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
