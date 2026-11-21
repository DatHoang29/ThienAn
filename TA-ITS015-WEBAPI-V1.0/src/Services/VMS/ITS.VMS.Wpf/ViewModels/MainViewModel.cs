using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;

namespace ITS.VMS.Wpf.ViewModels;

/// <summary>
/// Điều phối form theo dõi/kiểm thử luồng điều khiển VMS.
///
/// Form mô phỏng ĐỦ 2 đầu của luồng nên test được trọn vẹn khi chưa dựng Worker:
///   - Vai DỊCH VỤ  (ServiceRunning)   : VmsControlNatsListener nhận Type=request -> gọi SDK -> phát Type=result.
///   - Vai GỬI LỆNH (SenderConnected)  : VmsControlSender phát Type=request -> chờ Type=result theo RequestId.
///
/// 3 cách dùng:
///   1. Bật CẢ 2 vai  -> thử vòng kín ngay trên máy (không cần BE, không cần Worker).
///   2. Chỉ bật vai DỊCH VỤ -> form đứng thay Worker, để BE thật phát lệnh xuống.
///   3. Chỉ bật vai GỬI LỆNH -> form đứng thay BE, để Worker thật xử lý.
///
/// Author: ChuongLP
/// Created date: 22/09/2026
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private const int MaxLogRows = 500;
    private const int MaxPacketRows = 100;

    /// <summary>Tên file lưu danh sách thiết bị test cạnh exe.</summary>
    private const string TargetsFileName = "VmsTestTargets.json";

    private readonly VmsControlNatsOption _options;
    private readonly VmsControlNatsListener _listener;
    private readonly VmsControlSender _sender;
    private readonly VmsLogger _logger;

    public MainViewModel(VmsControlNatsOption options, VmsControlNatsListener listener,
                         VmsControlSender sender, VmsLogger logger)
    {
        _options = options;
        _listener = listener;
        _sender = sender;
        _logger = logger;

        NatsUrl = options.Url;
        Subject = string.IsNullOrWhiteSpace(options.ControlSubject) ? "ta.its.control.vms" : options.ControlSubject;
        AuthMode = options.AuthMode;
        DeviceTimeoutSeconds = options.DeviceTimeoutSeconds;
        StorageBasePath = options.StorageBasePath ?? string.Empty;

        // Log của Core (kết nối NATS, nạp driver, gửi thiết bị...) hiện thẳng lên Nhật ký form.
        _logger.EntryLogged += OnCoreLog;

        // Mọi gói tin đi qua kênh đều hiện nguyên văn để đối chiếu hợp đồng bản tin.
        _sender.PacketObserved += OnPacketObserved;

        LoadTargets();
        UpdateStatus();
    }

    #region Thuộc tính hiển thị

    /// <summary>Địa chỉ NATS (chỉ đọc trên form — sửa trong appsettings.json rồi mở lại).</summary>
    [ObservableProperty] private string _natsUrl = string.Empty;

    [ObservableProperty] private string _subject = string.Empty;

    [ObservableProperty] private string _authMode = string.Empty;

    /// <summary>Trần thời gian mỗi thiết bị ở vai dịch vụ.</summary>
    [ObservableProperty] private int _deviceTimeoutSeconds;

    /// <summary>Thư mục gốc map đường dẫn ảnh tương đối (Nats:StorageBasePath).</summary>
    [ObservableProperty] private string _storageBasePath = string.Empty;

    /// <summary>Vai DỊCH VỤ đang chạy (đang lắng nghe request).</summary>
    [ObservableProperty] private bool _serviceRunning;

    /// <summary>Vai GỬI LỆNH đã kết nối.</summary>
    [ObservableProperty] private bool _senderConnected;

    /// <summary>Dòng trạng thái tổng hợp trên thanh tiêu đề.</summary>
    [ObservableProperty] private string _statusText = string.Empty;

    /// <summary>Đang chờ kết quả một lệnh vừa gửi.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    private bool _isSending;

    /// <summary>Cho phép bấm GỬI LỆNH: chặn bấm chồng khi đang chờ kết quả.</summary>
    public bool CanSend => !IsSending;

    // ===== Tham số lệnh gửi =====

    /// <summary>Danh sách lệnh chọn trên combobox.</summary>
    public string[] Actions { get; } =
    {
        VmsControlAction.SendContent,
        VmsControlAction.ReadImage,
        VmsControlAction.GetStat,
        VmsControlAction.SetBrightness,
        VmsControlAction.CheckConnect,
    };

    [ObservableProperty] private string _selectedAction = VmsControlAction.SendContent;

    /// <summary>Bật = dịch vụ trả thành công giả lập, không gọi thiết bị thật.</summary>
    [ObservableProperty] private bool _simulate = true;

    /// <summary>
    /// Bật = đọc file ảnh của thiết bị đầu tiên rồi nhúng base64 vào gói tin (mô phỏng đúng cách BE làm).
    /// Tắt = chỉ gửi url, để dịch vụ tự lấy ảnh — dùng để so sánh 2 đường.
    /// </summary>
    [ObservableProperty] private bool _attachImageBase64 = true;

    /// <summary>Trần payload NATS server báo về sau khi kết nối, để biết nhúng base64 có an toàn không.</summary>
    [ObservableProperty] private string _maxPayloadText = "Trần payload: chưa kết nối";

    /// <summary>Số giây form chờ gói result. 0 = tự tính theo DeviceTimeoutSeconds + 5.</summary>
    [ObservableProperty] private int _waitSeconds;

    /// <summary>RequestId của lệnh gần nhất, để đối chiếu trong nhật ký.</summary>
    [ObservableProperty] private string _lastRequestId = string.Empty;

    /// <summary>Tóm tắt kết quả lệnh gần nhất.</summary>
    [ObservableProperty] private string _lastResultSummary = string.Empty;

    /// <summary>JSON của gói tin đang chọn trong danh sách gói tin.</summary>
    [ObservableProperty] private PacketItem? _selectedPacket;

    [ObservableProperty] private TargetItemViewModel? _selectedTarget;

    public ObservableCollection<TargetItemViewModel> Targets { get; } = new();

    public ObservableCollection<TargetResultItem> Results { get; } = new();

    public ObservableCollection<PacketItem> Packets { get; } = new();

    public ObservableCollection<LogItem> Logs { get; } = new();

    #endregion

    #region Bật/tắt 2 vai

    /// <summary>Bật/tắt vai DỊCH VỤ (thay ITS.VMS.Worker).</summary>
    [RelayCommand]
    private async Task ToggleServiceAsync()
    {
        try
        {
            if (ServiceRunning)
            {
                await _listener.StopAsync();
                ServiceRunning = false;
                AddLog("Info", "[Form] Đã TẮT vai DỊCH VỤ.");
            }
            else
            {
                // Listener tự bỏ qua khi Nats:Enabled = false -> báo rõ để đỡ ngồi đợi.
                if (!_options.Enabled)
                {
                    AddLog("Warning", "[Form] Nats:Enabled = false trong appsettings.json nên không lắng nghe được. Sửa lại rồi mở lại form.");
                    return;
                }

                _listener.Start();
                ServiceRunning = true;
                AddLog("Success", $"[Form] Đã BẬT vai DỊCH VỤ trên kênh '{Subject}'.");
            }
        }
        catch (Exception ex)
        {
            AddLog("Error", $"[Form] Bật/tắt vai DỊCH VỤ lỗi: {ex.Message}");
        }
        finally
        {
            UpdateStatus();
        }
    }

    /// <summary>Kết nối/ngắt vai GỬI LỆNH (thay VmsControlBridge của BE).</summary>
    [RelayCommand]
    private async Task ToggleSenderAsync()
    {
        try
        {
            if (SenderConnected)
            {
                await _sender.StopAsync();
                SenderConnected = false;
            }
            else
            {
                await _sender.StartAsync();
                SenderConnected = _sender.IsConnected;

                MaxPayloadText = _sender.ServerMaxPayload > 0
                    ? $"Trần payload: {_sender.ServerMaxPayload / 1024} KB"
                    : "Trần payload: server không báo";
            }
        }
        catch (Exception ex)
        {
            SenderConnected = false;
            AddLog("Error", $"[Form] Kết nối gửi lệnh lỗi: {ex.Message}");
        }
        finally
        {
            UpdateStatus();
        }
    }

    #endregion

    #region Gửi lệnh

    /// <summary>Phát gói lệnh cho các thiết bị đang bật và chờ kết quả.</summary>
    [RelayCommand]
    private async Task SendAsync()
    {
        if (IsSending) return;

        var selected = Targets.Where(t => t.Enabled).ToList();
        if (selected.Count == 0)
        {
            AddLog("Warning", "[Form] Chưa chọn thiết bị nào (tick cột 'Bật').");
            return;
        }

        if (!SenderConnected)
        {
            // Tự kết nối cho đỡ một bước thao tác.
            await ToggleSenderAsync();
            if (!SenderConnected) return;
        }

        // Nhắc khi gửi thật: lệnh sẽ đi tới thiết bị ngoài hiện trường.
        if (!Simulate)
        {
            var confirm = MessageBox.Show(
                $"Đang TẮT giả lập: lệnh '{SelectedAction}' sẽ được gửi THẬT tới {selected.Count} thiết bị.\n\nTiếp tục?",
                "VMS Control Test", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        var request = new VmsControlRequest
        {
            Action = SelectedAction,
            Simulate = Simulate,
            Targets = selected.Select(t => t.ToTarget()).ToList(),
        };

        // Nhúng ảnh giống cách BE làm, để test đúng nhánh base64 (dịch vụ không đi tìm file).
        if (AttachImageBase64 && SelectedAction == VmsControlAction.SendContent)
            TryAttachImage(request, selected[0].Url);

        try
        {
            IsSending = true;
            Results.Clear();
            LastResultSummary = "Đang chờ kết quả...";

            var result = await _sender.SendAsync(request, WaitSeconds);
            LastRequestId = request.RequestId ?? string.Empty;

            if (result == null)
            {
                LastResultSummary = "KHÔNG CÓ PHẢN HỒI (timeout) — kiểm tra vai DỊCH VỤ hoặc Worker đã bật chưa.";
                foreach (var t in selected)
                {
                    t.LastState = "TIMEOUT";
                    t.LastMessage = "Không có phản hồi";
                    t.LastAt = DateTime.Now.ToString("HH:mm:ss");
                }
                return;
            }

            ApplyResult(result, selected);
        }
        catch (Exception ex)
        {
            LastResultSummary = $"LỖI: {ex.Message}";
            AddLog("Error", $"[Form] Gửi lệnh lỗi: {ex.Message}");
        }
        finally
        {
            IsSending = false;
        }
    }

    /// <summary>
    /// Đọc file ảnh và nhúng base64 vào gói tin — mô phỏng đúng ManageSendToDevice.TryAttachImage của BE.
    /// Đường dẫn: tuyệt đối dùng luôn, tương đối thì ghép Nats:StorageBasePath.
    /// Lỗi thì bỏ qua, gói vẫn đi bằng url (đúng hành vi BE).
    /// </summary>
    private void TryAttachImage(VmsControlRequest request, string? url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            var path = Path.IsPathRooted(url)
                ? url
                : Path.Combine(StorageBasePath ?? string.Empty, url.TrimStart('\\', '/'));

            if (!File.Exists(path))
            {
                AddLog("Warning", $"[Form] Không thấy file ảnh '{path}' -> gửi bằng url, dịch vụ sẽ tự lấy ảnh.");
                return;
            }

            var bytes = File.ReadAllBytes(path);
            request.ImageBase64 = Convert.ToBase64String(bytes);
            request.ImageFormat = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

            AddLog("Info", $"[Form] Đã nhúng ảnh {Path.GetFileName(path)} · file {bytes.Length / 1024} KB · base64 {request.ImageBase64.Length / 1024} KB.");
        }
        catch (Exception ex)
        {
            AddLog("Warning", $"[Form] Không nhúng được ảnh, gửi bằng url: {ex.Message}");
        }
    }

    /// <summary>Đổ kết quả về lưới kết quả và cột trạng thái của từng thiết bị.</summary>
    private void ApplyResult(VmsControlResult result, List<TargetItemViewModel> sent)
    {
        LastResultSummary = $"{(result.Success ? "THÀNH CÔNG" : "CÓ LỖI")} · {result.Message} · lúc {result.FinishedAt:HH:mm:ss}";

        foreach (var one in result.Results)
        {
            Results.Add(new TargetResultItem
            {
                EquipmentId = one.EquipmentId,
                Success = one.Success,
                SendImage = FormatFlag(result.Action, VmsControlAction.SendContent, one.SendImageOk),
                SetBrightness = FormatFlag(result.Action, VmsControlAction.SendContent, one.SetBrightnessOk,
                                           VmsControlAction.SetBrightness),
                Message = one.Message,
                DataPreview = FormatData(one.Data),
            });

            var target = sent.FirstOrDefault(t => t.EquipmentId == one.EquipmentId);
            if (target != null)
            {
                target.LastState = one.Success ? "OK" : "LỖI";
                target.LastMessage = one.Message;
                target.LastAt = DateTime.Now.ToString("HH:mm:ss");
            }
        }

        // Thiết bị có trong lệnh nhưng dịch vụ không trả kết quả -> đánh dấu rõ, đừng để trống gây hiểu nhầm.
        foreach (var target in sent.Where(t => result.Results.All(r => r.EquipmentId != t.EquipmentId)))
        {
            target.LastState = "THIẾU KQ";
            target.LastMessage = "Dịch vụ không trả kết quả cho thiết bị này";
            target.LastAt = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    /// <summary>Chỉ hiện cờ OK/lỗi với những lệnh thực sự dùng cờ đó, còn lại để trống cho dễ đọc.</summary>
    private static string FormatFlag(string? action, string appliesTo, bool value, string? alsoAppliesTo = null)
    {
        var match = string.Equals(action, appliesTo, StringComparison.OrdinalIgnoreCase)
                    || (alsoAppliesTo != null && string.Equals(action, alsoAppliesTo, StringComparison.OrdinalIgnoreCase));
        return match ? (value ? "OK" : "lỗi") : string.Empty;
    }

    /// <summary>Cắt ngắn Data để lưới không bị kéo dài bởi chuỗi Base64 của ảnh.</summary>
    private static string FormatData(object? data)
    {
        if (data == null) return string.Empty;

        var text = data is string s ? s : JsonSerializer.Serialize(data);
        return text.Length <= 120 ? text : text.Substring(0, 120) + $"... (tổng {text.Length} ký tự)";
    }

    #endregion

    #region Danh sách thiết bị test

    [RelayCommand]
    private void AddTarget()
    {
        Targets.Add(new TargetItemViewModel
        {
            EquipmentId = $"EQ-VMS-{Targets.Count + 1:00}",
            Ip = "192.168.1.100",
        });
    }

    [RelayCommand]
    private void RemoveTarget()
    {
        if (SelectedTarget == null) return;
        Targets.Remove(SelectedTarget);
        SelectedTarget = null;
    }

    /// <summary>Lưu danh sách thiết bị test ra file cạnh exe để lần sau mở lại là có ngay.</summary>
    [RelayCommand]
    private void SaveTargets()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, TargetsFileName);
            var data = Targets.Select(t => new
            {
                t.Enabled, t.EquipmentId, t.Vendor, t.Ip, t.Port, t.Config, t.Url, t.Brightness,
            }).ToList();

            File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            AddLog("Success", $"[Form] Đã lưu {data.Count} thiết bị test vào {TargetsFileName}.");
        }
        catch (Exception ex)
        {
            AddLog("Error", $"[Form] Lưu danh sách thiết bị lỗi: {ex.Message}");
        }
    }

    /// <summary>Nạp danh sách thiết bị test từ file; chưa có file thì tạo sẵn 1 dòng mẫu.</summary>
    private void LoadTargets()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, TargetsFileName);
            if (File.Exists(path))
            {
                var items = JsonSerializer.Deserialize<List<TargetItemViewModel>>(
                    File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (items != null)
                {
                    foreach (var item in items) Targets.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            AddLog("Warning", $"[Form] Không nạp được {TargetsFileName}: {ex.Message}");
        }

        if (Targets.Count == 0) AddTarget();
    }

    #endregion

    #region Nhật ký & gói tin

    [RelayCommand]
    private void ClearLog() => Logs.Clear();

    [RelayCommand]
    private void ClearPackets()
    {
        Packets.Clear();
        SelectedPacket = null;
    }

    /// <summary>Nhận log từ Core (chạy ở thread nền) -> đẩy về UI thread.</summary>
    private void OnCoreLog(VmsLogEntry entry)
    {
        AddLog(entry.Level, entry.Message, entry.Timestamp);
    }

    /// <summary>Nhận gói tin quan sát được -> hiện nguyên văn (JSON đã format cho dễ đọc).</summary>
    private void OnPacketObserved(string type, string json)
    {
        RunOnUi(() =>
        {
            Packets.Insert(0, new PacketItem { Type = type, Json = PrettyJson(json) });
            while (Packets.Count > MaxPacketRows) Packets.RemoveAt(Packets.Count - 1);

            // Tự chọn gói mới nhất để xem ngay, trừ khi người dùng đang xem một gói cũ.
            if (SelectedPacket == null) SelectedPacket = Packets[0];
        });
    }

    private void AddLog(string level, string message, DateTime? at = null)
    {
        RunOnUi(() =>
        {
            Logs.Insert(0, new LogItem { Level = level, Message = message, Timestamp = at ?? DateTime.Now });
            while (Logs.Count > MaxLogRows) Logs.RemoveAt(Logs.Count - 1);
        });
    }

    private void UpdateStatus()
    {
        RunOnUi(() =>
        {
            var service = ServiceRunning ? "DỊCH VỤ: đang chạy" : "DỊCH VỤ: tắt";
            var sender = SenderConnected ? "GỬI LỆNH: đã kết nối" : "GỬI LỆNH: chưa kết nối";
            StatusText = $"{service}   |   {sender}   |   Kênh: {Subject}   |   NATS: {NatsUrl}";
        });
    }

    /// <summary>Format JSON cho dễ đọc; gói lỗi định dạng thì giữ nguyên văn.</summary>
    private static string PrettyJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    /// <summary>Đẩy thao tác về UI thread (Core bắn sự kiện từ thread nền của NATS).</summary>
    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }

    #endregion
}
