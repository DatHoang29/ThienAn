<template>
	<footer class="playback-bar is-compact">
		<el-tooltip :content="playToggleTooltip" placement="top" :show-after="300" :hide-after="100"
			:teleported="false" :z-index="CameraControlLayer.MESSAGE">
			<el-button class="camera-control-btn camera-control-btn--icon-only" plain
				:disabled="loading || playbackControlsDisabled" @click="togglePause">
				<el-icon>
					<ele-VideoPlay v-if="paused" />
					<ele-VideoPause v-else />
				</el-icon>
			</el-button>
		</el-tooltip>

		<!-- Quay về xem trực tiếp: xóa lựa chọn playback trong thanh rồi báo cha đổi nguồn video sang luồng live.
			 Sáng (is-active) khi đang ở chế độ live để người dùng biết trạng thái hiện tại. -->
		<el-tooltip :content="t('lz.button.cctvDashboard.backToLive')" placement="top" :show-after="300"
			:hide-after="100" :teleported="false" :z-index="CameraControlLayer.MESSAGE">
			<el-button class="camera-control-btn camera-control-btn--labeled playback-live-btn"
				:class="{ 'is-active': !props.playbackActive }" plain :disabled="loading" @click="backToLive">
				<el-icon><ele-RefreshLeft /></el-icon>
				Live
			</el-button>
		</el-tooltip>

		<!-- Luôn cho mở/chuyển tháng; chỉ cho chọn ngày có footage. -->
		<el-tooltip :content="dayPickerHint" placement="top" :show-after="180" :hide-after="100" :teleported="false"
			:z-index="CameraControlLayer.MESSAGE">
			<span class="playback-day-picker-wrap">
				<DatePicker ref="dayPickerRef" v-model="selectedDayModel" dateFormat="dd/mm/yy"
					:placeholder="t('lz.label.cctvDashboard.selectDate')"
					:maxDate="dayPickerMaxDate" :disabledDates="disabledDayDates" :manualInput="false"
					:selectOtherMonths="false" :disabled="!canSelectDate" @month-change="onMonthChange" @year-change="onYearChange" showIcon
					iconDisplay="input" appendTo="body" class="playback-day-picker" size="small"
					:baseZIndex="dayPanelZIndex"
					:pt="{ panel: { class: 'cctv-embedded-day-panel', style: { '--cctv-day-panel-z': dayPanelZIndex } } }"
					@show="watchOutsidePointer(); recordingCalendar.onShow(dayPickerRef)" @hide="unwatchOutsidePointer" />
			</span>
		</el-tooltip>

		<div class="playback-timeline">
			<div v-if="hasSelectedDay" class="timeline-strip" @click="seekFromTimelineClick"
				@mousemove="previewTimelineHover" @mouseleave="clearTimelineHover" @wheel.prevent="zoomTimeline">
				<div class="timeline-track">
					<span v-for="segment in timelineSegments" :key="segment.key" class="available-segment"
						:style="{ left: `${segment.left}%`, width: `${segment.width}%` }" :title="segment.title"></span>
					<span v-for="tick in timelineTicks" :key="tick.key" class="timeline-tick"
						:style="{ left: `${tick.left}%` }">
						<small>{{ tick.label }}</small>
					</span>
					<span v-if="hoverSecond !== null" class="hover-marker"
						:style="{ left: `${secondToPercent(hoverSecond)}%` }">
						<small>{{ hoverTimeLabel }}</small>
					</span>
					<span v-if="actualSecond !== null" class="actual-marker"
						:style="{ left: `${secondToPercent(actualSecond)}%` }"></span>
				</div>
				<input v-model.number="timelineSecond" class="timeline-range" type="range" :min="visibleStartSecond"
					:max="visibleEndSecond" step="1" :disabled="periodLoading || !deviceId" @click.stop
					@input="previewTimelineValue" @change="commitTimelineValue" />
			</div>

			<div class="timeline-labels">
				<span class="edge">{{ hasSelectedDay ? visibleStartLabel : '' }}</span>
				<span class="meta-time">{{ metaTimeText }}</span>
				<span class="edge">{{ hasSelectedDay ? visibleEndLabel : '' }}</span>
			</div>
		</div>

		<el-dialog v-model="exportDialogVisible" :title="t('lz.label.cctvDashboard.downloadVideo')" width="420px"
			:append-to-body="false" destroy-on-close class="cctv-export-dialog" modal-class="cctv-export-modal"
			:z-index="CameraControlLayer.DIALOG">
			<div class="export-dialog-body">
				<label>{{ t('lz.entity.base.startTime') }}</label>
				<DatePicker ref="exportStartPickerRef" v-model="exportStartModel" dateFormat="dd/mm/yy" showTime
					showSeconds hourFormat="24" :placeholder="t('lz.label.base.startTime')"
					:minDate="exportMinDate || undefined" :maxDate="exportMaxDate || undefined" appendTo="self" fluid
					class="export-date-picker" @show="watchOutsidePointer" @hide="unwatchOutsidePointer" />
				<label>{{ t('lz.entity.base.endTime') }}</label>
				<DatePicker ref="exportEndPickerRef" v-model="exportEndModel" dateFormat="dd/mm/yy" showTime showSeconds
					hourFormat="24" :placeholder="t('lz.label.base.endTime')" :minDate="exportMinDate || undefined"
					:maxDate="exportMaxDate || undefined" appendTo="self" fluid class="export-date-picker"
					@show="watchOutsidePointer" @hide="unwatchOutsidePointer" />
				<small v-if="exportLimitLabel">{{ exportLimitLabel }}</small>
			</div>
			<template #footer>
				<el-button :disabled="loading" @click="exportDialogVisible = false">{{ t('lz.button.base.cancel')
					}}</el-button>
				<el-button type="primary" :loading="loading" @click="submitExport">{{
					t('lz.label.cctvDashboard.downloadVideo') }}</el-button>
			</template>
		</el-dialog>
	</footer>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import DatePicker from 'primevue/datepicker';
import type { VideoMonitoringItem } from '/@/views/cctv/dashboard/types';
import { loadCctvRecordingPeriods, type CctvRecordingPeriod } from '/@/views/cctv/dashboard/services/cctvDashboardService';
import '/@/views/cctv/dashboard/styles/cameraControlTokens.scss';
import { CameraControlLayer } from '/@/views/cctv/dashboard/constants/cameraControlLayers';
import { getNextPlaybackSpeed, formatPlaybackSpeed, isPlaybackControlsDisabled, resolveToggleTooltip } from '/@/views/cctv/dashboard/utils/cameraControlUtils';
import { useRecordingCalendar } from '/@/views/cctv/dashboard/utils/useRecordingCalendar';
import { closeOutsideDatePickers, type DatePickerLike } from './datePickerOutsideClick';
import { ZIndexHighestEnum } from '/@/types/enums/baseEnum';
import { i18n } from '/@/i18n';

const t = i18n.global.t;

// Panel lịch dùng appendTo="body" nên phải nổi trên MỌI lớp của app, kể cả el-dialog mở bằng
// ZIndexHighestEnum.Max (vd cameraPopup ở màn hình sự kiện). Cộng 1 để luôn cao hơn lớp cao nhất đó.
const dayPanelZIndex = ZIndexHighestEnum.Max + 1;

const props = defineProps<{
	camera?: VideoMonitoringItem;
	loading?: boolean;
	playbackActive?: boolean;
}>();

const emit = defineEmits<{
	(e: 'playback', payload: { deviceId: string; fromDate: Date; requestedDate?: Date; snapped?: boolean; speed?: number }): void;
	(e: 'playback-pause', payload: { paused: boolean; atDate?: Date }): void;
	(e: 'export-video', payload: { deviceId: string; fromDate: Date; toDate: Date }): void;
	(e: 'snapshot', payload: { deviceId: string; fromDate: Date }): void;
	(e: 'live'): void;
}>();

type NormalizedPeriod = CctvRecordingPeriod & {
	startMs: number;
	endMs: number;
};

const dayMs = 24 * 60 * 60 * 1000;
const secondMs = 1000;
const RTP_CLOCK_HZ = 90000; // đồng hồ media RTP cho video (90kHz)
const PLAYBACK_CLOCK_DEBUG = false; // bật để log nguồn thời gian đang dùng ra console
const daySeconds = 24 * 60 * 60 - 1;
const minVisibleSeconds = 15 * 60;
const padNumber = (value: number) => String(value).padStart(2, '0');

const formatDate = (date: Date) => [date.getFullYear(), padNumber(date.getMonth() + 1), padNumber(date.getDate())].join('-');
const formatDateVn = (date: Date) => [padNumber(date.getDate()), padNumber(date.getMonth() + 1), date.getFullYear()].join('/');
const formatTime = (date: Date) => `${padNumber(date.getHours())}:${padNumber(date.getMinutes())}:${padNumber(date.getSeconds())}`;
const formatDateTime = (date: Date) => `${formatDateVn(date)} ${formatTime(date)}`;
const formatDateTimeValue = (date: Date) => `${formatDate(date)} ${formatTime(date)}`;

const parseDateTime = (value: string) => {
	const text = value.trim();
	const match = /^(\d{4})-(\d{2})-(\d{2})[ T](\d{2}):(\d{2})(?::(\d{2}))?$/.exec(text);
	const vnMatch = /^(\d{2})\/(\d{2})\/(\d{4})[ T](\d{2}):(\d{2})(?::(\d{2}))?$/.exec(text);
	if (vnMatch) {
		const [, day, month, year, hour, minute, second = '0'] = vnMatch;
		const date = new Date(Number(year), Number(month) - 1, Number(day), Number(hour), Number(minute), Number(second));
		return date.getFullYear() === Number(year) && date.getMonth() === Number(month) - 1 && date.getDate() === Number(day) ? date : null;
	}
	if (!match) return null;
	const [, year, month, day, hour, minute, second = '0'] = match;
	const date = new Date(Number(year), Number(month) - 1, Number(day), Number(hour), Number(minute), Number(second));
	if (
		date.getFullYear() !== Number(year) ||
		date.getMonth() !== Number(month) - 1 ||
		date.getDate() !== Number(day) ||
		date.getHours() !== Number(hour) ||
		date.getMinutes() !== Number(minute)
	) {
		return null;
	}
	return date;
};

const parseDate = (value: string) => {
	const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value.trim());
	if (!match) return null;
	const [, year, month, day] = match;
	const date = new Date(Number(year), Number(month) - 1, Number(day));
	return date.getFullYear() === Number(year) && date.getMonth() === Number(month) - 1 && date.getDate() === Number(day) ? date : null;
};

const startOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate());
const addDays = (date: Date, days: number) => new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);

const toSafeString = (value: unknown, fallback = '') => {
	try {
		if (value === undefined || value === null) return fallback;
		if (typeof value === 'string') return value;
		if (typeof value === 'number' || typeof value === 'boolean') return String(value);
		return fallback;
	} catch {
		return fallback;
	}
};

const selectedDay = ref(startOfDay(new Date()));
const selectedDayInput = ref('');
const timelineSecond = ref(0);
const visibleStartSecond = ref(0);
const visibleEndSecond = ref(daySeconds);
const hoverSecond = ref<number | null>(null);
const requestedMs = ref<number | null>(null);
const actualMs = ref<number | null>(null);
const actualStartMs = ref<number | null>(null);
const mediaAnchorSec = ref<number | null>(null);
// Nguồn thời gian theo từng khung hình từ WebRTC.
// Ưu tiên rtpTimestamp (đồng hồ 90kHz gắn theo footage PTS, độc lập tốc độ phát, không trôi khi giật);
// fallback currentTime × speed nếu browser/phiên bản NX không cấp.
let rtpBaseRaw: number | null = null; // rtpTimestamp thô tại mốc neo
let rtpLastRaw: number | null = null; // rtpTimestamp thô lần trước (để unwrap 32-bit)
let rtpUnwrappedTicks = 0; // tổng tick đã unwrap kể từ mốc neo
let rtpScaleMode: 'footage' | 'transport' | null = null; // rtp đếm theo footage hay theo realtime
let rtpCalibPerfMs = 0; // mốc performance.now() lúc bắt đầu hiệu chuẩn scale
let lastStreamEpoch: number | null = null; // epoch stream gần nhất (CameraTile cấp) để phát hiện đổi stream
const snapMessage = ref('');
const periodLoading = ref(false);
// Lần tải danh sách bản ghi gần nhất có lỗi hay không (trước đây lỗi bị nuốt im lặng khiến ô chọn ngày
// bị khoá mà không rõ nguyên nhân).
const periodsLoadFailed = ref(false);
const hasSelectedDay = ref(false);
const periods = ref<NormalizedPeriod[]>([]);
const exportDialogVisible = ref(false);
const exportStartText = ref('');
const exportEndText = ref('');
const exportLimit = ref<{ startMs: number; endMs: number } | null>(null);
const paused = ref(false);
const playbackSpeed = ref(1);
let periodRequestVersion = 0;

// Ref các DatePicker của PrimeVue trong thanh (dùng để tự đóng panel khi bấm ra ngoài — xem
// closeDatePickersOnOutsidePointerDown).
const dayPickerRef = ref<DatePickerLike & { currentYear?: number; currentMonth?: number }>();
const exportStartPickerRef = ref<DatePickerLike>();
const exportEndPickerRef = ref<DatePickerLike>();
const datePickerRefs = [dayPickerRef, exportStartPickerRef, exportEndPickerRef];

const deviceId = computed(() => toSafeString(props.camera?.deviceId));
const recordingCalendar = useRecordingCalendar<CctvRecordingPeriod>(
	(range) => props.camera && deviceId.value ? loadCctvRecordingPeriods(props.camera, range) : Promise.resolve([]),
	() => ElMessage.error(t('lz.message.cctvDashboard.loadFootageFailed'))
);
const { disabledDates: disabledDayDates, onMonthChange, onYearChange } = recordingCalendar;
const cameraName = computed(() => toSafeString(props.camera?.name, 'Camera'));
const cameraIp = computed(() => toSafeString(props.camera?.ip));
const cameraLocation = computed(() => toSafeString(props.camera?.location));
// Model dạng Date cho PrimeVue DatePicker chọn ngày (null khi chưa chọn để hiện placeholder).
const selectedDayModel = computed<Date | null>({
	get: () => (selectedDayInput.value ? selectedDay.value : null),
	set: (value: Date | null) => {
		if (!value) {
			selectedDayInput.value = '';
			return;
		}
		const day = startOfDay(value);
		if (!canSelectDate.value || !Number.isFinite(day.getTime()) || day.getTime() > todayStartMs.value || !recordingCalendar.isDateAvailable(day)) return;
		selectedDay.value = day;
		selectedDayInput.value = formatDate(day);
		hasSelectedDay.value = true;
		clearClock();
		timelineSecond.value = 0;
		resetVisibleRange();
		loadPeriods({ autoPlayFromStart: true });
	},
});
const dayStartMs = computed(() => selectedDay.value.getTime());
const dayEndMs = computed(() => dayStartMs.value + dayMs);
const todayStartMs = computed(() => startOfDay(new Date()).getTime());
const isSelectedToday = computed(() => dayStartMs.value === todayStartMs.value);
const nowSecond = computed(() => Math.max(0, Math.min(daySeconds, Math.floor((Date.now() - todayStartMs.value) / secondMs))));
const maxTimelineSecond = computed(() => (isSelectedToday.value ? nowSecond.value : daySeconds));
const actualPlaybackDate = computed(() => (actualMs.value ? new Date(actualMs.value) : null));
// Vô hiệu hóa Play/Speed khi chưa có thiết bị hoặc chưa có đoạn playback (R2.7)
const playbackControlsDisabled = computed(() => isPlaybackControlsDisabled(deviceId.value, actualPlaybackDate.value));
// Tooltip động khớp trạng thái Play/Pause (R4.4): đang phát -> "Tạm dừng", đang dừng -> "Phát"
const playToggleTooltip = computed(() => resolveToggleTooltip({
	isOpenOrActive: !paused.value,
	openLabel: t('lz.button.cctvDashboard.pause'),
	closeLabel: t('lz.button.cctvDashboard.play'),
}));
const requestedTimeLabel = computed(() => formatDateTime(new Date(dayStartMs.value + timelineSecond.value * secondMs)));
const actualTimeLabel = computed(() =>
	actualPlaybackDate.value
		? t('lz.label.cctvDashboard.playingAt', { time: formatDateTime(actualPlaybackDate.value) })
		: t('lz.label.cctvDashboard.noRecordingSelected')
);
// Đồng hồ thời gian thực (tick mỗi giây) để hiển thị khi chưa chọn ngày / chưa có timeline.
const nowTick = ref(new Date());
let clockTimer: ReturnType<typeof setInterval> | null = null;
const currentTimeText = computed(() => formatDateTime(nowTick.value));
// Thời gian đang phát (chỉ giá trị, không kèm chữ "Đang phát").
const playbackTimeText = computed(() => (actualPlaybackDate.value ? formatDateTime(actualPlaybackDate.value) : ''));
// Nhãn dưới timeline: có ngày + đang phát -> giờ đang phát; ngược lại -> giờ hiện tại.
const metaTimeText = computed(() => (hasSelectedDay.value && playbackTimeText.value ? playbackTimeText.value : currentTimeText.value));
const canGoNextDay = computed(() => addDays(hasSelectedDay.value ? selectedDay.value : startOfDay(new Date()), 1).getTime() <= todayStartMs.value);
const exportLimitLabel = computed(() =>
	exportLimit.value
		? t('lz.label.cctvDashboard.exportLimit', {
			from: formatDateTime(new Date(exportLimit.value.startMs)),
			to: formatDateTime(new Date(exportLimit.value.endMs)),
		})
		: ''
);

// Model dạng Date cho PrimeVue DatePicker export (giữ chuỗi exportStartText/exportEndText làm nguồn cho submitExport).
const exportStartModel = computed<Date | null>({
	get: () => parseDateTime(exportStartText.value),
	set: (value: Date | null) => {
		exportStartText.value = value ? formatDateTimeValue(value) : '';
	},
});
const exportEndModel = computed<Date | null>({
	get: () => parseDateTime(exportEndText.value),
	set: (value: Date | null) => {
		exportEndText.value = value ? formatDateTimeValue(value) : '';
	},
});
// Giới hạn khoảng thời gian export (thay cho isExportDateDisabled của Element Plus).
const exportMinDate = computed<Date | null>(() => (exportLimit.value ? new Date(exportLimit.value.startMs) : null));
const exportMaxDate = computed<Date | null>(() => (exportLimit.value ? new Date(exportLimit.value.endMs) : null));
const visibleDurationSecond = computed(() => Math.max(1, visibleEndSecond.value - visibleStartSecond.value));
const visibleStartLabel = computed(() => formatTime(new Date(dayStartMs.value + visibleStartSecond.value * secondMs)));
const visibleEndLabel = computed(() => formatTime(new Date(dayStartMs.value + visibleEndSecond.value * secondMs)));
const hoverTimeLabel = computed(() => (hoverSecond.value === null ? '' : formatDateTime(new Date(dayStartMs.value + hoverSecond.value * secondMs))));
// Kết quả trong một cửa sổ tải không quyết định quyền chọn ngày ở ngoài cửa sổ đó.
const canSelectDate = computed(() => Boolean(deviceId.value.trim()));

// Nhãn trạng thái của ô chọn ngày: nói rõ vì sao đang khoá / chưa có bản ghi, thay vì để người dùng bấm
// vào một ô im lặng không phản hồi.
const dayPickerHint = computed(() => {
	if (!deviceId.value) return t('lz.message.cctvDashboard.controlUnavailableNoDeviceId');
	if (periodLoading.value || recordingCalendar.loading.value) return t('lz.label.cctvDashboard.loadingFootage');
	if (periodsLoadFailed.value || recordingCalendar.failed.value) return t('lz.message.cctvDashboard.loadFootageFailed');
	if (hasSelectedDay.value && !dayPeriods.value.length) return t('lz.message.cctvDashboard.noFootageForDate');
	return t('lz.label.cctvDashboard.selectDate');
});

// Không giới hạn tháng lịch sử; từng ngày được bật theo footage của tháng đang hiển thị.
const dayPickerMaxDate = computed(() => startOfDay(new Date()));

const dayPeriods = computed(() =>
	periods.value
		.map((period) => ({
			...period,
			startMs: Math.max(period.startMs, dayStartMs.value),
			endMs: Math.min(period.endMs, dayEndMs.value),
		}))
		.filter((period) => period.endMs > dayStartMs.value && period.startMs < dayEndMs.value && period.endMs > period.startMs)
		.sort((left, right) => left.startMs - right.startMs)
);

const timelineSegments = computed(() =>
	dayPeriods.value.map((period, index) => {
		const startSecond = Math.max(0, Math.floor((period.startMs - dayStartMs.value) / secondMs));
		const endSecond = Math.min(daySeconds, Math.ceil((period.endMs - dayStartMs.value) / secondMs));
		const clippedStart = Math.max(startSecond, visibleStartSecond.value);
		const clippedEnd = Math.min(endSecond, visibleEndSecond.value);
		const left = secondToPercent(clippedStart);
		const right = secondToPercent(clippedEnd);
		return {
			key: `${period.serverId}-${period.startMs}-${index}`,
			left,
			width: clippedEnd > clippedStart ? Math.max(0.15, right - left) : 0,
			title: `${formatDateTime(new Date(period.startMs))} - ${formatDateTime(new Date(period.endMs))}`,
		};
	}).filter((segment) => segment.width > 0)
);

const actualSecond = computed(() => (actualMs.value ? Math.max(0, Math.min(daySeconds, Math.floor((actualMs.value - dayStartMs.value) / secondMs))) : null));
const timelineTicks = computed(() => {
	const ticks: { key: string; left: number; label: string }[] = [];
	for (let index = 1; index < 8; index++) {
		const second = Math.round(visibleStartSecond.value + (visibleDurationSecond.value * index) / 8);
		ticks.push({
			key: `eighth-${index}-${second}`,
			left: secondToPercent(second),
			label: formatTime(new Date(dayStartMs.value + second * secondMs)).slice(0, 5),
		});
	}
	return ticks;
});

function secondToPercent(second: number) {
	return ((Math.max(visibleStartSecond.value, Math.min(visibleEndSecond.value, second)) - visibleStartSecond.value) / visibleDurationSecond.value) * 100;
}

const normalizePeriod = (period: CctvRecordingPeriod, fallbackEndMs: number): NormalizedPeriod => {
	const startMs = Number(period.startTimeMs);
	const durationMs = period.durationMs === undefined ? undefined : Number(period.durationMs);
	const endMs = durationMs && Number.isFinite(durationMs) && durationMs > 0 ? Math.min(startMs + durationMs, fallbackEndMs) : fallbackEndMs;
	return {
		...period,
		startMs,
		endMs,
	};
};

const clearClock = () => {
	mediaAnchorSec.value = null;
	rtpBaseRaw = null;
	rtpLastRaw = null;
	rtpUnwrappedTicks = 0;
	rtpScaleMode = null;
	rtpCalibPerfMs = 0;
	lastStreamEpoch = null;
};

const prepareClock = (startMs: number) => {
	paused.value = false;
	playbackSpeed.value = 1;
	actualStartMs.value = startMs;
	actualMs.value = startMs;
	clearClock();
};

const confirmPlaybackStarted = () => {
	// Đồng hồ được dẫn động bằng tiến độ render của video; chỉ cần re-base mốc khi video báo sẵn sàng.
	if (paused.value || actualStartMs.value === null) return;
	clearClock();
};

// Áp footage (ms epoch) lên đồng hồ hiển thị, kèm xử lý hết đoạn ghi hình -> nhảy đoạn tiếp theo.
const applyFootageMs = (footageMs: number) => {
	if (actualStartMs.value === null) return;
	const currentActualMs = actualMs.value ?? actualStartMs.value;
	const activePeriod = findContainingPeriod(currentActualMs);
	if (activePeriod && footageMs >= activePeriod.endMs) {
		actualMs.value = activePeriod.endMs;
		clearClock();
		playNextAvailableFrom(activePeriod.endMs + 1);
		return;
	}
	// Không cho lùi về trước mốc bắt đầu (nhiễu mẫu đầu phiên).
	actualMs.value = footageMs >= actualStartMs.value ? footageMs : actualStartMs.value;
};

// Nguồn chuẩn: rtpTimestamp (đồng hồ 90kHz) = footage PTS THỰC của từng khung hình. Đây là thời gian
// gốc của khung hình đang hiển thị, đúng ở mọi tốc độ. KHÔNG dùng currentTime×speed vì server thường
// không đạt đúng tốc độ danh nghĩa (vd x2 thực tế chỉ ~1.82×) -> currentTime×2 sẽ vượt ~1.8s.
const applyProgressSample = (sample: { mediaTimeSec: number; rtpTimestamp?: number; captureTimeMs?: number; streamEpoch?: number }) => {
	if (actualStartMs.value === null) return;

	// Đổi stream (kết nối lại / đổi tốc độ): mốc rtp của stream mới không liên quan stream cũ -> neo lại
	// từ đầu (stream mới phát lại từ vị trí pos hiện tại). Dùng epoch thay vì đoán theo độ lớn bước nhảy,
	// nhờ vậy "cùng stream nhảy lớn" (tab ẩn lâu rồi quay lại) vẫn được cộng dồn để đuổi kịp video.
	if (sample.streamEpoch !== undefined && sample.streamEpoch !== lastStreamEpoch) {
		lastStreamEpoch = sample.streamEpoch;
		rtpBaseRaw = null;
		rtpLastRaw = null;
		rtpUnwrappedTicks = 0;
		mediaAnchorSec.value = null;
		actualMs.value = actualStartMs.value;
	}

	// (A) captureTime là UTC tuyệt đối (epoch ms) -> chính là footage UTC, dùng thẳng.
	if (typeof sample.captureTimeMs === 'number' && Number.isFinite(sample.captureTimeMs) && sample.captureTimeMs > 1e12) {
		applyFootageMs(Math.round(sample.captureTimeMs));
		return;
	}

	// (B) rtpTimestamp = footage PTS khung hình, dùng trực tiếp (không nhân tốc độ).
	if (typeof sample.rtpTimestamp === 'number' && Number.isFinite(sample.rtpTimestamp)) {
		const raw = sample.rtpTimestamp >>> 0; // ép uint32
		if (rtpBaseRaw === null) {
			rtpBaseRaw = raw;
			rtpLastRaw = raw;
			rtpUnwrappedTicks = 0;
			actualMs.value = actualStartMs.value;
			return;
		}
		// Unwrap 32-bit (rtpTimestamp vòng lại sau ~13 giờ ở 90kHz). Cộng dồn mọi bước (kể cả nhảy lớn
		// khi tab quay lại) để đồng hồ đuổi kịp video; việc đổi stream đã được epoch ở trên xử lý.
		let diff = raw - (rtpLastRaw as number);
		if (diff < -0x80000000) diff += 0x100000000;
		else if (diff > 0x80000000) diff -= 0x100000000;
		rtpUnwrappedTicks += diff;
		rtpLastRaw = raw;
		const rtpElapsedSec = rtpUnwrappedTicks / RTP_CLOCK_HZ;
		const footageMs = actualStartMs.value + rtpElapsedSec * secondMs;
		if (PLAYBACK_CLOCK_DEBUG) {
			console.log(`[clk] speed=${playbackSpeed.value} rtpElapsed=${rtpElapsedSec.toFixed(2)}s footage=${new Date(footageMs).toLocaleTimeString()}`);
		}
		applyFootageMs(footageMs);
		return;
	}

	// (C) Fallback (không có rtp): currentTime × speed.
	if (!Number.isFinite(sample.mediaTimeSec) || sample.mediaTimeSec < 0) return;
	if (mediaAnchorSec.value === null) {
		mediaAnchorSec.value = sample.mediaTimeSec;
		actualMs.value = actualStartMs.value;
		return;
	}
	const mediaElapsedSec = sample.mediaTimeSec - mediaAnchorSec.value;
	if (mediaElapsedSec < 0) {
		mediaAnchorSec.value = sample.mediaTimeSec;
		return;
	}
	applyFootageMs(actualStartMs.value + mediaElapsedSec * Math.max(1, playbackSpeed.value) * secondMs);
};

const syncPlaybackProgress = (sample: { mediaTimeSec: number; rtpTimestamp?: number; captureTimeMs?: number; final?: boolean; streamEpoch?: number }) => {
	if (paused.value || actualStartMs.value === null) return;
	applyProgressSample(sample);
};

const togglePause = () => {
	if (playbackControlsDisabled.value) return;
	const playbackDate = actualPlaybackDate.value;
	if (!playbackDate) return;
	if (paused.value) {
		const resumeMs = playbackDate.getTime();
		playbackSpeed.value = 1;
		actualStartMs.value = resumeMs;
		actualMs.value = resumeMs;
		clearClock();
		paused.value = false;
		emit('playback-pause', { paused: false, atDate: new Date(resumeMs) });
		emit('playback', {
			deviceId: deviceId.value,
			fromDate: new Date(resumeMs),
			requestedDate: new Date(resumeMs),
			speed: 1,
		});
		return;
	}
	// PAUSE: đóng băng tại giá trị hiện tại (đang bám đúng khung hình nhờ currentTime×speed) rồi khóa.
	// currentTime dừng tăng khi video pause nên đồng hồ cũng đứng theo, không chạy tiếp.
	clearClock();
	actualStartMs.value = playbackDate.getTime();
	paused.value = true;
	emit('playback-pause', { paused: true, atDate: playbackDate });
};

// Đưa thanh về trạng thái ban đầu (chưa chọn ngày, không có mốc playback) — dùng khi đổi camera hoặc
// khi quay về xem trực tiếp. Giữ nguyên danh sách `periods` để bộ chọn ngày vẫn dùng được.
const resetPlaybackSelection = () => {
	clearClock();
	selectedDay.value = startOfDay(new Date());
	selectedDayInput.value = '';
	hasSelectedDay.value = false;
	timelineSecond.value = 0;
	resetVisibleRange();
	requestedMs.value = null;
	actualMs.value = null;
	actualStartMs.value = null;
	hoverSecond.value = null;
	paused.value = false;
	playbackSpeed.value = 1;
	snapMessage.value = '';
};

// Nút Live: bỏ mốc playback đang chọn rồi báo cha (CctvFocusPanel) đổi nguồn video về luồng trực tiếp.
const backToLive = () => {
	resetPlaybackSelection();
	emit('live');
};

const cyclePlaybackSpeed = () => {
	if (playbackControlsDisabled.value) return;
	const playbackDate = actualPlaybackDate.value;
	if (!playbackDate) return;
	const startMs = playbackDate.getTime();
	const nextSpeed = getNextPlaybackSpeed(playbackSpeed.value);
	playbackSpeed.value = nextSpeed;
	paused.value = false;
	// Đổi tốc độ phát lại stream mới từ startMs -> video khởi động lại, re-base mốc.
	actualStartMs.value = startMs;
	actualMs.value = startMs;
	clearClock();
	emit('playback-pause', { paused: false, atDate: new Date(startMs) });
	emit('playback', {
		deviceId: deviceId.value,
		fromDate: new Date(startMs),
		requestedDate: new Date(startMs),
		speed: nextSpeed,
	});
};

const findContainingPeriod = (targetMs: number) => dayPeriods.value.find((period) => targetMs >= period.startMs && targetMs < period.endMs);

const findNearestPlaybackStart = (targetMs: number) => {
	if (!dayPeriods.value.length) return null;
	const containing = findContainingPeriod(targetMs);
	if (containing) return targetMs;
	const nextPeriod = dayPeriods.value.find((period) => period.startMs > targetMs);
	if (nextPeriod) return nextPeriod.startMs;
	return dayPeriods.value[dayPeriods.value.length - 1].startMs;
};

const findNextPeriodAfter = (targetMs: number) =>
	periods.value
		.filter((period) => period.endMs > targetMs)
		.sort((left, right) => left.startMs - right.startMs)
		.find((period) => period.startMs >= targetMs || (targetMs >= period.startMs && targetMs < period.endMs));

const setTimelineFromMs = (value: number) => {
	timelineSecond.value = Math.max(0, Math.min(maxTimelineSecond.value, Math.floor((value - dayStartMs.value) / secondMs)));
};

const resetVisibleRange = () => {
	visibleStartSecond.value = 0;
	visibleEndSecond.value = maxTimelineSecond.value;
};

const clampVisibleRange = () => {
	const maxSecond = maxTimelineSecond.value;
	if (visibleEndSecond.value > maxSecond) visibleEndSecond.value = maxSecond;
	if (visibleStartSecond.value >= visibleEndSecond.value) visibleStartSecond.value = Math.max(0, visibleEndSecond.value - minVisibleSeconds);
	if (visibleEndSecond.value - visibleStartSecond.value < minVisibleSeconds) {
		visibleEndSecond.value = Math.min(maxSecond, visibleStartSecond.value + minVisibleSeconds);
		visibleStartSecond.value = Math.max(0, visibleEndSecond.value - minVisibleSeconds);
	}
};

const selectInitialAvailableTime = () => {
	if (!dayPeriods.value.length) {
		timelineSecond.value = 0;
		requestedMs.value = null;
		actualMs.value = null;
		snapMessage.value = '';
		clearClock();
		return;
	}
	const firstPeriod = dayPeriods.value[0];
	setTimelineFromMs(firstPeriod.startMs);
	requestedMs.value = firstPeriod.startMs;
	actualMs.value = firstPeriod.startMs;
	snapMessage.value = t('lz.label.cctvDashboard.timelineHint');
};

const loadPeriods = async (options: { autoPlayFromStart?: boolean } = {}) => {
	const camera = props.camera;
	if (!camera || !deviceId.value) return;
	const version = ++periodRequestVersion;
	periodLoading.value = true;
	actualMs.value = null;
	actualStartMs.value = null;
	clearClock();
	if (props.playbackActive) {
		paused.value = true;
		emit('playback-pause', { paused: true });
	}
	try {
		const { periods: result, toDate } = await recordingCalendar.loadMonth(selectedDay.value);
		if (version !== periodRequestVersion) {
			return;
		}
		const rangeEndMs = toDate.getTime();
		periods.value = result.map((period) => normalizePeriod(period, rangeEndMs)).filter((period) => Number.isFinite(period.startMs) && period.endMs > period.startMs);
		periodsLoadFailed.value = false;
		selectInitialAvailableTime();
		if (options.autoPlayFromStart) {
			timelineSecond.value = 0;
			previewTimelineValue();
			commitTimelineValue();
		}
	} catch {
		if (version === periodRequestVersion) {
			periods.value = [];
			periodsLoadFailed.value = true;
			selectInitialAvailableTime();
			ElMessage.error(t('lz.message.cctvDashboard.loadFootageFailed'));
		}
	} finally {
		if (version === periodRequestVersion) periodLoading.value = false;
	}
};

const loadAvailablePeriods = async () => {
	if (!props.camera || !deviceId.value) return;
	await recordingCalendar.showMonth(new Date());
};

const previewTimelineValue = () => {
	if (timelineSecond.value > maxTimelineSecond.value) timelineSecond.value = maxTimelineSecond.value;
	requestedMs.value = dayStartMs.value + timelineSecond.value * secondMs;
	snapMessage.value = '';
};

const commitTimelineValue = () => {
	if (!deviceId.value) return;
	if (timelineSecond.value > maxTimelineSecond.value) timelineSecond.value = maxTimelineSecond.value;
	const targetMs = dayStartMs.value + timelineSecond.value * secondMs;
	requestedMs.value = targetMs;
	const playbackStartMs = findNearestPlaybackStart(targetMs);
	if (!playbackStartMs) {
		ElMessage.warning(t('lz.message.cctvDashboard.noFootageForDate'));
		return;
	}
	const snapped = Math.abs(playbackStartMs - targetMs) >= secondMs;
	if (snapped) {
		setTimelineFromMs(playbackStartMs);
		requestedMs.value = playbackStartMs;
		snapMessage.value = t('lz.message.cctvDashboard.playbackTimeAdjusted', {
			requested: formatDateTime(new Date(targetMs)),
			actual: formatDateTime(new Date(playbackStartMs)),
		});
		ElMessage.warning(snapMessage.value);
	} else {
		snapMessage.value = '';
	}
	prepareClock(playbackStartMs);
	emit('playback', {
		deviceId: deviceId.value,
		fromDate: new Date(playbackStartMs),
		requestedDate: new Date(targetMs),
		snapped,
		speed: 1,
	});
};

const playNextAvailableFrom = (targetMs: number) => {
	const nextPeriod = findNextPeriodAfter(targetMs);
	if (!nextPeriod) {
		snapMessage.value = t('lz.message.cctvDashboard.playbackFinished');
		return;
	}
	const nextDay = startOfDay(new Date(nextPeriod.startMs));
	if (nextDay.getTime() > todayStartMs.value || nextPeriod.startMs > Date.now()) {
		snapMessage.value = t('lz.message.cctvDashboard.reachedCurrentTime');
		return;
	}
	const crossedDay = nextDay.getTime() !== dayStartMs.value;
	if (crossedDay) {
		selectedDay.value = nextDay;
		selectedDayInput.value = formatDate(nextDay);
		timelineSecond.value = 0;
	} else {
		setTimelineFromMs(nextPeriod.startMs);
	}
	hasSelectedDay.value = true;
	previewTimelineValue();
	commitTimelineValue();
};

const getTimelineSecondFromPointer = (event: MouseEvent) => {
	const target = event.currentTarget as HTMLElement;
	const rect = target.getBoundingClientRect();
	if (!rect.width) return null;
	const percent = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
	return Math.round(visibleStartSecond.value + percent * visibleDurationSecond.value);
};

const seekFromTimelineClick = (event: MouseEvent) => {
	const second = getTimelineSecondFromPointer(event);
	if (second === null) return;
	timelineSecond.value = second;
	previewTimelineValue();
	commitTimelineValue();
};

const previewTimelineHover = (event: MouseEvent) => {
	const second = getTimelineSecondFromPointer(event);
	hoverSecond.value = second === null ? null : Math.max(visibleStartSecond.value, Math.min(visibleEndSecond.value, second));
};

const clearTimelineHover = () => {
	hoverSecond.value = null;
};

const zoomTimeline = (event: WheelEvent) => {
	const target = event.currentTarget as HTMLElement;
	const rect = target.getBoundingClientRect();
	if (!rect.width) return;
	const pointerRatio = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
	const currentDuration = visibleDurationSecond.value;
	const nextDuration = Math.max(
		minVisibleSeconds,
		Math.min(maxTimelineSecond.value || daySeconds, Math.round(currentDuration * (event.deltaY > 0 ? 1.25 : 0.8)))
	);
	const anchorSecond = visibleStartSecond.value + pointerRatio * currentDuration;
	let nextStart = Math.round(anchorSecond - pointerRatio * nextDuration);
	let nextEnd = nextStart + nextDuration;
	if (nextStart < 0) {
		nextEnd -= nextStart;
		nextStart = 0;
	}
	if (nextEnd > maxTimelineSecond.value) {
		nextStart -= nextEnd - maxTimelineSecond.value;
		nextEnd = maxTimelineSecond.value;
	}
	visibleStartSecond.value = Math.max(0, nextStart);
	visibleEndSecond.value = Math.max(visibleStartSecond.value + 1, nextEnd);
	clampVisibleRange();
};

const changeDay = async (offset: number) => {
	const baseDay = hasSelectedDay.value ? selectedDay.value : startOfDay(new Date());
	const nextDay = addDays(baseDay, offset);
	if (nextDay.getTime() > todayStartMs.value) return;
	const version = periodRequestVersion;
	await recordingCalendar.showMonth(nextDay);
	if (version !== periodRequestVersion) return;
	selectedDayModel.value = nextDay;
};

const openExportDialog = () => {
	if (!deviceId.value || !periods.value.length) {
		ElMessage.warning(t('lz.message.cctvDashboard.playbackBeforeDownload'));
		return;
	}
	// Giới hạn export = toàn bộ khoảng thời gian có footage (cho phép xuất qua nửa đêm/nhiều đoạn liền nhau).
	const availableStartMs = Math.min(...periods.value.map((period) => period.startMs));
	const availableEndMs = Math.max(...periods.value.map((period) => period.endMs));
	const limitStartMs = availableStartMs;
	const limitEndMs = Math.min(availableEndMs, Date.now());
	exportLimit.value = { startMs: limitStartMs, endMs: limitEndMs };
	// Mặc định điền cửa sổ 5 phút: quanh thời điểm đang phát; nếu chưa phát thì lấy mốc cuối footage.
	let startMs: number;
	let endMs: number;
	if (actualPlaybackDate.value) {
		const activeMs = actualPlaybackDate.value.getTime();
		startMs = Math.max(limitStartMs, activeMs);
		endMs = Math.min(limitEndMs, activeMs + 5 * 60 * 1000);
	} else {
		endMs = limitEndMs;
		startMs = Math.max(limitStartMs, endMs - 5 * 60 * 1000);
	}
	exportStartText.value = formatDateTimeValue(new Date(startMs));
	exportEndText.value = formatDateTimeValue(new Date(endMs));
	exportDialogVisible.value = true;
};

const submitExport = () => {
	if (!deviceId.value) {
		ElMessage.warning(t('lz.message.cctvDashboard.cameraDeviceIdNotFound'));
		return;
	}
	const fromDate = parseDateTime(exportStartText.value);
	const toDate = parseDateTime(exportEndText.value);
	if (!fromDate || !toDate || fromDate >= toDate) {
		ElMessage.warning(t('lz.message.cctvDashboard.invalidDownloadRange'));
		return;
	}
	if (!exportLimit.value || fromDate.getTime() < exportLimit.value.startMs || toDate.getTime() > exportLimit.value.endMs) {
		ElMessage.warning(t('lz.message.cctvDashboard.downloadRangeOutsideFootage'));
		return;
	}
	emit('export-video', { deviceId: deviceId.value, fromDate, toDate });
	exportDialogVisible.value = false;
};

const captureSnapshot = () => {
	if (!deviceId.value) {
		ElMessage.warning(t('lz.message.cctvDashboard.cameraDeviceIdNotFound'));
		return;
	}
	const fromDate = props.playbackActive && actualPlaybackDate.value ? actualPlaybackDate.value : new Date();
	emit('snapshot', { deviceId: deviceId.value, fromDate });
};

watch(
	() => [props.camera?.id, props.camera?.deviceId, props.camera?.ip].join('|'),
	() => {
		periodRequestVersion++;
		recordingCalendar.reset();
		periodLoading.value = false;
		periodsLoadFailed.value = false;
		resetPlaybackSelection();
		periods.value = [];
		exportStartText.value = '';
		exportEndText.value = '';
		exportLimit.value = null;
		exportDialogVisible.value = false;
		loadAvailablePeriods();
	},
	{ immediate: true }
);

// PrimeVue DatePicker đóng panel bằng listener 'mousedown' ở giai đoạn BUBBLE trên document. Khi thanh
// này được nhúng vào modal big screen (InfoPanel/DraggableModal chặn mousedown bằng @mousedown.stop để
// không kéo modal khi tương tác nội dung) event không lên tới document nên panel không bao giờ tự đóng.
// Vì vậy bắt mousedown ở giai đoạn CAPTURE (chạy trước mọi stopPropagation ở bubble) và tự đóng panel
// khi điểm bấm nằm ngoài ô input + ngoài panel (xem datePickerOutsideClick.ts).
const closeDatePickersOnOutsidePointerDown = (event: MouseEvent) => {
	closeOutsideDatePickers(
		datePickerRefs.map((pickerRef) => pickerRef.value),
		event.target
	);
};

// Chỉ theo dõi chuột khi có panel đang mở (bám theo event show/hide của chính PrimeVue). Nhờ vậy lần bấm
// MỞ panel không bao giờ đi qua listener này — listener chỉ được gắn sau khi panel đã mở xong.
let outsidePointerWatching = false;

const watchOutsidePointer = () => {
	if (outsidePointerWatching) return;
	outsidePointerWatching = true;
	document.addEventListener('mousedown', closeDatePickersOnOutsidePointerDown, true);
};

const unwatchOutsidePointer = () => {
	if (!outsidePointerWatching) return;
	// Vẫn còn panel khác đang mở (vd dialog xuất video có 2 ô thời gian) -> giữ listener.
	if (datePickerRefs.some((pickerRef) => pickerRef.value?.overlayVisible)) return;
	outsidePointerWatching = false;
	document.removeEventListener('mousedown', closeDatePickersOnOutsidePointerDown, true);
};

onMounted(() => {
	clockTimer = setInterval(() => {
		nowTick.value = new Date();
	}, 1000);
});

onBeforeUnmount(() => {
	periodRequestVersion++;
	recordingCalendar.reset();
	outsidePointerWatching = false;
	document.removeEventListener('mousedown', closeDatePickersOnOutsidePointerDown, true);
	clearClock();
	if (clockTimer) {
		clearInterval(clockTimer);
		clockTimer = null;
	}
});

defineExpose({
	confirmPlaybackStarted,
	syncPlaybackProgress,
});
</script>

<style scoped lang="scss">
.playback-bar {
	min-height: 112px;
	padding: 14px 18px;
	display: grid;
	grid-template-columns: minmax(280px, 0.9fr) minmax(360px, 2fr) minmax(320px, auto);
	align-items: center;
	gap: 18px;
	background: #0f172a;
	border-top: 1px solid rgba(255, 255, 255, 0.08);
	color: #ffffff;
}

.playback-left {
	display: flex;
	align-items: center;
	gap: 0px;
	min-width: 0;
}

.time-stack {
	display: grid;
	gap: 2px;
	min-width: 0;
	margin-left: 12px;

	strong,
	span,
	label,
	small {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	strong {
		font-size: 13px;
	}

	span {
		color: #cbd5e1;
		font-size: 12px;
	}

	label {
		color: #93c5fd;
		font-size: 12px;
	}

	small {
		color: #fbbf24;
		font-size: 11px;
	}
}

.playback-icon {
	width: 32px;
	height: 32px;
	border-radius: 999px;
	background: #2563eb;
	color: #ffffff;
	font-size: 16px;
}

.playback-timeline {
	display: grid;
	gap: 12px;
	min-width: 0;
	position: relative;
	z-index: 6;
}

.timeline-toolbar {
	display: grid;
	grid-template-columns: auto 150px auto;
	align-items: center;
	gap: 8px;

	:deep(.el-date-editor) {
		width: 150px;
	}
}

.timeline-strip {
	position: relative;
	height: 42px;
	cursor: pointer;
}

.timeline-track {
	position: absolute;
	left: 0;
	right: 0;
	top: 7px;
	height: 20px;
	border-radius: 6px;
	background:
		linear-gradient(to right, rgba(148, 163, 184, 0.14) 1px, transparent 1px) 0 0 / 8.333% 100%,
		rgba(15, 23, 42, 0.72);
	border: 1px solid rgba(148, 163, 184, 0.24);
	overflow: visible;
}

.available-segment {
	position: absolute;
	top: 0;
	bottom: 0;
	border-radius: 4px;
	background: rgba(34, 197, 94, 0.45);
	box-shadow: inset 0 0 0 1px rgba(74, 222, 128, 0.42);
}

.timeline-tick {
	position: absolute;
	top: 0;
	bottom: 0;
	width: 1px;
	background: rgba(226, 232, 240, 0.42);
	transform: translateX(-0.5px);
	pointer-events: none;

	small {
		position: absolute;
		top: 22px;
		left: 50%;
		color: #e2e8f0;
		font-size: 10px;
		line-height: 1;
		transform: translateX(-50%);
		white-space: nowrap;
		text-shadow: 0 1px 3px rgba(15, 23, 42, 0.8);
	}
}

.hover-marker {
	position: absolute;
	top: -3px;
	width: 1px;
	height: 26px;
	background: rgba(255, 255, 255, 0.85);
	transform: translateX(-0.5px);
	pointer-events: none;
	z-index: 3;

	small {
		position: absolute;
		left: 50%;
		bottom: 30px;
		padding: 4px 7px;
		border-radius: 4px;
		background: rgba(15, 23, 42, 0.94);
		border: 1px solid rgba(226, 232, 240, 0.2);
		color: #ffffff;
		font-size: 11px;
		line-height: 1;
		white-space: nowrap;
		box-shadow: 0 8px 18px rgba(15, 23, 42, 0.35);
		transform: translateX(-50%);
	}
}

.actual-marker {
	position: absolute;
	top: -4px;
	width: 2px;
	height: 28px;
	background: #f97316;
	border-radius: 999px;
	box-shadow: 0 0 0 3px rgba(249, 115, 22, 0.2);
	transform: translateX(-1px);
}

.timeline-range {
	position: absolute;
	left: 0;
	right: 0;
	top: 0;
	width: 100%;
	height: 34px;
	margin: 0;
	background: transparent;
	appearance: none;
	cursor: pointer;
}

.timeline-range::-webkit-slider-runnable-track {
	height: 20px;
	background: transparent;
}

.timeline-range::-webkit-slider-thumb {
	width: 2px;
	height: 28px;
	margin-top: -5px;
	border-radius: 4px;
	background: var(--el-color-primary, #409eff);
	box-shadow: 0 8px 18px rgba(15, 23, 42, 0.45);
	appearance: none;
}

.timeline-range::-moz-range-track {
	height: 20px;
	background: transparent;
}

.timeline-range::-moz-range-thumb {
	width: 2px;
	height: 28px;
	border: 0;
	border-radius: 4px;
	background: var(--el-color-primary, #409eff);
	box-shadow: 0 8px 18px rgba(15, 23, 42, 0.45);
}

.timeline-range:disabled {
	cursor: not-allowed;
	opacity: 0.55;
}

.timeline-labels {
	display: flex;
	align-items: center;
	justify-content: space-between;
	color: #cbd5e1;
	font-size: 11px;
}

.timeline-labels span:nth-child(2) {
	text-align: right;
}

.timeline-empty {
	min-height: 48px;
	display: flex;
	align-items: center;
	color: #94a3b8;
	font-size: 12px;
}

.playback-actions {
	display: flex;
	flex-wrap: wrap;
	justify-content: flex-end;
	gap: 8px;

	:deep(.el-button) {
		margin-left: 0;
	}
}

.export-dialog-body {
	display: grid;
	gap: 8px;
	overflow: visible;

	label {
		font-size: 13px;
		font-weight: 600;
		color: #344054;
	}

	:deep(.el-date-editor) {
		width: 100%;
	}
}

/* Export dialog dùng PrimeVue DatePicker với appendTo="self" (phải nằm trong cây fullscreen).
   Cho phép panel tràn ra ngoài thân dialog và nổi trên cùng (cao hơn DIALOG=10090). */
:deep(.cctv-export-dialog .el-dialog__body) {
	overflow: visible;
}

:deep(.export-date-picker) {
	width: 100%;
}

:deep(.p-datepicker-panel) {
	z-index: 10110 !important;
}

/* Khi nhúng (compact) DatePicker appendTo="body" -> panel ra ngoài scope component nên phải dùng
   :global. Không ghim cứng z-index nữa: khung camera có thể được nhúng trong el-dialog dùng
   ZIndexHighestEnum.Max (999999999) — ví dụ views/modal/common/cameraPopup.vue — nếu ghim 100000 thì
   panel nằm dưới dialog và "mở mà không thấy gì". Giá trị thật do biến --cctv-day-panel-z (đặt inline
   qua :pt từ dayPanelZIndex) quyết định; !important để thắng z-index inline mà PrimeVue tự set. */
:global(.cctv-embedded-day-panel) {
	z-index: var(--cctv-day-panel-z, 100000) !important;
}

:deep(.camera-playback-date-popper) {
	z-index: 10020 !important;
}

:global(.cctv-export-modal) {
	z-index: 10070 !important;
}

:global(.cctv-export-dialog) {
	z-index: 10071 !important;
}

:global(.cctv-fullscreen-popper) {
	z-index: 10080 !important;
}

/* ==== Chế độ gọn (small/nhúng bigScreen) ====
   Một hàng: play·pause, Live, chọn ngày, timeline.
   Các phần Xuất/Ảnh chụp, tên/IP/khu vực, nút Ngày trước-sau đã ẩn ở template. */
.playback-bar.is-compact {
	min-height: 0;
	padding: 10px 14px;
	grid-template-columns: auto auto auto minmax(0, 1fr);
	gap: 14px;
	align-items: center;
}

/* Nút Live: gọn hơn nút mặc định để không chiếm chỗ của timeline. */
.playback-bar.is-compact .playback-live-btn {
	--cctv-btn-padding-x: 10px;
	font-size: 12px;
}

.playback-bar.is-compact .playback-day-picker-wrap {
	display: inline-flex;
}

.playback-bar.is-compact .playback-day-picker {
	width: 150px;
}

.playback-bar.is-compact .playback-timeline {
	gap: 6px;
}

/* Nhãn thời gian (đang phát / hiện tại) nằm giữa, dưới timeline. */
.playback-bar.is-compact .timeline-labels .meta-time {
	color: #93c5fd;
	font-weight: 600;
	text-align: center;
	white-space: nowrap;
}

.playback-bar.is-compact .timeline-labels .edge {
	color: #cbd5e1;
	white-space: nowrap;
}

@media (max-width: 900px) {
	.playback-bar {
		grid-template-columns: 1fr;
		align-items: stretch;
	}

	.timeline-toolbar {
		grid-template-columns: auto minmax(0, 1fr) auto;

		:deep(.el-date-editor) {
			width: 100%;
		}
	}
}
</style>
