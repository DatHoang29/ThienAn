namespace Modules.CfgSystem.Core.Refresh;

/// <summary>Một thay đổi dữ liệu cần thông báo: nguồn, phạm vi, khóa và hành động.</summary>
public sealed record RefreshModifyRequest(string Source, string Scope, string? Key, string Action);
