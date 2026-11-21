using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNam;
using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNgay;
using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoQuy;
using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoThang;
using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoThoiGian;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoCa;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNam;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay;
using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoThang;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Report;
using Modules.Report.Base.Core.Dto;

namespace Modules.Mail.Infrastructure.Services.Mail.Report
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Báo cáo THU PHÍ (PluginCode = RptModule.Toll) gửi được qua email — chỗ DUY NHẤT trong phân hệ Mail
    ///              biết tới DTO export của Module.TOLL.Core. Gồm 2 phần đặt cạnh nhau:
    ///                - List : mã báo cáo + kỳ cố định (null = kỳ theo tần suất lịch);
    ///                - Build: switch theo mã báo cáo → DTO export.
    ///              Thêm báo cáo thu phí: thêm 1 dòng vào List + 1 nhánh trong Build.
    ///
    ///              Mail chỉ truyền KỲ và định dạng; mọi bộ lọc khác để trống = lấy tất cả (trạm, ca). Hướng của nhóm
    ///              lưu lượng ghi cứng theo mã (DV = 1, DR = 2) — KHÔNG được để trống: handler LLTN / LLTC / LLTT / LLTNam
    ///              rơi về nhánh đầu ra khi hướng rỗng. Nhóm doanh thu để trống hướng = cả hai hướng.
    ///
    ///              ReportDataId (thuộc BaseReportInput) do ReportExporter gán chung; Format nằm ở từng DTO export nên
    ///              nhánh nào cũng gán Format = c.Format.
    /// Created date: 17/08/2026
    /// </summary>
    public static class TollReports
    {
        /// <summary>Hướng xe gửi sang phân hệ báo cáo thu phí.</summary>
        private const string Vao = "1";
        private const string Ra = "2";

        /// <summary>Bộ lọc để trống = tất cả (trạm, ca, hướng của báo cáo doanh thu).</summary>
        private const string All = "";

        public static readonly MailRptReportInfo[] List =
        {
            /* ── Lưu lượng ── */
            Info(TollReportCode.LuuLuongTheoNgayDauVao, MailRptPeriodType.Day),
            Info(TollReportCode.LuuLuongTheoNgayDauRa, MailRptPeriodType.Day),
            Info(TollReportCode.LuuLuongTheoThangDauVao, MailRptPeriodType.Month),
            Info(TollReportCode.LuuLuongTheoThangDauRa, MailRptPeriodType.Month),
            Info(TollReportCode.LuuLuongTheoNamDauVao, MailRptPeriodType.Year),
            Info(TollReportCode.LuuLuongTheoNamDauRa, MailRptPeriodType.Year),
            // Theo ca: không chọn ca = mọi ca của ngày (cần SP nhận @shift rỗng là tất cả ca)
            Info(TollReportCode.LuuLuongTheoCaDauVao, MailRptPeriodType.Day),
            Info(TollReportCode.LuuLuongTheoCaDauRa, MailRptPeriodType.Day),

            /* ── Doanh thu ── */
            Info(TollReportCode.DoanhThuTheoNgay, MailRptPeriodType.Day),
            Info(TollReportCode.DoanhThuTheoThang, MailRptPeriodType.Month),
            Info(TollReportCode.DoanhThuTheoQuy, MailRptPeriodType.Quarter),
            Info(TollReportCode.DoanhThuTheoNam, MailRptPeriodType.Year),
            // Nhận KHOẢNG ngày nên kỳ nào cũng chạy được — kỳ theo tần suất của lịch
            Info(TollReportCode.DoanhThuTheoThoiGian, null),
        };

        /// <summary>DTO export theo mã báo cáo; null = mã không thuộc thu phí / chưa có nhánh.</summary>
        public static BaseReportInput? Build(string code, RptExportContext c) => code switch
        {
            /* ── Lưu lượng theo ngày ── */
            TollReportCode.LuuLuongTheoNgayDauVao => new LLTNExportInput { Ngay = c.Ngay, Tram = All, Huong = Vao, Format = c.Format },
            TollReportCode.LuuLuongTheoNgayDauRa => new LLTNExportInput { Ngay = c.Ngay, Tram = All, Huong = Ra, Format = c.Format },

            /* ── Lưu lượng theo tháng ── */
            TollReportCode.LuuLuongTheoThangDauVao => new LLTTExportInput { Thang = c.Thang, Nam = c.Nam, Tram = All, Huong = Vao, Format = c.Format },
            TollReportCode.LuuLuongTheoThangDauRa => new LLTTExportInput { Thang = c.Thang, Nam = c.Nam, Tram = All, Huong = Ra, Format = c.Format },

            /* ── Lưu lượng theo năm ── */
            TollReportCode.LuuLuongTheoNamDauVao => new LLTNamExportInput { Nam = c.Nam, Tram = All, Huong = Vao, Format = c.Format },
            TollReportCode.LuuLuongTheoNamDauRa => new LLTNamExportInput { Nam = c.Nam, Tram = All, Huong = Ra, Format = c.Format },

            /* ── Lưu lượng theo ca: không chọn ca = mọi ca của ngày ── */
            TollReportCode.LuuLuongTheoCaDauVao => new LLTCExportInput { Ngay = c.Ngay, Ca = All, Tram = All, Huong = Vao, Format = c.Format },
            TollReportCode.LuuLuongTheoCaDauRa => new LLTCExportInput { Ngay = c.Ngay, Ca = All, Tram = All, Huong = Ra, Format = c.Format },

            /* ── Doanh thu ── */
            TollReportCode.DoanhThuTheoNgay => new DTTNExportInput { Ngay = c.Ngay, Tram = All, Huong = All, Format = c.Format },
            TollReportCode.DoanhThuTheoThang => new DTTTExportInput { Thang = c.Thang, Nam = c.Nam, Tram = All, Huong = All, Format = c.Format },
            TollReportCode.DoanhThuTheoQuy => new DTTQExportInput { Quy = c.Quy, Nam = c.Nam, Tram = All, Huong = All, Format = c.Format },
            TollReportCode.DoanhThuTheoNam => new DTTNamExportInput { Nam = c.Nam, Tram = All, Huong = All, Format = c.Format },

            // Doanh thu theo thời gian nhận KHOẢNG ngày và KHÔNG có tham số hướng
            TollReportCode.DoanhThuTheoThoiGian => new DTTTGExportInput { NgayBatDau = c.From, NgayKetThuc = c.To, Tram = All, Format = c.Format },

            _ => null,
        };

        private static MailRptReportInfo Info(string code, string? periodType)
            => new() { Module = MailRptModule.Toll, Code = code, PeriodType = periodType };
    }
}
