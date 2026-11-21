using Modules.CfgSystem.Core.Entities;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;

namespace Modules.CfgSystem.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Author: TAC Team
    /// Description: Seed bảng tham số hoạt động (<c>SysOpConfig</c>).
    ///
    /// <para><c>GroupCode</c> phải khớp một bản ghi <c>SysConfigData</c> thuộc loại
    /// <c>operation_config_type</c>, vì màn hình Cấu hình hoạt động gom cấu hình theo nhóm đó.</para>
    ///
    /// Created date: 12/08/2026
    /// </summary>
    public class SysOpConfigSeedData : ISqlSugarEntitySeedData<SysOpConfig>
    {
        public IEnumerable<SysOpConfig> HasData()
        {
            return new[]
            {
                // Loại zone (danh mục tms_zone_type) dùng làm HÀNG của bảng ma trận giao thông.
                // Để rỗng ⇒ ma trận lấy thẳng zone có dữ liệu làm hàng, không gom lên đoạn tuyến.
                new SysOpConfig()
                {
                    ID = "1100000001001",
                    GroupCode = "tms_traffic_matrix",
                    Code = "row_zone_type",
                    Name = "Loại khu vực làm hàng",
                    Value = "section",
                    IsSysConfig = false,
                    Remark = "Mã thuộc danh mục tms_zone_type. Rỗng = lấy zone có dữ liệu làm hàng.",
                    Status = BaseEnums.StatusEnum.Enable,
                    CreateTime = BaseUtil.TimeServer.GetTimeServer(),
                    OrderNo = 100
                },

                // Nhánh zone được xem, thường là TmsZone.ID của tuyến. Chỉ zone thuộc nhánh này lên bảng.
                // Để rỗng ⇒ lấy toàn bộ zone của mọi tuyến.
                new SysOpConfig()
                {
                    ID = "1100000001002",
                    GroupCode = "tms_traffic_matrix",
                    Code = "root_zone_id",
                    Name = "Tuyến được xem",
                    Value = "",
                    IsSysConfig = false,
                    Remark = "TmsZone.ID của tuyến. Rỗng = lấy toàn bộ. FE truyền ZoneId sẽ ghi đè.",
                    Status = BaseEnums.StatusEnum.Enable,
                    CreateTime = BaseUtil.TimeServer.GetTimeServer(),
                    OrderNo = 200
                },
            };
        }
    }
}
