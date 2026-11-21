<template>
	<!-- Bảng tham số câu truy vấn: trích mỗi biến @tên dùng trong câu SQL và nguồn hệ thống cung cấp giá trị lúc chạy.
	     Biến đi theo câu SQL: mẫu chung có bộ biến của mẫu chung, SQL riêng của đối tác có bộ biến riêng (chép từ mẫu chung khi tạo SQL riêng). -->
	<div class="sql-variable-panel">
		<div class="svp-card svp-params-card" :class="{ 'is-scope-locked': scopeLocked }">
			<div class="svp-header" :class="{ 'is-collapsed': collapsed }">
				<div class="svp-header-left" @click="toggleCollapsed">
					<span class="svp-title">{{ $t('lz.label.sharedataSqlVariable.title') }}</span>
					<span class="svp-badge-count">{{ state.rows.length }}</span>
					<!-- Dải kiểm nhanh quy chuẩn SQL nối mốc (hover mở popup xem toàn bộ tiêu chuẩn, không màu cam) -->
					<div v-if="hasCursorVars" class="svp-checks" @click.stop>
						<el-popover placement="top-start" width="auto" trigger="hover" popper-class="svp-check-popover">
							<template #reference>
								<div class="svp-check-lead" :class="allChecksPass ? 'is-pass' : 'is-fail'">
									<span class="svp-check-lead-text">{{ $t('lz.label.sharedataSqlVariable.checkBarLead') }}</span>
									<span class="svp-check-lead-count">({{ passedChecksCount }}/{{ totalChecksCount }})</span>
									<el-icon class="svp-check-bulb"><ele-Opportunity /></el-icon>
								</div>
							</template>
							<div class="svp-popover-content">
								<div class="svp-popover-title">
									<el-icon class="mr4 svp-check-bulb" :class="allChecksPass ? 'is-lit' : 'is-unlit'"><ele-Opportunity /></el-icon>
									<strong>{{ $t('lz.label.sharedataSqlVariable.checkBarTitle') }}</strong>
									<el-button v-if="canAutoFix && failedChecks.length > 0" type="primary" size="small" class="svp-fix-all svp-fix-apply" @click="applyFixes(failedChecks.map((c) => c.key))">
										{{ $t('lz.button.sharedataSqlVariable.fixSql') }}
									</el-button>
								</div>
								<div class="svp-popover-list">
									<div v-for="check in cursorChecks" :key="check.key" class="svp-popover-item svp-check-chip" :class="check.pass ? 'is-pass' : 'is-fail'">
										<el-icon class="svp-check-icon">
											<ele-CircleCheckFilled v-if="check.pass" />
											<ele-CircleCloseFilled v-else />
										</el-icon>
										<div class="svp-check-body">
											<span>{{ check.label }}</span>
											<template v-if="!check.pass">
												<div class="svp-fix-text">{{ check.fixText }}</div>
												<div class="svp-fix-code">
													<code>{{ check.fixCode }}</code>
													<el-button v-if="!canAutoFix" link type="primary" size="small" class="svp-fix-copy" @click="copyText(check.fixCode)">{{ $t('lz.button.base.copy') }}</el-button>
												</div>
											</template>
										</div>
									</div>
								</div>
							</div>
						</el-popover>
					</div>
				</div>
				<el-tooltip :content="collapsed ? $t('lz.button.sharedataDataSource.sqlEditorExpandBtn') : $t('lz.button.sharedataDataSource.sqlEditorCollapseBtn')" placement="top">
					<el-button
						text
						size="small"
						type="primary"
						class="svp-collapse-btn"
						:icon="collapsed ? 'ele-ArrowDown' : 'ele-ArrowUp'"
						:aria-label="collapsed ? $t('lz.button.sharedataDataSource.sqlEditorExpandBtn') : $t('lz.button.sharedataDataSource.sqlEditorCollapseBtn')"
						@click="toggleCollapsed"
					/>
				</el-tooltip>
			</div>

			<el-collapse-transition>
			<div v-show="!collapsed">
			<el-table :data="state.rows" size="small" border style="width: 100%" class="svp-table" :empty-text="$t('lz.label.sharedataSqlVariable.empty')">
				<!-- Cột So mẫu chung (chỉ ở tab đối tác): ký hiệu so biến của đối tác với mẫu chung -->
				<el-table-column
					v-if="Boolean(props.partnerId)"
					:label="$t('lz.label.sharedataDataSource.fieldCompareColumn')"
					width="110"
					label-class-name="svp-nowrap-head"
					align="center"
					header-align="center"
				>
					<template #default="{ row }">
						<el-tooltip
							:content="compareTooltip(row)"
							placement="top"
							popper-class="sql-variable-tooltip"
						>
							<span class="cmp-st" :class="compareState(row)">{{ compareSymbol(compareState(row)) }}</span>
						</el-tooltip>
					</template>
				</el-table-column>

				<!-- Cột 1: Tham số -->
				<el-table-column :label="$t('lz.label.sharedataSqlVariable.colName')" width="95" header-align="center">
					<template #default="{ row }">
						<el-tooltip
							:disabled="!shownCodeError(row)"
							:content="shownCodeError(row)"
							placement="top"
							popper-class="sql-variable-tooltip"
						>
							<div class="svp-param-code-line" :class="{ 'is-error': Boolean(shownCodeError(row)) }">
								<span class="svp-param-code">@{{ normalizedCode(row) }}</span>
							</div>
						</el-tooltip>
					</template>
				</el-table-column>

				<!-- Cột 2: Nguồn giá trị -->
				<el-table-column :label="$t('lz.label.sharedataSqlVariable.colSource')" min-width="230" header-align="center">
					<template #default="{ row }">
						<div class="svp-source-cell">
							<el-tooltip
								:disabled="!isPageSize(row)"
								:content="$t('lz.tooltip.sharedataSqlVariable.pageSizeFixedOnly')"
								placement="top"
								popper-class="sql-variable-tooltip"
							>
								<el-select
									:model-value="sourceKey(row)"
									size="small"
									class="svp-source-select"
									:class="[`is-source-${sourceKey(row) || 'const'}`, { 'is-error': Boolean(shownValueError(row) && !sourceKey(row)) }]"
									popper-class="svp-source-popper"
									:disabled="isPageSize(row) || isCursorVariable(row) || scopeLocked"
									@update:model-value="(v: string) => onSourceChange(row, v)"
								>
									<template #prefix>
										<span class="svp-source-prefix" :class="sourceKey(row) === 'lastKey' ? 'is-key' : 'is-const'">
											<el-icon v-if="sourceKey(row) === 'lastKey'"><ele-Key /></el-icon>
											<el-icon v-else><ele-Edit /></el-icon>
										</span>
									</template>
									<el-option
										v-for="s in SOURCES"
										:key="s"
										:value="s"
										:label="$t(`lz.label.sharedataSqlVariable.source_${s}`)"
									>
										<div class="svp-source-opt">
											<span class="svp-source-opt-icon" :class="s === 'lastKey' ? 'is-key' : 'is-const'">
												<el-icon v-if="s === 'lastKey'"><ele-Key /></el-icon>
												<el-icon v-else><ele-Edit /></el-icon>
											</span>
											<div class="svp-source-opt-body">
												<div class="svp-source-opt-head">
													<span class="svp-source-opt-title">{{ $t(`lz.label.sharedataSqlVariable.source_${s}`) }}</span>
												</div>
												<div class="svp-source-opt-desc">
													{{ $t(s === 'lastKey' ? 'lz.hint.sharedataSqlVariable.sourceLastKey' : 'lz.hint.sharedataSqlVariable.sourceConst') }}
												</div>
											</div>
										</div>
									</el-option>
								</el-select>
							</el-tooltip>
						</div>
					</template>
				</el-table-column>

				<!-- Cột 3: Giá trị cấu hình -->
				<el-table-column :label="$t('lz.label.sharedataSqlVariable.colValue')" min-width="210" header-align="center">
					<template #default="{ row }">
						<!-- Nguồn 1: lastKey (chuỗi thời gian_đuôi của dòng cuối đã gửi): nhập mốc bắt đầu -->
						<div v-if="sourceKey(row) === 'lastKey'" class="svp-key-cursor-wrap">
							<cursor-value-input
								class="svp-key-input"
								:model-value="row.defaultValue"
								allow-empty
								:invalid="cursorSqlInvalid"
								:has-cursor="Boolean(props.partnerId) && Boolean(row.hasCursor)"
								:common-scope="!props.partnerId"
								:disabled="scopeLocked"
								@update:model-value="(v: string) => onStartKeyChange(row, v)"
							/>
						</div>

						<span v-else-if="isRowRemovedByFix(row)" class="svp-empty">{{ $t('lz.label.sharedataSqlVariable.rowRemovedByFix') }}</span>

						<!-- Nguồn 2: const -->
						<div v-else class="svp-val-const-block">
							<el-tooltip
								:disabled="!shownValueError(row)"
								:content="shownValueError(row)"
								placement="top"
								:trigger-keys="[]"
								popper-class="sql-variable-tooltip"
							>
								<div class="svp-const-input-row" :class="{ 'is-error': Boolean(shownValueError(row)) }">
									<el-input-number
										v-if="isPageSize(row)"
										:model-value="pageSizeNumber(row)"
										size="small"
										class="svp-const-input svp-page-size-input"
										:class="{ 'is-error': Boolean(shownValueError(row)) }"
										:min="1"
										:max="100000"
										:step="100"
										:precision="0"
										:value-on-clear="null"
										:disabled="scopeLocked"
										controls-position="right"
										:placeholder="shownValueError(row) || $t('lz.placeholder.sharedataSqlVariable.constValue')"
										@update:model-value="(v: number | null | undefined) => onPageSizeChange(row, v)"
									/>
									<el-input
										v-else
										v-model="row.value"
										size="small"
										class="svp-const-input"
										:class="{ 'is-error': Boolean(shownValueError(row)) }"
										:maxlength="256"
										:disabled="scopeLocked"
										:placeholder="shownValueError(row) || $t('lz.placeholder.sharedataSqlVariable.constValue')"
										@input="row.touched = true"
									/>
								</div>
							</el-tooltip>
						</div>

					</template>
				</el-table-column>
			</el-table>
			</div>
			</el-collapse-transition>
		</div>
	</div>
</template>

<script lang="ts" setup name="sqlVariablePanel">
import { computed, nextTick, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import type { PacketSqlVariableItemInput } from '/@/api-services/shareData';
import { type PacketSqlPreload, takePacketSqlPreload } from '/@/utils/sharedata/packetSqlPreload';
import {
	applyCursorFix,
	buildCursorSections,
	cursorColumn,
	cursorGroupOf,
	type CursorFixKind,
	firstTableAlias,
	hasBareUpdateTime,
	hasTimeKeyFormula,
	isCursorFormula,
	isValidCursorValue,
	withCreateTimeInFormula,
	withNonNullKeyTail,
	withTimeKeyFormula,
	hasTopLevelUnion,
	selectColumnExpression,
	withCreateTimeFallback,
} from '/@/utils/sharedata/cursorSections';
import commonFunction from '/@/utils/commonFunction';
import { Local } from '/@/utils/storage';
import CursorValueInput from './cursorValueInput.vue';

const { t: $t } = useI18n();
const { copyText } = commonFunction();

const props = defineProps<{
	packetCode?: string | null;
	/** Có giá trị: bộ biến của SQL riêng đối tác; trống: bộ biến của mẫu chung. */
	partnerId?: string | null;
	sql?: string | null;
	/** Trang cha đang hiện một vòng loading chung (đổi gói): không hiện vòng loading riêng. */
	silentLoading?: boolean;
	/** Cấu hình SQL mẫu chung nạp sẵn khi bấm gói: tab Mẫu chung lấy bộ biến từ đây thay vì gọi API riêng. */
	preload?: PacketSqlPreload | null;
	/** Đối tác có đăng ký gửi đang chạy: BE chặn sửa bộ biến của họ (ESH-2306); tab Mẫu chung khoá khi danh sách không rỗng. */
	lockedPartnerIds?: string[];
}>();

const isPartnerLocked = (partnerId?: string | null) => Boolean(partnerId) && (props.lockedPartnerIds ?? []).includes(partnerId!);
/** Bộ biến đang mở bị khoá: bộ riêng → đăng ký của đối tác đó; bộ mẫu chung → mọi đăng ký gửi của gói. */
const scopeLocked = computed(() => (props.partnerId ? isPartnerLocked(props.partnerId) : (props.lockedPartnerIds ?? []).length > 0));

const emit = defineEmits<{
	/** Danh sách biến đang khai (kể cả chưa lưu) để ô soạn SQL gợi ý khi gõ @. */
	(e: 'variables-change', items: { code: string; desc?: string }[]): void;
	(e: 'dirty-change', dirty: boolean): void;
	/** Bấm Sửa ở quy chuẩn mốc: câu SQL đã sửa để đưa vào khung soạn. */
	(e: 'apply-sql', sql: string): void;
	/** Câu SQL đang soạn có dùng mốc (biến nguồn lastKey hoặc cột __lastKey). */
	(e: 'cursor-vars-change', hasCursor: boolean): void;
}>();

/* Giữ khớp ShareDataConst (BE) */
const KIND_BUILTIN = 'Builtin';
const KIND_CONST = 'Const';
const TYPE_STRING = 'String';
const TYPE_INT = 'Int';
const TYPE_DECIMAL = 'Decimal';
const BUILTIN_TYPES: Record<string, string> = {
	lastKey: TYPE_STRING,
};
/** Các nguồn giá trị: Theo Mốc đang chạy, Giá trị nhập. Nguồn theo tên biến (normalizeSource), không chọn tay. */
const SOURCES = ['lastKey', 'const'];
const DEFAULT_PAGE_SIZE = '100';
const TOP_VARIABLE_PATTERN = /\bTOP\s*\(\s*@([A-Za-z][A-Za-z0-9_]{0,63})\s*\)/gi;
const CODE_PATTERN = /^[A-Za-z][A-Za-z0-9_]{0,63}$/;

interface VariableRow {
	code: string;
	name?: string | null;
	dataType: string;
	valueKind: string;
	value: string;
	defaultValue?: string;
	remark?: string | null;
	autoDetected?: boolean;
	touched?: boolean;
	/** Đối tác đang mở đã có mốc gửi của biến mốc này (máy chủ trả savedValue); chỉ giữ có hay không, không giữ giá trị mốc. */
	hasCursor?: boolean;
}

const state = reactive({
	loading: false,
	saving: false,
	/** Bộ biến gốc trả về từ máy chủ (trước khi lọc theo câu SQL đã nạp). */
	rawSaved: [] as VariableRow[],
	/** Bộ biến đã lưu của câu SQL (mẫu chung hoặc SQL riêng). */
	saved: [] as VariableRow[],
	/** Các dòng đang sửa. */
	rows: [] as VariableRow[],
	/** Chỉ báo lỗi sau khi đã bấm Lưu một lần; dòng vừa thêm chưa báo lỗi ngay */
	showErrors: false,
});

const inferConstDataType = (raw?: string | null): string => {
	const val = (raw ?? '').trim();
	if (/^-?\d+$/.test(val)) {
		const n = Number(val);
		if (Number.isSafeInteger(n) && n >= -2147483648 && n <= 2147483647) return TYPE_INT;
	}
	if (/^-?\d+(\.\d+)?$/.test(val)) return TYPE_DECIMAL;
	return TYPE_STRING;
};

/** Biến nằm trong TOP (@biến) của câu SQL là biến cỡ trang (số nguyên), dù đặt tên gì; @pageSize luôn là biến cỡ trang. */
const isPageSizeCode = (code: string): boolean => {
	if (/^pageSize$/i.test(code)) return true;
	const sql = (props.sql ?? '')
		.replace(/--[^\r\n]*/g, '')
		.replace(/\/\*[\s\S]*?(?:\*\/|$)/g, '')
		.replace(/N?'(?:''|[^'])*(?:'|$)/g, '');
	return [...sql.matchAll(TOP_VARIABLE_PATTERN)].some((m) => m[1].toLowerCase() === code.toLowerCase());
};

/**
 * Chuẩn hóa nguồn biến khi nạp từ cấu hình đã lưu:
 * - Biến cỡ trang (@pageSize hoặc biến trong TOP (@biến)) cố định là hằng số nguyên (mặc định 100).
 * - Biến đã lưu nguồn Const hoặc nguồn mốc lastKey hợp lệ được giữ nguyên.
 * - Nguồn hệ thống cũ không còn dùng trong SOURCES chuyển về giá trị nhập để người dùng cấu hình lại.
 */
const normalizeSource = (row: VariableRow): VariableRow => {
	const code = row.code.trim().replace(/^@/, '');
	if (isPageSizeCode(code)) {
		return { ...row, valueKind: KIND_CONST, value: (row.value ?? '').trim() || DEFAULT_PAGE_SIZE, dataType: TYPE_INT };
	}
	if (row.valueKind === KIND_CONST) return row;
	if (row.value === 'lastKey' && !/^lastTime(_|$)/i.test(code)) return { ...row, valueKind: KIND_BUILTIN, dataType: TYPE_STRING };
	if (/^lastKey(_|$)/i.test(code)) return { ...row, valueKind: KIND_BUILTIN, value: 'lastKey', dataType: TYPE_STRING };
	return { ...row, valueKind: KIND_CONST, value: '', dataType: TYPE_STRING };
};

const toRow = (item: any): VariableRow =>
	normalizeSource({
		code: item.code ?? '',
		name: item.name ?? null,
		dataType: item.valueKind === KIND_CONST ? inferConstDataType(item.value) : (item.dataType || ''),
		valueKind: item.valueKind || KIND_BUILTIN,
		value: item.value ?? '',
		defaultValue: (item.defaultValue ?? '').trim(),
		remark: item.remark ?? null,
		hasCursor: Boolean((item.savedValue ?? '').trim()),
	});

const snapshot = (rows: VariableRow[]) =>
	JSON.stringify(
		rows.map((r) => [
			r.code.trim().replace(/^@/, '').toLowerCase(),
			r.valueKind === KIND_CONST ? inferConstDataType(r.value) : r.dataType,
			r.valueKind,
			r.value ?? '',
			r.valueKind === KIND_BUILTIN && r.value === 'lastKey' ? (r.defaultValue ?? '').trim() : '',
			r.remark ?? '',
		])
	);
const isDirty = computed(() => {
	// Dòng tự động dò từ câu SQL mà người dùng chưa từng chạm vào thì không tính là thay đổi chưa lưu
	const activeRows = state.rows.filter((r) => !r.autoDetected || r.touched);
	return snapshot(activeRows) !== snapshot(state.saved);
});

/** Có biến dư thừa trong VariablesJson trên máy chủ đã được lọc bỏ theo câu SQL hiện tại. */
const hasStaleSaved = computed(() => snapshot(state.saved) !== snapshot(state.rawSaved));

watch(
	isDirty,
	(dirty) => {
		emit('dirty-change', dirty);
	},
	{ immediate: true }
);

const normalizedCode = (row: VariableRow) => (row.code || '').trim().replace(/^@/, '');

const SQL_TOKEN_RE = /--[^\r\n]*|\/\*[\s\S]*?(?:\*\/|$)|N?'(?:''|[^'])*(?:'|$)|\[[^\]]*(?:\]|$)|"(?:""|[^"])*(?:"|$)|@@[A-Za-z0-9_$#@]*|@([A-Za-z][A-Za-z0-9_]{0,63})/g;

const extractSqlVariables = (sql?: string | null): string[] => {
	if (!sql) return [];
	const seen = new Set<string>();
	const result: string[] = [];
	for (const match of sql.matchAll(SQL_TOKEN_RE)) {
		const name = match[1];
		if (!name) continue;
		const lower = name.toLowerCase();
		if (seen.has(lower)) continue;
		seen.add(lower);
		result.push(name);
	}
	return result;
};

/**
 * Lược bỏ các dòng biến lưu cũ không còn xuất hiện trong câu SQL đã nạp
 * (ví dụ @lastTime còn sót trong VariablesJson sau khi câu SQL mẫu chung chuyển sang 1 cột __lastKey).
 */
const pruneOrphanRows = (rows: VariableRow[], sql?: string | null): VariableRow[] => {
	const trimmed = (sql ?? '').trim();
	if (!trimmed) return rows;
	const detected = extractSqlVariables(trimmed);
	if (detected.length === 0) {
		return rows.filter((r) => !/^lastTime(_|$)/i.test(normalizedCode(r)));
	}
	const detectedLower = new Set(detected.map((name) => name.toLowerCase()));
	return rows.filter((r) => detectedLower.has(normalizedCode(r).toLowerCase()));
};

let pendingInitialSqlSync = false;

const syncSavedRowsWithSql = (sql?: string | null) => {
	const synced = pruneOrphanRows(state.rawSaved, sql);
	state.saved = synced.map((r) => ({ ...r }));
	state.rows = synced.map((r) => ({ ...r }));
};

const applyResult = (items: any[]) => {
	state.rawSaved = (items || []).map(toRow);
	const trimmedSql = (props.sql ?? '').trim();
	pendingInitialSqlSync = !trimmedSql;
	syncSavedRowsWithSql(trimmedSql);
	state.showErrors = false;
};

watch(
	() => props.sql,
	(sql) => {
		if (!pendingInitialSqlSync) return;
		const trimmed = (sql ?? '').trim();
		if (!trimmed) return;
		pendingInitialSqlSync = false;
		if (!state.rows.some((r) => r.touched)) {
			syncSavedRowsWithSql(trimmed);
		}
	}
);

const isPageSize = (row: VariableRow): boolean => isPageSizeCode(normalizedCode(row));

/** @lastKey, @lastKey_<nhóm> đang lấy theo Mốc đang chạy: đổi sang giá trị cố định thì câu nối đuôi gửi lại cùng dữ liệu mỗi lượt, nên khóa nguồn. */
const isCursorVariable = (row: VariableRow): boolean => {
	const code = normalizedCode(row);
	return sourceKey(row) === 'lastKey' && (/^lastKey$/i.test(code) || Boolean(cursorGroupOf(code)));
};

const codeError = (row: VariableRow) => {
	const code = normalizedCode(row);
	if (!CODE_PATTERN.test(code)) return $t('lz.validation.sharedataSqlVariable.codeInvalid');
	const dup = state.rows.filter((r) => normalizedCode(r).toLowerCase() === code.toLowerCase()).length > 1;
	return dup ? $t('lz.validation.sharedataSqlVariable.codeDuplicate') : '';
};

const shownCodeError = (row: VariableRow) => (state.showErrors ? codeError(row) : '');

let loadPromise: Promise<void> | null = null;
let loadScope = '';
const load = async (silent: boolean | unknown = false): Promise<void> => {
	const isSilent = silent === true;
	const packetCode = props.packetCode;
	const partnerId = props.partnerId;
	const scope = `${packetCode ?? ''}|${partnerId ?? ''}`;
	// Cùng gói, cùng đối tác thì dùng lượt đang nạp; đổi tab giữa chừng thì nạp theo tab mới và bỏ kết quả lượt cũ
	if (loadPromise && loadScope === scope) return loadPromise;
	loadScope = scope;
	const run: Promise<void> = (async () => {
		if (!packetCode) {
			applyResult([]);
			return;
		}
		if (!isSilent) state.loading = true;
		try {
			const preloaded = partnerId ? null : (await takePacketSqlPreload(props.preload, packetCode, 'variables'))?.variables;
			if (loadScope !== scope) return;
			if (preloaded) {
				applyResult(preloaded.items ?? []);
				return;
			}
			const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetsqlvariablesGet(packetCode, partnerId || undefined);
			if (loadScope !== scope) return;
			applyResult(res.data?.result?.items ?? []);
		} finally {
			if (!isSilent && loadScope === scope) state.loading = false;
		}
	})().finally(() => {
		if (loadPromise === run) loadPromise = null;
	});
	loadPromise = run;
	return run;
};

watch(() => [props.packetCode, props.partnerId], () => { void load(); }, { immediate: true });

/**
 * Khối 4 vừa Đặt mốc hoặc Đặt lại: đọc lại đối tác đang mở đã có mốc hay chưa để khóa hoặc mở ô Mốc bắt đầu.
 * Chỉ đổi cờ đã có mốc nên phần đang sửa chưa lưu vẫn giữ; tab Mẫu chung không khóa ô nên không cần đọc.
 */
const refreshCursorState = async (): Promise<void> => {
	const packetCode = props.packetCode;
	const partnerId = props.partnerId;
	if (!packetCode || !partnerId) return;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetsqlvariablesGet(packetCode, partnerId);
		if (props.packetCode !== packetCode || props.partnerId !== partnerId) return;
		const cursorCodes = new Set(
			(res.data?.result?.items ?? []).filter((i: any) => (i.savedValue ?? '').trim()).map((i: any) => String(i.code ?? '').toLowerCase())
		);
		for (const row of [...state.rows, ...state.saved, ...state.rawSaved]) {
			row.hasCursor = cursorCodes.has(normalizedCode(row).toLowerCase());
		}
	} catch {
		// Lỗi đã được interceptor báo; giữ trạng thái khóa đang có
	}
};

const sourceKey = (row: VariableRow) => {
	if (row.valueKind === KIND_CONST) return 'const';
	if (row.valueKind === KIND_BUILTIN && SOURCES.includes(row.value) && row.value !== 'const') return row.value;
	return '';
};

const isConstValueValid = (row: VariableRow): boolean => {
	const val = (row.value ?? '').trim();
	if (isPageSize(row)) {
		const n = Number(val);
		return Number.isInteger(n) && n > 0 && n <= 100000;
	}
	return val.length > 0 && val.length <= 256;
};

const valueError = (row: VariableRow): string => {
	if (row.valueKind === KIND_CONST) {
		const val = (row.value ?? '').trim();
		if (!val) {
			return $t('lz.placeholder.sharedataSqlVariable.constValue');
		}
		if (isPageSize(row)) {
			const n = Number(val);
			if (!Number.isInteger(n) || n <= 0) {
				return $t('lz.validation.sharedataSqlVariable.pageSizeInvalid');
			}
		}
		if (!isConstValueValid(row)) {
			return $t('lz.validation.sharedataSqlVariable.constValueInvalid');
		}
	}
	// Mốc bắt đầu gõ tay: trống là gửi từ đầu; ô đang khóa (câu chưa đúng quy chuẩn mốc, đối tác đã có mốc) không sửa được nên không xét
	if (sourceKey(row) === 'lastKey' && !cursorSqlInvalid.value && !(props.partnerId && row.hasCursor)) {
		const start = (row.defaultValue ?? '').trim();
		if (start && !isValidCursorValue(start)) {
			return $t('lz.validation.sharedataSqlVariable.cursorValueInvalid');
		}
	}
	return '';
};

const shownValueError = (row: VariableRow): string => (state.showErrors ? valueError(row) : '');

/** Câu có cột __lastKey là câu nối đuôi, kể cả khi lọc bằng biến tên khác @lastKey. */
const hasCursorColumn = computed(() => /\bAS\s+\[?__lastKey(_[A-Za-z0-9_]+)?\]?(?![A-Za-z0-9_])/i.test(cleanSql.value));
const hasCursorVars = computed(() => hasCursorColumn.value || state.rows.some((r) => sourceKey(r) === 'lastKey'));

/** Câu đang soạn đã bỏ chú thích, giữ nguyên chuỗi như '_' để đọc công thức cột mốc. */
const sqlNoComments = computed(() => (props.sql ?? '').replace(/--[^\r\n]*/g, '').replace(/\/\*[\s\S]*?(?:\*\/|$)/g, ''));

const cleanSql = computed(() => sqlNoComments.value.replace(/N?'(?:''|[^'])*(?:'|$)/g, ''));

/** Biến mốc đang sửa gom theo nhóm mốc (nhóm mặc định và các nhóm có tên của câu gộp nhiều nguồn). */
const cursorSections = computed(() => buildCursorSections(state.rows.map((r) => ({ code: normalizedCode(r), source: sourceKey(r) }))));

/** Cột mốc câu SQL phải trả: mỗi nhóm có __lastKey[_<nhóm>]. */
const expectedCursorColumns = computed(() => cursorSections.value.map((section) => cursorColumn('__lastKey', section)));
const expectedCursorColumnsText = computed(() => expectedCursorColumns.value.join(', ') || '__lastKey');

/** Cột mốc có trong câu: đặt bí danh (AS __lastKey) hoặc lấy từ bảng dẫn xuất (u.__lastKey_<nhóm>); __lastKey viết trong SELECT thì phải đúng công thức PA1. */
const checkCursorColumns = computed(() => {
	const sql = cleanSql.value;
	const hasColumns = expectedCursorColumns.value.every((column) => new RegExp(`(?:\\bAS\\s+|\\.)\\[?${column}\\]?(?![A-Za-z0-9_])`, 'i').test(sql));
	if (!hasColumns) return false;
	const keyExpr = selectColumnExpression(sql, '__lastKey');
	// Mốc theo UpdateTime trơn: dòng mới thêm chưa có UpdateTime nên không được gửi
	if (hasBareUpdateTime(keyExpr)) return false;
	return !keyExpr || isCursorFormula(selectColumnExpression(sqlNoComments.value, '__lastKey'));
});

const cursorFilterVarRows = computed(() => state.rows.filter((r) => sourceKey(r) === 'lastKey'));
const cursorFilterVarNamesText = computed(() => cursorFilterVarRows.value.map((r) => `@${normalizedCode(r)}`).join(', ') || '@lastKey');

const checkCursorFilter = computed(() => {
	const sql = cleanSql.value;
	// @lastTime khi __lastKey đã ghép thời gian là điều kiện riêng của người dùng, không tính là mốc
	if (/@lastTime/i.test(sql) && !checkCursorColumns.value) return false;
	if (cursorFilterVarRows.value.length === 0) return !hasCursorColumn.value;
	return cursorFilterVarRows.value.every((r) => {
		const code = normalizedCode(r);
		if (!code) return false;
		const re = new RegExp(`@${code}\\b`, 'i');
		return re.test(sql);
	});
});

const checkOrderAsc = computed(() => {
	const sql = cleanSql.value;
	const lastOrderIdx = sql.toUpperCase().lastIndexOf('ORDER BY');
	if (lastOrderIdx === -1) return false;
	const afterOrder = sql.slice(lastOrderIdx);
	if (/\bDESC\b/i.test(afterOrder)) return false;
	// Nhóm mốc mặc định: dòng cuối trang phải là mốc lớn nhất nên phải sắp theo cột __lastKey hoặc chính biểu thức của cột đó
	if (expectedCursorColumns.value.length > 0 && !expectedCursorColumns.value.includes('__lastKey')) return true;
	const firstOrder = afterOrder.replace(/^ORDER BY/i, '').split(',')[0].replace(/\bASC\b/i, '').replace(/\s+/g, ' ').trim();
	return /(^|\.)\[?__lastKey\]?$/i.test(firstOrder) || firstOrder === selectColumnExpression(sql, '__lastKey');
});

watch(
	() => state.rows,
	(rows) => {
		const items = rows
			.filter((r) => CODE_PATTERN.test(normalizedCode(r)))
			.map((r) => {
				const key = sourceKey(r);
				const source = key ? $t(`lz.label.sharedataSqlVariable.source_${key}`) : '';
				return { code: normalizedCode(r), desc: [source, r.remark].filter(Boolean).join(' — ') };
			});
		emit('variables-change', items);
	},
	{ deep: true, immediate: true }
);

/** Bộ biến gửi lên BE (lưu riêng hoặc gửi kèm câu SQL); còn dòng lỗi thì hiện lỗi, báo và trả về null. */
const buildSaveItems = (): PacketSqlVariableItemInput[] | null => {
	if (state.rows.some((r) => Boolean(codeError(r)) || Boolean(valueError(r)))) {
		state.showErrors = true;
		collapsed.value = false;
		ElMessage.warning($t(state.rows.some(isRowRemovedByFix) ? 'lz.message.sharedataSqlVariable.cursorNeedsFix' : 'lz.message.sharedataSqlVariable.fixRows'));
		return null;
	}
	return state.rows.map((r) => {
		const isConst = r.valueKind === KIND_CONST;
		const isCursor = !isConst && r.value === 'lastKey';
		const trimmedConst = (r.value ?? '').trim();
		const trimmedDefault = (r.defaultValue ?? '').trim();
		return {
			code: normalizedCode(r),
			name: r.name,
			dataType: isConst ? inferConstDataType(trimmedConst) : (BUILTIN_TYPES[r.value] ?? r.dataType),
			valueKind: isConst ? KIND_CONST : KIND_BUILTIN,
			value: isConst ? trimmedConst : r.value,
			defaultValue: isCursor ? (trimmedDefault || null) : null,
			remark: r.remark,
		};
	});
};

/** Lưu bộ biến của câu SQL; trả về true khi lưu được (BE báo lỗi thì interceptor đã hiện thông báo). */
const save = async (): Promise<boolean> => {
	if (!props.packetCode || scopeLocked.value) return false;
	const items = buildSaveItems();
	if (!items) return false;
	state.saving = true;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdatesqlvariablesPost({
			packetCode: props.packetCode,
			partnerId: props.partnerId || null,
			items,
		});
		applyResult(res.data?.result?.items ?? []);
		return true;
	} catch {
		return false;
	} finally {
		state.saving = false;
	}
};

/** Nút Lưu của trang cha gọi khi chỉ bảng biến đổi: lưu bảng biến nếu có thay đổi (đổi cả câu SQL thì bộ biến gửi kèm câu SQL). */
const saveIfDirty = async (): Promise<boolean> => (isDirty.value ? save() : true);

const buildDetectedRow = (name: string): VariableRow => {
	// @lastKey_<nhóm> là biến mốc của một nhóm: nguồn theo tiền tố, giống @lastKey
	const groupSource = cursorGroupOf(name) ? 'lastKey' : null;
	const matchedSource = groupSource ?? SOURCES.find((s) => s !== 'const' && s.toLowerCase() === name.toLowerCase());
	if (matchedSource) {
		return {
			code: name,
			name: null,
			dataType: BUILTIN_TYPES[matchedSource] ?? TYPE_STRING,
			valueKind: KIND_BUILTIN,
			value: matchedSource,
			defaultValue: '',
			remark: null,
			autoDetected: true,
			touched: false,
		};
	}
	if (isPageSizeCode(name)) {
		return {
			code: name,
			name: null,
			dataType: TYPE_INT,
			valueKind: KIND_CONST,
			value: DEFAULT_PAGE_SIZE,
			defaultValue: '',
			remark: null,
			autoDetected: true,
			touched: false,
		};
	}
	return {
		code: name,
		name: null,
		dataType: TYPE_STRING,
		valueKind: KIND_CONST,
		value: '',
		defaultValue: '',
		remark: null,
		autoDetected: true,
		touched: false,
	};
};

/**
 * Phân tích tham số khi bấm "Lưu & phân tích" (giống wireframe, không tự dò lúc gõ):
 * giữ các dòng còn dùng trong câu SQL (giữ nguyên cấu hình), thêm dòng cho @biến mới, bỏ dòng không còn trong câu SQL.
 * Thứ tự theo thứ tự xuất hiện trong câu SQL. Trả về các biến vừa thêm / vừa bỏ.
 */
const analyzeFromSql = (sql?: string | null): { added: string[]; removed: string[] } => {
	if (!props.packetCode || sql == null) return { added: [], removed: [] };
	const detected = extractSqlVariables(sql);
	const byCode = new Map(state.rows.map((r) => [normalizedCode(r).toLowerCase(), r]));
	const added: string[] = [];
	const next = detected.map((name) => {
		const old = byCode.get(name.toLowerCase());
		if (old) return old;
		added.push(name);
		return { ...buildDetectedRow(name), touched: true };
	});
	const detectedLower = new Set(detected.map((c) => c.toLowerCase()));
	const removed = state.rows.map((r) => normalizedCode(r)).filter((c) => c && !detectedLower.has(c.toLowerCase()));
	state.rows = next;
	return { added, removed };
};

/** Nạp danh sách biến từ nguồn khác ở trạng thái chưa lưu (khi thiết lập SQL riêng sao chép từ đối tác khác). */
const setRows = (items: any[]) => {
	// Mốc của đối tác nguồn không phải của đối tác đang mở
	state.rows = (items || []).map((item) => ({ ...toRow(item), hasCursor: false, touched: true }));
	state.showErrors = false;
};

const commonLoading = ref(false);
const loading = computed(() => state.loading || commonLoading.value);

/* So với mẫu (tab đối tác): bộ biến mẫu chung nạp kèm khi có partnerId */
type VariableCompareState = 'same' | 'new' | 'missing' | 'diff';
const commonRows = ref<VariableRow[]>([]);
let commonPromise: Promise<void> | null = null;
const loadCommonRows = async (silent: boolean | unknown = false): Promise<void> => {
	const isSilent = silent === true;
	if (commonPromise) return commonPromise;
	commonPromise = (async () => {
		if (!props.packetCode || !props.partnerId) {
			commonRows.value = [];
			return;
		}
		if (!isSilent) {
			commonRows.value = [];
			commonLoading.value = true;
		}
		const packetCode = props.packetCode;
		try {
			const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetsqlvariablesGet(packetCode);
			if (props.packetCode === packetCode) {
				const items = res.data?.result?.items ?? [];
				commonRows.value = items.map(toRow);
			}
		} catch {
			// Lỗi đã được interceptor báo; cột So với mẫu hiện như biến mới
		} finally {
			if (!isSilent) commonLoading.value = false;
		}
	})().finally(() => {
		commonPromise = null;
	});
	return commonPromise;
};
watch(
	() => [props.packetCode, props.partnerId],
	() => { void loadCommonRows(); },
	{ immediate: true }
);
const findCommon = (row: VariableRow) => {
	const byCode = commonRows.value.find((c) => normalizedCode(c).toLowerCase() === normalizedCode(row).toLowerCase());
	if (byCode) return byCode;
	if (isPageSize(row)) return commonRows.value.find(isPageSize);
	return undefined;
};
const compareDiffs = (row: VariableRow): string[] => {
	const common = findCommon(row);
	if (!common) return [];
	const diffs: string[] = [];
	if (sourceKey(common) !== sourceKey(row)) diffs.push($t('lz.label.sharedataSqlVariable.colSource'));
	if (sourceKey(row) === 'const' && (common.value ?? '').trim() !== (row.value ?? '').trim()) diffs.push($t('lz.label.sharedataSqlVariable.colValue'));
	if (sourceKey(row) === 'lastKey' && (common.defaultValue ?? '').trim() !== (row.defaultValue ?? '').trim()) diffs.push($t('lz.label.sharedataSqlVariable.initialSeedLabel').replace(/:$/, ''));
	return diffs;
};
const compareState = (row: VariableRow): VariableCompareState => {
	if (!findCommon(row)) return 'new';
	return compareDiffs(row).length ? 'diff' : 'same';
};
const compareSymbol = (st: VariableCompareState) => (st === 'same' ? '=' : st === 'new' ? '+' : st === 'missing' ? '-' : '≠');
const compareTooltip = (row: VariableRow): string => {
	const st = compareState(row);
	const label = $t(`lz.label.sharedataDataSource.compareState_${st}`);
	const diffs = compareDiffs(row);
	return diffs.length ? `${label}: ${diffs.join(', ')}` : label;
};

/** Nạp toàn bộ dữ liệu của panel theo chế độ hiện tại (tab đối tác nạp thêm bộ biến mẫu chung để so) */
const loadAll = async (silent: boolean | unknown = false): Promise<void> => {
	const isSilent = silent === true;
	// Trang vừa đổi tab: chờ panel nhận đối tác mới rồi mới nạp, không gọi API theo đối tác của tab trước
	await nextTick();
	if (props.partnerId) {
		await Promise.all([load(isSilent), loadCommonRows(isSilent)]);
	} else {
		await load(isSilent);
	}
};

const totalChecksCount = 3;

const passedChecksCount = computed(() => [checkCursorColumns.value, checkCursorFilter.value, checkOrderAsc.value].filter(Boolean).length);

const allChecksPass = computed(() => passedChecksCount.value === totalChecksCount);

/** Công thức cột mốc gợi ý sửa: giữ phần thời gian và đuôi người viết đã có khi nhận ra được, lấy cột thật từ câu đang soạn, không thì để chỗ trống cho người dùng điền. */
const suggestedFormula = computed(() => {
	const sql = sqlNoComments.value;
	const column = expectedCursorColumns.value[0] ?? '__lastKey';
	const keyExpr = selectColumnExpression(sql, column);
	const table = firstTableAlias(sql);
	const idColumn = table ? `${table}.ID` : $t('lz.label.sharedataSqlVariable.formulaKeyColumn');
	// Phần thời gian ghép kiểu khác đổi sang công thức đúng; đuôi có thể NULL thì bọc ISNULL, chưa có đuôi thì điền cột ID
	const formula = keyExpr ? withTimeKeyFormula(keyExpr) : '';
	if (hasTimeKeyFormula(formula)) return withNonNullKeyTail(withCreateTimeInFormula(withCreateTimeFallback(formula), table ? `${table}.CreateTime` : 'CreateTime'), idColumn);
	// Cột mốc chỉ là UpdateTime: dùng làm phần thời gian và ghép thêm khóa
	const keyTimeOnly = keyExpr && hasBareUpdateTime(keyExpr) ? keyExpr : null;
	const timeExpr = selectColumnExpression(sql, '__lastTime') ?? keyTimeOnly;
	const timeColumn = withCreateTimeFallback(timeExpr || (table ? `${table}.UpdateTime` : $t('lz.label.sharedataSqlVariable.formulaTimeColumn')));
	// Giữ cột ID người dùng đã viết (x.ID); cột khác có thể trống nên lấy ID của bảng
	const plainId = keyExpr && /^(?:\[?[A-Za-z_]\w*\]?\.)?\[?ID\]?$/i.test(keyExpr) ? keyExpr : null;
	return `CONVERT(varchar(8), ${timeColumn}, 112) + REPLACE(CONVERT(varchar(12), ${timeColumn}, 114), ':', '') + '_' + ${plainId || idColumn}`;
});

/** Ba mục quy chuẩn mốc; mục chưa đạt kèm việc cần làm và đoạn SQL để sao chép. */
const cursorChecks = computed(() => {
	const column = expectedCursorColumns.value[0] ?? '__lastKey';
	const variable = cursorFilterVarNamesText.value.split(', ')[0];
	const keyExpr = selectColumnExpression(cleanSql.value, column);
	return [
		{
			key: 'columns' as CursorFixKind,
			pass: checkCursorColumns.value,
			label: $t('lz.label.sharedataSqlVariable.checkCursorColumns', { columns: expectedCursorColumnsText.value }),
			fixText: $t(
				hasBareUpdateTime(keyExpr)
					? 'lz.label.sharedataSqlVariable.checkCursorColumnsFixEmptyTime'
					: keyExpr
						? 'lz.label.sharedataSqlVariable.checkCursorColumnsFixReplace'
						: 'lz.label.sharedataSqlVariable.checkCursorColumnsFix'
			),
			fixCode: `${suggestedFormula.value} AS ${column}`,
		},
		{
			key: 'filter' as CursorFixKind,
			pass: checkCursorFilter.value,
			label: $t('lz.label.sharedataSqlVariable.checkCursorFilter', { names: cursorFilterVarNamesText.value }),
			fixText: $t('lz.label.sharedataSqlVariable.checkCursorFilterFix'),
			fixCode: `WHERE ${suggestedFormula.value} > ${variable}`,
		},
		{
			key: 'order' as CursorFixKind,
			pass: checkOrderAsc.value,
			label: $t('lz.label.sharedataSqlVariable.checkOrderAsc'),
			fixText: $t('lz.label.sharedataSqlVariable.checkOrderAscFix'),
			fixCode: `ORDER BY ${column} ASC`,
		},
	];
});

const failedChecks = computed(() => cursorChecks.value.filter((check) => !check.pass));

/** Chỉ tự sửa câu một nguồn khi đang được phép sửa; câu gộp nhiều nguồn mỗi nhánh một công thức nên chỉ sao chép. */
const canAutoFix = computed(() => !scopeLocked.value && expectedCursorColumns.value.length <= 1 && (expectedCursorColumns.value[0] ?? '__lastKey') === '__lastKey' && !hasTopLevelUnion(props.sql ?? ''));

/** Sửa các mục đã chọn trong câu đang soạn rồi dò lại bảng tham số; công thức giữ nguyên qua các bước để WHERE khớp cột mốc. */
const applyFixes = (kinds: CursorFixKind[]) => {
	const formula = suggestedFormula.value;
	// Đổi công thức cột mốc thì WHERE đổi theo để so đúng biểu thức mới
	const steps: CursorFixKind[] = kinds.includes('columns') && !kinds.includes('filter') ? [...kinds, 'filter'] : kinds;
	const fixed = steps.reduce((sql, kind) => applyCursorFix(sql, kind, formula), props.sql ?? '');
	emit('apply-sql', fixed);
	analyzeFromSql(fixed);
	if (kinds.includes('filter')) {
		for (const r of state.rows) {
			const code = normalizedCode(r);
			if (/^lastKey(_|$)/i.test(code) && sourceKey(r) !== 'lastKey') {
				onSourceChange(r, 'lastKey');
			}
		}
	}
};

/** Câu chưa đúng quy chuẩn mốc: chưa chọn được ngày cho tới khi sửa câu. */
const cursorSqlInvalid = computed(() => hasCursorVars.value && !allChecksPass.value);

/** Biến @lastTime khi __lastKey chưa ghép thời gian: sửa câu xong dòng tự bỏ nên không nhập giá trị. */
const isRowRemovedByFix = (row: VariableRow) =>
	/^lastTime(_|$)/i.test(normalizedCode(row)) && !checkCursorColumns.value && sourceKey(row) === 'const' && !(row.value ?? '').trim();

/** Câu có cột __lastKey mà không lọc theo biến mốc: máy chủ coi là gửi toàn bộ, mốc không tiến và gửi lặp nên chặn trước khi lưu. */
const warnIfCursorVarMissing = (): boolean => {
	if (!hasCursorColumn.value || cursorFilterVarRows.value.length > 0) return false;
	ElMessage.warning($t('lz.message.sharedataSqlVariable.cursorNeedsFix'));
	return true;
};

const onSourceChange = (row: VariableRow, key: string) => {
	row.touched = true;
	row.defaultValue = '';
	if (key === 'const') {
		row.valueKind = KIND_CONST;
		row.value = isPageSize(row) ? DEFAULT_PAGE_SIZE : '';
		row.dataType = isPageSize(row) ? TYPE_INT : TYPE_STRING;
	} else {
		row.valueKind = KIND_BUILTIN;
		row.value = key;
		row.dataType = BUILTIN_TYPES[key] ?? TYPE_STRING;
	}
};

const onStartKeyChange = (row: VariableRow, value: string) => {
	row.defaultValue = value;
	row.touched = true;
};

const pageSizeNumber = (row: VariableRow) => {
	const n = Number(row.value);
	return Number.isInteger(n) && n > 0 ? n : undefined;
};

const onPageSizeChange = (row: VariableRow, value: number | null | undefined) => {
	row.value = value == null ? '' : String(value);
	row.touched = true;
};

// Thu gọn bảng tham số giống khối câu truy vấn và trường dữ liệu: chỉ còn thanh tiêu đề, nhớ trạng thái giữa các lần mở
const VARIABLES_COLLAPSED_KEY = 'sharedataDataSource:variablesCollapsed';
const collapsed = ref(Local.get(VARIABLES_COLLAPSED_KEY) === true);
const toggleCollapsed = () => {
	collapsed.value = !collapsed.value;
	Local.set(VARIABLES_COLLAPSED_KEY, collapsed.value);
};

/** Câu SQL đang soạn có dùng mốc thì trang cha hiện khối 4 Mốc đang chạy (đặt cuối để các computed đã khai báo xong). */
watch(hasCursorVars, (value) => emit('cursor-vars-change', value), { immediate: true });

defineExpose({ reload: load, loadAll, isDirty, hasStaleSaved, saveIfDirty, buildSaveItems, loading, setRows, analyzeFromSql, warnIfCursorVarMissing, refreshCursorState });
</script>

<style scoped lang="scss">
/* Bóng đèn quy chuẩn mốc: chỉ sáng vàng khi đạt đủ mọi tiêu chí, chưa đủ thì tắt (xám) */
$svp-bulb-lit: #f5c400;

.sql-variable-panel {
	display: flex;
	flex-direction: column;
	gap: 12px;
	height: auto;
}

.svp-card {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 6px 8px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	background: var(--el-bg-color);
	box-sizing: border-box;
	overflow: hidden;

	&.is-scope-locked {
		background: var(--el-fill-color-lighter);

		.svp-table :deep(.el-table__body tr > td.el-table__cell) {
			background-color: var(--el-fill-color-light);
			color: var(--el-text-color-secondary);
		}
	}
}

.svp-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding-bottom: 6px;
	border-bottom: 1px solid var(--el-border-color-lighter);

	&.is-collapsed {
		padding-bottom: 0;
		border-bottom: none;
	}

	.svp-header-left {
		display: flex;
		flex: 1;
		align-items: center;
		gap: 6px;
		cursor: pointer;
		user-select: none;
	}

	.svp-title {
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	.svp-badge-count {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		padding: 1px 7px;
		border-radius: 10px;
		font-size: 12px;
		font-weight: 500;
		background: var(--el-fill-color-darker);
		color: var(--el-text-color-regular);
		line-height: 1.2;
	}

}

.svp-checks {
	display: inline-flex;
	align-items: center;
	font-size: 12px;
	line-height: 1;
	padding-left: 8px;
	border-left: 1px solid var(--el-border-color);

	.svp-check-lead {
		display: inline-flex;
		align-items: center;
		gap: 4px;
		font-weight: 500;
		cursor: pointer;
		padding: 2px 6px;
		border-radius: 4px;
		transition: background-color 0.2s;
		flex-shrink: 0;

		&:hover {
			background: var(--el-fill-color-light);
		}

		&.is-pass {
			color: var(--el-color-success);
			.svp-check-bulb {
				color: $svp-bulb-lit;
			}
		}

		&.is-fail,
		&.is-warn {
			color: var(--el-color-danger);
			.svp-check-bulb {
				color: var(--el-text-color-placeholder);
			}
		}

		.svp-check-bulb {
			font-size: 15px;
		}

		.svp-check-lead-count {
			font-weight: 600;
		}
	}

	.svp-check-icon {
		font-size: 13px;
		flex-shrink: 0;
	}
}

.svp-table {
	width: 100%;
	height: auto;

	:deep(.el-table__inner-wrapper) {
		height: auto;
	}

	:deep(.el-table__body-wrapper) {
		height: auto;
	}

	:deep(.el-scrollbar) {
		height: auto;
	}

	:deep(.el-scrollbar__wrap) {
		height: auto;
	}

	:deep(.el-table__header-wrapper th.el-table__cell) {
		background-image: linear-gradient(180deg, color-mix(in srgb, var(--el-color-primary) 78%, #0f172a) 0%, var(--el-color-primary) 100%);
		color: #ffffff;
		font-weight: 600;
		font-size: 12px;
		height: 32px;
		padding: 3px 0;
		border-right-color: rgba(255, 255, 255, 0.22);
	}

	:deep(.el-table__row) {
		transition: background-color 0.15s ease;

		&:hover > td.el-table__cell {
			background-color: var(--el-fill-color-lighter);
		}
	}

	:deep(.el-table__cell) {
		padding: 5px 0;
	}

	:deep(.el-table__empty-block) {
		min-height: 0;
	}

	:deep(.el-table__empty-text) {
		width: auto;
		line-height: 1.5;
		padding: 10px 12px;
		font-size: 12px;
	}
}

.svp-param-code-line {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	min-width: 0;
}

.svp-param-code {
	font-family: 'Fira Code', Consolas, Monaco, monospace;
	font-size: 12px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
}

.svp-source-cell {
	display: flex;
	align-items: center;
	width: 100%;
	padding: 0 6px;
}

.svp-source-select {
	width: 100%;

	:deep(.el-select__wrapper) {
		min-height: 28px;
		height: 28px;
		padding: 1px 8px;
		transition: border-color 0.15s ease, box-shadow 0.15s ease;
	}

	:deep(.el-select__selected-item) {
		font-size: 12px;
		font-weight: 500;
		color: var(--el-text-color-primary);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}
}

.svp-source-prefix {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 18px;
	height: 18px;
	border-radius: 4px;
	font-size: 12px;
	flex-shrink: 0;

	&.is-key {
		color: var(--el-color-warning-dark-2);
		background: var(--el-color-warning-light-9);
		border: 1px solid var(--el-color-warning-light-5);
	}

	&.is-const {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
		border: 1px solid var(--el-color-success-light-5);
	}
}

.svp-key-cursor-wrap {
	display: flex;
	align-items: center;
	width: 100%;
	padding: 0 4px;
}

/* Tiêu đề cột So mẫu chung nằm trên một dòng */
:deep(.svp-nowrap-head .cell) {
	white-space: nowrap;
	padding: 0 4px;
}

.svp-key-input {
	flex: 1 1 auto;
	width: 100%;
}


.svp-val-const-block {
	display: flex;
	flex-direction: column;
	gap: 2px;
	width: 100%;
	padding: 0 4px;
}

.svp-const-input-row {
	display: flex;
	align-items: center;
	width: 100%;
}

.svp-const-input {
	width: 100%;

	:deep(.el-input__wrapper) {
		min-height: 28px;
		height: 28px;
	}
}

.svp-page-size-input {
	:deep(.el-input__inner) {
		text-align: left;
		padding-left: 10px;
	}
}

.svp-empty {
	color: var(--el-text-color-placeholder);
	padding: 0 4px;
}

.svp-error {
	margin-top: 2px;
	font-size: 11px;
	color: var(--el-color-danger);
}

.is-error :deep(.el-input__wrapper) {
	box-shadow: 0 0 0 1px var(--el-color-danger) inset;
}

.is-error :deep(.el-input__inner::placeholder) {
	color: var(--el-color-danger);
}

.svp-param-code-line.is-error .svp-param-code {
	color: var(--el-color-danger);
	border-color: var(--el-color-danger-light-5);
	background: var(--el-color-danger-light-9);
}

.cmp-st {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	border-radius: 50%;
	border: 1px solid;
	font-weight: 700;
	font-size: 12px;
	line-height: 1;

	&.same {
		color: var(--el-color-success);
		background: var(--el-color-success-light-9);
		border-color: var(--el-color-success-light-5);
	}

	&.new {
		color: var(--el-color-primary);
		background: var(--el-color-primary-light-9);
		border-color: var(--el-color-primary-light-5);
	}

	&.missing {
		color: var(--el-color-danger);
		background: var(--el-color-danger-light-9);
		border-color: var(--el-color-danger-light-5);
	}

	&.diff {
		color: var(--el-color-warning-dark-2);
		background: var(--el-color-warning-light-9);
		border-color: var(--el-color-warning-light-5);
	}
}

:global(.svp-check-popover) {
	max-width: min(380px, 28vw);
}

:global(.svp-check-popover .svp-popover-content) {
	font-size: 12px;
	line-height: 1.5;
}

:global(.svp-check-popover .svp-popover-title) {
	display: flex;
	align-items: center;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin-bottom: 8px;
	padding-bottom: 6px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	white-space: nowrap;
}

:global(.svp-check-popover .svp-popover-list) {
	display: flex;
	flex-direction: column;
	gap: 6px;
	margin-bottom: 8px;
}

:global(.svp-check-popover .svp-popover-item) {
	display: flex;
	align-items: flex-start;
	gap: 6px;
	font-size: 12px;
	line-height: 1.4;
}

:global(.svp-check-popover .svp-check-icon) {
	font-size: 14px;
	flex-shrink: 0;
	margin-top: 1px;
}

:global(.svp-check-popover .svp-check-body) {
	flex: 1;
	min-width: 0;
}

:global(.svp-check-popover .svp-fix-text) {
	margin-top: 3px;
	color: var(--el-text-color-regular);
}

:global(.svp-check-popover .svp-fix-code) {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 8px;
	margin-top: 3px;
	padding: 4px 8px;
	color: var(--el-text-color-primary);
	background: var(--el-fill-color-light);
	border-radius: 4px;

	code {
		min-width: 0;
		font-size: 12px;
		word-break: break-word;
	}
}

:global(.svp-check-popover .svp-fix-copy),
:global(.svp-check-popover .svp-fix-apply) {
	flex-shrink: 0;
}

:global(.svp-check-popover .svp-fix-all) {
	margin-left: auto;
}

:global(.svp-check-popover .svp-popover-item.is-pass) {
	color: var(--el-color-success);

	.svp-check-icon {
		color: var(--el-color-success);
	}
}

:global(.svp-check-popover .svp-popover-item.is-fail),
:global(.svp-check-popover .svp-popover-item.is-warn) {
	color: var(--el-color-danger);

	.svp-check-icon {
		color: var(--el-color-danger);
	}
}

:global(.svp-check-popover .svp-check-bulb.is-lit) {
	color: $svp-bulb-lit;
}

:global(.svp-check-popover .svp-check-bulb.is-unlit) {
	color: var(--el-text-color-placeholder);
}

:global(.svp-source-popper .el-select-dropdown__item) {
	height: auto;
	min-height: 44px;
	padding: 6px 12px;
	line-height: 1.35;
}

:global(.svp-source-popper .svp-source-opt) {
	display: flex;
	align-items: flex-start;
	gap: 8px;
	width: 100%;
}

:global(.svp-source-popper .svp-source-opt-icon) {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	border-radius: 6px;
	font-size: 13px;
	flex-shrink: 0;
	margin-top: 1px;
}

/* Màu icon trong danh sách chọn trùng với icon ở ô đã chọn: key cam, giá trị nhập xanh */
:global(.svp-source-popper .svp-source-opt-icon.is-key) {
	color: var(--el-color-warning-dark-2);
	background: var(--el-color-warning-light-9);
	border: 1px solid var(--el-color-warning-light-5);
}

:global(.svp-source-popper .svp-source-opt-icon.is-const) {
	color: var(--el-color-success);
	background: var(--el-color-success-light-9);
	border: 1px solid var(--el-color-success-light-5);
}

:global(.svp-source-popper .svp-source-opt-body) {
	display: flex;
	flex-direction: column;
	gap: 2px;
	min-width: 0;
	flex: 1;
}

:global(.svp-source-popper .svp-source-opt-head) {
	display: flex;
	align-items: center;
	gap: 6px;
}

:global(.svp-source-popper .svp-source-opt-title) {
	font-size: 12.5px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

:global(.svp-source-popper .svp-source-opt-desc) {
	font-size: 11.5px;
	color: var(--el-text-color-secondary);
	white-space: normal;
	line-height: 1.3;
}

</style>

<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.el-popper.sql-variable-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}
</style>
