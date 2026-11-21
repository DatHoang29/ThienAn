using Modules.TMS.Infrastructure.Services.Incident.Configuration;
using Modules.TMS.Infrastructure.Services.Incident.Mapping;
using Modules.TMS.Infrastructure.Services.Incident.Classification;
using Modules.TMS.Infrastructure.Services.Incident.Rules;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Wolverine;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Helper;
using Furion.Localization;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure.Services.Incident;
using Shared.Utility.Utilities;
﻿using Modules.TMS.Core.Entities;
using static Shared.DTO.Enums.BaseEnums;
using TmsEquipment = Modules.TMS.Core.Entities.TmsEquipment;

namespace Modules.TMS.Infrastructure.Services.TrafficInfo
{
    public class TrafficInfoService : IScoped
    {
        private BaseRepository<TmsIncident> _tmsIncident;
        private BaseRepository<TmsEquipment> _tmsEquipment;
        private BaseRepository<TmsEquipmentType> _tmsEquipmentType;
        private BaseRepository<TmsModule> _tmsModule;
        private BaseRepository<TmsTrafficPlans> _tmsTrafficPlan;
        private BaseRepository<TmsEventType> _tmsEventType;


        private IncidentGroupResolver _groupResolver;
        private IMessageBus _messageBus;

        public TrafficInfoService(BaseRepository<TmsIncident> tmsIncident,
            BaseRepository<TmsEquipment> tmsEquipment,
                            BaseRepository<TmsEquipmentType> tmsEquipmentType,
                            BaseRepository<TmsModule> tmsModule,
                            BaseRepository<TmsTrafficPlans> tmsTrafficPlan,
                            BaseRepository<TmsEventType> tmsEventType, IncidentGroupResolver groupResolver,
                            IMessageBus messageBus)
        {
            _tmsIncident = tmsIncident;
            _tmsEquipment = tmsEquipment;
            _tmsEquipmentType = tmsEquipmentType;
            _tmsModule = tmsModule;
            _tmsTrafficPlan = tmsTrafficPlan;
            _tmsEventType = tmsEventType;
            _groupResolver = groupResolver;
            _messageBus = messageBus;
        }

        /// <summary>
        /// KienLT - 26/10/2026: số ngày hiển thị sự cố đang mở (SysOpConfig <c>open_incident_days_limit</c>), cùng
        /// nguồn với bảng sự cố trên màn hình giao thông. Handler SysOpConfig đã cache danh sách nên gọi mỗi vòng poll được.
        /// </summary>
        private async Task<int> OpenIncidentDaysLimitAsync()
        {
            var configs = await _messageBus.InvokeAsync<List<SysOpConfigOutput>>(new SysOpConfigInput
            {
                GroupCode = IncidentConst.DisplayWindow.GroupCode,
                Code = IncidentConst.DisplayWindow.OpenDaysLimitCode,
            });
            var value = configs?.FirstOrDefault(c => string.Equals(c.GroupCode, IncidentConst.DisplayWindow.GroupCode, StringComparison.OrdinalIgnoreCase)
                                                    && string.Equals(c.Code, IncidentConst.DisplayWindow.OpenDaysLimitCode, StringComparison.OrdinalIgnoreCase))?.Value;
            return TmsIncidentMonitorRules.DaysOrDefault(value, IncidentConst.DisplayWindow.DefaultOpenDays);
        }
        public async Task<List<TmsIncident>> Incident()
        {
            // Truy vấn trực tiếp bảng tmsIncident, không cần join với TmsZone
            var openStates = new[] { "1", "2", "3" };
            var incidents = await _tmsIncident.AsQueryable()
                .Where(x => x.IsDelete == null && openStates.Contains(x.State))
                .ToListAsync();

            // Trả về danh sách các đối tượng tmsIncident thỏa mãn điều kiện
            await _groupResolver.PopulateGroupsAsync(incidents);
            return incidents.Where(x => IncidentRuntimeFields.IsVisible(x)).ToList();
        }

        /// <summary>
        /// Xử lý lấy danh sách các vùng zone xảy ra sự kiện và thông tin xảy ra sự kiện
        /// Author: TuanHA
        /// DateCreated: 2026-04-09
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// KienLT - 23/10/2026: icon trên bản đồ — SỰ KIỆN hiện ngay khi có (kể cả lúc đăng ký), còn SỰ CỐ chỉ hiện
        /// khi đã được xác nhận (Đã xác nhận / Đang xử lý): hồ sơ sự cố vừa đăng ký chưa được người trực xác nhận là
        /// thật nên chưa đưa lên bản đồ. Nhóm chưa xác định (null) giữ hành vi cũ.
        /// </summary>
        private static bool IsMapIconVisible(TmsIncident incident)
            => !(IncidentEventGroupConst.Normalize(incident.EventGroup) == IncidentEventGroupConst.Incident
                 && incident.State == ((int)TmsIncidentStateEnum.Registration).ToString());

        public async Task<List<ListZoneIncidentOutput>> GetIncident()
        {
            // Hiển thị hồ sơ đăng ký, xác minh và đang xử lý.
            var listStateToDisplayICon = new List<string>()
            {
               ((int)TmsIncidentStateEnum.Registration).ToString(), ((int)TmsIncidentStateEnum.Verified).ToString(), ((int)TmsIncidentStateEnum.InProgress).ToString()
            };

            // KienLT - 26/10/2026: cùng cửa sổ thời gian với bảng sự cố (IncidentDisplayRules.Window): sự cố đang mở chỉ
            // hiện nếu bắt đầu trong open_incident_days_limit ngày — trước đây icon hiện mọi sự cố chưa đóng nên bản đồ
            // lệch với bảng. Trạng thái đã đóng không có trong listStateToDisplayICon nên cửa sổ "đã đóng" không dùng tới.
            var openFrom = BaseUtil.TimeServer.GetTimeServer().AddDays(-await OpenIncidentDaysLimitAsync());

            // Truy vấn trực tiếp bảng tmsIncident
            var zoneEvents = await _tmsIncident.AsQueryable()
                .Where(incident => incident.IsDelete == null && listStateToDisplayICon.Contains(incident.State))
                .Where(IncidentDisplayRules.Window(openFrom, openFrom))
                .ToListAsync();

            await _groupResolver.PopulateGroupsAsync(zoneEvents);
            zoneEvents = zoneEvents.Where(x => IncidentRuntimeFields.IsVisible(x) && IsMapIconVisible(x)).ToList();

            // Lấy danh sách các zone xảy ra sự cố
            var listZoneEventReturn = zoneEvents.Select(incident => new ListZoneIncidentOutput
            {
                ListZoneId = incident.ZoneId != null ? incident.ZoneId.Split(',').ToList() : new List<string>(),
                EventGroup = incident.EventGroup,
                EventTypeId = incident?.EventTypeId ?? "",
                IncidentId = incident.ID,
                TrafficPlanId = incident?.TrafficPlanId ?? "",
                State = incident?.State,
                StartDate = incident.StartDate ?? incident.CreateTime,
                KmNumber = incident?.KmNumber,
                MetNumber = incident?.MetNumber,
                Direction = incident?.InfluenceScope
            }).ToList();

            // Chạy vòng lặp lấy tên traffic name
            foreach (var item in listZoneEventReturn)
            {
                if (!string.IsNullOrEmpty(item.TrafficPlanId))
                {
                    item.TrafficPlanName = _tmsTrafficPlan.GetFirst(s => s.ID == item.TrafficPlanId)?.Name ?? "";
                }
                if (!string.IsNullOrEmpty(item.EventTypeId))
                {
                    var eventType = _tmsEventType.GetFirst(s => s.ID == item.EventTypeId);
                    var metadata = zoneEvents.FirstOrDefault(x => x.ID == item.IncidentId)?.Metadata;
                    item.EventTypeName = IncidentEventDisplayHelper.Name(eventType?.Code, eventType?.Name, metadata,
                        L.Text[BaseMsg.TmsIncident.Label.UnknownEventType].Value) ?? "";
                }
            }

            // Trả về danh sách đối tượng
            return listZoneEventReturn;
        }




        public async Task<(TmsEquipmentType equipmentType, int? equipmentState, string moduleName)> GetEquipmentTypeByEquipmentId(string equipmentId)
        {
            // Lấy Equipment từ tmsEquipment theo EquipmentId
            var equipment = await _tmsEquipment.AsQueryable()
                .Where(e => e.ID == equipmentId)
                .FirstAsync();

            if (equipment == null)
            {
                // Nếu không tìm thấy thiết bị, trả về null hoặc xử lý lỗi
                throw new Exception($"Equipment with ID {equipmentId} not found.");
            }

            // Lấy EquipmentType từ tmsEquipmentType theo EquipmentTypeId của thiết bị đã tìm được
            var equipmentType = await _tmsEquipmentType.AsQueryable()
                .Where(et => et.ID == equipment.EquipmentTypeId)
                .FirstAsync();

            if (equipmentType == null)
            {
                // Nếu không tìm thấy loại thiết bị, trả về null hoặc xử lý lỗi
                throw new Exception($"EquipmentType with ID {equipment.EquipmentTypeId} not found.");
            }

            // Lấy Module từ tmsModule theo ModuleId của loại thiết bị
            var module = await _tmsModule.AsQueryable()
                .Where(m => m.ID == equipmentType.ModuleId)
                .FirstAsync();

            string moduleName = module?.Name; // Lấy tên module nếu tồn tại

            // Trả về thông tin loại thiết bị và tên module
            return (equipmentType, equipment.EquipmentState, moduleName); // Trả về cả equipmentType, equipmentState và moduleName
        }


    }
}
