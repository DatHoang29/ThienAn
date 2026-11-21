import { i18n } from '/@/i18n';
import type { DayOfWeek, ScheduleKind } from '/@/components/scheduler/index.vue';

const t = (k: string) => i18n.global.t(k);

// Danh mục dùng chung cho màn Lịch hẹn giờ (VwSchedule).
// Khớp BE: VwSchedule.ScheduleType (int), Weekdays (CSV "1,2,3"), Time ("HH:mm"), Action (string).

/** Loại lịch — giá trị số theo BE; `kind` là kiểu lịch tương ứng của component Scheduler. */
export const SCHEDULE_TYPES: ReadonlyArray<{ value: number; kind: ScheduleKind; label: string; tag: 'primary' | 'success' | 'warning' | 'info' }> = [
	{ value: 1, kind: 'daily', get label() { return t('lz.label.videoWall.daily'); }, tag: 'success' },
	{ value: 2, kind: 'weekly', get label() { return t('lz.label.videoWall.weekly'); }, tag: 'primary' },
	{ value: 3, kind: 'once', get label() { return t('lz.label.videoWall.once'); }, tag: 'warning' },
];

export const SCHEDULE_TYPE = { Daily: 1, Weekly: 2, Once: 3 } as const;

/** Toàn bộ hành động — chỉ dùng để hiển thị nhãn (kể cả bản ghi cũ đang dùng hành động bị ẩn). */
export const SCHEDULE_ACTION_LABELS: ReadonlyArray<{ value: string; label: string }> = [
	{ value: 'activate_scene', get label() { return t('lz.label.videoWall.actionActivateScene'); } },
	{ value: 'close_screens', get label() { return t('lz.label.videoWall.actionCloseScreens'); } },
	{ value: 'open_screens', get label() { return t('lz.label.videoWall.actionOpenScreens'); } },
];

/**
 * Hành động cho chọn khi đến giờ.
 * TẠM ẨN close_screens / open_screens — BE chưa xử lý (VwScheduleConst.Action.Supported).
 */
const ENABLED_ACTIONS = ['activate_scene'];
export const SCHEDULE_ACTIONS = SCHEDULE_ACTION_LABELS.filter((a) => ENABLED_ACTIONS.includes(a.value));

/** Ngày trong tuần theo Date.getDay() (0 = Chủ nhật). */
export const WEEKDAY_OPTIONS: ReadonlyArray<{ value: number; label: string }> = [
	{ value: 1, get label() { return t('lz.label.videoWall.weekday1'); } },
	{ value: 2, get label() { return t('lz.label.videoWall.weekday2'); } },
	{ value: 3, get label() { return t('lz.label.videoWall.weekday3'); } },
	{ value: 4, get label() { return t('lz.label.videoWall.weekday4'); } },
	{ value: 5, get label() { return t('lz.label.videoWall.weekday5'); } },
	{ value: 6, get label() { return t('lz.label.videoWall.weekday6'); } },
	{ value: 0, get label() { return t('lz.label.videoWall.weekday0'); } },
];

/** CSV "1,2,3" → [1,2,3]. */
export const parseWeekdays = (csv?: string | null): number[] =>
	(csv ?? '')
		.split(',')
		.map((d) => d.trim())
		.filter((d) => d !== '')
		.map(Number)
		.filter((d) => Number.isInteger(d) && d >= 0 && d <= 6);

/** [1,2,3] → CSV "1,2,3". */
export const joinWeekdays = (days?: number[] | null): string => (days ?? []).join(',');

/** Mã ngày của component Scheduler, vị trí theo Date.getDay() (0 = Chủ nhật). */
const DAY_OF_WEEK: ReadonlyArray<DayOfWeek> = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

/** CSV "1,2,3" → ['MON','TUE','WED'] (giá trị v-model của Scheduler). */
export const weekdaysToDaysOfWeek = (csv?: string | null): DayOfWeek[] => parseWeekdays(csv).map((d) => DAY_OF_WEEK[d]);

/** ['MON','TUE','WED'] → CSV "1,2,3". */
export const daysOfWeekToWeekdays = (days?: DayOfWeek[] | null): string => joinWeekdays((days ?? []).map((d) => DAY_OF_WEEK.indexOf(d)));
