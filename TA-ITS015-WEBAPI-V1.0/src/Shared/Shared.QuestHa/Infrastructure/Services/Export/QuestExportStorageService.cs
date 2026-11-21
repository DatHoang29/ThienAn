using System.Runtime.InteropServices;
using System.Text.Json;
using Shared.QuestHa.Infrastructure.Services.Export.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Export;

/// <summary>
/// Author: KienLT
/// Description: Thao tác file của bản xuất Parquet. Thư mục ngày chứa các thư mục PHIÊN BẢN <c>vNNNN</c>; phiên bản có manifest là bản đã
/// công bố và không bao giờ bị sửa (xuất lại ghi phiên bản mới), phiên bản chưa có manifest là bản nháp/dở. Ghi nguyên tử (file tạm + flush
/// + đổi tên), tạo cây thư mục dưới một gốc với kiểm reparse point/symlink ở MỌI cấp, xoá chỉ thư mục/tệp đúng mẫu tên do bản xuất tạo
/// (chứng từ sở hữu), đo dung lượng trống.
/// Created date: 02/11/2026
/// </summary>
internal static class QuestExportStorageService
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>Manifest của thư mục, null khi chưa có (bản dở/chưa xuất). JSON hỏng ⇒ <see cref="InvalidDataException"/>; manifest là link ⇒ <see cref="IOException"/>.</summary>
    public static QuestExportManifestOutput? ReadManifest(string folder)
    {
        var path = Path.Combine(folder, QuestExportDayRules.ManifestFile);
        if (!File.Exists(path)) return null;
        RejectLinks(path);
        try
        {
            return JsonSerializer.Deserialize<QuestExportManifestOutput>(File.ReadAllText(path))
                ?? throw new InvalidDataException("manifest.json rỗng.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("manifest.json không đọc được.", ex);
        }
    }

    /// <summary>Ghi manifest (công bố phiên bản): file tạm, flush xuống đĩa, đổi tên (nguyên tử trên cùng ổ).</summary>
    public static Task WriteManifestAsync(string folder, QuestExportManifestOutput manifest, CancellationToken ct) =>
        WriteAtomicAsync(Path.Combine(folder, QuestExportDayRules.ManifestFile), JsonSerializer.SerializeToUtf8Bytes(manifest, Json), ct);

    /// <summary>Ghi nội dung nhỏ (schema.sql, manifest, tiến độ) nguyên tử.</summary>
    public static async Task WriteAtomicAsync(string path, byte[] content, CancellationToken ct)
    {
        var temp = path + QuestExportDayRules.TempSuffix;
        await using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, FileOptions.Asynchronous))
        {
            await file.WriteAsync(content, ct).ConfigureAwait(false);
            await file.FlushAsync(ct).ConfigureAwait(false);
            file.Flush(flushToDisk: true);
        }
        File.Move(temp, path, overwrite: true);
    }

    /// <summary>
    /// Tạo (nếu chưa có) thư mục <paramref name="target"/> nằm TRONG <paramref name="root"/>: gốc và các cấp cha của gốc không là reparse
    /// point; MỌI cấp từ gốc xuống đích (kể cả thư mục đã có sẵn) không là reparse point/symlink và không là file. Vi phạm ⇒
    /// <see cref="IOException"/> trước mọi thao tác ghi bên dưới. <paramref name="mustBeNew"/> = true ⇒ đích đã tồn tại cũng là lỗi
    /// (thư mục phiên làm việc riêng). <paramref name="createRoot"/> = false ⇒ gốc phải có sẵn.
    /// </summary>
    public static string EnsureDirectoryUnder(string root, string target, bool mustBeNew = false, bool createRoot = true)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(target));
        if (!QuestExportRetentionRules.InsideRoot(full, fullRoot)) throw new IOException("Đường dẫn nằm ngoài thư mục gốc cho phép.");
        if (!Directory.Exists(fullRoot) && !createRoot) throw new IOException("Thư mục gốc chưa tồn tại.");
        RejectLinks(fullRoot);
        Directory.CreateDirectory(fullRoot);
        RejectLinks(fullRoot);
        var current = fullRoot;
        var parts = Path.GetRelativePath(fullRoot, full).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            current = Path.Combine(current, parts[i]);
            if (File.Exists(current)) throw new IOException("Đường dẫn đích trùng một file.");
            if (IsLink(current)) throw new IOException("Không thao tác qua reparse point/symlink trong cây đích.");
            if (Directory.Exists(current))
            {
                if (mustBeNew && i == parts.Length - 1) throw new IOException("Thư mục phiên đã tồn tại.");
                continue;
            }
            Directory.CreateDirectory(current);
            // Kiểm lại sau khi tạo: thư mục bị thay bằng link giữa hai bước thì dừng.
            if (IsLink(current)) throw new IOException("Không thao tác qua reparse point/symlink trong cây đích.");
        }
        return full;
    }

    /// <summary>Đường dẫn đang là reparse point/symlink (file hoặc thư mục; không đi theo link). Không tồn tại ⇒ false.</summary>
    public static bool IsLink(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    /// <summary>Không thao tác qua reparse point/symlink: bản thân <paramref name="path"/> và mọi thư mục cha.</summary>
    public static void RejectLinks(string path)
    {
        if (IsLink(path)) throw new IOException("Không thao tác qua reparse point/symlink.");
        QuestBackupStorageService.RejectLinks(path);
    }

    /// <summary>
    /// Các phiên bản trong thư mục ngày (tăng dần). Thư mục ngày chưa có ⇒ rỗng. Phiên bản là link ⇒ <see cref="IOException"/>.
    /// </summary>
    public static IReadOnlyList<(int Version, string Folder, bool Published)> Versions(string dayFolder)
    {
        if (!Directory.Exists(dayFolder)) return Array.Empty<(int, string, bool)>();
        var result = new List<(int, string, bool)>();
        foreach (var dir in Directory.EnumerateDirectories(dayFolder))
        {
            if (!QuestExportDayRules.TryParseVersion(Path.GetFileName(dir), out var version)) continue;
            if (IsLink(dir)) throw new IOException("Thư mục phiên bản là reparse point/symlink.");
            result.Add((version, dir, File.Exists(Path.Combine(dir, QuestExportDayRules.ManifestFile))));
        }
        return result.OrderBy(v => v.Item1).ToList();
    }

    /// <summary>
    /// Thư mục chứa manifest cần dùng khi chỉ biết một thư mục: thư mục phiên bản <c>vNNNN</c> có manifest ⇒ chính nó; thư mục ngày ⇒
    /// phiên bản đã công bố mới nhất bên dưới, chỉ khi chưa có phiên bản công bố nào mới dùng manifest phẳng kiểu cũ; null khi không có.
    /// </summary>
    public static string? ResolvePublished(string folder)
    {
        var hasManifest = File.Exists(Path.Combine(folder, QuestExportDayRules.ManifestFile));
        if (hasManifest && QuestExportDayRules.TryParseVersion(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)), out _)) return folder;
        // Bản phẳng cũ có thể nằm cạnh phiên bản mới (lượt xuất lại không xoá nó): phiên bản công bố mới nhất luôn thắng.
        return Versions(folder).LastOrDefault(v => v.Published).Folder ?? (hasManifest ? folder : null);
    }

    /// <summary>Dọn file tạm (<c>*.tmp</c>) của lần thử trước trong thư mục phiên bản nháp.</summary>
    public static void CleanTemp(string folder)
    {
        foreach (var temp in Directory.EnumerateFiles(folder, "*" + QuestExportDayRules.TempSuffix))
            File.Delete(temp);
    }

    /// <summary>
    /// Xoá một thư mục phiên bản trong <paramref name="dayFolder"/>: chỉ khi tên đúng mẫu <c>vNNNN</c>, nằm ngay dưới thư mục ngày, không
    /// link, chỉ chứa file do bản xuất tạo. Trả false (không xoá) khi vi phạm; không tồn tại ⇒ true.
    /// </summary>
    public static bool DeleteVersion(string versionFolder, string dayFolder)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(versionFolder));
        if (!string.Equals(Path.GetDirectoryName(full), Path.TrimEndingDirectorySeparator(Path.GetFullPath(dayFolder)), StringComparison.OrdinalIgnoreCase)
            || !QuestExportDayRules.TryParseVersion(Path.GetFileName(full), out _)) return false;
        if (!Directory.Exists(full)) return true;
        try { RejectLinks(full); }
        catch (IOException) { return false; }
        foreach (var entry in Directory.EnumerateFileSystemEntries(full))
            if (IsLink(entry) || Directory.Exists(entry) || !QuestExportDayRules.IsOwnedFileName(Path.GetFileName(entry))) return false;
        Directory.Delete(full, recursive: true);
        return true;
    }

    /// <summary>
    /// Xoá thư mục ngày hết hạn (mọi phiên bản). Trả false (không xoá) khi: ngoài root, có reparse point/symlink ở thư mục, cha hoặc bên
    /// trong, hoặc có mục không do bản xuất tạo (chứng từ sở hữu: chỉ thư mục <c>vNNNN</c> và file đúng mẫu tên). Không tồn tại ⇒ true.
    /// </summary>
    public static bool DeleteDay(string folder, string root)
    {
        if (!QuestExportRetentionRules.InsideRoot(folder, root) || IsLink(folder)) return false;
        if (!Directory.Exists(folder)) return true;
        try { RejectLinks(folder); }
        catch (IOException) { return false; }
        foreach (var entry in Directory.EnumerateFileSystemEntries(folder))
        {
            if (IsLink(entry)) return false;
            var name = Path.GetFileName(entry);
            if (!Directory.Exists(entry))
            {
                if (!QuestExportDayRules.IsOwnedFileName(name)) return false; // file phẳng của bản cũ không phiên bản
                continue;
            }
            if (!QuestExportDayRules.TryParseVersion(name, out _)) return false;
            foreach (var inner in Directory.EnumerateFileSystemEntries(entry))
                if (IsLink(inner) || Directory.Exists(inner) || !QuestExportDayRules.IsOwnedFileName(Path.GetFileName(inner))) return false;
        }
        Directory.Delete(folder, recursive: true);
        return true;
    }

    /// <summary>
    /// Xoá thư mục phiên chép tạm khi nạp ngược: chỉ khi nằm trong <paramref name="copyRoot"/>, không link và chỉ chứa file giờ
    /// <c>HH.parquet</c> không link (đúng những gì phiên đó chép vào). Không tồn tại ⇒ true; vi phạm hoặc lỗi I/O ⇒ false (không xoá).
    /// </summary>
    public static bool DeleteImportCopy(string session, string copyRoot)
    {
        if (!QuestExportRetentionRules.InsideRoot(session, copyRoot) || IsLink(session)) return false;
        if (!Directory.Exists(session)) return true;
        try
        {
            RejectLinks(session);
            foreach (var entry in Directory.EnumerateFileSystemEntries(session))
                if (IsLink(entry) || Directory.Exists(entry) || !QuestExportDayRules.IsOwnedFileName(Path.GetFileName(entry))) return false;
            Directory.Delete(session, recursive: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }

    /// <summary>Dung lượng trống của ổ chứa <paramref name="path"/> (thư mục cha gần nhất đã tồn tại); null nếu không đo được.</summary>
    public static long? FreeBytes(string path)
    {
        var probe = Path.GetFullPath(path);
        while (!Directory.Exists(probe))
        {
            var parent = Path.GetDirectoryName(probe);
            if (parent is null || parent == probe) return null;
            probe = parent;
        }
        if (OperatingSystem.IsWindows())
            return GetDiskFreeSpaceEx(probe, out var available, out _, out _) ? (long)available : null;
        try { return new DriveInfo(probe).AvailableFreeSpace; }
        catch (ArgumentException) { return null; }
    }

    [DllImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetDiskFreeSpaceEx(string path, out ulong available, out ulong total, out ulong free);
}
