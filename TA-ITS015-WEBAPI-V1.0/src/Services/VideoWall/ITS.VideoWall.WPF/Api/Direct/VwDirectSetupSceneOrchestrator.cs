using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Module.VideoWall.WPF.Api.Direct.Isapi;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.Api.Direct;

/// <summary>
/// Description: Tham số đầu vào để đẩy kịch bản trực tiếp xuống thiết bị ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed class VwDirectPushSceneInput
{
    /// <summary>ID kịch bản trên thiết bị.</summary>
    public int SceneId { get; set; }

    /// <summary>Tên kịch bản thiết lập.</summary>
    public string? SceneName { get; set; }

    /// <summary>Số hiệu tường cần thiết lập.</summary>
    public int? WallNo { get; set; }

    /// <summary>Cờ chạy thử nghiệm không ghi thật xuống thiết bị.</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Cờ xoá trắng cửa sổ cũ trước khi dựng cửa sổ mới.</summary>
    public bool ResetWindows { get; set; }

    /// <summary>Cờ lưu thông tin kịch bản vào bộ nhớ thiết bị.</summary>
    public bool SaveData { get; set; } = true;

    /// <summary>Cờ kích hoạt hiển thị kịch bản ngay sau khi dựng.</summary>
    public bool Activate { get; set; }

    /// <summary>Bỏ qua các bước kiểm tra giới hạn an toàn.</summary>
    public bool SkipGuardrails { get; set; }

    /// <summary>Danh sách thông tin các cửa sổ cần dựng.</summary>
    public List<VwDirectWindowInput> Windows { get; set; } = [];
}

/// <summary>
/// Description: Thông tin tham số cửa sổ cần dựng trên thiết bị ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed class VwDirectWindowInput
{
    /// <summary>Tọa độ X của cửa sổ trên tường.</summary>
    public int X { get; set; }

    /// <summary>Tọa độ Y của cửa sổ trên tường.</summary>
    public int Y { get; set; }

    /// <summary>Chiều rộng của cửa sổ (pixel).</summary>
    public int W { get; set; }

    /// <summary>Chiều cao của cửa sổ (pixel).</summary>
    public int H { get; set; }

    /// <summary>Thứ tự lớp hiển thị của cửa sổ.</summary>
    public int ZIndex { get; set; } = 1;

    /// <summary>Kênh tín hiệu vào gán cho cửa sổ.</summary>
    public int SignalNo { get; set; }

    /// <summary>Nhãn hiển thị trên cửa sổ.</summary>
    public string? Label { get; set; }

    /// <summary>Chế độ chia cửa sổ (1, 4, 9, 16).</summary>
    public int WindowMode { get; set; } = 1;

    /// <summary>Danh sách các kênh tín hiệu cho từng ô con.</summary>
    public List<int>? SubSignalNos { get; set; }

    /// <summary>Loại nguồn tín hiệu (hdmi, ip_stream...).</summary>
    public string? SourceType { get; set; }

    /// <summary>Đường dẫn luồng IP stream nếu có.</summary>
    public string? StreamUrl { get; set; }

    /// <summary>Giao thức truyền luồng IP stream.</summary>
    public string? StreamProtocol { get; set; }

    /// <summary>ID kênh stream trên thiết bị.</summary>
    public int? StreamingChannelId { get; set; }

    /// <summary>Danh sách chi tiết các cửa sổ con.</summary>
    public List<VwDirectSubWindowInput>? SubWindows { get; set; }
}

/// <summary>
/// Description: Thông tin tham số cửa sổ con khi ở chế độ chia nhỏ.
/// Created date: 08/09/2026
/// </summary>
public sealed class VwDirectSubWindowInput
{
    /// <summary>Chỉ số thứ tự ô cửa sổ con.</summary>
    public int SubId { get; set; } = 1;

    /// <summary>Kênh tín hiệu vào gán cho ô con.</summary>
    public int SignalNo { get; set; }

    /// <summary>Loại nguồn tín hiệu của ô con.</summary>
    public string? SourceType { get; set; }

    /// <summary>Đường dẫn luồng IP stream của ô con.</summary>
    public string? StreamUrl { get; set; }

    /// <summary>Giao thức truyền luồng IP stream của ô con.</summary>
    public string? StreamProtocol { get; set; }

    /// <summary>ID kênh stream của ô con.</summary>
    public int? StreamingChannelId { get; set; }
}

/// <summary>
/// Description: Kết quả thực thi thiết lập kịch bản trực tiếp trên thiết bị ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed class VwDirectSetupResult
{
    /// <summary>Cờ đánh dấu thiết lập kịch bản thành công.</summary>
    public bool Success { get; set; }

    /// <summary>Thông điệp kết quả hoặc lỗi.</summary>
    public string? Message { get; set; }

    /// <summary>Số hiệu tường được áp dụng kịch bản.</summary>
    public int? WallNo { get; set; }

    /// <summary>Nhật ký các bước thực hiện lệnh ISAPI.</summary>
    public List<VwSetupSceneStep> Steps { get; set; } = [];

    /// <summary>Danh sách kết quả thiết lập từng cửa sổ.</summary>
    public List<VwDirectWindowResult> Windows { get; set; } = [];
}

/// <summary>
/// Description: Kết quả thiết lập một cửa sổ cụ thể trên thiết bị ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed class VwDirectWindowResult
{
    /// <summary>Nhãn hiển thị của cửa sổ.</summary>
    public string? Label { get; set; }

    /// <summary>Tọa độ X thực tế của cửa sổ.</summary>
    public int X { get; set; }

    /// <summary>Tọa độ Y thực tế của cửa sổ.</summary>
    public int Y { get; set; }

    /// <summary>Chiều rộng thực tế của cửa sổ.</summary>
    public int W { get; set; }

    /// <summary>Chiều cao thực tế của cửa sổ.</summary>
    public int H { get; set; }

    /// <summary>Thứ tự lớp hiển thị thực tế của cửa sổ.</summary>
    public int ZIndex { get; set; }

    /// <summary>ID cửa sổ sinh ra trên thiết bị ISAPI.</summary>
    public int? DeviceWindowId { get; set; }

    /// <summary>Cờ đánh dấu thiết lập cửa sổ thành công.</summary>
    public bool Success { get; set; }

    /// <summary>Thông điệp chi tiết khi tạo cửa sổ.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Description: Điều phối quy trình thiết lập kịch bản và cửa sổ trực tiếp tới thiết bị ISAPI.
/// Created date: 08/09/2026
/// </summary>
public sealed class VwDirectSetupSceneOrchestrator(VwDirectISAPIClient client)
{
    public async Task<VwDirectSetupResult> Execute(VwDirectPushSceneInput input, CancellationToken ct)
    {
        var result = new VwDirectSetupResult();
        var sw = new Stopwatch();
        int order = 1;

        // 1. UserCheck
        sw.Restart();
        var userCheckRes = await client.UserCheck(ct);
        sw.Stop();
        result.Steps.Add(Track(order++, "UserCheck", "GET", "Security/userCheck", userCheckRes, sw.ElapsedMilliseconds));
        
        if (!userCheckRes.Success)
        {
            result.Success = false;
            result.Message = "UserCheck failed.";
            return result;
        }

        // 2. GetCapabilities
        sw.Restart();
        var capsRes = await client.GetCapabilities(ct);
        sw.Stop();
        result.Steps.Add(Track(order++, "GetCapabilities", "GET", "System/Capabilities", capsRes, sw.ElapsedMilliseconds));

        if (!input.SkipGuardrails && capsRes.Success && capsRes.Data != null)
        {
            var maxSceneNums = capsRes.Data.MaxSceneNums;
            if (maxSceneNums > 0 && (input.SceneId < 1 || input.SceneId > maxSceneNums))
            {
                result.Success = false;
                result.Message = $"SceneId {input.SceneId} must be in range 1..{maxSceneNums}.";
                return result;
            }

            var maxWindowNums = capsRes.Data.MaxWindowNums;
            if (maxWindowNums > 0 && input.Windows.Count > maxWindowNums)
            {
                result.Success = false;
                result.Message = $"Số lượng cửa sổ ({input.Windows.Count}) vượt quá giới hạn thiết bị cho phép ({maxWindowNums}).";
                return result;
            }
        }

        // 3. GetVideoWalls / Check WallNo
        sw.Restart();
        var wallsRes = await client.GetVideoWalls(ct);
        sw.Stop();
        result.Steps.Add(Track(order++, "GetVideoWalls", "GET", "DisplayDev/VideoWall", wallsRes, sw.ElapsedMilliseconds));

        if (!input.WallNo.HasValue)
        {
            result.Success = false;
            result.Message = "Chưa chỉ định WallNo — không tự chọn tường để tránh đẩy nhầm. Bấm Probe để xem danh sách tường rồi nhập tay.";
            return result;
        }

        var wallNo = input.WallNo.Value;
        result.WallNo = wallNo;

        // 4. GetOutputs
        sw.Restart();
        var outputsRes = await client.GetOutputs(wallNo, ct);
        sw.Stop();
        result.Steps.Add(Track(order++, "GetOutputs", "GET", $"DisplayDev/VideoWall/{wallNo}/outputs", outputsRes, sw.ElapsedMilliseconds));

        // 5. GetWindows (snapshot)
        sw.Restart();
        var snapshotRes = await client.GetWindows(wallNo, ct);
        sw.Stop();
        result.Steps.Add(Track(order++, "GetWindows", "GET", $"DisplayDev/VideoWall/{wallNo}/windows", snapshotRes, sw.ElapsedMilliseconds));

        // 6. DeleteAllWindows
        if (!input.ResetWindows)
        {
            result.Steps.Add(Skip(order++, "DeleteAllWindows", "DELETE", $"DisplayDev/VideoWall/{wallNo}/windows", "ResetWindows = false"));
        }
        else if (input.DryRun)
        {
            result.Steps.Add(Skip(order++, "DeleteAllWindows", "DELETE", $"DisplayDev/VideoWall/{wallNo}/windows", "DryRun — bỏ qua xoá cửa sổ"));
        }
        else
        {
            sw.Restart();
            var delRes = await client.DeleteAllWindows(wallNo, snapshotRes.Data, ct);
            sw.Stop();
            result.Steps.Add(Track(order++, "DeleteAllWindows", "DELETE", $"DisplayDev/VideoWall/{wallNo}/windows", delRes, sw.ElapsedMilliseconds));
        }

        // 7. AddWindow
        bool missingAnyDeviceWindowId = false;
        var orderedWindows = input.Windows.OrderBy(w => w.ZIndex).ToList();
        foreach (var w in orderedWindows)
        {
            var windowResult = new VwDirectWindowResult
            {
                Label = w.Label,
                X = w.X,
                Y = w.Y,
                W = w.W,
                H = w.H,
                ZIndex = w.ZIndex,
            };

            var req = BuildWindowRequest(w);
            var reqXml = VwIsapiXmlCodec.Serialize(req);

            if (input.DryRun)
            {
                result.Steps.Add(Skip(order++, $"AddWindow ({w.SignalNo})", "POST", $"DisplayDev/VideoWall/{wallNo}/windows", "DryRun — bỏ qua thêm cửa sổ", reqXml));
                windowResult.Success = true;
                windowResult.Message = "DryRun";
            }
            else
            {
                sw.Restart();
                var addRes = await client.AddWindow(wallNo, req, ct);
                sw.Stop();

                result.Steps.Add(Track(order++, $"AddWindow ({w.SignalNo})", "POST", $"DisplayDev/VideoWall/{wallNo}/windows", addRes, sw.ElapsedMilliseconds));

                windowResult.Success = addRes.Success;
                windowResult.Message = addRes.ErrorMessage;

                if (addRes.Success && addRes.Data != null && addRes.Data.Id > 0)
                {
                    windowResult.DeviceWindowId = addRes.Data.Id;
                }
                else
                {
                    missingAnyDeviceWindowId = true;
                }

                if (windowResult.Success && windowResult.DeviceWindowId.HasValue)
                {
                    var mode = w.WindowMode <= 0 ? 1 : w.WindowMode;
                    var subs = w.SubWindows ?? [];
                    for (var i = 0; i < mode; i++)
                    {
                        var subInput = i < subs.Count ? subs[i] : null;
                        var streamUrl = subInput?.StreamUrl ?? (i == 0 ? w.StreamUrl : null);
                        var isIpStream = subInput?.SourceType == "ip_stream" || !string.IsNullOrWhiteSpace(streamUrl) || (subInput?.StreamingChannelId > 0) || (w.StreamingChannelId > 0);
                        if (isIpStream)
                        {
                            sw.Restart();
                            var decodeRes = await client.StartDynamicDecode(wallNo, windowResult.DeviceWindowId.Value, i + 1, ct);
                            sw.Stop();
                            result.Steps.Add(Track(order++, $"StartDynamicDecode (wnd={windowResult.DeviceWindowId}, sub={i + 1})", "PUT", $"DisplayDev/VideoWall/{wallNo}/windows/{windowResult.DeviceWindowId}/sub/{i + 1}/start", decodeRes, sw.ElapsedMilliseconds));
                        }
                    }
                }
            }

            result.Windows.Add(windowResult);
        }

        // 8. GetWindows (resolve missing IDs)
        if (!input.DryRun && missingAnyDeviceWindowId)
        {
            sw.Restart();
            var resolveRes = await client.GetWindows(wallNo, ct);
            sw.Stop();
            result.Steps.Add(Track(order++, "GetWindows", "GET", $"DisplayDev/VideoWall/{wallNo}/windows", resolveRes, sw.ElapsedMilliseconds));

            if (resolveRes.Success && resolveRes.Data != null)
            {
                foreach (var wResult in result.Windows.Where(x => x.Success && x.DeviceWindowId == null).ToList())
                {
                    var match = resolveRes.Data.WallWindow.FirstOrDefault(x => x.Rect?.Coordinate?.X == wResult.X && x.Rect?.Coordinate?.Y == wResult.Y && x.Rect?.Width == wResult.W && x.Rect?.Height == wResult.H);
                    if (match != null)
                    {
                        wResult.DeviceWindowId = match.Id;
                        var origWindow = input.Windows.FirstOrDefault(x => x.X == wResult.X && x.Y == wResult.Y && x.W == wResult.W && x.H == wResult.H);
                        if (origWindow != null)
                        {
                            var wMode = origWindow.WindowMode <= 0 ? 1 : origWindow.WindowMode;
                            var subs = origWindow.SubWindows ?? [];
                            for (var i = 0; i < wMode; i++)
                            {
                                var subInput = i < subs.Count ? subs[i] : null;
                                var streamUrl = subInput?.StreamUrl ?? (i == 0 ? origWindow.StreamUrl : null);
                                var isIp = subInput?.SourceType == "ip_stream" || !string.IsNullOrWhiteSpace(streamUrl) || (subInput?.StreamingChannelId > 0) || (origWindow.StreamingChannelId > 0);
                                if (isIp)
                                {
                                    sw.Restart();
                                    var decodeRes = await client.StartDynamicDecode(wallNo, match.Id, i + 1, ct);
                                    sw.Stop();
                                    result.Steps.Add(Track(order++, $"StartDynamicDecode (wnd={match.Id}, sub={i + 1})", "PUT", $"DisplayDev/VideoWall/{wallNo}/windows/{match.Id}/sub/{i + 1}/start", decodeRes, sw.ElapsedMilliseconds));
                                }
                            }
                        }
                    }
                }
            }
        }

        // 9. SaveSceneData
        if (input.DryRun)
        {
            result.Steps.Add(Skip(order++, "SaveSceneData", "PUT", $"DisplayDev/VideoWall/{wallNo}/scene/{input.SceneId}/saveData", "DryRun"));
        }
        else if (!input.SaveData)
        {
            result.Steps.Add(Skip(order++, "SaveSceneData", "PUT", $"DisplayDev/VideoWall/{wallNo}/scene/{input.SceneId}/saveData", "SaveData = false (chỉ dựng window, không lưu scene)"));
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(input.SceneName))
            {
                sw.Restart();
                var setSceneRes = await client.SetScene(wallNo, input.SceneId, input.SceneName, ct);
                sw.Stop();
                result.Steps.Add(Track(order++, "SetScene", "PUT", $"DisplayDev/VideoWall/{wallNo}/scene/{input.SceneId}", setSceneRes, sw.ElapsedMilliseconds));
            }

            sw.Restart();
            var saveRes = await client.SaveSceneData(wallNo, input.SceneId, ct);
            sw.Stop();
            result.Steps.Add(Track(order++, "SaveSceneData", "PUT", $"DisplayDev/VideoWall/{wallNo}/scene/{input.SceneId}/saveData", saveRes, sw.ElapsedMilliseconds));

            if (!saveRes.Success && (saveRes.RawResponse?.Contains("invalidOperation", StringComparison.OrdinalIgnoreCase) == true
                || saveRes.ErrorMessage?.Contains("invalidOperation", StringComparison.OrdinalIgnoreCase) == true))
            {
                sw.Restart();
                var createRes = await client.CreateScene(wallNo, input.SceneName ?? $"Scene {input.SceneId}", ct);
                sw.Stop();
                result.Steps.Add(Track(order++, "CreateSceneFallback", "POST", $"DisplayDev/VideoWall/{wallNo}/scene", createRes, sw.ElapsedMilliseconds));

                if (createRes.Success)
                {
                    sw.Restart();
                    var retrySaveRes = await client.SaveSceneData(wallNo, input.SceneId, ct);
                    sw.Stop();
                    result.Steps.Add(Track(order++, "SaveSceneDataRetry", "PUT", $"DisplayDev/VideoWall/{wallNo}/scene/{input.SceneId}/saveData", retrySaveRes, sw.ElapsedMilliseconds));
                }
            }
        }

        // 10. ActivateScene
        if (!input.Activate)
        {
            result.Steps.Add(Skip(order++, "ActivateScene", "PUT", $"DisplayDev/VideoWall/{wallNo}/scene/{input.SceneId}/activate", "Activate = false"));
        }
        else if (input.DryRun)
        {
            result.Steps.Add(Skip(order++, "ActivateScene", "PUT", $"DisplayDev/VideoWall/{wallNo}/scene/{input.SceneId}/activate", "DryRun"));
        }
        else
        {
            sw.Restart();
            var activateRes = await client.ActivateScene(wallNo, input.SceneId, ct);
            sw.Stop();
            result.Steps.Add(Track(order++, "ActivateScene", "PUT", $"DisplayDev/VideoWall/{wallNo}/scene/{input.SceneId}/activate", activateRes, sw.ElapsedMilliseconds));
        }

        var criticalSteps = result.Steps.Where(s => !s.Skipped && s.Name != "CreateSceneFallback" && s.Name != "SetScene").ToList();
        result.Success = criticalSteps.All(s => s.Success);
        if (!result.Success)
        {
            var firstError = criticalSteps.FirstOrDefault(s => !s.Success);
            result.Message = firstError?.Message ?? "Có bước thực thi không thành công";
        }
        return result;
    }

    public static VwISAPIWindowRequest BuildWindowRequest(VwDirectWindowInput w)
    {
        var mode = w.WindowMode <= 0 ? 1 : w.WindowMode;
        var subWindows = new List<VwISAPISubWindow>();
        var subInputs = w.SubWindows != null && w.SubWindows.Count > 0
            ? w.SubWindows
            : (w.SubSignalNos != null && w.SubSignalNos.Count > 0
                ? w.SubSignalNos.Select((sig, idx) => new VwDirectSubWindowInput
                {
                    SubId = idx + 1,
                    SignalNo = sig,
                    SourceType = w.SourceType,
                    StreamUrl = w.StreamUrl,
                    StreamProtocol = w.StreamProtocol,
                }).ToList()
                : [new VwDirectSubWindowInput
                {
                    SubId = 1,
                    SignalNo = w.SignalNo,
                    SourceType = w.SourceType,
                    StreamUrl = w.StreamUrl,
                    StreamProtocol = w.StreamProtocol,
                }]);

        for (var i = 0; i < mode; i++)
        {
            var subInput = i < subInputs.Count ? subInputs[i] : null;
            var sig = subInput?.SignalNo > 0
                ? subInput.SignalNo
                : (w.SignalNo > 0 ? w.SignalNo : 1);
            var streamUrl = subInput?.StreamUrl ?? (i == 0 ? w.StreamUrl : null);
            var streamingChannelId = subInput?.StreamingChannelId ?? (i == 0 ? w.StreamingChannelId : null);
            var isStreamingChannel = streamingChannelId.HasValue && streamingChannelId.Value > 0;
            var isIpStream = subInput?.SourceType == "ip_stream" || !string.IsNullOrWhiteSpace(streamUrl) || isStreamingChannel;

            var subParam = new VwISAPISubWindowParam();
            if (isStreamingChannel)
            {
                subParam.SignalMode = VwDirectDeviceConstants.SignalModeStreamId;
                subParam.StreamingChannelId = streamingChannelId.Value.ToString();
                subParam.VideoInputChannelId = null;
            }
            else if (isIpStream && !string.IsNullOrWhiteSpace(streamUrl))
            {
                subParam.SignalMode = VwDirectDeviceConstants.SignalModeStreamSetting;
                subParam.VideoInputChannelId = null;
                subParam.StreamInput = new VwISAPIStreamInput
                {
                    StreamInputRealtime = new VwISAPIStreamInputRealtime
                    {
                        StreamRealtimeUnitList =
                        [
                            new VwISAPIStreamRealtimeUnit
                            {
                                StreamInUrl = new VwISAPIStreamInUrl { Url = streamUrl }
                            }
                        ]
                    }
                };
            }
            else
            {
                subParam.SignalMode = VwDirectDeviceConstants.SignalMode;
                subParam.VideoInputChannelId = sig.ToString();
            }

            subWindows.Add(new VwISAPISubWindow
            {
                Id = i + 1,
                SubWindowParam = subParam,
            });
        }

        return new VwISAPIWindowRequest
        {
            WindowMode = mode,
            WindowModeSpecified = true,
            WndShowMode = "subWndMode",
            Rect = new Isapi.VwISAPIRect
            {
                Coordinate = new Isapi.VwISAPICoordinate { X = w.X, Y = w.Y },
                Width = w.W,
                Height = w.H,
            },
            SubWindowList = new VwISAPISubWindowList
            {
                SubWindow = subWindows,
            },
        };
    }

    private static VwSetupSceneStep Track(int order, string name, string method, string endpoint, VwISAPIResult result, long durationMs)
        => new()
        {
            Order = order,
            Name = name,
            Method = method,
            Endpoint = endpoint,
            Success = result.Success,
            HttpStatus = (int?)result.HttpStatusCode,
            Message = result.ErrorMessage,
            DurationMs = durationMs,
            RequestXml = result.RawRequest,
            ResponseXml = result.RawResponse,
        };

    private static VwSetupSceneStep Skip(int order, string name, string method, string endpoint, string reason, string? requestXml = null)
        => new()
        {
            Order = order,
            Name = name,
            Method = method,
            Endpoint = endpoint,
            Success = true,
            Skipped = true,
            Message = reason,
            RequestXml = requestXml,
        };
}
