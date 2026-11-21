<template>
	<div class="eshare-sharing">
		<!-- TỔNG QUAN (thẻ thống kê) -->
		<div class="stat-row">
			<el-card shadow="hover" class="stat-card" :body-style="{ padding: '12px 14px' }">
				<div class="stat">
					<div class="stat__ic ic-success"><el-icon :size="20"><ele-Connection /></el-icon></div>
					<div>
						<div class="stat__v">{{ summary.activeSessions }}</div>
						<div class="stat__l">{{ $t('lz.label.sharedataSharing.activeSessions') }}</div>
					</div>
				</div>
			</el-card>
			<el-card shadow="hover" class="stat-card" :body-style="{ padding: '12px 14px' }">
				<div class="stat">
					<div class="stat__ic ic-warning"><el-icon :size="20"><ele-Tickets /></el-icon></div>
					<div>
						<div class="stat__v">{{ summary.activeSubs }}</div>
						<div class="stat__l">{{ $t('lz.label.sharedataSharing.activeSubs') }}</div>
					</div>
				</div>
			</el-card>
			<!-- <el-card shadow="hover" class="stat-card" :body-style="{ padding: '12px 14px' }">
				<div class="stat">
					<div class="stat__ic ic-danger"><el-icon :size="20"><ele-Warning /></el-icon></div>
					<div>
						<div class="stat__v">{{ alerts.length }}</div>
						<div class="stat__l">{{ $t('lz.label.sharedataSharing.alerts') }}</div>
					</div>
				</div>
			</el-card> -->
		</div>

		<div class="eshare-body">
			<!-- Partner -->
			<el-card v-loading="loadingPartners" :element-loading-text="$t('lz.label.base.loading')" shadow="hover" class="eshare-body__left" :body-style="{ padding: '0', height: '100%' }">
				<PartnerList :partners="partners" :sessions="sessions" :selected-id="selectedPartnerId"
					:can-operate="canOperate" @select="selectPartner" @handleQuery="handlePartnerQuery" />
			</el-card>

			<div class="eshare-body__right">
				<!-- Session -->
				<el-card v-loading="loadingSessions" :element-loading-text="$t('lz.label.base.loading')" shadow="hover" :body-style="{ padding: '12px 14px' }">
					<template v-if="selectedPartner">
						<div class="detail-head">
							<div class="detail-head__info">
								<span class="detail-title">{{ selectedPartner.name }}</span>
								<TagInfo
									:config-item="getConfigItemByCode(BaseConfigTypeEnum.SharedataSessionState, currentState)" />
							</div>
							<div class="detail-actions">
								<el-button v-if="canOperate && !isActive" type="primary" size="small" icon="ele-Connection"
									v-auth="'sharedataSharing:update'" :loading="connecting" @click="handleConnect">{{ $t('lz.button.sharedataSharing.connect') }}</el-button>
								<template v-if="canOperate && isActive">
									<el-button type="danger" size="small" v-auth="'sharedataSharing:update'"
										@click="handleDisconnect">{{ $t('lz.button.sharedataSharing.disconnect') }}</el-button>
								
								</template>
							</div>
						</div>
						<!-- Thông tin vận hành: mốc thời gian và số liệu người dùng hiểu ngay.
						     Các tham số kỹ thuật (RTT, heartbeat, datagram, hồ sơ mã hoá) đã chuyển hết
						     vào dialog "Cấu hình" — xem ở đây chỉ gây rối khi theo dõi phiên. -->
						<el-descriptions :column="4" size="small" border>
							<el-descriptions-item :label="$t('lz.label.sharedataSharing.address')">{{ selectedPartner.address }}:{{ selectedPartner.port }}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.label.sharedataSharing.lastConnectedTime')">{{
								formatDateTime(currentSession?.acceptedAt ||
									currentSession?.startedAt) }}</el-descriptions-item>
							<!-- <el-descriptions-item label="Lần gửi gần nhất">{{ formatDateTime(lastTransfer.sentAt) }}</el-descriptions-item>
							<el-descriptions-item label="Lần nhận gần nhất">{{ formatDateTime(lastTransfer.recvAt) }}</el-descriptions-item>
							<el-descriptions-item label="Lần ngắt gần nhất">
								{{ formatDateTime(currentSession?.endedAt) }}
								<span v-if="currentSession?.endReason" class="detail-sub">({{
									reasonLabel(currentSession.endReason) }})</span>
							</el-descriptions-item> -->
							<el-descriptions-item :label="$t('lz.label.sharedataSharing.createTime')">{{ formatDateTime(selectedPartner.createTime)
							}}</el-descriptions-item>
							<el-descriptions-item :label="$t('lz.label.sharedataSharing.updateTime')">{{
								formatDateTime(selectedPartner.updateTime)
							}}</el-descriptions-item>
						</el-descriptions>
					</template>
					<el-empty v-else :description="$t('lz.label.sharedataSharing.selectPartnerToViewDetail')" :image-size="80" />
				</el-card>

				<el-card v-loading="loadingSubscriptions" :element-loading-text="$t('lz.label.base.loading')" shadow="hover" class="sub-card" :body-style="{ padding: '8px', height: '100%' }">
					<SubscriptionTable :subscriptions="partnerSubscriptions" :can-operate="canOperate"
						:partner-connected="isActive" @add="handleAddSubscription" @edit="handleEditSubscription"
						@cancel="handleCancel" @toggle="handleToggle" @preview="handlePreview" @export="handleExport" />
				</el-card>
			</div>
		</div>

		<EditSubscription ref="editSubRef" :partners="partners" :default-partner-id="selectedPartnerId"
			@handleQuery="reloadSubscriptions" />
		<ExportPreviewDialog ref="exportRef" />
	</div>
</template>

<script lang="ts" setup name="sharedataSharing">
import { computed, defineAsyncComponent, onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import {
	BaseConfigTypeEnum,
	DateTimeFormatEnum,
	SharedataDirectionEnum,
	SharedataLogTypeEnum,
	SharedataSessionFsmStateEnum,
	SharedataSubStateEnum,
	SharedataTransferDirectionEnum,
} from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import PartnerList from './component/partnerList.vue';
import SubscriptionTable from './component/subscriptionTable.vue';
import EditSubscription from './component/editSubscription.vue';
import ExportPreviewDialog from './component/exportPreviewDialog.vue';
import { getAPI } from '/@/utils/axios-utils';
import {
	SessionState,
	ShareDataActivityLogApi,
	ShareDataPartnerApi,
	ShareDataSessionApi,
	ShareDataSubscriptionApi,
	StatusEnum,
} from '/@/api-services/shareData';
import type {
	ShareDataPartnerOutput,
	ShareDataSessionOutput,
	ShareDataSubscriptionOutput,
} from '/@/api-services/shareData';
import { partnerNameOf } from '/@/utils/sharedata/labelHelper';

const { t } = useI18n();

interface AlertPayload {
	severity: 'warning' | 'error';
	code: string;
	message: string;
	partnerId?: string;
}



const STATUS_DISABLE = StatusEnum.NUMBER_0;
const SESSION_CONNECTED = SessionState.NUMBER_1;

const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

/* Phân quyền do `v-auth` đảm nhiệm (chặn theo mã quyền trong DB).
   Trước đây có hook useSharedataScope nhưng backend chưa cấp `sharedataRole` nên nó luôn
   trả true — giữ cờ này để template không phải sửa, `v-auth` mới là lớp chặn thật. */
const canOperate = computed(() => true);

const editSubRef = ref<InstanceType<typeof EditSubscription>>();
const exportRef = ref<InstanceType<typeof ExportPreviewDialog>>();

const partners = ref<ShareDataPartnerOutput[]>([]);
const sessions = ref<ShareDataSessionOutput[]>([]);
const subscriptions = ref<ShareDataSubscriptionOutput[]>([]);
const selectedPartnerId = ref<string>('');
const connecting = ref(false);
const loadingPartners = ref(false);
const loadingSessions = ref(false);
const loadingSubscriptions = ref(false);
const loading = computed(() => loadingPartners.value || loadingSessions.value || loadingSubscriptions.value);
const lastTransfer = reactive<{ sentAt?: string; recvAt?: string }>({});
const alerts = ref<AlertPayload[]>([]);

const selectedPartner = computed(() => partners.value.find((p) => p.id === selectedPartnerId.value));
const currentSession = computed(() =>
	sessions.value
		.filter((s) => s.partnerId === selectedPartnerId.value)
		.sort((a, b) => ((a.startedAt ?? '') < (b.startedAt ?? '') ? 1 : -1))[0]
);
const currentState = computed(() => currentSession.value?.state ?? SharedataSessionFsmStateEnum.Idle);

const isActive = computed(
	() => selectedPartner.value?.sessionState === SESSION_CONNECTED || Number(currentState.value) === SharedataSessionFsmStateEnum.Active
);
const partnerSubscriptions = computed(() => subscriptions.value.filter((s) => s.partnerId === selectedPartnerId.value));

const connectedPartnerIds = computed(() => {
	const ids = new Set<string>();
	for (const p of partners.value) if (p.sessionState === SESSION_CONNECTED && p.id) ids.add(p.id);
	for (const s of sessions.value) if (Number(s.state) === SharedataSessionFsmStateEnum.Active && s.partnerId) ids.add(s.partnerId);
	return ids;
});

const summary = reactive({ activeSessions: 0, packetsSent: 0, packetsRecv: 0, activeSubs: 0 });

function recomputeSummary() {
	summary.activeSessions = sessions.value.filter((s) => Number(s.state) === SharedataSessionFsmStateEnum.Active).length;
	summary.packetsSent = sessions.value.reduce((a, s) => a + (s.packetsSent ?? 0), 0);
	summary.packetsRecv = sessions.value.reduce((a, s) => a + (s.packetsRecv ?? 0), 0);
	summary.activeSubs = subscriptions.value.filter(
		(s) => s.state === (SharedataSubStateEnum.Active as any) && Boolean(s.partnerId) && connectedPartnerIds.value.has(s.partnerId)
	).length;
}

function formatDateTime(value?: string | Date | null): string {
	if (!value) return '—';
	const d = new Date(value);
	return Number.isNaN(d.getTime()) ? '—' : useDateFormat(d, DateTimeFormatEnum.DateTimeDefault).value;
}

async function reloadLastTransfer(partnerId: string) {
	lastTransfer.sentAt = undefined;
	lastTransfer.recvAt = undefined;
	if (!partnerId) return;
	const query = { logType: SharedataLogTypeEnum.Transfer, partnerId, page: 1, pageSize: 1 } as any;
	const logApi = getAPI(ShareDataActivityLogApi);
	try {
		const [snd, rcv] = await Promise.all([
			logApi.apiSharedataSharedataactivitylogPagePost({ ...query, transferDirection: SharedataTransferDirectionEnum.Snd }),
			logApi.apiSharedataSharedataactivitylogPagePost({ ...query, transferDirection: SharedataTransferDirectionEnum.Rcv }),
		]);
		lastTransfer.sentAt = snd.data.result?.records?.[0]?.occurredAt ?? undefined;
		lastTransfer.recvAt = rcv.data.result?.records?.[0]?.occurredAt ?? undefined;
	} catch {
	}
}

async function reloadPartners() {
	loadingPartners.value = true;
	try {
		const res = await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerListGet();
		partners.value = (res.data.result ?? []).filter((p) => p.status !== STATUS_DISABLE);
		if (!partners.value.some((p) => p.id === selectedPartnerId.value)) {
			selectedPartnerId.value = partners.value[0]?.id ?? '';
			// void reloadLastTransfer(selectedPartnerId.value);
		}
	} catch (e) {
		console.error('reloadPartners failed', e);
	} finally {
		loadingPartners.value = false;
	}
}
async function reloadSessions() {
	loadingSessions.value = true;
	try {
		const res = await getAPI(ShareDataSessionApi).apiSharedataSharedatasessionListGet(
			undefined, undefined, undefined, undefined, true
		);
		sessions.value = res.data.result ?? [];
	} catch (e) {
		console.error('reloadSessions failed', e);
	} finally {
		loadingSessions.value = false;
	}
	await reloadSummary().catch(() => {});
}

async function reloadSummary() {
	try {
		const res = await getAPI(ShareDataSessionApi).apiSharedataSharedatasessionSummaryPost({});
		const s = res.data.result ?? {};
		summary.activeSessions = s.activeSessions ?? 0;
		summary.packetsSent = s.packetsSent ?? 0;
		summary.packetsRecv = s.packetsRecv ?? 0;
		summary.activeSubs = s.activeSubscriptions ?? 0;
	} catch {
		recomputeSummary();
	}
}
async function reloadSubscriptions() {
	loadingSubscriptions.value = true;
	try {
		const res = await getAPI(ShareDataSubscriptionApi).apiSharedataSharedatasubscriptionListGet();
		subscriptions.value = res.data.result ?? [];
		recomputeSummary();
	} catch (e) {
		console.error('reloadSubscriptions failed', e);
	} finally {
		loadingSubscriptions.value = false;
	}
}

function selectPartner(id: string | any) {
	selectedPartnerId.value = id;
	// void reloadLastTransfer(id);
}

async function handlePartnerQuery() {
	try {
		await reloadPartners();
		await Promise.allSettled([reloadSessions(), reloadSubscriptions()]);
	} catch (e) {
		console.error('handlePartnerQuery failed', e);
	}
}

function handleAddSubscription(direction: number) {
	editSubRef.value?.openDialog(selectedPartnerId.value, direction);
}


function handleEditSubscription(row: ShareDataSubscriptionOutput) {
	editSubRef.value?.openDialog(row.partnerId ?? '', Number(row.direction ?? SharedataDirectionEnum.Outbound), row);
}

async function handleConnect() {
	if (!selectedPartner.value) return;
	connecting.value = true;
	try {
		await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerConnectPost({ id: selectedPartner.value.id } as any);
		// Nạp lại đối tác để lấy cờ sessionState mới — nút bật/tắt đăng ký phụ thuộc cờ này.
		await Promise.allSettled([reloadPartners(), reloadSessions(), reloadSubscriptions()]);
		ElMessage.success(t('lz.message.sharedataSharing.connecting'));
	} catch {
	} finally {
		connecting.value = false;
	}
}
function handleDisconnect() {
	if (!selectedPartner.value) return;
	ElMessageBox.confirm(t('lz.message.sharedataSharing.confirmDisconnect', { name: selectedPartner.value.name }), t('lz.message.base.warning'), { type: 'warning', confirmButtonText: t('lz.button.sharedataSharing.disconnectShort'), cancelButtonText: t('lz.button.base.cancel') })
		.then(async () => {
			await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerDisconnectPost({
				id: selectedPartner.value!.id,
				reason: 'clientRequested',
			} as any);
			// Nạp lại đối tác để cờ sessionState về 0, kéo theo khóa nút bật đăng ký PENDING.
			await Promise.allSettled([reloadPartners(), reloadSessions(), reloadSubscriptions()]);
			ElMessage.success(t('lz.message.sharedataSharing.disconnectSuccess'));
		})
		.catch(() => { });
}
async function handleSuspend() {
	const active = partnerSubscriptions.value.filter(
		(s) => s.direction === (SharedataDirectionEnum.Outbound as any) && s.state === (SharedataSubStateEnum.Active as any)
	);
	if (!active.length) return ElMessage.info(t('lz.message.sharedataSharing.noActiveOutboundSub'));
	let failed = 0;
	const subApi = getAPI(ShareDataSubscriptionApi);
	for (const s of active) {
		try {
			await subApi.apiSharedataSharedatasubscriptionPausePost({ id: s.id } as any);
		} catch {
			failed++;
		}
	}
	await reloadSubscriptions();
	if (failed === 0) ElMessage.success(t('lz.message.sharedataSharing.pausedSubsSuccess', { count: active.length }));
	else if (failed === active.length) ElMessage.error(t('lz.message.sharedataSharing.pausedSubsFailedAll', { failed, total: active.length }));
	else ElMessage.warning(t('lz.message.sharedataSharing.pausedSubsPartial', { success: active.length - failed, total: active.length, failed }));
}
function handleCancel(row: ShareDataSubscriptionOutput) {
	ElMessageBox.confirm(t('lz.message.sharedataSharing.confirmCancelSub'), t('lz.message.base.warning'), { type: 'warning', confirmButtonText: t('lz.button.sharedataSharing.cancelSub'), cancelButtonText: t('lz.button.base.close') })
		.then(async () => {
			await getAPI(ShareDataSubscriptionApi).apiSharedataSharedatasubscriptionDeletePost({
				id: row.id
			} as any);
			await reloadSubscriptions();
			ElMessage.success(t('lz.message.sharedataSharing.cancelSubSuccess'));
		})
		.catch(() => { });
}

async function handleToggle(row: ShareDataSubscriptionOutput, enabled: boolean) {
	const subApi = getAPI(ShareDataSubscriptionApi);
	try {
		if (enabled) await subApi.apiSharedataSharedatasubscriptionResumePost({ id: row.id } as any);
		else await subApi.apiSharedataSharedatasubscriptionPausePost({ id: row.id } as any);
		await reloadSubscriptions();
		ElMessage.success(enabled ? t('lz.message.sharedataSharing.enabled') : t('lz.message.sharedataSharing.disabled'));
	} catch {
		// Lỗi đã được axios response interceptor (axios-utils.ts) hiển thị qua ElMessage.error;
		// không gọi lại để tránh 2 toast chồng lấn.
		await reloadSubscriptions();
	}
}
function withPartnerName(row: ShareDataSubscriptionOutput): ShareDataSubscriptionOutput {
	const name = partnerNameOf(partners.value, row.partnerId);
	return { ...row, partnerName: name || undefined } as ShareDataSubscriptionOutput;
}

function handlePreview(row: ShareDataSubscriptionOutput) {
	exportRef.value?.openDialog(withPartnerName(row));
}
function handleExport(row: ShareDataSubscriptionOutput) {
	exportRef.value?.openDialog(withPartnerName(row));
}

const offHandlers: (() => void)[] = [];

onMounted(async () => {
	try {
		await Promise.allSettled([
			reloadPartners().catch(() => {}),
			reloadSessions().catch(() => {}),
			reloadSubscriptions().catch(() => {}),
		]);
	} finally {
		loadingPartners.value = false;
		loadingSessions.value = false;
		loadingSubscriptions.value = false;
	}
});

</script>

<style scoped lang="scss">
.eshare-sharing {
	display: flex;
	flex-direction: column;
	gap: 6px;
	height: 100%;
}

.stat-row {
	display: flex;
	gap: 8px;
	flex-wrap: wrap;
}

.stat-card {
	flex: 1 1 160px;
}

.stat {
	display: flex;
	align-items: center;
	gap: 12px;
}

.stat__ic {
	width: 42px;
	height: 42px;
	border-radius: 10px;
	display: flex;
	align-items: center;
	justify-content: center;
	color: #fff;
	flex-shrink: 0;
}

.ic-success {
	background: var(--el-color-success);
}

.ic-primary {
	background: var(--el-color-primary);
}

.ic-info {
	background: var(--el-color-info);
}

.ic-warning {
	background: var(--el-color-warning);
}

.ic-danger {
	background: var(--el-color-danger);
}

.stat__v {
	font-size: 22px;
	font-weight: 700;
	line-height: 1.1;
}

.stat__l {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.eshare-body {
	display: flex;
	gap: 6px;
	flex: 1;
	min-height: 0;
}

.eshare-body__left {
	width: 280px;
	flex-shrink: 0;
}

.eshare-body__right {
	flex: 1;
	display: flex;
	flex-direction: column;
	gap: 6px;
	min-width: 0;
	min-height: 0;
}

/* Card đăng ký chiếm hết chiều cao còn lại. Trước đây phần này do card "live flow log"
   đảm nhiệm; khi bỏ log realtime thì không còn thành phần nào giãn ra nên bảng bị hụt
   và trang chừa một khoảng trắng lớn phía dưới. */
.sub-card {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
}

/* `el-card__body` phải cao 100% để vxe-grid tính được `height: 100%`. */
.sub-card :deep(.el-card__body) {
	flex: 1;
	min-height: 0;
}

.detail-sub {
	margin-left: 4px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.detail-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	margin-bottom: 10px;
}

/* Tên + tag trạng thái canh giữa theo trục dọc; gap thay cho margin của .detail-title
   để tag không bị lệch khi ẩn (thiếu cấu hình trạng thái). */
.detail-head__info {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.detail-title {
	font-size: 16px;
	font-weight: 600;
}

/* Loading Pattern (8-dots spinner + "Đang tải...") cho các thẻ trong trang sharing */
:deep(.el-loading-mask) {
	background-color: rgba(255, 255, 255, 0.78);
	backdrop-filter: blur(1px);
	border-radius: inherit;
	transition: opacity 0.25s ease;
	z-index: 100;
}

:deep(.el-loading-spinner) {
	top: 50%;
	transform: translateY(-50%);
	margin-top: 0;
	width: 100%;
	text-align: center;
	position: absolute;

	.circular {
		display: inline-block;
		width: 50px;
		height: 50px;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		.path {
			display: none;
		}
	}

	.el-loading-text {
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin: 10px 0 0;
		line-height: 1.4;
	}

	&:not(:has(.el-loading-text))::after {
		content: 'Đang tải...';
		display: block;
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin-top: 10px;
		line-height: 1.4;
	}
}

.sub-card :deep(.el-loading-spinner .circular) {
	width: 54px;
	height: 54px;
}

:global(html.dark) .eshare-sharing :deep(.el-loading-mask) {
	background-color: rgba(24, 28, 38, 0.82);
}

@keyframes sharedata-roll {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}
</style>
