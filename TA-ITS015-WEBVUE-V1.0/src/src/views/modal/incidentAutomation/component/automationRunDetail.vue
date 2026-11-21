<template>
	<el-dialog
		v-model="automationStore.isShowDetailDialog"
		draggable
		append-to-body
		:close-on-click-modal="false"
		:z-index="16000"
		width="780px"
		class="automation-detail-dialog"
		@closed="automationStore.closeDetail()"
	>
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-AlarmClock /></el-icon>
				<span> {{ $t('lz.label.tmsIncidentAutomation.detailTitle') }} </span>
			</div>
		</template>

		<template v-if="item">
			<!-- Khối đếm ngược: điểm nhìn chính của modal -->
			<div class="countdown-hero" :class="`is-${automationStore.getUrgency(item)}`">
				<span class="hero-label">{{ $t('lz.label.tmsIncidentAutomation.autoExecuteIn') }}</span>
				<span class="hero-value">{{ automationStore.formatRemaining(item) }}</span>

				<div class="hero-track">
					<span class="hero-bar" :style="{ width: automationStore.getRemainingPercent(item) + '%' }" />
				</div>

				<span class="hero-due">
					{{ $t('lz.label.tmsIncidentAutomation.executeDueAt') }}: <b>{{ formatDateTime(automation?.executeDueAt) }}</b>
				</span>
			</div>

			<!-- Thông tin sự cố -->
			<div class="detail-section">
				<div class="section-title">
					<el-icon><ele-WarnTriangleFilled /></el-icon>
					<span>{{ $t('lz.label.tmsIncidentAutomation.incidentSection') }}</span>
				</div>

				<el-descriptions :column="2" border size="small">
					<el-descriptions-item :label="$t('lz.entity.base.code')">{{ incident?.code || '-' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.base.status')">
						<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsIncidentStatus, incident?.state)" />
					</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.base.name')" :span="2">{{ incident?.name || '-' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.tmsEventType.eventType')">
						<el-tag v-if="incident?.eventTypeName" type="danger" effect="dark" size="small" round>{{ incident.eventTypeName }}</el-tag>
						<span v-else>-</span>
					</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.base.position')">{{ formatPosition(incident) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.base.startTime')" :span="2">{{ formatDateTime(incident?.startDate) }}</el-descriptions-item>
				</el-descriptions>
			</div>

			<!-- Kịch bản sẽ áp dụng -->
			<div class="detail-section">
				<div class="section-title">
					<el-icon><ele-Operation /></el-icon>
					<span>{{ $t('lz.label.tmsIncidentAutomation.planSection') }}</span>
				</div>

				<el-descriptions :column="2" border size="small">
					<el-descriptions-item :label="$t('lz.entity.tmsTrafficPlan.trafficPlan')" :span="2">
						<el-tag v-if="automation?.trafficPlanName" type="primary" effect="plain" size="small">{{ automation.trafficPlanName }}</el-tag>
						<span v-else>-</span>
					</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.tmsIncidentAutomationConfig.autoFinishAfterSeconds')" :span="2">
						<span v-if="autoFinishAfterSeconds" class="text-success">
							{{ autoFinishAfterSeconds }}s
						</span>
						<span v-else class="text-muted">{{ $t('lz.label.tmsIncidentAutomation.notConfigured') }}</span>
					</el-descriptions-item>
				</el-descriptions>
			</div>

			<!-- Giải thích các quyết định nghiệp vụ để người trực không phải đoán -->
			<div class="action-hints">
				<div class="hint-row">
					<el-tag type="primary" effect="dark" size="small" round>{{ $t('lz.button.tmsIncidentAutomation.executeNow') }}</el-tag>
					<span>{{ $t('lz.label.tmsIncidentAutomation.executeNowHint') }}</span>
				</div>
				<div class="hint-row">
					<el-tag type="warning" effect="dark" size="small" round>{{ $t('lz.button.tmsIncidentAutomation.convertToManual') }}</el-tag>
					<span>{{ $t('lz.label.tmsIncidentAutomation.convertToManualHint') }}</span>
				</div>
				<div class="hint-row">
					<el-tag type="danger" effect="dark" size="small" round>{{ $t('lz.button.tmsIncidentAutomation.markSkipped') }}</el-tag>
					<span>{{ $t('lz.label.tmsIncidentAutomation.skipTip') }}</span>
				</div>
			</div>
		</template>

		<template #footer>
			<span class="dialog-footer">
				<el-button type="danger" :disabled="state.isProcessing" v-auth="'tmsIncidentAutomation:skip'" @click="handleSkip">
					{{ $t('lz.button.tmsIncidentAutomation.markSkipped') }}
				</el-button>
				<el-button type="warning" :disabled="state.isProcessing" v-auth="'tmsIncidentAutomation:convertToManual'" @click="handleConvertToManual">
					{{ $t('lz.button.tmsIncidentAutomation.convertToManual') }}
				</el-button>
				<el-button type="primary" :disabled="state.isProcessing" v-auth="'tmsIncidentAutomation:executeNow'" @click="handleExecuteNow">
					{{ $t('lz.button.tmsIncidentAutomation.executeNow') }}
				</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="tmsAutomationRunDetail">
import { computed, defineAsyncComponent, reactive } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useIncidentAutomation } from '/@/stores/its/incidentAutomation';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { formatDate } from '/@/utils/formatTime';

const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const { t: $t } = useI18n();
const automationStore = useIncidentAutomation();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const state = reactive({
	isProcessing: false,
});

/** Phần tử đang xem: { incident, automation } lấy từ key automationPendingList của ListByAccount */
const item = computed(() => automationStore.selectedItem);
const incident = computed(() => item.value?.incident);
const automation = computed(() => item.value?.automation);
const autoFinishAfterSeconds = computed(() => {
	const seconds = automation.value?.autoFinishAfterSeconds ?? automation.value?.AutoFinishAfterSeconds;
	return Number(seconds) > 0 ? Number(seconds) : 0;
});

// Hiển thị vị trí Km+m
const formatPosition = (data: any) => {
	if (!data || (data.kmNumber == null && data.metNumber == null)) return '-';
	return `Km ${data.kmNumber ?? 0}+${String(data.metNumber ?? 0).padStart(3, '0')}`;
};

// Định dạng ngày giờ hiển thị
const formatDateTime = (value: any) => (value ? formatDate(new Date(value), 'YYYY-mm-dd HH:MM:SS') : '-');

// Xác nhận thực thi ngay (bỏ qua thời gian chờ còn lại)
const handleExecuteNow = async () => {
	const runId = automation.value?.runId;
	if (!runId) return;

	try {
		await ElMessageBox.confirm($t('lz.message.tmsIncidentAutomation.confirmExecuteNow', { name: incident.value?.name || incident.value?.code }), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
			modalClass: 'automation-confirm-overlay',
		});
	} catch {
		return; // Người dùng bấm hủy
	}

	state.isProcessing = true;
	try {
		await automationStore.executeNow(runId);
		ElMessage.success($t('lz.message.tmsIncidentAutomation.executeNowSuccess'));
		automationStore.closeDetail();
	} catch (error) {
		console.error('handleExecuteNow', error);
	} finally {
		state.isProcessing = false;
	}
};

// Hủy lần chạy tự động đang chờ và chuyển sự cố về luồng xử lý thủ công.
const handleConvertToManual = async () => {
	const runId = automation.value?.runId;
	if (!runId) return;

	try {
		await ElMessageBox.confirm($t('lz.message.tmsIncidentAutomation.confirmConvertToManual', { name: incident.value?.name || incident.value?.code }), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
			modalClass: 'automation-confirm-overlay',
		});
	} catch {
		return;
	}

	state.isProcessing = true;
	try {
		await automationStore.convertToManual(runId);
		ElMessage.success($t('lz.message.tmsIncidentAutomation.convertToManualSuccess'));
		automationStore.closeDetail();
	} catch (error) {
		console.error('handleConvertToManual', error);
	} finally {
		state.isProcessing = false;
	}
};

/**
 * Author: SonTH
 * Description: Đánh dấu sự cố là bỏ qua - không xử lý. Giống hệt nút "Bỏ qua" ở tab Registration
 *              của popup xử lý sự cố, vì sự cố ở giai đoạn này chưa được xác thực.
 *              KHÔNG chuyển sự cố về luồng thủ công để xử lý tiếp.
 * Created date: 24/07/2026
 */
const handleSkip = async () => {
	const incidentId = incident.value?.id;
	if (!incidentId) return;

	try {
		await ElMessageBox.confirm($t('lz.message.tmsIncidentAutomation.confirmSkip', { name: incident.value?.name || incident.value?.code }), $t('lz.message.base.warning'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
			modalClass: 'automation-confirm-overlay',
		});
	} catch {
		return;
	}

	state.isProcessing = true;
	try {
		await automationStore.skipIncident(incidentId);
		ElMessage.success($t('lz.message.tmsIncidentAutomation.skipSuccess'));
		automationStore.closeDetail();
	} catch (error) {
		console.error('handleSkip', error);
	} finally {
		state.isProcessing = false;
	}
};
</script>

<style scoped lang="scss">
/* Khối đếm ngược */
.countdown-hero {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 6px;
	padding: 18px 20px 16px;
	margin-bottom: 18px;
	border-radius: 12px;
	background: var(--el-fill-color-light);
	border: 1px solid var(--el-border-color-lighter);

	/* Ba mức độ gấp dùng chung một biến để nhãn, số và thanh tiến trình luôn đồng màu */
	&.is-normal {
		--hero-color: var(--el-color-warning);
	}

	&.is-high {
		--hero-color: var(--el-color-danger);
	}

	&.is-critical {
		--hero-color: var(--el-color-danger);
		border-color: var(--el-color-danger-light-5);
		background: var(--el-color-danger-light-9);
	}

	.hero-label {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}

	.hero-value {
		font-size: 44px;
		font-weight: 700;
		font-variant-numeric: tabular-nums;
		letter-spacing: 2px;
		line-height: 1.1;
		color: var(--hero-color);
	}

	.hero-track {
		width: 100%;
		height: 6px;
		margin-top: 4px;
		border-radius: 999px;
		overflow: hidden;
		background: var(--el-fill-color-dark);
	}

	.hero-bar {
		display: block;
		height: 100%;
		border-radius: 999px;
		background: var(--hero-color);
		transition: width 1s linear;
	}

	.hero-due {
		margin-top: 2px;
		font-size: 12px;
		color: var(--el-text-color-regular);
	}
}

/* Từng nhóm thông tin */
.detail-section {
	margin-bottom: 16px;

	.section-title {
		display: flex;
		align-items: center;
		gap: 6px;
		margin-bottom: 8px;
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);

		.el-icon {
			color: var(--el-color-primary);
		}
	}
}

.text-muted {
	color: var(--el-text-color-secondary);
}

/* Giải thích hành động */
.action-hints {
	display: flex;
	flex-direction: column;
	gap: 8px;
	padding: 12px 14px;
	border-radius: 10px;
	background: var(--el-fill-color-lighter);
	border: 1px dashed var(--el-border-color);

	.hint-row {
		display: flex;
		align-items: center;
		gap: 8px;
		font-size: 12px;
		color: var(--el-text-color-regular);

		.el-tag {
			flex: 0 0 auto;
		}
	}
}

/* Banner 15000 -> detail 16000 -> confirm 17000. */
:global(.automation-confirm-overlay) {
	z-index: 17000 !important;
}
</style>
