namespace Module.Integration.CCTV.Core.Dto
{
    /// <summary>
    /// DTO chung truyền vào BaseCctvService — dùng prefix "Vms" để tránh
    /// xung đột tên với DTO tầng CCTV Controller (CctvMediaUrlInput, CctvPtzPositionInput...).
    /// </summary>

    public class VmsMediaUrlInput
    {
        public string? DeviceId { get; set; }
        public string? Stream { get; set; }
        public string? Resolution { get; set; }
        public DateTime? FromDate { get; set; }   // trước: string?? FromDate
        public DateTime? ToDate { get; set; }
        public bool Download { get; set; } = false;
    }

    public class VmsWebRtcUrlInput
    {
        public string? DeviceId { get; set; }
        public string? Stream { get; set; } = "primary";
        public string? Resolution { get; set; } = "1920x1080";
        public DateTime? FromDate { get; set; }
    }

    public class VmsPtzPositionInput
    {
        public string? DeviceId { get; set; }
        public decimal Pan { get; set; }
        public decimal Tilt { get; set; }
        public decimal Zoom { get; set; }
        public decimal Focus { get; set; }
    }

    public class VmsPtzMoveInput
    {
        public string? DeviceId { get; set; }
        public decimal Pan { get; set; }
        public decimal Tilt { get; set; }
        public decimal Zoom { get; set; }
        public decimal Focus { get; set; }  
    }

    public class VmsActivatePresetInput
    {
        public string? DeviceId { get; set; }
        public string? PresetId { get; set; }
        public decimal? Speed { get; set; }
    }

    public class VmsActivateTourInput
    {
        public string? DeviceId { get; set; }
        public string? TourId { get; set; }
    }

    public class VmsPtzGetPresetsInput
    {
        public string? DeviceId { get; set; }
    }

    public class VmsDeletePresetInput
    {
        public string? DeviceId { get; set; }
        public string? PresetId { get; set; }
    }

    public class VmsGetToursInput
    {
        public string? DeviceId { get; set; }
    }

    public class VmsMediaDownloadInput
    {
        public string? DeviceId { get; set; }
        public string? Stream { get; set; } = "primary";
        public string? Resolution { get; set; } = "1920x1080";
        public DateTime FromDate { get; set; }    // trước: long FromDate
        public DateTime ToDate { get; set; }
        public string? VideoCodec { get; set; } = "h264";
    }

    public class VmsSnapshotInput
    {
        public string? DeviceId { get; set; }
        public DateTime? FromDate { get; set; }
        public string? Size { get; set; }
        public string? Format { get; set; } = "jpg";
        public string? Crop { get; set; }
        public int? Rotation { get; set; }
    }

    /// <summary>
    /// Input chung tầng Core cho footage. Thời gian đã là Unix ms.
    /// Author: ChuongLP
    /// Date: 2026-06-17
    /// </summary>
    public class VmsFootageInput
    {
        public string? DeviceId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? DetailLevelMs { get; set; }
        public string? PeriodType { get; set; }
    }

}
