namespace Shared.Utility.Threading;

/// <summary>
/// Author: KienLT
/// Description: Chạy một vòng việc nền trong ExecutionContext SẠCH — không thừa hưởng <c>AsyncLocal</c> của nơi khởi
/// động nó.
///
/// <para><b>Vì sao cần.</b> <c>SqlSugarScope</c> giữ client/connection theo <c>AsyncLocal</c>, KHÔNG theo DI scope:
/// <c>IServiceScopeFactory.CreateScope()</c> không tách được connection. Mọi <c>BackgroundService</c> được host gọi
/// <c>ExecuteAsync</c> từ cùng một luồng khởi động, nên nếu luồng đó đã chạm DB thì các worker thừa hưởng CHUNG một
/// connection và đọc/ghi chồng lên nhau ⇒ "Invalid operation. The connection is closed" / "Invalid attempt to call
/// MetaData when reader is closed" (17/10/2026: worker tự động, worker chụp ảnh, worker ảnh đăng ký VDS lỗi cùng lúc).
/// Cùng kỹ thuật đã dùng ở <c>DbReadyGate</c>, <c>SnapshotSseHub</c> và VMS <c>ProcessCommandHandler</c>.</para>
///
/// <para>KHÔNG được <c>await</c> bên trong vùng <c>SuppressFlow</c>: <c>AsyncFlowControl.Dispose()</c> phải chạy trên
/// đúng context đã tạo nó. Ở đây chỉ TẠO task rồi thoát vùng ngay.</para>
/// Created date: 17/10/2026
/// </summary>
public static class IsolatedFlow
{
    /// <summary>Khởi chạy <paramref name="work"/> trên thread pool với ExecutionContext rỗng.</summary>
    public static Task Run(Func<Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        using (ExecutionContext.SuppressFlow())
        {
            return Task.Run(work);
        }
    }
}
