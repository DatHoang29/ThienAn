<template>
    <el-dropdown split-button type="danger" :size="props.dropdownSize" @click="showEditIncidentComp('')">
        <p class="display-flex--horizontal">
            <el-icon><Plus /></el-icon>
            <span>{{ $t('Add Incident') }}</span>
        </p>
        <template #dropdown>
            <el-dropdown-menu>
            <el-dropdown-item v-for="item in state.eventTypeList" @click="showEditIncidentComp(item.id)" :icon="Plus">{{ getButtonTitle(item.code) }}</el-dropdown-item>
            </el-dropdown-menu>
        </template>
    </el-dropdown>
    <editIncidentComp ref="editIncidentCompRef" :title="$t('Add')" :operateType="'add'" @onSubmitSuccess="handleOnSubmitSuccess" />
</template>

<script lang="ts" setup>
    import { defineAsyncComponent, reactive, ref } from 'vue';
    import { getAPI } from '/@/utils/axios-utils';
    import { TmsEventTypeApi } from '/@/api-services/tms';
    import { useI18n } from 'vue-i18n';
    import { TmsEventType, StatusTypeEnum } from '/@/types/enums';
    import { Plus } from '@element-plus/icons-vue';
    const { t: $t } = useI18n();

    const editIncidentComp = defineAsyncComponent(() => import('/@/views/tms/incident/component/editIncident.vue'));
    const editIncidentCompRef = ref<InstanceType<typeof editIncidentComp>>();

    const props = defineProps({
        dropdownSize: {
            type: String,
            default: 'medium'
        },
        zoneId: String
    });

    const state = reactive({
        eventTypeList: []
    });

    const getButtonTitle = function(eventType: string, action: string = 'add'){
        const eventTypeName = getEventTypeName(eventType);
        const actionName = action == 'add' ? $t('fe.base.text.add') : (action == 'edit' ? $t('fe.base.text.edit') : $t('fe.base.text.detail'));

        return actionName + ' ' + eventTypeName;
    };

    const showEditIncidentComp = (eventTypeId: string) => {
        editIncidentCompRef.value?.openDialog({eventTypeId: eventTypeId, zoneId: props.zoneId}, 'add');
    }

    function getEventTypeName(eventType: string) {
        switch (eventType?.trim().toLocaleLowerCase()) {
            case TmsEventType.Accident:
                return $t(TmsEventType.Accident);

            case TmsEventType.Fire:
                return $t(TmsEventType.Fire);

            case TmsEventType.TrafficJam:
                return $t(TmsEventType.TrafficJam);

            default:
                return $t('Incident');
        }
    }

    const loadDefaultData = async() => {
        let response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet(undefined, undefined, undefined, undefined, StatusTypeEnum.Enable as any);
        state.eventTypeList = (response.data.result || []).filter((item: any) => item.code !== 'unknown');
    };

    // Handle on submit success
    const handleOnSubmitSuccess = () => {

    }

    loadDefaultData();
</script>

<style scoped>
.display-flex--horizontal{
    display: flex;
    align-items: center;
}
</style>