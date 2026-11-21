using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.Storage;

/// <summary>
/// Description: Kho lưu trữ danh sách Source gán theo thiết bị VideoWall dưới dạng file JSON cục bộ
/// Created date: 13/09/2026
/// </summary>
public static class VwLocalSourceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly object FileLock = new();

    public static string NormalizeControllerKey(string? deviceKey)
    {
        if (string.IsNullOrWhiteSpace(deviceKey))
            return "127.0.0.1";

        var key = deviceKey.Trim();
        var wallIndex = key.IndexOf("_wall_", StringComparison.OrdinalIgnoreCase);
        if (wallIndex > 0)
            key = key[..wallIndex];

        return string.IsNullOrWhiteSpace(key) ? "127.0.0.1" : key;
    }

    public static List<VwSourceDto> GetSampleSources(string? controllerId = null)
    {
        return
        [
            new()
            {
                ID = "sample_cam_01",
                Code = "CAM_KM01",
                Name = "Camera Tuyến Km01+200 (Bắc)",
                SourceType = "ip_stream",
                SignalType = "RTSP",
                Url = "rtsp://172.25.0.101:554/live/ch0",
                AspectRatio = "16:9",
                OrderNo = 101,
                ControllerId = controllerId,
                Remark = "Luồng RTSP chính tuyến cao tốc",
            },
            new()
            {
                ID = "sample_cam_02",
                Code = "CAM_KM05",
                Name = "Camera Tuyến Km05+800 (Nam)",
                SourceType = "ip_stream",
                SignalType = "RTSP",
                Url = "rtsp://172.25.0.102:554/live/ch0",
                AspectRatio = "16:9",
                OrderNo = 102,
                ControllerId = controllerId,
                Remark = "Luồng RTSP nút giao liên thông",
            },
            new()
            {
                ID = "sample_cam_03",
                Code = "CAM_TOLL_01",
                Name = "Camera Trạm Thu Phí Làn 01",
                SourceType = "ip_stream",
                SignalType = "RTSP",
                Url = "rtsp://172.25.0.103:554/live/ch0",
                AspectRatio = "16:9",
                OrderNo = 103,
                ControllerId = controllerId,
                Remark = "Luồng RTSP giám sát làn thu phí",
            },
            new()
            {
                ID = "sample_cam_04",
                Code = "CAM_TUNNEL_01",
                Name = "Camera Hầm Cửa Bắc",
                SourceType = "ip_stream",
                SignalType = "ONVIF",
                Url = "http://172.25.0.104:80/onvif/device_service",
                AspectRatio = "16:9",
                OrderNo = 104,
                ControllerId = controllerId,
                Remark = "Luồng ONVIF an ninh hầm",
            },
        ];
    }

    public static string GetDefaultDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Module.VideoWall.WPF", "Sources");
    }

    public static string SanitizeDeviceKey(string deviceKey)
    {
        if (string.IsNullOrWhiteSpace(deviceKey))
            return "default";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(deviceKey.Select(ch => invalidChars.Contains(ch) || ch == ':' ? '_' : ch));
        return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
    }

    public static string GetFilePath(string deviceKey, string? baseDirectory = null)
    {
        var dir = baseDirectory ?? GetDefaultDirectory();
        return Path.Combine(dir, $"{SanitizeDeviceKey(deviceKey)}.json");
    }

    public static bool Exists(string deviceKey, string? baseDirectory = null)
    {
        var normalizedKey = NormalizeControllerKey(deviceKey);
        var filePath = GetFilePath(normalizedKey, baseDirectory);
        if (File.Exists(filePath))
            return true;

        var directPath = GetFilePath(deviceKey, baseDirectory);
        return File.Exists(directPath);
    }

    public static List<VwSourceDto> Load(string deviceKey, string? baseDirectory = null)
    {
        lock (FileLock)
        {
            var normalizedKey = NormalizeControllerKey(deviceKey);
            var filePath = GetFilePath(normalizedKey, baseDirectory);

            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var list = JsonSerializer.Deserialize<List<VwSourceDto>>(json, JsonOptions);
                    if (list is { Count: > 0 })
                        return list;
                }
                catch
                {
                }
            }

            if (!string.Equals(normalizedKey, deviceKey, StringComparison.OrdinalIgnoreCase))
            {
                var legacyPath = GetFilePath(deviceKey, baseDirectory);
                if (File.Exists(legacyPath))
                {
                    try
                    {
                        var json = File.ReadAllText(legacyPath);
                        var legacyList = JsonSerializer.Deserialize<List<VwSourceDto>>(json, JsonOptions);
                        if (legacyList is { Count: > 0 })
                        {
                            Save(normalizedKey, legacyList, baseDirectory);
                            return legacyList;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            var dir = baseDirectory ?? GetDefaultDirectory();
            if (Directory.Exists(dir))
            {
                var legacyFiles = Directory.GetFiles(dir, $"{SanitizeDeviceKey(normalizedKey)}_wall_*.json");
                if (legacyFiles.Length > 0)
                {
                    var mergedLegacy = new List<VwSourceDto>();
                    foreach (var legacyFile in legacyFiles)
                    {
                        try
                        {
                            var json = File.ReadAllText(legacyFile);
                            var items = JsonSerializer.Deserialize<List<VwSourceDto>>(json, JsonOptions);
                            if (items != null)
                                mergedLegacy = Merge(mergedLegacy, items);
                        }
                        catch
                        {
                        }
                    }

                    if (mergedLegacy.Count > 0)
                    {
                        Save(normalizedKey, mergedLegacy, baseDirectory);
                        return mergedLegacy;
                    }
                }
            }

            var samples = GetSampleSources(normalizedKey);
            try
            {
                Save(normalizedKey, samples, baseDirectory);
            }
            catch
            {
            }
            return samples;
        }
    }

    public static void Save(string deviceKey, IEnumerable<VwSourceDto> sources, string? baseDirectory = null)
    {
        lock (FileLock)
        {
            var normalizedKey = NormalizeControllerKey(deviceKey);
            var filePath = GetFilePath(normalizedKey, baseDirectory);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(sources.ToList(), JsonOptions);
            File.WriteAllText(filePath, json);
        }
    }

    public static void SaveSource(string deviceKey, VwSourceDto source, string? baseDirectory = null)
    {
        lock (FileLock)
        {
            var normalizedKey = NormalizeControllerKey(deviceKey);
            var existing = Exists(normalizedKey, baseDirectory) ? Load(normalizedKey, baseDirectory) : [];
            var index = existing.FindIndex(s => s.ID == source.ID);
            if (index >= 0)
                existing[index] = source;
            else
                existing.Add(source);

            Save(normalizedKey, existing, baseDirectory);
        }
    }

    public static bool DeleteSource(string deviceKey, string sourceId, string? baseDirectory = null)
    {
        lock (FileLock)
        {
            var normalizedKey = NormalizeControllerKey(deviceKey);
            if (!Exists(normalizedKey, baseDirectory))
                return false;

            var existing = Load(normalizedKey, baseDirectory);
            var removed = existing.RemoveAll(s => s.ID == sourceId) > 0;
            if (removed)
                Save(normalizedKey, existing, baseDirectory);
            return removed;
        }
    }

    public static List<VwSourceDto> Merge(IEnumerable<VwSourceDto> probeSources, IEnumerable<VwSourceDto> customSources)
    {
        var result = new List<VwSourceDto>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var probeList = probeSources.ToList();
        var customList = customSources.ToList();

        foreach (var src in probeList)
        {
            var custom = customList.FirstOrDefault(c => string.Equals(c.ID, src.ID, StringComparison.OrdinalIgnoreCase)
                || (c.SignalNo.HasValue && src.SignalNo.HasValue && c.SignalNo == src.SignalNo));
            if (custom != null)
            {
                if (!string.IsNullOrWhiteSpace(custom.Url) && !custom.Url.Contains("172.25.0.10"))
                    src.Url = custom.Url;
                if (!string.IsNullOrWhiteSpace(custom.Name))
                    src.Name = custom.Name;
            }

            if (!string.IsNullOrWhiteSpace(src.ID) && seenIds.Add(src.ID))
                result.Add(src);
        }

        foreach (var src in customList)
        {
            if (probeList.Any(p => p.SourceType == "ip_stream") && src.ID?.StartsWith("sample_cam_", StringComparison.OrdinalIgnoreCase) == true)
                continue;

            if (!string.IsNullOrWhiteSpace(src.ID) && seenIds.Add(src.ID))
                result.Add(src);
        }

        return result
            .OrderBy(s => s.OrderNo ?? 1000)
            .ThenBy(s => s.Name ?? s.Code ?? string.Empty)
            .ToList();
    }
}
