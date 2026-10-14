using System.Security.Claims;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;
using Module.ShareData.Controllers.Packet.Commands;
using Module.ShareData.Controllers.Packet.Queries;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
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
public class ShareDataPacketExtractSqlTests(Host host)
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
        var (isValid, errorMsg) = ShareDataSqlValidator.Validate(unsafeSql);

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
        var (isValid, errorMsg) = ShareDataSqlValidator.Validate(validSql);

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
        Assert.Contains("Chỉ tài khoản SuperAdmin", ex.Message);
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
        Assert.Contains("Chỉ tài khoản SuperAdmin", ex.Message);
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
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
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
}
