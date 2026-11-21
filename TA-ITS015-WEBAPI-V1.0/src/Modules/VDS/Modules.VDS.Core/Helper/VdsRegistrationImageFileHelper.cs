using Modules.VDS.Core.Dto;

namespace Modules.VDS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Imports registration full and copies an already Ready registration for automation; copies never wait.
/// Created date: 02/10/2026
/// </summary>
public class VdsRegistrationImageFileHelper
{
    /// <summary>Publish selected VDS evidence into the incident's existing camera/stage slots.</summary>
    public static async Task PublishToIncidentSlotsAsync(string sourcePath, string incidentId,
        IEnumerable<string> equipmentIds, int pictureType, CancellationToken ct)
    {
        if (pictureType is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(pictureType));
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var source = new FileInfo(sourcePath);
        if (!source.Exists || source.Length is <= 0 or > 10 * 1024 * 1024)
            throw new IOException("Selected incident image is missing or exceeds the size limit.");
        var bytes = await File.ReadAllBytesAsync(sourcePath, ct);
        if (!MatchesImage(bytes, extension)) throw new IOException("Selected incident image is invalid.");
        foreach (var equipmentId in equipmentIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var target = Path.Combine(source.DirectoryName!, IncidentSlotFileName(incidentId, equipmentId, pictureType, extension));
            if (string.Equals(Path.GetFullPath(sourcePath), target, StringComparison.OrdinalIgnoreCase)) continue;
            var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temp, bytes, ct);
                File.Move(temp, target, overwrite: true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }

    public static string IncidentSlotFileName(string incidentId, string equipmentId, int pictureType, string extension)
    {
        if (pictureType is < 1 or > 3 || !VdsMediaPathHelper.ImageTypes.ContainsKey(extension))
            throw new ArgumentException("Invalid incident image stage or extension.");
        static string Part(string value) => new(value.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
        return $"{Part(incidentId)}_{Part(equipmentId)}_{pictureType}{extension}";
    }

    public static string FileName(VdsRegistrationImageInput request, string extension)
        => FileName(request.IncidentId, request.Source.EquipmentId, 1, extension);

    public static string FileName(string incidentId, string equipmentId, int pictureType, string extension)
    {
        if (pictureType is < 1 or > 3 || !VdsMediaPathHelper.ImageTypes.ContainsKey(extension))
            throw new ArgumentException("Invalid VDS image stage or extension.");
        static string Part(string value) => new(value.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
        return $"{Part(incidentId)}_vds_{Part(equipmentId)}_{pictureType}{extension}";
    }

    public static bool IsReady(VdsRegistrationImageInput request, string directory)
    {
        if (request.Status != "Ready" || request.FileName is not { } name || Path.GetFileName(name) != name) return false;
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!VdsMediaPathHelper.ImageTypes.TryGetValue(extension, out var mime) || request.ContentType != mime
            || name != FileName(request, extension)) return false;
        var file = new FileInfo(Path.Combine(directory, name));
        return file.Exists && file.Length is > 0 and <= 10 * 1024 * 1024;
    }

    /// <summary>Only an already registered image is copied; this method never probes or waits for raw full.</summary>
    public static async Task<VdsAutomationImageDecisionOutput> CopyToStageAsync(VdsAutomationImageDecisionOutput decision,
        string registrationDirectory, string destination, CancellationToken ct)
    {
        if (decision.ImageSource != "vds" || decision.PictureType is not (2 or 3) || decision.Source is null
            || decision.FileName is not { } name) return decision.UseCamera("RegistrationNotReady");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!VdsMediaPathHelper.ImageTypes.TryGetValue(extension, out var mime) || decision.ContentType != mime
            || name != FileName(decision.IncidentId, decision.Source.EquipmentId, decision.PictureType, extension))
            return decision.UseCamera("RegistrationFileInvalid");
        var original = new VdsRegistrationImageInput(decision.TenantId, decision.IncidentId, decision.RegistrationTime,
            DateTime.MaxValue, decision.Source) { Status = "Ready", ContentType = mime,
                FileName = FileName(decision.IncidentId, decision.Source.EquipmentId, 1, extension) };
        try
        {
            if (!IsReady(original, registrationDirectory)) return decision.UseCamera("RegistrationFileMissing");
            var bytes = await File.ReadAllBytesAsync(Path.Combine(registrationDirectory, original.FileName!), ct);
            if (bytes.Length > 10 * 1024 * 1024 || !MatchesImage(bytes, extension)) return decision.UseCamera("RegistrationFileInvalid");
            Directory.CreateDirectory(destination);
            var target = Path.Combine(destination, name);
            if (!File.Exists(target))
            {
                var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await File.WriteAllBytesAsync(temp, bytes, ct);
                    File.Move(temp, target, overwrite: false);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
            return decision with { Status = "Ready" };
        }
        catch (IOException) { return decision.UseCamera("RegistrationCopyFailed"); }
        catch (UnauthorizedAccessException) { return decision.UseCamera("RegistrationCopyFailed"); }
    }

    public static async Task<VdsRegistrationImageInput> ImportAsync(VdsRegistrationImageInput request,
        string mediaRoot, string destination, DateTime nowUtc, CancellationToken ct)
    {
        if (request.Status != "WaitingForSource") return request;
        if (nowUtc >= request.WaitUntilUtc) return request with { Status = "MissingSource", Error = "WaitDeadlineExceeded" };
        var source = VdsMediaPathHelper.FindFull(mediaRoot, request.Source);
        if (source is null) return request;
        if (new FileInfo(source).Length > 10 * 1024 * 1024) return request with { Error = "FullImageTooLarge" };
        var extension = Path.GetExtension(source).ToLowerInvariant();
        var bytes = await File.ReadAllBytesAsync(source, ct);
        if (bytes.Length > 10 * 1024 * 1024 || !MatchesImage(bytes, extension))
            return request with { Error = "InvalidFullImage" };
        Directory.CreateDirectory(destination);
        // Incident IDs come from SQL, source IDs from the catalog. Sanitize both file segments.
        var name = FileName(request, extension);
        var target = Path.Combine(destination, name);
        if (!File.Exists(target))
        {
            var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temp, bytes, ct);
                File.Move(temp, target, overwrite: false);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        return request with { Status = "Ready", FileName = name, ContentType = VdsMediaPathHelper.ImageTypes[extension], Error = null };
    }

    private static bool MatchesImage(byte[] bytes, string extension) => extension switch
    {
        ".jpeg" or ".jpg" => bytes.Length >= 5 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff
            && bytes[^2] == 0xff && bytes[^1] == 0xd9,
        ".png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        ".webp" => bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        ".bmp" => bytes.Length >= 14 && bytes[0] == 'B' && bytes[1] == 'M',
        _ => false
    };
}
