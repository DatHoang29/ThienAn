using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.VMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    [SugarTable("VmsMessage", TableDescription = "Bảng chứa mẫu câu, mẫu từ mặc định trong vms")]
    public class VmsMessage : EntityTenant
    {
        //VmsMessage {
            //ID varchar(64) pk unique
            //Code string(64) null
            //Name string(64) null
            //Lang string(16) null
            //Type string(16) null
            //Content1 string(256) null
            //Content2 string(256) null
            //Content3 string(256) null
            //OrderNo integer null
            //Status integer null
        //}
        // Description: Bảng chứa mẫu câu, mẫu từ mặc định trong vms

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Lang { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Content1 { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Content2 { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Content3 { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "0 = Disable, 1 = Enable")]
        public BaseEnums.StatusEnum? Status { get; set; }
    }
}
