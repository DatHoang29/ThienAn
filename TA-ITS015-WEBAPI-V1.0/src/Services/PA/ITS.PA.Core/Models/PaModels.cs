using System.Text.Json.Serialization;

namespace ITS.PA.Core.Models;

// DTO của REST API máy chủ PA (ITC). Tên thuộc tính JSON giữ NGUYÊN như bản cũ (ITC_PAControl_Model.cs):
// phần "forwarder" dùng chữ thường, dữ liệu trong "data" dùng PascalCase, danh sách nhóm dùng snake_case.
// Author: ChuongLP
// Created date: 31/10/2026

/// <summary>Cũ: ITC_PAControl_ResultWraper&lt;T&gt;. Vỏ bọc mọi gói gửi lên và nhận về.</summary>
public sealed class PaApiResponse<T> where T : class
{
    [JsonPropertyName("data")] public T? Data { get; set; }
    [JsonPropertyName("company")] public string Company { get; set; } = "BL";
    [JsonPropertyName("device_name")] public string DeviceName { get; set; } = "";
    [JsonPropertyName("return_message")] public string ReturnMessage { get; set; } = "";
    [JsonPropertyName("sign")] public string Sign { get; set; } = "";
    [JsonPropertyName("result")] public int Result { get; set; }
    [JsonPropertyName("actioncode")] public string ActionCode { get; set; } = "";
    [JsonPropertyName("token")] public string Token { get; set; } = "";
}

// ----- Trạng thái thiết bị đầu cuối -----

public sealed class PaTerminalStatusData
{
    public List<PaEndpoint>? EndPointsArray { get; set; }
}

public sealed class PaEndpoint
{
    public int EndpointID { get; set; }
    public string? EndpointIP { get; set; }
    public string? EndpointMac { get; set; }
    public string? EndpointName { get; set; }
    public int EndpointType { get; set; }
    public string? EndpointTypeName { get; set; }
    public string? EndpointVersion { get; set; }
    public int Status { get; set; }
    public string? StatusDsp { get; set; }
    public string? TaskID { get; set; }
    public string? TaskName { get; set; }
    public int Volume { get; set; }
}

// ----- Danh sách bản nhạc trên máy chủ -----

public sealed class PaMusicListData
{
    public PaMusicInfo? MusicInfo { get; set; }
}

public sealed class PaMusicInfo
{
    public List<PaMusicDir>? DirArray { get; set; }
    public List<PaMusic>? MusicArray { get; set; }
}

public sealed class PaMusicDir
{
    public int DirAudioTime { get; set; }
    public int DirID { get; set; }
    public string? DirName { get; set; }
}

public sealed class PaMusic
{
    public int AudioId { get; set; }
    public string? AudioName { get; set; }
    public int DirID { get; set; }
    public int Duration { get; set; }
}

// ----- Phát bản nhạc có trên máy chủ (c2ls_mobile_terminal_damand_music) -----

public sealed class PaDemandMusicRequest
{
    public string EndPointsAdditionalProp { get; set; } = "";
    public int[] EndPointIDs { get; set; } = [];
    public int[] EndPointGroupIDs { get; set; } = [];
    public int[] MusicIDs { get; set; } = [];
    public int[] MusicGroupIDs { get; set; } = [];
    public string? TaskID { get; set; }
    public string? TaskName { get; set; }
    public int Priority { get; set; }
    public int Volume { get; set; }
    public string? PlayMode { get; set; }
}

/// <summary>Cũ: ITC_PAControl_Music_Response_Data. Dùng cho cả phát nhạc local và phát URL.</summary>
public sealed class PaMusicResponse
{
    public int RemoteID { get; set; }
    public string? RemoteType { get; set; }
    public string? TaskID { get; set; }
    public string? TaskStatus { get; set; }
}

// ----- Phát luồng từ URL (c2ls_damand_media_stream) -----

public sealed class PaUrlMusicRequest
{
    public string? TaskName { get; set; }
    public string? TaskID { get; set; }
    public string? MediaStreamUrls { get; set; }
    public string? EndPointsList { get; set; }
    public int TaskPriority { get; set; }
    public int Volume { get; set; }
    public string? PlayMode { get; set; }
    public string EndPointsAdditionalProp { get; set; } = "";
}

// ----- Danh sách / dừng tác vụ -----

public sealed class PaTaskStatusData
{
    public List<PaTaskInfo>? TaskInfoArray { get; set; }
}

public sealed class PaTaskInfo
{
    public string? TaskBeginTime { get; set; }
    public string? TaskID { get; set; }
    public string? TaskName { get; set; }
    public string? TaskStatus { get; set; }
    public int TaskType { get; set; }
    public string? TaskTypeName { get; set; }
    public string? TaskUserName { get; set; }
    public int TaskVolume { get; set; }
}

public sealed class PaStopTaskRequest
{
    public string? TaskID { get; set; }
}

// ----- Nhóm thiết bị đầu cuối (GET terminals-groups/all) -----

public sealed class PaTerminalGroup
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("call_code")] public int CallCode { get; set; }
    [JsonPropertyName("terminals")] public List<PaTerminal>? Terminals { get; set; }
}

public sealed class PaTerminal
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("terminals_id")] public int TerminalsId { get; set; }
    [JsonPropertyName("terminals_groups_id")] public int TerminalsGroupsId { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("type")] public int Type { get; set; }
    [JsonPropertyName("ip_address")] public string? IpAddress { get; set; }
    [JsonPropertyName("call_code")] public int CallCode { get; set; }
}

// ----- Kết quả mức dịch vụ -----

/// <summary>Kết quả bật/dừng một tác vụ. Cũ: Tuple&lt;taskId, status, success&gt; hoặc Tuple&lt;success, taskId&gt;.</summary>
public sealed record PaTaskResult(bool Success, string? TaskId, string Message);
