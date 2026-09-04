using Furion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Shared.Core.Utilities.Constants;
using Shared.Infrastructure.Services;
using System.Security.Claims;

namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử toàn diện VwOrgAccessService cho cả 2 luồng:
    ///              - FullAccess (SuperAdmin hoặc Worker Background không có HTTP Context).
    ///              - Restricted (Tài khoản người dùng thông thường bị giới hạn theo OrgId).
    /// Created date: 17/08/2026
    /// </summary>
    [Collection("api")]
    public class VwOrgAccessServiceTests(Host host)
    {
        private const string TestPrefix = "TEST_VWORG_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly IHttpContextAccessor _httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();

        // VwOrgAccessService là Scoped nên phải resolve qua scope riêng (gate module đã nằm ở constructor)
        private VwOrgAccessService GetOrgAccessService(VwDeviceOptions? customDeviceOptions = null)
        {
            var scope = host.Services.CreateScope();
            if (customDeviceOptions == null)
                return scope.ServiceProvider.GetRequiredService<VwOrgAccessService>();

            return new VwOrgAccessService(
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwController>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwScreen>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwSource>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwScene>>(),
                scope.ServiceProvider.GetRequiredService<UserManager>(),
                Microsoft.Extensions.Options.Options.Create(customDeviceOptions),
                scope.ServiceProvider.GetRequiredService<ILogger<VwOrgAccessService>>());
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

        #region FullAccess Tests

        /// <summary>
        /// Author: Đạt
        /// Description: Trong môi trường Background/Test không có User Context, IsFullAccess luôn là true
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwOrgAccessService_NoUserContext_HasFullAccess_Test()
        {
            var srv = GetOrgAccessService();
            var scope = await srv.GetScopeAsync();

            Assert.True(srv.IsFullAccess);
            Assert.True(scope.IsFullAccess);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Với quyền FullAccess, EnsureControllerAccessAsync cho phép truy cập mọi ControllerId
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwOrgAccessService_FullAccess_EnsureControllerAccess_Succeeds_Test()
        {
            var ex = await Record.ExceptionAsync(() =>
                GetOrgAccessService().EnsureControllerAccessAsync("any_controller_id"));

            Assert.Null(ex);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Với quyền FullAccess, ResolveOrgIdAsync giữ nguyên OrgId được truyền vào
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwOrgAccessService_FullAccess_ResolveOrgId_ReturnsRequestedOrgId_Test()
        {
            var requestedOrg = "ORG_SPECIAL_001";

            var result = await GetOrgAccessService().ResolveOrgIdAsync(requestedOrg);

            Assert.Equal(requestedOrg, result);
        }

        #endregion

        #region Restricted User Tests

        /// <summary>
        /// Author: Đạt
        /// Description: Tài khoản thường chỉ thấy controller cùng org mình, không thấy controller org khác.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwOrgAccessService_RestrictedUser_GetScope_ReturnsOwnedControllerOnly_Test()
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

            var scope = await GetOrgAccessService().GetScopeAsync();

            Assert.False(scope.IsFullAccess);
            Assert.Contains(ownedCtrl.ID, scope.ControllerIds);
            Assert.DoesNotContain(foreignCtrl.ID, scope.ControllerIds);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Tài khoản thường thao tác được trên controller thuộc org của mình.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwOrgAccessService_RestrictedUser_EnsureControllerAccess_AllowsOwnedController_Test()
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

            var ex = await Record.ExceptionAsync(() => GetOrgAccessService().EnsureControllerAccessAsync(ownedCtrl.ID));

            Assert.Null(ex);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Tài khoản thường bị chặn khi thao tác trên controller thuộc org khác.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwOrgAccessService_RestrictedUser_EnsureControllerAccess_DeniesForeignController_Test()
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

            var ex = await Record.ExceptionAsync(() => GetOrgAccessService().EnsureControllerAccessAsync(foreignCtrl.ID));

            Assert.NotNull(ex);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đơn vị chỉ quản lý đúng 1 controller thì ResolveSceneControllerIdAsync tự suy ra controller đó.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwOrgAccessService_RestrictedUser_ResolveSceneControllerId_InfersSingleOwnedController_Test()
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

            var resolved = await GetOrgAccessService().ResolveSceneControllerIdAsync(null);

            Assert.Equal(soleCtrl.ID, resolved);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Cờ BypassPermission = true chỉ có hiệu lực ngoài Production: môi trường Development
        ///              cấp FullAccess cho tài khoản thường, còn Production phải vô hiệu hóa cờ này an toàn
        ///              (ngăn ngừa lỗ hổng phân quyền).
        /// Created date: 24/08/2026
        /// </summary>
        [Theory]
        [InlineData("Development", true)]
        [InlineData("Production", false)]
        public async Task VwOrgAccessService_BypassPermission_HonoredOnlyOutsideProduction_Test(
            string environmentName, bool expectedFullAccess)
        {
            var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            SetRestrictedUser(orgId);

            // PHẢI là App.HostEnvironment, KHÔNG phải host.Services.GetRequiredService<IHostEnvironment>().
            // Hai lời gọi đó trả về HAI instance KHÁC NHAU (đã đo bằng ReferenceEquals), mà
            // VwOrgAccessService.IsPermissionBypassed đọc qua App.HostEnvironment — set lên instance của
            // host.Services thì production code không thấy gì.
            var hostEnv = App.HostEnvironment;
            var originalEnv = hostEnv.EnvironmentName;

            try
            {
                hostEnv.EnvironmentName = environmentName;

                var srv = GetOrgAccessService(new VwDeviceOptions { BypassPermission = true });
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
    }
}
