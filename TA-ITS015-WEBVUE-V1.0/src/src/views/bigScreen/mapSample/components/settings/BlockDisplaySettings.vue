<template>
  <!-- Hiển thị chung cho mọi card block (tab "Cấu hình hiển thị" của Thiết lập block) -->
  <SettingsSection :title="$t('lz.label.tmsMapLocation.blockCardContentGroup')" icon="ele-Postcard">
    <SettingRow :label="$t('lz.label.tmsMapLocation.showBlockName')" :hint="$t('lz.label.tmsMapLocation.showBlockNameHint')">
      <el-switch v-model="draft.showBlockName" @change="emitConfiguration" />
    </SettingRow>
    <SettingRow :label="$t('lz.label.tmsMapLocation.showLevelName')" :hint="$t('lz.label.tmsMapLocation.showLevelNameHint')">
      <el-switch v-model="draft.showLevelName" @change="emitConfiguration" />
    </SettingRow>
    <SettingRow :label="$t('lz.label.tmsMapLocation.blockCardMinWidth')" :hint="$t('lz.label.tmsMapLocation.blockCardMinWidthHint')">
      <el-input-number
        v-model="draft.cardMinWidth"
        :min="0"
        :max="BLOCK_CARD_MIN_WIDTH_MAX"
        :step="10"
        :precision="0"
        size="default"
        controls-position="right"
        :aria-label="$t('lz.label.tmsMapLocation.blockCardMinWidth')"
        @change="emitConfiguration"
      />
    </SettingRow>
  </SettingsSection>

  <SettingsSection :title="$t('lz.label.tmsMapLocation.blockTranslucent')" icon="ele-MagicStick">
    <SettingRow :label="$t('lz.label.tmsMapLocation.blockTranslucent')" :hint="$t('lz.label.tmsMapLocation.blockTranslucentHint')">
      <el-switch v-model="draft.translucent" @change="emitConfiguration" />
    </SettingRow>

    <el-collapse-transition>
      <div v-show="draft.translucent" class="block-settings__sub">
        <div class="block-settings__sub-controls">
          <div class="block-settings__field">
            <span class="block-settings__field-label">{{ $t('lz.label.tmsMapLocation.blockTranslucentScope') }}</span>
            <el-segmented v-model="draft.translucentScope" :options="translucentScopeOptions" size="small" block @change="emitConfiguration" />
          </div>
          <div class="block-settings__field">
            <span class="block-settings__field-label">
              {{ $t('lz.label.tmsMapLocation.blockOpacity') }}
              <b class="block-settings__value">{{ formatOpacity(draft.translucentOpacity) }}</b>
            </span>
            <el-slider
              v-model="draft.translucentOpacity"
              size="small"
              :min="BLOCK_TRANSLUCENT_MIN_OPACITY"
              :max="1"
              :step="0.05"
              :format-tooltip="formatOpacity"
              :aria-label="$t('lz.label.tmsMapLocation.blockOpacity')"
              @change="emitConfiguration"
            />
          </div>
          <SettingRow :label="$t('lz.label.tmsMapLocation.blockOpaqueOnHover')" :hint="$t('lz.label.tmsMapLocation.blockOpaqueOnHoverHint')">
            <el-switch v-model="draft.opaqueOnHover" @change="emitConfiguration" />
          </SettingRow>
        </div>

        <!-- Xem trước: card mẫu trên nền giả lập bản đồ, áp đúng cấu hình đang chọn (rê chuột để thử) -->
        <div class="block-preview" aria-hidden="true">
          <div class="block-preview__stage">
            <div class="block-preview__card" :class="previewCardClass" :style="previewCardStyle">
              <div v-if="draft.showBlockName" class="block-preview__name">{{ $t('lz.label.tmsMapLocation.block') }}</div>
              <div class="block-preview__icons">
                <span class="block-preview__icon block-preview__icon--success" />
                <span class="block-preview__icon block-preview__icon--success" />
                <span class="block-preview__icon block-preview__icon--danger" />
              </div>
            </div>
          </div>
          <span class="block-preview__caption">{{ $t('lz.label.tmsMapLocation.blockPreview') }}</span>
        </div>
      </div>
    </el-collapse-transition>
  </SettingsSection>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import SettingsSection from './SettingsSection.vue';
import SettingRow from './SettingRow.vue';
import { BLOCK_CARD_MIN_WIDTH_MAX, BLOCK_TRANSLUCENT_MIN_OPACITY, cloneBlockConfiguration, normalizeBlockConfiguration } from '../../blockTypes';
import type { BlockConfiguration, BlockTranslucentScope } from '../../blockTypes';

const props = defineProps<{ config?: BlockConfiguration }>();
const emit = defineEmits<{ (event: 'update:config', value: BlockConfiguration): void }>();
const { t: $t } = useI18n();

type DisplayOptions = Pick<BlockConfiguration, 'showLevelName' | 'showBlockName' | 'translucent' | 'translucentOpacity' | 'translucentScope' | 'opaqueOnHover' | 'cardMinWidth'>;

const pickDisplayOptions = (config?: BlockConfiguration): DisplayOptions => {
  const normalized = normalizeBlockConfiguration(config);
  return {
    showLevelName: normalized.showLevelName,
    showBlockName: normalized.showBlockName,
    translucent: normalized.translucent,
    translucentOpacity: normalized.translucentOpacity,
    translucentScope: normalized.translucentScope as BlockTranslucentScope,
    opaqueOnHover: normalized.opaqueOnHover,
    cardMinWidth: normalized.cardMinWidth,
  };
};

const draft = reactive<DisplayOptions>(pickDisplayOptions(props.config));

watch(
  () => props.config,
  (value) => Object.assign(draft, pickDisplayOptions(value)),
  { deep: true }
);

/** Giữ nguyên danh sách block, chỉ thay phần tuỳ chọn hiển thị. */
const emitConfiguration = () => emit('update:config', cloneBlockConfiguration({ ...normalizeBlockConfiguration(props.config), ...draft }));

/** Hiển thị độ đậm dạng phần trăm trên tooltip của slider. */
const formatOpacity = (value: number) => `${Math.round(value * 100)}%`;

/** Mờ cả block (nền + icon + chữ) hay chỉ mờ nền (icon/chữ vẫn rõ). */
const translucentScopeOptions = computed(() => [
  { label: $t('lz.label.tmsMapLocation.blockTranslucentScopeBlock'), value: 'block' },
  { label: $t('lz.label.tmsMapLocation.blockTranslucentScopeBackground'), value: 'background' },
]);

/** Card xem trước dùng cùng quy tắc với BlockMapLayer để người cấu hình thấy ngay kết quả. */
const previewCardClass = computed(() => ({
  'block-preview__card--translucent': draft.translucentScope === 'block',
  'block-preview__card--translucent-bg': draft.translucentScope === 'background',
  'block-preview__card--opaque-on-hover': draft.opaqueOnHover,
}));

const previewCardStyle = computed(() => ({
  '--preview-opacity': String(draft.translucentOpacity),
  '--preview-bg-alpha': `${Math.round(draft.translucentOpacity * 100)}%`,
}));
</script>

<style scoped>
/* Khung chi tiết chế độ mờ: điều khiển bên trái, xem trước bên phải */
.block-settings__sub { display: grid; grid-template-columns: minmax(0, 1fr) 132px; gap: 12px; margin: 2px 0; padding: 10px 12px; border-left: 3px solid var(--el-color-primary-light-5); border-radius: 0 8px 8px 0; background: var(--el-fill-color-lighter); }
.block-settings__sub-controls { display: flex; flex-direction: column; gap: 10px; min-width: 0; }
.block-settings__field { display: flex; flex-direction: column; gap: 4px; }
.block-settings__field-label { display: flex; align-items: center; justify-content: space-between; color: var(--el-text-color-regular); font-size: 12px; font-weight: 600; }
.block-settings__value { padding: 0 6px; border-radius: 999px; background: var(--el-color-primary-light-9); color: var(--el-color-primary); font-size: 11px; }
.block-settings__field .el-slider { padding: 0 6px; }

/* Xem trước chế độ mờ: nền giả lập bản đồ (đường + vùng màu) + card mẫu */
.block-preview { display: flex; flex-direction: column; align-items: center; gap: 4px; }
.block-preview__stage {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 108px;
  border-radius: 8px;
  overflow: hidden;
  background:
    linear-gradient(115deg, transparent 44%, #f5c451 44%, #f5c451 52%, transparent 52%),
    linear-gradient(25deg, transparent 58%, #ffffff 58%, #ffffff 63%, transparent 63%),
    radial-gradient(circle at 20% 25%, #a7d8a0 0 22%, transparent 23%),
    radial-gradient(circle at 85% 80%, #9ccbe8 0 26%, transparent 27%),
    #e8eef3;
}
.block-preview__card { width: 96px; padding: 5px; border: 1px solid var(--el-border-color-light); border-radius: 8px; background: color-mix(in srgb, var(--el-bg-color) 92%, transparent); box-shadow: var(--el-box-shadow-light); transition: opacity 0.2s ease-out, background-color 0.2s ease-out, box-shadow 0.2s ease-out, border-color 0.2s ease-out; }
.block-preview__card--translucent { opacity: var(--preview-opacity, 0.5); }
.block-preview__card--translucent-bg { background: color-mix(in srgb, var(--el-bg-color) var(--preview-bg-alpha, 50%), transparent); border-color: color-mix(in srgb, var(--el-border-color-light) var(--preview-bg-alpha, 50%), transparent); box-shadow: none; }
.block-preview__card--opaque-on-hover:hover { opacity: 1; background: color-mix(in srgb, var(--el-bg-color) 92%, transparent); border-color: var(--el-border-color-light); box-shadow: var(--el-box-shadow-light); }
.block-preview__name { margin-bottom: 4px; padding-bottom: 3px; border-bottom: 1px solid var(--el-border-color-lighter); color: var(--el-text-color-primary); font-size: 10px; font-weight: 700; text-align: center; }
.block-preview__icons { display: flex; justify-content: center; gap: 4px; }
.block-preview__icon { width: 20px; height: 20px; border: 2px solid var(--el-color-success); border-radius: 50%; background: var(--el-bg-color); }
.block-preview__icon--danger { border-color: var(--el-color-danger); }
.block-preview__caption { color: var(--el-text-color-secondary); font-size: 11px; }

@media (max-width: 640px) {
  .block-settings__sub { grid-template-columns: 1fr; }
}
</style>
