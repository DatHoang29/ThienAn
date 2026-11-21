using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.ProcessImg.Dto;
using Modules.VMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.VMS.Controllers.Process.Dto
{
	/// <summary>
	/// Author: TuanHA
	/// Description: Tạo đối tượng để gọi handler chức năng lập lịch
	/// Created date: 08/03/2025
	/// </summary>
	/// 
	public class ScheduleProcessInput 
	{
		public List<string> EquipmentIds { get; set; } = new List<string>();
		public List<ControlScheduleInput> ScheduleList { get; set; } = new List<ControlScheduleInput>();
		public int Brightness { get; set; }
		public int Priority { get; set; }
		public int TimeToLive { get; set; }
		public string? ActionType { get; set; }
		public string? IncidentId { get; set; }
    }


	/// <summary>
	/// Author: TuanHA
	/// Description: Tạo đối tượng dto dùng cho hàm RunThread lập lịch
	/// Created date: 08/03/2025
	/// </summary>
	/// 
	public class RunThreadInput
	{
		public List<VmsProcessImg> ListVmsProcessImage { get; set; } = new List<VmsProcessImg>();
		public string VmsID { get; set; }
		public string ProcessID { get; set; }
		public int StartProcess { get; set; }
		public List<EquipmentInfoInput> ListEquipmentInfos { set; get; } = new List<EquipmentInfoInput>();
		public int Brightness { get; set; }
		public int? Priority { get; set; }
		public string ThreadId { get; set; }
		public string ProcessType { get; set; }
	}

}
