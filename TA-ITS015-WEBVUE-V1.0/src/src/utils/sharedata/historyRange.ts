import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums';

const pad = (value: number) => String(value).padStart(2, '0');

const ISO_DATE_TIME = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d+)?)?(Z|[+-]\d{2}:\d{2})?$/;

/* Ngày giờ ISO của JSON (BeforeJson / AfterJson) → DD/MM/YYYY HH:mm:ss; chuỗi khác trả null để giữ nguyên văn. */
export function formatIsoDateTimeText(value: string): string | null {
	if (!ISO_DATE_TIME.test(value)) return null;
	const date = new Date(value);
	return Number.isNaN(date.getTime()) ? null : useDateFormat(date, DateTimeFormatEnum.DateTimeDefault).value;
}

export const formatHistoryDate = (date: Date): string =>
	`${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;

export function defaultHistoryRange(now = new Date()): [string, string] {
	const start = new Date(now.getTime());
	start.setHours(0, 0, 0, 0);
	return [formatHistoryDate(start), formatHistoryDate(now)];
}

/* Parse the date picker's local wall time explicitly, including calendar validation. */
function parseHistoryDate(value: unknown): Date | undefined {
	if (typeof value !== 'string') return undefined;
	const match = /^(\d{4})-(\d{2})-(\d{2}) (\d{2}):(\d{2}):(\d{2})$/.exec(value);
	if (!match) return undefined;
	const [, year, month, day, hour, minute, second] = match.map(Number);
	const date = new Date(year, month - 1, day, hour, minute, second);
	return formatHistoryDate(date) === value ? date : undefined;
}

type HistoryRangeResult = { errorKey: string; fromDate?: never; toDate?: never }
	| { errorKey?: never; fromDate: Date; toDate: Date };

export function validateHistoryRange(range: unknown, now = new Date()): HistoryRangeResult {
	const fromDate = Array.isArray(range) ? parseHistoryDate(range[0]) : undefined;
	const toDate = Array.isArray(range) ? parseHistoryDate(range[1]) : undefined;
	if (!fromDate || !toDate) return { errorKey: 'lz.validation.sharedataHistory.invalidRange' };
	if (fromDate > toDate) return { errorKey: 'lz.validation.sharedataHistory.startAfterEnd' };
	if (fromDate > now || toDate > now) return { errorKey: 'lz.validation.sharedataHistory.futureTime' };
	return { fromDate, toDate };
}
