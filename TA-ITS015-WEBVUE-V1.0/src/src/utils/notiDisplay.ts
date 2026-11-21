import { markRaw } from 'vue';
import { dayjs } from 'element-plus';
import { useDateFormat } from '@vueuse/core';
import { Bell, CircleCheckFilled, Warning, WarningFilled } from '@element-plus/icons-vue';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import type { NotiListOutput } from '/@/api-services/noti';

/**
 * Bộ helper hiển thị dùng chung cho module notification:
 * - severityMeta: icon + màu + nhãn theo mức độ (map theo từ khóa, BE trả chuỗi tự do).
 * - formatDateTime: thời gian theo format chung của hệ thống (useDateFormat + DATE_TIME_FORMAT).
 * - groupByDate: nhóm theo ngày (hôm nay / hôm qua / trước đó) cho danh sách noti.
 */

export type SeverityKind = 'critical' | 'warning' | 'success' | 'info';

export interface SeverityMeta {
	kind: SeverityKind;
	/** Key i18n cho nhãn mức độ */
	labelKey: string;
	/** Màu chủ đạo (CSS var) cho icon/viền */
	color: string;
	/** Nền nhạt cho khối icon */
	bg: string;
	/** type cho el-tag */
	tag: 'primary' | 'success' | 'warning' | 'danger';
	/** Icon component (đã markRaw) */
	icon: any;
}

const SEVERITY_META: Record<SeverityKind, SeverityMeta> = {
	critical: {
		kind: 'critical',
		labelKey: 'lz.label.noti.severity.critical',
		color: 'var(--el-color-danger)',
		bg: 'var(--el-color-danger-light-9)',
		tag: 'danger',
		icon: markRaw(WarningFilled),
	},
	warning: {
		kind: 'warning',
		labelKey: 'lz.label.noti.severity.warning',
		color: 'var(--el-color-warning)',
		bg: 'var(--el-color-warning-light-9)',
		tag: 'warning',
		icon: markRaw(Warning),
	},
	success: {
		kind: 'success',
		labelKey: 'lz.label.noti.severity.info',
		color: 'var(--el-color-success)',
		bg: 'var(--el-color-success-light-9)',
		tag: 'success',
		icon: markRaw(CircleCheckFilled),
	},
	info: {
		kind: 'info',
		labelKey: 'lz.label.noti.severity.info',
		color: 'var(--el-color-primary)',
		bg: 'var(--el-color-primary-light-9)',
		tag: 'primary',
		icon: markRaw(Bell),
	},
};

/** Tra meta theo chuỗi severity tự do của BE (không khớp → info). */
export const severityMeta = (severity?: string | null): SeverityMeta => {
	const value = String(severity ?? '').toLowerCase();
	if (value.includes('crit') || value.includes('fatal') || value.includes('error')) return SEVERITY_META.critical;
	if (value.includes('warn')) return SEVERITY_META.warning;
	if (value.includes('success') || value.includes('ok')) return SEVERITY_META.success;
	return SEVERITY_META.info;
};

/** Thời gian theo format chung của hệ thống: dd/MM/yyyy HH:mm:ss (xem types/formats). */
export const formatDateTime = (time?: Date | string | null): string => {
	if (!time) return '';
	return useDateFormat(new Date(time), DATE_TIME_FORMAT)?.value ?? '';
};

export type NotiDateGroup = 'today' | 'yesterday' | 'earlier';

/** Xếp noti vào nhóm ngày để render tiêu đề nhóm (Hôm nay / Hôm qua / Trước đó). */
export const dateGroupKey = (time?: Date | string | null): NotiDateGroup => {
	if (!time) return 'earlier';
	const d = dayjs(time);
	if (d.isSame(dayjs(), 'day')) return 'today';
	if (d.isSame(dayjs().subtract(1, 'day'), 'day')) return 'yesterday';
	return 'earlier';
};

/** Gom danh sách noti (đã sort desc theo sendTime) thành các nhóm ngày, giữ thứ tự gặp đầu tiên. */
export interface NotiGroup {
	key: NotiDateGroup;
	items: NotiListOutput[];
}

export const groupByDate = (items: NotiListOutput[]): NotiGroup[] => {
	const groups: NotiGroup[] = [];
	const index = new Map<NotiDateGroup, NotiGroup>();
	for (const item of items) {
		const key = dateGroupKey(item.sendTime);
		if (!index.has(key)) {
			const group = { key, items: [item] };
			index.set(key, group);
			groups.push(group);
		} else {
			index.get(key)!.items.push(item);
		}
	}
	return groups;
};
