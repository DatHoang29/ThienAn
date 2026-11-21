using Modules.TMS.Core.Entities;
using Modules.VMS.Controllers.EventDefault.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Imaging;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Module;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.VMS.Controllers.EventDefault.Commands
{
    /// <summary>
    /// Author: 
    /// Description: Xử lý thêm/sửa/xóa nội dung mặc định VMS theo sự kiện sự cố.
    ///              Tái sử dụng ImageService để lưu ảnh base64, và gom cấu hình vào DefaultConfig (JSON).
    ///              Loại thiết bị (VMS/LCS) quyết định kích thước cố định: VMS = 384x64, LCS = 128x128 (chỉ nhận ảnh).
    /// Created date: 15/07/2026
    /// </summary>
    public class EventDefaultCommandHandler : IWolverineHandler
    {
        // Mã loại thiết bị hợp lệ (TmsEquipmentType.Code) cho tính năng này
        private const string EquipmentTypeCodeVms = "VMS";
        private const string EquipmentTypeCodeLcs = "LCS";

        // Kích thước cố định theo loại thiết bị
        private const string VmsSize = "384x64";
        private const string LcsSize = "128x128";

        private readonly BaseRepository<VmsEventDefault> _baseRepository;
        private readonly BaseRepository<TmsEquipmentType> _equipmentTypeRepository;
        private readonly ImageService _imageService;
        private BaseCacheService _cache;
        public EventDefaultCommandHandler(BaseRepository<VmsEventDefault> baseRepository, BaseRepository<TmsEquipmentType> equipmentTypeRepository, ImageService imageService, BaseCacheService cache)
        {
            _baseRepository = baseRepository;
            _equipmentTypeRepository = equipmentTypeRepository;
            _imageService = imageService;
            _cache = cache;
        }

        /// <summary>
        /// Thêm mới nội dung mặc định theo sự kiện sự cố.
        /// Author: ChuongLP
        /// Date: 2026-07-17
        /// </summary>
        public async Task HandleAsync(AddVmsEventDefaultInput command)
        {
            command.Code = command.Code?.Trim();

            bool isLcs = await ApplyEquipmentTypeRuleAsync(command);
            if (isLcs) command.RowData = null; // LCS chỉ nhận ảnh, không lưu nội dung text

            // Mỗi sự kiện chỉ được 1 nội dung mặc định cho mỗi loại thiết bị (VMS/LCS)
            if (await _baseRepository.IsAnyAsync(x => x.IsDelete == null && x.EventTypeId == command.EventTypeId && x.EquipmentTypeId == command.EquipmentTypeId))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.VmsEventDefault.Entity.EventTypeId);

            command.Url = await ResolveImageUrlAsync(command.Url);   // Nếu Url là base64 (ảnh preview/upload) -> lưu file, lấy đường dẫn tương đối

            var entity = command.Adapt<VmsEventDefault>();

            // Gom toàn bộ thông tin vào DefaultConfig (JSON) trước khi lưu (cùng 1 thao tác)
            entity.DefaultConfig = BuildDefaultConfig(entity, command.UseForEvent, command.FontSize);

            await _baseRepository.InsertWithDiffLogAsync(entity);

            _cache.RemoveByPrefixKey(CacheConst.Vms.VmseventDefault); // ChuongLP : Xóa cache để cập nhật dữ liệu mới nhất
        }

        /// <summary>
        /// Cập nhật nội dung mặc định theo sự kiện sự cố.
        /// Author: ChuongLP
        /// Date: 2026-07-17
        /// </summary>
        public async Task HandleAsync(UpdateVmsEventDefaultInput command)
        {
            var entity = await _baseRepository.GetFirstAsync(x => x.ID == command.ID);
            if (entity == null) throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            bool isLcs = await ApplyEquipmentTypeRuleAsync(command);
            if (isLcs) command.RowData = null; // LCS chỉ nhận ảnh, không lưu nội dung text

            // Mỗi sự kiện chỉ được 1 nội dung mặc định cho mỗi loại thiết bị (VMS/LCS) - bỏ qua chính bản ghi đang sửa
            if (await _baseRepository.IsAnyAsync(x => x.IsDelete == null 
                && x.EventTypeId == command.EventTypeId 
                && x.EquipmentTypeId == command.EquipmentTypeId 
                && x.ID != command.ID))
            throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.VmsEventDefault.Entity.EventTypeId);

            // Nếu Url là base64 -> lưu file mới; nếu không phải base64 thì giữ nguyên giá trị gửi lên
            command.Url = await ResolveImageUrlAsync(command.Url);

            entity.EventTypeId = command.EventTypeId;
            entity.EquipmentTypeId = command.EquipmentTypeId;
            entity.Size = command.Size;
            entity.Name = command.Name;
            entity.RowData = command.RowData;
            entity.Url = command.Url;
            entity.Font = command.Font;
            entity.Color = command.Color;
            entity.BackColor = command.BackColor;
            entity.Code = command.Code?.Trim();
            // Cấu hình phát khi tự động xử lý sự cố (Append/Override + TTL) - thiếu 2 dòng này thì sửa không lưu được
            entity.DefaultPlayMode = string.Equals(command.DefaultPlayMode, VmsPlayModeConst.Append, StringComparison.OrdinalIgnoreCase)
                ? VmsPlayModeConst.Append
                : VmsPlayModeConst.Override;
            entity.TimeToLive = command.TimeToLive;

            // Sinh lại DefaultConfig sau khi cập nhật
            entity.DefaultConfig = BuildDefaultConfig(entity, command.UseForEvent, command.FontSize);

            await _baseRepository.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vms.VmseventDefault); // ChuongLP : Xóa cache để cập nhật dữ liệu mới nhất
        }

        /// <summary>
        /// Tra cứu TmsEquipmentType theo EquipmentTypeId, xác thực chỉ nhận VMS hoặc LCS,
        /// và ép command.Size theo kích thước cố định của loại thiết bị (bỏ qua Size do FE gửi lên).
        /// Trả về true nếu là LCS.
        /// Author: ChuongLP
        /// Date: 2026-07-17
        /// </summary>
        private async Task<bool> ApplyEquipmentTypeRuleAsync(AddVmsEventDefaultInput command)
        {
            var equipmentType = await _equipmentTypeRepository.GetFirstAsync(x => x.ID == command.EquipmentTypeId);
            if (equipmentType == null) throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            bool isLcs = string.Equals(equipmentType.Code, EquipmentTypeCodeLcs, StringComparison.OrdinalIgnoreCase);
            bool isVms = string.Equals(equipmentType.Code, EquipmentTypeCodeVms, StringComparison.OrdinalIgnoreCase);
            if (!isLcs && !isVms) throw Oops.Oh(BaseMsg.VmsEventDefault.Validation.EquipmentTypeInvalid);

            command.Size = isLcs ? LcsSize : VmsSize;
            return isLcs;
        }

        /// <summary>
        /// Xóa nội dung mặc định theo sự kiện sự cố.
        /// Author: ChuongLP
        /// Date: 2026-07-17
        /// </summary>
        public async Task HandleAsync(DeleteVmsEventDefaultInput command)
        {
            var entity = await _baseRepository.GetFirstAsync(x => x.ID == command.ID);
            if (entity == null) throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _baseRepository.SoftDeleteAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vms.VmseventDefault); // ChuongLP : Xóa cache để cập nhật dữ liệu mới nhất
        }

        /// <summary>
        /// Nếu url là chuỗi base64 -> lưu thành file và trả về đường dẫn tương đối; nếu không -> giữ nguyên.
        /// Author: ChuongLP
        /// Date: 2026-07-17
        /// </summary>
        private async Task<string?> ResolveImageUrlAsync(string? url)
        {
            if (!_imageService.IsBase64String(url)) return url;

            var savedPath = await _imageService.SaveImageAsync(url, Guid.NewGuid().ToString("N"), "upload\\vms\\template\\preview");
            if (string.IsNullOrEmpty(savedPath))
                throw Oops.Oh(BaseMsg.VmsEventDefault.Entity.Url);

            return savedPath;
        }

        /// <summary>
        /// Gom toàn bộ thông tin bản ghi vào chuỗi JSON DefaultConfig.
        /// ImageUrl là đường dẫn đã lưu (không phải base64).
        /// Author: ChuongLP
        /// Date: 2026-07-17
        /// </summary>
        private static string BuildDefaultConfig(VmsEventDefault e, bool useForEvent, string? fontSize)
        {
            string? row1 = null, row2 = null;
            if (!string.IsNullOrWhiteSpace(e.RowData))
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(e.RowData);
                    if (obj != null)
                    {
                        obj.TryGetValue("Row1", out row1);
                        obj.TryGetValue("Row2", out row2);
                    }
                }
                catch
                {
                    // RowData không phải JSON hợp lệ -> để row1/row2 null
                }
            }

            var config = new
            {
                Row1 = row1 ?? string.Empty,
                Row2 = row2 ?? string.Empty,
                ImageUrl = e.Url ?? string.Empty,
                Font = e.Font ?? string.Empty,
                FontSize = fontSize ?? string.Empty,
                Color = e.Color ?? string.Empty,
                BackColor = e.BackColor ?? string.Empty,
                Size = e.Size ?? string.Empty,
                UseForEvent = useForEvent
            };

            return JsonConvert.SerializeObject(config);
        }
    }
}
