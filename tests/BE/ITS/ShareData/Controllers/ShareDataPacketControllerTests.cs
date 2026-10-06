using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Dto.PacketField;
using Module.ShareData.Core.Entities;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Tests.ShareData.Controllers
{
    /// <summary>
    /// Description: Bộ kiểm thử tích hợp cho PacketController và luật IsInUse (BE-16).
    ///              Kiểm chứng cờ IsInUse trong phân trang và cổng chặn xoá gói tin / trường gói tin.
    /// Created date: 06/10/2026
    /// </summary>
    [Collection("api")]
    public class ShareDataPacketControllerTests(Host host)
    {
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();

        [Fact]
        public async Task Page_WhenPacketHasActiveMapping_ReturnsIsInUseTrue_Test()
        {
            // Arrange
            var prefix = $"T16_{Guid.NewGuid():N}"[..12];
            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{prefix}_01",
                Name = "Gói có mapping",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var mapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                DatatypeId = packet.ID,
                Name = "Mapping test",
                CreateTime = DateTime.Now
            };

            await _db.Insertable(packet).ExecuteCommandAsync();
            await _db.Insertable(mapping).ExecuteCommandAsync();

            var input = new ShareDataPagePacketInput
            {
                Code = prefix,
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _bus.InvokeAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(input);

            // Assert
            Assert.NotNull(result);
            var item = result.Records.FirstOrDefault(u => u.ID == packet.ID);
            Assert.NotNull(item);
            Assert.True(item.IsInUse);
        }

        [Fact]
        public async Task Page_WhenPacketHasAliveSubscriptionOnly_ReturnsIsInUseTrue_Test()
        {
            // Arrange
            var prefix = $"T16_{Guid.NewGuid():N}"[..12];
            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{prefix}_02",
                Name = "Gói có subscription sống",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                DatatypeId = packet.ID,
                State = BaseEnums.SubSubscriptionState.Active,
                CreateTime = DateTime.Now
            };

            await _db.Insertable(packet).ExecuteCommandAsync();
            await _db.Insertable(sub).ExecuteCommandAsync();

            var input = new ShareDataPagePacketInput
            {
                Code = prefix,
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _bus.InvokeAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(input);

            // Assert
            Assert.NotNull(result);
            var item = result.Records.FirstOrDefault(u => u.ID == packet.ID);
            Assert.NotNull(item);
            Assert.True(item.IsInUse);
        }

        [Fact]
        public async Task Page_WhenSubscriptionIsNotAliveAndNoMapping_ReturnsIsInUseFalse_Test()
        {
            // Arrange
            var prefix = $"T16_{Guid.NewGuid():N}"[..12];
            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{prefix}_03",
                Name = "Gói có subscription chết",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                DatatypeId = packet.ID,
                State = BaseEnums.SubSubscriptionState.Cancelled,
                CreateTime = DateTime.Now
            };

            await _db.Insertable(packet).ExecuteCommandAsync();
            await _db.Insertable(sub).ExecuteCommandAsync();

            var input = new ShareDataPagePacketInput
            {
                Code = prefix,
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _bus.InvokeAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(input);

            // Assert
            Assert.NotNull(result);
            var item = result.Records.FirstOrDefault(u => u.ID == packet.ID);
            Assert.NotNull(item);
            Assert.False(item.IsInUse);
        }

        [Fact]
        public async Task Page_WhenInUseFlagTrue_DeleteIsBlockedConsistently_Test()
        {
            // Arrange
            var prefix = $"T16_{Guid.NewGuid():N}"[..12];
            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{prefix}_04",
                Name = "Gói để test chặn xóa",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var mapping = new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                DatatypeId = packet.ID,
                Name = "Mapping test 4",
                CreateTime = DateTime.Now
            };

            await _db.Insertable(packet).ExecuteCommandAsync();
            await _db.Insertable(mapping).ExecuteCommandAsync();

            var input = new ShareDataPagePacketInput
            {
                Code = prefix,
                Page = 1,
                PageSize = 10
            };

            // Act 1: Check flag
            var pageResult = await _bus.InvokeAsync<SqlSugarPagedList<ShareDataPagePacketOutput>>(input);
            var item = pageResult.Records.FirstOrDefault(u => u.ID == packet.ID);
            Assert.NotNull(item);
            Assert.True(item.IsInUse);

            // Act 2 & Assert: Check delete blocked
            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                _bus.InvokeAsync(new ShareDataDeletePacketInput { ID = packet.ID }));
            Assert.Contains("PacketInUse", ex.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeletePacketField_WhenPacketInUse_IsBlockedBySharedRule_Test()
        {
            // Arrange
            var prefix = $"T16_{Guid.NewGuid():N}"[..12];
            var packet = new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = $"{prefix}_05",
                Name = "Gói có trường để test",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var sub = new ShareDataSubscription
            {
                ID = Guid.NewGuid().ToString("N"),
                DatatypeId = packet.ID,
                State = BaseEnums.SubSubscriptionState.Active,
                CreateTime = DateTime.Now
            };
            var field = new ShareDataPacketField
            {
                ID = Guid.NewGuid().ToString("N"),
                DatatypeId = packet.ID,
                Name = "Field01",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };

            await _db.Insertable(packet).ExecuteCommandAsync();
            await _db.Insertable(sub).ExecuteCommandAsync();
            await _db.Insertable(field).ExecuteCommandAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                _bus.InvokeAsync(new ShareDataDeletePacketFieldInput { ID = field.ID }));
            Assert.Contains("PacketFieldInUse", ex.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
