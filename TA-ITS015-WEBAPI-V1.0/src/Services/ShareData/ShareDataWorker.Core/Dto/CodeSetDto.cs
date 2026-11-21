using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShareDataWorker.Core.Dto;

/// <summary>
/// Description: DTO đại diện cho cấu trúc bộ mã quy đổi giá trị (hỗ trợ cả cấu trúc Object mới và Array cũ).
/// Created date: 18/09/2026
/// </summary>
public class CodeSetDto
{
    [JsonPropertyName("values")]
    public List<CodeValueDto> Values { get; set; } = [];

    /// <summary>
    /// Description: Giá trị mặc định phía nội bộ — dùng cho chiều NHẬN khi không khớp mã nào (§3.4).
    /// Created date: 18/09/2026
    /// </summary>
    [JsonPropertyName("defaultSourceValue")]
    public string? DefaultSourceValue { get; set; }

    /// <summary>
    /// Description: Giá trị mặc định phía đối tác — dùng cho chiều GỬI khi không khớp mã nào (§3.4).
    /// Created date: 18/09/2026
    /// </summary>
    [JsonPropertyName("defaultPartnerValue")]
    public string? DefaultPartnerValue { get; set; }
}
