import type { PageTmsIncidentInput, PageTmsIncidentOutput } from '/@/api-services/tms';
import { TmsIncidentStateEnum } from '/@/types/enums';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import type { WorkspaceSort } from '/@/components/workspace/workspace';

export type IncidentRow = PageTmsIncidentOutput & { licensePlate?: string | null; vendorEventId?: number | null };
export type IncidentFilters = {
	keyword: string;
	dateRange: [Date, Date] | null;
	eventTypeId?: string;
	zoneId?: string;
	state?: string;
	source?: string;
	incidentGroup?: 'all' | 'event' | 'incident';
	fromKm?: number;
	fromMet?: number;
	toKm?: number;
	toMet?: number;
};

export function defaultFilters(): IncidentFilters {
	const end = new Date();
	end.setDate(end.getDate() + 1);
	end.setHours(0, 0, 0, 0);
	const start = new Date();
	start.setDate(start.getDate() - 90);
	start.setHours(0, 0, 0, 0);
	return { keyword: '', dateRange: [start, end], incidentGroup: 'all' };
}

export function rowKey(row: IncidentRow): string {
	return String(row.id ?? '');
}

export const stateLabelKeys: Record<string, string> = {
	[TmsIncidentStateEnum.Registration]: 'lz.label.tmsIncident.registrationState',
	[TmsIncidentStateEnum.Verified]: 'lz.label.tmsIncident.verifiedState',
	[TmsIncidentStateEnum.InProgress]: 'lz.label.tmsIncident.inProgressState',
	[TmsIncidentStateEnum.Finished]: 'lz.label.tmsIncident.finishedState',
	[TmsIncidentStateEnum.Skipped]: 'lz.label.tmsIncident.skippedState',
	[TmsIncidentStateEnum.Canceled]: 'lz.label.tmsIncident.canceledState',
};

// TMS local-time request serialization
// incident uses DateTimeValidInput (local wall time); trafficAnalysis sends Date values.
export function incidentDate(value?: Date): Date | undefined {
	if (!value) return undefined;
	return useDateFormat(value, DateTimeFormatEnum.DateTimeValidInput).value as unknown as Date;
}

export function incidentQuery(filters: IncidentFilters, page = 1, pageSize = 50, sort: WorkspaceSort = { field: 'startDate', order: 'desc' }): PageTmsIncidentInput {
	return {
		page,
		pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
		fromDate: incidentDate(filters.dateRange?.[0]),
		toDate: incidentDate(filters.dateRange?.[1]),
		name: filters.keyword.trim() || undefined,
		eventTypeId: filters.eventTypeId || undefined,
		zoneId: filters.zoneId || undefined,
		source: filters.source || undefined,
		state: filters.state || undefined,
		fromKm: filters.fromKm,
		fromMet: filters.fromMet,
		toKm: filters.toKm,
		toMet: filters.toMet,
		includeEvents: filters.incidentGroup !== 'incident',
		includeIncidents: filters.incidentGroup !== 'event',
	};
}
