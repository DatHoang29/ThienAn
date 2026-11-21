namespace Module.VideoWall.Core.Constants
{
    /// <summary>
    /// Description: Subject NATS dùng chung cho VideoWall. Chỉ còn 2 subject:
    ///              Control (BE → Worker) và Data (Worker → FE trực tiếp, broadcast realtime).
    ///              Subject Data KHÔNG còn là reply cho BE — BE không subscribe Data nữa.
    /// Created date: 11/09/2026
    /// </summary>
    public static class VwSubjects
    {
        /// <summary>BE publish lệnh điều khiển → Worker subscribe.</summary>
        public const string Control = "ta.its.data.videowall.control";

        /// <summary>Worker publish kết quả/telemetry → FE subscribe trực tiếp (BE không nghe).</summary>
        public const string Data = "ta.its.data.videowall";

        /// <summary>
        /// BE báo FE "kịch bản đang phát vừa đổi" — FE nghe qua useTransporterVideoWallScene
        /// (NatsSubject.DataVideoWallScene). Payload: { SceneId, Code, Name }.
        /// </summary>
        public const string DataScene = "ta.its.data.videowall.scene";
    }
}
