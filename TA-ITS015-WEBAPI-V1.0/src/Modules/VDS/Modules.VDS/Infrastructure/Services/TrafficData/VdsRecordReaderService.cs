using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Shared.Utility.Utilities;
using Modules.VDS.Infrastructure.Services.TrafficData;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Infrastructure.Services.TrafficData.Rules;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Infrastructure.Services.TrafficData;

/// <summary>
/// Author: KienLT
/// Description: Đọc một bản tin VDS theo khóa kép <c>vendorEventId</c> + <c>detectTime</c>.
///
/// <para><b>Đây là cửa kiểm miền khóa cho CẢ BA API chi tiết</b> (<c>vdsEvent/Detail</c>,
/// <c>vdsTrafficData/Detail</c>, <c>vdsMedia/File</c>) và cho cả hai đường vào: HTTP và message bus. Đặt ở
/// đây vì cả ba handler đều đi qua hàm này — kiểm ở từng handler thì có ba bản dễ lệch nhau, còn kiểm ở
/// validator thì không phủ được đường bus (FluentValidation chỉ chạy cho tham số action).</para>
///
/// <para>Miền hợp lệ và bằng chứng đo được nằm ở <see cref="VdsEventKeyRules"/>.</para>
/// Created date: 22/09/2026
/// </summary>
public sealed class VdsRecordReaderService(VdsRepository<VdsTrafficData> repo) : IScoped
{
    /// <summary>
    /// Đọc bản tin theo <paramref name="eventId"/> + <paramref name="time"/> (giờ server, quy về UTC) qua
    /// <see cref="VdsRepository{T}.GuardAsync{TResult}"/>. Khóa ngoài miền ⇒ lỗi nghiệp vụ theo <see cref="VdsEventKeyRules"/>;
    /// không có bản ghi ⇒ <c>RecordNotFound</c>; QuestDB/HA không dùng được ⇒ <c>QuestDbUnavailableException</c> (503).
    /// </summary>
    public Task<VdsTrafficData> ReadAsync(long eventId, DateTime time)
    {
        // Khóa sai và bản ghi không tồn tại là HAI nguyên nhân khác nhau: báo riêng để người dùng không đi
        // tìm dữ liệu trong khi thực ra thiếu tham số.
        if (VdsEventKeyRules.Validate(eventId, time) is { } keyError) throw Oops.Oh(keyError);

        var utc = BaseUtil.TimeServer.ToUtc(time);
        return repo.GuardAsync(async questDb =>
        await questDb.Queryable<VdsTrafficData>().Where(x => x.VendorEventId == eventId && x.DetectTime == utc
            && (x.IsDelete == null)).FirstAsync()
        ?? throw Oops.Oh(BaseMsg.Vds.Exception.RecordNotFound));
    }
}
