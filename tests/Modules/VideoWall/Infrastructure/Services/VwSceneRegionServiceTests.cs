namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử toàn diện VwSceneRegionService:
    ///              - Tính toán vùng bao phủ (EnsureWindowInsideSceneRegion, GetCoveredControllerIds).
    ///              - Quy đổi toạ độ tuyệt đối sang toạ độ cục bộ ISAPI uniformCoordinate (1920x1920).
    ///              - Phân cắt cửa sổ vắt đa controller (SliceWindowForControllers).
    /// Created date: 17/08/2026
    /// </summary>
    [Collection("api")]
    public class VwSceneRegionServiceTests(Host host) : IDisposable
    {
        private const string TestPrefix = "TEST_VWREGION_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        // VwSceneRegionService là Scoped nên phải resolve qua scope riêng (gate module đã nằm ở constructor)
        private VwSceneRegionService GetRegionService()
            => host.Services.CreateScope().ServiceProvider.GetRequiredService<VwSceneRegionService>();

        public void Dispose()
        {
            _db.Deleteable<VwScreen>()
                .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
                .ExecuteCommand();

            _db.Deleteable<VwScene>()
                .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
                .ExecuteCommand();

            _db.Deleteable<VwController>()
                .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
                .ExecuteCommand();

            GC.SuppressFinalize(this);
        }

        #region Region Boundary & Coverage Tests

        /// <summary>
        /// Author: Đạt
        /// Description: Kịch bản toàn tường (ControllerId rỗng) cho phép đặt cửa sổ ở bất kỳ toạ độ nào
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSceneRegionService_FullScreenScene_AllowsAnyCoordinates_Test()
        {
            var scene = new VwScene
            {
                Code = $"{TestPrefix}FULLSCN_{Guid.NewGuid():N}",
                Name = "Full Screen Scene",
                ControllerId = null,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var exception = await Record.ExceptionAsync(() =>
                GetRegionService().EnsureWindowInsideSceneRegionAsync(scene.ID, 10000, 10000, 1920, 1080, "Test Window"));

            Assert.Null(exception);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Cửa sổ có kích thước w <= 0 hoặc h <= 0 hoặc SceneId rỗng thì hàm bỏ qua kiểm tra
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSceneRegionService_ZeroOrNegativeDimensions_SkipsCheck_Test()
        {
            var ex1 = await Record.ExceptionAsync(() =>
                GetRegionService().EnsureWindowInsideSceneRegionAsync("any_id", 0, 0, 0, 100, "Window Zero W"));
            var ex2 = await Record.ExceptionAsync(() =>
                GetRegionService().EnsureWindowInsideSceneRegionAsync(null, 0, 0, 100, 100, "Window Null Scene"));

            Assert.Null(ex1);
            Assert.Null(ex2);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Cửa sổ nằm trọn vẹn trong panel của bộ điều khiển sở hữu kịch bản sẽ hợp lệ
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSceneRegionService_InsideOwnerPanel_Succeeds_Test()
        {
            var ctrlId = $"{TestPrefix}CTRL_OK_{Guid.NewGuid():N}";
            var ctrl = new VwController
            {
                ID = ctrlId,
                Code = ctrlId,
                Name = "Owner Controller",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(ctrl).ExecuteCommandAsync();

            var screen = new VwScreen
            {
                Code = $"{TestPrefix}SCR_OK_{Guid.NewGuid():N}",
                Name = "Owner Panel",
                ControllerId = ctrlId,
                GridCol = 0,
                GridRow = 0,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();

            var scene = new VwScene
            {
                Code = $"{TestPrefix}SCN_OK_{Guid.NewGuid():N}",
                Name = "Owner Scene",
                ControllerId = ctrlId,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var exception = await Record.ExceptionAsync(() =>
                GetRegionService().EnsureWindowInsideSceneRegionAsync(scene.ID, 100, 100, 1920, 1080, "Window Inside"));

            Assert.Null(exception);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: GetCoveredControllerIdsAsync trả về danh sách ControllerId bị một hình chữ nhật bao phủ
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSceneRegionService_GetCoveredControllerIds_ReturnsCoveredList_Test()
        {
            var ctrl1 = new VwController
            {
                Code = $"{TestPrefix}C1_{Guid.NewGuid():N}",
                Name = "Ctrl 1",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            var ctrl2 = new VwController
            {
                Code = $"{TestPrefix}C2_{Guid.NewGuid():N}",
                Name = "Ctrl 2",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(new[] { ctrl1, ctrl2 }).ExecuteCommandAsync();

            var scr1 = new VwScreen { Code = $"{TestPrefix}S1_{Guid.NewGuid():N}", ControllerId = ctrl1.ID, GridCol = 0, GridRow = 0, Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now, UpdateTime = DateTime.Now };
            var scr2 = new VwScreen { Code = $"{TestPrefix}S2_{Guid.NewGuid():N}", ControllerId = ctrl2.ID, GridCol = 1, GridRow = 0, Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now, UpdateTime = DateTime.Now };
            await _db.Insertable(new[] { scr1, scr2 }).ExecuteCommandAsync();

            var covered = await GetRegionService().GetCoveredControllerIdsAsync(100, 100, 4900, 1000);

            Assert.Contains(ctrl1.ID, covered);
            Assert.Contains(ctrl2.ID, covered);
        }

        #endregion

        #region Coordinate Conversion & Slicing Tests

        /// <summary>
        /// Author: Đạt
        /// Description: Window trùng khít panel tại điểm gốc (0,0) phải quy đổi đúng ra ô ảo (0,0,1920,1920).
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwSceneRegionService_ToIsapiLocalRect_WindowMatchesOriginPanel_ReturnsFullVirtualSquare_Test()
        {
            var (x, y, w, h) = VwSceneRegionService.ToIsapiLocalRect(
                originCol: 0, originRow: 0, x: 0, y: 0, w: VwSceneRegionService.PanelWidthPx, h: VwSceneRegionService.PanelHeightPx);

            Assert.Equal(0, x);
            Assert.Equal(0, y);
            Assert.Equal(1920, w);
            Assert.Equal(1920, h);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Panel lệch cột (originCol=1) quy đổi toạ độ cục bộ đúng gốc (0,0) sau khi trừ offset.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwSceneRegionService_ToIsapiLocalRect_ShiftedPanelOrigin_SubtractsOffsetCorrectly_Test()
        {
            var (x, y, w, h) = VwSceneRegionService.ToIsapiLocalRect(
                originCol: 1, originRow: 0,
                x: VwSceneRegionService.PanelWidthPx, y: 0,
                w: VwSceneRegionService.PanelWidthPx, h: VwSceneRegionService.PanelHeightPx);

            Assert.Equal(0, x);
            Assert.Equal(0, y);
            Assert.Equal(1920, w);
            Assert.Equal(1920, h);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Window vắt qua 2 panel theo chiều ngang quy đổi ra chiều rộng ảo gấp đôi (3840).
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwSceneRegionService_ToIsapiLocalRect_WindowSpansTwoPanelsWide_DoublesVirtualWidth_Test()
        {
            var (_, _, w, h) = VwSceneRegionService.ToIsapiLocalRect(
                originCol: 0, originRow: 0, x: 0, y: 0,
                w: VwSceneRegionService.PanelWidthPx * 2, h: VwSceneRegionService.PanelHeightPx);

            Assert.Equal(3840, w);
            Assert.Equal(1920, h);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Window lệch vào trong panel vẫn quy đổi đúng tỉ lệ tuyến tính.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwSceneRegionService_ToIsapiLocalRect_WindowOffsetInsidePanel_ScalesProportionally_Test()
        {
            var (x, y, _, _) = VwSceneRegionService.ToIsapiLocalRect(
                originCol: 0, originRow: 0, x: 192, y: 216, w: 100, h: 100);

            Assert.Equal(96, x);
            Assert.Equal(192, y);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Cửa sổ nằm gọn trong Controller 1 -> chỉ trả về đúng 1 slice cho Controller 1.
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public void VwSceneRegionService_SliceWindowForControllers_SingleControllerWindow_ReturnsOneSlice_Test()
        {
            var screens = new List<VwScreen>
            {
                new() { ControllerId = "ctrl-1", GridCol = 0, GridRow = 0 },
                new() { ControllerId = "ctrl-2", GridCol = 1, GridRow = 0 }
            };

            var slices = VwSceneRegionService.SliceWindowForControllers(
                screens, x: 0, y: 0, w: VwSceneRegionService.PanelWidthPx, h: VwSceneRegionService.PanelHeightPx);

            Assert.Single(slices);
            Assert.Equal("ctrl-1", slices[0].ControllerId);
            Assert.Equal(0, slices[0].X);
            Assert.Equal(0, slices[0].Y);
            Assert.Equal(1920, slices[0].W);
            Assert.Equal(1920, slices[0].H);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Cửa sổ toàn tường 2x2 tự động cắt lát và tạo 4 slice độc lập cho 4 controller.
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public void VwSceneRegionService_SliceWindowForControllers_CrossControllerWindow_2x2_Returns4Slices_Test()
        {
            var screens = new List<VwScreen>
            {
                new() { ControllerId = "ctrl-1", GridCol = 0, GridRow = 0 },
                new() { ControllerId = "ctrl-2", GridCol = 1, GridRow = 0 },
                new() { ControllerId = "ctrl-3", GridCol = 0, GridRow = 1 },
                new() { ControllerId = "ctrl-4", GridCol = 1, GridRow = 1 }
            };

            var slices = VwSceneRegionService.SliceWindowForControllers(
                screens, x: 0, y: 0,
                w: VwSceneRegionService.PanelWidthPx * 2,
                h: VwSceneRegionService.PanelHeightPx * 2);

            Assert.Equal(4, slices.Count);
            foreach (var slice in slices)
            {
                Assert.Equal(0, slice.X);
                Assert.Equal(0, slice.Y);
                Assert.Equal(1920, slice.W);
                Assert.Equal(1920, slice.H);
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Cửa sổ vắt nửa bên Controller 1, nửa bên Controller 2 -> tính toán chính xác toạ độ cắt.
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public void VwSceneRegionService_SliceWindowForControllers_WindowPartiallyOverlapping_ReturnsClippedSlice_Test()
        {
            var screens = new List<VwScreen>
            {
                new() { ControllerId = "ctrl-1", GridCol = 0, GridRow = 0 },
                new() { ControllerId = "ctrl-2", GridCol = 1, GridRow = 0 }
            };

            var slices = VwSceneRegionService.SliceWindowForControllers(
                screens,
                x: VwSceneRegionService.PanelWidthPx / 2,
                y: 0,
                w: VwSceneRegionService.PanelWidthPx,
                h: VwSceneRegionService.PanelHeightPx);

            Assert.Equal(2, slices.Count);

            var slice1 = slices.First(s => s.ControllerId == "ctrl-1");
            var slice2 = slices.First(s => s.ControllerId == "ctrl-2");

            Assert.Equal(960, slice1.X);
            Assert.Equal(0, slice1.Y);
            Assert.Equal(960, slice1.W);
            Assert.Equal(1920, slice1.H);

            Assert.Equal(0, slice2.X);
            Assert.Equal(0, slice2.Y);
            Assert.Equal(960, slice2.W);
            Assert.Equal(1920, slice2.H);
        }

        #endregion
    }
}
