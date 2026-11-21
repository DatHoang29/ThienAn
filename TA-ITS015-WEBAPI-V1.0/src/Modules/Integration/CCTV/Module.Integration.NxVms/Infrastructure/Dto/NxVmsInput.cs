using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Integration.NxVms.Infrastructure.Dto
{
    public class NxVmsMediaUrlInput
    {
        public string? DeviceId { get; set; }
        public string? Format { get; set; } = "mp4";
        public string? Stream { get; set; } = "secondary";
        public string? Resolution { get; set; } = "1920x1080";
        public long? PositionMs { get; set; }   
        public long? DurationMs { get; set; }
        public bool Download { get; set; } = false;
    }

    public class NxVmsWebRtcUrlInput
    {
        public string? DeviceId { get; set; }
        public string? Stream { get; set; } = "secondary";
        public string? Resolution { get; set; } = "1920x1080";
        
        public long? PositionMs { get; set; }   // Null = live, có giá trị = playback từ mốc đó.
    }

    public class NxVmsPtzPositionInput
    {
        public string? DeviceId { get; set; }
        public decimal Pan { get; set; }
        public decimal Tilt { get; set; }
        public decimal Zoom { get; set; }
    }

    public class NxVmsPtzMoveInput
    {
        public string? DeviceId { get; set; }
        public decimal Pan { get; set; }
        public decimal Tilt { get; set; }
        public decimal Zoom { get; set; }
    }

    public class NxVmsAddPresetInput
    {
        public string? DeviceId { get; set; }
        /// <summary>Optional — null thì NxVMS tự sinh ID.</summary>
        public string? PresetId { get; set; }
        public string? PresetName { get; set; }
    }

    public class NxVmsUpdatePresetInput
    {
        public string? DeviceId { get; set; }
        public string? PresetId { get; set; }
        public string? Name { get; set; }
    }

    public class NxVmsActivatePresetInput
    {
        public string? DeviceId { get; set; }
        public string? PresetId { get; set; }
    }

    public class NxVmsActivateTourInput
    {
        public string? DeviceId { get; set; }
        public string? TourId { get; set; }
    }

    /// <summary>
    /// Build HTTPS URL để download file video.
    /// Bắt buộc truyền positionMs + durationMs.
    /// </summary>
    public class NxVmsMediaDownloadInput
    {
        public string? DeviceId { get; set; }
        public string? Stream { get; set; } = "secondary";
        public string? Resolution { get; set; } = "1920x1080";
        public long PositionMs { get; set; }    // trước: long FromDate
        public long DurationMs { get; set; }
        public string? VideoCodec { get; set; } = "h264";
    }

    public class NxVmsSnapshotInput
    {
        public string? DeviceId { get; set; }
        public long? TimestampMs { get; set; }
        public string? Size { get; set; }
        public string? Format { get; set; } = "jpg";
        public string? Crop { get; set; }
        public int? Rotation { get; set; }
    }


    /// <summary>
    /// Input nội bộ NxVMS cho query footage. Thời gian đã là Unix ms.
    /// Author: ChuongLP
    /// Date: 2026-06-17
    /// </summary>
    public class NxVmsFootageInput
    {
        public string? DeviceId { get; set; }
        public long? StartTimeMs { get; set; }
        public long? EndTimeMs { get; set; }
        public long? DetailLevelMs { get; set; }
        public string? PeriodType { get; set; } // recording | motion | analytics
    }
}
