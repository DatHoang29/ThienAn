using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.Vds.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Bảng MÃ refresh mà VDS hiểu trên subject dùng chung <c>ta.its.refresh</c>.
///
/// <para>Bên gửi chỉ bắn một mã; service tự nạp lại mảng dữ liệu tương ứng TỪ DB, ghi xuống file cache và
/// dùng ngay — không restart. Mã lạ (của module khác) được bỏ qua.</para>
///
/// <para><b>Mã 0 = TẤT CẢ</b>: nạp lại mọi mảng có thể thay đổi để đảm bảo đồng bộ — camera, vùng, đoạn
/// A→B, danh mục hiển thị (SysConfigData), cấu hình VdsConfig, danh sách lọc xe.</para>
/// Created date: 04/09/2026
/// </summary>
public static class RefreshCodes
{
    /// <summary>Nạp lại TẤT CẢ các mảng dữ liệu có thể thay đổi.</summary>
    public const int All = 0;

    /// <summary>Cấu hình THAM SỐ — danh mục hiển thị <c>SysConfigData</c> (tên vị trí/hướng cho sự cố).</summary>
    public const int SysConfigData = 1;

    /// <summary>
    /// Cấu hình VDS — VdsConfig: camera, mapping, ngưỡng và các cửa skip/dedup/display.
    /// Giữ mã 2 để tương thích WebAPI và bên gửi cũ.
    /// </summary>
    public const int VdsConfig = 2;
    /// <summary>Tên cũ của mã 2; không còn đọc bảng SysOpConfig.</summary>
    public const int OpConfig = VdsConfig;

    /// <summary>Danh mục thiết bị (camera VDS) — <c>TmsEquipment</c>.</summary>
    public const int Equipment = 3;

    /// <summary>Danh mục vùng + liên kết camera↔vùng — <c>TmsZone</c> / <c>TmsZoneEquipment</c>.</summary>
    public const int Zone = 4;

    /// <summary>Danh mục đoạn A→B (khoảng cách) — <c>TmsTrafficSegment</c>. Dùng cho tốc độ trung bình.</summary>
    public const int Segment = 5;

    /// <summary>Danh sách lọc xe (biển số đen/trắng) — bảng lọc xe.</summary>
    public const int VehicleFilter = 6;

    /// <summary>VDS có xử lý mã này không.</summary>
    public static bool IsKnown(int code) =>
        code is All or SysConfigData or OpConfig or Equipment or Zone or Segment or VehicleFilter;
}

/// <summary>
/// Author: KienLT
/// Description: Payload của subject <c>ta.its.refresh</c> — chỉ một mã: <c>{ "code": n }</c>.
/// <see cref="TryParse"/> là hàm THUẦN nên test được không cần NATS.
/// Created date: 04/09/2026
/// </summary>
public sealed class RefreshMessage
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>Đọc mã từ payload. Chấp nhận cả <c>{"code":2}</c>, <c>{"code":"2"}</c> và số trần <c>2</c>.</summary>
    /// <returns>false = payload không dùng được.</returns>
    public static bool TryParse(string payload, out int code, out string error)
    {
        code = -1;
        error = "";

        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "payload rỗng";
            return false;
        }

        var raw = payload.Trim();

        // Số trần: "2".
        if (int.TryParse(raw, out var bare))
        {
            code = bare;
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // Số trần dạng JSON (2) mà không phải object.
            if (root.ValueKind == JsonValueKind.Number && root.TryGetInt32(out var n))
            {
                code = n;
                return true;
            }

            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("code", out var codeEl))
            {
                error = "thiếu trường 'code'";
                return false;
            }

            switch (codeEl.ValueKind)
            {
                case JsonValueKind.Number when codeEl.TryGetInt32(out var v):
                    code = v;
                    return true;
                case JsonValueKind.String when int.TryParse(codeEl.GetString(), out var v):
                    code = v;
                    return true;
                default:
                    error = "'code' không phải số nguyên";
                    return false;
            }
        }
        catch (JsonException ex)
        {
            error = $"JSON không hợp lệ ({ex.Message})";
            return false;
        }
    }
}
