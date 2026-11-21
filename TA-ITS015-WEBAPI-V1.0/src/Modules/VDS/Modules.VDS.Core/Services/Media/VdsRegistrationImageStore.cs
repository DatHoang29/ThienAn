using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Interfaces;
using Modules.VDS.Core.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Modules.VDS.Core.Services.Media;

/// <summary>
/// Author: KienLT
/// Description: Durable registration-image requests; no incident columns or SQL job table.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsRegistrationImageStore(VdsRegistrationImageOptions options, IVdsImageFileAccess? shareAccess = null)
{
    public VdsRegistrationImageOptions Options => options;
    public string Root => Path.GetFullPath(options.StateDirectory);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Key(string tenant, string incident) => Hash(tenant) + "_" + Hash(incident);
    private string FilePath(string folder, string tenant, string incident) => Path.Combine(Root, folder, Key(tenant, incident) + ".json");

    /// <summary>Only host/tenant configuration roots belong here; incident paths remain inside the operation.</summary>
    public Task<T> WithConfiguredRootAccessAsync<T>(string configuredRoot, Func<Task<T>> operation, CancellationToken ct) =>
        shareAccess is null ? operation() : shareAccess.RunAsync(configuredRoot, operation, ct);

    private Task<T> AccessAsync<T>(Func<Task<T>> operation, CancellationToken ct) => WithConfiguredRootAccessAsync(Root, operation, ct);
    private Task AccessAsync(Func<Task> operation, CancellationToken ct) => AccessAsync(async () => { await operation(); return true; }, ct);

    public Task<FileStream> LockAsync(string tenant, string incident, CancellationToken ct) =>
        AccessAsync(() => LockCoreAsync(tenant, incident, ct), ct);
    private async Task<FileStream> LockCoreAsync(string tenant, string incident, CancellationToken ct)
    {
        var folder = Path.Combine(Root, "locks");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, Key(tenant, incident) + ".lock");
        for (var attempt = 0; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (attempt < 20) { await Task.Delay(50, ct).ConfigureAwait(false); }
        }
    }

    public Task<VdsRegistrationImageInput?> ReadAsync(string tenant, string incident, CancellationToken ct) =>
        AccessAsync(() => ReadCoreAsync(tenant, incident, ct), ct);
    private async Task<VdsRegistrationImageInput?> ReadCoreAsync(string tenant, string incident, CancellationToken ct)
    {
        foreach (var folder in new[] { "completed", "pending" })
        {
            var path = FilePath(folder, tenant, incident);
            try
            {
                await using var file = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
                var request = await JsonSerializer.DeserializeAsync<VdsRegistrationImageInput>(file, cancellationToken: ct);
                if (request?.TenantId != tenant || request.IncidentId != incident)
                    throw new InvalidDataException("Registration image request identity does not match its file.");
                return request;
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        return null;
    }

    public Task RegisterAsync(string tenant, string incident, DateTime registrationTime, DateTime registrationUtc,
        VdsRegistrationImageSourceOutput source, CancellationToken ct) =>
        AccessAsync(() => RegisterCoreAsync(tenant, incident, registrationTime, registrationUtc, source, ct), ct);
    private async Task RegisterCoreAsync(string tenant, string incident, DateTime registrationTime, DateTime registrationUtc,
        VdsRegistrationImageSourceOutput source, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(incident)
            || string.IsNullOrWhiteSpace(source.EquipmentId) || source.DetectTimeUtc == default)
            throw new ArgumentException("Registration image requires tenant, incident and raw camera/time identity.");
        await using var held = await LockCoreAsync(tenant, incident, ct);
        if (await ReadCoreAsync(tenant, incident, ct) is not null) return; // Replay never changes source/deadline.
        if (DateTime.SpecifyKind(registrationUtc, DateTimeKind.Utc).AddSeconds(options.EffectiveWaitSeconds) <= DateTime.UtcNow)
            return; // Historical replay must not hide a legacy registration photo with a new missing-source record.
        await SaveCoreAsync(new(tenant, incident, registrationTime,
            DateTime.SpecifyKind(registrationUtc, DateTimeKind.Utc).AddSeconds(options.EffectiveWaitSeconds), source), ct);
    }

    /// <summary>Caller holds the incident lock. Terminal requests leave the pending scan.</summary>
    public Task SaveAsync(VdsRegistrationImageInput request, CancellationToken ct) =>
        AccessAsync(() => SaveCoreAsync(request, ct), ct);
    private async Task SaveCoreAsync(VdsRegistrationImageInput request, CancellationToken ct)
    {
        var pending = request.Status == "WaitingForSource";
        var target = FilePath(pending ? "pending" : "completed", request.TenantId, request.IncidentId);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(request), ct);
            File.Move(temp, target, overwrite: true);
            if (!pending) File.Delete(FilePath("pending", request.TenantId, request.IncidentId));
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    /// <summary>Cursor rotates through pending requests so a missing early file cannot starve later requests.</summary>
    public Task<IReadOnlyList<string>> PendingBatchAsync(string? after, CancellationToken ct) =>
        AccessAsync(() => Task.FromResult(PendingBatchCore(after)), ct);
    private IReadOnlyList<string> PendingBatchCore(string? after)
    {
        var folder = Path.Combine(Root, "pending");
        if (!Directory.Exists(folder)) return [];
        var paths = Directory.EnumerateFiles(folder, "*.json").Order(StringComparer.Ordinal).ToArray();
        var batch = paths.Where(p => after is null || string.CompareOrdinal(p, after) > 0)
            .Take(options.EffectiveBatchSize).ToArray();
        return batch.Length > 0 ? batch : paths.Take(options.EffectiveBatchSize).ToArray();
    }

    public Task<VdsRegistrationImageInput?> ReadPendingFileAsync(string path, CancellationToken ct) =>
        AccessAsync(() => ReadPendingFileCoreAsync(path, ct), ct);
    private async Task<VdsRegistrationImageInput?> ReadPendingFileCoreAsync(string path, CancellationToken ct)
    {
        // Paths only originate from PendingBatch, never from HTTP input.
        try { return JsonSerializer.Deserialize<VdsRegistrationImageInput>(await File.ReadAllTextAsync(path, ct)); }
        catch (FileNotFoundException) { return null; }
    }

    private string StageKey(string tenant, string incident, int pictureType, DateTime time)
    {
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(incident) || pictureType is not (2 or 3) || time == default)
            throw new ArgumentException("Stage image requires tenant, incident, picture type 2/3 and transition time.");
        return Key(tenant, incident) + "_" + pictureType + "_" + time.Ticks;
    }

    public Task<FileStream> LockStageAsync(string tenant, string incident, int pictureType, DateTime time, CancellationToken ct) =>
        AccessAsync(() => LockStageCoreAsync(tenant, incident, pictureType, time, ct), ct);
    private async Task<FileStream> LockStageCoreAsync(string tenant, string incident, int pictureType, DateTime time, CancellationToken ct)
    {
        var folder = Path.Combine(Root, "stage-locks");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, StageKey(tenant, incident, pictureType, time) + ".lock");
        for (var attempt = 0; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (attempt < 20) { await Task.Delay(50, ct).ConfigureAwait(false); }
        }
    }

    public Task<VdsAutomationImageDecisionOutput?> ReadStageAsync(string tenant, string incident, int pictureType,
        DateTime time, CancellationToken ct) => AccessAsync(() => ReadStageCoreAsync(tenant, incident, pictureType, time, ct), ct);
    private async Task<VdsAutomationImageDecisionOutput?> ReadStageCoreAsync(string tenant, string incident, int pictureType,
        DateTime time, CancellationToken ct)
    {
        var path = Path.Combine(Root, "stages", StageKey(tenant, incident, pictureType, time) + ".json");
        try
        {
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
            var decision = await JsonSerializer.DeserializeAsync<VdsAutomationImageDecisionOutput>(file, cancellationToken: ct);
            if (decision is null || !decision.Matches(tenant, incident, pictureType, time))
                throw new InvalidDataException("Stage image identity does not match its file.");
            return decision;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    /// <summary>Caller holds the stage lock. Camera choice never becomes VDS, completed images never change.</summary>
    public Task<VdsAutomationImageDecisionOutput> SaveStageAsync(VdsAutomationImageDecisionOutput decision, CancellationToken ct) =>
        AccessAsync(() => SaveStageCoreAsync(decision, ct), ct);
    private async Task<VdsAutomationImageDecisionOutput> SaveStageCoreAsync(VdsAutomationImageDecisionOutput decision, CancellationToken ct)
    {
        if (!decision.Matches(decision.TenantId, decision.IncidentId, decision.PictureType, decision.CaptureTime))
            throw new ArgumentException("Invalid stage image decision.");
        var existing = await ReadStageCoreAsync(decision.TenantId, decision.IncidentId, decision.PictureType, decision.CaptureTime, ct);
        if (existing is not null)
        {
            if (existing.RunId != decision.RunId) throw new InvalidDataException("Stage image belongs to another automation run.");
            if (existing.Status == "Ready" || existing.ImageSource == "camera" && decision.ImageSource == "vds") return existing;
        }
        var folder = Path.Combine(Root, "stages");
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, StageKey(decision.TenantId, decision.IncidentId, decision.PictureType, decision.CaptureTime) + ".json");
        var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(decision), ct);
            File.Move(temp, target, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return decision;
    }
}
