using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Logging;
using Shared.DTO.Enums;

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
