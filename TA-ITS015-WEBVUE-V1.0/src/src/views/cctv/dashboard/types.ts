export interface DashboardCCTVOutput {
    id: string;
    name: string;
    remark: string;
    tags?: string[];
    createdAt: string;
    updatedAt: string;
    cameraCount?: number;
}



export type DashboardCameraInfo = {
    id: string;
    code?: string;
    name: string;
    ip?: string;
    location: string;
    status: 'online' | 'warning' | 'offline';
    aspect: string;
    videoUrl: string;
    x: number;
    y: number;
    w: number;
    h: number;
    equipmentTypeId?: string;
    equipmentTypeName?: string;
    snapshotUrl?: string;
    snapshotTime?: string;
};

export type VideoMonitoringItem = {
	id: string;
	sourceCameraId?: string;
	deviceId?: string;
	ip?: string;
	name: string;
	location: string;
	status: string;
	aspect: string;
	videoUrl: string;
	x: number;
	y: number;
	w: number;
	h: number;
	isPlaceholder?: boolean;
	slotNo?: number;
};

export type DashboardCCTVDetailOutput = DashboardCCTVOutput & {
	cameras: VideoMonitoringItem[];
};



export const dashboardGridConfig = {
	column: 6,
	row: 6,
	minRow: 6,
	maxRow: 6,
	margin: 8,
	float: true,
	animate: false,
	minW: 1,
	minH: 1,
	defaultW: 2,
	defaultH: 2,
	maxItemsPerRow: 6,
};

const legacyDashboardGridConfig = {
	column: 6,
	row: 6,
};

const toFiniteNumber = (value: unknown, fallback: number) => {
	try {
		const nextValue = Number(value);
		return Number.isFinite(nextValue) ? nextValue : fallback;
	} catch {
		return fallback;
	}
};

const clampNumber = (value: unknown, min: number, max: number) => Math.max(min, Math.min(toFiniteNumber(value, min), max));

const scaleGridValue = (value: unknown, legacyMax: number, currentMax: number) => Math.round((toFiniteNumber(value, 0) / legacyMax) * currentMax);

const getSquareGridSize = (item: Pick<VideoMonitoringItem, 'w' | 'h'>) => {
	const rawW = toFiniteNumber(item.w, dashboardGridConfig.defaultW);
	const rawH = toFiniteNumber(item.h, dashboardGridConfig.defaultH);
	const usesLegacyShape = rawW > dashboardGridConfig.column || rawH > dashboardGridConfig.row || rawW !== rawH;
	const width = usesLegacyShape ? Math.ceil((rawW / legacyDashboardGridConfig.column) * dashboardGridConfig.column) : rawW;
	const height = usesLegacyShape ? Math.ceil((rawH / legacyDashboardGridConfig.row) * dashboardGridConfig.row) : rawH;
	return clampNumber(Math.max(width, height, dashboardGridConfig.minW), dashboardGridConfig.minW, Math.min(dashboardGridConfig.column, dashboardGridConfig.row));
};

export const normalizeCameraGridItem = <T extends Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>>(item: T): T => {
	const rawItemX = toFiniteNumber(item.x, 0);
	const rawItemY = toFiniteNumber(item.y, 0);
	const rawW = toFiniteNumber(item.w, dashboardGridConfig.defaultW);
	const rawH = toFiniteNumber(item.h, dashboardGridConfig.defaultH);
	const usesLegacyPosition = rawItemX + rawW > dashboardGridConfig.column || rawItemY + rawH > dashboardGridConfig.row || rawW !== rawH;
	const size = getSquareGridSize(item);
	const rawX = usesLegacyPosition ? scaleGridValue(rawItemX, legacyDashboardGridConfig.column, dashboardGridConfig.column) : rawItemX;
	const rawY = usesLegacyPosition ? scaleGridValue(rawItemY, legacyDashboardGridConfig.row, dashboardGridConfig.row) : rawItemY;
	const x = clampNumber(rawX, 0, dashboardGridConfig.column - size);
	const y = clampNumber(rawY, 0, dashboardGridConfig.row - size);
	return { ...item, x, y, w: size, h: size };
};

// TEMPLATE CONFIG


export type DashboardTemplateStatus = 0 | 1;

export interface ICctvDashboardTemplateSlot {
	SlotNo: number;
	X: number;
	Y: number;
	W: number;
	H: number;
}

export interface ICctvDashboardTemplate {
	ID: string;
	Name: string;
	Status: DashboardTemplateStatus;
	Items: ICctvDashboardTemplateSlot[];
}

const TEMPLATE_COLUMNS = 6;
const TEMPLATE_ROWS = 6;
const TEMPLATE_MIN_W = 1;
const TEMPLATE_MIN_H = 1;

const createItem = (slotNo: number, x: number, y: number, w: number, h: number): ICctvDashboardTemplateSlot => {
	const itemX = Math.max(0, Math.min(x, TEMPLATE_COLUMNS - TEMPLATE_MIN_W));
	const itemY = Math.max(0, Math.min(y, TEMPLATE_ROWS - TEMPLATE_MIN_H));
	const itemSize = Math.max(TEMPLATE_MIN_W, Math.min(Math.max(w, h), TEMPLATE_COLUMNS - itemX, TEMPLATE_ROWS - itemY));
	return {
		SlotNo: slotNo,
		X: itemX,
		Y: itemY,
		W: itemSize,
		H: itemSize,
	};
};

const createEqualItems = (cameraCount: number): ICctvDashboardTemplateSlot[] => {
	const slotColumns = Math.ceil(Math.sqrt(cameraCount));
	const slotRows = Math.ceil(cameraCount / slotColumns);
	const itemSize = Math.max(TEMPLATE_MIN_W, Math.min(Math.floor(TEMPLATE_COLUMNS / slotColumns), Math.floor(TEMPLATE_ROWS / slotRows)));

	return Array.from({ length: cameraCount }, (_, index) => {
		const colIndex = index % slotColumns;
		const rowIndex = Math.floor(index / slotColumns);
		const x = colIndex * itemSize;
		const y = rowIndex * itemSize;
		return createItem(index + 1, x, y, itemSize, itemSize);
	});
};

const normalizeTemplateItems = (items: ICctvDashboardTemplateSlot[]) =>
	items.map((item) => createItem(item.SlotNo, item.X, item.Y, item.W, item.H));

const createTemplate = (
	orderNo: number,
	input: {
        ID: string;
        Name: string;
        Status: DashboardTemplateStatus;
		Items: ICctvDashboardTemplateSlot[];
	}
): ICctvDashboardTemplate => ({
	...input,
	Items: normalizeTemplateItems(input.Items)
});

export const cameraDashboardTemplates: ICctvDashboardTemplate[] = [
	createTemplate(1, {
		ID: 'template-1-equal',
		Name: '1 camera',
		Status: 1,
		Items: [createItem(1, 0, 0, 6, 6)],
	}),
	createTemplate(2, {
		ID: 'template-4-equal',
		Name: '4 cameras equal',
		Status: 1,
		Items: [
			createItem(1, 0, 0, 3, 3),
			createItem(2, 3, 0, 3, 3),
			createItem(3, 0, 3, 3, 3),
			createItem(4, 3, 3, 3, 3),
		],
	}),
	createTemplate(3, {
		ID: 'template-6-focus',
		Name: '6 cameras focus first',
		Status: 1,
		Items: [
			createItem(1, 0, 0, 4, 4),
			createItem(2, 4, 0, 2, 2),
			createItem(3, 4, 2, 2, 2),
			createItem(4, 0, 4, 2, 2),
			createItem(5, 2, 4, 2, 2),
			createItem(6, 4, 4, 2, 2),
		],
	}),
	createTemplate(4, {
		ID: 'template-9-equal',
		Name: '9 cameras equal',
		Status: 1,
		Items: createEqualItems(9),
	}),
	createTemplate(5, {
		ID: 'template-12-dual-focus',
		Name: '12 cameras dual focus',
		Status: 1,
		Items: [
			createItem(1, 0, 0, 3, 3),
			createItem(2, 3, 0, 3, 3),
			...Array.from({ length: 10 }, (_, index) => {
				const colIndex = index % 5;
				const rowIndex = Math.floor(index / 5);
				return createItem(index + 3, colIndex, 3 + rowIndex, 1, 1);
			}),
		],
	}),
	createTemplate(6, {
		ID: 'template-16-equal',
		Name: '16 cameras equal',
		Status: 1,
		Items: createEqualItems(16),
	}),
	createTemplate(7, {
		ID: 'template-20-focus',
		Name: '20 cameras focus first',
		Status: 1,
		Items: createEqualItems(20),
	}),
	createTemplate(8, {
		ID: 'template-24-equal',
		Name: '24 cameras equal',
		Status: 1,
		Items: createEqualItems(24),
	}),
];
