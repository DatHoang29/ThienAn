using Modules.VMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.VMS.Controllers.Process.Dto
{
	/// <summary>
	/// Description: Đối tượng chuẩn bị dữ liệu gửi cho thiết bị
	///Author: TuanHA
	///Create Date:
	///</summary>
	public class DataSendToDevice
	{
		public EquipmentInfoInput EquipmentInfoInput { get; set; } = new EquipmentInfoInput();		
		public int Brightness { get; set; }
		public string Url { get; set; }
	}

}
