using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.IncidentControl;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Dto.Process;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure.Services.Process;
using Shared.DTO.Constants.Module;
using Shared.Infrastructure.Services;
using Modules.TMS.Core.Helper;

namespace Modules.VMS.Controllers.LcsProcess.Commands
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Handler điều khiển hình ảnh thiết bị LCS.
    ///              LCS chỉ dùng loại điều khiển Image, kích thước cố định 128x128.
    ///              Tái dùng pipeline chung VmsProcessService.ChangeVmsContent:
    ///              gửi thiết bị -> ghi VmsCurrent (bảng chung, phân biệt theo EquipmentId)
    ///              -> ghi TmsSignalLog -> broadcast NATS/socket.
    /// Created date: 2026-07-10
    /// Update: ChuongLP - 19/10/2026 - Đồng nhất quyền giữ LCS theo chế độ phát VMS của sự cố (LCS chỉ hiện 1 ảnh):
    ///         - Ghi đè / thủ công không gắn sự cố: sự cố khác mất quyền trên LCS (DEC-01, DEC-03).
    ///         - Nối tiếp: giữ quyền sự cố khác; LCS hiện sự cố ưu tiên cao nhất (cùng mức -> sự cố mới). Sự cố mới ưu tiên
    ///           thấp hơn chỉ được ghi nhận chờ, khi sự cố đang hiện kết thúc thì ResetIncidentToDefault trả ảnh của nó lên.
    /// </summary>
    public class LcsImageCommandHandler : IWolverineHandler
    {
        // Kích thước màn hình LCS cố định
        private const string LcsDefaultSize = "128x128";

        private readonly VmsProcessService _vmsProcessService;
        private readonly UserManager _userManager;
        private readonly IMessageBus _messageBus;

        public LcsImageCommandHandler(VmsProcessService vmsProcessService, UserManager userManager, IMessageBus messageBus)
        {
            _vmsProcessService = vmsProcessService;
            _userManager = userManager;
            _messageBus = messageBus;
        }

        public async Task HandleAsync(LcsImageControlInput command)
        {
            // 1. Kiểm tra thông tin thiết lập
            // 1.1. Kiểm tra thiết bị có được chọn hay không
            if (command.EquipmentList.Count < 1)
            {
                throw Oops.Oh(BaseMsg.VmsProcess.Validation.NoDeviceSelected);
            }

            // 1.2. Kiểm tra thiết bị có tồn tại hay không (lấy dữ liệu qua MessBus thay cho BaseApiClient)
            await _vmsProcessService.ValidateEquipmentExist(command.EquipmentList.Select(l => l.ID).ToList());

            // 1.3. LCS dùng kích thước cố định 128x128 (không nhận size từ FE)
            command.Size = LcsDefaultSize;

            // 1.4. Kiểm tra content truyền vào (LCS chỉ dùng hình ảnh -> bắt buộc có Url)
            if (string.IsNullOrEmpty(command.Url))
            {
                throw Oops.Oh(BaseMsg.Vms.Validation.NoContent);
            }

            // 2. Xác định quyền giữ LCS và danh sách LCS thực sự gửi ảnh
            var equipmentIds = command.EquipmentList.Select(l => l.ID).ToList();
            var sendIds = equipmentIds;
            var isAppend = false;
            var isAppendModeEnabled = App.GetConfig<bool?>("VmsConfig:EnableIncidentAppendMode") ?? false;
            if (isAppendModeEnabled)
            {
                isAppend = !string.IsNullOrEmpty(command.IncidentId)
                    && string.Equals(command.PlayMode, VmsPlayModeConst.Append, StringComparison.OrdinalIgnoreCase);

                if (isAppend)
                    sendIds = await RegisterStandbyAsync(command, equipmentIds);
                else
                    await _vmsProcessService.ClearOldIncidentControlsForOverrideAsync(equipmentIds, command.IncidentId);

                // Mọi LCS đang bị sự cố ưu tiên cao hơn hiển thị -> chỉ ghi nhận chờ, không gửi
                if (sendIds.Count == 0) return;
            }

            // 3. Gửi ra thiết bị theo pipeline chung
            // Lấy thông tin thiết bị (Ip/Port/Vendor/Config) để gửi
            var allListEquipmentInfos = await _vmsProcessService.GetListEquipmentInfo();
            var listEquipmentInfoSendToDevice = allListEquipmentInfos
                .Where(q => sendIds.Contains(q.EquipmentId))
                .ToList();

            // Gửi cả nhóm thiết bị bằng MỘT gói tin điều khiển. ChangeVmsContentMulti lo trọn:
            // gửi thiết bị (qua dịch vụ ITS.VMS.Worker) -> ghi VmsCurrent -> ghi TmsSignalLog -> broadcast NATS/socket
            var prepareData = new PrepareVmsData();
            prepareData.ListEquipmentInfos = listEquipmentInfoSendToDevice.Adapt<List<EquipmentInfoInput>>();
            prepareData.ContentInfoInput = command.Adapt<ContentInfoInput>();
            prepareData.ContentInfoInput.IncidentId = command.IncidentId;
            prepareData.ContentInfoInput.Url = command.Url;
            // Pipeline ghi quyền giữ theo PlayMode: append giữ quyền sự cố khác, override như cũ
            prepareData.ContentInfoInput.PlayMode = isAppend ? VmsPlayModeConst.Append : VmsPlayModeConst.Override;
            // Luồng tự động (background): ExecuteBy được set = SYSTEM_AUTOMATION (không có HttpContext).
            // Luồng thủ công (HTTP request): ExecuteBy = null → đọc từ UserManager.Account (JWT).
            var isAutoProcess = !string.IsNullOrEmpty(command.ExecuteBy);
            prepareData.ProcessInfoInput.ProcessType = isAutoProcess ? "auto" : "manual";
            prepareData.ProcessInfoInput.ExecuteBy = isAutoProcess ? command.ExecuteBy : _userManager.Account;

            // Bổ sung xử lý gửi nhiều thiết bị cùng lúc, ghi log, broadcast NATS/socket
            await _vmsProcessService.ChangeVmsContentMulti(prepareData, sendIds, true);
        }

        /// <summary>
        /// Nối tiếp trên LCS: LCS đang bị sự cố khác ưu tiên CAO hơn giữ thì không gửi, chỉ ghi nhận quyền giữ chờ
        /// (State = append, kèm Url để trả ảnh lên sau). Trả về danh sách LCS được gửi ảnh ngay.
        /// Author: ChuongLP
        /// Date: 2026-10-19
        /// </summary>
        private async Task<List<string>> RegisterStandbyAsync(LcsImageControlInput command, List<string> equipmentIds)
        {
            var occupancy = await _messageBus.InvokeAsync<List<TmsIncidentControlDataOutput>>(new GetIncidentControlOccupancyInput
            {
                EquipmentIds = equipmentIds,
                ExcludeIncidentId = command.IncidentId
            });
            var standbyIds = IncidentControlOccupancyHelper.GetOverrideBlockedEquipmentIds(
                occupancy, command.Priority, Modules.TMS.Core.Constants.IncidentControlConst.DefaultPriority);

            foreach (var equipmentId in standbyIds)
            {
                await _messageBus.InvokeAsync(new UpdateTmsIncidentControlInput
                {
                    EquipmentId = equipmentId,
                    IncidentId = command.IncidentId,
                    State = VmsPlayModeConst.Append,
                    Priority = command.Priority,
                    Url = command.Url
                });
            }

            return equipmentIds.Where(id => !standbyIds.Contains(id)).ToList();
        }
    }
}