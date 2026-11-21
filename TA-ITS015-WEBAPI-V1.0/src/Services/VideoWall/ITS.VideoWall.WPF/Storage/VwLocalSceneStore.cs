using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Module.VideoWall.WPF.Api.Direct.Isapi;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.Storage;

/// <summary>
/// Description: Mô hình dữ liệu lưu trữ Scene cục bộ trên đĩa theo từng thiết bị VideoWall
/// Created date: 13/09/2026
/// </summary>
public sealed class VwLocalSceneData
{
    /// <summary>ID của kịch bản đang được kích hoạt.</summary>
    public string? ActiveSceneId { get; set; }

    /// <summary>Danh sách kịch bản (VwSceneDto) đã lưu.</summary>
    public List<VwSceneDto> Scenes { get; set; } = [];

    /// <summary>Danh sách cửa sổ con trong các kịch bản (VwWindowSceneDto) đã lưu.</summary>
    public List<VwWindowSceneDto> Windows { get; set; } = [];
}

/// <summary>
/// Description: Kho lưu trữ kịch bản Scene và WindowScene cục bộ dưới dạng JSON
/// Created date: 13/09/2026
/// </summary>
public static class VwLocalSceneStore
{
    private const string SceneCodePrefix = "VWSCENE";
    private const string WindowCodePrefix = "VWWIN";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };

    public static string GetDefaultDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "Scenes");
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

    public static VwLocalSceneData LoadData(string deviceKey, string? baseDirectory = null)
    {
        lock (FileLock)
        {
            var filePath = GetFilePath(deviceKey, baseDirectory);
            if (!File.Exists(filePath))
                return new VwLocalSceneData();

            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<VwLocalSceneData>(json, JsonOptions) ?? new VwLocalSceneData();

                // Tự động đồng bộ nâng cấp nếu có Scene mẫu phiên bản cũ (16 cam hoặc Wall 1 dạng 1x2)
                var sample1 = data.Scenes.FirstOrDefault(s => s.Code == "VWSCENE_SAMPLE_01");
                var isOldSample1 = sample1 != null && (sample1.GridRows == 4 || (sample1.Name != null && sample1.Name.Contains("16 Cam")));
                var sampleW1 = data.Scenes.FirstOrDefault(s => s.Code == "VWSCENE_SAMPLE_W1_01");
                var isOldSampleW1 = sampleW1 != null && sampleW1.GridCols == 1;

                if (isOldSample1 || isOldSampleW1)
                {
                    var wallNo = ExtractWallNo(deviceKey);
                    PopulateSampleScenes(data, wallNo);
                }

                // Tự động chuẩn hoá toạ độ cũ (1080p / 2160p) sang hệ toạ độ chuẩn UniformCoordinate (1920x1920 tile)
                var modified = isOldSample1 || isOldSampleW1;
                foreach (var win in data.Windows)
                {
                    if (win.H == 2160)
                    {
                        win.H = 3840;
                        modified = true;
                    }
                    else if (win.H == 1080)
                    {
                        win.H = 1920;
                        modified = true;
                    }

                    if (win.Y == 2160)
                    {
                        win.Y = 3840;
                        modified = true;
                    }
                }

                if (modified)
                {
                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    var updatedJson = JsonSerializer.Serialize(data, JsonOptions);
                    File.WriteAllText(filePath, updatedJson);
                }

                return data;
            }
            catch
            {
                return new VwLocalSceneData();
            }
        }
    }

    public static void SaveData(string deviceKey, VwLocalSceneData data, string? baseDirectory = null)
    {
        lock (FileLock)
        {
            var filePath = GetFilePath(deviceKey, baseDirectory);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);
        }
    }

    public static List<VwSceneDto> ListScenes(string deviceKey, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        return data.Scenes;
    }

    public static List<VwSceneDto> SyncScenesFromDevice(
        string deviceKey,
        int wallNo,
        IReadOnlyList<VwWallSceneSummary> deviceScenes,
        string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        var changed = false;
        var deviceSids = deviceScenes.Select(d => d.Id.ToString()).ToHashSet();

        // 1. Loại bỏ các kịch bản cũ không còn tồn tại trên thiết bị phần cứng
        var staleScenes = data.Scenes.Where(s => string.IsNullOrWhiteSpace(s.OutputId) || !deviceSids.Contains(s.OutputId)).ToList();
        if (staleScenes.Count > 0)
        {
            var staleIds = staleScenes.Select(s => s.ID).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet();
            data.Scenes.RemoveAll(s => staleIds.Contains(s.ID));
            data.Windows.RemoveAll(w => staleIds.Contains(w.SceneId));
            if (staleIds.Contains(data.ActiveSceneId))
            {
                data.ActiveSceneId = null;
            }
            changed = true;
        }

        // 2. Cập nhật hoặc thêm mới kịch bản từ thiết bị phần cứng
        foreach (var devScene in deviceScenes)
        {
            var sidStr = devScene.Id.ToString();
            var existing = data.Scenes.FirstOrDefault(s => s.OutputId == sidStr);
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(devScene.Name) && existing.Name != devScene.Name)
                {
                    existing.Name = devScene.Name;
                    changed = true;
                }
            }
            else
            {
                var newScene = new VwSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"{SceneCodePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}_{devScene.Id}",
                    Name = string.IsNullOrWhiteSpace(devScene.Name) ? $"Kịch bản {devScene.Id}" : devScene.Name,
                    OutputId = sidStr,
                    ControllerId = deviceKey,
                    GridCols = wallNo == 1 ? 1 : 2,
                    GridRows = 2,
                    Status = VwStatus.Enable,
                    ActiveScene = VwActiveScene.DeActivate,
                    Remark = "Đồng bộ từ thiết bị phần cứng",
                };
                data.Scenes.Add(newScene);
                changed = true;
            }
        }

        data.Scenes.Sort((a, b) =>
        {
            var aHasSid = int.TryParse(a.OutputId, out var aSid);
            var bHasSid = int.TryParse(b.OutputId, out var bSid);
            if (aHasSid && bHasSid) return aSid.CompareTo(bSid);
            if (aHasSid) return -1;
            if (bHasSid) return 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        if (changed)
        {
            SaveData(deviceKey, data, baseDirectory);
        }

        return data.Scenes;
    }

    public static List<VwWindowSceneDto> SyncWindowsFromDevice(
        string deviceKey,
        string sceneId,
        IReadOnlyList<VwISAPIWallWindowItem> deviceWindows,
        IReadOnlyList<VwSourceDto> sources,
        string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        data.Windows.RemoveAll(w => w.SceneId == sceneId);

        var order = 1;
        var newWindows = new List<VwWindowSceneDto>();
        foreach (var win in deviceWindows)
        {
            var winId = win.Id.ToString();
            var x = win.Rect?.Coordinate?.X ?? 0;
            var y = win.Rect?.Coordinate?.Y ?? 0;
            var w = win.Rect?.Width ?? 1920;
            var h = win.Rect?.Height ?? 1920;
            var z = win.LayerIdx > 0 ? win.LayerIdx : order;

            var mode = win.WindowMode <= 0 ? 1 : win.WindowMode;
            var subWindows = new List<VwSubWindowDto>();
            string? firstSourceId = null;
            string? firstSourceName = null;

            if (win.SubWindowList?.SubWindow != null && win.SubWindowList.SubWindow.Count > 0)
            {
                foreach (var sub in win.SubWindowList.SubWindow)
                {
                    var chId = sub.SubWindowParam?.VideoInputChannelId;
                    var streamUrl = sub.SubWindowParam?.StreamInput?.StreamInputRealtime?.StreamRealtimeUnitList?.FirstOrDefault()?.StreamInUrl?.Url;
                    var matched = sources.FirstOrDefault(s => s.ID == chId || s.SignalNo.ToString() == chId);
                    if (matched == null && !string.IsNullOrWhiteSpace(streamUrl))
                        matched = sources.FirstOrDefault(s => string.Equals(s.Url, streamUrl, StringComparison.OrdinalIgnoreCase));
                    if (matched != null && string.IsNullOrWhiteSpace(matched.Url) && !string.IsNullOrWhiteSpace(streamUrl))
                        matched.Url = streamUrl;

                    var resolvedId = matched?.ID ?? chId;
                    var resolvedName = matched?.Name ?? (!string.IsNullOrWhiteSpace(chId) ? $"Kênh {chId}" : (!string.IsNullOrWhiteSpace(streamUrl) ? streamUrl : null));

                    subWindows.Add(new VwSubWindowDto
                    {
                        SubIndex = sub.Id > 0 ? sub.Id : (subWindows.Count + 1),
                        SourceId = resolvedId,
                        Label = resolvedName,
                    });

                    if (firstSourceId == null)
                    {
                        firstSourceId = resolvedId;
                        firstSourceName = resolvedName;
                    }
                }
            }

            if (mode > subWindows.Count)
            {
                for (var i = subWindows.Count + 1; i <= mode; i++)
                {
                    subWindows.Add(new VwSubWindowDto
                    {
                        SubIndex = i,
                        SourceId = null,
                        Label = null,
                    });
                }
            }

            var sourceId = firstSourceId;
            var sourceName = firstSourceName;

            var dto = new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{WindowCodePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}_{win.Id}_{order}",
                SceneId = sceneId,
                DeviceWindowId = winId,
                Name = sourceName != null ? $"Ô {order}: {sourceName}" : $"Cửa sổ {win.Id}",
                Label = sourceName ?? $"Cửa sổ {win.Id}",
                SourceId = sourceId,
                WindowMode = mode,
                SubWindows = subWindows,
                X = x,
                Y = y,
                W = w,
                H = h,
                ZIndex = z,
                OrderNo = order++,
                Visible = VwWindowVisible.Visible,
            };

            data.Windows.Add(dto);
            newWindows.Add(dto);
        }

        SaveData(deviceKey, data, baseDirectory);
        return newWindows;
    }

    public static VwSceneDto AddScene(string deviceKey, VwSceneDto scene, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        if (string.IsNullOrWhiteSpace(scene.ID))
            scene.ID = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(scene.Code))
            scene.Code = $"{SceneCodePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}_{Random.Shared.Next(100, 999)}";

        data.Scenes.RemoveAll(s => s.ID == scene.ID);
        data.Scenes.Add(scene);
        SaveData(deviceKey, data, baseDirectory);
        return scene;
    }

    public static void UpdateScene(string deviceKey, VwSceneDto scene, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        var index = data.Scenes.FindIndex(s => s.ID == scene.ID);
        if (index >= 0)
            data.Scenes[index] = scene;
        else
            data.Scenes.Add(scene);

        SaveData(deviceKey, data, baseDirectory);
    }

    public static void DeleteScene(string deviceKey, string sceneId, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        data.Scenes.RemoveAll(s => s.ID == sceneId);
        // Cascade delete all windows belonging to this scene
        data.Windows.RemoveAll(w => w.SceneId == sceneId);

        if (data.ActiveSceneId == sceneId)
            data.ActiveSceneId = null;

        SaveData(deviceKey, data, baseDirectory);
    }

    public static List<VwWindowSceneDto> ListWindowScenes(string deviceKey, string sceneId, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        return data.Windows.Where(w => w.SceneId == sceneId).ToList();
    }

    public static VwWindowSceneDto AddWindowScene(string deviceKey, VwWindowSceneDto window, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        if (string.IsNullOrWhiteSpace(window.ID))
            window.ID = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(window.Code))
            window.Code = $"{WindowCodePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}_{Random.Shared.Next(100, 999)}";

        data.Windows.RemoveAll(w => w.ID == window.ID);
        data.Windows.Add(window);
        SaveData(deviceKey, data, baseDirectory);
        return window;
    }

    public static void UpdateWindowScene(string deviceKey, VwWindowSceneDto window, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        var index = data.Windows.FindIndex(w => w.ID == window.ID);
        if (index >= 0)
            data.Windows[index] = window;
        else
            data.Windows.Add(window);

        SaveData(deviceKey, data, baseDirectory);
    }

    public static void DeleteWindowScene(string deviceKey, string windowId, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        data.Windows.RemoveAll(w => w.ID == windowId);
        SaveData(deviceKey, data, baseDirectory);
    }

    public static void SaveWindowScenes(string deviceKey, string sceneId, IEnumerable<VwWindowSceneDto> windows, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        data.Windows.RemoveAll(w => w.SceneId == sceneId);
        foreach (var window in windows)
        {
            if (string.IsNullOrWhiteSpace(window.ID))
                window.ID = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(window.Code))
                window.Code = $"{WindowCodePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}_{Random.Shared.Next(100, 999)}";
            window.SceneId = sceneId;
            data.Windows.Add(window);
        }
        SaveData(deviceKey, data, baseDirectory);
    }

    public static void DeleteWindowScenes(string deviceKey, IEnumerable<string> windowIds, string? baseDirectory = null)
    {
        var idSet = windowIds.ToHashSet();
        var data = LoadData(deviceKey, baseDirectory);
        data.Windows.RemoveAll(w => !string.IsNullOrEmpty(w.ID) && idSet.Contains(w.ID));
        SaveData(deviceKey, data, baseDirectory);
    }

    public static VwSceneDto? GetActiveScene(string deviceKey, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        if (string.IsNullOrWhiteSpace(data.ActiveSceneId))
            return null;

        return data.Scenes.FirstOrDefault(s => s.ID == data.ActiveSceneId);
    }

    public static void SetActiveScene(string deviceKey, string sceneId, string? baseDirectory = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        data.ActiveSceneId = sceneId;
        SaveData(deviceKey, data, baseDirectory);
    }

    private static int ExtractWallNo(string deviceKey)
    {
        if (deviceKey.Contains("_wall_2", StringComparison.OrdinalIgnoreCase))
            return 2;
        if (deviceKey.Contains("_wall_1", StringComparison.OrdinalIgnoreCase))
            return 1;
        return 0;
    }

    public static List<VwSceneDto> PopulateSampleScenes(
        VwLocalSceneData data,
        int wallNo = 0,
        IReadOnlyList<VwSourceDto>? availableSources = null)
    {
        // Xoá bỏ toàn bộ các sample scene cũ
        data.Scenes.RemoveAll(s => s.Code != null && s.Code.StartsWith("VWSCENE_SAMPLE_"));
        data.Windows.RemoveAll(w => w.Code != null && w.Code.StartsWith("VWWIN_SAMPLE_"));

        if (wallNo == 1)
        {
            // BỘ KỊCH BẢN CHO WALL 1: 4 Màn hình lưới 2x2 (3840x3840 px) khớp phần cứng thiết bị
            // Kịch bản 1: Bố cục Giám sát Thực tế (Ô Màn 1 + Cụm 4 ô Màn 4 + Màn 2 & 3)
            var s1Id = Guid.NewGuid().ToString("N");
            var s1 = new VwSceneDto
            {
                ID = s1Id,
                Code = "VWSCENE_SAMPLE_W1_01",
                Name = "Kịch bản 1: Giám sát Trực ban (Màn 1 Đơn + Màn 4 Chia 4)",
                OutputId = "1",
                GridCols = 2,
                GridRows = 2,
                OrderNo = 1,
                Remark = "Bố cục thực tế hiện trường: Màn 1 đơn, Màn 4 chia 4 ô con, Màn 2 và Màn 3 camera toàn cảnh.",
            };
            data.Scenes.Add(s1);

            var src1 = (availableSources != null && availableSources.Count > 0) ? availableSources[0].ID : "1";
            var src2 = (availableSources != null && availableSources.Count > 1) ? availableSources[1].ID : "2";
            var src3 = (availableSources != null && availableSources.Count > 2) ? availableSources[2].ID : "3";
            var src4 = (availableSources != null && availableSources.Count > 3) ? availableSources[3].ID : "4";

            // Ô 1: Màn 1 (0, 0)
            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W1_01_01",
                SceneId = s1Id,
                Name = "Camera Giám sát Màn 1",
                Label = "Màn 1 (H1-C1)",
                SourceId = src1,
                WindowMode = 1,
                X = 0,
                Y = 0,
                W = 1920,
                H = 1920,
                ZIndex = 1,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            });

            // Ô 2: Màn 2 (1920, 0)
            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W1_01_02",
                SceneId = s1Id,
                Name = "Camera Tuyến Màn 2",
                Label = "Màn 2 (H1-C2)",
                SourceId = src2,
                WindowMode = 1,
                X = 1920,
                Y = 0,
                W = 1920,
                H = 1920,
                ZIndex = 1,
                OrderNo = 2,
                Visible = VwWindowVisible.Visible,
            });

            // Ô 3: Màn 3 (0, 1920)
            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W1_01_03",
                SceneId = s1Id,
                Name = "Camera Điểm Màn 3",
                Label = "Màn 3 (H2-C1)",
                SourceId = src3,
                WindowMode = 1,
                X = 0,
                Y = 1920,
                W = 1920,
                H = 1920,
                ZIndex = 1,
                OrderNo = 3,
                Visible = VwWindowVisible.Visible,
            });

            // Ô 4: Màn 4 (1920, 1920) - CHIA 4 Ô CON GIỐNG LOG VÀ GIAO DIỆN THỰC TẾ
            var subWindowsM4 = new List<VwSubWindowDto>();
            for (var subIdx = 1; subIdx <= 4; subIdx++)
            {
                var matchedSrc = (availableSources != null && availableSources.Count >= subIdx)
                    ? availableSources[subIdx - 1]
                    : null;
                subWindowsM4.Add(new VwSubWindowDto
                {
                    SubIndex = subIdx,
                    SourceId = matchedSrc?.ID ?? subIdx.ToString(),
                    Label = matchedSrc?.Name ?? $"Camera {subIdx}",
                });
            }

            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W1_01_04",
                SceneId = s1Id,
                Name = "Cụm Camera Màn 4 (Chia 4 ô)",
                Label = "Màn 4 (H2-C2)",
                SourceId = src4,
                WindowMode = 4,
                SubWindows = subWindowsM4,
                X = 1920,
                Y = 1920,
                W = 1920,
                H = 1920,
                ZIndex = 1,
                OrderNo = 4,
                Visible = VwWindowVisible.Visible,
            });

            // Kịch bản 2: 4 Màn hình Độc lập (Lưới 2x2)
            var s2Id = Guid.NewGuid().ToString("N");
            var s2 = new VwSceneDto
            {
                ID = s2Id,
                Code = "VWSCENE_SAMPLE_W1_02",
                Name = "Kịch bản 2: Giám sát 4 Ô Độc lập (Lưới 2×2)",
                OutputId = "2",
                GridCols = 2,
                GridRows = 2,
                OrderNo = 2,
                Remark = "4 Ô camera độc lập tương ứng 4 màn hình ghép của Wall 1.",
            };
            data.Scenes.Add(s2);

            var w1Screens = new (int X, int Y, string Name, string Label, int Ord)[]
            {
                (0, 0, "Trạm thu phí Bắc (Màn 1)", "Màn 1 (H1-C1)", 1),
                (1920, 0, "Trạm thu phí Nam (Màn 2)", "Màn 2 (H1-C2)", 2),
                (0, 1920, "Km10 Đoạn Trọng điểm (Màn 3)", "Màn 3 (H2-C1)", 3),
                (1920, 1920, "Khu vực Cửa khẩu (Màn 4)", "Màn 4 (H2-C2)", 4),
            };

            for (var i = 0; i < w1Screens.Length; i++)
            {
                var scr = w1Screens[i];
                var src = (availableSources != null && availableSources.Count > i) ? availableSources[i].ID : (i + 1).ToString();
                data.Windows.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"VWWIN_SAMPLE_W1_02_{i + 1:D2}",
                    SceneId = s2Id,
                    Name = scr.Name,
                    Label = scr.Label,
                    SourceId = src,
                    X = scr.X,
                    Y = scr.Y,
                    W = 1920,
                    H = 1920,
                    ZIndex = 1,
                    OrderNo = scr.Ord,
                    Visible = VwWindowVisible.Visible,
                });
            }

            // Kịch bản 3: Bản đồ GIS / Toàn tường (Phủ kín 2x2)
            var s3Id = Guid.NewGuid().ToString("N");
            var s3 = new VwSceneDto
            {
                ID = s3Id,
                Code = "VWSCENE_SAMPLE_W1_03",
                Name = "Kịch bản 3: Bản đồ GIS / Dashboard (Toàn tường 2×2)",
                OutputId = "3",
                GridCols = 2,
                GridRows = 2,
                OrderNo = 3,
                Remark = "1 Khung hình siêu lớn phủ kín toàn bộ 4 màn hình của Wall 1 (3840x3840 px).",
            };
            data.Scenes.Add(s3);

            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W1_03_01",
                SceneId = s3Id,
                Name = "Bản đồ GIS / Dashboard Điều hành Toàn Tuyến",
                Label = "Toàn tường Wall 1 (2x2)",
                SourceId = src1,
                X = 0,
                Y = 0,
                W = 3840,
                H = 3840,
                ZIndex = 1,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            });

            return [s1, s2, s3];
        }
        else if (wallNo == 2)
        {
            // BỘ KỊCH BẢN CHO WALL 2: 4 Màn hình lưới 2x2 (3840x3840 px)
            // Kịch bản 1: Giám sát 4 Điểm Độc lập (Lưới 2x2)
            var s1Id = Guid.NewGuid().ToString("N");
            var s1 = new VwSceneDto
            {
                ID = s1Id,
                Code = "VWSCENE_SAMPLE_W2_01",
                Name = "Kịch bản 1: Giám sát 4 Ô Độc lập (Lưới 2×2)",
                OutputId = "1",
                GridCols = 2,
                GridRows = 2,
                OrderNo = 1,
                Remark = "4 Ô camera tương ứng 4 màn hình ghép 2x2 của Wall 2.",
            };
            data.Scenes.Add(s1);

            var w2Screens = new (int X, int Y, string Name, string Label, int Ord)[]
            {
                (0, 0, "Trạm thu phí Bắc (Màn 1)", "Màn 1 (H1-C1)", 1),
                (1920, 0, "Trạm thu phí Nam (Màn 2)", "Màn 2 (H1-C2)", 2),
                (0, 1920, "Km10 Đoạn Trọng điểm (Màn 3)", "Màn 3 (H2-C1)", 3),
                (1920, 1920, "Khu vực Cửa khẩu (Màn 4)", "Màn 4 (H2-C2)", 4),
            };

            for (var i = 0; i < w2Screens.Length; i++)
            {
                var scr = w2Screens[i];
                var src = (availableSources != null && availableSources.Count > i) ? availableSources[i].ID : (i + 1).ToString();
                data.Windows.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"VWWIN_SAMPLE_W2_01_{i + 1:D2}",
                    SceneId = s1Id,
                    Name = scr.Name,
                    Label = scr.Label,
                    SourceId = src,
                    X = scr.X,
                    Y = scr.Y,
                    W = 1920,
                    H = 1920,
                    ZIndex = 1,
                    OrderNo = scr.Ord,
                    Visible = VwWindowVisible.Visible,
                });
            }

            // Kịch bản 2: Sự cố Trọng điểm (Khối lớn 2x1 + 2 Ô phụ)
            var s2Id = Guid.NewGuid().ToString("N");
            var s2 = new VwSceneDto
            {
                ID = s2Id,
                Code = "VWSCENE_SAMPLE_W2_02",
                Name = "Kịch bản 2: Sự cố Trọng điểm (Khối lớn 2×1 + 2 Ô phụ)",
                OutputId = "2",
                GridCols = 2,
                GridRows = 2,
                OrderNo = 2,
                Remark = "1 Khung ngang lớn ở hàng trên cho Sự cố/Toàn cảnh và 2 ô camera hỗ trợ ở hàng dưới.",
            };
            data.Scenes.Add(s2);

            var srcL = (availableSources != null && availableSources.Count > 0) ? availableSources[0].ID : "1";
            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W2_02_01",
                SceneId = s2Id,
                Name = "Khối lớn Sự cố (Hàng trên)",
                Label = "Hàng trên (Màn 1-2)",
                SourceId = srcL,
                X = 0,
                Y = 0,
                W = 3840,
                H = 1920,
                ZIndex = 1,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            });

            var srcB1 = (availableSources != null && availableSources.Count > 1) ? availableSources[1].ID : "2";
            var srcB2 = (availableSources != null && availableSources.Count > 2) ? availableSources[2].ID : "3";
            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W2_02_02",
                SceneId = s2Id,
                Name = "Camera Phụ 1 (Dưới-Trái)",
                Label = "Màn 3 (H2-C1)",
                SourceId = srcB1,
                X = 0,
                Y = 1920,
                W = 1920,
                H = 1920,
                ZIndex = 1,
                OrderNo = 2,
                Visible = VwWindowVisible.Visible,
            });
            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W2_02_03",
                SceneId = s2Id,
                Name = "Camera Phụ 2 (Dưới-Phải)",
                Label = "Màn 4 (H2-C2)",
                SourceId = srcB2,
                X = 1920,
                Y = 1920,
                W = 1920,
                H = 1920,
                ZIndex = 1,
                OrderNo = 3,
                Visible = VwWindowVisible.Visible,
            });

            // Kịch bản 3: Bản đồ GIS / Dashboard (Toàn tường 2x2)
            var s3Id = Guid.NewGuid().ToString("N");
            var s3 = new VwSceneDto
            {
                ID = s3Id,
                Code = "VWSCENE_SAMPLE_W2_03",
                Name = "Kịch bản 3: Bản đồ GIS / Dashboard (Toàn tường 2×2)",
                OutputId = "3",
                GridCols = 2,
                GridRows = 2,
                OrderNo = 3,
                Remark = "1 Khung hình siêu lớn phủ kín toàn bộ 4 màn hình của Wall 2.",
            };
            data.Scenes.Add(s3);

            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_W2_03_01",
                SceneId = s3Id,
                Name = "Bản đồ GIS / Dashboard Điều hành",
                Label = "Toàn tường Wall 2 (2x2)",
                SourceId = srcL,
                X = 0,
                Y = 0,
                W = 3840,
                H = 3840,
                ZIndex = 1,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            });

            return [s1, s2, s3];
        }
        else
        {
            // MẪU CHO TƯỜNG 12 MÀN (4x3) — 4 KỊCH BẢN CHUẨN TRANSCRIPT (TỌA ĐỘ 1920x1920, CANVAS 7680x5760)
            string Src(int i) => (availableSources != null && availableSources.Count > 0)
                ? (availableSources[i % availableSources.Count].ID ?? (i + 1).ToString())
                : (i + 1).ToString();

            // KB 1: Lưới 12 ô — Không chồng (mỗi màn 1 camera)
            var scene1Id = Guid.NewGuid().ToString("N");
            var scene1 = new VwSceneDto
            {
                ID = scene1Id,
                Code = "VWSCENE_SAMPLE_01",
                Name = "Kịch bản 1: Lưới 12 ô — Không chồng (mỗi màn 1 camera)",
                OutputId = "1",
                GridCols = 4,
                GridRows = 3,
                OrderNo = 1,
                Remark = "Bố cục chuẩn 12 ô độc lập tương ứng 12 màn hình (4 cột x 3 hàng), mỗi màn hình gán 1 luồng camera riêng biệt.",
            };
            data.Scenes.Add(scene1);

            for (var n = 0; n < 12; n++)
            {
                var col = n % 4;
                var row = n / 4;
                data.Windows.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"VWWIN_SAMPLE_01_{n + 1:D2}",
                    SceneId = scene1Id,
                    Name = $"Camera Màn {n + 1}",
                    Label = $"Màn {n + 1} (H{row + 1}-C{col + 1})",
                    SourceId = Src(n),
                    X = col * 1920,
                    Y = row * 1920,
                    W = 1920,
                    H = 1920,
                    ZIndex = 1,
                    OrderNo = n + 1,
                    Visible = VwWindowVisible.Visible,
                });
            }

            // KB 2: Cửa sổ CHỒNG nhau (nền 12 ô + khối 2×2 đè lên)
            var scene2Id = Guid.NewGuid().ToString("N");
            var scene2 = new VwSceneDto
            {
                ID = scene2Id,
                Code = "VWSCENE_SAMPLE_02",
                Name = "Kịch bản 2: Cửa sổ CHỒNG nhau (nền 12 ô + khối 2×2 đè lên)",
                OutputId = "2",
                GridCols = 4,
                GridRows = 3,
                OrderNo = 2,
                Remark = "Bố cục kiểm thử Z-Order: nền 12 ô camera (Z=1) và 1 khối phóng to 2x2 đè lên ở giữa (cột 2-3, hàng 1-2, Z=2).",
            };
            data.Scenes.Add(scene2);

            for (var n = 0; n < 12; n++)
            {
                var col = n % 4;
                var row = n / 4;
                data.Windows.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"VWWIN_SAMPLE_02_{n + 1:D2}",
                    SceneId = scene2Id,
                    Name = $"Camera Màn {n + 1}",
                    Label = $"Màn {n + 1} (H{row + 1}-C{col + 1})",
                    SourceId = Src(n),
                    X = col * 1920,
                    Y = row * 1920,
                    W = 1920,
                    H = 1920,
                    ZIndex = 1,
                    OrderNo = n + 1,
                    Visible = VwWindowVisible.Visible,
                });
            }

            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_02_13",
                SceneId = scene2Id,
                Name = "Khối phóng to 2×2 (đè cột 2–3, hàng 1–2)",
                Label = "Khối phóng to 2x2 đè lên (Z=2)",
                SourceId = Src(0),
                X = 1920,
                Y = 0,
                W = 3840,
                H = 3840,
                ZIndex = 2,
                OrderNo = 13,
                Visible = VwWindowVisible.Visible,
            });

            // KB 3: Sự cố / Trigger (1 camera phóng to + phần còn lại nhỏ)
            var scene3Id = Guid.NewGuid().ToString("N");
            var scene3 = new VwSceneDto
            {
                ID = scene3Id,
                Code = "VWSCENE_SAMPLE_03",
                Name = "Kịch bản 3: Sự cố / Trigger (1 camera phóng to + phần còn lại nhỏ)",
                OutputId = "3",
                GridCols = 4,
                GridRows = 3,
                OrderNo = 3,
                Remark = "Bố cục kích hoạt sự cố: 1 camera trọng điểm phóng to 2 cột x 3 hàng ở bên trái (Z=2) và 6 ô nhỏ bên phải (Z=1).",
            };
            data.Scenes.Add(scene3);

            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_03_01",
                SceneId = scene3Id,
                Name = "Camera SỰ CỐ (phóng to 2 cột × 3 hàng)",
                Label = "Camera Sự cố (Cột 1-2, Hàng 1-3)",
                SourceId = Src(0),
                X = 0,
                Y = 0,
                W = 3840,
                H = 5760,
                ZIndex = 2,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            });

            var scene3Surrounding = new (int Col, int Row)[]
            {
                (2, 0), (3, 0),
                (2, 1), (3, 1),
                (2, 2), (3, 2),
            };

            for (var i = 0; i < scene3Surrounding.Length; i++)
            {
                var (c, r) = scene3Surrounding[i];
                data.Windows.Add(new VwWindowSceneDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    Code = $"VWWIN_SAMPLE_03_{i + 2:D2}",
                    SceneId = scene3Id,
                    Name = $"Camera phụ (H{r + 1}-C{c + 1})",
                    Label = $"Màn (H{r + 1}-C{c + 1})",
                    SourceId = Src(i + 1),
                    X = c * 1920,
                    Y = r * 1920,
                    W = 1920,
                    H = 1920,
                    ZIndex = 1,
                    OrderNo = i + 2,
                    Visible = VwWindowVisible.Visible,
                });
            }

            // KB 4: Camera Nhà xe / Bản đồ GIS — Toàn tường
            var scene4Id = Guid.NewGuid().ToString("N");
            var scene4 = new VwSceneDto
            {
                ID = scene4Id,
                Code = "VWSCENE_SAMPLE_04",
                Name = "Kịch bản 4: Camera Nhà xe / Bản đồ GIS — Toàn tường",
                OutputId = "4",
                GridCols = 4,
                GridRows = 3,
                OrderNo = 4,
                Remark = "Bố cục toàn tường: 1 khung hình camera nhà xe hoặc bản đồ GIS phủ kín toàn bộ 12 màn hình (4 cột x 3 hàng).",
            };
            data.Scenes.Add(scene4);

            data.Windows.Add(new VwWindowSceneDto
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = "VWWIN_SAMPLE_04_01",
                SceneId = scene4Id,
                Name = "Camera Nhà xe / Bản đồ GIS (Toàn tường 4×3)",
                Label = "Toàn tường 12 màn (4x3)",
                SourceId = Src(0),
                X = 0,
                Y = 0,
                W = 7680,
                H = 5760,
                ZIndex = 1,
                OrderNo = 1,
                Visible = VwWindowVisible.Visible,
            });

            return [scene1, scene2, scene3, scene4];
        }
    }

    public static List<VwSceneDto> SeedSampleScenes(
        string deviceKey,
        int wallNo = 1,
        string? baseDirectory = null,
        IReadOnlyList<VwSourceDto>? availableSources = null)
    {
        var data = LoadData(deviceKey, baseDirectory);
        var seeded = PopulateSampleScenes(data, wallNo, availableSources);
        SaveData(deviceKey, data, baseDirectory);
        return seeded;
    }

    public static List<VwSceneDto> SeedSampleScenes(
        string deviceKey,
        string? baseDirectory,
        IReadOnlyList<VwSourceDto>? availableSources) => SeedSampleScenes(deviceKey, 0, baseDirectory, availableSources);
}
