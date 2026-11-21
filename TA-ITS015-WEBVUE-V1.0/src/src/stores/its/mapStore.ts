import { defineStore } from 'pinia';
import { getAPI } from '/@/utils/axios-utils';
import mittBus from '/@/utils/mitt';
import { TmsMapApi, TmsMapLocationApi, TmsEquipmentApi, TmsEquipmentTypeApi, TmsZoneApi, TmsWorkUnitApi, TmsTrafficInfoApi, TmsEventTypeApi } from '/@/api-services/tms';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { VmsCurrentApi } from '/@/api-services/vms/api';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { applyVmsCurrentToList } from '/@/transporter/hooks/useTransporterVms';
import { batchUpdateZoneSpeed, effectiveSpeed, getZoneSpeedConfigList, type DirectionSpeedDefault, type EffectiveSpeed, type ZoneSpeedRow } from '/@/services/tms/zone-speed';

/** Số zone tối đa mỗi lần gọi BatchUpdateSpeed — khớp ZoneSpeedBatchRules.MaxItems phía BE. */
const ZONE_SPEED_SAVE_CHUNK_SIZE = 500;

/** Min/max đang nhập của 1 zone; null = bỏ ngưỡng riêng, dùng mặc định theo hướng. */
export interface ZoneSpeedValue {
    minSpeed: number | null;
    maxSpeed: number | null;
}


interface MapState {
    mapList: any[];
    currentMap: any | null;
    mapLocation: any | null;
    loading: boolean;
    error: string | null;
    equipmentList: any[];
    equipmentTypeList: any[];
    zoneList: any[];
    zoneTypeList: any[];
    workUnitList: any[];
    workUnitTypeList: any[];
    itemGroupList: any[];
    incidentZoneDataExistInTrafficInfoList: any[];
    /** Icon sự kiện VDS trực tiếp (chưa có hồ sơ), cùng dạng icon sự cố — bản đồ vẽ chung theo zone + lý trình. */
    liveEventIconList: any[];
    incidentZoneDataDetails: any[]
    vmsCurrentList: any[];
    baseUrl: string;
    eventTypeList: any[];
    showIconName?: boolean | null;
    /** Cấu hình tốc độ theo zoneId (màn cấu hình bản đồ) — nạp bằng fetchZoneSpeed. */
    zoneSpeedRows: Record<string, ZoneSpeedRow>;
    /** Mặc định min/max theo hướng (VdsConfig). */
    zoneSpeedDefaults: DirectionSpeedDefault[];
    /** Min/max đã sửa ở tab Vị trí, chưa lưu — lưu cùng "Lưu bản đồ" (saveZoneSpeedDrafts). */
    zoneSpeedDrafts: Record<string, ZoneSpeedValue>;
    equipmentStatusConfig: any[];
}
// Enhanced getItemDetails function
interface ItemDetails {
    name: string;
    iconUrl: string;
    unit?: string;
    measurement?: number;
    type?: string;
    additionalInfo?: any;
}
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;


export const useMapStore = defineStore('map', {
    state: (): MapState => ({
        mapList: [],
        equipmentList: [],
        equipmentTypeList: [],
        zoneList: [],
        zoneTypeList: [],
        workUnitList: [],
        workUnitTypeList: [],
        itemGroupList: [],
        currentMap: null,
        mapLocation: null,
        loading: false,
        error: null,
        incidentZoneDataExistInTrafficInfoList: [],
        liveEventIconList: [],
        incidentZoneDataDetails: [],
        vmsCurrentList: [],
        baseUrl: '' as string,
        eventTypeList: [],
        showIconName: false,
        zoneSpeedRows: {},
        zoneSpeedDefaults: [],
        zoneSpeedDrafts: {},
        equipmentStatusConfig: [] as any,
    }),

    getters: {
        defaultMap: (state) => state.mapList.find((map) => map.isDefault === 1),
        hasError: (state) => !!state.error,
    },

    actions: {
        // Reset error state
        clearError() {
            this.error = null;
        },
        async getMapList() {
            if (this.mapList.length === 0) {
                await this.fetchMapList();
            }
            return this.mapList;
        },
        // Fetch list of all maps
        async fetchMapList() {
            try {
                this.loading = true;
                this.clearError();
                const response = await getAPI(TmsMapApi).apiTmsTmsmapListGet();
                this.mapList = response?.data?.result?.filter((x) => x.status == (StatusTypeEnum.Enable as number)) || [];
            } catch (error) {
                console.error('Error fetching map list:', error);
                this.error = 'Failed to fetch map list';
                throw error;
            } finally {
                this.loading = false;
            }
        },

        // Fetch specific map configuration
        async fetchMapById(mapId: string) {
            try {
                this.loading = true;
                this.clearError();
                if (!this.mapList.length) {
                    await this.fetchMapList();
                }
                const map_config = this.mapList.find((item) => item.id === mapId);
                if (!map_config) {
                    throw new Error('Map configuration not found');
                }
                this.currentMap = map_config;
                return this.currentMap;
            } catch (error) {
                console.error('Error fetching map:', error);
                this.error = 'Failed to fetch map configuration';
                throw error;
            } finally {
                this.loading = false;
            }
        },

        // Fetch map location data
        async fetchMapLocation(mapId: string) {
            try {
                this.loading = true;
                this.clearError();
                const response = await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationDatalistIdGet(mapId);
                this.mapLocation = response?.data?.result?.[0];
                return this.mapLocation;
            } catch (error) {
                console.error('Error fetching map location:', error);
                this.error = 'Failed to fetch map location data';
                throw error;
            } finally {
                this.loading = false;
            }
        },

        // Load all data for a specific map
        async loadMapData(mapId: string) {
            try {
                this.loading = true;
                this.clearError();

                // Fetch both map config and location data in parallel
                const [mapConfig, locationData] = await Promise.all([this.fetchMapById(mapId), this.fetchMapLocation(mapId)]);

                return {
                    mapConfig,
                    locationData,
                };
            } catch (error) {
                console.error('Error loading map data:', error);
                this.error = 'Failed to load map data';
                throw error;
            } finally {
                this.loading = false;
            }
        },
        async fetchEquipmentList() {
            const response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
            this.equipmentList = response?.data?.result || [];
        },
        async fetchEquipmentTypeList() {
            const response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
            this.equipmentTypeList = response?.data?.result || [];
        },
        async fetchZoneList() {
            const response = await getAPI(TmsZoneApi).apiTmsTmszoneGetliststatusinfoGet();
            this.zoneList = response?.data?.result || [];
        },
        /**
         * Author: KienLT
         * Description: Nạp cấu hình tốc độ min/max của zone + mặc định theo hướng cho màn cấu hình bản đồ
         *              (nhãn tốc độ và ô nhập ở tab Vị trí). Không đụng tới bản nháp đang sửa.
         * Created date: 09/10/2026
         */
        async fetchZoneSpeed() {
            const { directionDefaults, zones } = await getZoneSpeedConfigList();
            this.zoneSpeedDefaults = directionDefaults;
            this.zoneSpeedRows = Object.fromEntries(zones.map(zone => [zone.id, zone]));
        },
        /** Min/max đang có hiệu lực trên màn hình: bản nháp nếu đã sửa, không thì giá trị đã lưu. */
        getZoneSpeedValue(zoneId: string): ZoneSpeedValue | undefined {
            const row = this.zoneSpeedRows[String(zoneId)];
            if (!row) return undefined;
            return this.zoneSpeedDrafts[row.id] ?? { minSpeed: row.minSpeed ?? null, maxSpeed: row.maxSpeed ?? null };
        },
        /** Ngưỡng hiệu lực (ghép mặc định theo hướng) — cùng cách tính với tab Tốc độ của trang Zone. */
        getEffectiveZoneSpeed(zoneId: string): EffectiveSpeed | undefined {
            const row = this.zoneSpeedRows[String(zoneId)];
            const value = this.getZoneSpeedValue(zoneId);
            if (!row || !value) return undefined;
            const defaults = new Map(this.zoneSpeedDefaults.map(item => [item.direction, item]));
            return effectiveSpeed(row, value.minSpeed, value.maxSpeed, defaults);
        },
        /** Ghi bản nháp 1 vế; trùng giá trị đã lưu thì bỏ nháp. */
        setZoneSpeedDraft(zoneId: string, key: keyof ZoneSpeedValue, value: number | null) {
            const row = this.zoneSpeedRows[String(zoneId)];
            const current = this.getZoneSpeedValue(zoneId);
            if (!row || !current) return;
            const next = { ...current, [key]: value };
            const drafts = { ...this.zoneSpeedDrafts };
            if (next.minSpeed === (row.minSpeed ?? null) && next.maxSpeed === (row.maxSpeed ?? null)) delete drafts[row.id];
            else drafts[row.id] = next;
            this.zoneSpeedDrafts = drafts;
        },
        /** Zone đang nháp có khai cả 2 vế mà Min > Max — BE từ chối cả lô nên phải chặn trước khi lưu. */
        getInvalidZoneSpeedDraftIds(): string[] {
            return Object.entries(this.zoneSpeedDrafts)
                .filter(([, value]) => value.minSpeed != null && value.maxSpeed != null && value.minSpeed > value.maxSpeed)
                .map(([id]) => id);
        },
        /** Lưu các bản nháp tốc độ (chia lô theo giới hạn BE); trả số zone đã cập nhật (0 nếu không có gì để lưu). */
        async saveZoneSpeedDrafts(): Promise<number> {
            const entries = Object.entries(this.zoneSpeedDrafts);
            if (!entries.length) return 0;
            let updated = 0;
            for (let index = 0; index < entries.length; index += ZONE_SPEED_SAVE_CHUNK_SIZE) {
                const chunk = entries.slice(index, index + ZONE_SPEED_SAVE_CHUNK_SIZE);
                updated += await batchUpdateZoneSpeed(chunk.map(([id, value]) => ({ id, ...value })));
            }
            const rows = { ...this.zoneSpeedRows };
            entries.forEach(([id, value]) => { if (rows[id]) rows[id] = { ...rows[id], ...value }; });
            this.zoneSpeedRows = rows;
            this.zoneSpeedDrafts = {};
            return updated;
        },
        async fetchZoneTypeList() {
            const response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_zone_type');
            this.zoneTypeList = response?.data?.result || [];
        },
        async fetchWorkUnitList() {
            const response = await getAPI(TmsWorkUnitApi).apiTmsTmsworkunitListGet();
            this.workUnitList = response?.data?.result || [];
        },
        async fetchWorkUnitTypeList() {
            const response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('work_unit_type');
            this.workUnitTypeList = response?.data?.result || [];
        },
        async fetchItemGroupList() {
            const response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_object_type');
            this.itemGroupList = response?.data?.result || [];
        },
        async fetchTrafficInforBaseEquipmentId(selectedEquipmentId: any) {
            const response = await getAPI(TmsTrafficInfoApi).apiTmsTmstrafficinfoGetequipmenttypebyequipmentidEquipmentIdGet(selectedEquipmentId);
            return response?.data?.result || [];
        },
        async fetchIncidentZoneDataExistInTrafficInfo() {
            const incidentZoneData = await getAPI(TmsTrafficInfoApi).apiTmsTmstrafficinfoCheckincidentexistsPost();
            this.incidentZoneDataExistInTrafficInfoList = incidentZoneData?.data?.result || [];
        },
        async fetchIncidentZoneDataDetails() {
            const incidentZoneDataDetails = await getAPI(TmsTrafficInfoApi).apiTmsTmstrafficinfoCheckincidentPost();
            this.incidentZoneDataDetails = incidentZoneDataDetails?.data?.result || [];
        },
        async fetchSysConfigData(type: any) {
            const sysConfigData = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet(type);
            return sysConfigData?.data?.result || [];
        },
        async fetchVmsCurrentListGet() {
            const response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
            this.vmsCurrentList = response?.data?.result || [];
        },
        // Cập nhật 1 bản ghi VMS/LCS vào vmsCurrentList từ dữ liệu NATS (không gọi lại API)
        updateVmsCurrentFromNats(payload: any) {
            this.vmsCurrentList = applyVmsCurrentToList(this.vmsCurrentList, payload);
        },
        async fetchSystemSysconfigFileserverGet() {
            // Dùng chung store media: chỉ gọi API 1 lần cho toàn app, baseUrl giữ lại cho code cũ
            this.baseUrl = await useItsMediaServerStore().ensureLoaded();
        },
        async fetchEventTypeData() {
            const response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
            this.eventTypeList = response?.data?.result || [];
        },

        /**
         * Lắng nghe sự kiện trạng thái thiết bị từ NATS thông qua mittBus
         */
        setupNatsListeners() {
            mittBus.on('event-dm-status' as any, (data: any) => {
                console.log('[Nats] event-dm-status', data);
                this.updateEquipmentStatusFromNats(data);
            });
        },

        /**
         * Cập nhật trạng thái thiết bị trong store khi nhận tin từ NATS
         */
        //   updateEquipmentStatusFromNats(payload: any) {
        //     console.log('nat data', payload)
        //     // const { Info: info, Data: data } = payload.Result;
        //     // console.log('data', data)
        //     if (!payload || !payload.Result || !Array.isArray(payload.Result) || payload.Result.length === 0) {
        //         console.warn('>>> [NATS] Result is empty or not an array');
        //         return;
        //     }
        //     const info = payload.Result[0]?.Info;
        //     const data = payload.Result[0]?.Data;
        //     console.log('[NATS]CHECK info', info)

        //     if (!info) return;

        //     const refId = info.ReferenceId
        //     //natEquipmentState
        //     const natEState = data.Status
        //     if (!refId || natEState === undefined) return;

        //     const equipment = this.equipmentList.find((item) => item.id === refId);
        //     if (equipment) {
        //         console.log(`[MapStore] Updating NATS status for ${refId}: ${equipment.equipmentState} -> ${natEState}`);
        //         equipment.equipmentState = natEState;
        //         // console.log('equipment', equipment)
        //     } else {
        //         console.log(`[MapStore] Equipment not found for ${refId}`);
        //     }
        // },
        /**
         * Cập nhật trạng thái thiết bị nhận từ hook NATS và trả về các thiết bị thực sự đổi state.
         * Vẫn chấp nhận payload FMS cũ để tương thích với các caller trước đây.
         */
        updateEquipmentStatusFromNats(payload: any): string[] {
            const updates = Array.isArray(payload)
                ? payload
                : (Array.isArray(payload?.Result)
                    ? payload.Result.map((item: any) => ({
                        equipmentId: item?.Info?.ReferenceId,
                        equipmentState: item?.Data?.Status,
                    }))
                    : []);
            const changedEquipmentIds = new Set<string>();

            updates.forEach((update: any) => {
                const equipmentId = update?.equipmentId ?? update?.ReferenceId;
                const equipmentState = update?.equipmentState ?? update?.Status;
                if (equipmentId == null || equipmentState == null) return;

                const index = this.equipmentList.findIndex((equipment) => String(equipment.id) === String(equipmentId));
                if (index === -1) {
                    console.warn(`[MapStore] Equipment not found for ReferenceId: ${equipmentId}`);
                    return;
                }

                if (String(this.equipmentList[index].equipmentState) === String(equipmentState)) return;

                this.equipmentList[index] = {
                    ...this.equipmentList[index],
                    equipmentState,
                };
                changedEquipmentIds.add(String(equipmentId));
            });

            return [...changedEquipmentIds];
        },

        async loadData() {
            await this.fetchEquipmentList();
            await this.fetchEquipmentTypeList();

            await this.fetchZoneList();
            await this.fetchZoneTypeList();

            await this.fetchWorkUnitList();
            await this.fetchWorkUnitTypeList();

            await this.fetchItemGroupList();
            await this.fetchMapList();
            await this.fetchVmsCurrentListGet();
            await this.fetchIncidentZoneDataExistInTrafficInfo();
            await this.fetchIncidentZoneDataDetails();
            await this.fetchSystemSysconfigFileserverGet();
            await this.fetchEventTypeData();

            this.equipmentStatusConfig = getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState);
        },
        getItemDetails(id: string, itemGroup: string | undefined): ItemDetails {
            try {
                if (!id || !itemGroup) {
                    return { name: 'Unknown', iconUrl: '' };
                }

                switch (itemGroup) {
                    case 'device': {
                        // Equipment

                        const equipment = this.equipmentList.find((item) => item.id === id);

                        if (!equipment) return { name: 'Unknown Equipment', iconUrl: '', type: 'equipment' };

                        const { equipmentState: equipmentStatus } = equipment;
                        const equiptState = this.equipmentStatusConfig.find((item: any) => item.code === String(equipmentStatus));

                        // lấy equipment Type
                        const equipmentType = this.equipmentTypeList.find((type) => type.id === equipment.equipmentTypeId);

                        // Switch case dựa vào trạng thái thiết bị chọn icon tương ứng
                        let iconUrlEquipmentType = equipmentType?.defaultIcon || '';
                        switch (equiptState?.value) {
                            case 'active':
                                iconUrlEquipmentType = equipmentType?.activeIcon || '';
                                break;
                            case 'inactive':
                                iconUrlEquipmentType = equipmentType?.inactiveIcon || '';
                                break;
                            case 'fault':
                                iconUrlEquipmentType = equipmentType?.faultIcon || '';
                                break;
                            default:
                                iconUrlEquipmentType = equipmentType?.defaultIcon || '';
                                break;
                        }

                        return {
                            name: equipment.name + '(Km ' + equipment.kmNumber + '+' + equipment.metNumber + ')',
                            iconUrl: iconUrlEquipmentType,
                            type: equipmentType?.name || 'equipment',
                            unit: equipment.unit || '',
                            measurement: equipment.measurement || 0,
                            additionalInfo: {
                                status: equipment.status,
                                model: equipment.model,
                                serialNumber: equipment.serialNumber,
                                equipmentState: equipment.equipmentState,
                                remark: equiptState?.remark,
                                equipStateName: equiptState?.name,
                            },
                        };
                    }

                    case 'zone': {
                        // Zones
                        const zone = this.zoneList.find((item) => item.id == id);

                        if (!zone) return { name: 'Unknown Zone', iconUrl: '', type: 'zone' };

                        return {
                            name: zone.name,
                            iconUrl: '',
                            type: zone.type || 'zone',
                            unit: 'm²',
                            measurement: zone.area || 0,
                            additionalInfo: {
                                description: zone.description,
                                capacity: zone.capacity,
                                zoneType: zone.type,
                                zoneStatusCondition: zone.zoneStatus.condition,
                            },
                        };
                    }

                    case 'unit': {
                        // Work Units
                        const workUnit = this.workUnitList.find((item) => item.id === id);

                        if (!workUnit) return { name: 'Unknown Work Unit', iconUrl: '', type: 'unit' };

                        return {
                            name: workUnit.name,
                            iconUrl: '',
                            type: workUnit.type || 'unit',
                            unit: workUnit.unit || '',
                            measurement: workUnit.measurement || 0,
                            additionalInfo: {
                                description: workUnit.description,
                                status: workUnit.status,
                            },
                        };
                    }

                    default:
                        return { name: 'Unknown', iconUrl: '' };
                }
            } catch (error) {
                console.error('Error getting item details:', error);
                return { name: 'Unknown', iconUrl: '' };
            }
        },
        getIncidentDetails(incidentId: string): any {
            if (!incidentId) return null;

            const incidentDetail =
                this.incidentZoneDataDetails.find(
                    (detail: any) => String(detail?.id) === String(incidentId)
                ) || null;

            if (!incidentDetail) {
                return null;
            }

            const trafficInfo =
                this.incidentZoneDataExistInTrafficInfoList.find(
                    (info: any) => String(info?.incidentId) === String(incidentId)
                ) || null;

            return {
                ...incidentDetail,
                trafficInfo,
            };
        }
    },
});
