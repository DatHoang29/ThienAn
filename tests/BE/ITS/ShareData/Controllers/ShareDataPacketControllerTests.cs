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
        await ShareDataSqlSeed.SetCommonAsync(db, seededPacketId, tpl.ExtractSql);

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

    /// <summary>
    /// Author: DatHQ
    /// Description: Gói đã có hồ sơ ánh xạ hoặc đăng ký đang chạy thì khóa đổi Mã, Tên, các thông tin mô tả vẫn sửa được; gói chưa ai dùng thì đổi được.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("anhXa", "doiTen", true)]
    [InlineData("dangKy", "doiMa", true)]
    [InlineData("anhXa", "doiGhiChu", false)]
    [InlineData("khong", "doiTen", false)]
    public async Task UpdatePacket_WhenPacketInUse_LocksCodeAndNameOnly_Test(string usedBy, string change, bool rejected)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        if (usedBy == "anhXa")
        {
            await db.Insertable(new ShareDataMapping { ID = Guid.NewGuid().ToString("N"), PartnerId = partner.ID, DatatypeId = packet.ID, Direction = BaseEnums.Direction.Outbound, IsActive = false }).ExecuteCommandAsync();
        }
        else if (usedBy == "dangKy")
        {
            var subscription = NewPartnerSqlSubscription(partner, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active);
            await db.Insertable(subscription).ExecuteCommandAsync();
        }
        var input = new ShareDataUpdatePacketInput { ID = packet.ID, Code = packet.Code, Name = packet.Name, OrderNo = 101, Status = packet.Status };
        switch (change)
        {
            case "doiTen": input.Name = $"{packet.Name} moi"; break;
            case "doiMa": input.Code = $"{packet.Code}_M"; break;
            default: input.Remark = "ghi chú mới"; break;
        }

        // Act
        var error = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync(input));

        // Assert
        var saved = await db.Queryable<ShareDataPacket>().InSingleAsync(packet.ID);
        if (rejected)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Exception.PacketCodeNameLocked);
            Assert.Equal(packet.Code, saved.Code);
            Assert.Equal(packet.Name, saved.Name);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(input.Name, saved.Name);
            Assert.Equal(input.Remark, saved.Remark);
        }
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
                lastKey: null,
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
    /// Modified date: 03/11/2026
    /// </summary>
    [Theory]
    [InlineData("SELECT TOP 100 ID, Code FROM TmsZone WHERE ID > @lastKey ORDER BY ID ASC")]
    [InlineData("SELECT TOP 100 ID AS __rowid, Code FROM TmsZone WHERE ID > @lastKey ORDER BY ID ASC")]
    [InlineData("SELECT TOP 100 ID AS __lastKey, Code FROM TmsZone WHERE ID > @lastKey ORDER BY __lastKey DESC")]
    [InlineData("SELECT TOP 100 ID AS __lastKey, Code FROM TmsZone WHERE ID > @lastKey")]
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
        // SQL không có @lastKey -> suy ra Snapshot, cho phép lưu mà không kiểm con trỏ
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

        // Lưu bộ biến: @pageSize là giá trị nhập kiểu DateTime bị Backend từ chối ngay
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packetCode,
            Items =
            [
                new PacketSqlVariableItemInput
                {
                    Code = "pageSize",
                    DataType = ShareDataConst.SqlVariableTypeDateTime,
                    ValueKind = ShareDataConst.SqlVariableKindConst,
                    Value = "2026-01-01"
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
        // Arrange: Câu SQL nối đuôi nhưng thiếu __lastKey
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

        // Act & Assert: SQL có @lastKey nhưng thiếu __lastKey, ORDER BY -> Throws
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "SELECT TOP 100 ID, Code FROM TmsZone WHERE ID > @lastKey"
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
        // Arrange: SQL khác chuẩn và nối đuôi -> VẪN LƯU RIÊNG
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_DIFF_MODE_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var customSql = "SELECT TOP 100 CONVERT(varchar(23), CreateTime, 121) + '_' + ID AS __lastKey, Code FROM TmsZone "
            + "WHERE CONVERT(varchar(23), CreateTime, 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC";
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
        string[] cursorNames = ["lastKey"];
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
                BuiltinVar("@afterKey", ShareDataConst.SqlVariableTypeDateTime, "LASTKEY"),
                ConstVar("@pageSize", "int", " 500 "),
                ConstVar("minRate", "decimal", "1.25"),
                ConstVar("tag", "string", "ACTIVE")
            ]
        });

        Assert.Equal(["afterKey", "pageSize", "minRate", "tag"], saved.Items.Select(i => i.Code).ToArray());
        Assert.All(saved.Items, i => Assert.Equal(ShareDataConst.SqlVariableScopeCommon, i.Scope));
        Assert.Equal(
            [
                ShareDataConst.SqlVariableKindBuiltin,
                ShareDataConst.SqlVariableKindConst, ShareDataConst.SqlVariableKindConst, ShareDataConst.SqlVariableKindConst
            ],
            saved.Items.Select(i => i.ValueKind).ToArray());
        Assert.Equal(
            [ShareDataConst.SqlVariableSourceLastKey, "500", "1.25", "ACTIVE"],
            saved.Items.Select(i => i.Value ?? string.Empty).ToArray());
        Assert.Equal(
            [
                ShareDataConst.SqlVariableTypeString,
                ShareDataConst.SqlVariableTypeInt, ShareDataConst.SqlVariableTypeDecimal, ShareDataConst.SqlVariableTypeString
            ],
            saved.Items.Select(i => i.DataType).ToArray());

        var read = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        Assert.Equal(4, read.Items.Count);
    }

    /// <summary>
    /// Description: Biến đi theo câu SQL. Tạo SQL riêng thì chép bộ biến mẫu chung sang làm bộ biến của đối tác; sửa bộ biến của đối tác
    ///              không đụng mẫu chung (và ngược lại); "Dùng lại mẫu chung" thì xóa bộ biến của đối tác.
    /// Created date: 24/10/2026
    /// Modified date: 25/10/2026 (Biến đi theo câu SQL thay cho ghi đè); 31/10/2026 (xóa hết biến gửi kèm câu SQL mới trong một lần lưu)
    /// </summary>
    [Fact]
    public async Task PartnerSqlVariables_FollowPartnerSql_CopiedOnCreateIndependentAndRemovedOnReset_Test()
    {
        // Arrange: mẫu chung có biến @fromDate
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [ConstVar("fromDate", ShareDataConst.SqlVariableTypeString, "2026-01-01")]
        });

        // Act 1: tạo SQL riêng dùng @fromDate (chép từ mẫu chung)
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packet.Code!,
            ExtractSql = "SELECT ID AS zoneId FROM TmsZone WHERE CreateTime >= @fromDate"
        });

        // Assert 1: đối tác có bộ biến riêng chép từ mẫu chung
        var partnerView = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID });
        Assert.Equal("2026-01-01", Assert.Single(partnerView.Items, i => i.Code == "fromDate").Value);
        Assert.Single(await ShareDataSqlSeed.GetVariablesAsync(db, packet.ID, partner.ID));

        // Act 2: đổi giá trị biến của đối tác
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            Items = [ConstVar("fromDate", ShareDataConst.SqlVariableTypeString, "2027-01-01")]
        });

        // Assert 2: mẫu chung giữ nguyên giá trị cũ
        var common = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        Assert.Equal("2026-01-01", Assert.Single(common.Items).Value);

        // Act 3: đối tác lưu SQL riêng không còn dùng biến, bộ biến rỗng gửi kèm câu (như Lưu & phân tích)
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packet.Code!,
            ExtractSql = "SELECT ID AS zoneId FROM TmsZone",
            Variables = []
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

        var input = new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [ConstVar("since", ShareDataConst.SqlVariableTypeString, "2026-01-01")] };
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(input);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [] });
        var again = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(input);

        Assert.Single(again.Items);
    }

    /// <summary>
    /// Description: Các dòng không hợp lệ đều bị từ chối: tên sai, nguồn lạ (kể cả now, lastTime, lastRunTime, startOfYear, partnerCode đã bỏ) hoặc để trống,
    ///              cách gán khác nguồn hệ thống và giá trị nhập
    /// Created date: 24/10/2026
    /// Modified date: 09/11/2026
    /// </summary>
    [Theory]
    [InlineData("1abc", "String", "Builtin", "lastKey")]
    [InlineData("since", "DateTime", "Builtin", "lastTime")]
    [InlineData("from date", "String", "Builtin", "lastKey")]
    [InlineData("yearStart", "DateTime", "Builtin", "tenant")]
    [InlineData("runAt", "DateTime", "Builtin", "now")]
    [InlineData("runAt", "DateTime", "Builtin", "")]
    [InlineData("prevRun", "DateTime", "Builtin", "lastRunTime")]
    [InlineData("yearStart", "DateTime", "Builtin", "startOfYear")]
    [InlineData("pc", "String", "Builtin", "partnerCode")]
    [InlineData("pageSize", "Int", "Const", "abc")]
    [InlineData("pageSize", "Int", "Const", "0")]
    [InlineData("pageSize", "String", "Builtin", "lastKey")]
    [InlineData("rate", "Decimal", "Const", "not-a-decimal")]
    [InlineData("tag", "String", "Const", "   ")]
    [InlineData("dt", "DateTime", "Const", "2026-01-01")]
    [InlineData("afterKey", "String", "Manual", "lastKey")]
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
            Items = [ConstVar("since", "String", "2026-01-01"), ConstVar("SINCE", "String", "2027-01-01")]
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
                Items = [ConstVar("yearStart", ShareDataConst.SqlVariableTypeString, "2026-01-01")]
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
    /// Description: Truy vấn nối đuôi gộp 1 cột (@lastKey đơn lẻ) ở cả mẫu chung và SQL riêng đối tác:
    ///              chấp nhận AS __lastKey kèm ORDER BY bí danh hoặc đúng biểu thức gộp ASC và loại __lastKey khỏi Fields;
    ///              từ chối khi thiếu AS __lastKey, thiếu ORDER BY, ORDER BY DESC hoặc ORDER BY khác biểu thức __lastKey.
    /// Created date: 27/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Theory]
    [InlineData("common", "alias-asc", true)]
    [InlineData("common", "expr-asc", true)]
    [InlineData("partner", "alias-asc", true)]
    [InlineData("common", "missing-alias", false)]
    [InlineData("common", "missing-orderby", false)]
    [InlineData("common", "orderby-desc", false)]
    [InlineData("partner", "orderby-other-col", false)]
    public async Task SaveExtractSql_WhenCompositeLastKey_EnforcesSingleCursorContract_Test(
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

    [Fact]
    public async Task UpdateExtractSql_WhenSqlColumnsChangedAndOldFieldEditsProvided_IgnoresRemovedFieldEditsAndSavesSuccessfully_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [ConstVar("pageSize", "int", "100")]
        });

        const string newSql = "SELECT TOP (@pageSize) ID AS eventId, Name AS speed, Remark AS eventTime FROM TmsZone";

        // Act: Gửi câu SQL mới kèm theo field edit của cột cũ không còn trong câu truy vấn mới
        var saved = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = newSql,
            Fields =
            [
                new SqlFieldEditInput { Key = "nonExistentOldColumn", Label = "Cột cũ", Required = true },
                new SqlFieldEditInput { Key = "eventId", Label = "Mã sự kiện", Required = true }
            ]
        });

        // Assert: Không bị ném lỗi SqlFieldKeyNotFound, trường cũ bị bỏ qua và trường mới nhận cấu hình
        Assert.Equal(["eventId", "speed", "eventTime"], saved.Fields.Select(f => f.Key));
        var eventId = saved.Fields.Single(f => f.Key == "eventId");
        Assert.Equal("Mã sự kiện", eventId.Label);
        Assert.True(eventId.Required);
    }

    [Fact]
    public async Task ActivityLogger_WhenSqlModified_LogsAccurateTargetDetailsWithPacketAndPartner_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_LOG_{unique}";
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Logging Test {unique}",
            Status = BaseEnums.StatusEnum.Enable,
        };
        var partner = new ShareDataPartner
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PT_LOG_{unique}",
            Name = $"Đối tác Logging Test {unique}",
            Status = BaseEnums.StatusEnum.Enable,
        };
        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT TOP 10 ID, Code, Name FROM TmsZone");

        // Act 1: Cập nhật SQL mẫu chung -> Ghi log chi tiết kèm mã gói tin
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            ExtractSql = "SELECT TOP 5 ID, Code, Name FROM TmsZone",
            Remark = "Cập nhật SQL mẫu chung qua API"
        });

        // Act 2: Cập nhật SQL riêng của đối tác -> Ghi log chi tiết kèm mã gói tin và tên đối tác
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "SELECT TOP 3 ID, Code FROM TmsZone"
        });

        // Assert 1: Kiểm tra log của SQL mẫu chung
        var commonLog = await db.Queryable<ShareDataActivityLog>()
            .Where(x => x.TargetId == packet.ID && x.DatatypeId == packet.ID && x.Action == BaseEnums.ActivityAction.Update)
            .OrderBy("OccurredAt DESC")
            .FirstAsync();
        Assert.NotNull(commonLog);
        Assert.Equal($"[{packetCode}] {packet.Name} (SQL mẫu chung)", commonLog.TargetName);
        Assert.Equal(packet.ID, commonLog.DatatypeId);
        Assert.Equal("Mẫu chung (tất cả đối tác)", commonLog.PartnerName);
        Assert.Contains($"[{packetCode}]", commonLog.Description);

        // Assert 2: Kiểm tra log của SQL riêng của đối tác
        var customLog = await db.Queryable<ShareDataActivityLog>()
            .Where(x => x.TargetId == partner.ID && x.DatatypeId == packet.ID)
            .OrderBy("OccurredAt DESC")
            .FirstAsync();
        Assert.NotNull(customLog);
        Assert.Equal($"[{packetCode}] {packet.Name} (SQL riêng)", customLog.TargetName);
        Assert.Equal(partner.Name, customLog.PartnerName);
        Assert.Equal(packet.ID, customLog.DatatypeId);
        Assert.Contains($"[{packetCode}]", customLog.Description);
        Assert.Contains(partner.Name, customLog.Description);
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

    /// <summary>Câu nối đuôi một chuỗi ghép CreateTime + ID trên TmsZone.</summary>
    private const string CursorCreateTimeSql = "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), CreateTime, 121) + '_' + ID AS __lastKey FROM TmsZone "
        + "WHERE CONVERT(varchar(23), CreateTime, 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC";

    /// <summary>Cùng công thức mốc với CursorCreateTimeSql, chỉ đổi TOP và thêm bí danh bảng.</summary>
    private const string CursorSameFormulaSql = "SELECT TOP 20 z.ID AS zoneId, CONVERT(varchar(23), z.CreateTime, 121) + '_' + z.ID AS __lastKey FROM TmsZone z "
        + "WHERE CONVERT(varchar(23), z.CreateTime, 121) + '_' + z.ID > @lastKey ORDER BY __lastKey ASC";

    /// <summary>Đổi công thức mốc: ghép COALESCE(UpdateTime, CreateTime) + ID thay cho CreateTime + ID.</summary>
    private const string CursorKeyOnlySql = "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone "
        + "WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey ORDER BY __lastKey";

    /// <summary>Câu một chuỗi ghép từ UpdateTime trơn: dòng mới thêm chưa có UpdateTime nên không được gửi.</summary>
    private const string InclusiveKeySql = "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone "
        + "WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID >= @lastKey ORDER BY __lastKey ASC";

    private const string BareUpdateTimeSql = "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey FROM TmsZone "
        + "WHERE CONVERT(varchar(23), UpdateTime, 121) + '_' + ID > @lastKey ORDER BY __lastKey";

    private async Task SeedCursorAsync(ISqlSugarClient db, ShareDataPartner partner, ShareDataPacket packet, string unique) =>
        await db.Insertable(new ShareDataLastSend
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            LastTime = new DateTime(2026, 10, 28, 8, 10, 0),
            LastKey = $"k2_{unique}"
        }).ExecuteCommandAsync();

    private static async Task<bool> HasCursorAsync(ISqlSugarClient db, ShareDataPartner partner, ShareDataPacket packet) =>
        await db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packet.Code).AnyAsync();

    /// <summary>
    /// Description: Lưu câu mẫu chung đổi công thức cột mốc (ghép CreateTime sang ghép UpdateTime) khi đối tác đang dùng mẫu chung có mốc nối đuôi:
    ///              lần đầu không lưu, trả RequiresConfirm kèm đối tác sẽ bị đặt lại mốc; gửi lại với Force thì lưu và xóa mốc của đối tác
    ///              (lượt gửi kế tiếp chạy lại từ mốc khởi tạo). Sửa câu nhưng giữ công thức mốc (đổi TOP, thêm bí danh bảng) thì lưu luôn, giữ mốc.
    /// Created date: 29/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Fact]
    public async Task UpdateExtractSql_WhenCursorFormulaChanges_RequiresConfirmThenResetsPartnerCursorWithForce_Test()
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
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await SeedCursorAsync(db, partner, packet, unique);

        // Act
        var sameFormula = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorSameFormulaSql });
        var pending = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorKeyOnlySql });
        var sqlAfterPending = (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql;
        var cursorAfterPending = await HasCursorAsync(db, partner, packet);
        var forced = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorKeyOnlySql, Force = true });

        // Assert
        Assert.False(sameFormula.RequiresConfirm);
        Assert.True(pending.RequiresConfirm);
        Assert.Single(pending.CursorResetPartners, p => p.PartnerId == partner.ID);
        Assert.Equal(CursorSameFormulaSql, sqlAfterPending);
        Assert.True(cursorAfterPending);
        Assert.False(forced.RequiresConfirm);
        Assert.Equal(CursorKeyOnlySql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
        Assert.False(await HasCursorAsync(db, partner, packet));
    }

    /// <summary>
    /// Description: Đối tác dùng mẫu chung mới có dòng ShareDataLastSend do Worker tạo ở lượt đầu nhưng chưa gửi được dòng nào (LastKey trống):
    ///              lưu câu mẫu chung đổi công thức cột mốc thì lưu luôn, không hỏi xác nhận vì đối tác chưa có mốc để đặt lại.
    /// Created date: 03/11/2026
    /// </summary>
    [Fact]
    public async Task UpdateExtractSql_WhenPartnerRowHasNoCursorKey_SavesFormulaChangeWithoutConfirm_Test()
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
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await db.Insertable(new ShareDataLastSend
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            LastTime = new DateTime(2026, 10, 28, 8, 10, 0),
            CursorSignature = SqlValidatorHelper.GetCursorSignature(CursorCreateTimeSql)
        }).ExecuteCommandAsync();

        // Act
        var saved = await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorKeyOnlySql });

        // Assert
        Assert.False(saved.RequiresConfirm);
        Assert.Empty(saved.CursorResetPartners);
        Assert.Equal(CursorKeyOnlySql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
    }

    /// <summary>
    /// Description: Đối tác tách cấu hình riêng (cùng công thức mốc với mẫu chung thì không hỏi), sau đó lưu câu riêng đổi công thức cột mốc:
    ///              lần đầu trả RequiresConfirm kèm đúng đối tác đó; gửi lại với Force thì chỉ xóa mốc của đối tác đó, đối tác khác dùng mẫu chung giữ mốc.
    /// Created date: 29/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Fact]
    public async Task UpdatePartnerExtractSql_WhenCursorFormulaChanges_RequiresConfirmThenResetsOnlyThatPartnerWithForce_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        var otherPartner = NewPartnerSqlPartner($"PT_VAR2_{unique}", unique);
        await db.Insertable(otherPartner).ExecuteCommandAsync();
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await SeedCursorAsync(db, partner, packet, unique);
        await SeedCursorAsync(db, otherPartner, packet, unique);

        // Act
        var created = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID, PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        var pending = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID, PacketCode = packet.Code!, ExtractSql = CursorKeyOnlySql });
        var cursorAfterPending = await HasCursorAsync(db, partner, packet);
        var forced = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID, PacketCode = packet.Code!, ExtractSql = CursorKeyOnlySql, Force = true });

        // Assert
        Assert.False(created.RequiresConfirm);
        Assert.True(pending.RequiresConfirm);
        Assert.Single(pending.CursorResetPartners, p => p.PartnerId == partner.ID);
        Assert.True(cursorAfterPending);
        Assert.False(forced.RequiresConfirm);
        Assert.Equal(CursorKeyOnlySql, forced.CustomSql);
        Assert.False(await HasCursorAsync(db, partner, packet));
        Assert.True(await HasCursorAsync(db, otherPartner, packet));
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Tách cấu hình riêng mà câu riêng đọc bảng nguồn khác, giữ nguyên công thức cột mốc: mốc đang chạy của đối tác tính theo bảng cũ
    ///              nên lần đầu không tạo câu riêng, trả RequiresConfirm kèm đối tác đó; gửi lại với Force thì tạo câu riêng và xóa mốc của đối tác.
    /// Created date: 06/11/2026
    /// </summary>
    [Fact]
    public async Task UpdatePartnerExtractSql_WhenSplitReadsOtherSourceTable_RequiresConfirmThenResetsCursorWithForce_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [BuiltinVar("lastKey", "", "lastKey")] });
        await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await SeedCursorAsync(db, partner, packet, unique);
        const string key = "CONVERT(varchar(23), CreateTime, 121) + '_' + ID";
        const string otherTableSql = $"SELECT TOP 10 ID AS statusId, {key} AS __lastKey FROM TmsZoneStatus WHERE {key} > @lastKey ORDER BY __lastKey ASC";

        // Act
        var pending = await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID, PacketCode = packet.Code!, ExtractSql = otherTableSql });
        var customAfterPending = await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).AnyAsync();
        var cursorAfterPending = await HasCursorAsync(db, partner, packet);
        var forced = await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID, PacketCode = packet.Code!, ExtractSql = otherTableSql, Force = true });

        // Assert
        Assert.True(pending.RequiresConfirm);
        Assert.Single(pending.CursorResetPartners, p => p.PartnerId == partner.ID);
        Assert.False(customAfterPending);
        Assert.True(cursorAfterPending);
        Assert.False(forced.RequiresConfirm);
        Assert.Equal(otherTableSql, forced.CustomSql);
        Assert.False(await HasCursorAsync(db, partner, packet));
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đổi công thức ở mẫu chung chỉ ảnh hưởng đối tác dùng mẫu chung và đang có mốc: hộp xác nhận nêu đúng đối tác đó, đồng ý thì xóa mốc của đối tác đó;
    ///              đối tác dùng mẫu chung chưa có mốc không bị hỏi, đối tác có cấu hình riêng giữ mốc và câu riêng.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task UpdateExtractSql_WhenCommonFormulaChanges_OnlyPartnersOnCommonWithCursorAreReset_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, onCommon) = await SeedSqlVariablePacketAsync(db, unique);
        var noCursor = NewPartnerSqlPartner($"PT_NOCUR_{unique}", unique);
        var custom = NewPartnerSqlPartner($"PT_CUSTOM_{unique}", unique);
        await db.Insertable(new List<ShareDataPartner> { noCursor, custom }).ExecuteCommandAsync();
        await services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [BuiltinVar("lastKey", "", "lastKey")] });
        await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = custom.ID!, PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await SeedCursorAsync(db, onCommon, packet, unique);
        await SeedCursorAsync(db, custom, packet, unique);

        // Act
        var pending = await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorKeyOnlySql });
        await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorKeyOnlySql, Force = true });

        // Assert
        var resetPartner = Assert.Single(pending.CursorResetPartners);
        var onCommonHasCursor = await HasCursorAsync(db, onCommon, packet);
        var customHasCursor = await HasCursorAsync(db, custom, packet);
        var customRow = await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == custom.ID).FirstAsync();
        Assert.True(pending.RequiresConfirm);
        Assert.Equal(onCommon.ID, resetPartner.PartnerId);
        Assert.False(onCommonHasCursor);
        Assert.True(customHasCursor);
        Assert.Equal(CursorCreateTimeSql, customRow.ExtractSql);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Sửa câu mẫu chung mà công thức cột mốc giữ nguyên thì không hỏi, không ai bị đặt lại mốc: thêm cột, thêm JOIN, đổi bí danh bảng,
    ///              đổi khoảng trắng, chú thích, hoa thường, viết lại công thức một lần trong câu con. Đổi từ câu một nhóm sang câu hai nhóm, đổi công thức
    ///              viết trong câu con, đổi bảng nguồn mà giữ công thức, hoặc lấy cột mốc từ bảng JOIN khác là đổi cột mốc nên hỏi xác nhận, đồng ý thì mốc cũ bị xóa.
    /// Created date: 05/11/2026
    /// Modified date: 06/11/2026
    /// </summary>
    [Theory]
    [InlineData("themCot", false)]
    [InlineData("themJoin", false)]
    [InlineData("doiBiDanh", false)]
    [InlineData("doiKhoangTrangChuThichHoaThuong", false)]
    [InlineData("vietLaiCauCon", false)]
    [InlineData("sangHaiNhom", true)]
    [InlineData("doiCongThucTrongCauCon", true)]
    [InlineData("doiBangNguon", true)]
    [InlineData("cotMocTuBangJoin", true)]
    public async Task UpdateExtractSql_WhenSqlEditedOrSplitIntoGroups_ConfirmsOnlyFormulaChange_Test(string edit, bool expectConfirm)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [BuiltinVar("lastKey", "", "lastKey")] });
        await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await SeedCursorAsync(db, partner, packet, unique);
        const string key = "CONVERT(varchar(23), CreateTime, 121) + '_' + ID";
        const string aliasKey = "CONVERT(varchar(23), z.CreateTime, 121) + '_' + z.ID";
        const string updateKey = "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID";
        var renamedKey = aliasKey.Replace("z.", "k.");
        var joinedKey = aliasKey.Replace("z.", "s.");
        var sql = edit switch
        {
            "themCot" => $"SELECT TOP 10 ID AS zoneId, Code AS zoneCode, {key} AS __lastKey FROM TmsZone WHERE {key} > @lastKey ORDER BY __lastKey ASC",
            "themJoin" => $"SELECT TOP 10 z.ID AS zoneId, s.Condition AS trafficCondition, {aliasKey} AS __lastKey FROM TmsZone z LEFT JOIN TmsZoneStatus s ON s.ZoneId = z.ID WHERE {aliasKey} > @lastKey ORDER BY __lastKey ASC",
            "doiBiDanh" => $"SELECT TOP 10 k.ID AS zoneId, {renamedKey} AS __lastKey FROM TmsZone k WHERE {renamedKey} > @lastKey ORDER BY __lastKey ASC",
            "doiKhoangTrangChuThichHoaThuong" => "SELECT TOP 10 ID AS zoneId, convert( varchar(23),  CreateTime , 121 ) /* mốc */ + '_' + id AS __lastKey FROM TmsZone "
                + "WHERE convert( varchar(23), CreateTime, 121 ) + '_' + id > @lastKey ORDER BY __lastKey ASC",
            "vietLaiCauCon" => $"SELECT TOP 10 x.zoneId, x.__lastKey FROM (SELECT ID AS zoneId, {key} AS __lastKey FROM TmsZone) x WHERE x.__lastKey > @lastKey ORDER BY x.__lastKey ASC",
            "doiCongThucTrongCauCon" => $"SELECT TOP 10 x.zoneId, x.__lastKey FROM (SELECT ID AS zoneId, {updateKey} AS __lastKey FROM TmsZone) x WHERE x.__lastKey > @lastKey ORDER BY x.__lastKey ASC",
            "doiBangNguon" => $"SELECT TOP 10 ID AS statusId, {key} AS __lastKey FROM TmsZoneStatus WHERE {key} > @lastKey ORDER BY __lastKey ASC",
            "cotMocTuBangJoin" => $"SELECT TOP 10 z.ID AS zoneId, {joinedKey} AS __lastKey FROM TmsZone z JOIN TmsZoneStatus s ON s.ZoneId = z.ID WHERE {joinedKey} > @lastKey ORDER BY __lastKey ASC",
            _ => "SELECT TOP 10 u.zoneId, u.__lastKey_upd, u.__lastKey_new FROM ("
                + "SELECT ID AS zoneId, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS sortKey, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey_upd, CAST(NULL AS nvarchar(100)) AS __lastKey_new FROM TmsZone "
                + "WHERE CONVERT(varchar(23), UpdateTime, 121) + '_' + ID > @lastKey_upd "
                + "UNION ALL SELECT ID, CONVERT(varchar(23), CreateTime, 121) + '_' + ID, NULL, CONVERT(varchar(23), CreateTime, 121) + '_' + ID FROM TmsZone "
                + "WHERE UpdateTime IS NULL AND CONVERT(varchar(23), CreateTime, 121) + '_' + ID > @lastKey_new"
                + ") u ORDER BY u.sortKey ASC"
        };
        List<PacketSqlVariableItemInput>? variables = edit == "sangHaiNhom"
            ? [BuiltinVar("lastKey_upd", "", "lastKey"), BuiltinVar("lastKey_new", "", "lastKey")]
            : null;

        // Act
        var first = await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql, Variables = variables });
        var cursorAfterFirst = await HasCursorAsync(db, partner, packet);
        if (expectConfirm)
        {
            await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql, Variables = variables, Force = true });
        }

        // Assert
        var common = await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        var hasCursor = await HasCursorAsync(db, partner, packet);
        Assert.Equal(expectConfirm, first.RequiresConfirm);
        Assert.True(cursorAfterFirst);
        Assert.Equal(sql, common?.ExtractSql);
        Assert.Equal(!expectConfirm, hasCursor);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Dùng lại mẫu chung: cùng công thức cột mốc thì không hỏi, mốc đi theo; khác công thức thì hỏi xác nhận, chưa đồng ý thì giữ câu riêng và mốc,
    ///              đồng ý thì xóa mốc và giờ chạy lần trước để gửi lại từ Mốc lần đầu của mẫu chung. Mẫu chung là câu gửi toàn bộ thì không hỏi, mốc còn trong CSDL nhưng câu không dùng tới.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(CursorSameFormulaSql, false, true)]
    [InlineData(CursorKeyOnlySql, true, false)]
    [InlineData("SELECT ID AS zoneId FROM TmsZone", false, true)]
    public async Task ResetPartnerExtractSql_WhenCommonFormulaVaries_ConfirmsOnlyFormulaChange_Test(string commonSql, bool expectConfirm, bool cursorKept)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, commonSql);
        await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packet.Code!,
            ExtractSql = CursorCreateTimeSql,
            Variables = [BuiltinVar("lastKey", "", "lastKey")]
        });
        await SeedCursorAsync(db, partner, packet, unique);
        var subscription = NewPartnerSqlSubscription(partner, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Paused);
        subscription.LastTimeRun = DateTime.Now.AddHours(-1);
        await db.Insertable(subscription).ExecuteCommandAsync();
        var reset = new ResetPartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code! };
        Task<bool> HasPartnerRow() => db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).AnyAsync();

        // Act
        var first = await services.CallApiAsync<PartnerExtractSqlOutput>(reset);
        var rowAfterFirst = await HasPartnerRow();
        var cursorAfterFirst = await HasCursorAsync(db, partner, packet);
        if (expectConfirm)
        {
            await services.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, Force = true });
        }

        // Assert
        var rowAtEnd = await HasPartnerRow();
        var hasCursor = await HasCursorAsync(db, partner, packet);
        var savedSubscription = await db.Queryable<ShareDataSubscription>().InSingleAsync(subscription.ID);
        Assert.Equal(expectConfirm, first.RequiresConfirm);
        Assert.Equal(expectConfirm, rowAfterFirst);
        Assert.True(cursorAfterFirst || !expectConfirm);
        Assert.False(rowAtEnd);
        Assert.Equal(cursorKept, hasCursor);
        Assert.Equal(cursorKept, savedSubscription.LastTimeRun != null);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đăng ký đang bật khóa câu SQL, bảng tham số và mốc đúng phạm vi (ESH-2306), tắt đăng ký thì làm được:
    ///              tab Mẫu chung khóa khi còn bất kỳ đăng ký gửi nào của gói đang bật, kể cả đăng ký hai chiều và đối tác có cấu hình riêng; đăng ký chiều nhận không khóa.
    ///              Tab đối tác chỉ khóa theo đăng ký gửi của đúng đối tác đó. Tắt gói thì còn đăng ký bật ở bất kỳ chiều nào cũng bị chặn. Bị từ chối thì dữ liệu giữ nguyên.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("commonSql", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("commonSql", false, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("commonSql", true, BaseEnums.Direction.Both, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("commonSql", true, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Active, null)]
    [InlineData("commonSql", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Paused, null)]
    [InlineData("commonVariables", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("commonVariables", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Paused, null)]
    [InlineData("partnerSql", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("partnerSql", false, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, null)]
    [InlineData("partnerVariables", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("partnerVariables", false, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, null)]
    [InlineData("resetPartnerSql", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("resetPartnerSql", false, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, null)]
    [InlineData("resetCursor", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("resetCursor", false, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, null)]
    [InlineData("disablePacket", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketDisableInUse)]
    [InlineData("disablePacket", true, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketDisableInUse)]
    [InlineData("disablePacket", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Paused, null)]
    public async Task SqlConfig_WhenSubscriptionActive_LocksOnlyItsScope_Test(string operation, bool ownSubscription, BaseEnums.Direction direction, BaseEnums.SubSubscriptionState state, string? errorKey)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        var other = NewPartnerSqlPartner($"PT_OTHER_{unique}", unique);
        await db.Insertable(other).ExecuteCommandAsync();
        await services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [BuiltinVar("lastKey", "", "lastKey")] });
        await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = CursorCreateTimeSql });
        await SeedCursorAsync(db, partner, packet, unique);
        var subscription = NewPartnerSqlSubscription(ownSubscription ? partner : other, packet, direction, state);
        await db.Insertable(subscription).ExecuteCommandAsync();

        const string newStart = "2026-11-05 08:00:00.000_";
        var variableItems = new List<PacketSqlVariableItemInput>
        {
            new() { Code = "lastKey", ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastKey, DefaultValue = newStart }
        };
        Task Act() => operation switch
        {
            "commonSql" => services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorSameFormulaSql }),
            "commonVariables" => services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = variableItems }),
            "partnerSql" => services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = CursorSameFormulaSql }),
            "partnerVariables" => services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID, Items = variableItems }),
            "resetPartnerSql" => services.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code! }),
            "resetCursor" => services.CallApiAsync(new ResetLastSendInput { PacketCode = packet.Code!, PartnerId = partner.ID! }),
            _ => services.CallApiAsync(new ShareDataUpdatePacketInput { ID = packet.ID, Code = packet.Code, Name = packet.Name, OrderNo = 101, Status = BaseEnums.StatusEnum.Disable })
        };
        Task<ShareDataSql> PartnerRow() => db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).FirstAsync();

        // Act
        var error = await Record.ExceptionAsync(Act);

        // Assert
        var changed = operation switch
        {
            "commonSql" => (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql == CursorSameFormulaSql,
            "commonVariables" => (await ShareDataSqlSeed.GetVariablesAsync(db, packet.ID, null)).Any(v => v.DefaultValue == newStart),
            "partnerSql" => (await PartnerRow()).ExtractSql == CursorSameFormulaSql,
            "partnerVariables" => (await ShareDataSqlSeed.GetVariablesAsync(db, packet.ID, partner.ID)).Any(v => v.DefaultValue == newStart),
            "resetPartnerSql" => await PartnerRow() == null,
            "resetCursor" => !await HasCursorAsync(db, partner, packet),
            _ => (await db.Queryable<ShareDataPacket>().InSingleAsync(packet.ID)).Status == BaseEnums.StatusEnum.Disable
        };
        if (errorKey == null)
        {
            Assert.Null(error);
            Assert.True(changed);
        }
        else
        {
            AssertRejectedWith(error, errorKey);
            Assert.False(changed);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đúng các lời gọi của trang Mốc gửi: nạp gói với cỡ trang 500, liệt kê đối tác kèm nhãn mẫu chung hoặc cấu hình riêng, đọc mốc từng đối tác;
    ///              đặt mốc và đặt lại chỉ đổi mốc của đúng đối tác đó.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task CursorPage_WhenLoadedThenSetAndReset_ShowsEachPartnerOwnCursor_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, onCommon) = await SeedSqlVariablePacketAsync(db, unique);
        var custom = NewPartnerSqlPartner($"P_CUS_{unique}", unique);
        await db.Insertable(custom).ExecuteCommandAsync();
        await db.Insertable(NewPartnerSqlSubscription(onCommon, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Paused)).ExecuteCommandAsync();
        await services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [BuiltinVar("lastKey", "", "lastKey")] });
        await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorKeyOnlySql });
        await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = custom.ID!,
            PacketCode = packet.Code!,
            ExtractSql = CursorCreateTimeSql,
            Variables = [BuiltinVar("lastKey", "", "lastKey")]
        });
        async Task<string?> SavedValue(ShareDataPartner partner)
        {
            var variables = await services.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID });
            return variables.Items.Single(i => i.Code == "lastKey").SavedValue;
        }

        // Act
        var packetPage = await services.CallApiAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(
            new ShareDataPagePacketInput { Page = 1, PageSize = 500, Field = "orderNo", Order = "asc", DescStr = "desc", Code = packet.Code });
        var partners = await services.CallApiAsync<List<PartnerSqlListItemOutput>>(new GetPartnerSqlListInput { PacketCode = packet.Code! });
        var key = "2026-11-05 08:00:00.000_";
        await services.CallApiAsync(new SetLastSendInput { PacketCode = packet.Code!, PartnerId = onCommon.ID, LastKey = key });
        var commonAfterSet = await SavedValue(onCommon);
        var customAfterSet = await SavedValue(custom);
        await services.CallApiAsync(new ResetLastSendInput { PacketCode = packet.Code!, PartnerId = onCommon.ID! });
        var commonAfterReset = await SavedValue(onCommon);

        // Assert
        var pagedPacket = Assert.Single(packetPage.Records);
        var commonRow = partners.Single(p => p.PartnerId == onCommon.ID);
        var customRow = partners.Single(p => p.PartnerId == custom.ID);
        Assert.Equal(packet.ID, pagedPacket.ID);
        Assert.False(commonRow.IsCustom);
        Assert.True(customRow.IsCustom);
        Assert.Equal(key, commonAfterSet);
        Assert.Null(customAfterSet);
        Assert.Null(commonAfterReset);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đặt mốc theo nhóm: nhóm câu đang chạy không có thì từ chối, không ghi gì; Đặt lại xóa mốc của mọi nhóm cùng giờ chạy lần trước.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task SetAndResetLastSend_WhenCursorGroups_RejectsUnknownGroupAndClearsEveryGroup_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("lastKey_a", "", "lastKey"), BuiltinVar("lastKey_b", "", "lastKey")]
        });
        await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = CursorGroupSql(true, CursorGroupWhereB) });
        var subscription = NewPartnerSqlSubscription(partner, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Paused);
        subscription.LastTimeRun = DateTime.Now.AddHours(-1);
        await db.Insertable(subscription).ExecuteCommandAsync();
        Task SetGroup(string group, string key) => services.CallApiAsync(new SetLastSendInput { PacketCode = packet.Code!, PartnerId = partner.ID, CursorKey = group, LastKey = key });
        Task<List<ShareDataLastSend>> Cursors() => db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packet.Code).ToListAsync();

        // Act
        await SetGroup("a", "k_a");
        await SetGroup("b", "k_b");
        var unknown = await Record.ExceptionAsync(() => SetGroup("khongCo", "k_x"));
        var cursorsAfterSet = await Cursors();
        await db.Updateable<ShareDataSubscription>().SetColumns(s => s.LastTimeRun == DateTime.Now).Where(s => s.ID == subscription.ID).ExecuteCommandAsync();
        await services.CallApiAsync(new ResetLastSendInput { PacketCode = packet.Code!, PartnerId = partner.ID! });

        // Assert
        var groups = cursorsAfterSet.Select(c => c.CursorKey).OrderBy(c => c).ToArray();
        var groupA = cursorsAfterSet.Single(c => c.CursorKey == "a");
        var cursorsAfterReset = await Cursors();
        var savedSubscription = await db.Queryable<ShareDataSubscription>().InSingleAsync(subscription.ID);
        AssertRejectedWith(unknown, BaseMsg.ShareData.Validation.SqlIncrementalGroupNotFound);
        Assert.Equal(["a", "b"], groups);
        Assert.Equal("k_a", groupA.LastKey);
        Assert.Empty(cursorsAfterReset);
        Assert.Null(savedSubscription.LastTimeRun);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Danh mục bảng nguồn chưa có dữ liệu: màn hình nhận danh sách rỗng, và mọi câu SQL bị từ chối vì bảng không nằm trong danh sách được phép đọc, không có chuyện đọc được mọi bảng.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task SourceCatalog_WhenEmpty_ReturnsNoTablesAndRejectsSql_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        var catalogIds = await db.Queryable<ShareDataSourceTable>().Where(t => t.IsDelete == null).Select(t => t.ID).ToListAsync();
        var deletedAt = DateTime.Now;
        await db.Updateable<ShareDataSourceTable>().SetColumns(t => t.IsDelete == deletedAt).Where(t => catalogIds.Contains(t.ID)).ExecuteCommandAsync();

        // Act
        var tables = await services.CallApiAsync<List<MetadataTableDto>>(new GetMetadataTablesInput());
        var error = await Record.ExceptionAsync(() => services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = "SELECT ID AS zoneId FROM TmsZone" }));
        await db.Updateable<ShareDataSourceTable>().SetColumns(t => t.IsDelete == null).Where(t => catalogIds.Contains(t.ID)).ExecuteCommandAsync();

        // Assert
        Assert.Empty(tables);
        AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlTableNotAllowed);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Các mẫu câu ở mục 5.8 Mẫu câu của tài liệu bàn giao, đổi bảng giữ chỗ sang bảng được phép đọc, dán vào là lưu được:
    ///              câu mẫu, cột mốc gộp, lọc 7 ngày gần nhất bằng GETDATE(), hai nhóm gói 101, nhiều nhóm gộp hai bảng.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("cauMau")]
    [InlineData("cotMocGop")]
    [InlineData("bayNgay")]
    [InlineData("haiNhom101")]
    [InlineData("nhieuNhom")]
    public async Task SaveExtractSql_WhenHandbookSample_SavesWithDetectedVariables_Test(string sample)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        var pageSize = ConstVar("pageSize", ShareDataConst.SqlVariableTypeInt, "100");
        var (sql, variables) = sample switch
        {
            "cauMau" => ("""
                SELECT TOP (@pageSize)                  -- số dòng mỗi trang, khai ở bảng tham số
                       zs.ZoneId    AS zoneId,
                       zs.Condition AS trafficCondition,
                       CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID AS __lastKey   -- thời gian + khóa duy nhất
                FROM   TmsZoneStatus zs
                WHERE  CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID > @lastKey
                ORDER BY __lastKey ASC
                """, new List<PacketSqlVariableItemInput> { pageSize, BuiltinVar("lastKey", "", "lastKey") }),
            "cotMocGop" => ("""
                SELECT TOP (@pageSize)
                       t.Code AS code,
                       CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID AS __lastKey
                FROM   TmsZone t
                WHERE  CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID > @lastKey
                ORDER BY __lastKey ASC
                """, [pageSize, BuiltinVar("lastKey", "", "lastKey")]),
            "bayNgay" => ("""
                SELECT TOP (@pageSize)
                       t.Code AS code,
                       CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID AS __lastKey
                FROM   TmsZone t
                WHERE  CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID > @lastKey
                  AND COALESCE(t.UpdateTime, t.CreateTime) >= DATEADD(DAY, -7, GETDATE())
                ORDER BY __lastKey ASC
                """, [pageSize, BuiltinVar("lastKey", "", "lastKey")]),
            "haiNhom101" => ("""
                SELECT TOP (@pageSize)
                    u.__lastKey_upd, u.__lastKey_new,
                    u.zoneStatusId, u.zoneId, u.zoneName, u.fromLocationKm, u.fromLocationMet, u.toLocationKm, u.toLocationMet,
                    u.laneId, u.averageSpeed, u.trafficCondition, u.dataTime, u.speedLimit, u.vehicleCount
                FROM (
                    SELECT
                        CONVERT(varchar(23), zs.UpdateTime, 121) + '_' + zs.ID AS sortKey,
                        CONVERT(varchar(23), zs.UpdateTime, 121) + '_' + zs.ID AS __lastKey_upd,
                        CAST(NULL AS NVARCHAR(100)) AS __lastKey_new,
                        zs.ID AS zoneStatusId, zs.ZoneId AS zoneId, z.Name AS zoneName,
                        z.FromKmNumber AS fromLocationKm, z.FromMetNumber AS fromLocationMet,
                        z.ToKmNumber AS toLocationKm, z.ToMetNumber AS toLocationMet, z.LaneId AS laneId,
                        CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed, zs.Condition AS trafficCondition,
                        zs.UpdateTime AS dataTime, z.MaxSpeed AS speedLimit, ts.TotalVehicleNumber AS vehicleCount
                    FROM dbo.TmsZoneStatus zs
                    LEFT JOIN dbo.TmsZone z ON zs.ZoneId = z.ID
                    LEFT JOIN dbo.TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId
                    WHERE CONVERT(varchar(23), zs.UpdateTime, 121) + '_' + zs.ID > @lastKey_upd
                    UNION ALL
                    SELECT
                        CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID, NULL, CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID,
                        zs.ID, zs.ZoneId, z.Name, z.FromKmNumber, z.FromMetNumber, z.ToKmNumber, z.ToMetNumber,
                        z.LaneId, CAST(zs.AverageSpeed AS DECIMAL(18, 2)), zs.Condition, zs.CreateTime, z.MaxSpeed, ts.TotalVehicleNumber
                    FROM dbo.TmsZoneStatus zs
                    LEFT JOIN dbo.TmsZone z ON zs.ZoneId = z.ID
                    LEFT JOIN dbo.TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId
                    WHERE zs.UpdateTime IS NULL
                      AND CONVERT(varchar(23), zs.CreateTime, 121) + '_' + zs.ID > @lastKey_new
                ) u
                ORDER BY u.sortKey ASC
                """, [pageSize, BuiltinVar("lastKey_upd", "", "lastKey"), BuiltinVar("lastKey_new", "", "lastKey")]),
            _ => ("""
                SELECT TOP (@pageSize) u.rowCode, u.__lastKey_inc, u.__lastKey_wea
                FROM (
                    SELECT i.Code AS rowCode,
                           CONVERT(varchar(23), COALESCE(i.UpdateTime, i.CreateTime), 121) + '_' + i.ID AS sortKey,
                           CONVERT(varchar(23), COALESCE(i.UpdateTime, i.CreateTime), 121) + '_' + i.ID AS __lastKey_inc,
                           CAST(NULL AS nvarchar(100)) AS __lastKey_wea
                    FROM TmsIncident i
                    WHERE CONVERT(varchar(23), COALESCE(i.UpdateTime, i.CreateTime), 121) + '_' + i.ID > @lastKey_inc
                    UNION ALL
                    SELECT w.RefId,
                           CONVERT(varchar(23), COALESCE(w.UpdateTime, w.CreateTime), 121) + '_' + w.ID,
                           NULL,
                           CONVERT(varchar(23), COALESCE(w.UpdateTime, w.CreateTime), 121) + '_' + w.ID
                    FROM TmsWeather w
                    WHERE CONVERT(varchar(23), COALESCE(w.UpdateTime, w.CreateTime), 121) + '_' + w.ID > @lastKey_wea
                ) u
                ORDER BY u.sortKey ASC
                """, [pageSize, BuiltinVar("lastKey_inc", "", "lastKey"), BuiltinVar("lastKey_wea", "", "lastKey")])
        };

        // Act
        var saved = await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql, Variables = variables });

        // Assert
        var row = await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        var savedVariables = await ShareDataSqlSeed.GetVariablesAsync(db, packet.ID, null);
        var expectedSql = sql.Trim();
        var expectedCodes = variables.Select(v => v.Code).ToArray();
        var savedCodes = savedVariables.Select(v => v.Code).ToArray();
        var fields = SqlFieldHelper.Read(row?.FieldsJson);
        Assert.False(saved.RequiresConfirm);
        Assert.Equal(expectedSql, row?.ExtractSql);
        Assert.Equal(expectedCodes, savedCodes);
        Assert.DoesNotContain(fields, f => f.Key.StartsWith("__", StringComparison.Ordinal));
    }

    /// <summary>Câu nối đuôi một chuỗi ghép COALESCE(UpdateTime, CreateTime) + ID trên TmsZone với điều kiện WHERE tùy ca.</summary>
    private static string CursorFilterSql(string where) => "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone "
        + $"WHERE {where} ORDER BY __lastKey ASC";

    /// <summary>Điều kiện lọc đúng của CursorFilterSql.</summary>
    private const string CursorFilterWhere = "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey";

    /// <summary>Câu gộp hai nguồn trên TmsZone (mã bắt đầu A và B), mỗi nguồn một nhóm mốc a, b; withKeyB = false bỏ cột __lastKey_b ở SELECT ngoài; orderBy là cột sắp ngoài cùng.</summary>
    private static string CursorGroupSql(bool withKeyB, string whereB, string orderBy = "u.sortKey") =>
        "SELECT TOP 10 u.zoneId, u.__lastKey_a" + (withKeyB ? ", u.__lastKey_b" : string.Empty) + " FROM ("
        + "SELECT z.ID AS zoneId, CONVERT(varchar(23), COALESCE(z.UpdateTime, z.CreateTime), 121) + '_' + z.ID AS sortKey, "
        + "CONVERT(varchar(23), COALESCE(z.UpdateTime, z.CreateTime), 121) + '_' + z.ID AS __lastKey_a, CAST(NULL AS nvarchar(100)) AS __lastKey_b FROM TmsZone z "
        + "WHERE z.Code LIKE 'A%' AND CONVERT(varchar(23), COALESCE(z.UpdateTime, z.CreateTime), 121) + '_' + z.ID > @lastKey_a "
        + "UNION ALL SELECT y.ID, CONVERT(varchar(23), COALESCE(y.UpdateTime, y.CreateTime), 121) + '_' + y.ID, NULL, CONVERT(varchar(23), COALESCE(y.UpdateTime, y.CreateTime), 121) + '_' + y.ID "
        + "FROM TmsZone y WHERE y.Code LIKE 'B%' AND " + whereB
        + ") u ORDER BY " + orderBy + " ASC";

    private const string CursorGroupWhereB = "CONVERT(varchar(23), COALESCE(y.UpdateTime, y.CreateTime), 121) + '_' + y.ID > @lastKey_b";

    /// <summary>
    /// Author: DatHQ
    /// Description: Thao tác bị từ chối đúng mã lỗi và câu dịch của mã đó hiện được ở cả hai đường.
    /// Created date: 30/10/2026
    /// Modified date: 05/11/2026
    /// </summary>
    private static void AssertRejectedWith(Exception? ex, string errorKey) => RejectionAssert.Rejected(ex, errorKey);

    /// <summary>
    /// Description: Các dạng câu nối đuôi làm mất dòng nên lưu bị từ chối:
    ///              - so cột khóa với hằng số, biến mốc chỉ để bật/tắt: lượt nào cũng lấy lại từ hằng số, có TOP thì kẹt ở trang đầu;
    ///              - WHERE so khác biểu thức cột mốc, ORDER BY không theo __lastKey hoặc sắp giảm dần;
    ///              - đặt tên cột mốc sai (__lastK): Worker không lấy được mốc.
    /// Created date: 30/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Theory]
    [InlineData("lastKey", "SELECT TOP 10 ID AS zoneId, ID AS __lastKey FROM TmsZone WHERE @lastKey IS NOT NULL AND ID > 'a' ORDER BY ID ASC", BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInvalid)]
    [InlineData("lastKey", "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey FROM TmsZone WHERE ID > @lastKey ORDER BY __lastKey ASC", BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInvalid)]
    [InlineData("lastKey", "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey FROM TmsZone WHERE CONVERT(varchar(23), CreateTime, 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC", BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInvalid)]
    [InlineData("lastKey", "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey FROM TmsZone WHERE CONVERT(varchar(23), UpdateTime, 121) + '_' + ID > @lastKey ORDER BY ID ASC", BaseMsg.ShareData.Validation.SqlIncrementalOrderByKeyMismatch)]
    [InlineData("lastKey", "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey FROM TmsZone WHERE CONVERT(varchar(23), UpdateTime, 121) + '_' + ID > @lastKey ORDER BY __lastKey DESC", BaseMsg.ShareData.Validation.SqlIncrementalOrderByDescNotAllowed)]
    [InlineData("lastKey", "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastK FROM TmsZone WHERE CONVERT(varchar(23), UpdateTime, 121) + '_' + ID > @lastKey ORDER BY __lastK ASC", BaseMsg.ShareData.Validation.SqlIncrementalMissingLastKey)]
    [InlineData("lastKey", "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey", BaseMsg.ShareData.Validation.SqlIncrementalMissingOrderBy)]
    [InlineData("lastKey", "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone WHERE __lastKey > @lastKey ORDER BY __lastKey ASC", BaseMsg.ShareData.Validation.SqlDatabaseExecutionError)]
    public async Task UpdateExtractSql_WhenCursorFilterLosesRowsAcrossPages_RejectsSql_Test(string cursorVariables, string sql, string errorKey)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [.. cursorVariables.Split(',').Select(item => BuiltinVar(item.Split(':')[0], "", item.Split(':')[^1]))]
        });

        // Act
        var error = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
            new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql }));

        // Assert
        AssertRejectedWith(error, errorKey);
        Assert.Null((await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cột mốc làm sót dòng bị từ chối khi lưu, ở cả mẫu chung lẫn SQL riêng của đối tác:
    ///              cột mốc chỉ có ngày giờ (UpdateTime AS __lastKey) sót các dòng trùng giờ; chuỗi mốc ghép từ UpdateTime trơn bỏ sót
    ///              dòng mới thêm vì UpdateTime trống khi thêm mới. Bọc COALESCE(UpdateTime, CreateTime) thì lưu được.
    ///              So đúng biểu thức cột mốc bằng >= cũng bị từ chối vì dòng cuối bị gửi lặp mỗi lượt.
    ///              Ghép ngày giờ không sắp đúng thứ tự thời gian bị từ chối.
    /// Created date: 04/11/2026
    /// Modified date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId, UpdateTime AS __lastKey FROM TmsZone WHERE UpdateTime > @lastKey ORDER BY __lastKey", BaseMsg.ShareData.Validation.SqlIncrementalKeyTimeOnly)]
    [InlineData(false, BareUpdateTimeSql, BaseMsg.ShareData.Validation.SqlIncrementalUpdateTimeEmpty)]
    [InlineData(true, BareUpdateTimeSql, BaseMsg.ShareData.Validation.SqlIncrementalUpdateTimeEmpty)]
    [InlineData(false, InclusiveKeySql, BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInclusive)]
    [InlineData(true, InclusiveKeySql, BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInclusive)]
    [InlineData(true, CursorKeyOnlySql, null)]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId, FORMAT(CreateTime, 'HHmm') + '_' + ID AS __lastKey FROM TmsZone WHERE FORMAT(CreateTime, 'HHmm') + '_' + ID > @lastKey ORDER BY __lastKey", BaseMsg.ShareData.Validation.SqlIncrementalKeyDateUnsortable)]
    [InlineData(true, "SELECT TOP 10 ID AS zoneId, FORMAT(CreateTime, 'dd/MM/yyyy') + '_' + ID AS __lastKey FROM TmsZone WHERE FORMAT(CreateTime, 'dd/MM/yyyy') + '_' + ID > @lastKey ORDER BY __lastKey", BaseMsg.ShareData.Validation.SqlIncrementalKeyDateUnsortable)]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId, FORMAT(CreateTime, 'yyyy-MMM-dd') + '_' + ID AS __lastKey FROM TmsZone WHERE FORMAT(CreateTime, 'yyyy-MMM-dd') + '_' + ID > @lastKey ORDER BY __lastKey", BaseMsg.ShareData.Validation.SqlIncrementalKeyDateUnsortable)]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(10), CreateTime, 103) + '_' + ID AS __lastKey FROM TmsZone WHERE CONVERT(varchar(10), CreateTime, 103) + '_' + ID > @lastKey ORDER BY __lastKey", BaseMsg.ShareData.Validation.SqlIncrementalKeyDateUnsortable)]
    public async Task SaveExtractSql_WhenCursorTimeMissesRows_RejectsSql_Test(bool partnerScope, string sql, string? errorKey)
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
            PartnerId = partnerScope ? partner.ID : null,
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });
        Task SaveSql() => partnerScope
            ? scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql })
            : scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql });

        // Act
        var error = await Record.ExceptionAsync(SaveSql);

        // Assert
        var row = partnerScope
            ? await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).FirstAsync()
            : await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        if (errorKey == null)
        {
            Assert.Null(error);
            Assert.Equal(sql, row?.ExtractSql);
        }
        else
        {
            AssertRejectedWith(error, errorKey);
            Assert.NotEqual(sql, row?.ExtractSql);
        }
    }

    /// <summary>
    /// Description: Câu đúng công thức PA1 lưu bình thường, không báo gì, kể cả có chặn dưới theo cột thời gian gốc dùng >=.
    ///              Cột mốc sai công thức bị từ chối, xem SaveExtractSql_WhenCursorColumnVaries_OnlyFormulaIsSaved_Test.
    /// Created date: 29/10/2026
    /// Modified date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(CursorKeyOnlySql)]
    [InlineData("SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone "
        + "WHERE COALESCE(UpdateTime, CreateTime) >= ISNULL(TRY_CONVERT(datetime, LEFT(@lastKey, 23), 121), '17530101') "
        + "AND CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey ORDER BY __lastKey")]
    public async Task UpdateExtractSql_WhenCursorFollowsFormula_SavesNormally_Test(string sql)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });

        // Act
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql });

        // Assert
        Assert.Equal(sql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Lọc theo giờ hiện tại bằng GETDATE() viết thẳng trong câu: lưu được cả câu gửi toàn bộ lẫn câu nối đuôi, không cần khai biến.
    /// Created date: 03/11/2026
    /// Modified date: 04/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, "SELECT ID AS zoneId FROM TmsZone WHERE UpdateTime >= DATEADD(day, -7, GETDATE())")]
    [InlineData(true, "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone "
        + "WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey AND UpdateTime >= DATEADD(day, -7, GETDATE()) ORDER BY __lastKey ASC")]
    public async Task UpdateExtractSql_WhenSqlFiltersByGetDate_SavesNormally_Test(bool withCursor, string sql)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        if (withCursor)
        {
            await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
            {
                PacketCode = packet.Code!,
                Items = [BuiltinVar("lastKey", "", "lastKey")]
            });
        }

        // Act
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql });

        // Assert
        Assert.Equal(sql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
    }

    /// <summary>
    /// Description: Câu một chuỗi ghép phủ cả dòng chưa có UpdateTime lưu được qua API mà không cần biến thời gian:
    ///              chuỗi ghép từ cột mốc gộp ISNULL(UpdateTime, CreateTime), và câu hai nhóm chỉ có @lastKey_&lt;nhóm&gt;.
    /// Created date: 03/11/2026
    /// </summary>
    [Theory]
    [InlineData("cotMocGop")]
    [InlineData("haiNhom")]
    public async Task UpdateExtractSql_WhenStringCursorCoversEmptyTime_SavesNormally_Test(string caseName)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = caseName == "haiNhom"
                ? [BuiltinVar("lastKey_upd", "", "lastKey"), BuiltinVar("lastKey_new", "", "lastKey")]
                : [BuiltinVar("lastKey", "", "lastKey")]
        });
        var sql = caseName == "haiNhom"
            ? "SELECT TOP 10 u.zoneId, u.__lastKey_upd, u.__lastKey_new FROM ("
                + "SELECT ID AS zoneId, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS sortKey, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey_upd, CAST(NULL AS nvarchar(100)) AS __lastKey_new FROM TmsZone "
                + "WHERE CONVERT(varchar(23), UpdateTime, 121) + '_' + ID > @lastKey_upd "
                + "UNION ALL SELECT ID, CONVERT(varchar(23), CreateTime, 121) + '_' + ID, NULL, CONVERT(varchar(23), CreateTime, 121) + '_' + ID FROM TmsZone "
                + "WHERE UpdateTime IS NULL AND CONVERT(varchar(23), CreateTime, 121) + '_' + ID > @lastKey_new"
                + ") u ORDER BY u.sortKey ASC"
            : "SELECT TOP 10 ID AS zoneId, CONVERT(varchar(23), ISNULL(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone "
                + "WHERE CONVERT(varchar(23), ISNULL(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC";

        // Act
        await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql });

        // Assert
        Assert.Equal(sql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
    }

    /// <summary>
    /// Description: Câu gộp hai nguồn, mỗi nguồn một nhóm mốc (cột __lastKey_&lt;nhóm&gt;, biến @lastKey_&lt;nhóm&gt;):
    ///              câu đủ cột, lọc đúng từng nhóm và sắp ngoài cùng theo cột cùng công thức cột mốc ở mỗi nhánh (sortKey) thì lưu được;
    ///              thiếu cột mốc của một nhóm, nhóm so khác biểu thức cột mốc, hay sắp theo cột khác (mã dòng, cột mốc của riêng một nhóm)
    ///              làm dòng của một nguồn không ra đúng thứ tự mốc nên bị từ chối đúng mã lỗi.
    /// Created date: 29/10/2026
    /// Modified date: 04/11/2026
    /// </summary>
    [Theory]
    [InlineData(true, CursorGroupWhereB, "u.sortKey", null)]
    [InlineData(false, CursorGroupWhereB, "u.sortKey", BaseMsg.ShareData.Validation.SqlIncrementalGroupColumnMissing)]
    [InlineData(true, "y.ID > @lastKey_b", "u.sortKey", BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInvalid)]
    [InlineData(true, "CONVERT(varchar(23), COALESCE(y.UpdateTime, y.CreateTime), 121) + '_' + y.ID >= @lastKey_b", "u.sortKey", BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInclusive)]
    [InlineData(true, CursorGroupWhereB, "u.zoneId", BaseMsg.ShareData.Validation.SqlIncrementalGroupOrderInvalid)]
    [InlineData(true, CursorGroupWhereB, "u.__lastKey_a", BaseMsg.ShareData.Validation.SqlIncrementalGroupOrderInvalid)]
    public async Task UpdateExtractSql_WhenSqlMergesSourcesWithCursorGroups_ValidatesEachGroup_Test(bool withKeyB, string whereB, string orderBy, string? errorKey)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("lastKey_a", "", "lastKey"), BuiltinVar("lastKey_b", "", "lastKey")]
        });
        var sql = CursorGroupSql(withKeyB, whereB, orderBy);

        // Act
        var error = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
            new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql }));

        // Assert
        if (errorKey == null)
        {
            Assert.Null(error);
            Assert.Equal(sql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
        }
        else
        {
            AssertRejectedWith(error, errorKey);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Nhóm mốc có tên sai công thức ở nhánh của mình cũng bị từ chối khi lưu: ghép ngày giờ sai định dạng, cột chuỗi trơn, phần thời gian không có CreateTime.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("CONVERT(varchar(10), COALESCE(y.UpdateTime, y.CreateTime), 103) + '_' + y.ID", BaseMsg.ShareData.Validation.SqlIncrementalKeyDateUnsortable)]
    [InlineData("y.ID", BaseMsg.ShareData.Validation.SqlIncrementalKeyFormulaInvalid)]
    [InlineData("CONVERT(varchar(23), y.IsDelete, 121) + '_' + y.ID", BaseMsg.ShareData.Validation.SqlIncrementalKeyFormulaInvalid)]
    public async Task UpdateExtractSql_WhenCursorGroupBreaksFormula_RejectsSql_Test(string keyB, string errorKey)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("lastKey_a", "", "lastKey"), BuiltinVar("lastKey_b", "", "lastKey")]
        });
        var groupSql = CursorGroupSql(true, CursorGroupWhereB);
        var sql = groupSql.Replace("CONVERT(varchar(23), COALESCE(y.UpdateTime, y.CreateTime), 121) + '_' + y.ID", keyB);

        // Act
        var error = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
            new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql }));

        // Assert
        var common = await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        AssertRejectedWith(error, errorKey);
        Assert.NotEqual(sql, common?.ExtractSql);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Khai biến mốc rồi lưu câu SQL ở mẫu chung hoặc cấu hình riêng của đối tác, trả về lỗi nếu bị từ chối và dòng SQL đang lưu sau khi gọi.
    /// Created date: 05/11/2026
    /// </summary>
    private async Task<(Exception? Error, ShareDataSql? Row)> SaveSqlWithLastKeyAsync(bool partnerScope, string sql)
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partnerScope ? partner.ID : null,
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });
        Task SaveSql() => partnerScope
            ? services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql })
            : services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql });
        var error = await Record.ExceptionAsync(SaveSql);
        var row = partnerScope
            ? await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).FirstAsync()
            : await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        return (error, row);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cột mốc phải đúng công thức PA1 CONVERT(varchar(23), &lt;thời gian&gt;, 121) + '_' + ID, phần thời gian có CreateTime, ở cả mẫu chung lẫn SQL riêng:
    ///              cột chuỗi trơn, ép số sang chuỗi, chỉ có cột thời gian khác, sai kiểu CONVERT, thiếu '_' hay thiếu khóa đều bị từ chối.
    ///              Phần khóa bắt đầu bằng cột ID vì cột khác có thể trống, cả chuỗi mốc thành NULL và dòng không bao giờ được gửi;
    ///              ghép thêm sau ID thì phần ghép phải là cột ID khác hoặc có giá trị thay thế (ISNULL, COALESCE).
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, "ID", true)]
    [InlineData(true, "ID", true)]
    [InlineData(false, "Code", true)]
    [InlineData(false, "CAST(FromKmNumber AS varchar(10))", true)]
    [InlineData(false, "CAST(FromKmNumber AS varchar(10)) + '_' + ID", true)]
    [InlineData(false, "Code + '_' + ID", true)]
    [InlineData(false, "CONVERT(varchar(23), IsDelete, 121) + '_' + ID", true)]
    [InlineData(true, "CONVERT(varchar(23), IsDelete, 121) + '_' + ID", true)]
    [InlineData(false, "CONVERT(varchar(23), COALESCE(UpdateTime, IsDelete), 121) + '_' + ID", true)]
    [InlineData(false, "FORMAT(CreateTime, 'yyyyMMddHHmmssfff') + '_' + ID", true)]
    [InlineData(false, "FORMAT(FromKmNumber, 'd10') + '_' + ID", true)]
    [InlineData(false, "CONVERT(varchar(10), CreateTime, 111) + '_' + ID", true)]
    [InlineData(false, "CONVERT(varchar(23), CreateTime, 120) + '_' + ID", true)]
    [InlineData(false, "CONVERT(varchar(23), CreateTime, 121)", true)]
    [InlineData(false, "CONVERT(varchar(23), CreateTime, 121) + ID", true)]
    [InlineData(false, "CONVERT(varchar(23), CreateTime, 121) + '_' + 'a'", true)]
    [InlineData(false, "CONCAT(CONVERT(varchar(23), CreateTime, 121), '_', ID)", true)]
    [InlineData(false, "CONVERT(varchar(23), CreateTime, 121) + '_' + ID", false)]
    [InlineData(false, "CONVERT(varchar(23), COALESCE(IsDelete, CreateTime), 121) + '_' + ID", false)]
    [InlineData(false, "CONVERT(nvarchar(23), ISNULL(UpdateTime, CreateTime), 121) + N'_' + ID", false)]
    [InlineData(true, "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + CAST(FromKmNumber AS varchar(10))", true)]
    [InlineData(false, "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + Code + '_' + ID", true)]
    [InlineData(false, "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + Name", true)]
    [InlineData(true, "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + Code", true)]
    [InlineData(false, "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ISNULL(Code, '')", true)]
    [InlineData(false, "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID + '_' + Code", true)]
    [InlineData(false, "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID + '_' + ISNULL(Code, '')", false)]
    [InlineData(true, "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + N'_' + [ID]", false)]
    public async Task SaveExtractSql_WhenCursorColumnVaries_OnlyFormulaIsSaved_Test(bool partnerScope, string keyExpr, bool rejected)
    {
        // Arrange
        var sql = $"SELECT TOP 10 ID AS zoneId, {keyExpr} AS __lastKey FROM TmsZone WHERE {keyExpr} > @lastKey ORDER BY __lastKey ASC";

        // Act
        var (error, row) = await SaveSqlWithLastKeyAsync(partnerScope, sql);

        // Assert
        if (rejected)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalKeyFormulaInvalid);
            Assert.NotEqual(sql, row?.ExtractSql);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(sql, row?.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cột ID ở phần khóa phải luôn có giá trị: ID của bảng phía tùy chọn trong LEFT, RIGHT, FULL JOIN có thể trống nên cả chuỗi mốc thành NULL và dòng không bao giờ được gửi.
    ///              ID của bảng chính hoặc bảng INNER JOIN thì lưu được; xét cả khi cột mốc nằm trong bảng dẫn xuất. Bọc ISNULL ở phần ghép thêm sau ID thì lưu được.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("TmsZoneStatus zs LEFT JOIN TmsZone z ON zs.ZoneId = z.ID", "z.ID", true)]
    [InlineData("TmsZone z RIGHT JOIN TmsZoneStatus zs ON zs.ZoneId = z.ID", "z.ID", true)]
    [InlineData("TmsZoneStatus zs FULL JOIN TmsZone z ON zs.ZoneId = z.ID", "zs.ID", true)]
    [InlineData("TmsZoneStatus zs LEFT JOIN TmsZone z ON zs.ZoneId = z.ID", "zs.ID + '_' + z.ID", true)]
    [InlineData("TmsZoneStatus zs LEFT JOIN TmsZone z ON zs.ZoneId = z.ID", "zs.ID", false)]
    [InlineData("TmsZone z RIGHT JOIN TmsZoneStatus zs ON zs.ZoneId = z.ID", "zs.ID", false)]
    [InlineData("TmsZoneStatus zs INNER JOIN TmsZone z ON zs.ZoneId = z.ID", "z.ID", false)]
    [InlineData("TmsZoneStatus zs LEFT JOIN TmsZone z ON zs.ZoneId = z.ID", "zs.ID + '_' + ISNULL(z.ID, '')", false)]
    public async Task SaveExtractSql_WhenCursorIdComesFromOptionalJoin_RejectsSql_Test(string from, string key, bool rejected)
    {
        // Arrange
        var keyExpr = $"CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + {key}";
        var sql = $"SELECT TOP 10 zs.ZoneId AS zoneId, {keyExpr} AS __lastKey FROM {from} WHERE {keyExpr} > @lastKey ORDER BY __lastKey ASC";
        var derivedSql = $"SELECT TOP 10 x.zoneId, x.__lastKey FROM (SELECT zs.ZoneId AS zoneId, {keyExpr} AS __lastKey FROM {from}) x WHERE x.__lastKey > @lastKey ORDER BY x.__lastKey ASC";

        // Act
        var (error, row) = await SaveSqlWithLastKeyAsync(false, sql);
        var (derivedError, derivedRow) = await SaveSqlWithLastKeyAsync(false, derivedSql);

        // Assert
        if (rejected)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalKeyFormulaInvalid);
            AssertRejectedWith(derivedError, BaseMsg.ShareData.Validation.SqlIncrementalKeyFormulaInvalid);
            Assert.NotEqual(sql, row?.ExtractSql);
            Assert.NotEqual(derivedSql, derivedRow?.ExtractSql);
        }
        else
        {
            Assert.Null(error);
            Assert.Null(derivedError);
            Assert.Equal(sql, row?.ExtractSql);
            Assert.Equal(derivedSql, derivedRow?.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: OFFSET bỏ qua dòng đầu mỗi trang nên các dòng đó không bao giờ được gửi: từ chối khi lưu, ở cả mẫu chung, câu riêng lẫn câu con. OFFSET 0 không bỏ dòng nào thì lưu được.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, "SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY __lastKey ASC OFFSET 1 ROWS", true)]
    [InlineData(true, "SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY __lastKey ASC OFFSET 1 ROWS", true)]
    [InlineData(false, "SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY __lastKey ASC OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY", true)]
    [InlineData(false, "SELECT TOP 10 x.zoneId, x.__lastKey FROM (SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone ORDER BY __lastKey ASC OFFSET 1 ROWS) x WHERE x.__lastKey > @lastKey ORDER BY x.__lastKey ASC", true)]
    [InlineData(false, "SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY __lastKey ASC OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", false)]
    public async Task SaveExtractSql_WhenCursorSqlSkipsRowsByOffset_RejectsSql_Test(bool partnerScope, string template, bool rejected)
    {
        // Arrange
        var sql = template.Replace("{K}", "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID");

        // Act
        var (error, row) = await SaveSqlWithLastKeyAsync(partnerScope, sql);

        // Assert
        if (rejected)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalOffsetNotAllowed);
            Assert.NotEqual(sql, row?.ExtractSql);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(sql, row?.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Câu con có TOP chọn dòng theo cột khác cột mốc (dạng mẫu của sếp: TOP trong câu con sắp theo NGAYSOAT, mốc theo Write_Date) thì dòng có mốc nhỏ hơn bị bỏ sót:
    ///              từ chối khi lưu, cả khi câu con không có ORDER BY, sắp giảm dần hay nằm trong CTE. Câu con sắp tăng dần theo cột mốc thì lưu được;
    ///              TOP 1 trong câu con ở cột SELECT hoặc OUTER APPLY chỉ lấy giá trị phụ cho từng dòng, không bỏ dòng nào, nên cũng lưu được.
    /// Created date: 06/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, "SELECT * FROM (SELECT TOP 10 ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY Code) T ORDER BY __lastKey ASC", true)]
    [InlineData(true, "SELECT * FROM (SELECT TOP 10 ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY Code) T ORDER BY __lastKey ASC", true)]
    [InlineData(false, "SELECT * FROM (SELECT TOP 10 ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey) T ORDER BY __lastKey ASC", true)]
    [InlineData(false, "SELECT * FROM (SELECT TOP 10 ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY __lastKey DESC) T ORDER BY __lastKey ASC", true)]
    [InlineData(false, "WITH T AS (SELECT TOP 10 ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY Code) SELECT * FROM T ORDER BY __lastKey ASC", true)]
    [InlineData(false, "SELECT * FROM (SELECT TOP 10 ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY __lastKey ASC) T ORDER BY __lastKey ASC", false)]
    [InlineData(false, "SELECT * FROM (SELECT TOP 10 ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey ORDER BY {K}) T ORDER BY __lastKey ASC", false)]
    [InlineData(false, "SELECT TOP 10 z.ID AS zoneId, (SELECT TOP 1 s.Condition FROM TmsZoneStatus s WHERE s.ZoneId = z.ID ORDER BY s.CreateTime DESC) AS lastCondition, {Z} AS __lastKey FROM TmsZone z WHERE {Z} > @lastKey ORDER BY __lastKey ASC", false)]
    [InlineData(false, "SELECT TOP 10 z.ID AS zoneId, s.Condition AS lastCondition, {Z} AS __lastKey FROM TmsZone z OUTER APPLY (SELECT TOP 1 x.Condition FROM TmsZoneStatus x WHERE x.ZoneId = z.ID ORDER BY x.CreateTime DESC) s WHERE {Z} > @lastKey ORDER BY __lastKey ASC", false)]
    public async Task SaveExtractSql_WhenInnerTopSortsByOtherColumn_RejectsSql_Test(bool partnerScope, string template, bool rejected)
    {
        // Arrange
        var sql = template
            .Replace("{K}", "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID")
            .Replace("{Z}", "CONVERT(varchar(23), COALESCE(z.UpdateTime, z.CreateTime), 121) + '_' + z.ID");

        // Act
        var (error, row) = await SaveSqlWithLastKeyAsync(partnerScope, sql);

        // Assert
        if (rejected)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalInnerTopOrderInvalid);
            Assert.NotEqual(sql, row?.ExtractSql);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(sql, row?.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Gộp nhiều nguồn bằng UNION mà dùng chung một mốc thì nguồn về trễ có giá trị nhỏ hơn mốc sẽ mất dòng: từ chối khi lưu, dù so mốc ở từng nhánh, ở câu ngoài hay qua CTE.
    ///              Câu con UNION chỉ dùng để lọc (IN) không gộp nguồn nên lưu được; mỗi nguồn một nhóm mốc đã có test riêng.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, "SELECT TOP 10 u.zoneId, u.__lastKey FROM (SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey UNION ALL SELECT ID, {K} FROM TmsWeather WHERE {K} > @lastKey) u ORDER BY u.__lastKey ASC", true)]
    [InlineData(true, "SELECT TOP 10 u.zoneId, u.__lastKey FROM (SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey UNION ALL SELECT ID, {K} FROM TmsWeather WHERE {K} > @lastKey) u ORDER BY u.__lastKey ASC", true)]
    [InlineData(false, "SELECT TOP 10 u.zoneId, u.__lastKey FROM (SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone UNION ALL SELECT ID, {K} FROM TmsWeather) u WHERE u.__lastKey > @lastKey ORDER BY u.__lastKey ASC", true)]
    [InlineData(false, "WITH u AS (SELECT ID AS zoneId, {K} AS __lastKey FROM TmsZone UNION ALL SELECT ID, {K} FROM TmsWeather) SELECT TOP 10 u.zoneId, u.__lastKey FROM u WHERE u.__lastKey > @lastKey ORDER BY u.__lastKey ASC", true)]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId, {K} AS __lastKey FROM TmsZone WHERE {K} > @lastKey AND ID IN (SELECT ZoneId FROM TmsZoneStatus UNION SELECT ZoneId FROM TmsTrafficStatistic) ORDER BY __lastKey ASC", false)]
    public async Task SaveExtractSql_WhenSourcesShareOneCursor_RejectsSql_Test(bool partnerScope, string template, bool rejected)
    {
        // Arrange
        var sql = template.Replace("{K}", "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID");

        // Act
        var (error, row) = await SaveSqlWithLastKeyAsync(partnerScope, sql);

        // Assert
        if (rejected)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalUnionOneCursor);
            Assert.NotEqual(sql, row?.ExtractSql);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(sql, row?.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cột mốc lấy từ bảng dẫn xuất, WHERE ngoài cùng so theo tên cột: công thức xét ở SELECT bên trong, không từ chối nhầm câu đúng.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("ID", true)]
    [InlineData("CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID", false)]
    public async Task SaveExtractSql_WhenCursorColumnComesFromDerivedTable_ChecksInnerFormula_Test(string keyExpr, bool rejected)
    {
        // Arrange
        var sql = "SELECT TOP 10 x.zoneId, x.__lastKey "
            + $"FROM (SELECT ID AS zoneId, {keyExpr} AS __lastKey FROM TmsZone) x "
            + "WHERE x.__lastKey > @lastKey ORDER BY x.__lastKey ASC";

        // Act
        var (error, row) = await SaveSqlWithLastKeyAsync(false, sql);

        // Assert
        if (rejected)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalKeyFormulaInvalid);
            Assert.NotEqual(sql, row?.ExtractSql);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(sql, row?.ExtractSql);
        }
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Câu có cột mốc mà không dùng biến mốc thì không phải câu nối đuôi, lượt nào cũng gửi lại cùng dữ liệu nên bị từ chối khi lưu, ở cả mẫu chung lẫn câu riêng:
    ///              câu không có biến nào, cột mốc của nhóm có tên, và @lastKey khai nguồn giá trị cố định. Câu gửi toàn bộ không có cột mốc thì lưu được.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId, ID AS __lastKey FROM TmsZone WHERE ID > 'a' ORDER BY ID ASC", false, true)]
    [InlineData(true, "SELECT TOP 10 ID AS zoneId, ID AS __lastKey FROM TmsZone WHERE ID > 'a' ORDER BY ID ASC", false, true)]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId, ID AS __lastKey_a FROM TmsZone WHERE ID > 'a' ORDER BY ID ASC", false, true)]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId, ID AS __lastKey FROM TmsZone WHERE ID > @lastKey ORDER BY ID ASC", true, true)]
    [InlineData(false, "SELECT TOP 10 ID AS zoneId FROM TmsZone WHERE ID > 'a' ORDER BY ID ASC", false, false)]
    public async Task SaveExtractSql_WhenCursorColumnHasNoCursorVariable_RejectsSql_Test(bool partnerScope, string sql, bool constantVariable, bool rejected)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        List<PacketSqlVariableItemInput>? variables = constantVariable
            ? [ConstVar("lastKey", ShareDataConst.SqlVariableTypeString, "a")]
            : null;
        Task SaveSql() => partnerScope
            ? scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql, Variables = variables })
            : scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql, Variables = variables });

        // Act
        var error = await Record.ExceptionAsync(SaveSql);

        // Assert
        var row = partnerScope
            ? await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).FirstAsync()
            : await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        if (rejected)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalCursorVariableMissing);
            Assert.NotEqual(sql, row?.ExtractSql);
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(sql, row?.ExtractSql);
        }
    }

    /// <summary>
    /// Description: Bộ biến gửi kèm câu SQL khi lưu (mẫu chung hoặc SQL riêng): BE kiểm câu theo đúng bộ biến này và ghi cả hai trong một lần.
    ///              Câu hợp lệ thì lưu cả câu lẫn bộ biến; câu bị từ chối thì không lưu gì, bộ biến cũ còn nguyên (trước đây bộ biến
    ///              lưu riêng trước câu SQL nên câu bị từ chối vẫn để lại bộ biến mới, lệch với câu cũ).
    /// Created date: 31/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task UpdateExtractSql_WhenVariablesSentWithSql_SavesBothOrNeither_Test(bool partnerScope, bool validSql)
    {
        // Arrange: câu đang lưu dùng biến @lastKey; câu mới gộp hai nguồn dùng bộ biến nhóm a, b
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        var partnerId = partnerScope ? partner.ID : null;
        var oldSql = CursorFilterSql(CursorFilterWhere);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partnerId,
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });
        Task SaveSql(string sql, List<PacketSqlVariableItemInput>? variables) => partnerScope
            ? scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
            {
                PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql, Variables = variables, Force = true
            })
            : scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
            {
                PacketCode = packet.Code!, ExtractSql = sql, Variables = variables, Force = true
            });
        await SaveSql(oldSql, null);
        var newSql = CursorGroupSql(true, validSql ? CursorGroupWhereB : "y.ID > @lastKey_b");
        List<PacketSqlVariableItemInput> groupVariables = [BuiltinVar("lastKey_a", "", "lastKey"), BuiltinVar("lastKey_b", "", "lastKey")];

        // Act
        var error = await Record.ExceptionAsync(() => SaveSql(newSql, groupVariables));

        // Assert
        var row = partnerScope
            ? await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).FirstAsync()
            : await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        var savedCodes = SqlTextHelper.ReadVariables(row?.VariablesJson).Select(v => v.Code).ToList();
        if (validSql)
        {
            Assert.Null(error);
            Assert.Equal(newSql, row?.ExtractSql);
            Assert.Equal(["lastKey_a", "lastKey_b"], savedCodes);
        }
        else
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalKeyFilterInvalid);
            Assert.Equal(oldSql, row?.ExtractSql);
            Assert.Equal(["lastKey"], savedCodes);
        }
    }

    /// <summary>
    /// Description: Bước 1b của tệp seed ShareDataSql: câu mẫu chung nối đuôi một chuỗi ghép (gói 101 - 111) chưa có bộ biến thì được điền bộ biến mốc
    ///              (@lastKey theo key), thêm @pageSize = 100 khi câu dùng TOP (@pageSize); dòng đã có bộ biến
    ///              (kể cả rỗng) và câu gộp nhiều nhóm mốc giữ nguyên. Chạy đúng khối lệnh trong tệp, trong giao dịch rồi hoàn tác
    ///              để không đổi dữ liệu dùng chung của CSDL test.
    /// Created date: 31/10/2026
    /// Modified date: 02/11/2026
    /// </summary>
    [Theory]
    [InlineData("SELECT TOP (@pageSize)", "", false, new[] { "pageSize", "lastKey" })]
    [InlineData("SELECT TOP 100", "", false, new[] { "lastKey" })]
    [InlineData("SELECT TOP 100", "", true, new string[0])]
    [InlineData("SELECT TOP 100", "_a", false, new string[0])]
    public async Task SeedSqlScript_WhenCommonCursorSqlHasNoVariables_FillsCursorVariables_Test(string selectTop, string group, bool hasEmptyVariables, string[] expectedCodes)
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetId = Guid.NewGuid().ToString("N");
        const string keyExpr = "CONVERT(varchar(23), UpdateTime, 121) + '_' + ID";
        var sql = $"{selectTop} ID AS zoneId, {keyExpr} AS __lastKey{group} FROM TmsZone "
            + $"WHERE {keyExpr} > @lastKey{group} "
            + $"ORDER BY __lastKey{group} ASC";
        var batch = SeedScriptReader.ReadSqlSeedBatch("-- 1b. ĐIỀN BỘ BIẾN CHO CÂU MẪU CHUNG NỐI ĐUÔI CÒN TRỐNG");

        // Act
        List<SqlVariableItem> saved;
        await db.Ado.BeginTranAsync();
        try
        {
            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = $"103_SEED_{unique}",
                Name = $"Seed {unique}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            // Ghi thẳng dòng mẫu chung như dữ liệu cũ trong CSDL (không qua helper tự khai bộ biến)
            await db.Insertable(new ShareDataSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketId = packetId,
                ExtractSql = sql,
                VariablesJson = hasEmptyVariables ? "[]" : null
            }).ExecuteCommandAsync();
            await db.Ado.ExecuteCommandAsync(batch);
            saved = SqlTextHelper.ReadVariables((await ShareDataSqlSeed.GetCommonAsync(db, packetId))?.VariablesJson);
        }
        finally
        {
            await db.Ado.RollbackTranAsync();
        }

        // Assert: mã biến đúng thứ tự, @pageSize nhập 100, biến mốc lấy nguồn cùng tên
        Assert.Equal(expectedCodes, saved.Select(v => v.Code));
        Assert.Equal(expectedCodes.Select(c => c == "pageSize" ? "100" : c), saved.Select(v => v.Value));
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: 7 câu mẫu nối đuôi trong tệp seed ShareDataSql cùng bộ biến của tệp seed lưu được qua API lưu câu SQL, bộ biến không còn lastTime
    ///              và chạy thử được qua API chạy thử trên cấu trúc bảng hiện hành, ra cột __lastKey, không còn cột __lastTime. Bắt lỗi gõ trong 7 câu.
    /// Created date: 02/11/2026
    /// </summary>
    [Theory]
    [InlineData("101", "Vars101")]
    [InlineData("103", "VarsCursor")]
    [InlineData("104", "VarsCursor")]
    [InlineData("106", "VarsCursor")]
    [InlineData("107", "VarsCursor")]
    [InlineData("109", "VarsCursor")]
    [InlineData("111", "VarsCursor")]
    public async Task SeedExtractSql_WhenSavedWithSeedVariables_SavesAndRunsAsCompositeKey_Test(string seedPacketCode, string seedVariablesName)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        var sql = SeedScriptReader.GetExtractSql(seedPacketCode);
        var variables = SqlTextHelper.ReadVariables(SeedScriptReader.ReadSeedVariablesJson(seedVariablesName))
            .Select(v => new PacketSqlVariableItemInput { Code = v.Code, Name = v.Name, DataType = v.DataType, ValueKind = v.ValueKind, Value = v.Value, DefaultValue = v.DefaultValue, Remark = v.Remark })
            .ToList();

        // Act
        var saveError = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput
        {
            PacketCode = packet.Code!,
            ExtractSql = sql,
            Variables = variables
        }));
        var sample = await scope.ServiceProvider.CallApiAsync<ExecutePacketSqlSampleOutput>(new ExecutePacketSqlSampleInput
        {
            PacketCode = packet.Code!,
            ExtractSql = sql,
            Top = 1
        });

        // Assert
        Assert.Null(saveError);
        var saved = await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        Assert.Equal(sql, saved?.ExtractSql);
        Assert.DoesNotContain(SqlTextHelper.ReadVariables(saved?.VariablesJson), v => v.Code == "lastTime");
        Assert.True(sample.IsSuccess, sample.ErrorMessage);
        Assert.Contains("__lastKey", sample.Columns);
        Assert.DoesNotContain("__lastTime", sample.Columns);
    }

    /// <summary>
    /// Description: Bước 1 của tệp seed ShareDataSql với cờ @GhiDeMaGoi = mã một gói: chỉ câu mẫu chung của gói đó bị ghi đè về câu chuẩn
    ///              trong tệp, gói khác đã có câu giữ nguyên (dùng để nạp lại câu mẫu gói 101 vào DB dev mà không đụng các gói khác).
    ///              Chạy đúng khối lệnh trong tệp, trong giao dịch rồi hoàn tác.
    /// Created date: 31/10/2026
    /// </summary>
    [Theory]
    [InlineData("104", "106")]
    [InlineData("106", "104")]
    public async Task SeedSqlScript_WhenOverwriteOnePacketCode_OverwritesOnlyThatPacket_Test(string overwriteCode, string keptCode)
    {
        // Arrange: hai gói đã có câu mẫu chung tự sửa
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var batch = SeedScriptReader.ReadSqlSeedBatch("-- 1. NẠP CÂU MẪU CHUNG (ShareDataSql) CHO GÓI 101 - 111")
            .Replace("DECLARE @GhiDeMaGoi NVARCHAR(16) = NULL;", $"DECLARE @GhiDeMaGoi NVARCHAR(16) = N'{overwriteCode}';");
        string? overwritten;
        string? kept;

        // Act
        await db.Ado.BeginTranAsync();
        try
        {
            async Task<string> SeedPacket(string code)
            {
                var packetId = Guid.NewGuid().ToString("N");
                await db.Insertable(new ShareDataPacket
                {
                    ID = packetId,
                    Code = $"{code}_SEED_{unique}",
                    Name = $"Seed {code} {unique}",
                    PacketVersion = "1.0",
                    Status = BaseEnums.StatusEnum.Enable
                }).ExecuteCommandAsync();
                await db.Insertable(new ShareDataSql
                {
                    ID = Guid.NewGuid().ToString("N"),
                    PacketId = packetId,
                    ExtractSql = $"SELECT 1 AS customized{code}"
                }).ExecuteCommandAsync();
                return packetId;
            }
            var overwriteId = await SeedPacket(overwriteCode);
            var keptId = await SeedPacket(keptCode);
            await db.Ado.ExecuteCommandAsync(batch);
            overwritten = (await ShareDataSqlSeed.GetCommonAsync(db, overwriteId))?.ExtractSql;
            kept = (await ShareDataSqlSeed.GetCommonAsync(db, keptId))?.ExtractSql;
        }
        finally
        {
            await db.Ado.RollbackTranAsync();
        }

        // Assert
        Assert.Equal(SeedScriptReader.GetExtractSql(overwriteCode), overwritten);
        Assert.Equal($"SELECT 1 AS customized{keptCode}", kept);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Chỉ lưu bảng tham số, đổi biến mốc sang giá trị cố định trong khi câu đang lưu còn cột mốc: câu thành gửi toàn bộ nên bị từ chối, bộ biến cũ còn nguyên.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateSqlVariables_WhenCursorVariableBecomesConstant_Rejects_Test(bool partnerScope)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        var partnerId = partnerScope ? partner.ID : null;
        var sql = CursorFilterSql(CursorFilterWhere);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partnerId,
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });
        if (partnerScope)
        {
            await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql, Force = true });
        }
        else
        {
            await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql, Force = true });
        }

        // Act
        var error = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partnerId,
            Items = [ConstVar("lastKey", ShareDataConst.SqlVariableTypeString, "a")]
        }));

        // Assert
        var row = partnerScope
            ? await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).FirstAsync()
            : await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        var savedVariables = SqlTextHelper.ReadVariables(row?.VariablesJson);
        var saved = savedVariables.Single(v => v.Code == "lastKey");
        AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlIncrementalCursorVariableMissing);
        Assert.Equal(ShareDataConst.SqlVariableSourceLastKey, saved.Value);
    }

    /// <summary>
    /// Description: Chỉ lưu bảng tham số (câu SQL không đổi): BE kiểm lại câu đang lưu của đúng phạm vi đó theo bộ biến mới.
    ///              Đổi ghi chú / mốc khởi tạo vẫn lưu được; đổi tên biến mốc làm câu đang lưu dùng @lastKey chưa khai
    ///              nên bị từ chối, bộ biến cũ còn nguyên (trước đây lưu được, Worker mới chặn lúc chạy).
    /// Created date: 31/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task UpdateSqlVariables_WhenSavedSqlNoLongerFitsNewVariables_Rejects_Test(bool partnerScope, bool breaksSql)
    {
        // Arrange: phạm vi đã có câu nối đuôi theo @lastKey
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        var partnerId = partnerScope ? partner.ID : null;
        var sql = CursorFilterSql(CursorFilterWhere);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partnerId,
            Items = [BuiltinVar("lastKey", "", "lastKey")]
        });
        if (partnerScope)
        {
            await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql, Force = true });
        }
        else
        {
            await scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql, Force = true });
        }
        var lastKey = BuiltinVar(breaksSql ? "lastKeyMoi" : "lastKey", "", "lastKey");
        lastKey.Remark = "Khóa nối đuôi";

        // Act
        var error = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partnerId,
            Items = [lastKey]
        }));

        // Assert
        var row = partnerScope
            ? await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).FirstAsync()
            : await ShareDataSqlSeed.GetCommonAsync(db, packet.ID);
        var saved = SqlTextHelper.ReadVariables(row?.VariablesJson).Single(v => v.Code == "lastKey");
        if (breaksSql)
        {
            AssertRejectedWith(error, BaseMsg.ShareData.Validation.SqlUnsupportedParameter);
            Assert.Equal(("lastKey", (string?)null), (saved.Value, saved.Remark));
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(("lastKey", (string?)"Khóa nối đuôi"), (saved.Value, saved.Remark));
        }
    }

    /// <summary>
    /// Description: Đặt mốc gửi dữ liệu cho đối tác qua API SetLastSend: SuperAdmin lưu mốc vào ShareDataLastSend
    ///              kèm chữ ký công thức cột mốc, xóa LastTimeRun của đăng ký, và ghi nhật ký hoạt động; người dùng thường bị từ chối.
    ///              Mốc trống bị từ chối, mốc đang lưu giữ nguyên (bỏ mốc thì dùng Đặt lại).
    /// Created date: 29/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Fact]
    public async Task SetLastSend_WhenSuperAdmin_SetsCursorWithSignatureAndClearsLastTimeRun_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, CursorCreateTimeSql);

        var sub = new ShareDataSubscription
        {
            ID = $"sub_{unique}",
            PartnerId = partner.ID,
            DatatypeId = packet.ID,
            State = BaseEnums.SubSubscriptionState.Paused,
            LastTimeRun = DateTime.Now.AddHours(-1)
        };
        await db.Insertable(sub).ExecuteCommandAsync();

        var input = new SetLastSendInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            LastKey = $"k_set_{unique}"
        };

        // Act 1: Người dùng thường gọi API -> bị từ chối
        SetUserContext(isSuperAdmin: false, account: $"user_{unique}");
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync(input));

        // Act 2: SuperAdmin gọi API -> thành công
        SetUserContext(isSuperAdmin: true, account: $"super_{unique}");
        await scope.ServiceProvider.CallApiAsync(input);

        // Act 3: mốc trống -> bị từ chối
        var emptyError = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync(new SetLastSendInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            LastKey = "  "
        }));

        // Assert
        Assert.NotNull(emptyError);
        var lastSend = await db.Queryable<ShareDataLastSend>()
            .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packet.Code)
            .FirstAsync();
        Assert.NotNull(lastSend);
        Assert.Equal($"k_set_{unique}", lastSend.LastKey);
        Assert.NotNull(lastSend.CursorSignature);

        var auditLog = await db.Queryable<ShareDataActivityLog>()
            .Where(x => x.TargetId == lastSend.ID && x.Action == BaseEnums.ActivityAction.Create)
            .FirstAsync();
        Assert.NotNull(auditLog);
        Assert.Equal($"super_{unique}", auditLog.OperatorName);

        var updatedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
        Assert.Null(updatedSub.LastTimeRun);
    }

    /// <summary>
    /// Description: Đặt mốc khóa như Đặt lại (ESH-2306): đăng ký gửi của đối tác còn bật thì từ chối, giữ nguyên mốc và LastTimeRun;
    ///              đã tắt thì đặt được, đăng ký đang bật của đối tác khác không khóa đối tác này.
    /// Created date: 01/11/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetLastSend_WhenOwnSubscriptionActiveOrOff_OnlyAllowedWhenOff_Test(bool ownActive)
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, CursorCreateTimeSql);
        var other = NewPartnerSqlPartner($"PT_OTHER_{unique}", unique);
        await db.Insertable(other).ExecuteCommandAsync();

        var lastRun = DateTime.Now.AddHours(-1);
        var sub = new ShareDataSubscription
        {
            ID = $"sub_{unique}",
            PartnerId = partner.ID,
            DatatypeId = packet.ID,
            Direction = BaseEnums.Direction.Outbound,
            State = ownActive ? BaseEnums.SubSubscriptionState.Active : BaseEnums.SubSubscriptionState.Paused,
            LastTimeRun = lastRun
        };
        var otherSub = new ShareDataSubscription
        {
            ID = $"sub_other_{unique}",
            PartnerId = other.ID,
            DatatypeId = packet.ID,
            Direction = BaseEnums.Direction.Outbound,
            State = BaseEnums.SubSubscriptionState.Active
        };
        await db.Insertable(new List<ShareDataSubscription> { sub, otherSub }).ExecuteCommandAsync();

        await db.Insertable(new ShareDataLastSend
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            LastKey = "k_old"
        }).ExecuteCommandAsync();

        SetUserContext(isSuperAdmin: true, account: $"super_{unique}");
        var input = new SetLastSendInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            LastKey = "k_new"
        };

        // Act
        if (ownActive)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync(input));
        }
        else
        {
            await scope.ServiceProvider.CallApiAsync(input);
        }

        // Assert
        var cursor = await db.Queryable<ShareDataLastSend>()
            .Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packet.Code)
            .FirstAsync();
        Assert.Equal(ownActive ? "k_old" : "k_new", cursor.LastKey);
        var savedSub = await db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
        Assert.Equal(ownActive, savedSub.LastTimeRun != null);
    }

    /// <summary>
    /// Description: Dò cột câu SQL đang soạn (chưa lưu) theo bộ biến của câu đó: câu dùng biến đã khai (@since) thì dò được cột,
    ///              biến chưa khai thì báo lỗi; không ghi dữ liệu.
    /// Created date: 26/10/2026
    /// Modified date: 03/11/2026
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
                Items = [ConstVar("since", ShareDataConst.SqlVariableTypeString, "2026-01-01")]
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
    /// Author: DatHQ
    /// Description: Câu ghi chiều nhận khóa theo đăng ký nhận (ESH-2306): câu riêng chỉ khóa khi đăng ký nhận của đúng đối tác đó đang bật, câu mẫu chung khóa khi còn bất kỳ đăng ký nhận nào của gói đang bật;
    ///              đăng ký gửi không khóa. Câu riêng để trống thì từ chối, muốn bỏ thì Dùng lại mẫu chung: xóa câu riêng, giữ câu mẫu chung.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData("partnerSave", true, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("partnerSave", true, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, null)]
    [InlineData("partnerSave", false, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Active, null)]
    [InlineData("partnerEmpty", true, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Paused, BaseMsg.ShareData.Validation.PartnerWriteSqlRequired)]
    [InlineData("partnerReset", true, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("partnerReset", true, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Paused, null)]
    [InlineData("commonSave", false, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Active, BaseMsg.ShareData.Exception.PacketSqlInUse)]
    [InlineData("commonSave", false, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active, null)]
    public async Task WriteSql_WhenSubscriptionOrInputVaries_LocksScopeAndKeepsCommonOnReset_Test(string operation, bool ownSubscription, BaseEnums.Direction direction, BaseEnums.SubSubscriptionState state, string? errorKey)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        var other = NewPartnerSqlPartner($"PT_OTHER_{unique}", unique);
        await db.Insertable(other).ExecuteCommandAsync();
        await services.CallApiAsync<PacketWriteSqlsOutput>(new UpdatePacketWriteSqlsInput { PacketCode = packet.Code!, WriteSql = ValidWriteSql, AffectedTables = ["dbo.ShareDataInboundPacket"] });
        await services.CallApiAsync<PartnerWriteSqlOutput>(new UpdatePartnerWriteSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, WriteSql = ValidWriteSql, AffectedTables = ["dbo.ShareDataInboundPacket"] });
        var subscription = NewPartnerSqlSubscription(ownSubscription ? partner : other, packet, direction, state);
        await db.Insertable(subscription).ExecuteCommandAsync();
        var firstWrite = ValidWriteSql.Trim();
        var secondWrite = SecondTableWriteSql.Trim();
        Task Act() => operation switch
        {
            "partnerSave" => services.CallApiAsync<PartnerWriteSqlOutput>(new UpdatePartnerWriteSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, WriteSql = SecondTableWriteSql, AffectedTables = ["TmsZone"] }),
            "partnerEmpty" => services.CallApiAsync<PartnerWriteSqlOutput>(new UpdatePartnerWriteSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, WriteSql = "   " }),
            "partnerReset" => services.CallApiAsync<PartnerWriteSqlOutput>(new ResetPartnerWriteSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code! }),
            _ => services.CallApiAsync<PacketWriteSqlsOutput>(new UpdatePacketWriteSqlsInput { PacketCode = packet.Code!, WriteSql = SecondTableWriteSql, AffectedTables = ["TmsZone"] })
        };

        // Act
        var error = await Record.ExceptionAsync(Act);

        // Assert
        var commonWrite = (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.WriteSql;
        var partnerWrite = (await db.Queryable<ShareDataSql>().Where(s => s.PacketId == packet.ID && s.PartnerId == partner.ID).FirstAsync())?.WriteSql;
        if (errorKey != null)
        {
            AssertRejectedWith(error, errorKey);
            Assert.Equal(firstWrite, partnerWrite);
            Assert.Equal(firstWrite, commonWrite);
            return;
        }

        Assert.Null(error);
        switch (operation)
        {
            case "partnerSave":
                Assert.Equal(secondWrite, partnerWrite);
                Assert.Equal(firstWrite, commonWrite);
                break;
            case "partnerReset":
                Assert.True(string.IsNullOrEmpty(partnerWrite));
                Assert.Equal(firstWrite, commonWrite);
                break;
            default:
                Assert.Equal(secondWrite, commonWrite);
                Assert.Equal(firstWrite, partnerWrite);
                break;
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
            Items = [ConstVar("yearStart", ShareDataConst.SqlVariableTypeString, "2026-01-01")]
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
            Items = [ConstVar("yearStart", ShareDataConst.SqlVariableTypeString, "2026-01-01")]
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

    /// <summary>
    /// Description: Khi đối tác đã có dòng ShareDataLastSend: GetSqlVariables trả savedValue của dòng lastKey, biến nguồn khác không có savedValue.
    /// Created date: 29/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    [Fact]
    public async Task GetSqlVariables_WhenPartnerHasLastSend_ReturnsSavedValueForLastKey_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: true);
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);

        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items =
            [
                new PacketSqlVariableItemInput
                {
                    Code = "since",
                    DataType = ShareDataConst.SqlVariableTypeString,
                    ValueKind = ShareDataConst.SqlVariableKindConst,
                    Value = "2026-01-01"
                },
                new PacketSqlVariableItemInput
                {
                    Code = "lastKey",
                    DataType = ShareDataConst.SqlVariableTypeString,
                    ValueKind = ShareDataConst.SqlVariableKindBuiltin,
                    Value = ShareDataConst.SqlVariableSourceLastKey
                }
            ]
        });

        const string recordedKey = "zone_xyz_123";
        await db.Insertable(new ShareDataLastSend
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            LastKey = recordedKey
        }).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(
            new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID });

        // Assert
        var sinceVar = Assert.Single(result.Items, i => i.Code == "since");
        var lastKeyVar = Assert.Single(result.Items, i => i.Code == "lastKey");
        Assert.Null(sinceVar.SavedValue);
        Assert.Equal(recordedKey, lastKeyVar.SavedValue);
    }

    #endregion
}
