using Furion.FriendlyException;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Constants;

public static class IncidentRemarkConst
{
    // Match the existing SQL mapping; include any mandatory cancellation reason.
    public const int MaxLength = EntityConst.RemarkFieldLength;

    public static void Validate(string? remark)
    {
        if (remark?.Length > MaxLength)
            throw new AppFriendlyException($"Ghi chú sự cố không được vượt quá {MaxLength} ký tự, bao gồm lý do hủy được hệ thống bổ sung. Vui lòng rút gọn ghi chú.", null).StatusCode(400);
    }
}
