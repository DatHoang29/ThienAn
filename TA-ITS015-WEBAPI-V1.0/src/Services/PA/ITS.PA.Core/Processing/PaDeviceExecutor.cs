using ITS.PA.Core.Configuration;
using ITS.PA.Core.Integration;
using ITS.PA.Core.Models;

namespace ITS.PA.Core.Processing;

/// <summary>
/// Phát và dừng tác vụ âm thanh trên máy chủ PA. Port các hàm cấp cao của ITC_PAControl (ITS008):
///   fn_StartSongLocal_Group_API → <see cref="StartLocalSongOnGroupAsync"/>
///   fn_StartSongURL_Group_API   → <see cref="StartUrlOnGroupAsync"/>
///   fn_StartSongLocal_API       → <see cref="StartLocalSongOnIpsAsync"/>
///   fn_StartSongURL_API         → <see cref="StartUrlOnIpsAsync"/>
///   fn_StopTask_API             → <see cref="StopTaskAsync"/>
///   fn_StopTaskMulti_API        → <see cref="StopTasksByNameAsync"/>
///
/// LƯU Ý — hành vi cũ được giữ nguyên dù có vẻ là lỗi: <see cref="StartLocalSongOnGroupAsync"/> bỏ qua
/// groupName và phát tới TẤT CẢ nhóm thiết bị (bản cũ chỉ lọc theo tên nhóm ở đường phát URL). Đổi sang lọc
/// theo tên cần xác nhận với nghiệp vụ vì sẽ làm "kiểm âm" và "sự cố" phát vào đúng một nhóm.
/// Khác bản cũ: bản cũ ném NullReferenceException khi API trả null; bản này trả kết quả thất bại kèm thông báo.
/// Author: ChuongLP
/// Created date: 31/10/2026
/// </summary>
public sealed class PaDeviceExecutor
{
    private readonly PaApiClient _api;
    private readonly PaOptions _options;
    private readonly ILogger<PaDeviceExecutor> _logger;

    public PaDeviceExecutor(PaApiClient api, IOptions<PaOptions> options, ILogger<PaDeviceExecutor> logger)
    {
        _api = api;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Phát bản nhạc đã upload lên máy chủ PA tới các nhóm thiết bị (xem lưu ý về groupName ở trên).</summary>
    public async Task<PaTaskResult> StartLocalSongOnGroupAsync(
        string songName, string groupName, string taskName, string taskId, int? volume = null, CancellationToken ct = default)
    {
        var musics = await _api.GetLocalMusicListAsync(ct).ConfigureAwait(false);
        var musicIds = musics?.MusicInfo?.MusicArray?.Where(m => m.AudioName == songName).Select(m => m.AudioId).ToArray() ?? [];

        var groups = await _api.GetTerminalGroupsAsync(ct).ConfigureAwait(false);
        var groupIds = groups?.Select(g => g.Id).ToArray() ?? [];

        if (musicIds.Length == 0 || groupIds.Length == 0)
            return new PaTaskResult(false, taskId, "Not Found");

        var response = await _api.StartSongLocalAsync(new PaDemandMusicRequest
        {
            TaskName = taskName,
            TaskID = taskId,
            EndPointIDs = [],
            EndPointGroupIDs = groupIds,
            MusicIDs = musicIds,
            MusicGroupIDs = [],
            Priority = _options.Priority,
            Volume = volume ?? _options.Volume,
            PlayMode = _options.PlayMode,
        }, ct).ConfigureAwait(false);

        return ToResult(response, taskId);
    }

    /// <summary>Phát bản nhạc đã upload tới các thiết bị theo IP (phân cách dấu phẩy).</summary>
    public async Task<PaTaskResult> StartLocalSongOnIpsAsync(
        string songName, string destinationIps, string taskName, CancellationToken ct = default)
    {
        var musics = await _api.GetLocalMusicListAsync(ct).ConfigureAwait(false);
        var musicIds = musics?.MusicInfo?.MusicArray?.Where(m => m.AudioName == songName).Select(m => m.AudioId).ToArray() ?? [];

        var endpointIds = await ResolveEndpointIdsAsync(destinationIps, ct).ConfigureAwait(false);
        if (musicIds.Length == 0 || endpointIds.Length == 0)
            return new PaTaskResult(false, null, "Not Found");

        var taskId = NewTaskId();
        var response = await _api.StartSongLocalAsync(new PaDemandMusicRequest
        {
            TaskName = taskName,
            TaskID = taskId,
            EndPointIDs = endpointIds,
            MusicIDs = musicIds,
            Priority = _options.Priority,
            Volume = _options.Volume,
            PlayMode = _options.PlayMode,
        }, ct).ConfigureAwait(false);

        return ToResult(response, taskId);
    }

    /// <summary>Phát luồng từ URL tới các thiết bị thuộc nhóm có tên groupName.</summary>
    public async Task<PaTaskResult> StartUrlOnGroupAsync(
        string mediaUrl, string groupName, string taskName, string taskId, int? volume = null, CancellationToken ct = default)
    {
        var groups = await _api.GetTerminalGroupsAsync(ct).ConfigureAwait(false);
        var terminalIds = groups?
            .Where(g => g.Name == groupName)
            .SelectMany(g => g.Terminals ?? [])
            .Select(t => t.Id)
            .Distinct()
            .ToList() ?? [];

        if (terminalIds.Count == 0)
            return new PaTaskResult(false, taskId, "Not Found");

        var response = await _api.StartSongUrlAsync(new PaUrlMusicRequest
        {
            TaskName = taskName,
            TaskID = taskId,
            EndPointsList = string.Join(",", terminalIds),
            MediaStreamUrls = mediaUrl,
            Volume = volume ?? _options.Volume,
            PlayMode = _options.PlayMode,
            TaskPriority = _options.Priority,
        }, ct).ConfigureAwait(false);

        return ToResult(response, taskId);
    }

    /// <summary>Phát luồng từ URL tới các thiết bị theo IP (phân cách dấu phẩy).</summary>
    public async Task<PaTaskResult> StartUrlOnIpsAsync(
        string mediaUrl, string destinationIps, string taskName, CancellationToken ct = default)
    {
        var endpointIds = await ResolveEndpointIdsAsync(destinationIps, ct).ConfigureAwait(false);
        if (endpointIds.Length == 0)
            return new PaTaskResult(false, null, "Not Found");

        var taskId = NewTaskId();
        var response = await _api.StartSongUrlAsync(new PaUrlMusicRequest
        {
            TaskName = taskName,
            TaskID = taskId,
            EndPointsList = string.Join(",", endpointIds),
            MediaStreamUrls = mediaUrl,
            Volume = _options.Volume,
            PlayMode = _options.PlayMode,
            TaskPriority = _options.Priority,
        }, ct).ConfigureAwait(false);

        return ToResult(response, taskId);
    }

    /// <summary>Dừng một tác vụ theo ID. Cũ: fn_StopTask_API (trả null khi lỗi).</summary>
    public async Task<PaTaskResult> StopTaskAsync(string taskId, CancellationToken ct = default)
    {
        var response = await _api.StopTaskAsync(new PaStopTaskRequest { TaskID = taskId }, ct).ConfigureAwait(false);
        return response is null
            ? new PaTaskResult(false, taskId, "Stop task failed")
            : new PaTaskResult(true, response.TaskID, "Stopped");
    }

    /// <summary>
    /// Dừng mọi tác vụ có tên chứa taskName. Cũ: fn_StopTaskMulti_API — dùng để dừng âm thanh cũ của cùng
    /// loại sự cố trước khi phát mới.
    /// </summary>
    public async Task<List<PaTaskResult>> StopTasksByNameAsync(string taskName, CancellationToken ct = default)
    {
        var tasks = await _api.GetTaskStatusAsync(ct).ConfigureAwait(false);
        var matches = tasks?.TaskInfoArray?.Where(t => t.TaskName != null && t.TaskName.Contains(taskName)).ToList() ?? [];

        var results = await Task.WhenAll(matches.Select(async t =>
        {
            var response = await _api.StopTaskAsync(new PaStopTaskRequest { TaskID = t.TaskID }, ct).ConfigureAwait(false);
            return new PaTaskResult(response is not null, response?.TaskID ?? t.TaskID, response is null ? "Stop task failed" : "Stopped");
        })).ConfigureAwait(false);

        _logger.LogInformation("Đã dừng {Count} tác vụ PA có tên chứa '{Name}'.", results.Length, taskName);
        return results.ToList();
    }

    private PaTaskResult ToResult(PaMusicResponse? response, string? taskId)
    {
        if (response is null) return new PaTaskResult(false, taskId, "No response from PA server");
        var ok = response.TaskStatus == _options.WorkingTaskStatus;
        return new PaTaskResult(ok, response.TaskID ?? taskId, response.TaskStatus ?? string.Empty);
    }

    /// <summary>Đổi danh sách IP thành EndpointID theo trạng thái thiết bị trên máy chủ.</summary>
    private async Task<int[]> ResolveEndpointIdsAsync(string destinationIps, CancellationToken ct)
    {
        var ips = destinationIps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var status = await _api.GetTerminalStatusAsync(ct).ConfigureAwait(false);
        return status?.EndPointsArray?
            .Where(e => e.EndpointIP != null && ips.Contains(e.EndpointIP))
            .Select(e => e.EndpointID)
            .ToArray() ?? [];
    }

    private static string NewTaskId() => Guid.NewGuid().ToString("N");
}
