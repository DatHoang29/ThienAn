using Module.ShareData.Controllers.Subscription.Validators;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Newtonsoft.Json;

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
}
