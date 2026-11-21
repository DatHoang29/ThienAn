<template>
	<div class="receive-preview">
		<!-- Dữ liệu nhận về: đi NGƯỢC tab gửi đi — payload đối tác gửi đến đi qua bộ khung để ra bản ghi nội bộ. -->
		<div class="mp-pane-head">
			<span class="mp-pane-title">{{ $t('lz.label.sharedataMapping.receivePayloadLabel') }}</span>
			<el-button type="primary" size="small" icon="ele-MagicStick" :disabled="!state.payload.trim()" @click="parsePayload">
				{{ $t('lz.button.sharedataMapping.parseReceivedData') }}
			</el-button>
			<el-button size="small" icon="ele-DocumentCopy" :disabled="!props.partnerJson.trim()" @click="useTemplateJson">
				{{ $t('lz.button.sharedataMapping.useTemplateJson') }}
			</el-button>
		</div>
		<el-input v-model="state.payload" type="textarea" :rows="10" resize="vertical" class="mp-json" spellcheck="false" />
		<div v-if="state.error" class="mp-err">{{ state.error }}</div>
		<div v-else class="rs-note">{{ $t('lz.label.sharedataMapping.receivePayloadHint') }}</div>

		<!-- Thông tin gói tin: đối chiếu các khoá $meta của bộ khung với giá trị trong payload. -->
		<template v-if="state.parsed">
			<div class="mp-pane-head mt10">
				<span class="mp-pane-title">{{ $t('lz.label.sharedataMapping.receiveMetaTitle') }}</span>
			</div>
			<el-table :data="state.meta" size="small" border class="mt5">
				<el-table-column prop="path" :label="$t('lz.label.sharedataMapping.partnerHeader')" min-width="160">
					<template #default="{ row }"><span class="mp-preview-key">{{ row.path }}</span></template>
				</el-table-column>
				<el-table-column prop="key" :label="$t('lz.label.sharedataMapping.systemHeader')" width="140" />
				<el-table-column :label="$t('lz.label.sharedataMapping.receiveActualValue')" min-width="180">
					<template #default="{ row }">
						<span class="mp-preview-raw" :class="{ 'mp-preview-raw-null': row.actual === undefined }">{{ row.actual === undefined ? '—' : displayValue(row.actual) }}</span>
					</template>
				</el-table-column>
				<el-table-column :label="$t('lz.label.sharedataMapping.receiveExpectedValue')" min-width="180">
					<template #default="{ row }">
						<span class="mp-preview-raw">{{ row.expected ?? '' }}</span>
					</template>
				</el-table-column>
				<el-table-column width="60" align="center">
					<template #default="{ row }">
						<el-icon v-if="row.ok" class="rp-ok"><ele-CircleCheckFilled /></el-icon>
						<el-icon v-else class="rp-warn"><ele-WarningFilled /></el-icon>
					</template>
				</el-table-column>
			</el-table>
			<div v-if="!state.meta.length" class="rs-note mt5">{{ $t('lz.label.sharedataMapping.receiveNoMetaInFramework') }}</div>
		</template>

		<div class="mp-pane-head mt10">
			<span class="mp-pane-title">{{ $t('lz.label.sharedataMapping.receivedRecords') }}</span>
			<span class="rs-note">{{ state.parsed ? $t('lz.label.sharedataMapping.receiveRecordCount', { count: state.rows.length }) : $t('lz.label.sharedataMapping.receiveRulesHint') }}</span>
		</div>
		<!-- Chú thích màu ô: rê chuột lên ô có màu để xem giá trị đã được xử lý thế nào. -->
		<div v-if="state.rows.length" class="rp-legend">
			<span class="rp-cell-info">{{ $t('lz.label.sharedataMapping.receiveLegendCodeSet') }}</span>
			<span class="rp-cell-warn">{{ $t('lz.label.sharedataMapping.receiveLegendDefault') }}</span>
			<span class="rp-cell-error">{{ $t('lz.label.sharedataMapping.receiveLegendError') }}</span>
		</div>
		<el-table :data="state.rows" size="small" border max-height="260" class="mt5">
			<el-table-column type="index" label="#" width="50" fixed="left" />
			<el-table-column v-for="f in usedFields" :key="f.aliasFieldKey!" min-width="165">
				<template #header>
					<el-tooltip :content="[f.name, f.type].filter((x) => (x ?? '') !== '').join(' · ')" placement="top">
						<span>
							<span class="mp-preview-key">{{ f.aliasFieldKey }}</span>
							<span v-if="f.isRequired === true" class="mp-preview-req">*</span>
						</span>
					</el-tooltip>
				</template>
				<template #default="{ row, $index }">
					<el-tooltip :disabled="!notesOf($index, f.aliasFieldKey!).length" placement="top">
						<template #content>
							<div v-for="(n, i) in notesOf($index, f.aliasFieldKey!)" :key="i">{{ n.text }}</div>
						</template>
						<span class="mp-preview-raw" :class="[cellClass($index, f.aliasFieldKey!), { 'mp-preview-raw-null': isEmpty(row[f.aliasFieldKey!]) }]">{{
							displayValue(row[f.aliasFieldKey!])
						}}</span>
					</el-tooltip>
				</template>
			</el-table-column>
		</el-table>
		<div v-if="state.parsed && !state.rows.length" class="rs-note mt5">{{ $t('lz.label.sharedataMapping.receiveNoRecords') }}</div>
		<div v-else-if="!state.rows.length" class="rs-note mt5">{{ $t('lz.label.sharedataMapping.noReceiveDataYet') }}</div>
	</div>
</template>

<script lang="ts" setup name="eshReceivePreview">
import { computed, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ShareDataCodeSetOutput } from '/@/api-services/shareData';
import { PacketFieldMeta } from '/@/utils/sharedata/packetFields';
const { t: $t } = useI18n();

interface Props {
	/** Bộ khung đầu ra (TargetShapeJson) — đúng tài liệu service đọc, không đi qua cấu hình trên cây. */
	shapeJson: string;
	/** JSON mẫu của đối tác ở tab Ánh xạ — dùng điền sẵn payload. */
	partnerJson: string;
	packetFields: PacketFieldMeta[];
	/** Bộ mã đã nạp kèm bảng giá trị — dùng quy đổi ngược partnerValue -> sourceValue. */
	codeSetList: ShareDataCodeSetOutput[];
}

const props = defineProps<Props>();

/** Một khoá $meta của bộ khung đối chiếu với payload. */
interface ReceiveMeta {
	path: string;
	key: string;
	actual: any;
	expected?: string;
	ok: boolean;
}

/**
 * Ghi chú gắn vào một ô của bảng bản ghi, hiện bằng màu ô + tooltip:
 *  - info : giá trị đã quy đổi qua bộ mã.
 *  - warn : giá trị lấy từ mặc định (của lá hoặc của bộ mã).
 *  - error: không đọc được / không quy đổi được / bắt buộc mà rỗng.
 */
interface CellNote {
	level: 'info' | 'warn' | 'error';
	text: string;
}

type ReceiveRow = Record<string, any>;
type ReceiveNotes = Record<string, CellNote[]>;

/**
 * Mảng nhân bản ghi tìm thấy khi đi bộ khung: phần tử khuôn và các phần tử payload ứng với nó.
 * Chiều gửi đặt hằng số trước/sau khuôn rồi chèn N bản ghi vào giữa, nên chiều nhận cắt bỏ
 * đúng số phần tử hằng ở hai đầu, phần còn lại mỗi phần tử là một bản ghi.
 */
interface RepeatArray {
	itemShape: any;
	itemPath: string;
	items: any[];
}

const MARK_FIELD = '$field';
const MARK_EXTEND = '$extend';
const MARK_META = '$meta';
const MARK_VALUE = '$value';
const LEVEL_RANK = { info: 1, warn: 2, error: 3 } as const;

const state = reactive({
	payload: '',
	error: '',
	/** Đã phân tích payload ít nhất một lần với bộ khung hiện tại. */
	parsed: false,
	meta: [] as ReceiveMeta[],
	/** Bản ghi tách ra, khoá tra là aliasFieldKey của gói tin. */
	rows: [] as ReceiveRow[],
	rowNotes: [] as ReceiveNotes[],
});

const hasOwn = (node: any, key: string) => Object.prototype.hasOwnProperty.call(node, key);
const isPlainObject = (v: any) => v !== null && typeof v === 'object' && !Array.isArray(v);
const isDirective = (node: any) => isPlainObject(node) && Object.keys(node).some((k) => k.startsWith('$'));
const isEmpty = (v: any) => v === null || v === undefined || (typeof v === 'string' && v.trim() === '');

/** Nhánh bộ khung có khoá $field nào không — cùng luật nhận diện mảng nhân bản ghi của chiều gửi. */
const hasFieldBinding = (node: any): boolean => {
	if (Array.isArray(node)) return node.some(hasFieldBinding);
	if (!isPlainObject(node)) return false;
	if (hasOwn(node, MARK_FIELD)) return true;
	if (isDirective(node)) return false;
	return Object.values(node).some(hasFieldBinding);
};

/** Trường gói tin có mặt trong bộ khung — mỗi trường là một cột của bảng bản ghi. */
const usedFields = computed(() => {
	const used = new Set<string>();
	const walk = (node: any) => {
		if (Array.isArray(node)) return node.forEach(walk);
		if (!isPlainObject(node)) return;
		if (hasOwn(node, MARK_FIELD)) {
			used.add(String(node[MARK_FIELD] ?? ''));
			return;
		}
		Object.values(node).forEach(walk);
	};
	try {
		if (props.shapeJson.trim()) walk(JSON.parse(props.shapeJson));
	} catch {
		return [];
	}
	return props.packetFields.filter((f) => f.aliasFieldKey && used.has(f.aliasFieldKey));
});

const fieldMeta = (fieldKey: string) => props.packetFields.find((f) => f.aliasFieldKey?.toLowerCase() === fieldKey.toLowerCase());

const notesOf = (rowIndex: number, fieldKey: string) => state.rowNotes[rowIndex]?.[fieldKey] ?? [];

/** Màu ô theo ghi chú nặng nhất. */
const cellClass = (rowIndex: number, fieldKey: string) => {
	const notes = notesOf(rowIndex, fieldKey);
	if (!notes.length) return '';
	const top = notes.reduce((a, b) => (LEVEL_RANK[b.level] > LEVEL_RANK[a.level] ? b : a));
	return `rp-cell-${top.level}`;
};

/**
 * Payload còn trống thì điền sẵn JSON mẫu, để người dùng bấm thử được ngay.
 * flush 'sync': mở lại đúng hồ sơ cũ thì JSON mẫu đi '' → giá trị cũ ngay trong một lượt,
 * watcher mặc định gộp lại thành "không đổi" và bỏ lỡ lần điền.
 */
watch(
	() => props.partnerJson,
	(json) => {
		if (!state.payload.trim()) state.payload = json ?? '';
	},
	{ immediate: true, flush: 'sync' }
);

/** Bộ khung, trường gói tin hay bộ mã đổi thì kết quả cũ không còn đúng; giữ payload để khỏi dán lại. */
watch(() => [props.shapeJson, props.packetFields, props.codeSetList], () => clearResult());

const useTemplateJson = () => {
	state.payload = props.partnerJson;
	clearResult();
};

const displayValue = (value: any) => {
	if (value === null || value === undefined) return 'NULL';
	if (typeof value === 'object') return JSON.stringify(value);
	return String(value);
};

/* ---- Thông tin gói tin ($meta) ---- */

/**
 * Đi bộ khung song song với payload, lấy giá trị tại các khoá $meta.
 * Mảng đi theo chỉ số của bộ khung; khoá $field bỏ qua — phần tách bản ghi xử lý riêng.
 */
const collectMeta = (shape: any, payload: any, path: string, out: ReceiveMeta[]) => {
	if (Array.isArray(shape)) {
		shape.forEach((item, i) => collectMeta(item, Array.isArray(payload) ? payload[i] : undefined, `${path}[${i}]`, out));
		return;
	}
	if (!isPlainObject(shape)) return;

	if (hasOwn(shape, MARK_META)) {
		const expected = shape[MARK_VALUE] == null ? undefined : String(shape[MARK_VALUE]);
		const ok = payload !== undefined && (expected === undefined || String(payload ?? '').trim() === expected);
		out.push({ path, key: String(shape[MARK_META] ?? ''), actual: payload, expected, ok });
		return;
	}
	if (hasOwn(shape, MARK_FIELD)) return;

	for (const key of Object.keys(shape)) {
		collectMeta(shape[key], isPlainObject(payload) ? payload[key] : undefined, path ? `${path}.${key}` : key, out);
	}
};

/* ---- Quy đổi giá trị một lá ----
 *
 * Đảo ngược thứ tự chiều gửi (nguồn -> bộ mã -> mặc định -> ép kiểu):
 *   giá trị đối tác -> đọc theo kiểu/định dạng phía đối tác (bỏ qua nếu có bộ mã)
 *                   -> bộ mã ngược partnerValue -> sourceValue
 *                   -> rỗng thì lấy mặc định phía nội bộ
 *                   -> ép về kiểu của trường gói tin.
 */

const DATE_TOKENS: Record<string, string> = { yyyy: '\\d{4}', fff: '\\d{3}', MM: '\\d{2}', dd: '\\d{2}', HH: '\\d{2}', mm: '\\d{2}', ss: '\\d{2}' };
const GUID_PATTERN = /^[{(]?[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}[)}]?$/i;

/** Đọc ngày theo định dạng khai ở lá (yyyy, MM, dd, HH, mm, ss, fff); không khai thì đọc như ngày chuẩn. */
const parseDate = (text: string, format: string): Date | null => {
	if (!format || /^[oO]$/.test(format)) {
		const ms = Date.parse(text);
		return Number.isNaN(ms) ? null : new Date(ms);
	}
	const order: string[] = [];
	const pattern = format
		.split(/(yyyy|fff|MM|dd|HH|mm|ss)/)
		.map((part) => {
			if (!DATE_TOKENS[part]) return part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
			order.push(part);
			return `(${DATE_TOKENS[part]})`;
		})
		.join('');
	const match = new RegExp(`^${pattern}$`).exec(text);
	if (!match) return null;

	const p: Record<string, number> = {};
	order.forEach((token, i) => (p[token] = Number(match[i + 1])));
	const month = (p.MM ?? 1) - 1;
	const day = p.dd ?? 1;
	const date = new Date(p.yyyy ?? 1970, month, day, p.HH ?? 0, p.mm ?? 0, p.ss ?? 0, p.fff ?? 0);
	// Ngày tràn (31/02 thành 03/03) thì coi là sai định dạng.
	return date.getMonth() === month && date.getDate() === day ? date : null;
};

/** Ngày lưu nội bộ, hiển thị theo giờ máy để khỏi lệch múi giờ khi so với payload. */
const formatLocalDate = (d: Date) => {
	const pad = (n: number, len = 2) => String(n).padStart(len, '0');
	const ms = d.getMilliseconds() ? `.${pad(d.getMilliseconds(), 3)}` : '';
	return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}${ms}`;
};

/** Đọc một giá trị theo kiểu; đọc không được thì giữ nguyên và ghi chú lỗi vào ô, không ném lỗi. */
const parseTyped = (value: any, type: string | null | undefined, dateFormat: string, numberFormat: string, notes: CellNote[]): any => {
	const t = (type ?? '').trim().toLowerCase();
	if (value === null || value === undefined || t === '') return value;

	const fail = () => {
		notes.push({ level: 'error', text: $t('lz.label.sharedataMapping.receiveCellParseFailed', { value: displayValue(value), type: type ?? '' }) });
		return value;
	};
	const toNumber = () => (typeof value === 'number' ? value : Number(String(value).trim().replace(/,/g, '')));

	if (['decimal', 'number', 'double', 'float'].includes(t)) {
		const n = toNumber();
		return Number.isFinite(n) ? n : fail();
	}
	if (['int', 'integer', 'long', 'bigint'].includes(t)) {
		const n = toNumber();
		return Number.isInteger(n) ? n : fail();
	}
	if (['bool', 'boolean'].includes(t)) {
		if (typeof value === 'boolean') return value;
		const v = String(value).trim().toLowerCase();
		if (v === 'true' || v === '1') return true;
		if (v === 'false' || v === '0') return false;
		return fail();
	}
	if (['date', 'datetime', 'time'].includes(t)) {
		const d = parseDate(String(value).trim(), dateFormat);
		return d ? formatLocalDate(d) : fail();
	}
	if (t === 'guid' || t === 'uuid') {
		return GUID_PATTERN.test(String(value).trim()) ? String(value).trim() : fail();
	}
	if (t === 'string') return typeof value === 'object' ? JSON.stringify(value) : String(value);
	return value;
};

/**
 * Bộ mã ngược: khớp partnerValue (bỏ qua hoa thường, khoảng trắng thừa) thì lấy sourceValue;
 * rỗng hoặc không khớp thì lấy mặc định chiều nhận của bộ mã. Form bộ mã đã chặn trùng
 * partnerValue nên mỗi giá trị khớp tối đa một dòng.
 */
const unmapCode = (code: string, value: any, notes: CellNote[]) => {
	const cs = props.codeSetList.find((c) => c.code?.toLowerCase() === code.toLowerCase());
	if (!cs) {
		notes.push({ level: 'error', text: $t('lz.label.sharedataMapping.receiveCellCodeSetNotFound', { code }) });
		return value;
	}
	const fallback = (cs.defaultSourceValue ?? '').trim();
	if (isEmpty(value)) {
		if (fallback === '') return null;
		notes.push({ level: 'warn', text: $t('lz.label.sharedataMapping.receiveCellCodeSetEmptyDefault', { code, to: fallback }) });
		return fallback;
	}

	const from = String(value).trim();
	const hit = (cs.values ?? []).find((v) => (v.partnerValue ?? '').trim().toLowerCase() === from.toLowerCase());
	if (hit) {
		const to = (hit.sourceValue ?? '').trim();
		notes.push({ level: 'info', text: $t('lz.label.sharedataMapping.receiveCellCodeSet', { code, from, to }) });
		return to;
	}

	if (fallback !== '') {
		notes.push({ level: 'warn', text: $t('lz.label.sharedataMapping.receiveCellCodeSetDefault', { code, from, to: fallback }) });
		return fallback;
	}
	notes.push({ level: 'error', text: $t('lz.label.sharedataMapping.receiveCellCodeSetKeep', { code, from }) });
	return value;
};

/** Giá trị nội bộ của một lá $field, kèm ghi chú cho ô. */
const convertIncoming = (fieldKey: string, raw: any, extend: any, path: string) => {
	const notes: CellNote[] = [];
	const fieldType = fieldMeta(fieldKey)?.type;
	if (raw === undefined) notes.push({ level: 'warn', text: $t('lz.label.sharedataMapping.receiveCellMissingKey', { path }) });

	let value: any = isEmpty(raw) ? null : typeof raw === 'string' ? raw.trim() : raw;

	const codeSet = String(extend.codeSet ?? '').trim();
	if (codeSet) value = unmapCode(codeSet, value, notes);
	else if (value !== null) {
		value = parseTyped(value, String(extend.targetType ?? '').trim() || fieldType, String(extend.dateFormat ?? '').trim(), String(extend.numberFormat ?? '').trim(), notes);
	}

	const fallback = String(extend.defaultSourceValue ?? '').trim();
	if (isEmpty(value) && fallback !== '') {
		value = fallback;
		notes.push({ level: 'warn', text: $t('lz.label.sharedataMapping.receiveCellDefault', { value: fallback }) });
	}

	if (!isEmpty(value)) value = parseTyped(value, fieldType, '', '', notes);
	// Cùng một lỗi có thể ghi hai lần (đọc theo kiểu đối tác rồi ép kiểu trường): gộp lại.
	const unique = notes.filter((n, i) => notes.findIndex((m) => m.text === n.text) === i);
	return { value: isEmpty(value) ? null : value, notes: unique };
};

/* ---- Tách bản ghi ---- */

/** Ghi giá trị vào bản ghi; cùng trường xuất hiện nhiều chỗ thì giữ giá trị khác rỗng đầu tiên. */
const assignField = (row: ReceiveRow, rowNotes: ReceiveNotes, fieldKey: string, value: any, notes: CellNote[]) => {
	if (!isEmpty(row[fieldKey])) return;
	row[fieldKey] = value;
	rowNotes[fieldKey] = notes;
};

/**
 * Đi bộ khung song song với payload, đổ giá trị $field vào `row`.
 * repeats = null khi đang đọc bên trong một bản ghi: mảng lồng chỉ đọc phần tử ở vị trí khuôn.
 */
const readNode = (shape: any, payload: any, path: string, row: ReceiveRow, rowNotes: ReceiveNotes, repeats: RepeatArray[] | null) => {
	if (Array.isArray(shape)) {
		const arr: any[] = Array.isArray(payload) ? payload : [];
		const bindIndex = shape.findIndex(hasFieldBinding);

		// Mảng nhân bản ghi ngoài cùng: cắt hằng số hai đầu, phần giữa mỗi phần tử là một bản ghi.
		if (repeats && bindIndex >= 0) {
			const after = shape.length - bindIndex - 1;
			for (let i = 0; i < bindIndex; i++) readNode(shape[i], arr[i], `${path}[${i}]`, row, rowNotes, repeats);
			for (let j = 0; j < after; j++) {
				const at = arr.length - after + j;
				readNode(shape[bindIndex + 1 + j], at >= bindIndex ? arr[at] : undefined, `${path}[${bindIndex + 1 + j}]`, row, rowNotes, repeats);
			}
			repeats.push({ itemShape: shape[bindIndex], itemPath: `${path}[${bindIndex}]`, items: arr.slice(bindIndex, Math.max(bindIndex, arr.length - after)) });
			return;
		}

		// Mảng hằng, hoặc mảng lồng trong một bản ghi: chiều gửi chỉ dựng đúng các phần tử của khuôn,
		// nên đọc theo vị trí; phần tử thừa bỏ qua.
		shape.forEach((item, i) => readNode(item, arr[i], `${path}[${i}]`, row, rowNotes, repeats));
		return;
	}

	if (!isPlainObject(shape)) return; // Hằng số trong bộ khung: không vào bản ghi.

	if (hasOwn(shape, MARK_FIELD)) {
		const fieldKey = String(shape[MARK_FIELD] ?? '');
		if (!fieldKey) return;
		const extend = isPlainObject(shape[MARK_EXTEND]) ? shape[MARK_EXTEND] : {};
		const { value, notes } = convertIncoming(fieldKey, payload, extend, path || '$');
		assignField(row, rowNotes, fieldKey, value, notes);
		return;
	}

	// $meta đối chiếu riêng ở bảng thông tin gói tin; chỉ thị lạ bỏ qua.
	if (isDirective(shape)) return;

	const obj = isPlainObject(payload) ? payload : undefined;
	for (const key of Object.keys(shape)) {
		readNode(shape[key], obj?.[key], path ? `${path}.${key}` : key, row, rowNotes, repeats);
	}
};

/** Gộp bản ghi: giá trị rỗng không đè lên giá trị đã có. */
const mergeInto = (target: { row: ReceiveRow; notes: ReceiveNotes }, source: { row: ReceiveRow; notes: ReceiveNotes }) => {
	for (const [key, value] of Object.entries(source.row)) {
		if (isEmpty(value) && !isEmpty(target.row[key])) continue;
		target.row[key] = value;
		target.notes[key] = source.notes[key] ?? [];
	}
};

/**
 * Tách payload thành bản ghi — đảo ngược cách chiều gửi nhân bản ghi:
 *  - Bộ khung không có mảng nhân bản ghi: cả payload là MỘT bản ghi.
 *  - Có mảng nhân bản ghi: mỗi phần tử là một bản ghi; lá ngoài mảng áp cho mọi bản ghi;
 *    mảng lồng bên trong phần tử chỉ đọc phần tử ở vị trí khuôn.
 *  - Nhiều mảng nhân bản ghi ngang hàng: ghép theo cùng vị trí, như chiều gửi nhân cùng danh sách.
 */
const extractRecords = (shape: any, payload: any) => {
	const base = { row: {} as ReceiveRow, notes: {} as ReceiveNotes };
	const repeats: RepeatArray[] = [];
	readNode(shape, payload, '', base.row, base.notes, repeats);

	if (!repeats.length) return [base];

	const lists = repeats.map((r) =>
		r.items.map((item, i) => {
			const record = { row: {} as ReceiveRow, notes: {} as ReceiveNotes };
			readNode(r.itemShape, item, r.itemPath.replace(/\[\d+\]$/, `[${i}]`), record.row, record.notes, null);
			return record;
		})
	);

	const total = Math.max(...lists.map((l) => l.length));
	return Array.from({ length: total }, (_, i) => {
		const record = { row: { ...base.row }, notes: { ...base.notes } };
		lists.forEach((l) => l[i] && mergeInto(record, l[i]));
		return record;
	});
};

const parsePayload = () => {
	clearResult();

	if (!props.shapeJson.trim()) {
		state.error = $t('lz.message.sharedataMapping.receiveBuildFrameworkFirst');
		return;
	}

	let payload: any;
	try {
		payload = JSON.parse(state.payload);
	} catch (e: any) {
		state.error = $t('lz.message.sharedataMapping.receiveInvalidJson', { reason: e?.message ?? '' });
		return;
	}

	let shape: any;
	try {
		shape = JSON.parse(props.shapeJson);
	} catch {
		state.error = $t('lz.message.sharedataMapping.cannotBuildFrameworkInvalidJson');
		return;
	}

	const meta: ReceiveMeta[] = [];
	collectMeta(shape, payload, '', meta);
	state.meta = meta;

	const records = extractRecords(shape, payload);

	// Trường bắt buộc rỗng: đánh dấu đỏ ở ô.
	for (const r of records) {
		for (const f of usedFields.value) {
			const key = f.aliasFieldKey!;
			if (f.isRequired === true && isEmpty(r.row[key])) {
				r.notes[key] = [...(r.notes[key] ?? []), { level: 'error', text: $t('lz.label.sharedataMapping.receiveCellRequiredEmpty') }];
			}
		}
	}

	state.rows = records.map((r) => r.row);
	state.rowNotes = records.map((r) => r.notes);
	state.parsed = true;
};

/** Xoá kết quả phân tích, giữ payload. */
const clearResult = () => {
	state.error = '';
	state.parsed = false;
	state.meta = [];
	state.rows = [];
	state.rowNotes = [];
};

/** Xoá cả payload — gọi khi mở hồ sơ khác hoặc đổi đối tác. */
const reset = () => {
	state.payload = '';
	clearResult();
};

defineExpose({ reset, clearResult });
</script>

<style scoped lang="scss">
.mp-pane-head {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 5px;
	min-height: 28px;
}

.mp-pane-title {
	font-weight: 600;
}

.mp-json :deep(.el-textarea__inner) {
	font-family: Consolas, Monaco, monospace;
	font-size: 12px;
	line-height: 1.5;
}

.mp-err {
	font-size: 12px;
	color: var(--el-color-danger);
	line-height: 1.4;
}

.rs-note {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.mt5 {
	margin-top: 5px;
}

.mt10 {
	margin-top: 10px;
}

.mp-preview-raw {
	font-family: Consolas, Monaco, monospace;
	font-size: 12px;
	word-break: break-all;
}

.mp-preview-raw-null {
	color: var(--el-text-color-placeholder);
	font-style: italic;
}

.mp-preview-key {
	font-family: Consolas, Monaco, monospace;
	font-weight: 600;
}

.mp-preview-req {
	margin-left: 2px;
	color: var(--el-color-danger);
	font-weight: 600;
}

/* Màu ô theo cách giá trị được xử lý; gạch chân chấm báo hiệu có tooltip. */
.rp-cell-info,
.rp-cell-warn,
.rp-cell-error {
	cursor: help;
	text-decoration: underline dotted;
	text-underline-offset: 3px;
}

.rp-cell-info {
	color: var(--el-color-primary);
}

.rp-cell-warn {
	color: var(--el-color-warning);
	font-style: italic;
}

.rp-cell-error {
	color: var(--el-color-danger);
}

.rp-legend {
	display: flex;
	flex-wrap: wrap;
	gap: 4px 16px;
	font-size: 12px;
}

.rp-legend span {
	cursor: default;
}

.rp-ok {
	color: var(--el-color-success);
}

.rp-warn {
	color: var(--el-color-warning);
}
</style>
