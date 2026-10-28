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
using Shared.DTO.Constants.Its015;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Xunit;
using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Constants;
using Modules.TMS.Core.Entities;

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

    /// <summary>Câu MERGE hợp lệ ghi vào bảng ShareDataInboundPacket của CSDL nhận test (khoá id).</summary>
    private const string ValidWriteSql = """
        MERGE dbo.ShareDataInboundPacket WITH (HOLDLOCK) AS t
        USING (
            SELECT id, MAX(code) AS code
            FROM OPENJSON(@records) WITH (id nvarchar(64), code nvarchar(64))
            WHERE id IS NOT NULL
            GROUP BY id
        ) AS s
           ON t.ID = s.id
        WHEN MATCHED THEN
            UPDATE SET t.PacketCode = s.code
        WHEN NOT MATCHED THEN
            INSERT (ID, PartnerCode, PacketCode) VALUES (s.id, 'TRY_WRITE', s.code)
        OUTPUT $action;
        """;

    private const string SecondTableWriteSql = """
        MERGE dbo.TmsZone WITH (HOLDLOCK) AS t
        USING (
            SELECT zoneId, MAX(zoneName) AS zoneName
            FROM OPENJSON(@records) WITH (zoneId nvarchar(64), zoneName nvarchar(255))
            WHERE zoneId IS NOT NULL
            GROUP BY zoneId
        ) AS s
           ON t.ID = s.zoneId
        WHEN MATCHED THEN
            UPDATE SET t.Name = s.zoneName
        WHEN NOT MATCHED THEN
            INSERT (ID, Name) VALUES (s.zoneId, s.zoneName)
        OUTPUT $action;
        """;

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
    /// Description: Khi tạo gói tin mới có OrderNo khớp gói có sẵn (101, 103, 111), chép câu SQL mẫu chung và cột mốc từ ShareDataSql của gói nguồn
    /// Created date: 17/10/2026
    /// Modified date: 27/10/2026 (bỏ TemplateSql, nguồn duy nhất là ShareDataSql)
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
            CreateTime = DateTime.UtcNow.AddYears(-10)
        }).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, seededPacketId, tpl.ExtractSql, tpl.CursorTimeColumn);

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
        var commonSql = await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        Assert.Equal(tpl.ExtractSql, commonSql?.ExtractSql);
        if (tpl.CursorTimeColumn != null)
        {
            Assert.Equal(tpl.CursorTimeColumn, commonSql?.CursorTimeColumn);
        }
    }

    [Fact]
    public async Task AddPacket_WhenNoSourcePacketExists_LeavesCommonSqlEmpty_Test()
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

        // Assert: Không có gói cùng loại có câu SQL -> gói mới chưa có câu SQL mẫu chung
        var packet = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.Code == code);
        Assert.NotNull(packet);
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
    public async Task UpdatePacket_WhenClientSendsDifferentOrderNo_PreservesOrderNoAndCommonSql_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var code = $"101_upd_lock_{unique}";
        var originalExtract = "SELECT 'ORIGINAL_EXTRACT' AS val";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = code,
            Name = $"Packet Update Lock {unique}",
            OrderNo = 101,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.UtcNow.AddYears(-10)
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, originalExtract);

        var updateInput = new ShareDataUpdatePacketInput
        {
            ID = packet.ID,
            Code = code,
            Name = $"Packet Update Lock Modified {unique}",
            OrderNo = 108
        };

        // Act
        await scope.ServiceProvider.CallApiAsync(updateInput);

        // Assert: OrderNo và câu SQL mẫu chung không bị thay đổi bởi UpdatePacket
        var inDb = await db.Queryable<ShareDataPacket>().FirstAsync(x => x.ID == packet.ID);
        Assert.NotNull(inDb);
        Assert.Equal($"Packet Update Lock Modified {unique}", inDb.Name);
        Assert.Equal(101, inDb.OrderNo);
        Assert.Equal(originalExtract, (await ShareDataSqlSeed.GetCommonAsync(db, inDb.ID))?.ExtractSql);
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
        var (isValid, errorMsg) = SqlValidatorHelper.Validate(unsafeSql);

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

    /// <summary>
    /// Description: Tên cột là khoá trường của gói tin (FieldsJson) nên câu SQL trả trùng tên cột bị từ chối khi chạy thử.
    /// Modified date: 26/10/2026 (trước đây tự thêm hậu tố _2, _3)
    /// Modified date: 27/10/2026 (gom mọi tên trùng, mỗi tên báo một lần)
    /// </summary>
    [Theory]
    [InlineData("SELECT 1 AS TestCol, 2 AS TestCol, 3 AS TestCol", new[] { "TestCol" })]
    [InlineData("SELECT 1 AS avgSpeed, 2 AS occupancy, 3 AS avgSpeed, 4 AS occupancy, 5 AS avgSpeed, 6 AS occupancy", new[] { "avgSpeed", "occupancy" })]
    public async Task ExecutePacketSqlSample_WithDuplicateColumns_ReturnsDuplicateColumnError_Test(string sql, string[] duplicates)
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = sql,
            Top = 5
        };

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(input);

        // Assert
        Assert.False(result.IsSuccess);
        foreach (var name in duplicates)
        {
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(result.ErrorMessage ?? string.Empty, $@"\b{name}\b"));
        }
        Assert.Empty(result.Rows);
    }

    [Fact]
    public async Task ExecutePacketSqlSample_WithTopLimitInSql_ExecutesSuccessfully_Test()
    {
        // Arrange: giới hạn dòng viết thẳng trong câu SQL (TOP 100), không cần khai biến
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_PS_{unique}";
        var packetId = Guid.NewGuid().ToString("N");
        var sql = "SELECT TOP 100 ID, Code FROM TmsZone";
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
    [InlineData("SELECT TOP 100 ID, Code, CreateTime AS __lastTime FROM TmsZone WHERE CreateTime > @lastTime ORDER BY CreateTime ASC")]
    [InlineData("SELECT TOP 100 ID AS __rowid, Code, CreateTime AS __lastTime FROM TmsZone WHERE @lastTime IS NULL OR CreateTime > @lastTime ORDER BY CreateTime ASC")]
    [InlineData("SELECT TOP 100 ID AS __lastKey, Code, CreateTime AS __lastTime FROM TmsZone WHERE @lastTime IS NULL OR CreateTime > @lastTime ORDER BY __lastTime DESC")]
    [InlineData("SELECT TOP 100 ID, Code FROM TmsZone WHERE CreateTime > @lastTime")]
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

    [Theory]
    [InlineData("SELECT * FROM OtherDb.dbo.TmsZone")]
    [InlineData("SELECT ConnectionConfig FROM ShareDataPartner")]
    [InlineData("SELECT * FROM SysOrg")]
    public void Validate_When3PartTableNameOrSysTableOrPartnerTable_FailsValidation_Test(string sql)
    {
        // RV-7: Chặn CSDL khác và bảng chứa bí mật
        var (isValid, errorMsg) = SqlValidatorHelper.Validate(sql);
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
        await ShareDataSqlSeed.DeclareStandardVariablesAsync(db, packetId, null, "SELECT TOP 100 ID, Code FROM TmsZone");

        // Act: SQL dạng Snapshot bình thường
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            ExtractSql = "SELECT TOP 100 ID, Code FROM TmsZone"
        });

        // Assert: Lưu thành công
        var output = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput { PacketCode = packetCode });
        Assert.NotNull(output);
        Assert.Contains("SELECT TOP 100 ID, Code FROM TmsZone", output.ExtractSql);
    }

    /// <summary>
    /// Description: Biến phân trang @pageSize mang nguồn 'now' (kiểu DateTime) bị Backend từ chối ngay khi lưu bộ biến
    ///              vì câu lệnh TOP trong SQL Server bắt buộc nhận số nguyên cố định.
    /// Created date: 28/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenPageSizeUsesDateTimeVariable_Throws_Test()
    {
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_TOP_{unique}";
        var packetId = Guid.NewGuid().ToString("N");

        await db.Insertable(new ShareDataPacket
        {
            ID = packetId,
            Code = packetCode,
            Name = $"Packet Top {unique}",
            Status = BaseEnums.StatusEnum.Enable
        }).ExecuteCommandAsync();

        // Lưu bộ biến: @pageSize có nguồn 'now' (DataType = DateTime) bị Backend từ chối ngay
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packetCode,
            Items =
            [
                new PacketSqlVariableItemInput
                {
                    Code = "pageSize",
                    DataType = ShareDataConst.SqlVariableTypeDateTime,
                    ValueKind = ShareDataConst.SqlVariableKindBuiltin,
                    Value = ShareDataConst.SqlVariableSourceNow
                }
            ]
        }));

        Assert.NotNull(ex);
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
            Id = zone.Id
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
            Id = "any-table-id"
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
            Id = invalidTableId
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
                Id = testId
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

        // Cột Đối tác: số đối tác và tên hiển thị phải có cho gói đăng ký theo ID, theo mã gói và gói chỉ có SQL riêng
        foreach (var packet in new[] { shared, custom, customOnly, byCode })
        {
            Assert.Equal(1, rows[packet.Code!].PartnerCount);
            Assert.Equal([partner.Code], rows[packet.Code!].PartnerNames);
        }
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
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT TOP 100 ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act & Assert: SQL có @lastTime nhưng thiếu __lastKey, __lastTime, ORDER BY -> Throws
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "SELECT TOP 100 ID, Code FROM TmsZone WHERE CreateTime > @lastTime"
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
        var customSql = "SELECT TOP 100 ID AS __lastKey, CreateTime AS __lastTime, Code FROM TmsZone WHERE CreateTime >= @lastTime ORDER BY CreateTime ASC, ID ASC";
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
        Assert.True(SqlValidatorHelper.IsIncrementalSql(output.CustomSql, cursorNames));
        Assert.False(SqlValidatorHelper.IsIncrementalSql(output.StandardSql, cursorNames));

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

    private static PacketSqlVariableItemInput BuiltinVar(string code, string dataType, string source) =>
        new() { Code = code, DataType = dataType, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source };

    private static PacketSqlVariableItemInput ConstVar(string code, string dataType, string value) =>
        new() { Code = code, DataType = dataType, ValueKind = ShareDataConst.SqlVariableKindConst, Value = value };

    /// <summary>
    /// Description: Lưu mẫu chung rồi đọc lại: đúng thứ tự, tên bỏ @, mã nguồn chuẩn hóa hoa thường, kiểu dữ liệu suy từ nguồn
    ///              (bỏ qua kiểu gửi lên, kể cả để trống hoặc sai), và hỗ trợ hằng số "Giá trị nhập" (String, Int, Decimal).
    /// Created date: 24/10/2026
    /// Modified date: 26/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenCommonScope_SavesSourcesAndDerivesType_Test()
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
                BuiltinVar("@since", "", "LASTTIME"),
                BuiltinVar("afterKey", ShareDataConst.SqlVariableTypeDateTime, "lastkey"),
                BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, "startofyear"),
                BuiltinVar("pc", "", "partnercode"),
                ConstVar("@pageSize", "int", " 500 "),
                ConstVar("minRate", "decimal", "1.25"),
                ConstVar("tag", "string", "ACTIVE")
            ]
        });

        Assert.Equal(["since", "afterKey", "yearStart", "pc", "pageSize", "minRate", "tag"], saved.Items.Select(i => i.Code).ToArray());
        Assert.All(saved.Items, i => Assert.Equal(ShareDataConst.SqlVariableScopeCommon, i.Scope));
        Assert.Equal(
            [
                ShareDataConst.SqlVariableKindBuiltin, ShareDataConst.SqlVariableKindBuiltin,
                ShareDataConst.SqlVariableKindBuiltin, ShareDataConst.SqlVariableKindBuiltin,
                ShareDataConst.SqlVariableKindConst, ShareDataConst.SqlVariableKindConst, ShareDataConst.SqlVariableKindConst
            ],
            saved.Items.Select(i => i.ValueKind).ToArray());
        Assert.Equal(
            [
                ShareDataConst.SqlVariableSourceLastTime, ShareDataConst.SqlVariableSourceLastKey,
                ShareDataConst.SqlVariableSourceStartOfYear, ShareDataConst.SqlVariableSourcePartnerCode,
                "500", "1.25", "ACTIVE"
            ],
            saved.Items.Select(i => i.Value ?? string.Empty).ToArray());
        Assert.Equal(
            [
                ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableTypeString,
                ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableTypeString,
                ShareDataConst.SqlVariableTypeInt, ShareDataConst.SqlVariableTypeDecimal, ShareDataConst.SqlVariableTypeString
            ],
            saved.Items.Select(i => i.DataType).ToArray());

        var read = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        Assert.Equal(7, read.Items.Count);
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
        Assert.Single(await ShareDataSqlSeed.GetVariablesAsync(db, packet.ID, partner.ID));

        // Act 2: đổi biến của đối tác sang nguồn khác (Thời điểm chạy)
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            Items = [BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceNow)]
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
        Assert.Empty(await ShareDataSqlSeed.GetVariablesAsync(db, packet.ID, partner.ID));
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

        var input = new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [BuiltinVar("since", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceLastTime)] };
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(input);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [] });
        var again = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(input);

        Assert.Single(again.Items);
    }

    /// <summary>
    /// Description: Các dòng không hợp lệ đều bị từ chối: tên sai, nguồn lạ hoặc để trống, cách gán khác nguồn hệ thống (hằng số đã bỏ)
    /// Created date: 24/10/2026
    /// Modified date: 28/10/2026
    /// </summary>
    [Theory]
    [InlineData("1abc", "DateTime", "Builtin", "lastTime")]
    [InlineData("from date", "DateTime", "Builtin", "now")]
    [InlineData("yearStart", "DateTime", "Builtin", "tenant")]
    [InlineData("runAt", "DateTime", "Builtin", "")]
    [InlineData("pageSize", "Int", "Const", "abc")]
    [InlineData("pageSize", "Int", "Const", "0")]
    [InlineData("pageSize", "DateTime", "Builtin", "now")]
    [InlineData("rate", "Decimal", "Const", "not-a-decimal")]
    [InlineData("tag", "String", "Const", "   ")]
    [InlineData("dt", "DateTime", "Const", "2026-01-01")]
    [InlineData("prevRun", "DateTime", "Manual", "lastRunTime")]
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

        Assert.Empty(await ShareDataSqlSeed.GetVariablesAsync(db, packet.ID, null));
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
            Items = [BuiltinVar("since", "DateTime", "lastTime"), BuiltinVar("SINCE", "DateTime", "now")]
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
        var sql = "SELECT TOP 100 CreateTime AS __lastTime, ID AS __lastKey FROM TmsZone "
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

    /// <summary>
    /// Description: Truy vấn nối đuôi gộp 1 cột (@lastKey đơn lẻ, không khai @lastTime) ở cả mẫu chung và SQL riêng đối tác:
    ///              chấp nhận AS __lastKey kèm ORDER BY bí danh hoặc đúng biểu thức gộp ASC và loại __lastKey khỏi Fields;
    ///              từ chối khi thiếu AS __lastKey, thiếu ORDER BY, ORDER BY DESC hoặc ORDER BY khác biểu thức __lastKey.
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData("common", "alias-asc", true)]
    [InlineData("common", "expr-asc", true)]
    [InlineData("partner", "alias-asc", true)]
    [InlineData("common", "missing-alias", false)]
    [InlineData("common", "missing-orderby", false)]
    [InlineData("common", "orderby-desc", false)]
    [InlineData("partner", "orderby-other-col", false)]
    public async Task SaveExtractSql_WhenCompositeLastKeyWithoutLastTime_EnforcesSingleCursorContract_Test(
        string sqlScope,
        string variant,
        bool expectSaved)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("lastKey", ShareDataConst.SqlVariableTypeString, ShareDataConst.SqlVariableSourceLastKey)]
        });

        const string compositeExpr = "CONVERT(varchar(23), CreateTime, 121) + '_' + ID";
        var sql = variant switch
        {
            "alias-asc" => $"SELECT TOP 50 ID AS zoneId, Name AS zoneName, {compositeExpr} AS __lastKey FROM TmsZone WHERE ({compositeExpr}) > @lastKey ORDER BY __lastKey ASC",
            "expr-asc" => $"SELECT TOP 50 ID AS zoneId, Name AS zoneName, {compositeExpr} AS __lastKey FROM TmsZone WHERE ({compositeExpr}) > @lastKey ORDER BY {compositeExpr}",
            "missing-alias" => $"SELECT TOP 50 ID AS zoneId, Name AS zoneName FROM TmsZone WHERE ({compositeExpr}) > @lastKey ORDER BY {compositeExpr}",
            "missing-orderby" => $"SELECT TOP 50 ID AS zoneId, {compositeExpr} AS __lastKey FROM TmsZone WHERE ({compositeExpr}) > @lastKey",
            "orderby-desc" => $"SELECT TOP 50 ID AS zoneId, {compositeExpr} AS __lastKey FROM TmsZone WHERE ({compositeExpr}) > @lastKey ORDER BY __lastKey DESC",
            _ => $"SELECT TOP 50 ID AS zoneId, {compositeExpr} AS __lastKey FROM TmsZone WHERE ({compositeExpr}) > @lastKey ORDER BY CreateTime ASC"
        };

        // Act
        var error = await Record.ExceptionAsync(() => sqlScope == "common"
            ? scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
                new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql })
            : scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(
                new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql }));

        // Assert
        var savedRow = sqlScope == "common"
            ? await ShareDataSqlSeed.GetCommonAsync(db, packet.ID)
            : await SqlTextHelper.SelectPartnerSql(db, partner.ID, packet);
        if (expectSaved)
        {
            Assert.Null(error);
            Assert.Equal(sql, savedRow?.ExtractSql);
            var fieldKeys = SqlFieldHelper.Read(savedRow?.FieldsJson).Select(f => f.Key).ToArray();
            Assert.Equal(["zoneId", "zoneName"], fieldKeys);
        }
        else
        {
            Assert.NotNull(error);
            Assert.Null(savedRow?.ExtractSql);
        }
    }

    /// <summary>
    /// Description: Khóa nối đuôi gộp 1 cột được so như chuỗi, nên định dạng phải cố định độ dài để thứ tự chuỗi trùng thứ tự thời gian:
    ///              lưu vẫn thành công nhưng trả cảnh báo khi ngày giờ thiếu năm/tháng/ngày hoặc dùng kiểu CONVERT không sắp được,
    ///              và khi ép số sang chuỗi không đệm số 0 (…_9 > …_10). Định dạng chuẩn (CONVERT 121 + mã chuỗi, hoặc số có RIGHT đệm 0) không cảnh báo.
    /// Created date: 28/10/2026
    /// </summary>
    [Theory]
    [InlineData("CONVERT(varchar(23), CreateTime, 121) + '_' + ID", "")]
    [InlineData("CONVERT(varchar(23), CreateTime, 121) + '_' + RIGHT('0000000000' + CAST(FromKmNumber AS varchar(10)), 10)", "")]
    [InlineData("FORMAT(CreateTime, 'HHmm') + '_' + ID", "date")]
    [InlineData("CONVERT(varchar(8), CreateTime, 108) + '_' + ID", "date")]
    [InlineData("CONVERT(varchar(23), CreateTime, 121) + '_' + CAST(FromKmNumber AS varchar(10))", "width")]
    public async Task SaveExtractSql_WhenCompositeLastKeyFormatUnsortable_SavesWithWarning_Test(string keyExpr, string expectedWarning)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("lastKey", ShareDataConst.SqlVariableTypeString, ShareDataConst.SqlVariableSourceLastKey)]
        });
        var sql = $"SELECT TOP 50 ID AS zoneId, {keyExpr} AS __lastKey FROM TmsZone WHERE ({keyExpr}) > @lastKey ORDER BY __lastKey";

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
            new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql });

        // Assert
        var expected = expectedWarning switch
        {
            "date" => new[] { BaseMsg.ShareData.Validation.SqlLastKeyDateFormatUnsortable },
            "width" => new[] { BaseMsg.ShareData.Validation.SqlLastKeyValueNotFixedWidth },
            _ => Array.Empty<string>()
        };
        Assert.Equal(sql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
        Assert.Equal(expected, output.Warnings.ToArray());
    }

    /// <summary>
    /// Description: Biến @lastKey gộp 1 cột: lưu DefaultValue (cắt khoảng trắng, chặn vượt 256 ký tự), đọc SavedValue tách biệt giữa mẫu chung (luôn null),
    ///              đối tác A dùng chung SQL nhưng có mốc trong ShareDataLastSend và đối tác B chưa chạy; chạy thử câu SQL (ExecutePacketSqlSampleInput)
    ///              áp dụng DefaultValue như lượt gửi đầu tiên.
    /// Created date: 27/10/2026
    /// </summary>
    [Fact]
    public async Task SqlVariables_CompositeLastKeyDefaultAndSavedValue_IsolatesCommonAndPartnersAndSamplePreview_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partnerA) = await SeedSqlVariablePacketAsync(db, unique);
        var partnerB = NewPartnerSqlPartner($"PT_B_{unique}", unique);
        await db.Insertable(partnerB).ExecuteCommandAsync();

        var newTime = new DateTime(2026, 10, 27, 9, 30, 0);
        var zoneOld = new TmsZone { ID = $"z_old_{unique}", Code = $"ZO_{unique}", Name = "2026-10-27 08:30:00.000" };
        var zoneNew = new TmsZone { ID = $"z_new_{unique}", Code = $"ZN_{unique}", Name = "2026-10-27 09:30:00.000" };
        await db.Insertable(new List<TmsZone> { zoneOld, zoneNew }).ExecuteCommandAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items =
            [
                new PacketSqlVariableItemInput
                {
                    Code = "lastKey",
                    ValueKind = ShareDataConst.SqlVariableKindBuiltin,
                    Value = ShareDataConst.SqlVariableSourceLastKey,
                    DefaultValue = new string('X', 257)
                }
            ]
        }));

        const string cutoff = "2026-10-27 09:00:00.000_";
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items =
            [
                new PacketSqlVariableItemInput
                {
                    Code = "lastKey",
                    ValueKind = ShareDataConst.SqlVariableKindBuiltin,
                    Value = ShareDataConst.SqlVariableSourceLastKey,
                    DefaultValue = $"  {cutoff}  "
                },
                new PacketSqlVariableItemInput
                {
                    Code = "prefix",
                    ValueKind = ShareDataConst.SqlVariableKindConst,
                    DataType = ShareDataConst.SqlVariableTypeString,
                    Value = $"%_{unique}"
                }
            ]
        });

        await db.Insertable(new ShareDataLastSend
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partnerA.Code,
            PacketCode = packet.Code,
            LastTime = newTime,
            LastKey = $"2026-10-27 09:30:00.000_{zoneNew.ID}"
        }).ExecuteCommandAsync();

        const string sampleSql = "SELECT TOP 10 Code AS zoneCode, Name + '_' + ID AS __lastKey "
            + "FROM TmsZone WHERE Code LIKE @prefix AND (Name + '_' + ID) > @lastKey ORDER BY __lastKey ASC";

        // Act
        var commonVars = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(
            new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        var partnerAVars = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(
            new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partnerA.ID });
        var partnerBVars = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(
            new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partnerB.ID });
        var sample = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(
            new ExecutePacketSqlSampleInput { PacketCode = packet.Code!, ExtractSql = sampleSql, Top = 10 });

        // Assert
        var commonLastKey = Assert.Single(commonVars.Items, i => i.Code == "lastKey");
        Assert.Equal(cutoff, commonLastKey.DefaultValue);
        Assert.Null(commonLastKey.SavedValue);

        var partnerALastKey = Assert.Single(partnerAVars.Items, i => i.Code == "lastKey");
        Assert.Equal(cutoff, partnerALastKey.DefaultValue);
        Assert.Equal($"2026-10-27 09:30:00.000_{zoneNew.ID}", partnerALastKey.SavedValue);
        Assert.Null(Assert.Single(partnerAVars.Items, i => i.Code == "prefix").SavedValue);

        var partnerBLastKey = Assert.Single(partnerBVars.Items, i => i.Code == "lastKey");
        Assert.Equal(cutoff, partnerBLastKey.DefaultValue);
        Assert.Null(partnerBLastKey.SavedValue);

        Assert.True(sample.IsSuccess, sample.ErrorMessage);
        var previewRow = Assert.Single(sample.Rows);
        Assert.Equal(zoneNew.Code, previewRow["zoneCode"]?.ToString());
    }

    #endregion

    #region Trường của gói tin (FieldsJson)

    /// <summary>
    /// Description: Lưu câu SQL mẫu chung kèm sửa trường: danh sách trường sinh từ cột của câu SQL (khoá = tên cột, bỏ cột hệ thống __),
    ///              nhãn / bắt buộc / bỏ qua người dùng sửa được giữ ở FieldsJson; GetFields (màn ánh xạ) không trả trường bỏ qua.
    /// Created date: 26/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateExtractSql_WhenFieldsEdited_GeneratesFieldsFromSqlAndKeepsEdits_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        const string sql = "SELECT TOP 10 ID AS zoneId, Name AS zoneName, MaxSpeed AS speedLimit FROM TmsZone";

        // Act
        var saved = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = sql,
            Fields =
            [
                new SqlFieldEditInput { Key = "zoneId", Label = "Mã đoạn tuyến", Required = true },
                new SqlFieldEditInput { Key = "speedLimit", Skip = true }
            ]
        });
        var mappingFields = await scope.ServiceProvider.CallApiAsync<List<SqlFieldItem>>(new GetPacketFieldsInput { DatatypeId = packet.ID! });

        // Assert
        Assert.Equal(["zoneId", "zoneName", "speedLimit"], saved.Fields.Select(f => f.Key));
        var stored = SqlFieldHelper.Read((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.FieldsJson);
        var zoneId = stored.Single(f => f.Key == "zoneId");
        Assert.Equal("Mã đoạn tuyến", zoneId.Label);
        Assert.True(zoneId.Required);
        Assert.True(stored.Single(f => f.Key == "speedLimit").Skip);
        Assert.Equal(["zoneId", "zoneName"], mappingFields.Select(f => f.Key));
    }

    /// <summary>
    /// Description: Người dùng chọn kiểu cho trường ở popup sửa trường: kiểu chọn tay được giữ qua các lần lưu sau (bộ dò không ghi đè),
    ///              chọn lại đúng kiểu dò được thì bỏ chọn tay.
    /// Created date: 27/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateExtractSql_WhenFieldTypeChosen_KeepsChosenTypeUntilResetToDetected_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        const string sql = "SELECT TOP 10 ID AS zoneId, Name AS zoneName, MaxSpeed AS speedLimit FROM TmsZone";
        UpdatePacketExtractSqlInput Save(string? type) => new()
        {
            PacketCode = packet.Code!,
            ExtractSql = sql,
            Fields = [new SqlFieldEditInput { Key = "speedLimit", Type = type }]
        };
        async Task<SqlFieldItem> StoredSpeedLimit() =>
            SqlFieldHelper.Read((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.FieldsJson).Single(f => f.Key == "speedLimit");

        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(Save(null));
        var detected = (await StoredSpeedLimit()).Type;
        var chosen = detected == SqlFieldType.String ? SqlFieldType.Number : SqlFieldType.String;

        // Act + Assert: chọn kiểu khác
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(Save(chosen));
        Assert.Equal(chosen, (await StoredSpeedLimit()).Type);
        Assert.Equal(chosen, (await StoredSpeedLimit()).TypeOverride);

        // Lưu lại không gửi kiểu: vẫn giữ kiểu đã chọn
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(Save(null));
        Assert.Equal(chosen, (await StoredSpeedLimit()).Type);

        // Chọn lại đúng kiểu dò được: bỏ chọn tay
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(Save(detected));
        Assert.Equal(detected, (await StoredSpeedLimit()).Type);
        Assert.Null((await StoredSpeedLimit()).TypeOverride);
    }

    /// <summary>
    /// Description: Chọn kiểu trong cấu hình sharedata_value_type (như luồng cũ) thì được nhận và giữ nguyên.
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData("decimal")]
    [InlineData("long")]
    [InlineData("guid")]
    public async Task UpdateExtractSql_WhenFieldTypeFromConfig_KeepsType_Test(string type)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);

        // Act
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT TOP 10 ID AS zoneId, Name AS zoneName FROM TmsZone",
            Fields = [new SqlFieldEditInput { Key = "zoneName", Type = type }]
        });

        // Assert
        var stored = SqlFieldHelper.Read((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.FieldsJson).Single(f => f.Key == "zoneName");
        Assert.Equal(type, stored.Type);
        Assert.Equal(type, stored.TypeOverride);
    }

    /// <summary>
    /// Description: Kiểu không thuộc kiểu bộ dò sinh hoặc cấu hình sharedata_value_type bị từ chối.
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData("text")]
    [InlineData("varchar")]
    [InlineData("money")]
    public async Task UpdateExtractSql_WhenFieldTypeInvalid_Throws_Test(string type)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);

        // Act + Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT TOP 10 ID AS zoneId, MaxSpeed AS speedLimit FROM TmsZone",
            Fields = [new SqlFieldEditInput { Key = "speedLimit", Type = type }]
        }));
    }

    /// <summary>
    /// Description: Câu SQL mới làm mất khoá mà hồ sơ ánh xạ chiều gửi đang dùng: lần đầu không lưu, trả RequiresConfirm kèm khoá mất;
    ///              gửi lại với Force thì lưu và xoá hẳn trường mất cột (như wireframe).
    /// Modified date: 27/10/2026
    /// Created date: 26/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateExtractSql_WhenLostKeyUsedByMapping_RequiresConfirmThenSavesWithForce_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        const string oldSql = "SELECT TOP 10 ID AS zoneId, Name AS zoneName FROM TmsZone";
        const string newSql = "SELECT TOP 10 ID AS zoneId FROM TmsZone";
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = oldSql });
        await db.Insertable(new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"MAP_{unique}",
            PartnerId = partner.ID,
            DatatypeId = packet.ID,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            TargetShapeJson = """{ "name": { "$field": "zoneName" } }"""
        }).ExecuteCommandAsync();

        // Act
        var pending = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = newSql });
        var sqlAfterPending = (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql;
        var forced = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = newSql, Force = true });

        // Assert
        Assert.True(pending.RequiresConfirm);
        Assert.Equal(["zoneName"], pending.Lost);
        Assert.Equal(oldSql, sqlAfterPending);
        Assert.False(forced.RequiresConfirm);
        Assert.Equal(newSql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
        // Như wireframe: xác nhận xong thì trường mất cột bị xoá hẳn khỏi danh sách
        Assert.DoesNotContain(forced.Fields, f => f.Key == "zoneName");
        Assert.DoesNotContain(SqlFieldHelper.Read((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.FieldsJson), f => f.Key == "zoneName");
    }

    /// <summary>
    /// Description: Dò cột câu SQL đang soạn (chưa lưu) theo bộ biến của câu đó: câu dùng biến đã khai (@since) thì dò được cột,
    ///              biến chưa khai thì báo lỗi; không ghi dữ liệu.
    /// Created date: 26/10/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DescribeColumns_WhenSqlUsesVariable_DescribesOnlyWithDeclaredVariable_Test(bool declared)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        if (declared)
        {
            await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
            {
                PacketCode = packet.Code!,
                Items = [BuiltinVar("since", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceLastTime)]
            });
        }
        const string sql = "SELECT TOP 10 ID AS zoneId, UpdateTime AS dataTime FROM TmsZone WHERE @since IS NULL OR UpdateTime > @since";

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<DescribeColumnsOutput>(new DescribeColumnsInput { PacketCode = packet.Code!, ExtractSql = sql });

        // Assert
        Assert.Equal(declared, result.IsValid);
        if (declared)
        {
            Assert.Equal(["zoneId", "dataTime"], result.Fields.Select(f => f.Key));
            Assert.Equal(SqlFieldType.DateTime, result.Fields.Single(f => f.Key == "dataTime").Type);
        }
        Assert.Null((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.FieldsJson);
    }

    /// <summary>
    /// Description: Màn ánh xạ lấy trường theo đối tác: đối tác có câu SQL riêng thì trả trường của câu riêng, không thì trường mẫu chung.
    /// Created date: 26/10/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetFields_ByPartner_ReturnsCustomSqlFieldsOnlyWhenPartnerHasCustomSql_Test(bool partnerHasCustomSql)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT TOP 10 ID AS zoneId, Name AS zoneName FROM TmsZone"
        });
        if (partnerHasCustomSql)
        {
            await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
            {
                PartnerId = partner.ID!,
                PacketCode = packet.Code!,
                ExtractSql = "SELECT TOP 10 ID AS zoneId, LaneId AS laneId FROM TmsZone"
            });
        }

        // Act
        var fields = await scope.ServiceProvider.CallApiAsync<List<SqlFieldItem>>(new GetPacketFieldsInput { DatatypeId = packet.Code!, PartnerId = partner.ID });

        // Assert
        Assert.Equal(partnerHasCustomSql ? ["zoneId", "laneId"] : ["zoneId", "zoneName"], fields.Select(f => f.Key));
    }

    #endregion

    #region Câu ghi chiều nhận (ShareDataSql.WriteSql)

    /// <summary>
    /// Description: Lưu khối câu ghi chiều nhận (nhiều câu MERGE trong một khối) qua API: tự dò bảng đích và khoá bản ghi từ ON,
    ///              lưu danh sách bảng bị ảnh hưởng, danh sách gói đếm đúng số bảng bị ảnh hưởng; xóa trắng câu ghi thì xóa mềm dòng đang lưu.
    /// Created date: 26/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateWriteSqls_WhenListSaved_KeepsOrderDetectsKeyAndSoftDeletesRemoved_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        var combinedSql = ValidWriteSql + "\n\n" + SecondTableWriteSql;

        // Act
        var first = await scope.ServiceProvider.CallApiAsync<PacketWriteSqlsOutput>(new UpdatePacketWriteSqlsInput
        {
            PacketCode = packet.Code!,
            WriteSql = combinedSql,
            AffectedTables = ["dbo.ShareDataInboundPacket", "TmsZone"],
            Remark = "gộp 2 bảng"
        });
        var pageTwoTables = await scope.ServiceProvider.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(new ShareDataPagePacketInput { Code = packet.Code, Page = 1, PageSize = 10 });

        var resaved = await scope.ServiceProvider.CallApiAsync<PacketWriteSqlsOutput>(new UpdatePacketWriteSqlsInput
        {
            PacketCode = packet.Code!,
            WriteSql = ValidWriteSql,
            AffectedTables = ["ShareDataInboundPacket"],
            Remark = "còn 1 bảng"
        });
        var pageOneTable = await scope.ServiceProvider.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(new ShareDataPagePacketInput { Code = packet.Code, Page = 1, PageSize = 10 });

        var cleared = await scope.ServiceProvider.CallApiAsync<PacketWriteSqlsOutput>(new UpdatePacketWriteSqlsInput
        {
            PacketCode = packet.Code!,
            WriteSql = string.Empty,
            AffectedTables = []
        });

        // Assert
        Assert.Equal("gộp 2 bảng", first.Remark);
        Assert.Equal(["dbo.ShareDataInboundPacket", "dbo.TmsZone"], first.AffectedTables);
        Assert.Equal(["dbo.ShareDataInboundPacket", "dbo.TmsZone"], first.DetectedTables);
        Assert.Equal(["id", "zoneId"], first.KeyFields);
        Assert.Equal(2, pageTwoTables.Records.Single(p => p.Code == packet.Code).WriteSqlCount);

        Assert.Equal("còn 1 bảng", resaved.Remark);
        Assert.Equal(["dbo.ShareDataInboundPacket"], resaved.AffectedTables);
        Assert.Equal(["id"], resaved.KeyFields);
        Assert.Equal(1, pageOneTable.Records.Single(p => p.Code == packet.Code).WriteSqlCount);

        Assert.Null(cleared.WriteSql);
        Assert.Empty(cleared.AffectedTables);
        Assert.Null(await ShareDataSqlSeed.GetWriteSqlAsync(db, packet.ID));
    }

    /// <summary>
    /// Description: Kiểm an toàn câu ghi: các trường hợp bị chặn (lỗi) và trường hợp chỉ cảnh báo (vẫn hợp lệ).
    ///              Cho phép nhiều câu MERGE/INSERT/UPDATE trong một khối miễn mọi bảng đích nằm trong AffectedTables.
    ///              Bị chặn: ghi bảng ngoài AffectedTables, câu SELECT, có DELETE, có EXEC, tham số lạ, bảng ở CSDL khác,
    ///              bảng không có trong CSDL nhận, sai tên cột (CSDL nhận biên dịch lỗi).
    ///              Cảnh báo: bảng trong AffectedTables nhưng chưa dùng trong SQL, thiếu GROUP BY / IS NOT NULL theo khoá, thiếu OUTPUT $action.
    /// Created date: 26/10/2026
    /// </summary>
    [Theory]
    [InlineData("valid", "dbo.ShareDataInboundPacket", true, 0)]
    [InlineData("two-merges", "dbo.ShareDataInboundPacket,dbo.TmsZone", true, 0)]
    [InlineData("unused-table", "dbo.ShareDataInboundPacket,dbo.TmsZone", true, 1)]
    [InlineData("other-table", "dbo.ShareDataPartner", false, 0)]
    [InlineData("two-statements-select", "dbo.ShareDataInboundPacket", false, 0)]
    [InlineData("select", "dbo.ShareDataInboundPacket", false, 0)]
    [InlineData("delete", "dbo.ShareDataInboundPacket", false, 0)]
    [InlineData("exec", "dbo.ShareDataInboundPacket", false, 0)]
    [InlineData("unknown-param", "dbo.ShareDataInboundPacket", false, 0)]
    [InlineData("cross-db", "dbo.ShareDataInboundPacket", false, 0)]
    [InlineData("missing-table", "dbo.NoSuchInboundTable", false, 0)]
    [InlineData("bad-column", "dbo.ShareDataInboundPacket", false, 0)]
    [InlineData("no-group-no-output", "dbo.ShareDataInboundPacket", true, 3)]
    public async Task ValidateWriteSql_WhenSqlChecked_BlocksUnsafeAndWarnsRisky_Test(string scenario, string affectedTablesCsv, bool expectValid, int expectWarnings)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        var affectedTables = affectedTablesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var sql = scenario switch
        {
            "two-merges" => ValidWriteSql + "\n\n" + SecondTableWriteSql,
            "two-statements-select" => ValidWriteSql + "\nSELECT 1;",
            "select" => "SELECT id FROM OPENJSON(@records) WITH (id nvarchar(64))",
            "delete" => ValidWriteSql.Replace("WHEN MATCHED THEN\n    UPDATE SET t.PacketCode = s.code", "WHEN MATCHED THEN DELETE"),
            "exec" => "EXEC sp_who",
            "unknown-param" => ValidWriteSql.Replace("'TRY_WRITE'", "@someone"),
            "cross-db" => ValidWriteSql.Replace("FROM OPENJSON(@records) WITH (id nvarchar(64), code nvarchar(64))", "FROM OPENJSON(@records) WITH (id nvarchar(64), code nvarchar(64)) CROSS JOIN master.dbo.spt_values v"),
            "missing-table" => ValidWriteSql.Replace("dbo.ShareDataInboundPacket", "dbo.NoSuchInboundTable"),
            "bad-column" => ValidWriteSql.Replace("t.PacketCode = s.code", "t.NoSuchColumn = s.code"),
            "no-group-no-output" => ValidWriteSql
                .Replace("SELECT id, MAX(code) AS code", "SELECT id, code")
                .Replace("WHERE id IS NOT NULL\n    GROUP BY id\n", string.Empty)
                .Replace("OUTPUT $action;", ";"),
            _ => ValidWriteSql
        };

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<ValidateWriteSqlOutput>(new ValidateWriteSqlInput
        {
            PacketCode = packet.Code!,
            WriteSql = sql,
            AffectedTables = affectedTables
        });

        // Assert
        Assert.True(expectValid == result.IsValid, string.Join(" | ", result.Checks.Select(c => $"{c.Level}: {c.Message}")));
        if (expectValid)
        {
            Assert.Equal(expectWarnings, result.Checks.Count(c => c.Level == ShareDataConst.WriteSqlCheckWarning));
            Assert.Contains("id", result.KeyFields);
        }
    }

    /// <summary>
    /// Description: Câu ghi không an toàn thì không lưu được: ném lỗi, câu ghi đang lưu giữ nguyên.
    /// Created date: 26/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateWriteSqls_WhenItemUnsafe_ThrowsAndKeepsSavedList_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketWriteSqlsOutput>(new UpdatePacketWriteSqlsInput
        {
            PacketCode = packet.Code!,
            WriteSql = ValidWriteSql,
            AffectedTables = ["dbo.ShareDataInboundPacket"]
        });

        // Act & Assert
        var error = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketWriteSqlsOutput>(new UpdatePacketWriteSqlsInput
        {
            PacketCode = packet.Code!,
            WriteSql = ValidWriteSql,
            AffectedTables = ["dbo.ShareDataPartner"]
        }));

        Assert.NotNull(error);
        var common = await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        Assert.Equal("dbo.ShareDataInboundPacket", common?.AffectedTables);
    }

    /// <summary>
    /// Description: Chạy thử câu ghi trên CSDL nhận với dữ liệu mẫu: đếm đúng số dòng thêm qua mọi tập kết quả, rồi quay lui nên CSDL nhận không có dòng nào;
    ///              dữ liệu mẫu không phải mảng JSON thì báo lỗi.
    /// Created date: 26/10/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TryWriteSql_WhenRecordsGiven_CountsActionsAndRollsBack_Test(bool recordsIsArray)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var inbound = db.AsTenant().GetConnectionScope(Its015Const.ConnectionConst.ShareData);
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        var ids = new[] { $"TRY_{unique}_1", $"TRY_{unique}_2" };
        var records = recordsIsArray
            ? $$"""[{ "id": "{{ids[0]}}", "code": "A" }, { "id": "{{ids[1]}}", "code": "B" }, { "id": "{{ids[1]}}", "code": "C" }]"""
            : """{ "id": "x" }""";

        // Act
        var call = () => scope.ServiceProvider.CallApiAsync<TryWriteSqlOutput>(new TryWriteSqlInput
        {
            PacketCode = packet.Code!,
            RecordsJson = records,
            WriteSql = ValidWriteSql,
            AffectedTables = ["dbo.ShareDataInboundPacket"]
        });

        // Assert
        if (!recordsIsArray)
        {
            Assert.NotNull(await Record.ExceptionAsync(call));
            return;
        }
        var result = await call();
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(3, result.RecordCount);
        Assert.Equal(2, result.Inserted);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, await inbound.Queryable<ShareDataInboundPacket>().Where(p => ids.Contains(p.ID)).CountAsync());
    }

    #endregion

    #region Các hàm Handle còn thiếu ca kiểm (27/10/2026)

    /// <summary>
    /// Description: Lấy danh sách gói theo mã (ShareDataPacketInput) và lấy một gói theo ID (ShareDataIdPacketInput); ID không tồn tại thì báo lỗi
    /// Created date: 27/10/2026
    /// </summary>
    [Fact]
    public async Task GetPacket_ByCodeListAndById_ReturnsPacketAndThrowsWhenMissing_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        // Act
        var list = await scope.ServiceProvider.CallApiAsync<List<ShareDataPagePacketOutput>>(new ShareDataPacketInput { Code = packet.Code });
        var one = await scope.ServiceProvider.CallApiAsync<ShareDataPacketOutput>(new ShareDataIdPacketInput { ID = packet.ID });

        // Assert
        Assert.Equal(packet.ID, Assert.Single(list).ID);
        Assert.Equal(packet.Code, one.Code);
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<ShareDataPacketOutput>(new ShareDataIdPacketInput { ID = Guid.NewGuid().ToString("N") }));
    }

    /// <summary>
    /// Description: Xóa một gói: không ai dùng thì xóa mềm; đang có đăng ký còn hiệu lực thì báo PacketInUse và giữ nguyên gói
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletePacket_WhenInUseOrNot_SoftDeletesOnlyUnusedPacket_Test(bool inUse)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        if (inUse)
        {
            await db.Insertable(NewPartnerSqlSubscription(partner, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active)).ExecuteCommandAsync();
        }

        // Act
        var call = () => scope.ServiceProvider.CallApiAsync(new ShareDataDeletePacketInput { ID = packet.ID });

        // Assert
        if (inUse)
        {
            await Assert.ThrowsAnyAsync<Exception>(call);
        }
        else
        {
            await call();
        }
        var saved = await db.Queryable<ShareDataPacket>().ClearFilter().Where(p => p.ID == packet.ID).FirstAsync();
        Assert.Equal(inUse, saved.IsDelete == null);
    }

    /// <summary>
    /// Description: Xóa nhiều gói: đủ ID thì xóa mềm cả lô; có ID không tồn tại thì báo lỗi và không xóa gói nào
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchDeletePacket_WhenAnyIdMissing_DeletesNothing_Test(bool includeMissingId)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (first, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);
        var (second, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);
        var input = new List<ShareDataDeletePacketInput> { new() { ID = first.ID }, new() { ID = second.ID } };
        if (includeMissingId) input.Add(new() { ID = Guid.NewGuid().ToString("N") });

        // Act
        var call = () => scope.ServiceProvider.CallApiAsync(input);

        // Assert
        if (includeMissingId)
        {
            await Assert.ThrowsAnyAsync<Exception>(call);
        }
        else
        {
            await call();
        }
        var activeCount = await db.Queryable<ShareDataPacket>().Where(p => (p.ID == first.ID || p.ID == second.ID) && p.IsDelete == null).CountAsync();
        Assert.Equal(includeMissingId ? 2 : 0, activeCount);
    }

    /// <summary>
    /// Description: Xem dữ liệu mẫu của gói (ShareDataPacketSampleDataInput): chạy câu SQL mẫu chung, giới hạn số dòng theo Top;
    ///              gói chưa có câu SQL thì báo PacketExtractSqlMissing
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PacketSampleData_WhenCommonSqlConfiguredOrNot_ReturnsRowsOrThrows_Test(bool hasSql)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);
        if (hasSql)
        {
            await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT TOP 3 ID AS zoneId, Name AS zoneName FROM TmsZone");
        }
        var input = new ShareDataPacketSampleDataInput { Code = packet.Code, Top = 2 };

        // Act & Assert
        if (!hasSql)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<ShareDataPacketSampleDataOutput>(input));
            return;
        }
        var sample = await scope.ServiceProvider.CallApiAsync<ShareDataPacketSampleDataOutput>(input);
        Assert.Equal(packet.Code, sample.PacketCode);
        Assert.Equal(["zoneId", "zoneName"], sample.Columns);
        Assert.Equal(sample.Rows.Count, sample.RowCount);
    }

    /// <summary>
    /// Description: Đọc câu SQL của đối tác (GetPartnerExtractSqlInput): có SQL riêng thì IsCustom và trả câu riêng; chưa có thì dùng câu mẫu chung
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPartnerExtractSql_WhenPartnerHasCustomSqlOrNot_ReturnsMatchingSource_Test(bool hasCustom)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);
        const string commonSql = "SELECT ID AS zoneId FROM TmsZone";
        const string customSql = "SELECT ID AS zoneId, Name AS zoneName FROM TmsZone";
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, commonSql);
        if (hasCustom)
        {
            await ShareDataSqlSeed.SetPartnerAsync(db, packet.ID, partner.ID, customSql);
        }

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new GetPartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code! });

        // Assert
        Assert.Equal(hasCustom, output.IsCustom);
        Assert.Equal(commonSql, output.StandardSql);
        Assert.Equal(hasCustom ? customSql : null, string.IsNullOrWhiteSpace(output.CustomSql) ? null : output.CustomSql);
    }

    /// <summary>
    /// Description: Đọc khối câu ghi chiều nhận (GetPacketWriteSqlsInput) đã lưu ở dòng mẫu chung, kèm bảng bị ảnh hưởng
    /// Created date: 27/10/2026
    /// </summary>
    [Fact]
    public async Task GetWriteSqls_ReturnsSavedBlockAndAffectedTables_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);
        await ShareDataSqlSeed.SetWriteAsync(db, packet.ID, ValidWriteSql, "dbo.ShareDataInboundPacket");

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PacketWriteSqlsOutput>(new GetPacketWriteSqlsInput { PacketCode = packet.Code! });

        // Assert
        Assert.Equal(ValidWriteSql, output.WriteSql);
        Assert.Equal(["dbo.ShareDataInboundPacket"], output.AffectedTables);
    }

    /// <summary>
    /// Description: Đặt lại mốc đã gửi (ResetLastSendInput): SuperAdmin xóa mốc của đúng đối tác × gói; người thường bị chặn và mốc giữ nguyên
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResetLastSend_BySuperAdminOrNot_DeletesOnlyThatPartnerCursor_Test(bool isSuperAdmin)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        var other = NewPartnerSqlPartner($"PT_OTHER_{unique}", unique);
        await db.Insertable(other).ExecuteCommandAsync();
        await db.Insertable(new List<ShareDataLastSend>
        {
            new() { ID = Guid.NewGuid().ToString("N"), PartnerCode = partner.Code, PacketCode = packet.Code, LastKey = "k1", LastTime = DateTime.Now },
            new() { ID = Guid.NewGuid().ToString("N"), PartnerCode = other.Code, PacketCode = packet.Code, LastKey = "k2", LastTime = DateTime.Now }
        }).ExecuteCommandAsync();
        SetUserContext(isSuperAdmin);
        var input = new ResetLastSendInput { PacketCode = packet.Code!, PartnerId = partner.ID! };

        // Act
        if (isSuperAdmin)
        {
            await scope.ServiceProvider.CallApiAsync(input);
        }
        else
        {
            await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync(input));
        }

        // Assert
        var left = await db.Queryable<ShareDataLastSend>().Where(ls => ls.PacketCode == packet.Code).Select(ls => ls.PartnerCode).ToListAsync();
        Assert.Equal(isSuperAdmin ? [other.Code] : new List<string?> { partner.Code, other.Code }.OrderBy(c => c).ToList(), left.OrderBy(c => c).ToList());
    }

    /// <summary>
    /// Description: Lưu câu SQL gửi (mẫu chung và SQL riêng) ghi lại cả dòng ShareDataSql nhưng không đè mất bộ biến (VariablesJson)
    ///              và khối câu ghi chiều nhận đã lưu ở màn khác cùng dòng
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveExtractSql_KeepsVariablesAndWriteSqlOnSameRow_Test(bool partnerScope)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);
        const string sql = "SELECT ID AS zoneId FROM TmsZone WHERE CreateTime >= @yearStart";
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, sql);
        await ShareDataSqlSeed.SetWriteAsync(db, packet.ID, ValidWriteSql, "dbo.ShareDataInboundPacket");
        var partnerId = partnerScope ? partner.ID : null;
        if (partnerScope)
        {
            await ShareDataSqlSeed.SetPartnerAsync(db, packet.ID, partner.ID, sql);
        }
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partnerId,
            Items = [BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceStartOfYear)]
        });

        // Act
        if (partnerScope)
        {
            await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql + " AND ID IS NOT NULL" });
        }
        else
        {
            await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql + " AND ID IS NOT NULL" });
        }

        // Assert
        Assert.Equal("yearStart", Assert.Single(await ShareDataSqlSeed.GetVariablesAsync(db, packet.ID, partnerId)).Code);
        Assert.Equal(ValidWriteSql, await ShareDataSqlSeed.GetWriteSqlAsync(db, packet.ID));
    }

    #endregion

    #region Chi tiết SQL của gói (GetPacketSqlDetail)

    /// <summary>
    /// Description: Một API trả gọn câu SQL mẫu chung (kèm trường), bộ biến mẫu chung và khối câu ghi của gói, khớp với ba API đọc riêng;
    ///              không phải SuperAdmin thì bị chặn
    /// Created date: 27/10/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPacketSqlDetail_ReturnsExtractVariablesAndWriteMatchingSeparateApis_Test(bool isSuperAdmin)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceStartOfYear)]
        });
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT ID AS zoneId, Name AS zoneName FROM TmsZone WHERE CreateTime >= @yearStart"
        });
        await ShareDataSqlSeed.SetWriteAsync(db, packet.ID, ValidWriteSql, "dbo.ShareDataInboundPacket");
        var input = new GetPacketSqlDetailInput { PacketCode = packet.Code! };

        if (!isSuperAdmin)
        {
            SetUserContext(isSuperAdmin: false);
            await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlDetailOutput>(input));
            return;
        }

        // Act
        var detail = await scope.ServiceProvider.CallApiAsync<PacketSqlDetailOutput>(input);
        var extract = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput { PacketCode = packet.Code! });
        var variables = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        var write = await scope.ServiceProvider.CallApiAsync<PacketWriteSqlsOutput>(new GetPacketWriteSqlsInput { PacketCode = packet.Code! });

        // Assert
        Assert.Equal(extract.ExtractSql, detail.Extract.ExtractSql);
        Assert.Equal(["zoneId", "zoneName"], detail.Extract.Fields.Select(f => f.Key).ToArray());
        Assert.Equal(extract.Fields.Select(f => f.Key), detail.Extract.Fields.Select(f => f.Key));
        Assert.Equal(variables.Items.Select(i => (i.Code, i.Value, i.Scope)), detail.Variables.Items.Select(i => (i.Code, i.Value, i.Scope)));
        Assert.Equal("yearStart", Assert.Single(detail.Variables.Items).Code);
        Assert.Equal(write.WriteSql, detail.Write.WriteSql);
        Assert.Equal(write.AffectedTables, detail.Write.AffectedTables);
        Assert.Equal(write.KeyFields, detail.Write.KeyFields);
    }

    /// <summary>
    /// Description: Gói chưa có dòng ShareDataSql nào: trả câu SQL trống, không biến, không câu ghi; mã gói không tồn tại thì báo lỗi
    /// Created date: 27/10/2026
    /// </summary>
    [Fact]
    public async Task GetPacketSqlDetail_WhenNoSqlRowOrUnknownPacket_ReturnsEmptyOrThrows_Test()
    {
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);

        var detail = await scope.ServiceProvider.CallApiAsync<PacketSqlDetailOutput>(new GetPacketSqlDetailInput { PacketCode = packet.Code! });

        Assert.Equal(packet.Code, detail.Extract.PacketCode);
        Assert.Equal(string.Empty, detail.Extract.ExtractSql);
        Assert.Empty(detail.Extract.Fields);
        Assert.Empty(detail.Variables.Items);
        Assert.Null(detail.Write.WriteSql);
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlDetailOutput>(new GetPacketSqlDetailInput { PacketCode = $"NOPE_{Guid.NewGuid():N}" }));
    }

    /// <summary>
    /// Description: Chỉnh sửa kiểu dữ liệu của trường gói tin (FieldsJson): hỗ trợ đầy đủ các kiểu trong AllowedFieldTypes (string, number, int, dateTime, bool, long, float, double, decimal, guid).
    ///              Kiểu chọn tay được lưu vào Type, TypeOverride và không bị bộ dò cột ghi đè khi dò lại.
    /// Created date: 28/10/2026
    /// </summary>
    [Theory]
    [InlineData("string")]
    [InlineData("number")]
    [InlineData("int")]
    [InlineData("dateTime")]
    [InlineData("bool")]
    [InlineData("long")]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("decimal")]
    [InlineData("guid")]
    public async Task UpdatePacketFields_WithAllSupportedTypes_SavesAndPersistsTypeCorrectly_Test(string targetType)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);

        // Tạo câu SQL mẫu có trường toLocationMet
        var initial = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT TOP 10 ID AS zoneId, 100 AS toLocationMet FROM TmsZone"
        });
        Assert.NotNull(initial);
        Assert.Contains(initial.Fields, f => f.Key == "toLocationMet");

        // Act: Chọn kiểu dữ liệu bất kỳ trong danh mục (int, long, float, double, decimal, dateTime, bool, guid, string, number)
        var updated = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT TOP 10 ID AS zoneId, 100 AS toLocationMet FROM TmsZone",
            Fields =
            [
                new SqlFieldEditInput
                {
                    Key = "toLocationMet",
                    Label = "Lý trình cuối (m)",
                    Type = targetType
                }
            ]
        });

        // Assert: Trả về trường với đúng kiểu đã chọn
        var field = Assert.Single(updated.Fields, f => f.Key == "toLocationMet");
        Assert.Equal(targetType, field.Type, ignoreCase: true);

        // Đọc lại từ CSDL qua GetPacketExtractSql
        var reloaded = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new GetPacketExtractSqlInput
        {
            PacketCode = packet.Code!
        });
        var reloadedField = Assert.Single(reloaded.Fields, f => f.Key == "toLocationMet");
        Assert.Equal(targetType, reloadedField.Type, ignoreCase: true);

        // Khi dò lại câu SQL (DescribeColumns), kiểu chọn tay không bị ghi đè
        var described = await scope.ServiceProvider.CallApiAsync<DescribeColumnsOutput>(new DescribeColumnsInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT TOP 10 ID AS zoneId, 100 AS toLocationMet FROM TmsZone"
        });
        var describedField = Assert.Single(described.Fields, f => f.Key == "toLocationMet");
        Assert.Equal(targetType, describedField.Type, ignoreCase: true);
    }

    /// <summary>
    /// Description: Chọn kiểu không hợp lệ ngoài AllowedFieldTypes ném lỗi SqlFieldTypeInvalid.
    /// Created date: 28/10/2026
    /// </summary>
    [Fact]
    public async Task UpdatePacketFields_WithInvalidType_ThrowsSqlFieldTypeInvalidException_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var (packet, _) = await SeedSqlVariablePacketAsync(db, Guid.NewGuid().ToString("N")[..8]);

        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT TOP 10 ID AS zoneId, 100 AS toLocationMet FROM TmsZone"
        });

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = "SELECT TOP 10 ID AS zoneId, 100 AS toLocationMet FROM TmsZone",
            Fields =
            [
                new SqlFieldEditInput
                {
                    Key = "toLocationMet",
                    Type = "unknown_type_xyz"
                }
            ]
        }));
    }

    #endregion
}
