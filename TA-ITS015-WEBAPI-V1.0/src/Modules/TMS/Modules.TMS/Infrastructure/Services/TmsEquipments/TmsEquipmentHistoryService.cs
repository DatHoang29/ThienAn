using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.FMS.Core.Entities;
using Modules.TMS.Core.Entities;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Infrastructure.Services.TmsEquipments
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xử lý lịch sử trạng thái thiết bị (FmsEquipmentHistory) và phát broadcast NATS
    /// khi trạng thái thiết bị (EquipmentState) thay đổi sau khi cập nhật thủ công từ BE.
    /// Tách ra từ EquipmentCommandHandler để tách riêng mối quan tâm lịch sử trạng thái / real-time
    /// khỏi luồng CRUD cơ bản.
    /// Created date: 12/05/2025
    /// </summary>
    public class TmsEquipmentHistoryService : IScoped
    {
        private BaseRepository<FmsEquipmentHistory> _fmsEquipHistoryRep;

        public TmsEquipmentHistoryService(BaseRepository<FmsEquipmentHistory> fmsEquipHistoryRep)
        {
            _fmsEquipHistoryRep = fmsEquipHistoryRep;
        }

        /// <summary>
        /// Xử lý lịch sử trạng thái thiết bị sau khi cập nhật thiết bị.
        /// - Nếu chưa có history: insert trạng thái đầu tiên.
        /// - Nếu trạng thái không đổi: chỉ update UpdateTime của history mới nhất.
        /// - Nếu trạng thái thay đổi: insert history mới.
        /// </summary>
        public async Task HandleEquipmentHistoryAfterUpdateAsync(string equipmentId, string? code, int? oldState, int? newState)
        {
            if (string.IsNullOrWhiteSpace(equipmentId))
                return;

            // Nếu không có trạng thái mới thì không xử lý history
            if (newState == null)
                return;

            var now = DateTime.Now;

            var latestHistory = await _fmsEquipHistoryRep
                .AsQueryable()
                .Where(x => x.EquipmentId == equipmentId)
                .OrderByDescending(x => x.CreateTime)
                .FirstAsync();

            // CASE 1: Chưa có history nào => insert trạng thái đầu tiên
            if (latestHistory == null)
            {
                var firstHistory = new FmsEquipmentHistory
                {
                    ID = Guid.NewGuid().ToString(),
                    EquipmentId = equipmentId,
                    Code = code,
                    StartTime = now,
                    UpdateTime = now,
                    CreateTime = now,
                    Type = "manual",
                    OldState = oldState?.ToString(),
                    NewState = newState.Value.ToString(),
                    Remark = $"Initial state from equipment update | {equipmentId} | NewState={newState}"
                };

                await _fmsEquipHistoryRep.InsertAsync(firstHistory);
                return;
            }

            // CASE 2: Trạng thái không đổi => chỉ update lại thời gian
            if (oldState == newState)
            {
                latestHistory.UpdateTime = now;

                await _fmsEquipHistoryRep.UpdateAsync(latestHistory);
                return;
            }

            // CASE 3: Trạng thái thay đổi => insert history mới
            var changedHistory = new FmsEquipmentHistory
            {
                ID = Guid.NewGuid().ToString(),
                EquipmentId = equipmentId,
                Code = code,
                StartTime = now,
                UpdateTime = now,
                CreateTime = now,
                OldState = oldState?.ToString(),
                NewState = newState.Value.ToString(),
                Remark = $"State changed from equipment update | {equipmentId} | {oldState} -> {newState}"
            };

            await _fmsEquipHistoryRep.InsertAsync(changedHistory);
        }

        /// <summary>
        /// Gửi trạng thái thiết bị thủ công từ BE lên NATS theo chuẩn WResult.
        /// Chỉ gửi khi EquipmentState thật sự thay đổi.
        /// </summary>
        public Task PublishMessageNatsAsync(TmsEquipment equipment, int? oldState, int? newState)
        {
            if (equipment == null)
                return Task.CompletedTask;

            // Không có trạng thái mới thì không gửi message trạng thái
            if (newState == null)
                return Task.CompletedTask;

            // Trạng thái không đổi thì không gửi NATS để tránh spam
            if (oldState == newState)
                return Task.CompletedTask;

            var payload = new
            {
                PacketType = ItsDataTransporterChannel.GetStatus,
                DeviceType = !string.IsNullOrWhiteSpace(equipment.EquipmentTypeId),
                Type = "manual",
                Time = DateTime.Now,
                Result = new[]
                {
                    new
                    {
                        Info = new
                        {
                            ReferenceId = equipment.ID,
                            Ip = equipment.Ip,
                            Port = ToNullableInt(equipment.Port) ?? 0,
                            Code = equipment.Code
                        },
                        Data = new
                        {
                            RawStatus = newState.Value.ToString(),
                            Status = newState.Value
                        }
                    }
                }
            };

            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.GetStatus, payload);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Hàm hỗ trợ chuyển đổi object sang int? để xử lý trạng thái thiết bị
        /// </summary>
        public static int? ToNullableInt(object? value)
        {
            if (value == null)
                return null;

            if (value is int intValue)
                return intValue;

            if (int.TryParse(value.ToString(), out var result))
                return result;

            return null;
        }
    }
}
