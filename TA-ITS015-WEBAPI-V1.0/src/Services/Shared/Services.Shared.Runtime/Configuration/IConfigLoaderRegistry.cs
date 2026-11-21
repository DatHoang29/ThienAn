namespace Services.Shared.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Registry điều phối các typed config loader. Consumer gọi LoadAsync&lt;TConfig&gt;()
/// và nhận đúng kiểu — registry tự resolve IConfigLoader&lt;TConfig&gt; tương ứng từ DI.
/// Created date: 17/07/2026
/// </summary>
public interface IConfigLoaderRegistry
{
    /// <summary>Đọc cấu hình typed. Nếu không có loader hoặc loader lỗi → trả về mặc định của kiểu.</summary>
    Task<TConfig> LoadAsync<TConfig>(CancellationToken cancellationToken)
        where TConfig : class, new();
}
