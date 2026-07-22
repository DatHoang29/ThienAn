using Modules.CfgSystem.Core.Entities;
using Modules.System.Core.Entities;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.CfgSystem.Infrastructure.Persistence.SeedData
{
    public class SysConfigDataSeedData : ISqlSugarEntitySeedData<SysConfigData>
    {
        public IEnumerable<SysConfigData> HasData()
        {
            return new[]
            {
                new SysConfigData(){ ID="1000000000001",ConfigTypeId = "1000000000000", Code = "1",Name = "Enable", Value = "",TagType = "success", Remark = "Hoạt động", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000000002",ConfigTypeId = "1000000000000", Code = "0",Name = "Disable", Value = "",TagType = "danger", Remark = "Không hoạt động", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                
                new SysConfigData(){ ID="1000000001001",ConfigTypeId = "1000000001000", Code = "111",Name = "Superadmin", Value = "",TagType = "primary", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000001002",ConfigTypeId = "1000000001000", Code = "222",Name = "System Admin", Value = "",TagType = "success", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000001003",ConfigTypeId = "1000000001000", Code = "333",Name = "User", Value = "",TagType = "success", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigData(){ ID="1000000001004",ConfigTypeId = "1000000001000", Code = "444",Name = "Viewer", Value = "",TagType = "success", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},

                new SysConfigData(){ ID="1000000002001",ConfigTypeId = "1000000002000", Code = "male",Name = "Male", Value = "",TagType = "primary", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000002002",ConfigTypeId = "1000000002000", Code = "female",Name = "Female", Value = "",TagType = "primary", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000002003",ConfigTypeId = "1000000002000", Code = "unknown",Name = "Unknown", Value = "",TagType = "primary", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},

                new SysConfigData(){ ID="1000000003001",ConfigTypeId = "1000000003000", Code = "vi-VN",Name = "Vietnamese", Value = "",TagType = "primary", Remark = "Tiếng Việt", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000003002",ConfigTypeId = "1000000003000", Code = "en-US",Name = "English", Value = "",TagType = "primary", Remark = "Tiếng Anh", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},

                new SysConfigData(){ ID="1000000004001",ConfigTypeId = "1000000004000", Code = "dir",Name = "Directory", Value = "",TagType = "warning", Remark = "Thư mục danh mục", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000004002",ConfigTypeId = "1000000004000", Code = "menu",Name = "Menu", Value = "",TagType = "success", Remark = "Menu", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000004003",ConfigTypeId = "1000000004000", Code = "btn",Name = "Button", Value = "",TagType = "info", Remark = "Thao tác", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},

                new SysConfigData(){ ID="1000000005001",ConfigTypeId = "1000000005000", Code = "carbone",Name = "Carbone", Value = "",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000005002",ConfigTypeId = "1000000005000", Code = "fast",Name = "Fast report", Value = "",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000005003",ConfigTypeId = "1000000005000", Code = "fastLicense",Name = "Fast report License", Value = "",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},

                new SysConfigData(){ ID="1000000006001",ConfigTypeId = "1000000006000", Code = "lz.message",Name = "Message", Value = "",TagType = "", Remark = "Dùng cho message  lz.message.xxx.yyy", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigData(){ ID="1000000006002",ConfigTypeId = "1000000006000", Code = "lz.router",Name = "Router", Value = "",TagType = "", Remark = "Dùng cho menu router. Route Name", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 500},
                new SysConfigData(){ ID="1000000006003",ConfigTypeId = "1000000006000", Code = "lz.validation",Name = "Validation", Value = "",TagType = "", Remark = "Dùng cho xác thực dữ liệu", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000006004",ConfigTypeId = "1000000006000", Code = "lz.entity",Name = "Entity", Value = "",TagType = "", Remark = "Dùng cho đối tượng", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000006005",ConfigTypeId = "1000000006000", Code = "lz.param",Name = "Params Config", Value = "",TagType = "", Remark = "Dùng trong cấu hình param.Code Type.Code Data", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 600},
                new SysConfigData(){ ID="1000000006006",ConfigTypeId = "1000000006000", Code = "lz.exception",Name = "Exception", Value = "",TagType = "", Remark = "Dùng cho báo lỗi/ngoại lệ", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},
                new SysConfigData(){ ID="1000000006007",ConfigTypeId = "1000000006000", Code = "lz.label",Name = "Label", Value = "",TagType = "", Remark = "Dùng cho FE - Tiêu đề", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 700},
                new SysConfigData(){ ID="1000000006008",ConfigTypeId = "1000000006000", Code = "lz.button",Name = "Button", Value = "",TagType = "", Remark = "Dùng cho FE - Nút bấm", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 800},
                new SysConfigData(){ ID="1000000006009",ConfigTypeId = "1000000006000", Code = "lz.ui",Name = "UI", Value = "",TagType = "", Remark = "Dùng cho FE - UI gốc", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 900},

                new SysConfigData(){ ID="1000000007001",ConfigTypeId = "1000000007000", Code = "company",Name = "Company", Value = "",TagType = "", Remark = "Công ty", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000007002",ConfigTypeId = "1000000007000", Code = "manager",Name = "Manager", Value = "",TagType = "", Remark = "Quản lý", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000007003",ConfigTypeId = "1000000007000", Code = "team",Name = "Team", Value = "",TagType = "", Remark = "Đội nhóm", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},

                new SysConfigData(){ ID="1000000008001",ConfigTypeId = "1000000008000", Code = "1",Name = "Yes", Value = "success",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000008002",ConfigTypeId = "1000000008000", Code = "0",Name = "No", Value = "danger",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},

                new SysConfigData(){ ID="1000000009001",ConfigTypeId = "1000000009000", Code = "true",Name = "Enable", Value = "success",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000009002",ConfigTypeId = "1000000009000", Code = "false",Name = "Disable", Value = "danger",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},

                new SysConfigData(){ ID="1000000010001",ConfigTypeId = "1000000010000", Code = "operation_config",Name = "Common Config", Value = "",TagType = "", Remark = "Cấu hình chung", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},

            };
        }
    }
}
