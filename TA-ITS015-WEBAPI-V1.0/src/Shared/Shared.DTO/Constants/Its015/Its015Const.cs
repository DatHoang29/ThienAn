using Shared.DTO.Constants.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.DTO.Constants.Its015
{
    public class Its015Const
    {

        public class ConnectionConst : BaseConnectionConst
        {
            public const string ShareData = "ShareDataDB";
        }


        public class SqlStoreConst
        {
            #region Báo cáo doanh thu
            public const string BaoCaoDoanhThuTheoGio = "Tcs023_Proc_BaoCaoDoanhThuTheoGio";
            #endregion

            #region Báo cáo lưu lượng
            /* Tên procedure có chứa dấu '.' nên bắt buộc bọc [] khi gọi qua CommandType.StoredProcedure,
               nếu không SQL Server sẽ hiểu "ITS015_Proc_1" là schema. */
            public const string LuuLuongTheoNgayDauVao = "[dbo].[ITS015_Proc_1.2_ReportTrafficVolume_Daily]";

            public const string LuuLuongTheoNgayDauRa = "[dbo].[ITS015_Proc_1.2_ReportTrafficVolume_Daily]";

            public const string LuuLuongTheoCa = "ITS015_Proc_ReportTrafficVolume_Shift";

            public const string LuuLuongTheoLoTrinh = "ITS015_Proc_ReportTrafficVolume_Road";

            public const string LuuLuongTheoNam = "ITS015_Proc_ReportTrafficVolume_Yearly";

            public const string LuuLuongTheoThang = "ITS015_Proc_ReportTrafficVolume_Monthly";

            public const string DoanhThuTheoThoiGian = "ITS015_Proc_ReportRevenue_Time";

            public const string DoanhThuTheoNgay = "ITS015_Proc_ReportRevenue_Daily";

            public const string DoanhThuTheoThang = "ITS015_Proc_ReportRevenue_Monthly";

            public const string DoanhThuTheoQuy = "ITS015_Proc_ReportRevenue_Quarterly";

            public const string DoanhThuTheoNam = "ITS015_Proc_ReportRevenue_Yearly";
            #endregion
        }
    }
}
