using Module.ShareData.Core.Dto.Inbound;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Extensions;
using Shared.DTO.Enums;
using SqlSugar;
using Wolverine;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Description: Kiểm thử điểm vào tiếp nhận gói tin (Inbound) qua Wolverine Bus
/// Created date: 17/10/2026
/// </summary>
[Collection("api")]
public class ShareDataInboundControllerTests(Host host)
{
    /// <summary>
    /// Description: Kiểm chứng tiếp nhận gói tin hợp lệ qua Wolverine Bus: lưu gói tin thô
    /// vào ShareDataInboundPacket (trạng thái Pending) và ghi ActivityLog đầy đủ dòng cha và dòng con Bước 1.
    /// Created date: 17/10/2026
    /// </summary>
    [Fact]
    public async Task ShareDataInboundController_WhenValidPacketReceived_LogsReceiveActivity_Test()
    {
        // Arrange
        var db = host.Services.GetRequiredService<ISqlSugarClient>();
        var inboundDb = db.AsTenant().GetConnectionScope(ShareDataWorkerExtensions.InboundConnectionKey);
        var bus = host.Services.GetRequiredService<IMessageBus>();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var partner = new ShareDataPartner
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"P_IN_{unique}",
            Name = $"Partner In {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(partner).ExecuteCommandAsync();

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"PKT_IN_{unique}",
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        var packetSql = new ShareDataPacketSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PacketCode = packet.Code,
            OrderNo = 1,
            Status = BaseEnums.StatusEnum.Enable,
            WriteSql = "SELECT 'INSERT' FROM OPENJSON(@records)"
        };
        await db.Insertable(packetSql).ExecuteCommandAsync();

        var sub = new ShareDataSubscription
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"SUB_IN_{unique}",
            PartnerId = partner.ID,
            DatatypeId = packet.ID,
            Direction = BaseEnums.Direction.Inbound,
            State = BaseEnums.SubSubscriptionState.Active,
            SerialNbr = 0,
            CreateTime = DateTime.Now
        };
        await db.Insertable(sub).ExecuteCommandAsync();

        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packet.ID,
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
        await db.Insertable(mapping).ExecuteCommandAsync();

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

        // Act: Tiếp nhận gói qua Wolverine Bus
        await bus.InvokeAsync(input);

        // Assert: Kiểm tra gói tin trong ShareDataInboundPacket
        var savedPacket = await inboundDb.Queryable<ShareDataInboundPacket>()
            .Where(p => p.PartnerCode == partner.Code && p.PacketCode == packet.Code)
            .FirstAsync();
        Assert.NotNull(savedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Pending, savedPacket.ProcessState);

        // Kiểm tra dòng nhật ký tiếp nhận (Dòng cha và Dòng con Bước 1)
        var parentLog = await db.Queryable<ShareDataActivityLog>()
            .Where(l => l.SubscriptionId == sub.ID && l.TransferDirection == BaseEnums.TransferDirection.RCV && l.ParentId == null)
            .FirstAsync();
        Assert.NotNull(parentLog);
        Assert.Null(parentLog.ParentId);
        Assert.Null(parentLog.StepNbr);
        Assert.Equal(BaseEnums.TransferDirection.RCV, parentLog.TransferDirection);
        Assert.Contains(packet.Code, parentLog.Description ?? string.Empty);

        var step1Log = await db.Queryable<ShareDataActivityLog>()
            .Where(l => l.SubscriptionId == sub.ID && l.TransferDirection == BaseEnums.TransferDirection.RCV && l.ParentId == parentLog.ID)
            .FirstAsync();
        Assert.NotNull(step1Log);
        Assert.Equal(1, step1Log.StepNbr);
        Assert.Equal(parentLog.ID, step1Log.ParentId);
    }
}
