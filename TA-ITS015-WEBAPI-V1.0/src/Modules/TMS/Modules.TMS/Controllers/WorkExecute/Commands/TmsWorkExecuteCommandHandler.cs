using Modules.TMS.Infrastructure.Services.Incident.Classification;
using Modules.TMS.Infrastructure.Services.Incident;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.WorkExecute;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Core.Utilities.Constants;
using Shared.Infrastructure.Extensions;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.WorkExecute.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 20/11/2024
    /// </summary>
    public class TmsWorkExecuteCommandHandler : IWolverineHandler
    {
        private IMessageBus _messageBus;
        private BaseRepository<TmsWorkExecute> _workExecuteRepo;

        private IncidentGroupResolver _groupResolver;

        public TmsWorkExecuteCommandHandler(BaseRepository<TmsWorkExecute> workExecuteRepo, IMessageBus messageBus, IncidentGroupResolver groupResolver)
        {
            this._groupResolver = groupResolver;
            this._messageBus = messageBus;
            this._workExecuteRepo = workExecuteRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật trạng thái state cho một record
        /// Created date: 20/11/2024
        /// </summary>
        public async Task<bool> HandleAsync(TmsWorkExecuteStateInput command)
        {
            if (string.IsNullOrWhiteSpace(command.Id) || string.IsNullOrWhiteSpace(command.State))
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Chỉ cập nhật state cho những work-execute hiện tại có state = Waiting
            var workExecute = await this._workExecuteRepo.GetByIdAsync(command.Id);
            if (workExecute == null || workExecute.State != ((int)TmsWorkExecuteStateEnum.Waiting).ToString())
                throw Oops.Oh(BaseMsg.BaseValidation.MustBeValidValue, BaseMsg.BaseEntity.State);

            var parent = await this._messageBus.InvokeAsync<TmsIncidentOutput>(new TmsIncidentSingleInput { Id = workExecute.IncidentId });
            if (parent is null || parent.IsDelete != null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            await this._groupResolver.PopulateGroupsAsync(new[] { parent });
            if (!IncidentEventGroupConst.AllowsProcessing(parent.EventGroup, parent.State))
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.NotProcessableIncident);

            // Kiểm tra tính hợp lệ của trạng thái state truyền vào
            int state = 0;
            if (!int.TryParse(command.State, out state))
                throw Oops.Oh(BaseMsg.BaseValidation.MustBeValidValue, BaseMsg.BaseEntity.State);

            // Kiểm tra tính hợp lệ của trạng thái state truyền vào
            if (state != (int)TmsWorkExecuteStateEnum.Waiting
                    && state != (int)TmsWorkExecuteStateEnum.Executed
                    && state != (int)TmsWorkExecuteStateEnum.Skipped)
                throw Oops.Oh(BaseMsg.BaseValidation.MustBeValidValue, BaseMsg.BaseEntity.State);

            string? userId = App.User?.FindFirst(ClaimConst.Account)?.Value ?? command.Actor;
            if (state == (int)TmsWorkExecuteStateEnum.Waiting)
            {
                workExecute.ExecuteBy = null;
                workExecute.ExecuteDate = null;

                workExecute.UpdateTime = DateTime.Now;
                workExecute.UpdateUId = userId;
            }
            else if (state == (int)TmsWorkExecuteStateEnum.Executed)
            {
                workExecute.ExecuteBy = userId;
                workExecute.ExecuteDate = DateTime.Now;

                workExecute.UpdateTime = DateTime.Now;
                workExecute.UpdateUId = userId;
            }
            else if (state == (int)TmsWorkExecuteStateEnum.Skipped)
            {
                workExecute.UpdateTime = DateTime.Now;
                workExecute.UpdateUId = userId;
            }

            workExecute.State = state.ToString();
            if (!string.IsNullOrWhiteSpace(command.Remark))
                workExecute.Remark = command.Remark;

            var updatedCount = await this._workExecuteRepo.UpdateWithDiffLogAsync(command.Adapt(workExecute));

            // Thông báo fronend biết nếu cập nhật state cho work-execute thành công
            if (updatedCount > 0)
            {
                var incident = await this._messageBus.InvokeAsync<TmsIncidentOutput>(new TmsIncidentSingleInput() { Id = workExecute.IncidentId });

                this.notifyToFrontEnd(incident, workExecute, state, command.Actor);
            }

            return updatedCount > 0;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thông báo frontend về biến động trong phân hệ sự cố
        /// Created date: 20/11/2024
        /// </summary>
        private void notifyToFrontEnd(TmsIncident incident, TmsWorkExecute workExecute, int state, string? actor = null)
        {
            var broadcastMessage = string.Empty;
            var broadcastDateTime = DateTime.Now.Ticks;
            var incidentName = incident?.Name ?? "Incident";
            var accountInfo = actor
                ?? $"{App.User?.FindFirst(ClaimConst.NickName)?.Value ?? App.User?.FindFirst(ClaimConst.OrgName)?.Value}";

            switch (state)
            {
                case (int)TmsWorkExecuteStateEnum.Executed:
                    broadcastMessage = "lz.message.tmsWorkExecute.executedNotify";
                    break;
                case (int)TmsWorkExecuteStateEnum.Skipped:
                    broadcastMessage = "lz.message.tmsWorkExecute.skippedNotify";
                    break;
            }

            if (!string.IsNullOrWhiteSpace(broadcastMessage))
            {
                ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.Incident, new
                {
                    broadcastCode = $"{incident?.ID}-{workExecute.ID}-{broadcastDateTime}-{state}",
                    broadcastObject = IncidentBroadcastObjectEnum.WorkExecute.ToString(),
                    broadcastMessage = new { Message = broadcastMessage, Incident = incident?.Name ?? "Incident", Work = workExecute.WorkName, Account = accountInfo },
                    broadcastDateTime
                });
            }
        }
    }
}
