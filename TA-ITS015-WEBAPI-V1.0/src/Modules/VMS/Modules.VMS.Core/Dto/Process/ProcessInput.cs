using Modules.VMS.Controllers.Control.Dtos;
using Modules.VMS.Controllers.ProcessImg.Dto;
using Modules.VMS.Core.Entities;
using Shared.Core.Domain;
using Shared.Core.Utilities;
using Shared.DTO.Dtos.Tms;


namespace Modules.VMS.Controllers.Process.Dto
{

    public class tmsSignalLogResponse : EntityTenant
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

        public string Status { get; set; }

        public int? Priority { get; set; }

        public string? ExecuteBy { get; set; }

        public DateTime? ExecuteTime { get; set; }

        public string? Remark { get; set; }
        public string? IncidentId { get; set; }
    }



	/// <summary>
	/// Author: TuanHA
	/// Description: Đối tượng lấy thông tin VmsProcess và VmsProcessImg cho modal VMS
	/// Created date: 06/02/2025
	/// </summary>
	/// 
	public class InformationProcessInput
	{
		public short DefaultValue { get; set; }
	}

	public class InformationProcessOutput
	{
		public string EquipmentId { get; set; }
		public string? ControlType { get; set; } //text,image,time,schedule,...
		public string? ActionType { get; set; } //start,stop,pending,....
		public string? Content1 { get; set; }
		public string? Content2 { get; set; }
		public string? Color { get; set; }
		public string? BackColor { get; set; }
		public string? Font { get; set; }
		public string? FontSize { get; set; }
		public int? Brightness { get; set; }
		public int Priority { get; set; }
		public string? Url { get; set; }

		#region Timer
		public DateTime? TimeStart { get; set; }
		public DateTime? TimeEnd { get; set; }
		#endregion

		#region Schedule
		public List<ControlScheduleInput> ScheduleList { get; set; } = new List<ControlScheduleInput>();
		#endregion
	}



	#region lập lịch

	/// <summary>
	/// Author: TuanHA
	/// Description: Tạo đối tượng dto dùng cho hàm ChangeVmsCurrent lập lịch
	/// Created date: 06/02/2025
	/// </summary>
	/// 
	public class ChangeVmsCurrentInput
	{
		public RunThreadInput RunThreadInput { get; set; }
		public string ImageUrl { get; set; }
		public int Timetolive { get; set; }
		public string Rowdata { get; set; }
	}

	/// <summary>
	/// Author: TuanHA
	/// Description: Đối tượng lấy thông tin trạng thái của các tiến trình VMS
	/// Created date: 13/03/2025
	/// </summary>
	/// 

	public class ProcessStatusInput
	{
		public short DefaultValue { get; set; }
	}

	public class ProcessStatusOutput
	{
		public string VmsId { get; set; }
		public bool Status { get; set; }
	}

	public class RestartProcessScheduleInput
	{
		public List<VmsProcess> ListProcess { get; set; } = new List<VmsProcess>();
		public List<VmsProcessImg> ListProcessImg { get; set; } = new List<VmsProcessImg>();
	}
	#endregion

	#region
	/// <summary>
	/// Author: TuanHA
	/// Description: Đối tượng gửi lại nội dung cho thiết bị
	/// Created date: 26/03/2025
	/// </summary>
	public class ResendToDeviceInput 
	{
		public List<VmsProcess> ListProcess { get; set; } = new List<VmsProcess>();
		public List<VmsCurrent> ListCurrent { get; set; } = new List<VmsCurrent>();
	}
	#endregion

	#region Incident Playlist DTOs
	/// <summary>
	/// Author: ChuongLP
	/// Description: DTO truy vấn playlist hình ảnh đang hiển thị/append trên thiết bị
	/// Created date: 2026-10-15
	/// </summary>
	public class GetActivePlaylistInput
	{
		public List<string> EquipmentIds { get; set; } = new List<string>();
	}

	public class EquipmentPlaylistOutput
	{
		public string EquipmentId { get; set; }
		public string? EquipmentCode { get; set; }
		public int MaxPlaylistItems { get; set; } = 5;
		public List<PlaylistItemOutput> Items { get; set; } = new List<PlaylistItemOutput>();
	}

	public class PlaylistItemOutput
	{
		public string? Id { get; set; }
		public string Url { get; set; }
		public int TimeToLive { get; set; }
		public string? IncidentId { get; set; }
		public bool IsIncident { get; set; }
		public int OrderNo { get; set; }
	}
	#endregion
}
