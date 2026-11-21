using ITS.PA.Core.Configuration;
using ITS.PA.Core.Models;

namespace ITS.PA.Core.Processing;

/// <summary>Chế độ điều khiển PA. Cũ: ControlParams.Mode (1 = kiểm âm, còn lại = phát theo sự cố).</summary>
public enum PaControlMode
{
    Incident = 0,
    SoundTest = 1,
}

/// <summary>
/// Lệnh bật/tắt PA cho một sự cố. Cũ: các tham số trong CommonFunctions (is4b.presentations.webapps.scs).
/// BE phải giải sẵn tên file âm thanh (PaRadioTemplate.AudioFileName theo loại sự kiện) và
/// mã tác vụ (cũ: phần đầu của IncidentNote trước dấu '|', là GUID) rồi mới gửi sang dịch vụ.
/// </summary>
public sealed class PaControlCommand
{
    public bool TurnOn { get; init; }

    public PaControlMode Mode { get; init; }

    /// <summary>Mã sự cố, dùng làm tên tác vụ. Cũ: IncidentCode ("inc_xx_...").</summary>
    public string IncidentCode { get; init; } = string.Empty;

    /// <summary>Mã tác vụ trên máy chủ PA. Cũ: IncidentNote.Split('|')[0].</summary>
    public string TaskId { get; init; } = string.Empty;

    /// <summary>Tên file âm thanh đã upload lên máy chủ PA. Lấy từ PaRadioTemplate.AudioFileName.</summary>
    public string SongName { get; init; } = string.Empty;
}

/// <summary>
/// Luồng bật/tắt/kiểm âm PA của sự cố. Port phần thiết bị của CommonFunctions (nhánh PA) ở ITS008:
///  - Bật, kiểm âm  : phát bản nhạc tới nhóm kiểm âm.
///  - Bật, sự cố    : dừng các tác vụ cũ cùng tiền tố mã sự cố rồi phát tới nhóm sự cố.
///  - Tắt           : dừng tác vụ theo mã.
///
/// KHÔNG nằm trong dịch vụ thiết bị (thuộc BE, gửi lệnh qua ta.its.control.pa): kiểm tra IP thiết bị/ping, cập nhật
/// ExecuteWorkDetail, tra mẫu âm thanh theo loại sự cố, ghi nhật ký prc_Its_Update_PA_Success.
/// Author: ChuongLP
/// Created date: 31/10/2026
/// </summary>
public sealed class PaControlService
{
    private readonly PaDeviceExecutor _executor;
    private readonly PaOptions _options;
    private readonly ILogger<PaControlService> _logger;

    public PaControlService(PaDeviceExecutor executor, IOptions<PaOptions> options, ILogger<PaControlService> logger)
    {
        _executor = executor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaTaskResult> ExecuteAsync(PaControlCommand command, CancellationToken ct = default)
    {
        if (!command.TurnOn)
        {
            _logger.LogInformation("Dừng âm thanh {TaskId}", command.TaskId);
            return await _executor.StopTaskAsync(command.TaskId, ct).ConfigureAwait(false);
        }

        if (command.Mode == PaControlMode.SoundTest)
        {
            return await _executor
                .StartLocalSongOnGroupAsync(command.SongName, _options.SoundTestGroup, command.IncidentCode, command.TaskId, ct: ct)
                .ConfigureAwait(false);
        }

        // Dừng toàn bộ âm thanh trước đó của cùng loại sự cố (cũ: IncidentCode.Substring(0, 6) = "inc_xx").
        var prefix = _options.StopTaskPrefixLength > 0 && command.IncidentCode.Length > _options.StopTaskPrefixLength
            ? command.IncidentCode[.._options.StopTaskPrefixLength]
            : command.IncidentCode;
        await _executor.StopTasksByNameAsync(prefix, ct).ConfigureAwait(false);

        _logger.LogInformation("Bật PA {TaskId} | File âm thanh: {Song}", command.TaskId, command.SongName);
        return await _executor
            .StartLocalSongOnGroupAsync(command.SongName, _options.EventGroup, command.IncidentCode, command.TaskId, ct: ct)
            .ConfigureAwait(false);
    }
}
