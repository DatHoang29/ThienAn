namespace Module.CCTV.Controllers.CctvDevices.Dto
{
    /// <summary>Input để trigger đồng bộ thiết bị từ NxVMS về DB.</summary>
    public class CctvGetDeviceInput
    {
    }

    /// <summary>
    /// Input để lấy danh sách thiết bị đã sync từ DB.
    /// Filter theo Status (mã ITS int) thay vì DeviceState (string NxVMS)
    /// vì DB lưu Status là int sau khi map.
    /// </summary>
    public class CctvGetDevicesListInput
    {
        /// <summary>Lọc chính xác theo DeviceId (NxVMS GUID). Ưu tiên hơn Ip.</summary>
        public string? DeviceId { get; set; }
        public string? Ip { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>Input để lấy danh sách camera tổng hợp ITS + NxVMS.</summary>
    public class CctvGetDeviceViewInput
    {
        /// <summary>Lọc chính xác theo DeviceId (NxVMS GUID). Ưu tiên hơn Ip.</summary>
        public string? DeviceId { get; set; }
        public string? Ip { get; set; }
    }

    public class CctvDeviceActiveInput
    {
        /// <summary>ID bản ghi thiết bị (khóa chính). Bỏ trống nếu định danh bằng Ip.</summary>
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự tra bản ghi theo Ip.</summary>
        public string? Ip { get; set; }
    }

    public class CctvDeleteDeviceInput
    {
        /// <summary>ID bản ghi thiết bị (khóa chính). Bỏ trống nếu định danh bằng Ip.</summary>
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự tra bản ghi theo Ip.</summary>
        public string? Ip { get; set; }
    }
}