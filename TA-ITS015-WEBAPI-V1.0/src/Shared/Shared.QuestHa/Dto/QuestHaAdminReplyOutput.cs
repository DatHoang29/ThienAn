namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Trả lời nguyên văn của host Writer cho lệnh IPC backup/restore/unfence (<c>OK …</c> hoặc <c>ERROR …</c>).
/// Created date: 31/10/2026
/// </summary>
public sealed record QuestHaAdminReplyOutput(string? Reply)
{
    /// <summary>True khi host báo hoàn tất; <c>OK RequiresBackfillNotReady</c> (restore chấp nhận journal thiếu) không tính là hoàn tất.</summary>
    public bool Completed => Reply?.StartsWith("OK ", StringComparison.Ordinal) == true && Reply != "OK RequiresBackfillNotReady";
}
