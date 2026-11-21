using Modules.VDS.Core.Dto;

namespace Modules.VDS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Shared on-disk conventions for the VDS media writer and readers.
/// Created date: 02/10/2026
/// </summary>
public class VdsMediaPathHelper
{
    public static readonly IReadOnlyDictionary<string, string> ImageTypes = new Dictionary<string, string>
    {
        [".jpeg"] = "image/jpeg", [".jpg"] = "image/jpeg", [".png"] = "image/png",
        [".webp"] = "image/webp", [".bmp"] = "image/bmp"
    };

    public static string Branch(string? eventType) =>
        string.Equals(eventType?.Trim(), "Normal", StringComparison.OrdinalIgnoreCase)
        || string.Equals(eventType?.Trim(), "Regular", StringComparison.OrdinalIgnoreCase)
            ? "data" : "event";

    public static string CameraFolder(string? camera)
    {
        var clean = new string((camera ?? "").Trim()
            .Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_').ToArray());
        return clean.Trim('.').Length == 0 ? "unknown" : clean;
    }

    public static string? FindFull(string root, VdsRegistrationImageSourceOutput source)
    {
        var utc = DateTime.SpecifyKind(source.DetectTimeUtc, DateTimeKind.Utc);
        var hour = Path.Combine(Path.GetFullPath(root), Branch(source.EventType), utc.ToString("yyyyMM"), utc.ToString("ddHH"));
        var stem = $"{utc:yyyyMMddHHmmssfff}_f";
        // A legacy hour-level file cannot prove camera ownership. Registration requires the camera folder.
        var dir = Path.Combine(hour, CameraFolder(source.CameraIp));
        foreach (var ext in ImageTypes.Keys)
        {
            var path = Path.Combine(dir, stem + ext);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public static bool IsVdsRegistration(string? source, int pictureType) =>
        pictureType == 1 && string.Equals(source?.Trim(), "vds", StringComparison.OrdinalIgnoreCase);
}
