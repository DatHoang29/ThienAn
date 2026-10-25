using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Shared.Core.Utilities.Constants;

using Tests.ShareData.Common;
using System.Diagnostics;
using Furion.FriendlyException;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Core.Helpers;
using Module.ShareData.Infrastructure.Services.ShareDataConfig;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using Module.ShareData.Core.Dto.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Xunit;
using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Constants;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử tích hợp cho ShareDataPacketController qua Wolverine IMessageBus (Rule 19.84).
///              Bao gồm quản lý thêm/sửa/xóa gói tin, tự động điền mẫu SQL trích xuất và đồng bộ.
/// Created date: 17/10/2026
/// </summary>
[Collection("api")]
public class ShareDataPacketControllerTests(Host host) : IAsyncLifetime
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

    #region ExtractSql

    [Theory]
    [InlineData("SELECT 1; DROP TABLE Test;")]
    [InlineData("SELECT 1; SELECT 2;")]
    [InlineData("SELECT * FROM SysUser")]
    [InlineData("SELECT * FROM SysTenant")]
    [InlineData("INSERT INTO ShareDataPacket (Code) VALUES ('HACK')")]
    [InlineData("DELETE FROM ShareDataPacket")]
    [InlineData("UPDATE ShareDataPacket SET Code = 'X'")]
    [InlineData("DROP TABLE ShareDataPacket")]
    [InlineData("EXEC sp_who2")]
    [InlineData("SELECT * INTO NewTable FROM ShareDataPacket")]
    public void ShareDataSqlValidator_Rejects_DangerousOrMultiStatementQueries(string unsafeSql)
    {
        // Act
        var (isValid, errorMsg) = SqlValidator.Validate(unsafeSql);

        // Assert
        Assert.False(isValid);
        Assert.False(string.IsNullOrWhiteSpace(errorMsg));
    }

    [Fact]
    public async Task UpdateExtractSql_WhenNotSuperAdmin_ThrowsOopsException_Test()
    {
        // Arrange: Người dùng thông thường (không phải SuperAdmin)
        var normalAccount = $"user_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: false, account: normalAccount);

        using var scope = _host.Services.CreateScope();

        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT ID, Code, Name FROM TmsZone"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(input));
    }

    [Fact]
    public async Task GetExtractSql_WhenNotSuperAdmin_ThrowsOopsException_Test()
    {
        // Arrange
        var normalAccount = $"user_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: false, account: normalAccount);

        using var scope = _host.Services.CreateScope();

        var input = new GetPacketExtractSqlInput
        {
            PacketCode = "101"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(input));
    }

    [Fact]
    public async Task UpdateExtractSql_WhenSuperAdmin_ValidSql_UpdatesAndAudits_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_{unique}";
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Test {unique}",
            Status = BaseEnums.StatusEnum.Enable,
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, null);

        var validSql = "SELECT ID, Code, Name FROM TmsZone";
        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            ExtractSql = validSql,
            Remark = "Cập nhật qua test xUnit"
        };

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(input);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(validSql, output.ExtractSql);

        var savedPacket = await db.Queryable<ShareDataPacket>()
            .Where(x => x.Code == packetCode)
            .FirstAsync();
        Assert.NotNull(savedPacket);
        Assert.Equal(validSql, (await ShareDataSqlSeed.GetCommonAsync(db, savedPacket.ID))?.ExtractSql);

        var auditLog = await db.Queryable<ShareDataActivityLog>()
            .Where(x => x.TargetId == packet.ID && x.Action == BaseEnums.ActivityAction.Update)
            .OrderBy("OccurredAt DESC")
            .FirstAsync();
        Assert.NotNull(auditLog);
        Assert.Equal(BaseEnums.ShareDataTargetType.Packet, auditLog.TargetType);
        Assert.Equal(operatorAccount, auditLog.OperatorName);
    }

    [Fact]
    public async Task Worker_DataOutboundExtractionProcess_GuardsAgainstUnsafeExtractSql_Test()
    {
        // Arrange: Worker trích xuất dữ liệu phải từ chối ngay nếu ExtractSql chứa câu lệnh nguy hiểm
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var extractionProcess = new DataOutboundExtractionProcess();

        var sub = new ShareDataSubscription
        {
            ID = Guid.NewGuid().ToString("N"),
            DatatypeId = "101",
            Direction = BaseEnums.Direction.Outbound
        };

        var unsafePacket = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = "101"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ShareDataWorker.Core.Exceptions.ShareDataException>(() =>
            extractionProcess.Extract(
                db,
                sub,
                unsafePacket,
                cursor: null,
                onLogWarning: (_, _) => { },
                cancellationToken: CancellationToken.None,
                partnerSql: "SELECT 1; DROP TABLE TestTbl;"));

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task GetExtractSql_ReturnsConfiguredExtractSqlOnPacket_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"GET_{unique}";
        var customSql = "SELECT TOP (10) ID, Code FROM TmsZone";
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, customSql);

        var input = new GetPacketExtractSqlInput
        {
            PacketCode = packetCode
        };

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(input);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(packetCode, output.PacketCode);
        Assert.Equal(customSql, output.ExtractSql);
    }

    [Fact]
    public async Task UpdateExtractSql_WhenExtractSqlNull_ClearsPacketSql_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"CLEAR_{unique}";
        var templateSql = "SELECT ID, Name FROM TmsZone";
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, templateSql);

        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            ExtractSql = null
        };

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(input);

        // Assert
        Assert.NotNull(output);
        Assert.True(string.IsNullOrEmpty(output.ExtractSql));

        var inDb = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == packetCode);
        Assert.True(string.IsNullOrEmpty((await ShareDataSqlSeed.GetCommonAsync(db, inDb.ID))?.ExtractSql));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task UpdateExtractSql_WhenLookbackDaysOutOfRange_ThrowsOops_Test(int lookBackDays)
    {
        // RV-1: MaxLookBackDays chỉ cho phép 1 - 365 ngày
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();

        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = "103",
            MaxLookBackDays = lookBackDays,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone"
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(input));
    }

    [Fact]
    public async Task ExecutePacketSqlSample_WithOrderByWithoutTop_ExecutesSuccessfully_Test()
    {
        // RV-2: SQL có ORDER BY nhưng không có TOP -> Tự động bọc lại hợp lệ, không gây lỗi cú pháp
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT ID, Code, Name FROM TmsZone ORDER BY Code ASC",
            Top = 5
        };

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(input);

        // Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Columns);
        Assert.Contains("Code", result.Columns);
        Assert.NotNull(result.Rows);
    }

    [Fact]
    public async Task ExecutePacketSqlSample_WithDuplicateColumns_AppendsSuffixIndex_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT 1 AS TestCol, 2 AS TestCol, 3 AS TestCol",
            Top = 5
        };

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(input);

        // Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Columns);
        Assert.Equal(["TestCol", "TestCol_2", "TestCol_3"], result.Columns);
        Assert.NotNull(result.Rows);
        Assert.NotEmpty(result.Rows);
        Assert.Equal(1, Convert.ToInt32(result.Rows[0]["TestCol"]));
        Assert.Equal(2, Convert.ToInt32(result.Rows[0]["TestCol_2"]));
        Assert.Equal(3, Convert.ToInt32(result.Rows[0]["TestCol_3"]));
    }

    [Fact]
    public async Task ExecutePacketSqlSample_WithPageSizeParameter_ExecutesSuccessfully_Test()
    {
        // Arrange: @pageSize phải được khai ở bảng tham số câu truy vấn của gói tin
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_PS_{unique}";
        var packetId = Guid.NewGuid().ToString("N");
        var sql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone";
        await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {unique}", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
        await ShareDataSqlSeed.DeclareStandardVariablesAsync(db, packetId, null, sql);

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = packetCode,
            ExtractSql = sql,
            Top = 3
        };

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(input);

        // Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Columns);
        Assert.Contains("ID", result.Columns);
        Assert.NotNull(result.Rows);
    }

    [Fact]
    public async Task ExecutePacketSqlSample_BenchmarkWithoutTop_MeasuresExecutionTime_Test()
    {
        // RV-3: Đo thời gian chạy SQL mẫu
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT ID, Code, Name FROM TmsZone",
            Top = 5
        };

        var sw = Stopwatch.StartNew();
        var result = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(input);
        sw.Stop();

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Rows);
        Assert.True(result.Rows.Count <= 5);
        Assert.True(sw.ElapsedMilliseconds < 3000, $"Thời gian thực thi: {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Description: Cập nhật câu truy vấn nối đuôi (Incremental) thiếu cấu trúc bắt buộc (__lastKey, ORDER BY ASC) bị từ chối
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("SELECT TOP (@pageSize) ID, Code, CreateTime AS __lastTime FROM TmsZone WHERE CreateTime > @lastTime ORDER BY CreateTime ASC")]
    [InlineData("SELECT TOP (@pageSize) ID AS __rowid, Code, CreateTime AS __lastTime FROM TmsZone WHERE @lastTime IS NULL OR CreateTime > @lastTime ORDER BY CreateTime ASC")]
    [InlineData("SELECT TOP (@pageSize) ID AS __lastKey, Code, CreateTime AS __lastTime FROM TmsZone WHERE @lastTime IS NULL OR CreateTime > @lastTime ORDER BY __lastTime DESC")]
    [InlineData("SELECT TOP (@pageSize) ID, Code FROM TmsZone WHERE CreateTime > @lastTime")]
    public async Task UpdateExtractSql_WhenIncrementalSqlStructureInvalid_ThrowsOops_Test(string extractSql)
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var existing = await db.Queryable<ShareDataPacket>().Where(x => x.Code == "103").FirstAsync();
        if (existing == null)
        {
            await db.Insertable(new ShareDataPacket { ID = Guid.NewGuid().ToString("N"), Code = "103", Name = "VDS Data", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
        }

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = "103",
            ExtractSql = extractSql
        }));
    }

    [Fact]
    public async Task UpdateExtractSql_WhenLookbackDaysResetToNull_ClearsDatabaseValue_Test()
    {
        // RV-5: Lưu MaxLookBackDays = 7, lưu lại với null -> đọc lại ra null
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_{unique}";
        var packetId = Guid.NewGuid().ToString("N");
        await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {unique}", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
        await ShareDataSqlSeed.DeclareStandardVariablesAsync(db, packetId, null, "SELECT TOP (@pageSize) ID, Code FROM TmsZone");

        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            MaxLookBackDays = 7,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone"
        });

        var out1 = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput { PacketCode = packetCode });
        Assert.Equal(7, out1.MaxLookBackDays);

        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            MaxLookBackDays = null,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone"
        });

        var out2 = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput { PacketCode = packetCode });
        Assert.Null(out2.MaxLookBackDays);
    }

    [Theory]
    [InlineData("SELECT * FROM OtherDb.dbo.TmsZone")]
    [InlineData("SELECT ConnectionConfig FROM ShareDataPartner")]
    [InlineData("SELECT * FROM SysOrg")]
    public void Validate_When3PartTableNameOrSysTableOrPartnerTable_FailsValidation_Test(string sql)
    {
        // RV-7: Chặn CSDL khác và bảng chứa bí mật
        var (isValid, errorMsg) = SqlValidator.Validate(sql);
        Assert.False(isValid);
        Assert.NotNull(errorMsg);
    }

    [Fact]
    public async Task UpdateExtractSql_WhenSqlIsSnapshot_AllowsSnapshotSql_Test()
    {
        // SQL không có @lastTime/@lastKey -> suy ra Snapshot, cho phép lưu mà không kiểm con trỏ
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_SNAP_{unique}";
        var packetId = Guid.NewGuid().ToString("N");
        await db.Insertable(new ShareDataPacket
        {
            ID = packetId,
            Code = packetCode,
            Name = $"Packet Snapshot {unique}",
            Status = BaseEnums.StatusEnum.Enable
        }).ExecuteCommandAsync();
        await ShareDataSqlSeed.DeclareStandardVariablesAsync(db, packetId, null, "SELECT TOP (@pageSize) ID, Code FROM TmsZone");

        // Act: SQL dạng Snapshot bình thường
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone"
        });

        // Assert: Lưu thành công
        var output = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput { PacketCode = packetCode });
        Assert.NotNull(output);
        Assert.Contains("SELECT TOP (@pageSize) ID, Code FROM TmsZone", output.ExtractSql);
    }

    [Fact]
    public async Task UpdateExtractSql_WhenTableNotQueryable_RejectsWithSecurityError_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N");
        var packetCode = $"PKT_SEC_{unique}";

        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable
        }).ExecuteCommandAsync();

        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            ExtractSql = "SELECT * FROM NonAllowedTable"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(input));
        var msg = ex.ToString();
        Assert.True(
            msg.Contains(BaseMsg.ShareData.Validation.SqlTableNotAllowed)
            || msg.Contains("NonAllowedTable")
            || (ex is AppFriendlyException afe && afe.ErrorMessage?.ToString() == BaseMsg.ShareData.Validation.SqlTableNotAllowed),
            $"Kỳ vọng ngoại lệ chứa mã SqlTableNotAllowed hoặc tên bảng NonAllowedTable, nhận được: {msg}");
    }

    [Fact]
    public async Task ParsePacketColumns_And_ExecutePacketSqlSample_WhenSqlViolatesSecurity_ReturnsBlockedError_Test()
    {
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var expectedError = _host.Localizer[BaseMsg.ShareData.Validation.SqlOnlySelectAllowed].Value;

        // 1. ParsePacketColumns
        var parseResult = await scope.ServiceProvider.CallApiAsync<ParsePacketColumnsOutput>(new ParsePacketColumnsInput
        {
            ExtractSql = "EXEC sp_who2"
        });
        Assert.False(parseResult.IsValid);
        Assert.True(
            parseResult.ErrorMessage == expectedError
            || parseResult.ErrorMessage?.Contains(BaseMsg.ShareData.Validation.SqlOnlySelectAllowed, StringComparison.OrdinalIgnoreCase) == true
            || parseResult.ErrorMessage?.Contains("SELECT", StringComparison.OrdinalIgnoreCase) == true,
            $"ParsePacketColumns error '{parseResult.ErrorMessage}' không khớp kỳ vọng.");

        // 2. ExecutePacketSqlSample
        var sampleResult = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "EXEC sp_who2",
            Top = 5
        });
        Assert.False(sampleResult.IsSuccess);
        Assert.True(
            sampleResult.ErrorMessage == expectedError
            || sampleResult.ErrorMessage?.Contains(BaseMsg.ShareData.Validation.SqlOnlySelectAllowed, StringComparison.OrdinalIgnoreCase) == true
            || sampleResult.ErrorMessage?.Contains("SELECT", StringComparison.OrdinalIgnoreCase) == true,
            $"ExecutePacketSqlSample error '{sampleResult.ErrorMessage}' không khớp kỳ vọng.");
    }

    [Fact]
    public async Task ExecutePacketSqlSample_WhenSqlExecutionFails_ReturnsFailureWithLocalizedMessage_Test()
    {
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT TOP 5 CAST('x' AS INT) AS a FROM TmsZone",
            Top = 5
        };

        var result = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(input);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);

        var template = _host.Localizer[BaseMsg.ShareData.Exception.SqlSampleExecutionFailed].Value;
        var prefix = template.Contains("{0}", StringComparison.Ordinal)
            ? template.Split("{0}")[0].Trim()
            : template;

        Assert.True(
            result.ErrorMessage.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains(BaseMsg.ShareData.Exception.SqlSampleExecutionFailed, StringComparison.OrdinalIgnoreCase),
            $"ErrorMessage '{result.ErrorMessage}' không khớp mẫu '{prefix}' hoặc khóa '{BaseMsg.ShareData.Exception.SqlSampleExecutionFailed}'.");
    }

    #endregion

    #region Metadata

    [Fact]
    public async Task GetMetadataTables_WhenSuperAdmin_ReturnsAllAllowedTables_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        // Act: Đi qua đúng điểm vào qua CallApiAsync (Rule 19.84)
        var tables = await scope.ServiceProvider.CallApiAsync<List<MetadataTableDto>>(new GetMetadataTablesInput());

        List<ShareDataSourceTable> sourceTables = null!;
        if (db.DbMaintenance.IsAnyTable("ShareDataSourceTable", false))
        {
            sourceTables = await db.Queryable<ShareDataSourceTable>()
                .Where(x => x.IsDelete == null && x.IsQueryable == true && !string.IsNullOrWhiteSpace(x.TableName))
                .OrderBy(x => x.OrderNo)
                .OrderBy(x => x.ID)
                .ToListAsync();
        }

        if (sourceTables == null || sourceTables.Count == 0)
        {
            sourceTables = SeedScriptReader.ReadSourceTables()
                .Where(x => x.IsQueryable == true && !string.IsNullOrWhiteSpace(x.TableName))
                .OrderBy(x => x.OrderNo)
                .ThenBy(x => x.ID)
                .ToList();
        }

        // Assert
        Assert.NotNull(tables);
        Assert.Equal(sourceTables.Count, tables.Count);
        Assert.Contains(tables, t => t.TableName == "TmsZone" && t.DefaultAlias == "z");
        Assert.Contains(tables, t => t.TableName == "TmsTrafficData" && t.DefaultAlias == "td");
        Assert.All(tables, t => Assert.NotNull(t.PacketCodes));

        var zoneStatus = Assert.Single(tables, t => t.TableName == "TmsZoneStatus");
        Assert.Contains("101", zoneStatus.PacketCodes);

        var weather = Assert.Single(tables, t => t.TableName == "TmsWeather");
        Assert.Contains("104", weather.PacketCodes);
    }

    [Fact]
    public async Task GetMetadataFields_WhenSuperAdminAndWhitelistedTable_ReturnsRealDbColumns_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();

        // Act: lấy mã định danh bảng từ danh mục rồi gọi lấy cột bằng mã (không gửi tên bảng)
        var tables = await scope.ServiceProvider.CallApiAsync<List<MetadataTableDto>>(new GetMetadataTablesInput());
        var zone = Assert.Single(tables, t => t.TableName == "TmsZone");
        Assert.False(string.IsNullOrWhiteSpace(zone.Id));
        var fields = await scope.ServiceProvider.CallApiAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableId = zone.Id
        });

        // Assert
        Assert.NotNull(fields);
        Assert.NotEmpty(fields);
        Assert.Contains(fields, f => f.ColumnName == "ID" && f.IsPrimaryKey == true && !string.IsNullOrWhiteSpace(f.DataType) && f.SuggestedAlias == null);
        Assert.Contains(fields, f => f.ColumnName == "Name" && !string.IsNullOrWhiteSpace(f.DataType) && f.SuggestedAlias == null);
    }

    [Fact]
    public async Task GetMetadataTables_WhenNotSuperAdmin_ThrowsSuperAdminRequiredException_Test()
    {
        // Arrange: Người dùng không phải SuperAdmin
        SetUserContext(isSuperAdmin: false, account: $"operator_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<List<MetadataTableDto>>(new GetMetadataTablesInput()));
        var msg = ex.Message;
        Assert.True(msg.Contains("superAdminRequiredForMetadata") || (ex is AppFriendlyException afe && afe.ErrorMessage?.ToString() == BaseMsg.ShareData.Exception.SuperAdminRequiredForMetadata));
    }

    [Fact]
    public async Task GetMetadataFields_WhenNotSuperAdmin_ThrowsSuperAdminRequiredException_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false, account: $"operator_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableId = "any-table-id"
        }));
        var msg = ex.Message;
        Assert.True(msg.Contains("superAdminRequiredForMetadata") || (ex is AppFriendlyException afe && afe.ErrorMessage?.ToString() == BaseMsg.ShareData.Exception.SuperAdminRequiredForMetadata));
    }

    /// <summary>
    /// Description: TableId không có trong danh mục (kể cả gửi tên bảng, bảng hệ thống, ID không tồn tại hoặc ký tự đặc biệt) thì trả về danh sách trường rỗng
    /// Created date: 17/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("SysUser")]
    [InlineData("ShareDataPartner")]
    [InlineData("TmsZone")]
    [InlineData("NonExistentTableId")]
    [InlineData("TmsZone; DROP TABLE Test;")]
    [InlineData("sys.tables")]
    public async Task GetMetadataFields_WhenTableIdNotWhitelistedOrInvalid_ReturnsEmpty_Test(string invalidTableId)
    {
        // Arrange: mã không có trong danh mục (kể cả gửi nhầm tên bảng thay vì mã, ký tự lạ) thì không trả cột
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();

        // Act & Assert
        var fields = await scope.ServiceProvider.CallApiAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableId = invalidTableId
        });
        Assert.NotNull(fields);
        Assert.Empty(fields);
    }

    [Fact]
    public async Task GetMetadataFields_WhenWhitelistedTableMissingInDb_ReturnsEmpty_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testId = Guid.NewGuid().ToString("N");
        var missingTableName = $"ZzMissing{testId}";
        var testRow = new ShareDataSourceTable
        {
            ID = testId,
            TableName = missingTableName,
            Name = $"Bảng mẫu chưa migrate {missingTableName}",
            IsQueryable = true,
            IsTracked = true,
            OrderNo = 9999,
            DefaultAlias = "zm",
            PacketCodes = "101",
            Remark = "Test missing table in database"
        };

        await db.Insertable(testRow).ExecuteCommandAsync();

        try
        {
            // Act
            var fields = await scope.ServiceProvider.CallApiAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
            {
                TableId = testId
            });

            // Assert
            Assert.NotNull(fields);
            Assert.Empty(fields);
        }
        finally
        {
            await db.Deleteable<ShareDataSourceTable>()
                .Where(x => x.ID == testId)
                .ExecuteCommandAsync();
        }
    }

    #endregion

    #region PartnerFilter

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

        // Dùng chung: có đăng ký gửi, không có SQL riêng
        var shared = NewPartnerSqlPacket($"PKT_SHARED_{unique}", unique);
        // Riêng: có đăng ký gửi và có SQL riêng
        var custom = NewPartnerSqlPacket($"PKT_CUSTOM_{unique}", unique);
        // Chỉ có SQL riêng, không có đăng ký
        var customOnly = NewPartnerSqlPacket($"PKT_ONLYSQL_{unique}", unique);
        // Đăng ký ghi theo mã gói thay vì ID
        var byCode = NewPartnerSqlPacket($"PKT_BYCODE_{unique}", unique);
        // Không liên quan, và gói có đăng ký đã hủy
        var unrelated = NewPartnerSqlPacket($"PKT_NONE_{unique}", unique);
        var cancelled = NewPartnerSqlPacket($"PKT_CANCEL_{unique}", unique);

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
            new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partner.ID, PacketId = custom.ID, ExtractSql = "SELECT ID FROM TmsZone" },
            new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partner.ID, PacketId = customOnly.ID, ExtractSql = "SELECT Code FROM TmsZone" },
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

    #endregion

    #region PartnerSql

    private static ShareDataPacket NewPartnerSqlPacket(string packetCode, string unique) => new()
    {
        ID = Guid.NewGuid().ToString("N"),
        Code = packetCode,
        Name = $"Packet {unique}",
        Status = BaseEnums.StatusEnum.Enable
    };

    private static ShareDataPartner NewPartnerSqlPartner(string code, string unique) => new()
    {
        ID = Guid.NewGuid().ToString("N"),
        Code = code,
        Name = $"Partner {code} {unique}",
        Status = BaseEnums.StatusEnum.Enable
    };

    private static ShareDataSubscription NewPartnerSqlSubscription(ShareDataPartner partner, ShareDataPacket packet, BaseEnums.Direction direction, BaseEnums.SubSubscriptionState state) => new()
    {
        ID = Guid.NewGuid().ToString("N"),
        PartnerId = partner.ID,
        DatatypeId = packet.ID,
        Direction = direction,
        State = state
    };

    [Fact]
    public async Task GetPartnerSqlList_WhenNotSuperAdmin_ThrowsOopsException_Test()
    {
        // Arrange
        var normalAccount = $"user_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: false, account: normalAccount);

        using var scope = _host.Services.CreateScope();
        var input = new GetPartnerSqlListInput { PacketCode = "101" };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<List<PartnerSqlListItemOutput>>(input));
    }

    [Fact]
    public async Task GetPartnerSqlList_ListsSubscribedPartnersAndPartnersWithCustomSql_WithFlags_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_LST_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);

        // A: có đăng ký gửi, dùng SQL chung, có hồ sơ ánh xạ đang bật
        var partnerA = NewPartnerSqlPartner($"PT_A_{unique}", unique);
        // B: có đăng ký gửi, có SQL riêng, chưa có hồ sơ ánh xạ
        var partnerB = NewPartnerSqlPartner($"PT_B_{unique}", unique);
        // C: có đăng ký gửi, có SQL riêng khác mẫu chung
        var partnerC = NewPartnerSqlPartner($"PT_C_{unique}", unique);
        // D: chỉ có đăng ký nhận -> không được liệt kê
        var partnerD = NewPartnerSqlPartner($"PT_D_{unique}", unique);
        // E: đăng ký gửi đã hủy -> không được liệt kê
        var partnerE = NewPartnerSqlPartner($"PT_E_{unique}", unique);
        // F: không có đăng ký nhưng đã có SQL riêng -> vẫn liệt kê để quản lý
        var partnerF = NewPartnerSqlPartner($"PT_F_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(new[] { partnerA, partnerB, partnerC, partnerD, partnerE, partnerF }).ExecuteCommandAsync();
        await db.Insertable(new[]
        {
            NewPartnerSqlSubscription(partnerA, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active),
            NewPartnerSqlSubscription(partnerB, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Paused),
            NewPartnerSqlSubscription(partnerC, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active),
            NewPartnerSqlSubscription(partnerD, packet, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Active),
            NewPartnerSqlSubscription(partnerE, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Cancelled),
        }).ExecuteCommandAsync();
        await db.Insertable(new[]
        {
            new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partnerB.ID, PacketId = packet.ID, ExtractSql = "SELECT ID, Code FROM TmsZone" },
            new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partnerC.ID, PacketId = packet.ID, ExtractSql = "SELECT ID, Name FROM TmsZone" },
            new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partnerF.ID, PacketId = packet.ID, ExtractSql = "SELECT ID FROM TmsZone" },
        }).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<List<PartnerSqlListItemOutput>>(new GetPartnerSqlListInput { PacketCode = packetCode });

        // Assert
        Assert.NotNull(result);
        var codes = result.Select(r => r.PartnerCode).ToList();
        Assert.Contains(partnerA.Code, codes);
        Assert.Contains(partnerB.Code, codes);
        Assert.Contains(partnerC.Code, codes);
        Assert.Contains(partnerF.Code, codes);
        Assert.DoesNotContain(partnerD.Code, codes);
        Assert.DoesNotContain(partnerE.Code, codes);

        var itemA = result.First(r => r.PartnerCode == partnerA.Code);
        Assert.False(itemA.IsCustom);

        var itemB = result.First(r => r.PartnerCode == partnerB.Code);
        Assert.True(itemB.IsCustom);

        var itemC = result.First(r => r.PartnerCode == partnerC.Code);
        Assert.True(itemC.IsCustom);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_FirstTime_CreatesRowWithoutMapping_AndSuperAdminRequired_Test()
    {
        // Arrange: chưa có hồ sơ ánh xạ nào, SQL riêng vẫn soạn trước được
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_UPD_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(partner).ExecuteCommandAsync();

        var customSql = "SELECT ID, Code FROM TmsZone";

        // Act 1: Non-SuperAdmin call must throw
        SetUserContext(isSuperAdmin: false, account: "normal_user");
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = customSql
        }));

        // Act 2: SuperAdmin lưu lần đầu -> tạo dòng SQL riêng
        SetUserContext(isSuperAdmin: true, account: superAdmin);
        var output = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = customSql
        });

        // Assert
        Assert.NotNull(output);
        Assert.True(output.IsCustom);
        Assert.Equal(customSql, output.CustomSql);

        var rows = await db.Queryable<ShareDataSql>().Where(s => s.PartnerId == partner.ID && s.PacketId == packet.ID).ToListAsync();
        Assert.Single(rows);
        Assert.Equal(customSql, rows[0].ExtractSql);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenIdenticalToPacketStandard_StillKeepsPartnerRow_Test()
    {
        // Arrange: tạo SQL riêng giống hệt mẫu chuẩn -> vẫn là SQL riêng (bản đóng băng), không tự xóa
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_EQ_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);
        var partnerSql = new ShareDataSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            PacketId = packet.ID,
            ExtractSql = "SELECT ID FROM TmsZone"
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(partnerSql).ExecuteCommandAsync();

        // Act: lưu SQL trùng chuẩn (khác khoảng trắng)
        var outEq = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "  SELECT   ID,   Code,   Name   FROM   TmsZone  "
        });

        // Assert
        Assert.True(outEq.IsCustom);
        Assert.False(outEq.HasDifferences);

        var remaining = await db.Queryable<ShareDataSql>().Where(s => s.PartnerId == partner.ID && s.PacketId == packet.ID).CountAsync();
        Assert.Equal(1, remaining);
    }

    [Fact]
    public async Task ResetPartnerExtractSql_DeletesPartnerRow_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_RST_{unique}";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);
        var partnerSql = new ShareDataSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            PacketId = packet.ID,
            ExtractSql = "SELECT ID, Code FROM TmsZone"
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(partnerSql).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode
        });

        // Assert
        Assert.False(result.IsCustom);
        Assert.Null(result.CustomSql);

        var remaining = await db.Queryable<ShareDataSql>().Where(s => s.ID == partnerSql.ID).CountAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_IncrementalWithoutStructure_ThrowsOops_Test()
    {
        // Arrange: Câu SQL nối đuôi nhưng thiếu __lastKey hoặc __lastTime
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_INC_ERR_{unique}";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT TOP (@pageSize) ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act & Assert: SQL có @lastTime nhưng thiếu __lastKey, __lastTime, ORDER BY -> Throws
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone WHERE CreateTime > @lastTime"
        }));

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenPartnerOrPacketNotExist_Throws_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_NOEX_{unique}";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = Guid.NewGuid().ToString("N"),
            PacketCode = packetCode,
            ExtractSql = "SELECT ID, Code FROM TmsZone"
        }));
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = $"NOT_EXIST_{unique}",
            ExtractSql = "SELECT ID, Code FROM TmsZone"
        }));
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenEmptySql_ThrowsPartnerExtractSqlRequired_Test()
    {
        // Arrange: SQL rỗng -> ném lỗi PartnerExtractSqlRequired
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_EMP_{unique}";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_EMP_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "   "
        }));
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenDifferentIncrementalSql_KeepsCustomRow_Test()
    {
        // Arrange: SQL khác chuẩn và có @lastTime -> VẪN LƯU RIÊNG
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_DIFF_MODE_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var customSql = "SELECT TOP (@pageSize) ID AS __lastKey, CreateTime AS __lastTime, Code FROM TmsZone WHERE CreateTime >= @lastTime ORDER BY CreateTime ASC, ID ASC";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_DM_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(partner).ExecuteCommandAsync();
        await ShareDataSqlSeed.DeclareStandardVariablesAsync(db, packet.ID, partner.ID, customSql);

        // Act: Gửi câu SQL riêng nối đuôi
        var output = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = customSql
        });

        // Assert: Lưu riêng, chế độ suy từ câu SQL riêng khác câu chuẩn
        Assert.NotNull(output);
        Assert.True(output.IsCustom);
        string[] cursorNames = ["lastTime", "lastKey"];
        Assert.True(SqlValidator.IsIncrementalSql(output.CustomSql, cursorNames));
        Assert.False(SqlValidator.IsIncrementalSql(output.StandardSql, cursorNames));

        var inDb = await db.Queryable<ShareDataSql>().Where(s => s.PartnerId == partner.ID && s.PacketId == packet.ID).FirstAsync();
        Assert.NotNull(inDb);
        Assert.Equal(customSql, inDb.ExtractSql);
    }

    [Fact]
    public async Task UpdateShareDataMapping_DoesNotTouchPartnerCustomSqlRow_Test()
    {
        // Arrange: Màn hình Ánh xạ cập nhật hồ sơ không được ghi đè hay xóa SQL riêng của đối tác
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_MAP_UPD_{unique}";
        var customSql = "SELECT ID, Code FROM TmsZone";
        var baseSql = "SELECT ID FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_MAP_{unique}", unique);
        var targetShape = "{\"now\":{\"$meta\":\"Now\"},\"serial\":{\"$meta\":\"Serial\"},\"packetCode\":{\"$meta\":\"PacketCode\"},\"partnerCode\":{\"$meta\":\"PartnerCode\"}}";
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"MAP_{unique}",
            Name = $"Mapping {unique}",
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            TargetShapeJson = targetShape
        };
        var partnerSql = new ShareDataSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            PacketId = packet.ID,
            ExtractSql = customSql
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, baseSql);
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();
        await db.Insertable(partnerSql).ExecuteCommandAsync();

        // Act: Cập nhật thông tin chung qua API Ánh xạ
        var updateInput = new ShareDataUpdateMappingInput
        {
            ID = mapping.ID,
            Code = mapping.Code,
            Name = $"Mapping Updated {unique}",
            PartnerId = mapping.PartnerId,
            DatatypeId = mapping.DatatypeId,
            Direction = mapping.Direction,
            TargetShapeJson = targetShape,
            IsActive = true
        };
        await scope.ServiceProvider.CallApiAsync<ShareDataMappingOutput>(updateInput);

        // Assert: dòng SQL riêng vẫn giữ nguyên 100%
        var mappingInDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.Equal($"Mapping Updated {unique}", mappingInDb.Name);
        var sqlInDb = await db.Queryable<ShareDataSql>().InSingleAsync(partnerSql.ID);
        Assert.NotNull(sqlInDb);
        Assert.Equal(customSql, sqlInDb.ExtractSql);
    }

    #endregion

    #region SqlVariables

    private async Task<(ShareDataPacket Packet, ShareDataPartner Partner)> SeedSqlVariablePacketAsync(ISqlSugarClient db, string unique)
    {
        var packet = NewPartnerSqlPacket($"PKT_VAR_{unique}", unique);
        var partner = NewPartnerSqlPartner($"PT_VAR_{unique}", unique);
        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        return (packet, partner);
    }

    private static PacketSqlVariableItemInput ConstVar(string code, string dataType, string value) =>
        new() { Code = code, DataType = dataType, ValueKind = ShareDataConst.SqlVariableKindConst, Value = value };

    private static PacketSqlVariableItemInput BuiltinVar(string code, string dataType, string source) =>
        new() { Code = code, DataType = dataType, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source };

    /// <summary>
    /// Description: Lưu mẫu chung rồi đọc lại: đúng thứ tự, hằng số ngày giờ được chuẩn hóa, nguồn hệ thống giữ mã nguồn
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenCommonScope_SavesAndNormalizesValues_Test()
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        var saved = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items =
            [
                ConstVar("pageSize", ShareDataConst.SqlVariableTypeInt, "500"),
                ConstVar("@fromDate", ShareDataConst.SqlVariableTypeDateTime, "2027-01-01"),
                BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, "startofyear"),
                ConstVar("rate", "decimal", "-12.50"),
                ConstVar("isActive", ShareDataConst.SqlVariableTypeBool, "1"),
                ConstVar("fromDay", ShareDataConst.SqlVariableTypeDate, "2027-03-05 08:30:00")
            ]
        });

        Assert.Equal(["pageSize", "fromDate", "yearStart", "rate", "isActive", "fromDay"], saved.Items.Select(i => i.Code).ToArray());
        Assert.All(saved.Items, i => Assert.Equal(ShareDataConst.SqlVariableScopeCommon, i.Scope));
        Assert.Equal("2027-01-01 00:00:00", saved.Items[1].Value);
        Assert.Equal("startOfYear", saved.Items[2].Value);
        Assert.Equal(ShareDataConst.SqlVariableTypeDecimal, saved.Items[3].DataType);
        Assert.Equal("-12.50", saved.Items[3].Value);
        Assert.Equal("true", saved.Items[4].Value);
        Assert.Equal("2027-03-05", saved.Items[5].Value);

        var read = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        Assert.Equal(6, read.Items.Count);
    }

    /// <summary>
    /// Description: Biến đi theo câu SQL. Tạo SQL riêng thì chép bộ biến mẫu chung sang làm bộ biến của đối tác; sửa bộ biến của đối tác
    ///              không đụng mẫu chung (và ngược lại); "Dùng lại mẫu chung" thì xóa bộ biến của đối tác.
    /// Created date: 24/10/2026
    /// Modified date: 25/10/2026 (Biến đi theo câu SQL thay cho ghi đè)
    /// </summary>
    [Fact]
    public async Task PartnerSqlVariables_FollowPartnerSql_CopiedOnCreateIndependentAndRemovedOnReset_Test()
    {
        // Arrange: mẫu chung có biến @yearStart
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceStartOfYear)]
        });

        // Act 1: tạo SQL riêng dùng @yearStart (chép từ mẫu chung)
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packet.Code!,
            ExtractSql = "SELECT ID AS zoneId FROM TmsZone WHERE CreateTime >= @yearStart"
        });

        // Assert 1: đối tác có bộ biến riêng chép từ mẫu chung
        var partnerView = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID });
        Assert.Equal(ShareDataConst.SqlVariableSourceStartOfYear, Assert.Single(partnerView.Items, i => i.Code == "yearStart").Value);
        Assert.Equal(1, await db.Queryable<ShareDataSqlVariable>().Where(v => v.PacketId == packet.ID && v.PartnerId == partner.ID).CountAsync());

        // Act 2: đổi bộ biến của đối tác sang hằng số
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            Items = [ConstVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, "2027-01-01")]
        });

        // Assert 2: mẫu chung giữ nguyên nguồn "Đầu năm"
        var common = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        Assert.Equal(ShareDataConst.SqlVariableSourceStartOfYear, Assert.Single(common.Items).Value);

        // Act 3: đối tác xóa hết biến rồi lưu lại SQL riêng không còn dùng biến
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID, Items = [] });
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packet.Code!,
            ExtractSql = "SELECT ID AS zoneId FROM TmsZone"
        });

        // Assert 3: bộ biến của đối tác vẫn rỗng — không hiện lại biến mẫu chung, không chép lại khi lưu SQL riêng
        var emptyView = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID });
        Assert.Empty(emptyView.Items);

        // Act 4: dùng lại mẫu chung
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code! });

        // Assert 4: bộ biến của đối tác bị xóa
        Assert.Equal(0, await db.Queryable<ShareDataSqlVariable>().Where(v => v.PacketId == packet.ID && v.PartnerId == partner.ID).CountAsync());
    }

    /// <summary>
    /// Description: Xóa dòng ở mẫu chung rồi khai lại cùng tên không bị chặn bởi chỉ mục duy nhất
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenRemovedThenRedeclared_SucceedsWithoutUniqueConflict_Test()
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        var input = new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [ConstVar("pageSize", ShareDataConst.SqlVariableTypeInt, "500")] };
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(input);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [] });
        var again = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(input);

        Assert.Single(again.Items);
    }

    /// <summary>
    /// Description: Các dòng không hợp lệ đều bị từ chối: tên sai, trùng tên, nguồn hệ thống sai kiểu, hằng số sai kiểu, nguồn lạ
    /// Created date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("1abc", "Int", "Const", "5")]
    [InlineData("from date", "DateTime", "Const", "2027-01-01")]
    [InlineData("lastKey", "DateTime", "Builtin", "lastKey")]
    [InlineData("pageSize", "Int", "Const", "abc")]
    [InlineData("fromDate", "DateTime", "Const", "01/02/2027")]
    [InlineData("yearStart", "DateTime", "Builtin", "tenant")]
    [InlineData("pageSize", "Guid", "Const", "5")]
    [InlineData("rate", "Decimal", "Const", "1,5")]
    [InlineData("isActive", "Bool", "Const", "yes")]
    [InlineData("fromDay", "Date", "Const", "2027-13-01")]
    public async Task UpdateSqlVariables_WhenItemInvalid_ThrowsAndSavesNothing_Test(string code, string dataType, string valueKind, string value)
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [new PacketSqlVariableItemInput { Code = code, DataType = dataType, ValueKind = valueKind, Value = value }]
        }));

        Assert.Equal(0, await db.Queryable<ShareDataSqlVariable>().Where(v => v.PacketId == packet.ID).CountAsync());
    }

    /// <summary>
    /// Description: Hai dòng trùng tên (không phân biệt hoa thường) bị từ chối
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenDuplicateCodes_Throws_Test()
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [ConstVar("pageSize", "Int", "5"), ConstVar("PAGESIZE", "Int", "6")]
        }));
    }

    /// <summary>
    /// Description: Không phải SuperAdmin thì không xem và không lưu được bảng tham số
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task SqlVariables_WhenNotSuperAdmin_Throws_Test()
    {
        SetUserContext(isSuperAdmin: false, account: $"user_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! }));
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code! }));
    }

    /// <summary>
    /// Description: Lưu câu SQL (mẫu chung hoặc SQL riêng của đối tác) có dùng biến @yearStart: chỉ lưu được khi biến đã khai ở phạm vi mà câu SQL nhìn thấy.
    ///              Mẫu chung chỉ thấy biến của mẫu chung; SQL riêng thấy biến của mẫu chung và biến riêng của đối tác đó.
    /// Created date: 25/10/2026
    /// </summary>
    [Theory]
    [InlineData("common", "common", true)]
    [InlineData("common", "partner", false)]
    [InlineData("common", "none", false)]
    [InlineData("partner", "common", true)]
    [InlineData("partner", "partner", true)]
    [InlineData("partner", "none", false)]
    public async Task SaveExtractSql_WhenSqlUsesVariable_AcceptsOnlyDeclaredVariables_Test(string sqlScope, string declaredAt, bool expectSaved)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        if (declaredAt != "none")
        {
            await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
            {
                PacketCode = packet.Code!,
                PartnerId = declaredAt == "partner" ? partner.ID : null,
                Items = [BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceStartOfYear)]
            });
        }
        const string sql = "SELECT ID AS zoneId FROM TmsZone WHERE CreateTime >= @yearStart";

        // Act
        var error = await Record.ExceptionAsync(() => sqlScope == "common"
            ? scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
                new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql })
            : scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(
                new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql }));

        // Assert
        var partnerId = partner.ID;
        var savedSql = await db.Queryable<ShareDataSql>()
            .Where(s => s.PacketId == packet.ID)
            .WhereIF(sqlScope == "common", s => s.PartnerId == null)
            .WhereIF(sqlScope != "common", s => s.PartnerId == partnerId)
            .Select(s => s.ExtractSql)
            .FirstAsync();
        if (expectSaved)
        {
            Assert.Null(error);
            Assert.Equal(sql, savedSql);
        }
        else
        {
            Assert.NotNull(error);
            Assert.Null(savedSql);
        }
    }

    /// <summary>
    /// Description: Lưu câu SQL nối đuôi: ORDER BY ngoài cùng phải theo __lastTime rồi __lastKey (viết bằng bí danh hoặc đúng biểu thức trong SELECT);
    ///              sắp theo cột khác thì dòng cuối trang không phải mốc lớn nhất, Worker sẽ bỏ sót dòng, nên bị từ chối.
    /// Created date: 25/10/2026
    /// </summary>
    [Theory]
    [InlineData("CreateTime, ID", true)]
    [InlineData("__lastTime, __lastKey", true)]
    [InlineData("ID", false)]
    [InlineData("ID, CreateTime", false)]
    public async Task SaveExtractSql_WhenIncrementalOrderBy_RequiresLastTimeThenLastKey_Test(string orderBy, bool expectSaved)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        var sql = "SELECT TOP (@pageSize) CreateTime AS __lastTime, ID AS __lastKey FROM TmsZone "
            + "WHERE @lastTime IS NULL OR CreateTime > @lastTime OR (CreateTime = @lastTime AND ID > @lastKey) "
            + $"ORDER BY {orderBy}";
        await ShareDataSqlSeed.DeclareStandardVariablesAsync(db, packet.ID, null, sql);

        // Act
        var error = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
            new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql }));

        // Assert
        if (expectSaved)
        {
            Assert.Null(error);
            Assert.Equal(sql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
        }
        else
        {
            Assert.NotNull(error);
        }
    }

    #endregion
}
