namespace ITS.VideoWall.Core.Dto.ISAPI
{
    /// <summary>
    /// Description: Trạng thái lỗi xác thực (Device Auth Failure) cho 1 thiết bị Video Wall theo IP.
    /// Created date: 13/09/2026
    /// </summary>
    public class VwDeviceAuthFailureState
    {
        public int FailureCount { get; set; }
        public long BlockedUntilTicks { get; set; }
    }
}
