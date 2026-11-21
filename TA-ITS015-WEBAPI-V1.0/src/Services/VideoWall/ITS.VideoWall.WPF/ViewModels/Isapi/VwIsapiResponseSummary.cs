using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Module.VideoWall.WPF.ViewModels.Isapi;

/// <summary>
/// Description: Thông tin tóm tắt và phân tích kết quả XML phản hồi từ ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed class VwIsapiResponseSummary
{
    /// <summary>Cho biết có nội dung phản hồi hay không.</summary>
    public bool HasResponse { get; init; }

    /// <summary>Tên thẻ gốc của gói tin XML phản hồi.</summary>
    public string RootElement { get; init; } = string.Empty;

    /// <summary>Tổng số phần tử con đếm được trong phản hồi.</summary>
    public int TotalItems { get; init; }

    /// <summary>Nhãn mô tả loại phần tử trong phản hồi.</summary>
    public string ItemTypeLabel { get; init; } = string.Empty;

    /// <summary>Số lượng mục ở trạng thái bình thường.</summary>
    public int NormalCount { get; init; }

    /// <summary>Số lượng mục ở trạng thái cảnh báo hoặc lỗi.</summary>
    public int WarningCount { get; init; }

    /// <summary>Huy hiệu trạng thái ngắn gọn hiển thị trên UI.</summary>
    public string StatusBadge { get; init; } = string.Empty;

    /// <summary>Cờ đánh dấu phản hồi biểu thị trạng thái thành công.</summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>Danh sách các huy hiệu thông tin chi tiết.</summary>
    public IReadOnlyList<string> Badges { get; init; } = [];

    public static VwIsapiResponseSummary Parse(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return new VwIsapiResponseSummary { HasResponse = false };
        }

        if (xml.Contains("BINARY IMAGE DATA") || xml.StartsWith("[data:image", StringComparison.OrdinalIgnoreCase) || xml.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            var matchSize = System.Text.RegularExpressions.Regex.Match(xml, @"Size:\s*([0-9,]+)\s*bytes", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var sizeStr = matchSize.Success ? matchSize.Groups[1].Value : "N/A";
            var matchType = System.Text.RegularExpressions.Regex.Match(xml, @"image/([a-zA-Z0-9]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var format = matchType.Success ? matchType.Groups[1].Value.ToUpperInvariant() : "JPEG";

            return new VwIsapiResponseSummary
            {
                HasResponse = true,
                RootElement = "Image",
                StatusBadge = "200 OK",
                IsSuccess = true,
                Badges = [$"🖼️ Ảnh {format}", $"📦 {sizeStr} bytes", "✅ Nhận ảnh nhị phân thành công"]
            };
        }

        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;
            if (root is null)
                return new VwIsapiResponseSummary { HasResponse = true, StatusBadge = "Phản hồi rỗng" };

            var rootName = root.Name.LocalName;
            var badges = new List<string>();

            // 1. ResponseStatus
            if (string.Equals(rootName, "ResponseStatus", StringComparison.OrdinalIgnoreCase))
            {
                var statusCode = root.Element(root.Name.Namespace + "statusCode")?.Value ?? root.Element("statusCode")?.Value;
                var statusString = root.Element(root.Name.Namespace + "statusString")?.Value ?? root.Element("statusString")?.Value ?? "OK";
                var subStatusCode = root.Element(root.Name.Namespace + "subStatusCode")?.Value ?? root.Element("subStatusCode")?.Value;
                var description = root.Element(root.Name.Namespace + "description")?.Value ?? root.Element("description")?.Value;

                var isSuccess = statusCode is "0" or "1" || string.Equals(statusString, "OK", StringComparison.OrdinalIgnoreCase);

                if (isSuccess)
                {
                    badges.Add("Thành công");
                    return new VwIsapiResponseSummary
                    {
                        HasResponse = true,
                        RootElement = rootName,
                        StatusBadge = "200 OK",
                        IsSuccess = true,
                        Badges = badges
                    };
                }

                if (!string.IsNullOrWhiteSpace(subStatusCode))
                    badges.Add(subStatusCode);
                if (!string.IsNullOrWhiteSpace(description))
                    badges.Add(description);

                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    StatusBadge = $"Lỗi ({statusString})",
                    IsSuccess = false,
                    Badges = badges
                };
            }

            // 2. VideoInputChannelList
            if (string.Equals(rootName, "VideoInputChannelList", StringComparison.OrdinalIgnoreCase))
            {
                var channels = root.Elements(root.Name.Namespace + "VideoInputChannel");
                var total = 0;
                var normal = 0;
                var warning = 0;

                foreach (var chan in channels)
                {
                    total++;
                    var st = (chan.Element(root.Name.Namespace + "videoInputChannelAccessStatus")?.Value
                              ?? chan.Element("videoInputChannelAccessStatus")?.Value ?? "").Trim();

                    if (string.Equals(st, "normal", StringComparison.OrdinalIgnoreCase))
                        normal++;
                    else if (!string.IsNullOrEmpty(st))
                        warning++;
                }

                badges.Add($"{total} Cổng vào (Inputs)");
                if (normal > 0)
                    badges.Add($"🟢 {normal} normal");
                if (warning > 0)
                    badges.Add($"🔴 {warning} notConnected");

                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    TotalItems = total,
                    ItemTypeLabel = "Cổng vào",
                    NormalCount = normal,
                    WarningCount = warning,
                    StatusBadge = "200 OK",
                    IsSuccess = warning == 0,
                    Badges = badges
                };
            }

            // 3. VideoOutputChannelList
            if (string.Equals(rootName, "VideoOutputChannelList", StringComparison.OrdinalIgnoreCase))
            {
                var channels = root.Elements(root.Name.Namespace + "VideoOutputChannel");
                var total = 0;
                var normal = 0;
                var warning = 0;

                foreach (var chan in channels)
                {
                    total++;
                    var st = (chan.Element(root.Name.Namespace + "outputPortAccessStatus")?.Value
                              ?? chan.Element("outputPortAccessStatus")?.Value ?? "").Trim();

                    if (string.Equals(st, "normal", StringComparison.OrdinalIgnoreCase))
                        normal++;
                    else if (!string.IsNullOrEmpty(st))
                        warning++;
                }

                badges.Add($"{total} Cổng ra (Outputs)");
                if (normal > 0)
                    badges.Add($"🟢 {normal} normal");
                if (warning > 0)
                    badges.Add($"🔴 {warning} notConnected");

                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    TotalItems = total,
                    ItemTypeLabel = "Cổng ra",
                    NormalCount = normal,
                    WarningCount = warning,
                    StatusBadge = "200 OK",
                    IsSuccess = warning == 0,
                    Badges = badges
                };
            }

            // 4. WallSceneList
            if (string.Equals(rootName, "WallSceneList", StringComparison.OrdinalIgnoreCase))
            {
                var scenes = root.Elements(root.Name.Namespace + "WallScene");
                var total = 0;
                foreach (var _ in scenes)
                    total++;

                badges.Add($"{total} Kịch bản (Scenes)");
                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    TotalItems = total,
                    StatusBadge = "200 OK",
                    IsSuccess = true,
                    Badges = badges
                };
            }

            // 5. WallPlanList
            if (string.Equals(rootName, "WallPlanList", StringComparison.OrdinalIgnoreCase))
            {
                var plans = root.Elements(root.Name.Namespace + "WallPlan");
                var total = 0;
                foreach (var _ in plans)
                    total++;

                badges.Add($"{total} Kế hoạch (Plans)");
                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    TotalItems = total,
                    StatusBadge = "200 OK",
                    IsSuccess = true,
                    Badges = badges
                };
            }

            // 6. WallWindowStatusList
            if (string.Equals(rootName, "WallWindowStatusList", StringComparison.OrdinalIgnoreCase))
            {
                var windows = root.Elements(root.Name.Namespace + "WallWindowStatus");
                var total = 0;
                var decoding = 0;

                foreach (var win in windows)
                {
                    total++;
                    var subList = win.Element(root.Name.Namespace + "SubWinStatusList") ?? win.Element("SubWinStatusList");
                    if (subList != null)
                    {
                        foreach (var sub in subList.Elements(root.Name.Namespace + "SubWinStatus"))
                        {
                            var isDec = (sub.Element(root.Name.Namespace + "isDecoding")?.Value ?? sub.Element("isDecoding")?.Value ?? "").Trim();
                            if (string.Equals(isDec, "true", StringComparison.OrdinalIgnoreCase))
                                decoding++;
                        }
                    }
                }

                badges.Add($"{total} Cửa sổ");
                if (decoding > 0)
                    badges.Add($"🟢 {decoding} isDecoding");

                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    TotalItems = total,
                    NormalCount = decoding,
                    StatusBadge = "200 OK",
                    IsSuccess = true,
                    Badges = badges
                };
            }

            // 7. DeviceInfo
            if (string.Equals(rootName, "DeviceInfo", StringComparison.OrdinalIgnoreCase))
            {
                var model = root.Element(root.Name.Namespace + "model")?.Value ?? root.Element("model")?.Value;
                var fw = root.Element(root.Name.Namespace + "firmwareVersion")?.Value ?? root.Element("firmwareVersion")?.Value;
                if (!string.IsNullOrWhiteSpace(model))
                    badges.Add(model);
                if (!string.IsNullOrWhiteSpace(fw))
                    badges.Add($"v{fw}");

                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    StatusBadge = "200 OK",
                    IsSuccess = true,
                    Badges = badges
                };
            }

            // 8. VideoWall
            if (string.Equals(rootName, "VideoWall", StringComparison.OrdinalIgnoreCase))
            {
                var id = root.Element(root.Name.Namespace + "id")?.Value ?? root.Element("id")?.Value ?? "1";
                var name = root.Element(root.Name.Namespace + "name")?.Value ?? root.Element("name")?.Value ?? $"Tường #{id}";
                var bindStatus = (root.Element(root.Name.Namespace + "wallBindOutputStatus")?.Value ?? root.Element("wallBindOutputStatus")?.Value ?? "").Trim();
                var isBound = string.Equals(bindStatus, "bound", StringComparison.OrdinalIgnoreCase);

                badges.Add($"Tường #{id}: {name}");
                badges.Add(isBound ? "🟢 bound" : "⚪ unbound");

                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    TotalItems = 1,
                    ItemTypeLabel = "Tường ghép",
                    StatusBadge = "200 OK",
                    IsSuccess = true,
                    Badges = badges
                };
            }

            // 9. VideoWallList
            if (string.Equals(rootName, "VideoWallList", StringComparison.OrdinalIgnoreCase))
            {
                var walls = root.Elements(root.Name.Namespace + "VideoWall");
                var total = 0;
                var boundCount = 0;

                foreach (var wall in walls)
                {
                    total++;
                    var bindStatus = (wall.Element(root.Name.Namespace + "wallBindOutputStatus")?.Value
                                      ?? wall.Element("wallBindOutputStatus")?.Value ?? "").Trim();
                    if (string.Equals(bindStatus, "bound", StringComparison.OrdinalIgnoreCase))
                        boundCount++;
                }

                badges.Add($"{total} Tường (VideoWalls)");
                if (boundCount > 0)
                    badges.Add($"🟢 {boundCount} bound");
                if (total - boundCount > 0)
                    badges.Add($"⚪ {total - boundCount} unbound");

                return new VwIsapiResponseSummary
                {
                    HasResponse = true,
                    RootElement = rootName,
                    TotalItems = total,
                    ItemTypeLabel = "Tường ghép",
                    NormalCount = boundCount,
                    StatusBadge = "200 OK",
                    IsSuccess = true,
                    Badges = badges
                };
            }

            // Default fallback for any other XML
            badges.Add($"Thẻ gốc: <{rootName}>");
            return new VwIsapiResponseSummary
            {
                HasResponse = true,
                RootElement = rootName,
                StatusBadge = "200 OK",
                IsSuccess = true,
                Badges = badges
            };
        }
        catch
        {
            return new VwIsapiResponseSummary
            {
                HasResponse = true,
                StatusBadge = "Phản hồi Text/Raw",
                IsSuccess = true,
                Badges = [$"{xml.Length} ký tự"]
            };
        }
    }
}
