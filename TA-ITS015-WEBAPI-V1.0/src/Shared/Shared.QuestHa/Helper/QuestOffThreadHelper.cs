using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Helper;

/// <summary>
/// Author: KienLT
/// Description: chạy một lời gọi I/O có thể CHẶN ĐỒNG BỘ trước await đầu (SqlSugar CheckConnection → Npgsql Open) trên
/// thread riêng (LongRunning): không bao giờ chạy inline trên thread của caller (đang giữ lock / đang phục vụ request) và
/// không chiếm thread pool trong lúc chặn (tránh đói pool làm trễ probe/continuation khác). Phần sau await đầu chạy như
/// thường trên thread pool.
/// Created date: 26/10/2026
/// </summary>
internal static class QuestOffThreadHelper
{
    /// <summary>Như <see cref="Run(Func{Task})"/> nhưng trả kết quả của <paramref name="op"/>.</summary>
    public static Task<T> Run<T>(Func<Task<T>> op) =>
        Task.Factory.StartNew(op, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default).Unwrap();

    /// <summary>Chạy <paramref name="op"/> trên thread riêng (LongRunning) để lời gọi mở kết nối đồng bộ không giữ thread pool/caller.</summary>
    public static Task Run(Func<Task> op) =>
        Task.Factory.StartNew(op, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default).Unwrap();
}
