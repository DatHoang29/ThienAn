using System.Security.Claims;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;
using Module.ShareData.Controllers.Packet.Commands;
using Module.ShareData.Controllers.Packet.Queries;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Core.Utils;
using Shared.Core.Utilities.Constants;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thử các chốt chặn của truy vấn động ShareData:
///              - Phân quyền: Chỉ SuperAdmin mới được xem/sửa/chạy thử câu truy vấn SQL nguồn.
///              - An toàn SQL: Validator chỉ nhận đúng 1 câu đọc (SELECT), chặn multi-statement (;), DDL/DML, bảng hệ thống nhạy cảm.
///              - Worker Guard: DataOutboundExtractionProcess từ chối thực thi nếu ExtractSql không an toàn.
///              - Ranh giới giao dịch: Cập nhật SQL và ghi Audit Log đồng bộ trong transaction, rollback nếu có lỗi.
/// Created date: 13/10/2026
/// </summary>
[Collection("api")]
public class ShareDataExtractSqlTests(Host host)
{
    private readonly IHttpContextAccessor _httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();

    private void SetUserContext(bool isSuperAdmin, string account = "test_user")
    {
        var accountType = isSuperAdmin ? ((int)BaseEnums.AccountTypeEnum.SuperAdmin).ToString() : "333";
        var claims = new List<Claim>
        {
            new(ClaimConst.AccountType, accountType),
            new(ClaimConst.Account, account),
            new(ClaimConst.UserId, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Name, account)
        };

        _httpContextAccessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
    }

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

    [Theory]
    [InlineData("SELECT ID, Code, Name FROM ShareDataPacket")]
    [InlineData("SELECT ID, Code, Name FROM ShareDataPacket;")]
    [InlineData("SELECT ID, Name FROM ShareDataPacket WHERE Name = 'a;b'")]
    [InlineData("SELECT ID, Name FROM ShareDataPacket WHERE Name = 'DROP TABLE'")]
    [InlineData("/* block comment */ SELECT ID, Code FROM ShareDataPacket -- line comment")]
    public void ShareDataSqlValidator_Accepts_ValidSingleReadQueries(string validSql)
    {
        // Act
        var (isValid, errorMsg) = SqlValidator.Validate(validSql);

        // Assert
        Assert.True(isValid, errorMsg ?? "Câu lệnh hợp lệ nhưng bị từ chối.");
        Assert.Null(errorMsg);
    }

    [Fact]
    public async Task UpdateExtractSql_WhenNotSuperAdmin_ThrowsOopsException_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false, account: "normal_user");
        var scope = host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();

        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT ID, Code FROM ShareDataPacket"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppFriendlyException>(() => handler.HandleAsync(input));
        Assert.True(ex.Message.Contains("Chỉ tài khoản SuperAdmin") || ex.Message.Contains(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlEdit));
    }

    [Fact]
    public async Task GetExtractSql_WhenNotSuperAdmin_ThrowsOopsException_Test()
    {
        // Arrange
        SetUserContext(isSuperAdmin: false, account: "normal_user");
        var scope = host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();

        var input = new GetPacketExtractSqlInput
        {
            PacketCode = "101"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppFriendlyException>(() => handler.HandleAsync(input));
        Assert.True(ex.Message.Contains("Chỉ tài khoản SuperAdmin") || ex.Message.Contains(BaseMsg.ShareData.Exception.SuperAdminRequiredForSqlView));
    }

    [Fact]
    public async Task UpdateExtractSql_WhenSuperAdmin_ValidSql_UpdatesAndAudits_Test()
    {
        // Arrange
        var operatorAccount = $"admin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_{unique}";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Test {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = null
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        var scope = host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();

        var validSql = "SELECT ID, Code, Name FROM ShareDataPacket";
        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            ExtractSql = validSql,
            Remark = "Cập nhật qua test xUnit"
        };

        // Act
        var output = await handler.HandleAsync(input);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(validSql, output.ExtractSql);

        var savedPacket = await db.Queryable<ShareDataPacket>()
            .Where(x => x.Code == packetCode)
            .FirstAsync();
        Assert.NotNull(savedPacket);
        Assert.Equal(validSql, savedPacket.ExtractSql);

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
        // Arrange
        var db = host.Services.GetRequiredService<ISqlSugarClient>();
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
            Code = "101",
            ExtractSql = "SELECT 1; DROP TABLE TestTbl;"
        };

        var warningLogged = false;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ShareDataWorker.Core.Exceptions.ShareDataException>(() =>
            extractionProcess.Extract(
                db,
                sub,
                unsafePacket,
                cursor: null,
                pageSize: 10,
                onLogWarning: (msg, args) => warningLogged = true,
                cancellationToken: CancellationToken.None));

        Assert.Contains("không an toàn hoặc không hợp lệ", ex.Message);
        Assert.True(warningLogged);
    }

    [Theory]
    [InlineData("SELECT td.ID, td.Speed FROM TmsTrafficData td WHERE td.DetectTime > @lastTime AND td.ID > @lastKey", true)]
    [InlineData("SELECT TOP (@pageSize) td.ID FROM TmsTrafficData td WHERE td.DetectTime > @lastTime", true)]
    [InlineData("SELECT td.ID FROM TmsTrafficData td WHERE td.DetectTime BETWEEN @fromDate AND @toDate", true)]
    [InlineData("SELECT td.ID FROM TmsTrafficData td WHERE td.DetectTime > @unsupportedParam", false)]
    [InlineData("SELECT td.ID FROM TmsTrafficData td WHERE td.DetectTime > @sqlInjection; SELECT 1", false)]
    public void ShareDataSqlValidator_Validates_ParametersStrictly(string sql, bool shouldBeValid)
    {
        // Act
        var (isValid, errorMsg) = SqlValidator.Validate(sql);

        // Assert
        if (shouldBeValid)
        {
            Assert.True(isValid, errorMsg ?? "Câu SQL chứa tham số hợp lệ nhưng bị từ chối");
            Assert.Null(errorMsg);
        }
        else
        {
            Assert.False(isValid);
            Assert.False(string.IsNullOrWhiteSpace(errorMsg));
        }
    }

    [Fact]
    public async Task GetExtractSql_ReturnsConfiguredExtractSqlOnPacket_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_FB_{unique}";

        var sqlTemplate = "SELECT ID, Speed AS speed_Template FROM TmsTrafficData";

        // Tạo gói tin chung
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Hierarchy {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = sqlTemplate
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        var scope = host.Services.CreateScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();

        // Act 1: Truy vấn có PartnerCode -> vẫn trả về câu SQL từ ShareDataPacket
        var resultA = await queryHandler.HandleAsync(new GetPacketExtractSqlInput
        {
            PacketCode = packetCode,
            PartnerCode = "ANY_PARTNER"
        });

        // Act 2: Truy vấn không có PartnerCode -> trả về câu SQL từ ShareDataPacket
        var resultB = await queryHandler.HandleAsync(new GetPacketExtractSqlInput
        {
            PacketCode = packetCode
        });

        // Assert
        Assert.NotNull(resultA);
        Assert.Equal(sqlTemplate, resultA.ExtractSql);
        Assert.False(resultA.IsCustomForPartner);
        Assert.Null(resultA.MappingId);

        Assert.NotNull(resultB);
        Assert.Equal(sqlTemplate, resultB.ExtractSql);
        Assert.False(resultB.IsCustomForPartner);
        Assert.Null(resultB.MappingId);
    }

    [Fact]
    public async Task UpdateExtractSql_WhenExtractSqlNull_ClearsPacketSql_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_CLR_{unique}";

        var templateSql = "SELECT ID, Name FROM ShareDataPacket";
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Clear {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = templateSql,
            SyncMode = BaseEnums.SyncMode.Snapshot
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        var scope = host.Services.CreateScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();
        var commandHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();

        // Act 1: Trước khi clear -> có SQL
        var beforeClear = await queryHandler.HandleAsync(new GetPacketExtractSqlInput
        {
            PacketCode = packetCode
        });
        Assert.Equal(templateSql, beforeClear.ExtractSql);

        // Act 2: Gửi lệnh Update với ExtractSql = null để xóa SQL
        var updateResult = await commandHandler.HandleAsync(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            ExtractSql = null
        });
        Assert.Equal(string.Empty, updateResult.ExtractSql);

        // Act 3: Đọc lại -> SQL rỗng
        var afterClear = await queryHandler.HandleAsync(new GetPacketExtractSqlInput
        {
            PacketCode = packetCode
        });
        Assert.Equal(string.Empty, afterClear.ExtractSql);

        var dbPacket = await db.Queryable<ShareDataPacket>().Where(p => p.ID == packet.ID).FirstAsync();
        Assert.Null(dbPacket.ExtractSql);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    [InlineData(-10)]
    [InlineData(500)]
    public async Task UpdateExtractSql_WhenLookbackDaysOutOfRange_ThrowsOops_Test(int lookBackDays)
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var scope = host.Services.CreateScope();
        var commandHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();

        var input = new UpdatePacketExtractSqlInput
        {
            PacketCode = "101",
            MaxLookBackDays = lookBackDays
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppFriendlyException>(() => commandHandler.HandleAsync(input));
        Assert.NotNull(ex);
        Assert.True(ex.ErrorMessage?.ToString() == BaseMsg.ShareData.Validation.LookbackDaysRange
            || ex.Message.Contains("quét lùi")
            || ex.Message.Contains("lookbackDaysRange"));
    }

    [Fact]
    public async Task ExecutePacketSqlSample_WithOrderByWithoutTop_ExecutesSuccessfully_Test()
    {
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var scope = host.Services.CreateScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT ID, Code, Name FROM ShareDataPacket ORDER BY Code ASC",
            Top = 5
        };

        // Act
        var result = await queryHandler.HandleAsync(input);

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

        var scope = host.Services.CreateScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT 1 AS TestCol, 2 AS TestCol, 3 AS TestCol",
            Top = 5
        };

        // Act
        var result = await queryHandler.HandleAsync(input);

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
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var scope = host.Services.CreateScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM ShareDataPacket",
            Top = 3
        };

        // Act
        var result = await queryHandler.HandleAsync(input);

        // Assert
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Columns);
        Assert.Contains("ID", result.Columns);
        Assert.NotNull(result.Rows);
    }

    [Fact]
    public async Task ExecutePacketSqlSample_BenchmarkWithoutTop_MeasuresExecutionTime_Test()
    {
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var scope = host.Services.CreateScope();
        var queryHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT ID, Code, Name FROM ShareDataPacket",
            Top = 5
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await queryHandler.HandleAsync(input);
        sw.Stop();

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Rows);
        Assert.True(result.Rows.Count <= 5);
        Assert.True(sw.ElapsedMilliseconds < 3000, $"Thời gian thực thi: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task UpdateExtractSql_WhenIncrementalPacketWithoutIncrementalStructure_ThrowsOops_Test()
    {
        // RV-4: Lưu SQL cho gói 103 (Incremental trên DB) không gửi SyncMode, SQL thiếu __rowid -> bị từ chối
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var existing = await db.Queryable<ShareDataPacket>().Where(x => x.Code == "103").FirstAsync();
        if (existing == null)
        {
            await db.Insertable(new ShareDataPacket { ID = Guid.NewGuid().ToString("N"), Code = "103", Name = "VDS Data", SyncMode = BaseEnums.SyncMode.Incremental, Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
        }
        else if (existing.SyncMode != BaseEnums.SyncMode.Incremental)
        {
            existing.SyncMode = BaseEnums.SyncMode.Incremental;
            await db.Updateable(existing).UpdateColumns(p => new { p.SyncMode }).ExecuteCommandAsync();
        }

        var scope = host.Services.CreateScope();
        var commandHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();

        var ex = await Assert.ThrowsAsync<AppFriendlyException>(() => commandHandler.HandleAsync(new UpdatePacketExtractSqlInput
        {
            PacketCode = "103",
            SyncMode = null,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code, CreateTime AS __lastTime FROM ShareDataPacket ORDER BY CreateTime ASC"
        }));

        Assert.True(ex.ErrorMessage?.ToString() == BaseMsg.ShareData.Validation.SqlIncrementalMissingRowid
            || ex.Message.Contains("__rowid")
            || ex.Message.Contains("sqlIncrementalMissingRowid"));
    }

    [Fact]
    public async Task UpdateExtractSql_WhenIncrementalSqlHasDescOrderBy_ThrowsOops_Test()
    {
        // RV-4: SQL nối đuôi có ORDER BY __lastTime DESC -> bị từ chối
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var existing = await db.Queryable<ShareDataPacket>().Where(x => x.Code == "103").FirstAsync();
        if (existing == null)
        {
            await db.Insertable(new ShareDataPacket { ID = Guid.NewGuid().ToString("N"), Code = "103", Name = "VDS Data", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
        }

        var scope = host.Services.CreateScope();
        var commandHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();

        var ex = await Assert.ThrowsAsync<AppFriendlyException>(() => commandHandler.HandleAsync(new UpdatePacketExtractSqlInput
        {
            PacketCode = "103",
            SyncMode = BaseEnums.SyncMode.Incremental,
            ExtractSql = "SELECT TOP (@pageSize) ID AS __rowid, Code, CreateTime AS __lastTime FROM ShareDataPacket ORDER BY __lastTime DESC"
        }));

        Assert.True(ex.ErrorMessage?.ToString() == BaseMsg.ShareData.Validation.SqlIncrementalOrderByDescNotAllowed
            || ex.Message.Contains("DESC")
            || ex.Message.Contains("sqlIncrementalOrderByDescNotAllowed"));
    }

    [Fact]
    public async Task UpdateExtractSql_WhenLookbackDaysResetToNull_ClearsDatabaseValue_Test()
    {
        // RV-5: Lưu MaxLookBackDays = 7, lưu lại với null -> đọc lại ra null
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_{unique}";
        await db.Insertable(new ShareDataPacket { ID = Guid.NewGuid().ToString("N"), Code = packetCode, Name = $"Packet {unique}", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();

        var scope = host.Services.CreateScope();
        var commandHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();
        var queryHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();

        await commandHandler.HandleAsync(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            SyncMode = BaseEnums.SyncMode.Snapshot,
            MaxLookBackDays = 7,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM ShareDataPacket"
        });

        var out1 = await queryHandler.HandleAsync(new GetPacketExtractSqlInput { PacketCode = packetCode });
        Assert.Equal(7, out1.MaxLookBackDays);

        await commandHandler.HandleAsync(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            SyncMode = BaseEnums.SyncMode.Snapshot,
            MaxLookBackDays = null,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM ShareDataPacket"
        });

        var out2 = await queryHandler.HandleAsync(new GetPacketExtractSqlInput { PacketCode = packetCode });
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
    public async Task UpdateExtractSql_WhenPacketSyncKindIs2AndCommandSyncKindIsNull_ValidatesIncrementalStructure_Test()
    {
        // 2.3: Packet có SyncMode = Incremental, command gửi SyncMode = null -> Vẫn kiểm cấu trúc nối đuôi
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_INCR_{unique}";
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Incremental {unique}",
            SyncMode = BaseEnums.SyncMode.Incremental,
            Status = BaseEnums.StatusEnum.Enable
        }).ExecuteCommandAsync();

        var scope = host.Services.CreateScope();
        var commandHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();

        // Act & Assert: SQL không có __lastTime và __rowid -> fail cấu trúc nối đuôi
        var ex = await Assert.ThrowsAsync<AppFriendlyException>(() => commandHandler.HandleAsync(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            SyncMode = null, // Không truyền SyncMode trong command
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM ShareDataPacket"
        }));

        Assert.True(ex.ErrorMessage?.ToString() == BaseMsg.ShareData.Validation.SqlIncrementalMissingOrderBy
            || ex.Message.Contains("sqlIncrementalMissing")
            || ex.Message.Contains("ORDER BY"));
    }

    [Fact]
    public async Task UpdateExtractSql_WhenPacketSyncKindIsNullAndCommandSyncKindIsNull_DefaultsToSnapshot_AllowsSnapshotSql_Test()
    {
        // 2.3: Packet có SyncMode = null, command gửi SyncMode = null -> Mặc định Snapshot, không kiểm nối đuôi
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_SNAP_{unique}";
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Snapshot {unique}",
            SyncMode = null, // NULL
            Status = BaseEnums.StatusEnum.Enable
        }).ExecuteCommandAsync();

        var scope = host.Services.CreateScope();
        var commandHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlCommandHandler>();
        var queryHandler = scope.ServiceProvider.GetRequiredService<PacketExtractSqlQueryHandler>();

        // Act: SQL dạng Snapshot bình thường (không có __lastTime hay __rowid)
        await commandHandler.HandleAsync(new UpdatePacketExtractSqlInput
        {
            PacketCode = packetCode,
            SyncMode = null,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM ShareDataPacket"
        });

        // Assert: Lưu thành công
        var output = await queryHandler.HandleAsync(new GetPacketExtractSqlInput { PacketCode = packetCode });
        Assert.NotNull(output);
        Assert.Contains("SELECT TOP (@pageSize) ID, Code FROM ShareDataPacket", output.ExtractSql);
    }
}
