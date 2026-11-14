using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Dto.Inbound;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Logging;
using Shared.DTO.Enums;
using Tests.ShareData.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Module.ShareData.Core.Constants;
using Modules.CfgSystem.Core.Entities;
using Modules.TMS.Core.Entities;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using Tests.ShareData.Common;
using System.Text.RegularExpressions;

namespace Tests.ShareData.Services;

/// <summary>
/// Description: Bộ kiểm thử tích hợp cho luồng NHẬN dữ liệu (SV-8b: Log 2 bước cha - con)
///              Kiểm thử Worker DataInboundService và ActivityLogger.
/// Created date: 02/10/2026
/// Modified date: 24/10/2026
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
        await ShareDataSqlSeed.SetWriteAsync(_db, packetId, "SELECT 'INSERT' FROM OPENJSON(@records)");

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
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, "SELECT 'INSERT' FROM OPENJSON(@records); SELECT 'UPDATE';");

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
        var inboundService = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        Assert.Contains("thêm 2, sửa 1", step2Log.Description ?? "");
        Assert.Equal(3, step2Log.RecordCount);

        // Kiểm tra dòng cha được cập nhật
        var updatedParent = await _db.Queryable<ShareDataActivityLog>().InSingleAsync(parentLogId);
        Assert.NotNull(updatedParent);
        Assert.Equal(BaseEnums.SuccessEnums.Success, updatedParent.Success);
        Assert.Equal(3, updatedParent.RecordCount);
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
        var inboundService = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var inboundService = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var inboundService = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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

        // 1. WebAPI tiếp nhận gói qua Wolverine Bus
        await _bus.InvokeAsync(new ShareDataAddInboundInput
        {
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            PacketVersion = "1.0",
            SerialNbr = 1,
            PduType = "DATA",
            Format = BaseEnums.PublishFormat.Data,
            RawContent = "{\"data\":[{\"id\":\"P1\",\"val\":\"V1\"},{\"id\":\"P2\",\"val\":\"V2\"}]}"
        });

        using var scope = _host.Services.CreateScope();

        // 2. Worker xử lý gói
        var inboundService = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Failed, updated.ProcessState);
        Assert.Contains("PacketCode", updated.ErrorMessage ?? "");
    }

    #endregion

    #region 12. Không có câu ghi SQL → Failed + WriteSqlMissing

    /// <summary>
    /// STT 12: Không có câu ghi SQL (ShareDataSql.WriteSql) → Failed + WriteSqlMissing
    /// Gói tin chưa khai câu ghi chiều nhận => Worker đánh dấu Failed.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenNoWriteSqlConfigured_MarksPacketFailed_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);

        // Xóa toàn bộ câu ghi SQL của gói tin này
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, null);

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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
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
        var svc = scope.ServiceProvider.GetRequiredService<DataInboundService>();
        await svc.ProcessPendingPackets(CancellationToken.None);

        // Assert: xử lý thành công dù không có phễu lọc
        var updated = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(packetEntity.ID);
        Assert.Equal(BaseEnums.InboundProcessState.Done, updated.ProcessState);
    }

    #endregion

    #region Pipeline

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
        // Một khối hai câu: câu thứ 2 lỗi thì câu thứ 1 (đã chạy) phải bị quay lui
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, markerSql + ";\nSELECT CAST('not-a-number' AS INT)");
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
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, markerSql);
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
    /// Author: DatHQ
    /// Description: Đối tác có câu ghi riêng thì Worker chạy câu riêng, không chạy câu mẫu chung; đối tác khác vẫn chạy câu mẫu chung.
    /// Created date: 05/11/2026
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessPendingPackets_WhenPartnerHasOwnWriteSql_UsesItInsteadOfCommon_Test(bool hasOwnWriteSql)
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique);
        var commonMarker = $"MARK_COMMON_{unique}";
        var partnerMarker = $"MARK_PARTNER_{unique}";
        static string MarkerSql(string marker) => $"INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode) OUTPUT 'INSERT' VALUES ('{marker}', 'MARK', 'MARK')";
        var commonSql = MarkerSql(commonMarker);
        var partnerSql = MarkerSql(partnerMarker);
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, commonSql);
        if (hasOwnWriteSql)
        {
            await _db.Insertable(new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PacketId = packet.ID, PartnerId = partner.ID, WriteSql = partnerSql }).ExecuteCommandAsync();
        }
        await SeedPendingPacket(partner.Code!, packet.Code!, 1, "{\"data\":[{\"id\":\"A\",\"val\":\"1\"}]}", DateTime.Now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var ranPartnerSql = await _inboundDb.Queryable<ShareDataInboundPacket>().Where(p => p.ID == partnerMarker).AnyAsync();
        var ranCommonSql = await _inboundDb.Queryable<ShareDataInboundPacket>().Where(p => p.ID == commonMarker).AnyAsync();
        Assert.Equal(hasOwnWriteSql, ranPartnerSql);
        Assert.Equal(!hasOwnWriteSql, ranCommonSql);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Câu ghi nhận đủ tham số ngữ cảnh của gói: @records, @now, @tenantId, @operator, @partnerId, @partnerCode, @datatypeId, @serialNbr, @packetId.
    ///              Câu ghi chép giá trị các tham số vào một dòng đánh dấu, đọc lại đúng từng giá trị.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenWriteSqlUsesContextParameters_BindsEachValue_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, _) = await SeedInboundEnvironmentAsync(unique);
        var markerId = $"MARK_PARAM_{unique}";
        var markerSql = $"""
            INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode, ErrorMessage)
            OUTPUT 'INSERT'
            SELECT '{markerId}', @partnerCode, @datatypeId,
                CONCAT_WS('|', @partnerId, @tenantId, @operator, @serialNbr, @packetId, (SELECT COUNT(*) FROM OPENJSON(@records)), CONVERT(varchar(10), @now, 120))
            """;
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, markerSql);
        var pending = await SeedPendingPacket(partner.Code!, packet.Code!, 7, "{\"data\":[{\"id\":\"A\",\"val\":\"1\"},{\"id\":\"B\",\"val\":\"2\"}]}", DateTime.Now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var marker = await _inboundDb.Queryable<ShareDataInboundPacket>().Where(p => p.ID == markerId).FirstAsync();
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var expectedValues = string.Join('|', partner.ID, "Default", "SHAREDATA_INBOUND", 7, pending.ID, 2, today);
        Assert.NotNull(marker);
        Assert.Equal(partner.Code, marker.PartnerCode);
        Assert.Equal(packet.Code, marker.PacketCode);
        Assert.Equal(expectedValues, marker.ErrorMessage);
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
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, markerSql);
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
    /// Author: DatHQ
    /// Description: Lá có bộ mã không bị ép kiểu trước khi tra ngược: mã đối tác "05" vẫn tra ra giá trị nội bộ, không thành 5 rồi lệch mã.
    ///              Lá không có bộ mã thì gỡ ép kiểu theo targetType của bộ khung.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenCodeSetLeafHasTargetType_KeepsPartnerCodeForReverseLookup_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, _, mapping) = await SeedInboundEnvironmentAsync(unique);
        var codeSetCode = $"CS_TYPE_{unique}";
        await _db.Insertable(new ShareDataCodeSet
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = codeSetCode,
            Name = "Bộ mã có số 0 đầu",
            Status = BaseEnums.StatusEnum.Enable,
            ValuesJson = """[ { "sourceValue": "NORMAL", "partnerValue": "05" }, { "sourceValue": "CONGESTED", "partnerValue": "5" } ]"""
        }).ExecuteCommandAsync();
        var shape = $$"""
            { "data": [ {
                "cond": { "$field": "val", "$extend": { "codeSet": "{{codeSetCode}}", "targetType": "int" } },
                "count": { "$field": "num", "$extend": { "targetType": "int" } }
            } ] }
            """;
        await _db.Updateable<ShareDataMapping>().SetColumns(m => m.TargetShapeJson == shape).Where(m => m.ID == mapping.ID).ExecuteCommandAsync();
        var markerId = $"MARK_TYPE_{unique}";
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, $"INSERT INTO ShareDataInboundPacket (ID, PartnerCode, PacketCode, RawContent) OUTPUT 'INSERT' VALUES ('{markerId}', 'MARK', 'MARK', @records)");
        await SeedPendingPacket(partner.Code!, packet.Code!, 1, "{\"data\":[{\"cond\":\"05\",\"count\":\"12\"}]}", DateTime.Now);

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var marker = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(markerId);
        Assert.NotNull(marker);
        using var doc = System.Text.Json.JsonDocument.Parse(marker.RawContent!);
        var records = doc.RootElement.EnumerateArray();
        var record = Assert.Single(records);
        var internalCode = record.GetProperty("val").GetString();
        var vehicleCount = record.GetProperty("num").GetInt32();
        Assert.Equal("NORMAL", internalCode);
        Assert.Equal(12, vehicleCount);
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
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, markerSql);
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
    /// Description: B1 - 9 khối câu ghi chiều nhận (gộp nhiều bảng mỗi gói) trong kịch bản seed 02 biên dịch thành công (0 lỗi)
    ///              với lược đồ CSDL Inbound hiện hành khi kiểm tra qua sys.sp_describe_first_result_set.
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    public async Task SeedWriteSql_WhenCompiledOnInboundDatabase_AllStatementsBindToCurrentSchema_Test()
    {
        // Arrange
        var statements = SeedScriptReader.ReadWriteSqlStatements();
        Assert.Equal(9, statements.Count);

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
        Assert.Single(seedWrites);

        if (string.IsNullOrWhiteSpace(await ShareDataSqlSeed.GetWriteSqlAsync(_db, packet.ID)))
        {
            await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, seedWrites[0].WriteSql, seedWrites[0].AffectedTables);
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
    /// Author: DatHQ
    /// Description: Câu ghi mẫu gói 104 (TmsWeather), 106 (TmsTrafficData): cùng một bản ghi tới hai lần, thời gian có mili-giây lẻ
    ///              (cột đích kiểu datetime chỉ lưu theo bước 1/300 giây) vẫn khớp dòng đã ghi: cập nhật dòng đó, không chèn dòng thứ hai.
    /// Created date: 13/11/2026
    /// </summary>
    [Theory]
    [InlineData("104_weatherData", "weatherStationId", "temperature", "TmsWeather", "RefId", "Temperature")]
    [InlineData("106_wimData", "locationCode", "speed", "TmsTrafficData", "Location", "Speed")]
    public async Task ProcessPendingPackets_WhenSameRecordArrivesTwiceWithOddMilliseconds_SeedMergeUpdatesOneRow_Test(
        string seedPacketCode, string keyField, string valueField, string table, string keyColumn, string valueColumn)
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var partnerCode = $"PARTNER_DUP_{unique}";
        var partner = new ShareDataPartner
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = partnerCode,
            Name = $"Đối tác gửi lại {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(partner).ExecuteCommandAsync();

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_DUP_{unique}",
            Name = $"Gói {seedPacketCode} {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(packet).ExecuteCommandAsync();
        var seedWrite = SeedScriptReader.ReadWriteSqlStatements().Single(s => s.PacketCode == seedPacketCode);
        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, seedWrite.WriteSql, seedWrite.AffectedTables);

        await _db.Insertable(new ShareDataSubscription
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packet.ID,
            Direction = BaseEnums.Direction.Inbound,
            State = BaseEnums.SubSubscriptionState.Active
        }).ExecuteCommandAsync();

        var key = $"DUP_{unique}";
        var firstContent = $"{{\"data\":[{{\"{keyField}\":\"{key}\",\"detectTime\":\"2026-10-21T08:15:07.613\",\"{valueField}\":20}}]}}";
        var secondContent = $"{{\"data\":[{{\"{keyField}\":\"{key}\",\"detectTime\":\"2026-10-21T08:15:07.613\",\"{valueField}\":25}}]}}";
        var firstPacket = await SeedPendingPacket(partnerCode, packet.Code!, 1, firstContent, DateTime.Now);
        var secondPacket = await SeedPendingPacket(partnerCode, packet.Code!, 2, secondContent, DateTime.Now.AddSeconds(1));

        // Act
        using var scope = _host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataInboundService>().ProcessPendingPackets(CancellationToken.None);

        // Assert
        var first = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(firstPacket.ID);
        var second = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(secondPacket.ID);
        var savedValues = await _inboundDb.Ado.SqlQueryAsync<double>(
            $"SELECT CONVERT(float, {valueColumn}) FROM dbo.{table} WHERE {keyColumn} = @key", new { key });
        Assert.Equal(BaseEnums.InboundProcessState.Done, first.ProcessState);
        Assert.Equal(BaseEnums.InboundProcessState.Done, second.ProcessState);
        Assert.Equal(new[] { 25d }, savedValues);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Tệp dọn trùng bên nhận gói 104 (TmsWeather), 106 (TmsTrafficData): câu ghi lỗi cũ để lại nhiều dòng cùng khóa so khớp,
    ///              chạy tệp còn một dòng ghi sau cùng; dòng do nơi khác ghi (không phải Worker nhận) giữ nguyên; chạy lại không xóa thêm.
    /// Created date: 14/11/2026
    /// </summary>
    [Theory]
    [InlineData("sharedata-don-trung-TmsWeather.sql", "TmsWeather", "RefId", "TimeDetect", "Temperature")]
    [InlineData("sharedata-don-trung-TmsTrafficData.sql", "TmsTrafficData", "Location", "DetectTime", "Speed")]
    public async Task DuplicateCleanupScript_WhenOldWriteSqlLeftDuplicateRows_KeepsLatestInboundRow_Test(
        string scriptFile, string table, string keyColumn, string timeColumn, string valueColumn)
    {
        // Arrange
        var key = $"DUP_{Guid.NewGuid().ToString("N")[..8]}";
        var insertSql = $"INSERT INTO dbo.{table} (ID, {keyColumn}, {timeColumn}, {valueColumn}, CreateTime, CreateUId) "
            + "VALUES (CONVERT(nvarchar(64), NEWID()), @key, '2026-10-21T08:15:07.613', @value, @createTime, @createUId)";
        await _inboundDb.Ado.ExecuteCommandAsync(insertSql, new { key, value = 20, createTime = DateTime.Now.AddMinutes(-2), createUId = "SHAREDATA_INBOUND" });
        await _inboundDb.Ado.ExecuteCommandAsync(insertSql, new { key, value = 25, createTime = DateTime.Now.AddMinutes(-1), createUId = "SHAREDATA_INBOUND" });
        await _inboundDb.Ado.ExecuteCommandAsync(insertSql, new { key, value = 99, createTime = DateTime.Now.AddMinutes(-3), createUId = "OTHER_MODULE" });

        // Act
        await RunInboundSqlScriptAsync(scriptFile);
        await RunInboundSqlScriptAsync(scriptFile);

        // Assert
        var savedValues = await _inboundDb.Ado.SqlQueryAsync<double>(
            $"SELECT CONVERT(float, {valueColumn}) FROM dbo.{table} WHERE {keyColumn} = @key ORDER BY {valueColumn}", new { key });
        Assert.Equal(new[] { 25d, 99d }, savedValues);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Chạy tệp script trong thư mục sql lên CSDL nhận như trên SSMS: bỏ dòng USE, chạy từng khối giữa các GO.
    /// Created date: 14/11/2026
    /// </summary>
    private async Task RunInboundSqlScriptAsync(string fileName)
    {
        var script = SeedScriptReader.ReadSqlScript(fileName);
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline);
        foreach (var batch in batches)
        {
            var body = Regex.Replace(batch, @"^USE \[[^\]]+\];\s*$", string.Empty, RegexOptions.Multiline);
            if (string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            await _inboundDb.Ado.ExecuteCommandAsync(body);
        }
    }

    /// <summary>
    /// Description: B3 - Khi CSDL hủy transaction (SET XACT_ABORT ON), RollbackQuietly bắt êm lỗi hủy giao dịch
    ///              để không làm gián đoạn lượt quét; gói 1 đánh dấu Failed và gói 2 kế tiếp được xử lý thành Done.
    /// Created date: 20/10/2026
    /// Modified date: 27/10/2026 (câu ghi gộp một khối: lỗi lưu là lỗi SQL gốc "Lỗi chạy câu ghi #1: …")
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

        await ShareDataSqlSeed.SetWriteAsync(_db, packet.ID, abortSql);

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
        // Giữ lỗi SQL gốc của câu ghi, không bị lỗi phụ "SqlTransaction has completed" lúc quay lui che mất
        Assert.StartsWith("Lỗi chạy câu ghi #1:", updated1.ErrorMessage);
        Assert.DoesNotContain("SqlTransaction has completed", updated1.ErrorMessage);
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

    #region Nhiều bản clone chiều nhận

    /// <summary>
    /// Author: DatHQ
    /// Description: Hai bản clone chiều nhận chạy cùng lúc, có cả gói kẹt do bản trước chết giữa chừng: mỗi gói chỉ được xử lý một lần,
    ///              tất cả về Done và số thứ tự đã nhận của đăng ký tới gói lớn nhất.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenTwoReceiversRunTogether_ProcessesEachPacketOnce_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);
        const int pendingCount = 10;
        const string rawContent = "{\"data\":[{\"id\":\"A\",\"val\":\"1\"}]}";
        var packetIds = new List<string>();
        for (var serial = 1; serial <= pendingCount; serial++)
        {
            var pending = await SeedPendingPacket(partner.Code!, packet.Code!, serial, rawContent, DateTime.Now);
            packetIds.Add(pending.ID);
        }

        var stuckSerial = pendingCount + 1;
        var stuckPacket = new ShareDataInboundPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            SerialNbr = stuckSerial,
            RawContent = rawContent,
            ProcessState = BaseEnums.InboundProcessState.Processing,
            ProcessedAt = DateTime.Now.AddDays(-1),
            CreateTime = DateTime.Now.AddDays(-1)
        };
        await _inboundDb.Insertable(stuckPacket).ExecuteCommandAsync();
        packetIds.Add(stuckPacket.ID);

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var workerConfig = _host.Services.GetRequiredService<DataWorkerConfigService>();
        var firstReceiver = new DataInboundService(scopeFactory, NullLogger<DataInboundService>.Instance, workerConfig);
        var secondReceiver = new DataInboundService(scopeFactory, NullLogger<DataInboundService>.Instance, workerConfig);

        // Act
        await Task.WhenAll(
            firstReceiver.ProcessPendingPackets(CancellationToken.None),
            secondReceiver.ProcessPendingPackets(CancellationToken.None));

        // Assert
        var saved = await _inboundDb.Queryable<ShareDataInboundPacket>().Where(p => packetIds.Contains(p.ID)).ToListAsync();
        var logs = await _db.Queryable<ShareDataActivityLog>()
            .Where(l => l.SubscriptionId == sub.ID && l.TransferDirection == BaseEnums.TransferDirection.RCV)
            .ToListAsync();
        var savedSub = await _db.Queryable<ShareDataSubscription>().InSingleAsync(sub.ID);

        Assert.Equal(packetIds.Count, saved.Count);
        Assert.All(saved, p => Assert.Equal(BaseEnums.InboundProcessState.Done, p.ProcessState));
        Assert.Equal(packetIds.Count, logs.Count);
        Assert.Equal(stuckSerial, savedSub.SerialNbr);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Bản clone chiều nhận quên khai ConnectionStrings:InboundConnection: bỏ qua cả lượt, không báo lỗi làm dừng dịch vụ, gói vẫn chờ cho tới khi khai đúng.
    /// Created date: 05/11/2026
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenInboundConnectionMissing_SkipsRunAndKeepsPackets_Test()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, _) = await SeedInboundEnvironmentAsync(unique);
        var pending = await SeedPendingPacket(partner.Code!, packet.Code!, 1, "{\"data\":[{\"id\":\"A\",\"val\":\"1\"}]}", DateTime.Now);

        var mainConnection = _db.CurrentConnectionConfig.ConnectionString;
        var services = new ServiceCollection();
        services.AddScoped<ISqlSugarClient>(_ => new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = mainConnection,
            DbType = SqlSugar.DbType.SqlServer,
            IsAutoCloseConnection = true,
            ConfigId = "Default"
        }));
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var emptyConfig = new ConfigurationBuilder().Build();
        var workerConfig = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, emptyConfig);
        var receiver = new DataInboundService(scopeFactory, NullLogger<DataInboundService>.Instance, workerConfig);

        // Act
        var error = await Record.ExceptionAsync(() => receiver.ProcessPendingPackets(CancellationToken.None));

        // Assert
        var kept = await _inboundDb.Queryable<ShareDataInboundPacket>().InSingleAsync(pending.ID);
        var logs = await _db.Queryable<ShareDataActivityLog>().Where(l => l.SubscriptionId == sub.ID).ToListAsync();
        Assert.Null(error);
        Assert.Equal(BaseEnums.InboundProcessState.Pending, kept.ProcessState);
        Assert.Empty(logs);
    }

    #endregion

    #endregion
}
