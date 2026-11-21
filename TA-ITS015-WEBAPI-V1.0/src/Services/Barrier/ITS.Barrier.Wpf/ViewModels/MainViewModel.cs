using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITS.Barrier.Core.Logging;
using ITS.Barrier.Core.Messaging;
using Modules.Integration.Barrier;
using Modules.Integration.Barrier.Base.Enums;
using Modules.Integration.Barrier.Base.Helpers;

namespace ITS.Barrier.Wpf.ViewModels;

/// <summary>
/// ViewModel chính cho giao diện kiểm thử Barrier, hỗ trợ:
/// 1. Kiểm thử thiết bị trực tiếp qua PLC Siemens S7-1200.
/// 2. Chế độ Giả lập (Simulate Mode) chạy không cần thiết bị thật.
/// 3. Kiểm thử kênh NATS 2 chiều (phát Request -> nhận Result đối chiếu từ Worker).
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private const string ConfigFileName = "BarrierTestTargets.json";
    private readonly IntgBarrierManager _barrierManager;
    private BarrierControlSender? _natsSender;
    private readonly BarrierLogger _logger;

    [ObservableProperty]
    private ObservableCollection<BarrierTargetItemViewModel> _targets = new();

    [ObservableProperty]
    private BarrierTargetItemViewModel? _selectedTarget;

    [ObservableProperty]
    private ObservableCollection<string> _logs = new();

    [ObservableProperty]
    private ObservableCollection<string> _packets = new();

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Chế độ Giả lập (Simulate Mode): Bật = mô phỏng ngay trên RAM không gọi socket phần cứng.
    /// </summary>
    [ObservableProperty]
    private bool _simulate = true;

    /// <summary>Địa chỉ NATS server để test.</summary>
    [ObservableProperty]
    private string _natsUrl = "nats://127.0.0.1:4222";

    /// <summary>Kênh điều khiển NATS.</summary>
    [ObservableProperty]
    private string _controlSubject = "ta.its.control.barrier";

    [ObservableProperty]
    private bool _isNatsConnected;

    public MainViewModel()
    {
        _barrierManager = new IntgBarrierManager();
        _logger = new BarrierLogger();
        _logger.LogOccurred += msg => Log(msg);

        LoadTargets();
    }

    [RelayCommand]
    private async Task PingAsync()
    {
        if (SelectedTarget is null)
        {
            MessageBox.Show("Vui lòng chọn 1 barrier từ danh sách!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        Log($"[PING] Bắt đầu kiểm tra tới {SelectedTarget.IpAddress}...");

        if (Simulate)
        {
            await Task.Delay(200);
            SelectedTarget.IsConnected = true;
            SelectedTarget.LastResult = "[GIẢ LẬP] Ping thành công (ICMP OK)";
            Log($"[PING] {SelectedTarget.LastResult}");
            IsBusy = false;
            return;
        }

        try
        {
            var ok = await IntgBarrierNetHelper.PingAsync(SelectedTarget.IpAddress, 2000);
            SelectedTarget.IsConnected = ok;
            SelectedTarget.LastResult = ok ? "Ping OK (ICMP Success)" : "Ping thất bại (Timeout/Host Unreachable)";
            Log($"[PING] Kết quả: {SelectedTarget.LastResult}");
        }
        catch (Exception ex)
        {
            SelectedTarget.LastResult = $"Lỗi: {ex.Message}";
            Log($"[PING] Ngoại lệ: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CheckConnectAsync()
    {
        if (SelectedTarget is null) return;

        IsBusy = true;
        Log($"[CHECK CONNECT] Kiểm tra cổng S7 {SelectedTarget.IpAddress}:{SelectedTarget.Port}...");

        if (Simulate)
        {
            await Task.Delay(250);
            SelectedTarget.IsConnected = true;
            SelectedTarget.LastResult = "[GIẢ LẬP] Bắt tay PLC S7-1200 thành công (Port 102 OK)";
            Log($"[CHECK CONNECT] {SelectedTarget.LastResult}");
            IsBusy = false;
            return;
        }

        try
        {
            var config = SelectedTarget.ToConfig();
            var ok = await _barrierManager.CheckConnectionAsync(config);
            SelectedTarget.IsConnected = ok;
            SelectedTarget.LastResult = ok ? "Kết nối PLC S7 thành công" : "Không thể bắt tay PLC S7";
            Log($"[CHECK CONNECT] {SelectedTarget.LastResult}");
        }
        catch (Exception ex)
        {
            SelectedTarget.LastResult = $"Lỗi: {ex.Message}";
            Log($"[CHECK CONNECT] Ngoại lệ: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenBarrierAsync()
    {
        if (SelectedTarget is null) return;

        IsBusy = true;
        Log($"[LỆNH MỞ] Kích xung MỞ tới {SelectedTarget.Code} ({SelectedTarget.IpAddress} - bit {SelectedTarget.OpenBit})...");

        if (Simulate)
        {
            SelectedTarget.CurrentStatus = BarrierStatusType.Moving;
            Log($"[GIẢ LẬP] Đang kích relay mở... Barie đang nâng cần lên.");
            await Task.Delay(600);
            SelectedTarget.CurrentStatus = BarrierStatusType.Opened;
            SelectedTarget.LastResult = "[GIẢ LẬP] MỞ BARIE THÀNH CÔNG (Cần đã mở 90°)";
            Log($"[LỆNH MỞ] {SelectedTarget.LastResult}");
            IsBusy = false;
            return;
        }

        try
        {
            var config = SelectedTarget.ToConfig();
            var res = await _barrierManager.OpenAsync(config);
            SelectedTarget.LastResult = res.Success ? $"MỞ THÀNH CÔNG ({res.DurationMs}ms)" : $"MỞ THẤT BẠI: {res.Message}";
            if (res.Success) SelectedTarget.CurrentStatus = BarrierStatusType.Opened;
            Log($"[LỆNH MỞ] Kết quả: {SelectedTarget.LastResult}");
        }
        catch (Exception ex)
        {
            SelectedTarget.LastResult = $"Lỗi: {ex.Message}";
            Log($"[LỆNH MỞ] Ngoại lệ: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CloseBarrierAsync()
    {
        if (SelectedTarget is null) return;

        IsBusy = true;
        Log($"[LỆNH ĐÓNG] Kích xung ĐÓNG tới {SelectedTarget.Code} ({SelectedTarget.IpAddress} - bit {SelectedTarget.CloseBit})...");

        if (Simulate)
        {
            SelectedTarget.CurrentStatus = BarrierStatusType.Moving;
            Log($"[GIẢ LẬP] Đang kích relay đóng... Barie đang hạ cần xuống.");
            await Task.Delay(600);
            SelectedTarget.CurrentStatus = BarrierStatusType.Closed;
            SelectedTarget.LastResult = "[GIẢ LẬP] ĐÓNG BARIE THÀNH CÔNG (Cần đã đóng ngang)";
            Log($"[LỆNH ĐÓNG] {SelectedTarget.LastResult}");
            IsBusy = false;
            return;
        }

        try
        {
            var config = SelectedTarget.ToConfig();
            var res = await _barrierManager.CloseAsync(config);
            SelectedTarget.LastResult = res.Success ? $"ĐÓNG THÀNH CÔNG ({res.DurationMs}ms)" : $"ĐÓNG THẤT BẠI: {res.Message}";
            if (res.Success) SelectedTarget.CurrentStatus = BarrierStatusType.Closed;
            Log($"[LỆNH ĐÓNG] Kết quả: {SelectedTarget.LastResult}");
        }
        catch (Exception ex)
        {
            SelectedTarget.LastResult = $"Lỗi: {ex.Message}";
            Log($"[LỆNH ĐÓNG] Ngoại lệ: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReadStatusAsync()
    {
        if (SelectedTarget is null) return;

        IsBusy = true;
        Log($"[ĐỌC TRẠNG THÁI] Đọc từ PLC {SelectedTarget.IpAddress} (StatusBit: {SelectedTarget.StatusBit}, LoopBit: {SelectedTarget.LoopBarrierBit})...");

        if (Simulate)
        {
            await Task.Delay(200);
            SelectedTarget.LastResult = $"[GIẢ LẬP] Trạng thái: {SelectedTarget.CurrentStatus} | Vòng từ: {(SelectedTarget.HasVehicleOnLoop ? "CÓ XE ĐÈ" : "TRỐNG")}";
            Log($"[ĐỌC TRẠNG THÁI] {SelectedTarget.LastResult}");
            IsBusy = false;
            return;
        }

        try
        {
            var config = SelectedTarget.ToConfig();
            var res = await _barrierManager.GetStatusAsync(config);
            if (res.IsConnected)
            {
                SelectedTarget.CurrentStatus = res.Status;
                SelectedTarget.HasVehicleOnLoop = res.LoopDetected ?? false;
                SelectedTarget.LastResult = $"Trạng thái: {res.Status} | Vòng từ: {(res.LoopDetected == true ? "CÓ XE" : "TRỐNG")} | Chi tiết: {res.RawDetail}";
            }
            else
            {
                SelectedTarget.LastResult = $"Không đọc được: {res.RawDetail}";
            }
            Log($"[ĐỌC TRẠNG THÁI] {SelectedTarget.LastResult}");
        }
        catch (Exception ex)
        {
            SelectedTarget.LastResult = $"Lỗi: {ex.Message}";
            Log($"[ĐỌC TRẠNG THÁI] Ngoại lệ: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleVehicleLoop()
    {
        if (SelectedTarget is null) return;
        SelectedTarget.HasVehicleOnLoop = !SelectedTarget.HasVehicleOnLoop;
        Log($"[GIẢ LẬP VÒNG TỪ] Thiết bị {SelectedTarget.Code}: {(SelectedTarget.HasVehicleOnLoop ? "🚗 Xe đã tiến vào đè vòng từ" : "🟢 Vòng từ đã giải phóng")}");
    }

    // ===== Nối NATS Sender Test =====

    [RelayCommand]
    private async Task ToggleNatsConnectionAsync()
    {
        if (IsNatsConnected)
        {
            if (_natsSender != null)
            {
                await _natsSender.StopAsync();
            }
            IsNatsConnected = false;
            Log("[NATS] Đã ngắt kết nối NATS.");
        }
        else
        {
            var url = NatsUrl?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ NATS hợp lệ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsBusy = true;
                Log($"[NATS] Đang chuẩn bị kết nối tới {url}...");

                // Giải phóng sender cũ nếu có
                if (_natsSender != null)
                {
                    try { await _natsSender.DisposeAsync(); } catch { }
                    _natsSender = null;
                }

                var natsOpt = new BarrierControlNatsOption
                {
                    Url = url,
                    ControlSubject = ControlSubject?.Trim() ?? "ta.its.control.barrier",
                    Enabled = true
                };

                _natsSender = new BarrierControlSender(natsOpt, _logger);
                _natsSender.PacketObserved += (type, json) =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        var entry = $"[{DateTime.Now:HH:mm:ss}] [{type.ToUpperInvariant()}] {json}";
                        Packets.Insert(0, entry);
                        if (Packets.Count > 100) Packets.RemoveAt(Packets.Count - 1);
                    });
                };

                await _natsSender.StartAsync();
                IsNatsConnected = true;
                Log($"[NATS] Đã kết nối thành công tới {url}. Kênh '{ControlSubject}'.");
            }
            catch (Exception ex)
            {
                IsNatsConnected = false;
                Log($"[NATS] Lỗi kết nối NATS: {ex.Message}");
                MessageBox.Show($"Không thể kết nối tới NATS ({url}):\n{ex.Message}", "Lỗi NATS", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task SendNatsCommandAsync(string action)
    {
        if (_natsSender is null || !_natsSender.IsConnected)
        {
            MessageBox.Show("Vui lòng kết nối NATS trước khi gửi lệnh qua NATS!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedTarget is null)
        {
            MessageBox.Show("Vui lòng chọn ít nhất 1 thiết bị!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        var req = new BarrierControlRequest
        {
            Action = action,
            Simulate = Simulate,
            Targets = new List<BarrierControlTarget>
            {
                new()
                {
                    EquipmentId = SelectedTarget.Code,
                    EquipmentCode = SelectedTarget.Code,
                    Ip = SelectedTarget.IpAddress,
                    Port = SelectedTarget.Port,
                    Rack = SelectedTarget.Rack,
                    Slot = SelectedTarget.Slot,
                    OpenBit = SelectedTarget.OpenBit,
                    CloseBit = SelectedTarget.CloseBit,
                    StatusBit = SelectedTarget.StatusBit,
                    LoopBarrierBit = SelectedTarget.LoopBarrierBit,
                    TimeoutMs = SelectedTarget.TimeoutMs,
                    PingBeforeConnect = SelectedTarget.PingBeforeConnect,
                    PulseResetMs = SelectedTarget.PulseResetMs
                }
            }
        };

        Log($"[NATS GỬI] Bắn lệnh {action} (Simulate={Simulate}) lên kênh {ControlSubject}...");
        try
        {
            var res = await _natsSender.SendAsync(req, 10);
            if (res != null)
            {
                SelectedTarget.LastResult = $"[NATS PHẢN HỒI] Thành công: {res.Success} | {res.Message}";
                Log($"[NATS KẾT QUẢ] RequestId={res.RequestId}: {res.Message}");
            }
            else
            {
                SelectedTarget.LastResult = "[NATS TIMEOUT] Quá thời gian chờ phản hồi từ Worker!";
                Log("[NATS TIMEOUT] Không nhận được phản hồi gói Result từ Worker.");
            }
        }
        catch (Exception ex)
        {
            Log($"[NATS LỖI] {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        Logs.Clear();
    }

    [RelayCommand]
    private void ClearPackets()
    {
        Packets.Clear();
    }

    [RelayCommand]
    private void SaveTargets()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            var configData = new BarrierTestConfigFile
            {
                NatsUrl = NatsUrl,
                ControlSubject = ControlSubject,
                Simulate = Simulate,
                Targets = Targets.ToList()
            };

            var json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            Log($"[LƯU] Đã lưu cấu hình (NATS: {NatsUrl}, {Targets.Count} thiết bị) vào {ConfigFileName}");
            MessageBox.Show($"Đã lưu toàn bộ cấu hình thành công!\n- NATS: {NatsUrl}\n- Thiết bị: {Targets.Count}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadTargets()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            if (!File.Exists(path))
            {
                Targets.Add(new BarrierTargetItemViewModel
                {
                    Code = "BR-01",
                    Name = "Barrier Cửa Hầm Phía Bắc (Km1+644)",
                    IpAddress = "192.168.0.6",
                    Port = 102,
                    OpenBit = "DB1.DBX0.1",
                    CloseBit = "DB1.DBX0.2",
                    StatusBit = "DB1.DBX0.4",
                    LoopBarrierBit = "DB1.DBX1.0"
                });
                SelectedTarget = Targets[0];
                return;
            }

            var json = File.ReadAllText(path);
            
            // Hỗ trợ đọc cả format mới (BarrierTestConfigFile) lẫn format cũ (List<BarrierTargetItemViewModel>)
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var list = JsonSerializer.Deserialize<List<BarrierTargetItemViewModel>>(json);
                    if (list != null)
                    {
                        Targets = new ObservableCollection<BarrierTargetItemViewModel>(list);
                        if (Targets.Count > 0) SelectedTarget = Targets[0];
                    }
                    return;
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var cfg = JsonSerializer.Deserialize<BarrierTestConfigFile>(json);
                    if (cfg != null)
                    {
                        if (!string.IsNullOrWhiteSpace(cfg.NatsUrl)) NatsUrl = cfg.NatsUrl;
                        if (!string.IsNullOrWhiteSpace(cfg.ControlSubject)) ControlSubject = cfg.ControlSubject;
                        Simulate = cfg.Simulate;
                        if (cfg.Targets != null)
                        {
                            Targets = new ObservableCollection<BarrierTargetItemViewModel>(cfg.Targets);
                            if (Targets.Count > 0) SelectedTarget = Targets[0];
                        }
                    }
                    return;
                }
            }
            catch
            {
                var list = JsonSerializer.Deserialize<List<BarrierTargetItemViewModel>>(json);
                if (list != null)
                {
                    Targets = new ObservableCollection<BarrierTargetItemViewModel>(list);
                    if (Targets.Count > 0) SelectedTarget = Targets[0];
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[LỖI TẢI CẤU HÌNH] {ex.Message}");
        }
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Logs.Insert(0, line);
            if (Logs.Count > 500) Logs.RemoveAt(Logs.Count - 1);
        });
    }
}

/// <summary>
/// Cấu trúc tệp lưu trữ cấu hình đầy đủ của công cụ test Barrier.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed class BarrierTestConfigFile
{
    public string NatsUrl { get; set; } = "nats://127.0.0.1:4222";
    public string ControlSubject { get; set; } = "ta.its.control.barrier";
    public bool Simulate { get; set; } = true;
    public List<BarrierTargetItemViewModel> Targets { get; set; } = new();
}
