using Shared.DTO.Dtos.Base;
using System.Collections.Generic;

namespace Shared.DTO.Dtos.System
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cơ bản SysOrg
    /// </summary>
    public class SysOrgResponseBase : BaseEntityDto
    {
		//SysOrg {
			//	ID varchar(64) pk
			//	Pid varchar(64) null > SysOrg.ID
			//	Name string(64) null
			//	Level integer null
			//	Type string(16) null
			//	DirectorId varchar(64) null > SysUser.ID
			//	OrderNo integer null
			//	Status integer null
			//	Remark string(256) null
			//	TenantId varchar(64) null
		//}

		public string? Pid { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin mở rộng SysOrg
    /// </summary>
    public class SysOrgResponse : SysOrgResponseBase
    {
        public List<SysOrgResponse> Children { get; set; }
    }
}
