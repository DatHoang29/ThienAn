using System;
using System.IO;

namespace ITS.TrafficLight.Core.Logging;

/// <summary>
/// Hệ thống ghi log độc lập cho dịch vụ Đèn tín hiệu giao thông theo từng ngày.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class TrafficLightLogger
{
    private readonly string _logDir;
    private readonly object _lock = new();

    public event Action<string>? LogOccurred;

    public TrafficLightLogger(string? customDir = null)
    {
        _logDir = customDir ?? Path.Combine(AppContext.BaseDirectory, "Logs", "TrafficLight");
        Directory.CreateDirectory(_logDir);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null) => Write("ERROR", ex != null ? $"{message} | Ex: {ex}" : message);

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        LogOccurred?.Invoke(line);

        lock (_lock)
        {
            try
            {
                var file = Path.Combine(_logDir, $"trafficlight_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(file, line + Environment.NewLine);
            }
            catch
            {
                // Bỏ qua lỗi ghi file để không chặn luồng điều khiển
            }
        }
    }
}
