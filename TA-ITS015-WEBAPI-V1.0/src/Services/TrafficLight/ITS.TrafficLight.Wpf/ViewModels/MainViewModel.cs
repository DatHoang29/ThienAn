using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITS.TrafficLight.Core.Logging;
using ITS.TrafficLight.Core.Messaging;
using Modules.Integration.TrafficLight;
using Modules.Integration.TrafficLight.Base.Enums;
using Modules.Integration.TrafficLight.Base.Helpers;

namespace ITS.TrafficLight.Wpf.ViewModels;

/// <summary>
/// ViewModel chính cho giao diện kiểm thử Đèn tín hiệu giao thông, hỗ trợ:
/// 1. Kiểm thử trực tiếp qua Siemens S7-1200 hoặc Modbus TCP.
/// 2. Chế độ Giả lập (Simulate Mode) không cần phần cứng.
/// 3. Kiểm thử kênh NATS 2 chiều (phát Request -> nhận Result đối chiếu từ Worker).
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private const string ConfigFileName = "TrafficLightTestTargets.json";
    private readonly IntgTrafficLightManager _trafficLightManager;
    private TrafficLightControlSender? _natsSender;
    private readonly TrafficLightLogger _logger;

    [ObservableProperty]
    private ObservableCollection<TrafficLightTargetItemViewModel> _targets = new();

    [ObservableProperty]
    private TrafficLightTargetItemViewModel? _selectedTarget;

    [ObservableProperty]
    private ObservableCollection<string> _logs = new();

    [ObservableProperty]
    private ObservableCollection<string> _packets = new();

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Chế độ Giả lập (Simulate Mode): Bật = mô phỏng ngay trên RAM.</summary>
    [ObservableProperty]
    private bool _simulate = true;

    [ObservableProperty]
    private string _natsUrl = "nats://127.0.0.1:4222";

    [ObservableProperty]
    private string _controlSubject = "ta.its.control.trafficlight";

    [ObservableProperty]
    private bool _isNatsConnected;

    public MainViewModel()
    {
        _trafficLightManager = new IntgTrafficLightManager();
        _logger = new TrafficLightLogger();
        _logger.LogOccurred += msg => Log(msg);

        LoadTargets();
    }

    [RelayCommand]
    private async Task PingAsync()
    {
        if (SelectedTarget is null)
        {
            MessageBox.Show("Vui lòng chọn 1 đèn từ danh sách!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var ok = await IntgTrafficLightNetHelper.PingAsync(SelectedTarget.IpAddress, 2000);
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
        Log($"[CHECK CONNECT] Kiểm tra kết nối {SelectedTarget.Vendor} tới {SelectedTarget.IpAddress}:{SelectedTarget.Port}...");

        if (Simulate)
        {
            await Task.Delay(250);
            SelectedTarget.IsConnected = true;
            SelectedTarget.LastResult = $"[GIẢ LẬP] Bắt tay {SelectedTarget.Vendor} thành công";
            Log($"[CHECK CONNECT] {SelectedTarget.LastResult}");
            IsBusy = false;
            return;
        }

        try
        {
            var config = SelectedTarget.ToConfig();
            var ok = await _trafficLightManager.CheckConnectionAsync(SelectedTarget.Vendor, config);
            SelectedTarget.IsConnected = ok;
            SelectedTarget.LastResult = ok ? $"Kết nối {SelectedTarget.Vendor} thành công" : $"Không thể kết nối {SelectedTarget.Vendor}";
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
    private async Task SetColorAsync(string colorName)
    {
        if (SelectedTarget is null) return;
        if (!Enum.TryParse<TrafficLightColor>(colorName, true, out var color)) return;

        IsBusy = true;
        Log($"[LỆNH ĐÈN] Đặt màu {color} tới {SelectedTarget.Code} ({SelectedTarget.IpAddress} - {SelectedTarget.Vendor})...");

        if (Simulate)
        {
            await Task.Delay(300);
            SelectedTarget.CurrentColor = color;
            SelectedTarget.LastResult = $"[GIẢ LẬP] Đặt màu {color} thành công";
            Log($"[LỆNH ĐÈN] {SelectedTarget.LastResult}");
            IsBusy = false;
            return;
        }

        try
        {
            var config = SelectedTarget.ToConfig();
            var res = await _trafficLightManager.SetColorAsync(SelectedTarget.Vendor, config, color);
            SelectedTarget.LastResult = res.Success ? $"THÀNH CÔNG ({res.DurationMs}ms): {color}" : $"THẤT BẠI: {res.Message}";
            if (res.Success) SelectedTarget.CurrentColor = color;
            Log($"[LỆNH ĐÈN] Kết quả: {SelectedTarget.LastResult}");
        }
        catch (Exception ex)
        {
            SelectedTarget.LastResult = $"Lỗi: {ex.Message}";
            Log($"[LỆNH ĐÈN] Ngoại lệ: {ex.Message}");
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
        Log($"[ĐỌC TRẠNG THÁI] Đọc từ {SelectedTarget.IpAddress} ({SelectedTarget.Vendor})...");

        if (Simulate)
        {
            await Task.Delay(200);
            SelectedTarget.LastResult = $"[GIẢ LẬP] Màu hiện tại: {SelectedTarget.CurrentColor} | Nguồn: BẬT";
            Log($"[ĐỌC TRẠNG THÁI] {SelectedTarget.LastResult}");
            IsBusy = false;
            return;
        }

        try
        {
            var config = SelectedTarget.ToConfig();
            var res = await _trafficLightManager.GetStatusAsync(SelectedTarget.Vendor, config);
            if (res.IsConnected)
            {
                SelectedTarget.CurrentColor = res.CurrentColor;
                SelectedTarget.LastResult = $"Màu: {res.CurrentColor} | Chi tiết: {res.RawDetail}";
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

                var natsOpt = new TrafficLightControlNatsOption
                {
                    Url = url,
                    ControlSubject = ControlSubject?.Trim() ?? "ta.its.control.trafficlight",
                    Enabled = true
                };

                _natsSender = new TrafficLightControlSender(natsOpt, _logger);
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
    private async Task SendNatsCommandAsync(string colorName)
    {
        if (_natsSender is null || !_natsSender.IsConnected)
        {
            MessageBox.Show("Vui lòng kết nối NATS trước khi gửi lệnh qua NATS!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedTarget is null)
        {
            MessageBox.Show("Vui lòng chọn ít nhất 1 thiết bị đèn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Enum.TryParse<TrafficLightColor>(colorName, true, out var color)) return;

        IsBusy = true;
        var req = new TrafficLightControlRequest
        {
            Action = TrafficLightControlAction.SetColor,
            Color = color,
            Simulate = Simulate,
            Targets = new List<TrafficLightControlTarget>
            {
                new()
                {
                    EquipmentId = SelectedTarget.Code,
                    EquipmentCode = SelectedTarget.Code,
                    Ip = SelectedTarget.IpAddress,
                    Port = SelectedTarget.Port,
                    Rack = SelectedTarget.Rack,
                    Slot = SelectedTarget.Slot,
                    Vendor = SelectedTarget.Vendor,
                    DbNumber = SelectedTarget.DbNumber,
                    TimeOn = SelectedTarget.TimeOn,
                    TimeOff = SelectedTarget.TimeOff,
                    RedOutput = SelectedTarget.RedOutput,
                    YellowOutput = SelectedTarget.YellowOutput,
                    GreenOutput = SelectedTarget.GreenOutput,
                    FmOutput = SelectedTarget.FmOutput,
                    TimeoutMs = SelectedTarget.TimeoutMs,
                    PingBeforeConnect = SelectedTarget.PingBeforeConnect
                }
            }
        };

        Log($"[NATS GỬI] Bắn lệnh {color} (Simulate={Simulate}) lên kênh {ControlSubject}...");
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
    private void ClearLog() => Logs.Clear();

    [RelayCommand]
    private void ClearPackets() => Packets.Clear();

    [RelayCommand]
    private void SaveTargets()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            var configData = new TrafficLightTestConfigFile
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
                Targets.Add(new TrafficLightTargetItemViewModel
                {
                    Code = "TL-01",
                    Name = "Đèn Tín Hiệu Cửa Hầm Bắc (S7-1200)",
                    IpAddress = "192.168.0.10",
                    Port = 102,
                    Vendor = TrafficLightVendor.SiemensS71200
                });
                SelectedTarget = Targets[0];
                return;
            }

            var json = File.ReadAllText(path);

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var list = JsonSerializer.Deserialize<List<TrafficLightTargetItemViewModel>>(json);
                    if (list != null)
                    {
                        Targets = new ObservableCollection<TrafficLightTargetItemViewModel>(list);
                        if (Targets.Count > 0) SelectedTarget = Targets[0];
                    }
                    return;
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var cfg = JsonSerializer.Deserialize<TrafficLightTestConfigFile>(json);
                    if (cfg != null)
                    {
                        if (!string.IsNullOrWhiteSpace(cfg.NatsUrl)) NatsUrl = cfg.NatsUrl;
                        if (!string.IsNullOrWhiteSpace(cfg.ControlSubject)) ControlSubject = cfg.ControlSubject;
                        Simulate = cfg.Simulate;
                        if (cfg.Targets != null)
                        {
                            Targets = new ObservableCollection<TrafficLightTargetItemViewModel>(cfg.Targets);
                            if (Targets.Count > 0) SelectedTarget = Targets[0];
                        }
                    }
                    return;
                }
            }
            catch
            {
                var list = JsonSerializer.Deserialize<List<TrafficLightTargetItemViewModel>>(json);
                if (list != null)
                {
                    Targets = new ObservableCollection<TrafficLightTargetItemViewModel>(list);
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
/// Cấu trúc tệp lưu trữ cấu hình đầy đủ của công cụ test Đèn tín hiệu.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed class TrafficLightTestConfigFile
{
    public string NatsUrl { get; set; } = "nats://127.0.0.1:4222";
    public string ControlSubject { get; set; } = "ta.its.control.trafficlight";
    public bool Simulate { get; set; } = true;
    public List<TrafficLightTargetItemViewModel> Targets { get; set; } = new();
}
