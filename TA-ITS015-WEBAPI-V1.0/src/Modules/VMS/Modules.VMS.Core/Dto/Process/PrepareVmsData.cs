using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.VMS.Controllers.Process.Dto
{
	public class PrepareVmsData 
	{
		public List<EquipmentInfoInput> ListEquipmentInfos { get; set; } = new List<EquipmentInfoInput>();
		public ContentInfoInput ContentInfoInput { get; set; } = new ContentInfoInput();
		public ProcessInfoInput ProcessInfoInput { get; set; } = new ProcessInfoInput();
		public EventInfoInput EventInfoInput { get; set; } = new EventInfoInput();
	}

	public class EquipmentInfoInput
	{
		public string EquipmentId { get; set; }
		public string EquipmentName { get; set; }
		public string EquipmentCode { get; set; }
		public string Ip { get; set; }		
		public string Username { get; set; }
		public string Password { get; set; }
		public string Vendor { get; set; }
		public string Port { get; set; }
		public string Config { get; set; }
		public int Priority { get; set; }

	}

	public class ContentInfoInput
	{
		public string? Font { get; set; }
		public string? FontSize { get; set; }
		public string? Color { get; set; }
		public string? BackColor { get; set; }
		public int? Priority { get; set; }
		public string Url { get; set; }
		public int Brightness { get; set; }
		public string Content1 { get; set; }
		public string Content2 { get; set; }
		public string ThreadId { get; set; }
		public string? IncidentId { get; set; }
		public string? PlayMode { get; set; } = "override";
		public int? TimeToLive { get; set; }

	}

	public class ProcessInfoInput
	{
		public string ProcessType { get; set; }
		public string ActionType { get; set; }
		public int TimeToLive { get; set; }
		public string? PlayMode { get; set; } = "override";
		public string ExecuteBy { get; set; }
	}

	public class EventInfoInput :  EventArgs
	{
		public string EventId { get; set; }
		public string EquipmentId { get; set; }
	}
}
