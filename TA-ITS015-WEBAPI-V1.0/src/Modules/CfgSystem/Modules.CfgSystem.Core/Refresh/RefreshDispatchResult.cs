namespace Modules.CfgSystem.Core.Refresh;

/// <summary>Kết quả publish transport; không phải ACK áp dụng cấu hình của service nhận.</summary>
public sealed record RefreshDispatchResult(bool Published, int? Code, Dictionary<string, bool> Transport);
