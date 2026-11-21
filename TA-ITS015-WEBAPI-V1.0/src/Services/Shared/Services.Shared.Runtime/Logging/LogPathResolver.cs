using System.Text.RegularExpressions;

namespace Services.Shared.Logging;

/// <summary>
/// Author: TAC Team
/// Description: Helper resolve PathTemplate (kiểu NLog) thành path file thực. Xử lý placeholder
/// <c>${basedir}</c>, <c>${shortdate}</c>, <c>${kind}</c> + nối relative/absolute vào BaseDirectory.
/// Created date: 20/07/2026
/// </summary>
internal static partial class LogPathResolver
{
    // Match ${name} không phân biệt hoa thường. Partial method + [GeneratedRegex] để tránh reflection regex.
    [GeneratedRegex(@"\$\{(.*?)\}", RegexOptions.IgnoreCase)]
    private static partial Regex PlaceholderRegex();

    /// <summary>
    /// Resolve template thành path file tương đối (chưa nối BaseDirectory). Trả về path có thể chứa
    /// subfolder (vd. <c>LogFile/2026-07-20/nats.log</c>).
    /// </summary>
    /// <param name="template">Template từ config (vd. <c>"LogFile/${shortdate}/${kind}.log"</c>).</param>
    /// <param name="kind">Tên kind (vd. "Nats") → <c>${kind}</c> thành "nats" viết thường.</param>
    /// <param name="timestamp">Thời gian log entry → <c>${shortdate}</c> thành <c>yyyy-MM-dd</c>.</param>
    /// <returns>Path đã resolve placeholder, thư mục separator theo OS.</returns>
    public static string ResolveTemplate(string template, string kind, DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(template))
            template = LoggingKindDefaultsDefaultPath;

        var kindLower = (kind ?? string.Empty).ToLowerInvariant();
        return PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value.Trim().ToLowerInvariant();
            return key switch
            {
                "shortdate" => timestamp.LocalDateTime.ToString("yyyy-MM-dd"),
                "date" => timestamp.LocalDateTime.ToString("yyyy-MM-dd"),
                "kind" => kindLower,
                "basedir" => match.Value, // giữ nguyên — caller thay sau khi biết BaseDirectory
                "year" => timestamp.LocalDateTime.ToString("yyyy"),
                "month" => timestamp.LocalDateTime.ToString("MM"),
                "day" => timestamp.LocalDateTime.ToString("dd"),
                _ => match.Value // placeholder không nhận diện — giữ nguyên để dễ debug
            };
        });
    }

    /// <summary>
    /// Nối path (sau khi resolve placeholder) vào BaseDirectory. Nếu path là absolute, giữ nguyên.
    /// </summary>
    /// <param name="resolvedPath">Path đã resolve (có thể relative hoặc absolute).</param>
    /// <param name="baseDirectory">Thư mục gốc. Nếu chuỗi literal "${basedir}" → resolve thành AppContext.BaseDirectory.</param>
    public static string CombineWithBase(string resolvedPath, string baseDirectory)
    {
        // Normalize separator về OS.
        resolvedPath = resolvedPath.Replace('/', Path.DirectorySeparatorChar)
                                   .Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(resolvedPath))
            return resolvedPath;

        // Resolve BaseDirectory literal.
        var baseDir = string.Equals(baseDirectory, "${basedir}", StringComparison.OrdinalIgnoreCase)
            ? AppContext.BaseDirectory
            : baseDirectory;

        if (string.IsNullOrWhiteSpace(baseDir))
            baseDir = AppContext.BaseDirectory;

        return Path.Combine(baseDir, resolvedPath);
    }

    /// <summary>
    /// Tách path thành (directory, fileNameKhôngExtension, extension) để chèn suffix sequence khi roll size.
    /// Vd. <c>"C:\\logs\\TACNatsLog.txt"</c> → <c>("C:\\logs", "TACNatsLog", ".txt")</c>.
    /// </summary>
    public static (string directory, string nameWithoutExt, string extension) SplitPath(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? string.Empty;
        var fileName = Path.GetFileName(path);
        var ext = Path.GetExtension(fileName); // bao gồm dấu chấm, vd. ".txt"
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        return (dir, nameWithoutExt, ext);
    }

    /// <summary>
    /// Sinh path file với suffix sequence khi roll size. Vd. base <c>TACNatsLog.txt</c> + sequence 1 →
    /// <c>TACNatsLog.1.txt</c> (chèn .N trước extension). Sequence 0 → không chèn (file gốc).
    /// </summary>
    public static string AppendSequence(string basePath, int sequence)
    {
        if (sequence <= 0)
            return basePath;

        var (dir, name, ext) = SplitPath(basePath);
        var newFileName = $"{name}.{sequence}{ext}";
        return string.IsNullOrEmpty(dir) ? newFileName : Path.Combine(dir, newFileName);
    }

    /// <summary>Path template mặc định (sync với <see cref="LoggingKindDefaults.PathTemplate"/>).</summary>
    private const string LoggingKindDefaultsDefaultPath = "logs/${shortdate}/${kind}.log";
}
