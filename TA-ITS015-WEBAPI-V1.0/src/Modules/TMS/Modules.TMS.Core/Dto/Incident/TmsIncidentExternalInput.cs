namespace Modules.TMS.Core.Dto.Incident
{
    /// <summary>
    /// Author: SonTH
    /// Description: DTO cho HỆ THỐNG NGOÀI tạo sự cố (VDS, báo cháy, trung tâm đối tác, dịch vụ đồng bộ...).
    ///
    ///              Cách dùng: dịch vụ gọi tham chiếu Modules.TMS + Modules.TMS.Core rồi dispatch qua
    ///              Wolverine mediator: await MessBus.InvokeAsync&lt;AddIncidentExternalOutput&gt;(input).
    ///              Handler được Wolverine tự phát hiện nhờ [assembly: WolverineModule] trong Modules.TMS,
    ///              và chạy NGAY TRONG tiến trình gọi trên cùng cơ sở dữ liệu.
    ///
    ///              Vì sao không dùng lại AddTmsIncidentInput:
    ///              - Handler thủ công đọc App.User để lấy CreatedBy, sẽ null ở tiến trình nền/worker.
    ///                Ở đây actor phải truyền tường minh qua Input.
    ///              - Source phải do phía tích hợp khai báo có kiểm soát, không nhận tuỳ ý từ payload
    ///                người dùng, vì cấu hình tự động lọc theo nguồn (AllowedSources).
    ///              - Cần khoá chống trùng khi hệ thống ngoài gọi lại sau timeout.
    /// Created date: 24/07/2026
    /// </summary>
    public class AddIncidentExternalInput
    {
        /// <summary>
        /// Loại sự kiện (TmsEventType.ID). Bắt buộc - không có thì không tra được cấu hình tự động.
        /// </summary>
        public string? EventTypeId { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Nguồn phát hiện, mã thuộc loại cấu hình tms_source_detect (vds, fa, api, user).
        ///              Quyết định sự cố có được tự động xử lý hay không (xem AllowedSources của cấu hình),
        ///              nên phía tích hợp phải gán theo danh tính của mình, đừng lấy từ dữ liệu người dùng.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Thời điểm xảy ra sự cố. BẮT BUỘC với đường tích hợp.
        ///
        ///              Lý do bắt buộc: mã sự cố sinh tự động có chứa thời điểm này tới từng giây, nên khi
        ///              hệ thống ngoài gọi lại cùng payload thì sinh ra ĐÚNG mã cũ và được nhận diện là
        ///              trùng thay vì tạo sự cố thứ hai. Nếu để trống rồi mặc định "giờ hiện tại" thì mỗi
        ///              lần gọi lại sinh mã khác và cơ chế chống trùng mất tác dụng.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Mã sự cố. Để trống thì handler tự sinh theo cấu trúc giống giao diện:
        ///              ICC_{mã loại sự kiện}_{vị trí}_{hướng}_{km}_{mét}_{ddMMyyyyHHmmss}
        ///              Đây cũng là KHOÁ CHỐNG TRÙNG: gọi lại với cùng mã sẽ trả về sự cố đã có.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Tên sự cố. Để trống thì handler sinh bản ngắn từ thông tin có sẵn.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>Vị trí trên mặt cắt ngang, mã thuộc loại cấu hình tms_zone_location.</summary>
        public string? Location { get; set; }

        /// <summary>Hướng/trục tuyến ảnh hưởng, mã thuộc loại cấu hình direction_config_type.</summary>
        public string? InfluenceScope { get; set; }

        /// <summary>Lý trình - km.</summary>
        public int? KmNumber { get; set; }

        /// <summary>Lý trình - mét.</summary>
        public int? MetNumber { get; set; }

        /// <summary>Vùng liên quan (TmsZone.ID), nhiều giá trị cách nhau bởi dấu phẩy.</summary>
        public string? ZoneId { get; set; }

        /// <summary>Thiết bị liên quan (TmsEquipment.ID), nhiều giá trị cách nhau bởi dấu phẩy.</summary>
        public string? EquipmentId { get; set; }

        /// <summary>Mô tả chi tiết.</summary>
        public string? Description { get; set; }

        /// <summary>Số người bị thương, nếu nguồn cung cấp được.</summary>
        public int? InjuredNumber { get; set; }

        /// <summary>Số xe liên quan, nếu nguồn cung cấp được.</summary>
        public int? VehicleNumber { get; set; }

        /// <summary>Ghi chú.</summary>
        public string? Remark { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: Tài khoản/định danh hệ thống thực hiện, ghi vào CreatedBy.
        ///              Bắt buộc truyền vì handler KHÔNG đọc App.User - nó phải chạy được ở tiến trình nền.
        /// </summary>
        public string? Actor { get; set; }
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Kết quả tạo sự cố từ hệ thống ngoài, kèm luôn kết quả lập lịch tự động để phía gọi
    ///              biết ngay sự cố này có được tự động xử lý hay không mà không phải truy vấn lại.
    /// Created date: 24/07/2026
    /// </summary>
    public class AddIncidentExternalOutput
    {
        /// <summary>ID sự cố đã tạo, hoặc ID sự cố đã tồn tại nếu trùng mã.</summary>
        public string? IncidentId { get; set; }

        /// <summary>Mã sự cố cuối cùng được dùng (do phía gọi cấp hoặc do handler sinh).</summary>
        public string? Code { get; set; }

        /// <summary>
        /// Author: SonTH
        /// Description: True nghĩa là mã này đã tồn tại nên KHÔNG tạo thêm sự cố mới.
        ///              Đây là kết quả THÀNH CÔNG của một lần gọi lại, không phải lỗi - phía gọi không cần
        ///              thử lại nữa. Handler cố tình không ném lỗi ở trường hợp này để hệ thống ngoài
        ///              không hiểu sai là thất bại rồi gọi lại mãi.
        /// </summary>
        public bool AlreadyExists { get; set; }

        /// <summary>Đã lập lịch tự động xử lý hay chưa.</summary>
        public bool AutomationScheduled { get; set; }

        /// <summary>
        /// Mã lý do của lần thử lập lịch tự động. Xem IncidentAutomationConst.ScheduleReason.
        /// Ví dụ NoConfig (loại sự kiện chưa bật tự động), SourceNotAllowed (nguồn không được phép).
        /// </summary>
        public string? AutomationReasonCode { get; set; }

        /// <summary>Thời điểm dự kiến tự động thực thi, nếu đã lập lịch.</summary>
        public DateTime? AutomationExecuteDueAt { get; set; }
    }
}
