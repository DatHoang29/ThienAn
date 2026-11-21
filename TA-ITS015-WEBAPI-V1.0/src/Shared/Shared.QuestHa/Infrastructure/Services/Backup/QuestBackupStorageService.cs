using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Shared.QuestHa.Infrastructure.Services.Backup;

/// <summary>
/// Author: KienLT
/// Description: Preflight local/UNC, xác minh nội dung và retention có chứng từ sở hữu.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestBackupStorageService
{
    private const string MarkerSuffix = ".questha.json";
    // manifest nằm NGOÀI thư mục backup (không bị chép vào root node), không khớp mẫu marker.
    private const string ManifestSuffix = ".questha-manifest.json";
    private const int ManifestVersion = 1;

    /// <summary>Từ chối symlink/reparse point trên đường dẫn và cây cha.</summary>
    public static void RejectLinks(string path)
    {
        for (var item = new DirectoryInfo(Path.GetFullPath(path)); item is not null; item = item.Parent)
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Không thao tác backup qua reparse point/symlink.");
    }

    /// <summary>Kiểm đường dẫn, quyền ghi và dung lượng; chấp nhận file sống vừa bị xoá khi ước lượng.</summary>
    public static long Preflight(string source, string root, long minimumFreeBytes, CancellationToken ct)
    {
        var src = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
        var dst = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        if (Inside(src, dst) || Inside(dst, src) || src.Equals(dst, StringComparison.OrdinalIgnoreCase))
            throw new IOException("BackupRoot và RootPath phải tách rời.");
        RejectLinks(source);
        RejectLinks(root);
        long bytes = 0;
        foreach (var file in Files(source, ct, ignoreVanished: true))
        {
            try { bytes = checked(bytes + new FileInfo(file).Length); }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        Directory.CreateDirectory(root);
        var probe = Path.Combine(root, ".questha-write-" + Guid.NewGuid().ToString("N"));
        try { using var file = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.WriteThrough); file.WriteByte(0); file.Flush(true); }
        finally { if (File.Exists(probe)) File.Delete(probe); }
        ulong free;
        if (OperatingSystem.IsWindows())
        {
            if (!GetDiskFreeSpaceEx(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, out free, out _, out _))
                throw new IOException("Không kiểm được dung lượng trống BackupRoot (local/UNC).", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
        }
        else free = (ulong)new DriveInfo(Path.GetPathRoot(dst)!).AvailableFreeSpace;
        if (free < (ulong)checked(bytes + minimumFreeBytes)) throw new IOException("BackupRoot không đủ dung lượng trống.");
        return bytes;
    }

    /// <summary>Duyệt cây không qua link; chỉ preflight cho phép bỏ file vừa biến mất.</summary>
    public static IEnumerable<string> Files(string directory, CancellationToken ct, bool ignoreVanished = false)
    {
        RejectLinks(directory);
        string[] entries;
        try { entries = Directory.GetFileSystemEntries(directory); }
        catch (DirectoryNotFoundException) when (ignoreVanished) { yield break; }
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            FileAttributes attr;
            try { attr = File.GetAttributes(entry); }
            catch (FileNotFoundException) when (ignoreVanished) { continue; }
            catch (DirectoryNotFoundException) when (ignoreVanished) { continue; }
            if ((attr & FileAttributes.ReparsePoint) != 0) throw new IOException("Root backup chứa symlink/reparse point.");
            if ((attr & FileAttributes.Directory) != 0)
                foreach (var file in Files(entry, ct, ignoreVanished)) yield return file;
            else yield return entry;
        }
    }

    /// <summary>Xác minh nội dung cho caller đồng bộ; băm theo chunk có cancellation.</summary>
    public static void Verify(string source, string destination, CancellationToken ct) =>
        VerifyAsync(source, destination, ct).GetAwaiter().GetResult();

    // trả manifest (đường dẫn tương đối, kích thước, SHA-256) của bản đã khớp để lưu bền cạnh
    // backup; restore băm lại theo manifest thay vì tin cờ Verified cũ.
    /// <summary>So tập đường dẫn, kích thước và SHA-256; huỷ giữa chunk, kiểm peer tối đa mỗi 5 giây. Trả manifest bản sao.</summary>
    public static async Task<IReadOnlyList<BackupManifestEntryOutput>> VerifyAsync(string source, string destination, CancellationToken ct, Func<CancellationToken, Task>? checkPeer = null)
    {
        checkPeer = ThrottlePeerCheck(checkPeer);
        var original = Files(source, ct).ToDictionary(f => Path.GetRelativePath(source, f), StringComparer.OrdinalIgnoreCase);
        var copied = Files(destination, ct).ToDictionary(f => Path.GetRelativePath(destination, f), StringComparer.OrdinalIgnoreCase);
        if (original.Count == 0 || !original.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(copied.Keys))
            throw new IOException("Backup thiếu file hoặc root rỗng.");
        var manifest = new List<BackupManifestEntryOutput>(original.Count);
        foreach (var (name, src) in original)
        {
            ct.ThrowIfCancellationRequested();
            using var left = File.OpenRead(src);
            using var right = File.OpenRead(copied[name]);
            var leftHash = await HashAsync(left, ct, checkPeer).ConfigureAwait(false);
            var rightHash = await HashAsync(right, ct, checkPeer).ConfigureAwait(false);
            if (left.Length != right.Length || !leftHash.SequenceEqual(rightHash))
                throw new IOException("Backup không khớp kích thước/nội dung file.");
            manifest.Add(new BackupManifestEntryOutput(name, right.Length, Convert.ToHexString(rightHash)));
        }
        return manifest;
    }

    /// <summary>
    /// Ghi manifest bền (WriteThrough + đổi tên) cạnh thư mục backup: <c>{path}.questha-manifest.json</c>. Ghi sau khi
    /// <see cref="VerifyAsync"/> đạt và TRƯỚC khi ghi catalog Verified.
    /// </summary>
    public static void WriteManifest(BackupRecordOutput record, IReadOnlyList<BackupManifestEntryOutput> entries)
    {
        if (entries.Count == 0) throw new IOException("Manifest backup rỗng.");
        var path = record.Path + ManifestSuffix;
        RejectLinks(path);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, new Manifest(ManifestVersion, record.BackupId, record.Node, entries));
                stream.Flush(true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    /// <summary>
    /// Kiểm lại NỘI DUNG backup theo manifest đã lưu (tập file, kích thước, SHA-256) — chỉ đọc, không chạm node.
    /// Thiếu manifest ở bản backup cũ, manifest không khớp id/node, thiếu/thừa file, sai kích thước hoặc hash ⇒
    /// <see cref="InvalidDataException"/>. Trả số file và tổng byte đã kiểm.
    /// </summary>
    public static async Task<(long Files, long Bytes)> VerifyManifestAsync(BackupRecordOutput record, CancellationToken ct)
    {
        var manifest = ReadManifest(record)
            ?? throw new InvalidDataException("Backup không có manifest nội dung hợp lệ (bản cũ thiếu manifest hoặc manifest hỏng) — từ chối restore; tạo bản backup Verified mới.");
        var root = Path.GetFullPath(record.Path);
        var actual = Files(root, ct).ToDictionary(f => Path.GetRelativePath(root, f), StringComparer.OrdinalIgnoreCase);
        var expected = new Dictionary<string, BackupManifestEntryOutput>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in manifest.Files)
            if (!expected.TryAdd(entry.Path, entry)) throw new InvalidDataException("Manifest backup trùng đường dẫn.");
        if (expected.Count == 0 || !expected.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(actual.Keys))
            throw new InvalidDataException("Backup thiếu hoặc thừa file so với manifest — từ chối restore.");
        long bytes = 0;
        foreach (var (name, file) in actual)
        {
            ct.ThrowIfCancellationRequested();
            using var input = File.OpenRead(file);
            var entry = expected[name];
            if (input.Length != entry.Length) throw new InvalidDataException($"Backup sai kích thước file {name} so với manifest — từ chối restore.");
            var hash = await HashAsync(input, ct, null).ConfigureAwait(false);
            if (!string.Equals(Convert.ToHexString(hash), entry.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Backup sai SHA-256 file {name} so với manifest — từ chối restore.");
            bytes = checked(bytes + entry.Length);
        }
        return (expected.Count, bytes);
    }

    /// <summary>Số file và tổng byte theo manifest (kiểm nhanh bản chép vào root sau restore); null nếu không có manifest hợp lệ.</summary>
    public static (long Files, long Bytes)? ManifestTotals(BackupRecordOutput record) =>
        ReadManifest(record) is { } m ? (m.Files.Count, m.Files.Sum(f => f.Length)) : null;

    private static Manifest? ReadManifest(BackupRecordOutput record)
    {
        var path = record.Path + ManifestSuffix;
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Manifest là reparse point.");
            var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path));
            return manifest is { Version: ManifestVersion, Files: not null } && manifest.Id == record.BackupId
                   && string.Equals(manifest.Node, record.Node, StringComparison.Ordinal) ? manifest : null;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        catch (JsonException) { return null; }
    }

    private static async Task<byte[]> HashAsync(Stream input, CancellationToken ct, Func<CancellationToken, Task>? checkPeer)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[128 * 1024];
        int read;
        while ((read = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            if (checkPeer is not null) await checkPeer(ct).ConfigureAwait(false);
            hash.AppendData(buffer, 0, read);
        }
        ct.ThrowIfCancellationRequested();
        return hash.GetHashAndReset();
    }

    // chỉ đọc đồng hồ giữa chunk; một probe/5s, luôn kiểm token dù chưa tới hạn.
    internal static Func<CancellationToken, Task>? ThrottlePeerCheck(Func<CancellationToken, Task>? check,
        TimeProvider? clock = null)
    {
        if (check is null) return null;
        clock ??= TimeProvider.System;
        long? last = null;
        return async token =>
        {
            token.ThrowIfCancellationRequested();
            var now = clock.GetTimestamp();
            if (last is { } previous && clock.GetElapsedTime(previous, now) < TimeSpan.FromSeconds(5)) return;
            await check(token).ConfigureAwait(false);
            last = now;
        };
    }

    /// <summary>Ghi chứng từ sở hữu và loại lịch/thủ công; marker cũ mặc định là bản theo lịch.</summary>
    public static void MarkOwned(BackupRecordOutput record, bool weekly, bool manual = false)
    {
        // không để crash khi ghi đè phá chứng từ sở hữu đã hợp lệ.
        var path = record.Path + MarkerSuffix;
        RejectLinks(path);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, new Ownership(record.BackupId, record.Node, weekly, manual));
                stream.Flush(true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    /// <summary>Đọc phân loại weekly; lỗi I/O được truyền ra, không coi là daily.</summary>
    public static bool IsWeekly(BackupRecordOutput record) => ReadMarker(record)?.Weekly == true;

    /// <summary>Đọc phân loại bản sao thủ công.</summary>
    public static bool IsManual(BackupRecordOutput record) => ReadMarker(record)?.Manual == true;

    /// <summary>Chỉ xoá cây có chứng từ sở hữu và đường dẫn đúng cấu trúc root/node/id.</summary>
    public static void DeleteOwned(BackupRecordOutput record, string root, CancellationToken ct)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(record.Path);
        if (!Inside(path, fullRoot) || !string.Equals(Directory.GetParent(path)?.FullName, Path.Combine(fullRoot, record.Node), StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(path).EndsWith(record.BackupId.ToString("N"), StringComparison.Ordinal)) return;
        RejectLinks(path);
        if (ReadMarker(record) is null) return;
        if (Directory.Exists(path)) { _ = Files(path, ct).Count(); Directory.Delete(path, true); }
        // xoá manifest trước marker (marker là chứng từ sở hữu, còn thì lượt sau dọn tiếp).
        File.Delete(path + ManifestSuffix);
        File.Delete(path + MarkerSuffix);
    }

    private static Ownership? ReadMarker(BackupRecordOutput record)
    {
        var path = record.Path + MarkerSuffix;
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Marker là reparse point.");
            var owner = JsonSerializer.Deserialize<Ownership>(File.ReadAllText(path));
            return owner?.Id == record.BackupId && owner.Node == record.Node ? owner : null;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        // cô lập riêng bản không chứng minh được sở hữu, vẫn retain node/bản khác.
        catch (JsonException) { return null; }
    }

    // chỉ dấu gián đoạn bền cấp quyền dọn; bất kỳ dòng Verified cùng đường dẫn đều bảo vệ file.
    /// <summary>Dọn đúng lượt bị ngắt sau khi node chạy lại; SQL lỗi hoặc quyền sở hữu không rõ thì giữ file.</summary>
    public static async Task CleanupInterruptedAsync(BackupRecordOutput record, string root, IJournalStore store, CancellationToken ct)
    {
        var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(record.Path));
        var records = await store.ListBackupsAsync(ct);
        var matching = records.Where(b => string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(b.Path)), path, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matching.Any(b => b.Verified)) return;
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        if (!Inside(path, fullRoot) || !string.Equals(Directory.GetParent(path)?.FullName, Path.Combine(fullRoot, record.Node), StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(path).EndsWith(record.BackupId.ToString("N"), StringComparison.Ordinal))
            throw new IOException("Đích backup bị ngắt không nằm đúng cấu trúc BackupRoot/node/id.");
        RejectLinks(path);
        if (!Directory.Exists(path) && !File.Exists(path + MarkerSuffix)) return;
        if (ReadMarker(record) is null) throw new IOException("Không chứng minh được quyền sở hữu bản backup bị ngắt.");
        foreach (var backup in matching)
            await store.SetBackupRetiredAsync(backup.BackupId, true, false, ct);
        DeleteOwned(record with { Path = path }, root, ct);
    }

    // phát hiện marker ngoài catalog chỉ để báo, không dùng danh sách này làm đầu vào xoá.
    /// <summary>Liệt kê bản owned mồ côi của một node; không theo link và không xoá file.</summary>
    public static IEnumerable<BackupRecordOutput> FindOrphans(string node, string root, IReadOnlyList<BackupRecordOutput> catalog)
    {
        var directory = Path.Combine(root, node);
        RejectLinks(directory);
        if (!Directory.Exists(directory)) yield break;
        foreach (var marker in Directory.EnumerateFiles(directory, "*" + MarkerSuffix))
        {
            RejectLinks(marker);
            Ownership? owner;
            try { owner = JsonSerializer.Deserialize<Ownership>(File.ReadAllText(marker)); }
            catch (JsonException) { continue; }
            if (owner is null || owner.Node != node) continue;
            var path = marker[..^MarkerSuffix.Length];
            if (catalog.Any(b => string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(b.Path)), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))) continue;
            var record = new BackupRecordOutput(owner.Id, node, 0, default, path, 0, false, default);
            if (Path.GetFileName(path).EndsWith(owner.Id.ToString("N"), StringComparison.Ordinal) && Directory.Exists(path)) yield return record;
        }
    }

    /// <summary>Snapshot marker một lần, giữ ngày/tuần/thủ công riêng và retire SQL trước khi xoá.</summary>
    public async Task<int> RetainAsync(IJournalStore store, BackupOptions options, bool dryRun, CancellationToken ct)
    {
        var records = await store.ListBackupsAsync(ct);
        var markers = records.ToDictionary(b => b.BackupId, ReadMarker);
        // catalog của file đã xoá vẫn còn; không để Manual mất marker bị đếm lại thành daily ở lượt sau.
        var owned = records.Where(b => markers[b.BackupId] is not null).ToArray();
        var keep = QuestBackupRetentionRules.Keep(owned, options.DailyCopies, options.WeeklyCopies,
            b => markers[b.BackupId]?.Weekly == true, b => markers[b.BackupId]?.Manual == true, options.ManualCopies);
        var count = 0;
        foreach (var backup in records.Where(b => !keep.Contains(b.BackupId)))
        {
            ct.ThrowIfCancellationRequested();
            var root = options.RootFor(backup.Node);
            if (string.IsNullOrWhiteSpace(root)) continue;
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            var path = Path.GetFullPath(backup.Path);
            // Chỉ đúng cấu trúc {root}/{node}/{stamp-guid}; không bao giờ xoá root/node hoặc đường dẫn ngoài root.
            if (!Inside(path, fullRoot) || !string.Equals(Directory.GetParent(path)?.FullName, Path.Combine(fullRoot, backup.Node), StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(path).EndsWith(backup.BackupId.ToString("N"), StringComparison.Ordinal)) continue;
            RejectLinks(path);
            if (!Directory.Exists(path) || markers[backup.BackupId] is null) continue;
            // Retire phải thành công trước khi xoá; lỗi SQL/thiếu migration thì giữ nguyên file.
            await store.SetBackupRetiredAsync(backup.BackupId, true, dryRun, ct);
            if (!dryRun)
            {
                // Kiểm cả cây, không đi qua junction trong lúc xoá đệ quy.
                _ = Files(path, ct).Count();
                Directory.Delete(path, recursive: true);
                File.Delete(path + ManifestSuffix); // manifest đi cùng bản bị retain.
                File.Delete(path + MarkerSuffix);
            }
            count++;
        }
        return count;
    }

    private static bool Inside(string path, string root) => path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    [DllImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetDiskFreeSpaceEx(string path, out ulong available, out ulong total, out ulong free);

    /// <summary>
    /// Author: KienLT
    /// Description: Chứng từ service tạo thư mục, gắn BackupId và node.
    /// Created date: 28/10/2026
    /// </summary>
    private sealed record Ownership(Guid Id, string Node, bool Weekly, bool Manual = false);

    /// <summary>
    /// Author: KienLT
    /// Description: Manifest nội dung backup: phiên bản định dạng, BackupId/node chủ sở hữu và danh sách file/kích thước/SHA-256.
    /// Created date: 30/10/2026
    /// </summary>
    private sealed record Manifest(int Version, Guid Id, string Node, IReadOnlyList<BackupManifestEntryOutput> Files);
}
