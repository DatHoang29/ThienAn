namespace Modules.Mail.Core.Constants
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Phân hệ sở hữu báo cáo đặt lịch gửi email. Giá trị trùng SysPlugin.Code — cùng mã với
    ///              RptEmailReport.PluginCode. ReportExporter switch theo hằng số này để chọn
    ///              Module.Mail/Infrastructure/Services/Mail/Report/{Phân hệ}Reports.
    /// Created date: 01/10/2026
    /// </summary>
    public static class MailRptModule
    {
        public const string Toll = "TOLL";
        public const string WP = "WP";
        public const string TMS = "TMS";
        //... khai báo thêm các phân hệ/plugin khác khi cần sử dụng chức năng gửi email tự động.

    }
}
