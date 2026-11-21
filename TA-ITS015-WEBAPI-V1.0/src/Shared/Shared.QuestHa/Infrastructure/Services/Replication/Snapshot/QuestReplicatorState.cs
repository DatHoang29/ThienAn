using Shared.QuestHa.Infrastructure.Services.Replication.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Replication.Snapshot;

/// <summary>
/// Author: KienLT
/// Description: Trạng thái dùng chung của một replicator; mọi phần giữ nguyên khoá Sync và epoch hiện có.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestReplicatorState
{
    internal readonly object Sync = new();
    internal readonly SemaphoreSlim Wake = new(0, int.MaxValue);
    internal readonly LinkedList<(DateTime At, long Seq)> History = new();
    // cursor mà worker biết (đọc/ghi gần nhất). Khác giá trị đọc được ⇒ thế hệ cursor mới ⇒ xoá History.
    internal long? KnownCursor;

    internal TaskCompletionSource ResumeSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource? PauseAck;
    internal bool PauseRequested;
    internal bool Running;
    internal bool IdleAcked;

    internal string? ManualFence;
    internal string? AutoFence;
    internal bool WalSuspended;
    internal bool Connected;
    internal bool EverConnected;
    // rewind sau (lần đầu) kết nối chưa thành công ⇒ giữ trạng thái phục hồi, thử lại ở lượt sau,
    // không giao/Ready qua cursor cũ.
    internal bool RewindPending;
    internal string? RewindWhy;

    // vẫn đòi WAL hoàn tất để kết thúc restore; Ready thường dùng thêm bằng chứng tiến độ.
    // WalConfirmed: đã có lần thăm dò thấy WAL apply xong kể từ lần (re)connect / start lại node.
    // WalPendingSince: mốc sớm nhất có dữ liệu đã ghi (hoặc probe thấy) mà chưa được xác nhận apply.
    internal bool WalConfirmed;
    internal DateTime? WalPendingSince;
    // tách sức khoẻ apply khỏi xác nhận WAL hoàn tất dùng cho restore.
    internal readonly Dictionary<string, QuestWalProgressRules.Progress> WalProgress = new(StringComparer.OrdinalIgnoreCase);
    // tuổi ghi riêng từng bảng; bảng đang gap=0 không mang tuổi pending của bảng khác.
    internal readonly Dictionary<string, DateTime> WalWrittenSince = new(StringComparer.OrdinalIgnoreCase);

    // pha phục hồi + rào thủ công LƯU BỀN (QuestNodeStatus, IJournalStore.GetRecoveryAsync) — nguồn sự thật là
    // SQL, đây chỉ là cache đọc lại mỗi ProbeInterval (hoặc ngay khi RecoveryReadDue). Rec = null: chưa từng đọc được
    // ⇒ rào + không giao (fail-closed). Đọc lỗi sau đó ⇒ giữ giá trị đã biết gần nhất.
    internal NodeRecoveryOutput? Rec;
    internal string? RecoveryReadError;
    internal bool RecoveryReadDue = true;
    internal DateTime LastRecoveryReadUtc = DateTime.MinValue;
    // Thế hệ restore do orchestrator TRONG process này bắt đầu (BeginRecoveryAsync) — chỉ GIỮ RÀO, chưa có quyền tiến pha.
    // Worker mới (restart process) không sở hữu ⇒ Restoring = không giao, không đọc tới khi operator restore lại.
    internal long? OwnedGen;
    // thế hệ đã được orchestrator BÀN GIAO (ArmRecovery) SAU khi thay root + SetCursor + mở lại node thành công.
    // Chỉ khi đó worker mới được giao lúc Restoring và Mark(g).
    internal long? ArmedGen;
    // Epoch tăng ở Begin/Arm/NotifyNodeRestarted. Mỗi nhịp và mỗi probe ghi epoch lúc BẮT ĐẦU; kết quả probe có epoch
    // cũ hơn epoch hiện tại bị bỏ (không đặt WalConfirmed); Mark chỉ từ nhịp bắt đầu sau handoff với WAL xác nhận bởi
    // probe bắt đầu sau handoff.
    internal long Epoch;
    internal long ArmEpoch = long.MaxValue;
    internal long WalConfirmedEpoch = -1;
    // bộ đếm mutation trạng thái phục hồi/rào CỤC BỘ — tăng ở Begin/SetManualFence/Arm/Mark/Complete/Fence/
    // Unfence. RefreshRecovery ghi số đếm lúc BẮT ĐẦU; kết quả về khi số đếm đã đổi ⇒ bỏ (không ghi đè Rec/rào, không xoá
    // ownership), đánh dấu đọc lại.
    internal long RecMutation;

    // rào do GHI lỗi phía node — giữ tới khi một lần ghi thử thành công; thử lại theo backoff luỹ thừa
    // (BackoffInitial → BackoffMax); timeline chỉ ghi khi bắt đầu/đổi thông điệp/hết rào (gộp số lần lặp).
    internal string? WriteFence;
    internal DateTime WriteFenceRetryAt;
    internal TimeSpan WriteFenceBackoff;
    internal int WriteFenceRepeats;
    internal DateTime LastProbeUtc = DateTime.MinValue;
    internal TimeSpan Backoff;

    internal NodeStatusOutput Snapshot = null!;
    internal DateTime LastStatusWriteUtc = DateTime.MinValue;
    internal long Delivered;
    internal long Rewinds;
    internal long Quarantined;
}
