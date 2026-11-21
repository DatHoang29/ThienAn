using Shared.Utility.Utilities;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Utility.Serialization;

/// <summary>
/// Author: KienLT
/// Description: Trả cột/giá trị ĐÃ LÀ UTC ra ngoài dưới dạng UTC có hậu tố Z, giữ nguyên độ chính xác tick.
///
/// <para>Chỉ dán <see cref="DateTimeKind"/>, KHÔNG dịch giá trị: dùng cho kho UTC (QuestDB) và cho payload
/// theo hợp đồng UTC nhưng thiếu hậu tố Z. Phép quy đổi nằm ở
/// <see cref="BaseUtil.TimeServer.ToUtc(DateTime)"/> để cả API boundary và code gọi trực tiếp dùng chung
/// một định nghĩa.</para>
///
/// <para><b>Chọn sai converter là lỗi lệch 7 tiếng.</b> Cột SQL Server ghi bằng <c>DateTime.Now</c> phải
/// dùng <see cref="ServerTimeToUtcConverter"/>, không dùng lớp này.</para>
///
/// <para>Không cần lớp riêng cho <c>DateTime?</c>: System.Text.Json tự bọc nullable khi converter được gắn
/// bằng <c>[JsonConverter]</c> trên property.</para>
///
/// <para>Thay cho ba bản trùng trước đây: <c>VdsUtcDateTimeConverter</c> (Modules.VDS),
/// <c>VehicleFilterHistoryUtcConverter</c> (Modules.TMS), <c>IncidentUtcDateTimeConverter</c>
/// (Modules.TMS.Core).</para>
/// Created date: 25/09/2026
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => BaseUtil.TimeServer.ToUtc(DateTime.Parse(
            reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(BaseUtil.TimeServer.ToUtc(value).ToString("O", CultureInfo.InvariantCulture));
}
