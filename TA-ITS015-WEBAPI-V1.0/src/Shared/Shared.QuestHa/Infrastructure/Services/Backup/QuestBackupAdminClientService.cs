using System.IO.Pipes;

namespace Shared.QuestHa.Infrastructure.Services.Backup;

/// <summary>
/// Author: KienLT
/// Description: Phía CLI của kênh IPC backup/restore/unfence: gửi một dòng lệnh tới host sở hữu replicator
/// (<see cref="Workers.QuestBackupAdminWorker"/>) và nhận một dòng trả lời. CLI không tự dựng backup/replicator thứ hai.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestBackupAdminClientService(QuestHaOptions options)
{
    /// <summary>
    /// Gửi <paramref name="request"/> (đã gồm tên node) tới pipe <c>Backup:AdminPipeName</c>; trả dòng trả lời (null nếu host đóng sớm).
    /// Thiếu tên pipe hoặc node không thuộc <c>Nodes</c> ⇒ <see cref="ArgumentException"/>; không kết nối được trong 5 s ⇒ lỗi timeout của pipe.
    /// </summary>
    public async Task<string?> SendAsync(string node, string request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.Backup.AdminPipeName) || !options.Nodes.Any(n => n.Name.Equals(node, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("QuestHa:Backup:AdminPipeName chưa khai hoặc node không thuộc QuestHa:Nodes.", nameof(node));
        await using var pipe = new NamedPipeClientStream(".", options.Backup.AdminPipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync(5000, ct).ConfigureAwait(false);
        using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipe, leaveOpen: true);
        await writer.WriteLineAsync(request).ConfigureAwait(false);
        return await reader.ReadLineAsync(ct).ConfigureAwait(false);
    }
}
