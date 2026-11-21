namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Yêu cầu tra mã loại sự kiện chưa xoá theo mã (kèm đếm theo tenant khi có) cho các luật ghi cấu hình VDS, gọi qua bus.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventTypeCodeInput
{
    public string? Code { get; set; }

    public string? TenantId { get; set; }
}
