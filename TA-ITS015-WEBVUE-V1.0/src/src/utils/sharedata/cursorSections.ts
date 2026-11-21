import { EntityFieldLength } from '/@/types/enums/baseEnum';

/** Một nhóm mốc: nhóm mặc định (group = null, cột __lastKey) hoặc nhóm có tên của câu gộp nhiều nguồn (biến lastKey_<nhóm>). */
export interface CursorSection {
	group: string | null;
	lastKey: string;
	/** Mốc bắt đầu của câu SQL (DefaultValue): lượt gửi lấy theo mốc này khi đối tác chưa có mốc. */
	start?: string;
}

/* Giữ khớp SqlVariableSet.CursorGroupOf (BE): biến lastKey_<nhóm> thuộc nhóm mốc đó, biến khác thuộc nhóm mặc định */
const CURSOR_GROUP_PATTERN = /^lastKey_([A-Za-z0-9_]+)$/i;
export const cursorGroupOf = (code: string): string | null => code.match(CURSOR_GROUP_PATTERN)?.[1] ?? null;
export const cursorColumn = (base: string, section: CursorSection) => (section.group ? `${base}_${section.group}` : base);

/** Gom biến mốc (nguồn lastKey) theo nhóm mốc kèm mốc đã lưu; nhóm mặc định đứng đầu. */
export const buildCursorSections = (items: { code: string; source: string; savedValue?: string | null; defaultValue?: string | null }[]): CursorSection[] => {
	const sections = new Map<string, CursorSection>();
	for (const item of items) {
		if (item.source !== 'lastKey') continue;
		const group = cursorGroupOf(item.code);
		sections.set((group ?? '').toLowerCase(), { group, lastKey: item.savedValue || '', start: item.defaultValue || '' });
	}
	return [...sections.entries()].sort(([a], [b]) => a.localeCompare(b)).map(([, section]) => section);
};

/** Mốc đã lưu hiện ở bảng mốc: nhóm có tên thêm tiền tố "<nhóm>: ", các nhóm cách nhau bằng dấu chấm phẩy. */
export const cursorSavedText = (sections: CursorSection[]): string | null => {
	const parts = sections.filter((section) => section.lastKey).map((section) => (section.group ? `${section.group}: ${section.lastKey}` : section.lastKey));
	return parts.length > 0 ? parts.join('; ') : null;
};

/** Nhóm đã nhập mốc khóa. */
export const isCursorSectionFilled = (section: CursorSection) => Boolean((section.lastKey || '').trim());

const SELECT_PREFIX = /^[\s\S]*?\bSELECT\b(?:\s+DISTINCT\b)?(?:\s+TOP\s*(?:\([^)]*\)|\S+)(?:\s+PERCENT\b)?(?:\s+WITH\s+TIES\b)?)?/i;

/** Vị trí biểu thức của cột có bí danh alias trong SELECT: đi lùi từ bí danh tới dấu phẩy ngang cấp hoặc đầu SELECT. */
const selectColumnRange = (sql: string, alias: string): { start: number; end: number } | null => {
	const match = new RegExp(`\\bAS\\s+\\[?${alias}\\]?(?![A-Za-z0-9_])`, 'i').exec(sql);
	if (!match) return null;
	let depth = 0;
	let start = match.index;
	while (start > 0) {
		const ch = sql[start - 1];
		if (ch === ')') depth++;
		if (ch === '(' && --depth < 0) break;
		if (ch === ',' && depth === 0) break;
		start--;
	}
	const raw = sql.slice(start, match.index);
	const prefix = raw.match(SELECT_PREFIX)?.[0] ?? '';
	return { start: start + prefix.length, end: match.index };
};

/** Biểu thức của cột có bí danh alias trong SELECT. */
export const selectColumnExpression = (sql: string, alias: string): string | null => {
	const range = selectColumnRange(sql, alias);
	return range ? sql.slice(range.start, range.end).replace(/\s+/g, ' ').trim() : null;
};

/** Vị trí cuối cùng của từ khóa ở cấp ngoài cùng, bỏ qua ngoặc, chuỗi và chú thích; không có thì -1. */
const topLevelIndex = (sql: string, keyword: RegExp): number => {
	const sticky = new RegExp(`(?<![A-Za-z0-9_@])(?:${keyword.source})(?![A-Za-z0-9_])`, 'iy');
	let depth = 0;
	let found = -1;
	for (let i = 0; i < sql.length; i++) {
		const ch = sql[i];
		if (ch === "'") {
			i = sql.indexOf("'", i + 1);
			if (i < 0) break;
		} else if (sql.startsWith('--', i)) {
			i = sql.indexOf('\n', i);
			if (i < 0) break;
		} else if (sql.startsWith('/*', i)) {
			i = sql.indexOf('*/', i);
			if (i < 0) break;
		} else if (ch === '(') {
			depth++;
		} else if (ch === ')') {
			depth--;
		} else if (depth === 0) {
			sticky.lastIndex = i;
			if (sticky.test(sql)) found = i;
		}
	}
	return found;
};

/** Câu gộp nhiều SELECT bằng UNION: không tự sửa được, vì mỗi nhánh một công thức. */
export const hasTopLevelUnion = (sql: string) => topLevelIndex(sql, /UNION/) >= 0;

/** Vị trí kết thúc phần WHERE: mệnh đề kế tiếp hoặc cuối câu. */
const whereEnd = (sql: string) => {
	const next = [/GROUP\s+BY/, /HAVING/, /ORDER\s+BY/].map((k) => topLevelIndex(sql, k)).filter((i) => i >= 0);
	return next.length > 0 ? Math.min(...next) : sql.trimEnd().length;
};

/** Tách điều kiện WHERE theo AND ngang cấp. */
const splitTopLevelAnd = (where: string): string[] => {
	const parts: string[] = [];
	let rest = where;
	for (let at = topLevelIndex(rest, /AND/); at >= 0; at = topLevelIndex(rest, /AND/)) {
		parts.unshift(rest.slice(at + 3));
		rest = rest.slice(0, at);
	}
	parts.unshift(rest);
	return parts.map((p) => p.trim()).filter(Boolean);
};

export type CursorFixKind = 'columns' | 'filter' | 'order';

/** Sửa một mục quy chuẩn mốc của câu SQL nhóm mặc định, giữ nguyên phần còn lại của câu. */
export const applyCursorFix = (sql: string, kind: CursorFixKind, formula: string, column = '__lastKey', variable = '@lastKey'): string => {
	if (kind === 'columns') {
		const range = selectColumnRange(sql, column);
		if (range) {
			const leading = sql.slice(range.start, range.end).match(/^\s*/)?.[0] || ' ';
			return `${sql.slice(0, range.start)}${leading}${formula} ${sql.slice(range.end)}`;
		}
		const prefix = sql.match(SELECT_PREFIX)?.[0] ?? '';
		return `${prefix}\n    ${formula} AS ${column},${sql.slice(prefix.length)}`;
	}
	if (kind === 'filter') {
		const condition = `${formula} > ${variable}`;
		const where = topLevelIndex(sql, /WHERE/);
		const end = whereEnd(sql);
		if (where < 0) return `${sql.slice(0, end).trimEnd()}\nWHERE ${condition}\n${sql.slice(end)}`.trimEnd();
		// Giữ các điều kiện khác của người dùng, chỉ thay các vế có biến mốc hoặc so chính công thức mốc với biến tên khác
		const normalize = (text: string) => text.replace(/\s+/g, ' ').trim().toLowerCase();
		const others = splitTopLevelAnd(sql.slice(where + 5, end)).filter(
			(part) => !/@last(Time|Key)(_|\b)/i.test(part) && !normalize(part).replace(/^\(+\s*/, '').startsWith(`${normalize(formula)} >`)
		);
		return `${sql.slice(0, where)}WHERE ${[condition, ...others].join('\n  AND ')}\n${sql.slice(end)}`.trimEnd();
	}
	const order = topLevelIndex(sql, /ORDER\s+BY/);
	const replacement = `ORDER BY ${column} ASC`;
	if (order < 0) return `${sql.trimEnd()}\n${replacement}`;
	const offset = topLevelIndex(sql, /OFFSET/);
	return `${sql.slice(0, order)}${replacement}${offset > order ? `\n${sql.slice(offset)}` : ''}`;
};

/** Bí danh (hoặc tên) của bảng đầu tiên sau FROM, dùng để gợi ý cột khi câu chưa có cột mốc. */
export const firstTableAlias = (sql: string): string | null => {
	const match = /\bFROM\s+([\w.[\]]+)(?:\s+(?:AS\s+)?([A-Za-z_]\w*))?/i.exec(sql);
	if (!match) return null;
	const alias = match[2];
	if (alias && !/^(WHERE|LEFT|RIGHT|INNER|OUTER|CROSS|FULL|JOIN|ORDER|GROUP|HAVING|WITH|ON|UNION)$/i.test(alias)) return alias;
	return match[1];
};

/* Giữ khớp SqlValidatorHelper.CursorFormulaTime, HasNonNullKeyTail (BE): cột mốc đúng một dạng
   CONVERT(varchar(8), <thời gian>, 112) + REPLACE(CONVERT(varchar(12), <thời gian>, 114), ':', '') + '_' + <đuôi>, ra chuỗi yyyyMMddHHmmssfff_<đuôi> */
const CURSOR_TIME_PART =
	/^CONVERT\s*\(\s*n?varchar\s*\(\s*8\s*\)\s*,([\s\S]+?),\s*112\s*\)\s*\+\s*REPLACE\s*\(\s*CONVERT\s*\(\s*n?varchar\s*\(\s*12\s*\)\s*,([\s\S]+?),\s*114\s*\)\s*,\s*N?':'\s*,\s*N?''\s*\)/i;
/** Dấu '_' ngăn phần thời gian với đuôi; nhóm 1 là đuôi. */
const KEY_SEPARATOR = /^\s*\+\s*N?'_'(?:\s*\+([\s\S]*))?$/i;
/** Phần thời gian ghép kiểu khác ở đầu cột mốc: CONVERT(varchar(n), <thời gian>, <kiểu>). */
const OTHER_TIME_PART = /^CONVERT\s*\(\s*n?varchar\s*\(\s*\d+\s*\)\s*,([\s\S]+?),\s*\d+\s*\)/i;
const CREATE_TIME_COLUMN = /(?<![\w])\[?CreateTime\]?(?![\w\]])/i;
const STRING_LITERAL = /^N?'(?:''|[^'])*'$/i;
const ID_COLUMN = /^(?:\[?[A-Za-z_]\w*\]?\.)?\[?ID\]?$/i;
const PLAIN_COLUMN = /^(?:\[?[A-Za-z_]\w*\]?\.)?\[?[A-Za-z_]\w*\]?$/;
const NON_COLUMN_WORD = /^(?:AS|NULL|N?VARCHAR|N?CHAR|INT|BIGINT|DECIMAL|NUMERIC|DATE|DATETIME2?|MAX)$/i;

/** Cặp ngoặc mở ở đầu chuỗi đóng đúng ở cuối chuỗi (bỏ qua chuỗi hằng). */
const isWrapped = (text: string) => {
	if (!text.startsWith('(')) return false;
	let depth = 0;
	for (let i = 0; i < text.length; i++) {
		const ch = text[i];
		if (ch === "'") {
			i = text.indexOf("'", i + 1);
			if (i < 0) return false;
		} else if (ch === '(') {
			depth++;
		} else if (ch === ')' && --depth === 0) {
			return i === text.length - 1;
		}
	}
	return false;
};

/** Bỏ cặp ngoặc bọc trọn biểu thức. */
const unwrap = (expr?: string | null) => {
	let text = (expr ?? '').trim();
	while (isWrapped(text)) text = text.slice(1, -1).trim();
	return text;
};

/** Tách biểu thức theo dấu sep ở cấp ngoài cùng, bỏ qua ngoặc và chuỗi hằng. */
const splitTopLevel = (expr: string, sep: string): string[] => {
	const parts: string[] = [];
	let depth = 0;
	let start = 0;
	for (let i = 0; i < expr.length; i++) {
		const ch = expr[i];
		if (ch === "'") {
			const end = expr.indexOf("'", i + 1);
			if (end < 0) break;
			i = end;
		} else if (ch === '(') {
			depth++;
		} else if (ch === ')') {
			depth--;
		} else if (ch === sep && depth === 0) {
			parts.push(expr.slice(start, i).trim());
			start = i + 1;
		}
	}
	parts.push(expr.slice(start).trim());
	return parts;
};

const sameExpr = (a: string, b: string) => a.replace(/\s+/g, '').toLowerCase() === b.replace(/\s+/g, '').toLowerCase();

/** Phần thời gian đúng công thức của một biểu thức thời gian. */
const cursorTimePart = (time: string) => `CONVERT(varchar(8), ${time}, 112) + REPLACE(CONVERT(varchar(12), ${time}, 114), ':', '')`;

/** Phần đuôi luôn có giá trị: chuỗi hằng, cột ID, hoặc ISNULL / COALESCE có giá trị cuối là chuỗi hằng. */
const isNonNullTailPart = (part: string) => {
	const text = unwrap(part);
	if (STRING_LITERAL.test(text) || ID_COLUMN.test(text)) return true;
	const call = text.match(/^(?:ISNULL|COALESCE)\s*(\([\s\S]*\))$/i);
	if (!call || !isWrapped(call[1])) return false;
	const args = splitTopLevel(call[1].slice(1, -1), ',');
	return args.length >= 2 && STRING_LITERAL.test(args[args.length - 1]);
};

/** Phần đuôi dựa trên cột: ngoài chuỗi hằng còn tên cột (không tính tên hàm, kiểu dữ liệu, biến @). */
const referencesColumn = (part: string) =>
	[...part.replace(/N?'(?:''|[^'])*'/gi, "''").matchAll(/(?<![\w@#$.\]])(\[[^\]]*\]|[A-Za-z_][\w$#]*)(?![\w$#])(?!\s*\()/g)].some((m) => !NON_COLUMN_WORD.test(m[1]));

/** Đuôi không thể NULL: mỗi phần ghép bằng + luôn có giá trị, có ít nhất một phần dựa trên cột. */
const hasNonNullKeyTail = (tail: string) => {
	const parts = splitTopLevel(tail, '+');
	return parts.every((part) => Boolean(part) && isNonNullTailPart(part)) && parts.some(referencesColumn);
};

/** Cột mốc có phần thời gian đúng công thức ở đầu: Sửa câu giữ phần thời gian và đuôi người viết đã có. */
export const hasTimeKeyFormula = (expr?: string | null) =>
	CURSOR_TIME_PART.test(unwrap((expr ?? '').replace(/--[^\r\n]*/g, '').replace(/\/\*[\s\S]*?(?:\*\/|$)/g, '')));

/** Cột mốc đúng công thức: hai chỗ thời gian giống nhau và có CreateTime nên dòng nào cũng có mốc, đuôi không thể NULL nên chuỗi mốc không thành NULL. */
export const isCursorFormula = (expr?: string | null) => {
	const text = unwrap(expr);
	const time = text.match(CURSOR_TIME_PART);
	const tail = time ? text.slice(time[0].length).match(KEY_SEPARATOR) : null;
	if (!time || !tail) return false;
	return sameExpr(time[1], time[2]) && CREATE_TIME_COLUMN.test(time[1]) && hasNonNullKeyTail((tail[1] ?? '').trim());
};

/** Cột mốc ghép ngày giờ kiểu khác ở đầu (CONVERT(varchar(n), <thời gian>, <kiểu>)): đổi phần đó sang công thức đúng, giữ thời gian và đuôi. */
export const withTimeKeyFormula = (expr: string) => {
	const text = unwrap(expr);
	if (CURSOR_TIME_PART.test(text)) return expr;
	const time = text.match(OTHER_TIME_PART);
	return time ? `${cursorTimePart(time[1].trim())}${text.slice(time[0].length)}` : expr;
};

/** Công thức mốc mà phần thời gian chưa có CreateTime: bọc COALESCE(<thời gian>, <bảng>.CreateTime); hai chỗ thời gian viết lại giống nhau. */
export const withCreateTimeInFormula = (expr: string, createTime: string) => {
	const text = unwrap(expr);
	const time = text.match(CURSOR_TIME_PART);
	if (!time) return expr;
	const value = time[1].trim();
	return `${cursorTimePart(CREATE_TIME_COLUMN.test(value) ? value : `COALESCE(${value}, ${createTime})`)}${text.slice(time[0].length)}`;
};

/** Công thức mốc mà đuôi có thể NULL: cột trơn khác ID bọc ISNULL(<cột>, ''); chưa có đuôi thì điền cột ID của bảng. */
export const withNonNullKeyTail = (expr: string, idColumn: string) => {
	const text = unwrap(expr);
	const time = text.match(CURSOR_TIME_PART);
	if (!time) return expr;
	const rest = text.slice(time[0].length);
	const separated = rest.match(KEY_SEPARATOR);
	const tail = (separated ? (separated[1] ?? '') : rest.replace(/^\s*\+/, '')).trim();
	const parts = splitTopLevel(tail, '+')
		.filter(Boolean)
		.map((part) => (PLAIN_COLUMN.test(part) && !ID_COLUMN.test(part) ? `ISNULL(${part}, '')` : part));
	return `${time[0]} + '_' + ${parts.length > 0 ? parts.join(' + ') : idColumn}`;
};

/* Giữ khớp SqlValidatorHelper.ValidateCursorTime (BE): UpdateTime trống khi thêm mới nên cột mốc phải lấy CreateTime thay vào */
const UPDATE_TIME_COLUMN = /(?<![\w.\]])((?:\[?[A-Za-z_]\w*\]?\.)?)\[?UpdateTime\]?(?![\w\]])/gi;
const UPDATE_TIME_FALLBACK = /\b(?:COALESCE|ISNULL)\s*\(\s*(?:\[?[A-Za-z_]\w*\]?\.)?\[?UpdateTime\]?\s*,/i;

/** Công thức mốc dùng UpdateTime trơn (chưa bọc COALESCE / ISNULL): dòng mới thêm chưa có UpdateTime nên không được gửi. */
export const hasBareUpdateTime = (expr?: string | null) =>
	new RegExp(UPDATE_TIME_COLUMN.source, 'i').test(expr ?? '') && !UPDATE_TIME_FALLBACK.test(expr ?? '');

/** Công thức mốc lấy CreateTime khi UpdateTime trống: UpdateTime trơn đổi thành COALESCE(<bảng>.UpdateTime, <bảng>.CreateTime). */
export const withCreateTimeFallback = (expr: string) =>
	hasBareUpdateTime(expr) ? expr.replace(UPDATE_TIME_COLUMN, (_match, prefix: string) => `COALESCE(${prefix}UpdateTime, ${prefix}CreateTime)`) : expr;

/* Giữ khớp PacketCommandHandler.IsCursorTimeKey (BE): mốc gõ tay là ngày giờ viết liền 8 tới 17 chữ số, hoặc đủ 17 chữ số rồi _<đuôi> (đuôi được để trống) */
const CURSOR_VALUE_PATTERN = /^(\d{8,17}|\d{17}_\S*)$/;
export const CURSOR_VALUE_MAX_LENGTH = EntityFieldLength.Length256;

/** Mốc gõ tay hợp lệ: bỏ khoảng trắng hai đầu thì khớp dạng trên, tối đa 256 ký tự; phần ngày giờ đệm 0 cho đủ 17 chữ số là ngày giờ có thật. */
export const isValidCursorValue = (value?: string | null) => {
	const text = (value ?? '').trim();
	if (text.length > CURSOR_VALUE_MAX_LENGTH || !CURSOR_VALUE_PATTERN.test(text)) return false;
	const digits = text.slice(0, 17).padEnd(17, '0');
	const part = (start: number, length: number) => Number(digits.slice(start, start + length));
	const year = part(0, 4);
	const month = part(4, 2);
	const day = part(6, 2);
	const isLeapYear = (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
	const daysInMonth = [31, isLeapYear ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][month - 1] ?? 0;
	return year >= 1 && day >= 1 && day <= daysInMonth && part(8, 2) <= 23 && part(10, 2) <= 59 && part(12, 2) <= 59;
};
