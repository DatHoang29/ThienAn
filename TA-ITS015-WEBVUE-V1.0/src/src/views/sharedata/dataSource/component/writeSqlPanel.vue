<template>
	<!-- Ghi ngược (chiều nhận): một khối gồm một hoặc nhiều câu MERGE / INSERT / UPDATE chạy trong một giao dịch trên CSDL nhận.
	     Bảng đích trong SQL được tự dò vào Bảng bị ảnh hưởng; Admin có thể khai thêm bảng phụ (trigger/view). -->
	<div class="write-sql-panel">
		<!-- Phạm vi câu ghi giống tab Gửi đi: Mẫu chung + đối tác đã có câu ghi riêng trong một ô chọn; "Tạo cấu hình riêng +" để thêm đối tác -->
		<div class="wsp-scope-bar">
			<span class="wsp-scope-label">{{ $t('lz.label.sharedataDataSource.selectScopeLabel') }}</span>
			<el-tooltip
				:content="activeScopeLabel"
				placement="top"
				:show-after="300"
				:trigger-keys="[]"
				popper-class="write-sql-tooltip"
			>
			<el-select
				:key="scopeSelectKey"
				:model-value="state.scopePartnerId || SCOPE_COMMON"
				size="small"
				filterable
				class="wsp-scope-select"
				popper-class="wsp-scope-select-popper"
				:disabled="!props.packetCode || state.saving || resetting"
				:placeholder="$t('lz.placeholder.sharedataDataSource.selectPartner')"
				@change="onScopeSelectChange"
			>
				<template #prefix>
					<el-icon><ele-Search /></el-icon>
				</template>
				<el-option :value="SCOPE_COMMON" :label="$t('lz.label.sharedataDataSource.tabStandardSql')" />
				<el-option v-for="p in scopeTabs" :key="p.partnerId" :value="p.partnerId" :label="partnerScopeLabel(p)" />
			</el-select>
			</el-tooltip>
			<el-button
				size="small"
				text
				type="primary"
				class="wsp-add-tab-btn"
				:disabled="!props.packetCode || !hasUnconfiguredPartners || state.saving || resetting"
				@click="state.setupVisible = true"
			>
				{{ $t('lz.button.sharedataDataSource.addPartnerSqlTab') }}
			</el-button>
		</div>

		<!-- Đầu khối: nhóm tiêu đề bên trái và nhóm nút thao tác bên phải luôn thẳng hàng -->
		<div class="wsp-header">
			<div class="wsp-title-group">
				<span class="wsp-title">{{ $t('lz.label.sharedataWriteSql.title') }}</span>
				<el-tooltip
					placement="top"
					:content="$t('lz.tooltip.sharedataWriteSql.orderHint')"
					popper-class="write-sql-tooltip"
				>
					<el-icon class="wsp-hint"><ele-InfoFilled /></el-icon>
				</el-tooltip>
				<el-tag
					size="small"
					:type="effectiveAffectedTables.length ? 'success' : 'info'"
					effect="plain"
				>
					{{
						effectiveAffectedTables.length
							? $t('lz.label.sharedataWriteSql.countTag', { count: effectiveAffectedTables.length })
							: $t('lz.label.sharedataWriteSql.sendOnly')
					}}
				</el-tag>
			</div>
			<div class="wsp-actions">
				<el-button
					v-if="state.scopePartnerId && state.isCustom"
					size="small"
					type="warning"
					plain
					icon="ele-RefreshLeft"
					:loading="resetting"
					:disabled="Boolean(lockNames) || state.saving || resetting"
					@click="handleUseCommon"
				>
					{{ $t('lz.button.sharedataWriteSql.useCommon') }}
				</el-button>
				<el-button
					size="small"
					icon="ele-DocumentAdd"
					:disabled="!props.packetCode || Boolean(lockNames)"
					@click="insertTemplate"
				>
					{{ $t('lz.button.sharedataWriteSql.insertTemplate') }}
				</el-button>
				<el-button
					size="small"
					type="primary"
					plain
					icon="ele-CircleCheck"
					:disabled="!props.packetCode || !state.writeSql.trim()"
					:loading="state.checking"
					@click="checkBlock"
				>
					{{ $t('lz.button.sharedataWriteSql.check') }}
				</el-button>

				<el-button
					size="small"
					type="primary"
					icon="ele-FolderChecked"
					class="wsp-save-btn"
					:disabled="!props.packetCode || !isDirty || Boolean(lockNames) || resetting"
					:loading="state.saving"
					@click="handleSaveClick"
				>
					{{ $t('lz.button.base.save') }}
				</el-button>
			</div>
		</div>
		<el-alert
			v-if="lockNames"
			type="warning"
			:closable="false"
			show-icon
			class="mt5 mb5 sql-lock-alert"
		>
			<template #title>
				<span class="sql-lock-alert-text">{{ $t('lz.label.sharedataDataSource.writeSqlLockedBySubscription', { partners: visibleLockNames }) }}</span>
				<el-button
					v-if="lockPartnerList.length > LOCK_PARTNERS_VISIBLE_LIMIT"
					size="small"
					text
					type="primary"
					class="sql-lock-toggle"
					@click="lockExpanded = !lockExpanded"
				>
					{{ lockExpanded ? $t('lz.button.base.collapse') : $t('lz.button.sharedataDataSource.partnerUsageShowAll', { count: lockPartnerList.length }) }}
				</el-button>
			</template>
		</el-alert>
		<el-alert
			v-if="state.scopePartnerId && !state.isCustom && !state.loading"
			:title="$t('lz.label.sharedataWriteSql.inheritCommonHint')"
			type="info"
			:closable="false"
			show-icon
			class="mt5 mb5"
		/>

		<!-- Bảng bị ảnh hưởng & Khoá bản ghi: tự dò từ câu SQL -->
		<div class="wsp-affected-section">
			<div class="wsp-meta-item">
				<span class="wsp-meta-label">
					{{ $t('lz.label.sharedataWriteSql.affectedTablesTitle') }}
					<el-tooltip
						placement="top"
						:content="$t('lz.label.sharedataWriteSql.affectedTablesHint')"
						popper-class="write-sql-tooltip"
					>
						<el-icon class="wsp-hint"><ele-InfoFilled /></el-icon>
					</el-tooltip>:
				</span>
				<div class="wsp-meta-tags">
					<template v-if="detectedTables.length">
						<el-tag
							v-for="tbl in detectedTables"
							:key="tbl"
							size="small"
							type="primary"
							effect="plain"
							class="wsp-detected-tag"
						>
							{{ tbl }}
						</el-tag>
					</template>
					<span v-else class="wsp-muted">—</span>
				</div>
			</div>

			<div v-if="state.keyFields.length" class="wsp-meta-item">
				<span class="wsp-meta-label">{{ $t('lz.label.sharedataWriteSql.colKey') }}:</span>
				<div class="wsp-meta-tags">
					<el-tag
						v-for="key in state.keyFields"
						:key="key"
						size="small"
						effect="plain"
						class="wsp-key-tag"
					>
						{{ key }}
					</el-tag>
				</div>
			</div>
		</div>

		<!-- Khung soạn câu ghi dùng chung trình soạn với chiều gửi: gợi ý từ khoá, bảng, tham số hệ thống {'@'}records... -->
		<div class="wsp-editor" :class="{ 'is-scope-locked': Boolean(lockNames) }">
			<SqlEditor
				v-model="state.writeSql"
				:packet-code="props.packetCode"
				:title="$t('lz.label.sharedataWriteSql.editorTitle')"
				:title-tooltip="$t('lz.label.sharedataWriteSql.systemParams', { names: SYSTEM_PARAMS })"
				:variables="systemParamItems"
				:saved-fields="editorFields"
				:min-height="260"
				:max-height="480"
				:readonly="Boolean(lockNames)"
				collapse-key="sharedataDataSource:writeSqlEditorCollapsed"
				@change="markChanged"
			/>
			<ul v-if="state.checks.length" class="wsp-checks">
				<li
					v-for="(check, i) in state.checks"
					:key="i"
					:class="`wsp-check-${check.level === LEVEL_ERROR ? 'error' : 'warning'}`"
				>
					<el-icon><ele-CircleCloseFilled v-if="check.level === LEVEL_ERROR" /><ele-WarningFilled v-else /></el-icon>
					<span>{{ check.message }}</span>
					<span v-if="check.line" class="wsp-muted">{{ $t('lz.label.sharedataWriteSql.atLine', { line: check.line, column: check.column ?? 1 }) }}</span>
				</li>
			</ul>
			<div v-else-if="state.checked" class="wsp-check-ok">
				<el-icon><ele-CircleCheckFilled /></el-icon>
				<span>{{ $t('lz.message.sharedataWriteSql.checkOk') }}</span>
			</div>
		</div>

		<PartnerSetupDialog
			v-model="state.setupVisible"
			:packet-code="props.packetCode || ''"
			:partners="state.partnerList"
			:alert-text="$t('lz.label.sharedataWriteSql.partnerSetupAlert')"
			:loading="state.saving"
			@confirm="handlePartnerSetup"
		/>
	</div>
</template>

<script lang="ts" setup name="sharedataWriteSqlPanel">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataPacketApi } from '/@/api-services/shareData';
import { type PacketSqlPreload, takePacketSqlPreload } from '/@/utils/sharedata/packetSqlPreload';
import type { PacketWriteSqlsOutput, PartnerSqlListItemOutput, PartnerWriteSqlOutput, SqlFieldItem, WriteSqlCheckItem } from '/@/api-services/shareData/models';
import SqlEditor from './sqlEditor.vue';
import PartnerSetupDialog from './partnerSetupDialog.vue';

const { t: $t } = useI18n();

const props = defineProps<{
	packetCode?: string | null;
	/** Trường của gói (FieldsJson, đã bỏ trường mất) để chèn mẫu MERGE. */
	fields?: SqlFieldItem[] | null;
	/** Trang cha đang hiện một vòng loading chung (đổi gói): không hiện vòng loading riêng. */
	silentLoading?: boolean;
	/** Cấu hình SQL mẫu chung nạp sẵn khi bấm gói: lấy khối câu ghi từ đây thay vì gọi API riêng. */
	preload?: PacketSqlPreload | null;
	/** Đăng ký nhận đang chạy của gói: BE chặn sửa câu ghi (ESH-2306) — mẫu chung khoá khi còn bất kỳ, câu ghi riêng chỉ khoá theo đúng đối tác. */
	lockedSubs?: Array<{ partnerId?: string | null; name: string }>;
}>();

const emit = defineEmits<{
	(e: 'saved', count: number): void;
}>();

/* Giữ khớp ShareDataConst (BE) */
const LEVEL_ERROR = 'Error';
const LEVEL_WARNING = 'Warning';
const SYSTEM_PARAMS = '@records, @now, @tenantId, @operator, @partnerId, @partnerCode, @datatypeId, @serialNbr, @packetId';
/** Gợi ý khi gõ @ trong khung soạn câu ghi: tham số hệ thống truyền vào mọi câu ghi. */
const systemParamItems = SYSTEM_PARAMS.split(',').map((p) => ({ code: p.trim().replace(/^@/, ''), desc: '' }));

const normalizeTable = (raw?: string | null): string => {
	const cleaned = (raw || '').replace(/[\[\]]/g, '').trim();
	if (!cleaned) return '';
	const parts = cleaned.split('.').map((p) => p.trim()).filter(Boolean);
	if (parts.length === 1) return `dbo.${parts[0]}`;
	if (parts.length === 2) return `${parts[0]}.${parts[1]}`;
	return cleaned;
};

/** Dò nhanh bảng đích của các câu MERGE / INSERT / UPDATE trong khối SQL trên trình duyệt. */
const detectTablesFromSql = (sqlText: string): string[] => {
	const clean = (sqlText || '')
		.replace(/\/\*[\s\S]*?\*\//g, ' ')
		.replace(/--[^\r\n]*/g, ' ');
	const result: string[] = [];
	const seen = new Set<string>();
	const add = (raw?: string) => {
		if (!raw) return;
		const norm = normalizeTable(raw);
		if (!norm) return;
		const lower = norm.toLowerCase();
		if (seen.has(lower)) return;
		seen.add(lower);
		result.push(norm);
	};

	const re = /\b(?:MERGE(?:\s+TOP\s*\([^)]+\))?(?:\s+INTO)?|INSERT(?:\s+TOP\s*\([^)]+\))?(?:\s+INTO)?|UPDATE(?:\s+TOP\s*\([^)]+\))?)\s+(?!SET\b)((?:\[?[A-Za-z0-9_]+\]?\.)?\[?[A-Za-z0-9_]+\]?)/gi;
	let m: RegExpExecArray | null;
	while ((m = re.exec(clean)) !== null) {
		add(m[1]);
	}
	return result;
};

const buildSnapshot = (writeSql: string, tables: string[], remark: string) =>
	JSON.stringify([
		(writeSql || '').replace(/\r\n?/g, '\n').trim(),
		tables.map((t) => normalizeTable(t).toLowerCase()).filter(Boolean).sort(),
		(remark || '').trim(),
	]);

const state = reactive({
	loading: false,
	saving: false,
	checking: false,
	writeSql: '',
	serverDetectedTables: [] as string[],
	remark: '',
	keyFields: [] as string[],
	checks: [] as WriteSqlCheckItem[],
	checked: false,
	savedSnapshot: buildSnapshot('', [], ''),
	savedCount: 0,
	/** Phạm vi đang sửa: '' = câu ghi mẫu chung, có giá trị = ID đối tác. */
	scopePartnerId: '',
	/** Đối tác đang có câu ghi riêng (false = đang hiện câu ghi mẫu chung mà đối tác kế thừa). */
	isCustom: false,
	/** Đối tác liên quan chiều nhận của gói (GetPartnerWriteSqlList); isCustom = đã có câu ghi riêng. */
	partnerList: [] as PartnerSqlListItemOutput[],
	/** Mở hộp thoại Tạo cấu hình riêng. */
	setupVisible: false,
});

/** Tên tab Mẫu chung (el-tabs coi tên rỗng là chưa chọn nên không dùng ''). */
const SCOPE_COMMON = '__common__';

/** Tab đối tác: chỉ đối tác đã có câu ghi riêng (giống chiều gửi). */
const scopeTabs = computed(() => state.partnerList.filter((p) => p.isCustom && p.partnerId));
/** Còn đối tác chưa có câu ghi riêng để tạo thêm. */
const hasUnconfiguredPartners = computed(() => state.partnerList.some((p) => !p.isCustom));

/** Tên đối tác đang chặn sửa câu ghi ở phạm vi đang mở; rỗng = không khoá. */
const lockNames = computed(() =>
	[...new Set((props.lockedSubs ?? []).filter((s) => !state.scopePartnerId || s.partnerId === state.scopePartnerId).map((s) => s.name))].join(', ')
);
const scopeParams = () => (state.scopePartnerId ? { partnerId: state.scopePartnerId } : {});

/** Trường của gói để gợi ý khi gõ AS / OPENJSON WITH. */
const editorFields = computed(() => (props.fields ?? []).filter((f) => f.key && !f.lost).map((f) => ({ key: f.key!, type: f.type ?? undefined, label: f.label ?? undefined })) as any[]);

const detectedTables = computed<string[]>(() => {
	const live = detectTablesFromSql(state.writeSql);
	if (state.checked && state.serverDetectedTables.length > 0) {
		return state.serverDetectedTables;
	}
	return live;
});

const effectiveAffectedTables = computed<string[]>(() => {
	if (!state.writeSql.trim()) return [];
	return detectedTables.value;
});

const isDirty = computed(() => !state.loading && buildSnapshot(state.writeSql, effectiveAffectedTables.value, state.remark) !== state.savedSnapshot);

/** Câu ghi đối tác (GetPartnerWriteSql) quy về cùng dạng câu ghi mẫu chung: hiện câu riêng nếu có, không thì câu mẫu chung đối tác đang dùng. */
const fromPartnerOutput = (data?: PartnerWriteSqlOutput | null): PacketWriteSqlsOutput & { isCustom?: boolean } => ({
	packetCode: data?.packetCode,
	writeSql: data?.isCustom ? data?.customWriteSql : data?.standardWriteSql,
	affectedTables: data?.affectedTables,
	detectedTables: data?.detectedTables,
	keyFields: data?.keyFields,
	remark: data?.remark,
	isCustom: Boolean(data?.isCustom),
});

const applyOutput = (data?: (PacketWriteSqlsOutput & { isCustom?: boolean }) | null) => {
	const sql = data?.writeSql ?? '';
	const srvDetected = (data?.detectedTables ?? detectTablesFromSql(sql)).map(normalizeTable).filter(Boolean);

	state.writeSql = sql;
	state.serverDetectedTables = srvDetected;
	state.remark = data?.remark ?? '';
	state.keyFields = data?.keyFields ?? [];
	state.isCustom = Boolean(data?.isCustom);
	state.checks = [];
	state.checked = false;
	state.savedCount = effectiveAffectedTables.value.length;
	state.savedSnapshot = buildSnapshot(state.writeSql, effectiveAffectedTables.value, state.remark);
};

// Gọi load chồng nhau (đổi gói, nạp lại cả khung) thì dùng chung một lượt tải — theo khoá gói × phạm vi:
// đổi phạm vi giữa lúc đang tải phải tải lượt mới, không được trả lượt cũ (kết quả lượt cũ bị bỏ vì sai phạm vi).
let writeLoadPromise: Promise<void> | null = null;
let writeLoadKey = '';
const load = async (silent: boolean | unknown = false): Promise<void> => {
	const isSilent = silent === true;
	const key = `${props.packetCode ?? ''}|${state.scopePartnerId}`;
	if (writeLoadPromise && writeLoadKey === key) return writeLoadPromise;
	writeLoadKey = key;
	const current = (async () => {
		if (!props.packetCode) {
			applyOutput(null);
			return;
		}
		if (!isSilent) state.loading = true;
		try {
			const packetCode = props.packetCode;
			const partnerId = state.scopePartnerId;
			// Bản nạp sẵn chỉ có câu ghi mẫu chung
			const preloaded = partnerId ? null : (await takePacketSqlPreload(props.preload, packetCode, 'write'))?.write;
			if (props.packetCode !== packetCode || state.scopePartnerId !== partnerId) return;
			if (preloaded) {
				applyOutput(preloaded);
				return;
			}
			if (partnerId) {
				const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnerwritesqlGet(partnerId, packetCode);
				if (props.packetCode !== packetCode || state.scopePartnerId !== partnerId) return;
				applyOutput(fromPartnerOutput(res.data?.result));
				return;
			}
			const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetwritesqlsGet(packetCode);
			if (props.packetCode !== packetCode || state.scopePartnerId !== partnerId) return;
			applyOutput(res.data?.result);
		} finally {
			if (!isSilent) state.loading = false;
		}
	})().finally(() => {
		if (writeLoadPromise === current) writeLoadPromise = null;
	});
	writeLoadPromise = current;
	return current;
};

/** Danh sách đối tác liên quan chiều nhận của gói (dựng tab và hộp thoại Tạo cấu hình riêng). */
const loadPartnerList = async () => {
	const packetCode = props.packetCode;
	if (!packetCode) {
		state.partnerList = [];
		return;
	}
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnerwritesqllistGet(packetCode);
		if (props.packetCode === packetCode) state.partnerList = res.data?.result ?? [];
	} catch {
		/* Lỗi API đã được interceptor hiển thị */
	}
};

// Đổi gói thì quay về câu ghi mẫu chung và nạp lại danh sách đối tác
watch(
	() => props.packetCode,
	() => {
		state.scopePartnerId = '';
		state.partnerList = [];
		load();
		loadPartnerList();
	},
	{ immediate: true }
);

/** Đang sửa dở thì hỏi trước khi rời phạm vi; trả false = ở lại. */
const confirmDiscard = async (): Promise<boolean> => {
	if (!isDirty.value) return true;
	try {
		await ElMessageBox.confirm($t('lz.confirm.sharedataWriteSql.discardChanges'), $t('lz.label.base.notification'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
		});
		return true;
	} catch {
		return false;
	}
};
const beforeScopeLeave = () => {
	if (resetting.value || state.saving) return false;
	return confirmDiscard();
};

const scopeSelectKey = ref(0);
const partnerScopeLabel = (p: PartnerSqlListItemOutput) =>
	p.partnerCode && p.partnerName && p.partnerName !== p.partnerCode ? `${p.partnerCode} — ${p.partnerName}` : p.partnerCode || p.partnerName || p.partnerId || '';
/** Tên đầy đủ của phạm vi đang chọn cho tooltip của ô chọn (ô hẹp nên tên dài bị cắt), giống ô chọn chiều gửi. */
const activeScopeLabel = computed(() => {
	if (!state.scopePartnerId) return $t('lz.label.sharedataDataSource.tabStandardSql');
	const partner = scopeTabs.value.find((p) => p.partnerId === state.scopePartnerId);
	return partner ? partnerScopeLabel(partner) : '';
});

const handleScopeChange = async (partnerId: string, silent: boolean | unknown = false) => {
	if (state.scopePartnerId === partnerId) return;
	state.scopePartnerId = partnerId;
	await load(silent === true);
};

const onScopeSelectChange = async (val: string) => {
	const targetId = val === SCOPE_COMMON ? '' : String(val);
	if (targetId === state.scopePartnerId) return;
	if (!(await beforeScopeLeave())) {
		scopeSelectKey.value++;
		return;
	}
	await handleScopeChange(targetId);
};

/**
 * Tạo câu ghi riêng cho đối tác (giống "Tạo cấu hình riêng" chiều gửi): chép câu ghi nguồn — mẫu chung hoặc câu ghi riêng
 * của đối tác khác — rồi lưu ngay thành câu ghi riêng và mở tab đối tác đó.
 */
const handlePartnerSetup = async (payload: { partnerId: string; sourcePartnerId: string }) => {
	const packetCode = props.packetCode;
	if (!packetCode || !payload.partnerId) return;
	const lockedName = (props.lockedSubs ?? []).find((s) => s.partnerId === payload.partnerId)?.name;
	if (lockedName) {
		ElMessage.warning($t('lz.label.sharedataDataSource.writeSqlLockedBySubscription', { partners: lockedName }));
		return;
	}
	if (!(await confirmDiscard())) return;

	state.saving = true;
	try {
		let source: { writeSql?: string | null; affectedTables?: string[] | null; remark?: string | null } | null | undefined;
		if (payload.sourcePartnerId === 'standard') {
			source = (await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetwritesqlsGet(packetCode)).data?.result;
		} else {
			const r = (await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketGetpartnerwritesqlGet(payload.sourcePartnerId, packetCode)).data?.result;
			source = r && { writeSql: r.isCustom ? r.customWriteSql : r.standardWriteSql, affectedTables: r.affectedTables, remark: r.remark };
		}
		if (!source?.writeSql?.trim()) {
			ElMessage.warning($t('lz.message.sharedataWriteSql.sourceWriteSqlEmpty'));
			return;
		}
		await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdatepartnerwritesqlPost({
			packetCode,
			partnerId: payload.partnerId,
			writeSql: source.writeSql,
			affectedTables: source.affectedTables ?? [],
			remark: source.remark ?? null,
		});
		await loadPartnerList();
		state.scopePartnerId = payload.partnerId;
		await load(true);
		ElMessage.success($t('lz.message.base.saveSuccess'));
		state.setupVisible = false;
	} catch {
		/* Lỗi API đã được interceptor hiển thị */
	} finally {
		state.saving = false;
	}
};

/** Bỏ câu ghi riêng của đối tác (ResetPartnerWriteSql, giống Dùng lại mẫu chung của SQL trích xuất). */
const handleUseCommon = async () => {
	if (!props.packetCode || !state.scopePartnerId || lockNames.value) return;
	try {
		await ElMessageBox.confirm($t('lz.confirm.sharedataWriteSql.useCommon'), $t('lz.label.base.notification'), {
			type: 'warning',
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
		});
	} catch {
		return;
	}
	resetting.value = true;
	try {
		const partnerId = state.scopePartnerId;
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketResetpartnerwritesqlPost({
			packetCode: props.packetCode,
			partnerId,
		});
		applyOutput(fromPartnerOutput(res.data?.result));
		await loadPartnerList();
		if (state.scopePartnerId === partnerId) await handleScopeChange('', true);
		ElMessage.success($t('lz.message.base.saveSuccess'));
	} catch {
		/* Lỗi API đã được interceptor hiển thị */
	} finally {
		resetting.value = false;
	}
};

const markChanged = () => {
	state.checks = [];
	state.checked = false;
};

/** Kiểm khối câu ghi qua BE; trả kết quả (null khi gọi lỗi, interceptor đã báo). */
const validateBlock = async () => {
	if (!props.packetCode) return null;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketValidatewritesqlPost({
			packetCode: props.packetCode,
			...scopeParams(),
			writeSql: state.writeSql,
			affectedTables: effectiveAffectedTables.value,
		});
		const result = res.data?.result;
		if (result) {
			state.checks = result.checks ?? [];
			state.keyFields = result.keyFields ?? [];
			if (result.detectedTables) {
				state.serverDetectedTables = result.detectedTables.map(normalizeTable).filter(Boolean);
			}
			state.checked = true;
		}
		return result ?? null;
	} catch {
		return null;
	}
};

const checkBlock = async () => {
	if (!state.writeSql.trim()) return;
	state.checking = true;
	try {
		await validateBlock();
	} finally {
		state.checking = false;
	}
};

const resolveDbType = (rawType?: string | null): string => {
	const t = (rawType || '').toLowerCase();
	switch (t) {
		case 'int':
			return 'int';
		case 'long':
		case 'bigint':
			return 'bigint';
		case 'float':
		case 'double':
			return 'float';
		case 'decimal':
		case 'number':
			return 'decimal(18, 4)';
		case 'datetime':
			return 'datetime2';
		case 'bool':
		case 'boolean':
			return 'bit';
		case 'guid':
			return 'uniqueidentifier';
		default:
			return 'nvarchar(max)';
	}
};

const toColumnName = (key: string) => key.charAt(0).toUpperCase() + key.slice(1);

/** Chèn thêm một khung MERGE chuẩn vào cuối khối câu ghi. */
const insertTemplate = () => {
	const fields = (props.fields ?? []).filter((f) => f.key && !f.lost);
	if (!fields.length) {
		ElMessage.warning($t('lz.message.sharedataWriteSql.noFields'));
		return;
	}
	const targetTable = detectedTables.value[0] || 'dbo.TmsTrafficData';
	const key = (fields.find((f) => /(^id$|Id$)/.test(f.key!)) ?? fields[0]).key!;
	const others = fields.filter((f) => f.key !== key).map((f) => f.key!);
	const indent = '            ';
	const withCols = [key, ...others]
		.map((k) => `${indent}${k} ${k === key ? 'nvarchar(64)' : resolveDbType(fields.find((f) => f.key === k)?.type)}`)
		.join(',\n');
	const selectCols = [`    SELECT  ${key}`, ...others.map((k) => `            MAX(${k}) AS ${k}`)].join(',\n');
	const updates = [...others.map((k) => `t.${toColumnName(k)} = ISNULL(s.${k}, t.${toColumnName(k)})`), 't.UpdateTime = @now', 't.UpdateUId = @operator'].join(',\n               ');
	const insertCols = ['ID', ...others.map(toColumnName), 'TenantId', 'CreateTime', 'CreateUId'].join(', ');
	const insertValues = [`s.${key}`, ...others.map((k) => `s.${k}`), '@tenantId', '@now', '@operator'].join(', ');
	const template = [
		`MERGE ${normalizeTable(targetTable)} WITH (HOLDLOCK) AS t`,
		'USING (',
		selectCols,
		'    FROM OPENJSON(@records) WITH (',
		`${withCols})`,
		`    WHERE ${key} IS NOT NULL`,
		`    GROUP BY ${key}`,
		') AS s',
		`   ON t.ID = s.${key}`,
		'WHEN MATCHED THEN',
		`    UPDATE SET ${updates}`,
		'WHEN NOT MATCHED THEN',
		`    INSERT (${insertCols})`,
		`    VALUES (${insertValues})`,
		'OUTPUT $action;',
	].join('\n');
	state.writeSql = state.writeSql.trim() ? `${state.writeSql.trimEnd()}\n\n${template}` : template;
	markChanged();
};

/** Lưu khối câu ghi chiều nhận: nếu không rỗng thì kiểm an toàn qua BE trước; có lỗi thì dừng, có cảnh báo thì hỏi xác nhận. */
const save = async (): Promise<boolean> => {
	if (!props.packetCode || lockNames.value) return false;
	// Câu ghi riêng không được rỗng (BE PartnerWriteSqlRequired): muốn bỏ thì dùng nút Dùng lại mẫu chung
	if (state.scopePartnerId && !state.writeSql.trim()) {
		ElMessage.warning($t('lz.message.sharedataWriteSql.partnerWriteSqlRequired'));
		return false;
	}
	state.saving = true;
	try {
		if (state.writeSql.trim()) {
			const result = state.checked
				? { isValid: state.checks.every((c) => c.level !== LEVEL_ERROR), checks: state.checks }
				: await validateBlock();
			if (!result) return false;
			if (!result.isValid) {
				ElMessage.error($t('lz.message.sharedataWriteSql.blockHasError'));
				return false;
			}
			const warnings = (result.checks ?? []).filter((c) => c.level === LEVEL_WARNING).map((c) => c.message).filter(Boolean);
			if (warnings.length) {
				try {
					await ElMessageBox.confirm(warnings.join('<br>'), $t('lz.message.sharedataWriteSql.confirmWarningsTitle'), {
						type: 'warning',
						dangerouslyUseHTMLString: true,
						confirmButtonText: $t('lz.button.base.save'),
						cancelButtonText: $t('lz.button.base.cancel'),
					});
				} catch {
					return false;
				}
			}
		}
		if (state.scopePartnerId) {
			const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdatepartnerwritesqlPost({
				packetCode: props.packetCode,
				partnerId: state.scopePartnerId,
				writeSql: state.writeSql,
				affectedTables: effectiveAffectedTables.value,
				remark: state.remark?.trim() || null,
			});
			applyOutput(fromPartnerOutput(res.data?.result));
			if (!scopeTabs.value.some((p) => p.partnerId === state.scopePartnerId)) await loadPartnerList();
			return true;
		}
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketUpdatewritesqlsPost({
			packetCode: props.packetCode,
			writeSql: state.writeSql.trim() ? state.writeSql : '',
			affectedTables: state.writeSql.trim() ? effectiveAffectedTables.value : [],
			remark: state.remark?.trim() || null,
		});
		applyOutput(res.data?.result);
		// Số bảng ở danh sách gói tính theo câu ghi mẫu chung
		if (!state.scopePartnerId) emit('saved', state.savedCount);
		return true;
	} catch {
		return false;
	} finally {
		state.saving = false;
	}
};

const handleSaveClick = async () => {
	if (await save()) {
		ElMessage.success($t('lz.message.base.saveSuccess'));
	}
};

const saveIfDirty = async (): Promise<boolean> => (isDirty.value ? save() : true);

/** Số bảng bị ảnh hưởng đã lưu (dòng gói tin ở danh sách hiện "{count} bảng"). */
const count = computed(() => state.savedCount);

const loading = computed(() => state.loading);

const resetting = ref(false);

const LOCK_PARTNERS_VISIBLE_LIMIT = 5;
const lockExpanded = ref(false);
const lockPartnerList = computed(() =>
	[...new Set((props.lockedSubs ?? []).filter((s) => !state.scopePartnerId || s.partnerId === state.scopePartnerId).map((s) => s.name).filter(Boolean))]
);
const visibleLockNames = computed(() => {
	const list = lockPartnerList.value;
	if (lockExpanded.value || list.length <= LOCK_PARTNERS_VISIBLE_LIMIT) return list.join(', ');
	return `${list.slice(0, LOCK_PARTNERS_VISIBLE_LIMIT).join(', ')} …`;
});
watch([() => props.packetCode, () => state.scopePartnerId], () => {
	lockExpanded.value = false;
});

defineExpose({ reload: load, isDirty, saveIfDirty, count, loading });
</script>

<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.el-popper.write-sql-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}
</style>

<style scoped lang="scss">
/* Thanh phạm vi Mẫu chung / đối tác: đồng bộ với thanh chọn phạm vi của chiều gửi (dataSource/index.vue .sql-partner-tabs-bar) */
.wsp-scope-bar {
	display: flex;
	align-items: center;
	gap: 8px;
	padding-bottom: 6px;
	white-space: nowrap;
	/* Cách khối dưới bằng gap của .write-sql-panel (8px), không cộng thêm lề */
	border-bottom: 1px solid var(--el-border-color-lighter);

	.wsp-scope-label {
		font-size: 13px;
		color: var(--el-text-color-regular);
	}

	.wsp-scope-select {
		width: 180px;
		flex: 0 0 180px;
	}

	.wsp-add-tab-btn {
		flex-shrink: 0;
		height: 24px;
		line-height: 22px;
		padding: 0 10px;
		font-size: 12px;
		font-weight: 500;
		border-radius: 4px;
		background-color: var(--el-color-primary-light-9);
		border: 1px solid var(--el-color-primary-light-5);
		color: var(--el-color-primary);

		&:hover:not(.is-disabled) {
			background-color: var(--el-color-primary-light-8);
			border-color: var(--el-color-primary-light-3);
		}

		&.is-disabled {
			background-color: var(--el-fill-color-light);
			border-color: var(--el-border-color-lighter);
			color: var(--el-text-color-placeholder);
		}
	}
}

/* Không cách trên: thanh tab đối tác nằm sát dưới tab Gửi đi / Nhận về giống chiều gửi (dataSource/index.vue .sql-header-row) */
.write-sql-panel {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.wsp-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	flex-wrap: wrap;
}

.wsp-title-group {
	display: flex;
	align-items: center;
	gap: 8px;
	flex: 0 0 auto;
}

.wsp-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
}

.wsp-hint {
	color: var(--el-text-color-secondary);
	cursor: help;
}

.wsp-actions {
	display: flex;
	align-items: center;
	gap: 6px;
	flex: 0 0 auto;
	margin-left: auto;
	white-space: nowrap;

	.el-button + .el-button {
		margin-left: 0;
	}
}

.wsp-key-tag {
	margin: 0;
}

.wsp-muted {
	color: var(--el-text-color-placeholder);
	font-size: 12px;
}

.wsp-editor {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 8px;
	border: 1px solid var(--el-border-color-light);
	border-radius: 4px;
	background: var(--el-fill-color-blank);

	&.is-scope-locked {
		background: var(--el-fill-color-lighter);

		:deep(.sql-editor-bar) {
			background: var(--el-fill-color-lighter);
		}
	}
}

.wsp-sql :deep(.el-textarea__inner) {
	font-family: Consolas, 'Courier New', monospace;
	font-size: 12px;
	line-height: 1.5;
}

.wsp-checks {
	margin: 0;
	padding: 0;
	list-style: none;
	display: flex;
	flex-direction: column;
	gap: 4px;
	font-size: 12px;

	li {
		display: flex;
		align-items: flex-start;
		gap: 6px;
	}
}

.wsp-check-error {
	color: var(--el-color-danger);
}

.wsp-check-warning {
	color: var(--el-color-warning-dark-2);
}

.wsp-check-ok {
	display: flex;
	align-items: center;
	gap: 6px;
	font-size: 12px;
	color: var(--el-color-success);
}

.wsp-affected-section {
	display: flex;
	flex-direction: column;
	gap: 6px;
	padding: 8px 12px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 4px;
	background: var(--el-fill-color-lighter);

	.wsp-meta-item {
		display: flex;
		align-items: center;
		gap: 8px;
		flex-wrap: wrap;
		min-height: 24px;
	}

	.wsp-meta-label {
		display: inline-flex;
		align-items: center;
		gap: 4px;
		font-size: 12px;
		font-weight: 600;
		color: var(--el-text-color-regular);
		white-space: nowrap;
	}

	.wsp-meta-tags {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 6px;
	}
}

.sql-lock-alert .sql-lock-toggle {
	height: 20px;
	padding: 0 4px;
	margin-left: 4px;
	font-size: 12px;
	font-weight: 600;
	vertical-align: baseline;
}
</style>
