<template>
  <div class="map-legend">
    <header class="map-legend__header">
      <span class="map-legend__header-mark"></span>
      <span class="map-legend__title">{{ $t('lz.label.tmsMapLocation.mapLegend') }}</span>
    </header>

    <!-- Màu trạng thái giao thông của zone (config tms_zone_status_color) -->
    <section class="map-legend__section">
      <div class="map-legend__section-title">{{ $t('lz.label.tmsMapLocation.legendZoneStatus') }}</div>
      <ul v-if="zoneStatusItems.length" class="map-legend__list">
        <li v-for="item in zoneStatusItems" :key="`zone_${item.code}`" class="map-legend__item" :style="{ '--legend-color': item.color }">
          <span class="map-legend__ribbon"></span>
          <span class="map-legend__name">{{ item.name }}</span>
        </li>
      </ul>
      <div v-else class="map-legend__empty">{{ $t('lz.label.base.noData') }}</div>
    </section>

    <!-- Màu viền icon thiết bị theo trạng thái (config equipment_state_color) -->
    <section class="map-legend__section">
      <div class="map-legend__section-title">{{ $t('lz.label.tmsMapLocation.legendEquipmentState') }}</div>
      <ul v-if="equipmentStateItems.length" class="map-legend__list">
        <li v-for="item in equipmentStateItems" :key="`equip_${item.code}`" class="map-legend__item" :style="{ '--legend-color': item.color }">
          <span class="map-legend__dot"></span>
          <span class="map-legend__name">{{ item.name }}</span>
        </li>
      </ul>
      <div v-else class="map-legend__empty">{{ $t('lz.label.base.noData') }}</div>
    </section>
  </div>
</template>

<script setup lang="ts" name="mapLegend">
/**
 * Bảng chú thích màu sắc trên bản đồ.
 *
 * Dữ liệu lấy trực tiếp từ `useBaseConfig` để chú thích luôn khớp với màu đang được
 * vẽ trên bản đồ:
 * - Zone: `tms_zone_status_color` (code = condition, value = mã màu) — cùng nguồn với
 *   `useZoneStatus.applyZoneStatusStyle`.
 * - Thiết bị: `equipment_state_color` (code = nhóm trạng thái, value = mã màu) — cùng
 *   nguồn với `markerIcons.resolveEquipmentStateColor`.
 */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const { t: $t } = useI18n();
const baseConfig = useBaseConfig();

type LegendItem = { code: string; name: string; color: string };

/**
 * Mã màu của 1 dòng cấu hình. Màu chuẩn nằm ở `value`; các bản ghi cũ có thể đặt màu
 * ở `code` nên chấp nhận thêm dạng #hex/rgb/hsl ở `code`.
 */
const pickColor = (item: any): string => {
  const value = String(item?.value ?? '').trim();
  if (value) return value;
  const code = String(item?.code ?? '').trim();
  return /^#|^rgb|^hsl/i.test(code) ? code : '';
};

/** Chuyển 1 nhóm cấu hình màu thành danh sách dòng chú thích (bỏ dòng thiếu màu). */
const buildLegendItems = (configTypeCode: string): LegendItem[] =>
  (baseConfig.getConfigDataByCode(configTypeCode) || [])
    .map((item: any) => ({
      code: String(item?.code ?? ''),
      name: baseConfig.getConfigItemName(configTypeCode, String(item?.code ?? '')),
      color: pickColor(item),
    }))
    .filter((item: LegendItem) => !!item.code && !!item.color);

const zoneStatusItems = computed<LegendItem[]>(() => buildLegendItems(BaseConfigTypeEnum.TmsZoneStatusColor));

const equipmentStateItems = computed<LegendItem[]>(() => buildLegendItems(BaseConfigTypeEnum.TmsEquipmentStateColor));
</script>

<style scoped lang="scss">
.map-legend {
  display: flex;
  flex-direction: column;
  gap: 12px;
  max-height: 62vh;
  overflow-y: auto;

  &::-webkit-scrollbar {
    width: 6px;
  }

  &::-webkit-scrollbar-thumb {
    background: var(--el-border-color);
    border-radius: 999px;
  }
}

.map-legend__header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

/* Vạch màu nhỏ thay cho icon: gọn, không cạnh tranh thị giác với các ô màu bên dưới */
.map-legend__header-mark {
  width: 3px;
  height: 14px;
  border-radius: 999px;
  background: var(--el-color-primary);
}

.map-legend__title {
  color: var(--el-text-color-primary);
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.01em;
}

.map-legend__section-title {
  margin-bottom: 6px;
  color: var(--el-text-color-secondary);
  font-size: 11px;
  font-weight: 600;
}

.map-legend__list {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.map-legend__item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 4px 6px;
  border-radius: 6px;
  transition: background-color 0.18s ease;

  &:hover {
    background: var(--el-fill-color-light);
  }
}

/* Dải màu mô phỏng đúng "route ribbon" đang tô cho zone trên bản đồ */
.map-legend__ribbon {
  flex: 0 0 auto;
  width: 28px;
  height: 10px;
  border-radius: 999px;
  background: linear-gradient(90deg, color-mix(in srgb, var(--legend-color) 55%, transparent), var(--legend-color));
  box-shadow: 0 0 6px color-mix(in srgb, var(--legend-color) 50%, transparent);
}

/* Vòng tròn mô phỏng viền icon thiết bị theo trạng thái */
.map-legend__dot {
  flex: 0 0 auto;
  width: 14px;
  height: 14px;
  border: 2px solid var(--legend-color);
  border-radius: 50%;
  background: color-mix(in srgb, var(--legend-color) 18%, var(--el-bg-color));
  box-shadow: 0 0 5px color-mix(in srgb, var(--legend-color) 42%, transparent);
}

.map-legend__name {
  color: var(--el-text-color-regular);
  font-size: 12px;
  line-height: 1.35;
  word-break: break-word;
}

.map-legend__empty {
  padding: 4px 6px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
</style>
