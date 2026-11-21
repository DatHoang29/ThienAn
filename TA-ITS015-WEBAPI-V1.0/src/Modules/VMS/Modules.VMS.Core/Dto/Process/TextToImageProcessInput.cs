using Modules.VMS.Controllers.Control.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Modules.VMS.Controllers.Process.Dto
{
	public class TextToImageProcessInput
	{
		public List<string> EquipmentIds { get; set; } = new List<string>();
		public TextToImageInput TextToImageInput { get; set; } = new TextToImageInput();
        /// <summary>
        /// Actor thực hiện — để null khi đến từ HTTP request (handler tự đọc JWT).
        /// Set = IncidentAutomationConst.SystemActor khi đến từ background automation.
        /// </summary>
        public string? ExecuteBy { get; set; }
	}
}
