using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [SugarTable("TmsIncident")]
    public class TmsIncident : EntityTenant
    {
        //TmsIncident {
        //ID varchar(64) pk
        //Code string(64) null
        //Name string(64) null
        //EventTypeId varchar(64) null > TmsEventType.ID
        //StartDate datetime null
        //Source string(16) null def(0)
        //Location string(16) null def(0)
        //ZoneId varchar(1024) null >* TmsZone.ID
        //KmNumber integer null
        //MetNumber integer null
        //InfluenceScope string(16) null def(0)
        //InjuredNumber integer null
        //VehicleNumber integer null
        //CreatedBy varchar(64) null > SysUser.Username
        //CreatedDate datetime null
        //ValidatedBy varchar(64) null > SysUser.Username
        //ValidatedDate datetime null
        //ApprovedBy varchar(64) > SysUser.Username
        //ApprovedDate datetime null
        //FinishedBy varchar(64) null > SysUser.Username
        //FinishedDate datetime null
        //TrafficPlanId varchar(64) null > TmsTrafficPlan.ID
        //EquipmentId varchar(1024) null >* TmsEquipment.ID
        //State varchar(16) null def(1)
        //Description string(512) null
        //Remark string(256) null, including policy cancellation reasons
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)] //2026/06/22: Tăng từ 64 -> 128 để lưu trữ
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EventTypeId { get; set; }

        /// <summary>Nhóm event/incident được lưu khi tạo; null dành cho dữ liệu legacy.</summary>
        [SugarColumn(IsNullable = true, Length = 16)]
        public string? EventGroup { get; set; }


        [SugarColumn(IsNullable = true)]
        public DateTime? StartDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Source { get; set; } = "0";

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Location { get; set; } = "0";

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? ZoneId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? KmNumber { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? MetNumber { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? InfluenceScope { get; set; } = "0";

        [SugarColumn(IsNullable = true)]
        public int? InjuredNumber { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? VehicleNumber { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? CreatedBy { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? CreatedDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ValidatedBy { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ValidatedDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ApprovedBy { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ApprovedDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? FinishedBy { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? FinishedDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? TrafficPlanId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? State { get; set; } = "1";

        /// <summary>
        /// Author: SonTH
        /// Description: Chế độ xử lý sự cố: manual / auto / auto-failed / auto-canceled.
        ///              Xem IncidentConst.ProcessMode.
        ///
        ///              Là dấu vết còn lại ở cấp sự cố sau khi bảng TmsIncidentAutomationRun bị job dọn
        ///              định kỳ xoá. Luồng thủ công không cần đọc trường này, nên mặc định "manual".
        /// Created date: 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "manual", ColumnDescription = "Chế độ xử lý: manual/auto/auto-failed/auto-canceled")]
        public string ProcessMode { get; set; } = "manual";

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Description { get; set; }

        /// <summary>
        /// Thông tin phụ dạng JSON, ví dụ:
        /// <c>{"VehiclePlate":"51H-123.45","VehicleSpeed":68,"VehicleLenght":4.75,"VehicleHeight":1.68}</c>.
        /// Thêm khoá mới khi cần, không phải thêm cột.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Metadata { get; set; }

        [SugarColumn(IsNullable = true, Length = Modules.TMS.Core.Constants.IncidentRemarkConst.MaxLength)]
        public string? Remark { get; set; }

        /// <summary>
        /// Sự kiện sinh ra sự cố này (TmsEventData.ID) — chỉ có khi sự cố do VDS sinh tự động;
        /// null với sự cố người dùng tạo từ web. Đồng thời là khoá chống trùng của sự cố tự động.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EventId { get; set; }

        /// <summary>
        /// Author: KienLT
        /// Description: Khoá ĐIỀU KIỆN của sự cố tự động — hash của (thiết bị phát hiện, loại bản tin,
        /// đối tượng: biển số / làn / chiều vi phạm). null với sự cố người dùng tạo từ web.
        ///
        /// <para>Khác <see cref="EventId"/>: <c>EventId</c> định danh MỘT BẢN TIN (mỗi bản tin một giá trị,
        /// dùng cho ràng buộc UNIQUE chống replay). <c>ConditionKey</c> định danh MỘT SỰ VIỆC — mọi bản tin
        /// của cùng đám cháy tại cùng camera đều có chung giá trị này.</para>
        ///
        /// <para>VDS dùng cột này để chống trùng khi tạo sự cố: có sự cố cùng <c>ConditionKey</c> mà
        /// <see cref="State"/> còn MỞ thì bản tin mới không tạo sự cố thứ hai. Trước 10/09/2026 việc này
        /// dựa vào cache trong tiến trình nên không thấy được trạng thái — sự cố đã kết thúc vẫn chặn bản
        /// tin mới cho tới khi hết cửa sổ.</para>
        /// Created date: 10/09/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ConditionKey { get; set; }
    }
}
