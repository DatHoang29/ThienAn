using System.Collections.Concurrent;
using ITS.VMS.Core.Integration.Helpers;
using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;
using Modules.Integration.VMS.Base.Core.Dtos;
using Modules.Integration.VMS.Base.Core.Helpers;
using Modules.Integration.VMS.Infrastructure.Services;
using Newtonsoft.Json;
using static Modules.Integration.VMS.Base.Core.Enums.IntgVmsEnums;

namespace ITS.VMS.Core.Integration
{
    /// <summary>
    /// Thực thi lệnh điều khiển ra MỘT thiết bị VMS.
    ///
    /// Trách nhiệm của lớp này CHỈ gồm phần việc của dịch vụ:
    ///   - lấy file ảnh (base64 trong gói tin / HTTP / thư mục dùng chung) qua VmsFileHelper
    ///   - giới hạn thời gian cho từng thiết bị
    ///   - đưa SDK đồng bộ ra thread nền
    ///   - chuẩn hoá kết quả về VmsTargetResult và dọn file tạm
    ///
    /// Toàn bộ xử lý ĐA HÃNG (chọn driver theo vendor, nạp DLL, Chainzone phải đăng ký/gửi qua cả Sdk lẫn Api,
    /// khác biệt khóa Metadata giữa các hãng, ánh xạ mã thông số) do <see cref="IntgVmsManager"/> của tầng
    /// Integration đảm nhiệm — DÙNG CHUNG với BE. Trước 22/09/2026 phần này bị chép sang đây vì
    /// IntgVmsManager còn phụ thuộc Furion; nay đã bỏ nên không còn lặp.
    ///
    /// Mỗi lần gọi tạo manager riêng rồi UnRegisterModule trong finally: SDK native giữ state toàn cục,
    /// dùng chung instance giữa các biển sẽ gửi sai dữ liệu.
    ///
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public sealed class VmsDeviceExecutor
    {
        private readonly VmsControlNatsOption _options;
        private readonly VmsLogger _logger;
        private readonly VmsFileHelper _fileHelper;

        public VmsDeviceExecutor(VmsControlNatsOption options, VmsLogger logger, VmsFileHelper fileHelper)
        {
            _options = options;
            _logger = logger;
            _fileHelper = fileHelper;
        }

        /// <summary>
        /// Thực thi lệnh của <paramref name="request"/> ra MỘT thiết bị. LUÔN trả về kết quả
        /// (không ném exception ra ngoài) để lỗi của một biển không làm gián đoạn cả mẻ.
        /// </summary>
        /// <param name="request">Gói lệnh. Action và ảnh dùng chung (ImageBase64/ImageFormat) lấy từ đây.</param>
        /// <param name="target">Thiết bị đích.</param>
        public async Task<VmsTargetResult> ExecuteAsync(VmsControlRequest request, VmsControlTarget target, CancellationToken ct)
        {
            var action = request.Action?.Trim().ToLowerInvariant() ?? VmsControlAction.SendContent;
            var result = new VmsTargetResult { EquipmentId = target.EquipmentId ?? string.Empty };

            // Khai ngoài try để khối finally xoá được file tạm và trả khóa thiết bị.
            string? imagePath = null;
            var imageIsTemp = false;
            SemaphoreSlim? gate = null;
            var waitingGate = false;
            Task<VmsTargetResult>? deviceTask = null;

            // Trần thời gian cho riêng thiết bị này (gồm cả thời gian chờ tới lượt): SDK treo không được giữ cả gói request.
            var timeoutSeconds = Math.Max(5, _options.DeviceTimeoutSeconds);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                // check-connect chỉ cần ping, không nạp driver.
                if (action == VmsControlAction.CheckConnect)
                {
                    var port = ResolvePort(target.Port);
                    var ok = await IntgVmsNetHelper.CheckConnectAsync(target.Ip, port, 5000, cts.Token).ConfigureAwait(false);
                    result.Success = ok;
                    result.Data = ok;
                    result.Message = ok ? "Thiết bị đang kết nối." : $"Không kết nối được {target.Ip}:{port}.";
                    return result;
                }

                // Các lệnh dùng SDK tới CÙNG một biển chạy tuần tự. Chờ tới lượt nằm trong cùng trần thời gian:
                // lệnh trước kéo dài thì lệnh này báo timeout, không gửi muộn sau khi BE đã coi là thất bại.
                var deviceGate = GetDeviceGate(target);
                waitingGate = true;
                await deviceGate.WaitAsync(cts.Token).ConfigureAwait(false);
                gate = deviceGate;
                waitingGate = false;

                // send-content cần lấy file ảnh TRƯỚC khi mở kết nối thiết bị.
                if (action == VmsControlAction.SendContent)
                {
                    // Ưu tiên ảnh NHÚNG trong gói tin: dịch vụ không phải đi tìm file, chạy được khi cài ở máy khác BE.
                    // Không có base64 thì mới quay về target.Url (HTTP tải về / ghép StorageBasePath).
                    (imagePath, imageIsTemp) = await _fileHelper
                        .ResolveAsync(request.ImageBase64, request.ImageFormat, target.Url, cts.Token)
                        .ConfigureAwait(false);
                }

                // Chạy trên thread nền: toàn bộ driver là API đồng bộ (blocking P/Invoke).
                // KHÔNG truyền token cho Task.Run và ghi kết quả vào đối tượng RIÊNG: SDK không hủy được giữa chừng,
                // khi timeout thread vẫn chạy tiếp tới UnRegisterModule; nó không được sửa result đã trả cho BE.
                // LongRunning = thread riêng cho mỗi biển: lệnh SDK chặn thread nhiều giây, nếu dùng thread pool thì khi
                // MaxParallelDevice lớn các biển phải chờ pool bơm thêm thread (~1-2 thread/giây) nên không gửi đồng loạt được.
                var deviceResult = new VmsTargetResult { EquipmentId = result.EquipmentId };
                var devicePath = imagePath;
                deviceTask = Task.Factory.StartNew(
                    () => RunDevice(action, target, devicePath, request.StatTypes, deviceResult),
                    CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                return await deviceTask.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Message = waitingGate
                    ? $"Thiết bị đang bận xử lý lệnh trước, quá {timeoutSeconds}s chưa tới lượt (timeout)."
                    : $"Thiết bị không phản hồi trong {timeoutSeconds}s (timeout).";
                _logger.Error(LogCategory.Device, $"[Device] {target.EquipmentId} · {action} · TIMEOUT" + (waitingGate ? " (chờ tới lượt)" : string.Empty));
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                _logger.Error(LogCategory.Device, $"[Device] {target.EquipmentId} · {action} · LỖI: {ex.Message}");
                return result;
            }
            finally
            {
                // CHỈ xoá file do dịch vụ tự dựng (giải mã base64 / tải HTTP) — đó là rác dùng một lần.
                // File lấy từ StorageBasePath là ảnh THẬT của BE, xoá là mất dữ liệu.
                if (deviceTask is { IsCompleted: false })
                {
                    // Timeout nhưng SDK vẫn đang chạy: giữ khóa thiết bị và file ảnh tới khi SDK xong hẳn,
                    // tránh lệnh kế tiếp mở kết nối chồng lên biển hoặc xoá file khi SDK còn đọc.
                    var pendingGate = gate;
                    var pendingPath = imagePath;
                    var pendingTemp = imageIsTemp;
                    var equipmentId = target.EquipmentId;
                    _ = deviceTask.ContinueWith(t =>
                    {
                        _ = t.Exception;   // quan sát lỗi để không thành UnobservedTaskException
                        _logger.Warning(LogCategory.Device, $"[Device] {equipmentId} · {action} · SDK kết thúc sau timeout, đã trả khóa thiết bị.");
                        if (pendingTemp) _fileHelper.DeleteTemp(pendingPath);
                        pendingGate?.Release();
                    }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
                else
                {
                    if (imageIsTemp) _fileHelper.DeleteTemp(imagePath);
                    gate?.Release();
                }
            }
        }

        /// <summary>
        /// Khóa theo thiết bị (theo IP, thiếu IP thì theo EquipmentId). Hai lệnh tới cùng một biển
        /// (vd lịch + sự cố, hoặc điều khiển + quét telemetry) chạy tuần tự để không mở 2 kết nối SDK chồng nhau;
        /// các biển khác nhau vẫn chạy song song theo MaxParallelDevice.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> DeviceGates = new(StringComparer.OrdinalIgnoreCase);

        private static SemaphoreSlim GetDeviceGate(VmsControlTarget target)
        {
            var key = !string.IsNullOrWhiteSpace(target.Ip) ? target.Ip.Trim() : (target.EquipmentId ?? string.Empty);
            return DeviceGates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Nạp driver qua IntgVmsManager rồi chạy lệnh. Chạy đồng bộ trong Task.Run.
        /// </summary>
        private VmsTargetResult RunDevice(string action, VmsControlTarget target, string? imagePath,
                                          string[]? statTypes, VmsTargetResult result)
        {
            var vendor = NormalizeVendor(target.Vendor);
            var port = ResolvePort(target.Port);
            var metadata = ParseConfig(target.Config);   // driver dùng Metadata[key] để lấy Width/Height/PortApi... nên KHÔNG được null.

            IntgVmsManager? manager = null;
            try
            {
                // Thư mục DLL driver: cạnh exe, hoặc Nats:VendorDllPath nếu tách riêng.
                manager = new IntgVmsManager(ResolveVendorDir());
                manager.RegisterModule(vendor);

                // InitConfig lo ping + đăng ký SDK/API theo từng hãng, trả về Message nêu rõ bước nào lỗi.
                var init = manager.InitConfig(new IntgVmsConfigureInput
                {
                    Vendor = vendor,
                    Ip = target.Ip,
                    Port = port,
                    Metadata = metadata,
                });

                if (!init.Succeeded)
                {
                    result.Success = false;
                    result.Message = init.Message ?? $"Không khởi tạo được kết nối {target.Ip}:{port}.";
                    _logger.Error(LogCategory.Device, $"[Device] {target.EquipmentId} · {action} · {result.Message}");
                    return result;
                }

                switch (action)
                {
                    case VmsControlAction.SendContent:
                        return SendContent(manager, vendor, target, imagePath!, metadata, result);

                    case VmsControlAction.SetBrightness:
                        result.SetBrightnessOk = ToBool(manager.SetBrightness(new IntgVmsSetBrightnessInput
                        {
                            Vendor = vendor,
                            Brightness = target.Brightness,
                            Metadata = metadata,
                        }));
                        result.Success = result.SetBrightnessOk;
                        result.Message = result.Success ? "Đặt độ sáng thành công." : "Đặt độ sáng thất bại.";
                        return result;

                    case VmsControlAction.ReadImage:
                        return ReadImage(manager, vendor, target, metadata, result);

                    case VmsControlAction.GetStat:
                        return GetStat(manager, vendor, statTypes, metadata, result);

                    default:
                        result.Success = false;
                        result.Message = $"Lệnh không hỗ trợ: '{action}'.";
                        return result;
                }
            }
            finally
            {
                // SDK giữ handle native -> bắt buộc ngắt kết nối, kể cả khi lỗi.
                try { manager?.UnRegisterModule(); } catch { /* bỏ qua lỗi dọn dẹp */ }
            }
        }

        #region Các lệnh cụ thể

        /// <summary>Gửi ảnh rồi đặt độ sáng. Việc gửi qua Sdk/Api theo hãng do IntgVmsManager lo.</summary>
        private VmsTargetResult SendContent(IntgVmsManager manager, string vendor, VmsControlTarget target,
                                            string imagePath, Dictionary<string, object> metadata, VmsTargetResult result)
        {
            result.SendImageOk = ToBool(manager.SendImage(new IntgVmsSendImageInput
            {
                Vendor = vendor,
                Url = imagePath,
                Metadata = metadata,
            }));

            // Độ sáng là bước phụ: thất bại không làm cả lệnh thất bại (giữ đúng hành vi BE cũ,
            // BE cũ chỉ xét kết quả SendImage để quyết định success/fail).
            try
            {
                result.SetBrightnessOk = ToBool(manager.SetBrightness(new IntgVmsSetBrightnessInput
                {
                    Vendor = vendor,
                    Brightness = target.Brightness,
                    Metadata = metadata,
                }));
            }
            catch (Exception ex)
            {
                result.SetBrightnessOk = false;
                _logger.Warning(LogCategory.Device, $"[Device] {target.EquipmentId} · đặt độ sáng lỗi: {ex.Message}");
            }

            result.Success = result.SendImageOk;
            result.Message = result.Success
                ? $"Gửi thành công ra thiết bị {vendor}" + (result.SetBrightnessOk ? "." : " (đặt độ sáng thất bại).")
                : "Gửi ảnh thất bại.";

            _logger.Log(LogCategory.Device, result.Success ? "Success" : "Error",
                $"[Device] {target.EquipmentId} · send-content · {vendor} · ảnh={Path.GetFileName(imagePath)} · {result.Message}");

            return result;
        }

        /// <summary>Đọc ảnh đang hiển thị, trả Base64 vào Data.</summary>
        private VmsTargetResult ReadImage(IntgVmsManager manager, string vendor, VmsControlTarget target,
                                          Dictionary<string, object> metadata, VmsTargetResult result)
        {
            // Chainzone cần đường dẫn file tạm để SDK ghi ảnh từ bảng xuống rồi mới đọc lên Base64.
            var tempPath = BuildReadImagePath(target.EquipmentId);
            try
            {
                var output = manager.ReadImage(new IntgVmsReadImageInput
                {
                    Vendor = vendor,
                    Metadata = metadata,
                    Url = tempPath,
                }) as IntgVmsReadImageOutput;

                result.Success = output?.Succeeded == true && !string.IsNullOrEmpty(output.Base64);
                result.Data = output?.Base64;
                result.Message = result.Success ? "Đọc ảnh thành công." : (output?.Message ?? "Đọc ảnh thất bại.");

                _logger.Log(LogCategory.Device, result.Success ? "Success" : "Error",
                    $"[Device] {target.EquipmentId} · read-image · {vendor} · {result.Message}");

                return result;
            }
            finally
            {
                // File này do SDK ghi ra, chỉ dùng để lấy Base64 -> xoá ngay, tránh phình đĩa
                // khi job telemetry chạy theo chu kỳ. (Bản BE cũ cũng xoá ở finally.)
                _fileHelper.DeleteTemp(tempPath);
            }
        }

        /// <summary>Đọc thông số kỹ thuật, trả Dictionary vào Data.</summary>
        private VmsTargetResult GetStat(IntgVmsManager manager, string vendor, string[]? statTypes,
                                        Dictionary<string, object> metadata, VmsTargetResult result)
        {
            // BE gửi danh sách thông số cần đọc; không gửi thì đọc tất cả.
            var types = statTypes is { Length: > 0 } ? statTypes : new[] { IntgVmsStatType.All };

            // IntgVmsManager tự ánh xạ mã thông số riêng của hãng về khóa chung (temperatureIn, powerStatus...).
            var output = manager.GetStats(new IntgVmsGetStatInput
            {
                Vendor = vendor,
                Metadata = metadata,
                Type = types,
            }) as IntgVmsGetStatOutput;

            result.Success = output?.Succeeded == true && output.Value != null;
            result.Data = output?.Value;
            result.Message = result.Success ? "Đọc thông số thành công." : (output?.Message ?? "Đọc thông số thất bại.");
            return result;
        }

        #endregion

        #region Tiện ích

        /// <summary>
        /// IntgVmsManager khai kiểu trả về là object? (bool bị boxing) nên phải quy đổi an toàn.
        /// Giá trị lạ coi như thất bại thay vì ném InvalidCastException.
        /// </summary>
        private static bool ToBool(object? value) => value switch
        {
            bool b => b,
            IntgVmsBaseOutput output => output.Succeeded,
            _ => false,
        };

        /// <summary>Chuẩn hóa tên hãng về đúng hằng IntgVmsVendor (DB có thể lưu khác hoa/thường).</summary>
        private static string NormalizeVendor(string? vendor) => vendor?.Trim().ToLowerInvariant() ?? string.Empty;

        /// <summary>Port null/0 -> 80 (đồng bộ mặc định BE đang dùng).</summary>
        private static int ResolvePort(int? port) => port is > 0 ? port.Value : 80;

        /// <summary>Đổi Config (JSON) sang Metadata. Luôn trả dictionary khác null vì driver gán trực tiếp Metadata[key].</summary>
        private static Dictionary<string, object> ParseConfig(string? config)
        {
            if (string.IsNullOrWhiteSpace(config)) return new Dictionary<string, object>();

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(config) ?? new Dictionary<string, object>();
            }
            catch
            {
                // Config sai định dạng -> để driver tự báo thiếu Width/Height/PortApi, thông báo sẽ rõ hơn.
                return new Dictionary<string, object>();
            }
        }

        /// <summary>Thư mục chứa DLL driver: cạnh exe, hoặc Nats:VendorDllPath nếu tách riêng.</summary>
        private string ResolveVendorDir()
        {
            var path = _options.VendorDllPath;
            if (string.IsNullOrWhiteSpace(path)) return AppContext.BaseDirectory;
            return Path.IsPathRooted(path) ? path.Trim() : Path.Combine(AppContext.BaseDirectory, path.Trim());
        }

        /// <summary>File tạm để SDK ghi ảnh đọc từ bảng xuống trước khi chuyển thành Base64.</summary>
        private static string BuildReadImagePath(string? equipmentId)
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "temp", "vms_read");
            Directory.CreateDirectory(dir);
            var name = $"{equipmentId ?? "unknown"}_{DateTime.Now:yyyyMMddHHmmssfff}.bmp";
            return Path.Combine(dir, string.Join("_", name.Split(Path.GetInvalidFileNameChars())));
        }

        #endregion
    }
}
