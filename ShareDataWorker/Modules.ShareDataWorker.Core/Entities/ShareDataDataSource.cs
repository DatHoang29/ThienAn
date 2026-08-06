using SqlSugar;
using Modules.ShareDataWorker.Core.Constants;

namespace Modules.ShareDataWorker.Core.Entities
{
    /// <summary>
    /// Nguồn dữ liệu nội bộ cấp cho luồng chia sẻ.
    /// Author: Đạt
    /// Created date: 05/08/2026
    /// </summary>
    [SugarTable("ShareDataDataSource")]
    public class ShareDataDataSource : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Code { get; set; }

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

        /// <summary>FIELD_PICKER: danh sách cột chọn dạng JSON.</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)", ColumnDescription = "Danh sách cột nguồn dạng JSON")]
        public string? FieldsJson { get; set; }

        /// <summary>SAVED_QUERY: câu truy vấn/view dựng sẵn đã duyệt.</summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
        public string? QueryText { get; set; }

        /// <summary>Giới hạn số bản ghi khi xuất/xem trước.</summary>
        [SugarColumn(IsNullable = true)]
        public int? TopN { get; set; } = 50;

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
