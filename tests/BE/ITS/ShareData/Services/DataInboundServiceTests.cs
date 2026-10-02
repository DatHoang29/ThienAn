using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Controllers.Inbound.Commands;
using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Dto.Inbound;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure.Services.ShareDataActivityService;
using ShareDataWorker.Core.Interfaces;
using ShareDataWorker.Infrastructure.Logging;
using Shared.DTO.Enums;
using Tests.ShareData.Mocks;

namespace Tests.ShareData.Services;

/// <summary>
/// Description: Bộ kiểm thử tích hợp cho luồng NHẬN dữ liệu (SV-8b: Log 2 bước cha - con)
///              Kiểm thử WebAPI InboundCommandHandler, Worker DataInboundService, và ActivityLogger.
/// Created date: 02/10/2026
/// </summary>
[Collection("api")]
public class DataInboundServiceTests(Host host)
{
    private readonly Host _host = host;
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
    private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();

    #region Helper Seed Data

    private async Task<(ShareDataPartner Partner, ShareDataPacket Packet, ShareDataSubscription Subscription, ShareDataMapping Mapping)>
        SeedInboundEnvironmentAsync(string unique, bool createSub = true)
    {
        var partnerId = Guid.NewGuid().ToString("N");
        var partner = new ShareDataPartner
        {
            ID = partnerId,
            Code = $"PARTNER_IN_{unique}",
            Name = $"Đối tác Inbound {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(partner).ExecuteCommandAsync();

        var packetId = Guid.NewGuid().ToString("N");
        var packetCode = $"PKT_IN_{unique}";
        var packet = new ShareDataPacket
        {
            ID = packetId,
            Code = packetCode,
            Name = $"Gói tin Inbound {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(packet).ExecuteCommandAsync();

        // Câu ghi SQL cho gói tin: giả lập câu MERGE ... OUTPUT $action trả về INSERT cho mỗi bản ghi
        var packetSql = new ShareDataPacketSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PacketCode = packetCode,
            OrderNo = 1,
            Status = BaseEnums.StatusEnum.Enable,
            WriteSql = "SELECT 'INSERT' FROM OPENJSON(@records)"
        };
        await _db.Insertable(packetSql).ExecuteCommandAsync();

        ShareDataSubscription? sub = null;
        ShareDataMapping? mapping = null;

        if (createSub)
        {
            var subId = Guid.NewGuid().ToString("N");
            sub = new ShareDataSubscription
            {
                ID = subId,
                Code = $"SUB_IN_{unique}",
                PartnerId = partnerId,
                DatatypeId = packetId,
                Direction = BaseEnums.Direction.Inbound,
                State = BaseEnums.SubSubscriptionState.Active,
                SerialNbr = 0,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(sub).ExecuteCommandAsync();

            var mappingId = Guid.NewGuid().ToString("N");
            mapping = new ShareDataMapping
            {
                ID = mappingId,
                PartnerId = partnerId,
                DatatypeId = packetId,
                Direction = BaseEnums.Direction.Inbound,
                IsActive = true,
                TargetShapeJson = """
                {
                    "data": [
                        {
                            "id": { "$field": "id" },
                            "value": { "$field": "val" }
                        }
                    ]
                }
                """,
                UpdateTime = DateTime.Now
            };
            await _db.Insertable(mapping).ExecuteCommandAsync();
        }

        return (partner, packet, sub!, mapping!);
    }

    #endregion

    #region 1. Kiểm thử tôn trọng ID gán sẵn

    /// <summary>
    /// STT 1: ID gán sẵn có được tôn trọng
    /// Gọi LogTransferAsync(..., logId: "<guid>") => đọc lại CSDL thấy đúng dòng có ID bằng chuỗi đã truyền.
    /// </summary>
    [Fact]
    public async Task LogTransferAsync_WhenLogIdProvided_PreservesGivenIdInDatabase_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ShareDataActivityLogger>();
        var customLogId = Guid.NewGuid().ToString("N");
        var partnerId = Guid.NewGuid().ToString("N");

        // Act
        var returnedId = await logger.LogTransferAsync(
            BaseEnums.TransferDirection.RCV,
            partnerId: partnerId,
            partnerName: "Partner Test",
            subscriptionId: null,
            sessionId: null,
            datatypeId: "101",
            pduType: "DATA",
            logId: customLogId);

        // Assert
        Assert.Equal(customLogId, returnedId);

        var saved = await _db.Queryable<ShareDataActivityLog>().InSingleAsync(customLogId);
        Assert.NotNull(saved);
        Assert.Equal(customLogId, saved.ID);
        Assert.Equal(partnerId, saved.PartnerId);
        Assert.Equal(BaseEnums.TransferDirection.RCV, saved.TransferDirection);
        Assert.Null(saved.ParentId);
        Assert.Null(saved.StepNo);
    }

    #endregion

    #region 2. WebAPI tiếp nhận gói: sinh 2 dòng (cha + con B1)

    /// <summary>
    /// STT 2: WebAPI tiếp nhận gói: sinh 2 dòng (cha + con B1)
    /// Gửi lệnh tiếp nhận gói hợp lệ => bảng ShareDataActivityLog xuất hiện đúng 1 dòng cha (ParentId = null, StepNo = null)
    /// và đúng 1 dòng con (ParentId = <cha>, StepNo = 1, Description có "Bước 1/2").
    /// Gói trong ShareDataInboundPacket có ReceiveLogId == <cha>.
    /// </summary>
    [Fact]
    public async Task InboundCommandHandler_WhenValidPacketReceived_LogsParentAndStep1Child_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        using var scope = _host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<InboundCommandHandler>();

        var input = new ShareDataAddInboundInput
        {
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            PacketVersion = "1.0",
            SerialNbr = 1,
            PduType = "DATA",
            Format = BaseEnums.PublishFormat.Data,
            RawContent = "{\"data\":[{\"id\":\"1\",\"val\":\"A\"}]}"
        };

        // Act
        await handler.HandleAsync(input);

        // Assert: Kiểm tra gói tin trong ShareDataInboundPacket
        var savedPacket = await _db.Queryable<ShareDataInboundPacket>()
            .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code)
            .FirstAsync();
        Assert.NotNull(savedPacket);
        Assert.False(string.IsNullOrWhiteSpace(savedPacket.ReceiveLogId));
        var parentLogId = savedPacket.ReceiveLogId!;

        // Kiểm tra dòng cha
        var parentLog = await _db.Queryable<ShareDataActivityLog>().InSingleAsync(parentLogId);
        Assert.NotNull(parentLog);
        Assert.Null(parentLog.ParentId);
        Assert.Null(parentLog.StepNo);
        Assert.Equal(BaseEnums.TransferDirection.RCV, parentLog.TransferDirection);
        Assert.Contains(packet.Code, parentLog.Description ?? "");

        // Kiểm tra dòng con bước 1
        var step1Log = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.ParentId == parentLogId && l.StepNo == InboundCommandHandler.StepReceive)
            .FirstAsync();
        Assert.NotNull(step1Log);
        Assert.Equal(parentLogId, step1Log.ParentId);
        Assert.Equal(1, step1Log.StepNo);
        Assert.Contains("Bước 1/2", step1Log.Description ?? "");
    }

    #endregion

    #region 3. Worker xử lý thành công: sinh con B2 và cập nhật cha

    /// <summary>
    /// STT 3: Worker xử lý thành công: sinh con B2 và cập nhật cha
    /// Tạo gói có ReceiveLogId hợp lệ trong ShareDataInboundPacket => chạy ProcessPendingPackets
    /// => bảng ShareDataActivityLog xuất hiện dòng con Bước 2 (ParentId = <cha>, StepNo = 2, Description có "Bước 2/2").
    /// Dòng cha được cập nhật Success = Success, RecordCount đúng số bản ghi đã nạp, Description có kết quả cuối.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPacketSucceeds_LogsStep2AndUpdatesParent_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, mapping) = await SeedInboundEnvironmentAsync(unique);

        var parentLogId = Guid.NewGuid().ToString("N");
        // Giả lập dòng cha do WebAPI đã ghi (chờ xử lý)
        await _db.Insertable(new ShareDataActivityLog
        {
            ID = parentLogId,
            SubscriptionId = sub.ID,
            PartnerId = partner.ID,
            PartnerName = partner.Name,
            TransferDirection = BaseEnums.TransferDirection.RCV,
            DatatypeId = packet.Code,
            Success = BaseEnums.SuccessEnums.Success,
            RecordCount = 0,
            Description = $"Tiếp nhận gói tin loại {packet.Code} từ đối tác \"{partner.Name}\" — chờ xử lý",
            OccurredAt = DateTime.Now,
            CreateTime = DateTime.Now
        }).ExecuteCommandAsync();

        // Gói tin Pending có ReceiveLogId trỏ tới parentLogId
        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            PacketVersion = "1.0",
            SerialNbr = 1,
            PduType = "DATA",
            Format = BaseEnums.PublishFormat.Data,
            RawContent = "{\"data\":[{\"id\":\"101\",\"val\":\"Val1\"},{\"id\":\"102\",\"val\":\"Val2\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            ReceiveLogId = parentLogId,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(packetEntity).ExecuteCommandAsync();

        // Act: Worker quét và xử lý
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói tin chuyển sang Done
        var updatedPacket = await _db.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updatedPacket.ProcessState);

        // Kiểm tra dòng con Bước 2
        var step2Log = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.ParentId == parentLogId && l.StepNo == 2)
            .FirstAsync();
        Assert.NotNull(step2Log);
        Assert.Equal(BaseEnums.SuccessEnums.Success, step2Log.Success);
        Assert.Contains("Bước 2/2", step2Log.Description ?? "");
        Assert.Equal(2, step2Log.RecordCount);

        // Kiểm tra dòng cha được cập nhật
        var updatedParent = await _db.Queryable<ShareDataActivityLog>().InSingleAsync(parentLogId);
        Assert.NotNull(updatedParent);
        Assert.Equal(BaseEnums.SuccessEnums.Success, updatedParent.Success);
        Assert.Equal(2, updatedParent.RecordCount);
        Assert.Null(updatedParent.ErrorMessage);
        Assert.Contains(partner.Name!, updatedParent.Description ?? "");
    }

    #endregion

    #region 4. Worker xử lý thất bại: sinh con B2 lỗi và cập nhật cha thất bại

    /// <summary>
    /// STT 4: Worker xử lý thất bại: sinh con B2 lỗi và cập nhật cha thất bại
    /// Tạo gói có nội dung hỏng (JSON sai cú pháp) => Worker xử lý => dòng con Bước 2 có Success = Fail, StepNo = 2.
    /// Dòng cha được cập nhật Success = Fail, ErrorMessage mang nội dung lỗi, RecordCount = 0.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPacketPayloadMalformed_LogsStep2FailedAndUpdatesParentFail_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        var parentLogId = Guid.NewGuid().ToString("N");
        await _db.Insertable(new ShareDataActivityLog
        {
            ID = parentLogId,
            SubscriptionId = sub.ID,
            PartnerId = partner.ID,
            PartnerName = partner.Name,
            TransferDirection = BaseEnums.TransferDirection.RCV,
            DatatypeId = packet.Code,
            Success = BaseEnums.SuccessEnums.Success,
            RecordCount = 0,
            Description = $"Tiếp nhận gói tin loại {packet.Code} từ đối tác \"{partner.Name}\" — chờ xử lý",
            OccurredAt = DateTime.Now,
            CreateTime = DateTime.Now
        }).ExecuteCommandAsync();

        // Gói tin Pending với JSON sai cú pháp
        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            PacketVersion = "1.0",
            SerialNbr = 1,
            PduType = "DATA",
            Format = BaseEnums.PublishFormat.Data,
            RawContent = "{ JSON_CORRUPT_NOT_VALID }}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            ReceiveLogId = parentLogId,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói tin chuyển sang Failed
        var updatedPacket = await _db.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updatedPacket.ProcessState);

        // Dòng con Bước 2 thất bại
        var step2Log = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.ParentId == parentLogId && l.StepNo == 2)
            .FirstAsync();
        Assert.NotNull(step2Log);
        Assert.Equal(BaseEnums.SuccessEnums.Fail, step2Log.Success);
        Assert.Contains("Bước 2/2", step2Log.Description ?? "");

        // Dòng cha được cập nhật Success = Fail
        var updatedParent = await _db.Queryable<ShareDataActivityLog>().InSingleAsync(parentLogId);
        Assert.NotNull(updatedParent);
        Assert.Equal(BaseEnums.SuccessEnums.Fail, updatedParent.Success);
        Assert.Equal(0, updatedParent.RecordCount);
        Assert.False(string.IsNullOrWhiteSpace(updatedParent.ErrorMessage));
    }

    #endregion

    #region 5. Worker xử lý gói không có đăng ký nhận: cập nhật cha thất bại

    /// <summary>
    /// STT 5: Worker xử lý gói không có đăng ký nhận: cập nhật cha thất bại
    /// Tạo gói mà loại gói chưa có đăng ký nhận => Worker quét => dòng cha được cập nhật Success = Fail,
    /// Description phản ánh lỗi. Không sinh dòng con bước 2.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenNoSubscription_UpdatesParentFailWithoutStep2_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        // Seed đối tác và gói tin nhưng KHÔNG tạo Subscription
        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique, createSub: false);

        var parentLogId = Guid.NewGuid().ToString("N");
        await _db.Insertable(new ShareDataActivityLog
        {
            ID = parentLogId,
            PartnerId = partner.ID,
            PartnerName = partner.Name,
            TransferDirection = BaseEnums.TransferDirection.RCV,
            DatatypeId = packet.Code,
            Success = BaseEnums.SuccessEnums.Success,
            RecordCount = 0,
            Description = $"Tiếp nhận gói tin loại {packet.Code} từ đối tác \"{partner.Name}\" — chờ xử lý",
            OccurredAt = DateTime.Now,
            CreateTime = DateTime.Now
        }).ExecuteCommandAsync();

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            PacketVersion = "1.0",
            SerialNbr = 1,
            PduType = "DATA",
            Format = BaseEnums.PublishFormat.Data,
            RawContent = "{\"data\":[]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            ReceiveLogId = parentLogId,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói tin chuyển sang NoSubscription
        var updatedPacket = await _db.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.NoSubscription, updatedPacket.ProcessState);

        // Không sinh dòng con Bước 2
        var step2Count = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.ParentId == parentLogId && l.StepNo == 2)
            .CountAsync();
        Assert.Equal(0, step2Count);

        // Dòng cha được cập nhật Success = Fail
        var updatedParent = await _db.Queryable<ShareDataActivityLog>().InSingleAsync(parentLogId);
        Assert.NotNull(updatedParent);
        Assert.Equal(BaseEnums.SuccessEnums.Fail, updatedParent.Success);
        Assert.False(string.IsNullOrWhiteSpace(updatedParent.ErrorMessage));
    }

    #endregion

    #region 6. Tương thích ngược: gói cũ ReceiveLogId == null

    /// <summary>
    /// STT 6: Tương thích ngược: gói cũ ReceiveLogId == null
    /// Tạo gói cũ có ReceiveLogId = null => Worker xử lý thành công => sinh đúng 1 dòng log cấp ngoài
    /// (ParentId = null, StepNo = null) như hành vi cũ. Không ném ngoại lệ, gói chuyển Done bình thường.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenLegacyPacketHasNullReceiveLogId_ProcessesSuccessfullyWithOldLog_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            PacketVersion = "1.0",
            SerialNbr = 1,
            PduType = "DATA",
            Format = BaseEnums.PublishFormat.Data,
            RawContent = "{\"data\":[{\"id\":\"L1\",\"val\":\"Legacy\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            ReceiveLogId = null, // Gói cũ không có ReceiveLogId
            CreateTime = DateTime.Now
        };
        await _db.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói chuyển Done bình thường
        var updatedPacket = await _db.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updatedPacket.ProcessState);

        // Sinh 1 dòng log độc lập với ParentId = null, StepNo = null
        var logs = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.SubscriptionId == sub.ID && l.TransferDirection == BaseEnums.TransferDirection.RCV)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Null(logs[0].ParentId);
        Assert.Null(logs[0].StepNo);
        Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
    }

    #endregion

    #region 7. Lưới không bị phình dòng & 8. API GetSteps trả đủ 2 bước

    /// <summary>
    /// STT 7: Lưới không bị phình dòng (ParentId == null lọc đúng dòng cha)
    /// STT 8: API GetSteps trả về đủ 2 bước lồng trong dòng cha
    /// </summary>
    [Fact]
    public async Task Inbound_FullPipeline_GridReturnsOnlyParent_AndGetStepsReturnsBothSteps_Test()
    {
        // Arrange: Chạy trọn vẹn luồng tiếp nhận qua WebAPI và xử lý qua Worker
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        using var scope = _host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<InboundCommandHandler>();

        // 1. WebAPI tiếp nhận gói
        await handler.HandleAsync(new ShareDataAddInboundInput
        {
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            PacketVersion = "1.0",
            SerialNbr = 1,
            PduType = "DATA",
            Format = BaseEnums.PublishFormat.Data,
            RawContent = "{\"data\":[{\"id\":\"P1\",\"val\":\"V1\"},{\"id\":\"P2\",\"val\":\"V2\"}]}"
        });

        // 2. Worker xử lý gói
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Act 7: Truy vấn phân trang cho lưới
        var pageInput = new ShareDataPageActivityLogInput
        {
            SubscriptionId = sub.ID,
            Page = 1,
            PageSize = 20
        };
        var pageResult = await _bus.InvokeAsync<SqlSugarPagedList<ShareDataPageActivityLogOutput>>(pageInput);

        // Assert 7: Lưới chỉ có đúng 1 dòng cha, không bị phình dòng con
        Assert.NotNull(pageResult);
        var gridRecords = pageResult.Records.ToList();
        Assert.Single(gridRecords);
        var parentGridLog = gridRecords[0];
        Assert.Null(parentGridLog.ParentId);
        Assert.Null(parentGridLog.StepNo);
        Assert.Equal(BaseEnums.SuccessEnums.Success, parentGridLog.Success);
        Assert.Equal(2, parentGridLog.RecordCount);

        // Act 8: Gọi API GetSteps tra cứu các bước của dòng cha
        var stepInput = new ShareDataStepActivityLogInput
        {
            ID = parentGridLog.ID
        };
        var stepResult = await _bus.InvokeAsync<List<ShareDataActivityLogOutput>>(stepInput);

        // Assert 8: API GetSteps trả về cây 1 nút gốc chứa đủ 2 bước con
        Assert.NotNull(stepResult);
        Assert.Single(stepResult);
        var rootNode = stepResult[0];
        Assert.Equal(parentGridLog.ID, rootNode.ID);
        Assert.NotNull(rootNode.Children);
        Assert.Equal(2, rootNode.Children.Count);

        var step1 = rootNode.Children.FirstOrDefault(c => c.StepNo == 1);
        var step2 = rootNode.Children.FirstOrDefault(c => c.StepNo == 2);

        Assert.NotNull(step1);
        Assert.Equal(parentGridLog.ID, step1.ParentId);
        Assert.Contains("Bước 1/2", step1.Description ?? "");
        Assert.Equal(BaseEnums.SuccessEnums.Success, step1.Success);

        Assert.NotNull(step2);
        Assert.Equal(parentGridLog.ID, step2.ParentId);
        Assert.Contains("Bước 2/2", step2.Description ?? "");
        Assert.Equal(BaseEnums.SuccessEnums.Success, step2.Success);
        Assert.Equal(2, step2.RecordCount);
    }

    #endregion
}
