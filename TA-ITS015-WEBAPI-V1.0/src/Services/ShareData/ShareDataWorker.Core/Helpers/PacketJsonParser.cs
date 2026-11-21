using System.Text.Json;
using ShareDataWorker.Core.Dto;

namespace ShareDataWorker.Core.Helpers;

/// <summary>
/// Description: Bộ phân tích cú pháp JSON cho gói tin và bộ mã CodeSet (dùng chung hai chiều Inbound và Outbound).
/// Created date: 18/09/2026
/// </summary>
public static class PacketJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };


    /// <summary>
    /// Description: Phân tích cú pháp JSON cấu hình bộ mã thành CodeSetDto (hỗ trợ cả cấu trúc Object mới và Array cũ).
    /// Created date: 18/09/2026
    /// </summary>
    public static CodeSetDto ParseCodeSet(string? valuesJson)
    {
        if (string.IsNullOrWhiteSpace(valuesJson))
            return new CodeSetDto();

        try
        {
            using var doc = JsonDocument.Parse(valuesJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return new CodeSetDto
                {
                    Values = ParseCodeValuesFromArray(root)
                };
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                var dto = new CodeSetDto();

                if (TryGetPropertyCaseInsensitive(root, "values", out var valuesElem) && valuesElem.ValueKind == JsonValueKind.Array)
                {
                    dto.Values = ParseCodeValuesFromArray(valuesElem);
                }

                if (TryGetPropertyCaseInsensitive(root, "defaultSourceValue", out var srcElem))
                {
                    dto.DefaultSourceValue = GetElementAsString(srcElem);
                }

                if (TryGetPropertyCaseInsensitive(root, "defaultPartnerValue", out var partnerElem))
                {
                    dto.DefaultPartnerValue = GetElementAsString(partnerElem);
                }

                return dto;
            }

            return new CodeSetDto();
        }
        catch
        {
            return new CodeSetDto();
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đọc mảng JSON thành danh sách giá trị bộ mã, chỉ xét các phần tử là object.
    /// Created date: 18/09/2026
    /// </summary>
    private static List<CodeValueDto> ParseCodeValuesFromArray(JsonElement arrayElement)
    {
        var result = new List<CodeValueDto>();
        foreach (var element in arrayElement.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    var item = JsonSerializer.Deserialize<CodeValueDto>(element.GetRawText(), JsonOptions);
                    if (item != null)
                        result.Add(item);
                }
                catch
                {
                    // Bỏ qua phần tử hỏng mà không làm đổ cả bộ
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Tìm thuộc tính đúng tên trước, không thấy mới so không phân biệt hoa thường.
    /// Created date: 18/09/2026
    /// </summary>
    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(propertyName, out value))
                return true;

            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đổi giá trị JSON thành chuỗi: null trả null, bool thành true hoặc false, kiểu khác giữ nguyên văn JSON.
    /// Created date: 18/09/2026
    /// </summary>
    private static string? GetElementAsString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => element.GetRawText()
        };
    }
}
