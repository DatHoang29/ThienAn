using System.Diagnostics;
using Furion.FriendlyException;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Core.Helpers;
using Module.ShareData.Infrastructure.Services.ShareDataConfig;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using Tests.ShareData.Common;

namespace Tests.ShareData.Controllers;

public partial class ShareDataPacketControllerTests
{
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
                pageSize: 10,
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
        // Arrange
        var operatorAccount = $"superadmin_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: operatorAccount);

        using var scope = _host.Services.CreateScope();

        var input = new ExecutePacketSqlSampleInput
        {
            PacketCode = "101",
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone",
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
        await db.Insertable(new ShareDataPacket { ID = Guid.NewGuid().ToString("N"), Code = packetCode, Name = $"Packet {unique}", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();

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
        await db.Insertable(new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet Snapshot {unique}",
            Status = BaseEnums.StatusEnum.Enable
        }).ExecuteCommandAsync();

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
            ExtractSql = "SELECT TOP (@pageSize) CAST('x' AS INT) AS a FROM TmsZone",
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
}
