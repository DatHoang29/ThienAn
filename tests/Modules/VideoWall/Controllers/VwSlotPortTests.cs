namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử tích hợp đầy đủ cho VwSlotPort (Page, GetList, GetById, Add, Update, Delete, BatchDelete).
    /// Created date: 17/08/2026
    /// </summary>
    [Collection("api")]
    public class VwSlotPortTests(Host host) : IDisposable
    {
        private const string TestPrefix = "TEST_VWSLOTPORT_";
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
        private readonly IStringLocalizer _localizer = host.Localizer;

        public void Dispose()
        {
            _db.Deleteable<VwSlotPort>()
                .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
                .ExecuteCommand();

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSlotPort);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra phân trang VwSlotPort trả về danh sách hợp lệ
        /// Created date: 15/08/2026
        /// </summary>
        [Theory]
        [InlineData(1, 10)]
        [InlineData(1, 5)]
        [InlineData(2, 20)]
        public async Task VwSlotPortQuery_Page_ReturnsSuccess_Test(int page, int pageSize)
        {
            var input = new VwPageSlotPortInput { Page = page, PageSize = pageSize };
            var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageSlotPortOutput>>(input);
            Assert.NotNull(result);
            Assert.NotNull(result.Records);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetList VwSlotPort trả về thành công
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSlotPortQuery_GetList_ReturnsSuccess_Test()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSlotPort);
            var input = new VwSlotPortInput();
            var result = await _bus.InvokeAsync<List<VwSlotPortOutput>>(input);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetById VwSlotPort trả về đúng bản ghi đã tạo
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSlotPortQuery_GetById_ReturnsSuccess_Test()
        {
            var uniqueCode = $"{TestPrefix}PORT_{Guid.NewGuid():N}";
            var slotPort = new VwSlotPort
            {
                Code = uniqueCode,
                Name = "Port HDMI In 1",
                PortNo = "1",
                PortType = "HDMI_IN",
                GlobalIndex = 1,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(slotPort).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSlotPort);

            var input = new VwIdSlotPortInput { ID = slotPort.ID };
            var result = await _bus.InvokeAsync<VwSlotPortOutput>(input);

            Assert.NotNull(result);
            Assert.Equal(uniqueCode, result.Code);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra thêm mới VwSlotPort ghi nhận bản ghi vào CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSlotPortCommand_AddVwSlotPort_InsertsRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}PORT_{Guid.NewGuid():N}";
            var input = new VwAddSlotPortInput
            {
                Code = uniqueCode,
                Name = "Port DVI In 2",
                PortNo = "2",
                PortType = "DVI_IN",
                GlobalIndex = 2,
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwAddSlotPortValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwSlotPort>()
                .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);
            Assert.NotNull(inserted);
            Assert.Equal("Port DVI In 2", inserted.Name);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwAddSlotPortValidator từ chối các input không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null, "1")]
        [InlineData("", "1")]
        [InlineData("ValidName", null)]
        [InlineData("ValidName", "")]
        public async Task VwSlotPortCommand_AddVwSlotPort_ValidationRejectsInvalidPayload_Test(string? name, string? portNo)
        {
            var input = new VwAddSlotPortInput
            {
                Name = name,
                PortNo = portNo
            };
            var validator = new VwAddSlotPortValidator(_localizer);
            var result = await validator.ValidateAsync(input);
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra cập nhật VwSlotPort thay đổi thông tin bản ghi trong CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSlotPortCommand_UpdateVwSlotPort_UpdatesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}PORT_{Guid.NewGuid():N}";
            var slotPort = new VwSlotPort
            {
                Code = uniqueCode,
                Name = "Original Port Name",
                PortNo = "1",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(slotPort).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSlotPort);

            var updateInput = new VwUpdateSlotPortInput
            {
                ID = slotPort.ID,
                Code = uniqueCode,
                Name = "Updated Port Name",
                PortNo = "1",
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwUpdateSlotPortValidator(_localizer);
            var valResult = await validator.ValidateAsync(updateInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(updateInput);

            var updated = await _db.Queryable<VwSlotPort>()
                .FirstAsync(u => u.ID == slotPort.ID && u.IsDelete == null);
            Assert.NotNull(updated);
            Assert.Equal("Updated Port Name", updated.Name);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwUpdateSlotPortValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwSlotPortCommand_UpdateVwSlotPort_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwUpdateSlotPortValidator(_localizer);
            var result = await validator.ValidateAsync(new VwUpdateSlotPortInput { ID = invalidId, Name = "Name", PortNo = "1" });
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwDeleteSlotPortValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwSlotPortCommand_DeleteVwSlotPort_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwDeleteSlotPortValidator(_localizer);
            var invalidResult = await validator.ValidateAsync(new VwDeleteSlotPortInput { ID = invalidId });
            Assert.False(invalidResult.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa 1 VwSlotPort thực hiện xóa mềm (IsDelete != null)
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSlotPortCommand_DeleteVwSlotPort_SoftDeletesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}PORT_{Guid.NewGuid():N}";
            var slotPort = new VwSlotPort
            {
                Code = uniqueCode,
                Name = "Port To Delete",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(slotPort).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSlotPort);

            var deleteInput = new VwDeleteSlotPortInput { ID = slotPort.ID };
            var validator = new VwDeleteSlotPortValidator(_localizer);
            var valResult = await validator.ValidateAsync(deleteInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(deleteInput);

            var active = await _db.Queryable<VwSlotPort>()
                .FirstAsync(u => u.ID == slotPort.ID && u.IsDelete == null);
            Assert.Null(active);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa nhiều VwSlotPort thực hiện xóa mềm danh sách bản ghi
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSlotPortCommand_BatchDeleteVwSlotPort_SoftDeletesRecords_Test()
        {
            var validator = new VwDeleteSlotPortValidator(_localizer);
            var code1 = $"{TestPrefix}PORT_{Guid.NewGuid():N}";
            var code2 = $"{TestPrefix}PORT_{Guid.NewGuid():N}";

            var p1 = new VwSlotPort { Code = code1, Name = "Port 1", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            var p2 = new VwSlotPort { Code = code2, Name = "Port 2", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            await _db.Insertable(new[] { p1, p2 }).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSlotPort);

            var batchInput = new List<VwDeleteSlotPortInput> { new() { ID = p1.ID }, new() { ID = p2.ID } };

            foreach (var item in batchInput)
            {
                var valResult = await validator.ValidateAsync(item);
                Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));
            }

            await _bus.InvokeAsync(batchInput);

            var countActive = await _db.Queryable<VwSlotPort>()
                .Where(u => (u.ID == p1.ID || u.ID == p2.ID) && u.IsDelete == null)
                .CountAsync();
            Assert.Equal(0, countActive);
        }
    }
}
