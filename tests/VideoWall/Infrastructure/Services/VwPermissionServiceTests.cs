using Furion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Dto.UserAreaPermission;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Newtonsoft.Json;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;
using SqlSugar;
using System.Security.Claims;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
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
            var scope = host.Services.CreateScope();
            if (customDeviceOptions == null)
                return scope.ServiceProvider.GetRequiredService<VwPermissionService>();

            return new VwPermissionService(
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwController>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwScreen>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwSource>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwScene>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwUserAreaPermission>>(),
                scope.ServiceProvider.GetRequiredService<UserManager>(),
                Microsoft.Extensions.Options.Options.Create(customDeviceOptions),
                scope.ServiceProvider.GetRequiredService<ILogger<VwPermissionService>>());
        }

        private void SetRestrictedUser(string orgId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimConst.AccountType, "333"),
                new Claim(ClaimConst.OrgId, orgId),
                new Claim(ClaimConst.UserId, "test-restricted-user-id"),
                new Claim(ClaimConst.Account, "test_restricted_user")
            }, "TestAuth");

            _httpContextAccessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        }

        #endregion

        #region Tầng 1: FullAccess Tests

        /// <summary>
        /// Description: Trong môi trường Background/Test không có User Context, IsFullAccess luôn là true
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwPermissionService_NoUserContext_HasFullAccess_Test()
        {
            var srv = GetPermissionService();
            var scope = await srv.GetScopeAsync();

            Assert.True(srv.IsFullAccess);
            Assert.True(scope.IsFullAccess);
        }

        /// <summary>
        /// Description: Với quyền FullAccess, EnsureControllerAccessAsync cho phép truy cập mọi ControllerId
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwPermissionService_FullAccess_EnsureControllerAccess_Succeeds_Test()
        {
            var ex = await Record.ExceptionAsync(() =>
                GetPermissionService().EnsureControllerAccessAsync("any_controller_id"));

            Assert.Null(ex);
        }

        /// <summary>
        /// Description: Với quyền FullAccess, ResolveOrgIdAsync giữ nguyên OrgId được truyền vào
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwPermissionService_FullAccess_ResolveOrgId_ReturnsRequestedOrgId_Test()
        {
            var requestedOrg = "ORG_SPECIAL_001";

            var result = await GetPermissionService().ResolveOrgIdAsync(requestedOrg);

            Assert.Equal(requestedOrg, result);
        }

        #endregion

        #region Tầng 1: Restricted User Tests

        /// <summary>
        /// Description: Tài khoản thường chỉ thấy controller cùng org mình, không thấy controller org khác.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwPermissionService_RestrictedUser_GetScope_ReturnsOwnedControllerOnly_Test()
        {
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var otherOrgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var ownedCtrl = new VwController
            {
                Code = $"{TestPrefix}OWN_{Guid.NewGuid():N}",
                Name = "Owned Controller",
                OrgId = orgId,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var foreignCtrl = new VwController
            {
                Code = $"{TestPrefix}FOREIGN_{Guid.NewGuid():N}",
                Name = "Foreign Controller",
                OrgId = otherOrgId,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(new[] { ownedCtrl, foreignCtrl }).ExecuteCommandAsync();
            SetRestrictedUser(orgId);

            var scope = await GetPermissionService().GetScopeAsync();

            Assert.False(scope.IsFullAccess);
            Assert.Contains(ownedCtrl.ID, scope.ControllerIds);
            Assert.DoesNotContain(foreignCtrl.ID, scope.ControllerIds);
        }

        /// <summary>
        /// Description: Tài khoản thường thao tác được trên controller thuộc org của mình.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwPermissionService_RestrictedUser_EnsureControllerAccess_AllowsOwnedController_Test()
        {
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var ownedCtrl = new VwController
            {
                Code = $"{TestPrefix}OWN2_{Guid.NewGuid():N}",
                Name = "Owned Controller 2",
                OrgId = orgId,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(ownedCtrl).ExecuteCommandAsync();
            SetRestrictedUser(orgId);

            var ex = await Record.ExceptionAsync(() => GetPermissionService().EnsureControllerAccessAsync(ownedCtrl.ID));

            Assert.Null(ex);
        }

        /// <summary>
        /// Description: Tài khoản thường bị chặn khi thao tác trên controller thuộc org khác.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwPermissionService_RestrictedUser_EnsureControllerAccess_DeniesForeignController_Test()
        {
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var otherOrgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var foreignCtrl = new VwController
            {
                Code = $"{TestPrefix}FOREIGN2_{Guid.NewGuid():N}",
                Name = "Foreign Controller 2",
                OrgId = otherOrgId,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(foreignCtrl).ExecuteCommandAsync();
            SetRestrictedUser(orgId);

            var ex = await Record.ExceptionAsync(() => GetPermissionService().EnsureControllerAccessAsync(foreignCtrl.ID));

            Assert.NotNull(ex);
        }

        /// <summary>
        /// Description: Đơn vị chỉ quản lý đúng 1 controller thì ResolveSceneControllerIdAsync tự suy ra controller đó.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwPermissionService_RestrictedUser_ResolveSceneControllerId_InfersSingleOwnedController_Test()
        {
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var soleCtrl = new VwController
            {
                Code = $"{TestPrefix}SOLE_{Guid.NewGuid():N}",
                Name = "Sole Controller",
                OrgId = orgId,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(soleCtrl).ExecuteCommandAsync();
            SetRestrictedUser(orgId);

            var resolved = await GetPermissionService().ResolveSceneControllerIdAsync(null);

            Assert.Equal(soleCtrl.ID, resolved);
        }

        /// <summary>
        /// Description: Cờ BypassPermission = true chỉ có hiệu lực ngoài Production: môi trường Development
        ///              cấp FullAccess cho tài khoản thường, còn Production phải vô hiệu hóa cờ này an toàn.
        /// Created date: 24/08/2026
        /// </summary>
        [Theory]
        [InlineData("Development", true)]
        [InlineData("Production", false)]
        public async Task VwPermissionService_BypassPermission_HonoredOnlyOutsideProduction_Test(
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
                var scope = await srv.GetScopeAsync();

                Assert.Equal(expectedFullAccess, srv.IsFullAccess);
                Assert.Equal(expectedFullAccess, scope.IsFullAccess);
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

            var result = VwPermissionService.IsWithinAllowedCells(covered, allowed);
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

            var result = VwPermissionService.IsWithinAllowedCells(covered, allowed);
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

            var result = VwPermissionService.IsWithinAllowedCells(covered, allowed);
            Assert.False(result);
        }

        /// <summary>
        /// Description: Khi user không có bản ghi VwUserAreaPermission nào, mặc định bypass
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
        public void VwUserAreaPermission_ConfigJsonCells_GeometryCheckPasses_Test()
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

        #endregion
    }
}
