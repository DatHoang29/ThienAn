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
        <!-- Một panel theo mã modal của loại thiết bị (bảng MODAL_PANELS) -->
        <div v-if="activePanel" style="height: 100%;">
            <component :is="activePanel.component" ref="panelRef" @closed="onChildClosed" @close-all="onChildCloseAll" @close-others="onChildCloseOthers" @control-tab-change="onChildControlTabChange" />
        </div>
    </DraggableModal>
</template>

<script setup lang="ts" name="equipmentModal">
import { reactive, ref, nextTick, computed } from 'vue';
import type { Component } from 'vue';

import DraggableModal from '/@/components/bigScreen/floatingModal/DraggableModal.vue';

// Import device info component
import VmsModal from './device/vmsInfo.vue';
import CctvModal from './device/cctvInfo.vue';
import LcsModal from './device/lcsInfo.vue';
import DeviceModal from './device/index.vue';
import VdsModal from './device/vdsInfo.vue';
import BarrierModal from './device/barrierInfo.vue';
import FanModal from './device/fanInfo.vue';
import TrafficLightModal from './device/trafficLightInfo.vue';

import { useMapStore } from '/@/stores/its/mapStore';
import { resolveBarrierSystem } from '/@/stores/its/barrierControl';
import type { BarrierSystem } from '/@/stores/its/barrierControl';
import { resolveEquipmentModalCode } from '/@/stores/its/deviceControlUtils';
import { TmsEquipmentModalCode } from '/@/types/enums';

/** Dữ liệu truyền cho panel con khi mở. */
interface ModalOpenContext {
    /** Dữ liệu gốc của lần bấm trên bản đồ (itemId, itemData...). */
    data: any;
    equipment: any;
    barrierSystem: BarrierSystem | null;
}

interface ModalPanel {
    component: Component;
    /** Kích thước modal theo LOẠI nội dung hiển thị (VMS cần cao hơn để đủ khung nội dung, CCTV cần rộng cho video, ...). */
    size: { initW: number; initH: number; minW: number; minH: number };
    /** Chiều cao (px) nới tới khi panel vào tab "Điều khiển" (panel phát 'controlTabChange'); bỏ trống = giữ nguyên. */
    controlH?: number;
    /** Nạp dữ liệu vào panel; bỏ trống = `openInfo(equipment)`. */
    open?: (panel: any, context: ModalOpenContext) => void;
}

// Bảng đăng ký modal theo MÃ MODAL của loại thiết bị (TmsEquipmentType.modalCode, danh mục cấu hình
// tms_equipment_modal_type). Loại thiết bị mới dùng lại modal có sẵn ⇒ chỉ cần gán mã ở trang Loại thiết bị.
// Kiểu modal mới ⇒ thêm mã vào danh mục + TmsEquipmentModalCode + một dòng ở đây.
const MODAL_PANELS: Record<string, ModalPanel> = {
    [TmsEquipmentModalCode.Device]: { component: DeviceModal, size: { initW: 580, initH: 430, minW: 420, minH: 320 } },
    [TmsEquipmentModalCode.Vms]: {
        component: VmsModal,
        size: { initW: 850, initH: 650, minW: 720, minH: 480 },
        controlH: 900,
        open: (panel, { data }) => panel.openDialog(data),
    },
    // CCTV/PTZ/FIX: khung rộng hơn để mở panel PTZ bên phải + thanh playback bên dưới mà không bóp video.
    // Khung view tự chọn phát qua CCTV (PTZ + playback) hoặc rơi về RTSP tuỳ theo CCTV có hỗ trợ IP hay không.
    [TmsEquipmentModalCode.Cctv]: { component: CctvModal, size: { initW: 900, initH: 560, minW: 640, minH: 420 } },
    [TmsEquipmentModalCode.Lcs]: { component: LcsModal, size: { initW: 680, initH: 650, minW: 500, minH: 400 }, controlH: 860 },
    [TmsEquipmentModalCode.Vds]: { component: VdsModal, size: { initW: 580, initH: 420, minW: 360, minH: 320 } },
    // Hệ Barrier: bấm barrier hoặc đèn tín hiệu barrier đều mở form điều khiển chung của hệ.
    [TmsEquipmentModalCode.Barrier]: {
        component: BarrierModal,
        size: { initW: 640, initH: 440, minW: 460, minH: 360 },
        open: (panel, { equipment, barrierSystem }) => panel.openInfo(equipment, barrierSystem),
    },
    // Đèn tín hiệu giao thông SS: mỗi đèn điều khiển riêng (đỏ / xanh), KHÔNG ghép với barrier cùng tầng block.
    [TmsEquipmentModalCode.TrafficLight]: { component: TrafficLightModal, size: { initW: 720, initH: 680, minW: 520, minH: 420 } },
    // Quạt phản lực / Jet fan: form điều khiển chạy thuận, nghịch, dừng, tần số
    [TmsEquipmentModalCode.Fan]: { component: FanModal, size: { initW: 680, initH: 460, minW: 480, minH: 380 } },
};
const DEFAULT_MODAL_SIZE = { initW: 675, initH: 450, minW: 450, minH: 300 };

const frameRef = ref<InstanceType<typeof DraggableModal>>();
const panelRef = ref();

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);
const mapStore = useMapStore();

const modalId = ref('');

const state = reactive({
    data: null as any,
    /** Khoá trong MODAL_PANELS của panel đang hiển thị. */
    type: null as string | null,
    equipment: null as any,
    /** Hệ barrier (barrier + đèn tín hiệu) của thiết bị được bấm — chỉ có khi type = 'barrier'. */
    barrierSystem: null as BarrierSystem | null,
});

const activePanel = computed(() => (state.type ? MODAL_PANELS[state.type] : null) || null);
const modalSize = computed(() => activePanel.value?.size || DEFAULT_MODAL_SIZE);

// Chuyển tiếp sự kiện đóng từ panel con -> khung DraggableModal
const onChildClosed = () => frameRef.value?.handleClose();
const onChildCloseOthers = () => frameRef.value?.handleCloseOthers();
const onChildCloseAll = () => frameRef.value?.handleCloseAll();

// Tab "Điều khiển" (VMS/LCS) cần thấy danh sách thiết bị bên dưới preview -> nới khung; rời tab thì trả lại.
const onChildControlTabChange = (isControl: boolean) => {
    const controlH = activePanel.value?.controlH;
    if (!controlH) return;
    if (isControl) frameRef.value?.expandHeight(controlH);
    else frameRef.value?.restoreHeight();
};

// Mở modal: XÁC ĐỊNH LOẠI TRƯỚC (để có kích thước đúng ngay khi mở, tránh mở rồi mới
// resize gây giật), sau đó mở khung rồi nạp panel con tương ứng.
const openDialog = async (data: any) => {
    state.data = data;
    resolveEquipmentType(data);       // set state.type -> modalSize cập nhật
    frameRef.value?.open();           // mở khung với kích thước đúng theo loại
    await nextTick();
    openChildPanel(data);             // nạp panel con (đã render trong slot)
};

// Chỉ xác định loại thiết bị + kích thước (chưa mở panel con). Panel con nằm trong slot
// của DraggableModal (v-if isShowDialog) nên chỉ mở được SAU khi frame.open().
const resolveEquipmentType = (data: any) => {
    // Bản đồ đã tải hai danh mục này trước khi cho chọn thiết bị. Không chờ API lại
    // mỗi lần mở popup, đặc biệt khi các kết nối SSE đang chiếm socket HTTP/1.1.
    const equipment = mapStore.equipmentList.find((item: any) => item.id == data.itemId) ?? data.itemData;
    state.equipment = equipment;
    const equipmentType = mapStore.equipmentTypeList.find((item: any) => item.id == equipment?.equipmentTypeId);
    let modalCode = resolveEquipmentModalCode(equipmentType);

    // Hệ Barrier: barrier và đèn tín hiệu dùng chung một form điều khiển.
    // barrierPeers = thiết bị cùng tầng block (BlockMapLayer gửi kèm) để ghép cặp barrier ↔ đèn.
    // Đèn tín hiệu không ghép được với barrier nào ⇒ không phải hệ barrier, mở modal thiết bị thường.
    state.barrierSystem = null;
    if (modalCode === TmsEquipmentModalCode.Barrier || modalCode === TmsEquipmentModalCode.BarrierLight) {
        state.barrierSystem = resolveBarrierSystem(String(equipment?.id ?? data?.itemId ?? ''), data?.itemData?.barrierPeers);
        modalCode = state.barrierSystem ? TmsEquipmentModalCode.Barrier : TmsEquipmentModalCode.Device;
    }

    // Mã khai trong cấu hình nhưng FE chưa có component ⇒ modal thiết bị mặc định.
    state.type = MODAL_PANELS[modalCode] ? modalCode : TmsEquipmentModalCode.Device;
};

// Nạp dữ liệu vào panel con tương ứng (gọi sau khi frame đã mở + slot đã render).
const openChildPanel = (data: any) => {
    const panel = panelRef.value;
    if (!panel || !activePanel.value) return;
    const context: ModalOpenContext = { data, equipment: state.equipment, barrierSystem: state.barrierSystem };
    if (activePanel.value.open) activePanel.value.open(panel, context);
    else panel.openInfo(context.equipment);
};

const setActive = (isActive: boolean) => frameRef.value?.setActive(isActive);

const setModalId = (id: string) => {
    modalId.value = id;
    frameRef.value?.setModalId(id);
};

const cancel = () => frameRef.value?.cancel();

defineExpose({ openDialog, setActive, setModalId, cancel, itemId: modalId });
</script>
