namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Description: Định danh loại gói tin (PackageType) cho NATS Envelope (phân biệt request Control vs response ControlResponse)
    /// Created date: 12/09/2026
    /// </summary>
    public static class VwPackageType
    {
        public const string Control = "Control";
        public const string ControlResponse = "ControlResponse";
    }

    /// <summary>
    /// Description: Discriminator cho các loại sự kiện trên kênh NATS Data (Worker -> FE trực tiếp)
    /// Created date: 12/09/2026
    /// </summary>
    public static class VwStatusEventType
    {
        public const string SceneActivated = "SceneActivated";
        public const string HardwareOutOfSync = "HardwareOutOfSync";
        public const string DeviceStatus = "DeviceStatus";
        public const string DeviceHeartbeat = "DeviceHeartbeat";
        public const string DeviceProbeCompleted = "DeviceProbeCompleted";
        public const string OutputsSynced = "OutputsSynced";
        public const string ScreensSynced = "ScreensSynced";
        public const string WallOutputsApplied = "WallOutputsApplied";
        public const string ScreenPowerChanged = "ScreenPowerChanged";
        public const string ScreenBacklightChanged = "ScreenBacklightChanged";
        public const string ScreenStatusRead = "ScreenStatusRead";
        public const string SourceSynced = "SourceSynced";
    }
}
