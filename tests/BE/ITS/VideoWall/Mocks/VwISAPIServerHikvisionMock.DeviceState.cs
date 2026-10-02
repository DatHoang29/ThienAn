namespace Tests.VideoWall.Mocks
{
    public partial class VwISAPIServerHikvisionMock
    {
        /// <summary>
        /// Trần SID kịch bản đo thật trên DS-C66S-H88-CL (09B-practical-guide §203).
        /// Test nào cần kiểm nhánh chặn SID thì hạ <see cref="MaxSceneNums"/> xuống, đừng đổi hằng này.
        /// </summary>
        public const int DefaultMaxSceneNums = 128;

        /// <summary>
        /// Trần SID kịch bản thiết bị nhận, trả về trong &lt;maxSceneNums&gt; của VideoWallCap.
        ///
        /// Mặc định 128 = số ĐO THẬT trên DS-C66S-H88-CL (09B-practical-guide §202-203:
        /// "isSupportScene: true (maxSceneNums: 128)"). Giữ đúng số thật để mọi test hiện có không
        /// đổi hành vi; bài nào cần kiểm nhánh chặn SID thì tự hạ giá trị này xuống.
        ///
        /// Trước khi có field này, mock KHÔNG trả maxSceneNums nên nó luôn deserialize ra 0, mà
        /// VwISAPIDeviceService.DeviceSetup kiểm SID bằng `if (output.MaxSceneNums > 0 && ...)` —
        /// tức nhánh chặn SID sai khoảng KHÔNG THỂ chạm tới được qua mock.
        /// </summary>
        public int MaxSceneNums { get; set; } = DefaultMaxSceneNums;

        public Dictionary<int, Dictionary<int, string>> WallSceneStores { get; } = new()
        {
            [1] = new()
            {
                [1] = "Kịch bản 1: Giám sát Toàn tuyến Hữu Nghị - Chi Lăng (12 Màn 1:1)",
                [2] = "Kịch bản 2: Sự cố Trọng điểm (Khối lớn 2×2 + 8 Ô phụ)",
                [3] = "Kịch bản 3: Giám sát Song song 2 Địa bàn (2 Khối 2×2 + 4 Ô Trạm thu phí)",
                [4] = "Kịch bản 4: Bản đồ GIS / Dashboard Toàn cảnh (Toàn tường 4×3)",
            },
            [2] = new()
            {
                [1] = "Kịch bản 1: Giám sát Toàn tuyến Hữu Nghị - Chi Lăng (12 Màn 1:1)",
                [2] = "Kịch bản 2: Sự cố Trọng điểm (Khối lớn 2×2 + 8 Ô phụ)",
                [3] = "Kịch bản 3: Giám sát Song song 2 Địa bàn (2 Khối 2×2 + 4 Ô Trạm thu phí)",
                [4] = "Kịch bản 4: Bản đồ GIS / Dashboard Toàn cảnh (Toàn tường 4×3)",
            }
        };

        public Dictionary<int, string> SceneStore => GetSceneStore(1);
        public int NextSceneId { get; set; } = 5;

        public Dictionary<int, string> GetSceneStore(int wallNo)
        {
            if (!WallSceneStores.TryGetValue(wallNo, out var store))
            {
                store = new Dictionary<int, string> { [1] = "Default Scene" };
                WallSceneStores[wallNo] = store;
            }
            return store;
        }

        public Dictionary<int, string> PlanStore { get; } = new() { [1] = "Default Plan" };
        public int NextPlanId { get; set; } = 2;
        public int? ActivePlanId { get; set; } = 1;

        public HashSet<int> ValidOutputChannelIds { get; } = [
            17235971, 17235972, 17235973, 17235974,
            17235975, 17235976, 17235977, 17235978,
            17235979, 17235980, 17235981, 17235982
        ];

        public bool IsValidOutputChannel(string? channelIdStr, out int channelId)
        {
            if (int.TryParse(channelIdStr, out channelId) && ValidOutputChannelIds.Contains(channelId))
                return true;
            channelId = 0;
            return false;
        }

        public int ActiveSceneId { get; set; } = 1;
        public HashSet<int> NotConnectedOutputChannels { get; } = [];

        public string GetWallBindStatus(string wallId)
        {
            if (SimulateNoBoundWall)
                return "unbound";
            if (wallId == "1")
                return SimulateWall1Unbound ? "unbound" : "bound";
            if (wallId == "2")
            {
                if (SimulateWall2Unbound)
                    return "unbound";
                if (SimulateWall1Unbound || SimulateMultipleBoundWalls)
                    return "bound";
                return "unbound";
            }
            return "bound";
        }
    }
}
