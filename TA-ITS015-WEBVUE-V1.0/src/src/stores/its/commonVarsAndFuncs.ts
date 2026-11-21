import { defineStore } from 'pinia';
import { getAPI } from '/@/utils/axios-utils';
import { SysConfigDataApi, SysConfigTypeApi, SysOpConfigApi } from '/@/api-services/cfgSystem';
import { TmsModulesApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { TmsIncidentStateEnum, TmsWorkExecuteStateEnum, TmsWorkShiftEnum } from '/@/types/enums';

export const useCommonVarsAndFuncs = defineStore('commonVarsAndFuncs', {
    // Using reactive to react and share this global-instance for components.
    state: () => ({
        serverHost: null,
        operationConfigList: null,
        systemConfigTypeList: null,
        systemConfigDataList: null,
        userOrgList: [], // Default must be an empty array

        // Cache loại thiết bị (equipmentType) thuộc module CCTV, dùng để xác định
        // thiết bị nào là camera. null = chưa load; [] hoặc mảng item = đã load xong.
        cctvEquipmentTypeList: null as any,
    }),
    getters: {

    },
    actions: {
        async loadDefaultData() {
            if ((this.operationConfigList?.length ?? 0) == 0) {
                const response = await getAPI(SysOpConfigApi).apiCfgsystemSysopconfigListGet();
                this.operationConfigList = response.data.result;
            }

            if ((this.systemConfigTypeList?.length ?? 0) == 0) {
                const response = await getAPI(SysConfigTypeApi).apiCfgsystemSysconfigtypeListGet();
                this.systemConfigTypeList = response.data.result;
            }

            if ((this.systemConfigDataList?.length ?? 0) == 0) {
                const response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataListGet();
                this.systemConfigDataList = response.data.result;
            }
        },
        // Operation Config
        async getOperationConfig(configCode: string) {
            if ((this.operationConfigList?.length ?? 0) == 0) {
                const response = await getAPI(SysOpConfigApi).apiCfgsystemSysopconfigListGet();
                this.operationConfigList = response.data.result;
            }

            return this.operationConfigList?.find(config => config.code?.trim().toLowerCase() == configCode?.trim().toLowerCase());
        },
        async getOperationConfigValue(configCode: string) {
            if ((this.operationConfigList?.length ?? 0) == 0) {
                const response = await getAPI(SysOpConfigApi).apiCfgsystemSysopconfigListGet();
                this.operationConfigList = response.data.result;
            }

            return this.operationConfigList?.find(config => config.code?.trim().toLowerCase() == configCode?.trim().toLowerCase())?.value ?? null;
        },
        // System Operation Config 
        async getOperationConfigList(groupCode: string) {
            if ((this.operationConfigList?.length ?? 0) == 0) {
                const response = await getAPI(SysOpConfigApi).apiCfgsystemSysopconfigListGet();
                this.operationConfigList = response.data.result;
            }

            if ((groupCode?.length ?? 0) == 0)
                return this.operationConfigList;

            return this.operationConfigList?.filter(config => config.groupCode?.trim().toLowerCase() == groupCode?.trim().toLowerCase()) ?? null;
        },
        // System Config Data 
        async getSysConfigDataList(configTypeCode: string) {
            if ((this.systemConfigTypeList?.length ?? 0) == 0) {
                const response = await getAPI(SysConfigTypeApi).apiCfgsystemSysconfigtypeListGet();
                this.systemConfigTypeList = response.data.result;
            }

            if ((this.systemConfigDataList?.length ?? 0) == 0) {
                const response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataListGet();
                this.systemConfigDataList = response.data.result;
            }

            if ((configTypeCode?.length ?? 0) == 0)
                return this.systemConfigDataList;

            const sysConfigType = this.systemConfigTypeList?.find(config => config.code?.trim().toLowerCase() == configTypeCode?.trim().toLowerCase());

            return sysConfigType ? this.systemConfigDataList?.filter(config => config.configTypeId == sysConfigType?.id) : null;
        },
        isCCTVCamera(equipmentTypeCode: string, cameraTypeCode: string = 'CCTV') {
            if (!equipmentTypeCode)
                return false;

            return equipmentTypeCode == cameraTypeCode
                || equipmentTypeCode.toLowerCase() == cameraTypeCode.toLowerCase()
                || equipmentTypeCode.toLowerCase().includes(cameraTypeCode.toLowerCase())
                || cameraTypeCode.toLowerCase().includes(equipmentTypeCode.toLowerCase())
                || equipmentTypeCode.toLowerCase() == 'cctv';
        },
        /**
         * Lấy danh sách loại thiết bị (equipmentType) thuộc module có KeyName = 'CCTV'.
         * Cache một lần trong store để dùng chung ở nhiều component.
         *
         * @param moduleKeyName KeyName của module cần lấy (mặc định 'CCTV').
         */
        async getCctvEquipmentTypeList(moduleKeyName: string = 'CCTV') {
            if ((this.cctvEquipmentTypeList?.length ?? 0) > 0)
                return this.cctvEquipmentTypeList;

            try {
                // TmsModules sử dụng KeyName (không có field Code). Tham số thứ hai
                // của API là keyName, tương tự pattern đang dùng cho module VMS.
                const moduleResponse = await getAPI(TmsModulesApi).apiTmsTmsmodulesListGet('', moduleKeyName);
                const cctvModuleId = moduleResponse.data.result?.find(
                    (item: any) => item.keyName?.trim().toLowerCase() == moduleKeyName.trim().toLowerCase()
                )?.id;

                if (!cctvModuleId) {
                    this.cctvEquipmentTypeList = [];
                    return this.cctvEquipmentTypeList;
                }

                const equipmentTypeResponse = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet('', '', '', cctvModuleId);
                this.cctvEquipmentTypeList = equipmentTypeResponse.data.result ?? [];
            }
            catch (error) {
                console.error('commonVarsAndFuncs.ts: getCctvEquipmentTypeList(...)', error);
                this.cctvEquipmentTypeList = [];
            }

            return this.cctvEquipmentTypeList;
        },
        /**
         * Kiểm tra thiết bị (theo equipmentTypeId) có phải camera (thuộc module CCTV) hay không.
         * PHẢI gọi getCctvEquipmentTypeList() (await) trước đó ít nhất 1 lần để đảm bảo cache đã load.
         */
        isCCTVEquipmentType(equipmentTypeId: string | null | undefined) {
            if (!equipmentTypeId)
                return false;

            return this.cctvEquipmentTypeList?.some((item: any) => item.id == equipmentTypeId) ?? false;
        },
        // Utilities Funcs
        getStatusName(status: number) {
            switch (parseInt(status)) {
                case 0:
                    return 'lz.param.status_type.0';
                case 1:
                    return 'lz.param.status_type.1';
            }

            return 'Underfined';
        },
        getWorkShiftName(workShift: string) {
            switch (workShift) {
                case TmsWorkShiftEnum.WorkShift1:
                    return 'lz.label.tmsTrafficInfoReport.workShift1';

                case TmsWorkShiftEnum.WorkShift2:
                    return 'lz.label.tmsTrafficInfoReport.workShift2';

                case TmsWorkShiftEnum.WorkShift3:
                    return 'lz.label.tmsTrafficInfoReport.workShift3';

                case TmsWorkShiftEnum.WorkTime1:
                    return 'lz.label.tmsTrafficInfoReport.workTime1';

                case TmsWorkShiftEnum.WorkTime2:
                    return 'lz.label.tmsTrafficInfoReport.workTime2';

                case TmsWorkShiftEnum.WorkTime3:
                    return 'lz.label.tmsTrafficInfoReport.workTime3';
            }

            return 'Underfined';
        },
        getIncidentStateName(state: string) {
            if (state == TmsIncidentStateEnum.Registration)
                return 'lz.label.tmsIncident.registrationState';

            if (state == TmsIncidentStateEnum.Verified)
                return 'lz.label.tmsIncident.verifiedState';

            if (state == TmsIncidentStateEnum.InProgress)
                return 'lz.label.tmsIncident.inProgressState';

            if (state == TmsIncidentStateEnum.Finished)
                return 'lz.label.tmsIncident.finishedState';

            if (state == TmsIncidentStateEnum.Canceled)
                return 'lz.label.tmsIncident.canceledState';

            if (state == TmsIncidentStateEnum.Skipped)
                return 'lz.label.tmsIncident.skippedState';

            return null;
        },
        getWorkExecuteStateName(state: string) {
            if (state == TmsWorkExecuteStateEnum.Executed)
                return 'lz.label.tmsIncident.executedState';

            if (state == TmsWorkExecuteStateEnum.Skipped)
                return 'lz.label.tmsIncident.skippedState';

            if (state == TmsWorkExecuteStateEnum.Waiting)
                return 'lz.label.tmsIncident.waitingState';

            return null;
        },
        getDataType(variable) {
            if (variable === null) {
                return 'null';
            } else if (Array.isArray(variable)) {
                return 'array';
            } else if (variable instanceof Date) {
                return 'date';
            } else {
                return typeof variable;
            }
        },
        getFullUrl(url) {
            if ((url?.length ?? 0) == 0)
                return '';

            // Ảnh vừa upload ở client là data URI (base64) hoặc blob URL -> dùng nguyên văn.
            // KHÔNG ghép serverHost và KHÔNG lowercase (base64 phân biệt hoa/thường, ghép host sẽ hỏng URL).
            const raw = url.trim();
            if (/^(data:|blob:)/i.test(raw))
                return raw;

            let newUrl = raw.toLowerCase();
            if ((this.serverHost?.length ?? 0) == 0)
                return newUrl;

            newUrl = newUrl.replace(this.serverHost.trim().toLowerCase(), "");

            return this.serverHost.trim().toLowerCase() + '/' + newUrl;
        },
        getRandomNumber(min: number, max: number) {
            min = Math.ceil(min);
            max = Math.floor(max);

            return Math.floor(Math.random() * (max - min + 1)) + min;
        },
        getDateFromDays(days: number) {
            const currentDate = new Date();
            return currentDate.setDate(currentDate.getDate() + days);
        },
        getISODatetimeFromDays(days: number, startOrEnd: string = '') {
            const currentDate = new Date();
            currentDate.setDate(currentDate.getDate() + days);

            return this.getISODatetimeString(currentDate, startOrEnd);
        },
        getISODatetimeString(date: any, startOrEnd: string = '') {
            if (!date)
                return null;

            let dateTime = null;

            if (this.getDataType(date) == 'number')
                dateTime = new Date(date);
            if (this.getDataType(date) == 'string') {
                const timestamp = Date.parse(date);
                if (timestamp > 0)
                    dateTime = new Date(timestamp);
            }
            else if (this.getDataType(date) == 'date' || this.getDataType(date) == 'datetime')
                dateTime = date;

            if (this.getDataType(dateTime) == 'date' || this.getDataType(dateTime) == 'datetime') {

                const year = dateTime.getFullYear();
                const month = String(dateTime.getMonth() + 1).padStart(2, '0');
                const day = String(dateTime.getDate()).padStart(2, '0');
                const hours = String(dateTime.getHours()).padStart(2, '0');
                const minutes = String(dateTime.getMinutes()).padStart(2, '0');
                const seconds = String(dateTime.getSeconds()).padStart(2, '0');

                return startOrEnd == 'start'
                    ? `${year}-${month}-${day}T00:00:00`
                    : (startOrEnd == 'end' ? `${year}-${month}-${day}T23:59:59` : `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`)
            }

            return null;
        },
        formatPosition(kmNumber: number, metNumber: number, prefix: string = '') {
            return (prefix ? prefix + ': ' : '') + `Km${kmNumber ?? 0} +${metNumber ?? 0}`;
        },
        formatZonePosition(fromKmNumber: number, fromMetNumber: number, toKmNumber: number, toMetNumber: number, prefix: string = '') {
            return (prefix ? prefix + ': ' : '') + `Km${fromKmNumber ?? 0} +${fromMetNumber ?? 0} - Km${toKmNumber ?? 0} +${toMetNumber ?? 0}`;
        },
        formatCameraName(cameraName: string, kmNumber: number, metNumber: number, prefix: string = '') {
            return (prefix ? prefix + ': ' : '') + `${cameraName} (Km${kmNumber ?? 0} +${metNumber ?? 0})`;
        },
        formatEquipName(equipName: string, kmNumber: number, metNumber: number, prefix: string = '') {
            return (prefix ? prefix + ': ' : '') + `${equipName} (Km${kmNumber ?? 0} +${metNumber ?? 0})`;
        }
    }
});
