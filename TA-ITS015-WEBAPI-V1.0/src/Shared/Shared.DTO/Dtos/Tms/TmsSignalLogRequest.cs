using Shared.DTO.Constants.Application;
using Shared.DTO.Dtos.Base;
using Shared.DTO.Enums;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO.Dtos.Tms
{
	/// <summary>
	/// Author: Tuan HA
	/// Description: Đối tượng request để thêm vào TmsSignalLog
	/// </summary>
	public class TmsSignalLogAddRequest : BaseEntityDto
	{
		public string EquipmentID { get; set; }

		public string OldData { get; set; }

		public string NewData { get; set; }

		public string OldUrl { get; set; }

		public string NewUrl { get; set; }

		public DateTime? OldFromDate { get; set; }

		public DateTime? NewFromDate { get; set; }

		public DateTime? OldToDate { get; set; }

		public DateTime? NewToDate { get; set; }

		public DateTime? OldEndDate { get; set; }

		public DateTime? NewEndDate { get; set; }

		public int? TimeToLive { get; set; }

		public string? Type { get; set; }

		public BaseEnums.StatusEnum? Status { get; set; }

		public int? Priority { get; set; }

		public string? ExecuteBy { get; set; }

		public DateTime? ExecuteTime { get; set; }

		public string? Remark { get; set; }

		public string? IncidentId { get; set; }

		public string? State { get; set; }
	}
}
