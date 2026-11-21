using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Core.Entities
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Bảng nội dung mặc định VMS theo sự kiện sự cố (không gắn theo thiết bị).
    ///              Chỉ lưu RowData + Url + cờ loại nội dung (text/image), kèm DefaultConfig (JSON tổng hợp).
    /// Created date: 15/07/2026
    /// </summary>
    [SugarTable("VmsEventDefault", TableDescription = "Nội dung mặc định VMS theo sự kiện sự cố")]
    public class VmsEventDefault : EntityTenant
    {
        /// <summary>
        /// Khóa liên kết loại sự kiện sự cố (TmsEventType.ID)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Mã loại sự kiện sự cố (TmsEventType)")]
        public string? EventTypeId { get; set; }

        /// <summary>
        /// Khóa liên kết loại thiết bị VMS hoặc LCS (TmsEquipmentType.ID)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Mã loại thiết bị VMS hoặc LCS (TmsEquipmentType)")]
        public string? EquipmentTypeId { get; set; }

        /// <summary>
        /// Kích thước bảng: VMS mặc định 384x64, LCS cố định 128x128
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Kích thước bảng (vd 384x64, 128x128)")]
        public string? Size { get; set; }

        /// <summary>
        /// Tên nội dung mặc định
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Tên nội dung mặc định")]
        public string? Name { get; set; }

        /// <summary>
        /// JSON nội dung 2 dòng: { "Row1": "...", "Row2": "..." }
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024, ColumnDescription = "Nội dung 2 dòng (JSON)")]
        public string? RowData { get; set; }

        /// <summary>
        /// Đường dẫn tương đối ảnh đã sinh
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Đường dẫn ảnh đã sinh")]
        public string? Url { get; set; }

        /// <summary>
        /// Font chữ
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Font chữ")]
        public string? Font { get; set; }

        /// <summary>
        /// Màu chữ
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Màu chữ")]
        public string? Color { get; set; }

        /// <summary>
        /// Màu nền
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Màu nền")]
        public string? BackColor { get; set; }

        /// <summary>
        /// JSON cấu hình tổng hợp sau khi lưu (nội dung text, đường dẫn ảnh, font, màu, cỡ chữ, loại nội dung, cờ dùng cho sự kiện...)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Cấu hình tổng hợp (JSON)")]
        public string? DefaultConfig { get; set; }

        /// <summary>
        /// Chế độ phát mặc định khi sự cố kích hoạt (override: ghi đè, append: nối tiếp)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Chế độ phát mặc định (override/append)")]
        public string? DefaultPlayMode { get; set; } = "override";

        /// <summary>
        /// Thời gian hiển thị (giây) khi phát nối tiếp trong vòng lặp sự cố
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian hiển thị (giây) khi lặp nối tiếp")]
        public int? TimeToLive { get; set; } = 10;
    }
}
