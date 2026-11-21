using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Modules.TMS.Core.Constants;

/// <summary>
/// Author: KienLT
/// Description: Các khóa dùng chung khi ghi sự cố VDS, thay token VMS và lập báo cáo sự cố.
/// SpeedMin/SpeedMax (km/h) giữ ngưỡng lúc phát hiện trên bản ghi thô; min chạy chậm ưu tiên payload.
/// Hành trình dùng ngưỡng dễ dãi nhất của hai camera: max lớn nhất, min nhỏ nhất.
/// Created date: 02/10/2026
/// </summary>
public static class IncidentMetadataConst
{
    public const string AidEventType = "AidEventType";
    public const string VehiclePlate = "VehiclePlate";
    public const string VehicleSpeed = "VehicleSpeed";
    // Giữ cách viết token để tương thích bản ghi và cấu hình VMS đã lưu.
    public const string VehicleLength = "VehicleLenght";
    public const string AverageSpeed = "AverageSpeed";
    public const string VehicleType = "VehicleType";
    public const string VehicleColor = "VehicleColor";
    public const string VehicleSize = "VehicleSize";
    public const string Lane = "Lane";

    public const string SpeedMin = "SpeedMin";
    public const string SpeedMax = "SpeedMax";

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc metadata an toàn; tùy chọn giữ nguyên chuỗi ngày để hiển thị AID đã lưu.
    /// Created date: 22/10/2026
    /// </summary>
    public static JObject Read(string? metadata, bool preserveDateStrings = false)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return new JObject();
        try
        {
            if (!preserveDateStrings) return JObject.Parse(metadata);
            return JsonConvert.DeserializeObject<JObject>(metadata, new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None,
                CheckAdditionalContent = true
            }) ?? new JObject();
        }
        catch (JsonException) { return new JObject(); }
    }
}
