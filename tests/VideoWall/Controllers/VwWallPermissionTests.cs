using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Dto.WallPermission;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Newtonsoft.Json;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using SqlSugar;
using System.Security.Claims;
using Wolverine;
using Xunit;

namespace Tests.Modules.VideoWall.Controllers
{
    /// <summary>
    /// Description: Kiểm thử tích hợp cho VwWallPermission (Page, GetList, GetById, Add, Update, Delete, BatchDelete).
    /// Created date: 11/09/2026
    /// </summary>
    [Collection("api")]
    public class VwWallPermissionTests
    {
        private const string TestPrefix = "TEST_VWWP_";
        private readonly IMessageBus _bus;
        private readonly ISqlSugarClient _db;
        private readonly BaseCacheService _cache;
        private readonly IStringLocalizer _localizer;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VwWallPermissionTests(Host host)
        {
            _bus = host.Services.GetRequiredService<IMessageBus>();
            _db = host.Services.GetRequiredService<ISqlSugarClient>();
            _cache = host.Services.GetRequiredService<BaseCacheService>();
            _localizer = host.Localizer;
            _httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();
            SetSuperAdminUser();
        }

        private void SetSuperAdminUser()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimConst.AccountType, "111"),
                new Claim(ClaimConst.UserId, "test-superadmin-id"),
                new Claim(ClaimConst.Account, "superadmin"),
                new Claim(ClaimTypes.Name, "superadmin")
            }, "TestAuth");

            _httpContextAccessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        }

        private void SetRestrictedUser()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimConst.AccountType, "333"),
                new Claim(ClaimConst.UserId, "test-user-id"),
                new Claim(ClaimConst.Account, "normal_user"),
                new Claim(ClaimTypes.Name, "normal_user")
            }, "TestAuth");

            _httpContextAccessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        }

        /// <summary>
        /// Description: Kiểm tra phân trang VwWallPermission trả về danh sách hợp lệ
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionQuery_Page_ReturnsSuccess_Test()
        {
            var input = new VwPageWallPermissionInput
            {
                Page = 1,
                PageSize = 10
            };
            var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageWallPermissionOutput>>(input);
            Assert.NotNull(result);
            Assert.NotNull(result.Records);
        }

        /// <summary>
        /// Description: Kiểm tra GetList VwWallPermission trả về danh sách
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionQuery_GetList_ReturnsSuccess_Test()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWallPermission);
            var input = new VwWallPermissionInput();
            var result = await _bus.InvokeAsync<List<VwWallPermissionOutput>>(input);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Description: Kiểm tra GetById VwWallPermission trả về đúng bản ghi đã tạo
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionQuery_GetById_ReturnsSuccess_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var permission = new VwWallPermission
            {
                UserId = testUserId,
                Config = "[{\"Col\":0,\"Row\":0}]",
                Description = "GetById test",
                CreateTime = DateTime.Now
            };
            await _db.Insertable(permission).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWallPermission);

            var input = new VwIdWallPermissionInput
            {
                ID = permission.ID
            };
            var result = await _bus.InvokeAsync<VwWallPermissionOutput>(input);

            Assert.NotNull(result);
            Assert.Equal(testUserId, result.UserId);

            // Cleanup
            await _db.Deleteable<VwWallPermission>()
                .Where(p => p.ID == permission.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Thêm mới phân quyền với UserId hợp lệ ghi nhận bản ghi vào CSDL
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionCommand_Add_ValidUserId_InsertsRecord_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var config = "[{\"Col\":0,\"Row\":0},{\"Col\":1,\"Row\":0}]";
            var input = new VwAddWallPermissionInput
            {
                UserId = testUserId,
                Config = config,
                Description = "Phân quyền user cụ thể"
            };

            var validator = new VwAddWallPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwWallPermission>()
                .FirstAsync(p => p.UserId == testUserId && p.IsDelete == null);

            Assert.NotNull(inserted);
            Assert.Equal(config, inserted.Config);

            // Cleanup
            await _db.Deleteable<VwWallPermission>()
                .Where(p => p.ID == inserted.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Thêm mới phân quyền chỉ có OrgId (không có UserId) thành công
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionCommand_Add_ValidOrgIdOnly_InsertsRecord_Test()
        {
            var testOrgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var config = "[{\"Col\":2,\"Row\":1}]";
            var input = new VwAddWallPermissionInput
            {
                OrgId = testOrgId,
                Config = config,
                Description = "Phân quyền cho cả đơn vị"
            };

            var validator = new VwAddWallPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwWallPermission>()
                .FirstAsync(p => p.OrgId == testOrgId && p.IsDelete == null);

            Assert.NotNull(inserted);
            Assert.True(string.IsNullOrEmpty(inserted.UserId));

            // Cleanup
            await _db.Deleteable<VwWallPermission>()
                .Where(p => p.ID == inserted.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Thiếu cả UserId lẫn OrgId thì validator từ chối
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionValidator_MissingBothUserIdAndOrgId_ReturnsError_Test()
        {
            var input = new VwAddWallPermissionInput
            {
                Config = "[{\"Col\":0,\"Row\":0}]"
            };

            var validator = new VwAddWallPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);

            Assert.False(valResult.IsValid);
        }

        /// <summary>
        /// Description: Thêm lần 2 cho cùng UserId đã có bản ghi thì bị từ chối (duplicate actor)
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionCommand_Add_DuplicateActorUserId_ThrowsOops_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var input1 = new VwAddWallPermissionInput
            {
                UserId = testUserId,
                Config = "[{\"Col\":0,\"Row\":0}]"
            };
            await _bus.InvokeAsync(input1);

            var input2 = new VwAddWallPermissionInput
            {
                UserId = testUserId,
                Config = "[{\"Col\":1,\"Row\":1}]"
            };

            await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input2));

            // Cleanup
            await _db.Deleteable<VwWallPermission>()
                .Where(p => p.UserId == testUserId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Thêm lần 2 cho cùng OrgId (không UserId) đã có bản ghi thì bị từ chối
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionCommand_Add_DuplicateActorOrgId_ThrowsOops_Test()
        {
            var testOrgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var input1 = new VwAddWallPermissionInput
            {
                OrgId = testOrgId,
                Config = "[{\"Col\":0,\"Row\":0}]"
            };
            await _bus.InvokeAsync(input1);

            var input2 = new VwAddWallPermissionInput
            {
                OrgId = testOrgId,
                Config = "[{\"Col\":1,\"Row\":1}]"
            };

            await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input2));

            // Cleanup
            await _db.Deleteable<VwWallPermission>()
                .Where(p => p.OrgId == testOrgId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Config có ô lưới trùng lặp trong mảng thì validator từ chối
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionValidator_DuplicateCellsInConfig_ReturnsError_Test()
        {
            var input = new VwAddWallPermissionInput
            {
                UserId = $"{TestPrefix}USER_{Guid.NewGuid():N}",
                Config = "[{\"Col\":0,\"Row\":0},{\"Col\":0,\"Row\":0}]"
            };

            var validator = new VwAddWallPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);

            Assert.False(valResult.IsValid);
        }

        /// <summary>
        /// Description: Config có toạ độ vượt quá số cột hoặc số hàng mặc định thì validator từ chối
        /// Created date: 11/09/2026
        /// </summary>
        [Theory]
        [InlineData(8, 0)]  // Cột 8 vượt quá 0..7
        [InlineData(0, 4)]  // Hàng 4 vượt quá 0..3
        [InlineData(-1, 0)] // Cột âm
        public async Task VwWallPermissionValidator_CellOutOfBounds_ReturnsError_Test(int col, int row)
        {
            var input = new VwAddWallPermissionInput
            {
                UserId = $"{TestPrefix}USER_{Guid.NewGuid():N}",
                Config = $"[{{\"Col\":{col},\"Row\":{row}}}]"
            };

            var validator = new VwAddWallPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);

            Assert.False(valResult.IsValid);
        }

        /// <summary>
        /// Description: Cập nhật Config của bản ghi đã có thì CSDL phản ánh cấu hình mới
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionCommand_Update_ModifiesConfig_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var initialConfig = "[{\"Col\":0,\"Row\":0}]";
            var addInput = new VwAddWallPermissionInput
            {
                UserId = testUserId,
                Config = initialConfig
            };
            await _bus.InvokeAsync(addInput);

            var created = await _db.Queryable<VwWallPermission>()
                .FirstAsync(p => p.UserId == testUserId && p.IsDelete == null);

            var updatedConfig = "[{\"Col\":1,\"Row\":1},{\"Col\":2,\"Row\":2}]";
            var updateInput = new VwUpdateWallPermissionInput
            {
                ID = created.ID,
                UserId = testUserId,
                Config = updatedConfig,
                Description = "Đã cập nhật vùng"
            };
            await _bus.InvokeAsync(updateInput);

            var updated = await _db.Queryable<VwWallPermission>()
                .FirstAsync(p => p.ID == created.ID);

            Assert.Equal(updatedConfig, updated.Config);
            Assert.Equal("Đã cập nhật vùng", updated.Description);

            // Cleanup
            await _db.Deleteable<VwWallPermission>()
                .Where(p => p.ID == created.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Xóa mềm bản ghi phân quyền
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionCommand_Delete_SoftDeletesRecord_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var addInput = new VwAddWallPermissionInput
            {
                UserId = testUserId,
                Config = "[{\"Col\":0,\"Row\":0}]"
            };
            await _bus.InvokeAsync(addInput);

            var created = await _db.Queryable<VwWallPermission>()
                .FirstAsync(p => p.UserId == testUserId && p.IsDelete == null);

            var deleteInput = new VwDeleteWallPermissionInput
            {
                ID = created.ID
            };
            await _bus.InvokeAsync(deleteInput);

            var deleted = await _db.Queryable<VwWallPermission>()
                .ClearFilter()
                .FirstAsync(p => p.ID == created.ID);

            Assert.NotNull(deleted);
            Assert.NotNull(deleted.IsDelete);

            // Cleanup
            await _db.Deleteable<VwWallPermission>()
                .Where(p => p.ID == created.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Tài khoản thường (không phải SuperAdmin) gọi lệnh ghi phân quyền vùng màn hình bị từ chối với Oops
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionCommand_Write_NonSuperAdmin_ThrowsOops_Test()
        {
            SetRestrictedUser();
            try
            {
                var input = new VwAddWallPermissionInput
                {
                    UserId = $"{TestPrefix}RESTRICTED_{Guid.NewGuid():N}",
                    Config = "[{\"Col\":0,\"Row\":0}]"
                };

                var ex = await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input));
                Assert.Contains("SuperAdmin", ex.Message);
            }
            finally
            {
                _httpContextAccessor.HttpContext = null;
            }
        }

        /// <summary>
        /// Description: Request ẩn danh qua HTTP (có HttpContext nhưng không có User) bị từ chối với Oops
        /// Created date: 12/09/2026
        /// </summary>
        [Fact]
        public async Task VwWallPermissionCommand_Write_AnonymousHttpRequest_ThrowsOops_Test()
        {
            _httpContextAccessor.HttpContext = new DefaultHttpContext();
            try
            {
                var input = new VwAddWallPermissionInput
                {
                    UserId = $"{TestPrefix}ANON_{Guid.NewGuid():N}",
                    Config = "[{\"Col\":0,\"Row\":0}]"
                };

                var ex = await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input));
                Assert.Contains("SuperAdmin", ex.Message);
            }
            finally
            {
                _httpContextAccessor.HttpContext = null;
            }
        }
    }
}
