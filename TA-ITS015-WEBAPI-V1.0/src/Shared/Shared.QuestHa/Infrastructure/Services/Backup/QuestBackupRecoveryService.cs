using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shared.QuestHa.Infrastructure.Services.Backup;

/// <summary>
/// Author: KienLT
/// Description: Khoá chủ sở hữu và dấu node do backup dừng, flush bền trước StopClean.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestBackupRecoveryService(BackupOptions options)
{
    /// <summary>Khoá cùng StateDirectory; process chết tự nhả khoá, dấu dừng vẫn còn.</summary>
    public FileStream AcquireOwner()
    {
        QuestBackupStorageService.RejectLinks(options.StateDirectory);
        Directory.CreateDirectory(options.StateDirectory);
        var path = Path.Combine(options.StateDirectory, "backup-owner.lock");
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Owner lock là link.");
        return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    /// <summary>Ghi trước khi stop; crash sau dấu nhưng trước stop chỉ gây EnsureStarted luỹ đẳng.</summary>
    // lưu đúng lượt backup trước Stop; không suy thư mục cần xoá từ tuổi hoặc marker mồ côi.
    public void MarkStopped(INodeControl control, BackupRecordOutput? backup = null)
    {
        var path = Marker(control.Node);
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Dấu phục hồi là link.");
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, new InterruptedBackup(Path.GetFullPath(control.RootPath), backup));
                stream.Flush(true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    /// <summary>Đọc dấu và xác minh root vẫn trùng cấu hình; lỗi I/O không được bỏ qua.</summary>
    public bool WasStopped(INodeControl control)
    {
        try
        {
            var path = Marker(control.Node);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Dấu phục hồi là link.");
            var root = ReadState(path).Root;
            if (string.IsNullOrWhiteSpace(root)) return true;
            if (!string.Equals(root, Path.GetFullPath(control.RootPath), StringComparison.OrdinalIgnoreCase))
                throw new IOException("RootPath khác dấu backup; cần đối chiếu cấu hình trước khi khởi động node.");
            return true;
        }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
        // dấu bị cắt do crash vẫn là bằng chứng có thể đã stop; EnsureStarted luỹ đẳng.
        catch (JsonException) { return true; }
    }

    // marker kiểu chuỗi cũ chỉ chứng minh node đã dừng, không chứng minh thư mục backup nào.
    /// <summary>Đọc đích backup đã lưu bền; dấu cũ hoặc hỏng không cấp quyền xoá thư mục.</summary>
    public BackupRecordOutput? ReadInterruptedBackup(INodeControl control)
    {
        var path = Marker(control.Node);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Dấu phục hồi là link.");
        try { return ReadState(path).Backup; }
        catch (JsonException) { return null; }
    }

    private static InterruptedBackup ReadState(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.ValueKind == JsonValueKind.String
            ? new InterruptedBackup(document.RootElement.GetString()!, null)
            : document.RootElement.Deserialize<InterruptedBackup>() ?? throw new JsonException("Dấu phục hồi rỗng.");
    }

    /// <summary>Chỉ xoá dấu sau khi node đã start và probe thành công.</summary>
    public void Clear(INodeControl control) => File.Delete(Marker(control.Node));

    private string Marker(string node) => Path.Combine(options.StateDirectory,
        "stopped-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(node.ToUpperInvariant()))) + ".json");

    /// <summary>
    /// Author: KienLT
    /// Description: Root node và đích duy nhất của lượt backup bị gián đoạn.
    /// Created date: 30/10/2026
    /// </summary>
    private sealed record InterruptedBackup(string Root, BackupRecordOutput? Backup);
}
