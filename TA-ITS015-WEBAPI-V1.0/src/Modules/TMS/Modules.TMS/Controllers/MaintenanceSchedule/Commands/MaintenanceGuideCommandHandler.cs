using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Core.Interfaces;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Commands;

/// <summary>
/// Author: System
/// Description: Handler xử lý các lệnh của MaintenanceGuide
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceGuideCommandHandler : IWolverineHandler
{
    private BaseRepository<TmsMaintenanceGuide> _guideRep;
    private BaseRepository<TmsEquipmentType> _equipmentTypeRep;
    private UserManager _user;
    private BaseCacheService _cache;
    private IUploadService _uploadService;



    public MaintenanceGuideCommandHandler(
        BaseRepository<TmsMaintenanceGuide> guideRep,
        BaseRepository<TmsEquipmentType> equipmentTypeRep,
        UserManager user,
        BaseCacheService cache,
        IUploadService uploadService)
    {
        _guideRep = guideRep;
        _equipmentTypeRep = equipmentTypeRep;
        _user = user;
        _cache = cache;
        _uploadService = uploadService;
    }

    /// <summary>
    /// Thêm mới hướng dẫn bảo trì
    /// </summary>
    public async Task HandleAsync(AddMaintenanceGuideInput command)
    {
        // Validate EquipmentType exists
        var equipmentTypeExists = await _equipmentTypeRep.IsAnyAsync(u => u.ID == command.EquipmentTypeId);
        if (!equipmentTypeExists)
            throw Oops.Oh(BaseMsg.TmsMaintenanceGuide.Exception.EquipmentTypeNotFound);

        // Check duplicate name for same EquipmentType + Manufacturer
        var duplicateExists = await _guideRep.IsAnyAsync(u =>
            u.EquipmentTypeId == command.EquipmentTypeId &&
            u.Manufacturer == command.Manufacturer &&
            u.Name == command.Name);
        if (duplicateExists)
            throw Oops.Oh(BaseMsg.TmsMaintenanceGuide.Exception.DuplicateName);

        var entity = command.Adapt<TmsMaintenanceGuide>();

        // Serialize checklist to JSON
        if (command.Checklist != null && command.Checklist.Any())
        {
            // Preserve input order; store only Task/Required/Note
            entity.Checklist = JsonConvert.SerializeObject(command.Checklist);
        }

        await _guideRep.InsertAsync(entity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceGuide); //ChuongLP - 01/04/2026 - Xóa cache sau khi thêm mới hướng dẫn bảo trì
    }

    /// <summary>
    /// Cập nhật hướng dẫn bảo trì
    /// </summary>
    public async Task HandleAsync(UpdateMaintenanceGuideInput command)
    {
        var entity = await _guideRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceGuide.Exception.NotFound);

        // Validate EquipmentType exists
        var equipmentTypeExists = await _equipmentTypeRep.IsAnyAsync(u => u.ID == command.EquipmentTypeId);
        if (!equipmentTypeExists)
            throw Oops.Oh(BaseMsg.TmsMaintenanceGuide.Exception.EquipmentTypeNotFound);

        // Check duplicate name (exclude current)
        var duplicateExists = await _guideRep.IsAnyAsync(u =>
            u.ID != command.ID &&
            u.EquipmentTypeId == command.EquipmentTypeId &&
            u.Manufacturer == command.Manufacturer &&
            u.Name == command.Name);
        if (duplicateExists)
            throw Oops.Oh(BaseMsg.TmsMaintenanceGuide.Exception.DuplicateName);

        var updatedEntity = command.Adapt(entity);

        // Serialize checklist to JSON
        if (command.Checklist != null && command.Checklist.Any())
        {
            // Preserve input order; store only Task/Required/Note
            updatedEntity.Checklist = JsonConvert.SerializeObject(command.Checklist);
        }
        else
        {
            updatedEntity.Checklist = null;
        }

        await _guideRep.UpdateAsync(updatedEntity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceGuide); //ChuongLP - 01/04/2026 - Xóa cache sau khi cập nhật hướng dẫn bảo trì

    }

    /// <summary>
    /// Xóa hướng dẫn bảo trì
    /// </summary>
    public async Task HandleAsync(DeleteMaintenanceGuideInput command)
    {
        var entity = await _guideRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceGuide.Exception.NotFound);

        await _guideRep.DeleteAsync(entity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceGuide); //ChuongLP - 01/04/2026 - Xóa cache sau khi xóa hướng dẫn bảo trì
    }

    /// <summary>
    /// Upload file tài liệu cho hướng dẫn bảo trì
    /// </summary>
    public async Task<GuideDocumentInfo> HandleAsync(UploadMaintenanceGuideDocumentInput command)
    {
        var entity = await _guideRep.GetByIdAsync(command.MaintenanceGuideId)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceGuide.Exception.NotFound);

        // Validate file
        if (command.File == null || command.File.Length == 0)
            throw Oops.Oh(BaseMsg.BaseException.NotExist);

        // Upload file using IUploadService with IFormFile
        var folderPath = "Upload/MaintenanceGuide/Documents";
        var fileName = $"{DateTime.Now:yyMMddHHmmss}_{command.File.FileName}";
        var filePath = await _uploadService.UploadAsync(command.File, folderPath, fileName);

        // Validate upload result
        if (string.IsNullOrEmpty(filePath))
            throw Oops.Oh(BaseMsg.BaseException.ServerError);

        // Parse existing documents
        var documents = new List<GuideDocumentInfo>();
        if (!string.IsNullOrEmpty(entity.Documents))
        {
            try
            {
                documents = JsonConvert.DeserializeObject<List<GuideDocumentInfo>>(entity.Documents) ?? new List<GuideDocumentInfo>();
            }
            catch { }
        }

        // Add new document
        var newDocument = new GuideDocumentInfo
        {
            FilePath = filePath,
            FileName = command.File.FileName,
            UploadedAt = DateTime.Now
        };
        documents.Add(newDocument);

        // Update entity with new documents list
        entity.Documents = JsonConvert.SerializeObject(documents);
        await _guideRep.UpdateAsync(entity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceGuide); //ChuongLP - 01/04/2026 - Xóa cache sau khi cập nhật tài liệu hướng dẫn bảo trì

        return newDocument;
    }
}
