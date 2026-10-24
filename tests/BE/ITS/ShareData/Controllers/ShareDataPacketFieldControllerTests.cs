using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Module.ShareData.Core.Dto.PacketField;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Enums;
using Tests.ShareData.Common;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử tích hợp cho ShareDataPacketFieldController qua Wolverine IMessageBus (Rule 19.84).
///              Kiểm tra API trích xuất cột SQL của gói tin, thêm trường, thêm hàng loạt và sửa trường.
/// Created date: 21/10/2026
/// Modified date: 24/10/2026
/// </summary>
[Collection("api")]
public class ShareDataPacketFieldControllerTests(Host host) : IAsyncLifetime
{
    private readonly Host _host = host;
    private readonly IHttpContextAccessor _httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();

    public async Task InitializeAsync()
    {
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
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
    public async Task GetExtractColumns_WhenPacketHasSqlAndExistingField_ReturnsColumnsWithoutSystemColumnsAndCorrectHasField_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_COL_{unique}",
            Name = $"Packet Col {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID AS id, Code AS code, Name AS name, CreateTime AS __lastTime FROM TmsZone");

        var existingField = new ShareDataPacketField
        {
            ID = Guid.NewGuid().ToString("N"),
            DatatypeId = packet.ID,
            AliasFieldKey = "code",
            Name = "Mã đoạn đường",
            Type = "string",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(existingField).ExecuteCommandAsync();

        var input = new GetPacketExtractColumnsInput
        {
            DatatypeId = packet.ID
        };

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketSqlColumnListOutput>(input);

        // Assert
        Assert.NotNull(output);
        Assert.True(output.SqlAvailable);
        Assert.Equal(3, output.Columns.Count);
        Assert.DoesNotContain(output.Columns, c => c.FieldKey.StartsWith("__", StringComparison.Ordinal));

        var colId = output.Columns.FirstOrDefault(c => c.FieldKey == "id");
        Assert.NotNull(colId);
        Assert.False(colId.HasField);

        var colCode = output.Columns.FirstOrDefault(c => c.FieldKey == "code");
        Assert.NotNull(colCode);
        Assert.True(colCode.HasField);

        var colName = output.Columns.FirstOrDefault(c => c.FieldKey == "name");
        Assert.NotNull(colName);
        Assert.False(colName.HasField);
    }

    [Fact]
    public async Task GetExtractColumns_WhenPacketHasNoExtractSql_ReturnsEmptyAndSqlAvailableFalse_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_NOSQL_{unique}",
            Name = $"Packet NoSql {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, null);

        var input = new GetPacketExtractColumnsInput
        {
            DatatypeId = packet.ID
        };

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketSqlColumnListOutput>(input);

        // Assert
        Assert.NotNull(output);
        Assert.False(output.SqlAvailable);
        Assert.Empty(output.Columns);
    }

    [Fact]
    public async Task GetExtractColumns_WhenNotSuperAdmin_CanExecuteSuccessfully_Test()
    {
        // Arrange: Người dùng thường (isSuperAdmin = false) tra cứu theo Code gói
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_NORM_{unique}";
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Normal {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID AS zoneId FROM TmsZone");

        var input = new GetPacketExtractColumnsInput
        {
            DatatypeId = packetCode
        };

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketSqlColumnListOutput>(input);

        // Assert
        Assert.NotNull(output);
        Assert.True(output.SqlAvailable);
        Assert.Single(output.Columns);
        Assert.Equal("zoneId", output.Columns[0].FieldKey);
    }

    [Fact]
    public async Task AddPacketField_WhenFieldKeyInSql_SucceedsWithNormalizedCasing_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_ADD_{unique}",
            Name = $"Packet Add {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID AS zoneId, Name AS zoneName FROM TmsZone");

        var input = new ShareDataAddPacketFieldInput
        {
            DatatypeId = packet.ID,
            AliasFieldKey = "ZONEID", // Gõ hoa nhưng SQL là zoneId
            Name = "Mã đoạn đường",
            Type = "string",
            Status = BaseEnums.StatusEnum.Enable
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(input);

        // Assert: Lưu đúng cách viết hoa/thường của bí danh trong SQL
        var saved = await db.Queryable<ShareDataPacketField>()
            .Where(x => x.DatatypeId == packet.ID && x.IsDelete == null)
            .FirstAsync();
        Assert.NotNull(saved);
        Assert.Equal("zoneId", saved.AliasFieldKey);
    }

    [Fact]
    public async Task AddPacketField_WhenFieldKeyNotInSql_ThrowsOops_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_NOTIN_{unique}",
            Name = $"Packet NotIn {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID AS zoneId FROM TmsZone");

        var input = new ShareDataAddPacketFieldInput
        {
            DatatypeId = packet.ID,
            AliasFieldKey = "nonExistentKey",
            Name = "Khóa không có trong SQL",
            Type = "string",
            Status = BaseEnums.StatusEnum.Enable
        };

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync(input));
        Assert.Contains(BaseMsg.ShareData.Validation.FieldKeyNotInExtractSql, ex.ToString());
    }

    [Fact]
    public async Task AddPacketField_WhenPacketHasNoSql_ThrowsOops_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_NOSQL2_{unique}",
            Name = $"Packet NoSql2 {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, null);

        var input = new ShareDataAddPacketFieldInput
        {
            DatatypeId = packet.ID,
            AliasFieldKey = "anyKey",
            Name = "Bất kỳ",
            Type = "string",
            Status = BaseEnums.StatusEnum.Enable
        };

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync(input));
        Assert.Contains(BaseMsg.ShareData.Validation.PacketExtractSqlMissingForField, ex.ToString());
    }

    [Fact]
    public async Task BatchAdd_WhenThreeKeysAndOneExists_AddsTwoAndSkipsOneWithoutOverwriting_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_BATCH_{unique}",
            Name = $"Packet Batch {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID AS col1, Code AS col2, Name AS col3 FROM TmsZone");

        // Đã có sẵn col1 với thuộc tính ban đầu
        var preExistingField = new ShareDataPacketField
        {
            ID = Guid.NewGuid().ToString("N"),
            DatatypeId = packet.ID,
            AliasFieldKey = "col1",
            Name = "Tên Gốc Ban Đầu",
            Type = "customType",
            Remark = "Ghi chú gốc",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(preExistingField).ExecuteCommandAsync();

        var input = new ShareDataBatchAddPacketFieldInput
        {
            DatatypeId = packet.ID,
            Fields =
            [
                new ShareDataAddPacketFieldInput
                {
                    AliasFieldKey = "COL1", // Trùng khóa đã có
                    Name = "Tên Mới Muốn Ghi Đè",
                    Type = "newType",
                    Remark = "Ghi chú mới"
                },
                new ShareDataAddPacketFieldInput
                {
                    AliasFieldKey = "col2",
                    Name = "Cột 2",
                    Type = "string"
                },
                new ShareDataAddPacketFieldInput
                {
                    AliasFieldKey = "col3",
                    Name = "Cột 3",
                    Type = "string"
                }
            ]
        };

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<ShareDataBatchAddPacketFieldOutput>(input);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(2, output.AddedCount);
        Assert.Single(output.SkippedKeys);
        Assert.Contains("col1", output.SkippedKeys);

        // Trường đã có không bị đổi Name, Type, Remark
        var reloadedCol1 = await db.Queryable<ShareDataPacketField>()
            .Where(x => x.ID == preExistingField.ID)
            .FirstAsync();
        Assert.NotNull(reloadedCol1);
        Assert.Equal("Tên Gốc Ban Đầu", reloadedCol1.Name);
        Assert.Equal("customType", reloadedCol1.Type);
        Assert.Equal("Ghi chú gốc", reloadedCol1.Remark);

        // 2 trường mới đã được tạo thành công
        var allFields = await db.Queryable<ShareDataPacketField>()
            .Where(x => x.DatatypeId == packet.ID && x.IsDelete == null)
            .ToListAsync();
        Assert.Equal(3, allFields.Count);
        Assert.Contains(allFields, f => f.AliasFieldKey == "col2");
        Assert.Contains(allFields, f => f.AliasFieldKey == "col3");
    }

    [Fact]
    public async Task UpdatePacketField_WhenModifyingAliasFieldKey_ThrowsOops_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_LOCKKEY_{unique}",
            Name = $"Packet LockKey {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID AS origKey, Name AS newKey FROM TmsZone");

        var field = new ShareDataPacketField
        {
            ID = Guid.NewGuid().ToString("N"),
            DatatypeId = packet.ID,
            AliasFieldKey = "origKey",
            Name = "Tên trường",
            Type = "string",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(field).ExecuteCommandAsync();

        var updateInput = new ShareDataUpdatePacketFieldInput
        {
            ID = field.ID,
            DatatypeId = packet.ID,
            AliasFieldKey = "newKey", // Cố ý đổi AliasFieldKey
            Name = "Tên cập nhật",
            Type = "string"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync(updateInput));
        Assert.Contains(BaseMsg.ShareData.Validation.FieldKeyCannotBeModified, ex.ToString());
    }

    [Fact]
    public async Task UpdatePacketField_WhenModifyingNameAndPacketNotInUse_Succeeds_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_UPDNAME_{unique}",
            Name = $"Packet UpdName {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID AS zoneId FROM TmsZone");

        var field = new ShareDataPacketField
        {
            ID = Guid.NewGuid().ToString("N"),
            DatatypeId = packet.ID,
            AliasFieldKey = "zoneId",
            Name = "Tên Cũ",
            Type = "string",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(field).ExecuteCommandAsync();

        var updateInput = new ShareDataUpdatePacketFieldInput
        {
            ID = field.ID,
            DatatypeId = packet.ID,
            AliasFieldKey = "zoneId", // Giữ nguyên khóa
            Name = "Tên Mới Đã Sửa",
            Type = "string",
            Status = BaseEnums.StatusEnum.Enable
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(updateInput);

        // Assert
        var updated = await db.Queryable<ShareDataPacketField>()
            .Where(x => x.ID == field.ID)
            .FirstAsync();
        Assert.NotNull(updated);
        Assert.Equal("Tên Mới Đã Sửa", updated.Name);
        Assert.Equal("zoneId", updated.AliasFieldKey);
    }
}
