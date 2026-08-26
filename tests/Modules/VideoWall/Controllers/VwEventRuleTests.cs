namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử tích hợp đầy đủ cho VwEventRule (Page, GetList, GetById, Add, Update, Delete, BatchDelete).
    /// Created date: 17/08/2026
    /// </summary>
    [Collection("api")]
    public class VwEventRuleTests(Host host)
    {
        private const string TestPrefix = "TEST_VWEVENTRULE_";
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
        private readonly IStringLocalizer _localizer = host.Localizer;

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra phân trang VwEventRule trả về danh sách hợp lệ
        /// Created date: 15/08/2026
        /// </summary>
        [Theory]
        [InlineData(1, 10)]
        [InlineData(1, 5)]
        [InlineData(2, 20)]
        public async Task VwEventRuleQuery_Page_ReturnsSuccess_Test(int page, int pageSize)
        {
            var input = new VwPageEventRuleInput { Page = page, PageSize = pageSize };
            var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageEventRuleOutput>>(input);
            Assert.NotNull(result);
            Assert.NotNull(result.Records);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetList VwEventRule trả về thành công
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwEventRuleQuery_GetList_ReturnsSuccess_Test()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);
            var input = new VwEventRuleInput();
            var result = await _bus.InvokeAsync<List<VwEventRuleOutput>>(input);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetById VwEventRule trả về đúng bản ghi đã tạo
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwEventRuleQuery_GetById_ReturnsSuccess_Test()
        {
            var uniqueCode = $"{TestPrefix}RULE_{Guid.NewGuid():N}";
            var rule = new VwEventRule
            {
                Code = uniqueCode,
                EventTypeId = "EVT_ACCIDENT",
                Priority = "HIGH",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(rule).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);

            var input = new VwIdEventRuleInput { ID = rule.ID };
            var result = await _bus.InvokeAsync<VwEventRuleOutput>(input);

            Assert.NotNull(result);
            Assert.Equal(uniqueCode, result.Code);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra thêm mới VwEventRule ghi nhận bản ghi vào CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwEventRuleCommand_AddVwEventRule_InsertsRecord_Test()
        {
            var scene = new VwScene
            {
                Code = $"{TestPrefix}SCN_{Guid.NewGuid():N}",
                Name = "Target Scene",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var uniqueCode = $"{TestPrefix}RULE_{Guid.NewGuid():N}";
            var input = new VwAddEventRuleInput
            {
                Code = uniqueCode,
                EventTypeId = "EVT_TRAFFIC_JAM",
                TargetSceneId = scene.ID,
                Priority = "NORMAL",
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwAddEventRuleValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwEventRule>()
                .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);
            Assert.NotNull(inserted);
            Assert.Equal("EVT_TRAFFIC_JAM", inserted.EventTypeId);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwAddEventRuleValidator từ chối các input không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null, "SceneId")]
        [InlineData("", "SceneId")]
        [InlineData("EVT_ACCIDENT", null)]
        [InlineData("EVT_ACCIDENT", "")]
        public async Task VwEventRuleCommand_AddVwEventRule_ValidationRejectsInvalidPayload_Test(string? eventTypeId, string? targetSceneId)
        {
            var input = new VwAddEventRuleInput
            {
                EventTypeId = eventTypeId,
                TargetSceneId = targetSceneId
            };
            var validator = new VwAddEventRuleValidator(_localizer);
            var result = await validator.ValidateAsync(input);
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra cập nhật VwEventRule thay đổi thông tin bản ghi trong CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwEventRuleCommand_UpdateVwEventRule_UpdatesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}RULE_{Guid.NewGuid():N}";
            var rule = new VwEventRule
            {
                Code = uniqueCode,
                EventTypeId = "EVT_WEATHER",
                Priority = "LOW",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(rule).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);

            var updateInput = new VwUpdateEventRuleInput
            {
                ID = rule.ID,
                Code = uniqueCode,
                EventTypeId = "EVT_WEATHER_STORM",
                Priority = "HIGH",
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwUpdateEventRuleValidator(_localizer);
            var valResult = await validator.ValidateAsync(updateInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(updateInput);

            var updated = await _db.Queryable<VwEventRule>()
                .FirstAsync(u => u.ID == rule.ID && u.IsDelete == null);
            Assert.NotNull(updated);
            Assert.Equal("EVT_WEATHER_STORM", updated.EventTypeId);
            Assert.Equal("HIGH", updated.Priority);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwUpdateEventRuleValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwEventRuleCommand_UpdateVwEventRule_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwUpdateEventRuleValidator(_localizer);
            var result = await validator.ValidateAsync(new VwUpdateEventRuleInput { ID = invalidId, EventTypeId = "EVT_1", TargetSceneId = "scene-1" });
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwDeleteEventRuleValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwEventRuleCommand_DeleteVwEventRule_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwDeleteEventRuleValidator(_localizer);
            var invalidResult = await validator.ValidateAsync(new VwDeleteEventRuleInput { ID = invalidId });
            Assert.False(invalidResult.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa 1 VwEventRule thực hiện xóa mềm (IsDelete != null)
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwEventRuleCommand_DeleteVwEventRule_SoftDeletesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}RULE_{Guid.NewGuid():N}";
            var rule = new VwEventRule
            {
                Code = uniqueCode,
                EventTypeId = "EVT_TEMP",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(rule).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);

            var deleteInput = new VwDeleteEventRuleInput { ID = rule.ID };
            var validator = new VwDeleteEventRuleValidator(_localizer);
            var valResult = await validator.ValidateAsync(deleteInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(deleteInput);

            var active = await _db.Queryable<VwEventRule>()
                .FirstAsync(u => u.ID == rule.ID && u.IsDelete == null);
            Assert.Null(active);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa nhiều VwEventRule thực hiện xóa mềm danh sách bản ghi
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwEventRuleCommand_BatchDeleteVwEventRule_SoftDeletesRecords_Test()
        {
            var validator = new VwDeleteEventRuleValidator(_localizer);
            var code1 = $"{TestPrefix}RULE_{Guid.NewGuid():N}";
            var code2 = $"{TestPrefix}RULE_{Guid.NewGuid():N}";

            var r1 = new VwEventRule { Code = code1, EventTypeId = "EVT_1", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            var r2 = new VwEventRule { Code = code2, EventTypeId = "EVT_2", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            await _db.Insertable(new[] { r1, r2 }).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);

            var batchInput = new List<VwDeleteEventRuleInput> { new() { ID = r1.ID }, new() { ID = r2.ID } };

            foreach (var item in batchInput)
            {
                var valResult = await validator.ValidateAsync(item);
                Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));
            }

            await _bus.InvokeAsync(batchInput);

            var countActive = await _db.Queryable<VwEventRule>()
                .Where(u => (u.ID == r1.ID || u.ID == r2.ID) && u.IsDelete == null)
                .CountAsync();
            Assert.Equal(0, countActive);
        }
    }
}
