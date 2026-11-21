using Services.Vds.Shared.State;
using Services.Vds.Configuration;

namespace Services.Vds.Pipeline.Logging;

/// <summary>
/// Author: KienLT
/// Description: Ghi NGUYÊN VĂN mỗi frame nhận được ra file log riêng, tách theo ngày.
///
/// <b>Vì sao cần dù đã có DB</b>: pipeline loại bỏ khá nhiều thứ (frame sai JSON, camera lạ, field không
/// có cột tương ứng). Khi cần đối chiếu "thiết bị có gửi cái đó không", chỉ file thô này trả lời được.
///
/// KHÔNG tự xoá file cũ: đây là bản ghi để đối chiếu, việc dọn thuộc về vận hành. Lỗi ghi chỉ cảnh báo
/// và pipeline tiếp tục — mất một dòng log không được phép làm mất một bản ghi dữ liệu.
/// Created date: 26/08/2026
/// </summary>
public sealed class VdsRawLogger
{
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VdsRawLogger> _logger;
    private readonly object _sync = new();

    private int _writeFailedLogged;

    public VdsRawLogger(IVdsConfigManager config, ILogger<VdsRawLogger> logger)
    {
        _config = config;
        _logger = logger;
    }

    public void Write(string? rawFrame)
    {
        if (string.IsNullOrWhiteSpace(rawFrame)) return;

        var ingestion = _config.Current.Ingestion;
        if (!ingestion.RawLogEnabled) return;

        try
        {
            // Một dòng một frame: gộp mọi ký tự xuống dòng trong payload lại để mỗi dòng file đúng là
            // một bản tin, nếu không thì không grep/đếm được.
            var line = rawFrame.ReplaceLineEndings(" ");
            var directory = StatePath.Resolve(ingestion.RawLogDirectory);

            lock (_sync)
            {
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, $"vds-raw-{DateTime.UtcNow:yyyyMMdd}.log");
                File.AppendAllText(path, line + Environment.NewLine);
            }

            Interlocked.Exchange(ref _writeFailedLogged, 0);
        }
        catch (Exception ex)
        {
            // Chỉ cảnh báo MỘT lần cho mỗi đợt lỗi: đĩa đầy/không có quyền sẽ lặp lại trên mọi frame.
            if (Interlocked.Exchange(ref _writeFailedLogged, 1) == 0)
                _logger.LogWarning(ex, "[RawLog] Ghi log frame thô thất bại; pipeline vẫn tiếp tục.");
        }
    }
}
