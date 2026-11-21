<template>
    <InfoPanel
        v-model:active-tab="state.activeTab"
        :tabs="tabs"
        :device-name="state.data?.code"
        :device-location="deviceLocation"
        :status-variant="statusVariant"
        :show-footer="state.activeTab === 'controlTab'"
        @closed="emits('closed')"
        @close-others="emits('closeOthers')"
        @close-all="emits('closeAll')"
    >
        <!-- Info -->
        <template #infoTab>
            <div class="content-text">
                <div class="border-traffic-info">
                    <h3 class="section-title">{{ $t('lz.label.vms.deviceSumary') }}</h3>
                    <div class="background-summary-device">
                        <span :style="{ color: '#000' }"> {{ $t('lz.label.base.status') }}: </span>
                        <div class="status-indicator">
                            <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.data?.equipmentState)" />
                        </div>
                    </div>
                </div>

                <div class="border-traffic-info mt10">
                    <h3 class="section-title">{{ $t('lz.label.vms.deviceInformation') }}</h3>
                    <div class="info-grid">
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.code') }}</span>
                            <span class="info-v">{{ state.data?.code || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.base.name') }}</span>
                            <span class="info-v">{{ state.data?.name || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.tms.location') }}</span>
                            <span class="info-v">Km{{ state.data?.kmNumber ?? '-' }}+{{ state.data?.metNumber ?? '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.entity.vms.ip') }}</span>
                            <span class="info-v">{{ state.data?.ip || '-' }}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-k">{{ $t('lz.label.trafficLight.currentColor') }}</span>
                            <span class="info-v">{{ lightText }}</span>
                        </div>
                    </div>
                </div>
            </div>
        </template>

        <!-- Điều khiển đèn tín hiệu giao thông: mỗi đèn điều khiển riêng, chỉ đỏ / xanh -->
        <template #controlTab>
            <div class="content-text traffic-light-control">
                <!-- Trạng thái hiện tại: đầu đèn 2 bóng + tên đèn + màu -->
                <div class="traffic-light-status" :class="`traffic-light-status--${lightVariant}`">
                    <div class="traffic-light-head" aria-hidden="true">
                        <span class="traffic-light-lamp traffic-light-lamp--red" :class="{ 'is-lit': lightState === 'red' }" />
                        <span class="traffic-light-lamp traffic-light-lamp--green" :class="{ 'is-lit': lightState === 'green' }" />
                    </div>
                    <div class="traffic-light-status__info">
                        <span class="traffic-light-status__label">{{ $t('lz.label.trafficLight.signalLight') }}</span>
                        <span class="traffic-light-status__name" :title="deviceName">{{ deviceName }}</span>
                        <span class="traffic-light-status__value">
                            <i class="traffic-light-status__dot" />{{ lightText }}
                        </span>
                    </div>
                </div>

                <!-- Lệnh điều khiển: áp dụng cho các đèn đang chọn trong danh sách bên dưới; mỗi nút hỏi xác nhận trước khi gửi -->
                <div class="traffic-light-commands">
                    <el-button
                        v-for="item in TRAFFIC_LIGHT_COMMANDS"
                        :key="item.command"
                        round
                        plain
                        :type="item.variant"
                        class="traffic-light-command"
                        :loading="state.runningCommand === item.command"
                        :disabled="!state.selectedIds.length || (!!state.runningCommand && state.runningCommand !== item.command)"
                        @click="handleCommand(item)"
                    >
                        {{ $t(item.labelKey) }}
                    </el-button>
                </div>

                <!-- Danh sách đèn tín hiệu của các hầm (sơ đồ tuyến như modal VMS): bấm để chọn nhiều đèn -->
                <div class="border-traffic-info traffic-light-list">
                    <TrafficLightRouteMap v-model="state.selectedIds" :lanes="lanes" :start="state.rangeStart" :end="state.rangeEnd" />
                </div>
            </div>
        </template>

        <template #footer>
            <el-button round @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="trafficLightInfoModal">
import { reactive, computed, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { auth } from '/@/utils/authFunction';
import { TRAFFIC_LIGHT_COMMANDS, isTrafficLightDevice, useTrafficLightControlStore } from '/@/stores/its/trafficLightControl';
import type { TrafficLightCommandDefinition } from '/@/stores/its/trafficLightControl';
import { useMapStore } from '/@/stores/its/mapStore';
import { ensureOpConfigLoaded, readOpConfigIntOrNull } from '/@/components/vms/controlVms/composables/useOpConfig';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';
import TrafficLightRouteMap from './trafficLightRouteMap.vue';
import type { TrafficLightMapLane } from './trafficLightRouteMap.vue';

const { t: $t } = useI18n();
const trafficLightStore = useTrafficLightControlStore();
const mapStore = useMapStore();
const baseConfig = useBaseConfig();
const getConfigItemByCode = baseConfig.getConfigItemByCode;

/** Khoảng lý trình cơ sở (SysOpConfig, mét = km*1000+m) làm trục của sơ đồ; chưa cấu hình thì sơ đồ tự lấy theo đèn xa nhất. */
const BASE_START_POSITION_CODE = 'base_start_position';
const BASE_END_POSITION_CODE = 'base_end_position';
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const state = reactive({
    activeTab: 'controlTab',
    data: null as any,
    /** Lệnh đang gửi — khoá nút còn lại tới khi xong. */
    runningCommand: '' as string,
    /** Các đèn đang chọn để điều khiển (mặc định = đèn vừa bấm trên bản đồ). */
    selectedIds: [] as string[],
    rangeStart: null as number | null,
    rangeEnd: null as number | null,
});

/**
 * Mọi đèn tín hiệu SS gom theo vị trí (Location = hầm phải / hầm trái / tuyến... lấy TÊN từ danh mục tms_zone_location),
 * mỗi nhóm là 1 làn trên sơ đồ. Đèn chưa khai báo Location vào nhóm "Khác" ở cuối.
 */
const lanes = computed<TrafficLightMapLane[]>(() => {
    const groups = new Map<string, TrafficLightMapLane>();
    (mapStore.equipmentList || [])
        .filter((equipment: any) => isTrafficLightDevice(String(equipment.id)))
        .forEach((equipment: any) => {
            const location = equipment.location === null || equipment.location === undefined ? '' : String(equipment.location);
            if (!groups.has(location)) {
                const name = location ? baseConfig.getConfigItemName(BaseConfigTypeEnum.TmsZoneLocation, location) : $t('lz.label.trafficLight.otherLocation');
                groups.set(location, { key: location, name, items: [] });
            }
            const id = String(equipment.id);
            groups.get(location)!.items.push({
                id,
                name: equipment.name || equipment.code || id,
                position: (Number(equipment.kmNumber) || 0) * 1000 + (Number(equipment.metNumber) || 0),
                color: trafficLightStore.getLightState(id),
            });
        });
    return [...groups.values()].sort((a, b) => (a.key === '' ? 1 : b.key === '' ? -1 : a.key.localeCompare(b.key, undefined, { numeric: true })));
});

// Cùng quyền điều khiển thiết bị với VMS/LCS/Barrier: không có quyền chỉ xem tab Thông tin.
const DEVICE_CONTROL_AUTH = 'tmsTrafficInfo:control';
const canControl = computed(() => auth(DEVICE_CONTROL_AUTH));

const tabs = computed(() => {
    const list = [{ label: $t('lz.label.base.info'), name: 'infoTab' }];
    if (canControl.value) list.push({ label: $t('lz.label.base.control'), name: 'controlTab' });
    return list;
});

const deviceLocation = computed(() => `Km ${state.data?.kmNumber ?? ''} + ${state.data?.metNumber ?? ''}`);
const statusVariant = computed(() => getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.data?.equipmentState)?.value || '');
const deviceName = computed(() => state.data?.name || state.data?.code || '-');

const lightState = computed(() => trafficLightStore.getLightState(state.data?.id));
const lightVariant = computed(() => (lightState.value === 'green' ? 'success' : lightState.value === 'red' ? 'danger' : 'unknown'));
const lightText = computed(() => {
    if (lightState.value === 'green') return $t('lz.label.trafficLight.green');
    if (lightState.value === 'red') return $t('lz.label.trafficLight.red');
    return $t('lz.label.trafficLight.unknown');
});

/** Mở panel với thiết bị được bấm (equipmentModal truyền bản ghi thiết bị). */
const openInfo = async (data: any) => {
    state.data = data;
    state.runningCommand = '';
    state.selectedIds = data?.id ? [String(data.id)] : [];
    state.activeTab = canControl.value ? 'controlTab' : 'infoTab';

    // Trục sơ đồ theo cấu hình cơ sở; lỗi/chưa cấu hình thì để null (sơ đồ tự lấy theo đèn xa nhất)
    await ensureOpConfigLoaded();
    state.rangeStart = readOpConfigIntOrNull(BASE_START_POSITION_CODE);
    state.rangeEnd = readOpConfigIntOrNull(BASE_END_POSITION_CODE);
};

/** Xác nhận → gửi lệnh tới TẤT CẢ đèn đang chọn → báo kết quả. Huỷ xác nhận thì không làm gì. */
const handleCommand = async (definition: TrafficLightCommandDefinition) => {
    const ids = [...state.selectedIds];
    if (!ids.length || state.runningCommand) return;
    const commandName = $t(definition.labelKey);
    const nameOf = (id: string) => (mapStore.equipmentList || []).find((item: any) => String(item.id) === id)?.name || id;
    const target = ids.length === 1 ? nameOf(ids[0]) : $t('lz.message.trafficLight.multiTarget', { count: ids.length });

    try {
        await ElMessageBox.confirm($t('lz.message.trafficLight.confirmCommand', { command: commandName, name: target }), $t('lz.message.base.warning'), {
            type: 'warning',
            confirmButtonText: $t('lz.button.base.confirm'),
            cancelButtonText: $t('lz.button.base.cancel'),
        });
    } catch {
        return; // người dùng huỷ
    }

    state.runningCommand = definition.command;
    try {
        // Gửi song song; từng đèn lỗi không làm hỏng các đèn còn lại
        const results = await Promise.allSettled(ids.map((id) => trafficLightStore.sendCommand(id, definition.command)));
        const failed = results.map((result, index) => (result.status === 'rejected' ? ids[index] : null)).filter((id): id is string => id !== null);
        failed.forEach((id) => console.error('Error sending traffic light command:', id));
        if (!failed.length) ElMessage.success($t('lz.message.trafficLight.commandSuccess', { command: commandName }));
        else if (failed.length === ids.length) ElMessage.error($t('lz.message.trafficLight.commandFailed', { command: commandName }));
        else ElMessage.warning($t('lz.message.trafficLight.commandPartial', { command: commandName, success: ids.length - failed.length, total: ids.length, names: failed.map(nameOf).join(', ') }));
    } finally {
        state.runningCommand = '';
    }
};

const cancel = () => emits('closed');

defineExpose({ openInfo });
</script>

<style scoped lang="scss">
@import '/@/theme/its/trafficInfo.scss';

.content-text {
    width: 100%;
    white-space: normal;
    word-wrap: break-word;
}

.traffic-light-control {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

/* Thẻ trạng thái: đầu đèn ở GIỮA, tên + màu hiện tại bên dưới */
.traffic-light-status {
    --traffic-light-color: #e8f2ff;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    padding: 12px 16px;
    border: 1px solid rgba(125, 165, 255, 0.4);
    border-top: 4px solid var(--traffic-light-color);
    border-radius: 8px;
    background: rgba(125, 165, 255, 0.1);
    text-align: center;
}
.traffic-light-status--success { --traffic-light-color: var(--el-color-success); }
.traffic-light-status--danger { --traffic-light-color: var(--el-color-danger); }

/* Đầu đèn: hộp đen 2 bóng đỏ (trên) / xanh (dưới); bóng đang sáng phát sáng, bóng còn lại mờ */
.traffic-light-head {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 8px;
    border-radius: 14px;
    background: #1f2937;
    box-shadow: inset 0 0 0 2px #374151;
}
.traffic-light-lamp {
    width: 30px;
    height: 30px;
    border-radius: 50%;
    opacity: 0.25;
    transition: opacity 0.25s ease, box-shadow 0.25s ease;
}
.traffic-light-lamp--red { background: var(--el-color-danger); }
.traffic-light-lamp--green { background: var(--el-color-success); }
.traffic-light-lamp.is-lit { opacity: 1; }
.traffic-light-lamp--red.is-lit { box-shadow: 0 0 14px 3px var(--el-color-danger); }
.traffic-light-lamp--green.is-lit { box-shadow: 0 0 14px 3px var(--el-color-success); }

.traffic-light-status__info { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.traffic-light-status__label { color: #ffffff; font-size: 12px; }
.traffic-light-status__name { overflow: hidden; color: #ffffff; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.traffic-light-status__value { display: inline-flex; align-items: center; gap: 6px; color: var(--traffic-light-color); font-weight: 700; }
.traffic-light-status__dot { width: 10px; height: 10px; border-radius: 50%; background: var(--traffic-light-color); box-shadow: 0 0 6px var(--traffic-light-color); }

/* Nút lệnh: 2 cột (đỏ | xanh), bo tròn như form barrier */
.traffic-light-commands {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px 14px;
}
.traffic-light-command.el-button {
    width: 100%;
    height: 40px;
    margin-left: 0;
    white-space: normal;
    line-height: 1.2;
}

.traffic-light-simulate {
    padding: 6px 10px;
    border-radius: 6px;
    background: var(--el-color-warning-light-9);
    color: var(--el-color-warning-dark-2);
    font-size: 12px;
}

@media (max-width: 520px) {
    .traffic-light-commands { grid-template-columns: minmax(0, 1fr); }
}
</style>
