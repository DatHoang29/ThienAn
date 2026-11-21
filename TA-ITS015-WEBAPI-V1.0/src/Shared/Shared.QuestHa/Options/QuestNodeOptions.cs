namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình một node QuestDB (tên, chuỗi kết nối PG wire, độ ưu tiên đọc).
/// Created date: 09/10/2026
/// </summary>
public sealed class QuestNodeOptions
{
    /// <summary>Tên node, vd <c>A</c>, <c>B</c>. Là khoá trong QuestNodeCursor/QuestNodeStatus.</summary>
    public string Name { get; set; } = "";

    /// <summary>PG wire, BẮT BUỘC có <c>ServerCompatibilityMode=NoTypeLoading</c> (tự thêm nếu thiếu).</summary>
    public string PgConnectionString { get; set; } = "";

    /// <summary>Số nhỏ = ưu tiên đọc hơn (node "chính" của router).</summary>
    public int Priority { get; set; }

    /// <summary>Cổng HTTP REST của node (vd <c>http://host:9000</c>) cho <c>/exp</c> khi xuất Parquet; rỗng ⇒ node không được chọn để xuất.</summary>
    public string HttpUrl { get; set; } = "";

    /// <summary>Tài khoản Basic auth của HTTP REST (chỉ khi node bật xác thực HTTP).</summary>
    public string HttpUser { get; set; } = "";

    /// <summary>Mật khẩu Basic auth — cấp qua biến môi trường/cấu hình bí mật, không ghi trong file, không bao giờ log.</summary>
    public string HttpPassword { get; set; } = "";

    /// <summary>
    /// Thư mục <c>cairo.sql.copy.root</c> của node nhìn từ host chạy lệnh (đường dẫn mà QuestDB node đọc được bằng <c>read_parquet</c>);
    /// nạp ngược chép file vào đây nếu thư mục ngày không nằm sẵn trong đó.
    /// </summary>
    public string ParquetCopyRoot { get; set; } = "";
}
