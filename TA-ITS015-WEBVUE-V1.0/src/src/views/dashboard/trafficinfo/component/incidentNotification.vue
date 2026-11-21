<template>
    <div v-if="state.lastestIncident" class="display-flex--horizontal flex-gap--5px" style="color: red;">
        <img style="max-height: 25px;" :src="commonVarsAndFuncs.getFullUrl(UploadAssetItemsEnum.WarningIcon)">
        <p class="text-uppercase"><strong>{{ state.lastestIncident?.eventTypeName ?? (getEventType(state.lastestIncident?.eventTypeId)?.name ?? state.lastestIncident?.name) }}</strong></p>
        <p><strong>({{ (state.lastestIncident?.kmNumber ?? 0) + 'Km +' + (state.lastestIncident?.metNumber ?? 0) }})</strong></p>
    </div>
</template>

<script lang="ts" setup>
    import { onMounted, reactive } from 'vue';
    import { getAPI } from '/@/utils/axios-utils';
    import { TmsEventTypeApi, TmsIncidentApi } from '/@/api-services/tms';
    import { useI18n } from 'vue-i18n';

    import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
    const commonVarsAndFuncs = useCommonVarsAndFuncs();

    import { UploadAssetItemsEnum } from '/@/types/enums/baseEnum';
    import { useTransporterIncident } from '/@/transporter/hooks/useTransporterIncident';

    const { t: $t } = useI18n();

    const state = reactive({
        lastestIncident: null,
        eventTypeList: []
    });

    const getLatestIncident = async() =>{
        let response = await getAPI(TmsIncidentApi).apiTmsTmsincidentLatestincidentPost();
        state.lastestIncident = response.data.result;
    }

    const getEventType = (evenTypeId: string | null) => {
        if(!evenTypeId)
            return null;

        return state.eventTypeList?.find(item => item.id == evenTypeId);
    };

    // Lắng nghe sự cố realtime qua mittBus (NATS) => thay cho watch store cũ
    useTransporterIncident(() => {
        getLatestIncident(); // No need to use the keywork "await" here
    });

    onMounted(async() => {
        let response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
        state.eventTypeList = response.data.result;

        getLatestIncident(); // No need to use the keywork "await" here
    });
</script>