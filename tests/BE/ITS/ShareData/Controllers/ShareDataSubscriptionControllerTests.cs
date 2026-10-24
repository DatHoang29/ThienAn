using FluentValidation;
using Furion.FriendlyException;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Modules.CfgSystem.Core.Entities;
using Newtonsoft.Json;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Description: Kiểm thử validator và luồng Thêm mới đăng ký chia sẻ qua CallApiAsync (mô phỏng MVC API thật).
/// Created date: 08/10/2026
/// </summary>
[Collection("api")]
public class ShareDataSubscriptionControllerTests(Host host)
{
    /// <summary>
    /// Description: Kiểm chứng EndTime nhỏ hơn StartTime bị validator chặn; EndTime hợp lệ thì
    /// lưu thành công qua CallApiAsync và ScheduleJson lưu đúng StartTime/EndTime đã nhập.
    /// Created date: 08/10/2026
    /// </summary>
    [Fact]
    public async Task AddSubscription_WhenEndTimeBeforeStartTime_ValidatorRejects_ValidRangeSavesSuccessfully_Test()
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N");

        var partner = new ShareDataPartner
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"P_TIMERANGE_{unique}",
            Name = $"Partner {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(partner).ExecuteCommandAsync();

        var invalidInput = new ShareDataAddSubscriptionInput
        {
            PartnerId = partner.ID,
            DatatypeId = $"DT_{unique}",
            Direction = BaseEnums.Direction.Outbound,
            Mode = BaseEnums.SubMode.Periodic,
            IntervalSeconds = 30,
            Schedule = new ShareDataScheduleDto { Kind = "continuous", IntervalSeconds = 30, StartTime = "22:00", EndTime = "05:00" }
        };

        // Act + Assert: nhánh KHÔNG hợp lệ — EndTime < StartTime bị validator chặn
        var ex = await Assert.ThrowsAsync<ValidationException>(() => sp.CallApiAsync(invalidInput));
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddSubscriptionInput.Schedule));

        var validInput = new ShareDataAddSubscriptionInput
        {
            PartnerId = partner.ID,
            DatatypeId = $"DT_{unique}",
            Direction = BaseEnums.Direction.Outbound,
            Mode = BaseEnums.SubMode.Periodic,
            IntervalSeconds = 30,
            Schedule = new ShareDataScheduleDto { Kind = "continuous", IntervalSeconds = 30, StartTime = "06:00", EndTime = "22:00" }
        };

        // Act + Assert: nhánh hợp lệ — EndTime >= StartTime, lưu thật qua CallApiAsync
        await sp.CallApiAsync(validInput);

        var saved = await db.Queryable<ShareDataSubscription>()
            .Where(s => s.PartnerId == partner.ID && s.DatatypeId == validInput.DatatypeId)
            .FirstAsync();
        Assert.NotNull(saved?.ScheduleJson);
        var schedule = JsonConvert.DeserializeObject<ShareDataScheduleDto>(saved!.ScheduleJson!);
        Assert.Equal("06:00", schedule!.StartTime);
        Assert.Equal("22:00", schedule.EndTime);
    }

    /// <summary>
    /// Description: Kiểm chứng các giá trị ngoài khoảng (dưới min hoặc trên max) của chu kỳ,
    /// ưu tiên và gom sự kiện bị từ chối kèm thông báo lỗi nêu rõ khoảng hợp lệ.
    /// Created date: 19/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("Interval", 4, 5, 86400)]
    [InlineData("Interval", 86401, 5, 86400)]
    [InlineData("Priority", -1, 0, 10)]
    [InlineData("Priority", 11, 0, 10)]
    [InlineData("Debounce", -1, 0, 3600)]
    [InlineData("Debounce", 3601, 0, 3600)]
    public async Task AddSubscription_WhenValueOutsideRange_RejectsWithRangeMessage_Test(string field, int value, int min, int max)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N");

        var partner = new ShareDataPartner
        {
            ID = $"P_{unique}",
            Code = $"P_RANGE_{unique}",
            Name = $"Partner {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(partner).ExecuteCommandAsync();

        var isEvent = field == "Debounce";
        var input = new ShareDataAddSubscriptionInput
        {
            PartnerId = partner.ID,
            DatatypeId = $"DT_{unique}",
            Direction = BaseEnums.Direction.Outbound,
            Mode = isEvent ? BaseEnums.SubMode.Event : BaseEnums.SubMode.Periodic,
            EventSourceId = isEvent ? $"EV_{unique}" : null,
            IntervalSeconds = field == "Interval" ? value : 30,
            Priority = field == "Priority" ? value : 1,
            DebounceSec = field == "Debounce" ? value : null,
            Schedule = new ShareDataScheduleDto
            {
                Kind = "continuous",
                IntervalSeconds = field == "Interval" ? value : 30,
                StartTime = "06:00",
                EndTime = "22:00"
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => sp.CallApiAsync(input));
        AssertRangeViolation(ex, min, max);
    }

    /// <summary>
    /// Description: Kiểm chứng các giá trị tại đúng cận min và max của chu kỳ, ưu tiên và
    /// gom sự kiện được chấp nhận và lưu thành công vào CSDL.
    /// Created date: 19/10/2026
    /// </summary>
    [Fact]
    public async Task AddSubscription_WhenBoundaryValues_SavesSuccessfully_Test()
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var config = sp.GetRequiredService<IConfiguration>();
        var cache = sp.GetRequiredService<BaseCacheService>();
        config[ShareDataConst.ConfigGroupOverrideKey] = null;
        cache.RemoveByPrefixKey("sysOpConfig");
        var unique = Guid.NewGuid().ToString("N");

        var partner = new ShareDataPartner
        {
            ID = $"P_{unique}",
            Code = $"P_BOUND_{unique}",
            Name = $"Partner {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(partner).ExecuteCommandAsync();

        // 1. Cận Min: Interval = 5, Priority = 1, DebounceSec = 0
        var minInput = new ShareDataAddSubscriptionInput
        {
            PartnerId = partner.ID,
            DatatypeId = $"DT_MIN_{unique}",
            Direction = BaseEnums.Direction.Outbound,
            Mode = BaseEnums.SubMode.Event,
            EventSourceId = $"EV_{unique}",
            IntervalSeconds = 5,
            Priority = 1,
            DebounceSec = 0,
            Schedule = new ShareDataScheduleDto
            {
                Kind = "continuous",
                IntervalSeconds = 5,
                StartTime = "06:00",
                EndTime = "22:00"
            }
        };

        // 2. Cận Max: Interval = 86400, Priority = 10, DebounceSec = 3600
        var maxInput = new ShareDataAddSubscriptionInput
        {
            PartnerId = partner.ID,
            DatatypeId = $"DT_MAX_{unique}",
            Direction = BaseEnums.Direction.Outbound,
            Mode = BaseEnums.SubMode.Event,
            EventSourceId = $"EV_{unique}",
            IntervalSeconds = 86400,
            Priority = 10,
            DebounceSec = 3600,
            Schedule = new ShareDataScheduleDto
            {
                Kind = "continuous",
                IntervalSeconds = 86400,
                StartTime = "06:00",
                EndTime = "22:00"
            }
        };

        // Act
        await sp.CallApiAsync(minInput);
        await sp.CallApiAsync(maxInput);

        // Assert
        var savedMin = await db.Queryable<ShareDataSubscription>()
            .Where(s => s.PartnerId == partner.ID && s.DatatypeId == minInput.DatatypeId)
            .FirstAsync();
        Assert.NotNull(savedMin);
        Assert.Equal(5, savedMin.IntervalSeconds);
        Assert.Equal(1, savedMin.Priority);
        Assert.Equal(0, savedMin.DebounceSec);

        var savedMax = await db.Queryable<ShareDataSubscription>()
            .Where(s => s.PartnerId == partner.ID && s.DatatypeId == maxInput.DatatypeId)
            .FirstAsync();
        Assert.NotNull(savedMax);
        Assert.Equal(86400, savedMax.IntervalSeconds);
        Assert.Equal(10, savedMax.Priority);
        Assert.Equal(3600, savedMax.DebounceSec);
    }

    /// <summary>
    /// Description: Kiểm chứng các giới hạn động được ghi đè và áp dụng chính xác
    /// thông qua nhóm cấu hình SysOpConfig cách ly.
    /// Created date: 19/10/2026
    /// </summary>
    [Fact]
    public async Task AddSubscription_WhenDynamicLimitsChangedViaIsolatedConfigGroup_AppliesCustomLimits_Test()
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var config = sp.GetRequiredService<IConfiguration>();
        var cache = sp.GetRequiredService<BaseCacheService>();
        var unique = Guid.NewGuid().ToString("N");
        var isolatedGroup = $"sd_sub_dyn_{unique}";

        var partner = new ShareDataPartner
        {
            ID = $"P_{unique}",
            Code = $"P_DYN_{unique}",
            Name = $"Partner {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(partner).ExecuteCommandAsync();

        var customConfigs = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_interval_sec_min", Value = "10", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_interval_sec_max", Value = "300", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_priority_min", Value = "2", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_priority_max", Value = "8", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_debounce_sec_min", Value = "15", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_debounce_sec_max", Value = "120", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(customConfigs).ExecuteCommandAsync();

        config[ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup;
        cache.RemoveByPrefixKey("sysOpConfig");

        ShareDataAddSubscriptionInput CreateInput(
            string datatypeSuffix,
            int? interval = 60,
            int? priority = 5,
            int? debounce = null,
            BaseEnums.SubMode mode = BaseEnums.SubMode.Periodic) =>
            new()
            {
                PartnerId = partner.ID,
                DatatypeId = $"DT_{datatypeSuffix}_{unique}",
                Direction = BaseEnums.Direction.Outbound,
                Mode = mode,
                EventSourceId = mode == BaseEnums.SubMode.Event ? $"EV_{unique}" : null,
                IntervalSeconds = interval,
                Priority = priority,
                DebounceSec = debounce,
                Schedule = new ShareDataScheduleDto
                {
                    Kind = "continuous",
                    IntervalSeconds = interval,
                    StartTime = "06:00",
                    EndTime = "22:00"
                }
            };

        // Act & Assert
        // 1. Kiểm tra dưới min (7 < 10) và trên max (301 > 300) của Interval
        var exIntervalUnder = await Assert.ThrowsAnyAsync<Exception>(() => sp.CallApiAsync(CreateInput("IV_UNDER", interval: 7)));
        AssertRangeViolation(exIntervalUnder, 10, 300);

        var exIntervalAbove = await Assert.ThrowsAnyAsync<Exception>(() => sp.CallApiAsync(CreateInput("IV_ABOVE", interval: 301)));
        AssertRangeViolation(exIntervalAbove, 10, 300);

        // 2. Kiểm tra dưới min (1 < 2) và trên max (9 > 8) của Priority
        var exPriorityUnder = await Assert.ThrowsAnyAsync<Exception>(() => sp.CallApiAsync(CreateInput("PR_UNDER", priority: 1)));
        AssertRangeViolation(exPriorityUnder, 2, 8);

        var exPriorityAbove = await Assert.ThrowsAnyAsync<Exception>(() => sp.CallApiAsync(CreateInput("PR_ABOVE", priority: 9)));
        AssertRangeViolation(exPriorityAbove, 2, 8);

        // 3. Kiểm tra dưới min (14 < 15) và trên max (121 > 120) của DebounceSec
        var exDebounceUnder = await Assert.ThrowsAnyAsync<Exception>(() => sp.CallApiAsync(CreateInput("DB_UNDER", debounce: 14, mode: BaseEnums.SubMode.Event)));
        AssertRangeViolation(exDebounceUnder, 15, 120);

        var exDebounceAbove = await Assert.ThrowsAnyAsync<Exception>(() => sp.CallApiAsync(CreateInput("DB_ABOVE", debounce: 121, mode: BaseEnums.SubMode.Event)));
        AssertRangeViolation(exDebounceAbove, 15, 120);

        // 4. Giá trị hợp lệ theo cấu hình mới lưu thành công
        var validInput = CreateInput("VALID", interval: 10, priority: 2, debounce: 15, mode: BaseEnums.SubMode.Event);
        await sp.CallApiAsync(validInput);

        var saved = await db.Queryable<ShareDataSubscription>()
            .Where(s => s.PartnerId == partner.ID && s.DatatypeId == validInput.DatatypeId)
            .FirstAsync();
        Assert.NotNull(saved);
        Assert.Equal(10, saved.IntervalSeconds);
        Assert.Equal(2, saved.Priority);
        Assert.Equal(15, saved.DebounceSec);

        // Cleanup
        config[ShareDataConst.ConfigGroupOverrideKey] = null;
        cache.RemoveByPrefixKey("sysOpConfig");
    }

    private static void AssertRangeViolation(Exception ex, int min, int max)
    {
        var rangeText = $"{min}..{max}";
        if (ex is AppFriendlyException afe)
        {
            Assert.True(
                ex.Message.Contains(rangeText, StringComparison.Ordinal)
                || afe.ErrorMessage?.ToString() == BaseMsg.ShareData.Validation.ValueOutOfRange,
                $"Expected exception to specify range [{rangeText}] or ErrorMessage to be '{BaseMsg.ShareData.Validation.ValueOutOfRange}', but Message was '{ex.Message}' and ErrorMessage was '{afe.ErrorMessage}'.");
        }
        else
        {
            Assert.Contains(rangeText, ex.Message);
        }
    }
}
