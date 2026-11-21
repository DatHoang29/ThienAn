using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Shared.QuestHa.Infrastructure.Services.Backup;

/// <summary>
/// Author: KienLT
/// Description: Adapter Windows service local cấu hình tường minh, không suy đường dẫn/PID.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestBackupNodeControlService(string node, BackupNodeControlOptions options,
    Func<string, CancellationToken, Task<string>>? executeCommand = null) : INodeControl
{
    public string Node => node;
    public string RootPath => options.RootPath;
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken ct = default)
    {
        while (true)
        {
            var state = await StateAsync(ct);
            if (state == 4) return;
            if (state == 1) await Command("start", ct);
            else if (state is not (2 or 3)) throw new IOException("Windows service không ở trạng thái có thể khởi động.");
            await Task.Delay(200, ct);
        }
    }
    /// <inheritdoc />
    public Task EnsureStartedAsync(CancellationToken ct = default) => StartAsync(ct);
    /// <inheritdoc />
    public async Task StopCleanAsync(CancellationToken ct = default)
    {
        await Command("stop", ct);
        while ((await StatusAsync(ct)).IsRunning) await Task.Delay(200, ct);
    }
    /// <inheritdoc />
    public Task KillAsync(CancellationToken ct = default) => throw new NotSupportedException("Adapter Windows service không kill tiến trình; giữ rào để xử lý vận hành.");
    /// <inheritdoc />
    public async Task<NodeProcessOutput> StatusAsync(CancellationToken ct = default)
    {
        var state = await StateAsync(ct);
        return new NodeProcessOutput(state != 1, null, false);
    }
    private async Task<int> StateAsync(CancellationToken ct)
    {
        var text = await Command("query", ct);
        var match = Regex.Match(text, @"STATE\s*:\s*(\d+)");
        if (!match.Success) throw new IOException("Không đọc được STATE của Windows service.");
        return int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }
    private async Task<string> Command(string command, CancellationToken ct)
    {
        if (executeCommand is not null) return await executeCommand(command, ct);
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(options.RootPath) ||
            string.IsNullOrWhiteSpace(options.ServiceName) || options.ServiceName.StartsWith('-') || options.ServiceName.StartsWith('/'))
            throw new InvalidOperationException("Backup:NodeControls cần Windows service local và RootPath tường minh.");
        var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "sc.exe")) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(command);
        info.ArgumentList.Add(options.ServiceName);
        using var process = Process.Start(info) ?? throw new IOException("Không mở được sc.exe.");
        var output = process.StandardOutput.ReadToEndAsync(ct);
        var error = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        await error;
        var result = await output;
        if (process.ExitCode != 0) throw new IOException("Windows service control lỗi, exit=" + process.ExitCode);
        return result;
    }
}
