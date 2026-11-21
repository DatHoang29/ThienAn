using System.Text.Json;
using Services.Vds.Shared.State;

namespace Services.Vds.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Lưu các tham số đã đổi LÚC CHẠY ra file, và đọc lại lúc khởi động.
///
/// <b>Đây là điểm khác chính so với bản cũ.</b> <c>ITS.VDS.Core.VdsConfigManager</c> giữ danh sách
/// override trong một <c>ConcurrentDictionary</c> ở RAM: đẩy tham số xuống thì có hiệu lực ngay, nhưng
/// service restart là mất hết và âm thầm quay về giá trị trong appsettings. Người vận hành không có cách
/// nào biết, chỉ thấy "hôm nay số liệu khác hôm qua".
///
/// File nội dung là <c>{ "TênTham số": "<giá trị JSON>" }</c> — giữ nguyên văn JSON của bên gửi thay vì
/// parse ra rồi serialize lại, để lúc phát lại (<c>ReapplyOverrides</c>) đi qua đúng một đường xử lý
/// với lúc nhận. Một chỗ parse thì không có hai hành vi khác nhau.
/// Created date: 24/08/2026
/// </summary>
public sealed class ConfigOverrideStore
{
    /// <summary>Tên file trong thư mục state.</summary>
    public const string FileName = "config-overrides.json";

    private readonly string _path;
    private readonly ILogger<ConfigOverrideStore> _logger;

    public ConfigOverrideStore(string stateDirectory, ILogger<ConfigOverrideStore> logger)
    {
        _path = StatePath.Resolve(stateDirectory, FileName);
        _logger = logger;
    }

    /// <summary>Đường dẫn file — log ra để người vận hành biết chỗ mà xem/xoá.</summary>
    public string Path => _path;

    /// <summary>
    /// Đọc override đã lưu. File không có / hỏng ⇒ trả dictionary rỗng, KHÔNG ném lỗi:
    /// mất file override chỉ làm service quay về cấu hình file, không phải lý do để không khởi động được.
    /// </summary>
    public Dictionary<string, string> Load()
    {
        var empty = new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            if (!File.Exists(_path)) return empty;

            var content = File.ReadAllText(_path);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
            if (parsed is null or { Count: 0 }) return empty;

            // Bỏ tham số không còn được hỗ trợ (đổi tên, bỏ tính năng) — giữ lại chỉ để ném lỗi về sau.
            var kept = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (name, json) in parsed)
            {
                if (ConfigParamApplier.SupportedNames.Contains(name)) kept[name] = json;
                else _logger.LogWarning(
                    "[Config] Bỏ override '{Name}' trong {Path}: tham số này không còn được hỗ trợ.",
                    name, _path);
            }

            _logger.LogInformation("[Config] Đọc {Count} override đã lưu từ {Path}.", kept.Count, _path);
            return kept;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Config] File override {Path} không đọc được; bỏ qua và dùng cấu hình từ appsettings.", _path);
            return empty;
        }
    }

    /// <summary>
    /// Ghi lại toàn bộ override. Lỗi ghi CHỈ log cảnh báo — tham số đã áp vào snapshot rồi, lượt chạy này
    /// vẫn đúng; hậu quả duy nhất là lần restart sau mất tham số đó.
    /// </summary>
    public void Save(IReadOnlyDictionary<string, string> overrides)
    {
        try
        {
            var json = JsonSerializer.Serialize(overrides, ConfigParamApplier.FileWriteOptions);
            StatePath.WriteAtomic(_path, json);
            _logger.LogDebug("[Config] Đã lưu {Count} override ra {Path}.", overrides.Count, _path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Config] Không ghi được override ra {Path}; tham số vẫn có hiệu lực nhưng sẽ MẤT sau khi " +
                "restart.", _path);
        }
    }
}
