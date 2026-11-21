using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.Storage;

/// <summary>
/// Description: Kho lưu trữ danh sách Screen gán theo thiết bị VideoWall dưới dạng file JSON cục bộ
/// Created date: 13/09/2026
/// </summary>
public static class VwLocalScreenStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string GetDefaultDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Module.VideoWall.WPF", "Screens");
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
        var filePath = GetFilePath(deviceKey, baseDirectory);
        return File.Exists(filePath);
    }

    private static readonly object FileLock = new();

    public static List<VwScreenDto> Load(string deviceKey, string? baseDirectory = null)
    {
        lock (FileLock)
        {
            var filePath = GetFilePath(deviceKey, baseDirectory);
            if (!File.Exists(filePath))
                return [];

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<VwScreenDto>>(json, JsonOptions) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    public static void Save(string deviceKey, List<VwScreenDto> screens, string? baseDirectory = null)
    {
        lock (FileLock)
        {
            var filePath = GetFilePath(deviceKey, baseDirectory);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(screens, JsonOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
