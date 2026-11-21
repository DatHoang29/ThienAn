<template>
    <DraggableModal
        ref="frameRef"
        :initW="modalSize.initW"
        :initH="modalSize.initH"
        :minW="modalSize.minW"
        :minH="modalSize.minH"
        @activated="emits('activated', $event)"
        @closed="emits('closed', $event)"
        @close-others="emits('closeOthers', $event)"
        @close-all="emits('closeAll')"
    >
        <div v-if="state.type === 'wos'" style="height: 100%;">
            <WosModal ref="wosModalRef" @closed="onChildClosed" @close-all="onChildCloseAll" @close-others="onChildCloseOthers" />
        </div>
        <div v-if="state.type === 'toll'" style="height: 100%;">
            <TollModal ref="tollModalRef" @closed="onChildClosed" @close-all="onChildCloseAll" @close-others="onChildCloseOthers" />
        </div>
        <div v-if="state.type === 'unit'" style="height: 100%;">
            <UnitModal ref="unitModalRef" @closed="onChildClosed" @close-all="onChildCloseAll" @close-others="onChildCloseOthers" />
        </div>
    </DraggableModal>
</template>

<script setup lang="ts" name="unitModal">
import { reactive, ref, nextTick, computed } from 'vue';

import DraggableModal from '/@/components/bigScreen/floatingModal/DraggableModal.vue';

// Import unit info component
import WosModal from './unit/wosInfo.vue';
import TollModal from './unit/tollInfo.vue';
import UnitModal from './unit/index.vue';

import { TmsWorkUnitApi } from '/@/api-services/tms';
import { getAPI } from '/@/utils/axios-utils';

const frameRef = ref<InstanceType<typeof DraggableModal>>();
const wosModalRef = ref<InstanceType<typeof WosModal>>();
const tollModalRef = ref<InstanceType<typeof TollModal>>();
const unitModalRef = ref<InstanceType<typeof UnitModal>>();

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const modalId = ref('');

const state = reactive({
    data: null as any,
    type: null as 'toll' | 'wos' | 'unit' | null,
    unitListData: [] as any,
    unit: null as any,
});

// Kích thước modal theo LOẠI đơn vị (tuỳ nội dung). Tinh chỉnh riêng từng loại tại đây.
const MODAL_SIZE_BY_TYPE: Record<string, { initW: number; initH: number; minW: number; minH: number }> = {
    wos: { initW: 580, initH: 480, minW: 480, minH: 360 },
    toll: { initW: 580, initH: 480, minW: 480, minH: 360 },
    unit: { initW: 540, initH: 420, minW: 420, minH: 320 },
};
const DEFAULT_MODAL_SIZE = { initW: 675, initH: 450, minW: 450, minH: 300 };
const modalSize = computed(() => (state.type ? MODAL_SIZE_BY_TYPE[state.type] : null) || DEFAULT_MODAL_SIZE);

// Chuyển tiếp sự kiện đóng từ panel con -> khung DraggableModal
const onChildClosed = () => frameRef.value?.handleClose();
const onChildCloseOthers = () => frameRef.value?.handleCloseOthers();
const onChildCloseAll = () => frameRef.value?.handleCloseAll();

// Mở modal: xác định loại TRƯỚC (để có kích thước đúng ngay khi mở), rồi mở khung + panel con.
const openDialog = async (data: any) => {
    state.data = data;
    await resolveUnitType(data);   // set state.type -> modalSize cập nhật
    frameRef.value?.open();        // mở khung với kích thước đúng theo loại
    await nextTick();
    openChildPanel();              // nạp panel con (đã render trong slot)
};

// Chỉ xác định loại đơn vị + kích thước (chưa mở panel con vì slot chưa render trước open).
const resolveUnitType = async (data: any) => {
    const response = await getAPI(TmsWorkUnitApi).apiTmsTmsworkunitListGet();
    state.unitListData = response?.data?.result ?? null;
    state.unit = state.unitListData?.find((item: any) => item.id == data.itemId) ?? null;

    //TODO enum
    switch (state.unit?.type) {
        case 'toll':
            state.type = 'toll';
            break;
        case 'wos':
            state.type = 'wos';
            break;
        default:
            state.type = 'unit';
            break;
    }
};

// Nạp dữ liệu vào panel con tương ứng (gọi sau khi frame đã mở + slot đã render).
const openChildPanel = () => {
    switch (state.type) {
        case 'toll':
            tollModalRef.value?.openInfo(state.unit);
            break;
        case 'wos':
            wosModalRef.value?.openInfo(state.unit);
            break;
        default:
            unitModalRef.value?.openInfo(state.unit);
            break;
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
