namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Description: Trạng thái cổng ra trên thiết bị ở lần đồng bộ gần nhất (VwControllerOutput.DeviceState).
    /// Created date: 06/11/2026
    /// </summary>
    public static class VwOutputDeviceState
    {
        /// <summary>Thiết bị có trả về cổng này.</summary>
        public const int Present = 1;

        /// <summary>Lần đồng bộ gần nhất không thấy cổng (giữ bản ghi vì có thể đang gắn kịch bản).</summary>
        public const int Missing = 2;

        /// <summary>Dữ liệu dựng tay khi chưa có thiết bị (script migration) — lần đồng bộ thật sẽ ghi đè theo ô.</summary>
        public const int Seed = 3;
    }
}
