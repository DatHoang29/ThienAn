using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Dto.IncidentAutomationConfig;

/// <summary>
/// Author: SonTH
/// Description: DTO phân trang cấu hình tự động theo loại sự kiện
/// Created date: 24/07/2026
/// </summary>
public class PageIncidentAutomationConfigInput : BasePageInput
{
    public string? EventTypeId { get; set; }
    public bool? IsAutoExecuteEnabled { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

/// <summary>
/// Author: SonTH
/// Description: DTO lọc danh sách cấu hình tự động
/// Created date: 24/07/2026
/// </summary>
public class IncidentAutomationConfigInput
{
    public string? EventTypeId { get; set; }
    public bool? IsAutoExecuteEnabled { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

/// <summary>
/// Author: SonTH
/// Description: DTO lấy cấu hình theo một loại sự kiện cụ thể
/// Created date: 24/07/2026
/// </summary>
public class GetIncidentAutomationConfigByEventTypeInput
{
    public string EventTypeId { get; set; }
}

/// <summary>
/// Author: SonTH
/// Description: DTO thêm mới cấu hình tự động
/// Created date: 24/07/2026
/// </summary>
public class AddIncidentAutomationConfigInput : TmsIncidentAutomationConfig
{
}

/// <summary>
/// Author: SonTH
/// Description: DTO cập nhật cấu hình tự động
/// Created date: 24/07/2026
/// </summary>
public class UpdateIncidentAutomationConfigInput : AddIncidentAutomationConfigInput
{
}

/// <summary>
/// Author: SonTH
/// Description: DTO xóa cấu hình tự động theo ID
/// Created date: 24/07/2026
/// </summary>
public class DeleteIncidentAutomationConfigInput : BaseIdInput
{
}
