using Module.ShareData.Core.Dto.CodeSet;

namespace Module.ShareData.Infrastructure.Services.ShareDataCodeSetService
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đọc / ghi cột ShareDataCodeSet.ValuesJson — gộp chung bảng quy đổi (values)
    ///              và giá trị mặc định (defaultSourceValue/defaultPartnerValue) trong 1 JSON object.
    /// Created date: 2026-08-21
    /// </summary>
    internal static class ShareDataCodeSetValueReader
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Hình dạng thật sự lưu trong cột ValuesJson.
        /// Created date: 2026-09-17
        /// </summary>
        private class ValuesJsonPayload
        {
            [JsonProperty("values")]
            public List<ShareDataCodeSetValueDto>? Values { get; set; }

            [JsonProperty("defaultSourceValue", NullValueHandling = NullValueHandling.Ignore)]
            public string? DefaultSourceValue { get; set; }

            [JsonProperty("defaultPartnerValue", NullValueHandling = NullValueHandling.Ignore)]
            public string? DefaultPartnerValue { get; set; }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Giải mã ValuesJson thành bảng quy đổi + giá trị mặc định.
        ///              Dữ liệu cũ (ValuesJson là mảng thuần, lưu trước khi có giá trị mặc định)
        ///              parse ra bảng quy đổi bình thường, 2 giá trị mặc định là null.
        /// Created date: 2026-08-21
        /// </summary>
        internal static (List<ShareDataCodeSetValueDto> Values, string? DefaultSourceValue, string? DefaultPartnerValue) Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return ([], null, null);

            try
            {
                // Định dạng mới: { "values": [...], "defaultSourceValue": ..., "defaultPartnerValue": ... }
                var payload = JsonConvert.DeserializeObject<ValuesJsonPayload>(json!);
                if (payload != null)
                    return (payload.Values ?? [], payload.DefaultSourceValue, payload.DefaultPartnerValue);
            }
            catch
            {
                // Rơi xuống thử định dạng cũ bên dưới.
            }

            try
            {
                // Định dạng cũ: mảng thuần, lưu trước khi gộp thêm giá trị mặc định.
                var legacyValues = JsonConvert.DeserializeObject<List<ShareDataCodeSetValueDto>>(json!);
                return (legacyValues ?? [], null, null);
            }
            catch
            {
                return ([], null, null);
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuyển bảng quy đổi + giá trị mặc định thành JSON để lưu DB.
        ///              Bỏ thuộc tính null cho gọn payload.
        /// Created date: 2026-08-21
        /// </summary>
        internal static string? Serialize(List<ShareDataCodeSetValueDto>? values, string? defaultSourceValue, string? defaultPartnerValue)
        {
            var hasValues = values != null && values.Count > 0;
            var hasDefault = !string.IsNullOrEmpty(defaultSourceValue) || !string.IsNullOrEmpty(defaultPartnerValue);
            if (!hasValues && !hasDefault) return null;

            var payload = new ValuesJsonPayload
            {
                Values = values,
                DefaultSourceValue = defaultSourceValue,
                DefaultPartnerValue = defaultPartnerValue,
            };

            return JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
    }
}
