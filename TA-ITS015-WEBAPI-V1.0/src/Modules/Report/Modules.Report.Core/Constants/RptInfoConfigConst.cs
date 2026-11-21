namespace Modules.Report.Core.Constants
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Hằng số dùng cho cấu hình thông tin báo cáo.
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigConst
    {
        /// <summary>
        /// Giá trị ReportCode đại diện cho cấu hình dùng chung mọi báo cáo
        /// </summary>
        public const string GlobalReportCode = "*";

        /// <summary>
        /// Mã nhóm cấu hình (GroupCode). Cũng là tên node trong JSON truyền vào FastReport.
        /// </summary>
        public struct Group
        {
            public const string Header = "header";
            public const string Footer = "footer";
            public const string Signature = "signature";
            public const string Company = "company";
        }

        /// <summary>
        /// Các key thông dụng (Code). Không bắt buộc, thêm key mới chỉ cần nhập trên UI.
        /// </summary>
        public struct Key
        {
            public const string Logo = "logo";
            public const string Title = "title";
            public const string SubTitle = "subtitle";
            public const string OrgParent = "org_parent";
            public const string OrgName = "org_name";

            public const string LeftName = "left_name";
            public const string CenterName = "center_name";
            public const string RightName = "right_name";

            /* Ảnh chữ ký của từng cột */
            public const string LeftSign = "left_sign";
            public const string CenterSign = "center_sign";
            public const string RightSign = "right_sign";

            public const string Place = "place";
            public const string Note = "note";
        }

        /// <summary>
        /// Kiểu giá trị (ValueType)
        /// </summary>
        public struct ValueType
        {
            public const string Text = "text";
            public const string Image = "image";
            public const string Date = "date";
            public const string Html = "html";
        }
    }
}
