using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Modules.VDS.Core.Helper;
using Microsoft.Extensions.DependencyInjection;
using Shared.QuestHa;
using Shared.QuestHa.Dto;
using Shared.QuestHa.Enums;
using Shared.QuestHa.Extensions;
using Shared.QuestHa.Options;

namespace QuestHa.Admin;

/// <summary>
/// Author: KienLT
/// Description: Cửa vận hành QuestDB HA: status/compare/repair/purge, dry-run mặc định; không chạy replicator.
/// Created date: 27/10/2026
/// </summary>
internal static class Program
{
    // vận hành restore qua IPC cùng chủ sở hữu, mặc định preview và kiểm journal bắt buộc.
    private const string Usage = "QuestHa.Admin status|compare|repair|purge|cursor-retire|backup-list|backup-retire|backup-run|restore|node-unfence|export|export-list|import --config <file> [--day yyyy-MM-dd (export: ngày giờ server)] [--dir <thư mục ngày|phiên bản vNNNN> --into <bảng> (import)] [--table X --from yyyy-MM-dd --to yyyy-MM-dd (export-list)] [--node X --id <guid> --unretire --stale-minutes 5 --force-live] [--source A --target B --table VdsTrafficData|VdsTrafficStatistic|all --from <UTC ISO8601> --to <UTC ISO8601>] [--apply] [--allow-journal-gap (restore only; requires backfill, never auto Ready)] [--timeout-seconds 300 --wait-seconds 60 --settle-seconds 120 --max-rows 100000 --max-windows 10080 --max-pending 1000]. Purge/cursor-retire mặc định dry-run, giới hạn purge lấy từ QuestHa:JournalMaintenance. Exit: 0=completed/equal, 2=differences/deferred/capped/warn/critical, 1=error, 124=timeout, 130=cancelled. Connection overrides: QUESTHA_QuestHa__JournalConnectionString, QUESTHA_QuestHa__Nodes__0__PgConnectionString, QUESTHA_QuestHa__Nodes__0__HttpPassword. Export/import mặc định chỉ xem trước; export exit 2 khi hoãn/chưa xong; import exit 2 khi WalNotApplied/RowsBelowManifest. Worker must already be running for repair.";
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "help") { Console.WriteLine(Usage); return 0; }
        using var interrupted = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; interrupted.Cancel(); };
        try
        {
            var command = args[0];
            // chỉ restore nhận cờ chấp nhận journal thiếu.
            if (command is not ("status" or "compare" or "repair" or "purge" or "cursor-retire" or "backup-list" or "backup-retire" or "backup-run" or "restore" or "node-unfence" or "export" or "export-list" or "import")) throw new ArgumentException("Unknown command.");
            var values = Parse(args.Skip(1).ToArray());
            if (command != "restore" && values.ContainsKey("allow-journal-gap")) throw new ArgumentException();
            if (command != "cursor-retire" && (values.ContainsKey("force-live") || values.ContainsKey("stale-minutes")))
                throw new ArgumentException();
            if (command != "backup-retire" && values.ContainsKey("unretire")) throw new ArgumentException();
            var timeout = Number(values, "timeout-seconds", 300, 1, 86400);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(interrupted.Token);
            deadline.CancelAfter(TimeSpan.FromSeconds(timeout));
            try { return await RunAsync(command, values, deadline.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested)
            { return interrupted.IsCancellationRequested ? 130 : 124; }
            // SqlClient có thể trả SqlException khi SQL bị huỷ bởi Ctrl+C/deadline.
            catch (SqlException) when (deadline.IsCancellationRequested)
            { return interrupted.IsCancellationRequested ? 130 : 124; }
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine("Invalid arguments. " + Usage);
            return 1;
        }
        catch (Exception ex) when (ex.Message.Contains("RetiredUtc", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Thiếu cột RetiredUtc: chạy migration 20261028_QuestBackupCatalog_Add_Retired.sql trước khi dùng lifecycle backup.");
            return 1;
        }
        catch (Exception ex)
        {
            // Exception SQL/PG có thể chứa chuỗi kết nối hoặc payload; chỉ xuất loại lỗi.
            Console.Error.WriteLine("QuestHa operation failed: " + ex.GetType().Name);
            return 1;
        }
    }

    private static async Task<int> RunAsync(string command, Dictionary<string, string> args, CancellationToken ct)
    {
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Required(args, "config")), optional: false)
            .AddEnvironmentVariables("QUESTHA_").Build();
        var options = config.GetSection(QuestHaOptions.SectionName).Get<QuestHaOptions>() ?? throw new ArgumentException();
        if (string.IsNullOrWhiteSpace(options.JournalConnectionString)) throw new ArgumentException();
        // Mọi lệnh đi qua QuestHaManager vai Admin (không replicator/worker nền); provider giải phóng journal khi xong.
        await using var provider = new ServiceCollection()
            .AddQuestHa(config, VdsBaseHelper.QuestTableHp.All, QuestHaHostRoleEnum.Admin).BuildServiceProvider();
        var questHa = provider.GetRequiredService<QuestHaManager>();
        if (command == "status")
        {
            if (args.ContainsKey("apply")) throw new ArgumentException();
            // status dùng số pending thực tế và metadata, kèm sức khoẻ.
            var status = await questHa.GetStatusAsync(ct).ConfigureAwait(false);
            Print(status);
            return status.Backlog.Level >= JournalBacklogLevelEnum.Warn || status.Health.Level >= QuestHaHealthLevelEnum.Warn ? 2 : 0;
        }
        if (command == "purge")
        {
            // purge mặc định chỉ đếm có trần, giới hạn lấy từ QuestHa:JournalMaintenance.
            var result = await questHa.PurgeAsync(apply: args.ContainsKey("apply"), ct).ConfigureAwait(false);
            Print(result);
            return result.Capped || result.Reason is not null ? 2 : 0;
        }
        if (command == "cursor-retire")
        {
            var staleMinutes = Number(args, "stale-minutes", 5, 1, 525600);
            var forceLive = args.ContainsKey("force-live");
            if (forceLive)
                Console.Error.WriteLine("WARNING: --force-live bỏ kiểm tra heartbeat; cursor của replicator đang sống có thể bị gỡ và tạo lại. Nghĩa vụ giao journal cho node này sẽ bị từ bỏ khi apply.");
            var result = await questHa.RetireCursorAsync(Required(args, "node"), staleMinutes, forceLive, args.ContainsKey("apply"), ct).ConfigureAwait(false);
            Print(result);
            return result.Reason is null ? 0 : 2;
        }

        // các lệnh catalog mặc định chỉ đọc/preview, apply phải tường minh.
        if (command == "backup-list")
        {
            if (args.ContainsKey("apply")) throw new ArgumentException();
            var records = await questHa.ListBackupsAsync(args.GetValueOrDefault("node"), ct);
            Print(records.Select(b => new { b.BackupId, b.Node, b.Verified, b.RetiredUtc, Retired = b.RetiredUtc is not null,
                AgeDays = Math.Max(0, (DateTime.UtcNow - b.CreatedUtc).TotalDays), b.SizeBytes, b.CreatedUtc, b.WatermarkSeq, b.Path }));
            return 0;
        }
        if (command == "backup-retire")
        {
            if (!Guid.TryParse(Required(args, "id"), out var id)) throw new ArgumentException();
            var retired = !args.ContainsKey("unretire");
            if (!retired) Console.Error.WriteLine("WARNING: unretire không khôi phục journal đã purge; backup quá ProtectedBackupDays vẫn retired theo tuổi, cần backfill.");
            var applied = await questHa.RetireBackupAsync(id, retired, args.ContainsKey("apply"), ct);
            Print(new { BackupId = id, Retired = retired, Applied = applied, DryRun = !applied });
            return 0;
        }
        // Xuất/nạp Parquet chạy ngay trong CLI qua Manager (HTTP /exp từ node phụ, read_parquet trên node đích); mặc định chỉ xem trước.
        if (command == "export")
        {
            if (!DateOnly.TryParseExact(Required(args, "day"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                throw new ArgumentException();
            var result = await questHa.ExportAsync(day, args.ContainsKey("apply"), ct).ConfigureAwait(false);
            Print(result);
            return result.DeferredReason is null && (!result.Apply || result.Completed) ? 0 : 2;
        }
        if (command == "export-list")
        {
            if (args.ContainsKey("apply")) throw new ArgumentException();
            var records = await questHa.ListExportsAsync(args.GetValueOrDefault("table"), Day(args, "from"), Day(args, "to"), ct).ConfigureAwait(false);
            Print(records);
            return 0;
        }
        if (command == "import")
        {
            var result = await questHa.ImportAsync(Required(args, "dir"), Required(args, "node"), args.ContainsKey("apply"),
                args.GetValueOrDefault("into"), ct).ConfigureAwait(false);
            Print(result);
            return result.Error is null ? 0 : 2;
        }

        // backup/restore/unfence qua IPC tới host sở hữu replicator (Worker); Manager kiểm pipe và node thuộc Nodes.
        if (command is "backup-run" or "restore" or "node-unfence")
        {
            var node = Required(args, "node");
            QuestHaAdminReplyOutput reply;
            if (command == "restore")
            {
                if (!Guid.TryParse(Required(args, "id"), out var id)) throw new ArgumentException();
                var allowGap = args.ContainsKey("allow-journal-gap");
                if (allowGap) Console.Error.WriteLine("WARNING --allow-journal-gap: bắt buộc backfill; node giữ rào/phục hồi và KHÔNG tự Ready.");
                reply = await questHa.RestoreAsync(node, id, args.ContainsKey("apply"), allowGap, ct);
            }
            else if (command == "node-unfence") reply = await questHa.UnfenceNodeAsync(node, args.ContainsKey("apply"), ct);
            else reply = await questHa.BackupAsync(node, args.ContainsKey("apply"), ct);
            Console.WriteLine(reply.Reply);
            return reply.Completed ? 0 : 2;
        }
        var sourceName = Required(args, "source");
        var targetName = Required(args, "target");
        var tableName = Required(args, "table");
        var tables = VdsBaseHelper.QuestTableHp.All.Where(t => tableName == "all" || t.Name == tableName).ToArray();
        if (tables.Length == 0) throw new ArgumentException();
        var from = Utc(Required(args, "from"));
        var to = Utc(Required(args, "to"));
        if (from >= to) throw new ArgumentException();
        var settle = TimeSpan.FromSeconds(Number(args, "settle-seconds", 120, 0, 86400));
        var maxWindows = Number(args, "max-windows", 10080, 1, 10080);
        var maxRows = Number(args, "max-rows", 100000, 1, 1000000);
        var maxPending = Number(args, "max-pending", 1000, 0, 1000000);
        var wait = TimeSpan.FromSeconds(Number(args, "wait-seconds", 60, 0, 3600));
        var apply = args.ContainsKey("apply");
        if (apply && command != "repair") throw new ArgumentException();

        if (command == "compare")
        {
            var compareExit = 0;
            foreach (var table in tables)
            {
                var result = await questHa.CompareAsync(new NodeCompareInput(sourceName, targetName, table, from, to, settle, maxWindows), ct).ConfigureAwait(false);
                Print(result);
                if (!result.Equal) compareExit = 2;
            }
            return compareExit;
        }

        // Manager kiểm nguồn/đích, chỉ cho apply ở Shadow với nguồn SourceNode và giữ khoá vận hành suốt các bảng.
        var patches = await questHa.PatchAsync(new NodePatchInput(sourceName, targetName, tables, from, to, apply, settle,
            maxWindows, maxRows, maxPending, wait), ct).ConfigureAwait(false);
        var exit = 0;
        foreach (var patch in patches)
        {
            if (patch.DeferredReason is not null) { Print(new { Table = patch.Table, Deferred = true, Reason = patch.DeferredReason }); return 2; }
            var repair = patch.Repair!;
            Print(repair);
            if (repair.ToUtc <= repair.FromUtc)
            { Print(new { Table = patch.Table, Deferred = true, Reason = "EmptyWindowAfterSettle" }); exit = 2; }
            if (repair.Deferred || repair.Capped || repair.Unrepairable > 0 || repair.TargetNewer > 0 || repair.ExtraInTarget > 0 || (!apply && repair.Candidates > 0)) exit = 2;
        }
        return exit;
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        // cờ force phải có tên phản ánh rõ nguy cơ journal thiếu.
        var allowed = new HashSet<string> { "config", "day", "dir", "into", "node", "id", "unretire", "allow-journal-gap", "source", "target", "table", "from", "to", "apply", "force-live", "stale-minutes", "timeout-seconds", "wait-seconds", "settle-seconds", "max-rows", "max-windows", "max-pending" };
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException();
            var key = args[i][2..];
            if (!allowed.Contains(key)) throw new ArgumentException();
            var value = key is "apply" or "force-live" or "unretire" or "allow-journal-gap" ? "true" : ++i < args.Length ? args[i] : throw new ArgumentException();
            if (!result.TryAdd(key, value)) throw new ArgumentException();
        }
        return result;
    }

    private static DateOnly? Day(Dictionary<string, string> args, string key) =>
        !args.TryGetValue(key, out var raw) ? null
        : DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day : throw new ArgumentException();

    private static string Required(Dictionary<string, string> args, string key) => args.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException();
    private static int Number(Dictionary<string, string> args, string key, int fallback, int min, int max)
    {
        if (!args.TryGetValue(key, out var raw)) return fallback;
        return int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max ? value : throw new ArgumentException();
    }
    private static DateTime Utc(string value) => value.EndsWith('Z') && DateTime.TryParse(value, CultureInfo.InvariantCulture,
        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date) ? date : throw new ArgumentException();
    private static void Print<T>(T result) => Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
}
