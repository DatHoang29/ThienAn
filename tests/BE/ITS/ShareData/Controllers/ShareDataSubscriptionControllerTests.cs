using FluentValidation;
using Furion.FriendlyException;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Modules.CfgSystem.Core.Entities;
using Newtonsoft.Json;
using Tests.ShareData.Common;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Description: Kiểm thử validator và luồng Thêm mới đăng ký chia sẻ qua CallApiAsync (mô phỏng MVC API thật).
/// Created date: 08/10/2026
/// </summary>
[Collection("api")]
public class ShareDataSubscriptionControllerTests(Host host)
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Đối tác và mã gói cho luồng đăng ký, kèm hồ sơ ánh xạ chiều gửi đang bật khi có yêu cầu.
    /// Created date: 05/11/2026
    /// </summary>
    private static async Task<(ShareDataPartner Partner, string DatatypeId)> SeedLifecycleAsync(ISqlSugarClient db, bool connected = true, bool hasMapping = true)
    {
        var unique = Guid.NewGuid().ToString("N");
        var partner = new ShareDataPartner
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"P_LC_{unique}",
            Name = $"Partner {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            SessionState = connected ? BaseEnums.SessionState.Connected : BaseEnums.SessionState.Disconnected
        };
        await db.Insertable(partner).ExecuteCommandAsync();

        var datatypeId = $"DT_{unique}";
        if (hasMapping)
        {
            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = datatypeId,
                Direction = BaseEnums.Direction.Outbound,
                IsActive = true,
                TargetShapeJson = "{}"
            }).ExecuteCommandAsync();
        }

        return (partner, datatypeId);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Tạo sẵn một đăng ký chiều gửi ở trạng thái cho trước, chu kỳ 30 giây, đã có lịch chạy kế tiếp.
    /// Created date: 05/11/2026
    /// </summary>
    private static async Task<ShareDataSubscription> SeedSubscriptionAsync(ISqlSugarClient db, ShareDataPartner partner, string datatypeId, BaseEnums.SubSubscriptionState state)
    {
        var subscription = new ShareDataSubscription
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = datatypeId,
            Direction = BaseEnums.Direction.Outbound,
            Mode = BaseEnums.SubMode.Periodic,
            State = state,
            IntervalSeconds = 30,
            NextTimeRun = DateTime.Now.AddHours(1)
        };
        await db.Insertable(subscription).ExecuteCommandAsync();
        return subscription;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Điền đối tác, gói, chiều gửi và lịch hợp lệ chu kỳ 60 giây vào dữ liệu thêm hoặc sửa đăng ký.
    /// Created date: 05/11/2026
    /// </summary>
    private static T WithValidSchedule<T>(T input, ShareDataPartner partner, string datatypeId) where T : ShareDataAddSubscriptionInput
    {
        input.PartnerId = partner.ID;
        input.DatatypeId = datatypeId;
        input.Direction = BaseEnums.Direction.Outbound;
        input.Mode = BaseEnums.SubMode.Periodic;
        input.IntervalSeconds = 60;
        input.Schedule = new ShareDataScheduleDto { Kind = "continuous", IntervalSeconds = 60, StartTime = "06:00", EndTime = "22:00" };
        return input;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đọc đăng ký kể cả dòng đã xóa mềm.
    /// Created date: 05/11/2026
    /// </summary>
    private static Task<ShareDataSubscription> ReadAsync(ISqlSugarClient db, string subscriptionId) =>
        db.Queryable<ShareDataSubscription>().ClearFilter().Where(s => s.ID == subscriptionId).FirstAsync();

    /// <summary>
    /// Author: DatHQ
    /// Description: Đăng ký mới nhận trạng thái theo điều kiện chạy: chưa có hồ sơ ánh xạ đang bật thì Tạm dừng, có hồ sơ mà đối tác chưa kết nối thì Chờ, đủ cả hai thì Hoạt động.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, true, BaseEnums.SubSubscriptionState.Paused)]
    [InlineData(true, false, BaseEnums.SubSubscriptionState.Pending)]
    [InlineData(true, true, BaseEnums.SubSubscriptionState.Active)]
    public async Task AddSubscription_WhenMappingAndConnectionVary_SetsMatchingState_Test(bool hasMapping, bool connected, BaseEnums.SubSubscriptionState expected)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var (partner, datatypeId) = await SeedLifecycleAsync(db, connected, hasMapping);
        var input = WithValidSchedule(new ShareDataAddSubscriptionInput(), partner, datatypeId);

        // Act
        await sp.CallApiAsync(input);

        // Assert
        var saved = await db.Queryable<ShareDataSubscription>().Where(s => s.PartnerId == partner.ID && s.DatatypeId == datatypeId).FirstAsync();
        Assert.Equal(expected, saved.State);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đăng ký có gửi đi bắt buộc có lịch, chiều nhận thì không cần vì thời điểm nhận do đối tác quyết định; chế độ theo sự kiện bắt buộc chọn nguồn sự kiện.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(BaseEnums.Direction.Outbound, BaseEnums.SubMode.Periodic, false, false, nameof(ShareDataAddSubscriptionInput.Schedule))]
    [InlineData(BaseEnums.Direction.Both, BaseEnums.SubMode.Periodic, false, false, nameof(ShareDataAddSubscriptionInput.Schedule))]
    [InlineData(BaseEnums.Direction.Outbound, BaseEnums.SubMode.Event, true, false, nameof(ShareDataAddSubscriptionInput.EventSourceId))]
    [InlineData(BaseEnums.Direction.Inbound, BaseEnums.SubMode.Periodic, false, false, null)]
    [InlineData(BaseEnums.Direction.Outbound, BaseEnums.SubMode.Event, true, true, null)]
    public async Task AddSubscription_WhenScheduleOrEventSourceMissing_RejectsOnlyRequiredCases_Test(BaseEnums.Direction direction, BaseEnums.SubMode mode, bool hasSchedule, bool hasEventSource, string? invalidProperty)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var (partner, datatypeId) = await SeedLifecycleAsync(db);
        var input = new ShareDataAddSubscriptionInput
        {
            PartnerId = partner.ID,
            DatatypeId = datatypeId,
            Direction = direction,
            Mode = mode,
            IntervalSeconds = hasSchedule ? 60 : null,
            EventSourceId = hasEventSource ? $"ES_{Guid.NewGuid():N}" : null,
            Schedule = hasSchedule ? new ShareDataScheduleDto { Kind = "continuous", IntervalSeconds = 60, StartTime = "06:00", EndTime = "22:00" } : null
        };

        // Act
        var error = await Record.ExceptionAsync(() => sp.CallApiAsync(input));

        // Assert
        var saved = await db.Queryable<ShareDataSubscription>().Where(s => s.PartnerId == partner.ID && s.DatatypeId == datatypeId).AnyAsync();
        if (invalidProperty == null)
        {
            Assert.Null(error);
            Assert.True(saved);
        }
        else
        {
            var validation = Assert.IsType<ValidationException>(error);
            Assert.Contains(validation.Errors, e => e.PropertyName == invalidProperty);
            Assert.False(saved);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Mỗi đối tác, gói, chiều chỉ một đăng ký đang chạy: trùng đăng ký đang chạy thì từ chối, đăng ký cùng bộ ba đã tạm dừng không chặn;
    ///              đối tác đã tắt thì không tạo được đăng ký.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("trungDangChay", BaseMsg.ShareData.Exception.SubscriptionDuplicated)]
    [InlineData("doiTacTat", BaseMsg.ShareData.Exception.PartnerDisabled)]
    [InlineData("trungDaTamDung", null)]
    public async Task AddSubscription_WhenTripleOrPartnerBlocked_RejectsDuplicate_Test(string caseName, string? errorKey)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var (partner, datatypeId) = await SeedLifecycleAsync(db);
        if (caseName == "doiTacTat")
        {
            await db.Updateable<ShareDataPartner>().SetColumns(p => p.Status == BaseEnums.StatusEnum.Disable).Where(p => p.ID == partner.ID).ExecuteCommandAsync();
        }
        else
        {
            await SeedSubscriptionAsync(db, partner, datatypeId, caseName == "trungDangChay" ? BaseEnums.SubSubscriptionState.Active : BaseEnums.SubSubscriptionState.Paused);
        }
        var countBefore = await db.Queryable<ShareDataSubscription>().Where(s => s.PartnerId == partner.ID).CountAsync();
        var input = WithValidSchedule(new ShareDataAddSubscriptionInput(), partner, datatypeId);

        // Act
        var error = await Record.ExceptionAsync(() => sp.CallApiAsync(input));

        // Assert
        var countAfter = await db.Queryable<ShareDataSubscription>().Where(s => s.PartnerId == partner.ID).CountAsync();
        if (errorKey == null)
        {
            Assert.Null(error);
            Assert.Equal(countBefore + 1, countAfter);
        }
        else
        {
            RejectionAssert.Rejected(error, errorKey);
            Assert.Equal(countBefore, countAfter);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Tạm dừng, bật lại, duyệt, hủy, từ chối chỉ chuyển từ đúng trạng thái nguồn; sai nguồn thì từ chối và giữ nguyên.
    ///              Vào Hoạt động thì xóa lịch chạy kế tiếp để chạy ngay nhịp đầu.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("pause", BaseEnums.SubSubscriptionState.Active, BaseEnums.SubSubscriptionState.Paused, false)]
    [InlineData("pause", BaseEnums.SubSubscriptionState.Pending, BaseEnums.SubSubscriptionState.Paused, false)]
    [InlineData("pause", BaseEnums.SubSubscriptionState.Paused, BaseEnums.SubSubscriptionState.Paused, true)]
    [InlineData("resume", BaseEnums.SubSubscriptionState.Paused, BaseEnums.SubSubscriptionState.Active, false)]
    [InlineData("resume", BaseEnums.SubSubscriptionState.Pending, BaseEnums.SubSubscriptionState.Active, false)]
    [InlineData("resume", BaseEnums.SubSubscriptionState.Cancelled, BaseEnums.SubSubscriptionState.Cancelled, true)]
    [InlineData("approve", BaseEnums.SubSubscriptionState.Pending, BaseEnums.SubSubscriptionState.Active, false)]
    [InlineData("approve", BaseEnums.SubSubscriptionState.Paused, BaseEnums.SubSubscriptionState.Paused, true)]
    [InlineData("cancel", BaseEnums.SubSubscriptionState.Active, BaseEnums.SubSubscriptionState.Cancelled, false)]
    [InlineData("cancel", BaseEnums.SubSubscriptionState.Rejected, BaseEnums.SubSubscriptionState.Rejected, true)]
    [InlineData("reject", BaseEnums.SubSubscriptionState.Pending, BaseEnums.SubSubscriptionState.Rejected, false)]
    [InlineData("reject", BaseEnums.SubSubscriptionState.Active, BaseEnums.SubSubscriptionState.Active, true)]
    public async Task ChangeSubscriptionState_WhenSourceStateVaries_OnlyAllowedTransitionsApply_Test(string action, BaseEnums.SubSubscriptionState from, BaseEnums.SubSubscriptionState expected, bool rejected)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var (partner, datatypeId) = await SeedLifecycleAsync(db);
        var subscription = await SeedSubscriptionAsync(db, partner, datatypeId, from);
        Task Act() => action switch
        {
            "pause" => sp.CallApiAsync(new ShareDataPauseSubscriptionInput { ID = subscription.ID }),
            "resume" => sp.CallApiAsync(new ShareDataResumeSubscriptionInput { ID = subscription.ID }),
            "approve" => sp.CallApiAsync(new ShareDataApproveSubscriptionInput { ID = subscription.ID }),
            "cancel" => sp.CallApiAsync(new ShareDataCancelSubscriptionInput { ID = subscription.ID }),
            _ => sp.CallApiAsync(new ShareDataRejectSubscriptionInput { ID = subscription.ID })
        };

        // Act
        var error = await Record.ExceptionAsync(Act);

        // Assert
        var saved = await ReadAsync(db, subscription.ID);
        Assert.Equal(expected, saved.State);
        if (rejected)
        {
            RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.SubscriptionStateInvalid);
        }
        else
        {
            Assert.Null(error);
        }
        if (!rejected && expected == BaseEnums.SubSubscriptionState.Active)
        {
            Assert.Null(saved.NextTimeRun);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cổng chặn bật lại đăng ký: đối tác chưa kết nối, hồ sơ ánh xạ không còn bật, hoặc đã có đăng ký cùng bộ ba đang chạy thì từ chối và đăng ký vẫn tạm dừng.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("chuaKetNoi", BaseMsg.ShareData.Exception.PartnerNotConnected)]
    [InlineData("khongCoHoSo", BaseMsg.ShareData.Exception.MappingNotResolved)]
    [InlineData("trungDangChay", BaseMsg.ShareData.Exception.SubscriptionDuplicated)]
    public async Task ResumeSubscription_WhenConditionMissing_RejectsAndStaysPaused_Test(string caseName, string errorKey)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var (partner, datatypeId) = await SeedLifecycleAsync(db, connected: caseName != "chuaKetNoi", hasMapping: caseName != "khongCoHoSo");
        var subscription = await SeedSubscriptionAsync(db, partner, datatypeId, BaseEnums.SubSubscriptionState.Paused);
        if (caseName == "trungDangChay")
        {
            await SeedSubscriptionAsync(db, partner, datatypeId, BaseEnums.SubSubscriptionState.Active);
        }

        // Act
        var error = await Record.ExceptionAsync(() => sp.CallApiAsync(new ShareDataResumeSubscriptionInput { ID = subscription.ID }));

        // Assert
        var saved = await ReadAsync(db, subscription.ID);
        RejectionAssert.Rejected(error, errorKey);
        Assert.Equal(BaseEnums.SubSubscriptionState.Paused, saved.State);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Sửa đăng ký: đang chạy thì phải tạm dừng trước, đã kết thúc thì không sửa được. Tạm dừng thì sửa được, giữ nguyên đối tác và gói kể cả khi gửi lên giá trị khác,
    ///              và xóa lịch chạy kế tiếp để Worker tính lại theo lịch mới.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.SubscriptionMustPauseBeforeUpdate)]
    [InlineData(BaseEnums.SubSubscriptionState.Cancelled, BaseMsg.ShareData.Exception.SubscriptionFinished)]
    [InlineData(BaseEnums.SubSubscriptionState.Paused, null)]
    public async Task UpdateSubscription_WhenStateVaries_OnlyEditableStatesChange_Test(BaseEnums.SubSubscriptionState state, string? errorKey)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var (partner, datatypeId) = await SeedLifecycleAsync(db);
        var (otherPartner, otherDatatype) = await SeedLifecycleAsync(db);
        var subscription = await SeedSubscriptionAsync(db, partner, datatypeId, state);
        var input = WithValidSchedule(new ShareDataUpdateSubscriptionInput { ID = subscription.ID }, otherPartner, otherDatatype);

        // Act
        var error = await Record.ExceptionAsync(() => sp.CallApiAsync(input));

        // Assert
        var saved = await ReadAsync(db, subscription.ID);
        Assert.Equal(partner.ID, saved.PartnerId);
        Assert.Equal(datatypeId, saved.DatatypeId);
        if (errorKey == null)
        {
            Assert.Null(error);
            Assert.Equal(60, saved.IntervalSeconds);
            Assert.Null(saved.NextTimeRun);
        }
        else
        {
            RejectionAssert.Rejected(error, errorKey);
            Assert.Equal(30, saved.IntervalSeconds);
            Assert.NotNull(saved.NextTimeRun);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Màn Cấu hình gói tin dựa vào danh sách này để khóa theo đăng ký đang bật: lọc đúng trạng thái và gói, không trả đăng ký đã tạm dừng, đăng ký gói khác hay đăng ký đã xóa.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task ListSubscriptions_WhenFilteredByActiveStateAndPacket_ReturnsOnlyMatchingRows_Test()
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var (partner, datatypeId) = await SeedLifecycleAsync(db);
        var (otherPartner, otherDatatype) = await SeedLifecycleAsync(db);
        var active = await SeedSubscriptionAsync(db, partner, datatypeId, BaseEnums.SubSubscriptionState.Active);
        await SeedSubscriptionAsync(db, otherPartner, datatypeId, BaseEnums.SubSubscriptionState.Paused);
        await SeedSubscriptionAsync(db, otherPartner, otherDatatype, BaseEnums.SubSubscriptionState.Active);
        var deleted = await SeedSubscriptionAsync(db, otherPartner, datatypeId, BaseEnums.SubSubscriptionState.Paused);
        await sp.CallApiAsync(new ShareDataDeleteSubscriptionInput { ID = deleted.ID });

        // Act
        var result = await sp.CallApiAsync<List<ShareDataSubscriptionOutput>>(new ShareDataSubscriptionInput { State = BaseEnums.SubSubscriptionState.Active, DatatypeId = datatypeId });

        // Assert
        var row = Assert.Single(result);
        Assert.Equal(active.ID, row.ID);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Xóa đăng ký: đang chạy thì phải tạm dừng trước (một bản ghi và cả lô), tạm dừng thì xóa mềm.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(BaseEnums.SubSubscriptionState.Active, false, true)]
    [InlineData(BaseEnums.SubSubscriptionState.Active, true, true)]
    [InlineData(BaseEnums.SubSubscriptionState.Paused, false, false)]
    [InlineData(BaseEnums.SubSubscriptionState.Paused, true, false)]
    public async Task DeleteSubscription_WhenActiveOrPaused_OnlyPausedIsDeleted_Test(BaseEnums.SubSubscriptionState state, bool batch, bool rejected)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var (partner, datatypeId) = await SeedLifecycleAsync(db);
        var subscription = await SeedSubscriptionAsync(db, partner, datatypeId, state);

        // Act
        var error = await Record.ExceptionAsync(() => batch
            ? sp.CallApiAsync(new List<ShareDataDeleteSubscriptionInput> { new() { ID = subscription.ID } })
            : sp.CallApiAsync(new ShareDataDeleteSubscriptionInput { ID = subscription.ID }));

        // Assert
        var saved = await ReadAsync(db, subscription.ID);
        Assert.Equal(rejected, saved.IsDelete == null);
        if (rejected)
        {
            RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.SubscriptionMustPauseBeforeDelete);
        }
        else
        {
            Assert.Null(error);
        }
    }

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
