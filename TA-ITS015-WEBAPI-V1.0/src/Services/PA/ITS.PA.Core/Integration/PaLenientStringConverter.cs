using System.Text.Json;
using System.Text.Json.Serialization;

namespace ITS.PA.Core.Integration;

/// <summary>
/// Đọc thuộc tính chuỗi một cách dễ tính: nhận cả số/boolean, còn object/mảng thì bỏ qua (trả null).
/// Cần vì trường "data" của gói đăng nhập không có tài liệu về kiểu, và System.Text.Json mặc định sẽ ném lỗi.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public sealed class PaLenientStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var number) ? number.ToString() : reader.GetDouble().ToString();
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
