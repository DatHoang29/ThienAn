using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Shared.Core.Utilities.Constants;

using Tests.ShareData.Common;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử tích hợp cho ShareDataPacketController qua Wolverine IMessageBus (Rule 19.84).
///              Bao gồm quản lý thêm/sửa/xóa gói tin, tự động điền mẫu SQL trích xuất và đồng bộ.
/// Created date: 17/10/2026
/// </summary>
[Collection("api")]
public partial class ShareDataPacketControllerTests(Host host) : IAsyncLifetime
{
    private readonly Host _host = host;
    private readonly IHttpContextAccessor _httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();

    public async Task InitializeAsync()
    {
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        await db.Deleteable<ShareDataPacket>()
            .Where(x => x.Code != null && (x.Code.Contains("_upd_lock_") || x.Code.Contains("_src_") || x.Code.Contains("_save_sql_")))
            .ExecuteCommandAsync();
        await EnsureCatalogTables(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task EnsureCatalogTables(ISqlSugarClient db)
    {
        var existingTables = (await db.Queryable<ShareDataSourceTable>().Where(x => x.IsDelete == null).ToListAsync())
            .Select(x => x.TableName?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seedRows = SeedScriptReader.ReadSourceTables();
        var toInsert = seedRows
            .Where(s => !string.IsNullOrEmpty(s.TableName) && !existingTables.Contains(s.TableName.Trim()))
            .Select(s => new ShareDataSourceTable
            {
                ID = s.ID,
                TableName = s.TableName,
                Name = s.Name,
                PacketCodes = s.PacketCodes,
                OrderNo = s.OrderNo,
                IsTracked = s.IsTracked,
                IsQueryable = s.IsQueryable,
                DefaultAlias = s.DefaultAlias,
                Remark = s.Remark,
                LastVersion = s.LastVersion,
                TenantId = s.TenantId,
                CreateTime = s.CreateTime
            }).ToList();

        if (toInsert.Count > 0)
        {
            await db.Insertable(toInsert).ExecuteCommandAsync();
        }
    }

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
    public async Task AddPacket_OrderNo101_AutoFillsTemplateFromSource_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        Assert.True(PacketTemplateSeed.TryGet("101", out var tpl101));
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"101_src_{unique}",
            Name = $"Source 101 {unique}",
            OrderNo = 101,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = tpl101.ExtractSql,
            CursorTimeColumn = tpl101.CursorTimeColumn,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();

        var code = $"101_commonData_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet 101 {unique}",
            PacketVersion = "1.0",
            OrderNo = 101
        };

        // Act: Gọi qua điểm vào thật CallApiAsync theo Rule 19.84
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Equal(tpl101.ExtractSql, packet.TemplateSql);
        Assert.Equal(tpl101.ExtractSql, packet.ExtractSql);
    }

    [Fact]
    public async Task AddPacket_CustomCodeWithOrderNo103_AutoFillsTemplateFromSource_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        Assert.True(PacketTemplateSeed.TryGet("103", out var tpl103));
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"103_src_{unique}",
            Name = $"Source 103 {unique}",
            OrderNo = 103,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = tpl103.ExtractSql,
            CursorTimeColumn = tpl103.CursorTimeColumn,
            MaxLookBackDays = tpl103.MaxLookBackDays,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();

        var code = $"custom_vds_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Custom 103 {unique}",
            PacketVersion = "1.0",
            OrderNo = 103
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Equal(tpl103.ExtractSql, packet.TemplateSql);
        Assert.Equal(tpl103.ExtractSql, packet.ExtractSql);
        Assert.Equal(tpl103.CursorTimeColumn, packet.CursorTimeColumn);
        Assert.Equal(tpl103.MaxLookBackDays, packet.MaxLookBackDays);
    }

    [Fact]
    public async Task AddPacket_OrderNo111_AutoFillsTemplateFromSource_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        Assert.True(PacketTemplateSeed.TryGet("111", out var tpl111));
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"111_src_{unique}",
            Name = $"Source 111 {unique}",
            OrderNo = 111,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = tpl111.ExtractSql,
            CursorTimeColumn = tpl111.CursorTimeColumn,
            MaxLookBackDays = tpl111.MaxLookBackDays,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();

        var code = $"111_intercenter_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet 111 {unique}",
            PacketVersion = "1.0",
            OrderNo = 111
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Equal(tpl111.ExtractSql, packet.TemplateSql);
        Assert.Equal(tpl111.ExtractSql, packet.ExtractSql);
        Assert.Equal(tpl111.CursorTimeColumn, packet.CursorTimeColumn);
        Assert.Equal(tpl111.MaxLookBackDays, packet.MaxLookBackDays);
    }

    [Fact]
    public async Task AddPacket_ClientSendsCustomTemplateSql_IsIgnoredAndOverwrittenBySource_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        Assert.True(PacketTemplateSeed.TryGet("101", out var tpl101));
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"101_src_{unique}",
            Name = $"Source 101 {unique}",
            OrderNo = 101,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = tpl101.ExtractSql,
            CursorTimeColumn = tpl101.CursorTimeColumn,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();

        var code = $"101_ignore_client_tpl_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Ignore Client Tpl {unique}",
            PacketVersion = "1.0",
            OrderNo = 101,
            TemplateSql = "SELECT 'HACKED' AS val"
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert: TemplateSql do client gửi lên bị bỏ qua, ghi đè bằng mẫu từ nguồn
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Equal(tpl101.ExtractSql, packet.TemplateSql);
        Assert.NotEqual("SELECT 'HACKED' AS val", packet.TemplateSql);
    }

    [Fact]
    public async Task AddPacket_WithExistingExtractSql_PreservesCustomExtractSqlAndLoadsTemplateSqlFromSource_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        Assert.True(PacketTemplateSeed.TryGet("101", out var tpl101));
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"101_src_{unique}",
            Name = $"Source 101 {unique}",
            OrderNo = 101,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = tpl101.ExtractSql,
            CursorTimeColumn = tpl101.CursorTimeColumn,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();

        var code = $"101_predefined_{unique}";
        var customSql = "SELECT TOP (1) 1 AS val";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Custom Sql Packet {unique}",
            PacketVersion = "1.0",
            OrderNo = 101,
            ExtractSql = customSql
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert: ExtractSql giữ giá trị client gửi, TemplateSql nạp từ nguồn
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Equal(customSql, packet.ExtractSql);
        Assert.Equal(tpl101.ExtractSql, packet.TemplateSql);
    }

    [Fact]
    public async Task AddPacket_WhenNoSourcePacketExists_LeavesTemplateSqlAndExtractSqlNull_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"102_nosource_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"No Source Packet {unique}",
            PacketVersion = "1.0",
            OrderNo = 102
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert: Không tìm thấy nguồn -> TemplateSql và ExtractSql đều null
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Null(packet.TemplateSql);
        Assert.Null(packet.ExtractSql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(112)]
    [InlineData(999)]
    public async Task AddPacket_InvalidOrderNo_ThrowsValidationException_Test(int? orderNo)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var input = new ShareDataAddPacketInput
        {
            Code = $"pkt_invalid_order_{unique}",
            Name = $"Packet Invalid Order {unique}",
            PacketVersion = "1.0",
            OrderNo = orderNo
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => scope.ServiceProvider.CallApiAsync(input));
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataAddPacketInput.OrderNo));
    }

    [Fact]
    public async Task UpdatePacket_WhenClientSendsDifferentTemplateSqlOrOrderNo_PreservesTemplateSqlAndOrderNo_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_upd_lock_{unique}";
        Assert.True(PacketTemplateSeed.TryGet("101", out var tpl101));
        var originalTemplate = tpl101.ExtractSql;
        var originalExtract = "SELECT 'ORIGINAL_EXTRACT' AS val";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = code,
            Name = $"Packet Update Lock {unique}",
            OrderNo = 101,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = originalTemplate,
            ExtractSql = originalExtract,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        var updateInput = new ShareDataUpdatePacketInput
        {
            ID = packet.ID,
            Code = code,
            Name = $"Packet Update Lock Modified {unique}",
            OrderNo = 108,
            TemplateSql = "SELECT 'HACKED_TEMPLATE' AS val",
            ExtractSql = "SELECT 'HACKED_EXTRACT' AS val"
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(updateInput);

        // Assert: TemplateSql và OrderNo không bị thay đổi bởi UpdatePacket
        var inDb = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.ID == packet.ID);
        Assert.NotNull(inDb);
        Assert.Equal($"Packet Update Lock Modified {unique}", inDb.Name);
        Assert.Equal(originalTemplate, inDb.TemplateSql);
        Assert.Equal(101, inDb.OrderNo);
        Assert.Equal(originalExtract, inDb.ExtractSql);
    }

    [Fact]
    public async Task UpdateExtractSql_UpdatesExtractSqlOnly_PreservesTemplateSql_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_save_sql_{unique}";
        Assert.True(PacketTemplateSeed.TryGet("101", out var tpl101));
        var originalTemplate = tpl101.ExtractSql;

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = code,
            Name = $"Packet Save Sql {unique}",
            OrderNo = 101,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = originalTemplate,
            ExtractSql = "SELECT z.ID AS zoneId FROM TmsZone z",
            CreateTime = DateTime.UtcNow.AddYears(-10)
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        var newExtractSql = "SELECT z.ID AS zoneId, z.Name AS zoneName FROM TmsZone z";
        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = code,
            ExtractSql = newExtractSql
        };

        // Act
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(input);

        // Assert: ExtractSql được cập nhật, TemplateSql giữ nguyên
        var inDb = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.ID == packet.ID);
        Assert.NotNull(inDb);
        Assert.Equal(newExtractSql, inDb.ExtractSql);
        Assert.Equal(originalTemplate, inDb.TemplateSql);
    }

    [Fact]
    public async Task AddPacket_DuplicateCode_ThrowsOopsException_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_dup_{unique}";
        var input1 = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Dup 1 {unique}",
            PacketVersion = "1.0",
            OrderNo = 101
        };
        var input2 = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Dup 2 {unique}",
            PacketVersion = "1.0",
            OrderNo = 101
        };

        await scope.ServiceProvider.CallApiAsync(input1);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync(input2));
    }

    [Fact]
    public async Task GetExtractSql_ForceDefaultTemplateTrue_ReturnsPacketTemplateSqlWithoutChangingDatabase_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"103_default_{unique}";
        var savedCustomSql = "SELECT TOP (1) ID FROM TmsTrafficData";
        var packetTemplateSql = "SELECT 'CUSTOM_PACKET_TEMPLATE' AS val";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = code,
            Name = $"Packet 103 Test {unique}",
            OrderNo = 103,
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = savedCustomSql,
            TemplateSql = packetTemplateSql,
            CursorTimeColumn = "UpdateTime",
            MaxLookBackDays = 2
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput
        {
            PacketCode = code,
            ForceDefaultTemplate = true
        });

        // Assert: Trả đúng TemplateSql của chính gói đó, giữ nguyên Cursor/Lookback của gói
        Assert.NotNull(output);
        Assert.True(output.HasDefaultTemplate);
        Assert.Equal(packetTemplateSql, output.ExtractSql);
        Assert.Equal("UpdateTime", output.CursorTimeColumn);
        Assert.Equal(2, output.MaxLookBackDays);

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
            TemplateSql = null,
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput
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
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        Assert.True(PacketTemplateSeed.TryGet("101", out var tpl101Reg));
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"101_src_{unique}",
            Name = $"Source 101 {unique}",
            OrderNo = 101,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = tpl101Reg.ExtractSql,
            CursorTimeColumn = tpl101Reg.CursorTimeColumn,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();

        var code = $"101_reg_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet Regression {unique}",
            PacketVersion = "1.0",
            OrderNo = 101
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert: Gói tin đã tự điền ExtractSql nên không bị thiếu câu truy vấn nguồn
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.False(string.IsNullOrWhiteSpace(packet.ExtractSql));
        Assert.True(PacketTemplateSeed.TryGet("101", out var tplExpected));
        Assert.Equal(tplExpected.ExtractSql, packet.ExtractSql);
        Assert.Equal(tplExpected.ExtractSql, packet.TemplateSql);
    }

    [Theory]
    [MemberData(nameof(GetInvalidAddPacketData))]
    public async Task AddPacket_InvalidData_ThrowsValidationException_Test(
        string? code, string? name, string? description, string expectedProperty)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var testCode = code switch
        {
            "" => "",
            null => null,
            _ => code.Length > 128 ? code : $"{code}_{unique}"
        };

        var input = new ShareDataAddPacketInput
        {
            Code = testCode,
            Name = name,
            Description = description,
            PacketVersion = "1.0",
            OrderNo = 101
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => scope.ServiceProvider.CallApiAsync(input));
        Assert.Contains(ex.Errors, e => e.PropertyName == expectedProperty);

        if (!string.IsNullOrEmpty(testCode))
        {
            var inDb = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == testCode);
            Assert.Null(inDb);
        }
    }

    public static IEnumerable<object?[]> GetInvalidAddPacketData()
    {
        yield return new object?[] { "", "Valid Name", null, nameof(ShareDataAddPacketInput.Code) };
        yield return new object?[] { new string('A', 129), "Valid Name", null, nameof(ShareDataAddPacketInput.Code) };
        yield return new object?[] { "VALID_CODE", "", null, nameof(ShareDataAddPacketInput.Name) };
        yield return new object?[] { "VALID_CODE", new string('B', 129), null, nameof(ShareDataAddPacketInput.Name) };
        yield return new object?[] { "VALID_CODE", "Valid Name", new string('C', 257), nameof(ShareDataAddPacketInput.Description) };
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task UpdatePacket_EmptyId_ThrowsValidationException_Test(string? id)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();

        var input = new ShareDataUpdatePacketInput
        {
            ID = id,
            Code = $"PKT_UPDATE_{Guid.NewGuid():N}",
            Name = "Packet Update Test",
            PacketVersion = "1.0"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => scope.ServiceProvider.CallApiAsync(input));
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataUpdatePacketInput.ID));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task DeletePacket_EmptyId_ThrowsValidationException_Test(string? id)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();

        var input = new ShareDataDeletePacketInput
        {
            ID = id
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => scope.ServiceProvider.CallApiAsync(input));
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(ShareDataDeletePacketInput.ID));
    }
}
