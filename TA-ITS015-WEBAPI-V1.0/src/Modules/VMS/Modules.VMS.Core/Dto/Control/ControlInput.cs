using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.VMS.Controllers.Control.Dtos
{

	/// <summary>
	/// Thêm mới hành động điều khiển thiết bị VMS
	/// </summary>
	public class AddControlInput
	{
		public string? ControlType { get; set; } //text,image,time,schedule,...
		public string? ActionType { get; set; } //start,stop,pending,....
		public string? DeviceType { get; set; } // Thêm loại xử lý của thiết bị VMS hoặc LCS
		public string? Content1 { get; set; }
		public string? Content2 { get; set; }
		public string? Color { get; set; }
		public string? BackColor { get; set; }
		public string? Font { get; set; }
		public string? FontSize { get; set; }
		public int? Brightness { get; set; }
		public int Priority { get; set; }
		public string? Url { get; set; }
		public string? Size { get; set; }
        public string? JsonContent { get; set; }   // THÊM: JSON template đã chỉnh sửa
        public string? IncidentId { get; set; }
        /// <summary>
        /// Chế độ phát khi có sự cố: "override" (ghi đè) hoặc "append" (nối tiếp). Mặc định "override".
        /// </summary>
        public string? PlayMode { get; set; } = "override";

        /// <summary>
        /// Thời gian hiển thị (giây) khi phát nối tiếp trong vòng lặp sự cố
        /// </summary>
        public int? TimeToLive { get; set; }

        /// <summary>
        /// Người/actor thực hiện lệnh điều khiển.
        /// - HTTP request (manual): để null → handler tự đọc từ UserManager.Account (JWT).
        /// - Background automation:  set = IncidentAutomationConst.SystemActor ("SYSTEM_AUTOMATION")
        ///   để tránh NullReferenceException khi HttpContext không tồn tại trong background context.
        /// </summary>
        public string? ExecuteBy { get; set; }
		public List<ControlEquipmentInput> EquipmentList { get; set; } = new();

		#region Timer
		public DateTime? TimeStart { get; set; }
		public DateTime? TimeEnd { get; set; }
		#endregion

		#region Schedule
		public List<ControlScheduleInput> ScheduleList { get; set; } = new List<ControlScheduleInput>();
		#endregion

	}

	public class ControlEquipmentInput
	{
		public string ID { get; set; }
		public string Code { get; set; }
		public string Name { get; set; }
	}
			


	public class ControlScheduleInput
	{
		public string? Url { get; set; }
		public int? TimeToLive { get; set; }
	}


	public class TextToImageInput
	{
		public string? Code { get; set; }
		public string? Content1 { get; set; } = "";
		public string? Content2 { get; set; } = "";
		public string? Content3 { get; set; } = "";

		public string? BackColor { get; set; }
		public string? Font { get; set; }
		public string? FontSize { get; set; }

		public string? Color { get; set; }

		public string? Size { get; set; }

		public string? Height { get; set; }

		public int Brightness { get; set; }
        public int? Priority { get; set; }
		public string? IncidentId { get; set; }
        /// <summary>
        /// Chế độ phát khi có sự cố: "override" (ghi đè) hoặc "append" (nối tiếp)
        /// </summary>
        public string? PlayMode { get; set; } = "override";

        /// <summary>
        /// Thời gian hiển thị (giây) khi phát nối tiếp trong vòng lặp sự cố
        /// </summary>
        public int? TimeToLive { get; set; }

	}

    /// <summary>
    /// Author: ChuongLP
    /// Description: Input sinh ảnh preview VMS (không lưu file)
    /// </summary>
    public class PreviewInput
    {
        public string? ControlType { get; set; } // text | message | phrase | template
        public string? Size { get; set; }        // "384x64"
        public string? Content1 { get; set; }
        public string? Content2 { get; set; }
        public string? Color { get; set; }
        public string? BackColor { get; set; }
        public string? Font { get; set; }
        public string? FontSize { get; set; }
        public string? JsonContent { get; set; } // template (jsonData)
        public int? Brightness { get; set; }      // 0..100
    }


}
