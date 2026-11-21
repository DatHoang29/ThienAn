using Mapster;
using Module.ShareData.Core.Dto.CodeSet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataActivityService;
using Module.ShareData.Infrastructure.Services.ShareDataCodeSetService;
using Module.ShareData.Infrastructure.Services.ShareDataMappingReader;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.ShareData.Controllers.CodeSet.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa Bộ mã quy đổi (ShareDataCodeSet).
    /// Created date: 2026-08-21
    /// </summary>
    public class CodeSetCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataCodeSet> _codeSetRep;
        private readonly BaseRepository<ShareDataMapping> _mappingRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseCacheService _cache;
        private readonly ShareDataActivityLogger _activityLogger;

        public CodeSetCommandHandler(
            BaseRepository<ShareDataCodeSet> codeSetRep,
            BaseRepository<ShareDataMapping> mappingRep,
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseCacheService cache,
            ShareDataActivityLogger activityLogger)
        {
            _codeSetRep = codeSetRep;
            _mappingRep = mappingRep;
            _subscriptionRep = subscriptionRep;
            _cache = cache;
            _activityLogger = activityLogger;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 bộ mã
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataCodeSetOutput> HandleAsync(ShareDataAddCodeSetInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.DefaultSourceValue = command.DefaultSourceValue?.Trim();
            command.DefaultPartnerValue = command.DefaultPartnerValue?.Trim();
            ValidateValues(command.Values);

            if (await _codeSetRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            var entity = command.Adapt<ShareDataCodeSet>();
            entity.ValuesJson = ShareDataCodeSetValueReader.Serialize(command.Values, command.DefaultSourceValue, command.DefaultPartnerValue);
            entity.CreateUId = null;

            await _codeSetRep.InsertWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataCodeSet);

            var output = entity.Adapt<ShareDataCodeSetOutput>();
            output.Values = command.Values ?? [];
            output.ValueCount = output.Values.Count;
            output.DefaultSourceValue = command.DefaultSourceValue;
            output.DefaultPartnerValue = command.DefaultPartnerValue;
            output.ValuesJson = null;

            await _activityLogger.LogCreateAsync(entity);

            return output;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 bộ mã.
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataCodeSetOutput> HandleAsync(ShareDataUpdateCodeSetInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.DefaultSourceValue = command.DefaultSourceValue?.Trim();
            command.DefaultPartnerValue = command.DefaultPartnerValue?.Trim();
            ValidateValues(command.Values);

            var entity = await _codeSetRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await _codeSetRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            // Đổi mã bộ trong khi vẫn còn nơi tham chiếu sẽ làm phễu lọc trỏ vào mã không tồn tại.
            if (!string.Equals(entity.Code, command.Code, StringComparison.Ordinal)
                && await IsCodeInUseAsync(entity.Code))
                throw Oops.Oh(BaseMsg.ShareData.Exception.CodeSetInUse);

            // Bảng quy đổi / giá trị mặc định đổi thì worker áp ngay ở lượt kế tiếp:
            // khoá giống hồ sơ ánh xạ — hồ sơ đang bật dùng bộ mã mà còn đăng ký bật thì phải tạm dừng trước.
            if (IsValuesChanged(entity.ValuesJson, command))
                await EnsureNotUsedByActiveMappingAsync(entity.Code);

            var before = entity.Adapt<ShareDataCodeSet>();

            entity.Code = command.Code;
            entity.Name = command.Name;
            entity.ValuesJson = ShareDataCodeSetValueReader.Serialize(command.Values, command.DefaultSourceValue, command.DefaultPartnerValue);
            entity.Remark = command.Remark;

            await _codeSetRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataCodeSet);

            var output = entity.Adapt<ShareDataCodeSetOutput>();
            output.Values = command.Values ?? [];
            output.ValueCount = output.Values.Count;
            output.DefaultSourceValue = command.DefaultSourceValue;
            output.DefaultPartnerValue = command.DefaultPartnerValue;
            output.ValuesJson = null;

            await _activityLogger.LogUpdateAsync(before, entity);

            return output;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 bộ mã (xóa mềm).
        /// Created date: 2026-08-21
        /// </summary>
        public async Task HandleAsync(ShareDataDeleteCodeSetInput command)
        {
            var entity = await _codeSetRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await IsCodeInUseAsync(entity.Code))
                throw Oops.Oh(BaseMsg.ShareData.Exception.CodeSetInUse);

            var before = entity.Adapt<ShareDataCodeSet>();

            await _codeSetRep.SoftDeleteAsync<ShareDataCodeSet>(entity);
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataCodeSet);

            await _activityLogger.LogDeleteAsync(before);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều bộ mã (xóa mềm).
        /// Created date: 2026-08-21
        /// </summary>
        public async Task HandleAsync(List<ShareDataDeleteCodeSetInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _codeSetRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            foreach (var item in entities)
                if (await IsCodeInUseAsync(item.Code))
                    throw Oops.Oh(BaseMsg.ShareData.Exception.CodeSetInUse);

            await _codeSetRep.SoftDeleteAsync<ShareDataCodeSet>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.ShareData.ShareDataCodeSet);

            foreach (var item in entities)
                await _activityLogger.LogDeleteAsync(item);
        }

        #region Private function
      
        /// <summary>
        /// Author: HieuNV
        /// Description: Kiểm tra bảng quy đổi
        ///              ESH-2203 (trùng giá trị nguồn).
        /// Created date: 2026-08-21
        /// </summary>
        private static void ValidateValues(List<ShareDataCodeSetValueDto>? values)
        {
            if (values == null || values.Count == 0)
                throw Oops.Oh(BaseMsg.ShareData.Validation.CodeSetValuesRequired);

            var duplicatedSource = values
                .Where(u => !string.IsNullOrWhiteSpace(u.SourceValue))
                .GroupBy(u => u.SourceValue!.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicatedSource != null)
                throw Oops.Oh(BaseMsg.ShareData.Exception.CodeSetSourceValueDuplicated, duplicatedSource.Key);

            var duplicatedPartner = values
                .Where(u => !string.IsNullOrWhiteSpace(u.PartnerValue))
                .GroupBy(u => u.PartnerValue!.Trim(), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicatedPartner != null)
                throw Oops.Oh(BaseMsg.ShareData.Exception.CodeSetPartnerValueDuplicated, duplicatedPartner.Key);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Mã bộ mã có đang được [EshField(CodeSet)] 
        /// Created date: 2026-08-21
        /// </summary>
        private async Task<bool> IsCodeInUseAsync(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            // KHÔNG dò chuỗi thô: FE lưu bộ khung dạng pretty-print nên trong CSDL là
            // "codeSet": "X" (có dấu cách), dò "codeSet":"X" sẽ không bao giờ khớp.
            // Lọc thô bằng LIKE cho nhẹ rồi đọc đúng ký hiệu $extend.codeSet bằng ShapeReader.
            var shapes = await _mappingRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.TargetShapeJson != null && u.TargetShapeJson.Contains("codeSet"))
                .Select(u => u.TargetShapeJson)
                .ToListAsync();

            return shapes.Any(json => ShapeReader.ReadShape(json).CodeSets
                .Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Bảng quy đổi hoặc giá trị mặc định có thay đổi không.
        ///              Chỉ so giá trị nguồn / đối tác / mặc định — tên hiển thị không ảnh hưởng dữ liệu gửi nhận.
        /// Created date: 2026-10-30
        /// </summary>
        private static bool IsValuesChanged(string? oldValuesJson, ShareDataUpdateCodeSetInput command)
        {
            var (oldValues, oldDefaultSource, oldDefaultPartner) = ShareDataCodeSetValueReader.Parse(oldValuesJson);
            var newValues = command.Values ?? [];

            if (!string.Equals(oldDefaultSource ?? string.Empty, command.DefaultSourceValue ?? string.Empty, StringComparison.Ordinal)
                || !string.Equals(oldDefaultPartner ?? string.Empty, command.DefaultPartnerValue ?? string.Empty, StringComparison.Ordinal))
                return true;

            if (oldValues.Count != newValues.Count) return true;

            return oldValues
                .Zip(newValues)
                .Any(p => !string.Equals(p.First.SourceValue ?? string.Empty, p.Second.SourceValue ?? string.Empty, StringComparison.Ordinal)
                    || !string.Equals(p.First.PartnerValue ?? string.Empty, p.Second.PartnerValue ?? string.Empty, StringComparison.Ordinal));
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: ESH-2206 · Chặn sửa bảng quy đổi khi bộ mã nằm trong hồ sơ ánh xạ đang dùng
        ///              (luật "đang dùng" ở ShareDataCodeSetUsageReader).
        /// Created date: 2026-10-30
        /// </summary>
        private async Task EnsureNotUsedByActiveMappingAsync(string? code)
        {
            var inUseMappings = await ShareDataCodeSetUsageReader.FindInUseMappingsAsync(_mappingRep, _subscriptionRep, code);
            if (inUseMappings.Count > 0)
                throw Oops.Oh(BaseMsg.ShareData.Exception.CodeSetValuesInUse, code ?? string.Empty, string.Join(", ", inUseMappings));
        }
        #endregion
    }
}
