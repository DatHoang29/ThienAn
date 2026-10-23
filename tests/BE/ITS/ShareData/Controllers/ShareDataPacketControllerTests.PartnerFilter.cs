using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Controllers;

public partial class ShareDataPacketControllerTests
{
    [Fact]
    public async Task PagePacket_WithPartnerId_WhenNotSuperAdmin_ThrowsOopsException_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false, account: $"user_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var input = new ShareDataPagePacketInput { Page = 1, PageSize = 50, PartnerId = "bat-ky" };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(input));
    }

    [Fact]
    public async Task PagePacket_FilterByPartner_ReturnsSubscribedAndCustomPackets_WithSqlSourceFlag_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var partner = NewPartnerSqlPartner($"PT_FLT_{unique}", unique);
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";

        // Dùng chung: có đăng ký gửi, không có SQL riêng
        var shared = NewPartnerSqlPacket($"PKT_SHARED_{unique}", unique, stdSql);
        // Riêng: có đăng ký gửi và có SQL riêng
        var custom = NewPartnerSqlPacket($"PKT_CUSTOM_{unique}", unique, stdSql);
        // Chỉ có SQL riêng, không có đăng ký
        var customOnly = NewPartnerSqlPacket($"PKT_ONLYSQL_{unique}", unique, stdSql);
        // Đăng ký ghi theo mã gói thay vì ID
        var byCode = NewPartnerSqlPacket($"PKT_BYCODE_{unique}", unique, stdSql);
        // Không liên quan, và gói có đăng ký đã hủy
        var unrelated = NewPartnerSqlPacket($"PKT_NONE_{unique}", unique, stdSql);
        var cancelled = NewPartnerSqlPacket($"PKT_CANCEL_{unique}", unique, stdSql);

        await db.Insertable(new[] { shared, custom, customOnly, byCode, unrelated, cancelled }).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(new[]
        {
            NewPartnerSqlSubscription(partner, shared, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active),
            NewPartnerSqlSubscription(partner, custom, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active),
            NewPartnerSqlSubscription(partner, cancelled, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Cancelled),
            new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partner.ID,
                DatatypeId = byCode.Code,
                Direction = BaseEnums.Direction.Outbound,
                State = BaseEnums.SubSubscriptionState.Active
            },
        }).ExecuteCommandAsync();
        await db.Insertable(new[]
        {
            new ShareDataPartnerSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partner.ID, PacketId = custom.ID, ExtractSql = "SELECT ID FROM TmsZone" },
            new ShareDataPartnerSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partner.ID, PacketId = customOnly.ID, ExtractSql = "SELECT Code FROM TmsZone" },
        }).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(
            new ShareDataPagePacketInput { Page = 1, PageSize = 50, Field = "code", Order = "asc", PartnerId = partner.ID });

        // Assert: đúng các gói trong phạm vi của đối tác, kèm cờ nguồn SQL
        Assert.NotNull(result);
        var rows = result.Records.ToDictionary(r => r.Code!, r => r);
        Assert.Equal(4, rows.Count);
        Assert.False(rows[shared.Code!].IsPartnerCustomSql);
        Assert.False(rows[byCode.Code!].IsPartnerCustomSql);
        Assert.True(rows[custom.Code!].IsPartnerCustomSql);
        Assert.True(rows[customOnly.Code!].IsPartnerCustomSql);
        Assert.DoesNotContain(unrelated.Code!, rows.Keys);
        Assert.DoesNotContain(cancelled.Code!, rows.Keys);
    }

    [Fact]
    public async Task PagePacket_FilterByPartner_WithoutAnyPacket_ReturnsEmpty_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var partner = NewPartnerSqlPartner($"PT_EMP_{unique}", unique);
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(
            new ShareDataPagePacketInput { Page = 1, PageSize = 50, Field = "code", Order = "asc", PartnerId = partner.ID });

        // Assert: đối tác chưa có gói nào thì bảng rỗng, không trả toàn bộ gói tin
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task PagePacket_WithoutPartnerId_DoesNotSetSqlSourceFlag_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false, account: $"user_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(
            new ShareDataPagePacketInput { Page = 1, PageSize = 5, Field = "code", Order = "asc" });

        // Assert: không lọc đối tác thì mọi người dùng vẫn xem được danh sách, và cờ nguồn SQL để trống
        Assert.NotNull(result);
        Assert.All(result.Records, r => Assert.Null(r.IsPartnerCustomSql));
    }
}
