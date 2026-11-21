using System.IO.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.QuestHa.Infrastructure.Services.Backup.Workers;

/// <summary>
/// Author: KienLT
/// Description: IPC local cùng user tới chủ sở hữu replicator, không dựng worker thứ hai trong CLI.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestBackupAdminWorker(IServiceProvider services, IOptions<QuestHaOptions> options,
    ILogger<QuestBackupAdminWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        if (config.WriteMode == QuestWriteModeEnum.Direct || config.Nodes.Count == 0 || string.IsNullOrWhiteSpace(config.Backup.AdminPipeName)) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            var accepted = false;
            try
            {
                await using var pipe = new NamedPipeServerStream(config.Backup.AdminPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(stoppingToken);
                accepted = true;
                using var reader = new StreamReader(pipe, leaveOpen: true);
                using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
                using var request = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                request.CancelAfter(TimeSpan.FromSeconds(5));
                var line = await reader.ReadLineAsync(request.Token);
                var parts = line?.Split('|');
                // restore cũng đi qua host sở hữu; force chỉ nhận tên cờ tường minh.
                var isBackup = parts is { Length: 2 } && parts[0] is ("preview" or "apply");
                var isRestore = parts is { Length: 4 } && parts[0] is ("restore-preview" or "restore") &&
                    Guid.TryParse(parts[2], out _) && parts[3] is ("strict" or "allow-journal-gap");
                var isUnfence = parts is { Length: 2 } && parts[0] is ("unfence-preview" or "unfence");
                if ((!isBackup && !isRestore && !isUnfence) || !config.Nodes.Any(n => n.Name.Equals(parts![1], StringComparison.OrdinalIgnoreCase)))
                { await ReplyAsync(writer, "ERROR InvalidRequest"); continue; }
                try
                {
                    var backup = services.GetRequiredService<QuestBackupService>();
                    if (isUnfence)
                    {
                        await backup.UnfenceAsync(parts![1], parts[0] == "unfence", stoppingToken);
                        await ReplyAsync(writer, parts[0] == "unfence" ? "OK Applied" : "OK DryRun");
                        continue;
                    }
                    if (isRestore)
                    {
                        var id = Guid.Parse(parts![2]);
                        var allowGap = parts[3] == "allow-journal-gap";
                        if (parts[0] == "restore") await backup.RestoreAsync(parts[1], id, stoppingToken, allowGap);
                        // preview băm lại nội dung theo manifest như restore thật; lệch/thiếu manifest
                        // ⇒ ERROR InvalidDataException, đạt ⇒ thêm ContentVerified.
                        else await backup.ValidateRestoreAsync(parts[1], id, stoppingToken, allowGap, verifyContent: true);
                        await ReplyAsync(writer, allowGap ? "OK RequiresBackfillNotReady" : parts[0] == "restore" ? "OK Applied" : "OK DryRun ContentVerified");
                        continue;
                    }
                    await backup.PreflightAsync(parts![1], stoppingToken);
                    if (parts[0] == "apply")
                    {
                        await backup.BackupColdAsync(parts[1], stoppingToken, requirePeerReady: true);
                        await backup.RetainAsync(false, stoppingToken);
                    }
                    await ReplyAsync(writer, parts[0] == "apply" ? "OK Applied" : "OK DryRun");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception error)
                {
                    logger.LogError("[QuestHa] Thao tác backup IPC thất bại {ErrorType}.", error.GetType().Name);
                    await ReplyAsync(writer, "ERROR " + error.GetType().Name + (isRestore || isUnfence
                        ? " RestoreRecoveryRequired: giữ .prerestore-*; backfill/tạo backup Verified có journal đủ, strict restore rồi node-unfence. Xem README và Timeline." : ""));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            // reader/reply/Dispose có thể báo broken pipe sau khi CLI đóng; lỗi nghiệp vụ đã được log trong catch nội bộ.
            catch (IOException) when (accepted) { }
            catch (Exception error)
            {
                logger.LogError("[QuestHa] Backup IPC lỗi {ErrorType}.", error.GetType().Name);
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            }
        }
    }

    private static async Task ReplyAsync(StreamWriter writer, string message)
    {
        try { await writer.WriteLineAsync(message); }
        catch (IOException) { /* CLI ngắt kết nối không thay đổi kết quả backup đã hoàn tất. */ }
    }
}
