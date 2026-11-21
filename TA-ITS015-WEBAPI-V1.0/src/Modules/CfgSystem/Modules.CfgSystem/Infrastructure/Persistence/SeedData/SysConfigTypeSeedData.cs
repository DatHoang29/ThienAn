using Modules.CfgSystem.Core.Entities;
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
                new SysConfigType(){ ID="1000000011000",Code = "rpt_info_config_type",Name = "Nhóm cấu hình thông tin báo cáo", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1200},

                // Bảng ma trận vận tốc / lưu lượng theo giờ (TMS). Dải màu: Code = khoảng giá trị,
                // Name = nhãn chú giải, Value = mã màu, OrderNo = thứ tự dải trên thanh chú giải.
                new SysConfigType(){ ID="1000000012000",Code = "tms_traffic_matrix_metric",Name = "Chế độ xem ma trận giao thông", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1300},
                new SysConfigType(){ ID="1000000013000",Code = "tms_traffic_matrix_speed_band",Name = "Dải màu vận tốc ma trận giao thông", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1400},
                new SysConfigType(){ ID="1000000014000",Code = "tms_traffic_matrix_volume_band",Name = "Dải màu lưu lượng ma trận giao thông", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1500},

                // Trạng thái thiết bị: dùng chung cho EquipmentTypeChart (pie 4 trạng thái) và
                // EquipmentHistoryChart (line/bar 3 trạng thái lỗi). Code = mã trạng thái (0/1/2/3), Value = bucket (active/inactive/error/fault).
                new SysConfigType(){ ID="1000000015000",Code = "equipment_state",Name = "Trạng thái thiết bị", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1600},
                new SysConfigType(){ ID="1000000016000",Code = "equipment_state_color",Name = "Màu trạng thái thiết bị", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1700},
                new SysConfigType(){ ID="1000000017000",Code = "equipment_state_percent_range",Name = "Khoảng tỉ lệ trạng thái thiết bị", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1800},
                new SysConfigType(){ ID="1000000018000",Code = "rpt_info_value_type",Name = "Kiểu giá trị cấu hình thông tin báo cáo", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 1900},
                // Loại thiết bị được giám sát ở màn Telemetry (FMS). Mỗi dòng: Code = Value = ID loại thiết bị (TmsEquipmentType), Name = tên loại.
                // Chưa có dòng nào -> màn Telemetry hiển thị toàn bộ thiết bị như trước.
                new SysConfigType(){ ID="1000000019000",Code = "fms_telemetry_equipment_type",Name = "Loại thiết bị giám sát Telemetry", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 2000},

            };
        }
    }
}
