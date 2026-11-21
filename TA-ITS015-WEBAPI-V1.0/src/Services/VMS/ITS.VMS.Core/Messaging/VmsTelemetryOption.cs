namespace ITS.VMS.Core.Messaging
{
    /// <summary>
    /// Ánh xạ mục "Telemetry" trong appsettings — cấu hình vòng lặp tự thu thập thông số thiết bị.
    /// Tách khỏi <see cref="VmsControlNatsOption"/> (mục "Nats") để phần điều khiển và phần telemetry
    /// đọc/sửa độc lập, tắt telemetry không ảnh hưởng luồng điều khiển.
    /// Author: ChuongLP
    /// Created date: 23/09/2026
    /// </summary>
    public class VmsTelemetryOption
    {
        public const string SectionName = "Telemetry";

        /// <summary>Bật/tắt toàn bộ luồng tự thu thập telemetry ở dịch vụ.</summary>
        public bool Enabled { get; set; } = true;

        // ===== Kênh dữ liệu =====

        /// <summary>
        /// Kênh dùng chung cho cả 3 loại gói (hỏi thiết bị / trả thiết bị / đẩy telemetry),
        /// phân biệt bằng trường Type trong gói tin.
        ///
        /// LƯU Ý: kênh này BE cũng dùng để bắn vms-success/vms-fail cho FE. Mọi bên nhận đều lọc
        /// theo Type nên không xử lý lẫn nhau. Muốn tách riêng cho đỡ nhiễu thì đổi giá trị ở đây
        /// và đổi tương ứng hằng bên BE.
        /// </summary>
        public string DataSubject { get; set; } = VmsTelemetrySubject.Data;

        /// <summary>Loại thiết bị cần đọc. Để trống thì BE tự quyết theo mặc định của nó.</summary>
        public string[]? DeviceTypeCodes { get; set; } = { "vms", "lcs" };

        /// <summary>
        /// Chu kỳ đồng bộ lại danh sách thiết bị (phút). Đây là lưới an toàn: BE có đẩy cập nhật
        /// khi người dùng sửa thiết bị, nhưng mất gói tin thì lần sync định kỳ sẽ bù.
        /// </summary>
        public int EquipmentSyncMinutes { get; set; } = 30;

        /// <summary>Số giây chờ BE trả danh sách thiết bị.</summary>
        public int EquipmentSyncTimeoutSeconds { get; set; } = 30;

        /// <summary>File lưu danh sách thiết bị (tương đối tính từ thư mục chạy exe).</summary>
        public string EquipmentFile { get; set; } = "State/vms_equipment.json";

        // ===== Vòng lặp đọc thông số =====

        /// <summary>Chu kỳ đọc thông số thiết bị (phút).</summary>
        public int CollectIntervalMinutes { get; set; } = 15;

        /// <summary>
        /// Danh sách thông số cần đọc (IntgVmsStatType). Để trống thì đọc tất cả ("all").
        /// Đọc ít thông số thì mỗi thiết bị nhanh hơn.
        /// </summary>
        public string[]? StatTypes { get; set; }

        /// <summary>Số thiết bị đọc đồng thời trong một lượt quét.</summary>
        public int MaxParallelDevice { get; set; } = 4;

        /// <summary>
        /// Số bản ghi tối đa của một gói đẩy về BE. Gói quá lớn dễ vượt max_payload của NATS,
        /// nên lượt quét nhiều thiết bị sẽ được chia thành nhiều gói.
        /// </summary>
        public int PushBatchSize { get; set; } = 50;

        // ===== Bộ đệm khi BE không nhận được =====

        /// <summary>File đệm các gói chưa đẩy được (tương đối tính từ thư mục chạy exe).</summary>
        public string BufferFile { get; set; } = "State/vms_telemetry_buffer.json";

        /// <summary>
        /// Số gói tối đa giữ trong bộ đệm. Vượt thì bỏ gói CŨ NHẤT — telemetry mới có giá trị hơn,
        /// và phải có trần để BE chết lâu ngày không làm phình đĩa.
        /// </summary>
        public int BufferMaxPackets { get; set; } = 200;
    }
}
