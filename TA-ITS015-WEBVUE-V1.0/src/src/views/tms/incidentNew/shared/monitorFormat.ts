export type ConfigOption = {
	id?: string | null;
	code?: string | null;
	name?: string | null;
	type?: string | null;
	url?: string | null;
};
export function metadataValues(metadata?: string | null): Record<string, string | number> {
	try {
		const data = JSON.parse(metadata || '{}');
		if (!data || typeof data !== 'object' || Array.isArray(data)) return {};
		return Object.fromEntries(Object.entries(data).filter(([, value]) => (typeof value === 'string' && value !== '') || (typeof value === 'number' && Number.isFinite(value)))) as Record<
			string,
			string | number
		>;
	} catch {
		return {};
	}
}

export function plateOf(row: { licensePlate?: string | null; metadata?: string | null }): string {
	return String(row.licensePlate || metadataValues(row.metadata).VehiclePlate || '—');
}

export function positionOf(row: { kmNumber?: number | null; metNumber?: number | null }, unknown = '—'): string {
	if (row.kmNumber == null && row.metNumber == null) return unknown;
	return `Km ${row.kmNumber ?? 0}+${String(row.metNumber ?? 0).padStart(3, '0')}`;
}
