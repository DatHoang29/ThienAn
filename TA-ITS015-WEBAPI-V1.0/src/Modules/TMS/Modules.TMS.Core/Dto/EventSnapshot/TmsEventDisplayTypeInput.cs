namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Yêu cầu kiểm tra mã loại sự kiện (nhóm event, đang bật) khi VDS khai thời gian hiển thị, gọi qua bus.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventDisplayTypeInput
{
    public string? Code { get; set; }
}
