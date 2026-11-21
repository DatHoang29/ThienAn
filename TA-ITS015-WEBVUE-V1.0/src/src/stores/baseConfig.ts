// Author: SonTH
import { defineStore } from 'pinia';
import { getAPI } from '/@/utils/axios-utils';
import { SysConfigTypeApi } from '/@/api-services/cfgSystem';
import { i18n } from '/@/i18n';
import { getTerminologyList } from '/@/utils/locale';
import { useThemeConfig } from '/@/stores/themeConfig';
import { Local } from '/@/utils/storage';
import { SysOpConfigApi } from '/@/api-services/cfgSystem';
const { t } = i18n.global;


export const useBaseConfig = defineStore('baseConfig', {
    state: () => ({
        configList: [] as any,
        opConfigList: [] as any
    }),

    actions: {
        /**
         * Thiết lập danh sách dữ liệu cấu hình
         */
        async setConfigDataList() {

            var language = useThemeConfig().themeConfig.globalI18n;
            if (Local.get('themeConfig'))
                language = Local.get('themeConfig').globalI18n;

            let data = await getTerminologyList(language);
            i18n.global.setLocaleMessage(language, data[language])

            var configList = await getAPI(SysConfigTypeApi)
                .apiCfgsystemSysconfigtypeAlldatalistGet()

                .then((res) => res.data.result ?? {});

            var configListTemp = JSON.parse(JSON.stringify(configList));
            await Promise.all(
                Object.keys(configList).map(async (key) => {
                    configList[key].forEach((da: any, index: any) => {
                        setConfigDataLanguage(configListTemp[key][index]);
                    });

                })
            );
            this.configList = configListTemp;


        },
        /**
         * Thiết lập danh sách dữ liệu cấu hình hệ thống
         */
        async setOpConfigList() {

            var configList = await getAPI(SysOpConfigApi)
                .apiCfgsystemSysopconfigAlldatalistGet()

                .then((res) => res.data.result ?? {});

            var configListTemp = JSON.parse(JSON.stringify(configList));
            // await Promise.all(
            //     Object.keys(configList).map(async (key) => {
            //         configList[key].forEach((da: any, index: any) => {
            //             setConfigDataLanguage(configListTemp[key][index]);
            //         });

            //     })
            // );
            this.opConfigList = configListTemp;
        },
        /**
         * Kiểm tra danh sách dữ liệu cấu hình
         */
        checkConfigDataList() {
            if (this.configList == null || this.configList.length == 0) {
                this.setConfigDataList();
            }
        },
        /**
         * Kiểm tra danh sách dữ liệu cấu hình hệ thống
         */
        checkOperationConfigList() {
            if (this.opConfigList == null || this.opConfigList.length == 0) {
                this.setOpConfigList();
            }
        },
        /**
         * Lấy danh sách dữ liệu cấu hình theo loại cấu hình
         * @param configTypeCode Loại cấu hình
         * @returns Danh sách dữ liệu cấu hình
         */
        getConfigDataByCode(configTypeCode: string) {
            this.checkConfigDataList();
            return this.configList[configTypeCode] || [];

        },
        /**
         * Lấy dữ liệu cấu hình theo mã & loại cấu hình
         * @param configTypeCode Loại cấu hình
         * @param code Mã cấu hình  
         * @returns Dữ liệu cấu hình
         */
        getConfigItemByCode(configTypeCode: string, code: string) {
            this.checkConfigDataList();

            if (code != undefined && code !== '') {
                const _code = code.toString();
                const ds = this.getConfigDataByCode(configTypeCode);
                for (const element of ds) {
                    if (element.code == _code) {
                        return element;
                    }
                }
            }
            return {};
        },

        /**
         * Lấy tên đã dịch của dữ liệu cấu hình theo mã & loại cấu hình
         * Ưu tiên bản dịch theo key `lz.param.{configTypeCode}.{code}`, nếu không có bản dịch thì fallback về name gốc của cấu hình (hoặc code nếu không tìm thấy cấu hình)
         * @param configTypeCode Loại cấu hình
         * @param code Mã cấu hình
         * @returns Tên đã dịch
         */
        getConfigItemName(configTypeCode: string, code: string) {
            const item = this.getConfigItemByCode(configTypeCode, code);
            return t(`lz.param.${configTypeCode}.${code}`, item?.name || code || '');
        },

        /**
         * Lấy mã cấu hình theo giá trị & loại cấu hình
         * @param configTypeCode Loại cấu hình
         * @param val Giá trị cấu hình
         * @returns Dữ liệu cấu hình
         */
        getConfigLabelByValue(configTypeCode: string, val: string) {
            this.checkConfigDataList();

            if (val != undefined && val !== '') {
                const _val = val.toString();
                const ds = this.getConfigDataByCode(configTypeCode);
                for (const element of ds) {
                    if (element.value == _val) {
                        return element;
                    }
                }
            }
            return {};
        },


        /**
        * Lấy danh sách dữ liệu cấu hình hệ thống
        * @returns Danh sách dữ liệu cấu hình
        */
        getOpConfigList() {
            this.checkOperationConfigList();
            return this.opConfigList || [];

        },

        /**
        * Lấy danh sách dữ liệu cấu hình hệ thống theo loại cấu hình
        * @param groupCode Loại cấu hình
        * @returns Danh sách dữ liệu cấu hình
        */
        getOpConfigListByGroupCode(groupCode: string) {
            this.checkOperationConfigList();
            return this.opConfigList[groupCode] || [];

        },
        /**
         * Lấy dữ liệu cấu hình hệ thống theo mã & loại cấu hình
         * @param groupCode Loại cấu hình
         * @param code Mã cấu hình  
         * @returns Dữ liệu cấu hình
         */
        getOpConfigItemByCode(groupCode: string, code: string) {
            this.checkOperationConfigList();

            if (code != undefined && code !== '') {
                const _code = code.toString();
                const ds = this.getOpConfigListByGroupCode(groupCode);
                for (const element of ds) {
                    if (element.code == _code) {
                        return element;
                    }
                }
            }
            return {};
        },

        /**
         * Lấy mã cấu hình hệ thống theo giá trị & loại cấu hình
         * @param groupCode Loại cấu hình
         * @param val Giá trị cấu hình
         * @returns Dữ liệu cấu hình
         */
        getOpConfigLabelByValue(groupCode: string, val: string) {
            this.checkOperationConfigList();

            if (val != undefined && val !== '') {
                const _val = val.toString();
                const ds = this.getOpConfigListByGroupCode(groupCode);
                for (const element of ds) {
                    if (element.value == _val) {
                        return element;
                    }
                }
            }
            return {};
        },

    }
});

// Dịch thuật dữ liệu cấu hình. Nếu không có dịch thuật, mặc định là tên của dữ liệu
const setConfigDataLanguage = async (data: any) => {
    data.langMessage = `lz.param.${data.typeCode}.${data.code}`;
    data.name = t(data.langMessage, data.name);
};
