using System.Net;

namespace Tests.VideoWall.Mocks
{
    public partial class VwISAPIServerHikvisionMock
    {
        // ─── Cờ điều khiển kịch bản giả lập (Dynamic Behavior Flags) ───
        public bool RequireDigestAuth { get; set; } = true;
        public bool SimulateChallengeWithoutAlgorithm { get; set; }
        public bool SimulateDualChallengeHeader { get; set; }
        public bool VerifyDigestResponseHash { get; set; }
        public bool SimulateAddWindowWithoutId { get; set; }
        public string? LastIssuedMd5Nonce { get; private set; }
        public string? LastReceivedAuthNonce { get; private set; }
        public bool SimulateDeviceFailure { get; set; }
        public string? SimulateFailureControllerId { get; set; }
        public HashSet<int> SimulateFailurePorts { get; } = [];
        public int FailedAuthLockoutThreshold { get; set; } = 0;
        public int ConsecutiveFailedAuthCount { get; private set; }
        public bool IsLockedOut { get; set; }
        public bool SimulateNonceExpiry { get; set; }
        public int NonceExpiryTriggerCount { get; set; } = 0;
        public bool IsSupportScene { get; set; } = true;
        public bool IsCascadeCenter { get; set; } = false;
        public int CascadeGridCols { get; set; } = 8;
        public int CascadeGridRows { get; set; } = 4;
        public int CascadeOutputSize { get; set; } = 1920;
        public bool SimulateSaveData403Once { get; set; } = false;
        public bool SimulateWindowInputChannelInvalid { get; set; } = false;
        public int TransientErrorCount { get; set; } = 0;
        public int TransientErrorCalls { get; set; } = 0;
        public HttpStatusCode TransientStatusCode { get; set; } = HttpStatusCode.ServiceUnavailable;
        public int TotalReceivedRequests { get; set; } = 0;

        public bool SimulateSaveDataFailure { get; set; }
        public bool SimulateMethodNotAllowed { get; set; }
        public bool SimulateBadXmlFormat { get; set; }
        public bool SimulateBadParameters { get; set; }
        public bool SimulateInvalidOperation { get; set; }
        public bool ScreenCtrlCloseAllThrowsInvalidOperation { get; set; } = true;
        public bool SimulateNoBoundWall { get; set; }
        public bool SimulateMultipleBoundWalls { get; set; }
        public bool SimulateWall1Unbound { get; set; }
        public bool SimulateWall2Unbound { get; set; }
        public bool SimulateUnreachable { get; set; }
        public HashSet<int> SimulateUnreachablePorts { get; } = [];
        public bool SimulateMalformedXmlResponse { get; set; }

        // ─── Bộ đếm số lần gọi API ───
        public int UserCheckCallCount { get; private set; }
        public int GetCapabilitiesCallCount { get; private set; }
        public int GetOutputsCallCount { get; private set; }

        public int GetWindowsCallCount { get; private set; }
        public int GetActiveSceneCallCount { get; private set; }
        public int SaveSceneDataCallCount { get; private set; }
        public int ActivateSceneCallCount { get; private set; }
        public int AddWindowCallCount { get; private set; }
        public int UpdateWindowCallCount { get; private set; }
        public int DeleteWindowCallCount { get; private set; }
        public int DeleteAllWindowsCallCount { get; private set; }
        public int StartDynamicDecodeCallCount { get; private set; }
        public int StopDynamicDecodeCallCount { get; private set; }
        public int SwitchSourceCallCount { get; private set; }
        public int WindowTopCallCount { get; private set; }
        public int WindowBottomCallCount { get; private set; }
        public int GetInputChannelsCallCount { get; private set; }
        public int GetOutputChannelsCallCount { get; private set; }
        public int GetVideoWallsCallCount { get; private set; }
        public string? LastReceivedContentType { get; private set; }

        /// <summary>Body thô của request gần nhất (trừ transData vì route đó tự đọc InputStream).</summary>
        public byte[]? LastReceivedBodyBytes { get; private set; }

        /// <summary>Body gần nhất đã giải mã UTF-8, tiện cho assert nội dung.</summary>
        public string? LastReceivedBody { get; private set; }
        public List<string> ReceivedRequests { get; } = [];

        /// <summary>
        /// Author: Đạt
        /// Description: Đưa toàn bộ cấu hình giả lập, cờ điều khiển và bộ đếm về trạng thái mặc định ban đầu
        /// Created date: 17/08/2026
        /// </summary>
        public void ResetDefaults()
        {
            RequireDigestAuth = true;
            SimulateChallengeWithoutAlgorithm = false;
            SimulateDualChallengeHeader = false;
            VerifyDigestResponseHash = false;
            SimulateAddWindowWithoutId = false;
            LastIssuedMd5Nonce = null;
            LastReceivedAuthNonce = null;
            SimulateDeviceFailure = false;
            SimulateFailureControllerId = null;
            SimulateFailurePorts.Clear();
            FailedAuthLockoutThreshold = 0;
            ConsecutiveFailedAuthCount = 0;
            IsLockedOut = false;
            SimulateNonceExpiry = false;
            NonceExpiryTriggerCount = 0;
            IsSupportScene = true;
            IsCascadeCenter = false;
            CascadeGridCols = 8;
            CascadeGridRows = 4;
            CascadeOutputSize = 1920;
            SimulateSaveData403Once = false;
            SimulateWindowInputChannelInvalid = false;
            TransientErrorCount = 0;
            TransientErrorCalls = 0;
            TransientStatusCode = HttpStatusCode.ServiceUnavailable;
            TotalReceivedRequests = 0;
            MaxSceneNums = DefaultMaxSceneNums;
            ActiveSceneId = 1;
            WallSceneStores.Clear();
            WallSceneStores[1] = new()
            {
                [1] = "Kịch bản 1: Giám sát Toàn tuyến Hữu Nghị - Chi Lăng (12 Màn 1:1)",
                [2] = "Kịch bản 2: Sự cố Trọng điểm (Khối lớn 2×2 + 8 Ô phụ)",
                [3] = "Kịch bản 3: Giám sát Song song 2 Địa bàn (2 Khối 2×2 + 4 Ô Trạm thu phí)",
                [4] = "Kịch bản 4: Bản đồ GIS / Dashboard Toàn cảnh (Toàn tường 4×3)",
            };
            WallSceneStores[2] = new()
            {
                [1] = "Kịch bản 1: Giám sát Toàn tuyến Hữu Nghị - Chi Lăng (12 Màn 1:1)",
                [2] = "Kịch bản 2: Sự cố Trọng điểm (Khối lớn 2×2 + 8 Ô phụ)",
                [3] = "Kịch bản 3: Giám sát Song song 2 Địa bàn (2 Khối 2×2 + 4 Ô Trạm thu phí)",
                [4] = "Kịch bản 4: Bản đồ GIS / Dashboard Toàn cảnh (Toàn tường 4×3)",
            };
            NextSceneId = 5;
            PlanStore.Clear();
            PlanStore[1] = "Default Plan";
            NextPlanId = 2;
            ActivePlanId = 1;
            SimulateSaveDataFailure = false;
            SimulateMethodNotAllowed = false;
            SimulateBadXmlFormat = false;
            SimulateBadParameters = false;
            SimulateInvalidOperation = false;
            ScreenCtrlCloseAllThrowsInvalidOperation = true;
            SimulateNoBoundWall = false;
            SimulateMultipleBoundWalls = false;
            SimulateWall1Unbound = false;
            SimulateWall2Unbound = false;
            SimulateUnreachable = false;
            SimulateUnreachablePorts.Clear();
            SimulateMalformedXmlResponse = false;

            UserCheckCallCount = 0;
            GetCapabilitiesCallCount = 0;
            GetOutputsCallCount = 0;
            GetWindowsCallCount = 0;
            GetActiveSceneCallCount = 0;
            SaveSceneDataCallCount = 0;
            ActivateSceneCallCount = 0;
            AddWindowCallCount = 0;
            UpdateWindowCallCount = 0;
            DeleteWindowCallCount = 0;
            DeleteAllWindowsCallCount = 0;
            StartDynamicDecodeCallCount = 0;
            StopDynamicDecodeCallCount = 0;
            SwitchSourceCallCount = 0;
            WindowTopCallCount = 0;
            WindowBottomCallCount = 0;
            GetInputChannelsCallCount = 0;
            GetOutputChannelsCallCount = 0;
            GetVideoWallsCallCount = 0;
            NotConnectedOutputChannels.Clear();
            LastReceivedContentType = null;
            LastReceivedBodyBytes = null;
            LastReceivedBody = null;
            ReceivedRequests.Clear();
        }
    }
}
