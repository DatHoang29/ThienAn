using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsWorkDetailContact")]
    public class TmsWorkDetailContacts : EntityTenant
    {


        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WorkDetailId { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WorkContactId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }







    }
}
