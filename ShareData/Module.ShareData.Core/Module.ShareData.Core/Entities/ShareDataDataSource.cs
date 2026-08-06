using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Description: Nguồn dữ liệu nội bộ cấp cho luồng chia sẻ.
    ///              V1 chỉ hỗ trợ chọn cột (FIELD_PICKER) và truy vấn dựng sẵn đã duyệt (SAVED_QUERY).
    /// Created date: 2026-08-04
    /// </summary>
    [SugarTable("ShareDataDataSource")]
    public class ShareDataDataSource : EntityTenant
    {

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>Loại nguồn: FIELD_PICKER | SAVED_QUERY.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "FIELD_PICKER | SAVED_QUERY")]
        public string? Kind { get; set; }

        /// <summary>Tham chiếu kết nối CSDL (V1 mặc định 'SqlServer').</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? DbRef { get; set; }

        /// <summary>FIELD_PICKER: bảng/view nguồn.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? TableOrView { get; set; }

        /// <summary>FIELD_PICKER: danh sách cột chọn dạng JSON — [{ column, alias, dataType }].</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Danh sách cột nguồn dạng JSON")]
        public string? FieldsJson { get; set; }

        /// <summary>SAVED_QUERY: câu truy vấn/view dựng sẵn đã duyệt (KHÔNG phải raw SQL người dùng).</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString)]
        public string? QueryText { get; set; }

        /// <summary>Giới hạn số bản ghi khi xuất/xem trước.</summary>
        [SugarColumn(IsNullable = true)]
        public int? TopN { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
