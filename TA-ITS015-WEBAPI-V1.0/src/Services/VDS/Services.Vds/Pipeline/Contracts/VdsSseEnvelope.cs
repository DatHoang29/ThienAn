using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services.Vds.Shared.Parsing;

namespace Services.Vds.Pipeline.Contracts;

/// <summary>
/// Author: KienLT
/// Description: Phần envelope tối thiểu cần cho vertical slice raw VDS API v1.
/// Created date: 25/08/2026
/// </summary>
public sealed class VdsSseEnvelope
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("camera_ip")]
    public string? CameraIp { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("occurred_utc")]
    public DateTime? OccurredUtc { get; set; }

    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonProperty("metadata")]
    public JObject? Metadata { get; set; }

    /// <summary>Ảnh/clip đính kèm bản tin. Vắng hoặc rỗng là bình thường: không phải bản tin nào cũng có.</summary>
    [JsonProperty("media")]
    public List<VdsMediaItem>? Media { get; set; }

    /// <summary>
    /// Để ở dạng token vì vendor có thể gửi số hoặc chuỗi <c>"-"</c>. Ép sang int ngay khi deserialize
    /// sẽ làm hỏng cả frame chỉ vì một giá trị kích thước không có.
    /// </summary>
    [JsonProperty("vehicle_length_mm")]
    public JToken? VehicleLengthMmToken { get; set; }

    [JsonIgnore]
    public int? VehicleLengthMm
    {
        get
        {
            if (VehicleLengthMmToken is null) return null;

            double? value = VehicleLengthMmToken.Type switch
            {
                JTokenType.Integer or JTokenType.Float => VehicleLengthMmToken.Value<double>(),
                JTokenType.String => VdsParse.Double(VehicleLengthMmToken.Value<string>()?.Trim()),
                _ => null,
            };

            if (value is not double length || length <= 0 || length > int.MaxValue) return null;
            return (int)Math.Round(length, MidpointRounding.AwayFromZero);
        }
    }
}


/// <summary>
/// Một ảnh/clip đính kèm bản tin.
///
/// <c>url</c> có thể là đường dẫn tương đối của thiết bị nên phải ghép với địa chỉ gốc của API trước khi tải.
/// <c>mime</c> khai ở đây KHÔNG được tin tuyệt đối: đuôi file lấy theo kiểu nội dung của phản hồi thật, vì
/// thiết bị có thể khai lệch với nội dung trả về.
/// </summary>
public sealed class VdsMediaItem
{
    [JsonProperty("id")]
    public long Id { get; set; }

    /// <summary>Loại media: <c>vehicle</c>, <c>plate</c>, <c>full</c>, <c>violation</c>, <c>clip</c>, <c>snapshot</c>.</summary>
    [JsonProperty("kind")]
    public string? Kind { get; set; }

    [JsonProperty("mime")]
    public string? Mime { get; set; }

    [JsonProperty("url")]
    public string? Url { get; set; }
}
