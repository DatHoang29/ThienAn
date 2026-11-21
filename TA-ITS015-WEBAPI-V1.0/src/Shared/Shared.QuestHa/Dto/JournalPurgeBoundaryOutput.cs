namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Mốc; MaxSeq=null nghĩa là cấm xoá, CreatedUtc phải nhỏ hơn RetentionBeforeUtc.
/// Khi export Parquet được tính như backup: <see cref="ColdBackupMaxSeq"/> khác null ⇒ MaxSeq chỉ theo cursor và từng dòng phải có
/// Seq ≤ ColdBackupMaxSeq (đã nằm trong backup lạnh Verified) HOẶC đã nằm trong bản xuất Verified của ngày nó; -1 = không có backup lạnh.
/// Created date: 28/10/2026
/// </summary>
public sealed record JournalPurgeBoundaryOutput(long? MaxSeq, DateTime RetentionBeforeUtc, string? BlockedReason, long? ColdBackupMaxSeq = null);
