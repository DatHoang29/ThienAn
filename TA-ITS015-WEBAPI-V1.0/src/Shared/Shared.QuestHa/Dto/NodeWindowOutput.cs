namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả đối soát một cửa sổ thời gian trên một node (số dòng, hash, thiếu/thừa, trùng).
/// Created date: 09/10/2026
/// </summary>
internal sealed record NodeWindowOutput(string Node, long Count, string Hash, WindowStatusEnum Status, int Missing, int Extra, int Different);
