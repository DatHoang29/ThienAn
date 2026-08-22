namespace Tests.Modules.VideoWall;

/// <summary>
/// Description: Bộ kiểm thử tích hợp cho phân hệ VwController (Bộ điều khiển Video Wall)
/// Created date: 15/08/2026
/// </summary>
[Collection("api")]
public class VwControllerTests(Host host) : IDisposable
{
    private const string TestPrefix = "TEST_VWCTRL_";
    private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
    private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
    private readonly IStringLocalizer _localizer = host.Localizer;

    /// <summary>
    /// Author: Đạt
    /// Description: Dọn dẹp dữ liệu test trong CSDL sau khi thực thi xong test suite
    /// Created date: 15/08/2026
    /// </summary>
    public void Dispose()
    {
        _db.Deleteable<VwController>()
            .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
            .ExecuteCommand();

        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Description: Kiểm tra phân trang VwController trả về danh sách hợp lệ
    /// Created date: 15/08/2026
    /// </summary>
    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 5)]
    [InlineData(2, 20)]
    public async Task VwControllerQuery_Page_ReturnsSuccess_Test(int page, int pageSize)
    {
        // Arrange
        var input = new VwPageControllerInput
        {
            Page = page,
            PageSize = pageSize
        };

        // Act
        var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageControllerOutput>>(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Records);
    }

    /// <summary>
    /// Description: Kiểm tra GetList VwController trả về thành công sau khi xóa cache
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwControllerQuery_GetList_ReturnsSuccess_Test()
    {
        // Arrange
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        var input = new VwControllerInput();

        // Act
        var result = await _bus.InvokeAsync<List<VwControllerOutput>>(input);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Description: Kiểm tra GetById VwController trả về đúng bản ghi đã tạo
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwControllerQuery_GetById_ReturnsSuccess_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var controller = new VwController
        {
            Code = uniqueCode,
            Name = "Test Controller ById",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);

        var input = new VwIdControllerInput { ID = controller.ID };

        // Act
        var result = await _bus.InvokeAsync<VwControllerOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uniqueCode, result.Code);
    }

    /// <summary>
    /// Description: Kiểm tra thêm mới VwController qua Validator rồi ghi nhận bản ghi vào CSDL
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwControllerCommand_AddVwController_InsertsRecord_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var input = new VwAddControllerInput
        {
            Code = uniqueCode,
            Name = "Test Controller Add",
            Model = "H_SERIES",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = "admin",
            PassWord = "Password123!",
            OriginCol = 0,
            OriginRow = 0,
            CoverCols = 4,
            CoverRows = 2,
            Status = BaseEnums.StatusEnum.Enable
        };

        // Validate (FluentValidation)
        var validator = new VwAddControllerValidator(_localizer);
        var valResult = await validator.ValidateAsync(input);
        Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

        // Act
        await _bus.InvokeAsync(input);

        // Assert
        var inserted = await _db.Queryable<VwController>()
            .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);

        Assert.NotNull(inserted);
        Assert.Equal("Test Controller Add", inserted.Name);
    }

    /// <summary>
    /// Description: Kiểm tra VwAddControllerValidator từ chối các payload không hợp lệ
    /// Created date: 16/08/2026
    /// </summary>
    [Theory]
    [InlineData("", "192.168.1.1", "admin", "123")]
    [InlineData("Name", "", "admin", "123")]
    [InlineData("Name", "192.168.1.1", "", "123")]
    [InlineData("Name", "192.168.1.1", "admin", "")]
    [InlineData(null, null, null, null)]
    public async Task VwControllerCommand_AddVwController_ValidationRejectsInvalidPayload_Test(
        string? name, string? ip, string? account, string? passWord)
    {
        // Arrange
        var invalidInput = new VwAddControllerInput
        {
            Name = name,
            IP = ip,
            Account = account,
            PassWord = passWord
        };

        // Act
        var validator = new VwAddControllerValidator(_localizer);
        var result = await validator.ValidateAsync(invalidInput);

        // Assert
        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Description: Kiểm tra cập nhật VwController qua Validator rồi thay đổi thông tin bản ghi trong CSDL
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwControllerCommand_UpdateVwController_UpdatesRecord_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var controller = new VwController
        {
            Code = uniqueCode,
            Name = "Original Controller Name",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = "admin",
            PassWord = "Password123!",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);

        var updateInput = new VwUpdateControllerInput
        {
            ID = controller.ID,
            Code = uniqueCode,
            Name = "Updated Controller Name",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = "admin",
            PassWord = "Password123!",
            Model = "SAMSUNG_MOD",
            Status = BaseEnums.StatusEnum.Enable
        };

        // Validate (FluentValidation)
        var validator = new VwUpdateControllerValidator(_localizer);
        var valResult = await validator.ValidateAsync(updateInput);
        Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

        // Act
        await _bus.InvokeAsync(updateInput);

        // Assert
        var updated = await _db.Queryable<VwController>()
            .FirstAsync(u => u.ID == controller.ID && u.IsDelete == null);

        Assert.NotNull(updated);
        Assert.Equal("Updated Controller Name", updated.Name);
    }

    /// <summary>
    /// Description: Kiểm tra VwUpdateControllerValidator từ chối khi ID rỗng hoặc null
    /// Created date: 16/08/2026
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VwControllerCommand_UpdateVwController_ValidationRejectsMissingId_Test(string? invalidId)
    {
        // Arrange
        var invalidInput = new VwUpdateControllerInput
        {
            ID = invalidId,
            Name = "Valid Name",
            IP = "192.168.1.1",
            Account = "admin",
            PassWord = "123"
        };

        // Act
        var validator = new VwUpdateControllerValidator(_localizer);
        var result = await validator.ValidateAsync(invalidInput);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(VwUpdateControllerInput.ID));
    }

    /// <summary>
    /// Description: Kiểm tra xóa 1 VwController qua Validator rồi thực hiện xóa mềm (IsDelete != null)
    /// Created date: 15/08/2026
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VwControllerCommand_DeleteVwController_ValidationRejectsInvalidId_Test(string? invalidId)
    {
        var validator = new VwDeleteControllerValidator(_localizer);
        var invalidResult = await validator.ValidateAsync(new VwDeleteControllerInput { ID = invalidId });
        Assert.False(invalidResult.IsValid);
    }

    /// <summary>
    /// Description: Kiểm tra xóa 1 VwController thực hiện xóa mềm thành công trong CSDL
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwControllerCommand_DeleteVwController_SoftDeletesRecord_Test()
    {
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var controller = new VwController
        {
            Code = uniqueCode,
            Name = "To Delete Controller",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = "admin",
            PassWord = "Password123!",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);

        var deleteInput = new VwDeleteControllerInput { ID = controller.ID };
        var validator = new VwDeleteControllerValidator(_localizer);
        var valResult = await validator.ValidateAsync(deleteInput);
        Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

        // 4. Thực thi và assert
        await _bus.InvokeAsync(deleteInput);

        var active = await _db.Queryable<VwController>()
            .FirstAsync(u => u.ID == controller.ID && u.IsDelete == null);
        Assert.Null(active);

        var deleted = await _db.Queryable<VwController>()
            .ClearFilter()
            .FirstAsync(u => u.ID == controller.ID && u.IsDelete != null);
        Assert.NotNull(deleted);
    }

    /// <summary>
    /// Description: Kiểm tra xóa nhiều VwController qua Validator rồi thực hiện xóa mềm tất cả bản ghi
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwControllerCommand_BatchDeleteVwController_SoftDeletesRecords_Test()
    {
        var validator = new VwDeleteControllerValidator(_localizer);
        var uniqueCode1 = $"{TestPrefix}{Guid.NewGuid():N}";
        var uniqueCode2 = $"{TestPrefix}{Guid.NewGuid():N}";

        var ctrl1 = new VwController
        {
            Code = uniqueCode1,
            Name = "Batch Delete Controller 1",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = "admin",
            PassWord = "Password123!",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var ctrl2 = new VwController
        {
            Code = uniqueCode2,
            Name = "Batch Delete Controller 2",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = "admin",
            PassWord = "Password123!",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { ctrl1, ctrl2 }).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);

        var batchInput = new List<VwDeleteControllerInput>
        {
            new() { ID = ctrl1.ID },
            new() { ID = ctrl2.ID }
        };

        foreach (var item in batchInput)
        {
            var valResult = await validator.ValidateAsync(item);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));
        }

        await _bus.InvokeAsync(batchInput);

        // Assert
        var activeCount = await _db.Queryable<VwController>()
            .Where(u => (u.ID == ctrl1.ID || u.ID == ctrl2.ID) && u.IsDelete == null)
            .CountAsync();
        Assert.Equal(0, activeCount);

        var deletedCount = await _db.Queryable<VwController>()
            .ClearFilter()
            .Where(u => (u.ID == ctrl1.ID || u.ID == ctrl2.ID) && u.IsDelete != null)
            .CountAsync();
        Assert.Equal(2, deletedCount);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Cập nhật Controller (ví dụ đổi mật khẩu) -> Tự động reset Circuit Breaker
    ///              để hệ thống có thể kết nối lại ngay lập tức mà không bị chặn 5 phút.
    /// Created date: 17/08/2026
    /// </summary>
    [Fact]
    public async Task VwControllerCommand_UpdateVwController_ResetsCircuitBreaker_Test()
    {
        var client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();
        var testIp = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}";

        // Gây lỗi để Circuit Breaker khóa testIp
        client.RecordCircuitBreakerFailure(testIp, 401);
        client.RecordCircuitBreakerFailure(testIp, 401);
        Assert.True(client.IsCircuitBreakerBlocked(testIp, out _));

        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var ctrl = new VwController
        {
            Code = uniqueCode,
            Name = "Original Controller Name",
            IP = testIp,
            Account = "admin",
            PassWord = "OldPassword",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(ctrl).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);

        var updateInput = new VwUpdateControllerInput
        {
            ID = ctrl.ID,
            Name = "Updated Controller Name",
            IP = testIp,
            Account = "admin",
            PassWord = "NewPasswordCorrect",
            Status = BaseEnums.StatusEnum.Enable
        };

        // Act: cập nhật thông tin
        await _bus.InvokeAsync(updateInput);

        // Assert: Circuit Breaker cho testIp đã được tự động Reset (không còn bị blocked)
        Assert.False(client.IsCircuitBreakerBlocked(testIp, out _));
    }
}
