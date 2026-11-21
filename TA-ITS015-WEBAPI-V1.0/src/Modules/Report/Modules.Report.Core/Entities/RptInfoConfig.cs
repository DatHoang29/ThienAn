using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.Report.Core.Entities
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Bảng cấu hình thông tin hiển thị trên báo cáo (header/footer/chữ ký/đơn vị...).
    /// Cấu trúc group - key - value, cho phép cấu hình riêng theo từng báo cáo hoặc dùng chung.
    /// Created date: 27/07/2026
    /// </summary>
    [SugarTable("RptInfoConfig")]
    public class RptInfoConfig : EntityTenant
    {
        /// <summary>
        /// Mã báo cáo áp dụng (RptManagement.Code).
        /// Để trống hoặc '*' = cấu hình dùng chung cho tất cả báo cáo.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Mã báo cáo áp dụng, rỗng hoặc '*' = dùng chung")]
        public string? ReportCode { get; set; }

        /// <summary>
        /// Nhóm cấu hình: header, footer, signature, company...
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Nhóm cấu hình: header/footer/signature/company")]
        public string? GroupCode { get; set; }

        /// <summary>
        /// Tên hiển thị của tham số cấu hình
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>
        /// Giá trị truyền vào báo cáo
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length2048)]
        public string? Value { get; set; }

        /// <summary>
        /// Kiểu giá trị: text / image / date / html
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Kiểu giá trị: text/image/date/html")]
        public string? ValueType { get; set; }

        /// <summary>
        /// Cấu hình hệ thống thì không cho xóa
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public bool? IsSysConfig { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; }
    }
}
