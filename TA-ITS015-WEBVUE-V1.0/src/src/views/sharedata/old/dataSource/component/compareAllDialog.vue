<template>
	<!-- So sánh mẫu chung với tất cả đối tác có SQL riêng (theo wireframe sharedata-datasource-doi-tac.html):
	     tab Tổng quan là ma trận câu truy vấn / trường / tham số × đối tác; mỗi đối tác một tab xem chi tiết. -->
	<el-dialog
		v-model="visible"
		width="min(1100px, 90vw)"
		align-center
		destroy-on-close
		draggable
		class="compare-all-dialog"
	>
		<template #header>
			<div class="cad-header">
				<el-icon class="mr5"><ele-Operation /></el-icon>
				<span>{{ $t('lz.title.sharedataDataSource.compareAll', { packet: props.packetCode || '' }) }}</span>
			</div>
		</template>

		<div v-loading="loading" class="cad-body">
			<el-tabs v-model="activeTab" class="cad-tabs">
				<el-tab-pane name="overview" :label="$t('lz.label.sharedataDataSource.compareOverviewTab')" />
				<el-tab-pane v-for="p in customPartners" :key="p.partnerId" :name="p.partnerId">
					<template #label>
						<span>{{ p.partnerCode || p.partnerName }}</span>
						<span v-if="partnerHasDiff(p.partnerId)" class="cad-dot" />
					</template>
				</el-tab-pane>
			</el-tabs>

			<!-- Chú giải ký hiệu: tab Tổng quan hiện ở đây; tab đối tác hiện ở tiêu đề khối Trường gói tin -->
			<div v-if="activeTab === 'overview'" class="cad-toolbar">
				<div class="cad-legend">
					<span v-for="st in LEGEND" :key="st" class="cad-legend-item">
						<span class="cmp-st" :class="st">{{ stateSymbol(st) }}</span>
						{{ $t(`lz.label.sharedataDataSource.compareState_${st}`) }}
					</span>
				</div>
				<span class="cad-grow" />
				<el-checkbox v-model="onlyDiff">{{ $t('lz.label.sharedataDataSource.onlyShowDiffRows') }}</el-checkbox>
			</div>

			<!-- Tổng quan -->
			<template v-if="activeTab === 'overview'">

				<!-- Bảng tổng quan không kéo dài popup: tối đa ~45% chiều cao màn hình, còn lại cuộn trong bảng -->
				<el-table :data="filteredRows" size="small" border :max-height="overviewMaxHeight" class="cad-table" :row-class-name="overviewRowClass">
					<el-table-column :label="$t('lz.label.sharedataDataSource.compareColKey')" width="180" fixed="left" show-overflow-tooltip header-align="center">
						<template #default="{ row }">
							<span v-if="row.kind === 'section'" class="cad-section-label">{{ row.label }}</span>
							<span v-else-if="row.kind === 'sql'" class="cad-sql-row-label">{{ row.label }}</span>
							<span v-else class="cad-field-key">{{ row.key }}</span>
						</template>
					</el-table-column>

					<el-table-column :label="$t('lz.label.sharedataDataSource.tabStandardSql')" min-width="220" header-align="center">
						<template #default="{ row }">
							<span class="cad-std-spec">{{ row.standardSpec }}</span>
						</template>
					</el-table-column>

					<el-table-column v-for="p in customPartners" :key="p.partnerId" min-width="150" align="center" header-align="center">
						<template #header>
							<el-button text type="primary" size="small" class="cad-partner-name" :title="p.partnerName" @click="activeTab = p.partnerId">
								{{ p.partnerCode || p.partnerName }}
							</el-button>
						</template>
						<template #default="{ row }">
							<template v-if="row.kind === 'sql'">
								<el-tag
									v-if="sqlHunkCount(p.partnerId)"
									size="small"
									effect="plain"
									class="cad-hunk-tag"
								>
									{{ $t('lz.label.sharedataDataSource.sqlHunkCount', { count: sqlHunkCount(p.partnerId) }) }}
								</el-tag>
								<el-tag
									v-else
									size="small"
									type="success"
									effect="plain"
								>
									{{ $t('lz.label.sharedataDataSource.compareState_same') }}
								</el-tag>
							</template>
							<template v-else-if="row.kind !== 'section' && row.cells[p.partnerId]">
								<el-tooltip :content="$t(`lz.label.sharedataDataSource.compareState_${row.cells[p.partnerId].state}`) + (row.cells[p.partnerId].tooltip ? ': ' + row.cells[p.partnerId].tooltip : '')" placement="top">
									<span class="cmp-st" :class="row.cells[p.partnerId].state">{{ stateSymbol(row.cells[p.partnerId].state) }}</span>
								</el-tooltip>
							</template>
							<template v-else-if="row.kind !== 'section'">
								<span class="cad-empty">·</span>
							</template>
						</template>
					</el-table-column>

					<el-table-column v-if="customPartners.length === 0" min-width="200" header-align="center">
						<template #default>
							<span class="cad-empty">{{ $t('lz.label.sharedataDataSource.compareNoCustomPartner') }}</span>
						</template>
					</el-table-column>
				</el-table>

				<p v-if="templatePartners.length" class="cad-note">
					{{ $t('lz.label.sharedataDataSource.compareTemplateUsers', { names: templatePartners.map((p) => p.partnerCode || p.partnerName).join(', ') }) }}
				</p>
			</template>

			<!-- Chi tiết một đối tác -->
			<template v-else-if="activePartner">
				<div class="cad-toolbar">
					<el-tag size="small" type="primary" effect="plain">{{ $t('lz.label.sharedataDataSource.tabStandardSql') }}</el-tag>
					<span>⇄</span>
					<el-tag size="small" type="warning" effect="plain">{{ activePartner.partnerCode || activePartner.partnerName }}</el-tag>
					<span class="cad-note-inline">{{ $t('lz.label.sharedataDataSource.compareIgnoreWhitespace') }}</span>
					<span class="cad-grow" />
					<el-checkbox v-model="onlyDiff">{{ $t('lz.label.sharedataDataSource.onlyShowDiffRows') }}</el-checkbox>
					<el-button size="small" type="primary" plain @click="emit('open-diff', activePartner.partnerId)">
						{{ $t('lz.button.sharedataDataSource.viewDiffDetail') }}
					</el-button>
					<el-button size="small" @click="emit('open-tab', activePartner.partnerId)">
						{{ $t('lz.button.sharedataDataSource.openPartnerTab', { partner: activePartner.partnerCode || activePartner.partnerName }) }}
					</el-button>
				</div>

				<h4 class="cad-subtitle">
					{{ $t('lz.label.sharedataDataSource.compareSqlRow') }}
					<el-tag
						v-if="sqlHunkCount(activePartner.partnerId)"
						size="small"
						effect="plain"
						class="cad-hunk-tag"
					>
						{{ $t('lz.label.sharedataDataSource.sqlHunkCount', { count: sqlHunkCount(activePartner.partnerId) }) }}
					</el-tag>
					<el-tag
						v-else
						size="small"
						type="success"
						effect="plain"
					>
						{{ $t('lz.label.sharedataDataSource.compareState_same') }}
					</el-tag>
					<!-- Dòng khác biệt dùng màu chuẩn cho code diff (xanh lá/đỏ), không dùng icon hay màu của giống/mới/thiếu/khác -->
					<span v-if="sqlHunkCount(activePartner.partnerId)" class="cad-legend cad-diff-legend">
						<span class="cad-legend-item">
							<span class="cad-diff-swatch cad-diff-swatch-tpl" />
							{{ $t('lz.label.sharedataDataSource.compareDiffOnlyTemplate') }}
						</span>
						<span class="cad-legend-item">
							<span class="cad-diff-swatch cad-diff-swatch-ptn" />
							{{ $t('lz.label.sharedataDataSource.compareDiffOnlyPartner') }}
						</span>
					</span>
				</h4>
				<div class="cad-diff-box">
					<table class="cad-diff">
						<colgroup><col style="width: 38px" /><col /><col style="width: 38px" /><col /></colgroup>
						<thead>
							<tr>
								<th colspan="2">{{ $t('lz.label.sharedataDataSource.tabStandardSql') }}</th>
								<th colspan="2">{{ activePartner.partnerCode || activePartner.partnerName }}</th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="(line, i) in activeDiffLines" :key="i" :class="{ 'cad-hunk-row': line.hunk }">
								<template v-if="line.hunk">
									<td colspan="4">{{ $t('lz.label.sharedataDataSource.sqlHunkTitle', { no: line.hunk, left: line.left, right: line.right }) }}</td>
								</template>
								<template v-else>
									<td class="ln">{{ line.ln ?? '' }}</td>
									<td class="code" :class="line.lType">{{ line.lText }}</td>
									<td class="ln">{{ line.rn ?? '' }}</td>
									<td class="code" :class="line.rType">{{ line.rText }}</td>
								</template>
							</tr>
						</tbody>
					</table>
				</div>

				<h4 class="cad-subtitle">
					{{ $t('lz.label.sharedataDataSource.packetFieldTitle') }}
					<span class="cad-legend">
						<span v-for="st in LEGEND" :key="st" class="cad-legend-item">
							<span class="cmp-st" :class="st">{{ stateSymbol(st) }}</span>{{ $t(`lz.label.sharedataDataSource.compareState_${st}`) }}
						</span>
					</span>
				</h4>
				<el-table :data="filteredActiveFieldRows" size="small" border max-height="300" class="cad-table">
					<el-table-column :label="$t('lz.label.sharedataDataSource.fieldCompareColumn')" width="100" align="center" header-align="center">
						<template #default="{ row }">
							<el-tooltip :content="$t(`lz.label.sharedataDataSource.compareState_${row.state}`)" placement="top">
								<span class="cmp-st" :class="row.state">{{ stateSymbol(row.state) }}</span>
							</el-tooltip>
						</template>
					</el-table-column>
					<el-table-column :label="$t('lz.entity.sharedataDataSource.aliasFieldKey')" min-width="160" header-align="center">
						<template #default="{ row }"><span class="cad-field-key">{{ row.key }}</span></template>
					</el-table-column>
					<el-table-column :label="$t('lz.label.sharedataDataSource.tabStandardSql')" min-width="180" header-align="center">
						<template #default="{ row }">{{ formatFieldSpec(row.common) }}</template>
					</el-table-column>
					<el-table-column :label="activePartner.partnerCode || activePartner.partnerName" min-width="180" header-align="center">
						<template #default="{ row }">{{ formatFieldSpec(row.partner) }}</template>
					</el-table-column>
				</el-table>

				<h4 class="cad-subtitle">{{ $t('lz.label.sharedataDataSource.compareVariableSection') }}</h4>
				<el-table :data="filteredActiveVariableRows" size="small" border max-height="240" class="cad-table" :empty-text="$t('lz.label.sharedataDataSource.compareNoVariable')">
					<el-table-column :label="$t('lz.label.sharedataDataSource.fieldCompareColumn')" width="100" align="center" header-align="center">
						<template #default="{ row }">
							<el-tooltip :content="$t(`lz.label.sharedataDataSource.compareState_${row.state}`)" placement="top">
								<span class="cmp-st" :class="row.state">{{ stateSymbol(row.state) }}</span>
							</el-tooltip>
						</template>
					</el-table-column>
					<el-table-column :label="$t('lz.label.sharedataSqlVariable.colName')" min-width="140" header-align="center">
						<template #default="{ row }"><span class="cad-field-key">@{{ row.code }}</span></template>
					</el-table-column>
					<el-table-column :label="$t('lz.label.sharedataDataSource.tabStandardSql')" min-width="180" header-align="center">
						<template #default="{ row }">{{ formatVariableSpec(row.common) }}</template>
					</el-table-column>
					<el-table-column :label="activePartner.partnerCode || activePartner.partnerName" min-width="180" header-align="center">
						<template #default="{ row }">{{ formatVariableSpec(row.partner) }}</template>
					</el-table-column>
				</el-table>
			</template>
		</div>

		<template #footer>
			<span class="dialog-footer">
				<el-button @click="visible = false">{{ $t('lz.button.base.close') }}</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="sharedataCompareAllDialog">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import type { PacketSqlVariableItem, PartnerExtractSqlOutput, SqlFieldItem } from '/@/api-services/shareData/models';
import { compareFields, type FieldCompareRow, type FieldCompareState } from '/@/utils/sharedata/fieldCompare';

const { t: $t } = useI18n();

interface PartnerRow {
	partnerId: string;
	partnerCode?: string | null;
	partnerName: string;
	isCustom: boolean;
}

const props = defineProps<{
	modelValue: boolean;
	packetCode?: string | null;
	/** Mọi đối tác dùng gói: có SQL riêng thì so sánh, dùng thẳng mẫu chung thì chỉ liệt kê. */
	partners?: PartnerRow[] | null;
	commonFields?: SqlFieldItem[] | null;
	resetting?: boolean;
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', val: boolean): void;
	(e: 'open-diff', partnerId: string): void;
	(e: 'open-tab', partnerId: string): void;
	(e: 'reset-many', items: PartnerRow[]): void;
}>();

const visible = computed({
	get: () => props.modelValue,
	set: (val: boolean) => emit('update:modelValue', val),
});

const loading = ref(false);
const overviewMaxHeight = Math.max(260, Math.round(window.innerHeight * 0.45));
const onlyDiff = ref(false);
const activeTab = ref('overview');
const detailsMap = ref<Record<string, PartnerExtractSqlOutput>>({});
const commonFields = ref<SqlFieldItem[]>([]);
const commonVariables = ref<PacketSqlVariableItem[]>([]);
const partnerVariables = ref<Record<string, PacketSqlVariableItem[]>>({});

const customPartners = computed(() => (props.partners || []).filter((p) => p.isCustom));
const templatePartners = computed(() => (props.partners || []).filter((p) => !p.isCustom));
const activePartner = computed(() => customPartners.value.find((p) => p.partnerId === activeTab.value));

const loadVariables = async (partnerId?: string) => {
	const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetsqlvariablesGet(props.packetCode, partnerId);
	return res.data?.result?.items ?? [];
};

const loadAll = async () => {
	activeTab.value = 'overview';
	if (!props.packetCode || customPartners.value.length === 0) {
		detailsMap.value = {};
		commonFields.value = (props.commonFields ?? []).filter((f) => f?.key && !f.lost);
		return;
	}
	loading.value = true;
	try {
		const [entries, common, vars] = await Promise.all([
			Promise.all(
				customPartners.value.map(async (p) => {
					const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnerextractsqlGet(p.partnerId, props.packetCode);
					return [p.partnerId, res.data?.result] as const;
				})
			),
			loadVariables(),
			Promise.all(customPartners.value.map(async (p) => [p.partnerId, await loadVariables(p.partnerId)] as const)),
		]);
		const map: Record<string, PartnerExtractSqlOutput> = {};
		let foundCommon: SqlFieldItem[] = [];
		for (const [id, detail] of entries) {
			if (!detail) continue;
			map[id] = detail;
			if (foundCommon.length === 0 && detail.commonFields?.length) foundCommon = detail.commonFields;
		}
		detailsMap.value = map;
		commonFields.value = (foundCommon.length > 0 ? foundCommon : props.commonFields ?? []).filter((f) => f?.key && !f.lost);
		commonVariables.value = common;
		partnerVariables.value = Object.fromEntries(vars);
	} finally {
		loading.value = false;
	}
};

watch(
	() => [props.modelValue, customPartners.value.map((p) => p.partnerId).join(',')],
	([isOpen]) => {
		if (isOpen) void loadAll();
	},
	{ immediate: true }
);

/* ---------- So sánh câu SQL theo dòng (bỏ qua khác biệt khoảng trắng), gom thành các đoạn khác ---------- */
interface DiffOp {
	t: 'eq' | 'del' | 'add';
	a?: string;
	b?: string;
}

const normLine = (s: string) => s.trim().replace(/\s+/g, ' ');

const diffLines = (a: string[], b: string[]): DiffOp[] => {
	const n = a.length;
	const m = b.length;
	const dp: number[][] = Array.from({ length: n + 1 }, () => new Array(m + 1).fill(0));
	for (let i = n - 1; i >= 0; i--) {
		for (let j = m - 1; j >= 0; j--) {
			dp[i][j] = normLine(a[i]) === normLine(b[j]) ? dp[i + 1][j + 1] + 1 : Math.max(dp[i + 1][j], dp[i][j + 1]);
		}
	}
	const ops: DiffOp[] = [];
	let i = 0;
	let j = 0;
	while (i < n && j < m) {
		if (normLine(a[i]) === normLine(b[j])) ops.push({ t: 'eq', a: a[i++], b: b[j++] });
		else if (dp[i + 1][j] >= dp[i][j + 1]) ops.push({ t: 'del', a: a[i++] });
		else ops.push({ t: 'add', b: b[j++] });
	}
	while (i < n) ops.push({ t: 'del', a: a[i++] });
	while (j < m) ops.push({ t: 'add', b: b[j++] });
	return ops;
};

interface DiffLine {
	hunk?: number;
	left?: number;
	right?: number;
	ln?: number;
	rn?: number;
	lText?: string;
	rText?: string;
	lType?: string;
	rType?: string;
}

const toLines = (sql?: string | null) => (sql || '').replace(/\r\n?/g, '\n').trim().split('\n');

const buildDiff = (partnerId: string) => {
	const detail = detailsMap.value[partnerId];
	const ops = diffLines(toLines(detail?.standardSql), toLines(detail?.customSql));
	const lines: DiffLine[] = [];
	let li = 0;
	let ri = 0;
	let hunks = 0;
	let k = 0;
	while (k < ops.length) {
		if (ops[k].t === 'eq') {
			li++;
			ri++;
			lines.push({ ln: li, rn: ri, lText: ops[k].a, rText: ops[k].b });
			k++;
			continue;
		}
		const dels: string[] = [];
		const adds: string[] = [];
		while (k < ops.length && ops[k].t !== 'eq') {
			if (ops[k].t === 'del') dels.push(ops[k].a!);
			else adds.push(ops[k].b!);
			k++;
		}
		hunks++;
		lines.push({ hunk: hunks, left: dels.length, right: adds.length });
		for (let x = 0; x < Math.max(dels.length, adds.length); x++) {
			const l = x < dels.length ? { ln: ++li, lText: dels[x], lType: 'd-tpl' } : { lType: 'd-empty' };
			const r = x < adds.length ? { rn: ++ri, rText: adds[x], rType: 'd-ptn' } : { rType: 'd-empty' };
			lines.push({ ...l, ...r });
		}
	}
	return { lines, hunks };
};

const diffCache = computed(() => Object.fromEntries(customPartners.value.map((p) => [p.partnerId, buildDiff(p.partnerId)])));
const sqlHunkCount = (partnerId: string) => diffCache.value[partnerId]?.hunks ?? 0;
const activeDiffLines = computed(() => (activePartner.value ? diffCache.value[activePartner.value.partnerId]?.lines ?? [] : []));

/* ---------- So sánh trường và tham số ---------- */
const fieldCompareCache = computed(() =>
	Object.fromEntries(customPartners.value.map((p) => [p.partnerId, compareFields(commonFields.value, detailsMap.value[p.partnerId]?.fields)]))
);
const activeFieldRows = computed(() => (activePartner.value ? fieldCompareCache.value[activePartner.value.partnerId]?.rows ?? [] : []));
const filteredActiveFieldRows = computed(() => (onlyDiff.value ? activeFieldRows.value.filter((r) => r.state !== 'same') : activeFieldRows.value));

interface VariableCompareRow {
	code: string;
	state: FieldCompareState;
	common?: PacketSqlVariableItem;
	partner?: PacketSqlVariableItem;
	tooltip: string;
}

const variableSignature = (v?: PacketSqlVariableItem) =>
	v ? `${v.valueKind || ''}|${v.value || ''}|${v.dataType || ''}|${v.defaultValue || ''}`.toLowerCase() : '';

const compareVariables = (common: PacketSqlVariableItem[], partner: PacketSqlVariableItem[]): VariableCompareRow[] => {
	const pMap = new Map(partner.filter((v) => v.code).map((v) => [v.code!.toLowerCase(), v]));
	const rows: VariableCompareRow[] = [];
	const seen = new Set<string>();
	for (const c of common.filter((v) => v.code)) {
		const lower = c.code!.toLowerCase();
		seen.add(lower);
		const p = pMap.get(lower);
		const state: FieldCompareState = !p ? 'missing' : variableSignature(c) === variableSignature(p) ? 'same' : 'diff';
		rows.push({ code: c.code!, state, common: c, partner: p, tooltip: state === 'diff' ? `${formatVariableSpec(c)} → ${formatVariableSpec(p)}` : '' });
	}
	for (const p of partner.filter((v) => v.code && !seen.has(v.code.toLowerCase()))) {
		rows.push({ code: p.code!, state: 'new', partner: p, tooltip: formatVariableSpec(p) });
	}
	return rows;
};

const variableCompareCache = computed(() =>
	Object.fromEntries(customPartners.value.map((p) => [p.partnerId, compareVariables(commonVariables.value, partnerVariables.value[p.partnerId] ?? [])]))
);
const activeVariableRows = computed(() => (activePartner.value ? variableCompareCache.value[activePartner.value.partnerId] ?? [] : []));
const filteredActiveVariableRows = computed(() => (onlyDiff.value ? activeVariableRows.value.filter((r) => r.state !== 'same') : activeVariableRows.value));

const partnerHasDiff = (partnerId: string) =>
	sqlHunkCount(partnerId) > 0 ||
	(fieldCompareCache.value[partnerId]?.rows ?? []).some((r) => r.state !== 'same') ||
	(variableCompareCache.value[partnerId] ?? []).some((r) => r.state !== 'same');

/* ---------- Ma trận tổng quan ---------- */
interface MatrixCell {
	state: FieldCompareState;
	tooltip: string;
}

interface MatrixRow {
	id: string;
	kind: 'sql' | 'section' | 'field' | 'variable';
	key: string;
	label: string;
	standardSpec: string;
	hasDiff: boolean;
	cells: Record<string, MatrixCell>;
}

const formatFieldSpec = (f?: SqlFieldItem | null) => {
	if (!f) return '—';
	const parts = [f.label && f.label !== f.key ? f.label : '', f.type || 'string'];
	if (f.dbType) parts.push(`(${f.dbType})`);
	if (f.required) parts.push($t('lz.label.sharedataDataSource.required'));
	return parts.filter(Boolean).join(' · ');
};

const formatVariableSpec = (v?: PacketSqlVariableItem | null) => {
	if (!v) return '—';
	if ((v.valueKind || '').toLowerCase() === 'const') return `= ${v.value ?? ''}${v.dataType ? ` (${v.dataType})` : ''}`;
	const label = v.value ? $t(`lz.label.sharedataSqlVariable.source_${v.value}`) : '—';
	if (v.value === 'lastKey' && v.defaultValue) return `${label} (${v.defaultValue})`;
	return label;
};

const formatCellTooltip = (row?: FieldCompareRow) => {
	if (!row || row.state === 'same') return '';
	if (row.state === 'new') return formatFieldSpec(row.partner);
	if (row.state === 'missing') return $t('lz.label.sharedataDataSource.fieldCompareMissingHint');
	return row.details.map((d) => `${d.aspect}: ${d.commonValue} → ${d.partnerValue}`).join(' · ');
};

const matrixRows = computed<MatrixRow[]>(() => {
	const partners = customPartners.value;
	const rows: MatrixRow[] = [
		{
			id: '__sql__',
			kind: 'sql',
			key: '__sql__',
			label: $t('lz.label.sharedataDataSource.compareSqlRow'),
			standardSpec: $t('lz.label.sharedataDataSource.compareSqlHunkRow'),
			hasDiff: partners.some((p) => sqlHunkCount(p.partnerId) > 0),
			cells: {},
		},
		{ id: '__sec_fields__', kind: 'section', key: '', label: $t('lz.label.sharedataDataSource.packetFieldTitle'), standardSpec: '', hasDiff: true, cells: {} },
	];

	// Thứ tự khoá: theo mẫu chung trước, rồi các khoá chỉ đối tác có
	const keys: string[] = [];
	const seen = new Set<string>();
	const pushKey = (k?: string | null) => {
		if (!k || seen.has(k.toLowerCase())) return;
		seen.add(k.toLowerCase());
		keys.push(k);
	};
	commonFields.value.forEach((f) => pushKey(f.key));
	partners.forEach((p) => (fieldCompareCache.value[p.partnerId]?.rows ?? []).forEach((r) => pushKey(r.key)));

	const commonMap = new Map(commonFields.value.filter((f) => f.key).map((f) => [f.key!.toLowerCase(), f]));
	for (const key of keys) {
		const cf = commonMap.get(key.toLowerCase());
		const cells: Record<string, MatrixCell> = {};
		let hasDiff = false;
		let label = cf?.label || key;
		for (const p of partners) {
			const r = (fieldCompareCache.value[p.partnerId]?.rows ?? []).find((x) => x.key.toLowerCase() === key.toLowerCase());
			if (!r) continue;
			if (r.state !== 'same') hasDiff = true;
			if (r.label && label === key) label = r.label;
			cells[p.partnerId] = { state: r.state, tooltip: formatCellTooltip(r) };
		}
		rows.push({ id: `__field__${key}`, kind: 'field', key, label, standardSpec: cf ? formatFieldSpec(cf) : $t('lz.label.sharedataDataSource.compareNotInTemplate'), hasDiff, cells });
	}

	rows.push({ id: '__sec_vars__', kind: 'section', key: '', label: $t('lz.label.sharedataDataSource.compareVariableSection'), standardSpec: '', hasDiff: true, cells: {} });
	const codes: string[] = [];
	const seenCodes = new Set<string>();
	const pushCode = (c?: string | null) => {
		if (!c || seenCodes.has(c.toLowerCase())) return;
		seenCodes.add(c.toLowerCase());
		codes.push(c);
	};
	commonVariables.value.forEach((v) => pushCode(v.code));
	partners.forEach((p) => (variableCompareCache.value[p.partnerId] ?? []).forEach((r) => pushCode(r.code)));
	const commonVarMap = new Map(commonVariables.value.filter((v) => v.code).map((v) => [v.code!.toLowerCase(), v]));
	for (const code of codes) {
		const cv = commonVarMap.get(code.toLowerCase());
		const cells: Record<string, MatrixCell> = {};
		let hasDiff = false;
		for (const p of partners) {
			const r = (variableCompareCache.value[p.partnerId] ?? []).find((x) => x.code.toLowerCase() === code.toLowerCase());
			if (!r) continue;
			if (r.state !== 'same') hasDiff = true;
			cells[p.partnerId] = { state: r.state, tooltip: r.tooltip };
		}
		rows.push({ id: `__var__${code}`, kind: 'variable', key: `@${code}`, label: '', standardSpec: cv ? formatVariableSpec(cv) : $t('lz.label.sharedataDataSource.compareNotInTemplate'), hasDiff, cells });
	}

	return rows;
});

const filteredRows = computed(() => (onlyDiff.value ? matrixRows.value.filter((r) => r.hasDiff) : matrixRows.value));
const overviewRowClass = ({ row }: { row: MatrixRow }) => {
	if (row.kind === 'section') return 'cad-section-row';
	if (row.kind === 'sql') return 'cad-sql-row';
	return '';
};

const LEGEND: FieldCompareState[] = ['same', 'new', 'missing', 'diff'];
const stateSymbol = (s: FieldCompareState) => (s === 'same' ? '=' : s === 'new' ? '+' : s === 'missing' ? '-' : '≠');
const stateTagType = (s: FieldCompareState) => (s === 'same' ? 'success' : s === 'new' ? 'primary' : s === 'missing' ? 'danger' : 'warning');
</script>

<style scoped lang="scss">
:deep(.el-dialog__body) {
	padding-top: 12px;
}

:global(.el-overlay .el-overlay-dialog .el-dialog.compare-all-dialog .el-dialog__body) {
	padding-top: 12px;
}

.cad-header {
	display: flex;
	align-items: center;
	color: #fff;
}

.cad-body {
	display: flex;
	flex-direction: column;
	gap: 8px;
	min-height: 200px;
}

.cad-tabs {
	:deep(.el-tabs__header) {
		margin: 0;
	}

	:deep(.el-tabs__content) {
		display: none;
	}
}

.cad-dot {
	display: inline-block;
	width: 7px;
	height: 7px;
	border-radius: 50%;
	background: var(--el-color-warning);
	margin-left: 5px;
	vertical-align: middle;
}

.cad-toolbar {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 8px;

	.cad-grow {
		flex: 1;
	}
}

.cad-note,
.cad-note-inline {
	margin: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.cad-subtitle {
	display: flex;
	align-items: center;
	gap: 6px;
	margin: 6px 0 0;
	font-size: 14px;
	font-weight: 700;
}

.cad-table {
	/* Tiêu đề cột cùng kiểu các bảng khác của hệ thống (nền xanh chủ đạo, chữ trắng), chữ lớn hơn nội dung dòng */
	:deep(.el-table__header-wrapper th.el-table__cell) {
		background-image: linear-gradient(180deg, color-mix(in srgb, var(--el-color-primary) 78%, #0f172a) 0%, var(--el-color-primary) 100%);
		color: #ffffff;
		font-size: 13px;
		font-weight: 600;
		border-right-color: rgba(255, 255, 255, 0.22);
	}

	:deep(.el-table__header-wrapper th.el-table__cell .el-button) {
		color: #ffffff;
	}

	:deep(.el-table__body td.el-table__cell) {
		font-size: 12px;
	}

	:deep(.cad-section-row td) {
		background: var(--el-fill-color-light);
	}

	:deep(.cad-sql-row td) {
		background: var(--el-fill-color-lighter);
	}
}

/* Ký hiệu so với mẫu: vòng tròn màu giống khối Trường dữ liệu */
.cmp-st {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	border-radius: 50%;
	border: 1px solid;
	font-weight: 700;
	font-size: 13px;
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

.cad-legend-item {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	font-size: 12px;
	font-weight: 600;
	margin-right: 10px;
}

.cad-section-label,
.cad-sql-row-label {
	font-size: 14px;
	font-weight: 700;
	color: var(--el-text-color-primary);
}

.cad-field-cell {
	display: flex;
	flex-direction: column;
	line-height: 1.3;
}

.cad-field-key {
	font-family: Consolas, Monaco, monospace;
	font-weight: 600;
}

.cad-field-label {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.cad-std-spec {
	font-size: 12px;
	color: var(--el-text-color-regular);
}

.cad-empty {
	color: var(--el-text-color-placeholder);
}

.cad-partner-header {
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 4px;

	.cad-partner-name {
		font-weight: 600;
	}
}

.cad-diff-box {
	border: 1px solid var(--el-border-color);
	border-radius: 4px;
	overflow: auto;
	max-height: 360px;
}

.cad-diff {
	width: 100%;
	border-collapse: collapse;
	table-layout: fixed;
	font-family: Consolas, Monaco, monospace;
	font-size: 12px;

	th {
		text-align: left;
		padding: 4px 8px;
		background: var(--el-fill-color-light);
		border-bottom: 1px solid var(--el-border-color);
		font-family: inherit;
	}

	td {
		padding: 1px 6px;
		vertical-align: top;
		border-bottom: 1px solid var(--el-border-color-extra-light);
	}

	td.ln {
		color: var(--el-text-color-placeholder);
		text-align: right;
		user-select: none;
		background: var(--el-fill-color-lighter);
	}

	td.code {
		white-space: pre-wrap;
		word-break: break-word;
	}

	/* Màu chuẩn cho code diff (xanh lá / đỏ như git diff thông thường) */
	td.d-tpl {
		background: #ffeef0;
		box-shadow: inset 3px 0 0 #f56c6c;
	}

	td.d-ptn {
		background: #e6ffed;
		box-shadow: inset 3px 0 0 #67c23a;
	}

	td.d-empty {
		background: var(--el-fill-color-lighter);
	}

	/* Dòng tiêu đề đoạn khác theo tông trung tính như git diff hunk header */
	.cad-hunk-row td {
		background: var(--el-fill-color-light);
		color: var(--el-text-color-regular);
		font-size: 12px;
		font-weight: 600;
		padding: 3px 8px;
		border-top: 1px solid var(--el-border-color-lighter);
		border-bottom: 1px solid var(--el-border-color-lighter);
	}
}

html.dark {
	.cad-diff {
		td.d-tpl {
			background: rgba(245, 108, 108, 0.16);
			box-shadow: inset 3px 0 0 #f56c6c;
		}

		td.d-ptn {
			background: rgba(103, 194, 58, 0.18);
			box-shadow: inset 3px 0 0 #67c23a;
		}

		.cad-hunk-row td {
			background: var(--el-fill-color-light);
			color: var(--el-text-color-secondary);
			border-color: var(--el-border-color);
		}
	}
}

/* Tag "x đoạn khác" dùng màu đỏ báo khác biệt (đối trọng với xanh lá "giống mẫu"), không trùng cam của đối tác / tab-dot */
.cad-hunk-tag {
	--el-tag-text-color: var(--el-color-danger);
	--el-tag-bg-color: var(--el-color-danger-light-9);
	--el-tag-border-color: var(--el-color-danger-light-8);
	color: var(--el-color-danger);
	background-color: var(--el-color-danger-light-9);
	border-color: var(--el-color-danger-light-8);
	font-weight: 600;
}

html.dark .cad-hunk-tag {
	--el-tag-text-color: #f89898;
	--el-tag-bg-color: rgba(245, 108, 108, 0.16);
	--el-tag-border-color: rgba(245, 108, 108, 0.36);
	color: #f89898;
	background-color: rgba(245, 108, 108, 0.16);
	border-color: rgba(245, 108, 108, 0.36);
}

/* Ô swatch mẫu màu cho chú giải dòng code khác biệt (đỏ = mẫu chung, xanh = đối tác) */
.cad-diff-swatch {
	display: inline-block;
	width: 12px;
	height: 12px;
	border-radius: 2px;
	vertical-align: -1px;
	margin-right: 4px;
}

.cad-diff-swatch-tpl {
	background: #ffeef0;
	border: 1px solid #f56c6c;
	box-shadow: inset 2px 0 0 #f56c6c;
}

.cad-diff-swatch-ptn {
	background: #e6ffed;
	border: 1px solid #67c23a;
	box-shadow: inset 2px 0 0 #67c23a;
}

html.dark {
	.cad-diff-swatch-tpl {
		background: rgba(245, 108, 108, 0.16);
		border-color: #f56c6c;
		box-shadow: inset 2px 0 0 #f56c6c;
	}

	.cad-diff-swatch-ptn {
		background: rgba(103, 194, 58, 0.18);
		border-color: #67c23a;
		box-shadow: inset 2px 0 0 #67c23a;
	}
}

.cad-legend {
	display: inline-flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px;
}

.cad-subtitle .cad-legend {
	margin-left: auto;
	font-weight: 400;
}
</style>
