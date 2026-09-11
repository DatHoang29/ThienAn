using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure.Services.Scene;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Bộ kiểm thử Nhóm A (A1-A9) cho quy đổi toạ độ uniformCoordinate
    ///              của bộ trung tâm cascade DS-C66S và kiểm tra biên canvas toàn tường.
    /// Created date: 08/09/2026
    /// </summary>
    public class VwCascadeCoordTests
    {
        /// <summary>
        /// Description: A1 - Toàn màn hình khi wallCanvas bằng centerCanvas trả về nguyên vẹn toạ độ centerCanvas.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void A1_FullScreenWindow_WhenWallCanvasEqualsCenterCanvas_ReturnsCenterCanvas()
        {
            // Arrange
            var wallCanvas = (15360, 7680);
            var centerCanvas = (15360, 7680);

            // Act
            var result = VwSceneRegionService.ToCenterUniformRect(0, 0, 15360, 7680, wallCanvas, centerCanvas);

            // Assert
            Assert.NotNull(result);
            Assert.Equal((0, 0, 15360, 7680), result.Value);
        }

        /// <summary>
        /// Description: A2 - Toàn màn hình khi tỷ lệ thu nhỏ trả về kích thước centerCanvas tương ứng.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void A2_FullScreenWindow_ScaledDown_ReturnsExpectedCenterCanvas()
        {
            // Arrange
            var wallCanvas = (15360, 4320);
            var centerCanvas = (7680, 3840);

            // Act
            var result = VwSceneRegionService.ToCenterUniformRect(0, 0, 15360, 4320, wallCanvas, centerCanvas);

            // Assert
            Assert.NotNull(result);
            Assert.Equal((0, 0, 7680, 3840), result.Value);
        }

        /// <summary>
        /// Description: A3 - Cửa sổ ITS ở giữa 12 màn tính toán tỉ lệ chính xác sang toạ độ centerCanvas.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void A3_ITSWindow_MiddleTwelveScreens_CalculatesProportionalRect()
        {
            // Arrange
            var wallCanvas = (15360, 4320);
            var centerCanvas = (7680, 3840);

            // Act
            var result = VwSceneRegionService.ToCenterUniformRect(3840, 2160, 11520, 2160, wallCanvas, centerCanvas);

            // Assert
            Assert.NotNull(result);
            Assert.Equal((1920, 1920, 5760, 1920), result.Value);
        }

        /// <summary>
        /// Description: A4 - Màn hình góc dưới bên phải nằm trọn trong biên centerCanvas, không bị tràn.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void A4_BottomRightScreen_StaysWithinCenterCanvasBounds()
        {
            // Arrange
            var wallCanvas = (15360, 4320);
            var centerCanvas = (7680, 3840);
            var (x, y, w, h) = (7 * 1920, 3 * 1080, 1920, 1080);

            // Act
            var result = VwSceneRegionService.ToCenterUniformRect(x, y, w, h, wallCanvas, centerCanvas);

            // Assert
            Assert.NotNull(result);
            var (rx, ry, rw, rh) = result.Value;
            Assert.Equal(7680, rx + rw);
            Assert.Equal(3840, ry + rh);
        }

        /// <summary>
        /// Description: A5 - Kích thước không chia hết vẫn đảm bảo tối thiểu 1 pixel cho W và H.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void A5_NonDivisibleCanvas_MaintainsDimensionsAndMinOnePx()
        {
            // Arrange
            var wallCanvas = (15360, 4320);
            var centerCanvas = (7000, 3000);

            // Act
            var result = VwSceneRegionService.ToCenterUniformRect(0, 0, 100, 100, wallCanvas, centerCanvas);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Value.W >= 1);
            Assert.True(result.Value.H >= 1);
        }

        /// <summary>
        /// Description: A6 - Kích thước w=0 hoặc h=0 trả về null.
        /// Created date: 08/09/2026
        /// </summary>
        [Theory]
        [InlineData(0, 100)]
        [InlineData(100, 0)]
        [InlineData(-10, 100)]
        [InlineData(100, -10)]
        public void A6_ZeroDimensions_ReturnsNull(int w, int h)
        {
            // Arrange
            var wallCanvas = (15360, 7680);
            var centerCanvas = (1920, 1920);

            // Act
            var result = VwSceneRegionService.ToCenterUniformRect(100, 100, w, h, wallCanvas, centerCanvas);

            // Assert
            Assert.Null(result);
        }

        // A7 is moved to VwCascadeCoordDatabaseTests below

        /// <summary>
        /// Description: A8 - Ánh xạ đồng nhất khi hai canvas kích thước như nhau.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void A8_IdentityMapping_WhenCanvasesMatch()
        {
            // Arrange
            var canvas = (1920, 1080);
            var (x, y, w, h) = (200, 300, 400, 500);

            // Act
            var result = VwSceneRegionService.ToCenterUniformRect(x, y, w, h, canvas, canvas);

            // Assert
            Assert.NotNull(result);
            Assert.Equal((x, y, w, h), result.Value);
        }

        /// <summary>
        /// Description: A9 - Tính đơn điệu: kích thước đầu vào lớn hơn sinh ra kích thước đầu ra lớn hơn hoặc bằng.
        /// Created date: 08/09/2026
        /// </summary>
        [Theory]
        [InlineData(100, 200)]
        [InlineData(500, 1000)]
        [InlineData(1920, 3840)]
        public void A9_Theory_Monotonicity(int w1, int w2)
        {
            // Arrange
            var wallCanvas = (15360, 7680);
            var centerCanvas = (1920, 1920);

            // Act
            var res1 = VwSceneRegionService.ToCenterUniformRect(0, 0, w1, 1000, wallCanvas, centerCanvas);
            var res2 = VwSceneRegionService.ToCenterUniformRect(0, 0, w2, 1000, wallCanvas, centerCanvas);

            // Assert
            Assert.NotNull(res1);
            Assert.NotNull(res2);
            Assert.True(res2.Value.W >= res1.Value.W);
        }
    }

    [Collection("api")]
    public class VwCascadeCoordDatabaseTests(Host host)
    {
        private const string TestPrefix = "TEST_VWCOORD_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();

        private VwSceneRegionService GetRegionService()
            => host.Services.CreateScope().ServiceProvider.GetRequiredService<VwSceneRegionService>();

        /// <summary>
        /// Description: A7 - Cửa sổ vượt quá canvas toàn tường bị chặn với thông báo lỗi rõ ràng.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task A7_WindowExceedingWallCanvas_ThrowsException()
        {
            // Arrange
            var topoId = $"{TestPrefix}TOPO_{Guid.NewGuid():N}";
            var topo = new VwWallTopology
            {
                ID = topoId,
                Code = topoId,
                Name = "Test Wall Topology 8x4",
                Cols = 8,
                Rows = 4,
                ScreenWidth = 1920,
                ScreenHeight = 1080,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(topo).ExecuteCommandAsync();

            var sceneId = $"{TestPrefix}SCN_{Guid.NewGuid():N}";
            var scene = new VwScene
            {
                ID = sceneId,
                Code = sceneId,
                Name = "Full Wall Scene",
                ControllerId = null,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            // Act & Assert (x=15000 + w=1000 = 16000 > 15360)
            var ex = await Record.ExceptionAsync(() =>
                GetRegionService().EnsureWindowInsideSceneRegionAsync(sceneId, 15000, 0, 1000, 500, "Window Exceeding"));

            Assert.NotNull(ex);
            Assert.Contains("ngoài tường", ex.Message);
        }
    }
}
