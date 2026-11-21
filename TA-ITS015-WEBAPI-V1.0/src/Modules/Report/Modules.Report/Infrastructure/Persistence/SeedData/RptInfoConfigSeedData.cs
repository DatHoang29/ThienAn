using Modules.Report.Core.Constants;
using Modules.Report.Core.Entities;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Report.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Dữ liệu khởi tạo cấu hình thông tin báo cáo dùng chung (ReportCode = '*')
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigSeedData : ISqlSugarEntitySeedData<RptInfoConfig>
    {
        public IEnumerable<RptInfoConfig> HasData()
        {
            var g = RptInfoConfigConst.GlobalReportCode;

            return new[]
            {
                /* Header */
                new RptInfoConfig(){ ID="1100000001001", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Header, Code=RptInfoConfigConst.Key.Logo, Name="Logo báo cáo", Value="https://tacorp.vn/wp-content/uploads/2020/06/LOGO-THIEN-AN-small.png", ValueType=RptInfoConfigConst.ValueType.Image, IsSysConfig=true, OrderNo=100, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },
                new RptInfoConfig(){ ID="1100000001002", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Header, Code=RptInfoConfigConst.Key.OrgParent, Name="Đơn vị cấp trên", Value="CN TỔNG CÔNG TY CẢNG HÀNG KHÔNG VIỆT NAM-CTCP", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=true, OrderNo=200, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },
                new RptInfoConfig(){ ID="1100000001003", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Header, Code=RptInfoConfigConst.Key.OrgName, Name="Đơn vị trực thuộc", Value="CẢNG HÀNG KHÔNG QUỐC TẾ TÂN SƠN NHẤT", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=true, OrderNo=300, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },
                new RptInfoConfig(){ ID="1100000001004", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Header, Code=RptInfoConfigConst.Key.Title, Name="Tiêu đề", Value="BÁO CÁO", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=false, OrderNo=400, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },
                new RptInfoConfig(){ ID="1100000001005", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Header, Code=RptInfoConfigConst.Key.SubTitle, Name="Tiêu đề phụ", Value="", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=false, OrderNo=500, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },

                /* Footer */
                new RptInfoConfig(){ ID="1100000002001", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Footer, Code=RptInfoConfigConst.Key.Place, Name="Địa danh", Value="Tp. Hồ Chí Minh", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=true, OrderNo=100, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },
                new RptInfoConfig(){ ID="1100000002002", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Footer, Code=RptInfoConfigConst.Key.Note, Name="Ghi chú ngày xác định", Value="ngày....tháng....năm.....", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=true, OrderNo=200, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },

                /* Signature */
                new RptInfoConfig(){ ID="1100000003001", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Signature, Code=RptInfoConfigConst.Key.LeftName, Name="Chức danh bên trái", Value="GIÁM ĐỐC", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=true, OrderNo=100, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },
                new RptInfoConfig(){ ID="1100000003002", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Signature, Code=RptInfoConfigConst.Key.CenterName, Name="Chức danh giữa", Value="TRƯỞNG CA", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=true, OrderNo=200, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },
                new RptInfoConfig(){ ID="1100000003003", ReportCode=g, GroupCode=RptInfoConfigConst.Group.Signature, Code=RptInfoConfigConst.Key.RightName, Name="Chức danh bên phải", Value="NGƯỜI LẬP BIỂU", ValueType=RptInfoConfigConst.ValueType.Text, IsSysConfig=true, OrderNo=300, Status=BaseEnums.StatusEnum.Enable, CreateTime=DateTime.Now },
            };
        }
    }
}
