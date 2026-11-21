using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Modules.TMS.Core.Dto.IncidentControl;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure.Services.Process;
using Shared.DTO.Constants.Module;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Wolverine;

namespace Modules.VMS.Infrastructure.Services.Schedule
{
    public class ScheduleService : IScoped
    {
        private readonly BaseRepository<VmsProcess> _vmsProcessRepository;
        private readonly BaseRepository<VmsProcessImg> _vmsProcessImgRepository;
        private readonly VmsProcessService _vmsProcessService;
        private readonly IMessageBus _messageBus;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduleService> _logger;

        // Thời gian chờ tối thiểu khi một chu kỳ phát bị lỗi, tránh vòng lặp quay liên tục khi lỗi kéo dài (DB, NATS...)
        private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(3);

        // State dùng chung — BẮT BUỘC static + ConcurrentDictionary
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsDict = new();
        private static readonly ConcurrentDictionary<string, Task> _taskDict = new();

        // Khóa theo biển cho AppendImageToIncidentProcess: 2 lệnh nối tiếp đồng thời (2 sự cố tự động, 2 người trực) vào cùng
        // một biển sẽ cùng thấy "chưa có tiến trình" và tạo 2 schedule-incident; thread chỉ phát ảnh của 1 tiến trình.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _appendLocks = new(StringComparer.OrdinalIgnoreCase);

        public ScheduleService(BaseRepository<VmsProcess> vmsProcessRepository,BaseRepository<VmsProcessImg> vmsProcessImgRepository,
            VmsProcessService vmsProcessService,IMessageBus messageBus,IServiceScopeFactory scopeFactory,
            ILogger<ScheduleService> logger)
        {
            _logger = logger;
            _vmsProcessRepository = vmsProcessRepository;
            _vmsProcessImgRepository = vmsProcessImgRepository;
            _vmsProcessService = vmsProcessService;
            _messageBus = messageBus;
            _scopeFactory = scopeFactory;
        }

        // Đọc trạng thái thread đang chạy (thay cho việc đọc thẳng TaskDictionary)
        public static List<string> GetRunningThreadIds() => _taskDict.Keys.ToList();

        /// <summary>
        /// Khóa thread của vòng lặp sự cố tách riêng khỏi lịch thường (lịch thường dùng ThreadId = vmsId).
        /// Nếu dùng chung khóa, pause lịch thường sẽ giết luôn playlist sự cố và ngược lại.
        /// </summary>
        public static string IncidentThreadKey(string vmsId) => $"{vmsId}|incident";

        private static string GetThreadKey(VmsProcess process) =>
            process.Type == VmsProcessTypeConst.ScheduleIncident ? IncidentThreadKey(process.VmsId) : (process.ThreadId ?? process.VmsId);


        /// <summary>
        /// Bắt đầu thực thi tiến trình lập lịch (chuyển từ ScheduleCommandHandler.StartSchedule)
        /// Author: ChuongLP
        /// Date: 2026-06-25
        /// Update: 14/10/2026 - Hỗ trợ schedule-incident và tôn trọng trạng thái pause-incident
        /// </summary>
        public async Task StartSchedule(List<VmsProcess> listVmsProcessMemory, List<VmsProcessImg> listVmsProcessImgMemory)
        {
            if (listVmsProcessMemory == null || listVmsProcessMemory.Count == 0) return;

            // Lọc chỉ những process có State == "run" để chạy. Bỏ qua các process đang pause-incident.
            var activeProcesses = listVmsProcessMemory
                .Where(p => p.State == VmsProcessStateConst.Run || p.State == "run")
                .ToList();

            if (activeProcesses.Count == 0) return;

            var equipmentListInfos = await _vmsProcessService.GetListEquipmentInfo(activeProcesses);

            foreach (var vmsProcess in activeProcesses)
            {
                try
                {
                    var id = vmsProcess.ID;
                    var runThreadInput = new RunThreadInput
                    {
                        // Sắp xếp danh sách ảnh theo thứ tự tạo để đảm bảo thứ tự phát chuẩn xác
                        ListVmsProcessImage = listVmsProcessImgMemory != null
                            ? listVmsProcessImgMemory.Where(l => l.ProcessId == id && (l.IsActive == null || l.IsActive == true)).OrderBy(l => l.CreateTime).ThenBy(l => l.ID).ToList()
                            : new List<VmsProcessImg>(),
                        ProcessID = id,
                        VmsID = vmsProcess.VmsId,
                        ListEquipmentInfos = equipmentListInfos.Adapt<List<EquipmentInfoInput>>(),
                        Brightness = vmsProcess.Brightness ?? 100,
                        Priority = vmsProcess.Priority,
                        ThreadId = vmsProcess.ThreadId,
                        ProcessType = vmsProcess.Type ?? VmsProcessTypeConst.Schedule
                    };

                    // Nếu biển đã có thread đang chạy thì không tạo thêm (tránh 2 vòng cùng đẩy ra 1 biển)
                    var threadKey = GetThreadKey(vmsProcess);
                    var cts = new CancellationTokenSource();
                    if (_ctsDict.TryAdd(threadKey, cts))
                    {
                        using (ExecutionContext.SuppressFlow())
                        {
                            // Task.Run(async) trả về task của CẢ vòng lặp (new Task(async lambda) xong ngay ở await đầu,
                            // khiến Stop không chờ được lệnh gửi đang bay và IsCompleted luôn true).
                            _taskDict[threadKey] = Task.Run(() => RunThreadAsync(runThreadInput, threadKey, cts));
                        }
                    }
                    else
                    {
                        cts.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không khởi động được vòng lặp lịch VMS cho process {ProcessId} (biển {VmsId})", vmsProcess.ID, vmsProcess.VmsId);
                }
            }
        }

        /// <summary>
        /// Hàm chạy lập lịch (chuyển từ ScheduleCommandHandler.RunThread, dùng TryGetValue)
        /// Author: ChuongLP
        /// Date: 2026-06-25
        /// Update: 14/10/2026 - Tự động nạp động danh sách ảnh mới nhất cho schedule-incident mỗi chu kỳ
        /// </summary>
        private async Task RunThreadAsync(RunThreadInput runThreadInput, string threadKey, CancellationTokenSource cts)
        {
            try
            {
                await RunThreadLoopAsync(runThreadInput, cts);
            }
            finally
            {
                // Tự dọn khi vòng lặp tự kết thúc (hết ảnh), chỉ khi khóa vẫn là của chính thread này
                if (_ctsDict.TryRemove(new KeyValuePair<string, CancellationTokenSource>(threadKey, cts)))
                    _taskDict.TryRemove(threadKey, out _);
            }
        }

        private async Task RunThreadLoopAsync(RunThreadInput runThreadInput, CancellationTokenSource cts)
        {
            var detectWriteLog = true;
            var isWriteLog = true;

            while (!cts.IsCancellationRequested)
            {
                // Đối với vòng lặp sự cố (schedule-incident): mỗi chu kỳ đọc lại danh sách ảnh active từ DB
                // để lập tức nhận ảnh mới được append hoặc loại bỏ ảnh của sự cố vừa kết thúc mà không cần kill thread
                if (runThreadInput.ProcessType == VmsProcessTypeConst.ScheduleIncident)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var imgRepo = scope.ServiceProvider.GetRequiredService<BaseRepository<VmsProcessImg>>();
                        var latestImages = await imgRepo.AsQueryable()
                            .Where(img => img.ProcessId == runThreadInput.ProcessID && (img.IsActive == null || img.IsActive == true))
                            .OrderBy(img => img.CreateTime)
                            .ToListAsync();

                        if (latestImages == null || latestImages.Count == 0)
                        {
                            // Hết ảnh sự cố trong playlist -> dừng tiến trình
                            return;
                        }
                        runThreadInput.ListVmsProcessImage = latestImages;
                    }
                    catch (Exception ex)
                    {
                        // Đọc DB lỗi: dùng tạm danh sách ảnh đang có, không dừng vòng lặp
                        _logger.LogWarning(ex, "Không đọc được danh sách ảnh sự cố của process {ProcessId}, dùng danh sách cũ", runThreadInput.ProcessID);
                    }
                }
                else
                {
                    if (!runThreadInput.ListVmsProcessImage.Any()) return;   // [SỬA] continue -> return (hết busy-loop 100% CPU)
                }

                try
                {
                    foreach (var vmsProcessImg in runThreadInput.ListVmsProcessImage)
                    {
                        try
                        {
                            // Chỉ dùng CTS của chính thread này: tra lại theo khóa có thể "mượn" CTS của thread mới
                            // cùng khóa, làm thread cũ đã bị dừng sống lại và phát đè lên biển.
                            if (cts.IsCancellationRequested)
                                return;

                            // Xác định thời gian hiển thị và mốc kết thúc mục tiêu của ảnh
                            var ttlSeconds = vmsProcessImg.TimeToLive.HasValue && vmsProcessImg.TimeToLive.Value > 0
                                ? vmsProcessImg.TimeToLive.Value
                                : 10;
                            var targetEndTime = DateTime.UtcNow.AddMilliseconds(ttlSeconds * 1000);

                            var prepareVmsData = new PrepareVmsData
                            {
                                ListEquipmentInfos = runThreadInput.ListEquipmentInfos.Adapt<List<EquipmentInfoInput>>()
                            };

                            if (prepareVmsData.ListEquipmentInfos?.Count > 0)
                            {
                                prepareVmsData.ContentInfoInput.Brightness = runThreadInput.Brightness;
                                prepareVmsData.ContentInfoInput.ThreadId = runThreadInput.ThreadId;
                                prepareVmsData.ContentInfoInput.Priority = runThreadInput.Priority;
                                prepareVmsData.ContentInfoInput.Url = vmsProcessImg.Url;
                                prepareVmsData.ContentInfoInput.IncidentId = vmsProcessImg.IncidentId;

                                prepareVmsData.ProcessInfoInput.ProcessType = runThreadInput.ProcessType;
                                prepareVmsData.ProcessInfoInput.ActionType = "run";
                                prepareVmsData.ProcessInfoInput.TimeToLive = ttlSeconds;
                            }

                            if (!cts.IsCancellationRequested)
                            {
                                using var scope = _scopeFactory.CreateScope();        // Thuê scope mới mỗi chu kỳ
                                var processService = scope.ServiceProvider.GetRequiredService<VmsProcessService>();

                                await processService.ChangeVmsContent(prepareVmsData, // Gửi nội dung ra thiết bị
                                                    runThreadInput.VmsID, isWriteLog);
                            }

                            if (!detectWriteLog) isWriteLog = false;

                            // Ảnh cuối cùng → tắt log để tránh log lặp khi quay vòng
                            if (runThreadInput.ListVmsProcessImage.IndexOf(vmsProcessImg)
                                    == runThreadInput.ListVmsProcessImage.Count - 1)
                                detectWriteLog = false;

                            // Ngủ đúng khoảng thời gian còn lại đến targetEndTime để các thiết bị giữ đều nhịp
                            var timeToSleep = (int)(targetEndTime - DateTime.UtcNow).TotalMilliseconds;
                            if (timeToSleep > 5)
                            {
                                await Task.Delay(timeToSleep, cts.Token);
                            }
                        }
                        catch (OperationCanceledException)                            // [SỬA] bắt đúng lớp cha
                        {
                            if (!string.IsNullOrEmpty(runThreadInput.ThreadId) && _taskDict.ContainsKey(runThreadInput.ThreadId))
                            {
                                using var scope = _scopeFactory.CreateScope();        // [THÊM] repo từ scope sống
                                var repo = scope.ServiceProvider
                                                .GetRequiredService<BaseRepository<VmsProcess>>();
                                repo.AsUpdateable()
                                    .Where(l => runThreadInput.ProcessID == l.ID && l.State == "run" && l.Type == "schedule")
                                    .SetColumns(l => l.State, "stop")
                                    .ExecuteCommand();
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            // Lỗi ở chu kỳ này (gửi thiết bị, cập nhật DB...) vẫn phải chờ trước khi sang ảnh kế,
                            // nếu không vòng lặp sẽ quay liên tục và dồn lệnh ra biển khi lỗi kéo dài.
                            _logger.LogError(ex, "Lỗi phát ảnh lịch VMS (process {ProcessId}, biển {VmsId}, ảnh {Url})",
                                runThreadInput.ProcessID, runThreadInput.VmsID, vmsProcessImg.Url);
                            try { await Task.Delay(ErrorBackoff, cts.Token); }
                            catch (OperationCanceledException) { return; }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi vòng lặp lịch VMS (process {ProcessId})", runThreadInput.ProcessID);
                    try { await Task.Delay(ErrorBackoff, cts.Token); }
                    catch (OperationCanceledException) { return; }
                }
            }
        }

        /// <summary>
        /// Dừng 1 thread lập lịch (chuyển từ ScheduleCommandHandler.StopThreadRunSchedule)
        /// Author: ChuongLP
        /// Date: 2026-06-25
        /// </summary>
        /// <param name="threadId"></param>
        public static void StopThreadRunSchedule(string threadId)
        {
            try
            {
                if (!_ctsDict.TryGetValue(threadId, out var cts)) return;

                cts.Cancel();

                // Chờ vòng lặp thoát hẳn (kể cả lệnh gửi đang bay) để lệnh điều khiển mới không bị ảnh lịch đè lại.
                if (_taskDict.TryGetValue(threadId, out var task))
                {
                    try { task.Wait(TimeSpan.FromSeconds(10)); } // timeout, tránh treo request nếu thiết bị nghẽn = ChuongLP - 14-07-2026
                    catch (AggregateException) { }
                }

                // Dọn dictionary. Không Dispose CTS: thread có thể còn chạy nếu quá timeout và vẫn đọc token.
                _taskDict.TryRemove(threadId, out _);
                _ctsDict.TryRemove(new KeyValuePair<string, CancellationTokenSource>(threadId, cts));
            }
            catch (Exception ex)
            {
                // TODO: log
                throw; // dùng throw; thay vì throw ex; để giữ stack trace
            }
        }


        /// <summary>
        /// Dừng nhiều thread cùng lúc. Mỗi StopThreadRunSchedule có thể chờ tới 10s cho lệnh gửi đang bay;
        /// dừng tuần tự thì lệnh Ghi đè / reset nhiều biển phải chờ cộng dồn trước khi gửi nội dung mới.
        /// </summary>
        public static void StopThreadsRunSchedule(IEnumerable<string> threadIds)
        {
            var ids = (threadIds ?? Enumerable.Empty<string>()).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            if (ids.Count == 1) StopThreadRunSchedule(ids[0]);
            else if (ids.Count > 1) Parallel.ForEach(ids, StopThreadRunSchedule);
        }

        /// <summary>
        /// Dừng các tiến trình lập lịch theo danh sách VmsId
        /// Author: ChuongLP
        /// Date: 2026-06-25
        /// </summary>
        public async Task StopScheduleByVmsIds(List<string> vmsIds)
        {
            if (vmsIds == null || vmsIds.Count == 0) return;

            await _vmsProcessRepository.AsUpdateable()
                .Where(l => vmsIds.Contains(l.VmsId) && l.State == "run" && l.Type == "schedule")
                .SetColumns(l => l.State, "stop")
                .ExecuteCommandAsync();

            StopThreadsRunSchedule(vmsIds); // vmsId chính là threadId
        }

        /// <summary>
        /// Tạm dừng các tiến trình lập lịch thường khi có sự cố chiếm giữ biển 
        /// Chuyển State từ "run" sang "pause-incident" và dừng thread phát
        /// Author: ChuongLP
        /// Date: 2026-10-14
        /// </summary>
        public async Task PauseScheduleByVmsIds(List<string> vmsIds)
        {
            if (vmsIds == null || vmsIds.Count == 0) return;

            await _vmsProcessRepository.AsUpdateable()
                .Where(l => vmsIds.Contains(l.VmsId) && l.State == VmsProcessStateConst.Run && l.Type == VmsProcessTypeConst.Schedule)
                .SetColumns(l => l.State, VmsProcessStateConst.PauseIncident)
                .ExecuteCommandAsync();

            StopThreadsRunSchedule(vmsIds);
        }

        /// <summary>
        /// Xử lý lịch thường trước khi Ghi đè:
        /// - Lệnh thuộc sự cố: tạm dừng (pause-incident) để khôi phục khi hết sự cố (DEC-04).
        /// - Lệnh thủ công không gắn sự cố: dừng hẳn như logic gốc, kể cả lịch đang pause-incident. Không có sự cố nào
        ///   kết thúc để đánh thức lịch này, nếu chỉ pause thì lịch treo mãi rồi bất ngờ chạy lại ở lần reset sự cố sau.
        /// Author: ChuongLP
        /// Date: 2026-10-19
        /// </summary>
        public async Task HoldScheduleBeforeOverrideAsync(List<string> vmsIds, string? incidentId)
        {
            if (vmsIds == null || vmsIds.Count == 0) return;

            if (!string.IsNullOrEmpty(incidentId))
            {
                await PauseScheduleByVmsIds(vmsIds);
                return;
            }

            await _vmsProcessRepository.AsUpdateable()
                .Where(l => vmsIds.Contains(l.VmsId) && l.Type == VmsProcessTypeConst.Schedule
                    && (l.State == VmsProcessStateConst.Run || l.State == VmsProcessStateConst.PauseIncident))
                .SetColumns(l => l.State, VmsProcessStateConst.Stop)
                .ExecuteCommandAsync();

            StopThreadsRunSchedule(vmsIds);
        }

        /// <summary>
        /// Khôi phục các tiến trình lập lịch thường đang bị tạm dừng do sự cố
        /// Chuyển State từ "pause-incident" sang "run" và khởi động lại thread phát
        /// Author: ChuongLP
        /// Date: 2026-10-14
        /// </summary>
        public async Task<List<VmsProcess>> ResumeScheduleByVmsIds(List<string> vmsIds)
        {
            if (vmsIds == null || vmsIds.Count == 0) return new List<VmsProcess>();

            var pausedProcesses = await _vmsProcessRepository.AsQueryable()
                .Where(l => vmsIds.Contains(l.VmsId) && l.State == VmsProcessStateConst.PauseIncident && l.Type == VmsProcessTypeConst.Schedule)
                .ToListAsync();

            if (pausedProcesses == null || pausedProcesses.Count == 0) return new List<VmsProcess>();

            var processIds = pausedProcesses.Select(p => p.ID).ToList();

            await _vmsProcessRepository.AsUpdateable()
                .Where(l => processIds.Contains(l.ID))
                .SetColumns(l => l.State, VmsProcessStateConst.Run)
                .ExecuteCommandAsync();

            foreach (var p in pausedProcesses)
            {
                p.State = VmsProcessStateConst.Run;
            }

            var images = await _vmsProcessImgRepository.AsQueryable()
                .Where(img => processIds.Contains(img.ProcessId) && (img.IsActive == null || img.IsActive == true))
                .OrderBy(img => img.CreateTime)
                .ToListAsync();

            await StartSchedule(pausedProcesses, images);
            return pausedProcesses;
        }

        /// <summary>
        /// Chuyển các lịch đang kẹt ở pause-incident về run khi biển KHÔNG còn sự cố nào đang xử lý.
        /// Lịch chỉ được đánh thức khi reset sự cố; nếu reset không chạy / lỗi thì lịch treo mãi và restart BE cũng không
        /// nạp lại (chỉ nạp state run). Gọi lúc khởi động, trước khi nạp danh sách lịch cần chạy lại.
        /// Chỉ đổi state trong DB, việc khởi động vòng lặp do bước nạp lịch phía sau đảm nhiệm.
        /// </summary>
        public async Task<int> RecoverStalePausedSchedulesAsync()
        {
            var paused = await _vmsProcessRepository.AsQueryable()
                .Where(l => l.State == VmsProcessStateConst.PauseIncident && l.Type == VmsProcessTypeConst.Schedule)
                .ToListAsync();
            if (paused == null || paused.Count == 0) return 0;

            var recoverIds = new List<string>();
            foreach (var p in paused)
            {
                // Tra cứu lỗi thì giữ nguyên pause để không chen vào biển đang có sự cố
                var controls = await _vmsProcessService.GetIncidentControlsByEquipmentAsync(p.VmsId, onlyActiveIncident: true);
                if (controls == null || controls.Count == 0)
                    recoverIds.Add(p.ID);
            }
            if (recoverIds.Count == 0) return 0;

            await _vmsProcessRepository.AsUpdateable()
                .Where(l => recoverIds.Contains(l.ID) && l.State == VmsProcessStateConst.PauseIncident)
                .SetColumns(l => l.State, VmsProcessStateConst.Run)
                .ExecuteCommandAsync();

            _logger.LogInformation("Đã khôi phục {Count} lịch VMS kẹt ở pause-incident (biển không còn sự cố)", recoverIds.Count);
            return recoverIds.Count;
        }

        /// <summary>
        /// Dừng và xóa vòng lặp sự cố (schedule-incident) khi có lệnh Ghi đè (Override)
        /// hoặc khi kết thúc toàn bộ sự cố trên biển 
        /// </summary>
        public async Task StopIncidentScheduleByVmsIds(List<string> vmsIds)
        {
            if (vmsIds == null || vmsIds.Count == 0) return;

            var processes = await _vmsProcessRepository.AsQueryable()
                .Where(l => vmsIds.Contains(l.VmsId) && l.Type == VmsProcessTypeConst.ScheduleIncident)
                .ToListAsync();

            if (processes == null || processes.Count == 0) return;

            var processIds = processes.Select(p => p.ID).ToList();

            // Dọn các ảnh của vòng lặp
            await _vmsProcessImgRepository.AsDeleteable()
                .Where(img => processIds.Contains(img.ProcessId))
                .ExecuteCommandAsync();

            // Cập nhật State về stop
            await _vmsProcessRepository.AsUpdateable()
                .Where(l => processIds.Contains(l.ID))
                .SetColumns(l => l.State, VmsProcessStateConst.Stop)
                .ExecuteCommandAsync();

            StopThreadsRunSchedule(vmsIds.Select(IncidentThreadKey));
        }

        /// <summary>
        /// Chèn một hình ảnh sự cố vào vòng lặp phát luân phiên của biển VMS
        /// Author: ChuongLP
        /// Date: 2026-10-14
        /// </summary>
        public async Task AppendImageToIncidentProcess(string vmsId, string? incidentId, string url, int? ttl, int? priority,
            string sourceType,int brightness = 100)
        {
            if (string.IsNullOrEmpty(vmsId) || string.IsNullOrEmpty(url)) return;

            var appendLock = _appendLocks.GetOrAdd(vmsId, _ => new SemaphoreSlim(1, 1));
            await appendLock.WaitAsync();
            try
            {
                await AppendImageToIncidentProcessCore(vmsId, incidentId, url, ttl, priority, sourceType, brightness);
            }
            finally
            {
                appendLock.Release();
            }
        }

        private async Task AppendImageToIncidentProcessCore(string vmsId, string? incidentId, string url, int? ttl, int? priority,
            string sourceType, int brightness)
        {

            // 1. Tạm dừng lịch thường nếu đang chạy trên biển này
            await PauseScheduleByVmsIds(new List<string> { vmsId });

            // 2. Tìm hoặc tạo tiến trình schedule-incident cho thiết bị
            var incidentProcess = await _vmsProcessRepository.AsQueryable()
                .Where(p => p.VmsId == vmsId && p.Type == VmsProcessTypeConst.ScheduleIncident)
                .FirstAsync();

            if (incidentProcess == null)
            {
                incidentProcess = new VmsProcess
                {
                    VmsId = vmsId,
                    Type = VmsProcessTypeConst.ScheduleIncident,
                    State = VmsProcessStateConst.Run,
                    Brightness = brightness,
                    ThreadId = vmsId,
                    Priority = priority ?? 10
                };
                await _vmsProcessRepository.InsertAsync(incidentProcess);
            }
            else
            {
                // Giữ nguyên tiến trình, đảm bảo State = run
                if (incidentProcess.State != VmsProcessStateConst.Run)
                {
                    await _vmsProcessRepository.AsUpdateable()
                        .Where(p => p.ID == incidentProcess.ID)
                        .SetColumns(p => p.State, VmsProcessStateConst.Run)
                        .ExecuteCommandAsync();
                    incidentProcess.State = VmsProcessStateConst.Run;
                }
            }

            // 3. Đọc danh sách ảnh active hiện tại của tiến trình này
            var currentImages = await _vmsProcessImgRepository.AsQueryable()
                .Where(img => img.ProcessId == incidentProcess.ID && (img.IsActive == null || img.IsActive == true))
                .OrderBy(img => img.CreateTime)
                .ToListAsync();

            // 3.1 Chuyển Ghi đè -> Nối tiếp: playlist đang trống nhưng biển vẫn đang chiếu ảnh gửi thẳng của một sự cố
            // (Override). Đưa ảnh đó vào playlist trước để vòng phát là [ảnh sự cố cũ, ảnh mới] theo sơ đồ trạng thái.
            // Không làm vậy thì ảnh sự cố cũ biến mất, và khi sự cố mới kết thúc biển kẹt ảnh của sự cố đã đóng.
            if (currentImages.Count == 0)
            {
                var seeded = await SeedOverrideImageAsync(vmsId, incidentProcess.ID);
                if (seeded != null) currentImages.Add(seeded);
            }

            // 4. Chống trùng lặp nếu đã có ảnh cùng URL và IncidentId thì bỏ qua
            var isDuplicate = currentImages.Any(img => img.Url == url && (string.IsNullOrEmpty(incidentId) || img.IncidentId == incidentId));
            if (isDuplicate)
            {
                await EnsureScheduleThreadRunning(incidentProcess, currentImages);
                return;
            }

            // 5. Kiểm tra giới hạn playlist từ SysOpConfig
            int maxItems = await GetMaxIncidentPlaylistItemsAsync();
            if (currentImages.Count >= maxItems)
            {
                // ưu tiên loại ảnh có Priority thấp nhất (số lớn nhất), cùng mức thì loại ảnh cũ nhất.
                // Priority của ảnh lấy từ TmsIncidentControl theo IncidentId; ảnh không gắn sự cố coi như thấp nhất.
                var controls = await _vmsProcessService.GetIncidentControlsByEquipmentAsync(vmsId);
                var priorityByIncident = controls
                    .Where(c => !string.IsNullOrEmpty(c.IncidentId))
                    .GroupBy(c => c.IncidentId!)
                    .ToDictionary(g => g.Key, g => g.Min(c => c.Priority ?? int.MaxValue));
                int PriorityOf(VmsProcessImg img) =>
                    !string.IsNullOrEmpty(img.IncidentId) && priorityByIncident.TryGetValue(img.IncidentId, out var p) ? p : int.MaxValue;

                var evictCandidate = currentImages
                    .OrderByDescending(PriorityOf)
                    .ThenBy(img => img.CreateTime)
                    .FirstOrDefault();

                // Ảnh mới ưu tiên thấp hơn mọi ảnh đang phát (số Priority lớn hơn) -> từ chối append.
                var newPriority = priority ?? int.MaxValue;
                if (evictCandidate != null && newPriority > PriorityOf(evictCandidate))
                {
                    await EnsureScheduleThreadRunning(incidentProcess, currentImages);
                    return;
                }

                if (evictCandidate != null)
                {
                    await _vmsProcessImgRepository.AsDeleteable()
                        .Where(img => img.ID == evictCandidate.ID)
                        .ExecuteCommandAsync();

                    currentImages.Remove(evictCandidate);
                }
            }

            // 6. Thêm ảnh mới vào VmsProcessImg
            var finalTtl = (ttl.HasValue && ttl.Value > 0) ? ttl.Value : VmsSysOpConfigConst.DefaultTimeToLiveValue;
            var newImg = new VmsProcessImg
            {
                ProcessId = incidentProcess.ID,
                IncidentId = incidentId,
                Url = url,
                TimeToLive = finalTtl,
                SourceType = sourceType,
                IsActive = true
            };
            await _vmsProcessImgRepository.InsertAsync(newImg);
            currentImages.Add(newImg);

            // 7. Cập nhật quyền sở hữu trong TmsIncidentControl (nếu có IncidentId)
            if (!string.IsNullOrEmpty(incidentId))
            {
                try
                {
                    await _messageBus.InvokeAsync(new UpdateTmsIncidentControlInput
                    {
                        EquipmentId = vmsId,
                        IncidentId = incidentId,
                        State = VmsPlayModeConst.Append,
                        Priority = priority,
                        Url = url
                    });
                }
                catch (Exception)
                {
                }
            }

            // 8. Đảm bảo background thread đang chạy để luân phiên phát ảnh
            await EnsureScheduleThreadRunning(incidentProcess, currentImages);
        }

        /// <summary>
        /// Đưa ảnh đang chiếu (VmsCurrent) của sự cố đang Ghi đè biển vào playlist sự cố, đổi quyền giữ của sự cố đó sang append.
        /// Chỉ thực hiện khi thiết bị đang có sự cố còn xử lý giữ biển; biển rảnh / đang chiếu lịch thường thì bỏ qua.
        /// Author: ChuongLP
        /// Date: 2026-10-19
        /// </summary>
        private async Task<VmsProcessImg?> SeedOverrideImageAsync(string vmsId, string processId)
        {
            try
            {
                var owner = (await _vmsProcessService.GetIncidentControlsByEquipmentAsync(vmsId, onlyActiveIncident: true))
                    .OrderByDescending(c => c.UpdateTime ?? c.CreateTime)
                    .FirstOrDefault();
                if (owner == null) return null;

                var current = await _vmsProcessService.GetVmsCurrentByEquipment(vmsId);
                if (current == null || string.IsNullOrEmpty(current.Url)) return null;

                var seedImg = new VmsProcessImg
                {
                    ProcessId = processId,
                    IncidentId = owner.IncidentId,
                    Url = current.Url,
                    TimeToLive = VmsSysOpConfigConst.DefaultTimeToLiveValue,
                    SourceType = VmsProcessImgSourceTypeConst.IncidentOverrideSeed,
                    IsActive = true
                };
                await _vmsProcessImgRepository.InsertAsync(seedImg);

                await _messageBus.InvokeAsync(new UpdateTmsIncidentControlInput
                {
                    EquipmentId = vmsId,
                    IncidentId = owner.IncidentId,
                    State = VmsPlayModeConst.Append,
                    Priority = owner.Priority,
                    Url = current.Url
                });

                return seedImg;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Đọc giá trị MaxPlaylistItems từ SysOpConfig, nếu không có hoặc lỗi thì trả về mặc định
        /// Author: ChuongLP
        /// Date: 2026-10-14
        /// </summary>
        /// <returns></returns>
        private Task<int> GetMaxIncidentPlaylistItemsAsync() => ReadMaxIncidentPlaylistItemsAsync(_messageBus);

        /// <summary>
        /// Dùng chung cho API xem playlist (ProcessQueryHandler) để FE hiển thị đúng giới hạn đang cấu hình.
        /// </summary>
        public static async Task<int> ReadMaxIncidentPlaylistItemsAsync(IMessageBus messageBus)
        {
            try
            {
                var configs = await messageBus.InvokeAsync<List<SysOpConfigOutput>>(
                    new SysOpConfigInput { GroupCode = VmsSysOpConfigConst.GroupCode, Code = VmsSysOpConfigConst.MaxPlaylistItems });

                var val = configs?.FirstOrDefault(c => c.Code == VmsSysOpConfigConst.MaxPlaylistItems)?.Value;
                if (int.TryParse(val, out var max) && max > 0)
                    return max;
            }
            catch
            {
            }
            return VmsSysOpConfigConst.DefaultMaxPlaylistItemsValue;
        }

        /// <summary>
        /// Đảm bảo tiến trình lập lịch đang chạy (nếu chưa có hoặc đã dừng thì khởi động lại)
        /// Author: ChuongLP
        /// Date: 2026-10-14
        /// </summary>
        private async Task EnsureScheduleThreadRunning(VmsProcess incidentProcess, List<VmsProcessImg> images)
        {
            var threadKey = GetThreadKey(incidentProcess);
            if (!_taskDict.TryGetValue(threadKey, out var existingTask) || existingTask.IsCompleted)
            {
                // Dọn dẹp task cũ nếu có (đã kết thúc thật, vì task giờ là task của cả vòng lặp)
                _taskDict.TryRemove(threadKey, out _);
                if (_ctsDict.TryRemove(threadKey, out var oldCts))
                    oldCts.Cancel();

                await StartSchedule(new List<VmsProcess> { incidentProcess }, images);
            }
        }
    }
}