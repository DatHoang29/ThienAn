using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Module.VideoWall.WPF.Api.Direct.Isapi;
using Module.VideoWall.WPF.Api.Dto;
using Services.Shared.Events;

namespace Module.VideoWall.WPF.Api.Direct;

/// <summary>
/// Description: Client thực thi kết nối trực tiếp thiết bị VideoWall, hỗ trợ Ping kiểm tra và Probe khảo sát
/// Created date: 13/09/2026
/// </summary>
public sealed class VwDirectDeviceConnectionClient(VwDirectISAPIClient isApiClient, IPublisher publisher) : IVwDeviceConnectionClient
{
    public async Task<VwSetupSceneStep> Ping(CancellationToken ct = default)
    {
        try
        {
            var result = await isApiClient.UserCheck(ct).ConfigureAwait(false);
            
            var step = new VwSetupSceneStep
            {
                Name = "UserCheck",
                Method = "GET",
                Endpoint = "Security/userCheck",
                Success = result.Success,
                HttpStatus = result.HttpStatusCode.HasValue ? (int)result.HttpStatusCode.Value : null,
                Message = result.ErrorMessage,
                RequestXml = result.RawRequest,
                ResponseXml = result.RawResponse
            };

            await PublishNotification(step, ct).ConfigureAwait(false);
            return step;
        }
        catch (Exception ex)
        {
            var step = new VwSetupSceneStep
            {
                Name = "UserCheck",
                Method = "GET",
                Endpoint = "Security/userCheck",
                Success = false,
                Message = $"Lỗi kết nối thiết bị: {ex.InnerException?.Message ?? ex.Message}"
            };

            await PublishNotification(step, ct).ConfigureAwait(false);
            return step;
        }
    }

    public async Task<VwProbeDeviceOutput> Probe(int? wallNo, CancellationToken ct = default)
    {
        var steps = new List<VwSetupSceneStep>();
        try
        {
            var userCheckRes = await isApiClient.UserCheck(ct).ConfigureAwait(false);
            var userCheckStep = ToStep("UserCheck", "GET", "Security/userCheck", userCheckRes);
            steps.Add(userCheckStep);
            await PublishNotification(userCheckStep, ct).ConfigureAwait(false);

            if (!userCheckRes.Success)
                return new VwProbeDeviceOutput { Reachable = false, Steps = steps };

            var capRes = await isApiClient.GetCapabilities(ct).ConfigureAwait(false);
            var capStep = ToStep("GetCapabilities", "GET", "ISAPI/DisplayDev/VideoWall/capabilities", capRes);
            steps.Add(capStep);
            await PublishNotification(capStep, ct).ConfigureAwait(false);

            var wallsRes = await isApiClient.GetVideoWalls(ct).ConfigureAwait(false);
            var wallsStep = ToStep("GetVideoWalls", "GET", "ISAPI/DisplayDev/VideoWall", wallsRes);
            steps.Add(wallsStep);
            await PublishNotification(wallsStep, ct).ConfigureAwait(false);

            var wallsList = wallsRes.Data?.VideoWall ?? [];
            var boundWall = wallsList.FirstOrDefault(w => string.Equals(w.WallBindOutputStatus, "bound", StringComparison.OrdinalIgnoreCase));
            var boundWallNo = boundWall?.Id;
            var resolvedWallNo = wallNo ?? boundWallNo ?? (wallsList.Count == 1 ? wallsList[0].Id : null);

            List<Dto.VwISAPIOutputItem>? outputs = null;
            if (resolvedWallNo.HasValue)
            {
                var outputsRes = await isApiClient.GetOutputs(resolvedWallNo.Value, ct).ConfigureAwait(false);
                var outputsStep = ToStep("GetOutputs", "GET", $"ISAPI/DisplayDev/VideoWall/{resolvedWallNo.Value}/outputs", outputsRes);
                steps.Add(outputsStep);
                await PublishNotification(outputsStep, ct).ConfigureAwait(false);

                outputs = outputsRes.Data?.WallOutput?.Select(o => new Dto.VwISAPIOutputItem
                {
                    Id = o.Id,
                    OutputId = o.OutputId,
                    OutputWinNum = o.OutputWinNum,
                    CoordinateMode = o.CoordinateMode,
                    Rect = o.Rect == null ? null : new Dto.VwISAPIRect
                    {
                        Width = o.Rect.Width,
                        Height = o.Rect.Height,
                        Coordinate = o.Rect.Coordinate == null ? null : new Dto.VwISAPICoordinate
                        {
                            X = o.Rect.Coordinate.X,
                            Y = o.Rect.Coordinate.Y,
                        },
                    },
                }).ToList();
            }

            var inputsRes = await isApiClient.GetInputChannels(ct).ConfigureAwait(false);
            var inputsStep = ToStep("GetInputChannels", "GET", "ISAPI/DisplayDev/Video/inputs/channels", inputsRes);
            steps.Add(inputsStep);
            await PublishNotification(inputsStep, ct).ConfigureAwait(false);

            var streamingRes = await isApiClient.GetStreamingChannels(ct).ConfigureAwait(false);
            if (streamingRes.Success)
            {
                var streamingStep = ToStep("GetStreamingChannels", "GET", "ISAPI/DisplayDev/Video/streaming/channels", streamingRes);
                steps.Add(streamingStep);
                await PublishNotification(streamingStep, ct).ConfigureAwait(false);
            }

            var combinedChannels = new List<Dto.VwISAPIInputChannel>();
            if (inputsRes.Data?.VideoInputChannel != null)
            {
                foreach (var i in inputsRes.Data.VideoInputChannel)
                {
                    combinedChannels.Add(new Dto.VwISAPIInputChannel
                    {
                        Id = i.Id,
                        InputPortType = i.InputPortType ?? "HDMI",
                        Name = i.Name,
                        VideoInputChannelAccessStatus = i.VideoInputChannelAccessStatus ?? "normal",
                        PortInBoard = i.PortInBoard == null ? null : new Dto.VwISAPIPortInBoard
                        {
                            BoardId = i.PortInBoard.BoardId,
                            PortId = i.PortInBoard.PortId,
                            IpAddress = i.PortInBoard.IpAddress,
                            Port = i.PortInBoard.Port,
                        },
                    });
                }
            }

            if (streamingRes.Success && streamingRes.Data?.StreamInputChannel != null)
            {
                foreach (var s in streamingRes.Data.StreamInputChannel)
                {
                    var unit = s.StreamInput?.StreamInputRealtime?.StreamRealtimeUnitList?.FirstOrDefault();
                    var enc = unit?.StreamByDomain?.EncodeDevInfo;
                    var ip = enc?.Domain?.Trim();
                    var port = enc?.Port > 0 ? enc.Port : 8000;
                    var streamUrl = unit?.StreamInUrl?.Url?.Trim();

                    if (string.IsNullOrWhiteSpace(streamUrl) && !string.IsNullOrWhiteSpace(ip))
                    {
                        var user = enc?.Username?.Trim() ?? "admin";
                        var pass = enc?.Password?.Trim() ?? string.Empty;
                        var chNo = enc?.ChannelNormal > 0 ? (enc.ChannelNormal * 100 + 1) : 101;
                        var cred = !string.IsNullOrWhiteSpace(pass) ? $"{user}:{pass}@" : (!string.IsNullOrWhiteSpace(user) ? $"{user}@" : string.Empty);
                        streamUrl = $"rtsp://{cred}{ip}:554/Streaming/Channels/{chNo}";
                    }

                    combinedChannels.Add(new Dto.VwISAPIInputChannel
                    {
                        Id = s.Id,
                        InputPortType = "IPCamera",
                        Name = !string.IsNullOrWhiteSpace(s.Name) ? s.Name : $"Camera IP {s.Id}",
                        VideoInputChannelAccessStatus = "normal",
                        StreamUrl = streamUrl,
                        StreamingChannelId = s.Id,
                        PortInBoard = new Dto.VwISAPIPortInBoard
                        {
                            BoardId = 0,
                            PortId = s.Id,
                            IpAddress = ip,
                            Port = port,
                        },
                    });
                }
            }

            List<VwWallSceneSummary>? scenes = null;
            int? runningSceneId = null;
            List<VwISAPIWallWindowItem>? activeWindows = null;

            if (resolvedWallNo.HasValue)
            {
                var scenesRes = await isApiClient.GetScenes(resolvedWallNo.Value, ct).ConfigureAwait(false);
                var scenesStep = ToStep("GetScenes", "GET", $"ISAPI/DisplayDev/VideoWall/{resolvedWallNo.Value}/scene", scenesRes);
                steps.Add(scenesStep);
                await PublishNotification(scenesStep, ct).ConfigureAwait(false);

                if (scenesRes.Success && scenesRes.Data?.WallScene != null)
                {
                    scenes = scenesRes.Data.WallScene.Select(s => new VwWallSceneSummary
                    {
                        Id = s.Id,
                        Name = s.Name,
                    }).ToList();
                }

                var runningRes = await isApiClient.GetRunningScene(resolvedWallNo.Value, ct).ConfigureAwait(false);
                var runningStep = ToStep("GetRunningScene", "GET", $"ISAPI/DisplayDev/VideoWall/{resolvedWallNo.Value}/scene/isRunning", runningRes);
                steps.Add(runningStep);
                await PublishNotification(runningStep, ct).ConfigureAwait(false);

                if (runningRes.Success && runningRes.Data != null)
                {
                    runningSceneId = runningRes.Data.SceneId;
                }

                var windowsRes = await isApiClient.GetWindows(resolvedWallNo.Value, ct).ConfigureAwait(false);
                var windowsStep = ToStep("GetWindows", "GET", $"ISAPI/DisplayDev/VideoWall/{resolvedWallNo.Value}/windows", windowsRes);
                steps.Add(windowsStep);
                await PublishNotification(windowsStep, ct).ConfigureAwait(false);

                if (windowsRes.Success && windowsRes.Data?.WallWindow != null)
                {
                    activeWindows = windowsRes.Data.WallWindow;
                }
            }

            return new VwProbeDeviceOutput
            {
                Reachable = true,
                IsSupportScene = capRes.Data?.IsSupportScene ?? false,
                MaxWindowNums = capRes.Data?.MaxWindowNums ?? 0,
                MaxSceneNums = capRes.Data?.MaxSceneNums ?? 0,
                BaseOutputSize = capRes.Data?.BaseOutputSize ?? 0,
                WallNo = resolvedWallNo,
                BoundWallNo = boundWallNo,
                Walls = wallsList.Select(w => new VwWallSummary
                {
                    Id = w.Id,
                    Name = w.Name,
                    BindOutputStatus = w.WallBindOutputStatus,
                }).ToList(),
                Outputs = outputs,
                InputChannels = combinedChannels,
                Scenes = scenes,
                RunningSceneId = runningSceneId,
                ActiveWindows = activeWindows,
                Steps = steps,
            };
        }
        catch (Exception ex)
        {
            var errStep = new VwSetupSceneStep
            {
                Name = "ProbeError",
                Method = "PROBE",
                Endpoint = "DirectProbe",
                Success = false,
                Message = $"Không thể hoàn thành Probe: {ex.InnerException?.Message ?? ex.Message}"
            };
            steps.Add(errStep);
            await PublishNotification(errStep, ct).ConfigureAwait(false);
            return new VwProbeDeviceOutput { Reachable = false, Steps = steps };
        }
    }

    public async Task<VwSetupSceneStep> SendIsapi(string method, string path, string? body, string? contentType, CancellationToken ct = default)
    {
        try
        {
            var result = await isApiClient.SendRaw(method, path, body, contentType, ct).ConfigureAwait(false);
            var step = new VwSetupSceneStep
            {
                Name = "ISAPI",
                Method = method,
                Endpoint = path,
                Success = result.Success,
                HttpStatus = result.HttpStatusCode.HasValue ? (int)result.HttpStatusCode.Value : null,
                Message = result.ErrorMessage,
                RequestXml = result.RawRequest,
                ResponseXml = result.RawResponse
            };

            await PublishNotification(step, ct).ConfigureAwait(false);
            return step;
        }
        catch (Exception ex)
        {
            var step = new VwSetupSceneStep
            {
                Name = "ISAPI",
                Method = method,
                Endpoint = path,
                Success = false,
                Message = $"Lỗi gửi ISAPI: {ex.InnerException?.Message ?? ex.Message}"
            };

            await PublishNotification(step, ct).ConfigureAwait(false);
            return step;
        }
    }

    private Task PublishNotification(VwSetupSceneStep step, CancellationToken ct)
    {
        var level = step.Success ? ActivityLevel.Success : ActivityLevel.Error;
        var activity = new Activity(System.DateTime.Now, "Direct", $"{step.Method} {step.Endpoint} → {step.Outcome}", level);
        return publisher.Publish(new DeviceStepNotification(activity, step), ct);
    }

    private static VwSetupSceneStep ToStep(string name, string method, string endpoint, VwISAPIResult result)
    {
        return new VwSetupSceneStep
        {
            Name = name,
            Method = method,
            Endpoint = endpoint,
            Success = result.Success,
            HttpStatus = result.HttpStatusCode.HasValue ? (int)result.HttpStatusCode.Value : null,
            Message = result.ErrorMessage,
            RequestXml = result.RawRequest,
            ResponseXml = result.RawResponse
        };
    }
}
