using Module.ShareData.Core.Dto.CodeSet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Tests.ShareData.Common;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thử luật lưu bộ mã quy đổi qua API: giá trị không trùng, khóa khi hồ sơ ánh xạ đang dùng.
/// Created date: 05/11/2026
/// </summary>
[Collection("api")]
public class ShareDataCodeSetControllerTests(Host host)
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Dựng bảng quy đổi từ các cặp giá trị nguồn, giá trị đối tác.
    /// Created date: 05/11/2026
    /// </summary>
    private static List<ShareDataCodeSetValueDto> Values(params (string Source, string Partner)[] rows) =>
        rows.Select(r => new ShareDataCodeSetValueDto { SourceValue = r.Source, PartnerValue = r.Partner }).ToList();

    /// <summary>
    /// Author: DatHQ
    /// Description: Tạo một bộ mã qua API, giá trị A đổi thành 1 và B đổi thành 2.
    /// Created date: 05/11/2026
    /// </summary>
    private static async Task<ShareDataCodeSetOutput> AddCodeSetAsync(IServiceProvider sp, string code)
    {
        var values = Values(("A", "1"), ("B", "2"));
        return await sp.CallApiAsync<ShareDataCodeSetOutput>(new ShareDataAddCodeSetInput { Code = code, Name = "Bộ mã", Values = values });
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Hồ sơ ánh xạ dùng bộ mã trong bộ khung, kèm đăng ký chiều gửi ở trạng thái cho trước (null là không có đăng ký).
    /// Created date: 05/11/2026
    /// </summary>
    private static async Task SeedMappingUsingAsync(ISqlSugarClient db, string codeSet, BaseEnums.SubSubscriptionState? subscriptionState)
    {
        var partnerId = Guid.NewGuid().ToString("N");
        var datatypeId = $"DT_{Guid.NewGuid():N}";
        await db.Insertable(new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerId,
            DatatypeId = datatypeId,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            TargetShapeJson = $"{{\"zoneId\": {{\"$field\": \"zoneId\", \"$extend\": {{\"codeSet\": \"{codeSet}\"}}}}}}"
        }).ExecuteCommandAsync();
        if (subscriptionState != null)
        {
            await db.Insertable(new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partnerId,
                DatatypeId = datatypeId,
                Direction = BaseEnums.Direction.Outbound,
                State = subscriptionState
            }).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Bảng quy đổi không được trùng giá trị nguồn hay giá trị đối tác, không phân biệt hoa thường; hợp lệ thì lưu đủ giá trị và giá trị mặc định.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("A", "1", "a", "2", BaseMsg.ShareData.Exception.CodeSetSourceValueDuplicated)]
    [InlineData("A", "x", "B", "X", BaseMsg.ShareData.Exception.CodeSetPartnerValueDuplicated)]
    [InlineData("A", "1", "B", "2", null)]
    public async Task AddCodeSet_WhenValuesVary_RejectsDuplicates_Test(string source1, string partner1, string source2, string partner2, string? errorKey)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var code = $"CS_{Guid.NewGuid():N}";
        var input = new ShareDataAddCodeSetInput
        {
            Code = code,
            Name = "Bộ mã",
            DefaultPartnerValue = "KHAC",
            Values = Values((source1, partner1), (source2, partner2))
        };

        // Act
        var error = await Record.ExceptionAsync(() => sp.CallApiAsync<ShareDataCodeSetOutput>(input));

        // Assert
        var saved = await db.Queryable<ShareDataCodeSet>().Where(c => c.Code == code).FirstAsync();
        if (errorKey == null)
        {
            Assert.Null(error);
            Assert.Contains("KHAC", saved.ValuesJson);
            Assert.Contains("\"B\"", saved.ValuesJson, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            RejectionAssert.Rejected(error, errorKey);
            Assert.Null(saved);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Sửa bộ mã: đổi mã khi hồ sơ ánh xạ còn tham chiếu thì từ chối; đổi bảng quy đổi khi hồ sơ đang dùng có đăng ký đang chạy thì từ chối,
    ///              đăng ký đã tạm dừng thì được; chỉ đổi tên thì luôn được.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("doiMa", null, BaseMsg.ShareData.Exception.CodeSetInUse)]
    [InlineData("doiGiaTri", BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.CodeSetValuesInUse)]
    [InlineData("doiGiaTri", BaseEnums.SubSubscriptionState.Paused, null)]
    [InlineData("doiTen", BaseEnums.SubSubscriptionState.Active, null)]
    public async Task UpdateCodeSet_WhenUsedByMapping_LocksCodeAndValues_Test(string change, BaseEnums.SubSubscriptionState? subscriptionState, string? errorKey)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var code = $"CS_{Guid.NewGuid():N}";
        var codeSet = await AddCodeSetAsync(sp, code);
        await SeedMappingUsingAsync(db, code, subscriptionState);
        var input = new ShareDataUpdateCodeSetInput
        {
            ID = codeSet.ID,
            Code = change == "doiMa" ? $"CS_{Guid.NewGuid():N}" : code,
            Name = change == "doiTen" ? "Tên mới" : "Bộ mã",
            Values = change == "doiGiaTri" ? Values(("A", "9"), ("B", "2")) : Values(("A", "1"), ("B", "2"))
        };

        // Act
        var error = await Record.ExceptionAsync(() => sp.CallApiAsync<ShareDataCodeSetOutput>(input));

        // Assert
        var saved = await db.Queryable<ShareDataCodeSet>().InSingleAsync(codeSet.ID);
        if (errorKey == null)
        {
            var hasNewValue = saved.ValuesJson!.Contains("\"9\"");
            Assert.Null(error);
            Assert.Equal(input.Name, saved.Name);
            Assert.Equal(change == "doiGiaTri", hasNewValue);
        }
        else
        {
            RejectionAssert.Rejected(error, errorKey);
            Assert.Equal(code, saved.Code);
            Assert.DoesNotContain("\"9\"", saved.ValuesJson);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Xóa bộ mã đang được hồ sơ ánh xạ tham chiếu thì từ chối (một bản ghi và cả lô); không ai dùng thì xóa mềm.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task DeleteCodeSet_WhenInUseOrNot_OnlyUnusedIsDeleted_Test(bool inUse, bool batch)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var code = $"CS_{Guid.NewGuid():N}";
        var codeSet = await AddCodeSetAsync(sp, code);
        if (inUse)
        {
            await SeedMappingUsingAsync(db, code, null);
        }

        // Act
        var error = await Record.ExceptionAsync(() => batch
            ? sp.CallApiAsync(new List<ShareDataDeleteCodeSetInput> { new() { ID = codeSet.ID } })
            : sp.CallApiAsync(new ShareDataDeleteCodeSetInput { ID = codeSet.ID }));

        // Assert
        var saved = await db.Queryable<ShareDataCodeSet>().ClearFilter().Where(c => c.ID == codeSet.ID).FirstAsync();
        Assert.Equal(inUse, saved.IsDelete == null);
        if (inUse)
        {
            RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.CodeSetInUse);
        }
        else
        {
            Assert.Null(error);
        }
    }
}
