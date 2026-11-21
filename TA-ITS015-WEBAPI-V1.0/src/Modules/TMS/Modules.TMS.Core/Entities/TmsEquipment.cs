using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsEquipment")]
    public class TmsEquipment : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? Ip { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length8)]
        public string? Port { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Url { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Username { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Password { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "0: không hoạt động; 1:hoạt động")]
        public BaseEnums.StatusEnum? Status { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? KmNumber { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? MetNumber { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EquipmentTypeId { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? LaneId { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "Hướng di chuyển 0:B-N; 1:N-B")]
        public int? DirectionId { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "Hướng hoạt động 0:Ngược chiều; 1:Cùng chiều")]
        public int? ActiveDirection { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Vị trí 0:tuyến; 1:hầm")]
        public string? Location { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
        public string? Remark { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? EquipmentVisible { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? EquipmentState { get; set; }


        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Vendor { get; set; }


        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Manufacturer { get; set; }


        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ActiveSize { get; set; }


        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Config { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "Cho phép scan (0: không, 1: có)")]
        public bool? IsScan { get; set; } = false;
    }
}
