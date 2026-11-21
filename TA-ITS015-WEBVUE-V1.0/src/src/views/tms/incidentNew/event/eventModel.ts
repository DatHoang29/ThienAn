import type { PageTrafficDataInput, PageTrafficDataOutput } from '/@/api-services/vds';

export type EventRow = PageTrafficDataOutput & {
	id: string;
	type?: string | null;
	length?: number | null;
	ipAddress?: string | null;
	metadata?: string | null;
	description?: string | null;
	remark?: string | null;
};
export type EventFilters = {
	keyword: string;
	dateRange: [Date, Date] | null;
	zoneId?: string;
	equipmentId?: string;
	direction?: string;
	eventType?: string;
	vehicleSize?: string;
	lengthFrom?: number;
	lengthTo?: number;
	speedFrom?: number;
	speedTo?: number;
};

// Tạo sự cố từ sự kiện: sự kiện cũ hơn ngưỡng này thì mặc định TẮT kịch bản tự động,
// tránh đổi biển báo/đóng làn bây giờ cho sự việc đã qua.
export const createIncidentAutomationMaxAgeMinutes = 10;

export function defaultFilters(): EventFilters {
	const end = new Date();
	end.setHours(23, 59, 59, 999);
	const start = new Date();
	start.setDate(start.getDate() - 3);
	start.setHours(0, 0, 0, 0);
	return { keyword: '', dateRange: [start, end] };
}

export function rowKey(row: PageTrafficDataOutput): string {
	return `${row.vendorEventId}@${String(row.detectTime)}`;
}

export function trafficRow(row: PageTrafficDataOutput): EventRow {
	return { ...row, id: rowKey(row), type: row.vehicleSize, length: row.lengthMm, ipAddress: row.cameraIp };
}

// VDS request serialization
export function trafficQuery(filters: EventFilters, page = 1, pageSize = 50): PageTrafficDataInput {
	return {
		page,
		pageSize,
		field: 'detectTime',
		order: 'desc',
		descStr: 'desc',
		fromDate: filters.dateRange?.[0],
		toDate: filters.dateRange?.[1],
		licensePlate: filters.keyword.trim() || undefined,
		equipmentId: filters.equipmentId || undefined,
		direction: filters.direction || undefined,
		// Tab Sự kiện chỉ lấy bản ghi có sự kiện: BE bỏ loại 'normal' (xe đi bình thường).
		eventsOnly: true,
		eventType: filters.eventType || undefined,
		vehicleSize: filters.vehicleSize || undefined,
		lengthFrom: filters.lengthFrom,
		lengthTo: filters.lengthTo,
		zoneId: filters.zoneId || undefined,
		speedFrom: filters.speedFrom,
		speedTo: filters.speedTo,
	};
}
