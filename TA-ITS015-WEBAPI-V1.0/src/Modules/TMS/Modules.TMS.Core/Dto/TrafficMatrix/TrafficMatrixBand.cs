namespace Modules.TMS.Core.Dto.TrafficMatrix;

/// <summary>
/// Author: KienLT
/// Description: Một dải giá trị và màu tương ứng của ma trận giao thông. Khoảng nửa mở <c>[From, To)</c>.
/// Created date: 12/08/2026
/// </summary>
public sealed class TrafficMatrixBand
{
    public TrafficMatrixBand() { }

    public TrafficMatrixBand(double? from, double? to, string? color)
    {
        From = from;
        To = to;
        Color = color;
    }

    /// <summary>Biên dưới (bao gồm); null = không có biên dưới.</summary>
    public double? From { get; set; }

    /// <summary>Biên trên (KHÔNG bao gồm); null = không có biên trên.</summary>
    public double? To { get; set; }

    /// <summary>Màu nền của ô, lấy từ <c>SysConfigData.Value</c>.</summary>
    public string? Color { get; set; }

    /// <summary>Nhãn hiển thị trên thanh chú giải, lấy từ <c>SysConfigData.Name</c>.</summary>
    public string? Name { get; set; }

    public bool Contains(double value)
        => (From is null || value >= From.Value) && (To is null || value < To.Value);
}
