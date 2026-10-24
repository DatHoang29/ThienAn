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

    /// <summary>
    /// Description: Khi tạo gói tin mới có OrderNo khớp mẫu có sẵn (101, 103, 111), tự động điền câu truy vấn mẫu và thông tin con trỏ từ nguồn
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData(101, "101_commonData")]
    [InlineData(103, "custom_vds")]
    [InlineData(111, "111_intercenter")]
    public async Task AddPacket_WhenOrderNoMatchesTemplateSeed_AutoFillsTemplateFromSource_Test(int orderNo, string codePrefix)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        Assert.True(PacketTemplateSeed.TryGet(orderNo.ToString(), out var tpl));
        var seededPacketId = Guid.NewGuid().ToString("N");
        await db.Insertable(new ShareDataPacket
        {
            ID = seededPacketId,
            Code = $"{orderNo}_src_{unique}",
            Name = $"Source {orderNo} {unique}",
            OrderNo = orderNo,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = tpl.ExtractSql,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, seededPacketId, null, tpl.CursorTimeColumn, tpl.MaxLookBackDays);

        var code = $"{codePrefix}_{unique}";
        var input = new ShareDataAddPacketInput
        {
            Code = code,
            Name = $"Packet {orderNo} {unique}",
            PacketVersion = "1.0",
            OrderNo = orderNo
        };

        // Act: Gọi qua điểm vào thật CallApiAsync theo Rule 19.84
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
        Assert.Equal(tpl.ExtractSql, packet.TemplateSql);
        var commonSql = await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        Assert.Equal(tpl.ExtractSql, commonSql?.ExtractSql);
        if (tpl.CursorTimeColumn != null)
        {
            Assert.Equal(tpl.CursorTimeColumn, commonSql?.CursorTimeColumn);
            Assert.Equal(tpl.MaxLookBackDays, commonSql?.MaxLookBackDays);
        }
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
        var seededPacketId4 = Guid.NewGuid().ToString("N");
        await db.Insertable(new ShareDataPacket
        {
            ID = seededPacketId4,
            Code = $"101_src_{unique}",
            Name = $"Source 101 {unique}",
            OrderNo = 101,
            Status = BaseEnums.StatusEnum.Enable,
            TemplateSql = tpl101.ExtractSql,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, seededPacketId4, null, tpl101.CursorTimeColumn);

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
        Assert.Null((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
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
            CreateTime = DateTime.UtcNow.AddYears(-10)
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, originalExtract);

        var updateInput = new ShareDataUpdatePacketInput
        {
            ID = packet.ID,
            Code = code,
            Name = $"Packet Update Lock Modified {unique}",
            OrderNo = 108,
            TemplateSql = "SELECT 'HACKED_TEMPLATE' AS val"
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(updateInput);

        // Assert: TemplateSql và OrderNo không bị thay đổi bởi UpdatePacket
        var inDb = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.ID == packet.ID);
        Assert.NotNull(inDb);
        Assert.Equal($"Packet Update Lock Modified {unique}", inDb.Name);
        Assert.Equal(originalTemplate, inDb.TemplateSql);
        Assert.Equal(101, inDb.OrderNo);
        Assert.Equal(originalExtract, (await ShareDataSqlSeed.GetCommonAsync(db, inDb.ID))?.ExtractSql);
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
            CreateTime = DateTime.UtcNow.AddYears(-10)
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT z.ID AS zoneId FROM TmsZone z");

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
        Assert.Equal(newExtractSql, (await ShareDataSqlSeed.GetCommonAsync(db, inDb.ID))?.ExtractSql);
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
            TemplateSql = packetTemplateSql,
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, savedCustomSql, "UpdateTime", 2);

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
        Assert.Equal(savedCustomSql, (await ShareDataSqlSeed.GetCommonAsync(db, inDb.ID))?.ExtractSql);
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
            TemplateSql = null,
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, savedSql);

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
