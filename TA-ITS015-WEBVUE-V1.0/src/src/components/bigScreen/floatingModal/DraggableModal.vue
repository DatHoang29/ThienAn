<template>
    <Teleport to="body">
        <div v-if="state.isShowDialog" class="drag-modal-wrapper" :style="{ zIndex: state.zIndex }" @mousedown.stop>
            <vue-draggable-resizable
                :initW="initW"
                :initH="initH"
                v-model:h="state.height"
                :minW="minW"
                :minH="minH"
                v-model:x="state.position.x"
                v-model:y="state.position.y"
                :draggable="true"
                :resizable="true"
                :parent="true"
                :parentLimitation="true"
                :parentW="parentSize.width"
                :parentH="parentSize.height"
                :minTop="100"
                :class="['floating-modal', { 'modal-active': state.isActive }]"
                @activated="bringToFront"
                @dragstop="onDragStop"
                @resizestop="onResizeStop"
                @mousedown.stop="bringToFront"
            >
                <div class="modal-content-wrapper">
                    <!-- Nội dung modal: panel/tab do nơi dùng truyền vào -->
                    <slot />
                </div>
            </vue-draggable-resizable>
        </div>
    </Teleport>
</template>

<script setup lang="ts" name="draggableModal">
import { reactive, ref, onMounted, onUnmounted } from 'vue';
import VueDraggableResizableRaw from 'vue3-draggable-resizable'; //pnpm install vue3-draggable-resizable
import 'vue3-draggable-resizable/dist/Vue3DraggableResizable.css';

const VueDraggableResizable = (VueDraggableResizableRaw as any).default || VueDraggableResizableRaw;

const props = withDefaults(
    defineProps<{
        initW?: number;
        initH?: number;
        minW?: number;
        minH?: number;
        /** Selector vùng cha giới hạn kéo-thả */
        parentSelector?: string;
    }>(),
    {
        initW: 675,
        initH: 450,
        minW: 450,
        minH: 300,
        parentSelector: '.visualizing-traffic-info',
    }
);

const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

// Lớp nội dung của trafficInfo dùng dải 100–1200. Drag modal nằm trên các panel đó,
// nhưng phải luôn thấp hơn lớp phủ Element Plus (bắt đầu từ 2000) và banner incident (15000).
const BASE_Z_INDEX = 1500;
const MAX_Z_INDEX = 1900;
const modalId = ref('');

const state = reactive({
    isShowDialog: false,
    isActive: false,
    zIndex: BASE_Z_INDEX,
    position: { x: 100, y: 100 },
    /** Chiều cao hiện tại của khung (đồng bộ 2 chiều với vue-draggable-resizable qua v-model:h). */
    height: props.initH,
});

// Chiều cao trước khi panel con xin nới (expandHeight) + chiều cao đã nới, để restoreHeight trả lại đúng.
const heightBeforeExpand = ref<number | null>(null);
const expandedHeight = ref<number | null>(null);
const MIN_TOP = 100; // khớp :minTop / giới hạn y trong open()/onDragStop()
const BOTTOM_GAP = 10;

const parentSize = reactive({ width: 0, height: 0 });

const updateParentSize = () => {
    const container = document.querySelector(props.parentSelector);
    if (container) {
        parentSize.width = container.clientWidth;
        parentSize.height = container.clientHeight;
    }
};

onMounted(() => {
    updateParentSize();
    window.addEventListener('resize', updateParentSize);
});

onUnmounted(() => {
    window.removeEventListener('resize', updateParentSize);
});

// z-index management (đưa modal đang thao tác lên trên cùng)
const getHighestZIndex = () => {
    const elements = document.getElementsByClassName('drag-modal-wrapper');
    let highest = BASE_Z_INDEX;
    for (const element of elements) {
        const zIndex = parseInt(window.getComputedStyle(element).zIndex);
        if (zIndex > highest) highest = zIndex;
    }
    return highest;
};

const bringToFront = () => {
    const newZIndex = getHighestZIndex() + 1;
    // Không để drag modal tăng vào dải z-index của các dialog toàn hệ thống.
    state.zIndex = Math.min(Math.max(newZIndex, BASE_Z_INDEX), MAX_Z_INDEX);
    state.isActive = true;
    emits('activated', modalId.value);
};

const onDragStop = (x: number, y: number) => {
    state.position.x = Math.max(0, Math.min(x, parentSize.width - props.initW));
    state.position.y = Math.max(100, Math.min(y, parentSize.height - props.initH));
};

const onResizeStop = () => {
    // Hook cho resize (vd: resize chart) nếu cần
};

/**
 * Mở khung modal với vị trí XẾP BẬC THANG (cascade) — KHÔNG random để tránh modal
 * "nhảy lung tung" mỗi lần mở. Vị trí lệch dần theo SỐ modal đang mở: modal đầu ở góc
 * trên-trái vùng bản đồ, các modal sau lệch xuống-phải một bước cố định (wrap sau N cái).
 * Khi đóng hết, modal mới lại bắt đầu từ vị trí gốc → nhất quán, dễ nhìn.
 */
const open = () => {
    if (!state.isShowDialog) {
        // Đếm số modal đang mở TRƯỚC modal này (wrapper của modal này chưa render vào DOM
        // tại thời điểm này vì v-if cập nhật ở tick sau) → dùng làm chỉ số bậc thang.
        const openCount = document.getElementsByClassName('drag-modal-wrapper').length;

        state.isShowDialog = true;
        updateParentSize();
        // Khung mount lại (v-if) và tự phát update:h = initH của loại panel mới -> chỉ cần xoá trạng thái nới
        heightBeforeExpand.value = null;
        expandedHeight.value = null;

        const STEP = 34; // khoảng lệch mỗi bậc
        const MAX_CASCADE = 8; // wrap sau 8 modal để không trôi ra ngoài
        const i = openCount % MAX_CASCADE;
        const START_X = 72;
        const START_Y = 110;

        const maxX = Math.max(0, parentSize.width - props.initW);
        const maxY = Math.max(100, parentSize.height - props.initH);
        const x = Math.max(0, Math.min(START_X + i * STEP, maxX));
        const y = Math.max(100, Math.min(START_Y + i * STEP, maxY));
        state.position = { x, y };
    }
    bringToFront();
};

/**
 * Nới chiều cao khung tới `target` (px) trong giới hạn màn hình, dùng khi panel con cần nhiều chỗ hơn
 * (vd tab "Điều khiển" VMS/LCS cần thấy danh sách thiết bị). Không thu nhỏ nếu khung đang cao hơn
 * (người dùng đã tự kéo to). Khung bị tràn đáy thì đẩy lên, nhưng không cao hơn mép trên MIN_TOP.
 */
const expandHeight = (target: number) => {
    updateParentSize();
    const available = Math.max(props.minH, parentSize.height - MIN_TOP - BOTTOM_GAP);
    const nextHeight = Math.min(target, available);
    if (nextHeight <= state.height) return;

    if (heightBeforeExpand.value === null) heightBeforeExpand.value = state.height;
    expandedHeight.value = nextHeight;
    state.height = nextHeight;

    const overflow = state.position.y + nextHeight - (parentSize.height - BOTTOM_GAP);
    if (overflow > 0) state.position.y = Math.max(MIN_TOP, state.position.y - overflow);
};

/** Trả khung về chiều cao trước khi nới (bỏ qua nếu người dùng đã tự resize sau khi nới). */
const restoreHeight = () => {
    if (heightBeforeExpand.value !== null && state.height === expandedHeight.value) {
        state.height = heightBeforeExpand.value;
    }
    heightBeforeExpand.value = null;
    expandedHeight.value = null;
};

const setActive = (isActive: boolean) => {
    state.isActive = isActive;
    if (!isActive) state.zIndex = BASE_Z_INDEX;
};

const setModalId = (id: string) => {
    modalId.value = id;
};

const cancel = () => {
    state.isShowDialog = false;
    emits('closed', modalId.value);
};

// Các handler đóng dùng cho panel con bind vào
const handleClose = () => cancel();
const handleCloseOthers = () => emits('closeOthers', modalId.value);
const handleCloseAll = () => emits('closeAll');

defineExpose({ open, expandHeight, restoreHeight, setActive, setModalId, cancel, bringToFront, handleClose, handleCloseOthers, handleCloseAll, itemId: modalId });
</script>

<style lang="scss">
.drag-modal-wrapper {
    position: fixed;
    top: 0;
    left: 0;
    width: 100vw;
    height: 100vh;
    pointer-events: none;
}

.floating-modal {
    background: #ffffff;
    border: 1px solid color-mix(in srgb, var(--el-color-primary) 18%, transparent);
    border-radius: 14px;
    transition: border-color 0.25s ease, box-shadow 0.25s ease;
    pointer-events: auto;
    /* KHÔNG dùng overflow:hidden ở đây vì sẽ cắt mất các handle resize ở góc/cạnh
       (khiến khó/không thể resize). Việc bo góc + cắt nội dung chuyển xuống
       .modal-content-wrapper bên trong. */
    overflow: visible;
    box-shadow: 0 18px 50px -18px rgba(15, 30, 55, 0.45), 0 2px 8px rgba(15, 30, 55, 0.08);

    &.modal-active {
        border-color: color-mix(in srgb, var(--el-color-primary) 65%, transparent) !important;
        box-shadow: 0 0 0 1px color-mix(in srgb, var(--el-color-primary) 38%, transparent),
            0 0 26px -8px color-mix(in srgb, var(--el-color-primary) 45%, transparent),
            0 22px 58px -18px rgba(15, 30, 55, 0.5) !important;
    }
}

.modal-content-wrapper {
    height: 100%;
    width: 100%;
    /* Bo góc + cắt nội dung ở đây (thay cho overflow:hidden của .floating-modal) */
    border-radius: 14px;
    overflow: hidden;
}

:deep(.vdr-container) {
    height: 100%;
}

/* Vùng bắt (hit area) của handle to hơn để dễ resize, đặc biệt ở 4 góc */
:deep(.vdr-handle) {
    width: 14px;
    height: 14px;
    background-color: var(--el-color-primary);
    box-shadow: 0 0 6px 1px color-mix(in srgb, var(--el-color-primary) 60%, transparent);
    border-radius: 3px;
    z-index: 20;

    &:hover {
        background-color: var(--el-color-primary-light-2);
    }
}

/* Handle 4 góc: kéo rộng vùng bắt ra ngoài mép modal để bắt chuột dễ hơn */
:deep(.vdr-handle-tl) {
    top: -8px;
    left: -8px;
    cursor: nwse-resize;
}
:deep(.vdr-handle-tr) {
    top: -8px;
    right: -8px;
    cursor: nesw-resize;
}
:deep(.vdr-handle-bl) {
    bottom: -8px;
    left: -8px;
    cursor: nesw-resize;
}
:deep(.vdr-handle-br) {
    bottom: -8px;
    right: -8px;
    cursor: nwse-resize;
}
</style>
