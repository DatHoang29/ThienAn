using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Entities;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Description: Kiểm thử tích hợp cho ShareDataActivityLog Controller và QueryHandler (log cha - con)
/// Created date: 02/10/2026
/// </summary>
[Collection("api")]
public class ShareDataActivityLogControllerTests(Host host)
{
    private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();

    private async Task<(ShareDataActivityLog Parent, ShareDataActivityLog Child1, ShareDataActivityLog Child2, string SubId)> SeedParentAndTwoChildrenAsync()
    {
        var subId = Guid.NewGuid().ToString("N");
        var parentId = Guid.NewGuid().ToString("N");
        var now = DateTime.Now;

        var parent = new ShareDataActivityLog
        {
            ID = parentId,
            SubscriptionId = subId,
            ParentId = null,
            StepNo = null,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Action = BaseEnums.ActivityAction.Send,
            TransferDirection = BaseEnums.TransferDirection.SND,
            Success = BaseEnums.SuccessEnums.Success,
            OccurredAt = now,
            CreateTime = now,
            UpdateTime = now,
            RecordCount = 100,
            Description = $"Parent log {subId}"
        };

        var child1 = new ShareDataActivityLog
        {
            ID = Guid.NewGuid().ToString("N"),
            SubscriptionId = subId,
            ParentId = parentId,
            StepNo = 1,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Action = BaseEnums.ActivityAction.Send,
            TransferDirection = BaseEnums.TransferDirection.SND,
            Success = BaseEnums.SuccessEnums.Success,
            OccurredAt = now.AddMilliseconds(10),
            CreateTime = now.AddMilliseconds(10),
            UpdateTime = now.AddMilliseconds(10),
            RecordCount = 100,
            Description = $"Child step 1 {subId}"
        };

        var child2 = new ShareDataActivityLog
        {
            ID = Guid.NewGuid().ToString("N"),
            SubscriptionId = subId,
            ParentId = parentId,
            StepNo = 2,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Action = BaseEnums.ActivityAction.Send,
            TransferDirection = BaseEnums.TransferDirection.SND,
            Success = BaseEnums.SuccessEnums.Success,
            OccurredAt = now.AddMilliseconds(20),
            CreateTime = now.AddMilliseconds(20),
            UpdateTime = now.AddMilliseconds(20),
            RecordCount = 100,
            Description = $"Child step 2 {subId}"
        };

        await _db.Insertable(new[] { parent, child1, child2 }).ExecuteCommandAsync();
        return (parent, child1, child2, subId);
    }

    /// <summary>
    /// Description: Kiểm tra truy vấn phân trang chỉ trả về dòng cha, loại bỏ dòng con
    /// Created date: 02/10/2026
    /// </summary>
    [Fact]
    public async Task Page_WhenParentAndChildrenExist_ReturnsOnlyParent_Test()
    {
        // Arrange
        var (parent, _, _, subId) = await SeedParentAndTwoChildrenAsync();
        var input = new ShareDataPageActivityLogInput
        {
            SubscriptionId = subId,
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _bus.InvokeAsync<SqlSugarPagedList<ShareDataPageActivityLogOutput>>(input);

        // Assert
        Assert.NotNull(result);
        var records = result.Records.ToList();
        Assert.Single(records);
        Assert.Equal(parent.ID, records[0].ID);
        Assert.Null(records[0].ParentId);
    }

    /// <summary>
    /// Description: Kiểm tra thống kê Summary chỉ tính dòng cha, không tính trùng dòng con
    /// Created date: 02/10/2026
    /// </summary>
    [Fact]
    public async Task Summary_WhenParentAndChildrenExist_CountsOnlyParent_Test()
    {
        // Arrange
        var (_, _, _, subId) = await SeedParentAndTwoChildrenAsync();
        var partnerId = Guid.NewGuid().ToString("N");
        await _db.Updateable<ShareDataActivityLog>()
            .SetColumns(it => it.PartnerId == partnerId)
            .Where(it => it.SubscriptionId == subId)
            .ExecuteCommandAsync();

        var input = new ShareDataSummaryActivityLogInput
        {
            PartnerId = partnerId,
            LogType = BaseEnums.LogTypeEnum.Transfer
        };

        // Act
        var result = await _bus.InvokeAsync<ShareDataSummaryActivityLogOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.SentCount);
    }

    /// <summary>
    /// Description: Kiểm tra GetList chỉ trả về dòng cha (ParentId == null)
    /// Created date: 02/10/2026
    /// </summary>
    [Fact]
    public async Task GetList_WhenParentAndChildrenExist_ReturnsOnlyParents_Test()
    {
        // Arrange
        var (parent, _, _, subId) = await SeedParentAndTwoChildrenAsync();
        var input = new ShareDataActivityLogInput
        {
            SubscriptionId = subId
        };

        // Act
        var result = await _bus.InvokeAsync<List<ShareDataActivityLogOutput>>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(parent.ID, result[0].ID);
        Assert.Null(result[0].ParentId);
    }

    /// <summary>
    /// Description: Kiểm tra API GetSteps trả đúng cấu trúc lồng 1 dòng cha kèm 2 dòng con có StepNo 1 và 2
    /// Created date: 02/10/2026
    /// </summary>
    [Fact]
    public async Task GetSteps_WhenParentHasTwoChildren_ReturnsNestedTreeStructure_Test()
    {
        // Arrange
        var (parent, child1, child2, _) = await SeedParentAndTwoChildrenAsync();
        var input = new ShareDataStepActivityLogInput
        {
            ID = parent.ID
        };

        // Act
        var result = await _bus.InvokeAsync<List<ShareDataActivityLogOutput>>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var rootNode = result[0];
        Assert.Equal(parent.ID, rootNode.ID);
        Assert.Null(rootNode.ParentId);

        Assert.NotNull(rootNode.Children);
        Assert.Equal(2, rootNode.Children.Count);
        Assert.Equal(1, rootNode.Children[0].StepNo);
        Assert.Equal(2, rootNode.Children[1].StepNo);
        Assert.Equal(child1.ID, rootNode.Children[0].ID);
        Assert.Equal(child2.ID, rootNode.Children[1].ID);
        Assert.Equal(parent.ID, rootNode.Children[0].ParentId);
        Assert.Equal(parent.ID, rootNode.Children[1].ParentId);
    }

    /// <summary>
    /// Description: Kiểm tra GetById vẫn đọc được bản ghi dòng con theo ID
    /// Created date: 02/10/2026
    /// </summary>
    [Fact]
    public async Task GetById_WhenPassingChildId_ReturnsChildRecord_Test()
    {
        // Arrange
        var (parent, child1, _, _) = await SeedParentAndTwoChildrenAsync();
        var input = new ShareDataIdActivityLogInput
        {
            ID = child1.ID
        };

        // Act
        var result = await _bus.InvokeAsync<ShareDataActivityLogOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(child1.ID, result.ID);
        Assert.Equal(parent.ID, result.ParentId);
        Assert.Equal(1, result.StepNo);
    }

    /// <summary>
    /// Description: Kiểm tra thuộc tính Children (IsIgnore) không bị tạo thành cột trong CSDL
    /// Created date: 02/10/2026
    /// </summary>
    [Fact]
    public async Task Schema_ChildrenProperty_IsNotCreatedAsDatabaseColumn_Test()
    {
        // Act
        var count = await _db.Ado.GetIntAsync(@"
            SELECT COUNT(1) FROM sys.columns 
            WHERE object_id = OBJECT_ID(N'dbo.ShareDataActivityLog') 
              AND name = 'Children'");

        // Assert
        Assert.Equal(0, count);
    }
}
