using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Các trường kế thừa từ EntityTenant mà mọi bản ghi Video Wall đều trả về.
/// Đây là POCO thuần phía client — CỐ Ý không tham chiếu Module.VideoWall.Core để khỏi kéo
/// SqlSugar vào ứng dụng desktop; chỉ tái sử dụng HÌNH DẠNG dữ liệu, không tái sử dụng assembly.
///
/// Toàn bộ DTO trong thư mục này dựa vào JsonNamingPolicy.CamelCase (xem ApiJson) để khớp tên
/// trường trên đường truyền. Chỉ những tên mà quy tắc camelCase dễ gây nhầm mới khai báo
/// [JsonPropertyName] tường minh.
/// Created date: 25/08/2026
/// </summary>
public abstract class VwEntityDto
{
    /// <summary>Khóa chính (ID) định danh duy nhất của bản ghi.</summary>
    [JsonPropertyName("id")]
    public string? ID { get; set; }

    /// <summary>Mã nghiệp vụ của bản ghi.</summary>
    public string? Code { get; set; }

    /// <summary>Thời điểm tạo bản ghi.</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>ID người dùng tạo bản ghi.</summary>
    [JsonPropertyName("createUId")]
    public string? CreateUId { get; set; }

    /// <summary>Thời điểm cập nhật bản ghi gần nhất.</summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>ID người dùng cập nhật bản ghi gần nhất.</summary>
    [JsonPropertyName("updateUId")]
    public string? UpdateUId { get; set; }

    /// <summary>Trạng thái dòng dữ liệu.</summary>
    public string? RowStatus { get; set; }

    /// <summary>Mốc thời gian xoá mềm — null nghĩa là bản ghi còn hiệu lực (không phải bool).</summary>
    public DateTime? IsDelete { get; set; }

    /// <summary>ID đơn vị / khách thuê (TenantId) trong hệ thống đa người dùng.</summary>
    public string? TenantId { get; set; }
}
