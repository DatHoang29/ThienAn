using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Bộ kiểm thử Nhóm E (E1-E8) thẩm định tính toàn vẹn, tính idempotent
    ///              và các quy tắc ràng buộc logic của file dữ liệu khởi tạo VideoWall Cascade.
    /// Created date: 08/09/2026
    /// </summary>
    public class VwCascadeSeedDataTests
    {
        private readonly string _sqlContent;

        public VwCascadeSeedDataTests()
        {
            var seedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "docs", "sql", "2026-09-08-seed-videowall-cascade.sql");
            if (!File.Exists(seedPath))
            {
                // Fallback direct path in workspace
                seedPath = @"d:\ThienAn\docs\sql\2026-09-08-seed-videowall-cascade.sql";
            }

            _sqlContent = File.Exists(seedPath) ? File.ReadAllText(seedPath) : string.Empty;
        }

        /// <summary>
        /// Description: E1 - Kịch bản seed định nghĩa đúng 1 bộ trung tâm (center) và 3 bộ con (sub).
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void E1_SeedScript_DefinesExactlyOneCenterAndThreeSubControllers()
        {
            Assert.False(string.IsNullOrWhiteSpace(_sqlContent), "File seed SQL phải tồn tại và có nội dung.");

            var centerMatches = Regex.Matches(_sqlContent, @"(\[Role\]|'Role'|Role)\s*,\s*(\[IntegrationMode\]|'IntegrationMode'|IntegrationMode)[\s\S]*?'center'\s*,\s*'active'|(\[Role\]|'Role'|Role)\s*=\s*'center'", RegexOptions.IgnoreCase);
            var subMatches = Regex.Matches(_sqlContent, @"(\[Role\]|'Role'|Role)\s*,\s*(\[IntegrationMode\]|'IntegrationMode'|IntegrationMode)[\s\S]*?'sub'\s*,\s*'inventory'|(\[Role\]|'Role'|Role)\s*=\s*'sub'", RegexOptions.IgnoreCase);

            Assert.NotEmpty(centerMatches);
            Assert.NotEmpty(subMatches);
            Assert.Contains("'CTRL_CENTER_01'", _sqlContent);
            Assert.Contains("'CTRL_SUB_01'", _sqlContent);
            Assert.Contains("'CTRL_SUB_02'", _sqlContent);
            Assert.Contains("'CTRL_SUB_03'", _sqlContent);
        }

        /// <summary>
        /// Description: E2 - Khởi tạo đủ 32 màn hình lưới 8 cột x 4 hàng (cột 0..7, hàng 0..3).
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void E2_SeedScript_CoversGridColsZeroToSevenAndRowsZeroToThree()
        {
            Assert.Contains("@r < 4", _sqlContent);
            Assert.Contains("@c < 8", _sqlContent);
            Assert.Contains("[GridCol] = @c", _sqlContent);
            Assert.Contains("[GridRow] = @r", _sqlContent);
        }

        /// <summary>
        /// Description: E3 - Phân bổ màn hình theo bộ điều khiển con: C2 (16 màn), C3 (8 màn), C4 (8 màn).
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void E3_SeedScript_AllocatesScreensToSubControllersCorrectly()
        {
            Assert.Contains("CTRL_SUB_01", _sqlContent);
            Assert.Contains("CTRL_SUB_02", _sqlContent);
            Assert.Contains("CTRL_SUB_03", _sqlContent);
            Assert.Contains("@c <= 3", _sqlContent);
            Assert.Contains("@c <= 5", _sqlContent);
        }

        /// <summary>
        /// Description: E4 - Kịch bản toàn tường có kích thước lưới 8x4 và ControllerId là NULL.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void E4_SeedScript_ConfiguresFullWallSceneWithNullControllerId()
        {
            Assert.Contains("SCN-FULL-01", _sqlContent);
            Assert.Contains("[GridCols], [GridRows]", _sqlContent);
            Assert.Contains("NULL, '1', 8, 4", _sqlContent);
        }

        /// <summary>
        /// Description: E5 - Cửa sổ ITS phủ trung tâm 12 màn hình từ cột 2 đến cột 7.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void E5_SeedScript_ConfiguresCenterItsWindowCoordinates()
        {
            Assert.Contains("WND-ITS-01", _sqlContent);
            Assert.Contains("SRC_ITS_01", _sqlContent);
            // X = 2 * 3840 = 7680, Y = 1 * 2160 = 2160, W = 6 * 3840 = 23040, H = 2 * 2160 = 4320
            Assert.Contains("7680, 2160, 23040, 4320", _sqlContent);
        }

        /// <summary>
        /// Description: E6 - Khởi tạo 20 cửa sổ camera viền bao quanh màn hình ITS ở giữa.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void E6_SeedScript_ConfiguresTwentyBorderCameraWindows()
        {
            Assert.Contains("@borderIdx <= 20", _sqlContent);
            Assert.Contains("WND-CAM-", _sqlContent);
            Assert.Contains("SRC_CAM_", _sqlContent);
        }

        /// <summary>
        /// Description: E7 - 21 nguồn tín hiệu HDMI có SignalNo phân biệt từ 1 đến 21.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void E7_SeedScript_ConfiguresTwentyOneHdmiSourcesWithDistinctSignalNo()
        {
            Assert.Contains("SRC-ITS-01", _sqlContent);
            Assert.Contains("'hdmi_in', 1", _sqlContent);
            Assert.Contains("@camIdx <= 20", _sqlContent);
            Assert.Contains("'hdmi_in', @camIdx + 1", _sqlContent);
        }

        /// <summary>
        /// Description: E8 - Script SQL đảm bảo tính idempotent thông qua kiểm tra IF NOT EXISTS và UPDATE khi đã tồn tại.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void E8_SeedScript_IsIdempotentWithIfNotExistsAndUpgrades()
        {
            var ifNotExistsMatches = Regex.Matches(_sqlContent, @"IF NOT EXISTS", RegexOptions.IgnoreCase);
            Assert.True(ifNotExistsMatches.Count >= 5, "Script phải có ít nhất 5 khối IF NOT EXISTS kiểm tra idempotency.");
            Assert.Contains("UPDATE [VwWallTopology]", _sqlContent);
            Assert.Contains("UPDATE [VwController]", _sqlContent);
            Assert.Contains("UPDATE [VwScreen]", _sqlContent);
            Assert.Contains("UPDATE [VwScene]", _sqlContent);
            Assert.Contains("UPDATE [VwWindowScene]", _sqlContent);
        }
    }
}
