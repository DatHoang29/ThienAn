using Module.ShareData.Controllers.Subscription.Validators;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
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
        var validator = new AddSubscriptionValidator(host.Localizer);
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
    /// khi CSDL không có cấu hình.
    /// Created date: 17/10/2026
    /// </summary>
    [Fact]
    public async Task AddSubscription_PriorityAndDebounceSec_ValidatesFallbackRanges_Test()
    {
        // Arrange
        var bus = host.Services.GetRequiredService<IMessageBus>();
        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N");

        var partner = new ShareDataPartner
        {
            ID = $"P_{unique}",
            Code = $"P_CODE_{unique}",
            Name = $"Partner {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(partner).ExecuteCommandAsync();

        ShareDataAddSubscriptionInput CreateInput(int? priority = null, int? debounce = null, BaseEnums.SubMode mode = BaseEnums.SubMode.Periodic) =>
            new()
            {
                PartnerId = partner.ID,
                DatatypeId = $"DT_{unique}",
                Direction = BaseEnums.Direction.Outbound,
                Mode = mode,
                EventSourceId = mode == BaseEnums.SubMode.Event ? $"EV_{unique}" : null,
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
        await Assert.ThrowsAnyAsync<Exception>(() => bus.InvokeAsync(CreateInput(priority: 11)));
        await Assert.ThrowsAnyAsync<Exception>(() => bus.InvokeAsync(CreateInput(debounce: 3601)));

        // 2. Valid input within fallback range succeeds (Mode = Event to preserve DebounceSec)
        await bus.InvokeAsync(CreateInput(priority: 5, debounce: 60, mode: BaseEnums.SubMode.Event));

        var saved = await db.Queryable<ShareDataSubscription>()
            .Where(s => s.PartnerId == partner.ID && s.DatatypeId == $"DT_{unique}")
            .FirstAsync();
        Assert.NotNull(saved);
        Assert.Equal(5, saved.Priority);
        Assert.Equal(60, saved.DebounceSec);
    }
}
