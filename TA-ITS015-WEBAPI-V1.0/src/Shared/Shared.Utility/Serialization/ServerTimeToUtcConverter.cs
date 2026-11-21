using Shared.Utility.Utilities;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Utility.Serialization;

/// <summary>
/// Author: KienLT
/// Description: Trả cột SQL Server đang lưu GIỜ SERVER ra ngoài dưới dạng UTC có hậu tố Z.
///
/// <para><b>Vì sao không dùng <see cref="UtcDateTimeConverter"/>:</b> lớp đó gọi
/// <see cref="BaseUtil.TimeServer.ToUtc(DateTime)"/>, và hàm đó coi <see cref="DateTimeKind.Unspecified"/>
/// LÀ UTC — chỉ dán Kind chứ không dịch giá trị. Đúng cho dữ liệu QuestDB (vốn là UTC), nhưng SAI cho các cột
/// SQL Server được ghi bằng <c>DateTime.Now</c>: SqlSugar trả về <c>Unspecified</c>, nên dán Z vào là công bố
/// sai thời điểm đúng 7 tiếng ở múi giờ Việt Nam.</para>
///
/// <para>Lớp này gọi <see cref="BaseUtil.TimeServer.ToServerUtc(DateTime)"/> — coi <c>Unspecified</c> là GIỜ
/// SERVER và quy về UTC thật. Phép quy đổi nằm ở <c>TimeServerUtil</c> để cả API boundary và code gọi trực
/// tiếp (handler gán tay khi DTO kế thừa entity) dùng chung một định nghĩa.</para>
///
/// <para><b>Chỉ dùng cho cột đã xác minh là giờ server.</b> Danh sách cột và các cột KHÔNG được dùng ghi tại
/// XML doc của <see cref="BaseUtil.TimeServer.ToServerUtc(DateTime)"/>.</para>
///
/// <para>Không cần lớp riêng cho <c>DateTime?</c>: System.Text.Json tự bọc nullable khi converter được gắn
/// bằng <c>[JsonConverter]</c> trên property — hành vi này đã được
/// <c>VehicleFilterHistoryWindowOutput.DetectTime</c> và test của nó chứng minh.</para>
/// Created date: 22/09/2026
/// </summary>
public sealed class ServerTimeToUtcConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        // Đầu vào mang Z hoặc offset thì đã là UTC; ngược lại hiểu là giờ server và quy về UTC.
        => BaseUtil.TimeServer.ToServerUtc(DateTime.Parse(
            reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(
            BaseUtil.TimeServer.ToServerUtc(value).ToString("O", CultureInfo.InvariantCulture));
}
