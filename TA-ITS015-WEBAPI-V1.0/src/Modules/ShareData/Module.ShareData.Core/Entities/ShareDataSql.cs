using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.ShareData.Core.Entities
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Câu SQL trích xuất và thiết lập SQL của gói tin (chiều gửi).
    ///              Dòng PartnerId trống là câu mẫu chung của gói; dòng có PartnerId là cấu hình riêng của đối tác.
    ///              Ở dòng đối tác, cột nào để trống thì lấy theo dòng mẫu chung (ExtractSql trống = dùng câu mẫu chung).
    ///              Mỗi cặp (gói tin × đối tác) có tối đa một dòng; xoá cứng khi đặt lại để chỉ mục duy nhất luôn đúng.
    ///              Danh sách trường của gói do chính câu SQL của dòng sinh ra (bộ dò cột), lưu ở FieldsJson cùng dòng.
    /// Created date: 24/10/2026
    /// Modified date: 24/10/2026 (Thêm FieldsJson, DescribedTime: danh sách trường sinh từ câu SQL)
    /// Modified date: 27/10/2026 (Gộp câu ghi chiều nhận vào dòng mẫu chung: WriteSql, AffectedTables, WriteRemark)
    /// Modified date: 27/10/2026 (Thêm VariablesJson: bộ biến của câu SQL)
    /// Modified date: 03/11/2026
    /// </summary>
    [SugarTable("ShareDataSql")]
    [SugarIndex("index_{table}_Packet_Partner", nameof(PacketId), OrderByType.Asc, nameof(PartnerId), OrderByType.Asc, true)]
    public class ShareDataSql : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID gói tin (ShareDataPacket.ID)")]
        public string? PacketId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID đối tác (ShareDataPartner.ID); trống = câu mẫu chung của gói")]
        public string? PartnerId { get; set; }

        [SugarColumn(ColumnDescription = "Câu lệnh SQL trích xuất; ở dòng đối tác trống = dùng câu mẫu chung", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
        public string? ExtractSql { get; set; }

        /// <summary>Mảng SqlFieldItem (JSON). Khoá, kiểu do bộ dò ghi; nhãn, bắt buộc, bỏ qua do người dùng khai. Dòng đối tác không có câu SQL riêng thì để trống.</summary>
        [SugarColumn(ColumnDescription = "Danh sách trường do câu SQL của dòng trả về (JSON); dòng đối tác không có SQL riêng thì trống", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
        public string? FieldsJson { get; set; }

        /// <summary>Mảng SqlVariableItem (JSON): bộ biến @tên khai cho câu SQL của dòng này. Dòng đối tác chưa khai riêng (null) thì kế thừa mẫu chung; "[]" là đã lưu rỗng.</summary>
        [SugarColumn(ColumnDescription = "Danh sách biến tham số của câu SQL (JSON); ở dòng đối tác null = chưa tách riêng", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
        public string? VariablesJson { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm dò cột trả về gần nhất")]
        public DateTime? DescribedTime { get; set; }

        /// <summary>Khối câu ghi chiều nhận (một hoặc nhiều câu MERGE / INSERT / UPDATE). Dòng mẫu chung: trống = gói chỉ gửi; dòng đối tác: câu ghi riêng của đối tác, trống = dùng câu ghi mẫu chung. Nên có OUTPUT $action để đếm được số bản ghi thêm / sửa.</summary>
        [SugarColumn(ColumnDescription = "Khối câu ghi chiều nhận; chỉ dùng ở dòng mẫu chung", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
        public string? WriteSql { get; set; }

        /// <summary>Danh sách bảng bị ảnh hưởng khi chạy khối câu ghi (phân tách bằng dấu phẩy).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024, ColumnDescription = "Danh sách bảng bị ảnh hưởng của khối câu ghi (phân tách bằng dấu phẩy)")]
        public string? AffectedTables { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Ghi chú của khối câu ghi chiều nhận")]
        public string? WriteRemark { get; set; }
    }
}
