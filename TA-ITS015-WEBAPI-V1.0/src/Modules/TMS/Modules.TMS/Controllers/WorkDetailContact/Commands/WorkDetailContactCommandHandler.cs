using Modules.TMS.Controllers.WorkDetailContact.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.TmsWorkDetailContact;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.WorkDetailContact.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng TmsWorkDetailContact
    /// Created date: 11/08/2024
    /// </summary>
    public class WorkDetailContactCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsWorkDetailContacts> _tmsWorkDetailContactRep;
        public TmsWorkDetailContactService _tmsWorkDetailContact;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public WorkDetailContactCommandHandler(BaseRepository<TmsWorkDetailContacts> TmsWorkDetailContactRep, TmsWorkDetailContactService tmsWorkDetailContact,
            BaseCacheService cache, UserManager userManager)
        {
            _tmsWorkDetailContactRep = TmsWorkDetailContactRep;
            _cache = cache;
            _userManager = userManager;
            _tmsWorkDetailContact = tmsWorkDetailContact;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddTmsWorkDetailContactInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            // Chặn trùng: cùng 1 công việc (WorkDetailId) không được gắn trùng 1 liên hệ (WorkContactId)
            if (await _tmsWorkDetailContactRep.IsAnyAsync(u =>
                    u.WorkDetailId == command.WorkDetailId && u.WorkContactId == command.WorkContactId && u.IsDelete == null))
                throw Oops.Oh(BaseMsg.TmsWorkDetailContact.Exception.ContactExist);

            // Nếu OrderNo đã tồn tại, cần tăng OrderNo cho các bản ghi khác
            if (command.OrderNo != null)
            {
                // Lấy các bản ghi có OrderNo >= command.OrderNo theo WorkDetailId
                var contactsToUpdate = await _tmsWorkDetailContactRep.AsQueryable()
                    .Where(u => u.WorkDetailId == command.WorkDetailId && u.OrderNo >= command.OrderNo)
                    .OrderBy(u => u.OrderNo)
                    .ToListAsync();

                // Tăng OrderNo cho các bản ghi hiện tại
                foreach (var contact in contactsToUpdate)
                {
                    contact.OrderNo += 1;
                    await _tmsWorkDetailContactRep.UpdateAsync(contact);
                }
            }

            // Set CreateUId là null
            command.CreateUId = null;

            // Thêm bản ghi mới với OrderNo đã được xử lý
            await _tmsWorkDetailContactRep.InsertWithDiffLogAsync(command.Adapt<TmsWorkDetailContacts>());
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng TmsWorkDetailContact
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsWorkDetailContactInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            // Chặn trùng: cùng 1 công việc (WorkDetailId) không được gắn trùng 1 liên hệ (WorkContactId), bỏ qua chính bản ghi đang sửa
            if (await _tmsWorkDetailContactRep.IsAnyAsync(u =>
                    u.WorkDetailId == command.WorkDetailId && u.WorkContactId == command.WorkContactId && u.ID != command.ID && u.IsDelete == null))
                throw Oops.Oh(BaseMsg.TmsWorkDetailContact.Exception.ContactExist);

            // Lấy bản ghi theo ID hoặc ném ra lỗi nếu không tìm thấy
            var entity = await _tmsWorkDetailContactRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Kiểm tra quyền truy cập
            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(ErrorCodeEnum.D6002);

            // Lấy giá trị OrderNo ban đầu của bản ghi hiện tại
            var originalOrderNo = entity.OrderNo;

            // Nếu giá trị OrderNo mới khác với giá trị ban đầu
            if (command.OrderNo != originalOrderNo)
            {
                // Nếu OrderNo mới nhỏ hơn OrderNo ban đầu, cần tăng các OrderNo từ OrderNo mới đến OrderNo ban đầu
                if (command.OrderNo < originalOrderNo)
                {
                    // Tăng OrderNo từ command.OrderNo đến originalOrderNo - 1
                    var contactsToIncrease = await _tmsWorkDetailContactRep.AsQueryable()
                        .Where(u => u.WorkDetailId == command.WorkDetailId && u.OrderNo >= command.OrderNo && u.OrderNo < originalOrderNo)
                        .OrderBy(u => u.OrderNo)
                        .ToListAsync();

                    foreach (var contact in contactsToIncrease)
                    {
                        contact.OrderNo += 1;
                        await _tmsWorkDetailContactRep.UpdateAsync(contact);
                    }
                }
                // Nếu OrderNo mới lớn hơn OrderNo ban đầu, cần giảm các OrderNo từ OrderNo ban đầu + 1 đến OrderNo mới
                else if (command.OrderNo > originalOrderNo)
                {
                    // Giảm OrderNo từ originalOrderNo + 1 đến command.OrderNo
                    var contactsToDecrease = await _tmsWorkDetailContactRep.AsQueryable()
                        .Where(u => u.WorkDetailId == command.WorkDetailId && u.OrderNo > originalOrderNo && u.OrderNo <= command.OrderNo)
                        .OrderBy(u => u.OrderNo)
                        .ToListAsync();

                    foreach (var contact in contactsToDecrease)
                    {
                        contact.OrderNo -= 1;
                        await _tmsWorkDetailContactRep.UpdateAsync(contact);
                    }
                }

                // Cập nhật OrderNo của bản ghi hiện tại
                entity.OrderNo = command.OrderNo;
            }

            // Cập nhật bản ghi hiện tại với các giá trị từ command
            await _tmsWorkDetailContactRep.UpdateWithDiffLogAsync(command.Adapt(entity));
        }


        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteTmsWorkDetailContactInput command)
        {
            var workDetailContacts = await _tmsWorkDetailContactRep.GetFirstAsync(u => u.ID == command.ID)
                                     ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            //if (!_userManager.SuperAdmin && workDetailContacts.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(ErrorCodeEnum.D6002);

            await _tmsWorkDetailContactRep.DeleteAsync(workDetailContacts);
            //await _tmsWorkDetailContact.deleteworkDetailContacts(workDetailContacts);   
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsWorkDetailContactInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsWorkDetailContactRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);


            await _tmsWorkDetailContactRep.DeleteAsync(l => ids.Contains(l.ID));
        }
    }
}
