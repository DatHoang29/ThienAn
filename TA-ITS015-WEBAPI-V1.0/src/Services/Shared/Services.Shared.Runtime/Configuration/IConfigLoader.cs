namespace Services.Shared.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Loader cấu hình typed — tự biết đọc bảng/nội dung nào và trả về đúng class config.
/// Mỗi loại config (Queue, Api, Devices, ...) có một implementation riêng. Thêm config mới chỉ cần
/// thêm 1 class + đăng ký DI — không sửa code cũ (Open/Closed). Khi DB lỗi, trả về <see cref="DefaultValue"/>.
/// Created date: 17/07/2026
/// </summary>
public interface IConfigLoader<TConfig>
    where TConfig : class, new()
{
    /// <summary>Giá trị mặc định (thường lấy từ file) dùng khi DB không đọc được.</summary>
    TConfig DefaultValue { get; }

    /// <summary>Đọc cấu hình từ CSDL, trả về typed class.</summary>
    Task<TConfig> LoadAsync(CancellationToken cancellationToken);
}
