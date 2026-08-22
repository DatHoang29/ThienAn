namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử tích hợp đầy đủ cho VwSchedule (Page, GetList, GetById, Add, Update, Delete, BatchDelete).
    /// Created date: 17/08/2026
    /// </summary>
    [Collection("api")]
    public class VwScheduleTests(Host host) : IDisposable
    {
        private const string TestPrefix = "TEST_VWSCHEDULE_";
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
        private readonly IStringLocalizer _localizer = host.Localizer;

        public void Dispose()
        {
            _db.Deleteable<VwSchedule>()
                .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
                .ExecuteCommand();

            _db.Deleteable<VwScene>()
                .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
                .ExecuteCommand();

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra phân trang VwSchedule trả về danh sách hợp lệ
        /// Created date: 15/08/2026
        /// </summary>
        [Theory]
        [InlineData(1, 10)]
        [InlineData(1, 5)]
        [InlineData(2, 20)]
        public async Task VwScheduleQuery_Page_ReturnsSuccess_Test(int page, int pageSize)
        {
            var input = new VwPageScheduleInput { Page = page, PageSize = pageSize };
            var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageScheduleOutput>>(input);
            Assert.NotNull(result);
            Assert.NotNull(result.Records);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetList VwSchedule trả về thành công
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScheduleQuery_GetList_ReturnsSuccess_Test()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
            var input = new VwScheduleInput();
            var result = await _bus.InvokeAsync<List<VwScheduleOutput>>(input);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetById VwSchedule trả về đúng bản ghi đã tạo
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScheduleQuery_GetById_ReturnsSuccess_Test()
        {
            var uniqueCode = $"{TestPrefix}SCH_{Guid.NewGuid():N}";
            var schedule = new VwSchedule
            {
                Code = uniqueCode,
                Name = "Morning Schedule ById",
                CronExpr = "0 8 * * *",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(schedule).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);

            var input = new VwIdScheduleInput { ID = schedule.ID };
            var result = await _bus.InvokeAsync<VwScheduleOutput>(input);

            Assert.NotNull(result);
            Assert.Equal(uniqueCode, result.Code);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra thêm mới VwSchedule ghi nhận bản ghi vào CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScheduleCommand_AddVwSchedule_InsertsRecord_Test()
        {
            var scene = new VwScene
            {
                Code = $"{TestPrefix}SCN_{Guid.NewGuid():N}",
                Name = "Scheduled Scene",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var uniqueCode = $"{TestPrefix}SCH_{Guid.NewGuid():N}";
            var input = new VwAddScheduleInput
            {
                Code = uniqueCode,
                Name = "Nightly Switch Scene",
                CronExpr = "0 22 * * *",
                TargetSceneId = scene.ID,
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwAddScheduleValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwSchedule>()
                .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);
            Assert.NotNull(inserted);
            Assert.Equal("Nightly Switch Scene", inserted.Name);
            Assert.Equal("0 22 * * *", inserted.CronExpr);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwAddScheduleValidator từ chối các input không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null, "SceneId")]
        [InlineData("", "SceneId")]
        [InlineData("ValidName", null)]
        [InlineData("ValidName", "")]
        public async Task VwScheduleCommand_AddVwSchedule_ValidationRejectsInvalidPayload_Test(string? name, string? targetSceneId)
        {
            var input = new VwAddScheduleInput
            {
                Name = name,
                TargetSceneId = targetSceneId,
                CronExpr = "0 0 * * *"
            };
            var validator = new VwAddScheduleValidator(_localizer);
            var result = await validator.ValidateAsync(input);
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra cập nhật VwSchedule thay đổi thông tin bản ghi trong CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScheduleCommand_UpdateVwSchedule_UpdatesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}SCH_{Guid.NewGuid():N}";
            var schedule = new VwSchedule
            {
                Code = uniqueCode,
                Name = "Old Schedule Name",
                CronExpr = "0 6 * * *",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(schedule).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);

            var updateInput = new VwUpdateScheduleInput
            {
                ID = schedule.ID,
                Code = uniqueCode,
                Name = "Updated Schedule Name",
                CronExpr = "0 7 * * *",
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwUpdateScheduleValidator(_localizer);
            var valResult = await validator.ValidateAsync(updateInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(updateInput);

            var updated = await _db.Queryable<VwSchedule>()
                .FirstAsync(u => u.ID == schedule.ID && u.IsDelete == null);
            Assert.NotNull(updated);
            Assert.Equal("Updated Schedule Name", updated.Name);
            Assert.Equal("0 7 * * *", updated.CronExpr);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwUpdateScheduleValidator từ chối khi ID rỗng hoặc null
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwScheduleCommand_UpdateVwSchedule_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwUpdateScheduleValidator(_localizer);
            var result = await validator.ValidateAsync(new VwUpdateScheduleInput { ID = invalidId, Name = "Name", TargetSceneId = "scene-1" });
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwDeleteScheduleValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwScheduleCommand_DeleteVwSchedule_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwDeleteScheduleValidator(_localizer);
            var invalidResult = await validator.ValidateAsync(new VwDeleteScheduleInput { ID = invalidId });
            Assert.False(invalidResult.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa 1 VwSchedule thực hiện xóa mềm (IsDelete != null)
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScheduleCommand_DeleteVwSchedule_SoftDeletesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}SCH_{Guid.NewGuid():N}";
            var schedule = new VwSchedule
            {
                Code = uniqueCode,
                Name = "To Delete",
                CronExpr = "0 0 * * *",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(schedule).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);

            var deleteInput = new VwDeleteScheduleInput { ID = schedule.ID };
            var validator = new VwDeleteScheduleValidator(_localizer);
            var valResult = await validator.ValidateAsync(deleteInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(deleteInput);

            var active = await _db.Queryable<VwSchedule>()
                .FirstAsync(u => u.ID == schedule.ID && u.IsDelete == null);
            Assert.Null(active);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa nhiều VwSchedule thực hiện xóa mềm danh sách bản ghi
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwScheduleCommand_BatchDeleteVwSchedule_SoftDeletesRecords_Test()
        {
            var validator = new VwDeleteScheduleValidator(_localizer);
            var code1 = $"{TestPrefix}SCH_{Guid.NewGuid():N}";
            var code2 = $"{TestPrefix}SCH_{Guid.NewGuid():N}";

            var sch1 = new VwSchedule { Code = code1, Name = "Batch 1", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            var sch2 = new VwSchedule { Code = code2, Name = "Batch 2", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            await _db.Insertable(new[] { sch1, sch2 }).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);

            var batchInput = new List<VwDeleteScheduleInput> { new() { ID = sch1.ID }, new() { ID = sch2.ID } };

            foreach (var item in batchInput)
            {
                var valResult = await validator.ValidateAsync(item);
                Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));
            }

            await _bus.InvokeAsync(batchInput);

            var countActive = await _db.Queryable<VwSchedule>()
                .Where(u => (u.ID == sch1.ID || u.ID == sch2.ID) && u.IsDelete == null)
                .CountAsync();
            Assert.Equal(0, countActive);
        }
    }
}
