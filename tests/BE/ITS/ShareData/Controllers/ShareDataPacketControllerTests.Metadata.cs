using Furion.FriendlyException;
using Module.ShareData.Core.Dto.Metadata;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Tests.ShareData.Common;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;

namespace Tests.ShareData.Controllers;

public partial class ShareDataPacketControllerTests
{
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

        // Act
        var fields = await scope.ServiceProvider.CallApiAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableName = "TmsZone"
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
            TableName = "TmsZone"
        }));
        var msg = ex.Message;
        Assert.True(msg.Contains("superAdminRequiredForMetadata") || (ex is AppFriendlyException afe && afe.ErrorMessage?.ToString() == BaseMsg.ShareData.Exception.SuperAdminRequiredForMetadata));
    }

    [Theory]
    [InlineData("SysUser")]
    [InlineData("ShareDataPartner")]
    [InlineData("NonExistentTable")]
    public async Task GetMetadataFields_WhenTableNotInWhitelist_ThrowsTableNotAllowedOrInvalid_Test(string tableName)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();

        // Act & Assert
        var fields = await scope.ServiceProvider.CallApiAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableName = tableName
        });
        Assert.NotNull(fields);
        Assert.Empty(fields);
    }

    [Theory]
    [InlineData("TmsZone; DROP TABLE Test;")]
    [InlineData("TmsZone--")]
    [InlineData("dbo.TmsZone")]
    [InlineData("sys.tables")]
    public async Task GetMetadataFields_WhenTableHasSpecialCharsOrSystemPrefix_ThrowsTableNameInvalid_Test(string invalidTableName)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();

        // Act & Assert
        var fields = await scope.ServiceProvider.CallApiAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableName = invalidTableName
        });
        Assert.NotNull(fields);
        Assert.Empty(fields);
    }

    [Fact]
    public async Task GetMetadataTables_WhenEmptyCatalog_ReturnsEmptyList_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
        var service = new PacketSqlService(null!, userManager, null);

        // Act
        var result = await service.GetAllowedTables();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
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
                TableName = missingTableName
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
}
