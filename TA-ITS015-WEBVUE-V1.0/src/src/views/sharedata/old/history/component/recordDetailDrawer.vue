<template>
	<el-drawer v-model="visible" :title="$t('lz.label.sharedataRecordDetail.title')" size="480px" direction="rtl">
		<template v-if="record">
			<el-descriptions :column="1" border size="small">
				<el-descriptions-item v-for="f in fields" :key="f.key" :label="f.label">
					{{ f.value }}
				</el-descriptions-item>
			</el-descriptions>

			<div class="drawer-json">
				<div class="drawer-json__title">{{ $t('lz.label.sharedataRecordDetail.originalDataJson') }}</div>
				<pre class="drawer-json__pre">{{ jsonText }}</pre>
			</div>
		</template>
		<el-empty v-else :description="$t('lz.label.sharedataRecordDetail.noData')" />
	</el-drawer>
</template>

<script lang="ts" setup name="sharedataRecordDetailDrawer">
import { computed, onMounted, ref } from 'vue';
import { DateTimeFormatEnum } from '/@/types/enums';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { datatypeLabel } from '/@/utils/sharedata/labelHelper';

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

/** Mã lý do theo ISO 14827-2 (từ chối Login / Logout / quản lý xuất bản) → nhãn dịch. */
const REASON_LABELS = computed<Record<string, string>>(() => ({
	unknownDomainName: $t('lz.label.sharedataRecordDetail.reasonUnknownDomain'),
	accessDenied: $t('lz.label.sharedataRecordDetail.reasonAccessDenied'),
	invalidNamePassword: $t('lz.label.sharedataRecordDetail.reasonInvalidCredentials'),
	timeoutTooSmall: $t('lz.label.sharedataRecordDetail.reasonTimeoutTooSmall'),
	timeoutTooLarge: $t('lz.label.sharedataRecordDetail.reasonTimeoutTooLarge'),
	heartbeatTooSmall: $t('lz.label.sharedataRecordDetail.reasonHeartbeatTooSmall'),
	heartbeatTooLarge: $t('lz.label.sharedataRecordDetail.reasonHeartbeatTooLarge'),
	sessionExists: $t('lz.label.sharedataRecordDetail.reasonSessionExists'),
	maxSessionsReached: $t('lz.label.sharedataRecordDetail.reasonMaxSessionsReached'),
	other: $t('lz.label.sharedataRecordDetail.reasonOther'),
	serverRequested: $t('lz.label.sharedataRecordDetail.reasonServerRequested'),
	clientRequested: $t('lz.label.sharedataRecordDetail.reasonClientRequested'),
	serverShutdown: $t('lz.label.sharedataRecordDetail.reasonServerShutdown'),
	clientShutdown: $t('lz.label.sharedataRecordDetail.reasonClientShutdown'),
	serverCommProblems: $t('lz.label.sharedataRecordDetail.reasonServerCommProblems'),
	clientCommProblems: $t('lz.label.sharedataRecordDetail.reasonClientCommProblems'),
	temporarilySuspended: $t('lz.label.sharedataRecordDetail.reasonTemporarilySuspended'),
	resume: $t('lz.label.sharedataRecordDetail.reasonResume'),
	unknownRequest: $t('lz.label.sharedataRecordDetail.reasonUnknownRequest'),
}));

/** Diễn giải mã lý do; trả lại mã gốc nếu không có trong danh mục. */
function reasonLabel(code?: string): string {
	if (!code) return '';
	return REASON_LABELS.value[code] ?? code;
}

const visible = ref(false);
const record = ref<Record<string, any> | null>(null);

// Nhãn cho từng khóa dữ liệu (dùng chung mọi loại nhật ký).
const LABELS = computed<Record<string, string>>(() => ({
	partnerName: $t('lz.label.sharedataRecordDetail.partnerName'),
	partnerId: $t('lz.label.sharedataRecordDetail.partnerId'),
	direction: $t('lz.label.sharedataRecordDetail.direction'),
	state: $t('lz.label.sharedataRecordDetail.state'),
	startedAt: $t('lz.label.sharedataRecordDetail.startedAt'),
	acceptedAt: $t('lz.label.sharedataRecordDetail.acceptedAt'),
	endedAt: $t('lz.label.sharedataRecordDetail.endedAt'),
	endReason: $t('lz.label.sharedataRecordDetail.endReason'),
	packetsSent: $t('lz.label.sharedataRecordDetail.packetsSent'),
	packetsRecv: $t('lz.label.sharedataRecordDetail.packetsRecv'),
	heartbeatRttMs: $t('lz.label.sharedataRecordDetail.heartbeatRttMs'),
	negotiatedDatagramSize: $t('lz.label.sharedataRecordDetail.negotiatedDatagramSize'),
	requestedAt: $t('lz.label.sharedataRecordDetail.requestedAt'),
	resolvedAt: $t('lz.label.sharedataRecordDetail.resolvedAt'),
	datatypeId: $t('lz.label.sharedataRecordDetail.datatypeId'),
	mode: $t('lz.label.sharedataRecordDetail.mode'),
	format: $t('lz.label.sharedataRecordDetail.format'),
	priority: $t('lz.label.sharedataRecordDetail.priority'),
	serialNbr: $t('lz.label.sharedataRecordDetail.serialNbr'),
	sentAt: $t('lz.label.sharedataRecordDetail.sentAt'),
	confirmedAt: $t('lz.label.sharedataRecordDetail.confirmedAt'),
	byteSize: $t('lz.label.sharedataRecordDetail.byteSize'),
	fragmentCount: $t('lz.label.sharedataRecordDetail.fragmentCount'),
	retryCount: $t('lz.label.sharedataRecordDetail.retryCount'),
	loggedAt: $t('lz.label.sharedataRecordDetail.loggedAt'),
	pduType: $t('lz.label.sharedataRecordDetail.pduType'),
	packetNbr: $t('lz.label.sharedataRecordDetail.packetNbr'),
	fredConfirmNbr: $t('lz.label.sharedataRecordDetail.fredConfirmNbr'),
	crcOk: $t('lz.label.sharedataRecordDetail.crcOk'),
	rejectReason: $t('lz.label.sharedataRecordDetail.rejectReason'),
	cancelReason: $t('lz.label.sharedataRecordDetail.cancelReason'),
	// AlertLog
	at: $t('lz.label.sharedataRecordDetail.at'),
	severity: $t('lz.label.sharedataRecordDetail.severity'),
	source: $t('lz.label.sharedataRecordDetail.source'),
	code: $t('lz.label.sharedataRecordDetail.code'),
	message: $t('lz.label.sharedataRecordDetail.message'),
	acknowledged: $t('lz.label.sharedataRecordDetail.acknowledged'),
	ackBy: $t('lz.label.sharedataRecordDetail.ackBy'),
	ackAt: $t('lz.label.sharedataRecordDetail.ackAt'),
}));

const SEVERITY_MAP = computed<Record<string, string>>(() => ({
	warning: $t('lz.label.sharedataRecordDetail.severityWarning'),
	error: $t('lz.label.sharedataRecordDetail.severityError'),
}));
const SOURCE_MAP = computed<Record<string, string>>(() => ({
	session: $t('lz.label.sharedataRecordDetail.sourceSession'),
	packet: $t('lz.label.sharedataRecordDetail.sourcePacket'),
	subscription: $t('lz.label.sharedataRecordDetail.sourceSubscription'),
	publication: $t('lz.label.sharedataRecordDetail.sourcePublication'),
	protocol: $t('lz.label.sharedataRecordDetail.sourceProtocol'),
}));

const DIRECTION_MAP = computed<Record<string, string>>(() => ({
	OUT: $t('lz.label.sharedataRecordDetail.directionOut'),
	IN: $t('lz.label.sharedataRecordDetail.directionIn'),
	OUTBOUND: $t('lz.label.sharedataRecordDetail.directionOutbound'),
	INBOUND: $t('lz.label.sharedataRecordDetail.directionInbound'),
	SND: $t('lz.label.sharedataRecordDetail.directionSnd'),
	RCV: $t('lz.label.sharedataRecordDetail.directionRcv'),
}));

function isIsoDate(v: any): boolean {
	return typeof v === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(v);
}

function formatValue(key: string, v: any): string {
	if (v === null || v === undefined || v === '') return '—';
	if (key === 'datatypeId') return datatypeLabel(packetList.value, v);
	if (key === 'direction') return DIRECTION_MAP.value[v] ?? String(v);
	if (key === 'crcOk') return v ? $t('lz.label.sharedataRecordDetail.crcOkValue') : $t('lz.label.sharedataRecordDetail.crcErrorValue');
	if (key === 'acknowledged') return v ? $t('lz.label.sharedataRecordDetail.acknowledgedValue') : $t('lz.label.sharedataRecordDetail.notAcknowledgedValue');
	if (key === 'severity') return SEVERITY_MAP.value[v] ?? String(v);
	if (key === 'source') return SOURCE_MAP.value[v] ?? String(v);
	if (key === 'code' || key === 'message') return String(v);
	if (key.endsWith('Reason')) return reasonLabel(v);
	if (isIsoDate(v)) return useDateFormat(new Date(v), DateTimeFormatEnum.DateTimeDefault).value;
	return String(v);
}

// Chỉ hiển thị các trường vô hướng, ưu tiên các khóa đã biết trước.
const fields = computed(() => {
	if (!record.value) return [] as { key: string; label: string; value: string }[];
	const out: { key: string; label: string; value: string }[] = [];
	for (const [k, v] of Object.entries(record.value)) {
		if (k === 'id' || typeof v === 'object') continue;
		out.push({ key: k, label: LABELS.value[k] ?? k, value: formatValue(k, v) });
	}
	return out;
});

const jsonText = computed(() => (record.value ? JSON.stringify(record.value, null, 2) : ''));

function open(row: Record<string, any>) {
	record.value = row;
	visible.value = true;
}

defineExpose({ open });
</script>

<style scoped lang="scss">
:deep(.el-drawer__header) {
	margin-bottom: 0;
	padding: 14px 16px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}

:deep(.el-drawer__body) {
	padding: 12px 16px 16px;
	background: var(--el-bg-color-page);
}

.drawer-json {
	margin-top: 14px;
}

.drawer-json__title {
	font-weight: 600;
	margin-bottom: 6px;
	color: var(--el-text-color-primary);
}

.drawer-json__pre {
	background: var(--el-fill-color-light);
	border: 1px solid var(--el-border-color-lighter);
	color: var(--el-text-color-primary);
	padding: 10px;
	border-radius: 4px;
	font-size: 12px;
	max-height: 300px;
	overflow: auto;
	white-space: pre-wrap;
	word-break: break-all;
}
</style>
