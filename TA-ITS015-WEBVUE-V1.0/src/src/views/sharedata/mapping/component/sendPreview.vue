<template>
	<div class="send-preview">
		<!-- Dữ liệu gửi đi: lấy bản ghi THẬT dưới CSDL rồi đi qua bộ khung để ra payload đối tác sẽ nhận. -->
		<div v-if="props.shapeError" class="mp-err">{{ props.shapeError }}</div>
		<div v-else-if="!previewFields.length" class="rs-note">
			{{ $t('lz.label.sharedataMapping.noFieldsUsedGoToMappingHint') }}
		</div>

		<template v-else>
			<div class="mp-pane-head">
				<span class="mp-pane-title">{{ $t('lz.label.sharedataMapping.systemSampleValues') }}</span>
				<el-button type="primary" size="small" icon="ele-Download" :loading="state.loadingSample" :disabled="!props.datatypeId" @click="loadSampleRows">{{
					$t('lz.button.sharedataMapping.fetchSampleData')
				}}</el-button>
				<span class="rs-note">{{ $t('lz.label.sharedataMapping.sampleTopLabel') }}</span>
				<el-input-number v-model="state.sampleTop" :min="feConfig.mappingSampleTop.min" :max="feConfig.mappingSampleTop.max" :step="1" size="small"
					controls-position="right" class="mp-sample-top" />
				<el-button size="small" icon="ele-Delete" :disabled="!state.sampleRows.length" @click="clearSampleRows">{{ $t('lz.button.sharedataMapping.clearAllRecords') }}</el-button>
				<span class="rs-note">{{ sampleSourceHint }}</span>
			</div>

			<el-table :data="state.sampleRows" size="small" border max-height="260" class="mt5">
				<el-table-column type="index" label="#" width="50" align="center" fixed="left" />
				<el-table-column v-for="f in previewFields" :key="f.aliasFieldKey" min-width="165" header-align="center">
					<template #header>
						<el-tooltip
							:content="f.headerHint"
							placement="top"
							popper-class="send-preview-tooltip"
						>
							<span>
								<span class="mp-preview-key">{{ f.aliasFieldKey }}</span>
								<span v-if="f.isRequired" class="mp-preview-req">*</span>
								<el-tag v-if="f.codeSets.length" size="small" type="warning" effect="plain" class="ml5">{{ $t('lz.label.sharedataMapping.codeSetTag') }}</el-tag>
							</span>
						</el-tooltip>
					</template>
					<template #default="{ row }">
						<span class="mp-preview-raw" :class="{ 'mp-preview-raw-null': isEmptyRaw(sampleValue(row, f.aliasFieldKey)) }">{{
							displayRawValue(sampleValue(row, f.aliasFieldKey))
						}}</span>
					</template>
				</el-table-column>
			</el-table>
			<div v-if="!state.sampleRows.length" class="rs-note mt5">{{ $t('lz.label.sharedataMapping.noSampleDataYet') }}</div>

			<div class="mp-preview-actions">
				<el-button type="primary" size="small" icon="ele-MagicStick" @click="buildPreview">{{ $t('lz.button.sharedataMapping.buildPayload') }}</el-button>
				<span class="rs-note">{{ previewModeHint }}</span>
			</div>

			<div v-if="state.previewWarnings.length" class="mp-preview-warn">
				<div v-for="(w, i) in state.previewWarnings" :key="i">• {{ w }}</div>
			</div>

			<div class="mp-pane-head mt10">
				<span class="mp-pane-title">{{ $t('lz.label.sharedataMapping.payloadPartnerReceives') }}</span>
				<span class="rs-note">{{ $t('lz.label.sharedataMapping.valuesProcessedHint') }}</span>
			</div>
			<el-input v-if="state.previewJson" :model-value="state.previewJson" type="textarea" :rows="16" readonly resize="vertical" class="mp-json" />
			<div v-else class="rs-note">{{ $t('lz.label.sharedataMapping.fetchSampleThenBuildHint') }}</div>
		</template>
	</div>
</template>

<script lang="ts" setup name="eshSendPreview">
import { computed, reactive, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { httpErrorMessage } from '/@/utils/httpErrorMessage';
import { ShareDataPacketApi, ShareDataCodeSetOutput } from '/@/api-services/shareData';
import { PacketFieldMeta } from '/@/utils/sharedata/packetFields';
import { useSharedataFeConfig } from '/@/hooks/useSharedataFeConfig';
const { t: $t } = useI18n();

interface Props {
	/** Bộ khung đầu ra (TargetShapeJson) — đúng tài liệu service đọc, không đi qua cấu hình trên cây. */
	shapeJson: string;
	/** Lỗi dựng bộ khung (VD giá trị mặc định sai kiểu) — có lỗi thì không dựng thử được. */
	shapeError: string;
	/** ID gói tin của hồ sơ — dùng lấy bản ghi mẫu dưới CSDL. */
	datatypeId?: string | null;
	packetFields: PacketFieldMeta[];
	/** Bộ mã đã nạp kèm bảng giá trị — dùng quy đổi sourceValue -> partnerValue. */
	codeSetList: ShareDataCodeSetOutput[];
	/** Mã đối tác (tuỳ chọn) — dùng để lấy ExtractSql riêng của đối tác nếu có. */
	partnerCode?: string | null;
}

const props = defineProps<Props>();

const MARK_FIELD = '$field';
const MARK_EXTEND = '$extend';
const MARK_META = '$meta';
const MARK_VALUE = '$value';

const feConfig = useSharedataFeConfig();

const state = reactive({
	/**
	 * Dữ liệu gửi thử: MỖI PHẦN TỬ là một BẢN GHI THẬT dưới CSDL, lấy bằng đúng câu truy vấn nguồn
	 * của gói tin; khoá tra là tên trường gói tin. Người dùng KHÔNG tự gõ: xem thử mà gõ tay thì
	 * không nói lên được dữ liệu thật chảy qua hồ sơ ánh xạ sẽ ra hình gì.
	 */
	sampleRows: [] as Record<string, any>[],
	/** Đang gọi service lấy dữ liệu mẫu. */
	loadingSample: false,
	/** Số bản ghi muốn lấy về — service chặn tối đa 50. */
	sampleTop: feConfig.value.mappingSampleTop.default,
	/** Gói tin và số bản ghi của lần lấy gần nhất, để hiện cạnh nút. */
	sampleLoadedCode: '',
	sampleLoadedCount: 0,
	/** Payload dựng thử từ giá trị mẫu. */
	previewJson: '',
	/** Cảnh báo khi dựng thử: giá trị không có trong bộ mã, trường bắt buộc còn rỗng... */
	previewWarnings: [] as string[],
});

const hasOwn = (node: any, key: string) => Object.prototype.hasOwnProperty.call(node, key);
const isPlainObject = (v: any) => v !== null && typeof v === 'object' && !Array.isArray(v);
const isDirective = (node: any) => isPlainObject(node) && Object.keys(node).some((k) => k.startsWith('$'));

const fieldMeta = (fieldKey: string) => props.packetFields.find((f) => f.aliasFieldKey?.toLowerCase() === fieldKey.toLowerCase());
const sampleValue = (row: Record<string, any>, key: string) => row[Object.keys(row).find((k) => k.toLowerCase() === key.toLowerCase()) ?? key];

/** Bộ khung đã dựng được thì trả về, không thì null. */
const parsedShape = computed(() => {
	if (props.shapeError || !props.shapeJson.trim()) return null;
	try {
		return JSON.parse(props.shapeJson);
	} catch {
		return null;
	}
});

/** Bộ khung đổi thì payload đang xem không còn đúng; giữ bản ghi mẫu để dựng lại không phải lấy lại. */
watch(() => [props.shapeJson, props.codeSetList], () => clearPreview());

/** Đổi gói tin hoặc đổi đối tác thì bản ghi mẫu cũ không còn nghĩa. */
watch(() => [props.datatypeId, props.partnerCode], () => clearSampleRows());

/* ---- Quy đổi giá trị một lá ----
 *
 * Chạy hoàn toàn trên trình duyệt, KHÔNG gọi service. Hai thứ phải giữ đúng như
 * bộ dựng payload phía service để xem thử không nói dối:
 *
 * 1. Thứ tự áp luật cho một lá:
 *    giá trị nguồn -> quy đổi bộ mã -> rỗng thì lấy mặc định -> ép kiểu
 *    (bỏ ép kiểu nếu đã qua bộ mã, vì giá trị bộ mã trả ra đã là giá trị cuối).
 *
 * 2. Cách nhân bản ghi. Service xét khuôn trước:
 *    - Khuôn CÓ mảng chứa trường đã ánh xạ  -> gộp mọi bản ghi vào MỘT gói tin,
 *      mảng đó nhân theo từng bản ghi, còn lá nằm ngoài mảng lấy bản ghi đầu tiên.
 *    - Khuôn KHÔNG có mảng như vậy          -> mỗi bản ghi thành một gói tin riêng.
 */

/** Giá trị dựng thử cho 4 khoá hệ thống — service sẽ sinh giá trị thật lúc gửi; mã gói tin / đối tác lấy từ $value. */
const builtinPreviewValue = (key: string, value: any) => {
	switch (key) {
		case 'Now':
			return new Date().toISOString();
		case 'Serial':
			return 1;
		case 'PacketCode':
		case 'PartnerCode':
			return value ?? '';
		default:
			return null;
	}
};

/**
 * Quy đổi một giá trị theo bộ mã: khớp sourceValue (bỏ qua hoa thường và khoảng trắng
 * thừa) thì lấy partnerValue; không khớp hoặc rỗng thì lấy mặc định chiều gửi của bộ mã;
 * vẫn không có thì giữ nguyên giá trị gốc và ghi một dòng cảnh báo.
 */
const previewMapCode = (code: string, value: any, warnings: string[]) => {
	const cs = props.codeSetList.find((c) => c.code?.toLowerCase() === code.toLowerCase());
	const values = cs?.values ?? [];
	const directionDefault = cs?.defaultPartnerValue?.trim() ? cs.defaultPartnerValue : undefined;
	if (value === null || value === undefined || String(value).trim() === '') return directionDefault ?? null;

	if (!values.length) {
		if (directionDefault !== undefined) return directionDefault;
		warnings.push($t('lz.message.sharedataMapping.codeSetNoConversionLines', { code }));
		return value;
	}

	const valStr = String(value).trim();
	const match = values.find((v) => (v.sourceValue ?? '').toString().trim().toLowerCase() === valStr.toLowerCase());
	if (match) return match.partnerValue;
	if (directionDefault !== undefined) return directionDefault;

	warnings.push($t('lz.message.sharedataMapping.codeSetValueNoMatchNoDefault', { code, value: valStr }));
	return value;
};

/** Ép kiểu như service: ép không được thì giữ nguyên, không ném lỗi. */
const previewCoerce = (value: any, type?: string | null, numberFormat = '', dateFormat = ''): any => {
	const t = (type ?? '').trim().toLowerCase();
	if (value === null || value === undefined || t === '') return value;

	if (['decimal', 'number', 'double', 'float'].includes(t) || (t === 'string' && numberFormat)) {
		let n = Number(String(value).replaceAll(',', ''));
		if (!Number.isFinite(n)) return value;
		const precision = /^(?:[fFnN])(\d{1,2})$/.exec(numberFormat)?.[1] ?? (/^(?:#,##)?0(?:\.(0+))?$/.test(numberFormat) ? /\.(0+)$/.exec(numberFormat)?.[1].length ?? 0 : undefined);
		if (precision !== undefined && Number(precision) > 20) return value;
		if (precision !== undefined) n = Number(n.toFixed(Number(precision)));
		if (t !== 'string') return n;
		const text = precision !== undefined ? n.toFixed(Number(precision)) : String(n);
		const parts = text.split('.');
		if (/^[nN]/.test(numberFormat) || numberFormat.startsWith('#,##')) parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
		return parts.join('.');
	}
	if (['int', 'integer', 'long'].includes(t)) {
		const n = Number(String(value).replaceAll(',', ''));
		if (!Number.isFinite(n) || !Number.isSafeInteger(Math.trunc(n))) return value;
		const floor = Math.floor(n);
		return n - floor === 0.5 ? (floor % 2 === 0 ? floor : floor + 1) : Math.round(n);
	}
	if (['bool', 'boolean'].includes(t)) {
		if (typeof value === 'number') return value !== 0;
		const v = String(value).trim().toLowerCase();
		if (v === 'true') return true;
		if (v === 'false') return false;
		return value;
	}
	if (['date', 'datetime', 'time'].includes(t)) {
		const date = new Date(value);
		if (Number.isNaN(date.getTime())) return value;
		if (!dateFormat || dateFormat === 'o' || dateFormat === 'O') return date.toISOString();
		const pad = (n: number) => String(n).padStart(2, '0');
		const tokens: Record<string, string> = {
			yyyy: String(date.getFullYear()),
			MM: pad(date.getMonth() + 1),
			dd: pad(date.getDate()),
			HH: pad(date.getHours()),
			mm: pad(date.getMinutes()),
			ss: pad(date.getSeconds()),
			fff: String(date.getMilliseconds()).padStart(3, '0'),
		};
		return dateFormat.replace(/yyyy|MM|dd|HH|mm|ss|fff/g, (token) => tokens[token]);
	}
	if (t === 'guid') {
		const v = String(value).trim();
		return v === '' ? null : v;
	}
	if (t === 'string') return String(value);
	return value;
};

/** Giá trị cuối cùng của một lá $field, tính trên MỘT bản ghi. */
const previewLeafValue = (fieldKey: string, extend: any, row: Record<string, any>, warnings: string[]) => {
	const meta = fieldMeta(fieldKey);
	// Giữ kiểu số/boolean của nguồn; chỉ chuẩn hoá khoảng trắng cho chuỗi.
	const raw = sampleValue(row, fieldKey);
	if (!Object.keys(row).some((k) => k.toLowerCase() === fieldKey.toLowerCase())) warnings.push($t('lz.message.sharedataMapping.sampleColumnMissing', { field: fieldKey }));
	const typed = raw === null || raw === undefined ? '' : String(raw).trim();

	let val: any = typed === '' ? null : typeof raw === 'string' ? typed : raw;

	const codeSet = String(extend.codeSet ?? '').trim();
	if (codeSet) val = previewMapCode(codeSet, val, warnings);

	// Mặc định áp SAU bộ mã, đúng như service. Chiều gửi chỉ dùng giá trị đối tác mặc định.
	const fallback = String(extend.defaultPartnerValue ?? '').trim();
	if ((val === null || val === undefined) && fallback !== '') val = fallback;

	// Đã qua bộ mã thì giữ nguyên giá trị bộ mã trả ra, không ép kiểu nữa.
	if (!codeSet) {
		const numberFormat = String(extend.numberFormat ?? '').trim();
		const dateFormat = String(extend.dateFormat ?? '').trim();
		const supportedNumber = !numberFormat || /^(?:[fFnN]\d{1,2}|(?:#,##)?0(?:\.0+)?)$/.test(numberFormat);
		const supportedDate = !dateFormat || /^[oO]$/.test(dateFormat) || !/[A-Za-z]/.test(dateFormat.replace(/yyyy|MM|dd|HH|mm|ss|fff/g, ''));
		if (!supportedNumber || !supportedDate) warnings.push($t('lz.message.sharedataMapping.previewFormatUnsupported', { field: fieldKey }));
		val = previewCoerce(val, String(extend.targetType ?? '').trim() || meta?.type, supportedNumber ? numberFormat : '', supportedDate ? dateFormat : '');
	}

	if (meta?.isRequired === true && (val === null || String(val).trim() === '')) {
		warnings.push($t('lz.message.sharedataMapping.requiredFieldEmptyWillBlock', { field: fieldKey }));
	}

	return val;
};

/* ---- Dựng payload trên bộ khung ----
 *
 * Bộ khung đã rút gọn mảng (mảng có ánh xạ chỉ còn một phần tử khuôn) và đã đổi hằng số
 * về đúng kiểu JSON, nên đi thẳng theo bộ khung là ra đúng hình payload service dựng.
 */

/** Nhánh bộ khung có khoá $field nào không. */
const hasFieldBinding = (node: any): boolean => {
	if (Array.isArray(node)) return node.some(hasFieldBinding);
	if (!isPlainObject(node)) return false;
	if (hasOwn(node, MARK_FIELD)) return true;
	if (isDirective(node)) return false;
	return Object.values(node).some(hasFieldBinding);
};

/** Bộ khung có mảng nhân theo bản ghi không — quyết định payload ra một gói hay nhiều gói. */
const hasRepeat = (node: any): boolean => {
	if (Array.isArray(node)) return node.some((item) => hasFieldBinding(item) || hasRepeat(item));
	if (!isPlainObject(node) || isDirective(node)) return false;
	return Object.values(node).some(hasRepeat);
};

/** Dựng payload cho MỘT bản ghi: thay mỗi chỉ thị trong bộ khung bằng giá trị sẽ gửi. */
const buildPreviewNode = (node: any, row: Record<string, any>, warnings: string[]): any => {
	if (Array.isArray(node)) return node.map((item) => buildPreviewNode(item, row, warnings));
	if (!isPlainObject(node)) return node;

	if (hasOwn(node, MARK_META)) return builtinPreviewValue(String(node[MARK_META] ?? ''), node[MARK_VALUE]);
	if (hasOwn(node, MARK_FIELD)) {
		const extend = isPlainObject(node[MARK_EXTEND]) ? node[MARK_EXTEND] : {};
		return previewLeafValue(String(node[MARK_FIELD] ?? ''), extend, row, warnings);
	}
	if (isDirective(node)) return null;

	const out: Record<string, any> = {};
	for (const key of Object.keys(node)) out[key] = buildPreviewNode(node[key], row, warnings);
	return out;
};

/**
 * Dựng payload GỘP: một gói tin duy nhất, mảng có trường bên trong thì nhân theo từng
 * bản ghi, lá nằm ngoài mảng lấy bản ghi đầu tiên — đúng như nhánh gộp của service.
 */
const buildPreviewAggregate = (node: any, rows: Record<string, any>[], warnings: string[]): any => {
	if (Array.isArray(node)) {
		const out: any[] = [];
		for (const item of node) {
			// Phần tử khuôn: nhân theo từng bản ghi.
			if (hasFieldBinding(item)) rows.forEach((row) => out.push(buildPreviewNode(item, row, warnings)));
			else out.push(buildPreviewAggregate(item, rows, warnings));
		}
		return out;
	}

	if (!isPlainObject(node) || isDirective(node)) return buildPreviewNode(node, rows[0] ?? {}, warnings);

	const out: Record<string, any> = {};
	for (const key of Object.keys(node)) out[key] = buildPreviewAggregate(node[key], rows, warnings);
	return out;
};

/**
 * Trường của gói tin đang được bộ khung dùng — mỗi trường là MỘT CỘT trong bảng mẫu.
 * Dùng computed để el-table không dựng lại cột mỗi lần vẽ.
 */
const previewFields = computed(() => {
	const used: Record<string, Set<string>> = {};
	const walk = (node: any) => {
		if (Array.isArray(node)) return node.forEach(walk);
		if (!isPlainObject(node)) return;
		if (hasOwn(node, MARK_FIELD)) {
			const key = String(node[MARK_FIELD] ?? '');
			if (!used[key]) used[key] = new Set<string>();
			const codeSet = isPlainObject(node[MARK_EXTEND]) ? String(node[MARK_EXTEND].codeSet ?? '').trim() : '';
			if (codeSet) used[key].add(codeSet);
			return;
		}
		Object.values(node).forEach(walk);
	};
	walk(parsedShape.value);

	return props.packetFields
		.filter((f) => f.aliasFieldKey && used[f.aliasFieldKey])
		.map((f) => {
			const codeSets = Array.from(used[f.aliasFieldKey!]);
			const parts = [f.name, f.type];
			if (f.isRequired === true) parts.push($t('lz.label.sharedataDataSource.required'));
			if (codeSets.length) parts.push(`${$t('lz.label.sharedataMapping.codeSet')} ${codeSets.join(', ')}`);
			return {
				aliasFieldKey: f.aliasFieldKey!,
				isRequired: f.isRequired,
				codeSets,
				headerHint: parts.filter((x) => (x ?? '') !== '').join(' · '),
			};
		});
});

/** Câu nhắc cho biết payload sắp dựng ra một gói tin hay nhiều gói tin. */
const previewModeHint = computed(() => {
	if (!parsedShape.value) return '';
	const n = state.sampleRows.length;
	return hasRepeat(parsedShape.value)
		? $t('lz.label.sharedataMapping.previewArrayHint', { count: n })
		: $t('lz.label.sharedataMapping.previewNoArrayHint', { count: n });
});

/** Giá trị rỗng thì tô nhạt cho khỏi lẫn với chuỗi rỗng do đối tác gửi. */
const isEmptyRaw = (value: any) => value === null || value === undefined || value === '';

/** Đưa giá trị thô lên bảng: rỗng thì hiện dấu hiệu, còn lại giữ nguyên dạng CSDL trả về. */
const displayRawValue = (value: any) => {
	if (value === null || value === undefined) return 'NULL';
	if (value === '') return $t('lz.label.sharedataMapping.emptyValue');
	if (typeof value === 'object') return JSON.stringify(value);
	return String(value);
};

/** Câu nhắc cạnh nút: lần lấy gần nhất được bao nhiêu bản ghi của gói tin nào. */
const sampleSourceHint = computed(() => {
	if (!state.sampleLoadedCode) return $t('lz.label.sharedataMapping.sampleNotLoadedHint');
	return $t('lz.label.sharedataMapping.sampleLoadedHint', {
		count: state.sampleLoadedCount,
		packet: state.sampleLoadedCode,
	});
});

/**
 * Lấy bản ghi THẬT dưới CSDL cho gói tin đang chọn.
 *
 * Service chạy đúng câu truy vấn nguồn của gói tin (câu mà dịch vụ nền dùng khi gửi cho đối tác)
 * và chỉ trả DỮ LIỆU THÔ — toàn bộ phần quy đổi bộ mã, ép kiểu, dựng payload vẫn do màn này làm,
 * nhờ vậy người dùng so được hai đầu: dữ liệu dưới CSDL và payload đối tác sẽ nhận.
 */
let sampleRequest = 0;
/** Lỗi tự ném khi BE trả sai dạng — không phải lý do để hiện cho người dùng. */
const INVALID_SAMPLE_RESPONSE = 'Invalid sample response';
const loadSampleRows = async () => {
	if (state.loadingSample) return;
	if (!props.datatypeId) {
		ElMessage.warning($t('lz.message.sharedataMapping.selectPacketBeforeSample'));
		return;
	}

	clearSampleRows();
	const request = ++sampleRequest;
	const datatypeId = props.datatypeId;
	state.loadingSample = true;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketSampledataPost({
			datatypeId,
			top: state.sampleTop,
			partnerCode: props.partnerCode || undefined,
		} as any);

		if (request !== sampleRequest || datatypeId !== props.datatypeId) return;
		const result = res.data.result;
		if (!result || !Array.isArray(result.rows) || result.rows.some((row) => !row || typeof row !== 'object' || Array.isArray(row))) throw new Error(INVALID_SAMPLE_RESPONSE);
		const rows = result.rows;

		state.sampleRows = rows;
		state.sampleLoadedCode = result.packetCode ?? '';
		state.sampleLoadedCount = rows.length;
		clearPreview();

		if (!rows.length) {
			ElMessage.warning($t('lz.message.sharedataMapping.sampleDataEmpty'));
			return;
		}

		// Lấy xong dựng luôn payload: người dùng bấm nút là để nhìn kết quả, không phải để nhìn bảng.
		buildPreview();
	} catch (err: any) {
		if (request !== sampleRequest || datatypeId !== props.datatypeId) return;
		// Hiện đúng lý do BE trả về (VD câu SQL dùng biến chưa khai) thay vì một câu chung chung; không có lý do thì giữ câu cũ.
		const reason = err?.message === INVALID_SAMPLE_RESPONSE ? '' : httpErrorMessage(err?.response?.data, httpErrorMessage(err, ''));
		state.previewWarnings = [
			reason
				? $t('lz.message.sharedataMapping.sampleLoadFailedReason', { reason })
				: $t('lz.message.sharedataMapping.sampleLoadFailed'),
		];
	} finally {
		if (request === sampleRequest) state.loadingSample = false;
	}
};

const clearPreview = () => {
	state.previewJson = '';
	state.previewWarnings = [];
};

/** Bỏ bản ghi mẫu và payload đang xem; lượt lấy mẫu đang chạy (nếu có) bị bỏ qua khi trả về. */
const clearSampleRows = () => {
	sampleRequest++;
	state.loadingSample = false;
	state.sampleRows = [];
	state.sampleLoadedCode = '';
	state.sampleLoadedCount = 0;
	clearPreview();
};

/** Dựng payload gửi đi từ các bản ghi mẫu vừa lấy. */
const buildPreview = () => {
	clearPreview();
	const shape = parsedShape.value;
	if (!shape) return;

	if (!state.sampleRows.length) {
		ElMessage.warning($t('lz.message.sharedataMapping.fetchSampleBeforeBuild'));
		return;
	}

	const rows = state.sampleRows;
	const warnings: string[] = [];
	const payload = hasRepeat(shape) ? buildPreviewAggregate(shape, rows, warnings) : rows.map((row) => buildPreviewNode(shape, row, warnings));

	// Cảnh báo trùng nhau giữa các bản ghi thì gộp lại cho đỡ rối.
	state.previewWarnings = Array.from(new Set(warnings));
	state.previewJson = JSON.stringify(payload, null, 2);
};

defineExpose({ reset: clearSampleRows });
</script>

<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.el-popper.send-preview-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}
</style>

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

.mp-sample-top {
	width: 110px;
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

.mp-preview-actions {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
	margin-top: 8px;
}

.mp-preview-warn {
	margin-top: 8px;
	padding: 6px 8px;
	border-radius: 4px;
	font-size: 12px;
	line-height: 1.6;
	color: var(--el-color-warning);
	background: var(--el-color-warning-light-9);
}
</style>
