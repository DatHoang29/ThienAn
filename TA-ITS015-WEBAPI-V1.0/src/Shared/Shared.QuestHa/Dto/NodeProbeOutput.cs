namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả probe một node QuestDB: bảng tồn tại, WAL suspend, writerTxn/sequencerTxn.
/// Created date: 09/10/2026
/// </summary>
internal sealed record NodeProbeOutput(bool TableExists, bool WalSuspended, long WriterTxn, long SequencerTxn, string? Error);
