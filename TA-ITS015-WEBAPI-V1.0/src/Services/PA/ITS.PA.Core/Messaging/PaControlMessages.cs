namespace ITS.PA.Core.Messaging;

/// <summary>Lệnh trên kênh ta.its.control.pa.</summary>
public static class PaControlAction
{
    /// <summary>Dừng tác vụ cũ cùng tiền tố mã sự cố rồi phát tới nhóm sự cố.</summary>
    public const string Play = "play";

    /// <summary>Phát tới nhóm kiểm âm.</summary>
    public const string SoundTest = "sound-test";

    /// <summary>Dừng một tác vụ theo mã.</summary>
    public const string Stop = "stop";
}

/// <summary>Loại gói tin trên kênh dùng chung 2 chiều, cùng quy ước với kênh điều khiển VMS.</summary>
public static class PaControlType
{
    public const string Request = "request";
    public const string Result = "result";
}

/// <summary>
/// Gói yêu cầu (Type = "request"). Dịch vụ không chạm DB: BE tra sẵn tên file từ PaRadioTemplate.AudioFileName.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public sealed class PaControlRequest
{
    public string? Type { get; set; }

    /// <summary>Mã truy vết để BE đối chiếu request/result.</summary>
    public string? RequestId { get; set; }

    /// <summary>Xem <see cref="PaControlAction"/>.</summary>
    public string? Action { get; set; }

    /// <summary>Mã sự cố, dùng làm tên tác vụ trên máy chủ PA.</summary>
    public string? IncidentCode { get; set; }

    /// <summary>Mã tác vụ trên máy chủ PA. Cũ: IncidentNote.Split('|')[0].</summary>
    public string? TaskId { get; set; }

    /// <summary>Tên file âm thanh đã upload lên máy chủ PA.</summary>
    public string? SongName { get; set; }

    /// <summary>true = trả thành công giả lập, không gọi máy chủ PA (test xuyên suốt luồng NATS).</summary>
    public bool Simulate { get; set; }
}

/// <summary>Gói kết quả (Type = "result"), giữ nguyên RequestId và Action của yêu cầu.</summary>
public sealed class PaControlResult
{
    public string Type { get; set; } = PaControlType.Result;
    public string? RequestId { get; set; }
    public string? Action { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TaskId { get; set; }
    public DateTime FinishedAt { get; set; }
}
