using Newtonsoft.Json;

namespace Module.ShareData.Core.Dto.CodeSet
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Một dòng quy đổi giá trị (ánh xạ với ShareDataCodeSet.ValuesJson — mảng đối tượng này).

    /// Created date: 2026-08-21
    /// </summary>
    public class ShareDataCodeSetValueDto
    {
        /// <summary>Giá trị trong CSDL nội bộ.</summary>
        [JsonProperty("sourceValue")]
        public string? SourceValue { get; set; }
        /// <summary>Giá trị chuẩn gửi ra theo đặc tả của Partner.</summary>
        [JsonProperty("partnerValue")]
        public string? PartnerValue { get; set; }
        /// <summary>Nhãn tiếng Việt để hiển thị — VD 'Chậm'.</summary>
        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }
    }
}
