using Microsoft.AspNetCore.Http;
using Shared.Core.Utilities.Constants;
using System.Security.Claims;

namespace Shared.Utility.Sse;

/// <summary>
/// Author: KienLT
/// Description: Danh tính của một kết nối SSE, chụp lại lúc còn trong request để vòng lấy dữ liệu dùng chung
/// tái lập được đúng phạm vi dữ liệu.
///
/// <para><b>Vì sao BẮT BUỘC phải có.</b> Filter tenant của SqlSugar được gắn trong
/// <c>SqlSugarSetup.SetDbAop</c> và nó chỉ được thêm KHI <c>App.User</c> có claim <c>TenantId</c>. Chạy ngoài
/// request thì <c>App.User</c> là null, filter KHÔNG được thêm, và truy vấn trả về dữ liệu của MỌI tenant —
/// <b>không có exception nào báo</b>. Filter soft-delete thì vô điều kiện nên vẫn còn, càng dễ nhầm là mọi
/// thứ vẫn bình thường. Vì vậy một vòng dùng chung sống lâu hơn request phải tự mang theo danh tính.</para>
///
/// <para><b><see cref="AccountType"/> nằm trong khóa nhóm, không phải để trang trí.</b> Với
/// <c>AccountType = 1</c> (SuperAdmin) thì <c>SetDbAop</c> BỎ HẲN filter tenant, nghĩa là truy vấn thấy mọi
/// tenant. Nếu khóa nhóm chỉ có tenant thì một kết nối SuperAdmin và một kết nối người dùng thường của cùng
/// tenant sẽ bị gom chung nhóm, và người dùng thường nhận dữ liệu của cả hệ thống.</para>
/// Created date: 22/09/2026
/// </summary>
public sealed record SnapshotSseScope(string? TenantId, string? AccountType, ClaimsPrincipal? User)
{
    /// <summary>Chụp danh tính từ request hiện tại. Gọi trong action, KHÔNG gọi trong vòng nền.</summary>
    public static SnapshotSseScope Capture(HttpContext? context)
    {
        var user = context?.User;
        return new SnapshotSseScope(
            user?.FindFirst(ClaimConst.TenantId)?.Value,
            user?.FindFirst(ClaimConst.AccountType)?.Value,
            user);
    }

    /// <summary>
    /// Phần định danh phạm vi dữ liệu, dùng làm tiền tố khóa nhóm.
    ///
    /// <para>Hai kết nối chỉ được dùng chung một vòng lấy dữ liệu khi chuỗi này giống nhau.</para>
    /// </summary>
    public string DataScopeKey => $"t={TenantId ?? "<none>"}|a={AccountType ?? "<none>"}";

    /// <summary>
    /// Tái lập danh tính trong async-context hiện tại để filter tenant được gắn đúng.
    ///
    /// <para>Phải gọi TRƯỚC lần chạm DB đầu tiên của vòng: <c>SqlSugarScope</c> tạo một provider cho mỗi
    /// async-context và chạy <c>SetDbAop</c> LAZY ở lần chạm DB đầu tiên, nên bộ filter bị đóng băng theo
    /// thời điểm đó.</para>
    ///
    /// <para>Trả về <c>false</c> khi không có danh tính để tái lập — người gọi phải coi đó là lỗi và KHÔNG
    /// được chạy truy vấn, vì truy vấn sẽ vượt phạm vi tenant.</para>
    /// </summary>
    public bool Restore(IServiceProvider scopedProvider)
    {
        if (User is null || string.IsNullOrWhiteSpace(TenantId)) return false;

        var accessor = Accessor(scopedProvider);
        if (accessor is null) return false;

        // DefaultHttpContext rỗng mang đúng một thứ: ClaimsPrincipal. Không giả lập request, không Response.
        accessor.HttpContext = new DefaultHttpContext { User = User, RequestServices = scopedProvider };
        return true;
    }

    /// <summary>
    /// Bỏ HttpContext giả khỏi async-context hiện tại.
    ///
    /// <para>Phải gọi TRƯỚC khi scope bị dispose. Nếu không, <c>IHttpContextAccessor.HttpContext</c> của
    /// producer còn trỏ tới một context mà <c>RequestServices</c> đã đóng; nhịp sau, mọi code chạm vào
    /// <c>App.HttpContext</c> trước khi <see cref="Restore"/> kịp gán lại sẽ lấy đúng cái context chết đó.</para>
    /// </summary>
    public static void Clear(IServiceProvider scopedProvider)
    {
        var accessor = Accessor(scopedProvider);
        if (accessor is not null) accessor.HttpContext = null;
    }

    private static IHttpContextAccessor? Accessor(IServiceProvider scopedProvider)
    {
        try { return scopedProvider.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor; }
        catch (ObjectDisposedException) { return null; }
    }
}
