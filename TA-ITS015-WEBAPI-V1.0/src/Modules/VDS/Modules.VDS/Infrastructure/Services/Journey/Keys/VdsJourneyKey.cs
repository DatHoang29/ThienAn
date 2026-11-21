using Modules.VDS.Core.Constants;
using Shared.Utility.Utilities;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Modules.VDS.Infrastructure.Services.Journey.Keys;

/// <summary>
/// Author: KienLT
/// Description: Khoá <c>TmsIncident.EventId</c> của sự cố quá tốc độ trung bình sinh từ một hành trình A→B.
///
/// <para><b>Đây là hợp đồng giữa hai tiến trình.</b> Worker <c>Services.Vds</c> GHI khoá này khi nâng sự cố
/// (<c>IncidentPromoter</c>), còn WebAPI ĐỌC lại đúng khoá đó để tìm sự cố của một hành trình
/// (<c>VdsJourneyController.Detail</c>). Lệch một ký tự trong <c>raw</c> là bên đọc không bao giờ tìm thấy
/// gì, mà không có lỗi nào để lần ra — nên công thức phải nằm ở MỘT chỗ duy nhất.</para>
///
/// <para>Dùng hash thay vì ghép chuỗi thô vì cột <c>EventId</c> giới hạn 64 ký tự, còn segmentId là GUID
/// cộng mốc thời gian ISO thì đã vượt. Hash gồm cả <c>fromTime</c> nên hai chuyến khác nhau của cùng biển
/// trên cùng đoạn KHÔNG bị gộp làm một.</para>
/// Created date: 21/09/2026
/// </summary>
public static class VdsJourneyKey
{
    /// <summary>Tiền tố phân biệt khoá sinh từ hành trình với các nguồn sự cố khác.</summary>
    public const string Prefix = VdsJourneyEventConst.EventIdPrefix;

    /// <summary>Loại điều kiện của sự cố quá tốc độ trung bình.</summary>
    public const string AvgSpeedCondition = VdsJourneyEventConst.ConditionDiscriminator;

    /// <summary>
    /// Khoá cho sự cố quá tốc độ trung bình mà <c>VdsJourneyController.Detail</c> dùng để đọc.
    ///
    /// <para><b>Từ 25/09/2026 công thức chỉ còn MỘT bản</b> ở
    /// <see cref="VdsJourneyEventConst.EventId"/> (<c>Modules.VDS.Core</c>), và worker
    /// <c>IncidentPromoter</c> gọi đúng hàm đó khi ghi. Trước đó mỗi bên tự dựng chuỗi — đã đối chiếu
    /// 21/09/2026 là giống nhau, nhưng để hai bản thì đổi một bên là bên đọc không tìm thấy gì và không có
    /// lỗi nào để lần ra. Lớp này giữ lại chỉ để caller hiện có không phải sửa.</para>
    /// </summary>
    public static string ForAvgSpeed(string? segmentId, string? licensePlate, DateTime fromTime)
        => VdsJourneyEventConst.EventId(segmentId, licensePlate, fromTime);
}
