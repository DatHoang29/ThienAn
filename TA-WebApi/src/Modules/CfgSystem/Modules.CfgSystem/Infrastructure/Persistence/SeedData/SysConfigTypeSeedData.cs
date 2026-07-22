using Modules.CfgSystem.Core.Entities;
using Modules.System.Core.Entities;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.CfgSystem.Infrastructure.Persistence.SeedData
{
    public class SysConfigTypeSeedData : ISqlSugarEntitySeedData<SysConfigType>
    {
        public IEnumerable<SysConfigType> HasData()
        {
            return new[]
            {
                new SysConfigType(){ ID="1000000000000",Code = "status_type",Name = "Trạng thái hoạt động", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigType(){ ID="1000000001000",Code = "sys_account_type",Name = "Loại tài khoản", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigType(){ ID="1000000002000",Code = "sys_gender_type",Name = "Giới tính", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigType(){ ID="1000000003000",Code = "language_type",Name = "Ngôn ngữ", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},
                new SysConfigType(){ ID="1000000004000",Code = "menu_type",Name = "Loại danh mục", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 500},
                new SysConfigType(){ ID="1000000005000",Code = "report_type",Name = "Loại báo cáo", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 600},
                new SysConfigType(){ ID="1000000006000",Code = "i18n_prefix",Name = "Phân loại nhóm dịch thuật", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 700},
                new SysConfigType(){ ID="1000000007000",Code = "org_type",Name = "Loại tổ chức", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 800},
                new SysConfigType(){ ID="1000000008000",Code = "yesno_type",Name = "Yes/No", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 900},
                new SysConfigType(){ ID="1000000009000",Code = "boolean_type",Name = "True/False", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1000},
                new SysConfigType(){ ID="1000000010000",Code = "operation_config_type",Name = "Nhóm cấu hình hoạt động", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1100},

            };
        }
    }
}
