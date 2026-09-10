namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử tích hợp đầy đủ cho VwSource (Page, GetList, GetById, Add, Update, Delete, BatchDelete).
    /// Created date: 17/08/2026
    /// </summary>
    [Collection("api")]
    public class VwSourceTests(Host host)
    {
        private const string TestPrefix = "TEST_VWSOURCE_";
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
        private readonly IStringLocalizer _localizer = host.Localizer;

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra phân trang VwSource trả về danh sách hợp lệ
        /// Created date: 15/08/2026
        /// </summary>
        [Theory]
        [InlineData(1, 10)]
        [InlineData(1, 5)]
        [InlineData(2, 20)]
        public async Task VwSourceQuery_Page_ReturnsSuccess_Test(int page, int pageSize)
        {
            var input = new VwPageSourceInput { Page = page, PageSize = pageSize };
            var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageSourceOutput>>(input);
            Assert.NotNull(result);
            Assert.NotNull(result.Records);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetList VwSource trả về thành công
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSourceQuery_GetList_ReturnsSuccess_Test()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);
            var input = new VwSourceInput();
            var result = await _bus.InvokeAsync<List<VwSourceOutput>>(input);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra GetById VwSource trả về đúng bản ghi đã tạo
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSourceQuery_GetById_ReturnsSuccess_Test()
        {
            var uniqueCode = $"{TestPrefix}SRC_{Guid.NewGuid():N}";
            var source = new VwSource
            {
                Code = uniqueCode,
                Name = "Camera Source 01",
                SourceType = "CAMERA",
                Url = "rtsp://192.168.1.50/live",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(source).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);

            var input = new VwIdSourceInput { ID = source.ID };
            var result = await _bus.InvokeAsync<VwSourceOutput>(input);

            Assert.NotNull(result);
            Assert.Equal(uniqueCode, result.Code);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra thêm mới VwSource ghi nhận bản ghi vào CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSourceCommand_AddVwSource_InsertsRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}SRC_{Guid.NewGuid():N}";
            var input = new VwAddSourceInput
            {
                Code = uniqueCode,
                Name = "Workstation HDMI In",
                SourceType = "HDMI",
                Url = "hdmi://1",
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwAddSourceValidator(_localizer);
            var valResult = await validator.ValidateAsync(input);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(input);

            var inserted = await _db.Queryable<VwSource>()
                .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);
            Assert.NotNull(inserted);
            Assert.Equal("Workstation HDMI In", inserted.Name);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwAddSourceValidator từ chối các input không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwSourceCommand_AddVwSource_ValidationRejectsInvalidPayload_Test(string? name)
        {
            var input = new VwAddSourceInput { Name = name };
            var validator = new VwAddSourceValidator(_localizer);
            var result = await validator.ValidateAsync(input);
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra cập nhật VwSource thay đổi thông tin bản ghi trong CSDL
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSourceCommand_UpdateVwSource_UpdatesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}SRC_{Guid.NewGuid():N}";
            var source = new VwSource
            {
                Code = uniqueCode,
                Name = "Original Source Name",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(source).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);

            var updateInput = new VwUpdateSourceInput
            {
                ID = source.ID,
                Code = uniqueCode,
                Name = "Updated Source Name",
                Status = BaseEnums.StatusEnum.Enable
            };

            var validator = new VwUpdateSourceValidator(_localizer);
            var valResult = await validator.ValidateAsync(updateInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(updateInput);

            var updated = await _db.Queryable<VwSource>()
                .FirstAsync(u => u.ID == source.ID && u.IsDelete == null);
            Assert.NotNull(updated);
            Assert.Equal("Updated Source Name", updated.Name);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwUpdateSourceValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwSourceCommand_UpdateVwSource_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwUpdateSourceValidator(_localizer);
            var result = await validator.ValidateAsync(new VwUpdateSourceInput { ID = invalidId, Name = "Name" });
            Assert.False(result.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra VwDeleteSourceValidator từ chối khi ID không hợp lệ
        /// Created date: 17/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VwSourceCommand_DeleteVwSource_ValidationRejectsInvalidId_Test(string? invalidId)
        {
            var validator = new VwDeleteSourceValidator(_localizer);
            var invalidResult = await validator.ValidateAsync(new VwDeleteSourceInput { ID = invalidId });
            Assert.False(invalidResult.IsValid);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa 1 VwSource thực hiện xóa mềm (IsDelete != null)
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSourceCommand_DeleteVwSource_SoftDeletesRecord_Test()
        {
            var uniqueCode = $"{TestPrefix}SRC_{Guid.NewGuid():N}";
            var source = new VwSource
            {
                Code = uniqueCode,
                Name = "Source To Delete",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(source).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);

            var deleteInput = new VwDeleteSourceInput { ID = source.ID };
            var validator = new VwDeleteSourceValidator(_localizer);
            var valResult = await validator.ValidateAsync(deleteInput);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

            await _bus.InvokeAsync(deleteInput);

            var active = await _db.Queryable<VwSource>()
                .FirstAsync(u => u.ID == source.ID && u.IsDelete == null);
            Assert.Null(active);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra xóa nhiều VwSource thực hiện xóa mềm danh sách bản ghi
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwSourceCommand_BatchDeleteVwSource_SoftDeletesRecords_Test()
        {
            var validator = new VwDeleteSourceValidator(_localizer);
            var code1 = $"{TestPrefix}SRC_{Guid.NewGuid():N}";
            var code2 = $"{TestPrefix}SRC_{Guid.NewGuid():N}";

            var s1 = new VwSource { Code = code1, Name = "Source 1", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            var s2 = new VwSource { Code = code2, Name = "Source 2", Status = BaseEnums.StatusEnum.Enable, CreateTime = DateTime.Now };
            await _db.Insertable(new[] { s1, s2 }).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);

            var batchInput = new List<VwDeleteSourceInput> { new() { ID = s1.ID }, new() { ID = s2.ID } };

            foreach (var item in batchInput)
            {
                var valResult = await validator.ValidateAsync(item);
                Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));
            }

            await _bus.InvokeAsync(batchInput);

            var countActive = await _db.Queryable<VwSource>()
                .Where(u => (u.ID == s1.ID || u.ID == s2.ID) && u.IsDelete == null)
                .CountAsync();
            Assert.Equal(0, countActive);
        }

        /// <summary>
        /// Description: Kiểm tra bất biến cascade — nguồn cục bộ hdmi_in bỏ trống ControllerId
        ///              thì handler tự gán bộ điều khiển trung tâm (Role=center).
        ///              Xác nhận Mục 2.3 trong videowall-device-nats-plan.md.
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public async Task EnsureLocalSourceOnCenterAsync_LocalSourceWithoutControllerId_AssignsCenterController_Test()
        {
            // Arrange — dựng bộ trung tâm trong DB
            var centerId = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}";
            var center = new VwController
            {
                ID = centerId,
                Code = $"{TestPrefix}C1",
                Name = "Bộ Trung Tâm",
                Role = "center",
                IntegrationMode = "active",
                IP = "127.0.0.1:80",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var uniqueCode = $"{TestPrefix}SRC_HDMI_{Guid.NewGuid():N}";
            var input = new VwAddSourceInput
            {
                Code = uniqueCode,
                Name = "Camera HDMI In",
                SourceType = "hdmi_in",
                Status = BaseEnums.StatusEnum.Enable,
                ControllerId = null  // bỏ trống → handler tự gán
            };

            // Act
            await _bus.InvokeAsync(input);

            // Assert — bản ghi được lưu với ControllerId = bộ trung tâm
            var inserted = await _db.Queryable<VwSource>()
                .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);
            Assert.NotNull(inserted);
            Assert.Equal(centerId, inserted.ControllerId);
        }

        /// <summary>
        /// Description: Kiểm tra bất biến cascade — nguồn cục bộ hdmi_in trỏ vào bộ con (Role=sub)
        ///              bị VwAddSourceValidator từ chối và handler ném ngoại lệ.
        ///              Xác nhận Mục 2.3 trong videowall-device-nats-plan.md.
        /// Created date: 10/09/2026
        /// </summary>
        [Fact]
        public async Task EnsureLocalSourceOnCenterAsync_LocalSourceWithSubController_ThrowsException_Test()
        {
            // Arrange — dựng bộ con trong DB
            var subId = $"{TestPrefix}CTRL_SUB_{Guid.NewGuid():N}";
            var subController = new VwController
            {
                ID = subId,
                Code = $"{TestPrefix}C2_SUB",
                Name = "Bộ Con 1",
                Role = "sub",
                IntegrationMode = "inventory",
                IP = "127.0.0.1:81",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(subController).ExecuteCommandAsync();

            var input = new VwAddSourceInput
            {
                Code = $"{TestPrefix}SRC_SUB_{Guid.NewGuid():N}",
                Name = "Nguồn gắn nhầm bộ con",
                SourceType = "hdmi_in",
                Status = BaseEnums.StatusEnum.Enable,
                ControllerId = subId
            };

            // Kiểm tra validator đã chặn từ đầu — dùng isSubControllerCheck mock trực tiếp
            var validator = new VwAddSourceValidator(
                _localizer,
                isSubControllerCheck: async id =>
                {
                    var ctrl = await _db.Queryable<VwController>()
                        .FirstAsync(c => c.IsDelete == null && c.ID == id);
                    return ctrl?.Role == "sub";
                });
            var valResult = await validator.ValidateAsync(input);
            Assert.False(valResult.IsValid);
            Assert.Contains(valResult.Errors, e => e.ErrorMessage.Contains("trung tâm"));

            // Act & Assert — bus cũng ném ngoại lệ do handler chặn lại (không cần bộ trung tâm trong DB)
            await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input));
        }
    }
}
