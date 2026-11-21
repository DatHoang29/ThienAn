namespace Modules.VDS.Core.Entities;

/// <summary>
/// Author: KienLT
/// Description: Ảnh chụp các cột hệ thống của một bản ghi đang lưu trong DB (đi cùng VdsSystemColumns).
/// Created date: 23/09/2026
/// </summary>
public sealed class VdsStoredColumns
{
    public string ID { get; set; } = "";
    public string TenantId { get; set; } = default!;
    public DateTime CreateTime { get; set; }
    public string CreateUId { get; set; } = default!;
    public string RowStatus { get; set; } = default!;
    public DateTime? IsDelete { get; set; }
}
