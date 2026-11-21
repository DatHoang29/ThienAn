<template>
  <el-tooltip :content="tooltipContent" :placement="props.tooltipPlacement">
    <el-button class="map-compass" :class="{ 'is-locked': props.locked }" circle :aria-label="tooltipContent" :aria-disabled="props.locked" @click="handleClick" @mousedown="handleMouseDown" @mouseup="handleMouseUp" @mousemove="handleMouseMove" @mouseleave="handleMouseLeave" @contextmenu.prevent="handleContextMenu">
      <svg class="map-compass__rose" viewBox="0 0 32 32" aria-hidden="true">
        <!-- Vành la bàn cố định + vạch mốc hướng Bắc trên đỉnh nút -->
        <circle class="map-compass__ring" cx="16" cy="16" r="13.5" />
        <path class="map-compass__tick" d="M16 1.5 L16 4.5" />
        <!-- Kim xoay theo bearing nên luôn chỉ đúng hướng Bắc thực tế của bản đồ -->
        <g :style="{ transform: `rotate(${normalizedBearing}deg)` }" class="map-compass__needle">
          <polygon class="map-compass__needle-north" points="16,5 20,17 16,14.5 12,17" />
          <polygon class="map-compass__needle-south" points="16,27 12,15 16,17.5 20,15" />
        </g>
        <circle class="map-compass__pivot" cx="16" cy="16" r="1.8" />
      </svg>
    </el-button>
  </el-tooltip>
</template>

<script setup lang="ts" name="mapCompassControl">
/**
 * La bàn định hướng bản đồ.
 *
 * - Bấm: xoay bản đồ về hướng Bắc (0°). Nếu đang ở hướng Bắc thì bấm lần nữa sẽ
 *   xoay về hướng Nam (180°) — tức là nút đảo qua lại Bắc ⇄ Nam.
 * - Kéo ngang: xoay tự do theo con trỏ.
 * - Chuột phải: trả bản đồ về góc xoay đã cấu hình cho bản đồ hiện tại.
 */
import { computed, reactive } from 'vue';
import { useI18n } from 'vue-i18n';

const { t: $t } = useI18n();

const props = withDefaults(
  defineProps<{
    bearing?: number;
    tooltipPlacement?: 'left' | 'right' | 'top' | 'bottom';
    /** Bản đồ đã khóa xoay: la bàn chỉ hiển thị hướng, không nhận bấm/kéo/chuột phải. */
    locked?: boolean;
  }>(),
  { bearing: 0, tooltipPlacement: 'left', locked: false }
);

const emit = defineEmits<{
  (event: 'set-bearing', value: number): void;
  (event: 'rotate-by-delta', value: number): void;
  (event: 'reset'): void;
}>();

/** Sai số coi như đã trùng hướng (bản đồ có thể lệch chút ít sau animation). */
const BEARING_EPSILON = 1;
const NORTH_BEARING = 0;
const SOUTH_BEARING = 180;

const normalizedBearing = computed(() => (((Number(props.bearing) || 0) % 360) + 360) % 360);

const isFacingNorth = computed(() => {
  const diff = Math.abs(normalizedBearing.value - NORTH_BEARING);
  return Math.min(diff, 360 - diff) <= BEARING_EPSILON;
});

/** Đang ở Bắc thì lần bấm sau xoay xuống Nam, các trường hợp còn lại đưa về Bắc. */
const nextBearing = computed(() => (isFacingNorth.value ? SOUTH_BEARING : NORTH_BEARING));

const tooltipContent = computed(() => {
  if (props.locked) return $t('lz.label.tmsMapLocation.rotationLocked');
  return nextBearing.value === NORTH_BEARING ? $t('lz.button.tmsMapLocation.orientNorth') : $t('lz.button.tmsMapLocation.orientSouth');
});

// Kéo ngang để xoay tự do; chỉ tính là "bấm" khi con trỏ gần như không di chuyển.
const drag = reactive({ active: false, startX: 0, hasMoved: false });

const handleMouseDown = (event: MouseEvent) => {
  if (props.locked || event.button !== 0) return;
  drag.active = true;
  drag.startX = event.clientX;
  drag.hasMoved = false;
};

const handleMouseMove = (event: MouseEvent) => {
  if (!drag.active) return;
  const deltaX = event.clientX - drag.startX;
  if (Math.abs(deltaX) > 3) drag.hasMoved = true;
  if (!drag.hasMoved) return;
  emit('rotate-by-delta', deltaX * 0.5);
  drag.startX = event.clientX;
};

const handleMouseUp = () => {
  drag.active = false;
};

const handleMouseLeave = () => {
  drag.active = false;
};

/** Bỏ qua click sinh ra ở cuối một lần kéo xoay. */
const handleClick = () => {
  if (props.locked) return;
  if (drag.hasMoved) {
    drag.hasMoved = false;
    return;
  }
  emit('set-bearing', nextBearing.value);
};

/** Chuột phải: về góc xoay đã cấu hình (khóa xoay thì bản đồ vốn đã ở góc đó). */
const handleContextMenu = () => {
  if (props.locked) return;
  emit('reset');
};
</script>

<style scoped lang="scss">
/* Kích thước kim/vành ăn theo nút tròn của panel điều khiển (không đặt kích thước cứng
   để la bàn luôn cùng cỡ với các nút zoom/refresh bên cạnh). */
.map-compass__rose {
  width: 100%;
  height: 100%;
  max-width: 24px;
  max-height: 24px;
  overflow: visible;
}

.map-compass__ring {
  fill: none;
  stroke: var(--el-border-color);
  stroke-width: 1.4;
}

.map-compass__tick {
  stroke: var(--el-text-color-secondary);
  stroke-width: 1.6;
  stroke-linecap: round;
}

.map-compass__needle {
  transform-origin: 16px 16px;
  transition: transform 0.25s ease-out;
}

.map-compass__needle-north {
  fill: var(--el-color-danger);
}

.map-compass__needle-south {
  fill: var(--el-text-color-secondary);
  opacity: 0.65;
}

.map-compass__pivot {
  fill: var(--el-bg-color);
  stroke: var(--el-text-color-secondary);
  stroke-width: 1;
}

/* Khóa xoay: la bàn chỉ để xem hướng */
.map-compass.is-locked {
  cursor: not-allowed;
  opacity: 0.6;
}
</style>
