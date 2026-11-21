/**
 * Author: KienLT
 * Description: Hiển thị unknown theo AID trong metadata lịch sử, không suy lại ánh xạ.
 * Created date: 19/10/2026
 */
import { i18n } from '/@/i18n';
import type { EventTypeOutput, TmsIncidentOutput } from '/@/api-services/tms';

type IncidentRow = Pick<TmsIncidentOutput, 'eventTypeId' | 'eventTypeCode' | 'eventTypeName' | 'metadata'>;
type EventTypeRow = Pick<EventTypeOutput, 'id' | 'code' | 'name'> & Partial<Pick<EventTypeOutput, 'url'>>;

export function incidentEventTypeDisplay(row?: IncidentRow | null, types?: EventTypeRow[] | null): string {
	if (!row) return '';
	const type = types?.find((item) => item.id === row.eventTypeId);
	if ((row.eventTypeCode ?? type?.code) === 'unknown') {
		try {
			// Hợp đồng là chuỗi JSON; giữ nhánh object cho dữ liệu đã parse sẵn từ luồng SSE sự cố.
			const raw: unknown = row.metadata;
			const metadata = typeof raw === 'string' ? JSON.parse(raw) : (raw as { AidEventType?: string } | null);
			if (metadata?.AidEventType) return i18n.global.t('lz.message.tms.unknownAidEvent', { aid: metadata.AidEventType });
		} catch {
			/* Metadata legacy có thể không phải JSON. */
		}
		return row.eventTypeName ?? i18n.global.t('lz.label.base.unknown');
	}
	return row.eventTypeName ?? type?.name ?? '';
}

/** Tìm loại sự cố của dòng theo id, nếu không có thì theo code (dòng sự kiện VDS chỉ có code). */
export function findIncidentEventType<T extends EventTypeRow>(row?: { eventTypeId?: string | null; eventTypeCode?: string | null } | null, types?: T[] | null): T | undefined {
	if (!row || !types?.length) return undefined;
	const byId = row.eventTypeId ? types.find((item) => item.id === row.eventTypeId) : undefined;
	if (byId) return byId;
	const code = String(row.eventTypeCode ?? '').trim().toLowerCase();
	return code ? types.find((item) => String(item.code ?? '').toLowerCase() === code) : undefined;
}
