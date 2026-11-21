<template>
	<Teleport to="body">
		<transition-group name="vehicle-filter-alert" tag="section" class="vehicle-filter-alert-region"
			:aria-label="$t('lz.label.tmsVehicleFilterAlert.regionLabel')" aria-live="assertive">
			<article v-for="item in alertStore.alerts" :key="item.id" class="vehicle-filter-alert" role="alert">
				<span class="alert-accent" />

				<div class="alert-icon" aria-hidden="true">
					<el-icon><ele-WarningFilled /></el-icon>
				</div>

				<div class="alert-content">
					<div class="alert-eyebrow">{{ $t('lz.label.tmsVehicleFilterAlert.attentionRequired') }}</div>
					<div class="alert-plate">{{ item.licensePlate }}</div>

					<div class="alert-meta">
						<span class="meta-item">
							<el-icon><ele-Clock /></el-icon>
							{{ formatDetectedAt(item.detectedAt) }}
						</span>
						<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsVehicleFilterType, item.type)" />
					</div>
				</div>

				<!-- <div class="alert-countdown" :aria-label="$t('lz.label.tmsVehicleFilterAlert.autoCloseIn', { seconds: alertStore.getRemainingSeconds(item) })">
					<span class="countdown-value">{{ alertStore.getRemainingSeconds(item) }}</span>
					<span class="countdown-unit">{{ $t('lz.label.tmsVehicleFilterAlert.secondUnit') }}</span>
				</div> -->

				<button type="button" class="alert-close" :aria-label="$t('lz.label.tmsVehicleFilterAlert.closeAlert', { licensePlate: item.licensePlate })"
					@click="alertStore.dismissAlert(item.id)">
					<el-icon><ele-Close /></el-icon>
				</button>

				<span class="alert-progress" :style="{ width: `${alertStore.getRemainingPercent(item)}%` }" />
			</article>
		</transition-group>
	</Teleport>
</template>

<script lang="ts" setup name="tmsVehicleFilterAlert">
import { defineAsyncComponent, onMounted, onUnmounted, ref } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { useI18n } from 'vue-i18n';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useVehicleFilterAlert } from '/@/stores/its/vehicleFilterAlert';
import { useTransporterVehicleAlert, type VehicleAlertPayload } from '/@/transporter/hooks/useTransporterVehicleFilter';

const alertStore = useVehicleFilterAlert();
const { t: $t } = useI18n();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const tickTimer = ref<number | null>(null);

const formatDetectedAt = (value: Date) => useDateFormat(value, 'YYYY-MM-DD HH:mm:ss').value;

useTransporterVehicleAlert((data: VehicleAlertPayload) => {
	void alertStore.processVdsData(data).catch((error) => {
		console.error('[VehicleFilterAlert] Không xử lý được dữ liệu VDS.', error);
	});
});

onMounted(() => {
	tickTimer.value = window.setInterval(() => alertStore.tick(), 100);
});

onUnmounted(() => {
	if (tickTimer.value !== null) window.clearInterval(tickTimer.value);
});
</script>

<style scoped lang="scss">
.vehicle-filter-alert-region {
	position: fixed;
	top: 72px;
	left: 20px;
	z-index: 15000;
	display: flex;
	flex-direction: column;
	gap: 8px;
	width: min(410px, calc(100vw - 32px));
	max-height: calc(100vh - 92px);
	padding: 4px;
	overflow-y: auto;
	pointer-events: none;
	scrollbar-width: thin;
}

.vehicle-filter-alert {
	--alert-accent-color: var(--el-color-danger);
	position: relative;
	display: flex;
	align-items: center;
	gap: 10px;
	min-height: 84px;
	padding: 10px 38px 12px 14px;
	overflow: hidden;
	pointer-events: auto;
	background: var(--el-bg-color-overlay);
	border: 1px solid var(--el-color-danger-light-7);
	border-radius: 11px;
	box-shadow: var(--el-box-shadow-light);
}

.alert-accent {
	position: absolute;
	top: 0;
	bottom: 0;
	left: 0;
	width: 4px;
	background: var(--alert-accent-color);
}

.alert-icon {
	flex: 0 0 auto;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 38px;
	height: 38px;
	font-size: 21px;
	color: var(--alert-accent-color);
	background: var(--el-color-danger-light-9);
	border-radius: 10px;
}

.alert-content {
	flex: 1 1 auto;
	min-width: 0;
}

.alert-eyebrow {
	margin-bottom: 2px;
	font-size: 10px;
	font-weight: 700;
	line-height: 1.2;
	letter-spacing: 0.08em;
	text-transform: uppercase;
	color: var(--alert-accent-color);
}

.alert-plate {
	overflow: hidden;
	font-size: 20px;
	font-weight: 800;
	font-variant-numeric: tabular-nums;
	line-height: 1.25;
	letter-spacing: 0.035em;
	text-overflow: ellipsis;
	white-space: nowrap;
	color: var(--el-text-color-primary);
}

.alert-meta {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 5px 8px;
	margin-top: 5px;
	font-size: 11px;
	color: var(--el-text-color-secondary);
}

.meta-item {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	white-space: nowrap;
}

.alert-countdown {
	flex: 0 0 auto;
	display: flex;
	flex-direction: column;
	align-items: center;
	min-width: 38px;
	padding-left: 9px;
	border-left: 1px solid var(--el-border-color-lighter);
}

.countdown-value {
	font-size: 19px;
	font-weight: 800;
	font-variant-numeric: tabular-nums;
	line-height: 1.1;
	color: var(--alert-accent-color);
}

.countdown-unit {
	font-size: 9px;
	line-height: 1.2;
	color: var(--el-text-color-secondary);
}

.alert-close {
	position: absolute;
	top: 7px;
	right: 7px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 25px;
	height: 25px;
	padding: 0;
	font: inherit;
	color: var(--el-text-color-secondary);
	cursor: pointer;
	background: transparent;
	border: 0;
	border-radius: 6px;
	transition: color 0.2s ease, background-color 0.2s ease;

	&:hover,
	&:focus-visible {
		color: var(--el-text-color-primary);
		background: var(--el-fill-color-light);
		outline: none;
	}

	&:focus-visible {
		box-shadow: 0 0 0 2px var(--el-color-primary-light-5);
	}
}

/* Đồng bộ banner incident: thanh vơi dần bám sát cạnh dưới. */
.alert-progress {
	position: absolute;
	bottom: 0;
	left: 0;
	height: 3px;
	background: var(--alert-accent-color);
	opacity: 0.88;
	transition: width 0.1s linear;
}

.vehicle-filter-alert-enter-active,
.vehicle-filter-alert-leave-active {
	transition: opacity 0.25s ease, transform 0.25s ease;
}

.vehicle-filter-alert-enter-from,
.vehicle-filter-alert-leave-to {
	opacity: 0;
	transform: translateX(-20px);
}

@media (max-width: 560px) {
	.vehicle-filter-alert-region {
		top: 60px;
		left: 12px;
		width: calc(100vw - 24px);
	}

	.vehicle-filter-alert {
		padding-right: 34px;
	}

	.alert-countdown {
		display: none;
	}
}
</style>
