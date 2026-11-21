namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: 1 quarantine đang mở. Attempts = thế hệ (tăng mỗi lần quarantine lại); Superseded lấy từ QuestOutbox.
/// Created date: 12/10/2026
/// </summary>
internal sealed record OpenQuarantineOutput(long Seq, string Node, int Attempts, bool Superseded);
