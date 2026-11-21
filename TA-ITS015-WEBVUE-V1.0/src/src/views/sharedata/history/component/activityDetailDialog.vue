<template>
	<div class="sharedata-dialog-container">
		<el-dialog
			v-model="visible"
			width="1080px"
			draggable
			@closed="close"
			class="activity-detail-dialog"
		>
			<template #header>
				<div class="dialog-header">
					<el-icon class="mr5"><ele-InfoFilled /></el-icon>
					<span>{{ $t('lz.label.sharedataActivityDetail.title') }}</span>
				</div>
			</template>

			<template v-if="row">
				<!-- Khối thông tin chung -->
				<el-descriptions :column="2" size="small" border>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.time')">{{ fmt(row.occurredAt) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.group')">
						<el-tag size="small" :type="isTransfer ? 'primary' : 'info'">
							{{ isTransfer ? $t('lz.label.sharedataHistory.logTransfer') : $t('lz.label.sharedataHistory.logConfig') }}
						</el-tag>
					</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.action')">
						<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SharedataactionType, row.action)" />
					</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.result')">
					<el-tag size="small" :type="isSuccess ? 'success' : 'danger'">
						{{ isSuccess ? $t('lz.label.sharedataActivityDetail.resultSuccess') : $t('lz.label.sharedataActivityDetail.resultFailed') }}
					</el-tag>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.partner')">{{ partnerDisplayName }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.target')">{{ targetDisplayName }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.operator')" :span="2">{{ row.operatorName || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.content')" :span="2">{{ contentDescription }}</el-descriptions-item>
				<el-descriptions-item v-if="row.errorMessage" :label="$t('lz.label.sharedataActivityDetail.errorMessage')" :span="2">
					<div class="main-err-wrapper">
						<span class="err-text">{{ row.errorMessage }}</span>
						<el-tooltip :content="$t('lz.label.sharedataActivityDetail.copyError')" placement="top">
							<el-button link size="small" class="copy-err-btn" @click="copyText(row.errorMessage)">
								<el-icon><ele-CopyDocument /></el-icon>
							</el-button>
						</el-tooltip>
					</div>
				</el-descriptions-item>
			</el-descriptions>

			<!-- Nhóm Truyền nhận: Khối Step Log 2 bước + Thông tin gói tin -->
			<template v-if="isTransfer">
				<div class="sect-title">
					<el-icon class="mr5"><ele-Odometer /></el-icon>
					<span>{{ processTitleText }}</span>
				</div>
				<div
					class="steps-card"
					:class="{ 'is-empty': !loadingSteps && !steps.length, 'is-loading': loadingSteps }"
					v-loading="loadingSteps"
					element-loading-background="var(--el-bg-color, #ffffff)"
					:element-loading-text="$t('lz.label.base.loading')"
				>
					<!-- Khi đang tải: giữ khoảng trống nền trắng sạch sẽ, không dựng các bước mờ phía sau -->
					<div v-if="loadingSteps" class="steps-loading-placeholder" />

					<!-- Chưa có bước con nào (lỗi ngay từ đầu, hoặc bản ghi chỉ có dòng tổng kết): hiển thị đơn giản, chỉ dựng pipeline khi có bước thực tế -->
					<div v-else-if="!steps.length" class="empty-process-simple">
						<el-icon :size="24" class="empty-icon"><ele-InfoFilled v-if="isSuccess" /><ele-Warning v-else /></el-icon>
						<span class="empty-text">{{ isSuccess ? $t('lz.label.sharedataActivityDetail.noStepDetail') : $t('lz.label.sharedataActivityDetail.noProcess') }}</span>
					</div>

					<!-- Khi có tiến trình xử lý các bước -->
					<template v-else>
						<el-steps direction="vertical">
							<el-step v-if="stepVisible(1)" :status="stepStatus(1)">
								<template #icon>
									<div class="custom-step-node" :class="`is-${stepStatus(1)}`">
										<svg v-if="stepStatus(1) === 'success'" class="check-icon" viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
											<polyline points="20 6 9 17 4 12"></polyline>
										</svg>
										<svg v-else-if="stepStatus(1) === 'error'" class="err-icon" viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
											<line x1="18" y1="6" x2="6" y2="18"></line>
											<line x1="6" y1="6" x2="18" y2="18"></line>
										</svg>
										<span v-else-if="stepStatus(1) === 'process'" class="spinner-ring"></span>
										<span v-else class="wait-ring"></span>
									</div>
								</template>
								<template #title>
									<el-tooltip
										:disabled="!stepMissingReason(1)"
										:content="stepMissingReason(1)"
										placement="top"
										popper-class="activity-detail-tooltip"
									>
										<div class="step-title-wrap">
											<div class="step-title-text">{{ $t(isSend ? 'lz.label.sharedataActivityDetail.stepExtract' : 'lz.label.sharedataActivityDetail.stepReceive') }}</div>
											<div class="step-subtitle" :class="{ 'is-err': stepStatus(1) === 'error' }">
												{{ stepSubtitle(1) }}
											</div>
										</div>
									</el-tooltip>
								</template>
								<template #description>
									<div class="step-desc-text">{{ stepDescription(1) }}</div>
									<div v-if="stepErrorMessage(1)" class="step-err-box step-err-callout">
										<div class="step-err-header">
											<div class="step-err-title">
												<el-icon class="err-header-icon"><ele-CircleCloseFilled /></el-icon>
												<span class="err-friendly-text">{{ diagnoseError(stepErrorMessage(1)).title }}</span>
											</div>
											<el-tooltip :content="$t('lz.label.sharedataActivityDetail.copyError')" placement="top">
												<el-button link size="small" class="err-copy-btn" @click="copyText(stepErrorMessage(1))">
													<el-icon class="mr3"><ele-CopyDocument /></el-icon>
													<span>{{ $t('lz.label.sharedataActivityDetail.copy') }}</span>
												</el-button>
											</el-tooltip>
										</div>
										<div v-if="diagnoseError(stepErrorMessage(1)).hint" class="step-err-hint">
											<el-icon class="hint-icon"><ele-InfoFilled /></el-icon>
											<span>{{ diagnoseError(stepErrorMessage(1)).hint }}</span>
										</div>
										<div class="step-err-raw">
											<span class="raw-label">{{ $t('lz.label.sharedataActivityDetail.rawErrorLabel') }}:</span>
											<code class="raw-code">{{ displayedStepError(1) }}</code>
											<el-button
												v-if="isErrorTruncatable(stepErrorMessage(1))"
												link
												type="primary"
												size="small"
												class="raw-toggle-btn"
												@click="toggleStepErrorExpand(1)"
											>
												{{ isStepErrorExpanded(1) ? $t('lz.label.sharedataActivityDetail.collapseError') : $t('lz.label.sharedataActivityDetail.expandError') }}
											</el-button>
										</div>
									</div>
								</template>
							</el-step>
							<el-step v-if="stepVisible(2)" :status="stepStatus(2)">
								<template #icon>
									<div class="custom-step-node" :class="`is-${stepStatus(2)}`">
										<svg v-if="stepStatus(2) === 'success'" class="check-icon" viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
											<polyline points="20 6 9 17 4 12"></polyline>
										</svg>
										<svg v-else-if="stepStatus(2) === 'error'" class="err-icon" viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
											<line x1="18" y1="6" x2="6" y2="18"></line>
											<line x1="6" y1="6" x2="18" y2="18"></line>
										</svg>
										<span v-else-if="stepStatus(2) === 'process'" class="spinner-ring"></span>
										<span v-else class="wait-ring"></span>
									</div>
								</template>
								<template #title>
									<el-tooltip
										:disabled="!stepMissingReason(2)"
										:content="stepMissingReason(2)"
										placement="top"
										popper-class="activity-detail-tooltip"
									>
										<div class="step-title-wrap">
											<div class="step-title-text">{{ isOldTwoStepOutbound ? $t('lz.label.sharedataActivityDetail.stepLegacySend') : $t(isSend ? 'lz.label.sharedataActivityDetail.stepMap' : 'lz.label.sharedataActivityDetail.stepStore') }}</div>
											<div class="step-subtitle" :class="{ 'is-err': stepStatus(2) === 'error' }">
												{{ stepSubtitle(2) }}
											</div>
										</div>
									</el-tooltip>
								</template>
								<template #description>
									<div class="step-desc-text">{{ stepDescription(2) }}</div>
									<div v-if="stepErrorMessage(2)" class="step-err-box step-err-callout">
										<div class="step-err-header">
											<div class="step-err-title">
												<el-icon class="err-header-icon"><ele-CircleCloseFilled /></el-icon>
												<span class="err-friendly-text">{{ diagnoseError(stepErrorMessage(2)).title }}</span>
											</div>
											<el-tooltip :content="$t('lz.label.sharedataActivityDetail.copyError')" placement="top">
												<el-button link size="small" class="err-copy-btn" @click="copyText(stepErrorMessage(2))">
													<el-icon class="mr3"><ele-CopyDocument /></el-icon>
													<span>{{ $t('lz.label.sharedataActivityDetail.copy') }}</span>
												</el-button>
											</el-tooltip>
										</div>
										<div v-if="diagnoseError(stepErrorMessage(2)).hint" class="step-err-hint">
											<el-icon class="hint-icon"><ele-InfoFilled /></el-icon>
											<span>{{ diagnoseError(stepErrorMessage(2)).hint }}</span>
										</div>
										<div class="step-err-raw">
											<span class="raw-label">{{ $t('lz.label.sharedataActivityDetail.rawErrorLabel') }}:</span>
											<code class="raw-code">{{ displayedStepError(2) }}</code>
											<el-button
												v-if="isErrorTruncatable(stepErrorMessage(2))"
												link
												type="primary"
												size="small"
												class="raw-toggle-btn"
												@click="toggleStepErrorExpand(2)"
											>
												{{ isStepErrorExpanded(2) ? $t('lz.label.sharedataActivityDetail.collapseError') : $t('lz.label.sharedataActivityDetail.expandError') }}
											</el-button>
										</div>
									</div>
								</template>
							</el-step>
							<el-step v-if="hasStep3 && stepVisible(3)" :status="stepStatus(3)">
								<template #icon>
									<div class="custom-step-node" :class="`is-${stepStatus(3)}`">
										<svg v-if="stepStatus(3) === 'success'" class="check-icon" viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
											<polyline points="20 6 9 17 4 12"></polyline>
										</svg>
										<svg v-else-if="stepStatus(3) === 'error'" class="err-icon" viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
											<line x1="18" y1="6" x2="6" y2="18"></line>
											<line x1="6" y1="6" x2="18" y2="18"></line>
										</svg>
										<span v-else-if="stepStatus(3) === 'process'" class="spinner-ring"></span>
										<span v-else class="wait-ring"></span>
									</div>
								</template>
								<template #title>
									<el-tooltip
										:disabled="!stepMissingReason(3)"
										:content="stepMissingReason(3)"
										placement="top"
										popper-class="activity-detail-tooltip"
									>
										<div class="step-title-wrap">
											<div class="step-title-text">{{ $t('lz.label.sharedataActivityDetail.stepSend') }}</div>
											<div class="step-subtitle" :class="{ 'is-err': stepStatus(3) === 'error' }">
												{{ stepSubtitle(3) }}
											</div>
										</div>
									</el-tooltip>
								</template>
								<template #description>
									<div class="step-desc-text">{{ stepDescription(3) }}</div>
									<div v-if="stepErrorMessage(3)" class="step-err-box step-err-callout">
										<div class="step-err-header">
											<div class="step-err-title">
												<el-icon class="err-header-icon"><ele-CircleCloseFilled /></el-icon>
												<span class="err-friendly-text">{{ diagnoseError(stepErrorMessage(3)).title }}</span>
											</div>
											<el-tooltip :content="$t('lz.label.sharedataActivityDetail.copyError')" placement="top">
												<el-button link size="small" class="err-copy-btn" @click="copyText(stepErrorMessage(3))">
													<el-icon class="mr3"><ele-CopyDocument /></el-icon>
													<span>{{ $t('lz.label.sharedataActivityDetail.copy') }}</span>
												</el-button>
											</el-tooltip>
										</div>
										<div v-if="diagnoseError(stepErrorMessage(3)).hint" class="step-err-hint">
											<el-icon class="hint-icon"><ele-InfoFilled /></el-icon>
											<span>{{ diagnoseError(stepErrorMessage(3)).hint }}</span>
										</div>
										<div class="step-err-raw">
											<span class="raw-label">{{ $t('lz.label.sharedataActivityDetail.rawErrorLabel') }}:</span>
											<code class="raw-code">{{ displayedStepError(3) }}</code>
											<el-button
												v-if="isErrorTruncatable(stepErrorMessage(3))"
												link
												type="primary"
												size="small"
												class="raw-toggle-btn"
												@click="toggleStepErrorExpand(3)"
											>
												{{ isStepErrorExpanded(3) ? $t('lz.label.sharedataActivityDetail.collapseError') : $t('lz.label.sharedataActivityDetail.expandError') }}
											</el-button>
										</div>
									</div>
								</template>
							</el-step>
						</el-steps>
						<div v-if="stepDurationText" class="steps-card-footer">
							<span class="duration-badge">
								<el-icon class="mr3"><ele-Timer /></el-icon>
								<span>{{ $t('lz.label.sharedataActivityDetail.duration') }}: <strong>{{ stepDurationText }}</strong></span>
							</span>
						</div>
					</template>
				</div>

				<div class="sect-title">
					<el-icon class="mr5"><ele-Document /></el-icon>
					<span>{{ $t('lz.label.sharedataActivityDetail.packetInfo') }}</span>
				</div>
				<el-descriptions :column="2" size="small" border>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.transferDirection')">
						<el-tag size="small" :type="isSend ? 'success' : 'primary'">
							{{ transferDirectionLabel(row.transferDirection) }}
						</el-tag>
					</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.datatype')">
						{{ datatypeLabel(packetList, row.datatypeId) }}
					</el-descriptions-item>
					<el-descriptions-item :label="hasDifferentPacketNbr ? 'Serial' : 'Serial / ' + $t('lz.label.sharedataActivityDetail.packetNbr')">
						{{ row.serialNbr ?? '—' }}
					</el-descriptions-item>
					<el-descriptions-item v-if="hasDifferentPacketNbr" :label="$t('lz.label.sharedataActivityDetail.packetNbr')">
						{{ row.packetNbr ?? '—' }}
					</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.pduType')">{{ row.pduType || '—' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.format')">{{ formatLabel(row) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.byteSize')">{{ row.byteSize ?? '—' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.recordCount')">{{ row.recordCount ?? '—' }}</el-descriptions-item>
				</el-descriptions>

				<template v-if="row.filePath">
					<div class="sect-title">
						<el-icon class="mr5"><ele-Folder /></el-icon>
						<span>{{ $t('lz.label.sharedataActivityDetail.evidenceFile') }}</span>
					</div>
					<el-descriptions :column="1" size="small" border class="evidence-descriptions">
						<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.filePath')" label-class-name="evidence-label" class-name="evidence-value">
							<span class="evidence-path-text">{{ row.filePath }}</span>
						</el-descriptions-item>
						<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.hash')" label-class-name="evidence-label">
							{{ row.hash || '—' }}
						</el-descriptions-item>
					</el-descriptions>
				</template>
			</template>

			<!-- Nhóm Cấu hình: Thay đổi trường & So sánh trước / sau -->
			<template v-else>
				<template v-if="row.changedFields">
					<div class="sect-title">
						<el-icon class="mr5"><ele-List /></el-icon>
						<span>{{ $t('lz.label.sharedataActivityDetail.changedFields') }}</span>
					</div>
					<div class="keys-wrap">
						<el-tag v-for="k in changedList" :key="k" size="small" effect="plain">{{ k }}</el-tag>
					</div>
				</template>

				<div class="sect-title">
					<el-icon class="mr5"><ele-Switch /></el-icon>
					<span>{{ $t('lz.label.sharedataActivityDetail.compareValues') }}</span>
				</div>
				<el-table
					v-if="diffRows.length"
					:data="diffRows"
					size="small"
					border
					max-height="260"
					:tooltip-options="{ popperClass: 'activity-detail-tooltip' }"
				>
					<el-table-column prop="key" :label="$t('lz.label.sharedataActivityDetail.field')" width="180" header-align="center" show-overflow-tooltip />
					<el-table-column prop="before" :label="$t('lz.label.sharedataActivityDetail.before')" header-align="center" show-overflow-tooltip />
					<el-table-column prop="after" :label="$t('lz.label.sharedataActivityDetail.after')" header-align="center" show-overflow-tooltip />
				</el-table>
				<div v-else class="empty-diff">{{ $t('lz.label.sharedataActivityDetail.noDiff') }}</div>
			</template>
		</template>

		<template #footer>
			<span class="dialog-footer">
				<el-button @click="visible = false">{{ $t('lz.button.base.close') }}</el-button>
			</span>
		</template>
	</el-dialog>
	</div>
</template>
<script lang="ts" setup name="sharedataActivityDetailDialog">
import { useI18n } from 'vue-i18n';
import { computed, onMounted, ref, defineAsyncComponent } from 'vue';
import { DateTimeFormatEnum, BaseConfigTypeEnum } from '/@/types/enums';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi, ShareDataActivityLogApi } from '/@/api-services/shareData';
import { useDateFormat } from '@vueuse/core';
import type { ShareDataActivityLogOutput } from '/@/api-services/shareData';
import { useBaseConfig } from '/@/stores/baseConfig';
import { datatypeLabel } from '/@/utils/sharedata/labelHelper';
import { formatIsoDateTimeText } from '/@/utils/sharedata/historyRange';
import { ElMessage } from 'element-plus';
const { t: $t } = useI18n();
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const LOG_TYPE_TRANSFER = 1;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const SUCCESS_FAIL = 0;
const SUCCESS_OK = 1;

const TRANSFER_SND = 0;
const TRANSFER_RCV = 1;

const visible = ref(false);
const row = ref<ShareDataActivityLogOutput | null>(null);

const isTransfer = computed(() => Number(row.value?.logType) === LOG_TYPE_TRANSFER);
const isSuccess = computed(() => Number(row.value?.success) === SUCCESS_OK);
const isSend = computed(() => Number(row.value?.transferDirection) === TRANSFER_SND);

const processTitleText = computed(() => {
	if (!loadingSteps.value && !steps.value.length) {
		return $t('lz.label.sharedataActivityDetail.processTitleSimple') || 'Tiến trình xử lý';
	}
	return $t('lz.label.sharedataActivityDetail.processTitle');
});

const GUID_PATTERN = /^[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}$/i;

const resolvedPacket = computed(() => {
	const tid = row.value?.targetId;
	const dtid = row.value?.datatypeId;
	const after = parseJson(row.value?.afterJson);
	const before = parseJson(row.value?.beforeJson);
	const packetId = dtid || tid || after?.PacketId || before?.PacketId;
	if (!packetId) return null;
	return packetList.value.find((p) => p.id === packetId || p.code === packetId) ?? null;
});

const partnerDisplayName = computed(() => {
	if (row.value?.partnerName) return row.value.partnerName;
	const isCommonSql = row.value?.targetName === 'SQL mẫu chung'
		|| (row.value?.description && row.value.description.includes('SQL mẫu chung'));
	if (isCommonSql) {
		return 'Mẫu chung (tất cả đối tác)';
	}
	return '—';
});

const resolveTargetName = (): string => {
	if (row.value?.targetName) return row.value.targetName;
	if (isTransfer.value) {
		if (row.value?.datatypeId) {
			const label = datatypeLabel(packetList.value, row.value.datatypeId);
			if (label && label !== '—') return label;
			return row.value.datatypeId;
		}
		if (row.value?.targetId) {
			return $t('lz.label.sharedataActivityDetail.subscriptionTarget', { id: row.value.targetId.slice(0, 8) });
		}
	}
	if (row.value?.targetId) return row.value.targetId;
	return '—';
};

// ID nội bộ (GUID) không có ý nghĩa với người xem nhật ký: thay bằng dấu gạch.
const targetDisplayName = computed(() => {
	const name = resolveTargetName();
	if (name === 'SQL mẫu chung' || name === 'SQL riêng') {
		const pkt = resolvedPacket.value;
		if (pkt) {
			const pktTitle = pkt.name ? `[${pkt.code}] ${pkt.name}` : `[${pkt.code}]`;
			return `${pktTitle} (${name})`;
		}
	}
	return GUID_PATTERN.test(name) ? '—' : name;
});

const contentDescription = computed(() => {
	const desc = cleanDescription(row.value?.description, row.value?.errorMessage);
	if (!desc) return '—';
	if (desc.includes('"SQL mẫu chung"')) {
		const pkt = resolvedPacket.value;
		if (pkt) {
			return desc.replace('"SQL mẫu chung"', `"[${pkt.code}] SQL mẫu chung"`);
		}
	} else if (desc.includes('"SQL riêng"')) {
		const pkt = resolvedPacket.value;
		if (pkt) {
			return desc.replace('"SQL riêng"', `"[${pkt.code}] SQL riêng"`);
		}
	}
	return desc;
});

const changedList = computed(() =>
	(row.value?.changedFields ?? '').split(',').map((s) => s.trim()).filter(Boolean)
);

/** Ghép beforeJson và afterJson thành bảng so sánh, chỉ hiện các trường thực sự đổi. */
const diffRows = computed(() => {
	const before = parseJson(row.value?.beforeJson);
	const after = parseJson(row.value?.afterJson);
	if (!before && !after) return [];

	const keys = changedList.value.length
		? changedList.value
		: Array.from(new Set([...Object.keys(before ?? {}), ...Object.keys(after ?? {})]));

	return keys.map((key) => ({
		key,
		before: formatValueText(before?.[key]),
		after: formatValueText(after?.[key]),
	}));
});



function transferDirectionLabel(direction: any): string {
	if (direction === null || direction === undefined || direction === '') return '—';
	const dir = Number(direction);
	if (dir === TRANSFER_SND) return $t('lz.label.sharedataActivityDetail.directionOut');
	if (dir === TRANSFER_RCV) return $t('lz.label.sharedataActivityDetail.directionIn');
	return String(direction);
}

const packetList = ref<any[]>([]);

const loadPacketList = async () => {
	if (packetList.value.length) return;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketListGet();
		packetList.value = res.data.result ?? [];
	} catch {
		packetList.value = [];
	}
};

onMounted(loadPacketList);

const hasDifferentPacketNbr = computed(() => {
	const p = row.value?.packetNbr;
	const s = row.value?.serialNbr;
	return p !== null && p !== undefined && s !== null && s !== undefined && p !== s;
});

function formatLabel(r?: ShareDataActivityLogOutput | null): string {
	if (!r) return '—';
	if (r.filePath) {
		const ext = r.filePath.split('.').pop()?.toUpperCase();
		if (ext && ext.length <= 5) return ext;
	}
	if (r.format !== null && r.format !== undefined && r.format !== '') {
		const f = Number(r.format);
		if (f === 0) return 'JSON';
		if (f === 1) return $t('lz.label.sharedataActivityDetail.formatFile') || 'File';
	}
	return 'JSON';
}

const loadingSteps = ref(false);
const steps = ref<any[]>([]);

const loadSteps = async (parentId?: string | null) => {
	steps.value = [];
	if (!parentId) return;

	try {
		loadingSteps.value = true;
		const res = await getAPI(ShareDataActivityLogApi).apiSharedataSharedataactivitylogStepsGet(parentId);
		const rows = res.data.result ?? [];

		steps.value = rows[0]?.children?.length ? rows[0].children : rows.filter((r: any) => r.parentId != null);
	} catch {
		steps.value = [];
	} finally {
		loadingSteps.value = false;
	}
};

const stepByNbr = (nbr: number) => steps.value.find((s: any) => Number(s.stepNbr) === nbr);

const isOldTwoStepOutbound = computed(() => {
	return isSend.value &&
		steps.value.length === 2 &&
		!steps.value.some((s: any) => Number(s.stepNbr) === 3) &&
		steps.value.some((s: any) => s.description?.includes('Ánh xạ và gửi'));
});

const hasStep3 = computed(() => isSend.value && !isOldTwoStepOutbound.value);

/** Bước chỉ có hai kết cục: thành công hoặc lỗi. Bản ghi thành công không có dữ liệu của bước nào thì bước đó không được hiển thị. */
function stepVisible(nbr: number): boolean {
	return !isSuccess.value || Boolean(stepByNbr(nbr));
}

function stepStatus(nbr: number): 'process' | 'success' | 'error' {
	if (loadingSteps.value) return 'process';
	const step = stepByNbr(nbr);
	if (!step) return 'error';
	return Number(step.success) === SUCCESS_OK ? 'success' : 'error';
}

function stepSubtitle(nbr: number): string {
	const step = stepByNbr(nbr);
	const status = stepStatus(nbr);
	if (!step && status === 'error') {
		return $t('lz.label.sharedataActivityDetail.zeroRecords');
	}
	const time = stepTime(nbr);
	if (status === 'success') {
		return time
			? ($t('lz.label.sharedataActivityDetail.completedAt', { time }) !== 'lz.label.sharedataActivityDetail.completedAt'
				? $t('lz.label.sharedataActivityDetail.completedAt', { time })
				: `Hoàn tất lúc ${time}`)
			: ($t('lz.label.sharedataActivityDetail.resultSuccess') || 'Hoàn tất');
	}
	if (status === 'error') {
		return time
			? ($t('lz.label.sharedataActivityDetail.failedAt', { time }) !== 'lz.label.sharedataActivityDetail.failedAt'
				? $t('lz.label.sharedataActivityDetail.failedAt', { time })
				: `Thất bại lúc ${time}`)
			: ($t('lz.label.sharedataActivityDetail.resultFailed') || 'Thất bại');
	}
	if (status === 'process') {
		return $t('lz.label.sharedataActivityDetail.processing') || 'Đang xử lý...';
	}
	return '';
}

interface ErrorDiagnosis {
	title: string;
	hint: string;
}

/**
 * Làm sạch chuỗi mô tả, loại bỏ phần nối chuỗi lỗi kỹ thuật lặp lại để giữ giao diện gọn gàng.
 */
function cleanDescription(desc?: string | null, errMsg?: string | null): string {
	if (!desc) return '';
	let result = desc.trim();
	if (errMsg) {
		const trimmedErr = errMsg.trim();
		if (trimmedErr && result.endsWith(trimmedErr)) {
			result = result.slice(0, -trimmedErr.length).trim();
			result = result.replace(/[:\-–—]\s*$/, '').trim();
		}
	}
	return result;
}

/**
 * Phân tích lỗi kỹ thuật thành chẩn đoán trực quan và gợi ý khắc phục thân thiện.
 */
function diagnoseError(err?: string | null): ErrorDiagnosis {
	if (!err) {
		return {
			title: $t('lz.label.sharedataActivityDetail.diagGeneralError'),
			hint: '',
		};
	}

	const lower = err.toLowerCase();

	if (lower.includes('ssl') || lower.includes('certificate') || lower.includes('trust relationship') || lower.includes('remote certificate')) {
		return {
			title: $t('lz.label.sharedataActivityDetail.diagSsl'),
			hint: $t('lz.label.sharedataActivityDetail.diagSslHint'),
		};
	}

	if (lower.includes('refused') || lower.includes('actively refused')) {
		return {
			title: $t('lz.label.sharedataActivityDetail.diagRefused'),
			hint: $t('lz.label.sharedataActivityDetail.diagRefusedHint'),
		};
	}

	if (lower.includes('timed out') || lower.includes('timeout') || lower.includes('taskcanceledexception')) {
		return {
			title: $t('lz.label.sharedataActivityDetail.diagTimeout'),
			hint: $t('lz.label.sharedataActivityDetail.diagTimeoutHint'),
		};
	}

	if (lower.includes('401') || lower.includes('unauthorized') || lower.includes('403') || lower.includes('forbidden')) {
		return {
			title: $t('lz.label.sharedataActivityDetail.diagAuth'),
			hint: $t('lz.label.sharedataActivityDetail.diagAuthHint'),
		};
	}

	if (lower.includes('404') || lower.includes('not found')) {
		return {
			title: $t('lz.label.sharedataActivityDetail.diagNotFound'),
			hint: $t('lz.label.sharedataActivityDetail.diagNotFoundHint'),
		};
	}

	if (lower.includes('500') || lower.includes('502') || lower.includes('503') || lower.includes('bad gateway') || lower.includes('internal server error')) {
		return {
			title: $t('lz.label.sharedataActivityDetail.diagServerError'),
			hint: $t('lz.label.sharedataActivityDetail.diagServerErrorHint'),
		};
	}

	if (lower.includes('sending the request') || lower.includes('socketexception') || lower.includes('httprequestexception') || lower.includes('network')) {
		return {
			title: $t('lz.label.sharedataActivityDetail.diagConnectionFailed'),
			hint: $t('lz.label.sharedataActivityDetail.diagConnectionFailedHint'),
		};
	}

	return {
		title: $t('lz.label.sharedataActivityDetail.diagGeneralError'),
		hint: '',
	};
}

/**
 * Sao chép văn bản vào bộ nhớ tạm.
 */
async function copyText(text?: string | null) {
	if (!text) return;
	try {
		if (navigator?.clipboard?.writeText) {
			await navigator.clipboard.writeText(text);
		} else {
			const textarea = document.createElement('textarea');
			textarea.value = text;
			textarea.style.position = 'fixed';
			textarea.style.opacity = '0';
			document.body.appendChild(textarea);
			textarea.select();
			document.execCommand('copy');
			document.body.removeChild(textarea);
		}
		ElMessage.success($t('lz.label.sharedataActivityDetail.copiedError'));
	} catch {
		ElMessage.warning($t('lz.label.sharedataActivityDetail.copyFailed'));
	}
}

function stepDescription(nbr: number): string {
	const step = stepByNbr(nbr);

	if (!step) {
		if (isSend.value) {
			if (nbr === 1) return $t('lz.label.sharedataActivityDetail.stepExtractDesc');
			if (nbr === 2) return isOldTwoStepOutbound.value ? $t('lz.label.sharedataActivityDetail.stepLegacySendDesc') : $t('lz.label.sharedataActivityDetail.stepMapDesc');
			return $t('lz.label.sharedataActivityDetail.stepSendDesc');
		}
		return $t(nbr === 1 ? 'lz.label.sharedataActivityDetail.stepReceiveDesc' : 'lz.label.sharedataActivityDetail.stepStoreDesc');
	}

	const desc = step.description || '';
	const err = step.errorMessage || '';
	const cleaned = cleanDescription(desc, err);
	return cleaned || desc || '—';
}

function stepErrorMessage(nbr: number): string {
	const step = stepByNbr(nbr);
	return step?.errorMessage || '';
}

function stepTime(nbr: number): string {
	const step = stepByNbr(nbr);
	const time = step?.occurredAt || step?.createTime;
	if (!time) return '';
	const formatted = fmt(time);
	return formatted === '—' ? '' : formatted;
}

const stepDurationText = computed(() => {
	const s1 = stepByNbr(1);
	const lastStep = (hasStep3.value && stepByNbr(3)) ? stepByNbr(3) : stepByNbr(2);
	const t1 = s1?.occurredAt || s1?.createTime;
	const t2 = lastStep?.occurredAt || lastStep?.createTime;
	if (!t1 || !t2) return '';
	const d1 = new Date(t1).getTime();
	const d2 = new Date(t2).getTime();
	if (isNaN(d1) || isNaN(d2) || d2 < d1) return '';
	const diffMs = d2 - d1;
	if (diffMs < 1000) return `${diffMs} ms`;
	return `${(diffMs / 1000).toFixed(2)} s`;
});

const fmt = (s?: string | null) => {
	if (!s) return '—';
	const d = new Date(s);
	return Number.isNaN(d.getTime()) ? '—' : useDateFormat(d, DateTimeFormatEnum.DateTimeDefault).value;
};

function parseJson(json?: string | null): Record<string, any> | null {
	if (!json) return null;
	try {
		return JSON.parse(json);
	} catch {
		return null;
	}
}

function formatValueText(v: any): string {
	if (v === null || v === undefined || v === '') return '—';
	if (typeof v === 'boolean') return v ? $t('lz.label.sharedataActivityDetail.yes') : $t('lz.label.sharedataActivityDetail.no');
	if (typeof v === 'string') return formatIsoDateTimeText(v) ?? v;
	return String(v);
}

async function open(r: ShareDataActivityLogOutput) {
	row.value = r;
	expandedSteps.value = {};
	visible.value = true;

	const promises: Promise<any>[] = [];

	if (r.id) {
		promises.push(
			getAPI(ShareDataActivityLogApi).apiSharedataSharedataactivitylogByidGet(r.id)
				.then((res) => {
					if (res.data?.result) {
						row.value = { ...row.value, ...res.data.result };
					}
				})
				.catch(() => {})
		);
	}

	if (Number(r.logType) === LOG_TYPE_TRANSFER && r.id) {
		promises.push(loadSteps(r.id));
	}

	if (!packetList.value.length) {
		promises.push(loadPacketList());
	}

	await Promise.all(promises);
}

function close() {
	visible.value = false;
	steps.value = [];
	expandedSteps.value = {};
	loadingSteps.value = false;
}

const MAX_RAW_ERROR_LENGTH = 200;
const expandedSteps = ref<Record<number, boolean>>({});

function truncateErrorText(text?: string | null, maxLen = MAX_RAW_ERROR_LENGTH): string {
	if (!text) return '';
	if (text.length <= maxLen) return text;
	let cutIndex = maxLen;
	const lastCode = text.charCodeAt(cutIndex - 1);
	if (lastCode >= 0xd800 && lastCode <= 0xdbff) {
		cutIndex -= 1;
	}
	return `${text.slice(0, cutIndex)}…`;
}

function isErrorTruncatable(text?: string | null): boolean {
	return Boolean(text && text.length > MAX_RAW_ERROR_LENGTH);
}

function isStepErrorExpanded(nbr: number): boolean {
	return Boolean(expandedSteps.value[nbr]);
}

function toggleStepErrorExpand(nbr: number) {
	expandedSteps.value[nbr] = !expandedSteps.value[nbr];
}

function displayedStepError(nbr: number): string {
	const msg = stepErrorMessage(nbr);
	if (!msg) return '';
	return isStepErrorExpanded(nbr) ? msg : truncateErrorText(msg);
}

function stepMissingReason(nbr: number): string {
	if (loadingSteps.value || isSuccess.value || stepByNbr(nbr)) return '';
	for (let prev = nbr - 1; prev >= 1; prev--) {
		const prevErr = stepErrorMessage(prev);
		if (prevErr) return truncateErrorText(prevErr);
	}
	const anyStepErr = steps.value.find((s: any) => Boolean(s?.errorMessage))?.errorMessage;
	if (anyStepErr) return truncateErrorText(anyStepErr);
	if (row.value?.errorMessage) return truncateErrorText(row.value.errorMessage);
	return $t('lz.label.sharedataActivityDetail.stepNotRunDueToPrevError');
}

defineExpose({ open, close });
</script>


<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.el-popper.activity-detail-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}
</style>

<style scoped lang="scss">
.dialog-header {
	display: flex;
	align-items: center;
	color: var(--el-color-white);
}

.sect-title {
	display: flex;
	align-items: center;
	margin: 16px 0 8px;
	font-size: 13px;
	color: var(--el-text-color-primary);
}

.steps-card {
	padding: 20px 24px 16px;
	background-color: var(--el-bg-color, #ffffff);
	border: 1px solid var(--el-border-color-light);
	border-radius: 8px;
	margin-bottom: 12px;
	min-height: 120px;

	:deep(.el-loading-mask) {
		background-color: var(--el-bg-color, #ffffff);
		backdrop-filter: none;
		-webkit-backdrop-filter: none;
		border-radius: 8px;
	}

	.steps-loading-placeholder {
		min-height: 140px;
		width: 100%;
	}

	&.is-empty {
		background-color: var(--el-fill-color-light);
		border: 1px solid var(--el-border-color-lighter);
		min-height: 120px;
		padding: 20px 24px 16px;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	:deep(.el-step.is-vertical) {
		min-height: 80px;
		display: flex;
		align-items: stretch;

		&:last-child,
		&:last-of-type {
			min-height: auto;
			.el-step__line {
				display: none;
			}
			.el-step__main {
				padding-bottom: 0;
			}
		}

		.el-step__head {
			width: 24px;
			position: relative;
			flex-shrink: 0;
			flex-grow: 0;

			.el-step__icon {
				width: 24px;
				height: 24px;
				border: none;
				background: transparent;
				z-index: 2;
				position: relative;
			}

			.el-step__line {
				left: 11px;
				top: 24px;
				bottom: 0px;
				width: 2px;
				background-color: var(--el-border-color-lighter);
				display: block;
				z-index: 1;

				.el-step__line-inner {
					display: none;
				}
			}
		}

		.el-step__main {
			flex-grow: 1;
			padding-left: 16px;
			padding-bottom: 24px;
		}

		.el-step__title {
			line-height: normal;
			padding-bottom: 0;
			color: inherit;

			&.is-success,
			&.is-error,
			&.is-process,
			&.is-wait {
				color: inherit;
			}
		}

		.el-step__description {
			padding-right: 0;
			margin-top: 3px;
			color: inherit;
		}
	}

	.custom-step-node {
		width: 24px;
		height: 24px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		box-sizing: border-box;
		position: relative;

		&.is-success {
			background-color: var(--el-color-success-light-9);
			border: 1px solid var(--el-color-success-light-8);
			color: var(--el-color-success);

			.check-icon {
				display: block;
			}
		}

		&.is-error {
			background-color: var(--el-color-danger-light-9);
			border: 1px solid var(--el-color-danger-light-8);
			color: var(--el-color-danger);

			.err-icon {
				display: block;
			}
		}

		&.is-process {
			background-color: var(--el-color-primary-light-9);
			border: 1px solid var(--el-color-primary-light-8);
			border-top-color: var(--el-color-primary);
			color: var(--el-color-primary);
			animation: step-arc-spin 0.9s linear infinite;
		}
	}

	.step-title-text {
		font-size: 14.5px;
		font-weight: 600;
		color: var(--el-text-color-primary);
		line-height: 24px;
		letter-spacing: -0.01em;
	}

	.step-subtitle {
		font-size: 13px;
		color: var(--el-text-color-secondary);
		line-height: 1.4;
		margin-top: 2px;
		font-weight: 400;

		&.is-err {
			color: var(--el-color-danger);
			font-weight: 500;
		}
	}

	.empty-process-simple {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 10px;
		width: 100%;
		padding: 0;
		background-color: transparent;

		.empty-icon {
			font-size: 24px;
			color: var(--el-text-color-secondary);
			flex-shrink: 0;
		}

		.empty-text {
			font-size: 14.5px;
			font-weight: 500;
			color: var(--el-text-color-regular);
			line-height: 1.5;
		}
	}

	.step-desc-text {
		font-size: 12.5px;
		color: var(--el-text-color-regular);
		line-height: 1.5;
		word-break: break-word;
	}

	.step-err-box {
		margin-top: 8px;
		padding: 10px 14px;
		background-color: var(--el-color-danger-light-9);
		border: 1px solid var(--el-color-danger-light-8);
		border-left: 3px solid var(--el-color-danger);
		border-radius: 6px;
		max-width: 100%;
		display: flex;
		flex-direction: column;
		gap: 6px;
		line-height: 1.45;
		word-break: break-word;

		.step-err-header {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: 8px;

			.step-err-title {
				display: flex;
				align-items: center;
				gap: 6px;
				color: var(--el-color-danger);
				font-weight: 600;
				font-size: 13px;
				line-height: 1.4;

				.err-header-icon {
					font-size: 15px;
					flex-shrink: 0;
				}
			}

			.err-copy-btn {
				font-size: 12px;
				color: var(--el-color-danger);
				padding: 2px 6px;
				height: auto;

				&:hover {
					color: var(--el-color-danger-dark-2);
					background-color: var(--el-color-danger-light-8);
				}
			}
		}

		.step-err-hint {
			display: flex;
			align-items: flex-start;
			gap: 5px;
			font-size: 12px;
			color: var(--el-text-color-regular);
			line-height: 1.45;

			.hint-icon {
				font-size: 13px;
				margin-top: 2px;
				color: var(--el-color-warning);
				flex-shrink: 0;
			}
		}

		.step-err-raw {
			display: flex;
			align-items: baseline;
			flex-wrap: wrap;
			gap: 6px;
			font-size: 11.5px;
			margin-top: 2px;

			.raw-label {
				color: var(--el-text-color-secondary);
				font-weight: 500;
				flex-shrink: 0;
			}

			.raw-code {
				background-color: var(--el-fill-color-blank);
				border: 1px solid var(--el-color-danger-light-7);
				border-radius: 4px;
				padding: 2px 8px;
				color: var(--el-color-danger-dark-2);
				font-family: Consolas, Monaco, monospace;
				word-break: break-all;
				font-size: 11.5px;
				line-height: 1.4;
			}
		}
	}

	.step-err-callout {
		/* alias để tương thích ngược */
		box-sizing: border-box;
	}

	.main-err-wrapper {
		display: flex;
		align-items: center;
		gap: 6px;
		word-break: break-word;

		.err-text {
			color: var(--el-color-danger);
			font-weight: 500;
		}

		.copy-err-btn {
			padding: 2px 4px;
			height: auto;
			color: var(--el-color-danger);

			&:hover {
				color: var(--el-color-danger-dark-2);
			}
		}
	}

	.steps-card-footer {
		margin-top: 10px;
		padding-top: 10px;
		border-top: 1px dashed var(--el-border-color-lighter);
		display: flex;
		align-items: center;
		justify-content: flex-end;

		.duration-badge {
			display: inline-flex;
			align-items: center;
			font-size: 12px;
			color: var(--el-text-color-secondary);
			background-color: var(--el-fill-color-light);
			padding: 3px 10px;
			border-radius: 4px;
			border: 1px solid var(--el-border-color-lighter);

			strong {
				margin-left: 4px;
				color: var(--el-color-primary);
				font-weight: 600;
			}
		}
	}
}

@keyframes step-arc-spin {
	0% {
		transform: rotate(0deg);
	}
	100% {
		transform: rotate(360deg);
	}
}

.keys-wrap {
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
	margin-bottom: 8px;
}

.empty-diff {
	padding: 16px;
	text-align: center;
	font-size: 13px;
	color: var(--el-text-color-secondary);
	background-color: var(--el-fill-color-light);
	border-radius: 4px;
}

.err-text {
	color: var(--el-color-danger);
	font-weight: 500;
}

.evidence-descriptions {
	:deep(.evidence-label) {
		width: 130px;
		min-width: 130px;
		white-space: nowrap;
	}

	:deep(.evidence-value) {
		word-break: break-all;
	}

	.evidence-path-text {
		font-family: Consolas, Monaco, monospace;
		font-size: 12px;
	}
}

.step-title-wrap {
	display: inline-block;
}

.raw-toggle-btn {
	font-size: 11.5px;
	padding: 0 4px;
	height: auto;
}
</style>
