using System.Text.Json;
using ITS.VMS.Core.Messaging;

namespace ITS.VMS.Wpf.ViewModels;

/// <summary>
/// Một thiết bị trong danh sách đã đồng bộ từ BE, hiển thị trên lưới của tab Telemetry.
/// Chỉ đọc — muốn sửa thông tin thiết bị thì sửa trên giao diện BE rồi bấm đồng bộ lại.
/// Author: ChuongLP
/// Created date: 23/09/2026
/// </summary>
public sealed class EquipmentRowItem
{
    public string EquipmentId { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? Name { get; init; }
    public string? TypeCode { get; init; }
    public string? Vendor { get; init; }
    public string? Ip { get; init; }
    public int? Port { get; init; }
    public string? Config { get; init; }

    /// <summary>Đủ Vendor + Ip để gọi ra thiết bị hay chưa.</summary>
    public bool Ready { get; init; }

    /// <summary>Nhãn ngắn để đọc nhanh trên lưới.</summary>
    public string ReadyText => Ready ? "OK" : "THIẾU";

    public static EquipmentRowItem From(VmsEquipmentInfo info) => new()
    {
        EquipmentId = info.EquipmentId,
        Code = info.Code,
        Name = info.Name,
        TypeCode = info.EquipmentTypeCode,
        Vendor = info.Vendor,
        Ip = info.Ip,
        Port = info.Port,
        Config = info.Config,
        Ready = info.IsReady(),
    };
}

/// <summary>
/// Một dòng kết quả đọc thông số của lượt quét gần nhất.
/// Cột Stats hiển thị nguyên văn để hiện trường đối chiếu với đồng hồ/bảng thật.
/// </summary>
public sealed class TelemetryRowItem
{
    public string EquipmentId { get; init; } = string.Empty;
    public string? Code { get; init; }
    public bool Success { get; init; }
    public string State => Success ? "OK" : "LỖI";
    public string? Message { get; init; }

    /// <summary>Số lượng thông số đọc được — nhìn nhanh biết thiết bị trả về đủ hay thiếu.</summary>
    public int StatCount { get; init; }

    /// <summary>Toàn bộ thông số dạng JSON một dòng.</summary>
    public string Stats { get; init; } = string.Empty;

    public static TelemetryRowItem From(VmsTelemetryItem item)
    {
        var stats = item.Stats;
        return new TelemetryRowItem
        {
            EquipmentId = item.EquipmentId,
            Code = item.EquipmentCode,
            Success = item.Success,
            Message = item.Message,
            StatCount = stats?.Count ?? 0,
            Stats = stats == null || stats.Count == 0
                ? string.Empty
                : JsonSerializer.Serialize(stats, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        };
    }
}
