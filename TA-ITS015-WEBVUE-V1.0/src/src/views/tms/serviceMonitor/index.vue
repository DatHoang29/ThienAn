<template>
	<div class="service-monitor-container">

		<!-- Thanh công cụ + tổng quan -->
		<el-card shadow="hover" class="mb5" :body-style="{ padding: '10px' }">
			<div class="sm-toolbar">
				<div class="sm-title">
					<span>{{ $t('lz.label.tms.serviceMonitor') }}</span>
					<el-tag :type="connected ? 'success' : 'info'" size="small" effect="light" round class="ml10">
						<span class="sm-dot" :class="connected ? 'is-online' : 'is-idle'"></span>
						{{ connected ? $t('lz.label.tmsServiceMonitor.natsConnected') : $t('lz.label.tmsServiceMonitor.natsIdle') }}
					</el-tag>
				</div>
				<div class="sm-actions">
					<!-- Toggle chế độ quản lý -->
					<el-tooltip :content="$t('lz.label.tmsServiceMonitor.editMode')" placement="top" :auto-close="1000">
						<el-switch
							v-model="editMode"
							v-auth="'tmsServiceMonitor:update'"
							:active-action-icon="EditPen"
							:inactive-action-icon="View"
							inline-prompt
							class="mr10"
						/>
					</el-tooltip>
					<!-- Các nút quản lý: chỉ hiện khi bật chế độ quản lý -->
					<transition name="sm-slide">
						<div v-if="editMode" class="sm-edit-actions">
							<el-button icon="ele-Plus" type="primary" v-auth="'tmsServiceMonitor:add'" @click="handleAdd">{{ $t('lz.button.base.add') }}</el-button>
							<el-button icon="ele-RefreshRight" @click="handleResetDefault" v-auth="'tmsServiceMonitor:add'">{{ $t('lz.button.tmsServiceMonitor.resetDefault') }}</el-button>
						</div>
					</transition>
				</div>
			</div>

			<el-row :gutter="10" class="sm-stats">
				<el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
					<div class="sm-stat">
						<div class="sm-stat-num">{{ stats.total }}</div>
						<div class="sm-stat-label">{{ $t('lz.label.tmsServiceMonitor.total') }}</div>
					</div>
				</el-col>
				<el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
					<div class="sm-stat is-online">
						<div class="sm-stat-num">{{ stats.online }}</div>
						<div class="sm-stat-label">{{ $t('lz.label.tmsServiceMonitor.online') }}</div>
					</div>
				</el-col>
				<el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
					<div class="sm-stat is-offline">
						<div class="sm-stat-num">{{ stats.offline }}</div>
						<div class="sm-stat-label">{{ $t('lz.label.tmsServiceMonitor.offline') }}</div>
					</div>
				</el-col>
				<el-col :xs="12" :sm="6" :md="6" :lg="6" :xl="6">
					<div class="sm-stat is-paused">
						<div class="sm-stat-num">{{ stats.paused }}</div>
						<div class="sm-stat-label">{{ $t('lz.label.tmsServiceMonitor.paused') }}</div>
					</div>
				</el-col>
			</el-row>
		</el-card>

		<!-- Dịch vụ chưa cấu hình nhưng có heartbeat đến -->
		<el-alert v-if="discovered.length && editMode" type="warning" :closable="false" class="mb5" show-icon>
			<template #title>
				<span>{{ $t('lz.label.tmsServiceMonitor.discoveredHint') }}</span>
				<el-tag v-for="s in discovered" :key="s" class="ml5" size="small" type="warning" effect="plain" @click="handleAddDiscovered(s)" style="cursor: pointer">
					+ {{ s }}
				</el-tag>
			</template>
		</el-alert>

		<!-- Lưới thẻ trạng thái dịch vụ -->
		<el-row :gutter="10">
			<el-col v-for="row in rows" :key="row.id" :xs="24" :sm="12" :md="8" :lg="6" :xl="6" class="mb10">
				<el-card shadow="hover" class="sm-card" :class="'is-' + row.state" :body-style="{ padding: '12px' }">
					<div class="sm-card-head">
						<span class="sm-dot" :class="dotClass(row.state)"></span>
						<span class="sm-card-name" :title="row.name">{{ row.name }}</span>
						<el-tag size="small" effect="plain" class="sm-module">{{ row.module || '-' }}</el-tag>
					</div>

					<div class="sm-card-subject" :title="row.subject">{{ row.subject }}</div>

					<!-- Icon trạng thái lớn / Loading spinner khi đang chờ gói tin -->
					<div class="sm-card-status">
						<!-- Loading: chưa nhận heartbeat và đang trong thời gian chờ -->
						<div v-if="isCardLoading(row)" class="sm-status-icon-wrapper">
							<el-icon :size="70" class="sm-loading-spinner" color="var(--el-color-primary)">
								<ele-Loading />
							</el-icon>
						</div>
						<!-- Trạng thái thực tế khi hết loading -->
						<el-tooltip v-else :content="stateLabel(row.state)" placement="top" :auto-close="1000">
							<div class="sm-status-icon-wrapper">
								<el-icon v-if="row.state === 'online'" :size="70" color="#67C23A">
									<ele-CircleCheckFilled />
								</el-icon>
								<el-icon v-else-if="row.state === 'offline'" :size="70" color="#F56C6C">
									<ele-CircleCloseFilled />
								</el-icon>
								<el-icon v-else :size="70" color="#909399">
									<ele-VideoPause />
								</el-icon>
							</div>
						</el-tooltip>
					</div>

					<div class="sm-card-meta">
						<span v-if="isCardLoading(row)" class="sm-lastseen">{{ $t('lz.label.tmsServiceMonitor.loadingHint') }}</span>
						<span v-else class="sm-lastseen">{{ lastSeenText(row) }}</span>
					</div>

					<!-- Các nút sửa/xóa chỉ hiển thị khi bật chế độ quản lý -->
					<transition name="sm-slide">
						<div v-if="editMode" class="sm-card-foot">
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'tmsServiceMonitor:update'" @click="handleEdit(row)" />
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'tmsServiceMonitor:delete'" @click="handleDelete(row)" />
							</el-tooltip>
						</div>
					</transition>
				</el-card>
			</el-col>

			<el-col v-if="!rows.length" :span="24">
				<el-empty :description="$t('lz.label.tmsServiceMonitor.emptyHint')" :image-size="160" />
			</el-col>
		</el-row>

		<EditServiceMonitorComp ref="editRef" :title="state.title" :operate-type="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsServiceMonitor">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { EditPen, View } from '@element-plus/icons-vue';
import mittBus from '/@/utils/mitt';

import { OperateType } from '/@/types/constants';
import { Local } from '/@/utils/storage';
import { useServiceMonitor, ServiceMonitorItem } from '/@/stores/its/serviceMonitor';
import { useTransporterHeartbeat } from '/@/transporter/hooks/useTransporterHeartbeat';
import EditServiceMonitorComp from '/@/views/tms/serviceMonitor/component/editServiceMonitor.vue';

type ServiceState = 'online' | 'offline' | 'paused';
interface ServiceRow extends ServiceMonitorItem {
	state: ServiceState;
	lastSeen: number | null;
}

const { t: $t } = useI18n();
const serviceMonitor = useServiceMonitor();
const editRef = ref<InstanceType<typeof EditServiceMonitorComp>>();

const state = reactive({
	title: '',
	operateType: 'add' as OperateType,
});

// ─── Loading per-card: mỗi thẻ hiển thị spinner khi vừa vào trang, tới khi nhận heartbeat mới
// hoặc hết số giây cấu hình (intervalSeconds + dung sai) ─
const LOADING_TOLERANCE_SEC = 5;
const pageEnteredAt = ref<number>(Date.now());

const startLoadingCountdown = () => {
	pageEnteredAt.value = Date.now();
};

/** Số giây chờ tối đa của 1 dịch vụ = chu kỳ cấu hình + dung sai (đồng bộ với computeState). */
const getWaitSec = (item: ServiceMonitorItem) => (item.intervalSeconds > 0 ? item.intervalSeconds : 30) + LOADING_TOLERANCE_SEC;

/**
 * Card đang loading = dịch vụ đang bật (status=1), CHƯA nhận heartbeat mới kể từ lúc vào trang
 * (beat cũ khôi phục từ LocalStorage không tính), và chưa quá số giây cấu hình.
 */
const isCardLoading = (row: ServiceRow): boolean => {
	if (row.status !== 1) return false; // paused → không loading
	if (row.state === 'online') return false; // còn heartbeat hợp lệ → hiển thị trạng thái thật
	if (row.lastSeen && row.lastSeen >= pageEnteredAt.value) return false; // đã nhận heartbeat mới → không loading
	return now.value - pageEnteredAt.value < getWaitSec(row) * 1000;
};

// ─── Chế độ quản lý (toggle hiển thị nút thêm/sửa/xóa) ──────────
const editMode = ref<boolean>(false);

// ─── Heartbeat & trạng thái ──────────────────────────────────────
const BEATS_STORAGE_KEY = 'tmsServiceMonitorBeats';
const normSubject = (s: string) => (s || '').trim().toLowerCase();

// Bản đồ subject -> thời điểm nhận heartbeat gần nhất (epoch ms).
// Khôi phục từ LocalStorage để khi người dùng F5 thì các dịch vụ vừa hoạt động không bị rơi vào trạng thái mất kết nối giả.
const beats = reactive<Record<string, number>>(Local.get(BEATS_STORAGE_KEY) || {});
// Đồng hồ đánh dấu để tính lại trạng thái theo thời gian thực.
const now = ref<number>(Date.now());
let timer: any = null;
// Trạng thái kết nối NATS (nghe từ transporter).
const connected = ref<boolean>(false);

// Nhận heartbeat realtime.
useTransporterHeartbeat((beat) => {
	const key = normSubject(beat.subject);
	beats[key] = beat.receivedAt;
	beats[beat.subject] = beat.receivedAt;
	Local.set(BEATS_STORAGE_KEY, beats);
	connected.value = true;
	const known = serviceMonitor.list.some((x) => normSubject(x.subject) === key);
	console.log(
		`%c[ServiceMonitor] cập nhật ${beat.subject} (${known ? 'đã cấu hình → chuyển Online' : 'CHƯA cấu hình, bấm thẻ gợi ý để thêm'})`,
		`color:${known ? '#409EFF' : '#E6A23C'}`
	);
});

const onNatsStatus = (status: any) => {
	const t = status?.type;
	if (t === 'disconnect' || t === 'error') connected.value = false;
	else if (t === 'reconnect' || t === 'connect') connected.value = true;
};

onMounted(() => {
	timer = setInterval(() => (now.value = Date.now()), 1000);
	mittBus.on('event-nats-status' as any, onNatsStatus);
	startLoadingCountdown();
	// Log tổng quan để đối chiếu: nếu KHÔNG thấy log "[Heartbeat] ⇩ ..." sau đó thì client chưa nhận được gói nào.
	console.log(
		'[ServiceMonitor] Bắt đầu giám sát. Đang chờ heartbeat trên các subject:',
		serviceMonitor.list.map((x) => x.subject)
	);
	console.log('[ServiceMonitor] Client cần subscribe wildcard "ta.its.heartbeat.>" — tìm dòng "[NATS] Subscribed: ta.its.heartbeat.>" phía trên.');
});

onUnmounted(() => {
	if (timer) clearInterval(timer);
	mittBus.off('event-nats-status' as any, onNatsStatus);
});

const computeState = (item: ServiceMonitorItem, lastSeen: number | null): ServiceState => {
	if (item.status !== 1) return 'paused';
	if (!lastSeen) return 'offline';
	// Timeout = chu kỳ cấu hình + 5 giây dung sai mạng.
	// VD: intervalSeconds=30 → quá 35s không nhận gói → offline.
	const intervalSec = item.intervalSeconds > 0 ? item.intervalSeconds : 30;
	const timeoutMs = (intervalSec + 5) * 1000;
	return now.value - lastSeen <= timeoutMs ? 'online' : 'offline';
};

const rows = computed<ServiceRow[]>(() =>
	serviceMonitor.sortedList.map((item) => {
		const key = normSubject(item.subject);
		const lastSeen = beats[key] ?? beats[item.subject] ?? null;
		return { ...item, lastSeen, state: computeState(item, lastSeen) };
	})
);

const stats = computed(() => {
	let online = 0,
		offline = 0,
		paused = 0;
	for (const r of rows.value) {
		if (r.state === 'online') online++;
		else if (r.state === 'paused') paused++;
		else if (!isCardLoading(r)) offline++; // đang chờ heartbeat thì chưa kết luận mất kết nối
	}
	return { total: rows.value.length, online, offline, paused };
});

// Subject có heartbeat đến nhưng chưa được khai báo trong danh sách theo dõi.
const discovered = computed<string[]>(() => {
	const known = new Set(serviceMonitor.list.map((x) => normSubject(x.subject)));
	// Lọc các subject đã chuẩn hóa và loại bỏ các key trùng lặp
	const rawKeys = Object.keys(beats).filter((s) => s.startsWith('ta.its.heartbeat.'));
	return Array.from(new Set(rawKeys)).filter((s) => !known.has(normSubject(s)));
});

const dotClass = (s: ServiceState) => (s === 'online' ? 'is-online' : s === 'paused' ? 'is-idle' : 'is-offline');
const stateTagType = (s: ServiceState) => (s === 'online' ? 'success' : s === 'paused' ? 'info' : 'danger');
const stateLabel = (s: ServiceState) => $t(`lz.label.tmsServiceMonitor.${s}`);

/** Định dạng số giây: <60 → giây; <1 giờ → phút + giây; >=1 giờ → giờ + phút + giây. */
const formatDuration = (totalSec: number): string => {
	const uH = $t('lz.label.tmsServiceMonitor.unitHour');
	const uM = $t('lz.label.tmsServiceMonitor.unitMinute');
	const uS = $t('lz.label.tmsServiceMonitor.unitSecond');
	const h = Math.floor(totalSec / 3600);
	const m = Math.floor((totalSec % 3600) / 60);
	const s = totalSec % 60;
	if (h > 0) return `${h} ${uH} ${m} ${uM} ${s} ${uS}`;
	if (m > 0) return `${m} ${uM} ${s} ${uS}`;
	return `${s} ${uS}`;
};

const lastSeenText = (row: ServiceRow) => {
	if (!row.lastSeen) return $t('lz.label.tmsServiceMonitor.neverSeen');
	const secs = Math.max(0, Math.floor((now.value - row.lastSeen) / 1000));
	return $t('lz.label.tmsServiceMonitor.lastSeenAgo', { time: formatDuration(secs) });
};

const handleQuery = () => {
	/* Store phản ứng tự động; hàm giữ để edit dialog emit về. */
};

const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.tms.serviceMonitor') });
	state.operateType = 'add';
	editRef.value?.openDialog({ name: '', module: '', subject: 'ta.its.heartbeat.', intervalSeconds: 30, status: 1, orderNo: 100, remark: '' });
};

const handleAddDiscovered = (subject: string) => {
	const module = subject.split('.').pop() || '';
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.tms.serviceMonitor') });
	state.operateType = 'add';
	editRef.value?.openDialog({ name: module.toUpperCase(), module, subject, intervalSeconds: 30, status: 1, orderNo: 100, remark: '' });
};

const handleEdit = (row: ServiceRow) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.tms.serviceMonitor') });
	state.operateType = 'edit';
	const { state: _s, lastSeen: _l, ...item } = row;
	editRef.value?.openDialog(item);
};

const handleDelete = (row: ServiceRow) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.button.base.delete'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(() => {
			serviceMonitor.remove(row.id);
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

const handleResetDefault = () => {
	ElMessageBox.confirm($t('lz.message.tmsServiceMonitor.confirmReset'), $t('lz.button.tmsServiceMonitor.resetDefault'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(() => {
			serviceMonitor.resetDefault();
			ElMessage.success($t('lz.message.base.processSuccess'));
		})
		.catch(() => {});
};
</script>

<style scoped lang="scss">
.service-monitor-container {
	padding: 5px;

	.mb5 {
		margin-bottom: 5px;
	}
	.mb10 {
		margin-bottom: 10px;
	}
	.ml5 {
		margin-left: 5px;
	}
	.ml10 {
		margin-left: 10px;
	}
	.mr10 {
		margin-right: 10px;
	}

	// ─── Loading spinner per-card ─────────────────────
	.sm-loading-spinner {
		animation: sm-spin 1.2s linear infinite;
	}

	@keyframes sm-spin {
		from {
			transform: rotate(0deg);
		}
		to {
			transform: rotate(360deg);
		}
	}

	// ─── Slide transition cho edit actions ─────────────
	.sm-slide-enter-active,
	.sm-slide-leave-active {
		transition: all 0.3s ease;
		overflow: hidden;
	}
	.sm-slide-enter-from,
	.sm-slide-leave-to {
		opacity: 0;
		max-height: 0;
		transform: translateY(-6px);
	}
	.sm-slide-enter-to,
	.sm-slide-leave-from {
		opacity: 1;
		max-height: 60px;
		transform: translateY(0);
	}

	.sm-toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 8px;

		.sm-title {
			display: flex;
			align-items: center;
			font-size: 16px;
			font-weight: 600;
			color: var(--el-text-color-primary);

			.sm-title-icon {
				margin-right: 6px;
				color: var(--el-color-primary);
			}
		}

		.sm-actions {
			display: flex;
			align-items: center;
			gap: 8px;

			.sm-edit-actions {
				display: flex;
				align-items: center;
				gap: 8px;
			}
		}
	}

	.sm-stats {
		margin-top: 10px;

		.sm-stat {
			border: 1px solid var(--el-border-color-light);
			border-radius: 6px;
			padding: 10px;
			text-align: center;
			background: var(--el-fill-color-light);

			.sm-stat-num {
				font-size: 24px;
				font-weight: 700;
				color: var(--el-text-color-primary);
			}
			.sm-stat-label {
				font-size: 12px;
				color: var(--el-text-color-secondary);
			}

			&.is-online .sm-stat-num {
				color: var(--el-color-success);
			}
			&.is-offline .sm-stat-num {
				color: var(--el-color-danger);
			}
			&.is-paused .sm-stat-num {
				color: var(--el-color-info);
			}
		}
	}

	.sm-card {
		border-left: 3px solid var(--el-border-color);

		&.is-online {
			border-left-color: var(--el-color-success);
		}
		&.is-offline {
			border-left-color: var(--el-color-danger);
		}
		&.is-paused {
			border-left-color: var(--el-color-info);
		}

		.sm-card-head {
			display: flex;
			align-items: center;
			gap: 6px;

			.sm-card-name {
				flex: 1;
				font-weight: 600;
				white-space: nowrap;
				overflow: hidden;
				text-overflow: ellipsis;
				color: var(--el-text-color-primary);
			}
			.sm-module {
				text-transform: uppercase;
			}
		}

		.sm-card-subject {
			margin-top: 6px;
			font-family: monospace;
			font-size: 12px;
			color: var(--el-text-color-secondary);
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}

		.sm-card-status {
			display: flex;
			align-items: center;
			justify-content: center;
			padding: 16px 0 8px 0;

			.sm-status-icon-wrapper {
				display: flex;
				align-items: center;
				justify-content: center;
				cursor: pointer;
				transition: transform 0.2s ease;

				&:hover {
					transform: scale(1.12);
				}
			}
		}

		.sm-card-meta {
			margin-top: 4px;
			display: flex;
			align-items: center;
			justify-content: center;

			.sm-lastseen {
				font-size: 12px;
				color: var(--el-text-color-secondary);
				text-align: center;
			}
		}

		.sm-card-foot {
			margin-top: 6px;
			text-align: right;
			border-top: 1px dashed var(--el-border-color-lighter);
			padding-top: 4px;
		}
	}

	.sm-dot {
		display: inline-block;
		width: 8px;
		height: 8px;
		border-radius: 50%;
		background: var(--el-color-info);

		&.is-online {
			background: var(--el-color-success);
		}
		&.is-offline {
			background: var(--el-color-danger);
		}
		&.is-idle {
			background: var(--el-color-info);
		}
	}
}
</style>
