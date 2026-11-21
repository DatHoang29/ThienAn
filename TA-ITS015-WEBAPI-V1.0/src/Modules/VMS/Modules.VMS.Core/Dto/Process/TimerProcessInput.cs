using Modules.VMS.Controllers.ProcessImg.Dto;
using Modules.VMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.VMS.Controllers.Process.Dto
{
	public class TimerProcessInput : VmsProcess
	{
		public List<string> EquipmentIds { get; set; }
		public string Url { get; set; }
	}
}
