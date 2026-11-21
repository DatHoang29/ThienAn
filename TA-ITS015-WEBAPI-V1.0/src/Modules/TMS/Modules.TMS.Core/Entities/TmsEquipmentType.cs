using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsEquipmentType")]
    public class TmsEquipmentType : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Kích thước icon")]
        public string? Size { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Link hình icon mặc định")]
        public string? DefaultIcon { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Link hình icon hoạt động")]
        public string? ActiveIcon { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Link hình Icon không hoạt động")]
        public string? InactiveIcon { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Link hình Icon lỗi")]
        public string? FaultIcon { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }


        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ModuleId { get; set; }

        /// <summary>
        /// Author: KienLT
        /// Created date: 11/11/2026
        /// Description: Mã modal hiển thị/điều khiển thiết bị trên bigscreen = SysConfigData.Code của danh mục
        ///              'tms_equipment_modal_type' (vms, cctv, lcs, vds, barrier, barrier_light, fan, device).
        ///              Nhiều loại thiết bị trỏ chung một mã để dùng chung một modal; FE không còn phải đoán
        ///              theo mã/tên loại. Null ⇒ FE dùng modal thông tin thiết bị mặc định.
        ///              Cột thật: phải chạy 20261111_TmsEquipmentType_Add_ModalCode.sql trước khi deploy.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã modal hiển thị trên bigscreen (tms_equipment_modal_type)")]
        public string? ModalCode { get; set; }

    }
}
