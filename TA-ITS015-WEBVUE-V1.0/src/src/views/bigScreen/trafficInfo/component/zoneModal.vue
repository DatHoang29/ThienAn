<template>
    <DraggableModal
        ref="frameRef"
        :initW="640"
        :initH="520"
        :minW="500"
        :minH="380"
        @activated="emits('activated', $event)"
        @closed="emits('closed', $event)"
        @close-others="emits('closeOthers', $event)"
        @close-all="emits('closeAll')"
    >
        <InfoPanel
            v-model:active-tab="state.activeTab"
            :tabs="tabs"
            @tab-change="handleTabChange"
            @closed="onChildClosed"
            @close-others="onChildCloseOthers"
            @close-all="onChildCloseAll"
        >
            <!-- Zone info -->
            <template #zoneInfoTab>
                <div class="content-text">
                    <zoneInfoComp ref="zoneInfoCompRef" :zoneInfo="state.zoneInfo" :incidentList="state.incidentList" :equipmentList="state.equipmentList" />
                </div>
            </template>

            <!-- Volume -->
            <template #volumeInfoTab>
                <volumeInfoComp :zoneInfo="state.zoneInfo" :requestTime="state.volumeInfoRequestTime" />
            </template>

            <!-- Speed -->
            <template #speedInfoTab>
                <speedInfoComp :zoneInfo="state.zoneInfo" :requestTime="state.speedInfoRequestTime" />
            </template>
        </InfoPanel>
    </DraggableModal>
</template>

<script setup lang="ts" name="zoneModal">
import { reactive, ref, computed } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { useI18n } from 'vue-i18n';
import { TmsEquipmentApi, TmsIncidentApi, TmsZoneApi } from '/@/api-services/tms';

import DraggableModal from '/@/components/bigScreen/floatingModal/DraggableModal.vue';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';

import zoneInfoComp from './zone/info.vue';
import volumeInfoComp from './zone/volume.vue';
import speedInfoComp from './zone/speed.vue';

const { t: $t } = useI18n();

const frameRef = ref<InstanceType<typeof DraggableModal>>();
const zoneInfoCompRef = ref<InstanceType<typeof zoneInfoComp>>();

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const modalId = ref('');

const state = reactive({
    activeTab: 'zoneInfoTab',
    zoneInfoRequestTime: null as any,
    volumeInfoRequestTime: null as any,
    speedInfoRequestTime: null as any,
    zoneInfo: null as any,
    incidentList: [] as any,
    equipmentList: [] as any,
});

const tabs = computed(() => [
    { label: $t('lz.label.zoneInfo.infoTabTitle'), name: 'zoneInfoTab' },
    { label: $t('lz.label.zoneInfo.volumeTabTitle'), name: 'volumeInfoTab' },
    { label: $t('lz.label.zoneInfo.speedTabTitle'), name: 'speedInfoTab' },
]);

// Chuyển tiếp sự kiện đóng từ panel -> khung DraggableModal
const onChildClosed = () => frameRef.value?.handleClose();
const onChildCloseOthers = () => frameRef.value?.handleCloseOthers();
const onChildCloseAll = () => frameRef.value?.handleCloseAll();

// Mở modal: hiển thị khung + nạp dữ liệu khu vực
const openDialog = async (data: any) => {
    frameRef.value?.open();
    loadZoneInfo(data?.itemId);
};

const loadZoneInfo = async (zoneId: string | null) => {
    if (!zoneId) return;

    let response = await getAPI(TmsZoneApi).apiTmsTmszoneListzonebyidsGet(zoneId);
    state.zoneInfo = response.data.result?.length > 0 ? response.data.result[0] : null;

    response = await getAPI(TmsIncidentApi).apiTmsTmsincidentListPost({ zoneId: zoneId });
    state.incidentList = response.data.result;

    response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListbypositionPost({ zoneId: zoneId });
    state.equipmentList = response.data.result;
};

const handleTabChange = async (tabName: string) => {
    if (tabName == 'zoneInfoTab') {
        state.zoneInfoRequestTime = Date.now();
    } else if (tabName == 'volumeInfoTab') {
        state.volumeInfoRequestTime = Date.now();
    } else if (tabName == 'speedInfoTab') {
        state.speedInfoRequestTime = Date.now();
    }
};

const setActive = (isActive: boolean) => frameRef.value?.setActive(isActive);

const setModalId = (id: string) => {
    modalId.value = id;
    frameRef.value?.setModalId(id);
};

const cancel = () => frameRef.value?.cancel();

defineExpose({ openDialog, setActive, setModalId, cancel, itemId: modalId });
</script>
