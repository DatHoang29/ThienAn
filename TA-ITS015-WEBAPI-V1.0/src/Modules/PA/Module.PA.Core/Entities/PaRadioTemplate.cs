using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.PA.Core.Entities;

/// <summary>
/// Mẫu phát thanh theo loại sự kiện: cho biết loại sự kiện nào phát file âm thanh nào trên máy chủ PA.
/// Bảng cũ: INCIDENT_RADIO_TEMPLATES
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
[SugarTable("PaRadioTemplate")]
public class PaRadioTemplate : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Loại sự kiện (TmsEventType.ID). Cũ: INCIDENT_TYPE_ID")]
    public string? EventTypeId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Tên mẫu. Cũ: NAME")]
    public string? Name { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Mô tả. Cũ: DESCRIPTIONS")]
    public string? Description { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length2048, ColumnDescription = "Nội dung mẫu dạng văn bản. Cũ: PLAIN_TEMPLATE")]
    public string? PlainTemplate { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length2048, ColumnDescription = "Tên file âm thanh đã upload lên máy chủ PA. Cũ: HTML_TEMPLATE")]
    public string? AudioFileName { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Trạng thái. Cũ: STATUS")]
    public BaseEnums.StatusEnum? Status { get; set; }
}
