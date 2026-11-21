using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Module.VideoWall.WPF.Storage;

/// <summary>
/// Description: Mô hình dữ liệu kịch bản gọi chuỗi ISAPI (Scenario) lưu trữ cục bộ
/// Created date: 13/09/2026
/// </summary>
public sealed class VwScenarioData
{
    /// <summary>Tên kịch bản thử nghiệm / kiểm thử.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Danh sách các bước (step) thực thi trong kịch bản.</summary>
    public List<VwScenarioStepData> Steps { get; set; } = [];
}

/// <summary>
/// Description: Mô hình dữ liệu từng bước trong kịch bản gọi ISAPI
/// Created date: 13/09/2026
/// </summary>
public sealed class VwScenarioStepData
{
    /// <summary>Mã mục tiêu chuẩn ISAPI (Section, ví dụ 9.7.5.2).</summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>Các giá trị tham số URL đường dẫn tương ứng với bước.</summary>
    public Dictionary<string, string> PathFieldValues { get; set; } = [];

    /// <summary>Nội dung Body XML gửi kèm request ISAPI.</summary>
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Description: Kho lưu trữ và quản lý kịch bản ISAPI (Scenario) dạng JSON trên đĩa
/// Created date: 13/09/2026
/// </summary>
public static class VwScenarioStore
{
    public static string GetScenariosDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "Scenarios");
    }

    public static void EnsureDefaultsSeeded(string? customDirectory = null)
    {
        // Kịch bản mặc định được lưu trữ trực tiếp trong bộ nhớ (In-Memory), không cần tạo file ra đĩa
    }

    public static List<VwScenarioData> GetDefaultScenarios()
    {
        return
        [
            new VwScenarioData
            {
                Name = "1. Thiết lập scene (không chụp hình)",
                Steps =
                [
                    new VwScenarioStepData { Section = "9.7.5.2", PathFieldValues = new() { ["videoWallID"] = "1" } },
                    new VwScenarioStepData { Section = "9.7.5.3", PathFieldValues = new() { ["videoWallID"] = "1" }, Body = "<WallWindowList xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><WallWindow><id>1</id><WindowPosition><positionX>0</positionX><positionY>0</positionY><height>1080</height><width>1920</width></WindowPosition><LayerIdx>1</LayerIdx></WallWindow></WallWindowList>" },
                    new VwScenarioStepData { Section = "9.7.7.2", PathFieldValues = new() { ["videoWallID"] = "1", ["SID"] = "1" }, Body = "<Scene xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><id>1</id><name>Scene 1</name></Scene>" },
                    new VwScenarioStepData { Section = "9.7.7.4", PathFieldValues = new() { ["videoWallID"] = "1", ["SID"] = "1" } }
                ]
            },
            new VwScenarioData
            {
                Name = "2. Thiết lập scene (có chụp hình)",
                Steps =
                [
                    new VwScenarioStepData { Section = "9.7.5.2", PathFieldValues = new() { ["videoWallID"] = "1" } },
                    new VwScenarioStepData { Section = "9.7.5.3", PathFieldValues = new() { ["videoWallID"] = "1" }, Body = "<WallWindowList xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><WallWindow><id>1</id><WindowPosition><positionX>0</positionX><positionY>0</positionY><height>1080</height><width>1920</width></WindowPosition><LayerIdx>1</LayerIdx></WallWindow></WallWindowList>" },
                    new VwScenarioStepData { Section = "9.7.7.2", PathFieldValues = new() { ["videoWallID"] = "1", ["SID"] = "1" }, Body = "<Scene xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><id>1</id><name>Scene 1</name></Scene>" },
                    new VwScenarioStepData { Section = "9.7.7.4", PathFieldValues = new() { ["videoWallID"] = "1", ["SID"] = "1" } },
                    new VwScenarioStepData { Section = "9.7.4.18", PathFieldValues = new() { ["channelID"] = "1" } }
                ]
            },
            new VwScenarioData
            {
                Name = "3. Active scene",
                Steps =
                [
                    new VwScenarioStepData { Section = "9.7.7.3", PathFieldValues = new() { ["videoWallID"] = "1", ["SID"] = "1" } },
                    new VwScenarioStepData { Section = "9.7.7.6", PathFieldValues = new() { ["videoWallID"] = "1" } }
                ]
            },
            new VwScenarioData
            {
                Name = "4. Màn hình không chồng nhau",
                Steps =
                [
                    new VwScenarioStepData { Section = "9.7.11.3", PathFieldValues = new() { ["videoWallID"] = "1" } },
                    new VwScenarioStepData { Section = "9.7.5.3", PathFieldValues = new() { ["videoWallID"] = "1" }, Body = "<WallWindowList xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><WallWindow><id>1</id><WindowPosition><positionX>0</positionX><positionY>0</positionY><height>540</height><width>960</width></WindowPosition><LayerIdx>1</LayerIdx></WallWindow><WallWindow><id>2</id><WindowPosition><positionX>960</positionX><positionY>0</positionY><height>540</height><width>960</width></WindowPosition><LayerIdx>1</LayerIdx></WallWindow></WallWindowList>" }
                ]
            },
            new VwScenarioData
            {
                Name = "5. Màn hình chồng nhau",
                Steps =
                [
                    new VwScenarioStepData { Section = "9.7.11.3", PathFieldValues = new() { ["videoWallID"] = "1" } },
                    new VwScenarioStepData { Section = "9.7.5.3", PathFieldValues = new() { ["videoWallID"] = "1" }, Body = "<WallWindowList xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><WallWindow><id>1</id><WindowPosition><positionX>0</positionX><positionY>0</positionY><height>1080</height><width>1920</width></WindowPosition><LayerIdx>1</LayerIdx></WallWindow><WallWindow><id>2</id><WindowPosition><positionX>480</positionX><positionY>270</positionY><height>540</height><width>960</width></WindowPosition><LayerIdx>2</LayerIdx></WallWindow></WallWindowList>" }
                ]
            }
        ];
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "scenario";

        var invalidChars = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalidChars.Contains(c) || c == ':' || c == '/' || c == '\\' ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "scenario" : clean.Trim();
    }

    public static string GetFilePath(string name, string? customDirectory = null)
    {
        var dir = customDirectory ?? GetScenariosDirectory();
        var cleanName = SanitizeFileName(name);
        return Path.Combine(dir, $"{cleanName}.json");
    }

    public static List<string> ListAll(string? customDirectory = null)
    {
        var result = GetDefaultScenarios().Select(s => s.Name).ToList();

        var dir = customDirectory ?? GetScenariosDirectory();
        if (Directory.Exists(dir))
        {
            var customFiles = Directory.GetFiles(dir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            foreach (var custom in customFiles)
            {
                if (!result.Contains(custom, StringComparer.OrdinalIgnoreCase))
                    result.Add(custom!);
            }
        }

        return result;
    }

    public static bool Exists(string name, string? customDirectory = null)
    {
        if (GetDefaultScenarios().Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            return true;

        var file = GetFilePath(name, customDirectory);
        return File.Exists(file);
    }

    public static void Save(VwScenarioData data, string? customDirectory = null)
    {
        var file = GetFilePath(data.Name, customDirectory);
        var dir = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(file, json, Encoding.UTF8);
    }

    public static VwScenarioData? Load(string name, string? customDirectory = null)
    {
        var defaultMatch = GetDefaultScenarios().FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        if (defaultMatch != null)
        {
            return new VwScenarioData
            {
                Name = defaultMatch.Name,
                Steps = defaultMatch.Steps.Select(s => new VwScenarioStepData
                {
                    Section = s.Section,
                    PathFieldValues = new Dictionary<string, string>(s.PathFieldValues),
                    Body = s.Body
                }).ToList()
            };
        }

        var file = GetFilePath(name, customDirectory);
        if (!File.Exists(file))
            return null;

        var json = File.ReadAllText(file, Encoding.UTF8);
        return JsonSerializer.Deserialize<VwScenarioData>(json);
    }

    public static bool Delete(string name, string? customDirectory = null)
    {
        var file = GetFilePath(name, customDirectory);
        if (File.Exists(file))
        {
            File.Delete(file);
            return true;
        }
        return false;
    }
}
