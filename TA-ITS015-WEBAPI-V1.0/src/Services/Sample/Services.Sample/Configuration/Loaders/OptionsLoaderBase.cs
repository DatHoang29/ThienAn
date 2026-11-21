using Microsoft.Extensions.Configuration;

namespace Services.Sample.Configuration.Loaders;

/// <summary>
/// Author: TAC Team
/// Description: Base mỏng cho các typed config loader đọc một section từ file appsettings (live IConfiguration).
/// Mỗi loader kế thừa, khai báo section path và bind về typed options qua <see cref="BindSection{TOptions}"/>.
/// Không phụ thuộc CSDL — sẵn sàng mở rộng để merge override từ bảng cấu hình khi được chỉ định sau này.
/// Created date: 17/07/2026
/// </summary>
public abstract class OptionsLoaderBase(IConfiguration configuration, string sectionPath)
{
    /// <summary>Bind section hiện tại từ file về typed options. Đọc live nên phản ánh mỗi lần reload.</summary>
    protected TOptions BindSection<TOptions>() where TOptions : class, new() =>
        configuration.GetSection(sectionPath).Get<TOptions>() ?? new TOptions();
}
