/**
 * Các helper thuần phục vụ hiển thị danh sách và preview template CCTV.
 */

export type TemplateCreateTime = Date | string | number | null | undefined;

interface TemplateWithCreateTime {
	createTime?: TemplateCreateTime;
}

interface TemplateWithId {
	id?: string | null;
}

/** Chuyển thời gian API về timestamp; giá trị thiếu/sai được xếp cuối danh sách. */
const toTimestamp = (value: TemplateCreateTime): number => {
	if (value === null || value === undefined || value === '') return Number.NEGATIVE_INFINITY;
	const timestamp = value instanceof Date
		? value.getTime()
		: typeof value === 'number'
			? value
			: Date.parse(value);
	return Number.isFinite(timestamp) ? timestamp : Number.NEGATIVE_INFINITY;
};

/**
 * Sắp xếp template theo thời gian tạo giảm dần mà không thay đổi mảng nguồn.
 * Giữ nguyên thứ tự API khi hai bản ghi có cùng timestamp.
 */
export const sortTemplatesByCreateTimeDesc = <T extends TemplateWithCreateTime>(templates: readonly T[]): T[] =>
	templates
		.map((template, index) => ({ template, index, timestamp: toTimestamp(template.createTime) }))
		.sort((left, right) => right.timestamp - left.timestamp || left.index - right.index)
		.map(({ template }) => template);

/** Đưa template vừa tạo lên đầu nếu nó có trong kết quả tải lại. */
export const prioritizeTemplateById = <T extends TemplateWithId>(templates: readonly T[], templateId?: string): T[] => {
	const copiedTemplates = [...templates];
	if (!templateId) return copiedTemplates;
	const index = copiedTemplates.findIndex((template) => template.id === templateId);
	if (index <= 0) return copiedTemplates;
	const [createdTemplate] = copiedTemplates.splice(index, 1);
	return [createdTemplate, ...copiedTemplates];
};

/** Tạo CSS grid theo toàn bộ kích thước lưới cấu hình, không co theo bounding box của item. */
export const getFullGridStyle = (columns: number, rows: number) => {
	const safeColumns = Number.isFinite(columns) ? Math.max(1, Math.floor(columns)) : 1;
	const safeRows = Number.isFinite(rows) ? Math.max(1, Math.floor(rows)) : 1;
	return {
		gridTemplateColumns: `repeat(${safeColumns}, minmax(0, 1fr))`,
		gridTemplateRows: `repeat(${safeRows}, minmax(0, 1fr))`,
	};
};
