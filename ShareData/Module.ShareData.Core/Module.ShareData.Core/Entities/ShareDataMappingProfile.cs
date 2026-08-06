using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Hồ sơ ánh xạ trường dữ liệu.
    ///              V1: mỗi (đối tác × gói tin × chiều) chỉ có DUY NHẤT 1 hồ sơ đang áp dụng,
    ///              map trực tiếp cột nguồn → key đối tác (OUT) hoặc key đối tác → trường nội bộ (IN).
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataMappingProfile")]
    public class ShareDataMappingProfile : EntityTenant
    {

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>Đối tác áp dụng (ShareDataPartner.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? VendorId { get; set; }

        /// <summary>Loại dữ liệu chia sẻ (101–111).</summary>
        [SugarColumn(IsNullable = true)]
        public int? DatatypeId { get; set; }

        /// <summary>Chiều ánh xạ: OUT | IN.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length8, ColumnDescription = "OUT | IN")]
        public string? Direction { get; set; }

        /// <summary>Nguồn dữ liệu nhúng (ShareDataDataSource.ID) — dùng khi chạy xuất chiều OUT.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? DataSourceId { get; set; }

        /// <summary>
        /// Danh sách dòng ánh xạ dạng JSON —
        /// [{ targetKey, sourceKey, expression, defaultValue, required }].
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Danh sách ánh xạ field dạng JSON")]
        public string? MappingsJson { get; set; }

        /// <summary>Đang áp dụng. Duy nhất 1 bản ghi active cho mỗi (đối tác × gói tin × chiều).</summary>
        [SugarColumn(IsNullable = true)]
        public bool? IsActive { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
