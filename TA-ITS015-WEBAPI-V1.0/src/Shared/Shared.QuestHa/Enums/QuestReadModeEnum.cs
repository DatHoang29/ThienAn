namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Cách WebAPI/Services.Vds đọc QuestDB.
/// Created date: 27/10/2026
/// </summary>
public enum QuestReadModeEnum
{
    /// <summary>Như hiện nay: đọc 1 node cố định (ConfigId <c>VdsDefault</c>). Mặc định.</summary>
    Single,

    /// <summary>Đọc qua router: chọn node Ready, failover khi lỗi kết nối, failback sau đối soát.</summary>
    Router,
}
