using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Tests.ShareData.Common;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thử luật lưu hồ sơ ánh xạ qua API: đủ giá trị hệ thống, bộ mã có thật, mỗi bộ ba chỉ một hồ sơ bật, khóa khi đăng ký đang chạy.
/// Created date: 05/11/2026
/// </summary>
[Collection("api")]
public class ShareDataMappingControllerTests(Host host)
{
    /// <summary>Bộ khung khai đủ 4 giá trị hệ thống bắt buộc của hồ sơ chiều gửi.</summary>
    private const string MetaShape = "{\"now\":{\"$meta\":\"Now\"},\"serial\":{\"$meta\":\"Serial\"},\"packetCode\":{\"$meta\":\"PacketCode\"},\"partnerCode\":{\"$meta\":\"PartnerCode\"},\"zoneId\":{\"$field\":\"zoneId\"}}";

    /// <summary>Bộ khung thiếu giá trị hệ thống, chỉ hợp lệ với hồ sơ chiều nhận.</summary>
    private const string NoMetaShape = "{\"zoneId\":{\"$field\":\"zoneId\"}}";

    /// <summary>
    /// Author: DatHQ
    /// Description: Bộ khung đủ giá trị hệ thống, lá zoneId dùng bộ mã codeSet.
    /// Created date: 05/11/2026
    /// </summary>
    private static string ShapeWithCodeSet(string codeSet) =>
        MetaShape.Replace("{\"$field\":\"zoneId\"}", $"{{\"$field\":\"zoneId\",\"$extend\":{{\"codeSet\":\"{codeSet}\"}}}}");

    /// <summary>
    /// Author: DatHQ
    /// Description: Tạo một đối tác riêng cho ca test, trạng thái theo tham số.
    /// Created date: 05/11/2026
    /// </summary>
    private static async Task<ShareDataPartner> SeedPartnerAsync(ISqlSugarClient db, BaseEnums.StatusEnum status = BaseEnums.StatusEnum.Enable)
    {
        var unique = Guid.NewGuid().ToString("N");
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"P_MAP_{unique}", Name = $"Partner {unique}", Status = status };
        await db.Insertable(partner).ExecuteCommandAsync();
        return partner;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Tạo sẵn một hồ sơ ánh xạ của đối tác và gói.
    /// Created date: 05/11/2026
    /// </summary>
    private static async Task<ShareDataMapping> SeedMappingAsync(ISqlSugarClient db, ShareDataPartner partner, string datatypeId, BaseEnums.Direction direction = BaseEnums.Direction.Outbound, bool isActive = true)
    {
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"MAP_{Guid.NewGuid():N}",
            Name = "Hồ sơ",
            PartnerId = partner.ID,
            DatatypeId = datatypeId,
            Direction = direction,
            IsActive = isActive,
            TargetShapeJson = MetaShape
        };
        await db.Insertable(mapping).ExecuteCommandAsync();
        return mapping;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Dữ liệu thêm hồ sơ ánh xạ như màn hình gửi lên.
    /// Created date: 05/11/2026
    /// </summary>
    private static ShareDataAddMappingInput NewInput(ShareDataPartner partner, string datatypeId, BaseEnums.Direction direction, string shape, bool isActive = true) => new()
    {
        Code = $"MAP_{Guid.NewGuid():N}",
        Name = "Hồ sơ mới",
        PartnerId = partner.ID,
        DatatypeId = datatypeId,
        Direction = direction,
        IsActive = isActive,
        TargetShapeJson = shape
    };

    /// <summary>
    /// Author: DatHQ
    /// Description: Hồ sơ có gửi đi phải khai đủ giá trị hệ thống, hồ sơ chỉ nhận thì không cần; bộ mã trong bộ khung phải có thật; đối tác đã tắt thì không tạo được.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("guiThieuMeta", BaseMsg.ShareData.Exception.MappingMetaFieldsRequired)]
    [InlineData("haiChieuThieuMeta", BaseMsg.ShareData.Exception.MappingMetaFieldsRequired)]
    [InlineData("boMaKhongCo", BaseMsg.ShareData.Exception.MappingCodeSetNotExist)]
    [InlineData("doiTacTat", BaseMsg.ShareData.Exception.PartnerDisabled)]
    [InlineData("nhanKhongMeta", null)]
    [InlineData("guiDuMeta", null)]
    public async Task AddMapping_WhenShapeOrPartnerVaries_SavesOnlyValidProfile_Test(string caseName, string? errorKey)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var partner = await SeedPartnerAsync(db, caseName == "doiTacTat" ? BaseEnums.StatusEnum.Disable : BaseEnums.StatusEnum.Enable);
        var datatypeId = $"DT_{Guid.NewGuid():N}";
        var missingCodeSetShape = ShapeWithCodeSet($"CS_MISSING_{Guid.NewGuid():N}");
        var input = caseName switch
        {
            "guiThieuMeta" => NewInput(partner, datatypeId, BaseEnums.Direction.Outbound, NoMetaShape),
            "haiChieuThieuMeta" => NewInput(partner, datatypeId, BaseEnums.Direction.Both, NoMetaShape),
            "boMaKhongCo" => NewInput(partner, datatypeId, BaseEnums.Direction.Outbound, missingCodeSetShape),
            "nhanKhongMeta" => NewInput(partner, datatypeId, BaseEnums.Direction.Inbound, NoMetaShape),
            _ => NewInput(partner, datatypeId, BaseEnums.Direction.Outbound, MetaShape)
        };

        // Act
        var error = await Record.ExceptionAsync(() => sp.CallApiAsync<ShareDataMappingOutput>(input));

        // Assert
        var saved = await db.Queryable<ShareDataMapping>().Where(m => m.PartnerId == partner.ID && m.DatatypeId == datatypeId).AnyAsync();
        if (errorKey == null)
        {
            Assert.Null(error);
            Assert.True(saved);
        }
        else
        {
            RejectionAssert.Rejected(error, errorKey);
            Assert.False(saved);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Mỗi đối tác, gói, chiều chỉ một hồ sơ bật: thêm hồ sơ bật trùng thì từ chối, hồ sơ hai chiều đụng cả chiều gửi lẫn chiều nhận; chiều khác hoặc hồ sơ tắt không đụng.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(BaseEnums.Direction.Outbound, BaseEnums.Direction.Outbound, true, true)]
    [InlineData(BaseEnums.Direction.Both, BaseEnums.Direction.Inbound, true, true)]
    [InlineData(BaseEnums.Direction.Outbound, BaseEnums.Direction.Both, true, true)]
    [InlineData(BaseEnums.Direction.Outbound, BaseEnums.Direction.Inbound, true, false)]
    [InlineData(BaseEnums.Direction.Outbound, BaseEnums.Direction.Outbound, false, false)]
    public async Task AddMapping_WhenActiveProfileExists_RejectsConflictOnSameTriple_Test(BaseEnums.Direction existing, BaseEnums.Direction incoming, bool incomingActive, bool rejected)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var partner = await SeedPartnerAsync(db);
        var datatypeId = $"DT_{Guid.NewGuid():N}";
        await SeedMappingAsync(db, partner, datatypeId, existing);
        var input = NewInput(partner, datatypeId, incoming, MetaShape, incomingActive);

        // Act
        var error = await Record.ExceptionAsync(() => sp.CallApiAsync<ShareDataMappingOutput>(input));

        // Assert
        var count = await db.Queryable<ShareDataMapping>().Where(m => m.PartnerId == partner.ID && m.DatatypeId == datatypeId).CountAsync();
        if (rejected)
        {
            RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.MappingConflictInUse);
            Assert.Equal(1, count);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(2, count);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Hồ sơ đang bật mà đăng ký cùng đối tác, gói, chiều đang chạy thì không sửa, tắt, xóa được; đăng ký đã tạm dừng thì được. Bị từ chối thì hồ sơ giữ nguyên.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("sua", BaseEnums.SubSubscriptionState.Active, true)]
    [InlineData("tat", BaseEnums.SubSubscriptionState.Active, true)]
    [InlineData("xoa", BaseEnums.SubSubscriptionState.Active, true)]
    [InlineData("xoaCaLo", BaseEnums.SubSubscriptionState.Active, true)]
    [InlineData("sua", BaseEnums.SubSubscriptionState.Paused, false)]
    [InlineData("tat", BaseEnums.SubSubscriptionState.Paused, false)]
    [InlineData("xoa", BaseEnums.SubSubscriptionState.Paused, false)]
    [InlineData("xoaCaLo", BaseEnums.SubSubscriptionState.Paused, false)]
    public async Task ChangeMapping_WhenSubscriptionRunning_RejectsAndKeepsProfile_Test(string action, BaseEnums.SubSubscriptionState subscriptionState, bool rejected)
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var partner = await SeedPartnerAsync(db);
        var datatypeId = $"DT_{Guid.NewGuid():N}";
        var mapping = await SeedMappingAsync(db, partner, datatypeId);
        await db.Insertable(new ShareDataSubscription
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = datatypeId,
            Direction = BaseEnums.Direction.Outbound,
            State = subscriptionState
        }).ExecuteCommandAsync();
        Task Act() => action switch
        {
            "sua" => sp.CallApiAsync<ShareDataMappingOutput>(new ShareDataUpdateMappingInput
            {
                ID = mapping.ID,
                Code = mapping.Code,
                Name = "Hồ sơ đã sửa",
                DatatypeId = datatypeId,
                Direction = BaseEnums.Direction.Outbound,
                IsActive = true,
                TargetShapeJson = MetaShape
            }),
            "tat" => sp.CallApiAsync<ShareDataMappingOutput>(new ShareDataSetActiveMappingInput { ID = mapping.ID, IsActive = false }),
            "xoa" => sp.CallApiAsync(new ShareDataDeleteMappingInput { ID = mapping.ID }),
            _ => sp.CallApiAsync(new List<ShareDataDeleteMappingInput> { new() { ID = mapping.ID } })
        };

        // Act
        var error = await Record.ExceptionAsync(Act);

        // Assert
        var saved = await db.Queryable<ShareDataMapping>().ClearFilter().Where(m => m.ID == mapping.ID).FirstAsync();
        if (rejected)
        {
            RejectionAssert.Rejected(error, BaseMsg.ShareData.Exception.MappingInUse);
            Assert.Equal("Hồ sơ", saved.Name);
            Assert.True(saved.IsActive);
            Assert.Null(saved.IsDelete);
            return;
        }

        Assert.Null(error);
        switch (action)
        {
            case "sua": Assert.Equal("Hồ sơ đã sửa", saved.Name); break;
            case "tat": Assert.False(saved.IsActive); break;
            default: Assert.NotNull(saved.IsDelete); break;
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Sửa hồ sơ giữ nguyên đối tác, gói, chiều dù gửi lên giá trị khác, chỉ nhận phần cấu hình quy đổi.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task UpdateMapping_WhenClientSendsOtherTriple_KeepsPartnerPacketAndDirection_Test()
    {
        // Arrange
        var sp = host.Services;
        var db = sp.GetRequiredService<ISqlSugarClient>();
        var partner = await SeedPartnerAsync(db);
        var otherPartner = await SeedPartnerAsync(db);
        var datatypeId = $"DT_{Guid.NewGuid():N}";
        var mapping = await SeedMappingAsync(db, partner, datatypeId, isActive: false);

        // Act
        await sp.CallApiAsync<ShareDataMappingOutput>(new ShareDataUpdateMappingInput
        {
            ID = mapping.ID,
            Code = mapping.Code,
            Name = "Đổi tên",
            PartnerId = otherPartner.ID,
            DatatypeId = $"DT_{Guid.NewGuid():N}",
            Direction = BaseEnums.Direction.Inbound,
            IsActive = false,
            TargetShapeJson = MetaShape
        });

        // Assert
        var saved = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.Equal("Đổi tên", saved.Name);
        Assert.Equal(partner.ID, saved.PartnerId);
        Assert.Equal(datatypeId, saved.DatatypeId);
        Assert.Equal(BaseEnums.Direction.Outbound, saved.Direction);
    }
}
