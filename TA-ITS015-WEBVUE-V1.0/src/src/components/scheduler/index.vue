<template>
	<el-col
		v-if="allowedKinds.length > 1"
		:xs="24"
		:sm="24"
		:md="$slots['header-extra'] ? 14 : 24"
		:lg="$slots['header-extra'] ? 14 : 24"
		:xl="$slots['header-extra'] ? 14 : 24"
		class="mb20"
	>
		<el-form-item :label="text.kind" :prop="kindProp">
			<el-radio-group v-model="scheduleKind" :disabled="disabled">
				<el-radio-button
					v-for="kind in allowedKinds"
					:key="kind"
					:label="kind"
				>
					{{ text[kind] }}
				</el-radio-button>
			</el-radio-group>
		</el-form-item>
	</el-col>

	<slot name="header-extra" />

	<!-- Mode liên tục (continuous) -->
	<template v-if="scheduleKind === 'continuous'">
		<el-col
			:xs="24"
			:sm="24"
			:md="24"
			:lg="24"
			:xl="24"
			class="mb20"
		>
			<el-form-item
				:label="text.intervalSeconds"
				:prop="intervalProp"
			>
				<el-input-number
					:ref="setInputNumberRef"
					v-model="intervalSeconds"
					:min="intervalMin"
					:max="intervalMax"
					:step="intervalStep"
					:precision="0"
					:disabled="disabled"
					class="w100 interval-number-input"
					controls-position="right"
					@input="onNumberInput"
				/>
			</el-form-item>
		</el-col>
		<el-col
			:xs="24"
			:sm="24"
			:md="24"
			:lg="24"
			:xl="24"
			class="mb20"
		>
			<el-form-item
				:label="text.timeRange"
				:error="scheduleKind === 'continuous' ? timeRangeError : ''"
			>
				<div class="time-range">
					<!-- Thẻ bọc bắt change / Enter trước el-time-picker: gõ sai giờ thì trả về giá trị cũ -->
					<div
						class="time-range__input"
						@change.capture="restoreInvalidTime($event, startTime)"
						@keydown.enter.capture="restoreInvalidTime($event, startTime)"
					>
						<el-time-picker
							v-model="startTime"
							format="HH:mm"
							value-format="HH:mm"
							:disabled="disabled"
							:placeholder="text.startTimePlaceholder"
							class="w100"
							@keydown="onlyNumberKeydown($event, ':')"
						/>
					</div>
					<span class="time-sep">→</span>
					<div
						class="time-range__input"
						@change.capture="restoreInvalidTime($event, endTime)"
						@keydown.enter.capture="restoreInvalidTime($event, endTime)"
					>
						<el-time-picker
							v-model="endTime"
							format="HH:mm"
							value-format="HH:mm"
							:disabled="disabled"
							:placeholder="text.endTimePlaceholder"
							class="w100"
							@keydown="onlyNumberKeydown($event, ':')"
						/>
					</div>
				</div>
			</el-form-item>
		</el-col>
	</template>

	<!-- Mode chu kỳ thuần (interval) -->
	<template v-else-if="scheduleKind === 'interval'">
		<el-col
			:xs="24"
			:sm="24"
			:md="24"
			:lg="24"
			:xl="24"
			class="mb20"
		>
			<el-form-item
				:label="text.intervalSeconds"
				:prop="intervalProp"
			>
				<el-input-number
					:ref="setInputNumberRef"
					v-model="intervalSeconds"
					:min="intervalMin"
					:max="intervalMax"
					:step="intervalStep"
					:precision="0"
					:disabled="disabled"
					class="w100 interval-number-input"
					controls-position="right"
					@input="onNumberInput"
				/>
			</el-form-item>
		</el-col>
	</template>

	<!-- Mode một lần (once): chọn ngày giờ chạy -->
	<template v-else-if="scheduleKind === 'once'">
		<el-col
			:xs="24"
			:sm="12"
			:md="12"
			:lg="12"
			:xl="12"
			class="mb20"
		>
			<el-form-item
				:label="text.onceDate"
				:prop="onceDateProp"
			>
				<el-date-picker
					v-model="onceDate"
					type="datetime"
					:format="onceDateFormat"
					:value-format="onceDateValueFormat"
					:disabled="disabled"
					:placeholder="text.onceDatePlaceholder"
					class="w100"
				/>
			</el-form-item>
		</el-col>
	</template>

	<!-- Mode hàng ngày (daily) hoặc hàng tuần (weekly) -->
	<template v-else>
		<el-col
			v-if="daysOfWeekKinds.includes(scheduleKind)"
			:xs="24"
			:sm="24"
			:md="24"
			:lg="24"
			:xl="24"
			class="mb20"
		>
			<el-form-item
				:label="text.daysOfWeek"
				:prop="daysOfWeekProp"
			>
				<el-checkbox-group v-model="daysOfWeek" :disabled="disabled">
					<el-checkbox-button
						v-for="d in WEEK_DAYS"
						:key="d.value"
						:label="d.value"
					>
						{{ d.label }}
					</el-checkbox-button>
				</el-checkbox-group>
			</el-form-item>
		</el-col>
		<el-col
			:xs="24"
			:sm="12"
			:md="12"
			:lg="12"
			:xl="12"
			class="mb20"
		>
			<el-form-item
				:label="text.time"
				:prop="timeProp"
			>
				<!-- Thẻ bọc bắt change / Enter trước el-time-picker: gõ sai giờ thì trả về giá trị cũ -->
				<div
					class="w100"
					@change.capture="restoreInvalidTime($event, startTime)"
					@keydown.enter.capture="restoreInvalidTime($event, startTime)"
				>
					<el-time-picker
						v-model="startTime"
						format="HH:mm"
						value-format="HH:mm"
						:disabled="disabled"
						:placeholder="text.timePlaceholder"
						class="w100"
						@keydown="onlyNumberKeydown($event, ':')"
					/>
				</div>
			</el-form-item>
		</el-col>
	</template>
</template>

<script setup lang="ts" name="Scheduler">
import { computed, nextTick, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { onlyNumberKeydown, restoreInvalidTime } from '/@/utils/inputNumber';

export type ScheduleKind = 'continuous' | 'daily' | 'weekly' | 'interval' | 'once';
export type DayOfWeek = 'MON' | 'TUE' | 'WED' | 'THU' | 'FRI' | 'SAT' | 'SUN';

/** Nhãn hiển thị của bộ lập lịch; mỗi nơi dùng truyền đè phần cần đổi qua prop `labels`. */
export interface SchedulerLabels {
	/** Nhãn ô chọn kiểu lịch */
	kind: string;
	/** Nhãn từng kiểu lịch */
	continuous: string;
	daily: string;
	weekly: string;
	interval: string;
	once: string;
	/** Nhãn ô chu kỳ (giây) */
	intervalSeconds: string;
	/** Khung giờ của kiểu liên tục */
	timeRange: string;
	startTimePlaceholder: string;
	endTimePlaceholder: string;
	/** Ngày trong tuần */
	daysOfWeek: string;
	/** Giờ chạy của kiểu hàng ngày / hàng tuần */
	time: string;
	timePlaceholder: string;
	/** Ngày giờ chạy của kiểu một lần */
	onceDate: string;
	onceDatePlaceholder: string;
}

export interface Props {
	/** Danh sách kiểu lịch được phép chọn, hiển thị theo đúng thứ tự truyền vào. Mặc định: ['continuous', 'daily'] */
	allowedKinds?: ScheduleKind[];
	/** Các kiểu lịch có hiện ô chọn ngày trong tuần. Mặc định: ['daily', 'weekly'] */
	daysOfWeekKinds?: ScheduleKind[];
	/** Nhãn hiển thị truyền đè lên nhãn mặc định */
	labels?: Partial<SchedulerLabels>;
	/** Giá trị chu kỳ nhỏ nhất (giây) */
	intervalMin?: number;
	/** Giá trị chu kỳ lớn nhất (giây) */
	intervalMax?: number;
	/** Bước nhảy chu kỳ (giây) */
	intervalStep?: number;
	/** Tên prop trên el-form-item của chu kỳ để gắn rule validate của el-form cha */
	intervalProp?: string;
	/** Tên prop trên el-form-item của kiểu lịch / ngày trong tuần / giờ chạy / ngày giờ một lần (rule khai báo ở el-form cha) */
	kindProp?: string;
	daysOfWeekProp?: string;
	timeProp?: string;
	onceDateProp?: string;
	/** Định dạng hiển thị và định dạng giá trị của ô ngày giờ (kiểu một lần) */
	onceDateFormat?: string;
	onceDateValueFormat?: string;
	/** Khóa toàn bộ các trường trong bộ lập lịch */
	disabled?: boolean;
}

const { t: $t } = useI18n();

const props = withDefaults(defineProps<Props>(), {
	allowedKinds: () => ['continuous', 'daily'],
	daysOfWeekKinds: () => ['daily', 'weekly'],
	intervalMin: 1,
	intervalMax: 86400,
	intervalStep: 1,
	intervalProp: 'intervalSeconds',
	onceDateFormat: 'DD/MM/YYYY HH:mm',
	onceDateValueFormat: 'YYYY-MM-DD HH:mm',
	disabled: false,
});

const scheduleKind = defineModel<ScheduleKind>('kind', { default: 'continuous' });
const intervalSeconds = defineModel<number | undefined>('intervalSeconds');
const startTime = defineModel<string>('startTime', { default: '06:00' });
const endTime = defineModel<string>('endTime', { default: '22:00' });
const daysOfWeek = defineModel<DayOfWeek[]>('daysOfWeek', {
	default: () => ['MON', 'TUE', 'WED', 'THU', 'FRI'],
});
const onceDate = defineModel<string | null>('onceDate');

const text = computed<SchedulerLabels>(() => ({
	kind: $t('lz.label.sharedataSubscription.scheduleKind'),
	continuous: $t('lz.label.sharedataSubscription.continuous'),
	daily: $t('lz.label.sharedataSubscription.daily'),
	weekly: $t('lz.label.sharedataSubscription.weekly'),
	interval: $t('lz.label.sharedataSubscription.interval'),
	once: $t('lz.label.base.once'),
	intervalSeconds: $t('lz.entity.sharedataSubscription.intervalSeconds'),
	timeRange: $t('lz.label.sharedataSubscription.timeRange'),
	startTimePlaceholder: $t('lz.label.sharedataSubscription.startTime'),
	endTimePlaceholder: $t('lz.label.sharedataSubscription.endTime'),
	daysOfWeek: $t('lz.label.sharedataSubscription.daysOfWeek'),
	time: $t('lz.label.sharedataSubscription.sendTime'),
	timePlaceholder: $t('lz.label.sharedataSubscription.sendTimePlaceholder'),
	onceDate: $t('lz.label.base.dateTime'),
	onceDatePlaceholder: $t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.dateTime') }),
	...props.labels,
}));

const WEEK_DAYS = computed<{ label: string; value: DayOfWeek }[]>(() => [
	{ label: $t('lz.label.sharedataSubscription.weekdayMon'), value: 'MON' },
	{ label: $t('lz.label.sharedataSubscription.weekdayTue'), value: 'TUE' },
	{ label: $t('lz.label.sharedataSubscription.weekdayWed'), value: 'WED' },
	{ label: $t('lz.label.sharedataSubscription.weekdayThu'), value: 'THU' },
	{ label: $t('lz.label.sharedataSubscription.weekdayFri'), value: 'FRI' },
	{ label: $t('lz.label.sharedataSubscription.weekdaySat'), value: 'SAT' },
	{ label: $t('lz.label.sharedataSubscription.weekdaySun'), value: 'SUN' },
]);

const inputNumberRef = ref<any>();
const setInputNumberRef = (el: unknown) => {
	if (el) {
		inputNumberRef.value = el;
		const inputEl = (el as any)?.$el?.querySelector('input') as HTMLInputElement | null;
		if (inputEl && !(inputEl as any)._hasMaxGuard) {
			(inputEl as any)._hasMaxGuard = true;
			inputEl.addEventListener('input', () => {
				const num = Number(inputEl.value);
				const max = props.intervalMax;
				if (!isNaN(num) && num > max) {
					intervalSeconds.value = max;
					inputEl.value = String(max);
					inputEl.dispatchEvent(new Event('change', { bubbles: true }));
				}
			});
		}
	}
};

/** Chặn ngay khi nhập vượt trần chu kỳ, tự động ép về giá trị tối đa. */
function onNumberInput(val: number | null) {
	const max = props.intervalMax;
	if (val != null && val > max) {
		intervalSeconds.value = max;
		nextTick(() => {
			const inputEl = inputNumberRef.value?.$el?.querySelector('input') as HTMLInputElement | null;
			if (inputEl) {
				inputEl.value = String(max);
				inputEl.dispatchEvent(new Event('change', { bubbles: true }));
			}
		});
	}
}

const isEndBeforeStart = computed(() => {
	if (!startTime.value || !endTime.value) return false;
	return endTime.value <= startTime.value;
});

const isTimeRangeIncomplete = computed(() => {
	return (Boolean(startTime.value) && !endTime.value) || (!startTime.value && Boolean(endTime.value));
});

const timeRangeError = computed<string>(() => {
	if (scheduleKind.value !== 'continuous') return '';
	if (isTimeRangeIncomplete.value) {
		return $t('lz.validation.sharedataSubscription.timeRangeIncomplete');
	}
	if (isEndBeforeStart.value) {
		return $t('lz.validation.sharedataSubscription.timeRangeInvalid');
	}
	return '';
});

defineExpose({
	timeRangeError,
});
</script>

<style scoped lang="scss">
/* Khung giờ: 2 ô giờ chia đều nhau, ngang hàng, mũi tên nằm giữa. */
.time-range {
	display: flex;
	align-items: center;
	width: 100%;
}

.time-range__input {
	flex: 1 1 0;
	min-width: 0;

	:deep(.el-input__wrapper) {
		width: 100%;
	}
}

.time-sep {
	flex: 0 0 auto;
	margin: 0 10px;
	color: var(--el-text-color-secondary);
}

/* Canh giữa value chu kỳ nhưng giữ mũi tên controls ở cuối bên phải */
.interval-number-input {
	:deep(.el-input__inner) {
		text-align: center;
	}
}
</style>
