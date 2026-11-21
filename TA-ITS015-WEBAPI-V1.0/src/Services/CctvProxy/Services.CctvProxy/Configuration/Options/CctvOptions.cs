namespace Services.CctvProxy.Configuration.Options;

/// <summary>
/// Author: kien.le
/// Description: Cấu hình kết nối NxVMS + ticket. Nguồn chính là SysOpConfig (nhóm cctv_config) — dùng CHUNG
/// với WebAPI để chỉ có một nơi cấu hình tài khoản; section "Service:Cctv" trong file chỉ là fallback.
/// Created date: 06/10/2026
/// </summary>
public sealed class CctvOptions
{
    /// <summary>Địa chỉ NxVMS, vd https://10.10.8.30:7001 (SysOpConfig: cctv_base_url).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Tài khoản NxVMS (SysOpConfig: cctv_username).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Mật khẩu NxVMS (SysOpConfig: cctv_password).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Secret ký ticket, phải trùng với WebAPI (SysOpConfig: cctv_proxy_ticket_secret).</summary>
    public string TicketSecret { get; set; } = string.Empty;

    /// <summary>Chấp nhận chứng chỉ tự ký của NxVMS (mặc định true vì NxVMS thường dùng cert tự ký).</summary>
    public bool AcceptAnyCertificate { get; set; } = true;
}
