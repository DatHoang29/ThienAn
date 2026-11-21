<template>
	<div class="sql-editor-container">
		<!-- Khối Khung soạn thảo Monaco Editor -->
		<div class="sql-editor-bar">
			<div class="sql-editor-bar-left" @click="toggleEditorCollapsed">
				<span class="sql-editor-title">{{ props.title || (props.partnerName ? $t('lz.label.sharedataDataSource.sqlEditorPartnerTitle', { partner: props.partnerName }) : $t('lz.label.sharedataDataSource.sqlEditorTitle')) }}</span>
				<el-tooltip v-if="props.titleTooltip" placement="top" :content="props.titleTooltip">
					<el-icon class="sql-editor-hint ml5" @click.stop><ele-InfoFilled /></el-icon>
				</el-tooltip>
				<slot name="title-extra" />
			</div>
			<div class="sql-editor-bar-actions">
				<!-- Chỉ icon (tiết kiệm bề rộng), tên thao tác ở tooltip, giống khối Trường dữ liệu -->
				<el-tooltip :content="editorCollapsed ? $t('lz.button.sharedataDataSource.sqlEditorExpandBtn') : $t('lz.button.sharedataDataSource.sqlEditorCollapseBtn')" placement="top">
					<el-button
						text
						size="small"
						type="primary"
						class="sql-editor-collapse-btn"
						:icon="editorCollapsed ? 'ele-ArrowDown' : 'ele-ArrowUp'"
						:aria-label="editorCollapsed ? $t('lz.button.sharedataDataSource.sqlEditorExpandBtn') : $t('lz.button.sharedataDataSource.sqlEditorCollapseBtn')"
						@click="toggleEditorCollapsed"
					/>
				</el-tooltip>
			</div>
		</div>
		<el-collapse-transition>
			<div v-show="!editorCollapsed" class="sql-monaco-section">
				<div
					ref="monacoContainerRef"
					class="sql-monaco-editor"
					:style="{ height: `${rawEditorHeight}px` }"
				/>

				<!-- Thanh kéo ngang điều chỉnh chiều cao -->
				<div
					class="sql-editor-resizer"
					:class="{ 'is-resizing': isResizingRawEditor }"
					:title="$t('lz.label.sharedataDataSource.dragToResize')"
					@mousedown="handleStartResizeRawEditor"
					@dblclick="resetRawEditorHeight"
				>
					<div class="resizer-handle-line" />
				</div>

				<!-- Fallback input container for tests and screen readers -->
				<div class="sql-raw-textarea-fallback" style="position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0); border: 0;">
					<textarea :value="internalSql" @input="(e: Event) => handleRawSqlChanged((e.target as HTMLTextAreaElement).value)" />
				</div>
			</div>
		</el-collapse-transition>
	</div>
</template>

<script lang="ts" setup name="sqlEditor">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { debounce } from 'lodash-es';
import { Local } from '/@/utils/storage';
import { fetchSourceTableColumns, findSourceTable, loadSourceTables, matchesPacketCode } from '/@/utils/sharedata/sourceTables';

const { t: $t } = useI18n();

// Bản đồ bảng nguồn liên quan theo chuẩn nghiệp vụ 02-mapping-goi-tin-101-111.md
interface TableColumnMeta {
	columnName: string;
	dataType?: string;
}

// Bảng không có trong danh mục bảng nguồn (hoặc chưa nạp được danh mục) thì không có cột để gợi ý
const fetchTableColumnsMeta = async (tableName: string): Promise<TableColumnMeta[]> => {
	const fields = await fetchSourceTableColumns(tableName);
	return fields
		.map((f: any) => ({
			columnName: String(f.columnName || f.name || '').trim(),
			dataType: String(f.dataType || '').trim(),
		}))
		.filter((f: TableColumnMeta) => Boolean(f.columnName));
};

const SQL_RESERVED_WORDS = new Set([
	'SELECT', 'FROM', 'WHERE', 'JOIN', 'LEFT', 'RIGHT', 'INNER', 'OUTER', 'CROSS', 'APPLY',
	'ON', 'AND', 'OR', 'NOT', 'IN', 'IS', 'NULL', 'ORDER', 'BY', 'GROUP', 'HAVING', 'AS',
	'TOP', 'DISTINCT', 'ASC', 'DESC', 'UNION', 'ALL', 'WITH', 'NOLOCK',
]);

const parseSqlTableAliases = (sqlText: string): { table: string; alias?: string }[] => {
	const result: { table: string; alias?: string }[] = [];
	const seen = new Set<string>();
	const addEntry = (rawTable?: string, rawAlias?: string) => {
		const cleanTable = (rawTable || '').replace(/[\[\]]/g, '').trim();
		if (!cleanTable || SQL_RESERVED_WORDS.has(cleanTable.toUpperCase())) return;
		let cleanAlias = (rawAlias || '').replace(/[\[\]]/g, '').trim();
		if (cleanAlias && SQL_RESERVED_WORDS.has(cleanAlias.toUpperCase())) {
			cleanAlias = '';
		}
		const dedupKey = `${cleanTable.toLowerCase()}|${cleanAlias.toLowerCase()}`;
		if (seen.has(dedupKey)) return;
		seen.add(dedupKey);
		result.push({ table: cleanTable, alias: cleanAlias || undefined });
	};

	const fromRegex = /\bFROM\s+([A-Za-z0-9_#$\[\]]+)(?:\s+(?:AS\s+)?([A-Za-z0-9_#$\[\]]+))?/gi;
	let m: RegExpExecArray | null;
	while ((m = fromRegex.exec(sqlText)) !== null) {
		addEntry(m[1], m[2]);
	}
	const joinRegex = /\bJOIN\s+([A-Za-z0-9_#$\[\]]+)(?:\s+(?:AS\s+)?([A-Za-z0-9_#$\[\]]+))?/gi;
	while ((m = joinRegex.exec(sqlText)) !== null) {
		addEntry(m[1], m[2]);
	}
	const applyRegex = /\bAPPLY\s*\(\s*SELECT\b[\s\S]*?\bFROM\s+([A-Za-z0-9_#$\[\]]+)(?:\s+(?:AS\s+)?([A-Za-z0-9_#$\[\]]+))?[\s\S]*?\)\s+(?:AS\s+)?([A-Za-z0-9_#$\[\]]+)/gi;
	while ((m = applyRegex.exec(sqlText)) !== null) {
		addEntry(m[1], m[3]);
	}
	return result;
};

const prefetchTablesInSql = (sqlText: string) => {
	const entries = parseSqlTableAliases(sqlText);
	for (const entry of entries) {
		fetchTableColumnsMeta(entry.table);
	}
};

interface ExtractedColumnItem {
	fieldKey?: string;
	name?: string;
	dataType?: string;
	assigned?: boolean;
}

/** Trường đã khai của gói tin: nhận thẳng model Swagger nên thuộc tính chuỗi có thể null */
interface SavedFieldItem {
	id?: string | null;
	aliasFieldKey?: string | null;
	fieldKey?: string | null;
	name?: string | null;
	isRequired?: number;
	fieldType?: string | null;
	status?: number;
}

/** Biến đã khai ở bảng tham số câu truy vấn (tên không có @, mô tả hiện cạnh gợi ý). */
interface DeclaredVariableItem {
	code: string;
	desc?: string;
}

const props = defineProps<{
	modelValue?: string;
	packetCode?: string | null;
	partnerName?: string | null;
	savedFields?: SavedFieldItem[];
	detectedColumns?: ExtractedColumnItem[];
	variables?: DeclaredVariableItem[];
	/** Tiêu đề thanh trên khung soạn; trống thì dùng tiêu đề câu truy vấn (mẫu chung / đối tác). */
	title?: string | null;
	/** Chú thích tooltip bên cạnh tiêu đề khung soạn. */
	titleTooltip?: string | null;
	/** Chiều cao tối thiểu / tối đa tự giãn theo nội dung (mặc định 120 / 200). */
	minHeight?: number;
	maxHeight?: number;
	/** Khoá lưu trạng thái Thu gọn (mỗi khung soạn một khoá). */
	collapseKey?: string;
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', val: string): void;
	(e: 'change', val: string): void;
	(e: 'columns-change', columns: string[]): void;
}>();

const internalSql = ref(props.modelValue || '');
const debouncedSqlForChips = ref(props.modelValue || '');

const updateDebouncedChipsAndEmit = debounce((val: string) => {
	debouncedSqlForChips.value = val;
	emit('update:modelValue', val);
	emit('change', val);
}, 450);

let editorInstance: any = null;
let monacoApi: any = null;
let completionProviderDisposable: any = null;
const ERROR_MARKER_OWNER = 'sharedata-sql-error';

/** Đánh dấu đỏ vị trí lỗi (dòng, cột đếm từ 1) và đưa con trỏ tới đó. */
const setErrorMarker = (line: number, column: number, message: string) => {
	const model = editorInstance?.getModel?.();
	if (!model || !monacoApi || line < 1 || column < 1 || line > model.getLineCount()) return;
	const word = model.getWordAtPosition({ lineNumber: line, column });
	monacoApi.editor.setModelMarkers(model, ERROR_MARKER_OWNER, [
		{
			severity: monacoApi.MarkerSeverity.Error,
			message,
			startLineNumber: line,
			startColumn: word?.startColumn ?? column,
			endLineNumber: line,
			endColumn: word?.endColumn ?? column + 1,
		},
	]);
	editorInstance.revealLineInCenterIfOutsideViewport?.(line);
	editorInstance.setPosition?.({ lineNumber: line, column });
	editorInstance.focus?.();
};

/** Đưa con trỏ về trình soạn thảo khi lỗi không có vị trí dòng/cột để đánh dấu. */
const focusEditor = () => {
	editorInstance?.focus?.();
};

const clearErrorMarker = () => {
	const model = editorInstance?.getModel?.();
	if (model && monacoApi) monacoApi.editor.setModelMarkers(model, ERROR_MARKER_OWNER, []);
};
const monacoContainerRef = ref<HTMLDivElement | null>(null);

const computeSqlContentHeight = (sqlText: string): number => {
	const lines = (sqlText || '').split('\n').length;
	return Math.max(props.minHeight ?? 120, Math.min(lines * 20 + 24, props.maxHeight ?? 200));
};

const rawEditorHeight = ref(computeSqlContentHeight(props.modelValue || ''));
const isResizingRawEditor = ref(false);
const isManuallyResized = ref(false);
let resizeStartYRaw = 0;
let startRawEditorHeight = 0;
let latestClientYRaw = 0;
let rawEditorRafId: number | null = null;

const adjustEditorHeightToContent = (preserveScroll = false) => {
	if (!editorInstance) {
		rawEditorHeight.value = computeSqlContentHeight(internalSql.value);
		return;
	}
	const contentHeight = editorInstance.getContentHeight?.() || 0;
	const rawH = contentHeight > 0 ? contentHeight : computeSqlContentHeight(editorInstance.getValue?.() || internalSql.value);
	const minH = props.minHeight ?? 120;
	const maxH = props.maxHeight ?? 200;
	const targetHeight = Math.max(minH, Math.min(Math.ceil(rawH) + 4, maxH));
	if (Math.abs(rawEditorHeight.value - targetHeight) > 2) {
		rawEditorHeight.value = targetHeight;
		nextTick(() => {
			editorInstance?.layout?.();
			if (!preserveScroll) {
				scrollToTop();
			}
		});
	}
};

const scrollToTop = () => {
	if (!editorInstance) return;
	editorInstance.setScrollPosition({ scrollTop: 0, scrollLeft: 0 });
	editorInstance.setPosition({ lineNumber: 1, column: 1 });
	editorInstance.revealLine(1);
};

/* Gợi ý từ khóa, hàm và mẫu câu T-SQL. Monaco chỉ tô màu ngôn ngữ sql chứ không tự gợi ý nên phải khai ở đây. */

/** Từ khóa chỉ đọc. Cố ý không có INSERT, UPDATE, DELETE, DROP, ALTER, EXEC, INTO: trình kiểm tra SQL chỉ nhận một câu SELECT nên gõ các từ này sẽ bị từ chối khi lưu. */
const SQL_KEYWORDS = [
	'SELECT', 'DISTINCT', 'TOP', 'PERCENT', 'FROM', 'WHERE', 'GROUP BY', 'HAVING', 'ORDER BY', 'ASC', 'DESC', 'AS',
	'JOIN', 'INNER JOIN', 'LEFT JOIN', 'RIGHT JOIN', 'FULL JOIN', 'CROSS JOIN', 'CROSS APPLY', 'OUTER APPLY', 'ON',
	'AND', 'OR', 'NOT', 'IN', 'EXISTS', 'BETWEEN', 'LIKE', 'IS NULL', 'IS NOT NULL',
	'CASE', 'WHEN', 'THEN', 'ELSE', 'END', 'UNION', 'UNION ALL', 'INTERSECT', 'EXCEPT', 'WITH', 'OVER', 'PARTITION BY', 'NULL',
];

/** Hàm T-SQL hay dùng khi trích dữ liệu. Không đưa vào OPENROWSET, OPENQUERY, OPENDATASOURCE, OPENXML, sp_*, xp_* vì server chặn. args là phần trong ngoặc theo cú pháp snippet của Monaco. */
const SQL_FUNCTIONS: { name: string; args: string; detail: string }[] = [
	{ name: 'ISNULL', args: '${1:expr}, ${2:value}', detail: 'ISNULL(expr, value)' },
	{ name: 'COALESCE', args: '${1:expr1}, ${2:expr2}', detail: 'COALESCE(expr1, expr2, ...)' },
	{ name: 'NULLIF', args: '${1:expr1}, ${2:expr2}', detail: 'NULLIF(expr1, expr2)' },
	{ name: 'IIF', args: '${1:condition}, ${2:whenTrue}, ${3:whenFalse}', detail: 'IIF(condition, whenTrue, whenFalse)' },
	{ name: 'CAST', args: '${1:expr} AS ${2:type}', detail: 'CAST(expr AS type)' },
	{ name: 'TRY_CAST', args: '${1:expr} AS ${2:type}', detail: 'TRY_CAST(expr AS type)' },
	{ name: 'CONVERT', args: '${1:type}, ${2:expr}', detail: 'CONVERT(type, expr [, style])' },
	{ name: 'TRY_CONVERT', args: '${1:type}, ${2:expr}', detail: 'TRY_CONVERT(type, expr [, style])' },
	{ name: 'FORMAT', args: '${1:value}, ${2:\'format\'}', detail: 'FORMAT(value, format)' },
	{ name: 'GETDATE', args: '', detail: 'GETDATE()' },
	{ name: 'SYSDATETIME', args: '', detail: 'SYSDATETIME()' },
	{ name: 'GETUTCDATE', args: '', detail: 'GETUTCDATE()' },
	{ name: 'DATEADD', args: '${1:day}, ${2:number}, ${3:date}', detail: 'DATEADD(datepart, number, date)' },
	{ name: 'DATEDIFF', args: '${1:day}, ${2:startDate}, ${3:endDate}', detail: 'DATEDIFF(datepart, startDate, endDate)' },
	{ name: 'DATEPART', args: '${1:hour}, ${2:date}', detail: 'DATEPART(datepart, date)' },
	{ name: 'DATENAME', args: '${1:month}, ${2:date}', detail: 'DATENAME(datepart, date)' },
	{ name: 'EOMONTH', args: '${1:date}', detail: 'EOMONTH(date [, offset])' },
	{ name: 'YEAR', args: '${1:date}', detail: 'YEAR(date)' },
	{ name: 'MONTH', args: '${1:date}', detail: 'MONTH(date)' },
	{ name: 'DAY', args: '${1:date}', detail: 'DAY(date)' },
	{ name: 'LEN', args: '${1:text}', detail: 'LEN(text)' },
	{ name: 'LEFT', args: '${1:text}, ${2:length}', detail: 'LEFT(text, length)' },
	{ name: 'RIGHT', args: '${1:text}, ${2:length}', detail: 'RIGHT(text, length)' },
	{ name: 'SUBSTRING', args: '${1:text}, ${2:start}, ${3:length}', detail: 'SUBSTRING(text, start, length)' },
	{ name: 'CHARINDEX', args: '${1:search}, ${2:text}', detail: 'CHARINDEX(search, text)' },
	{ name: 'REPLACE', args: '${1:text}, ${2:old}, ${3:new}', detail: 'REPLACE(text, old, new)' },
	{ name: 'CONCAT', args: '${1:text1}, ${2:text2}', detail: 'CONCAT(text1, text2, ...)' },
	{ name: 'UPPER', args: '${1:text}', detail: 'UPPER(text)' },
	{ name: 'LOWER', args: '${1:text}', detail: 'LOWER(text)' },
	{ name: 'LTRIM', args: '${1:text}', detail: 'LTRIM(text)' },
	{ name: 'RTRIM', args: '${1:text}', detail: 'RTRIM(text)' },
	{ name: 'TRIM', args: '${1:text}', detail: 'TRIM(text)' },
	{ name: 'ROUND', args: '${1:number}, ${2:digits}', detail: 'ROUND(number, digits)' },
	{ name: 'CEILING', args: '${1:number}', detail: 'CEILING(number)' },
	{ name: 'FLOOR', args: '${1:number}', detail: 'FLOOR(number)' },
	{ name: 'ABS', args: '${1:number}', detail: 'ABS(number)' },
	{ name: 'COUNT', args: '${1:*}', detail: 'COUNT(expr | *)' },
	{ name: 'SUM', args: '${1:expr}', detail: 'SUM(expr)' },
	{ name: 'AVG', args: '${1:expr}', detail: 'AVG(expr)' },
	{ name: 'MIN', args: '${1:expr}', detail: 'MIN(expr)' },
	{ name: 'MAX', args: '${1:expr}', detail: 'MAX(expr)' },
	{ name: 'ROW_NUMBER', args: '', detail: 'ROW_NUMBER() OVER (ORDER BY ...)' },
];

/** Mẫu câu bấm một lần. Mẫu nối đuôi dạng một chuỗi: cột mốc __lastKey ghép thời gian (CONVERT kiểu 121, UpdateTime trống thì lấy CreateTime) + '_' + khóa duy nhất,
 *  WHERE so chuỗi ghép với @lastKey, ORDER BY theo __lastKey; TOP (@pageSize) là số dòng mỗi trang.
 *  Bấm Lưu & phân tích thì bảng tham số tự nhận các biến và gán nguồn theo tên. */
const SQL_SNIPPETS: { label: string; detailKey: string; body: string[] }[] = [
	{
		label: 'select-incremental',
		detailKey: 'lz.label.sharedataDataSource.snippetSelectIncremental',
		body: [
			'SELECT TOP (@pageSize)',
			'       ${1:t}.${2:Column} AS ${3:aliasName},',
			"       CONVERT(varchar(23), COALESCE(${1:t}.${4:UpdateTime}, ${1:t}.CreateTime), 121) + '_' + ${1:t}.${5:ID} AS __lastKey",
			'FROM   ${6:TableName} ${1:t}',
			"WHERE  CONVERT(varchar(23), COALESCE(${1:t}.${4:UpdateTime}, ${1:t}.CreateTime), 121) + '_' + ${1:t}.${5:ID} > @lastKey",
			'ORDER BY __lastKey ASC',
		],
	},
	{
		label: 'select-snapshot',
		detailKey: 'lz.label.sharedataDataSource.snippetSelectSnapshot',
		body: ['SELECT ${1:t}.${2:Column} AS ${3:aliasName}', 'FROM   ${4:TableName} ${1:t}', 'WHERE  ${5:1 = 1}'],
	},
	{
		label: 'where-incremental',
		detailKey: 'lz.label.sharedataDataSource.snippetWhereIncremental',
		body: ["WHERE  CONVERT(varchar(23), COALESCE(${1:t}.${2:UpdateTime}, ${1:t}.CreateTime), 121) + '_' + ${1:t}.${3:ID} > @lastKey"],
	},
	{
		label: 'left-join-on',
		detailKey: 'lz.label.sharedataDataSource.snippetLeftJoin',
		body: ['LEFT JOIN ${1:TableName} ${2:j} ON ${2:j}.${3:Id} = ${4:t}.${5:Id}'],
	},
	{
		label: 'with-nolock',
		detailKey: 'lz.label.sharedataDataSource.snippetNoLock',
		body: ['WITH (NOLOCK)'],
	},
];

const initMonaco = async () => {
	if (!monacoContainerRef.value || editorInstance) return;
	try {
		const monaco = await import('monaco-editor');
		monacoApi = monaco;
		const isDark = document.documentElement.classList.contains('dark');
		editorInstance = monaco.editor.create(monacoContainerRef.value, {
			value: internalSql.value || '',
			language: 'sql',
			theme: isDark ? 'vs-dark' : 'vs',
			minimap: { enabled: false },
			wordWrap: 'on',
			lineNumbers: 'on',
			scrollBeyondLastLine: false,
			stickyScroll: { enabled: false },
			scrollbar: {
				alwaysConsumeMouseWheel: false,
				verticalScrollbarSize: 6,
				horizontalScrollbarSize: 6,
				useShadows: false,
			},
			automaticLayout: true,
			// Khung gợi ý / hover nổi ra ngoài khung soạn (không bị cắt ở mép dưới, tự lật lên trên khi thiếu chỗ)
			fixedOverflowWidgets: true,
			fontSize: 13,
			tabSize: 4,
		});

		scrollToTop();

		editorInstance.onDidChangeModelContent(() => {
			const val = editorInstance.getValue();
			if (val !== internalSql.value) {
				clearErrorMarker();
				internalSql.value = val;
				updateDebouncedChipsAndEmit(val);
			}
		});

		editorInstance.onDidBlurEditorText?.(() => {
			updateDebouncedChipsAndEmit.flush();
		});

		editorInstance.onDidContentSizeChange((e: { contentHeightChanged: boolean; contentHeight: number }) => {
			if (isResizingRawEditor.value || isManuallyResized.value) return;
			if (e.contentHeightChanged) {
				adjustEditorHeightToContent(true);
			}
		});

		adjustEditorHeightToContent();
		scrollToTop();
		prefetchTablesInSql(internalSql.value || '');

		// Gợi ý bảng, cột theo alias (zs.field) và biến hệ thống (@)
		if (!completionProviderDisposable && monaco.languages?.registerCompletionItemProvider) {
			completionProviderDisposable = monaco.languages.registerCompletionItemProvider('sql', {
				triggerCharacters: ['.', '@'],
				provideCompletionItems: async (model: any, position: any) => {
					// Gợi ý đăng ký chung cho ngôn ngữ sql: trang có nhiều khung soạn (chiều gửi + chiều nhận) thì mỗi khung chỉ trả gợi ý cho chính nó
					if (editorInstance && model !== editorInstance.getModel?.()) return { suggestions: [] };
					const fullSql = model.getValue?.() || internalSql.value || '';
					const lineBeforeCursor = model.getValueInRange({
						startLineNumber: position.lineNumber,
						startColumn: 1,
						endLineNumber: position.lineNumber,
						endColumn: position.column,
					});

					const word = model.getWordUntilPosition(position);
					const range = {
						startLineNumber: position.lineNumber,
						endLineNumber: position.lineNumber,
						startColumn: word.startColumn,
						endColumn: word.endColumn,
					};

					// 1. Khi gõ <alias>. hoặc <TableName>. (ví dụ: zs. hoặc zs.Upd)
					const dotMatch = lineBeforeCursor.match(/([A-Za-z0-9_]+)\.([A-Za-z0-9_]*)$/);
					if (dotMatch) {
						const prefixIdent = dotMatch[1];
						const prefixLower = prefixIdent.toLowerCase();
						const tableEntries = parseSqlTableAliases(fullSql);
						let resolvedTable = '';
						for (const entry of tableEntries) {
							if (entry.alias && entry.alias.toLowerCase() === prefixLower) {
								resolvedTable = entry.table;
								break;
							}
							if (entry.table.toLowerCase() === prefixLower) {
								resolvedTable = entry.table;
								break;
							}
						}
						if (!resolvedTable && (await findSourceTable(prefixIdent))) {
							resolvedTable = prefixIdent;
						}
						if (resolvedTable) {
							const cols = await fetchTableColumnsMeta(resolvedTable);
							return {
								suggestions: cols.map((col, idx) => ({
									label: col.columnName,
									kind: monaco.languages.CompletionItemKind.Field,
									insertText: col.columnName,
									detail: `${resolvedTable}.${col.columnName}${col.dataType ? ` (${col.dataType})` : ''}`,
									sortText: String(idx).padStart(3, '0'),
									range,
								})),
							};
						}
					}

					// 2. Khi gõ AS <alias> -> gợi ý các aliasFieldKey từ Danh sách trường kèm Tên trường tiếng Việt
					const asAliasMatch = lineBeforeCursor.match(/\bAS\s+([A-Za-z0-9_]*)$/i);
					if (asAliasMatch && (props.savedFields || []).length > 0) {
						const existingSet = new Set(
							parseSelectAliasesFromSql(fullSql).map((c) => (c.fieldKey || '').toLowerCase())
						);
						const asSuggestions = (props.savedFields || [])
							.filter((f) => {
								const k = (f.aliasFieldKey || f.fieldKey || '').trim();
								return Boolean(k) && !k.startsWith('__');
							})
							.map((f, idx) => {
								const aliasKey = (f.aliasFieldKey || f.fieldKey || '').trim();
								const isAlreadyUsed = existingSet.has(aliasKey.toLowerCase());
								return {
									label: aliasKey,
									kind: monaco.languages.CompletionItemKind.Property,
									insertText: aliasKey,
									detail: `${f.name || aliasKey}${f.fieldType ? ` (${f.fieldType})` : ''}`,
									sortText: `${isAlreadyUsed ? '1' : '0'}_${String(idx).padStart(3, '0')}`,
									range,
								};
							});
						if (asSuggestions.length > 0) {
							return { suggestions: asSuggestions };
						}
					}

					// 3. Khi gõ @: chỉ gợi ý biến đã khai ở bảng tham số câu truy vấn, chưa khai biến nào thì không gợi ý
					const paramMatch = lineBeforeCursor.match(/@([A-Za-z0-9_]*)$/);
					if (paramMatch) {
						const declared = (props.variables || []).filter((v) => Boolean(v.code));
						if (declared.length === 0) {
							return { suggestions: [] };
						}
						const paramRange = {
							startLineNumber: position.lineNumber,
							endLineNumber: position.lineNumber,
							startColumn: position.column - paramMatch[0].length,
							endColumn: position.column,
						};
						return {
							suggestions: declared.map((v) => ({
								label: `@${v.code}`,
								kind: monaco.languages.CompletionItemKind.Variable,
								insertText: `@${v.code}`,
								detail: v.desc || '',
								range: paramRange,
							})),
						};
					}

					// 3. Gợi ý chung: Bảng nguồn theo gói tin + <alias>.<column> của các bảng đang có trong FROM/JOIN
					const currentCode = (props.packetCode || '').trim();
					const numMatch = currentCode.match(/\d{3}/);
					const packetNum = numMatch ? numMatch[0] : '';
					// Bảng gắn với gói tin này theo danh mục bảng nguồn; gói chưa gắn bảng nào thì gợi ý mọi bảng được phép
					const catalogTables = await loadSourceTables();
					const packetTables = catalogTables.filter((t) => matchesPacketCode(t.packetCodes, currentCode));
					const relatedTables = (packetTables.length > 0 ? packetTables : catalogTables).map((t) => t.tableName);

					const suggestions: any[] = relatedTables.map((tableName) => ({
						label: tableName,
						kind: monaco.languages.CompletionItemKind.Class,
						insertText: tableName,
						detail: `Bảng nguồn gói ${packetNum || ''}`,
						range,
					}));

					const tableEntries = parseSqlTableAliases(fullSql);
					for (const entry of tableEntries) {
						const cols = await fetchTableColumnsMeta(entry.table);
						const prefix = entry.alias || entry.table;
						for (const col of cols) {
							const qualified = `${prefix}.${col.columnName}`;
							suggestions.push({
								label: qualified,
								kind: monaco.languages.CompletionItemKind.Field,
								insertText: qualified,
								detail: `${entry.table}.${col.columnName}${col.dataType ? ` (${col.dataType})` : ''}`,
								range,
							});
						}
					}

					// 4. Mẫu câu, từ khóa và hàm T-SQL: xếp sau bảng và cột (sortText bắt đầu bằng zz) để không lấn gợi ý của gói
					const snippetRule = monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet;
					for (const s of SQL_SNIPPETS) {
						suggestions.push({
							label: s.label,
							kind: monaco.languages.CompletionItemKind.Snippet,
							insertText: s.body.join('\n'),
							insertTextRules: snippetRule,
							detail: $t(s.detailKey),
							sortText: `zz1_${s.label}`,
							range,
						});
					}
					for (const kw of SQL_KEYWORDS) {
						suggestions.push({
							label: kw,
							kind: monaco.languages.CompletionItemKind.Keyword,
							insertText: kw,
							sortText: `zz2_${kw}`,
							range,
						});
					}
					for (const fn of SQL_FUNCTIONS) {
						suggestions.push({
							label: fn.name,
							kind: monaco.languages.CompletionItemKind.Function,
							insertText: `${fn.name}(${fn.args})`,
							insertTextRules: snippetRule,
							detail: fn.detail,
							sortText: `zz3_${fn.name}`,
							range,
						});
					}

					return { suggestions };
				},
			});
		}
	} catch (e) {
		console.error('Failed to load Monaco editor:', e);
	}
};

const handleRawSqlChanged = (val: string) => {
	internalSql.value = val;
	if (editorInstance && editorInstance.getValue() !== val) {
		editorInstance.setValue(val);
	}
	if (!isManuallyResized.value) {
		adjustEditorHeightToContent();
	}
	updateDebouncedChipsAndEmit(val);
};

const handleStartResizeRawEditor = (e: MouseEvent) => {
	e.preventDefault();
	isResizingRawEditor.value = true;
	isManuallyResized.value = true;
	resizeStartYRaw = e.clientY;
	latestClientYRaw = e.clientY;
	startRawEditorHeight = monacoContainerRef.value?.clientHeight || rawEditorHeight.value;
	document.body.style.userSelect = 'none';
	document.body.style.cursor = 'row-resize';
	window.addEventListener('mousemove', onResizingRawEditor, { passive: true });
	window.addEventListener('mouseup', stopResizeRawEditor);
};

const onResizingRawEditor = (e: MouseEvent) => {
	if (!isResizingRawEditor.value) return;
	latestClientYRaw = e.clientY;
	if (rawEditorRafId !== null) return;
	rawEditorRafId = requestAnimationFrame(() => {
		rawEditorRafId = null;
		if (!isResizingRawEditor.value) return;
		const deltaY = latestClientYRaw - resizeStartYRaw;
		const minH = 120;
		const wrapper = monacoContainerRef.value?.closest('.packet-details-wrapper') as HTMLElement | null;
		const maxH = wrapper ? Math.max(300, wrapper.clientHeight - 260) : 750;
		rawEditorHeight.value = Math.max(minH, Math.min(maxH, startRawEditorHeight + deltaY));
		editorInstance?.layout();
	});
};

const stopResizeRawEditor = () => {
	if (!isResizingRawEditor.value) return;
	isResizingRawEditor.value = false;
	if (rawEditorRafId !== null) {
		cancelAnimationFrame(rawEditorRafId);
		rawEditorRafId = null;
	}
	document.body.style.userSelect = '';
	document.body.style.cursor = '';
	window.removeEventListener('mousemove', onResizingRawEditor);
	window.removeEventListener('mouseup', stopResizeRawEditor);
	editorInstance?.layout();
};

const resetRawEditorHeight = () => {
	isManuallyResized.value = false;
	adjustEditorHeightToContent();
	nextTick(() => editorInstance?.layout());
};

const parseSelectAliasesFromSql = (sqlText: string): ExtractedColumnItem[] => {
	const raw = (sqlText || '')
		.replace(/\/\*[\s\S]*?\*\//g, ' ')
		.replace(/--[^\r\n]*/g, ' ');
	if (!raw.trim()) return [];

	// Tìm vị trí SELECT đầu tiên và FROM cấp 0 (ngoài ngoặc đơn)
	const selectMatch = raw.match(/\bSELECT\b/i);
	if (!selectMatch || selectMatch.index === undefined) return [];

	const startIdx = selectMatch.index + selectMatch[0].length;
	let depth = 0;
	let fromIdx = -1;
	const upper = raw.toUpperCase();
	for (let i = startIdx; i < raw.length; i++) {
		const ch = raw[i];
		if (ch === '(') depth++;
		else if (ch === ')') depth = Math.max(0, depth - 1);
		else if (depth === 0 && (ch === 'F' || ch === 'f')) {
			const prevChar = i > 0 ? raw[i - 1] : ' ';
			const nextChar = i + 4 < raw.length ? raw[i + 4] : ' ';
			if (
				!/[A-Za-z0-9_]/.test(prevChar) &&
				upper.slice(i, i + 4) === 'FROM' &&
				!/[A-Za-z0-9_]/.test(nextChar)
			) {
				fromIdx = i;
				break;
			}
		}
	}

	const selectClause = (fromIdx !== -1 ? raw.slice(startIdx, fromIdx) : raw.slice(startIdx))
		.replace(/^\s*DISTINCT\b/i, '')
		.replace(/^\s*TOP\s*(?:\(\s*[^)]+\s*\)|\d+)/i, '');

	// Tách danh sách biểu thức SELECT theo dấu phẩy cấp 0
	const expressions: string[] = [];
	let cur = '';
	depth = 0;
	for (let i = 0; i < selectClause.length; i++) {
		const ch = selectClause[i];
		if (ch === '(') depth++;
		else if (ch === ')') depth = Math.max(0, depth - 1);
		if (ch === ',' && depth === 0) {
			if (cur.trim()) expressions.push(cur.trim());
			cur = '';
		} else {
			cur += ch;
		}
	}
	if (cur.trim()) expressions.push(cur.trim());

	const result: ExtractedColumnItem[] = [];
	const seen = new Set<string>();
	for (const expr of expressions) {
		let fieldKey = '';
		// 1. Nếu có alias sau AS hoàn chỉnh: col AS alias
		const asMatch = expr.match(/\bAS\s+\[?([A-Za-z_][A-Za-z0-9_]*)\]?\s*$/i);
		if (asMatch) {
			fieldKey = asMatch[1];
		} else {
			// 2. Nếu đang gõ dở sau AS (ví dụ: "z.FromKmNumber AS " hoặc "z.FromKmNumber AS"):
			// Giữ lại tên cột gốc trước AS để giữ nguyên vị trí chip, không làm mất cột khiến các chip phía sau bị nhảy giật
			const beforeAsMatch = expr.match(/(?:[A-Za-z_][A-Za-z0-9_]*\.)?\[?([A-Za-z_][A-Za-z0-9_]*)\]?\s+\bAS\b\s*$/i);
			if (beforeAsMatch && !SQL_RESERVED_WORDS.has(beforeAsMatch[1].toUpperCase())) {
				fieldKey = beforeAsMatch[1];
			} else {
				// 3. Cột thông thường không có AS: col hoặc table.col
				const colMatch = expr.match(/(?:[A-Za-z_][A-Za-z0-9_]*\.)?\[?([A-Za-z_][A-Za-z0-9_]*)\]?\s*$/);
				if (colMatch && !SQL_RESERVED_WORDS.has(colMatch[1].toUpperCase())) {
					fieldKey = colMatch[1];
				}
			}
		}
		if (fieldKey && !seen.has(fieldKey.toLowerCase())) {
			seen.add(fieldKey.toLowerCase());
			result.push({ fieldKey, name: fieldKey });
		}
	}
	return result;
};

const nonSystemColumns = computed(() => {
	const detectedMap = new Map<string, ExtractedColumnItem>();
	for (const c of props.detectedColumns || []) {
		const key = (c.fieldKey || c.name || '').trim();
		if (key && !key.startsWith('__')) {
			detectedMap.set(key.toLowerCase(), { ...c, fieldKey: key });
		}
	}

	const liveParsed = parseSelectAliasesFromSql(debouncedSqlForChips.value).filter((c) => {
		const key = (c.fieldKey || '').trim();
		return Boolean(key) && !key.startsWith('__');
	});

	if (liveParsed.length > 0) {
		return liveParsed.map((c) => {
			const k = (c.fieldKey || '').trim();
			const existing = detectedMap.get(k.toLowerCase());
			return existing ? { ...existing, fieldKey: k } : { fieldKey: k, name: k };
		});
	}

	return Array.from(detectedMap.values());
});

let resizeObserver: ResizeObserver | null = null;

const layout = () => {
	nextTick(() => {
		if (editorInstance && monacoContainerRef.value) {
			const { clientWidth, clientHeight } = monacoContainerRef.value;
			if (clientWidth > 0 && clientHeight > 0) {
				if (!isManuallyResized.value) {
					adjustEditorHeightToContent();
				}
				editorInstance.layout();
			}
		}
	});
};

watch(
	() => props.modelValue,
	(newVal) => {
		const val = newVal || '';
		const isChanged = val !== internalSql.value;
		if (isChanged) {
			internalSql.value = val;
			if (editorInstance && editorInstance.getValue() !== val) {
				editorInstance.setValue(val);
			}
		}
		updateDebouncedChipsAndEmit.cancel();
		debouncedSqlForChips.value = val;
		prefetchTablesInSql(val);
		if (!isManuallyResized.value) {
			adjustEditorHeightToContent(!isChanged);
		}
		layout();
		if (isChanged) {
			nextTick(() => {
				scrollToTop();
			});
			setTimeout(() => {
				scrollToTop();
			}, 60);
		}
	}
);

watch(
	() => props.packetCode,
	() => {
		isManuallyResized.value = false;
		nextTick(() => {
			adjustEditorHeightToContent();
			scrollToTop();
		});
		setTimeout(() => {
			scrollToTop();
		}, 60);
	}
);

const handleMonacoWheel = (e: WheelEvent) => {
	if (!editorInstance || !monacoContainerRef.value) return;
	const layoutInfo = editorInstance.getLayoutInfo?.();
	const scrollHeight = editorInstance.getScrollHeight?.() || 0;
	const scrollTop = editorInstance.getScrollTop?.() || 0;
	const height = layoutInfo?.height || monacoContainerRef.value.clientHeight || 0;
	const atTop = scrollTop <= 0;
	const atBottom = scrollTop + height >= scrollHeight - 2;

	// Khi Monaco không còn nội dung để cuộn theo hướng lăn chuột (hoặc chiều cao editor vừa khít nội dung SQL)
	if ((e.deltaY < 0 && atTop) || (e.deltaY > 0 && atBottom) || scrollHeight <= height + 2) {
		let el: HTMLElement | null = monacoContainerRef.value.parentElement;
		while (el && el !== document.body) {
			const style = window.getComputedStyle(el);
			const overflowY = style.overflowY;
			if ((overflowY === 'auto' || overflowY === 'scroll') && el.scrollHeight > el.clientHeight) {
				const canScrollParentDown = el.scrollTop + el.clientHeight < el.scrollHeight - 1;
				const canScrollParentUp = el.scrollTop > 0;
				if ((e.deltaY > 0 && canScrollParentDown) || (e.deltaY < 0 && canScrollParentUp)) {
					el.scrollTop += e.deltaY;
					break;
				}
			}
			el = el.parentElement;
		}
	}
};

onMounted(async () => {
	await initMonaco();
	monacoContainerRef.value?.addEventListener('wheel', handleMonacoWheel, { passive: true });
	if (monacoContainerRef.value && typeof ResizeObserver !== 'undefined') {
		resizeObserver = new ResizeObserver(() => {
			if (editorInstance && monacoContainerRef.value && monacoContainerRef.value.clientHeight > 0) {
				editorInstance.layout();
			} else if (!editorInstance && monacoContainerRef.value && monacoContainerRef.value.clientHeight > 0) {
				initMonaco();
			}
		});
		resizeObserver.observe(monacoContainerRef.value);
	}
});

onUnmounted(() => {
	updateDebouncedChipsAndEmit.cancel();
	monacoContainerRef.value?.removeEventListener('wheel', handleMonacoWheel);
	if (resizeObserver) {
		resizeObserver.disconnect();
		resizeObserver = null;
	}
	if (completionProviderDisposable) {
		completionProviderDisposable.dispose();
		completionProviderDisposable = null;
	}
	if (editorInstance) {
		editorInstance.dispose();
		editorInstance = null;
	}
	if (rawEditorRafId !== null) {
		cancelAnimationFrame(rawEditorRafId);
		rawEditorRafId = null;
	}
	window.removeEventListener('mousemove', onResizingRawEditor);
	window.removeEventListener('mouseup', stopResizeRawEditor);
	if (expandLayoutTimer) {
		clearTimeout(expandLayoutTimer);
		expandLayoutTimer = null;
	}
});

// Báo danh sách cột SQL (đã bỏ cột hệ thống __) lên màn cha để danh sách trường hiện các cột chưa khai
watch(
	() => nonSystemColumns.value.map((c) => c.fieldKey).join('\u0001'),
	() => emit('columns-change', nonSystemColumns.value.map((c) => c.fieldKey)),
	{ immediate: true }
);

// Thu gọn trình soạn: ẩn hẳn khung soạn thảo (thanh biến và nút Lưu ở trên vẫn thấy), phần SQL co lại nhường chỗ cho danh sách trường
const EDITOR_COLLAPSED_KEY = props.collapseKey || 'sharedataDataSource:sqlEditorCollapsed';
const editorCollapsed = ref(Local.get(EDITOR_COLLAPSED_KEY) === true);
let expandLayoutTimer: ReturnType<typeof setTimeout> | null = null;
const toggleEditorCollapsed = () => {
	editorCollapsed.value = !editorCollapsed.value;
	Local.set(EDITOR_COLLAPSED_KEY, editorCollapsed.value);
	if (expandLayoutTimer) {
		clearTimeout(expandLayoutTimer);
		expandLayoutTimer = null;
	}
	// Mở ra thì Monaco phải tính lại kích thước ngay và sau khi hiệu ứng mở 300ms hoàn tất
	if (!editorCollapsed.value) {
		nextTick(() => {
			editorInstance?.layout();
			layout();
		});
		expandLayoutTimer = setTimeout(() => {
			editorInstance?.layout();
			layout();
			expandLayoutTimer = null;
		}, 320);
	}
};

defineExpose({ layout, setErrorMarker, clearErrorMarker, focusEditor, scrollToTop });
</script>

<style scoped lang="scss">
.sql-editor-container {
	display: flex;
	flex-direction: column;
	width: 100%;
	min-height: 0;
	overflow: hidden;
}

.sql-monaco-section {
	border-radius: var(--el-border-radius-base);
	border: 1px solid var(--el-border-color);
	background: var(--el-bg-color);
	overflow: hidden;
	display: flex;
	flex-direction: column;
	min-height: 0;

	.sql-monaco-editor {
		width: 100%;
		min-height: 120px;
	}
}

.sql-editor-resizer {
	height: 6px;
	flex-shrink: 0;
	background: var(--el-fill-color-light);
	border-top: 1px solid var(--el-border-color-lighter);
	cursor: row-resize;
	display: flex;
	align-items: center;
	justify-content: center;
	transition: background-color 0.2s;

	&:hover,
	&.is-resizing {
		background: var(--el-color-primary-light-9);
	}

	.resizer-handle-line {
		width: 36px;
		height: 2px;
		border-radius: 1px;
		background: var(--el-border-color-darker);
	}
}

/* Thanh phía trên trình soạn: tiêu đề bên trái, nút Thu nhỏ / Mở rộng nằm bên phải, dính ở đầu vùng cuộn để cuộn xuống vẫn bấm được; có nền để khung SQL cuộn qua không lộ phía sau */
.sql-editor-bar {
	position: sticky;
	top: 0;
	z-index: 5;
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 2px;
	background: var(--el-bg-color);
	flex-shrink: 0;

	.sql-editor-bar-left {
		display: flex;
		align-items: center;
		gap: 6px;
		cursor: pointer;
		user-select: none;
	}

	.sql-editor-title {
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	.sql-editor-hint {
		font-size: 14px;
		font-weight: 400;
		color: var(--el-text-color-secondary);
		cursor: help;
	}

	.sql-editor-bar-actions {
		display: flex;
		align-items: center;
		gap: 4px;
	}
}

</style>