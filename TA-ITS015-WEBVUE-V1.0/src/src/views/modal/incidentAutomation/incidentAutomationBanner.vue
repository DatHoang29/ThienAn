<template>
	<!-- Banner cảnh báo tự động: nổi giữa màn hình, cách top một khoảng, hiển thị trên mọi trang -->
	<Teleport to="body">
		<div v-if="automationStore.pendingRuns.length" class="automation-banner-wrap">
			<!-- Thanh tóm tắt: chỉ xuất hiện khi có nhiều hơn một sự cố, kiêm luôn nút thu gọn/mở rộng -->
			<div v-if="automationStore.pendingRuns.length > 1" class="automation-summary">
				<span class="summary-label">
					<el-icon class="summary-icon"><ele-BellFilled /></el-icon>
					{{ $t('lz.label.tmsIncidentAutomation.pendingSummary', { count: automationStore.pendingRuns.length }) }}
				</span>
				<el-button v-if="hiddenCount > 0 || isExpanded" link type="primary" size="small" @click="isExpanded = !isExpanded">
					{{ isExpanded ? $t('lz.label.tmsIncidentAutomation.collapseRuns') : $t('lz.label.tmsIncidentAutomation.moreRuns', { count: hiddenCount }) }}
					<el-icon class="ml5"><ele-ArrowUp v-if="isExpanded" /><ele-ArrowDown v-else /></el-icon>
				</el-button>
			</div>

			<transition-group name="automation-banner" tag="div" class="automation-banner-list">
				<div
					v-for="item in visibleRuns"
					:key="item.automation.runId"
					class="automation-banner"
					:class="[`is-${automationStore.getUrgency(item)}`, { 'is-readonly': !item.automation.canOperate }]"
					:role="item.automation.canOperate ? 'button' : undefined"
					:tabindex="item.automation.canOperate ? 0 : undefined"
					@click="handleOpenDetail(item)"
					@keydown.enter="handleOpenDetail(item)"
					@keydown.space.prevent="handleOpenDetail(item)"
				>
					<!-- Dải màu thể hiện mức độ gấp -->
					<span class="banner-accent" />

					<!-- Icon loại sự kiện -->
					<div class="banner-icon">
						<el-image v-if="resolveEventIcon(item)" :src="resolveEventIcon(item)" fit="contain" class="event-icon">
							<template #error>
								<el-icon><ele-Warning /></el-icon>
							</template>
						</el-image>
						<el-icon v-else class="fallback-icon"><ele-Warning /></el-icon>
					</div>

					<!-- Thông tin sự cố: 2 dòng, dòng trên tên + loại sự kiện, dòng dưới vị trí/thời gian/kịch bản -->
					<div class="banner-body">
						<div class="banner-title">
							<span class="title-text">{{ item.incident?.name || item.incident?.code }}</span>
							<el-tag v-if="item.incident?.eventTypeName" size="small" type="danger" effect="dark" round class="nowrap-tag">
								{{ item.incident.eventTypeName }}
							</el-tag>
							<el-tag v-if="!item.automation.canOperate" size="small" type="info" effect="plain" round class="nowrap-tag">
								{{ $t('lz.label.tmsIncidentAutomation.viewOnly') }}
							</el-tag>
						</div>

						<div class="banner-meta">
							<!-- Lý trình -->
							<span v-if="hasPosition(item)" class="meta-item meta-strong">
								<el-icon><ele-Location /></el-icon>
								{{ commonVars.formatPosition(item.incident?.kmNumber, item.incident?.metNumber) }}
							</span>

							<!-- Vị trí trên mặt cắt ngang (làn/lề) và trục tuyến (hướng ảnh hưởng) -->
							<TagInfo v-if="item.incident?.location" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, item.incident.location)" />
							<TagInfo v-if="item.incident?.influenceScope" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, item.incident.influenceScope)" />

							<!-- Thời điểm xảy ra -->
							<span v-if="item.incident?.startDate" class="meta-item">
								<el-icon><ele-Clock /></el-icon>
								{{ formatDate(new Date(item.incident.startDate), 'HH:MM dd/mm') }}
							</span>

							<!-- Kịch bản sẽ áp dụng -->
							<span v-if="item.automation.trafficPlanName" class="meta-item meta-plan">
								<el-icon><ele-Operation /></el-icon>
								{{ item.automation.trafficPlanName }}
							</span>
						</div>
					</div>

					<!-- Đếm ngược -->
					<div class="banner-countdown">
						<span class="countdown-label">{{ $t('lz.label.tmsIncidentAutomation.autoExecuteIn') }}</span>
						<span class="countdown-value">{{ automationStore.formatRemaining(item) }}</span>
					</div>

					<!-- Hành động: nhóm chỉ-xem không có nút nào vì hủy tự động là thao tác thật -->
					<div v-if="item.automation.canOperate" class="banner-actions" @click.stop>
						<el-button size="small" type="primary" @click="handleOpenDetail(item)">
							{{ $t('lz.button.base.detail') }}
						</el-button>

						<el-tooltip :content="$t('lz.label.tmsIncidentAutomation.skipTip')" placement="bottom" :show-after="300">
							<el-button size="small" type="danger" plain :disabled="state.skippingRunId === item.automation.runId" @click="handleSkip(item)">
								{{ $t('lz.button.tmsIncident.skip') }}
							</el-button>
						</el-tooltip>
					</div>

					<!-- Thanh tiến trình vơi dần theo thời gian còn lại -->
					<span class="banner-progress" :style="{ width: automationStore.getRemainingPercent(item) + '%' }" />
				</div>
			</transition-group>
		</div>
	</Teleport>

	<!-- Modal chi tiết + xác nhận thực thi / chuyển thủ công -->
	<AutomationRunDetail />
</template>

<script lang="ts" setup name="tmsIncidentAutomationBanner">
import { computed, defineAsyncComponent, onMounted, onUnmounted, reactive, ref, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useIncidentAutomation } from '/@/stores/its/incidentAutomation';
import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { formatDate } from '/@/utils/formatTime';

const AutomationRunDetail = defineAsyncComponent(() => import('./component/automationRunDetail.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const { t: $t } = useI18n();
const automationStore = useIncidentAutomation();
const commonVars = useCommonVarsAndFuncs();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

/** Số banner hiển thị tối đa khi chưa mở rộng, tránh nhiều sự cố cùng lúc chiếm hết màn hình */
const MAX_VISIBLE_BANNERS = 3;

const tickTimer = ref<number | null>(null);
/** Người dùng đã bấm mở rộng để xem toàn bộ danh sách hay chưa */
const isExpanded = ref(false);

const state = reactive({
	/** RunId đang gửi yêu cầu bỏ qua, dùng để chặn bấm lặp */
	skippingRunId: '' as string,
});

/** Danh sách banner thực sự render: giới hạn khi thu gọn, đầy đủ khi mở rộng */
const visibleRuns = computed(() => {
	const runs = automationStore.pendingRuns;
	return isExpanded.value ? runs : runs.slice(0, MAX_VISIBLE_BANNERS);
});

/** Số sự cố tự động bị ẩn khi đang thu gọn */
const hiddenCount = computed(() => Math.max(0, automationStore.pendingRuns.length - MAX_VISIBLE_BANNERS));

// Ghép URL icon loại sự kiện
const resolveEventIcon = (item: any) => {
	const url = item?.automation?.eventTypeUrl;
	if (!url) return '';
	const host = commonVars.serverHost ?? '';
	return `${host}/${url}`.replace(/([^:]\/)\/+/g, '$1');
};

// Có dữ liệu lý trình hay không
const hasPosition = (item: any) => item?.incident?.kmNumber != null || item?.incident?.metNumber != null;

/**
 * Mở modal chi tiết. Store tự chặn nếu tài khoản không được phép thao tác, nên các nhóm chỉ-xem
 * bấm vào banner cũng không mở được gì.
 */
const handleOpenDetail = (item: any) => automationStore.openDetail(item);

/**
 * Author: SonTH
 * Description: Đánh dấu sự cố là bỏ qua - không xử lý, giống nút "Bỏ qua" của luồng thủ công.
 *              Là thao tác THẬT trên dữ liệu và ảnh hưởng mọi máy trạm nên bắt buộc hỏi xác nhận.
 *              Backend tự hủy lần chạy tự động kèm theo, rồi phát broadcast để popup xử lý sự cố
 *              nạp lại danh sách và banner tự biến mất.
 * Created date: 24/07/2026
 */
const handleSkip = async (item: any) => {
	const runId = item?.automation?.runId;
	const incidentId = item?.incident?.id;
	if (!runId || !incidentId) return;

	try {
		await ElMessageBox.confirm(
			$t('lz.message.tmsIncidentAutomation.confirmSkip', { name: item.incident?.name || item.incident?.code }),
			$t('lz.message.base.warning'),
			{
				confirmButtonText: $t('lz.button.base.confirm'),
				cancelButtonText: $t('lz.button.base.cancel'),
				type: 'warning',
			}
		);
	} catch {
		return; // Người dùng bấm hủy hộp thoại
	}

	state.skippingRunId = runId;
	try {
		await automationStore.skipIncident(incidentId);
		ElMessage.success($t('lz.message.tmsIncidentAutomation.skipSuccess'));
	} catch (error) {
		console.error('handleSkip', error);
	} finally {
		state.skippingRunId = '';
	}
};

/**
 * Chỉ chạy đồng hồ đếm ngược. KHÔNG gọi API và KHÔNG poll:
 * dữ liệu do incidentPopup nạp từ ListByAccount rồi đẩy vào store (xem syncFromIncidentList),
 * bám đúng luồng xử lý sự cố thủ công vốn đã lắng nghe NATS.
 */
onMounted(() => {
	tickTimer.value = window.setInterval(() => automationStore.tick(), 1000);
});

onUnmounted(() => {
	if (tickTimer.value) window.clearInterval(tickTimer.value);
});

// Không còn sự cố nào bị ẩn thì thu gọn lại để lần sau hiện đúng trạng thái mặc định
watch(hiddenCount, (value) => {
	if (value === 0) isExpanded.value = false;
});
</script>

<style scoped lang="scss">
.automation-banner-wrap {
	position: fixed;
	top: 72px;
	left: 50%;
	transform: translateX(-50%);
	/* Trên modal bản đồ (≤10110) nhưng dưới mọi lớp phủ Element Plus (mốc 20000, xem main.ts):
	   banner là cảnh báo nền, không được che dialog mà người trực đang làm việc.
	   Bảng thứ tự lớp phủ: theme/element.scss mục "Thứ tự lớp phủ". */
	z-index: 15000;
	display: flex;
	flex-direction: column;
	gap: 6px;
	width: min(820px, calc(100vw - 32px));
	pointer-events: none;
	/* Khi mở rộng danh sách nhiều sự cố: giới hạn chiều cao và cho cuộn, không tràn khỏi màn hình */
	max-height: calc(100vh - 96px);
	overflow-y: auto;
	/* Chừa chỗ cho bóng đổ khỏi bị cắt khi có thanh cuộn */
	padding: 4px;
	scrollbar-width: thin;
}

/* Thanh tóm tắt khi có nhiều sự cố cùng lúc */
.automation-summary {
	pointer-events: auto;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	padding: 4px 12px;
	border-radius: 999px;
	font-size: 12px;
	color: var(--el-text-color-regular);
	background: var(--el-bg-color-overlay);
	border: 1px solid var(--el-border-color-light);
	box-shadow: var(--el-box-shadow-light);

	.summary-label {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		font-weight: 500;
	}

	.summary-icon {
		font-size: 14px;
		color: var(--el-color-danger);
	}
}

.automation-banner-list {
	display: flex;
	flex-direction: column;
	gap: 6px;
}

.automation-banner {
	pointer-events: auto;
	position: relative;
	display: flex;
	align-items: center;
	gap: 10px;
	/* Chừa thêm chỗ ở dưới cho thanh tiến trình bám sát đáy */
	padding: 8px 12px 10px 14px;
	border-radius: 10px;
	overflow: hidden;
	cursor: pointer;
	background: var(--el-bg-color-overlay);
	border: 1px solid var(--el-border-color-light);
	box-shadow: var(--el-box-shadow-light);
	transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;

	&:hover {
		transform: translateY(-1px);
		box-shadow: var(--el-box-shadow);
	}

	/* Nhóm không có quyền thao tác: chỉ là khối thông tin, không gợi ý click */
	&.is-readonly {
		cursor: default;

		&:hover {
			transform: none;
			box-shadow: var(--el-box-shadow-light);
		}
	}

	/* Ba mức độ gấp, dùng biến cục bộ để dải màu / số đếm / thanh tiến trình luôn đồng màu */
	&.is-normal {
		--banner-accent-color: var(--el-color-warning);
	}

	&.is-high {
		--banner-accent-color: var(--el-color-danger);
	}

	&.is-critical {
		--banner-accent-color: var(--el-color-danger);
		border-color: var(--el-color-danger-light-5);
		animation: banner-pulse 1.1s ease-in-out infinite;
	}
}

/* Dải màu dọc bên trái */
.banner-accent {
	position: absolute;
	top: 0;
	bottom: 0;
	left: 0;
	width: 3px;
	background: var(--banner-accent-color);
}

.banner-icon {
	flex: 0 0 auto;
	width: 32px;
	height: 32px;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: 8px;
	background: var(--el-fill-color-light);
	color: var(--banner-accent-color);

	.event-icon {
		width: 22px;
		height: 22px;
	}

	.fallback-icon {
		font-size: 18px;
	}
}

.banner-body {
	flex: 1 1 auto;
	min-width: 0;

	.banner-title {
		display: flex;
		align-items: center;
		gap: 6px;
		min-width: 0;

		.title-text {
			font-weight: 600;
			font-size: 14px;
			line-height: 1.3;
			color: var(--el-text-color-primary);
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
		}
	}

	.banner-meta {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 4px 10px;
		margin-top: 3px;
		font-size: 12px;
		line-height: 1.4;
		color: var(--el-text-color-secondary);

		.meta-item {
			display: inline-flex;
			align-items: center;
			gap: 3px;
			white-space: nowrap;

			.el-icon {
				font-size: 12px;
			}
		}

		/* Lý trình là thông tin định vị quan trọng nhất nên đậm hơn phần còn lại */
		.meta-strong {
			font-weight: 600;
			color: var(--el-text-color-primary);
		}

		/* Tên kịch bản có thể dài, cho phép cắt bớt thay vì đẩy các mục khác xuống dòng */
		.meta-plan {
			min-width: 0;
			overflow: hidden;
			text-overflow: ellipsis;
		}

		/* TagInfo không nhận prop size, phải thu nhỏ tại đây cho khớp dòng meta 12px */
		:deep(.el-tag) {
			height: 20px;
			padding: 0 6px;
			font-size: 11px;
			line-height: 18px;
		}
	}
}

/* Tag không được co lại làm mất chữ */
.nowrap-tag {
	flex: 0 0 auto;
}

.banner-countdown {
	flex: 0 0 auto;
	display: flex;
	flex-direction: column;
	align-items: center;
	padding: 0 10px;
	border-left: 1px solid var(--el-border-color-lighter);

	.countdown-label {
		font-size: 10px;
		line-height: 1.2;
		white-space: nowrap;
		color: var(--el-text-color-secondary);
	}

	.countdown-value {
		font-size: 20px;
		font-weight: 700;
		font-variant-numeric: tabular-nums;
		letter-spacing: 0.5px;
		line-height: 1.25;
		color: var(--banner-accent-color);
	}
}

.banner-actions {
	flex: 0 0 auto;
	display: flex;
	flex-direction: column;
	align-items: stretch;
	gap: 4px;

	/* Hai nút xếp dọc nên cần cùng bề rộng để mép phải thẳng hàng */
	.el-button {
		margin-left: 0;
		min-width: 96px;
	}
}

/* Thanh tiến trình nằm sát đáy, vơi dần về 0 khi tới hạn */
.banner-progress {
	position: absolute;
	bottom: 0;
	left: 0;
	height: 3px;
	background: var(--banner-accent-color);
	opacity: 0.85;
	transition: width 1s linear;
}

@keyframes banner-pulse {
	0%,
	100% {
		box-shadow: var(--el-box-shadow-light);
	}
	50% {
		box-shadow: 0 0 0 4px var(--el-color-danger-light-8), var(--el-box-shadow);
	}
}

/* Hiệu ứng vào/ra của banner */
.automation-banner-enter-active,
.automation-banner-leave-active {
	transition: all 0.3s ease;
}

.automation-banner-enter-from {
	opacity: 0;
	transform: translateY(-16px);
}

.automation-banner-leave-to {
	opacity: 0;
	transform: translateY(-16px);
}

/* Màn hình nhỏ: cho đếm ngược và nút xuống dòng riêng cho khỏi chật */
@media (max-width: 700px) {
	.automation-banner {
		flex-wrap: wrap;
	}

	.banner-countdown {
		flex-direction: row;
		align-items: baseline;
		gap: 6px;
		padding-left: 0;
		border-left: none;
	}

	.banner-actions {
		flex-direction: row;
		flex: 1 1 auto;

		.el-button {
			flex: 1 1 0;
			min-width: 0;
		}
	}
}
</style>
