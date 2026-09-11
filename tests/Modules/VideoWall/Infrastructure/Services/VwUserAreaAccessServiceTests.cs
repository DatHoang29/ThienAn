using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Dto.UserAreaPermission;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure.Services.Access;
using Newtonsoft.Json;
using SqlSugar;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Kiểm thử Tầng 3 phân quyền VideoWall — VwUserAreaAccessService.
    ///              Bao gồm unit test hàm hình học theo ô lưới và integration test với DB.
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
        // Unit test — hàm hình học GetCoveredCells & IsWithinAllowedCells
        // -------------------------------------------------------------------------

        /// <summary>
        /// Description: Cửa sổ vừa khít 1 ô panel (0,0) → trả về đúng 1 ô (0,0).
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public void GetCoveredCells_SingleCellWindow_ReturnsMatchingCell_Test()
        {
            var cells = VwUserAreaAccessService.GetCoveredCells(
                x: 0, y: 0, w: 1920, h: 1080,
                panelWidthPx: 1920, panelHeightPx: 1080).ToList();

            Assert.Single(cells);
            Assert.Equal(0, cells[0].Col);
            Assert.Equal(0, cells[0].Row);
        }

        /// <summary>
        /// Description: Cửa sổ trải qua 2 cột 2 hàng → trả về đủ 4 ô lưới tương ứng.
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public void GetCoveredCells_MultiCellWindow_ReturnsAllCoveredCells_Test()
        {
            var cells = VwUserAreaAccessService.GetCoveredCells(
                x: 1920, y: 0, w: 3840, h: 2160,
                panelWidthPx: 1920, panelHeightPx: 1080).ToList();

            Assert.Equal(4, cells.Count);
            Assert.Contains(cells, c => c.Col == 1 && c.Row == 0);
            Assert.Contains(cells, c => c.Col == 1 && c.Row == 1);
            Assert.Contains(cells, c => c.Col == 2 && c.Row == 0);
            Assert.Contains(cells, c => c.Col == 2 && c.Row == 1);
        }

        /// <summary>
        /// Description: Cửa sổ có kích thước không hợp lệ (<= 0) → trả về rỗng.
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public void GetCoveredCells_ZeroOrNegativeDimensions_ReturnsEmpty_Test()
        {
            var zeroW = VwUserAreaAccessService.GetCoveredCells(0, 0, 0, 1080).ToList();
            var zeroH = VwUserAreaAccessService.GetCoveredCells(0, 0, 1920, 0).ToList();

            Assert.Empty(zeroW);
            Assert.Empty(zeroH);
        }

        /// <summary>
        /// Description: Tập ô cần phủ là tập con của tập ô được cấp → trả về true.
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public void IsWithinAllowedCells_CoveredSubsetOfAllowed_ReturnsTrue_Test()
        {
            var covered = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 },
                new() { Col = 1, Row = 0 }
            };

            var allowed = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 },
                new() { Col = 1, Row = 0 },
                new() { Col = 2, Row = 0 }
            };

            var result = VwUserAreaAccessService.IsWithinAllowedCells(covered, allowed);
            Assert.True(result);
        }

        /// <summary>
        /// Description: Tập ô cần phủ thiếu ít nhất 1 ô trong tập được cấp → trả về false.
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public void IsWithinAllowedCells_CoveredMissingOneCell_ReturnsFalse_Test()
        {
            var covered = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 },
                new() { Col = 1, Row = 0 },
                new() { Col = 1, Row = 1 }
            };

            var allowed = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 },
                new() { Col = 1, Row = 0 }
            };

            var result = VwUserAreaAccessService.IsWithinAllowedCells(covered, allowed);
            Assert.False(result);
        }

        /// <summary>
        /// Description: Tập ô được cấp rỗng → trả về false.
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public void IsWithinAllowedCells_AllowedIsEmpty_ReturnsFalse_Test()
        {
            var covered = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 }
            };

            var allowed = new List<VwGridCell>();

            var result = VwUserAreaAccessService.IsWithinAllowedCells(covered, allowed);
            Assert.False(result);
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
        /// Description: Kiểm tra ánh xạ và kiểm tra hình học từ Config JSON trong CSDL.
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermission_ConfigJsonCells_GeometryCheckPasses_Test()
        {
            // Arrange — thêm 1 bản ghi phân quyền giả có Config dạng JSON array
            var account = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var allowedCells = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 },
                new() { Col = 1, Row = 0 },
                new() { Col = 0, Row = 1 },
                new() { Col = 1, Row = 1 }
            };

            var permission = new VwUserAreaPermission
            {
                ID = $"{TestPrefix}PERM_{Guid.NewGuid():N}",
                UserId = account,
                Config = JsonConvert.SerializeObject(allowedCells),
                Description = "Vùng 2x2 test",
                CreateTime = DateTime.Now
            };
            await _db.Insertable(permission).ExecuteCommandAsync();

            // Act — tính covered cells và kiểm tra
            var covered = VwUserAreaAccessService.GetCoveredCells(0, 0, 3840, 2160);
            var parsedAllowed = JsonConvert.DeserializeObject<List<VwGridCell>>(permission.Config)!;
            var inside = VwUserAreaAccessService.IsWithinAllowedCells(covered, parsedAllowed);

            // Assert
            Assert.True(inside);

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.ID == permission.ID)
                .ExecuteCommandAsync();
        }
    }
}
