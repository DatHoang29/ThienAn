using System.Security.Claims;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Persistence.SeedData;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Enums;
using SqlSugar;
using Wolverine;
using Xunit;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử tích hợp cho ShareDataPacketController qua Wolverine IMessageBus (Rule 19.84).
///              Bao gồm quản lý thêm/sửa/xóa gói tin, tự động điền mẫu SQL trích xuất và đồng bộ.
/// Created date: 17/10/2026
/// </summary>
[Collection("api")]
public partial class ShareDataPacketControllerTests(Host host)
{
    private readonly Host _host = host;
    private readonly IHttpContextAccessor _httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();

    private void SetUserContext(bool isSuperAdmin, string account = "test_operator")
    {
        var accountType = isSuperAdmin ? ((int)BaseEnums.AccountTypeEnum.SuperAdmin).ToString() : "333";
        var claims = new List<Claim>
        {
            new(ClaimConst.AccountType, accountType),
            new(ClaimConst.Account, account),
            new(ClaimConst.UserId, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Name, account)
        };

        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
    }

    [Fact]
    public async Task AddPacket_StandardCode101CommonData_AutoFillsTemplateAndSyncMode_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_commonData_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet 101 {unique}",
            PacketVersion = "1.0"
        };

        // Act: Gọi qua điểm vào thật IMessageBus theo Rule 19.84
        await bus.InvokeAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.True(PacketDefaultTemplate.TryGet("101", out var tpl101));
        Assert.Equal(tpl101.ExtractSql, packet.ExtractSql);
        Assert.Equal(BaseEnums.SyncMode.Snapshot, packet.SyncMode);
    }

    [Fact]
    public async Task AddPacket_PrefixMatches101Custom_AutoFillsTemplate_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_tuychinh_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Custom 101 {unique}",
            PacketVersion = "1.0"
        };

        // Act
        await bus.InvokeAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.True(PacketDefaultTemplate.TryGet("101", out var tpl101Prefix));
        Assert.Equal(tpl101Prefix.ExtractSql, packet.ExtractSql);
        Assert.Equal(BaseEnums.SyncMode.Snapshot, packet.SyncMode);
    }

    [Fact]
    public async Task AddPacket_NonStandardCode_KeepsExtractSqlNull_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"999_xyz_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Non standard packet {unique}",
            PacketVersion = "1.0"
        };

        // Act
        await bus.InvokeAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Null(packet.ExtractSql);
    }

    [Fact]
    public async Task AddPacket_WithExistingExtractSql_PreservesCustomSql_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_predefined_{unique}";
        var customSql = "SELECT TOP (1) 1 AS val";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Custom Sql Packet {unique}",
            PacketVersion = "1.0",
            ExtractSql = customSql
        };

        // Act
        await bus.InvokeAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Equal(customSql, packet.ExtractSql);
    }

    [Fact]
    public async Task AddPacket_Code111_DoesNotAutoFillTemplate_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"111_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet 111 {unique}",
            PacketVersion = "1.0"
        };

        // Act
        await bus.InvokeAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Null(packet.ExtractSql);
    }

    [Fact]
    public async Task AddPacket_DuplicateCode_ThrowsOopsException_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_dup_{unique}";
        var input1 = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Dup 1 {unique}",
            PacketVersion = "1.0"
        };
        var input2 = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Dup 2 {unique}",
            PacketVersion = "1.0"
        };

        await bus.InvokeAsync(input1);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => bus.InvokeAsync(input2));
    }

    [Fact]
    public async Task GetExtractSql_ForceDefaultTemplateTrue_ReturnsStandardTemplateWithoutChangingDatabase_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"103_default_{unique}";
        var savedCustomSql = "SELECT TOP (1) ID FROM TmsTrafficData";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = code,
            Name = $"Packet 103 Test {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = savedCustomSql,
            SyncMode = BaseEnums.SyncMode.Snapshot
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        // Act
        var output = await bus.InvokeAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput
        {
            PacketCode = code,
            ForceDefaultTemplate = true
        });

        // Assert
        Assert.NotNull(output);
        Assert.True(output.HasDefaultTemplate);
        Assert.True(PacketDefaultTemplate.TryGet("103", out var tpl103));
        Assert.Equal(tpl103.ExtractSql, output.ExtractSql);
        Assert.Equal(BaseEnums.SyncMode.Incremental, output.SyncMode);
        Assert.Equal(1, output.MaxLookBackDays);

        // DB không bị thay đổi
        var inDb = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.Equal(savedCustomSql, inDb.ExtractSql);
    }

    [Fact]
    public async Task GetExtractSql_WhenNoTemplateAvailable_ReturnsCurrentDatabaseValueWithHasDefaultFalse_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"999_notemplate_{unique}";
        var savedSql = "SELECT TOP (1) ID FROM TmsTrafficData";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = code,
            Name = $"Non template packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = savedSql,
            SyncMode = BaseEnums.SyncMode.Snapshot
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        // Act
        var output = await bus.InvokeAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput
        {
            PacketCode = code,
            ForceDefaultTemplate = true
        });

        // Assert
        Assert.NotNull(output);
        Assert.False(output.HasDefaultTemplate);
        Assert.Equal(savedSql, output.ExtractSql);
    }

    [Fact]
    public async Task AddPacket_AutoFilledExtractSql_PreventsMissingExtractSqlAlert_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_reg_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Regression {unique}",
            PacketVersion = "1.0"
        };

        // Act
        await bus.InvokeAsync(input);

        // Assert: Gói tin đã tự điền ExtractSql nên không bị thiếu câu truy vấn nguồn
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.False(string.IsNullOrWhiteSpace(packet.ExtractSql));
        Assert.True(PacketDefaultTemplate.TryGet("101", out var tpl101Reg));
        Assert.Equal(tpl101Reg.ExtractSql, packet.ExtractSql);
    }
}
