using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Dto.UserAreaPermission;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Newtonsoft.Json;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using SqlSugar;
using Wolverine;
using Xunit;

namespace Tests.Modules.VideoWall.Controllers
{
    /// <summary>
    /// Description: Kiểm thử tích hợp cho VwUserAreaPermission (Page, GetList, GetById, Add, Update, Delete, BatchDelete).
    /// Created date: 11/09/2026
    /// </summary>
    [Collection("api")]
    public class VwUserAreaPermissionTests(Host host)
    {
        private const string TestPrefix = "TEST_VWUAP_";
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
        private readonly IStringLocalizer _localizer = host.Localizer;

        /// <summary>
        /// Description: Kiểm tra phân trang VwUserAreaPermission trả về danh sách hợp lệ
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionQuery_Page_ReturnsSuccess_Test()
        {
            var input = new VwPageUserAreaPermissionInput
            {
                Page = 1,
                PageSize = 10
            };
            var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageUserAreaPermissionOutput>>(input);
            Assert.NotNull(result);
            Assert.NotNull(result.Records);
        }

        /// <summary>
        /// Description: Kiểm tra GetList VwUserAreaPermission trả về danh sách
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionQuery_GetList_ReturnsSuccess_Test()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwUserAreaPermission);
            var input = new VwUserAreaPermissionInput();
            var result = await _bus.InvokeAsync<List<VwUserAreaPermissionOutput>>(input);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Description: Kiểm tra GetById VwUserAreaPermission trả về đúng bản ghi đã tạo
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionQuery_GetById_ReturnsSuccess_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var permission = new VwUserAreaPermission
            {
                UserId = testUserId,
                Config = "[{\"Col\":0,\"Row\":0}]",
                Description = "GetById test",
                CreateTime = DateTime.Now
            };
            await _db.Insertable(permission).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwUserAreaPermission);

            var input = new VwIdUserAreaPermissionInput
            {
                ID = permission.ID
            };
            var result = await _bus.InvokeAsync<VwUserAreaPermissionOutput>(input);

            Assert.NotNull(result);
            Assert.Equal(testUserId, result.UserId);

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.ID == permission.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Thêm mới phân quyền với UserId hợp lệ ghi nhận bản ghi vào CSDL
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionCommand_Add_ValidUserId_InsertsRecord_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var config = "[{\"Col\":0,\"Row\":0},{\"Col\":1,\"Row\":0}]";
            var input = new VwAddUserAreaPermissionInput
            {
                UserId = testUserId,
                Config = config,
                Description = "Phân quyền user cụ thể"
            };

            var validator = new VwAddUserAreaPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwUserAreaPermission>()
                .FirstAsync(p => p.UserId == testUserId && p.IsDelete == null);

            Assert.NotNull(inserted);
            Assert.Equal(config, inserted.Config);

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.ID == inserted.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Thêm mới phân quyền chỉ có OrgId (không có UserId) thành công
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionCommand_Add_ValidOrgIdOnly_InsertsRecord_Test()
        {
            var testOrgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var config = "[{\"Col\":2,\"Row\":1}]";
            var input = new VwAddUserAreaPermissionInput
            {
                OrgId = testOrgId,
                Config = config,
                Description = "Phân quyền cho cả đơn vị"
            };

            var validator = new VwAddUserAreaPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwUserAreaPermission>()
                .FirstAsync(p => p.OrgId == testOrgId && p.IsDelete == null);

            Assert.NotNull(inserted);
            Assert.True(string.IsNullOrEmpty(inserted.UserId));

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.ID == inserted.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Thiếu cả UserId lẫn OrgId thì validator từ chối
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionValidator_MissingBothUserIdAndOrgId_ReturnsError_Test()
        {
            var input = new VwAddUserAreaPermissionInput
            {
                Config = "[{\"Col\":0,\"Row\":0}]"
            };

            var validator = new VwAddUserAreaPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);

            Assert.False(valResult.IsValid);
        }

        /// <summary>
        /// Description: Thêm lần 2 cho cùng UserId đã có bản ghi thì bị từ chối (duplicate actor)
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionCommand_Add_DuplicateActorUserId_ThrowsOops_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var input1 = new VwAddUserAreaPermissionInput
            {
                UserId = testUserId,
                Config = "[{\"Col\":0,\"Row\":0}]"
            };
            await _bus.InvokeAsync(input1);

            var input2 = new VwAddUserAreaPermissionInput
            {
                UserId = testUserId,
                Config = "[{\"Col\":1,\"Row\":1}]"
            };

            await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input2));

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.UserId == testUserId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Thêm lần 2 cho cùng OrgId (không UserId) đã có bản ghi thì bị từ chối
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionCommand_Add_DuplicateActorOrgId_ThrowsOops_Test()
        {
            var testOrgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
            var input1 = new VwAddUserAreaPermissionInput
            {
                OrgId = testOrgId,
                Config = "[{\"Col\":0,\"Row\":0}]"
            };
            await _bus.InvokeAsync(input1);

            var input2 = new VwAddUserAreaPermissionInput
            {
                OrgId = testOrgId,
                Config = "[{\"Col\":1,\"Row\":1}]"
            };

            await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input2));

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.OrgId == testOrgId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Config có ô lưới trùng lặp trong mảng thì validator từ chối
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionValidator_DuplicateCellsInConfig_ReturnsError_Test()
        {
            var input = new VwAddUserAreaPermissionInput
            {
                UserId = $"{TestPrefix}USER_{Guid.NewGuid():N}",
                Config = "[{\"Col\":0,\"Row\":0},{\"Col\":0,\"Row\":0}]"
            };

            var validator = new VwAddUserAreaPermissionValidator(_localizer);
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
        public async Task VwUserAreaPermissionValidator_CellOutOfBounds_ReturnsError_Test(int col, int row)
        {
            var input = new VwAddUserAreaPermissionInput
            {
                UserId = $"{TestPrefix}USER_{Guid.NewGuid():N}",
                Config = $"[{{\"Col\":{col},\"Row\":{row}}}]"
            };

            var validator = new VwAddUserAreaPermissionValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);

            Assert.False(valResult.IsValid);
        }

        /// <summary>
        /// Description: Cập nhật Config của bản ghi đã có thì CSDL phản ánh cấu hình mới
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionCommand_Update_ModifiesConfig_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var initialConfig = "[{\"Col\":0,\"Row\":0}]";
            var addInput = new VwAddUserAreaPermissionInput
            {
                UserId = testUserId,
                Config = initialConfig
            };
            await _bus.InvokeAsync(addInput);

            var created = await _db.Queryable<VwUserAreaPermission>()
                .FirstAsync(p => p.UserId == testUserId && p.IsDelete == null);

            var updatedConfig = "[{\"Col\":1,\"Row\":1},{\"Col\":2,\"Row\":2}]";
            var updateInput = new VwUpdateUserAreaPermissionInput
            {
                ID = created.ID,
                UserId = testUserId,
                Config = updatedConfig,
                Description = "Đã cập nhật vùng"
            };
            await _bus.InvokeAsync(updateInput);

            var updated = await _db.Queryable<VwUserAreaPermission>()
                .FirstAsync(p => p.ID == created.ID);

            Assert.Equal(updatedConfig, updated.Config);
            Assert.Equal("Đã cập nhật vùng", updated.Description);

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.ID == created.ID)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Description: Xóa mềm bản ghi phân quyền
        /// Created date: 11/09/2026
        /// </summary>
        [Fact]
        public async Task VwUserAreaPermissionCommand_Delete_SoftDeletesRecord_Test()
        {
            var testUserId = $"{TestPrefix}USER_{Guid.NewGuid():N}";
            var addInput = new VwAddUserAreaPermissionInput
            {
                UserId = testUserId,
                Config = "[{\"Col\":0,\"Row\":0}]"
            };
            await _bus.InvokeAsync(addInput);

            var created = await _db.Queryable<VwUserAreaPermission>()
                .FirstAsync(p => p.UserId == testUserId && p.IsDelete == null);

            var deleteInput = new VwDeleteUserAreaPermissionInput
            {
                ID = created.ID
            };
            await _bus.InvokeAsync(deleteInput);

            var deleted = await _db.Queryable<VwUserAreaPermission>()
                .ClearFilter()
                .FirstAsync(p => p.ID == created.ID);

            Assert.NotNull(deleted);
            Assert.NotNull(deleted.IsDelete);

            // Cleanup
            await _db.Deleteable<VwUserAreaPermission>()
                .Where(p => p.ID == created.ID)
                .ExecuteCommandAsync();
        }
    }
}
