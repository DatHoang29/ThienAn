using Modules.CfgSystem.Core.Entities;
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
                new SysConfigData(){ ID="1000000010002",ConfigTypeId = "1000000010000", Code = "tms_traffic_matrix",Name = "Ma trận giao thông", Value = "",TagType = "", Remark = "Cấu hình bảng ma trận vận tốc / lưu lượng theo giờ", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},

                new SysConfigData(){ ID="1000000011001",ConfigTypeId = "1000000011000", Code = "header",Name = "Phần đầu báo cáo", Value = "",TagType = "primary", Remark = "Logo, tiêu đề, đơn vị", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000011002",ConfigTypeId = "1000000011000", Code = "footer",Name = "Phần chân báo cáo", Value = "",TagType = "info", Remark = "Ghi chú, địa danh", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000011003",ConfigTypeId = "1000000011000", Code = "signature",Name = "Khối chữ ký", Value = "",TagType = "success", Remark = "Chức danh người ký", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigData(){ ID="1000000011004",ConfigTypeId = "1000000011000", Code = "company",Name = "Thông tin đơn vị", Value = "",TagType = "warning", Remark = "Tên, địa chỉ đơn vị", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},

                // Chế độ xem của bảng ma trận. Code là HỢP ĐỒNG với FE (quyết định đọc vận tốc hay lưu
                // lượng của ô), Name là nhãn nút bấm nên sửa được tuỳ dự án.
                new SysConfigData(){ ID="1000000012001",ConfigTypeId = "1000000012000", Code = "speed",Name = "Vận tốc", Value = "km/h",TagType = "primary", Remark = "Vận tốc trung bình có trọng số theo giờ", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000012002",ConfigTypeId = "1000000012000", Code = "volume",Name = "Lưu lượng", Value = "xe",TagType = "success", Remark = "Số lượt xe trong giờ", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},

                // Dải màu VẬN TỐC (km/h): Code = khoảng nửa mở [from, to), Value = mã màu.
                // Thang màu giảm dần: chậm = đỏ, nhanh = xanh.
                new SysConfigData(){ ID="1000000013001",ConfigTypeId = "1000000013000", Code = "-30",Name = "Dưới 30 km/h", Value = "#FF0000",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000013002",ConfigTypeId = "1000000013000", Code = "30-40",Name = "30~40 km/h", Value = "#FF5A00",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000013003",ConfigTypeId = "1000000013000", Code = "40-50",Name = "40~50 km/h", Value = "#FF9900",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigData(){ ID="1000000013004",ConfigTypeId = "1000000013000", Code = "50-60",Name = "50~60 km/h", Value = "#FFC000",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},
                new SysConfigData(){ ID="1000000013005",ConfigTypeId = "1000000013000", Code = "60-70",Name = "60~70 km/h", Value = "#FFFF00",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 500},
                new SysConfigData(){ ID="1000000013006",ConfigTypeId = "1000000013000", Code = "70-80",Name = "70~80 km/h", Value = "#D4FF00",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 600},
                new SysConfigData(){ ID="1000000013007",ConfigTypeId = "1000000013000", Code = "80-90",Name = "80~90 km/h", Value = "#7FE03C",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 700},
                new SysConfigData(){ ID="1000000013008",ConfigTypeId = "1000000013000", Code = "90-",Name = "Trên 90 km/h", Value = "#00D42A",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 800},

                // Dải màu LƯU LƯỢNG (lượt xe/giờ): thang màu NGƯỢC chiều vận tốc — ít xe = xanh (thông
                // thoáng), nhiều xe = đỏ (đông). Ngưỡng là giá trị khởi điểm, cần hiệu chỉnh sau khi có
                // số liệu thật vài tuần.
                new SysConfigData(){ ID="1000000014001",ConfigTypeId = "1000000014000", Code = "-100",Name = "Dưới 100 xe", Value = "#00D42A",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000014002",ConfigTypeId = "1000000014000", Code = "100-300",Name = "100~300 xe", Value = "#7FE03C",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000014003",ConfigTypeId = "1000000014000", Code = "300-500",Name = "300~500 xe", Value = "#D4FF00",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigData(){ ID="1000000014004",ConfigTypeId = "1000000014000", Code = "500-800",Name = "500~800 xe", Value = "#FFFF00",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},
                new SysConfigData(){ ID="1000000014005",ConfigTypeId = "1000000014000", Code = "800-1200",Name = "800~1200 xe", Value = "#FFC000",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 500},
                new SysConfigData(){ ID="1000000014006",ConfigTypeId = "1000000014000", Code = "1200-1600",Name = "1200~1600 xe", Value = "#FF9900",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 600},
                new SysConfigData(){ ID="1000000014007",ConfigTypeId = "1000000014000", Code = "1600-2000",Name = "1600~2000 xe", Value = "#FF5A00",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 700},
                new SysConfigData(){ ID="1000000014008",ConfigTypeId = "1000000014000", Code = "2000-",Name = "Trên 2000 xe", Value = "#FF0000",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 800},

                // Trạng thái thiết bị: Code = mã trạng thái lưu ở TmsEquipment.EquipmentState / FmsEquipmentHistory.NewState (0/1/2/3), Value = bucket mà service dùng để group.
                // BẮT BUỘC đồng bộ với logic EquipmentTypeChartService.GetStateInfo / EquipmentHistoryChartService.GetStateInfo (active=1, inactive=0, error=2, fault=3).
                new SysConfigData(){ ID="1000000015001",ConfigTypeId = "1000000015000", Code = "1",Name = "Hoạt động", Value = "active",TagType = "success", Remark = "Thiết bị hoạt động bình thường", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000015002",ConfigTypeId = "1000000015000", Code = "0",Name = "Không hoạt động", Value = "inactive",TagType = "info", Remark = "Thiết bị không hoạt động", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000015003",ConfigTypeId = "1000000015000", Code = "2",Name = "Lỗi", Value = "error",TagType = "warning", Remark = "Thiết bị lỗi", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigData(){ ID="1000000015004",ConfigTypeId = "1000000015000", Code = "3",Name = "Sự cố", Value = "fault",TagType = "danger", Remark = "Thiết bị sự cố", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},

                // Màu cho 4 bucket + warning/danger dùng ở ValidEquipmentRateList (đánh giá tỉ lệ)
                new SysConfigData(){ ID="1000000016001",ConfigTypeId = "1000000016000", Code = "active",Name = "Màu hoạt động", Value = "#67C23A",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000016002",ConfigTypeId = "1000000016000", Code = "inactive",Name = "Màu không hoạt động", Value = "#909399",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000016003",ConfigTypeId = "1000000016000", Code = "error",Name = "Màu lỗi", Value = "#E6A23C",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigData(){ ID="1000000016004",ConfigTypeId = "1000000016000", Code = "fault",Name = "Màu sự cố", Value = "#F56C6C",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},
                new SysConfigData(){ ID="1000000016005",ConfigTypeId = "1000000016000", Code = "warning",Name = "Màu cảnh báo", Value = "#E6A23C",TagType = "", Remark = "Dùng cho tỉ lệ warning", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 500},

                // Khoảng tỉ lệ: Code = success/warning/danger, Value = "from-to". Phải khớp ValidEquipmentRateListService.GetStateRange
                new SysConfigData(){ ID="1000000017001",ConfigTypeId = "1000000017000", Code = "success",Name = "Tỉ lệ tốt", Value = "100-100",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000017002",ConfigTypeId = "1000000017000", Code = "warning",Name = "Tỉ lệ cảnh báo", Value = "50-99",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000017003",ConfigTypeId = "1000000017000", Code = "danger",Name = "Tỉ lệ nguy hiểm", Value = "0-49",TagType = "", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},

                // Kiểu giá trị cấu hình thông tin báo cáo: Code khớp RptInfoValueTypeEnum ở FE (text/image/date/html)
                new SysConfigData(){ ID="1000000018001",ConfigTypeId = "1000000018000", Code = "text",Name = "Văn bản", Value = "",TagType = "primary", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 100},
                new SysConfigData(){ ID="1000000018002",ConfigTypeId = "1000000018000", Code = "image",Name = "Hình ảnh", Value = "",TagType = "success", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 200},
                new SysConfigData(){ ID="1000000018003",ConfigTypeId = "1000000018000", Code = "date",Name = "Ngày", Value = "",TagType = "warning", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 300},
                new SysConfigData(){ ID="1000000018004",ConfigTypeId = "1000000018000", Code = "html",Name = "HTML", Value = "",TagType = "info", Remark = "", Status = BaseEnums.StatusEnum.Enable ,CreateTime=DateTime.Now, OrderNo = 400},

            };
        }
    }
}
