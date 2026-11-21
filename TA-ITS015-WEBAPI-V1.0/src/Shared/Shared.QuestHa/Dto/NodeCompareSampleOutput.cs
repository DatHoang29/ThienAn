namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một dòng mẫu lệch khi đối soát node nguồn ↔ node đích: loại lệch, khoá DEDUP và (với dòng khác nhau)
/// danh sách cột lệch dạng <c>cột: nguồn=… đích=…</c>.
/// Created date: 27/10/2026
/// </summary>
public sealed record NodeCompareSampleOutput(string Kind, string Key, IReadOnlyList<string> Columns);
