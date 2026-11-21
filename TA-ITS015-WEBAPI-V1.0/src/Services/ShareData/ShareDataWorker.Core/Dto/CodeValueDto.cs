using System.Text.Json.Serialization;

namespace ShareDataWorker.Core.Dto
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Một dòng quy đổi của bộ mã (ShareDataCodeSet.ValuesJson).
    ///              Tên khoá JSON khớp ShareDataCodeSetValueDto bên WebAPI (sourceValue / partnerValue).
    ///              Chiều gửi: SourceValue → PartnerValue. Chiều nhận: PartnerValue → SourceValue.
    /// Created date: 15/09/2026
    /// </summary>
    public class CodeValueDto
    {
        [JsonPropertyName("sourceValue")]
        public string? SourceValue { get; set; }

        /// <summary>Giá trị gửi đối tác (trước đây gọi là standardValue).</summary>
        [JsonPropertyName("partnerValue")]
        public string? PartnerValue { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("isDefault")]
        public bool? IsDefault { get; set; }

        [JsonPropertyName("orderNo")]
        public int? OrderNo { get; set; }
    }
}
