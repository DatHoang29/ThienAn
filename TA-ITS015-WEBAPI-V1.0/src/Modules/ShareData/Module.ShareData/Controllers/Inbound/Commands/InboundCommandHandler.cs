using System.Text;
using Mapster;
using Module.ShareData.Core.Dto.Inbound;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataActivityService;
using Shared.DTO.Enums;
using Wolverine;

namespace Module.ShareData.Controllers.Inbound.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Tiếp nhận gói tin từ đối tác — lưu thô vào ShareDataInboundPacket
    ///              với trạng thái Pending và ghi nhật ký tiếp nhận. Không mapping, không ghi CSDL đích.
    /// Created date: 15/09/2026
    /// </summary>
    public class InboundCommandHandler : IWolverineHandler
    {
        /// <summary>Loại PDU của gói dữ liệu, trùng giá trị bên gửi ghi nhật ký.</summary>
        private const string DataPacketPdu = "DATA_PACKET";

        /// <summary>Loại PDU của gói tập tin, trùng giá trị bên gửi ghi nhật ký.</summary>
        private const string FilePacketPdu = "FILE_PACKET";

        private readonly BaseRepository<ShareDataPartner> _partnerRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseRepository<ShareDataInboundPacket> _inboundPacketRep;
        private readonly BaseRepository<ShareDataPacket> _packetRep;
        private readonly ShareDataActivityLogger _activityLogger;

        public InboundCommandHandler(
            BaseRepository<ShareDataPartner> partnerRep,
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseRepository<ShareDataInboundPacket> inboundPacketRep,
            BaseRepository<ShareDataPacket> packetRep,
            ShareDataActivityLogger activityLogger)
        {
            _partnerRep = partnerRep;
            _subscriptionRep = subscriptionRep;
            _inboundPacketRep = inboundPacketRep;
            _packetRep = packetRep;
            _activityLogger = activityLogger;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 gói tin nhận về; định dạng và loại PDU lấy theo đăng ký nhận.
        /// Created date: 15/09/2026
        /// Modified date: 16/11/2026
        /// </summary>
        public async Task HandleAsync(ShareDataAddInboundInput command)
        {
            command.PartnerCode = command.PartnerCode?.Trim();
            command.PacketCode = command.PacketCode?.Trim();

            var partner = await _partnerRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .Where(u => u.Code == command.PartnerCode)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            if (partner.Status != BaseEnums.StatusEnum.Enable)
                throw Oops.Oh(BaseMsg.ShareData.Exception.PartnerDisabled);

            command.CreateUId = null;
            command.ByteSize = string.IsNullOrEmpty(command.RawContent)
                ? 0
                : Encoding.UTF8.GetByteCount(command.RawContent);

            // PacketCode của gói nhận về là PacketCode (lấy từ header), trong khi Subscription/Mapping
            // lại trỏ gói tin bằng ShareDataPacket.ID — nên phải tra gói tin ra trước rồi mới so được.
            var packetCode = command.PacketCode;
            var packet = await _packetRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .Where(u => u.Code == packetCode)
                .FirstAsync();

            var packetId = packet?.ID;

            // Đăng ký chiều nhận tương ứng — lấy bản mới nhất nếu có nhiều.
            // Nhận cả hai cách khai DatatypeId (ID của gói tin hoặc Code) cho khỏi kén dữ liệu.
            var subscription = packet == null
                ? null
                : await _subscriptionRep.AsQueryable()
                    .Where(u => u.IsDelete == null)
                    .Where(u => u.PartnerId == partner.ID)
                    .Where(u => u.DatatypeId == packetId || u.DatatypeId == packetCode)
                    .Where(u => u.Direction == BaseEnums.Direction.Inbound)
                    .OrderByDescending(u => u.CreateTime)
                    .FirstAsync();

            string? noSubscriptionReason = null;
            if (packet == null)
            {
                noSubscriptionReason = $"Không tìm thấy cấu hình gói tin có mã [{packetCode}].";
            }
            else if (subscription == null)
            {
                noSubscriptionReason = "Chưa có đăng ký nhận cho loại gói tin này.";
            }
            else if (!IsUsableSubscription(subscription.State))
            {
                noSubscriptionReason = $"Đăng ký nhận đang ở trạng thái {subscription.State} — không còn hiệu lực.";
            }

            if (noSubscriptionReason == null)
            {
                command.ProcessState = BaseEnums.InboundProcessState.Pending;
            }
            else
            {
                command.ProcessState = BaseEnums.InboundProcessState.NoSubscription;
                command.ErrorMessage = noSubscriptionReason;
            }

            if (command.SerialNbr.HasValue && command.SerialNbr.Value > 0)
            {
                var isDuplicate = await _inboundPacketRep.AsQueryable()
                    .Where(u => u.PartnerCode == command.PartnerCode && u.PacketCode == command.PacketCode && u.SerialNbr == command.SerialNbr)
                    .AnyAsync();
                if (isDuplicate)
                {
                    throw Oops.Oh(BaseMsg.ShareData.Exception.InboundPacketDuplicated, command.PartnerCode, command.PacketCode, command.SerialNbr);
                }
            }

            var entity = command.Adapt<ShareDataInboundPacket>();
            // Bên gửi chỉ gửi header PartnerCode, PacketCode, SerialNbr: định dạng và loại PDU lấy theo đăng ký nhận, giống cách bên gửi ghi nhật ký
            entity.Format ??= subscription?.Format ?? BaseEnums.PublishFormat.Data;
            entity.PduType ??= entity.Format == BaseEnums.PublishFormat.File ? FilePacketPdu : DataPacketPdu;
            var parentLogId = Guid.NewGuid().ToString("N");
            entity.ReceiveLogId = parentLogId;

            try
            {
                await _inboundPacketRep.InsertAsync(entity);
            }
            catch (Exception ex)
            {
                await LogReceiveAsync(partner, subscription, entity, false, ex.Message);
                throw;
            }

            // Không có đăng ký: gói đã lưu nhưng sẽ không được xử lý → ghi nhật ký dạng lỗi.
            await LogReceiveAsync(partner, subscription, entity, noSubscriptionReason == null, noSubscriptionReason);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đăng ký còn hiệu lực để nhận gói (Active, hoặc tạm dừng / chờ duyệt — gói giữ Pending chờ).
        /// Created date: 15/09/2026
        /// </summary>
        private static bool IsUsableSubscription(BaseEnums.SubSubscriptionState? state)
            => state is BaseEnums.SubSubscriptionState.Active
                or BaseEnums.SubSubscriptionState.Paused
                or BaseEnums.SubSubscriptionState.Pending;

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi nhật ký tiếp nhận gói tin (SV-8b: sinh Dòng cha và Dòng con Bước 1)
        /// Created date: 15/09/2026
        /// </summary>
        private async Task LogReceiveAsync(ShareDataPartner partner, ShareDataSubscription? subscription,
            ShareDataInboundPacket packet, bool success, string? errorMessage)
        {
            var partnerName = partner.Name ?? partner.Code;
            var parentLogId = packet.ReceiveLogId ?? Guid.NewGuid().ToString("N");
            var parentDesc = success
                ? $"Tiếp nhận gói tin loại {packet.PacketCode} từ đối tác \"{partnerName}\" — chờ xử lý"
                : packet.ProcessState == BaseEnums.InboundProcessState.NoSubscription
                    ? $"Tiếp nhận gói tin loại {packet.PacketCode} từ đối tác \"{partnerName}\" — không xử lý: {errorMessage}"
                    : $"Lỗi tiếp nhận gói tin loại {packet.PacketCode} từ đối tác \"{partnerName}\": {errorMessage}";

            var isStep1Success = success || packet.ProcessState == BaseEnums.InboundProcessState.NoSubscription;
            var step1Desc = isStep1Success
                ? $"Bước 1/2: Tiếp nhận gói tin và lưu tạm thành công ({packet.ByteSize ?? 0} bytes)"
                : $"Bước 1/2: Lỗi tiếp nhận gói tin: {errorMessage}";

            // Dòng Cha (Parent): ID = parentLogId, ParentId = null, StepNbr = null
            await _activityLogger.LogTransferAsync(
                BaseEnums.TransferDirection.RCV,
                partner.ID,
                partnerName,
                subscription?.ID,
                subscription?.SessionId,
                packet.PacketCode,
                packet.PduType,
                serialNbr: packet.SerialNbr,
                packetNbr: packet.SerialNbr,
                format: packet.Format,
                byteSize: packet.ByteSize,
                recordCount: 0,
                success: success,
                errorMessage: errorMessage,
                description: parentDesc,
                parentId: null,
                stepNbr: null,
                id: parentLogId);

            // Dòng Con Bước 1 (Step 1): ID = tự sinh, ParentId = parentLogId, StepNbr = 1
            await _activityLogger.LogTransferAsync(
                BaseEnums.TransferDirection.RCV,
                partner.ID,
                partnerName,
                subscription?.ID,
                subscription?.SessionId,
                packet.PacketCode,
                packet.PduType,
                serialNbr: packet.SerialNbr,
                packetNbr: packet.SerialNbr,
                format: packet.Format,
                byteSize: packet.ByteSize,
                recordCount: 0,
                success: isStep1Success,
                errorMessage: isStep1Success ? null : errorMessage,
                description: step1Desc,
                parentId: parentLogId,
                stepNbr: 1);
        }
    }
}
