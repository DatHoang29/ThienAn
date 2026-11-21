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
                    </div>
                </div>
            </div>
        </template>

        <!-- Điều khiển quạt -->
        <template #controlTab>
            <div class="content-text fan-control">
                <div v-if="fanStore.simulate" class="fan-control__simulate">
                    <el-icon><ele-WarningFilled /></el-icon>
                    <span>{{ $t('lz.label.fan.simulateHint') }}</span>
                </div>

                <!-- Trạng thái hiện tại -->
                <div class="fan-control__status">
                    <div class="fan-status" :class="`fan-status--${modeVariant}`">
                        <span class="fan-status__label">{{ $t('lz.label.fan.mode') }}</span>
                        <span class="fan-status__value">
                            <el-icon class="fan-status__icon">
                                <ele-Right v-if="fanState?.mode === 'forward'" />
                                <ele-Back v-else-if="fanState?.mode === 'reverse'" />
                                <ele-Minus v-else />
                            </el-icon>{{ modeText }}
                        </span>
                    </div>
                    <div class="fan-status" :class="`fan-status--${modeVariant}`">
                        <span class="fan-status__label">{{ $t('lz.label.fan.frequency') }}</span>
                        <span class="fan-status__value">{{ formatHz(fanState?.frequency) }}</span>
                        <span class="fan-status__sub">{{ $t('lz.label.fan.setpoint') }}: {{ formatHz(fanState?.setpoint) }}</span>
                    </div>
                    <div class="fan-status" :class="`fan-status--${modeVariant}`">
                        <span class="fan-status__label">{{ $t('lz.label.fan.direction') }}</span>
                        <span class="fan-status__value">
                            <el-icon v-if="fanState?.mode === 'forward'"><ele-Right /></el-icon>
                            <el-icon v-else-if="fanState?.mode === 'reverse'"><ele-Back /></el-icon>
                            {{ directionText }}
                        </span>
                    </div>
                </div>

                <!-- Lệnh chạy / dừng -->
                <div class="fan-control__commands">
                    <el-button
                        v-for="item in FAN_COMMANDS"
                        :key="item.command"
                        round
                        plain
                        :type="item.variant"
                        class="fan-command"
                        :loading="state.running === item.command"
                        :disabled="(!!state.running && state.running !== item.command) || fanState?.mode === item.command"
                        @click="handleCommand(item)"
                    >
                        {{ $t(item.labelKey) }}
                    </el-button>
                </div>

                <!-- Đặt tần số biến tần -->
                <div class="fan-control__frequency">
                    <span class="fan-control__frequency-label">{{ $t('lz.label.fan.setFrequency') }}</span>
                    <el-slider
                        v-model="state.frequencyInput"
                        :min="FAN_FREQUENCY_MIN"
                        :max="FAN_FREQUENCY_MAX"
                        :step="FAN_FREQUENCY_STEP"
                        :format-tooltip="formatHz"
                        :disabled="!!state.running"
                        class="fan-control__slider"
                    />
                    <el-input-number
                        v-model="state.frequencyInput"
                        :min="FAN_FREQUENCY_MIN"
                        :max="FAN_FREQUENCY_MAX"
                        :step="FAN_FREQUENCY_STEP"
                        :precision="1"
                        size="default"
                        controls-position="right"
                        :disabled="!!state.running"
                        class="fan-control__number"
                    />
                    <el-button
                        round
                        type="primary"
                        :loading="state.running === 'frequency'"
                        :disabled="(!!state.running && state.running !== 'frequency') || state.frequencyInput === fanState?.setpoint"
                        @click="handleSetFrequency"
                    >
                        {{ $t('lz.button.fan.applyFrequency') }}
                    </el-button>
                </div>
            </div>
        </template>

        <template #footer>
            <el-button round @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="fanInfoModal">
import { reactive, computed, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { auth } from '/@/utils/authFunction';
import {
    FAN_COMMANDS,
    FAN_FREQUENCY_MAX,
    FAN_FREQUENCY_MIN,
    FAN_FREQUENCY_STEP,
    useFanControlStore,
} from '/@/stores/its/fanControl';
import type { FanCommandDefinition } from '/@/stores/its/fanControl';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';

const { t: $t } = useI18n();
const fanStore = useFanControlStore();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const state = reactive({
    activeTab: 'controlTab',
    data: null as any,
    fanId: '' as string,
    /** Lệnh đang gửi ('forward' | 'reverse' | 'stop' | 'frequency') — khoá thao tác khác tới khi xong. */
    running: '' as string,
    frequencyInput: FAN_FREQUENCY_MIN as number,
});

// Cùng quyền điều khiển thiết bị với VMS/LCS/Barrier.
const DEVICE_CONTROL_AUTH = 'tmsTrafficInfo:control';
const canControl = computed(() => auth(DEVICE_CONTROL_AUTH));

const tabs = computed(() => {
    const list = [{ label: $t('lz.label.base.info'), name: 'infoTab' }];
    if (canControl.value) list.push({ label: $t('lz.label.base.control'), name: 'controlTab' });
    return list;
});

const deviceLocation = computed(() => `Km ${state.data?.kmNumber ?? ''} + ${state.data?.metNumber ?? ''}`);
const statusVariant = computed(() => getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.data?.equipmentState)?.value || '');

const fanState = computed(() => fanStore.getState(state.fanId));

const modeVariant = computed(() => (fanState.value?.mode === 'forward' ? 'success' : fanState.value?.mode === 'reverse' ? 'warning' : 'danger'));

const modeText = computed(() => {
    if (fanState.value?.mode === 'forward') return $t('lz.label.fan.modeForward');
    if (fanState.value?.mode === 'reverse') return $t('lz.label.fan.modeReverse');
    return $t('lz.label.fan.modeStop');
});

const directionText = computed(() => {
    if (fanState.value?.mode === 'forward') return $t('lz.label.fan.directionForward');
    if (fanState.value?.mode === 'reverse') return $t('lz.label.fan.directionReverse');
    return $t('lz.label.fan.directionNone');
});

const formatHz = (value?: number) => (Number.isFinite(Number(value)) ? `${Number(value).toFixed(1)} Hz` : '-');

/** Mở panel: đọc số liệu (sinh ngẫu nhiên nếu quạt chưa có), ô tần số lấy theo giá trị đặt hiện tại. */
const openInfo = (data: any) => {
    state.data = data;
    state.fanId = String(data?.id ?? data?.itemId ?? '');
    state.running = '';
    state.activeTab = canControl.value ? 'controlTab' : 'infoTab';
    if (state.fanId) state.frequencyInput = fanStore.readTelemetry(state.fanId).setpoint;
};

/** Hỏi xác nhận trước mọi lệnh; huỷ → false. */
const confirmAction = async (commandName: string) => {
    try {
        await ElMessageBox.confirm(
            $t('lz.message.fan.confirmCommand', { command: commandName, name: state.data?.name || state.data?.code || '' }),
            $t('lz.message.base.warning'),
            { type: 'warning', confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel') }
        );
        return true;
    } catch {
        return false;
    }
};

const run = async (key: string, commandName: string, action: () => Promise<void>) => {
    if (!state.fanId || state.running) return;
    if (!(await confirmAction(commandName))) return;

    state.running = key;
    try {
        await action();
        ElMessage.success($t('lz.message.fan.commandSuccess', { command: commandName }));
    } catch (error) {
        console.error('Error sending fan command:', error);
        ElMessage.error($t('lz.message.fan.commandFailed', { command: commandName }));
    } finally {
        state.running = '';
    }
};

const handleCommand = (definition: FanCommandDefinition) =>
    run(definition.command, $t(definition.labelKey), () => fanStore.sendCommand(state.fanId, definition.command));

const handleSetFrequency = () => {
    const hz = Number(state.frequencyInput);
    return run('frequency', $t('lz.message.fan.setFrequencyCommand', { value: formatHz(hz) }), () => fanStore.setFrequency(state.fanId, hz));
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

.fan-control {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.fan-control__simulate {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 6px 10px;
    border-radius: 6px;
    background: var(--el-color-warning-light-9);
    color: var(--el-color-warning-dark-2);
    font-size: 12px;
}

/* Trạng thái: chế độ | tần số | hướng */
.fan-control__status {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 10px;
}

.fan-status {
    --fan-status-color: var(--el-text-color-secondary);
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
    padding: 8px 12px;
    border: 1px solid var(--el-border-color-lighter);
    border-left: 4px solid var(--fan-status-color);
    border-radius: 8px;
    background: var(--el-bg-color);
}
.fan-status--success { --fan-status-color: var(--el-color-success); }
.fan-status--warning { --fan-status-color: var(--el-color-warning); }
.fan-status--danger { --fan-status-color: var(--el-color-danger); }

.fan-status__label { color: var(--el-text-color-secondary); font-size: 12px; }
.fan-status__value { display: inline-flex; align-items: center; gap: 6px; color: var(--fan-status-color); font-size: 16px; font-weight: 700; font-variant-numeric: tabular-nums; }
.fan-status__sub { color: var(--el-text-color-secondary); font-size: 11px; font-variant-numeric: tabular-nums; }


.fan-control__commands {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 10px;
}

.fan-command.el-button {
    width: 100%;
    height: 40px;
    margin-left: 0;
}

/* Đặt tần số: nhãn | thanh kéo | ô số | nút áp dụng */
.fan-control__frequency {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) 120px auto;
    align-items: center;
    gap: 12px;
    padding: 10px 12px;
    border: 1px solid var(--el-border-color-lighter);
    border-radius: 8px;
    background: var(--el-fill-color-lighter);
}
.fan-control__frequency-label { color: var(--el-text-color-regular); font-size: 13px; font-weight: 600; white-space: nowrap; }
.fan-control__slider { padding: 0 6px; }
.fan-control__number { width: 120px; }

@media (max-width: 560px) {
    .fan-control__status,
    .fan-control__commands { grid-template-columns: minmax(0, 1fr); }
    .fan-control__frequency { grid-template-columns: minmax(0, 1fr); }
}
</style>
