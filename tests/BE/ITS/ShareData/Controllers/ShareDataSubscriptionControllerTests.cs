using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Module.ShareData.Controllers.Subscription.Validators;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Configuration;
using Modules.CfgSystem.Core.Entities;
using Newtonsoft.Json;
using Shared.DTO.Enums;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Description: Kiểm thử validator và luồng Thêm mới đăng ký chia sẻ qua Wolverine Bus
/// Created date: 08/10/2026
/// </summary>
[Collection("api")]
public class ShareDataSubscriptionControllerTests(Host host)
{
    /// <summary>
    /// Description: Kiểm chứng EndTime nhỏ hơn StartTime bị validator chặn; EndTime hợp lệ thì
    /// lưu thành công qua Wolverine Bus và ScheduleJson lưu đúng StartTime/EndTime đã nhập.
    /// Created date: 08/10/2026
    /// </summary>
    [Fact]
    public async Task AddSubscription_WhenEndTimeBeforeStartTime_ValidatorRejects_ValidRangeSavesSuccessfully_Test()
    {
        // Arrange
        var bus = host.Services.GetRequiredService<IMessageBus>();
        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var limitOption = host.Services.GetRequiredService<LimitOption>();
        var validator = new AddSubscriptionValidator(host.Localizer, limitOption);
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

        // Act + Assert: nhánh KHÔNG hợp lệ — EndTime < StartTime
        var invalidResult = await validator.ValidateAsync(invalidInput);
        Assert.False(invalidResult.IsValid);

        var validInput = new ShareDataAddSubscriptionInput
        {
            PartnerId = partner.ID,
            DatatypeId = $"DT_{unique}",
            Direction = BaseEnums.Direction.Outbound,
            Mode = BaseEnums.SubMode.Periodic,
            IntervalSeconds = 30,
            Schedule = new ShareDataScheduleDto { Kind = "continuous", IntervalSeconds = 30, StartTime = "06:00", EndTime = "22:00" }
        };

        // Act + Assert: nhánh hợp lệ — EndTime >= StartTime, lưu thật qua bus
        var validResult = await validator.ValidateAsync(validInput);
        Assert.True(validResult.IsValid);

        await bus.InvokeAsync(validInput);

        var saved = await db.Queryable<ShareDataSubscription>()
            .Where(s => s.PartnerId == partner.ID && s.DatatypeId == validInput.DatatypeId)
            .FirstAsync();
        Assert.NotNull(saved?.ScheduleJson);
        var schedule = JsonConvert.DeserializeObject<ShareDataScheduleDto>(saved!.ScheduleJson!);
        Assert.Equal("06:00", schedule!.StartTime);
        Assert.Equal("22:00", schedule.EndTime);
    }

    /// <summary>
    /// Description: Kiểm chứng Priority và DebounceSec dùng giá trị dự phòng (0..10 và 0..3600)
    /// khi CSDL không có cấu hình, và áp dụng đúng cấu hình tùy biến khi đọc từ SysOpConfig.
    /// Created date: 17/10/2026
    /// </summary>
    [Fact]
    public async Task AddSubscription_PriorityAndDebounceSec_ValidatesFallbackAndConfiguredRanges_Test()
    {
        // Arrange
        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var fallbackLimitOption = host.Services.GetRequiredService<LimitOption>();
        var fallbackValidator = new AddSubscriptionValidator(host.Localizer, fallbackLimitOption);
        var unique = Guid.NewGuid().ToString("N");

        ShareDataAddSubscriptionInput CreateInput(int? priority = null, int? debounce = null) =>
            new()
            {
                PartnerId = $"P_{unique}",
                DatatypeId = $"DT_{unique}",
                Direction = BaseEnums.Direction.Outbound,
                Mode = BaseEnums.SubMode.Periodic,
                IntervalSeconds = 30,
                Priority = priority,
                DebounceSec = debounce,
                Schedule = new ShareDataScheduleDto
                {
                    Kind = "continuous",
                    IntervalSeconds = 30,
                    StartTime = "06:00",
                    EndTime = "22:00"
                }
            };

        // 1. Fallback verification (Priority: 0..10, DebounceSec: 0..3600)
        var resPrioFail = await fallbackValidator.ValidateAsync(CreateInput(priority: 11));
        var resDebFail = await fallbackValidator.ValidateAsync(CreateInput(debounce: 3601));
        var resFallbackOk = await fallbackValidator.ValidateAsync(CreateInput(priority: 5, debounce: 60));

        Assert.False(resPrioFail.IsValid);
        Assert.False(resDebFail.IsValid);
        Assert.True(resFallbackOk.IsValid);

        // 2. Custom configuration from SysOpConfig (isolated group)
        var isolatedGroup = $"sd_sub_cfg_{unique}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataConfig:GroupCode"] = isolatedGroup
            })
            .Build();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_priority_min", Value = "2", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_priority_max", Value = "7", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_debounce_sec_min", Value = "10", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_debounce_sec_max", Value = "120", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var customLimitOption = new LimitOption(
            db: db,
            logger: NullLogger<LimitOption>.Instance,
            configuration: config);
        var customValidator = new AddSubscriptionValidator(
            host.Localizer,
            customLimitOption);

        var resCustomPrioLow = await customValidator.ValidateAsync(CreateInput(priority: 1));
        var resCustomPrioHigh = await customValidator.ValidateAsync(CreateInput(priority: 8));
        var resCustomDebLow = await customValidator.ValidateAsync(CreateInput(debounce: 5));
        var resCustomDebHigh = await customValidator.ValidateAsync(CreateInput(debounce: 150));
        var resCustomOk = await customValidator.ValidateAsync(CreateInput(priority: 5, debounce: 30));

        Assert.False(resCustomPrioLow.IsValid);
        Assert.False(resCustomPrioHigh.IsValid);
        Assert.False(resCustomDebLow.IsValid);
        Assert.False(resCustomDebHigh.IsValid);
        Assert.True(resCustomOk.IsValid);
    }
}
