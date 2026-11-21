using Modules.TMS.Controllers.PatrolTeam.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.PatrolTeam.Queries
{
    /// <summary>
    /// Author: System
    /// Description: Handler xử lý các query của PatrolTeam
    /// Created date: 07/11/2025
    /// </summary>
    public class PatrolTeamQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsPatrolTeam> _patrolTeamRep;
        private BaseRepository<TmsPatrolTeamMember> _memberRep;
        private BaseCacheService _cache;

        public PatrolTeamQueryHandler(
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
        /// Description: Lấy danh sách đội tuần tra
        /// Created date: 07/11/2025
        /// </summary>
        public async Task<List<PatrolTeamOutput>> HandleAsync(PatrolTeamInput command)
        {
            var cacheKey = $"{CacheConst.Tms.TmsPatrolTeam}:list:{command.Name}:{command.Code}:{command.Status}";

            return await _cache.GetOrAdd(cacheKey, async _ =>
            {
                var query = _patrolTeamRep.AsQueryable()
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code!))
                    .WhereIF(command.Status.HasValue, u => u.Status == (Shared.DTO.Enums.BaseEnums.StatusEnum)command.Status!.Value)
                    .OrderByDescending(u => u.CreateTime);

                var teams = await query.ToListAsync();
                var teamIds = teams.Select(t => t.ID).ToList();

                // Get all members for these teams
                var allMembers = await _memberRep.AsQueryable()
                    .Where(m => teamIds.Contains(m.PatrolTeamId))
                    .ToListAsync();

                // Map to output with members
                var result = teams.Select(t =>
                {
                    var output = t.Adapt<PatrolTeamOutput>();
                    output.Members = allMembers
                        .Where(m => m.PatrolTeamId == t.ID)
                        .Select(m => m.Adapt<PatrolTeamMemberOutput>())
                        .ToList();
                    return output;
                }).ToList();

                return result;
            });
        }
    }
}
