<template>
  <!-- 2 tab con: "Thiết lập block" (danh sách block, thiết bị) và "Cấu hình hiển thị" (áp cho mọi block) -->
  <el-tabs v-model="activeTab" class="block-editor block-editor__tabs">
    <el-tab-pane name="blocks">
      <template #label>
        <span class="block-editor__tab-label">
          <el-icon><ele-Grid /></el-icon>
          {{ $t('lz.label.tmsMapLocation.blockSetupTab') }}
          <span class="block-editor__tab-count">{{ draft.blocks.length }}</span>
        </span>
      </template>

      <div class="block-editor__pane">
    <div class="block-editor__heading">
      <div class="block-editor__heading-text">
        <div class="block-editor__hint">{{ $t('lz.label.tmsMapLocation.blockConfigurationHint') }}</div>
      </div>
      <div class="block-editor__heading-actions">
        <el-tooltip :content="isAllCollapsed ? $t('lz.button.tmsMapLocation.expandAllBlocks') : $t('lz.button.tmsMapLocation.collapseAllBlocks')" placement="top">
          <el-button size="small" circle :disabled="!draft.blocks.length" @click="toggleAllBlocks">
            <el-icon><component :is="isAllCollapsed ? 'ele-ArrowDown' : 'ele-ArrowUp'" /></el-icon>
          </el-button>
        </el-tooltip>
        <el-button size="small" type="primary" @click="addBlock">
          <el-icon><ele-Plus /></el-icon>
          {{ $t('lz.button.base.add') }} {{ $t('lz.label.tmsMapLocation.block') }}
        </el-button>
      </div>
    </div>

    <!-- Tìm nhanh theo tên block hoặc tên thiết bị khi có nhiều block -->
    <el-input v-if="draft.blocks.length > 3" v-model="blockSearch" size="small" clearable prefix-icon="Search"
      class="block-editor__search" :placeholder="$t('lz.label.tmsMapLocation.searchBlock')" />

    <el-empty v-if="!draft.blocks.length" :description="$t('lz.label.tmsMapLocation.noBlocks')" :image-size="56" />
    <div v-else-if="!visibleBlockCount" class="block-editor__no-match">{{ $t('lz.label.tmsMapLocation.locationNoMatch') }}</div>

    <!-- Mỗi block là 1 thẻ: dòng tiêu đề (số thứ tự, tên, số tầng/thiết bị, icon thiết bị, thao tác) bấm để mở/thu gọn -->
    <article
      v-for="(block, blockIndex) in draft.blocks"
      v-show="isBlockVisible(block)"
      :key="block.id"
      class="block-card"
      :class="{ 'is-open': !collapsedBlocks[block.id] }"
    >
      <header
        class="block-card__head"
        role="button"
        tabindex="0"
        :aria-expanded="!collapsedBlocks[block.id]"
        @click="toggleBlock(block.id)"
        @keydown.enter.prevent="toggleBlock(block.id)"
        @keydown.space.prevent="toggleBlock(block.id)"
      >
        <el-icon class="block-card__chevron"><ele-ArrowRight /></el-icon>
        <span class="block-card__index">{{ blockIndex + 1 }}</span>
        <div class="block-card__info">
          <div class="block-card__name" :title="getBlockName(block, blockIndex)">{{ getBlockName(block, blockIndex) }}</div>
          <div class="block-card__meta">
            <span class="block-card__stat"><el-icon><ele-Files /></el-icon>{{ $t('lz.label.tmsMapLocation.blockLevelCount', { count: block.levels.length }) }}</span>
            <span class="block-card__stat" :class="{ 'is-empty': !getBlockDeviceCount(block) }">
              <el-icon><ele-Cpu /></el-icon>{{ $t('lz.label.tmsMapLocation.blockDeviceCount', { count: getBlockDeviceCount(block) }) }}
            </span>
          </div>
        </div>
        <!-- Xem nhanh icon thiết bị trong block (không cần mở thẻ) -->
        <div v-if="getBlockDeviceCount(block)" class="block-card__devices" aria-hidden="true">
          <span v-for="device in getBlockDevicePreview(block)" :key="device.id" class="block-card__device" :title="getDeviceName(device.deviceId)">
            <img v-if="getDeviceIconById(device.deviceId)" :key="getDeviceIconById(device.deviceId)" :src="getDeviceIconById(device.deviceId)" alt="" @error="hideBrokenImage" />
            <span v-else>{{ getDeviceName(device.deviceId).slice(0, 1) }}</span>
          </span>
          <span v-if="getBlockDeviceCount(block) > BLOCK_DEVICE_PREVIEW" class="block-card__device block-card__device--more">
            +{{ getBlockDeviceCount(block) - BLOCK_DEVICE_PREVIEW }}
          </span>
        </div>
        <div class="block-card__actions" @click.stop @keydown.stop>
          <el-tooltip :content="$t('lz.button.tmsMapLocation.locateOnMap')">
            <el-button size="small" text class="block-card__btn" :aria-label="$t('lz.button.tmsMapLocation.locateOnMap')" @click="locateBlock(block)">
              <el-icon><ele-Aim /></el-icon>
            </el-button>
          </el-tooltip>
          <el-popconfirm
            :title="$t('lz.message.tmsMapLocation.confirmDeleteBlock', { name: getBlockName(block, blockIndex) })"
            :confirm-button-text="$t('lz.button.base.delete')"
            :cancel-button-text="$t('lz.button.base.cancel')"
            confirm-button-type="danger"
            width="240"
            @confirm="removeBlock(blockIndex)"
          >
            <template #reference>
              <el-button size="small" text class="block-card__btn block-card__btn--danger" :aria-label="$t('lz.button.base.delete')">
                <el-icon><ele-Delete /></el-icon>
              </el-button>
            </template>
          </el-popconfirm>
        </div>
      </header>

      <el-collapse-transition>
      <div v-if="!collapsedBlocks[block.id]" class="block-card__body">
      <label class="block-card__name-field">
        <span>{{ $t('lz.label.tmsMapLocation.blockName') }}</span>
        <el-input v-model="block.name" size="small" :placeholder="$t('lz.label.tmsMapLocation.blockName')" @change="emitConfiguration" />
      </label>

      <!-- Thiết bị tham chiếu (đang tắt):
          <el-form-item :label="$t('lz.label.tmsMapLocation.referenceEquipment')">
            <div class="block-editor__linked-selects">
              <el-select
                v-model="getBlockSelection(block.id).equipmentTypeId"
                clearable
                filterable
                :placeholder="$t('lz.label.tmsMapLocation.selectEquipmentType')"
                size="small"
                @change="handleAnchorEquipmentTypeChange(block)"
              >
                <el-option v-for="type in availableEquipmentTypes" :key="type.id" :label="type.name" :value="String(type.id)" />
              </el-select>
              <el-select
                v-model="block.anchorDeviceId"
                clearable
                filterable
                :disabled="!getBlockSelection(block.id).equipmentTypeId"
                :placeholder="$t('lz.label.tmsMapLocation.selectEquipment')"
                size="small"
                @change="setAnchorFromDevice(block)"
              >
                <el-option
                  v-for="device in getDevicesByType(getBlockSelection(block.id).equipmentTypeId)"
                  :key="device.id"
                  :label="device.name"
                  :value="String(device.id)"
                />
              </el-select>
            </div>
          </el-form-item> -->

      <!-- Vị trí: điểm thực tế (đầu đường nối) và vị trí đặt card trên bản đồ -->
      <SettingsSection class="block-section" :title="$t('lz.label.tmsMapLocation.blockPositionGroup')" icon="ele-Location"
        :collapsed="isSectionCollapsed(block.id, 'position')" @update:collapsed="(value: boolean) => setSectionCollapsed(block.id, 'position', value)">
        <div class="block-coords">
          <span />
          <span class="block-coords__axis">{{ $t('lz.label.tmsMapLocation.latitude') }}</span>
          <span class="block-coords__axis">{{ $t('lz.label.tmsMapLocation.longitude') }}</span>

          <span class="block-coords__label">
            <i class="block-coords__dot block-coords__dot--actual" :style="{ background: block.anchorColor || undefined }" />
            {{ $t('lz.label.tmsMapLocation.blockActualPosition') }}
          </span>
          <el-input-number v-model="block.actualPosition.lat" :precision="6" :step="0.0001" controls-position="right" size="small" :aria-label="$t('lz.label.tmsMapLocation.actualAnchorLatitude')" @change="emitConfiguration" />
          <el-input-number v-model="block.actualPosition.lng" :precision="6" :step="0.0001" controls-position="right" size="small" :aria-label="$t('lz.label.tmsMapLocation.actualAnchorLongitude')" @change="emitConfiguration" />

          <span class="block-coords__label">
            <i class="block-coords__dot block-coords__dot--display" />
            {{ $t('lz.label.tmsMapLocation.blockDisplayPosition') }}
          </span>
          <el-input-number v-model="block.displayPosition.lat" :precision="6" :step="0.0001" controls-position="right" size="small" :aria-label="$t('lz.label.tmsMapLocation.displayLatitude')" @change="emitConfiguration" />
          <el-input-number v-model="block.displayPosition.lng" :precision="6" :step="0.0001" controls-position="right" size="small" :aria-label="$t('lz.label.tmsMapLocation.displayLongitude')" @change="emitConfiguration" />
        </div>
      </SettingsSection>

      <!-- Tuỳ biến hiển thị: màu chấm vị trí, màu/độ dày/kiểu nét đường nối, điểm bẻ góc -->
      <SettingsSection class="block-section" :title="$t('lz.label.tmsMapLocation.blockLineStyleGroup')" icon="ele-Connection"
        :collapsed="isSectionCollapsed(block.id, 'line')" @update:collapsed="(value: boolean) => setSectionCollapsed(block.id, 'line', value)">
        <div class="block-style-grid">
          <span class="block-style-grid__label">{{ $t('lz.label.tmsMapLocation.anchorColor') }}</span>
          <div class="block-style-grid__controls">
            <el-color-picker v-model="block.anchorColor" size="small" :predefine="colorPresets" @change="emitConfiguration" />
            <el-input-number
              v-model="block.anchorRadius"
              :min="2"
              :max="20"
              :step="1"
              :precision="0"
              controls-position="right"
              size="small"
              class="block-style-grid__number"
              :placeholder="String(BLOCK_ANCHOR_DEFAULT_RADIUS)"
              @change="emitConfiguration"
            />
            <span class="block-style-grid__unit">px</span>
          </div>

          <span class="block-style-grid__label">{{ $t('lz.label.tmsMapLocation.blockLineColor') }}</span>
          <div class="block-style-grid__controls">
            <el-color-picker v-model="block.lineColor" size="small" :predefine="colorPresets" @change="emitConfiguration" />
            <el-input-number
              v-model="block.lineWidth"
              :min="1"
              :max="12"
              :step="1"
              :precision="0"
              controls-position="right"
              size="small"
              class="block-style-grid__number"
              :placeholder="String(BLOCK_LINE_DEFAULT_WIDTH)"
              @change="emitConfiguration"
            />
            <span class="block-style-grid__unit">px</span>
          </div>

          <span class="block-style-grid__label">{{ $t('lz.label.tmsMapLocation.blockLineStyle') }}</span>
          <div class="block-style-grid__controls">
            <el-segmented
              :model-value="block.lineStyle || 'solid'"
              :options="lineStyleOptions"
              size="small"
              @change="(value: any) => updateLineStyle(block, value)"
            />
          </div>

          <span class="block-style-grid__label">{{ $t('lz.label.tmsMapLocation.blockLineWaypoints') }}</span>
          <div class="block-style-grid__controls">
            <span class="block-form__waypoint-count">{{ block.waypoints.length }}</span>
            <el-button size="small" plain :disabled="!block.waypoints.length" @click="clearWaypoints(block)">
              {{ $t('lz.button.base.clear') }}
            </el-button>
          </div>
        </div>
        <div class="block-section__hint">
          <el-icon><ele-InfoFilled /></el-icon>
          <span>{{ $t('lz.label.tmsMapLocation.blockLineWaypointHint') }}</span>
        </div>
      </SettingsSection>

      <SettingsSection class="block-section" :title="$t('lz.label.tmsMapLocation.blockLevelsGroup')" icon="ele-Files"
        :collapsed="isSectionCollapsed(block.id, 'levels')" @update:collapsed="(value: boolean) => setSectionCollapsed(block.id, 'levels', value)">
      <Draggable v-model="block.levels" item-key="id" handle=".block-level__drag" class="block-levels" @end="emitConfiguration">
        <template #item="{ element: level, index: levelIndex }">
          <div class="block-level">
            <div class="block-level__header">
              <span class="block-level__drag" :title="$t('lz.label.tmsMapLocation.reorder')"><el-icon><ele-Rank /></el-icon></span>
              <span class="block-level__index">{{ levelIndex + 1 }}</span>
              <el-input v-model="level.name" size="small" :placeholder="$t('lz.label.tmsMapLocation.levelName')" @change="emitConfiguration" />
              <span class="block-level__count" :title="$t('lz.label.tmsMapLocation.blockDeviceCount', { count: level.devices.length })">
                <el-icon><ele-Cpu /></el-icon>{{ level.devices.length }}
              </span>
              <el-popconfirm
                :title="$t('lz.message.tmsMapLocation.confirmDeleteLevel', { name: level.name || levelIndex + 1 })"
                :confirm-button-text="$t('lz.button.base.delete')"
                :cancel-button-text="$t('lz.button.base.cancel')"
                confirm-button-type="danger"
                width="240"
                @confirm="removeLevel(block, levelIndex)"
              >
                <template #reference>
                  <el-button size="small" text class="block-card__btn block-card__btn--danger" :aria-label="$t('lz.button.base.delete')">
                    <el-icon><ele-Delete /></el-icon>
                  </el-button>
                </template>
              </el-popconfirm>
            </div>

            <!-- Bố trí theo Km: thiết bị đặt đúng vị trí tương đối theo lý trình, chiều dài tuyến/hầm tự đo -->
            <div class="block-level__layout">
              <el-switch
                :model-value="level.layout === 'route'"
                size="small"
                :active-text="$t('lz.label.tmsMapLocation.levelLayoutRoute')"
                @change="(value: boolean) => setLevelLayout(level, value)"
              />
              <template v-if="level.layout === 'route'">
                <el-checkbox :model-value="level.routeReverse === true" size="small" @change="(value: boolean) => setLevelRouteReverse(level, value)">
                  {{ $t('lz.label.tmsMapLocation.levelRouteReverse') }}
                </el-checkbox>
                <el-checkbox :model-value="level.splitRowsByType === true" size="small" @change="(value: boolean) => setLevelSplitRowsByType(level, value)">
                  {{ $t('lz.label.tmsMapLocation.levelSplitRowsByType') }}
                </el-checkbox>
                <span class="block-level__layout-hint">{{ getLevelRouteSummary(level) }}</span>
              </template>
            </div>

            <!-- Khoảng lý trình cố định của tầng: chọn zone hầm/tuyến, theo cấu hình cơ sở hoặc nhập tay (mét = km*1000+m) -->
            <div v-if="level.layout === 'route'" class="block-level__range">
              <el-select
                :model-value="level.zoneId || ''"
                clearable
                filterable
                size="small"
                class="block-level__range-zone"
                :placeholder="$t('lz.label.tmsMapLocation.levelRangeZone')"
                @change="(value: string) => setLevelZone(level, value)"
              >
                <el-option v-for="zone in routeZoneOptions" :key="zone.id" :label="zone.label" :value="zone.id" />
              </el-select>
              <el-input-number
                v-model="getLevelSelection(level.id).rangeFrom"
                :controls="false"
                :min="0"
                :precision="0"
                size="small"
                class="block-level__range-input"
                :placeholder="$t('lz.label.tmsMapLocation.levelRangeFrom')"
                @change="commitLevelRange(level)"
              />
              <el-input-number
                v-model="getLevelSelection(level.id).rangeTo"
                :controls="false"
                :min="0"
                :precision="0"
                size="small"
                class="block-level__range-input"
                :placeholder="$t('lz.label.tmsMapLocation.levelRangeTo')"
                @change="commitLevelRange(level)"
              />
              <el-button size="small" plain @click="applyBaseRange(level)">{{ $t('lz.button.tmsMapLocation.levelRangeBase') }}</el-button>
            </div>

            <!-- Bộ lọc tự điền: vị trí (Location) + hướng (DirectionId) + khoảng Km của tầng -->
            <div v-if="level.layout === 'route'" class="block-level__range">
              <el-select
                v-model="getLevelSelection(level.id).location"
                clearable
                size="small"
                class="block-level__range-input"
                :placeholder="$t('lz.label.tmsMapLocation.levelLocation')"
              >
                <el-option v-for="option in locationOptions" :key="option.value" :label="option.label" :value="option.value" />
              </el-select>
              <el-select
                :model-value="level.directionId ?? ''"
                clearable
                size="small"
                class="block-level__range-input"
                :placeholder="$t('lz.label.tmsMapLocation.levelDirection')"
                @change="(value: number | '') => setLevelDirection(level, value)"
              >
                <el-option v-for="option in directionOptions" :key="option.value" :label="option.label" :value="option.value" />
              </el-select>
              <el-button size="small" type="primary" plain @click="autoFillDevices(level)">
                {{ $t('lz.button.tmsMapLocation.levelAutoFill') }}
              </el-button>
            </div>

            <div class="block-level__add-device">
              <el-select
                v-model="getLevelSelection(level.id).equipmentTypeId"
                clearable
                filterable
                :placeholder="$t('lz.label.tmsMapLocation.selectEquipmentType')"
                size="small"
                @change="handleLevelEquipmentTypeChange(level)"
              >
                <el-option v-for="type in availableEquipmentTypes" :key="type.id" :label="type.name" :value="String(type.id)" />
              </el-select>
              <el-select
                v-model="getLevelSelection(level.id).deviceIds"
                multiple
                collapse-tags
                collapse-tags-tooltip
                clearable
                filterable
                :disabled="!getLevelSelection(level.id).equipmentTypeId"
                :placeholder="$t('lz.label.tmsMapLocation.selectEquipment')"
                size="small"
              >
                <el-option
                  v-for="device in getAvailableDevicesByType(getLevelSelection(level.id).equipmentTypeId)"
                  :key="device.id"
                  :label="device.name"
                  :value="String(device.id)"
                />
              </el-select>
              <el-button size="small" plain :disabled="!getLevelSelection(level.id).equipmentTypeId" @click="selectAllAvailableDevices(level)">
                {{ $t('lz.button.tmsMapLocation.selectAllDevices') }}
              </el-button>
              <el-button size="small" type="primary" :disabled="!getLevelSelection(level.id).deviceIds?.length" @click="addSelectedDevices(level)">
                <el-icon><ele-Plus /></el-icon>
                {{ $t('lz.button.base.add') }}
              </el-button>
            </div>

            <Draggable
              v-model="level.devices"
              item-key="id"
              :group="{ name: 'block-device', pull: true, put: true }"
              class="block-level__devices"
              ghost-class="block-device--ghost"
              @end="emitConfiguration"
            >
              <template #item="{ element: device, index: deviceIndex }">
                <div class="block-device">
                  <img v-if="getDeviceIconById(device.deviceId)" :key="getDeviceIconById(device.deviceId)" :src="getDeviceIconById(device.deviceId)" alt=""
                    :class="{ 'is-flipped': device.flipX }" @error="hideBrokenImage" />
                  <span>{{ getDeviceName(device.deviceId) }}</span>
                  <small v-if="level.layout === 'route'" class="block-device__km" :class="{ 'is-missing': getDeviceRoutePosition(device.deviceId) === null }">{{ getDeviceKmLabel(device.deviceId) }}</small>
                  <el-tooltip :content="$t('lz.button.tmsMapLocation.flipIconHorizontal')" placement="top">
                    <el-button text size="small" :type="device.flipX ? 'primary' : 'default'" class="block-device__flip"
                      :aria-label="$t('lz.button.tmsMapLocation.flipIconHorizontal')" :aria-pressed="!!device.flipX"
                      @click="toggleDeviceFlip(device)">
                      <el-icon><ele-Switch /></el-icon>
                    </el-button>
                  </el-tooltip>
                  <el-button text type="danger" size="small" @click="removeDevice(level, deviceIndex)">
                    <el-icon><ele-Close /></el-icon>
                  </el-button>
                </div>
              </template>
              <template #footer>
                <div v-if="!level.devices.length" class="block-level__drop-placeholder">{{ $t('lz.label.tmsMapLocation.dropDeviceHere') }}</div>
              </template>
            </Draggable>
          </div>
        </template>
      </Draggable>

      <el-button size="small" plain class="block-form__add-level" @click="addLevel(block)">
        <el-icon><ele-Plus /></el-icon>
        {{ $t('lz.button.base.add') }} {{ $t('lz.label.tmsMapLocation.level') }}
      </el-button>
      </SettingsSection>
      </div>
      </el-collapse-transition>
    </article>
      </div>
    </el-tab-pane>

    <el-tab-pane name="display">
      <template #label>
        <span class="block-editor__tab-label">
          <el-icon><ele-Setting /></el-icon>
          {{ $t('lz.label.tmsMapLocation.blockDisplayTab') }}
        </span>
      </template>

      <!-- Nội dung card + chế độ mờ (kèm xem trước) -->
      <BlockDisplaySettings :config="draft" @update:config="handleDisplayConfigUpdate" />
    </el-tab-pane>
  </el-tabs>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import Draggable from 'vuedraggable';
import { locateBlockOnMap } from '../helpers/blockLocate';
import SettingsSection from './settings/SettingsSection.vue';
import BlockDisplaySettings from './settings/BlockDisplaySettings.vue';
import { useMapStore } from '/@/stores/its/mapStore';
import { getAPI } from '/@/utils/axios-utils';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { TmsZoneApi } from '/@/api-services/tms';
import { ensureOpConfigLoaded, readOpConfigIntOrNull } from '/@/components/vms/controlVms/composables/useOpConfig';
import { resolveDeviceIconUrl } from '../helpers/markerIcons';
import { formatRouteKm, getEquipmentRoutePosition } from '../helpers/routeLayout';
import { cloneBlockConfiguration, createEmptyBlockConfiguration, createMapBlock } from '../blockTypes';
import { BLOCK_ANCHOR_DEFAULT_RADIUS, BLOCK_LINE_DEFAULT_WIDTH } from '../blockTypes';
import type { BlockConfiguration, BlockDevice, BlockLevel, BlockLineStyle, MapBlock } from '../blockTypes';

type EquipmentSelection = {
  equipmentTypeId?: string;
  /** Thiết bị neo của block (chọn 1). */
  deviceId?: string;
  /** Thiết bị thêm vào tầng (chọn nhiều một lần). */
  deviceIds?: string[];
  /** Ô nhập khoảng Km (mét) của tầng — chỉ ghi vào level khi đủ cả hai và đầu < cuối. */
  rangeFrom?: number | null;
  rangeTo?: number | null;
  /** Bộ lọc Location thiết bị khi tự điền (không lưu vào cấu hình). */
  location?: string;
};

/** Mã cấu hình (SysOpConfig) khoảng lý trình cơ sở, đơn vị mét = km*1000+m. */
const BASE_START_POSITION_CODE = 'base_start_position';
const BASE_END_POSITION_CODE = 'base_end_position';

type RouteZoneOption = { id: string; label: string; from: number; to: number; location: string };

const props = defineProps<{ config?: BlockConfiguration; map?: any; geoJsonData?: any }>();
const emit = defineEmits<{ (event: 'update:config', value: BlockConfiguration): void }>();
const { t: $t } = useI18n();
const mapStore = useMapStore();
/** Zone hầm/tuyến có Km đầu–cuối để chọn cho tầng (nạp khi mở editor). Khai báo trước watch immediate vì syncSelections dùng tới. */
const routeZones = ref<RouteZoneOption[]>([]);
const draft = ref<BlockConfiguration>(cloneBlockConfiguration(props.config || createEmptyBlockConfiguration()));
const blockSelections = reactive<Record<string, EquipmentSelection>>({});
const levelSelections = reactive<Record<string, EquipmentSelection>>({});
/** Trạng thái thu gọn của từng block (key = block.id). Mặc định thu gọn cho gọn danh sách. */
const collapsedBlocks = reactive<Record<string, boolean>>({});

type BlockSection = 'position' | 'line' | 'levels';
/**
 * Thu gọn từng nhóm trong 1 block (key = `${blockId}:${section}`). Mặc định chỉ mở
 * "Tầng & thiết bị" vì vị trí/đường nối thường chỉnh bằng cách kéo trực tiếp trên bản đồ.
 */
const SECTION_COLLAPSED_BY_DEFAULT: Record<BlockSection, boolean> = { position: true, line: true, levels: false };
const collapsedSections = reactive<Record<string, boolean>>({});

const isSectionCollapsed = (blockId: string, section: BlockSection) => collapsedSections[`${blockId}:${section}`] ?? SECTION_COLLAPSED_BY_DEFAULT[section];

const setSectionCollapsed = (blockId: string, section: BlockSection, collapsed: boolean) => {
  collapsedSections[`${blockId}:${section}`] = collapsed;
};

const availableDevices = computed(() => [...(mapStore.equipmentList || [])].sort((a: any, b: any) => String(a.name || '').localeCompare(String(b.name || ''))));
const availableEquipmentTypes = computed(() =>
  [...(mapStore.equipmentTypeList || [])]
    .filter((type: any) => availableDevices.value.some((device: any) => String(device.equipmentTypeId) === String(type.id)))
    .sort((a: any, b: any) => String(a.name || '').localeCompare(String(b.name || '')))
);
const assignedDeviceIds = computed(() =>
  new Set(draft.value.blocks.flatMap((block) => block.levels.flatMap((level) => level.devices.map((device) => String(device.deviceId)))))
);

const getDevice = (deviceId?: string) => availableDevices.value.find((device: any) => String(device.id) === String(deviceId));
const getBlockSelection = (blockId: string) => (blockSelections[blockId] ||= {});
const getLevelSelection = (levelId: string) => (levelSelections[levelId] ||= { deviceIds: [] });
const getDevicesByType = (equipmentTypeId?: string) =>
  availableDevices.value.filter((device: any) => equipmentTypeId && String(device.equipmentTypeId) === String(equipmentTypeId));
const getAvailableDevicesByType = (equipmentTypeId?: string) =>
  getDevicesByType(equipmentTypeId).filter((device: any) => !assignedDeviceIds.value.has(String(device.id)));

const syncSelections = () => {
  const blockIds = new Set<string>();
  const levelIds = new Set<string>();

  draft.value.blocks.forEach((block) => {
    blockIds.add(block.id);
    if (collapsedBlocks[block.id] === undefined) collapsedBlocks[block.id] = true;
    const selection = getBlockSelection(block.id);
    if (!selection.equipmentTypeId && block.anchorDeviceId) {
      selection.equipmentTypeId = String(getDevice(block.anchorDeviceId)?.equipmentTypeId || '');
    }

    block.levels.forEach((level) => {
      levelIds.add(level.id);
      const selection = getLevelSelection(level.id);
      // Ô nhập khoảng chỉ nạp từ cấu hình lần đầu để không ghi đè giá trị người dùng đang gõ dở
      if (selection.rangeFrom === undefined) selection.rangeFrom = level.rangeFrom ?? null;
      if (selection.rangeTo === undefined) selection.rangeTo = level.rangeTo ?? null;
      if (selection.location === undefined && level.zoneId) selection.location = routeZones.value.find((zone) => zone.id === level.zoneId)?.location || undefined;
    });
  });

  Object.keys(blockSelections).forEach((id) => !blockIds.has(id) && delete blockSelections[id]);
  Object.keys(levelSelections).forEach((id) => !levelIds.has(id) && delete levelSelections[id]);
  Object.keys(collapsedBlocks).forEach((id) => !blockIds.has(id) && delete collapsedBlocks[id]);
  Object.keys(collapsedSections).forEach((key) => !blockIds.has(key.split(':')[0]) && delete collapsedSections[key]);
};

watch(
  () => props.config,
  (value) => {
    draft.value = cloneBlockConfiguration(value || createEmptyBlockConfiguration());
    syncSelections();
  },
  { deep: true, immediate: true }
);

const emitConfiguration = () => emit('update:config', cloneBlockConfiguration(draft.value));

/** Tab con đang mở: danh sách block hay cấu hình hiển thị chung. */
const activeTab = ref<'blocks' | 'display'>('blocks');

/** Tab "Cấu hình hiển thị" đổi tuỳ chọn card → cập nhật bản nháp rồi phát cấu hình mới. */
const handleDisplayConfigUpdate = (value: BlockConfiguration) => {
  draft.value = cloneBlockConfiguration(value);
  emitConfiguration();
};
const createId = (prefix: string) => `${prefix}_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;

const lineStyleOptions = computed(() => [
  { label: $t('lz.label.tmsMapLocation.lineStyleSolid'), value: 'solid' },
  { label: $t('lz.label.tmsMapLocation.lineStyleDashed'), value: 'dashed' },
  { label: $t('lz.label.tmsMapLocation.lineStyleDotted'), value: 'dotted' },
]);

const updateLineStyle = (block: MapBlock, value: BlockLineStyle) => {
  block.lineStyle = value;
  emitConfiguration();
};

/** Màu gợi ý sẵn cho chấm vị trí và đường nối block. */
const colorPresets = ['#409eff', '#67c23a', '#e6a23c', '#f56c6c', '#909399', '#ffffff', '#000000', '#22d3ee'];

/** Xoá toàn bộ điểm bẻ góc để đường nối trở lại đường thẳng. */
const clearWaypoints = (block: MapBlock) => {
  block.waypoints = [];
  emitConfiguration();
};

/** Số icon thiết bị xem nhanh trên tiêu đề thẻ block; còn lại hiện "+n". */
const BLOCK_DEVICE_PREVIEW = 5;

const getBlockName = (block: MapBlock, blockIndex: number) =>
  block.name || $t('lz.label.tmsMapLocation.defaultBlockName', { index: blockIndex + 1 });

const getBlockDeviceCount = (block: MapBlock) => block.levels.reduce((total, level) => total + level.devices.length, 0);

const getBlockDevicePreview = (block: MapBlock) => block.levels.flatMap((level) => level.devices).slice(0, BLOCK_DEVICE_PREVIEW);

const toggleBlock = (blockId: string) => {
  collapsedBlocks[blockId] = !collapsedBlocks[blockId];
};

/** Đưa bản đồ tới card block và nhấp nháy card. */
const locateBlock = (block: MapBlock) => locateBlockOnMap(props.map, block);

// ---- Tìm block theo tên block hoặc tên thiết bị bên trong ----
const blockSearch = ref('');

const isBlockVisible = (block: MapBlock) => {
  const search = blockSearch.value.trim().toLowerCase();
  if (!search) return true;
  if (String(block.name || '').toLowerCase().includes(search)) return true;
  return block.levels.some((level) => level.devices.some((device) => getDeviceName(device.deviceId).toLowerCase().includes(search)));
};

const visibleBlockCount = computed(() => draft.value.blocks.filter(isBlockVisible).length);

const isAllCollapsed = computed(() => draft.value.blocks.length > 0 && draft.value.blocks.every((block) => collapsedBlocks[block.id]));

const toggleAllBlocks = () => {
  const shouldCollapse = !isAllCollapsed.value;
  draft.value.blocks.forEach((block) => (collapsedBlocks[block.id] = shouldCollapse));
};

const addBlock = () => {
  const center = props.map?.getCenter?.() || { lat: 0, lng: 0 };
  const index = draft.value.blocks.length + 1;
  const block = createMapBlock({ lat: center.lat, lng: center.lng }, index);
  block.name = $t('lz.label.tmsMapLocation.defaultBlockName', { index });
  block.levels[0].name = $t('lz.label.tmsMapLocation.defaultLevelName', { index: 1 });
  draft.value.blocks.push(block);
  syncSelections();
  // Block vừa thêm luôn mở sẵn để người dùng bố trí thiết bị ngay.
  collapsedBlocks[block.id] = false;
  emitConfiguration();
};

const removeBlock = (index: number) => {
  draft.value.blocks.splice(index, 1);
  syncSelections();
  emitConfiguration();
};

const addLevel = (block: MapBlock) => {
  const index = block.levels.length + 1;
  const level = { id: createId('level'), name: $t('lz.label.tmsMapLocation.defaultLevelName', { index }), devices: [] };
  block.levels.push(level);
  getLevelSelection(level.id);
  emitConfiguration();
};

const removeLevel = (block: MapBlock, index: number) => {
  block.levels.splice(index, 1);
  syncSelections();
  emitConfiguration();
};

const removeDevice = (level: BlockLevel, index: number) => {
  level.devices.splice(index, 1);
  emitConfiguration();
};

/** Bật/tắt lật ngang icon của riêng thiết bị này trong block. */
const toggleDeviceFlip = (device: BlockDevice) => {
  if (device.flipX) delete device.flipX;
  else device.flipX = true;
  emitConfiguration();
};

const handleAnchorEquipmentTypeChange = (block: MapBlock) => {
  block.anchorDeviceId = undefined;
  emitConfiguration();
};

const handleLevelEquipmentTypeChange = (level: BlockLevel) => {
  getLevelSelection(level.id).deviceIds = [];
};

/** Lý trình (mét) của thiết bị; chưa có Km = null. */
const getDeviceRoutePosition = (deviceId: string) => getEquipmentRoutePosition(getDevice(deviceId));

const getDeviceKmLabel = (deviceId: string) => {
  const position = getDeviceRoutePosition(deviceId);
  return position === null ? $t('lz.label.tmsMapLocation.levelRouteNoKm') : formatRouteKm(position);
};

/** Chọn mọi thiết bị cùng loại chưa được gán vào block nào (vẫn phải bấm Thêm để áp dụng). */
const selectAllAvailableDevices = (level: BlockLevel) => {
  const selection = getLevelSelection(level.id);
  selection.deviceIds = getAvailableDevicesByType(selection.equipmentTypeId).map((device: any) => String(device.id));
};

/** Thêm các thiết bị đã chọn vào tầng, sắp theo Km tăng dần (chưa có Km xếp cuối) để danh sách dễ đối chiếu. */
const addSelectedDevices = (level: BlockLevel) => {
  const selection = getLevelSelection(level.id);
  const ids = (selection.deviceIds || []).map(String).filter((id) => !assignedDeviceIds.value.has(id));
  if (!ids.length) return;

  ids
    .map((id, index) => ({ id, index, position: getDeviceRoutePosition(id) }))
    .sort((a, b) => (a.position ?? Infinity) - (b.position ?? Infinity) || a.index - b.index)
    .forEach(({ id }) => level.devices.push({ id: createId('block-device'), deviceId: id }));
  selection.deviceIds = [];
  emitConfiguration();
};

/** Bật/tắt bố trí theo Km của tầng; tắt thì bỏ luôn tuỳ chọn đảo chiều để config gọn như cũ. */
const setLevelLayout = (level: BlockLevel, enabled: boolean) => {
  if (enabled) {
    level.layout = 'route';
  } else {
    delete level.layout;
    delete level.routeReverse;
    delete level.rangeFrom;
    delete level.rangeTo;
    delete level.zoneId;
    delete level.directionId;
    delete level.splitRowsByType;
    Object.assign(getLevelSelection(level.id), { rangeFrom: null, rangeTo: null, location: undefined });
  }
  emitConfiguration();
};

const setLevelSplitRowsByType = (level: BlockLevel, split: boolean) => {
  if (split) level.splitRowsByType = true;
  else delete level.splitRowsByType;
  emitConfiguration();
};

const setLevelRouteReverse = (level: BlockLevel, reverse: boolean) => {
  if (reverse) level.routeReverse = true;
  else delete level.routeReverse;
  emitConfiguration();
};

/** "Km0+500 → Km1+700 · 1200 m" — khoảng cố định của tầng hoặc tự đo; kèm số thiết bị chưa có Km (nếu có). */
const getLevelRouteSummary = (level: BlockLevel) => {
  if (level.rangeFrom !== undefined && level.rangeTo !== undefined) {
    const missingCount = level.devices.filter((device) => getDeviceRoutePosition(device.deviceId) === null).length;
    const fixedText = `${formatRouteKm(level.rangeFrom)} → ${formatRouteKm(level.rangeTo)} · ${level.rangeTo - level.rangeFrom} m`;
    return missingCount ? `${fixedText} · ${$t('lz.label.tmsMapLocation.levelRouteNoKmCount', { count: missingCount })}` : fixedText;
  }
  const positions = level.devices.map((device) => getDeviceRoutePosition(device.deviceId));
  const known = positions.filter((position): position is number => position !== null);
  const missing = positions.length - known.length;
  if (!known.length) return missing ? $t('lz.label.tmsMapLocation.levelRouteNoKmCount', { count: missing }) : '';
  const start = Math.min(...known);
  const end = Math.max(...known);
  const text = `${formatRouteKm(start)} → ${formatRouteKm(end)} · ${end - start} m`;
  return missing ? `${text} · ${$t('lz.label.tmsMapLocation.levelRouteNoKmCount', { count: missing })}` : text;
};

// #region Khoảng Km cố định + tự điền thiết bị theo zone/vị trí/hướng

const routeZoneOptions = computed(() => routeZones.value);

const toPositionMeters = (km: any, met: any): number | null => {
  if ((km === null || km === undefined || km === '') && (met === null || met === undefined || met === '')) return null;
  const value = Number(km ?? 0) * 1000 + Number(met ?? 0);
  return Number.isFinite(value) ? value : null;
};

/** Nạp danh sách zone hầm/tuyến có Km đầu–cuối (zone cha như "Hầm" không có Km nên bị loại). Lỗi mạng thì bỏ trống, vẫn nhập tay được. */
const loadRouteZones = async () => {
  try {
    const response = await getAPI(TmsZoneApi).apiTmsTmszoneListGet();
    const zones: any[] = response?.data?.result || [];
    routeZones.value = zones
      .filter((zone) => ['tunnel', 'route'].includes(String(zone?.type || '').toLowerCase()))
      .map((zone) => {
        const from = toPositionMeters(zone.fromKmNumber, zone.fromMetNumber);
        const to = toPositionMeters(zone.toKmNumber, zone.toMetNumber);
        if (from === null || to === null || from === to) return null;
        // Hầm chạy ngược (Km giảm dần) vẫn cho khoảng min → max; chiều hiển thị do "Đảo chiều" quyết định
        const start = Math.min(from, to);
        const end = Math.max(from, to);
        const location = zone.location === null || zone.location === undefined ? '' : String(zone.location);
        return { id: String(zone.id), label: `${zone.name} · ${formatRouteKm(start)} → ${formatRouteKm(end)}`, from: start, to: end, location };
      })
      .filter((zone): zone is RouteZoneOption => zone !== null)
      .sort((a, b) => a.label.localeCompare(b.label));
    syncSelections();
  } catch (error) {
    console.error('BlockEditor.loadRouteZones', error);
  }
};

onMounted(loadRouteZones);

const baseConfig = useBaseConfig();
onMounted(() => baseConfig.checkConfigDataList());

/**
 * Lựa chọn lọc hiển thị TÊN theo danh mục cấu hình (tms_zone_location, direction_config_type), value vẫn là mã
 * khớp với Location / DirectionId của thiết bị. Mã có trên thiết bị nhưng chưa khai báo danh mục thì hiện chính mã đó.
 */
const buildConfigOptions = (configType: string, deviceCodes: string[]) => {
  const options = new Map<string, string>();
  (baseConfig.getConfigDataByCode(configType) || []).forEach((item: any) => {
    if (item?.code !== undefined && item?.code !== null && item.code !== '') options.set(String(item.code), baseConfig.getConfigItemName(configType, String(item.code)));
  });
  deviceCodes.forEach((code) => options.has(code) || options.set(code, code));
  return [...options].map(([value, label]) => ({ value, label }));
};

const locationOptions = computed(() =>
  buildConfigOptions(
    BaseConfigTypeEnum.TmsZoneLocation,
    availableDevices.value.map((device: any) => device.location).filter((value: any) => value !== null && value !== undefined && value !== '').map(String)
  )
);
/** Hướng 0 = "Tất cả" trong danh mục nên không đưa vào lọc (bỏ trống đã là mọi hướng); value là số để so với DirectionId. */
const directionOptions = computed(() =>
  buildConfigOptions(
    BaseConfigTypeEnum.TmsDirectionType,
    availableDevices.value.map((device: any) => Number(device.directionId)).filter((value: number) => Number.isFinite(value) && value > 0).map(String)
  )
    .filter((option) => Number(option.value) > 0)
    .map((option) => ({ value: Number(option.value), label: option.label }))
);

const setLevelRange = (level: BlockLevel, from: number | null, to: number | null) => {
  const selection = getLevelSelection(level.id);
  selection.rangeFrom = from;
  selection.rangeTo = to;
  if (from !== null && to !== null && to > from) {
    level.rangeFrom = Math.round(from);
    level.rangeTo = Math.round(to);
  } else {
    delete level.rangeFrom;
    delete level.rangeTo;
  }
  emitConfiguration();
};

/** Ô nhập tay đổi: đủ 2 giá trị và đầu < cuối thì áp dụng, ngược lại bỏ khoảng cố định (tự đo từ thiết bị). */
const commitLevelRange = (level: BlockLevel) => {
  const selection = getLevelSelection(level.id);
  setLevelRange(level, selection.rangeFrom ?? null, selection.rangeTo ?? null);
};

/** Chọn zone hầm/tuyến: điền khoảng Km + Location lọc thiết bị; bỏ chọn chỉ gỡ liên kết zone, giữ khoảng đang dùng. */
const setLevelZone = (level: BlockLevel, zoneId: string) => {
  const zone = routeZones.value.find((item) => item.id === zoneId);
  if (!zone) {
    delete level.zoneId;
    emitConfiguration();
    return;
  }
  level.zoneId = zone.id;
  getLevelSelection(level.id).location = zone.location || undefined;
  setLevelRange(level, zone.from, zone.to);
};

/** Dùng khoảng cơ sở (SysOpConfig base_start_position/base_end_position) cho tầng, vd đoạn ngoài hầm. */
const applyBaseRange = async (level: BlockLevel) => {
  await ensureOpConfigLoaded();
  const from = readOpConfigIntOrNull(BASE_START_POSITION_CODE);
  const to = readOpConfigIntOrNull(BASE_END_POSITION_CODE);
  if (from === null || to === null || to <= from) {
    ElMessage.warning($t('lz.message.tmsMapLocation.levelRangeBaseMissing'));
    return;
  }
  delete level.zoneId;
  setLevelRange(level, from, to);
};

const setLevelDirection = (level: BlockLevel, value: number | '') => {
  if (value === '' || value === null || value === undefined) delete level.directionId;
  else level.directionId = Number(value);
  emitConfiguration();
};

/**
 * Tự điền: thiết bị chưa gán block nào, khớp Location/hướng đang chọn và có Km nằm trong khoảng của tầng
 * (khi tầng có khoảng). Thiết bị thiếu Km/Location/hướng không bị tự điền (vẫn thêm tay được).
 * - Đã chọn loại thiết bị: chỉ lấy loại đó; chưa chọn: lấy mọi loại khớp bộ lọc.
 * - TẤT CẢ vào CHÍNH tầng này (cùng 1 thanh, cùng độ rộng). Có từ 2 loại thiết bị trở lên thì tầng tự bật
 *   "Tách dòng theo loại": mỗi loại nằm ở dòng riêng, Km vẫn thẳng hàng giữa các dòng. Tắt ô đó nếu muốn xếp chung theo Km.
 */
const autoFillDevices = (level: BlockLevel) => {
  const selection = getLevelSelection(level.id);
  const hasRange = level.rangeFrom !== undefined && level.rangeTo !== undefined;
  const candidates = selection.equipmentTypeId
    ? getAvailableDevicesByType(selection.equipmentTypeId)
    : availableDevices.value.filter((device: any) => !assignedDeviceIds.value.has(String(device.id)));
  const matched = candidates.filter((device: any) => {
    if (selection.location && String(device.location ?? '') !== selection.location) return false;
    if (level.directionId !== undefined && Number(device.directionId) !== level.directionId) return false;
    if (hasRange) {
      const position = getEquipmentRoutePosition(device);
      if (position === null || position < (level.rangeFrom as number) || position > (level.rangeTo as number)) return false;
    }
    return true;
  });
  if (!matched.length) {
    ElMessage.info($t('lz.message.tmsMapLocation.levelAutoFillNone'));
    return;
  }

  const typeName = (device: any) =>
    (mapStore.equipmentTypeList || []).find((type: any) => String(type.id) === String(device.equipmentTypeId))?.name || $t('lz.label.base.unknown');
  // Theo tên loại rồi Km tăng dần (chưa có Km xếp cuối) để danh sách trong tầng dễ đối chiếu
  matched
    .map((device: any, index: number) => ({ id: String(device.id), index, type: String(typeName(device)), position: getEquipmentRoutePosition(device) }))
    .sort((a, b) => a.type.localeCompare(b.type) || (a.position ?? Infinity) - (b.position ?? Infinity) || a.index - b.index)
    .forEach(({ id }) => level.devices.push({ id: createId('block-device'), deviceId: id }));

  // Cả tầng (kể cả thiết bị đã có từ trước) có từ 2 loại → tách dòng theo loại
  const typeCount = new Set(level.devices.map((item) => String(getDevice(item.deviceId)?.equipmentTypeId ?? ''))).size;
  if (typeCount > 1) level.splitRowsByType = true;

  emitConfiguration();
  ElMessage.success($t('lz.message.tmsMapLocation.levelAutoFillDone', { count: matched.length, types: typeCount }));
};

// #endregion

const setAnchorFromDevice = (block: MapBlock) => {
  const feature = props.geoJsonData?.features?.find((item: any) =>
    item?.geometry?.type === 'Point' &&
    item?.properties?.itemGroup === 'device' &&
    String(item?.properties?.id) === String(block.anchorDeviceId)
  );
  if (feature?.geometry?.coordinates?.length === 2) {
    block.actualPosition = {
      lat: Number(feature.geometry.coordinates[1]),
      lng: Number(feature.geometry.coordinates[0]),
    };
  }
  emitConfiguration();
};

const getDeviceName = (deviceId: string) => getDevice(deviceId)?.name || $t('lz.label.base.unknown');

/** Màn thiết lập block luôn là chế độ cấu hình → icon mặc định, giống marker đặt lẻ. */
const getDeviceIcon = (device: any) => resolveDeviceIconUrl(String(device.id), true);

const getDeviceIconById = (deviceId: string) => {
  const device = getDevice(deviceId);
  return device ? getDeviceIcon(device) : '';
};

const hideBrokenImage = (event: Event) => {
  (event.target as HTMLImageElement).style.display = 'none';
};
</script>

<style scoped>

/* ===== Tiêu đề + thao tác chung ===== */
.block-editor__heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; }
.block-editor__heading-text { min-width: 0; }
.block-editor__heading-actions { display: flex; align-items: center; gap: 6px; flex: 0 0 auto; }
.block-editor__hint { color: var(--el-text-color-secondary); font-size: 12px; line-height: 1.45; margin-top: 2px; }
.block-editor__linked-selects { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); gap: 8px; width: 100%; }

/* ===== Thẻ block: tiêu đề bấm để mở/thu gọn + thân chứa các nhóm cài đặt ===== */
.block-editor__search :deep(.el-input__wrapper) { border-radius: 8px; background: var(--el-fill-color-lighter); }
.block-editor__no-match { padding: 20px 12px; border: 1px dashed var(--el-border-color); border-radius: 10px; color: var(--el-text-color-secondary); font-size: 12px; text-align: center; }

.block-card { border: 1px solid var(--el-border-color-lighter); border-radius: 12px; background: var(--el-bg-color); box-shadow: 0 1px 3px rgba(15, 23, 42, 0.04); transition: border-color 0.2s ease, box-shadow 0.2s ease; }
.block-card:hover { border-color: var(--el-border-color); box-shadow: 0 2px 8px rgba(15, 23, 42, 0.06); }
.block-card.is-open { border-color: var(--el-color-primary-light-5); box-shadow: 0 0 0 3px var(--el-color-primary-light-9), 0 2px 8px rgba(15, 23, 42, 0.06); }

.block-card__head { display: flex; align-items: center; gap: 10px; min-height: 52px; padding: 8px 8px 8px 10px; border-radius: 12px; cursor: pointer; user-select: none; }
.block-card.is-open .block-card__head { border-bottom: 1px solid var(--el-border-color-extra-light); border-radius: 12px 12px 0 0; background: var(--el-fill-color-lighter); }
.block-card__head:focus-visible { outline: 2px solid var(--el-color-primary); outline-offset: -2px; }
.block-card__chevron { flex: 0 0 auto; color: var(--el-text-color-placeholder); font-size: 12px; transition: transform 0.2s ease; }
.block-card.is-open .block-card__chevron { transform: rotate(90deg); color: var(--el-color-primary); }
.block-card__index { display: inline-flex; flex: 0 0 auto; align-items: center; justify-content: center; width: 26px; height: 26px; border-radius: 8px; background: var(--el-color-primary-light-9); color: var(--el-color-primary); font-size: 12px; font-weight: 700; }
.block-card.is-open .block-card__index { background: var(--el-color-primary); color: #ffffff; }
.block-card__info { flex: 1; min-width: 0; }
.block-card__name { overflow: hidden; color: var(--el-text-color-primary); font-size: 13px; font-weight: 700; line-height: 1.35; text-overflow: ellipsis; white-space: nowrap; }
.block-card__meta { display: flex; flex-wrap: wrap; gap: 4px 10px; margin-top: 2px; }
.block-card__stat { display: inline-flex; align-items: center; gap: 3px; color: var(--el-text-color-secondary); font-size: 11px; }
.block-card__stat.is-empty { color: var(--el-color-warning); }

/* Dải icon thiết bị xếp chồng nhẹ lên nhau */
.block-card__devices { display: flex; flex: 0 0 auto; align-items: center; }
.block-card__device { display: inline-flex; align-items: center; justify-content: center; width: 24px; height: 24px; margin-left: -6px; overflow: hidden; border: 2px solid var(--el-bg-color); border-radius: 50%; background: var(--el-fill-color-light); color: var(--el-text-color-secondary); font-size: 10px; font-weight: 700; box-shadow: 0 0 0 1px var(--el-border-color-lighter); }
.block-card__device:first-child { margin-left: 0; }
.block-card__device img { width: 16px; height: 16px; object-fit: contain; }
.block-card__device--more { background: var(--el-color-primary-light-9); color: var(--el-color-primary); }

.block-card__actions { display: flex; flex: 0 0 auto; align-items: center; gap: 2px; }
.block-card__btn { width: 26px; height: 26px; margin: 0 !important; padding: 0; border-radius: 6px; color: var(--el-text-color-secondary); }
.block-card__btn:hover { background: var(--el-color-primary-light-9); color: var(--el-color-primary); }
.block-card__btn--danger:hover { background: var(--el-color-danger-light-9); color: var(--el-color-danger); }

.block-card__body { padding: 10px; }
.block-card__name-field { display: flex; flex-direction: column; gap: 4px; margin-bottom: 10px; }
.block-card__name-field > span { color: var(--el-text-color-secondary); font-size: 11px; font-weight: 600; }

.block-form__waypoint-count { min-width: 24px; padding: 1px 7px; border-radius: 999px; background: var(--el-color-primary-light-9); color: var(--el-color-primary); font-size: 12px; font-weight: 700; text-align: center; }
.block-form__add-level { width: 100%; margin-top: 8px; border-style: dashed; }

/* Nhóm con trong 1 block: Vị trí / Đường nối / Tầng & thiết bị */

/* ===== Tab con: Thiết lập block / Cấu hình hiển thị ===== */
.block-editor__tabs :deep(.el-tabs__header) { margin-bottom: 10px; }
.block-editor__tabs :deep(.el-tabs__item) { height: 34px; padding: 0 14px; font-size: 13px; }
.block-editor__tab-label { display: inline-flex; align-items: center; gap: 6px; }
.block-editor__tab-count { min-width: 18px; padding: 0 6px; border-radius: 999px; background: var(--el-color-primary-light-9); color: var(--el-color-primary); font-size: 11px; font-weight: 700; line-height: 18px; text-align: center; }
.block-editor__pane { display: flex; flex-direction: column; gap: 12px; }
/* Nhóm con trong 1 block dùng SettingsSection; thu nhỏ đệm cho vừa Fieldset */
.block-section :deep(.settings-section__body) { padding: 8px 10px 10px; }
.block-section + .block-section { margin-top: 8px; }
.block-section__hint { display: flex; align-items: flex-start; gap: 4px; margin-top: 8px; color: var(--el-text-color-secondary); font-size: 11px; line-height: 1.45; }
.block-section__hint .el-icon { flex: 0 0 auto; margin-top: 2px; color: var(--el-color-info); }

/* Toạ độ: bảng 3 cột (nhãn | Vĩ độ | Kinh độ) */
.block-coords { display: grid; grid-template-columns: auto minmax(0, 1fr) minmax(0, 1fr); align-items: center; gap: 6px 8px; }
.block-coords__axis { color: var(--el-text-color-secondary); font-size: 11px; font-weight: 600; text-align: center; }
.block-coords__label { display: flex; align-items: center; gap: 6px; color: var(--el-text-color-regular); font-size: 12px; white-space: nowrap; }
.block-coords__dot { display: inline-block; width: 9px; height: 9px; border-radius: 50%; }
.block-coords__dot--actual { background: var(--el-text-color-primary); box-shadow: 0 0 0 2px var(--el-bg-color), 0 0 0 3px var(--el-border-color); }
.block-coords__dot--display { border: 1.5px solid var(--el-color-primary); border-radius: 2px; background: var(--el-color-primary-light-9); }
.block-coords :deep(.el-input-number) { width: 100%; }

/* Đường nối & chấm vị trí: nhãn bên trái, điều khiển gọn bên phải */
.block-style-grid { display: grid; grid-template-columns: minmax(96px, max-content) minmax(0, 1fr); align-items: center; gap: 8px 10px; }
.block-style-grid__label { color: var(--el-text-color-regular); font-size: 12px; }
.block-style-grid__controls { display: flex; align-items: center; gap: 6px; min-width: 0; }
.block-style-grid__number { width: 96px !important; }
.block-style-grid__unit { color: var(--el-text-color-secondary); font-size: 11px; }

/* Tầng & thiết bị */
.block-levels { display: flex; flex-direction: column; gap: 8px; }
.block-level { padding: 8px 8px 6px 10px; border: 1px solid var(--el-border-color-lighter); border-left: 3px solid var(--el-color-primary-light-5); border-radius: 6px; background: var(--el-fill-color-blank); }
.block-level__header { display: flex; align-items: center; gap: 8px; }
.block-level__header .el-input { flex: 1; }
.block-level__drag { display: inline-flex; color: var(--el-text-color-placeholder); cursor: grab; }
.block-level__drag:hover { color: var(--el-color-primary); }
.block-level__index { display: inline-flex; flex: 0 0 auto; align-items: center; justify-content: center; width: 20px; height: 20px; border-radius: 6px; background: var(--el-fill-color); color: var(--el-text-color-regular); font-size: 11px; font-weight: 700; }
.block-level__count { display: inline-flex; flex: 0 0 auto; align-items: center; gap: 3px; color: var(--el-text-color-secondary); font-size: 11px; }
.block-level__add-device { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto auto; gap: 6px; margin-top: 7px; }
.block-level__add-device .el-button + .el-button { margin-left: 0; }
.block-level__layout { display: flex; flex-wrap: wrap; align-items: center; gap: 4px 12px; margin-top: 7px; }
.block-level__layout-hint { color: var(--el-text-color-secondary); font-size: 11px; }
.block-level__range { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; margin-top: 7px; }
.block-level__range-zone { flex: 1 1 180px; min-width: 140px; }
.block-level__range-input { flex: 0 1 130px; width: 130px; }
.block-level__range .el-button + .el-button { margin-left: 0; }
.block-device__km { color: var(--el-text-color-secondary); font-size: 10px; white-space: nowrap; }
.block-device__km.is-missing { color: var(--el-color-warning); }
.block-level__devices { min-height: 38px; display: flex; flex-wrap: wrap; gap: 6px; padding: 6px 0; }
.block-device { display: inline-flex; align-items: center; gap: 5px; min-width: 0; max-width: 210px; padding: 3px 5px 3px 6px; border: 1px solid var(--el-color-success-light-5); border-radius: 4px; background: var(--el-bg-color); color: var(--el-text-color-regular); font-size: 12px; cursor: grab; }
.block-device span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.block-device img.is-flipped { transform: scaleX(-1); }
.block-device__flip.el-button { margin-left: 0; padding: 0 2px; }
.block-device img { width: 18px; height: 18px; object-fit: contain; flex: 0 0 auto; }
.block-device .el-button { margin-left: -2px; }
.block-device--ghost { opacity: 0.45; background: var(--el-fill-color); }
.block-level__drop-placeholder { width: 100%; padding: 8px; border: 1px dashed var(--el-border-color); border-radius: 4px; color: var(--el-text-color-secondary); font-size: 12px; text-align: center; }

@media (max-width: 640px) {
  .block-editor__linked-selects, .block-level__add-device { grid-template-columns: 1fr; }
  .block-coords { grid-template-columns: 1fr 1fr; }
  .block-coords > span:first-child { display: none; }
  .block-coords__label { grid-column: 1 / -1; }
}
</style>
