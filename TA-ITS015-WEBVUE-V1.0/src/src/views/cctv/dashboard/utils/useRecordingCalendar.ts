import { computed, ref, shallowRef } from 'vue';
import { getRecordingPeriodRange } from './recordingPeriodUtils';

type Recording = { startTimeMs: number; durationMs?: number };
type Range = ReturnType<typeof getRecordingPeriodRange>;
type MonthData<T> = Range & { periods: T[] };
const monthKey = (date: Date) => `${date.getFullYear()}-${date.getMonth()}`;

/** Calendar metadata is separate from the selected day's playback timeline. */
export const useRecordingCalendar = <T extends Recording>(load: (range: Range) => Promise<T[]>, onError: () => void) => {
	const displayedMonth = ref(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
	const months = shallowRef<Record<string, MonthData<T>>>({});
	const loading = ref(false);
	const failed = ref(false);
	const pending = new Map<string, Promise<MonthData<T>>>();
	let generation = 0;
	let viewVersion = 0;

	const loadMonth = (day: Date): Promise<MonthData<T>> => {
		const key = monthKey(day);
		if (months.value[key]) return Promise.resolve(months.value[key]);
		const existing = pending.get(key);
		if (existing) return existing;
		const epoch = generation;
		const range = getRecordingPeriodRange(day);
		const request = load(range).then((periods) => {
			const data = { ...range, periods };
			if (epoch === generation) months.value = { ...months.value, [key]: data };
			return data;
		}).finally(() => {
			if (epoch === generation) pending.delete(key);
		});
		pending.set(key, request);
		return request;
	};

	const isDateAvailable = (day: Date) => {
		const data = months.value[monthKey(day)];
		if (!data) return false;
		const start = new Date(day.getFullYear(), day.getMonth(), day.getDate()).getTime();
		const end = new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1).getTime();
		if (start > Date.now()) return false;
		return data.periods.some((period) => {
			const from = Number(period.startTimeMs);
			const duration = Number(period.durationMs);
			const to = Number.isFinite(duration) && duration > 0 ? Math.min(from + duration, data.toDate.getTime()) : data.toDate.getTime();
			return Number.isFinite(from) && to > from && from < end && to > start;
		});
	};

	const disabledDates = computed(() => {
		const month = displayedMonth.value;
		const dates: Date[] = [];
		const count = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
		for (let day = 1; day <= count; day++) {
			const date = new Date(month.getFullYear(), month.getMonth(), day);
			if (!isDateAvailable(date)) dates.push(date);
		}
		return dates;
	});

	const showMonth = async (day: Date) => {
		const version = ++viewVersion;
		displayedMonth.value = new Date(day.getFullYear(), day.getMonth(), 1);
		loading.value = true;
		failed.value = false;
		try {
			await loadMonth(displayedMonth.value);
		} catch {
			if (version === viewVersion) {
				failed.value = true;
				onError();
			}
		} finally {
			if (version === viewVersion) loading.value = false;
		}
	};

	const reset = () => {
		generation++;
		viewVersion++;
		months.value = {};
		pending.clear();
		displayedMonth.value = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
		loading.value = false;
		failed.value = false;
	};

	return {
		loading, failed, disabledDates, displayedMonth, loadMonth, showMonth, isDateAvailable, reset,
		// PrimeVue emits month-change with a 1-based month and year-change with a 0-based month.
		onMonthChange: ({ year, month }: { year: number; month: number }) => showMonth(new Date(year, month - 1, 1)),
		onYearChange: ({ year, month }: { year: number; month: number }) => showMonth(new Date(year, month, 1)),
		onShow: (picker?: { currentYear?: number; currentMonth?: number }) => showMonth(new Date(
			picker?.currentYear ?? displayedMonth.value.getFullYear(), picker?.currentMonth ?? displayedMonth.value.getMonth(), 1
		)),
	};
};
