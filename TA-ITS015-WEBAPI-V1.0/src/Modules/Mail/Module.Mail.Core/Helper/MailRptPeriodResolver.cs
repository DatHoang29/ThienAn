using System.Globalization;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Render;

namespace Modules.Mail.Core.Helper
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Tính KỲ DỮ LIỆU của báo cáo đính kèm (gửi số liệu nào) từ loại kỳ + kỳ trước / hiện tại,
    ///              và các câu cảnh báo / ghi chú khi tần suất chạy lệch kỳ dữ liệu.
    ///              Không có kỳ theo ca — báo cáo theo ca dùng kỳ ngày (mọi ca của hôm qua).
    /// Created date: 17/08/2026
    /// </summary>
    public static class MailRptPeriodResolver
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Kỳ dữ liệu theo loại kỳ + PeriodOffset tính từ thời điểm chạy.
        ///                0 = kỳ hiện tại, lũy kế từ đầu kỳ tới lúc gửi (ResolveCurrentPeriod), kể cả kỳ ngày
        ///                1 = kỳ trước (đã chốt): chạy 01/09 → tháng 08, chạy 01/01/2027 → năm 2026
        ///              Theo đúng lựa chọn của người dùng, hệ thống không tự đổi.
        /// Created date: 17/08/2026
        /// </summary>
        public static MailRptPeriod ResolvePeriod(string? periodType, DateTime now, int periodOffset)
        {
            if (periodOffset == MailRptPeriodOffset.Current)
                return ResolveCurrentPeriod(periodType, now);

            return ResolvePeriod(periodType, now);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kỳ ĐANG DIỄN RA: từ đầu kỳ tới đúng thời điểm gửi (now).
        ///
        ///              Cắt tại thời điểm gửi chứ không phải hết hôm qua: báo cáo tháng / quý / năm chỉ nhận
        ///              Thang / Quy / Nam nên phân hệ báo cáo luôn trả số liệu tới lúc truy vấn — cắt kiểu khác
        ///              thì báo cáo nhận khoảng ngày (DoanhThuTheoThoiGian) sẽ lệch với các báo cáo còn lại.
        ///
        ///              Chạy đúng ngày đầu kỳ (01/10 với tháng hiện tại) thì kỳ gần như rỗng — chấp nhận,
        ///              thư có ghi chú "số liệu tạm tính" (xem PeriodNote).
        /// Created date: 01/10/2026
        /// </summary>
        private static MailRptPeriod ResolveCurrentPeriod(string? periodType, DateTime now)
        {
            DateTime start;
            string label, text;
            int quy;

            switch (periodType)
            {
                case MailRptPeriodType.Day:
                    start = now.Date;
                    quy = (start.Month - 1) / 3 + 1;
                    label = $"Ngay{start:ddMMyyyy}";
                    text = $"Ngày {start:dd/MM/yyyy}";
                    break;

                case MailRptPeriodType.Week:
                    {
                        start = now.Date.AddDays(1 - RptScheduleTime.IsoDayOfWeek(now));
                        var week = ISOWeek.GetWeekOfYear(start);
                        var weekYear = ISOWeek.GetYear(start);
                        label = $"Tuan{week:00}_{weekYear}";
                        text = $"Tuần {week:00}/{weekYear}";
                        quy = (start.Month - 1) / 3 + 1;
                        break;
                    }

                case MailRptPeriodType.Quarter:
                    start = new DateTime(now.Year, RptScheduleTime.FirstMonthOfQuarter(now.Month), 1);
                    quy = (start.Month - 1) / 3 + 1;
                    label = $"Quy{quy}_{start.Year}";
                    text = $"Quý {quy}/{start.Year}";
                    break;

                case MailRptPeriodType.Year:
                    start = new DateTime(now.Year, 1, 1);
                    quy = 1;
                    label = $"Nam{start.Year}";
                    text = $"Năm {start.Year}";
                    break;

                default: // month
                    start = new DateTime(now.Year, now.Month, 1);
                    quy = (start.Month - 1) / 3 + 1;
                    label = $"Thang{start:MM_yyyy}";
                    text = $"Tháng {start:MM/yyyy}";
                    break;
            }

            return new MailRptPeriod
            {
                From = start,
                To = now,
                Nam = periodType == MailRptPeriodType.Week ? ISOWeek.GetYear(start) : start.Year,
                Thang = start.Month,
                Quy = quy,
                Ngay = start,
                // Hậu tố LK (lũy kế) để tệp tạm tính không trùng tên tệp chốt kỳ khi người nhận lưu chung thư mục
                Label = $"{label}_LK",
                Text = $"{text} (lũy kế đến {now:HH:mm dd/MM/yyyy})",
                IsPartial = true,
            };
        }

        /// <summary>Kỳ trước — giữ chữ ký cũ, tương đương PeriodOffset = 1.</summary>
        public static MailRptPeriod ResolvePeriod(string? periodType, DateTime now)
        {
            switch (periodType)
            {
                case MailRptPeriodType.Day:
                    {
                        var day = now.Date.AddDays(-1);
                        return new MailRptPeriod
                        {
                            From = day,
                            To = day.AddDays(1).AddSeconds(-1),
                            Nam = day.Year,
                            Thang = day.Month,
                            Quy = (day.Month - 1) / 3 + 1,
                            Ngay = day,
                            Label = $"Ngay{day:ddMMyyyy}",
                            Text = $"Ngày {day:dd/MM/yyyy}",
                        };
                    }

                // Tuần ISO liền trước (thứ Hai → Chủ nhật). PHẢI có nhánh riêng: rơi xuống
                // nhánh default thì báo cáo tuần âm thầm nhận số liệu cả tháng.
                case MailRptPeriodType.Week:
                    {
                        var thisMonday = now.Date.AddDays(1 - RptScheduleTime.IsoDayOfWeek(now));
                        var start = thisMonday.AddDays(-7);
                        var end = start.AddDays(7).AddSeconds(-1);
                        var week = ISOWeek.GetWeekOfYear(start);
                        var weekYear = ISOWeek.GetYear(start);
                        return new MailRptPeriod
                        {
                            From = start,
                            To = end,
                            Nam = weekYear,
                            Thang = start.Month,
                            Quy = (start.Month - 1) / 3 + 1,
                            Ngay = start,
                            Label = $"Tuan{week:00}_{weekYear}",
                            Text = $"Tuần {week:00}/{weekYear} ({start:dd/MM} - {end:dd/MM/yyyy})",
                        };
                    }

                case MailRptPeriodType.Quarter:
                    {
                        var start = new DateTime(now.Year, RptScheduleTime.FirstMonthOfQuarter(now.Month), 1).AddMonths(-3);
                        var quy = (start.Month - 1) / 3 + 1;
                        return new MailRptPeriod
                        {
                            From = start,
                            To = start.AddMonths(3).AddSeconds(-1),
                            Nam = start.Year,
                            Thang = start.Month,
                            Quy = quy,
                            Ngay = start,
                            Label = $"Quy{quy}_{start.Year}",
                            Text = $"Quý {quy}/{start.Year}",
                        };
                    }

                case MailRptPeriodType.Year:
                    {
                        var start = new DateTime(now.Year - 1, 1, 1);
                        return new MailRptPeriod
                        {
                            From = start,
                            To = start.AddYears(1).AddSeconds(-1),
                            Nam = start.Year,
                            Thang = 1,
                            Quy = 1,
                            Ngay = start,
                            Label = $"Nam{start.Year}",
                            Text = $"Năm {start.Year}",
                        };
                    }

                // Mặc định coi là kỳ tháng: đây là kỳ phổ biến nhất và cũng là kỳ mà dữ liệu
                // chắc chắn đã chốt xong tại thời điểm gửi.
                default:
                    {
                        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                        return new MailRptPeriod
                        {
                            From = start,
                            To = start.AddMonths(1).AddSeconds(-1),
                            Nam = start.Year,
                            Thang = start.Month,
                            Quy = (start.Month - 1) / 3 + 1,
                            Ngay = start,
                            Label = $"Thang{start:MM_yyyy}",
                            Text = $"Tháng {start:MM/yyyy}",
                        };
                    }
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: So tần suất chạy với kỳ dữ liệu của một báo cáo, trả câu cảnh báo khi lệch.
        ///
        ///              KHÔNG chặn lưu — đây là lựa chọn hợp lệ (VD lịch hàng tháng gửi kèm báo cáo năm
        ///              để nhắc lại), chỉ cần người soạn biết mình đang chọn gì:
        ///                - Tần suất DÀY hơn kỳ (ngày + báo cáo tháng): mọi lần gửi trong kỳ đều cùng một số liệu.
        ///                - Tần suất THƯA hơn kỳ (tháng + báo cáo ngày): chỉ gửi một kỳ, các kỳ còn lại bị bỏ sót.
        /// Created date: 30/09/2026
        /// </summary>
        public static string? CheckFrequencyMatch(string? frequency, string? periodType, int periodOffset = MailRptPeriodOffset.Previous)
        {
            if (frequency == null || periodType == null) return null;
            if (!MailRptPeriodType.Rank.TryGetValue(frequency, out var f)) return null;
            if (!MailRptPeriodType.Rank.TryGetValue(periodType, out var p)) return null;
            if (f == p) return null;

            // Kỳ hiện tại (lũy kế) chạy dày hơn kỳ là cách dùng đúng: mỗi lần gửi số liệu lại nhiều thêm,
            // không còn là "lặp lại cùng một số liệu".
            if (f < p && periodOffset == MailRptPeriodOffset.Current) return null;

            var freqText = PeriodNoun(frequency);
            var periodText = PeriodNoun(periodType);

            return f < p
                ? $"lịch chạy hàng {freqText} nhưng báo cáo lấy kỳ {periodText}: mọi lần gửi trong cùng một {periodText} đều lặp lại số liệu của {periodText} trước."
                : $"lịch chạy hàng {freqText} nhưng báo cáo lấy kỳ {periodText}: mỗi lần chỉ gửi số liệu của một {periodText} liền trước, các {periodText} khác không được gửi.";
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi chú IN TRONG THƯ cho một báo cáo, để người nhận không hiểu nhầm phạm vi số liệu.
        ///              Trả null khi không cần ghi chú. Hiện ở khối {LuuY}:
        ///                - Kỳ báo cáo NGẮN hơn tần suất (lịch tháng + báo cáo ngày): chỉ có một kỳ nhỏ.
        ///                - Kỳ hiện tại: số liệu tạm tính.
        ///              Trường hợp kỳ DÀI hơn tần suất (lịch ngày + báo cáo tháng) KHÔNG ghi: tệp vẫn đúng số
        ///              liệu tháng trước, người nhận không hiểu nhầm — chỉ cảnh báo ở form soạn lịch.
        /// Created date: 01/10/2026
        /// </summary>
        public static string? PeriodNote(string? frequency, string? periodType, MailRptPeriod period, string reportName)
        {
            var freqNoun = frequency != null ? PeriodNoun(frequency) : null;

            if (period.IsPartial)
                return $"{reportName}: số liệu tạm tính {period.Text}, chưa chốt kỳ.";

            if (frequency != null && periodType != null
                && MailRptPeriodType.Rank.TryGetValue(frequency, out var f)
                && MailRptPeriodType.Rank.TryGetValue(periodType, out var p)
                && p < f)
                return $"{reportName}: chỉ gồm số liệu {period.Text}, không phải số liệu cả {freqNoun}.";

            return null;
        }

        private static string PeriodNoun(string periodType) => periodType switch
        {
            MailRptPeriodType.Day => "ngày",
            MailRptPeriodType.Week => "tuần",
            MailRptPeriodType.Month => "tháng",
            MailRptPeriodType.Quarter => "quý",
            MailRptPeriodType.Year => "năm",
            _ => periodType,
        };
    }
}
