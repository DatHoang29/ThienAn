using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Entities;
using Modules.CfgSystem.Core.Entities;
using Modules.TMS.Core.Entities;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using Shared.DTO.Enums;
using Tests.ShareData.Common;

namespace Tests.ShareData.Services;

public partial class DataInboundServiceTests
{
    #region 18. Rollback toàn gói, thứ tự xử lý theo SerialNbr, ghi đúng CSDL Inbound

    /// <summary>
    /// Description: I02 - Lỗi ghi CSDL đích ở câu ghi thứ 2: câu ghi thứ 1 (đã chạy) bị rollback, KHÔNG còn dòng mới nào,
    ///              gói = Failed, cảnh báo ESH-1505 (WriteFailed), không phải ESH-1307.
    ///              Nguồn: DataInboundService.WriteSql.cs WriteRecordsBySqlAsync (BeginTran dòng 62, Rollback dòng 80),
    ///              DataInboundService.cs ProcessPacket catch → FailPacketAsync(WriteFailed) dòng 233-238.
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenSecondWriteFails_RollsBackWholePacketAndAlertsEsh1505_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);
        var markerId = $"MARK_I02_{unique}";
        var markerSql = $"INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode) OUTPUT 'INSERT' VALUES ('{markerId}', 'MARK', 'MARK')";
        await _db.Updateable<ShareDataPacketSql>()
            .SetColumns(s => s.WriteSql == markerSql)
            .Where(s => s.PacketCode == packet.Code)
            .ExecuteCommandAsync();
        await _db.Insertable(new ShareDataPacketSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PacketCode = packet.Code,
            OrderNo = 2,
            Status = BaseEnums.StatusEnum.Enable,
            WriteSql = "SELECT CAST('not-a-number' AS INT)"
        }).ExecuteCommandAsync();
        var packetEntity = await SeedPendingPacket(partner.Code!, packet.Code!, 1, "{\"data\":[{\"id\":\"A\",\"val\":\"1\"}]}", DateTime.Now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.True(updated.ProcessState == BaseEnums.InboundProcessState.Failed, $"ProcessState={updated.ProcessState}, Error={updated.ErrorMessage}");

        var markers = await _inboundDb.Queryable<ShareDataInboundPacket>().Where(p => p.ID == markerId).CountAsync();
        Assert.Equal(0, markers);

        var alertCodes = await _db.Queryable<ShareDataAlertLog>()
            .Where(a => a.SubscriptionId == sub.ID)
            .Select(a => a.AlertCode)
            .ToListAsync();
        Assert.Contains(ShareDataAlertCode.Inbound.WriteFailed, alertCodes);
        Assert.DoesNotContain("ESH-1307", alertCodes);
    }

    /// <summary>
    /// Description: I03 - Nhiều gói Pending trong cùng nhóm được xử lý theo SerialNbr TĂNG DẦN, bất kể thứ tự nhận (CreateTime).
    ///              Nguồn: DataInboundService.cs ProcessGroup dòng 135-138 (OrderBy SerialNbr rồi CreateTime).
    ///              Gói SerialNbr null: ghi lại hành vi thực tế ở test I03b.
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPacketsArriveOutOfOrder_ProcessesBySerialNbrAscending_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique);
        var now = DateTime.Now;
        const string content = "{\"data\":[{\"id\":\"A\",\"val\":\"1\"}]}";
        var serial3 = await SeedPendingPacket(partner.Code!, packet.Code!, 3, content, now);
        var serial1 = await SeedPendingPacket(partner.Code!, packet.Code!, 1, content, now);
        var serial2 = await SeedPendingPacket(partner.Code!, packet.Code!, 2, content, now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var processed = await _inboundDb.Queryable<ShareDataInboundPacket>()
            .Where(p => p.PartnerCode == partner.Code)
            .OrderBy(p => p.ProcessedAt)
            .ToListAsync();
        Assert.All(processed, p => Assert.Equal(BaseEnums.InboundProcessState.Done, p.ProcessState));
        Assert.Equal([serial1.ID, serial2.ID, serial3.ID], processed.Select(p => p.ID).ToArray());
    }

    /// <summary>
    /// Description: I03b - Gói có SerialNbr null (2 gói) cùng nhóm: ghi lại hành vi thực tế — cả hai đều được xử lý (Done),
    ///              thứ tự giữa chúng theo CreateTime (OrderBy SerialNbr rồi CreateTime).
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenSerialNbrIsNull_ProcessesByCreateTime_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique);
        var now = DateTime.Now;
        const string content = "{\"data\":[{\"id\":\"A\",\"val\":\"1\"}]}";
        var earlier = await SeedPendingPacket(partner.Code!, packet.Code!, null, content, now);
        var later = await SeedPendingPacket(partner.Code!, packet.Code!, null, content, now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var processed = await _inboundDb.Queryable<ShareDataInboundPacket>()
            .Where(p => p.PartnerCode == partner.Code)
            .OrderBy(p => p.ProcessedAt)
            .ToListAsync();
        Assert.All(processed, p => Assert.Equal(BaseEnums.InboundProcessState.Done, p.ProcessState));
        Assert.Equal([earlier.ID, later.ID], processed.Select(p => p.ID).ToArray());
    }

    /// <summary>
    /// Description: I04 - Câu ghi của gói chạy trên kết nối Inbound (CSDL Inbound), không chạy trên CSDL vận hành chính.
    ///              Câu ghi chèn dòng đánh dấu mang DB_NAME() hiện hành vào ShareDataInboundPacket; dòng đó phải
    ///              mang tên CSDL Inbound và không xuất hiện ở CSDL chính.
    ///              Nguồn: DataInboundService.WriteSql.cs dùng inboundDb.Ado.SqlQueryAsync (dòng 71).
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenWriting_RunsOnInboundDatabaseOnly_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique);
        var markerId = $"MARK_I04_{unique}";
        var markerSql = $"INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode) OUTPUT 'INSERT' VALUES ('{markerId}', DB_NAME(), 'MARK')";
        await _db.Updateable<ShareDataPacketSql>()
            .SetColumns(s => s.WriteSql == markerSql)
            .Where(s => s.PacketCode == packet.Code)
            .ExecuteCommandAsync();
        await SeedPendingPacket(partner.Code!, packet.Code!, 1, "{\"data\":[{\"id\":\"A\",\"val\":\"1\"}]}", DateTime.Now);
        var inboundDbName = _inboundDb.Ado.Connection.Database;
        var mainDbName = _db.Ado.Connection.Database;

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var marker = await _inboundDb.Queryable<ShareDataInboundPacket>().Where(p => p.ID == markerId).FirstAsync();
        Assert.NotNull(marker);
        Assert.Equal(inboundDbName, marker.PartnerCode);
        Assert.NotEqual(mainDbName, marker.PartnerCode);

        var tableInMain = await _db.Ado.GetIntAsync("SELECT COUNT(1) FROM sys.tables WHERE name = 'ShareDataInboundPacket'");
        Assert.Equal(0, tableInMain);
    }

    /// <summary>
    /// Description: I06 - Không có đăng ký nhận: gói chuyển NoSubscription (không phải Failed), ErrorMessage ghi rõ
    ///              "Chưa có đăng ký nhận cho loại gói tin này.", không ghi nhật ký truyền nhận gắn đăng ký nào.
    ///              Nguồn: DataInboundService.cs ResolveContextAsync dòng 384-390. Mã HTTP trả về cho đối tác thuộc
    ///              controller, nằm ngoài phạm vi test Worker này (liên quan mục B3 của báo cáo).
    ///              Test cũ …WhenNoSubscription_UpdatesParentFailWithoutStep2 đã phủ dòng log cha; test này khẳng định
    ///              nội dung lỗi và việc không có log theo đăng ký.
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenNoSubscription_SetsNoSubscriptionStateWithExactMessage_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique, createSub: false);
        var packetEntity = await SeedPendingPacket(partner.Code!, packet.Code!, 1, "{\"data\":[]}", DateTime.Now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.NoSubscription, updated.ProcessState);
        Assert.Equal("Chưa có đăng ký nhận cho loại gói tin này.", updated.ErrorMessage);

        var logs = await _db.Queryable<ShareDataActivityLog>().Where(l => l.PartnerId == partner.ID).CountAsync();
        Assert.Equal(0, logs);
    }

    /// <summary>
    /// Description: I07 - Body rỗng (không phải JSON): gói chuyển Failed với ErrorMessage, ghi cảnh báo ESH-1503 (ParseFailed),
    ///              không ném lỗi. (Trường hợp XML đã có test riêng; giới hạn kích thước body không có trong mã Worker nên chưa kiểm.)
    ///              Nguồn: DataInboundService.cs ProcessPacketAsync dòng 205-216.
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenBodyIsEmpty_MarksFailedWithParseAlert_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);
        var packetEntity = await SeedPendingPacket(partner.Code!, packet.Code!, 1, "", DateTime.Now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updated.ProcessState);
        Assert.False(string.IsNullOrWhiteSpace(updated.ErrorMessage));

        var alertCodes = await _db.Queryable<ShareDataAlertLog>()
            .Where(a => a.SubscriptionId == sub.ID)
            .Select(a => a.AlertCode)
            .ToListAsync();
        Assert.Contains(ShareDataAlertCode.Inbound.ParseFailed, alertCodes);
    }

    /// <summary>
    /// Description: I08 - Gói Pending khi đăng ký bị tạm dừng được giữ nguyên Pending; khi đăng ký bật lại (Active)
    ///              lượt chạy kế tiếp xử lý gói thành Done. Bổ sung cho test …WhenSubscriptionPaused_KeepsPacketPending.
    ///              Nguồn: DataInboundService.cs ResolveContextAsync dòng 395-399 (Skip khi không Active).
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenSubscriptionResumed_ProcessesHeldPacket_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);
        await _db.Updateable<ShareDataSubscription>()
            .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
            .Where(s => s.ID == sub.ID)
            .ExecuteCommandAsync();
        var packetEntity = await SeedPendingPacket(partner.Code!, packet.Code!, 1, "{\"data\":[{\"id\":\"1\",\"val\":\"A\"}]}", DateTime.Now);

        using var scope = _host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<DataInboundService>();
        await service.ProcessPendingPackets(CancellationToken.None);
        var stateWhilePaused = (await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID)).ProcessState;

        await _db.Updateable<ShareDataSubscription>()
            .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Active)
            .Where(s => s.ID == sub.ID)
            .ExecuteCommandAsync();

        // Act
        await service.ProcessPendingPackets(CancellationToken.None);

        // Assert
        Assert.Equal(BaseEnums.InboundProcessState.Pending, stateWhilePaused);
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updated.ProcessState);
    }

    /// <summary>
    /// Description: I05 - Tra ngược bộ mã khi nhận (khóa "cond" trong JSON đối tác ↔ trường nội bộ "val"): mã đối tác (partnerValue) được đổi về giá trị nội bộ (sourceValue);
    ///              giá trị đối tác để trống thì lấy dòng đánh dấu isDefault của bộ mã.
    ///              Câu ghi của gói chèn @records vào RawContent của một dòng đánh dấu để đối chiếu dữ liệu thực sự được ghi.
    ///              Nguồn: DataInboundService.Parse.cs MapCodeReverse (dòng 374-401), bộ mã lấy từ $extend.codeSet của phễu lọc.
    ///              Mã lạ chưa khai (D4 đảo) phụ thuộc DefaultSourceValue của bộ mã nên chưa kiểm ở đây.
    /// Created date: 12/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenCodeSetReverse_MapsPartnerValueAndFillsEmptyWithDefault_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, mapping) = await SeedInboundEnvironmentAsync(unique);
        var codeSetCode = $"CS_I05_{unique}";
        var valuesJson = """
            [
                { "sourceValue": "CONGESTED", "partnerValue": "UNCS_02", "displayName": "Ùn tắc" },
                { "sourceValue": "NORMAL", "partnerValue": "UNCS_01", "displayName": "Bình thường", "isDefault": true }
            ]
            """;
        await _db.Insertable(new ShareDataCodeSet
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = codeSetCode,
            Name = "Bộ mã I05",
            Status = BaseEnums.StatusEnum.Enable,
            ValuesJson = valuesJson
        }).ExecuteCommandAsync();

        var shape = $$"""
            { "data": [ { "id": { "$field": "id" }, "cond": { "$field": "val", "$extend": { "codeSet": "{{codeSetCode}}" } } } ] }
            """;
        await _db.Updateable<ShareDataMapping>().SetColumns(m => m.TargetShapeJson == shape).Where(m => m.ID == mapping.ID).ExecuteCommandAsync();

        var markerId = $"MARK_I05_{unique}";
        var markerSql = $"INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode, RawContent) OUTPUT 'INSERT' VALUES ('{markerId}', 'MARK', 'MARK', @records)";
        await _db.Updateable<ShareDataPacketSql>().SetColumns(x => x.WriteSql == markerSql).Where(x => x.PacketCode == packet.Code).ExecuteCommandAsync();
        var packetEntity = await SeedPendingPacket(partner.Code!, packet.Code!, 1,
            "{\"data\":[{\"id\":\"R1\",\"cond\":\"UNCS_02\"},{\"id\":\"R2\",\"cond\":\"\"}]}", DateTime.Now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.True(updated.ProcessState == BaseEnums.InboundProcessState.Done, $"ProcessState={updated.ProcessState}, Error={updated.ErrorMessage}");

        var marker = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(markerId);
        using var doc = System.Text.Json.JsonDocument.Parse(marker.RawContent!);
        var conds = doc.RootElement.EnumerateArray().ToDictionary(r => r.GetProperty("id").GetString()!, r => r.GetProperty("val").GetString());
        Assert.Equal("CONGESTED", conds["R1"]);
        Assert.Equal("NORMAL", conds["R2"]);
    }

    /// <summary>
    /// Description: I05b - Mã đối tác LẠ (không khai trong bộ mã) khi tra ngược: bộ mã có defaultSourceValue thì đổi sang mặc định nội bộ;
    ///              bộ mã không khai mặc định thì giữ nguyên giá trị đối tác (để lần ra dữ liệu bẩn). Mã đã khai vẫn map đúng.
    ///              Nguồn: DataInboundService.Parse.cs MapCodeReverse (D4 đảo, dòng 401-403).
    /// Created date: 12/10/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessPendingPackets_WhenPartnerCodeIsUnknown_UsesDefaultSourceValueOrKeepsRaw_Test(bool hasDefault)
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, mapping) = await SeedInboundEnvironmentAsync(unique);
        var codeSetCode = $"CS_I05B_{unique}";
        var defaultPart = hasDefault ? ", \"defaultSourceValue\": \"UNKNOWN\"" : string.Empty;
        var valuesJson = "{ \"values\": [ { \"sourceValue\": \"CONGESTED\", \"partnerValue\": \"UNCS_02\", \"displayName\": \"Ùn tắc\" } ]" + defaultPart + " }";
        await _db.Insertable(new ShareDataCodeSet
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = codeSetCode,
            Name = "Bộ mã I05b",
            Status = BaseEnums.StatusEnum.Enable,
            ValuesJson = valuesJson
        }).ExecuteCommandAsync();

        var shape = $$"""
            { "data": [ { "id": { "$field": "id" }, "cond": { "$field": "val", "$extend": { "codeSet": "{{codeSetCode}}" } } } ] }
            """;
        await _db.Updateable<ShareDataMapping>().SetColumns(m => m.TargetShapeJson == shape).Where(m => m.ID == mapping.ID).ExecuteCommandAsync();

        var markerId = $"MARK_I05B_{unique}";
        var markerSql = $"INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode, RawContent) OUTPUT 'INSERT' VALUES ('{markerId}', 'MARK', 'MARK', @records)";
        await _db.Updateable<ShareDataPacketSql>().SetColumns(x => x.WriteSql == markerSql).Where(x => x.PacketCode == packet.Code).ExecuteCommandAsync();
        var packetEntity = await SeedPendingPacket(partner.Code!, packet.Code!, 1,
            "{\"data\":[{\"id\":\"R1\",\"cond\":\"UNCS_02\"},{\"id\":\"R2\",\"cond\":\"ZZZ_99\"}]}", DateTime.Now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.True(updated.ProcessState == BaseEnums.InboundProcessState.Done, $"ProcessState={updated.ProcessState}, Error={updated.ErrorMessage}");

        var marker = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(markerId);
        using var doc = System.Text.Json.JsonDocument.Parse(marker.RawContent!);
        var conds = doc.RootElement.EnumerateArray().ToDictionary(r => r.GetProperty("id").GetString()!, r => r.GetProperty("val").GetString());
        Assert.Equal("CONGESTED", conds["R1"]);
        Assert.Equal(hasDefault ? "UNKNOWN" : "ZZZ_99", conds["R2"]);
    }

    /// <summary>
    /// Description: B1 - 18 câu ghi chiều nhận trong kịch bản seed 02 biên dịch thành công (0 lỗi)
    ///              với lược đồ CSDL Inbound hiện hành khi kiểm tra qua sys.sp_describe_first_result_set.
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    public async Task SeedWriteSql_WhenCompiledOnInboundDatabase_AllStatementsBindToCurrentSchema_Test()
    {
        // Arrange
        var statements = SeedScriptReader.ReadWriteSqlStatements();
        Assert.Equal(18, statements.Count);

        const string sqlParams = "@records nvarchar(max), @now datetime, @tenantId nvarchar(64), @operator nvarchar(64), @partnerId nvarchar(64), @partnerCode nvarchar(64), @datatypeId nvarchar(64), @serialNbr int, @packetId nvarchar(64)";

        // Act & Assert
        foreach (var stmt in statements)
        {
            var parameters = new SugarParameter[]
            {
                new("@tsql", stmt.WriteSql),
                new("@params", sqlParams)
            };

            var dt = await _inboundDb.Ado.GetDataTableAsync(
                "EXEC sys.sp_describe_first_result_set @tsql = @tsql, @params = @params;",
                parameters);

            if (dt.Columns.Contains("error_number") && dt.Rows.Count > 0 && dt.Rows[0]["error_number"] != DBNull.Value)
            {
                var errNum = Convert.ToInt32(dt.Rows[0]["error_number"]);
                var errMsg = dt.Rows[0]["error_message"]?.ToString();
                Assert.True(errNum == 0, $"Lỗi biên dịch câu ghi gói {stmt.PacketCode} bảng {stmt.TableName}: [{errNum}] {errMsg}");
            }
        }
    }

    /// <summary>
    /// Description: B2 - Câu MERGE thật của gói 101 giữ giá trị cũ khi đối tác bỏ trống trường.
    ///              Gói 1 tạo dòng TmsZone với Name=\"Đoạn 1\", MaxSpeed=80. Gói 2 cùng zoneId, bỏ trường zoneName, đổi speedLimit=100.
    ///              Sau khi xử lý, Name giữ nguyên \"Đoạn 1\", MaxSpeed cập nhật thành 100, UpdateUId = \"SHAREDATA_INBOUND\".
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPartnerOmitsField_SeedMergeKeepsExistingValue_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var partnerId = Guid.NewGuid().ToString("N");
        var partnerCode = $"PARTNER_B2_{unique}";
        var partner = new ShareDataPartner
        {
            ID = partnerId,
            Code = partnerCode,
            Name = $"Đối tác B2 {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(partner).ExecuteCommandAsync();

        var packetCode = "101_commonData";
        var packet = await _db.Queryable<ShareDataPacket>().Where(p => p.Code == packetCode).FirstAsync();
        if (packet == null)
        {
            packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = "Gói 101 CommonData",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(packet).ExecuteCommandAsync();
        }

        var seedWrites = SeedScriptReader.ReadWriteSqlStatements().Where(s => s.PacketCode == packetCode).ToList();
        Assert.Equal(3, seedWrites.Count);

        var existingWrites = await _db.Queryable<ShareDataPacketSql>()
            .Where(w => w.PacketCode == packetCode && w.IsDelete == null)
            .ToListAsync();
        if (existingWrites.Count == 0)
        {
            var packetSqlEntities = seedWrites.Select(sw => new ShareDataPacketSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                TableName = sw.TableName,
                OrderNo = sw.OrderNo,
                WriteSql = sw.WriteSql,
                Status = BaseEnums.StatusEnum.Enable
            }).ToList();
            await _db.Insertable(packetSqlEntities).ExecuteCommandAsync();
        }

        var sub = new ShareDataSubscription
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packet.ID,
            Direction = BaseEnums.Direction.Inbound,
            State = BaseEnums.SubSubscriptionState.Active
        };
        await _db.Insertable(sub).ExecuteCommandAsync();

        var zoneId = $"ZONE_B2_{unique}";
        var content1 = $"{{\"data\":[{{\"zoneId\":\"{zoneId}\",\"zoneName\":\"Đoạn 1\",\"speedLimit\":80}}]}}";
        var content2 = $"{{\"data\":[{{\"zoneId\":\"{zoneId}\",\"speedLimit\":100}}]}}";

        var packet1 = await SeedPendingPacket(partnerCode, packetCode, 1, content1, DateTime.Now);
        var packet2 = await SeedPendingPacket(partnerCode, packetCode, 2, content2, DateTime.Now.AddSeconds(1));

        // Act
        using var scope = _host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<DataInboundService>();
        await service.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var p1Updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packet1.ID);
        var p2Updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packet2.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Done, p1Updated.ProcessState);
        Assert.Equal(BaseEnums.InboundProcessState.Done, p2Updated.ProcessState);

        var zoneRow = await _inboundDb.Ado.GetDataTableAsync(
            "SELECT Name, MaxSpeed, UpdateUId FROM dbo.TmsZone WHERE ID = @id",
            new { id = zoneId });
        Assert.True(zoneRow.Rows.Count > 0, $"Không tìm thấy bản ghi TmsZone với ID = {zoneId}");
        Assert.Equal("Đoạn 1", zoneRow.Rows[0]["Name"]?.ToString());
        Assert.Equal(100, Convert.ToInt32(zoneRow.Rows[0]["MaxSpeed"]));
        Assert.Equal("SHAREDATA_INBOUND", zoneRow.Rows[0]["UpdateUId"]?.ToString());
    }

    /// <summary>
    /// Description: B3 - Khi CSDL hủy transaction (SET XACT_ABORT ON), RollbackQuietly bắt êm lỗi hủy giao dịch
    ///              để không làm gián đoạn lượt quét; gói 1 đánh dấu Failed và gói 2 kế tiếp được xử lý thành Done.
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenServerAbortsTransaction_KeepsSqlErrorAndProcessesNextPacket_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, mapping) = await SeedInboundEnvironmentAsync(unique);

        var shape = """
            {
                "data": [
                    {
                        "id": { "$field": "id" },
                        "isError": { "$field": "isError" }
                    }
                ]
            }
            """;
        await _db.Updateable<ShareDataMapping>()
            .SetColumns(m => m.TargetShapeJson == shape)
            .Where(m => m.ID == mapping.ID)
            .ExecuteCommandAsync();

        var abortSql = """
            SET XACT_ABORT ON;
            IF EXISTS (SELECT 1 FROM OPENJSON(@records) WITH (isError bit '$.isError') WHERE isError = 1)
                SELECT CAST('not-a-number' AS INT);
            ELSE
                SELECT 'INSERT';
            """;

        await _db.Updateable<ShareDataPacketSql>()
            .SetColumns(s => s.WriteSql == abortSql)
            .Where(s => s.PacketCode == packet.Code)
            .ExecuteCommandAsync();

        var content1 = "{\"data\":[{\"id\":\"1\",\"isError\":true}]}";
        var content2 = "{\"data\":[{\"id\":\"2\",\"isError\":false}]}";

        var packet1 = await SeedPendingPacket(partner.Code!, packet.Code!, 1, content1, DateTime.Now);
        var packet2 = await SeedPendingPacket(partner.Code!, packet.Code!, 2, content2, DateTime.Now.AddSeconds(1));

        // Act
        using var scope = _host.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<DataInboundService>();
        await service.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated1 = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packet1.ID);
        var updated2 = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packet2.ID);

        Assert.Equal(BaseEnums.InboundProcessState.Failed, updated1.ProcessState);
        Assert.Contains("SqlTransaction has completed", updated1.ErrorMessage);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updated2.ProcessState);
    }

    /// <summary>
    /// Description: B4 - Khi cấu hình InboundMaxPacketsPerRun = 1, mỗi lượt chạy chỉ xử lý đúng 1 gói,
    ///              các gói còn lại vẫn giữ nguyên Pending.
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenMaxPacketsPerRunConfigured_ProcessesOnlyThatMany_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var isolatedGroup = $"sd_cfg_b4_max_{unique}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique);
        var p1 = await SeedPendingPacket(partner.Code!, packet.Code!, 1, "{\"data\":[{\"id\":\"1\",\"val\":\"A\"}]}", DateTime.Now);
        var p2 = await SeedPendingPacket(partner.Code!, packet.Code!, 2, "{\"data\":[{\"id\":\"2\",\"val\":\"B\"}]}", DateTime.Now.AddSeconds(1));
        var p3 = await SeedPendingPacket(partner.Code!, packet.Code!, 3, "{\"data\":[{\"id\":\"3\",\"val\":\"C\"}]}", DateTime.Now.AddSeconds(2));

        var cfgRow = new SysOpConfig
        {
            ID = Guid.NewGuid().ToString("N"),
            GroupCode = isolatedGroup,
            Code = ShareDataConst.ConfigKeyInboundMaxPacketsPerRun,
            Value = "1",
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(cfgRow).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var workerConfig = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);
        var service = new DataInboundService(scopeFactory, NullLogger<DataInboundService>.Instance, workerConfig);

        // Act
        await service.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated1 = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(p1.ID);
        var updated2 = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(p2.ID);
        var updated3 = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(p3.ID);

        Assert.Equal(BaseEnums.InboundProcessState.Done, updated1.ProcessState);
        Assert.Equal(BaseEnums.InboundProcessState.Pending, updated2.ProcessState);
        Assert.Equal(BaseEnums.InboundProcessState.Pending, updated3.ProcessState);
    }

    /// <summary>
    /// Description: B4 - Khi cấu hình InboundStuckTimeoutMin nhỏ hơn mặc định 10 phút,
    ///              gói bị kẹt ở Processing quá thời gian cấu hình được nhả về Pending và xử lý thành Done.
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenStuckTimeoutConfigured_ReleasesPacketAfterConfiguredMinutes_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var isolatedGroup = $"sd_cfg_b4_stuck_{unique}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique);

        var cfgRow = new SysOpConfig
        {
            ID = Guid.NewGuid().ToString("N"),
            GroupCode = isolatedGroup,
            Code = ShareDataConst.ConfigKeyInboundStuckTimeoutMin,
            Value = "2",
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(cfgRow).ExecuteCommandAsync();

        var stuckPacket = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 1,
            RawContent = "{\"data\":[{\"id\":\"STUCK\",\"val\":\"Val\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Processing,
            ProcessedAt = DateTime.Now.AddMinutes(-4), // 4 phút trước: vượt ngưỡng 2 phút cấu hình nhưng chưa tới 10 phút mặc định
            CreateTime = DateTime.Now.AddMinutes(-5)
        };
        await _inboundDb.Insertable(stuckPacket).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var workerConfig = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);
        var service = new DataInboundService(scopeFactory, NullLogger<DataInboundService>.Instance, workerConfig);

        // Act
        await service.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(stuckPacket.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updated.ProcessState);
    }

    /// <summary>
    /// Description: Seed một gói Pending vào CSDL Inbound. CreateTime do AOP gán lúc insert nên thứ tự nhận được quyết định
    ///              bởi thứ tự gọi hàm này (cách nhau 30ms để mốc thời gian phân biệt được).
    /// Created date: 12/10/2026
    /// </summary>
    private async Task<ShareDataInboundPacket> SeedPendingPacket(string partnerCode, string packetCode, int? serialNbr, string rawContent, DateTime createTime)
    {
        var entity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partnerCode,
            PacketCode = packetCode,
            SerialNbr = serialNbr,
            RawContent = rawContent,
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = createTime
        };
        await _inboundDb.Insertable(entity).ExecuteCommandAsync();
        await Task.Delay(30);
        return entity;
    }

    #endregion
}
