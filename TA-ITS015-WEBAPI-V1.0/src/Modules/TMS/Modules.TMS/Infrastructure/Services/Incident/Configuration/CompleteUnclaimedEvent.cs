namespace Modules.TMS.Infrastructure.Services.Incident.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Lệnh nội bộ tự kết thúc sự kiện không ai nhận (chỉ phát từ EventCompletionPolicy, không nhận từ API công khai).
/// Created date: 28/09/2026
/// </summary>
public sealed record CompleteUnclaimedEvent(string IncidentId, string TenantId);
