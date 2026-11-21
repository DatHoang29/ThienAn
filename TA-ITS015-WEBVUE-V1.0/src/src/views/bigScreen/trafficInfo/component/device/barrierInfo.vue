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

        <!-- Điều khiển hệ thống Barrier -->
        <template #controlTab>
            <div class="content-text barrier-control">
                <!-- Trạng thái hiện tại của hệ: barrier + đèn tín hiệu -->
                <div class="barrier-control__status">
                    <div class="barrier-status" :class="`barrier-status--${armVariant}`">
                        <span class="barrier-status__label">{{ $t('lz.label.barrier.barrier') }}</span>
                        <span class="barrier-status__name" :title="barrierName">{{ barrierName }}</span>
                    </div>
                    <div class="barrier-status" :class="`barrier-status--${lightVariant}`">
                        <span class="barrier-status__label">{{ $t('lz.label.barrier.signalLight') }}</span>
                        <span class="barrier-status__name" :title="lightName">{{ lightName }}</span>
                    </div>
                </div>

                <!-- Lệnh điều khiển: mỗi nút hỏi xác nhận trước khi gửi -->
                <div class="barrier-control__commands">
                    <el-button
                        v-for="item in BARRIER_COMMANDS"
                        :key="item.command"
                        round
                        plain
                        :type="item.variant === 'default' ? '' : item.variant"
                        class="barrier-command"
                        :loading="state.runningCommand === item.command"
                        :disabled="!!state.runningCommand && state.runningCommand !== item.command || !barrierStore.canRun(state.system, item)"
                        @click="handleCommand(item)"
                    >
                        {{ $t(item.labelKey) }}
                    </el-button>
                </div>
            </div>
        </template>

        <template #footer>
            <el-button round @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
        </template>
    </InfoPanel>
</template>

<script setup lang="ts" name="barrierInfoModal">
import { reactive, computed, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useMapStore } from '/@/stores/its/mapStore';
import { auth } from '/@/utils/authFunction';
import { BARRIER_COMMANDS, useBarrierControlStore } from '/@/stores/its/barrierControl';
import type { BarrierCommandDefinition, BarrierSystem } from '/@/stores/its/barrierControl';
import InfoPanel from '/@/components/bigScreen/floatingModal/InfoPanel.vue';

const { t: $t } = useI18n();
const mapStore = useMapStore();
const barrierStore = useBarrierControlStore();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const emits = defineEmits(['activated', 'closed', 'closeOthers', 'closeAll']);

const state = reactive({
    activeTab: 'controlTab',
    data: null as any,
    system: null as BarrierSystem | null,
    /** Lệnh đang gửi — khoá các nút khác tới khi xong. */
    runningCommand: '' as string,
});

// Cùng quyền điều khiển thiết bị với VMS/LCS: không có quyền chỉ xem tab Thông tin.
const DEVICE_CONTROL_AUTH = 'tmsTrafficInfo:control';
const canControl = computed(() => auth(DEVICE_CONTROL_AUTH));

const tabs = computed(() => {
    const list = [{ label: $t('lz.label.base.info'), name: 'infoTab' }];
    if (canControl.value) list.push({ label: $t('lz.label.base.control'), name: 'controlTab' });
    return list;
});

const deviceLocation = computed(() => `Km ${state.data?.kmNumber ?? ''} + ${state.data?.metNumber ?? ''}`);
const statusVariant = computed(() => getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, state.data?.equipmentState)?.value || '');

const getEquipmentName = (deviceId?: string | null) => {
    if (!deviceId) return '-';
    return mapStore.equipmentList.find((item: any) => String(item.id) === String(deviceId))?.name || '-';
};

const barrierName = computed(() => getEquipmentName(state.system?.barrierId));
// Đèn tích hợp (lightId = barrierId) → ghi rõ để người vận hành không tưởng là đèn rời
const lightName = computed(() =>
    state.system?.lightId && state.system.lightId === state.system.barrierId
        ? $t('lz.label.barrier.integratedLight')
        : getEquipmentName(state.system?.lightId)
);

const armState = computed(() => barrierStore.getArmState(state.system?.barrierId));
const lightState = computed(() => barrierStore.getLightState(state.system?.lightId));

const armVariant = computed(() => (armState.value === 'open' ? 'success' : armState.value === 'closed' ? 'danger' : 'unknown'));
const lightVariant = computed(() => (lightState.value === 'green' ? 'success' : lightState.value === 'red' ? 'danger' : 'unknown'));

const armText = computed(() => {
    if (!state.system?.barrierId) return $t('lz.label.barrier.notAvailable');
    if (armState.value === 'open') return $t('lz.label.barrier.armOpen');
    if (armState.value === 'closed') return $t('lz.label.barrier.armClosed');
    return $t('lz.label.barrier.unknown');
});
const lightText = computed(() => {
    if (lightState.value === 'green') return $t('lz.label.barrier.lightGreen');
    if (lightState.value === 'red') return $t('lz.label.barrier.lightRed');
    return $t('lz.label.barrier.unknown');
});

/** Mở panel: `system` do equipmentModal xác định (barrier + đèn cùng hệ). */
const openInfo = (data: any, system: BarrierSystem | null) => {
    state.data = data;
    state.system = system;
    state.runningCommand = '';
    state.activeTab = canControl.value ? 'controlTab' : 'infoTab';
};

/** Xác nhận → gửi lệnh → báo kết quả. Huỷ xác nhận thì không làm gì. */
const handleCommand = async (definition: BarrierCommandDefinition) => {
    if (!state.system || state.runningCommand) return;
    const commandName = $t(definition.labelKey);

    try {
        await ElMessageBox.confirm(
            $t('lz.message.barrier.confirmCommand', { command: commandName, name: barrierName.value !== '-' ? barrierName.value : lightName.value }),
            $t('lz.message.base.warning'),
            {
                type: 'warning',
                confirmButtonText: $t('lz.button.base.confirm'),
                cancelButtonText: $t('lz.button.base.cancel'),
            }
        );
    } catch {
        return; // người dùng huỷ
    }

    state.runningCommand = definition.command;
    try {
        await barrierStore.sendCommand(state.system, definition.command);
        ElMessage.success($t('lz.message.barrier.commandSuccess', { command: commandName }));
    } catch (error) {
        console.error('Error sending barrier command:', error);
        ElMessage.error($t('lz.message.barrier.commandFailed', { command: commandName }));
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

.barrier-control {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.barrier-control__simulate {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 6px 10px;
    border-radius: 6px;
    background: var(--el-color-warning-light-9);
    color: var(--el-color-warning-dark-2);
    font-size: 12px;
}

/* Trạng thái hiện tại: 2 thẻ barrier | đèn tín hiệu */
.barrier-control__status {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
}

.barrier-status {
    --barrier-status-color: var(--el-text-color-secondary);
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
    padding: 8px 12px;
    border: 1px solid var(--el-border-color-lighter);
    border-left: 4px solid var(--barrier-status-color);
    border-radius: 8px;
    background: var(--el-bg-color);
}
.barrier-status--success { --barrier-status-color: var(--el-color-success); }
.barrier-status--danger { --barrier-status-color: var(--el-color-danger); }

.barrier-status__label { color: var(--el-text-color-secondary); font-size: 12px; }
.barrier-status__name { overflow: hidden; color: var(--el-text-color-primary); font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.barrier-status__value { display: inline-flex; align-items: center; gap: 6px; color: var(--barrier-status-color); font-weight: 700; }
.barrier-status__dot { width: 10px; height: 10px; border-radius: 50%; background: var(--barrier-status-color); box-shadow: 0 0 6px var(--barrier-status-color); }

/* Lưới nút lệnh như mẫu: 2 cột, nút bo tròn; nút trung tính (mở/đóng barrier) ở hàng cuối */
.barrier-control__commands {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px 14px;
}

.barrier-command.el-button {
    width: 100%;
    height: 40px;
    margin-left: 0;
    white-space: normal;
    line-height: 1.2;
}

@media (max-width: 520px) {
    .barrier-control__status,
    .barrier-control__commands { grid-template-columns: minmax(0, 1fr); }
}
</style>
