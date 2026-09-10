using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure.Services.Access;
using Shared.DTO.Constants.Application;
using SqlSugar;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Kiểm thử Tầng 3 phân quyền VideoWall — VwUserAreaAccessService.
    ///              Bao gồm unit test hàm hình học thuần và integration test với DB.
    ///              Lưới toạ độ chuẩn: 8 cột × 4 hàng, panel 1920×1080 → canvas 15360×4320.
    ///              Khớp với sơ đồ thietkevideowall.jpg (KienTruc_VideoWall_DS-C66S-Cascade.md).
    /// Created date: 10/09/2026
    /// </summary>
    [Collection("api")]
    public class VwUserAreaAccessServiceTests(Host host)
    {
        private const string TestPrefix = "TEST_UA3_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();

        // -------------------------------------------------------------------------
        // Unit test — hàm hình học IsInsideAllowedArea (không cần DB, không cần DI)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Description: Cửa sổ hoàn toàn nằm trong vùng được cấp → trả về true.
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void IsInsideAllowedArea_WindowFullyInsideGrantedArea_ReturnsTrue_Test()
        {
            // Vùng cấp: cột 0-3, hàng 0-1 → X=0, Y=0, W=7680, H=2160 (pixel)
            // Cửa sổ: cột 1-2, hàng 0 → X=1920, Y=0, W=3840, H=1080
            var result = VwUserAreaAccessService.IsInsideAllowedArea(
                x: 1920, y: 0, w: 3840, h: 1080,
                colStart: 0, colEnd: 3, rowStart: 0, rowEnd: 1,
                panelWidthPx: 1920, panelHeightPx: 1080);

            Assert.True(result);
        }

        /// <summary>
        /// Description: Cửa sổ vượt ra ngoài ranh giới phải của vùng → trả về false.
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void IsInsideAllowedArea_WindowExceedsRightBoundary_ReturnsFalse_Test()
        {
            // Vùng cấp: cột 0-3, hàng 0-3 → areaX=0, areaW=7680
            // Cửa sổ: X=7000, W=1000 → X+W=8000 > 7680 → vượt biên
            var result = VwUserAreaAccessService.IsInsideAllowedArea(
                x: 7000, y: 0, w: 1000, h: 1080,
                colStart: 0, colEnd: 3, rowStart: 0, rowEnd: 3,
                panelWidthPx: 1920, panelHeightPx: 1080);

            Assert.False(result);
        }

        /// <summary>
        /// Description: Cửa sổ vượt xuống dưới ranh giới hàng cuối → trả về false.
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void IsInsideAllowedArea_WindowExceedsBottomBoundary_ReturnsFalse_Test()
        {
            // Vùng cấp: cột 0-7, hàng 0-1 → areaH=2160
            // Cửa sổ: Y=2000, H=500 → Y+H=2500 > 2160
            var result = VwUserAreaAccessService.IsInsideAllowedArea(
                x: 0, y: 2000, w: 1920, h: 500,
                colStart: 0, colEnd: 7, rowStart: 0, rowEnd: 1,
                panelWidthPx: 1920, panelHeightPx: 1080);

            Assert.False(result);
        }

        /// <summary>
        /// Description: Cửa sổ chính xác khớp biên của vùng được cấp → trả về true (boundary inclusive).
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void IsInsideAllowedArea_WindowExactlyMatchesBoundary_ReturnsTrue_Test()
        {
            // Vùng cấp: cột 2-7, hàng 1-2 → areaX=3840, areaY=1080, areaW=11520, areaH=2160
            // ITS/MAP window: X=3840, Y=1080, W=11520, H=2160 → khớp chính xác
            var result = VwUserAreaAccessService.IsInsideAllowedArea(
                x: 3840, y: 1080, w: 11520, h: 2160,
                colStart: 2, colEnd: 7, rowStart: 1, rowEnd: 2,
                panelWidthPx: 1920, panelHeightPx: 1080);

            Assert.True(result);
        }

        /// <summary>
        /// Description: Hàm grid — cửa sổ nằm trọn trong vùng theo cột/hàng → trả về true.
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void IsInsideAllowedAreaGrid_WindowFullyInsideGrid_ReturnsTrue_Test()
        {
            // Cửa sổ: cột 3-5, hàng 1-2 nằm trong vùng cột 2-7, hàng 0-3
            var result = VwUserAreaAccessService.IsInsideAllowedAreaGrid(
                winColStart: 3, winColEnd: 5, winRowStart: 1, winRowEnd: 2,
                colStart: 2, colEnd: 7, rowStart: 0, rowEnd: 3);

            Assert.True(result);
        }

        // -------------------------------------------------------------------------
        // Integration test — EnsureWindowInsideUserAllowedAreaAsync với DB thật
        // -------------------------------------------------------------------------

        /// <summary>
        /// Description: Khi user không có bản ghi VwUserAreaPermission nào, mặc định bypass
        ///              (không ném exception dù tọa độ có vẻ nhạy cảm).
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public async Task EnsureWindowInsideUserAllowedAreaAsync_NoPermissionRecord_BypassesCheck_Test()
        {
            // Arrange — IsFullAccess=true trong môi trường test (BypassPermission=true)
            using var scope = host.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<VwUserAreaAccessService>();

            // Act & Assert — không được ném bất kỳ exception nào (bypass bởi IsFullAccess=true)
            var ex = await Record.ExceptionAsync(() =>
                service.EnsureWindowInsideUserAllowedAreaAsync(0, 0, 99999, 99999, "TestWindow"));

            Assert.Null(ex);
        }

        /// <summary>
        /// Description: Khi đã có bản ghi VwUserAreaPermission và kiểm tra hình học bằng hàm tĩnh,
        ///              cửa sổ trong vùng → IsInsideAllowedArea trả true.
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermission_WindowInsideGrantedArea_GeometryCheckPasses_Test()
        {
            // Arrange — thêm 1 bản ghi phân quyền giả để kiểm tra hình học
            var account = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var permission = new VwUserAreaPermission
            {
                ID = $"{TestPrefix}PERM_{Guid.NewGuid():N}",
                UserId = account,
                ColStart = 0,
                ColEnd = 3,
                RowStart = 0,
                RowEnd = 3,
                Description = "Vùng test",
                CreateTime = DateTime.Now
            };
            await _db.Insertable(permission).ExecuteCommandAsync();

            // Act — kiểm tra hàm hình học với cửa sổ nằm trong vùng cột 0-3
            var inside = VwUserAreaAccessService.IsInsideAllowedArea(
                x: permission.ColStart * 1920,
                y: permission.RowStart * 1080,
                w: (permission.ColEnd - permission.ColStart + 1) * 1920,
                h: (permission.RowEnd - permission.RowStart + 1) * 1080,
                colStart: permission.ColStart,
                colEnd: permission.ColEnd,
                rowStart: permission.RowStart,
                rowEnd: permission.RowEnd,
                panelWidthPx: 1920,
                panelHeightPx: 1080);

            // Assert
            Assert.True(inside);

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.ID == permission.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Cửa sổ cột 0-1 không nằm trong vùng được cấp cột 4-7
        ///              → IsInsideAllowedArea trả false.
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public void IsInsideAllowedArea_CameraWindowOutsideGrantedZone_ReturnsFalse_Test()
        {
            // Vùng cấp: cột 4-7 (vùng phải)
            // Cửa sổ: cột 0 (cam viền trái) → X=0, W=1920 → nằm ngoài vùng cấp [4-7]
            var result = VwUserAreaAccessService.IsInsideAllowedArea(
                x: 0, y: 0, w: 1920, h: 1080,
                colStart: 4, colEnd: 7, rowStart: 0, rowEnd: 3,
                panelWidthPx: 1920, panelHeightPx: 1080);

            Assert.False(result);
        }
    }
}
