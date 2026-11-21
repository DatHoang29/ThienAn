import type { SqlFieldItem } from '/@/api-services/shareData/models';

export type FieldCompareState = 'same' | 'new' | 'missing' | 'diff';

export interface FieldDiffDetail {
	prop: 'type' | 'dbType' | 'required';
	aspect: 'type' | 'dbType' | 'required';
	commonValue: string | boolean;
	partnerValue: string | boolean;
}

export interface FieldCompareRow {
	key: string;
	label: string;
	state: FieldCompareState;
	common?: SqlFieldItem;
	partner?: SqlFieldItem;
	diffs: FieldDiffDetail[];
	details: FieldDiffDetail[];
}

export interface FieldCompareCounts {
	same: number;
	added: number;
	missing: number;
	diff: number;
}

export interface FieldCompareResult {
	rows: FieldCompareRow[];
	counts: FieldCompareCounts;
	byKey: Record<string, FieldCompareRow>;
	sameCount: number;
	newCount: number;
	missingCount: number;
	diffCount: number;
}

const normalizeText = (v?: string | null): string => (v ?? '').trim().toLowerCase();

/**
 * So sánh danh sách trường giữa Mẫu chung và SQL riêng của đối tác:
 * - 'same'    (=): có ở cả 2 bên, khớp type / dbType / required
 * - 'new'     (+): chỉ có ở đối tác (mẫu chung không có)
 * - 'missing' (-): có ở mẫu chung nhưng thiếu ở đối tác
 * - 'diff'    (≠): có ở cả 2 bên nhưng khác type, dbType hoặc required
 */
export function compareFields(
	commonFields?: SqlFieldItem[] | null,
	partnerFields?: SqlFieldItem[] | null
): FieldCompareResult {
	const commonActive = (commonFields ?? []).filter((f) => f?.key && !f.lost);
	const partnerActive = (partnerFields ?? []).filter((f) => f?.key && !f.lost);

	const commonMap = new Map<string, SqlFieldItem>();
	for (const f of commonActive) {
		if (f.key) commonMap.set(f.key, f);
	}

	const partnerMap = new Map<string, SqlFieldItem>();
	for (const f of partnerActive) {
		if (f.key) partnerMap.set(f.key, f);
	}

	const allKeys: string[] = [];
	const seen = new Set<string>();
	for (const f of commonActive) {
		if (f.key && !seen.has(f.key)) {
			seen.add(f.key);
			allKeys.push(f.key);
		}
	}
	for (const f of partnerActive) {
		if (f.key && !seen.has(f.key)) {
			seen.add(f.key);
			allKeys.push(f.key);
		}
	}

	const rows: FieldCompareRow[] = [];
	const byKey: Record<string, FieldCompareRow> = {};
	const counts: FieldCompareCounts = { same: 0, added: 0, missing: 0, diff: 0 };

	for (const key of allKeys) {
		const c = commonMap.get(key);
		const p = partnerMap.get(key);
		let state: FieldCompareState = 'same';
		const diffs: FieldDiffDetail[] = [];

		if (c && !p) {
			state = 'missing';
			counts.missing++;
		} else if (!c && p) {
			state = 'new';
			counts.added++;
		} else if (c && p) {
			if (normalizeText(c.type) !== normalizeText(p.type)) {
				diffs.push({ prop: 'type', aspect: 'type', commonValue: c.type ?? '', partnerValue: p.type ?? '' });
			}
			if (normalizeText(c.dbType) !== normalizeText(p.dbType)) {
				diffs.push({ prop: 'dbType', aspect: 'dbType', commonValue: c.dbType ?? '', partnerValue: p.dbType ?? '' });
			}
			if (Boolean(c.required) !== Boolean(p.required)) {
				diffs.push({ prop: 'required', aspect: 'required', commonValue: Boolean(c.required), partnerValue: Boolean(p.required) });
			}
			if (diffs.length > 0) {
				state = 'diff';
				counts.diff++;
			} else {
				state = 'same';
				counts.same++;
			}
		}

		const row: FieldCompareRow = {
			key,
			label: p?.label || c?.label || key,
			state,
			common: c,
			partner: p,
			diffs,
			details: diffs,
		};
		rows.push(row);
		byKey[key] = row;
	}

	return {
		rows,
		counts,
		byKey,
		sameCount: counts.same,
		newCount: counts.added,
		missingCount: counts.missing,
		diffCount: counts.diff,
	};
}
