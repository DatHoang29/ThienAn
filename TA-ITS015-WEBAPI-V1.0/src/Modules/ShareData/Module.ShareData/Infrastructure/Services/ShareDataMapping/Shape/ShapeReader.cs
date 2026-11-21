using Newtonsoft.Json.Linq;

namespace Module.ShareData.Infrastructure.Services.ShareDataMappingReader
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đọc cột ShareDataMapping.TargetShapeJson — bộ khung đầu ra.
    ///              Đặt ở tầng Infrastructure để tầng Controllers và tầng Services dùng chung
    /// Created date: 2026-08-26
    /// </summary>
    internal static class ShapeReader
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Đếm số khoá trong bộ khung có lấy dữ liệu từ gói tin, tức số lần xuất hiện
        ///              ký hiệu $field. Dữ liệu hỏng trả 0 để không làm chết cả màn hình.
        /// Created date: 2026-08-25
        /// </summary>
        internal static int CountMappedFields(string? shapeJson) => ReadShape(shapeJson).FieldKeys.Count;

        /// <summary>
        /// Author: HieuNV
        /// Description: Gom các khoá giá trị hệ thống khai bằng ký hiệu $meta trong bộ khung
        ///              (Now, Serial, PacketCode, PartnerCode). Dữ liệu hỏng trả rỗng.
        /// Created date: 2026-09-18
        /// </summary>
        internal static List<string> ReadMetaKeys(string? shapeJson)
        {
            var metaKeys = new List<string>();
            if (string.IsNullOrWhiteSpace(shapeJson)) return metaKeys;

            try
            {
                WalkMeta(JToken.Parse(shapeJson!), metaKeys);
            }
            catch
            {
                metaKeys.Clear();
            }

            return metaKeys;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Một bước đi gom $meta. Gặp $meta hoặc $field thì dừng nhánh đó lại,
        ///              vì bên trong là tên khoá chứ không phải cấu trúc con.
        /// Created date: 2026-09-18
        /// </summary>
        private static void WalkMeta(JToken? node, List<string> metaKeys)
        {
            switch (node)
            {
                case JArray array:
                    foreach (var item in array) WalkMeta(item, metaKeys);
                    return;

                case JObject obj:
                    var meta = obj[ShapeMark.Meta];
                    if (meta != null)
                    {
                        if (meta.Type != JTokenType.Null) metaKeys.Add(meta.ToString());
                        return;
                    }

                    if (obj[ShapeMark.Field] != null) return;

                    foreach (var prop in obj.Properties()) WalkMeta(prop.Value, metaKeys);
                    return;
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đi đệ quy bộ khung đầu ra, gom tên trường khai ở $field và mã bộ mã khai ở
        ///              $extend.codeSet. Đi một lượt lấy cả hai để phía gọi không phải quét hai lần.
        ///
        ///              Trả về danh sách KHÔNG loại trùng: một trường được phép dùng ở nhiều khoá,
        ///              và mỗi khoá có luật quy đổi riêng.
        ///
        ///              JSON hỏng thì trả rỗng — việc chặn hay không do phía gọi quyết định.
        /// Created date: 2026-08-25
        /// </summary>
        internal static (List<string> FieldKeys, List<string> CodeSets) ReadShape(string? shapeJson)
        {
            var fieldKeys = new List<string>();
            var codeSets = new List<string>();

            if (string.IsNullOrWhiteSpace(shapeJson)) return (fieldKeys, codeSets);

            try
            {
                Walk(JToken.Parse(shapeJson!), fieldKeys, codeSets);
            }
            catch
            {
                fieldKeys.Clear();
                codeSets.Clear();
            }

            return (fieldKeys, codeSets);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Một bước đi trên bộ khung. Object có $exclude thì bỏ qua; có $field thì dừng
        ///              lại ở đó (giá trị bên trong là tên trường, không phải cấu trúc con);
        ///              còn lại thì đi tiếp mọi thuộc tính. Mảng thì đi từng phần tử.
        /// Created date: 2026-08-25
        /// </summary>
        private static void Walk(JToken? node, List<string> fieldKeys, List<string> codeSets)
        {
            switch (node)
            {
                // Mảng: đi từng phần tử. Chuyện mảng có nhân theo dòng dữ liệu hay không là việc của
                // bộ dựng payload (mảng có $field bên trong thì nhân), ở đây chỉ cần gom tên trường.
                case JArray array:
                    foreach (var item in array) Walk(item, fieldKeys, codeSets);
                    return;

                case JObject obj:
                    // Khoá bị loại trừ: không có $field nên không tính vào ánh xạ, dừng luôn để
                    // không đi lạc vào giá trị true bên trong.
                    if (obj[ShapeMark.Exclude] != null) return;

                    // Có $field thì DỪNG: giá trị bên trong là tên trường, không phải cấu trúc con.
                    var field = obj[ShapeMark.Field];
                    if (field != null)
                    {
                        fieldKeys.Add(field.Type == JTokenType.Null ? string.Empty : field.ToString());

                        var codeSet = obj[ShapeMark.Extend]?[ShapeMark.ExtendCodeSet]?.ToString();
                        if (!string.IsNullOrWhiteSpace(codeSet)) codeSets.Add(codeSet!);
                        return;
                    }

                    foreach (var prop in obj.Properties()) Walk(prop.Value, fieldKeys, codeSets);
                    return;
            }
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Ký hiệu của bộ khung đầu ra — khớp chú thích ShareDataMapping.TargetShapeJson
    ///              và hằng cùng tên bên FE (editMapping.vue).
    /// Created date: 2026-08-25
    /// </summary>
    internal static class ShapeMark
    {
        internal const string Field = "$field";
        internal const string Extend = "$extend";
        internal const string Exclude = "$exclude";
        internal const string ExtendCodeSet = "codeSet";

        /// <summary>Giá trị hệ thống: {"$meta":"Now"} — service tự sinh giá trị lúc gửi.</summary>
        internal const string Meta = "$meta";

        /// <summary>Giá trị đã biết lúc lưu hồ sơ, đi kèm $meta: {"$meta":"PacketCode","$value":"101_commonData"}.</summary>
        internal const string MetaValue = "$value";
    }
}
