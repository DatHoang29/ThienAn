using Shared.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.TMS.Report.Controllers.TrafficAnalysis.Dto
{

	public class PageTrafficAnalysisInput : BasePageInput
	{
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string Direction { get; set; }
		public int LengthTo { get; set; }
		public int LengthFrom { get; set; }
		public string LicensePlate { get; set; }
		public string VehicleType { get; set; }
		public string EquipmentId {get;set;}
	}

	public class TrafficAnalysisInput : PageTrafficAnalysisInput
	{
		
	}

}
