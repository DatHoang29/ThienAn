<template>
	<el-drawer v-model="visible" :title="$t('lz.label.sharedataActivityDetail.title')" size="620px" destroy-on-close>
		<template v-if="row">
			<el-descriptions :column="1" size="small" border>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.time')">{{ fmt(row.occurredAt) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.group')">{{ row.logType === 'TRANSFER' ? $t('lz.label.sharedataActivityDetail.groupTransfer') : $t('lz.label.sharedataActivityDetail.groupConfig')
				}}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.action')">{{ actionLabel(row.action) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.result')">
					<el-tag size="small" :type="row.status === 'FAILED' ? 'danger' : 'success'">{{ row.status === 'FAILED'
						? $t('lz.label.sharedataActivityDetail.resultFailed') : $t('lz.label.sharedataActivityDetail.resultSuccess') }}</el-tag>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.content')">{{ row.description || '—' }}</el-descriptions-item>
				<el-descriptions-item v-if="row.errorMessage" :label="$t('lz.label.sharedataActivityDetail.error')">
					<span class="err">{{ row.errorMessage }}</span>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.target')">{{ row.targetName || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.partner')">{{ row.partnerName || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.operator')">{{ row.operatorName || '—' }}</el-descriptions-item>
				<el-descriptions-item v-if="row.operatorIp" :label="$t('lz.label.sharedataActivityDetail.operatorIp')">{{ row.operatorIp }}</el-descriptions-item>
			</el-descriptions>

			<!-- Riêng nhóm Truyền nhận -->
			<template v-if="row.logType === 'TRANSFER'">
				<div class="sect">{{ $t('lz.label.sharedataActivityDetail.packetInfo') }}</div>
				<el-descriptions :column="2" size="small" border>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.direction')">{{ row.transferDirection === 'SND' ? $t('lz.label.sharedataActivityDetail.directionOut') :
						row.transferDirection === 'RCV' ? $t('lz.label.sharedataActivityDetail.directionIn') : '—' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.datatype')">{{ datatypeLabel(packetList, row.datatypeId) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.serial')">{{ row.serialNbr ?? '—' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.packetNbr')">{{ row.packetNbr ?? '—' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.pduType')">{{ row.pduType || '—' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.format')">{{ row.format || '—' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.byteSize')">{{ row.byteSize ?? '—' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.recordCount')">{{ row.recordCount ?? '—' }}</el-descriptions-item>
				</el-descriptions>

				<template v-if="row.filePath">
					<div class="sect">{{ $t('lz.label.sharedataActivityDetail.evidenceFile') }}</div>
					<el-descriptions :column="1" size="small" border>
						<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.filePath')">{{ row.filePath }}</el-descriptions-item>
						<el-descriptions-item :label="$t('lz.label.sharedataActivityDetail.hash')">{{ row.hash || '—' }}</el-descriptions-item>
					</el-descriptions>
				</template>
			</template>

			<!-- Riêng nhóm Cấu hình: so sánh trước/sau -->
			<template v-else>
				<template v-if="row.changedFields">
					<div class="sect">{{ $t('lz.label.sharedataActivityDetail.changedFields') }}</div>
					<div class="keys-wrap">
						<el-tag v-for="k in changedList" :key="k" size="small">{{ k }}</el-tag>
					</div>
				</template>

				<div v-if="diffRows.length" class="sect">{{ $t('lz.label.sharedataActivityDetail.compareValues') }}</div>
				<el-table v-if="diffRows.length" :data="diffRows" size="small" border>
					<el-table-column prop="key" :label="$t('lz.label.sharedataActivityDetail.field')" width="180" show-overflow-tooltip />
					<el-table-column prop="before" :label="$t('lz.label.sharedataActivityDetail.before')" show-overflow-tooltip />
					<el-table-column prop="after" :label="$t('lz.label.sharedataActivityDetail.after')" show-overflow-tooltip />
				</el-table>
			</template>
		</template>
	</el-drawer>
</template>

<script lang="ts" setup name="sharedataActivityDetailDrawer">
import { computed, onMounted, ref } from 'vue';
import { DateTimeFormatEnum } from '/@/types/enums';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import type { ShareDataActivityLogOutput } from '/@/api-services/shareData';
import { datatypeLabel } from '/@/utils/sharedata/labelHelper';
import { formatIsoDateTimeText } from '/@/utils/sharedata/historyRange';

const { t: $t } = useI18n();

/**
 * Tên gói tin lấy thẳng từ danh sách gói tin (ShareDataPacket).
 * DatatypeId lưu ID gói tin, nên tra theo ID; không còn dùng danh mục shareData_type.
 */
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

const visible = ref(false);
const row = ref<ShareDataActivityLogOutput | null>(null);

const ACTION_LABELS = computed<Record<string, string>>(() => ({
	CREATE: $t('lz.label.sharedataActivityDetail.actionCreate'),
	UPDATE: $t('lz.label.sharedataActivityDetail.actionUpdate'),
	DELETE: $t('lz.label.sharedataActivityDetail.actionDelete'),
	CONNECT: $t('lz.label.sharedataActivityDetail.actionConnect'),
	DISCONNECT: $t('lz.label.sharedataActivityDetail.actionDisconnect'),
	PAUSE: $t('lz.label.sharedataActivityDetail.actionPause'),
	RESUME: $t('lz.label.sharedataActivityDetail.actionResume'),
	CANCEL: $t('lz.label.sharedataActivityDetail.actionCancel'),
	APPROVE: $t('lz.label.sharedataActivityDetail.actionApprove'),
	REJECT: $t('lz.label.sharedataActivityDetail.actionReject'),
	SEND: $t('lz.label.sharedataActivityDetail.actionSend'),
	RECEIVE: $t('lz.label.sharedataActivityDetail.actionReceive'),
	EXPORT: $t('lz.label.sharedataActivityDetail.actionExport'),
}));

function actionLabel(action?: string): string {
	if (!action) return '—';
	return ACTION_LABELS.value[action] ?? action;
}

/** Ngày + giờ đầy đủ; định dạng do useDateFormat lo, chỉ giữ chốt rỗng / không hợp lệ để trả '—'. */
const fmt = (s?: string) => {
	if (!s) return '—';
	const d = new Date(s);
	return Number.isNaN(d.getTime()) ? '—' : useDateFormat(d, DateTimeFormatEnum.DateTimeDefault).value;
};

const changedList = computed(() =>
	(row.value?.changedFields ?? '').split(',').map((s) => s.trim()).filter(Boolean)
);

/** Ghép beforeJson và afterJson thành bảng so sánh, chỉ hiện các trường thực sự đổi. */
const diffRows = computed(() => {
	const before = parse(row.value?.beforeJson);
	const after = parse(row.value?.afterJson);
	if (!before && !after) return [];

	const keys = changedList.value.length
		? changedList.value
		: Array.from(new Set([...Object.keys(before ?? {}), ...Object.keys(after ?? {})]));

	return keys.map((key) => ({
		key,
		before: text(before?.[key]),
		after: text(after?.[key]),
	}));
});

function parse(json?: string): Record<string, any> | null {
	if (!json) return null;
	try {
		return JSON.parse(json);
	} catch {
		return null;
	}
}

function text(v: any): string {
	if (v === null || v === undefined || v === '') return '—';
	if (typeof v === 'boolean') return v ? $t('lz.label.sharedataActivityDetail.yes') : $t('lz.label.sharedataActivityDetail.no');
	if (typeof v === 'string') return formatIsoDateTimeText(v) ?? v;
	return String(v);
}

function open(r: ShareDataActivityLogOutput) {
	row.value = r;
	visible.value = true;
}

defineExpose({ open });
</script>

<style scoped lang="scss">
.sect {
	margin: 14px 0 6px;
	font-weight: 600;
	font-size: 13px;
	color: var(--el-text-color-primary);
}

.keys-wrap {
	display: flex;
	flex-wrap: wrap;
	gap: 4px;
}

.err {
	color: var(--el-color-danger);
}
</style>
