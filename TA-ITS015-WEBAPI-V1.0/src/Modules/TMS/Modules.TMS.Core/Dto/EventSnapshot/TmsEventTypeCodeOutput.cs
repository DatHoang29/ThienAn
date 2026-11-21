namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Kết quả tra mã loại sự kiện: <c>CatalogCode</c> là mã đúng cách viết trong danh mục (null nếu không có),
/// <c>TenantCount</c> là số loại cùng mã thuộc tenant được hỏi (0 khi không truyền tenant).
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventTypeCodeOutput
{
    public string? CatalogCode { get; set; }

    public int TenantCount { get; set; }
}
