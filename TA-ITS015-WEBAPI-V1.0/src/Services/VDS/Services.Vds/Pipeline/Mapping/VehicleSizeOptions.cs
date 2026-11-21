namespace Services.Vds.Pipeline.Mapping;

/// <summary>Đơn vị kích thước xe được nhận cho <see cref="VehicleSizeOptions.VehicleDimensionUnit"/>.</summary>
public static class DimensionUnits
{
    public const string Meter = "meter";
    public const string Centimeter = "centimeter";
    public const string Millimeter = "millimeter";

    /// <summary>Tên đơn vị có hợp lệ không — dùng để từ chối tham số cấu hình gõ sai.</summary>
    public static bool IsSupported(string? unit) => unit?.Trim().ToLowerInvariant() switch
    {
        Meter or "m" or Centimeter or "cm" or Millimeter or "mm" => true,
        _ => false,
    };
}

/// <summary>
/// Author: KienLT
/// Description: Suy cỡ xe short/medium/long (section <c>"Service:VehicleSize"</c>).
///
/// Tách thành cấu hình thay vì hằng số trong code vì hai lý do: ngưỡng là quy ước nghiệp vụ có thể đổi,
/// và đơn vị kích thước do vendor quyết định — VDS đổi từ mét sang milimét thì phải sửa cấu hình chứ
/// không phải sửa code rồi build lại.
/// Created date: 26/08/2026
/// </summary>
public sealed class VehicleSizeOptions
{
    /// <summary>
    /// Cách suy cỡ xe:
    /// <list type="bullet">
    /// <item><c>length</c> (mặc định): từ CHIỀU DÀI xe do thiết bị cấp.</item>
    /// <item><c>random</c>: ngẫu nhiên nhưng TẤT ĐỊNH theo id sự kiện (replay cho cùng kết quả).
    /// Chỉ để chạy thông luồng/test giao diện, KHÔNG có ý nghĩa nghiệp vụ.</item>
    /// </list>
    /// Giá trị khác ⇒ coi như không xác định được cỡ (nhóm khác).
    /// </summary>
    public string VehicleSizeMode { get; set; } = "length";

    /// <summary>
    /// Chiều dài tối đa của xe <c>short</c>, LUÔN tính bằng MÉT. Giá trị thiết bị cấp được quy đổi về mét
    /// theo <see cref="VehicleDimensionUnit"/> trước khi so, nên đổi đơn vị không phải sửa ngưỡng.
    /// </summary>
    public double VehicleLengthShortMax { get; set; } = 6.0;

    /// <summary>
    /// Chiều dài tối đa của xe <c>medium</c>; từ giá trị này trở lên là <c>long</c>.
    ///
    /// Biên trên là <c>&lt;</c> chứ không phải <c>&lt;=</c>: xe dài đúng 12 m thuộc nhóm LỚN theo đặc tả.
    /// </summary>
    public double VehicleLengthMediumMax { get; set; } = 12.0;

    /// <summary>
    /// ĐƠN VỊ của chiều dài xe trong bản tin thiết bị (áp dụng cho field kiểu chuỗi của payload cũ):
    /// <c>meter</c> (mặc định), <c>centimeter</c> hoặc <c>millimeter</c>.
    ///
    /// Không áp dụng cho field <c>vehicle_length_mm</c> ở mức gốc — field đó đã là milimét theo tên.
    /// </summary>
    public string VehicleDimensionUnit { get; set; } = DimensionUnits.Meter;

    /// <summary>Hệ số quy đổi kích thước thiết bị cấp về MILIMÉT; đơn vị lạ ⇒ coi như mét.</summary>
    public double DimensionToMillimeter => VehicleDimensionUnit?.Trim().ToLowerInvariant() switch
    {
        DimensionUnits.Millimeter or "mm" => 1d,
        DimensionUnits.Centimeter or "cm" => 10d,
        _ => 1000d,
    };

    public VehicleSizeOptions Clone() => new()
    {
        VehicleSizeMode = VehicleSizeMode,
        VehicleLengthShortMax = VehicleLengthShortMax,
        VehicleLengthMediumMax = VehicleLengthMediumMax,
        VehicleDimensionUnit = VehicleDimensionUnit,
    };
}
