using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.Api.Direct;

/// <summary>
/// Description: Interface giao tiếp kết nối trực tiếp thiết bị VideoWall (Ping, Probe, SendIsapi)
/// Created date: 13/09/2026
/// </summary>
public interface IVwDeviceConnectionClient
{
    Task<VwSetupSceneStep> Ping(CancellationToken ct = default);

    Task<VwProbeDeviceOutput> Probe(int? wallNo, CancellationToken ct = default);

    Task<VwSetupSceneStep> SendIsapi(string method, string path, string? body, string? contentType, CancellationToken ct = default);
}
