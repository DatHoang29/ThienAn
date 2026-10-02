using Furion;

using Microsoft.AspNetCore.Http;


using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Dto.WallPermission;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Newtonsoft.Json;
using Shared.Core.Utilities.Constants;




using System.Security.Claims;




namespace Tests.VideoWall.WebApi.Services
{
    /// <summary>
    /// Description: Kiểm thử toàn diện dịch vụ phân quyền hợp nhất VwPermissionService:
    ///              - Tầng 1: Org / Controller Access (FullAccess, Restricted User, BypassPermission).
    ///              - Tầng 3: User Area Grid Access (Tính toán hình học ô lưới và quyền vùng người dùng).
    /// Created date: 11/09/2026
    /// </summary>
    [Collection("api")]
    public class VwPermissionServiceTests(Host host)
    {
        private const string TestPrefix = "TEST_VWPERM_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly IHttpContextAccessor _httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();

        #region Helper khởi tạo VwPermissionService & Giả lập User Context

        private VwPermissionService GetPermissionService(VwDeviceOptions? customDeviceOptions = null)
        {
            host.Services.GetRequiredService<BaseCacheService>().Remove(CacheConst.Vw.VwWallPermission);
            var scope = host.Services.CreateScope();
            if (customDeviceOptions == null)
                return scope.ServiceProvider.GetRequiredService<VwPermissionService>();

            return new VwPermissionService(
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwWallPermission>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwWindowScene>>(),
                scope.ServiceProvider.GetRequiredService<UserManager>(),
                scope.ServiceProvider.GetRequiredService<BaseCacheService>(),
                Microsoft.Extensions.Options.Options.Create(customDeviceOptions ?? new VwDeviceOptions()),
                scope.ServiceProvider.GetRequiredService<ILogger<VwPermissionService>>());
        }

        private void SetRestrictedUser(string orgId, string account = "test_restricted_user", string? userId = null)
        {
            userId ??= account;
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimConst.AccountType, "333"),
                new Claim(ClaimConst.OrgId, orgId),
                new Claim(ClaimConst.UserId, userId),
                new Claim(ClaimConst.Account, account),
                new Claim(ClaimTypes.Name, account)
            }, "TestAuth");

            _httpContextAccessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        }

        private void SetSuperAdminUser(string account = "test_superadmin", string userId = "test-superadmin-id")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimConst.AccountType, "111"),
                new Claim(ClaimConst.UserId, userId),
                new Claim(ClaimConst.Account, account),
                new Claim(ClaimTypes.Name, account)
            }, "TestAuth");

            _httpContextAccessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        }

        #endregion

        #region Tầng 1: FullAccess Tests

        /// <summary>
        /// Description: Request ẩn danh qua HTTP (có HttpContext nhưng không có User) và không bật bypass thì IsFullAccess là false (an toàn tuyệt đối)
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public void VwPermissionService_AnonymousHttpRequest_DoesNotHaveFullAccess_Test()
        {
            _httpContextAccessor.HttpContext = new DefaultHttpContext();
            try
            {
                var srv = GetPermissionService();

                Assert.False(srv.IsFullAccess);
            }
            finally
            {
                _httpContextAccessor.HttpContext = null;
            }
        }

        /// <summary>
        /// Description: Khi chạy nền không có HttpContext (Hangfire / NATS / MessageBus), IsFullAccess luôn là true
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public void VwPermissionService_BackgroundContext_HasFullAccess_Test()
        {
            _httpContextAccessor.HttpContext = null;
            var srv = GetPermissionService();

            Assert.True(srv.IsFullAccess);
        }

        /// <summary>
        /// Description: Khi tài khoản là SuperAdmin (AccountType = 111), IsFullAccess luôn là true
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public void VwPermissionService_SuperAdmin_HasFullAccess_Test()
        {
            SetSuperAdminUser();
            try
            {
                var srv = GetPermissionService();

                Assert.True(srv.IsFullAccess);
            }
            finally
            {
                _httpContextAccessor.HttpContext = null;
            }
        }

        #endregion

        #region Tầng 1: Restricted User Tests

        /// <summary>
        /// Description: Cờ BypassPermission = true chỉ có hiệu lực ngoài Production: môi trường Development
        ///              cấp FullAccess cho tài khoản thường, còn Production phải vô hiệu hóa cờ này an toàn.
        /// Created date: 24/08/2026
        /// </summary>
        [Theory]
        [InlineData("Development", true)]
        [InlineData("Production", false)]
        public void VwPermissionService_BypassPermission_HonoredOnlyOutsideProduction_Test(
            string environmentName, bool expectedFullAccess)
        {
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            SetRestrictedUser(orgId);

            var hostEnv = App.HostEnvironment;
            var originalEnv = hostEnv.EnvironmentName;

            try
            {
                hostEnv.EnvironmentName = environmentName;

                var srv = GetPermissionService(new VwDeviceOptions { BypassPermission = true });

                Assert.Equal(expectedFullAccess, srv.IsFullAccess);
            }
            finally
            {
                hostEnv.EnvironmentName = originalEnv;
            }
        }

        #endregion

        #region Tầng 3: User Area Grid Geometry Tests

        /// <summary>
        /// Description: Cửa sổ vừa khít 1 ô panel (0,0) → trả về đúng 1 ô (0,0).
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public void GetCoveredCells_SingleCellWindow_ReturnsMatchingCell_Test()
        {
            var cells = VwPermissionService.GetCoveredCells(
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
            var cells = VwPermissionService.GetCoveredCells(
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
            var zeroW = VwPermissionService.GetCoveredCells(0, 0, 0, 1080).ToList();
            var zeroH = VwPermissionService.GetCoveredCells(0, 0, 1920, 0).ToList();

            Assert.Empty(zeroW);
            Assert.Empty(zeroH);
        }

        private static List<VwGridCell> ParseCells(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return [];

            return input.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(pair =>
                {
                    var parts = pair.Split(',');
                    return new VwGridCell { Col = int.Parse(parts[0]), Row = int.Parse(parts[1]) };
                })
                .ToList();
        }

        /// <summary>
        /// Description: Kiểm tra logic IsWithinAllowedCells với các tập ô covered và allowed khác nhau.
        /// Created date: 11/09/2026
        /// </summary>
        [Theory]
        [InlineData("0,0;1,0", "0,0;1,0;2,0", true)]
        [InlineData("0,0;1,0;1,1", "0,0;1,0", false)]
        [InlineData("0,0", "", false)]
        public void IsWithinAllowedCells_EvaluatesSubsetCorrectly_Test(string coveredStr, string allowedStr, bool expected)
        {
            var covered = ParseCells(coveredStr);
            var allowed = ParseCells(allowedStr);

            var result = VwPermissionService.IsWithinAllowedCells(covered, allowed);
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Description: Khi user không có bản ghi VwWallPermission nào, mặc định bypass
        ///              (không ném exception dù tọa độ có vẻ nhạy cảm).
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public async Task EnsureWindowInsideUserAllowedAreaAsync_NoPermissionRecord_BypassesCheck_Test()
        {
            using var scope = host.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<VwPermissionService>();

            var ex = await Record.ExceptionAsync(() =>
                service.EnsureWindowInsideUserAllowedAreaAsync(0, 0, 99999, 99999, "TestWindow"));

            Assert.Null(ex);
        }

        /// <summary>
        /// Description: Kiểm tra ánh xạ và kiểm tra hình học từ Config JSON dạng List&lt;VwGridCell&gt;.
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public void VwWallPermission_ConfigJsonCells_GeometryCheckPasses_Test()
        {
            var allowedCells = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 },
                new() { Col = 1, Row = 0 },
                new() { Col = 0, Row = 1 },
                new() { Col = 1, Row = 1 }
            };

            var json = JsonConvert.SerializeObject(allowedCells);
            var parsedAllowed = JsonConvert.DeserializeObject<List<VwGridCell>>(json)!;

            var covered = VwPermissionService.GetCoveredCells(0, 0, 3840, 2160);
            var inside = VwPermissionService.IsWithinAllowedCells(covered, parsedAllowed);

            Assert.True(inside);
        }

        /// <summary>
        /// Description: Khi user có bản ghi permission nhưng Config không hợp lệ (trống, JSON lỗi, mảng rỗng) -> ném exception tương ứng.
        /// Created date: 11/09/2026
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("invalid-json-{broken")]
        [InlineData("[]")]
        public async Task EnsureWindowInsideUserAllowedAreaAsync_InvalidConfig_ThrowsException_Test(string config)
        {
            var account = $"{TestPrefix}ACC_{Guid.NewGuid():N}";
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            SetRestrictedUser(orgId, account);

            var perm = new VwWallPermission
            {
                UserId = account,
                Config = config,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(perm).ExecuteCommandAsync();

            var service = GetPermissionService();
            var ex = await Record.ExceptionAsync(() =>
                service.EnsureWindowInsideUserAllowedAreaAsync(0, 0, 1920, 1080, "TestWin"));

            Assert.NotNull(ex);
            Assert.Contains("wallPermissionNotAssigned", ex.Message);
        }

        /// <summary>
        /// Description: Khi user có Config hợp lệ và cửa sổ nằm hoàn toàn trong vùng -> thành công, không ném lỗi
        /// </summary>
        [Fact]
        public async Task EnsureWindowInsideUserAllowedAreaAsync_ConfigValid_WindowInside_Succeeds_Test()
        {
            var account = $"{TestPrefix}ACC_{Guid.NewGuid():N}";
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            SetRestrictedUser(orgId, account);

            var cells = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 },
                new() { Col = 1, Row = 0 }
            };
            var perm = new VwWallPermission
            {
                UserId = account,
                Config = JsonConvert.SerializeObject(cells),
                CreateTime = DateTime.Now
            };
            await _db.Insertable(perm).ExecuteCommandAsync();

            var service = GetPermissionService();
            var ex = await Record.ExceptionAsync(() =>
                service.EnsureWindowInsideUserAllowedAreaAsync(0, 0, 3840, 1080, "InsideWin"));

            Assert.Null(ex);
        }

        /// <summary>
        /// Description: Khi user có Config hợp lệ nhưng cửa sổ lấn ra ngoài vùng -> ném lỗi nằm ngoài khu vực
        /// </summary>
        [Fact]
        public async Task EnsureWindowInsideUserAllowedAreaAsync_ConfigValid_WindowOutside_ThrowsException_Test()
        {
            var account = $"{TestPrefix}ACC_{Guid.NewGuid():N}";
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            SetRestrictedUser(orgId, account);

            var cells = new List<VwGridCell>
            {
                new() { Col = 0, Row = 0 }
            };
            var perm = new VwWallPermission
            {
                UserId = account,
                Config = JsonConvert.SerializeObject(cells),
                CreateTime = DateTime.Now
            };
            await _db.Insertable(perm).ExecuteCommandAsync();

            var service = GetPermissionService();
            var ex = await Record.ExceptionAsync(() =>
                service.EnsureWindowInsideUserAllowedAreaAsync(0, 0, 7680, 2160, "OutsideWin"));

            Assert.NotNull(ex);
            Assert.True(ex.Message.Contains("windowOutsideAllowedRegion", StringComparison.OrdinalIgnoreCase) ||
                        ex.Message.Contains("ngoài khu vực màn hình", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region GetEffectivePermissionAsync Tests

        /// <summary>
        /// Description: Khi không có user đăng nhập (HttpContext = null hoặc App.User = null) -> GetEffectivePermissionAsync trả về null
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task GetEffectivePermissionAsync_NoUserContext_ReturnsNull_Test()
        {
            _httpContextAccessor.HttpContext = null;
            var service = GetPermissionService();
            var perm = await service.GetEffectivePermissionAsync();
            Assert.Null(perm);
        }

        /// <summary>
        /// Description: Khi user không có bản ghi phân quyền nào -> GetEffectivePermissionAsync trả về null
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task GetEffectivePermissionAsync_NoRecord_ReturnsNull_Test()
        {
            var account = $"{TestPrefix}ACC_{Guid.NewGuid():N}";
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            SetRestrictedUser(orgId, account);

            var service = GetPermissionService();
            var perm = await service.GetEffectivePermissionAsync();
            Assert.Null(perm);
        }

        /// <summary>
        /// Description: Khi có cả bản ghi UserId và OrgId -> GetEffectivePermissionAsync ưu tiên trả về bản ghi của UserId
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task GetEffectivePermissionAsync_BothUserAndOrgExist_ReturnsUserRecord_Test()
        {
            var account = $"{TestPrefix}ACC_{Guid.NewGuid():N}";
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            SetRestrictedUser(orgId, account);

            var userPerm = new VwWallPermission
            {
                UserId = account,
                Config = "[{\"Col\":0,\"Row\":0}]",
                CreateTime = DateTime.Now
            };
            var orgPerm = new VwWallPermission
            {
                OrgId = orgId,
                UserId = null,
                Config = "[{\"Col\":1,\"Row\":1}]",
                CreateTime = DateTime.Now
            };

            await _db.Insertable(new[] { userPerm, orgPerm }).ExecuteCommandAsync();
            var service = GetPermissionService();
            var perm = await service.GetEffectivePermissionAsync();

            Assert.NotNull(perm);
            Assert.Equal(userPerm.ID, perm.ID);
            Assert.Equal(account, perm.UserId);
        }

        /// <summary>
        /// Description: Khi không có bản ghi UserId nhưng có bản ghi OrgId -> GetEffectivePermissionAsync trả về bản ghi của OrgId
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task GetEffectivePermissionAsync_NoUserRecord_HasOrgRecord_ReturnsOrgRecord_Test()
        {
            var account = $"{TestPrefix}ACC_{Guid.NewGuid():N}";
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            SetRestrictedUser(orgId, account);

            var orgPerm = new VwWallPermission
            {
                OrgId = orgId,
                UserId = null,
                Config = "[{\"Col\":2,\"Row\":3}]",
                CreateTime = DateTime.Now
            };

            await _db.Insertable(orgPerm).ExecuteCommandAsync();

            var service = GetPermissionService();
            var perm = await service.GetEffectivePermissionAsync();

            Assert.NotNull(perm);
            Assert.Equal(orgPerm.ID, perm.ID);
            Assert.Equal(orgId, perm.OrgId);
        }



        #endregion

    }

}

