namespace Modules.Mail.Core.Constants
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lùi kỳ của một báo cáo đính kèm (MailScheduleItem.PeriodOffset) = số kỳ lùi lại tính từ lúc gửi.
    ///              Danh sách lựa chọn khai ở SysConfigData loại mail_period (MailConst.ConfigTypePeriodOffset);
    ///              hai hằng dưới đây là các giá trị logic tính kỳ hiểu được, và là danh sách dự phòng khi chưa khai.
    /// Created date: 01/10/2026
    /// </summary>
    public static class MailRptPeriodOffset
    {
        public const int Current = 0;
        public const int Previous = 1;
        public static readonly int[] All = { Current, Previous };
    }
}
