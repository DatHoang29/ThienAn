using Furion.FriendlyException;
using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.Metadata;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Tests.ShareData.Common;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using SqlSugar;
using Wolverine;
using Xunit;

namespace Tests.ShareData.Controllers;

public partial class ShareDataPacketControllerTests
{
    [Fact]
    public async Task GetMetadataTables_WhenSuperAdmin_ReturnsAllAllowedTables_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        // Act: Đi qua đúng điểm vào IMessageBus (Rule 19.84)
        var tables = await bus.InvokeAsync<List<MetadataTableDto>>(new GetMetadataTablesInput());

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
    }

    [Fact]
    public async Task GetMetadataFields_WhenSuperAdminAndWhitelistedTable_ReturnsColumnsWithSuggestedAliases_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"superadmin_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act
        var fields = await bus.InvokeAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableName = "TmsZone"
        });

        // Assert
        Assert.NotNull(fields);
        Assert.NotEmpty(fields);
        Assert.Contains(fields, f => f.ColumnName == "ID" && f.SuggestedAlias == "zoneId");
        Assert.Contains(fields, f => f.ColumnName == "Name" && f.SuggestedAlias == "zoneName");
    }

    [Fact]
    public async Task GetMetadataTables_WhenNotSuperAdmin_ThrowsSuperAdminRequiredException_Test()
    {
        // Arrange: Người dùng không phải SuperAdmin
        SetUserContext(isSuperAdmin: false, account: $"operator_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => bus.InvokeAsync<List<MetadataTableDto>>(new GetMetadataTablesInput()));
        var msg = ex.Message;
        Assert.True(msg.Contains("superAdminRequiredForMetadata") || (ex is AppFriendlyException afe && afe.ErrorMessage?.ToString() == BaseMsg.ShareData.Exception.SuperAdminRequiredForMetadata));
    }

    [Fact]
    public async Task GetMetadataFields_WhenNotSuperAdmin_ThrowsSuperAdminRequiredException_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false, account: $"operator_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => bus.InvokeAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
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
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => bus.InvokeAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableName = tableName
        }));
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
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => bus.InvokeAsync<List<MetadataFieldDto>>(new GetMetadataFieldsInput
        {
            TableName = invalidTableName
        }));
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
}
