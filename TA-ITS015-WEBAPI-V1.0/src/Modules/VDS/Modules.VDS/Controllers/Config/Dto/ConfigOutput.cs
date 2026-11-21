using Shared.Utility.Serialization;
using Modules.VDS.Core.Entities;

namespace Modules.VDS.Controllers.Config.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một nhóm cấu hình VDS cho dropdown.
/// Created date: 16/09/2026
/// </summary>
public class VdsConfigGroupOutput
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

/// <summary>
/// Author: KienLT
/// Description: Một dòng cấu hình VDS trả ra cho trang quản trị.
///
/// <para><b>Kế thừa entity theo đúng mẫu <c>{Feature}Output : {Entity}</c> của repo</b> — giống
/// <c>SysOpConfigOutput : SysOpConfig</c>. Bản cũ khai tường minh từng field để khỏi "lộ" <c>TenantId</c>,
/// <c>IsDelete</c>, <c>RowStatus</c>, <c>CreateUId</c>, <c>UpdateUId</c>; đó là lệch chuẩn tự đặt ra, vì mọi
/// feature CRUD khác trong repo đều trả đủ các cột đó.</para>
///
/// <para>Hai việc KHÔNG nằm ở DTO mà ở <c>ConfigQueryHandler</c>, để giữ được thân lớp rỗng:</para>
/// <list type="bullet">
/// <item><c>Value</c> của mã bí mật (xem <c>VdsConfigRules.IsSecret</c>) bị che thành <c>null</c>.</item>
/// <item><c>CreateTime</c>/<c>UpdateTime</c> quy từ GIỜ SERVER về UTC bằng
/// <c>BaseUtil.TimeServer.ToServerUtc</c> — cùng phép quy đổi mà attribute
/// <c>[JsonConverter]</c> làm trước đây. Gắn attribute lên property <c>new</c> che property của base sẽ tạo
/// hai property cùng tên, rủi ro cho cả auto-map của SqlSugar và ghi JSON; quy đổi trong handler cho đúng
/// kết quả mà không phải che.</item>
/// </list>
/// Created date: 21/09/2026
/// </summary>
public class VdsConfigOutput : VdsConfig { }

/// <summary>
/// Author: KienLT
/// Description: KẾT QUẢ HANDLER của thao tác thêm, KHÔNG phải response HTTP.
///
/// <para>Controller bóc lấy <c>Id</c> và trả về một chuỗi trần — hợp đồng HTTP của <c>Add</c> là
/// <c>string</c>, cố ý giữ nguyên. Đặt tên <c>Result</c> (không phải <c>Output</c>) để không ai đọc code mà
/// tưởng đây là hình dạng JSON trả ra.</para>
/// Created date: 16/09/2026
/// </summary>
public class VdsConfigAddResult
{
    public string Id { get; set; } = "";
}

/// <summary>
/// Author: KienLT
/// Description: KẾT QUẢ HANDLER của thao tác cập nhật, KHÔNG phải response HTTP.
///
/// <para>Controller bóc lấy <c>Success</c> và trả về <c>bool</c> trần — hợp đồng HTTP của <c>Update</c> là
/// <c>bool</c>, cố ý giữ nguyên.</para>
/// Created date: 16/09/2026
/// </summary>
public class VdsConfigWriteResult
{
    public bool Success { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Thống kê loại AID chưa ánh xạ trong khoảng tra cứu.
/// Created date: 19/10/2026
/// </summary>
public sealed class UnmappedEventTypeOutput
{
    public string? AidEventType { get; set; }
    public long Count { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime LastSeen { get; set; }
}
