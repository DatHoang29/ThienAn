using Modules.TMS.Controllers.PatrolTeam.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.PatrolTeam.Commands
{
    /// <summary>
    /// Author: System
    /// Description: Handler xử lý các lệnh của PatrolTeam
    /// Created date: 07/11/2025
    /// </summary>
    public class PatrolTeamCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsPatrolTeam> _patrolTeamRep;
        private BaseRepository<TmsPatrolTeamMember> _memberRep;
        private BaseCacheService _cache;

        public PatrolTeamCommandHandler(
            BaseRepository<TmsPatrolTeam> patrolTeamRep,
            BaseRepository<TmsPatrolTeamMember> memberRep,
            BaseCacheService cache)
        {
            _patrolTeamRep = patrolTeamRep;
            _memberRep = memberRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: System
        /// Description: Thêm mới đội tuần tra
        /// Created date: 07/11/2025
        /// Updated: 11/11/2025 - Cho phép lưu kèm danh sách thành viên
        /// </summary>
        public async Task HandleAsync(AddPatrolTeamInput command)
        {
            command.Code = command.Code?.Trim(); //  ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            // Kiểm tra trùng Name hoặc Code (chỉ khi có giá trị)
            if (!string.IsNullOrWhiteSpace(command.Name) && await _patrolTeamRep.IsAnyAsync(u => u.Name == command.Name))
                throw Oops.Oh(BaseMsg.TmsPatrolTeam.Exception.AlreadyExists, "Name");

            if (!string.IsNullOrWhiteSpace(command.Code) && await _patrolTeamRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.TmsPatrolTeam.Exception.AlreadyExists, "Code");

            command.CreateUId = null;

            // Begin transaction
            var db = _patrolTeamRep.Context;
            await db.AsTenant().BeginTranAsync();

            try
            {
                // Insert patrol team
                var team = command.Adapt<TmsPatrolTeam>();
                await _patrolTeamRep.InsertWithDiffLogAsync(team);

                // Insert members if provided
                if (command.Members != null && command.Members.Any())
                {
                    var members = command.Members.Select(m => new TmsPatrolTeamMember
                    {
                        PatrolTeamId = team.ID,
                        UserId = m.UserId,
                        Role = m.Role ?? "Member",
                        Remark = m.Remark
                    }).ToList();

                    await _memberRep.InsertRangeAsync(members);
                }

                await db.AsTenant().CommitTranAsync();
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsPatrolTeam);
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }
        }

        /// <summary>
        /// Author: System
        /// Description: Cập nhật đội tuần tra
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(UpdatePatrolTeamInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối 

            // Kiểm tra trùng Name hoặc Code (chỉ khi có giá trị, và loại trừ chính nó)
            if (!string.IsNullOrWhiteSpace(command.Name) &&
                await _patrolTeamRep.IsAnyAsync(u => u.ID != command.ID))
                throw Oops.Oh(BaseMsg.TmsPatrolTeam.Exception.AlreadyExists, "Name");

            if (!string.IsNullOrWhiteSpace(command.Code) &&
                await _patrolTeamRep.IsAnyAsync(u => u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.TmsPatrolTeam.Exception.AlreadyExists, "Code");

            var entity = await _patrolTeamRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.TmsPatrolTeam.Exception.NotFound);

            await _patrolTeamRep.UpdateWithDiffLogAsync(command.Adapt(entity));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsPatrolTeam);
        }

        /// <summary>
        /// Author: System
        /// Description: Xóa 1 đội tuần tra
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(DeletePatrolTeamInput command)
        {
            var entity = await _patrolTeamRep.GetFirstAsync(u => u.ID == command.ID)
                         ?? throw Oops.Oh(BaseMsg.TmsPatrolTeam.Exception.NotFound);

            await _patrolTeamRep.SoftDeleteAsync<TmsPatrolTeam>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsPatrolTeam);
        }

        /// <summary>
        /// Author: System
        /// Description: Xóa nhiều đội tuần tra
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(List<DeletePatrolTeamInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _patrolTeamRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await _patrolTeamRep.SoftDeleteAsync<TmsPatrolTeam>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsPatrolTeam);
        }

        /// <summary>
        /// Author: System
        /// Description: Thêm thành viên vào đội tuần tra
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(AddMemberToTeamInput command)
        {
            // Kiểm tra đội tuần tra tồn tại
            var team = await _patrolTeamRep.GetFirstAsync(u => u.ID == command.PatrolTeamId)
                       ?? throw Oops.Oh(BaseMsg.TmsPatrolTeam.Exception.NotFound);

            // Kiểm tra thành viên đã tồn tại trong đội chưa
            if (await _memberRep.IsAnyAsync(m => m.PatrolTeamId == command.PatrolTeamId && m.UserId == command.UserId))
                throw Oops.Oh(BaseMsg.TmsPatrolTeamMember.Exception.AlreadyExists);

            await _memberRep.InsertAsync(command.Adapt<TmsPatrolTeamMember>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsPatrolTeam);
        }

        /// <summary>
        /// Author: System
        /// Description: Xóa thành viên khỏi đội tuần tra
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(RemoveMemberFromTeamInput command)
        {
            var member = await _memberRep.GetFirstAsync(m => m.ID == command.MemberId)
                         ?? throw Oops.Oh(BaseMsg.TmsPatrolTeamMember.Exception.NotFound);

            await _memberRep.DeleteAsync(member);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsPatrolTeam);
        }

        /// <summary>
        /// Author: System
        /// Description: Cập nhật vai trò thành viên trong đội tuần tra
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(UpdateMemberRoleInput command)
        {
            var member = await _memberRep.GetFirstAsync(m => m.ID == command.MemberId)
                         ?? throw Oops.Oh(BaseMsg.TmsPatrolTeamMember.Exception.NotFound);

            member.Role = command.Role;
            await _memberRep.UpdateAsync(member);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsPatrolTeam);
        }
    }
}
