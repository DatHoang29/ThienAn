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
    private readonly ISqlSugarClient _inboundDb = host.Services.GetRequiredService<ISqlSugarClient>().AsTenant().GetConnectionScope(ShareDataWorker.Extensions.ShareDataWorkerExtensions.InboundConnectionKey);
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

    #region 1. Kiểm thử ID tự sinh cho ActivityLog

    /// <summary>
    /// STT 1: ID tự sinh tự động bởi SqlSugar hook AOP DataExecuting.
    /// Gọi LogTransferAsync(...) => đọc lại CSDL thấy dòng log có ID tự sinh, không rỗng, và khớp returnedId.
    /// </summary>
    [Fact]
    public async Task LogTransferAsync_WhenInserted_AutoGeneratesIdInDatabase_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ShareDataActivityLogger>();
        var partnerId = Guid.NewGuid().ToString("N");

        // Act
        await logger.LogTransferAsync(
            BaseEnums.TransferDirection.RCV,
            partnerId: partnerId,
            partnerName: "Partner Test",
            subscriptionId: null,
            sessionId: null,
            datatypeId: "101",
            pduType: "DATA");

        // Assert
        var saved = await _db.Queryable<ShareDataActivityLog>()
            .Where(x => x.PartnerId == partnerId && x.DatatypeId == "101")
            .FirstAsync();
        Assert.NotNull(saved);
        Assert.False(string.IsNullOrWhiteSpace(saved.ID));
        Assert.Equal(partnerId, saved.PartnerId);
        Assert.Equal(BaseEnums.TransferDirection.RCV, saved.TransferDirection);
        Assert.Null(saved.ParentId);
        Assert.Null(saved.StepNbr);
    }

    #endregion

    #region 2. WebAPI tiếp nhận gói: ghi nhật ký tiếp nhận

    /// <summary>
    /// STT 2: WebAPI tiếp nhận gói: ghi nhật ký tiếp nhận
    /// Gửi lệnh tiếp nhận gói hợp lệ => bảng ShareDataActivityLog xuất hiện đúng 1 dòng nhật ký tiếp nhận
    /// Gói trong ShareDataInboundPacket được lưu ở trạng thái Pending.
    /// </summary>
    [Fact]
    public async Task InboundCommandHandler_WhenValidPacketReceived_LogsReceiveActivity_Test()
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
        var savedPacket = await _inboundDb.Queryable<ShareDataInboundPacket>()
            .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code)
            .FirstAsync();
        Assert.NotNull(savedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Pending, savedPacket.ProcessState);

        // Kiểm tra dòng nhật ký tiếp nhận do WebAPI ghi (Dòng cha và Dòng con Bước 1)
        var parentLog = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.SubscriptionId == sub.ID && l.TransferDirection == BaseEnums.TransferDirection.RCV && l.ParentId == null)
            .FirstAsync();
        Assert.NotNull(parentLog);
        Assert.Null(parentLog.ParentId);
        Assert.Null(parentLog.StepNbr);
        Assert.Equal(BaseEnums.TransferDirection.RCV, parentLog.TransferDirection);
        Assert.Contains(packet.Code, parentLog.Description ?? "");

        var step1Log = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.SubscriptionId == sub.ID && l.TransferDirection == BaseEnums.TransferDirection.RCV && l.ParentId == parentLog.ID)
            .FirstAsync();
        Assert.NotNull(step1Log);
        Assert.Equal(1, step1Log.StepNbr);
        Assert.Equal(parentLog.ID, step1Log.ParentId);
    }

    #endregion

    #region 3. Worker xử lý thành công: sinh con B2 và cập nhật cha

    /// <summary>
    /// STT 3: Worker xử lý thành công: sinh con B2 và cập nhật cha
    /// Tạo gói có ReceiveLogId hợp lệ trong ShareDataInboundPacket => chạy ProcessPendingPackets
    /// => bảng ShareDataActivityLog xuất hiện dòng con Bước 2 (ParentId = <cha>, StepNbr = 2, Description có "Bước 2/2").
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

        // Giả lập dòng con Bước 1 do WebAPI đã ghi
        await _db.Insertable(new ShareDataActivityLog
        {
            ID = Guid.NewGuid().ToString("N"),
            ParentId = parentLogId,
            StepNbr = 1,
            SubscriptionId = sub.ID,
            PartnerId = partner.ID,
            PartnerName = partner.Name,
            TransferDirection = BaseEnums.TransferDirection.RCV,
            DatatypeId = packet.Code,
            Success = BaseEnums.SuccessEnums.Success,
            Description = "Bước 1/2: Đã nhận payload (64 bytes)",
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
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act: Worker quét và xử lý
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói tin chuyển sang Done
        var updatedPacket = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updatedPacket.ProcessState);

        // Kiểm tra dòng con Bước 2
        var step2Log = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.ParentId == parentLogId && l.StepNbr == 2)
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
    /// Tạo gói có nội dung hỏng (JSON sai cú pháp) => Worker xử lý => dòng con Bước 2 có Success = Fail, StepNbr = 2.
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

        // Giả lập dòng con Bước 1 do WebAPI đã ghi
        await _db.Insertable(new ShareDataActivityLog
        {
            ID = Guid.NewGuid().ToString("N"),
            ParentId = parentLogId,
            StepNbr = 1,
            SubscriptionId = sub.ID,
            PartnerId = partner.ID,
            PartnerName = partner.Name,
            TransferDirection = BaseEnums.TransferDirection.RCV,
            DatatypeId = packet.Code,
            Success = BaseEnums.SuccessEnums.Success,
            Description = "Bước 1/2: Đã nhận payload (32 bytes)",
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
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói tin chuyển sang Failed
        var updatedPacket = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updatedPacket.ProcessState);

        // Dòng con Bước 2 thất bại
        var step2Log = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.ParentId == parentLogId && l.StepNbr == 2)
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
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói tin chuyển sang NoSubscription
        var updatedPacket = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.NoSubscription, updatedPacket.ProcessState);

        // Không sinh dòng con Bước 2
        var step2Count = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.ParentId == parentLogId && l.StepNbr == 2)
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
    /// (ParentId = null, StepNbr = null) như hành vi cũ. Không ném ngoại lệ, gói chuyển Done bình thường.
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
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói chuyển Done bình thường
        var updatedPacket = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updatedPacket.ProcessState);

        // Sinh 1 dòng log độc lập với ParentId = null, StepNbr = null
        var logs = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.SubscriptionId == sub.ID && l.TransferDirection == BaseEnums.TransferDirection.RCV)
            .ToListAsync();
        Assert.Single(logs);
        Assert.Null(logs[0].ParentId);
        Assert.Null(logs[0].StepNbr);
        Assert.Equal(BaseEnums.SuccessEnums.Success, logs[0].Success);
    }

    #endregion

    #region 7. Luồng Inbound đầy đủ: WebAPI tiếp nhận & Worker xử lý

    /// <summary>
    /// STT 7: Luồng Inbound đầy đủ: WebAPI tiếp nhận & Worker xử lý
    /// Chạy trọn vẹn luồng tiếp nhận qua WebAPI và xử lý qua Worker => bảng ActivityLog lưu đầy đủ các lượt
    /// tiếp nhận và xử lý chiều nhận, gói tin chuyển sang trạng thái Done.
    /// </summary>
    [Fact]
    public async Task Inbound_FullPipeline_ProcessesSuccessfully_Test()
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

        // Act: Truy vấn phân trang cho lưới
        var pageInput = new ShareDataPageActivityLogInput
        {
            SubscriptionId = sub.ID,
            Page = 1,
            PageSize = 20
        };
        var pageResult = await _bus.InvokeAsync<SqlSugarPagedList<ShareDataPageActivityLogOutput>>(pageInput);

        // Assert: Lưới hiển thị các bản ghi chiều nhận, gói tin chuyển trạng thái Done
        Assert.NotNull(pageResult);
        var gridRecords = pageResult.Records.ToList();
        Assert.NotEmpty(gridRecords);
        Assert.All(gridRecords, r => Assert.Equal(BaseEnums.TransferDirection.RCV, r.TransferDirection));

        var updatedPacket = await _inboundDb.Queryable<ShareDataInboundPacket>()
            .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code)
            .FirstAsync();
        Assert.NotNull(updatedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updatedPacket.ProcessState);
    }

    #endregion

    #region 8. Đối tác không tồn tại → Failed + PartnerInvalid

    /// <summary>
    /// STT 8: Đối tác không tồn tại → Failed + PartnerInvalid
    /// Gói Pending có PartnerCode không khớp đối tác nào trong CSDL
    /// => Worker đánh dấu Failed, ErrorMessage phản ánh lỗi đối tác.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPartnerNotFound_MarksPacketFailed_Test()
    {
        // Arrange
        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = $"GHOST_{Guid.NewGuid().ToString("N")[..8]}",
            PacketCode = "PKT_ANY",
            SerialNbr = 1,
            RawContent = "{}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now
        };
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updated.ProcessState);
        Assert.False(string.IsNullOrWhiteSpace(updated.ErrorMessage));
    }

    #endregion

    #region 9. Đối tác bị vô hiệu hóa → Failed + PartnerInvalid

    /// <summary>
    /// STT 9: Đối tác bị vô hiệu hóa (Disabled) → Failed + PartnerInvalid
    /// Partner.Status = Disable => Worker đánh dấu Failed, ErrorMessage chứa "trạng thái không sử dụng".
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPartnerDisabled_MarksPacketFailed_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var partnerId = Guid.NewGuid().ToString("N");
        var partnerCode = $"DIS_{unique}";
        await _db.Insertable(new ShareDataPartner
        {
            ID = partnerId,
            Code = partnerCode,
            Name = $"Disabled Partner {unique}",
            Status = BaseEnums.StatusEnum.Disable
        }).ExecuteCommandAsync();

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partnerCode,
            PacketCode = "PKT_ANY",
            SerialNbr = 1,
            RawContent = "{}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now
        };
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updated.ProcessState);
        Assert.Contains("trạng thái không sử dụng", updated.ErrorMessage ?? "");
    }

    #endregion

    #region 10. PacketCode không tìm thấy → Failed + PacketNotFound

    /// <summary>
    /// STT 10: PacketCode không tìm thấy → Failed + PacketNotFound
    /// Partner hợp lệ nhưng PacketCode không tồn tại trong ShareDataPacket
    /// => Worker đánh dấu Failed, ErrorMessage chứa "PacketCode".
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPacketCodeNotFound_MarksPacketFailed_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var partnerId = Guid.NewGuid().ToString("N");
        var partnerCode = $"P10_{unique}";
        await _db.Insertable(new ShareDataPartner
        {
            ID = partnerId,
            Code = partnerCode,
            Name = $"Partner {unique}",
            Status = BaseEnums.StatusEnum.Enable
        }).ExecuteCommandAsync();

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partnerCode,
            PacketCode = $"PKT_GHOST_{unique}",
            SerialNbr = 1,
            RawContent = "{}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now
        };
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updated.ProcessState);
        Assert.Contains("PacketCode", updated.ErrorMessage ?? "");
    }

    #endregion

    #region 11. Subscription Paused → giữ gói ở Pending

    /// <summary>
    /// STT 11: Subscription Paused → Skip, giữ gói ở Pending
    /// Đăng ký nhận đang tạm dừng => gói KHÔNG bị chuyển Failed mà giữ nguyên Pending chờ kích hoạt lại.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenSubscriptionPaused_KeepsPacketPending_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        // Chuyển đăng ký sang Paused
        await _db.Updateable<ShareDataSubscription>()
            .SetColumns(s => s.State == BaseEnums.SubSubscriptionState.Paused)
            .Where(s => s.ID == sub.ID)
            .ExecuteCommandAsync();

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 1,
            RawContent = "{\"data\":[{\"id\":\"1\",\"val\":\"A\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now
        };
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert: gói vẫn ở Pending, KHÔNG bị Failed
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Pending, updated.ProcessState);
    }

    #endregion

    #region 12. Không có câu ghi SQL → Failed + WriteSqlMissing

    /// <summary>
    /// STT 12: Không có câu ghi SQL (ShareDataPacketSql) → Failed + WriteSqlMissing
    /// Gói tin chưa khai câu ghi chiều nhận => Worker đánh dấu Failed.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenNoWriteSqlConfigured_MarksPacketFailed_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        // Xóa toàn bộ câu ghi SQL của gói tin này
        await _db.Deleteable<ShareDataPacketSql>()
            .Where(w => w.PacketCode == packet.Code)
            .ExecuteCommandAsync();

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 1,
            RawContent = "{\"data\":[{\"id\":\"1\",\"val\":\"A\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now
        };
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updated.ProcessState);
        Assert.Contains("chưa khai câu ghi", updated.ErrorMessage ?? "");
    }

    #endregion

    #region 13. Payload XML → Failed + ParseFailed

    /// <summary>
    /// STT 13: Payload XML → Failed + ParseFailed
    /// Luồng nhận hiện chỉ hỗ trợ JSON. Gửi XML => Worker đánh dấu Failed, ErrorMessage chứa "XML".
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPayloadIsXml_MarksPacketFailed_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 1,
            RawContent = "<root><item id=\"1\"><value>ABC</value></item></root>",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            ReceiveLogId = Guid.NewGuid().ToString("N"),
            CreateTime = DateTime.Now
        };
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updated.ProcessState);
        Assert.Contains("XML", updated.ErrorMessage ?? "");
    }

    #endregion

    #region 14. Gói kẹt Processing quá timeout → release về Pending và xử lý

    /// <summary>
    /// STT 14: Gói kẹt Processing quá timeout → release về Pending và xử lý tiếp
    /// Worker tắt giữa chừng để lại gói ở Processing quá 10 phút => lượt chạy tiếp gỡ kẹt
    /// trả về Pending rồi xử lý thành công (Done).
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPacketStuckInProcessing_ReleasesAndProcesses_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 1,
            RawContent = "{\"data\":[{\"id\":\"S1\",\"val\":\"Stuck\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Processing,
            ProcessedAt = DateTime.Now.AddMinutes(-15), // Quá 10 phút timeout
            CreateTime = DateTime.Now
        };
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert: Gói bị kẹt đã được gỡ và xử lý thành công
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updated.ProcessState);
    }

    #endregion

    #region 15. Nhiều gói cùng nhóm — gói lỗi không chặn gói sau

    /// <summary>
    /// STT 15: Nhiều gói cùng nhóm — gói lỗi không chặn gói sau
    /// 2 gói cùng PartnerCode + PacketCode: gói 1 JSON hỏng (Failed), gói 2 hợp lệ (Done).
    /// Xác nhận cơ chế xử lý độc lập từng gói.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenOnePacketFails_ContinuesProcessingNext_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        var badPacket = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 1,
            RawContent = "NOT_VALID_JSON",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now
        };

        var goodPacket = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 2,
            RawContent = "{\"data\":[{\"id\":\"G1\",\"val\":\"OK\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now.AddSeconds(1)
        };

        await _inboundDb.Insertable(badPacket).ExecuteCommandAsync();
        await _inboundDb.Insertable(goodPacket).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert: gói 1 Failed, gói 2 Done — không bị chặn
        var updatedBad = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(badPacket.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updatedBad.ProcessState);

        var updatedGood = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(goodPacket.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updatedGood.ProcessState);
    }

    #endregion

    #region 16. SerialNbr cập nhật đúng sau khi xử lý thành công

    /// <summary>
    /// STT 16: SerialNbr cập nhật đúng sau khi xử lý nhiều gói thành công
    /// 2 gói SerialNbr = 5 và 10 => Subscription.SerialNbr = 10 (lấy max), LastTimeRun được cập nhật.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenMultiplePacketsSucceed_UpdatesSubscriptionSerialNbr_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        var packet1 = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 5,
            RawContent = "{\"data\":[{\"id\":\"A\",\"val\":\"1\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now
        };

        var packet2 = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 10,
            RawContent = "{\"data\":[{\"id\":\"B\",\"val\":\"2\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now.AddSeconds(1)
        };

        await _inboundDb.Insertable(packet1).ExecuteCommandAsync();
        await _inboundDb.Insertable(packet2).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert: Subscription.SerialNbr phải bằng 10 (max)
        var updatedSub = await _db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);
        Assert.Equal(10, updatedSub.SerialNbr);
        Assert.NotNull(updatedSub.LastTimeRun);
    }

    #endregion

    #region 17. Fallback không có phễu lọc — dùng khoá quy ước "data"

    /// <summary>
    /// STT 17: Fallback không có phễu lọc (TargetShapeJson = null)
    /// Gói tin có key "data" ở gốc => Worker đọc theo đường lùi quy ước, xử lý thành công.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenNoMappingShape_UsesFallbackAndSucceeds_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, mapping) = await SeedInboundEnvironmentAsync(unique);

        // Xóa TargetShapeJson để kích hoạt đường lùi fallback
        await _db.Updateable<ShareDataMapping>()
            .SetColumns(m => m.TargetShapeJson == null)
            .Where(m => m.ID == mapping.ID)
            .ExecuteCommandAsync();

        var packetEntity = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = 1,
            RawContent = "{\"data\":[{\"id\":\"FB1\",\"val\":\"Fallback\"}]}",
            ProcessState = BaseEnums.InboundProcessState.Pending,
            CreateTime = DateTime.Now
        };
        await _inboundDb.Insertable(packetEntity).ExecuteCommandAsync();

        // Act
        using var scope = _host.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert: xử lý thành công dù không có phễu lọc
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updated.ProcessState);
    }

    #endregion
}
