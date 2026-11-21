namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Vai của host khi đăng ký QuestDB HA (<c>AddQuestHa</c>): quyết định thành phần nào được dựng và method nào
/// của <see cref="QuestHaManager"/> dùng được.
/// Created date: 31/10/2026
/// </summary>
public enum QuestHaHostRoleEnum
{
    /// <summary>Worker/WPF: chủ sở hữu journal — replicator, health, bảo trì journal, backup, IPC vận hành.</summary>
    Writer,

    /// <summary>WebAPI: chỉ đọc có chứng nhận coverage; không replicator/backup/IPC.</summary>
    Reader,

    /// <summary>CLI QuestHa.Admin: trạng thái và lệnh vận hành; backup/restore/unfence đi qua IPC tới host Writer.</summary>
    Admin,
}
