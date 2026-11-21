using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Helper;
using Modules.VDS.Core.Services.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Utility.Utilities;
using Wolverine;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// TMS gọi CctvSnapshotInput của module CCTV theo IP camera và lưu ảnh theo sự cố.
/// Nếu CCTV không trả được ảnh thì chụp một frame trực tiếp từ RTSP của thiết bị bằng FFmpeg.
/// Lỗi camera hoặc file không rollback thao tác tạo hay chuyển trạng thái sự cố đã thành công.
/// KienLT - 14/10/2026: ảnh lưu ở <see cref="IncidentImageStorage"/> (share cấu hình) thay vì wwwroot/tms/incident;
/// chỉ đoạn đọc/ghi file chạy trong quyền share, việc gọi camera thì không.
/// </summary>
public sealed class IncidentSnapshotDispatcher : IScoped
{
    private static readonly TimeSpan RtspSnapshotTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan CctvSnapshotTimeout = TimeSpan.FromSeconds(20);
    private const int MaxFfmpegDiagnosticLength = 1000;

    private static readonly Regex RtspUrlPattern = new(
        @"rtsps?://[^\s'""<>]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private const string CctvSnapshotInputTypeName =
        "Module.CCTV.Controllers.CctvMedia.Dto.CctvSnapshotInput, Module.CCTV";

    private static readonly Lazy<Type?> CctvSnapshotInputType =
        new(() => Type.GetType(CctvSnapshotInputTypeName, throwOnError: false));

    private readonly BaseRepository<TmsIncident> _incidentRepo;
    private readonly BaseRepository<TmsEquipment> _equipmentRepo;
    private readonly IncidentImageStorage _images;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<IncidentSnapshotDispatcher> _logger;
    private readonly TmsVdsRegistrationImageService _vdsRegistration;
    private readonly VdsRegistrationImageStore _vdsImages;

    public IncidentSnapshotDispatcher(
        BaseRepository<TmsIncident> incidentRepo,
        BaseRepository<TmsEquipment> equipmentRepo,
        IncidentImageStorage images,
        IMessageBus messageBus,
        ILogger<IncidentSnapshotDispatcher> logger, TmsVdsRegistrationImageService vdsRegistration,
        VdsRegistrationImageStore vdsImages)
    {
        _incidentRepo = incidentRepo;
        _equipmentRepo = equipmentRepo;
        _images = images;
        _messageBus = messageBus;
        _logger = logger;
        _vdsRegistration = vdsRegistration;
        _vdsImages = vdsImages;
    }

    public async Task CaptureAsync(
        string incidentId,
        int state,
        CancellationToken cancellationToken = default,
        VdsAutomationImageDecisionOutput? automationImage = null)
    {
        var traceId = Guid.NewGuid().ToString("N")[..12];
        var totalStopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "[IncidentSnapshot][{TraceId}][START] Bắt đầu chụp ảnh sự cố {IncidentId}, state {State}.",
            traceId,
            incidentId,
            state);

        if (string.IsNullOrWhiteSpace(incidentId))
        {
            _logger.LogWarning(
                "[IncidentSnapshot][{TraceId}][SKIP] IncidentId trống; dừng chụp ảnh.",
                traceId);
            return;
        }

        try
        {
            var pictureType = PictureTypeFromState(state);
            if (pictureType == 0)
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][STATE_SKIP] State {State} không thuộc thời điểm cần chụp; dừng xử lý sự cố {IncidentId}.",
                    traceId,
                    state,
                    incidentId);
                return;
            }

            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][STATE_OK] State {State} được ánh xạ sang picType {PictureType}.",
                traceId,
                state,
                pictureType);

            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][INCIDENT_QUERY] Đang đọc sự cố {IncidentId}.",
                traceId,
                incidentId);
            var incident = automationImage is null ? await _incidentRepo.GetByIdAsync(incidentId)
                : await _incidentRepo.Context.Queryable<TmsIncident>()
                    .Where(x => x.ID == incidentId && x.TenantId == automationImage.TenantId && x.IsDelete == null).FirstAsync(cancellationToken);
            if (incident == null)
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][INCIDENT_NOT_FOUND] Không tìm thấy sự cố {IncidentId}.",
                    traceId,
                    incidentId);
                return;
            }

            var captureTime = CaptureTime(incident, pictureType);
            if (captureTime == null)
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][TIME_MISSING] Sự cố {IncidentId} chưa có thời gian tương ứng picType {PictureType}.",
                    traceId,
                    incident.ID,
                    pictureType);
                return;
            }

            if (automationImage is not null
                && !automationImage.Matches(incident.TenantId, incident.ID, pictureType, captureTime.Value))
            {
                _logger.LogInformation("[VdsAutomationImage] Skip stale queued stage {IncidentId}/{PictureType}; transition timestamp changed.", incident.ID, pictureType);
                return;
            }

            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][TIME_OK] Sử dụng thời điểm {CaptureTime:O} cho sự cố {IncidentId}, picType {PictureType}.",
                traceId,
                captureTime.Value,
                incident.ID,
                pictureType);

            if (VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, pictureType))
            {
                await _vdsRegistration.CaptureAsync(incident, cancellationToken);
                return;
            }

            // Use the transition's persisted choice, never reconsider a pending full when dequeuing.
            VdsAutomationImageDecisionOutput? stageImage = null;
            FileStream? stageLock = null;
            if (automationImage is not null)
            {
                try
                {
                    stageLock = await _vdsImages.LockStageAsync(automationImage.TenantId, incident.ID,
                        pictureType, captureTime.Value, cancellationToken);
                    var stored = await _vdsImages.ReadStageAsync(automationImage.TenantId, incident.ID,
                        pictureType, captureTime.Value, cancellationToken);
                    if (stored is not null && stored.RunId != automationImage.RunId)
                    {
                        _logger.LogInformation("[VdsAutomationImage] Skip stage {IncidentId}/{PictureType}; another run owns this transition.", incident.ID, pictureType);
                        await stageLock.DisposeAsync();
                        return;
                    }
                    stageImage = stored ?? automationImage.UseCamera("StageChoiceMissing");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (IOException ex) when ((ex.HResult & 0xffff) is 32 or 33)
                {
                    if (stageLock is not null) await stageLock.DisposeAsync();
                    _logger.LogInformation("[VdsAutomationImage] Stage {IncidentId}/{PictureType} is already being captured; skip duplicate.", incident.ID, pictureType);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[VdsAutomationImage] Cannot read frozen choice for {IncidentId}/{PictureType}; use CCTV/RTSP.", incident.ID, pictureType);
                    if (stageLock is not null) await stageLock.DisposeAsync();
                    stageLock = null;
                    stageImage = automationImage.UseCamera("StageChoiceReadFailed");
                }
            }
            await using var heldStageLock = stageLock;
            if (stageImage?.Status == "Ready") return; // Completed stage images are immutable on replay.
            if (automationImage is not null && !await IsAutomationStageCurrentAsync(automationImage, cancellationToken)) return;
            if (stageImage?.ImageSource == "vds")
            {
                var vdsStage = stageImage;
                var copied = VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, 1)
                    ? await _images.RunAsync(() => VdsBaseHelper.RegistrationImageFileHp.CopyToStageAsync(vdsStage,
                        _images.Directory(vdsStage.RegistrationTime), _images.Directory(captureTime.Value), cancellationToken),
                        cancellationToken)
                    : stageImage.UseCamera("IncidentSourceChanged");
                if (copied.Status == "Ready" && copied.FileName is { } selectedFile)
                    await _images.RunAsync(() => VdsBaseHelper.RegistrationImageFileHp.PublishToIncidentSlotsAsync(
                        Path.Combine(_images.Directory(captureTime.Value), selectedFile), incident.ID,
                        EquipmentIds(incident.EquipmentId), pictureType, cancellationToken), cancellationToken);
                try { stageImage = await _vdsImages.SaveStageAsync(copied, cancellationToken); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[VdsAutomationImage] Cannot save copy result for {IncidentId}/{PictureType}.", incident.ID, pictureType);
                    if (copied.Status == "Ready") return;
                    stageImage = copied;
                }
                if (stageImage.Status == "Ready") return;
                _logger.LogInformation("[VdsAutomationImage] Use CCTV/RTSP immediately for {IncidentId}/{PictureType}: {Reason}.",
                    incident.ID, pictureType, stageImage.Reason);
            }

            var equipmentIds = EquipmentIds(incident.EquipmentId);
            if (equipmentIds.Count == 0)
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][EQUIPMENT_EMPTY] Sự cố {IncidentId} không có EquipmentId.",
                    traceId,
                    incident.ID);
                return;
            }

            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][EQUIPMENT_OK] Sự cố {IncidentId} có {EquipmentCount} thiết bị: {EquipmentIds}.",
                traceId,
                incident.ID,
                equipmentIds.Count,
                string.Join(",", equipmentIds));

            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][CAMERA_QUERY] Đang tra camera theo EquipmentId.",
                traceId);
            var cameras = await _equipmentRepo.AsQueryable()
                .Where(equipment => equipmentIds.Contains(equipment.ID)
                    && equipment.IsDelete == null
                    && ((equipment.Ip != null && equipment.Ip != string.Empty)
                        || (equipment.Url != null && equipment.Url != string.Empty)))
                .ToListAsync(cancellationToken);

            if (cameras.Count == 0)
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][CAMERA_EMPTY] Không tìm thấy thiết bị đang hoạt động có IP hoặc URL trong {EquipmentCount} EquipmentId của sự cố {IncidentId}.",
                    traceId,
                    equipmentIds.Count,
                    incident.ID);
                return;
            }

            var matchedIds = cameras.Select(camera => camera.ID).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unmatchedIds = equipmentIds.Where(id => !matchedIds.Contains(id)).ToList();
            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][CAMERA_OK] Tìm thấy {CameraCount}/{EquipmentCount} camera; không khớp {UnmatchedCount}: {UnmatchedIds}.",
                traceId,
                cameras.Count,
                equipmentIds.Count,
                unmatchedIds.Count,
                unmatchedIds.Count == 0 ? "<none>" : string.Join(",", unmatchedIds));

            var directory = _images.Directory(captureTime.Value);
            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][DIRECTORY] Chuẩn bị thư mục lưu ảnh {Directory}.",
                traceId,
                directory);
            await _images.RunAsync(() => Task.FromResult(Directory.CreateDirectory(directory)), cancellationToken);

            var savedCount = 0;
            var failedCount = 0;
            for (var cameraIndex = 0; cameraIndex < cameras.Count; cameraIndex++)
            {
                var camera = cameras[cameraIndex];
                var cameraStopwatch = Stopwatch.StartNew();
                try
                {
                    var filePath = Path.Combine(directory, FileName(incident.ID, camera.ID, pictureType));
                    var savedFile = stageImage is null ? null
                        : await _images.RunAsync(() => { var info = new FileInfo(filePath); info.Refresh(); return Task.FromResult(info); }, cancellationToken);
                    if (stageImage is not null && savedFile is not null && savedFile.Exists && savedFile.Length > 0
                        && savedFile.LastWriteTimeUtc >= BaseUtil.TimeServer.ToUtc(captureTime.Value).AddMilliseconds(-4))
                    {
                        savedCount++; // A retry may fill missing cameras, but never replace saved evidence.
                        continue;
                    }
                    _logger.LogInformation(
                        "[IncidentSnapshot][{TraceId}][CAMERA_START] Camera {CameraIndex}/{CameraCount}: EquipmentId {EquipmentId}, IP {Ip}, có URL {HasUrl}, vendor {Vendor}.",
                        traceId,
                        cameraIndex + 1,
                        cameras.Count,
                        camera.ID,
                        camera.Ip,
                        !string.IsNullOrWhiteSpace(camera.Url),
                        camera.Vendor);

                    var snapshotData = await CaptureWithFallbackAsync(
                        camera,
                        captureTime.Value,
                        traceId,
                        cancellationToken);

                    // A slow camera request must not write after a later transition changed the stage date.
                    if (automationImage is not null && !await IsAutomationStageCurrentAsync(automationImage, cancellationToken)) return;

                    var atomicWrite = stageImage is not null;
                    var overwritesExistingFile = await _images.RunAsync(async () =>
                    {
                        var overwrites = File.Exists(filePath);
                        if (!atomicWrite) await File.WriteAllBytesAsync(filePath, snapshotData, cancellationToken);
                        else
                        {
                            var temp = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                            try
                            {
                                await File.WriteAllBytesAsync(temp, snapshotData, cancellationToken);
                                File.Move(temp, filePath, overwrite: true);
                            }
                            finally { if (File.Exists(temp)) File.Delete(temp); }
                        }
                        return overwrites;
                    }, cancellationToken);
                    _logger.LogInformation(
                        "[IncidentSnapshot][{TraceId}][FILE_WRITE] Đã ghi {ByteCount} byte vào {FilePath}; ghi đè {Overwrite}.",
                        traceId,
                        snapshotData.Length,
                        filePath,
                        overwritesExistingFile);
                    savedCount++;
                    _logger.LogInformation(
                        "[IncidentSnapshot][{TraceId}][FILE_OK] Đã lưu ảnh camera {EquipmentId} tại {FilePath} sau {ElapsedMs} ms.",
                        traceId,
                        camera.ID,
                        filePath,
                        cameraStopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogWarning(
                        ex,
                        "[IncidentSnapshot][{TraceId}][CAMERA_FAILED] Chụp camera {EquipmentId} ({Ip}) cho sự cố {IncidentId}, picType {PictureType} thất bại sau {ElapsedMs} ms.",
                        traceId,
                        camera.ID,
                        camera.Ip,
                        incident.ID,
                        pictureType,
                        cameraStopwatch.ElapsedMilliseconds);
                }
            }

            if (stageImage is not null)
            {
                try
                {
                    await _vdsImages.SaveStageAsync(stageImage with { Status = savedCount > 0 ? "Ready" : "CaptureFailed" }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[VdsAutomationImage] Cannot save camera result for {IncidentId}/{PictureType}.", incident.ID, pictureType);
                }
            }

            const string summaryMessage =
                "[IncidentSnapshot][{TraceId}][SUMMARY] Sự cố {IncidentId}, picType {PictureType}: lưu {SavedCount}/{CameraCount} ảnh, lỗi {FailedCount} camera sau {ElapsedMs} ms.";
            if (failedCount == 0)
                _logger.LogInformation(
                    summaryMessage,
                    traceId,
                    incident.ID,
                    pictureType,
                    savedCount,
                    cameras.Count,
                    failedCount,
                    totalStopwatch.ElapsedMilliseconds);
            else
                _logger.LogWarning(
                    summaryMessage,
                    traceId,
                    incident.ID,
                    pictureType,
                    savedCount,
                    cameras.Count,
                    failedCount,
                    totalStopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[IncidentSnapshot][{TraceId}][FATAL] Không thể chụp ảnh cho sự cố {IncidentId} tại state {State} sau {ElapsedMs} ms.",
                traceId,
                incidentId,
                state,
                totalStopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<bool> IsAutomationStageCurrentAsync(VdsAutomationImageDecisionOutput queued, CancellationToken ct)
    {
        var current = await _incidentRepo.Context.Queryable<TmsIncident>()
            .Where(x => x.ID == queued.IncidentId && x.TenantId == queued.TenantId && x.IsDelete == null).FirstAsync(ct);
        var capture = current is null ? null : CaptureTime(current, queued.PictureType);
        if (current is not null && capture is not null
            && queued.Matches(current.TenantId, current.ID, queued.PictureType, capture.Value)) return true;
        _logger.LogInformation("[VdsAutomationImage] Skip stale stage {IncidentId}/{PictureType} before writing; incident or transition changed.", queued.IncidentId, queued.PictureType);
        return false;
    }

    private async Task<byte[]> CaptureWithFallbackAsync(
        TmsEquipment camera,
        DateTime captureTime,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(camera.Ip))
        {
            var cctvStopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation(
                    "[IncidentSnapshot][{TraceId}][CCTV_START] Gọi CctvSnapshotInput cho camera {EquipmentId}, IP {Ip}, thời điểm {CaptureTime:O}.",
                    traceId,
                    camera.ID,
                    camera.Ip,
                    captureTime);
                var data = await CaptureFromCctvAsync(camera, captureTime, traceId, cancellationToken);
                _logger.LogInformation(
                    "[IncidentSnapshot][{TraceId}][CCTV_OK] CCTV trả {ByteCount} byte cho camera {EquipmentId} sau {ElapsedMs} ms; JPEG hợp lệ {IsJpeg}.",
                    traceId,
                    data.Length,
                    camera.ID,
                    cctvStopwatch.ElapsedMilliseconds,
                    IsJpeg(data));
                return data;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][CCTV_CANCELED] Hủy gọi CCTV cho camera {EquipmentId} sau {ElapsedMs} ms.",
                    traceId,
                    camera.ID,
                    cctvStopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[IncidentSnapshot][{TraceId}][CCTV_FAILED] CctvSnapshotInput thất bại cho camera {EquipmentId} ({Ip}) sau {ElapsedMs} ms; chuyển sang chụp RTSP.",
                    traceId,
                    camera.ID,
                    camera.Ip,
                    cctvStopwatch.ElapsedMilliseconds);
            }
        }
        else
        {
            _logger.LogWarning(
                "[IncidentSnapshot][{TraceId}][CCTV_SKIP] Camera {EquipmentId} không có IP; chuyển thẳng sang RTSP.",
                traceId,
                camera.ID);
        }

        return await CaptureFromRtspAsync(camera, traceId, cancellationToken);
    }

    private async Task<byte[]> CaptureFromCctvAsync(
        TmsEquipment camera,
        DateTime captureTime,
        string traceId,
        CancellationToken cancellationToken)
    {
        var inputType = CctvSnapshotInputType.Value;
        if (inputType == null)
        {
            _logger.LogError(
                "[IncidentSnapshot][{TraceId}][CCTV_TYPE_MISSING] Không tìm thấy runtime type {InputTypeName}.",
                traceId,
                CctvSnapshotInputTypeName);
            throw new InvalidOperationException(
                $"Không tìm thấy kiểu {CctvSnapshotInputTypeName} trong module CCTV.");
        }

        var input = Activator.CreateInstance(inputType)
            ?? throw new InvalidOperationException("Không thể khởi tạo CctvSnapshotInput.");

        SetProperty(inputType, input, "Ip", camera.Ip);
        SetProperty(inputType, input, "Vendor", camera.Vendor);
        SetProperty(inputType, input, "FromDate", captureTime);
        SetProperty(inputType, input, "Format", "jpg");

        // Không tham chiếu project CCTV để tránh dependency vòng. Wolverine vẫn dispatch
        // instance runtime này đến CctvSnapshotQueryHandler hiện có trong module CCTV.
        // CCTV treo thì không chờ mãi: quá hạn ném OperationCanceledException (token ngoài chưa huỷ) ⇒ nơi gọi chuyển sang RTSP.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(CctvSnapshotTimeout);
        var output = await _messageBus.InvokeAsync<object>(input, timeoutSource.Token, CctvSnapshotTimeout);
        var dataProperty = output?.GetType().GetProperty("Data");
        var dataValue = dataProperty?.GetValue(output);
        var data = dataValue as byte[];
        _logger.LogInformation(
            "[IncidentSnapshot][{TraceId}][CCTV_RESPONSE] Camera {EquipmentId}: output type {OutputType}, có property Data {HasDataProperty}, kiểu Data {DataType}, kích thước {ByteCount} byte.",
            traceId,
            camera.ID,
            output?.GetType().FullName ?? "<null>",
            dataProperty != null,
            dataValue?.GetType().FullName ?? "<null>",
            data?.Length ?? 0);
        if (data == null || data.Length == 0)
            throw new InvalidOperationException("CCTV không trả về dữ liệu ảnh.");

        return data;
    }

    private async Task<byte[]> CaptureFromRtspAsync(
        TmsEquipment camera,
        string traceId,
        CancellationToken cancellationToken)
    {
        var rtspStopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "[IncidentSnapshot][{TraceId}][RTSP_CONFIG] Camera {EquipmentId}: có IP {HasIp}, có URL {HasUrl}, port {Port}, có tài khoản {HasCredentials}.",
            traceId,
            camera.ID,
            !string.IsNullOrWhiteSpace(camera.Ip),
            !string.IsNullOrWhiteSpace(camera.Url),
            camera.Port,
            !string.IsNullOrWhiteSpace(camera.Username));

        var rtspUrl = BuildRtspUrl(camera);
        if (rtspUrl == null)
        {
            _logger.LogWarning(
                "[IncidentSnapshot][{TraceId}][RTSP_URL_INVALID] Camera {EquipmentId} chưa cấu hình URL RTSP hợp lệ.",
                traceId,
                camera.ID);
            throw new InvalidOperationException(
                $"Camera {camera.ID} chưa cấu hình URL RTSP hợp lệ.");
        }

        var safeEndpoint = DescribeRtspEndpoint(rtspUrl);
        var startInfo = CreateFfmpegStartInfo(rtspUrl);
        _logger.LogInformation(
            "[IncidentSnapshot][{TraceId}][RTSP_START] Chạy FFmpeg {FfmpegPath} cho camera {EquipmentId}, endpoint {RtspEndpoint}, timeout {TimeoutSeconds} giây.",
            traceId,
            startInfo.FileName,
            camera.ID,
            safeEndpoint,
            RtspSnapshotTimeout.TotalSeconds);

        using var process = new Process
        {
            StartInfo = startInfo
        };
        var started = false;

        try
        {
            started = process.Start();
            if (!started)
            {
                _logger.LogError(
                    "[IncidentSnapshot][{TraceId}][FFMPEG_START_FAILED] Process FFmpeg không khởi động cho camera {EquipmentId}.",
                    traceId,
                    camera.ID);
                throw new InvalidOperationException("Không thể khởi động FFmpeg.");
            }

            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][FFMPEG_STARTED] FFmpeg PID {ProcessId} đã khởi động cho camera {EquipmentId}.",
                traceId,
                process.Id,
                camera.ID);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(RtspSnapshotTimeout);
            await using var imageStream = new MemoryStream();

            var standardOutputTask = process.StandardOutput.BaseStream.CopyToAsync(
                imageStream,
                timeoutSource.Token);
            var standardErrorTask = process.StandardError.ReadToEndAsync(timeoutSource.Token);

            try
            {
                await Task.WhenAll(
                    process.WaitForExitAsync(timeoutSource.Token),
                    standardOutputTask,
                    standardErrorTask);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][FFMPEG_TIMEOUT] FFmpeg camera {EquipmentId}, endpoint {RtspEndpoint} vượt quá {TimeoutSeconds} giây; đang kết thúc process.",
                    traceId,
                    camera.ID,
                    safeEndpoint,
                    RtspSnapshotTimeout.TotalSeconds);
                TryKill(process);
                throw new TimeoutException(
                    $"FFmpeg không chụp được frame RTSP trong {RtspSnapshotTimeout.TotalSeconds:0} giây.");
            }

            var standardError = await standardErrorTask;
            var safeDiagnostic = SanitizeFfmpegDiagnostic(
                standardError,
                rtspUrl,
                camera.Username,
                camera.Password);
            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][FFMPEG_EXIT] Camera {EquipmentId}: exit code {ExitCode}, stdout {ByteCount} byte, stderr {FfmpegDiagnostic}, thời gian {ElapsedMs} ms.",
                traceId,
                camera.ID,
                process.ExitCode,
                imageStream.Length,
                safeDiagnostic,
                rtspStopwatch.ElapsedMilliseconds);

            if (process.ExitCode != 0)
                throw new InvalidOperationException(
                    $"FFmpeg chụp RTSP thất bại với exit code {process.ExitCode}: {safeDiagnostic}");

            var data = imageStream.ToArray();
            if (!IsJpeg(data))
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][RTSP_INVALID_JPEG] FFmpeg camera {EquipmentId} trả {ByteCount} byte nhưng không đúng dấu hiệu JPEG.",
                    traceId,
                    camera.ID,
                    data.Length);
                throw new InvalidOperationException("FFmpeg không trả về dữ liệu JPEG hợp lệ.");
            }

            _logger.LogInformation(
                "[IncidentSnapshot][{TraceId}][RTSP_OK] Đã chụp {ByteCount} byte JPEG từ camera {EquipmentId}, endpoint {RtspEndpoint}, sau {ElapsedMs} ms.",
                traceId,
                data.Length,
                camera.ID,
                safeEndpoint,
                rtspStopwatch.ElapsedMilliseconds);

            return data;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "[IncidentSnapshot][{TraceId}][RTSP_CANCELED] Đã hủy chụp RTSP camera {EquipmentId} sau {ElapsedMs} ms.",
                traceId,
                camera.ID,
                rtspStopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Win32Exception ex)
        {
            _logger.LogError(
                ex,
                "[IncidentSnapshot][{TraceId}][FFMPEG_NOT_FOUND] Không thể chạy FFmpeg {FfmpegPath} cho camera {EquipmentId}.",
                traceId,
                startInfo.FileName,
                camera.ID);
            throw new InvalidOperationException(
                "Không thể chạy FFmpeg. Hãy cài FFmpeg vào PATH hoặc thư mục ffmpeg của ứng dụng.",
                ex);
        }
        finally
        {
            if (started && !process.HasExited)
            {
                _logger.LogWarning(
                    "[IncidentSnapshot][{TraceId}][FFMPEG_CLEANUP] FFmpeg camera {EquipmentId} vẫn chạy; đang kết thúc process.",
                    traceId,
                    camera.ID);
                TryKill(process);
            }
        }
    }

    internal static string? BuildRtspUrl(TmsEquipment camera)
    {
        var configuredUrl = camera.Url?.Trim();
        if (string.IsNullOrWhiteSpace(configuredUrl))
            return null;

        UriBuilder builder;
        if (Uri.TryCreate(configuredUrl, UriKind.Absolute, out var absoluteUri))
        {
            if (!absoluteUri.Scheme.Equals("rtsp", StringComparison.OrdinalIgnoreCase)
                && !absoluteUri.Scheme.Equals("rtsps", StringComparison.OrdinalIgnoreCase))
                return null;

            builder = new UriBuilder(absoluteUri);
        }
        else
        {
            var ip = camera.Ip?.Trim();
            if (string.IsNullOrWhiteSpace(ip))
                return null;

            var port = int.TryParse(camera.Port, out var configuredPort)
                && configuredPort is > 0 and <= 65535
                    ? configuredPort
                    : 554;
            var baseUri = new UriBuilder("rtsp", ip, port, "/").Uri;
            if (!Uri.TryCreate(baseUri, configuredUrl.TrimStart('/'), out var relativeUri))
                return null;

            builder = new UriBuilder(relativeUri);
        }

        if (string.IsNullOrEmpty(builder.UserName) && !string.IsNullOrWhiteSpace(camera.Username))
        {
            builder.UserName = camera.Username.Trim();
            builder.Password = camera.Password ?? string.Empty;
        }
        else if (string.IsNullOrEmpty(builder.Password) && !string.IsNullOrEmpty(camera.Password))
        {
            builder.Password = camera.Password;
        }

        return builder.Uri.AbsoluteUri;
    }

    private static ProcessStartInfo CreateFfmpegStartInfo(string rtspUrl)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveFfmpegExecutable(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in new[]
        {
            "-nostdin",
            "-hide_banner",
            "-loglevel", "error",
            "-rtsp_transport", "tcp",
            // Không dùng -rw_timeout vì một số bản FFmpeg cũ/bản build rút gọn không hỗ trợ.
            // CaptureFromRtspAsync đã giới hạn toàn bộ process bằng RtspSnapshotTimeout và TryKill.
            "-i", rtspUrl,
            "-map", "0:v:0",
            "-frames:v", "1",
            "-an",
            "-f", "image2pipe",
            "-c:v", "mjpeg",
            "-q:v", "2",
            "pipe:1"
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static string ResolveFfmpegExecutable()
    {
        var executableName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var configuredPath = Environment.GetEnvironmentVariable("FFMPEG_PATH")?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (File.Exists(configuredPath))
                return configuredPath;

            var configuredExecutable = Path.Combine(configuredPath, executableName);
            if (File.Exists(configuredExecutable))
                return configuredExecutable;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ffmpeg", "bin", executableName),
            Path.Combine(AppContext.BaseDirectory, "ffmpeg", executableName),
            Path.Combine(AppContext.BaseDirectory, executableName)
        };

        return candidates.FirstOrDefault(File.Exists) ?? executableName;
    }

    private static bool IsJpeg(byte[] data)
        => data.Length >= 5
            && data[0] == 0xff
            && data[1] == 0xd8
            && data[2] == 0xff
            && data[^2] == 0xff
            && data[^1] == 0xd9;

    private static string DescribeRtspEndpoint(string rtspUrl)
    {
        if (!Uri.TryCreate(rtspUrl, UriKind.Absolute, out var uri))
            return "<invalid-rtsp-url>";

        var host = uri.HostNameType == UriHostNameType.IPv6
            ? $"[{uri.Host}]"
            : uri.Host;
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        return $"{uri.Scheme}://{host}{port}{uri.AbsolutePath}";
    }

    private static string SanitizeFfmpegDiagnostic(
        string? diagnostic,
        string rtspUrl,
        string? username,
        string? password)
    {
        if (string.IsNullOrWhiteSpace(diagnostic))
            return "<empty>";

        var sanitized = diagnostic.Replace(
            rtspUrl,
            DescribeRtspEndpoint(rtspUrl),
            StringComparison.OrdinalIgnoreCase);
        sanitized = RtspUrlPattern.Replace(
            sanitized,
            match => DescribeRtspEndpoint(match.Value));

        if (!string.IsNullOrWhiteSpace(username))
            sanitized = sanitized.Replace(username, "***", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(password))
            sanitized = sanitized.Replace(password, "***", StringComparison.Ordinal);

        sanitized = sanitized
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        return sanitized.Length <= MaxFfmpegDiagnosticLength
            ? sanitized
            : $"{sanitized[..MaxFfmpegDiagnosticLength]}...";
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort cleanup; giữ lại lỗi chụp RTSP ban đầu.
        }
    }

    private static void SetProperty(Type type, object target, string propertyName, object? value)
    {
        var property = type.GetProperty(propertyName)
            ?? throw new InvalidOperationException(
                $"CctvSnapshotInput không có thuộc tính {propertyName}.");
        property.SetValue(target, value);
    }

    internal static string FileName(string incidentId, string equipmentId, int pictureType)
        => $"{SafeFilePart(incidentId)}_{SafeFilePart(equipmentId)}_{pictureType}.jpeg";

    internal static string SafeFilePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Trim().Select(character => invalid.Contains(character) ? '_' : character));
    }

    internal static List<string> EquipmentIds(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    internal static DateTime? CaptureTime(TmsIncident incident, int pictureType)
        => pictureType switch
        {
            1 => incident.CreatedDate ?? incident.StartDate,
            2 => incident.ValidatedDate,
            3 => incident.ApprovedDate,
            4 => incident.FinishedDate,
            _ => null
        };

    private static int PictureTypeFromState(int state)
        => state switch
        {
            (int)TmsIncidentStateEnum.Registration => 1,
            (int)TmsIncidentStateEnum.Verified => 2,
            (int)TmsIncidentStateEnum.InProgress => 3,
            (int)TmsIncidentStateEnum.Skipped
                or (int)TmsIncidentStateEnum.Finished
                or (int)TmsIncidentStateEnum.Canceled => 4,
            _ => 0
        };
}
