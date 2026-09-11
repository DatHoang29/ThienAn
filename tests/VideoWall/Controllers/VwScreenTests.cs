namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử tích hợp đầy đủ cho VwScreen (Page, GetList, GetById, Add, Update, Delete, BatchDelete).
    /// Created date: 17/08/2026
    /// </summary>
    [Collection("api")]
    public class VwScreenTests(Host host)
    {
        private const string TestPrefix = "TEST_VWSCREEN_";
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
        private readonly IStringLocalizer _localizer = host.Localizer;

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra phân trang VwScreen trả về danh sách hợp lệ
        /// Created date: 15/08/2026
        /// </summary>
        [Theory]
        [InlineData(1, 10)]
        [InlineData(1, 5)]
        [InlineData(2, 20)]
        public async Task VwScreenQuery_Page_ReturnsSuccess_Test(int page, int pageSize)
        {
            var input = new VwPageScreenInput { Page = page, PageSize = pageSize };
            var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageScreenOutput>>(input);
            Assert.NotNull(result);
            Assert.NotNull(result.Records);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetList VwScreen trả về thành công
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScreenQuery_GetList_ReturnsSuccess_Test()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
            var input = new VwScreenInput();
            var result = await _bus.InvokeAsync<List<VwScreenOutput>>(input);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetById VwScreen trả về đúng bản ghi đã tạo
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScreenQuery_GetById_ReturnsSuccess_Test()
        {
            var uniqueCode = $"{TestPrefix}SCR_{Guid.NewGuid():N}";
            var screen = new VwScreen
            {
                Code = uniqueCode,
                Name = "Test Screen ById",
                GridCol = 0,
                GridRow = 0,
                Resolution = "1920x1080",
                WidthPx = "1920",
                HeightPx = "1080",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);

            var input = new VwIdScreenInput { ID = screen.ID };
            var result = await _bus.InvokeAsync<VwScreenOutput>(input);

            Assert.NotNull(result);
            Assert.Equal(uniqueCode, result.Code);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra thêm mới VwScreen ghi nhận bản ghi vào CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScreenCommand_AddVwScreen_InsertsRecord_Test()
        {
            var ctrl = new VwController
            {
                Code = $"{TestPrefix}CTRL_{Guid.NewGuid():N}",
                Name = "Ctrl For Screen",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(ctrl).ExecuteCommandAsync();

            var uniqueCode = $"{TestPrefix}SCR_{Guid.NewGuid():N}";
            var input = new VwAddScreenInput
            {
                Code = uniqueCode,
                Name = "Screen Panel (0,0)",
                ControllerId = ctrl.ID,
                GridCol = 0,
                GridRow = 0,
                Resolution = "1920x1080",
                WidthPx = "1920",
                HeightPx = "1080",
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwAddScreenValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwScreen>()
                .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);
            Assert.NotNull(inserted);
            Assert.Equal("Screen Panel (0,0)", inserted.Name);
            Assert.Equal(ctrl.ID, inserted.ControllerId);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwAddScreenValidator từ chối các input không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null, "ctrl-1")]
        [InlineData("", "ctrl-1")]
        [InlineData("ValidName", null)]
        [InlineData("ValidName", "")]
        public async Task VwScreenCommand_AddVwScreen_ValidationRejectsInvalidPayload_Test(string? name, string? controllerId)
        {
            var input = new VwAddScreenInput
            {
                Name = name,
                ControllerId = controllerId
            };
            var validator = new VwAddScreenValidator(_localizer);
            var result = await validator.ValidateAsync(input);
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra cập nhật VwScreen thay đổi thông tin bản ghi trong CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScreenCommand_UpdateVwScreen_UpdatesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}SCR_{Guid.NewGuid():N}";
            var screen = new VwScreen
            {
                Code = uniqueCode,
                Name = "Original Screen Name",
                GridCol = 0,
                GridRow = 0,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);

            var updateInput = new VwUpdateScreenInput
            {
                ID = screen.ID,
                Code = uniqueCode,
                Name = "Updated Screen Name",
                GridCol = 1,
                GridRow = 0,
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwUpdateScreenValidator(_localizer);
            var valResult = await validator.ValidateAsync(updateInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(updateInput);

            var updated = await _db.Queryable<VwScreen>()
                .FirstAsync(u => u.ID == screen.ID && u.IsDelete == null);
            Assert.NotNull(updated);
            Assert.Equal("Updated Screen Name", updated.Name);
            Assert.Equal(1, updated.GridCol);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwUpdateScreenValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwScreenCommand_UpdateVwScreen_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwUpdateScreenValidator(_localizer);
            var result = await validator.ValidateAsync(new VwUpdateScreenInput { ID = invalidId, Name = "Name", ControllerId = "ctrl-1" });
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwDeleteScreenValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwScreenCommand_DeleteVwScreen_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwDeleteScreenValidator(_localizer);
            var invalidResult = await validator.ValidateAsync(new VwDeleteScreenInput { ID = invalidId });
            Assert.False(invalidResult.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa 1 VwScreen thực hiện xóa mềm (IsDelete != null)
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScreenCommand_DeleteVwScreen_SoftDeletesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}SCR_{Guid.NewGuid():N}";
            var screen = new VwScreen
            {
                Code = uniqueCode,
                Name = "Screen To Delete",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);

            var deleteInput = new VwDeleteScreenInput { ID = screen.ID };
            var validator = new VwDeleteScreenValidator(_localizer);
            var valResult = await validator.ValidateAsync(deleteInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(deleteInput);

            var active = await _db.Queryable<VwScreen>()
                .FirstAsync(u => u.ID == screen.ID && u.IsDelete == null);
            Assert.Null(active);

            var deleted = await _db.Queryable<VwScreen>()
                .ClearFilter()
                .FirstAsync(u => u.ID == screen.ID && u.IsDelete != null);
            Assert.NotNull(deleted);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa nhiều VwScreen thực hiện xóa mềm danh sách bản ghi
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScreenCommand_BatchDeleteVwScreen_SoftDeletesRecords_Test()
        {
            var validator = new VwDeleteScreenValidator(_localizer);
            var code1 = $"{TestPrefix}SCR_{Guid.NewGuid():N}";
            var code2 = $"{TestPrefix}SCR_{Guid.NewGuid():N}";

            var s1 = new VwScreen { Code = code1, Name = "Batch Screen 1", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            var s2 = new VwScreen { Code = code2, Name = "Batch Screen 2", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            await _db.Insertable(new[] { s1, s2 }).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);

            var batchInput = new List<VwDeleteScreenInput> { new() { ID = s1.ID }, new() { ID = s2.ID } };

            foreach (var item in batchInput)
            {
                var valResult = await validator.ValidateAsync(item);
                Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));
            }

            await _bus.InvokeAsync(batchInput);

            var countActive = await _db.Queryable<VwScreen>()
                .Where(u => (u.ID == s1.ID || u.ID == s2.ID) && u.IsDelete == null)
                .CountAsync();
            Assert.Equal(0, countActive);
        }
    }
}
