/**
 * Author: KienLT
 * Description: Đưa sự kiện VDS trực tiếp (SSE api/vds/vdsEvent/Stream, chưa có hồ sơ) về cùng dạng với icon sự cố
 *   (ListZoneIncidentOutput) để bản đồ đặt icon theo ZONE + LÝ TRÌNH như sự cố, thay cho việc bám vào marker của
 *   camera phát hiện — marker đó không tồn tại ở chế độ Block nên trước đây sự kiện mất icon.
 *   Hàm thuần: không gọi API, không đụng DOM.
 * Created date: 11/11/2026
 */
import type { EventSnapshotItemOutput } from '/@/api-services/vds';
import { StatusTypeEnum } from '/@/types/enums';

/** Tiền tố id để phân biệt với id hồ sơ sự cố thật. */
export const LIVE_EVENT_ID_PREFIX = 'vds:';

/** Icon sự kiện trực tiếp, cùng các trường bản đồ đọc ở icon sự cố + bản tin gốc để mở chi tiết. */
export interface LiveEventIcon {
    incidentId: string;
    listZoneId: string[];
    eventGroup: 'event';
    eventTypeId: string;
    eventTypeName: string;
    kmNumber: number;
    metNumber: number;
    direction: string;
    liveEvent: EventSnapshotItemOutput;
}

const toPosition = (km: unknown, met: unknown) => (Number(km) || 0) * 1000 + (Number(met) || 0);

/**
 * Vùng cấp nhỏ nhất chứa lý trình của sự kiện. PHẢI cùng luật với BE `IncidentPositionHelper.Zones` (luật gán
 * ZoneId khi tạo sự cố từ sự kiện) để icon sự kiện và icon sự cố tạo từ chính nó nằm cùng một zone:
 * khoảng nửa mở [from, to) kể cả zone khai lý trình giảm dần; lọc theo khu vực tuyến và hướng (zone.laneId).
 */
export const resolveLiveEventZoneIds = (item: EventSnapshotItemOutput, zoneList: any[]): string[] => {
    if (item.kmNumber == null) return [];
    const position = toPosition(item.kmNumber, item.metNumber);
    const location = item.location ?? '';
    const direction = item.influenceScope ?? '';
    const anyDirection = !direction || direction === '0';

    const matched = (zoneList || []).filter((zone: any) => {
        if (zone?.isDelete != null || (zone?.status != null && zone.status !== StatusTypeEnum.Enable)) return false;
        if (location && String(zone.location ?? '') !== String(location)) return false;
        if (!anyDirection && zone.laneId != null && String(zone.laneId) !== String(direction)) return false;
        const from = toPosition(zone.fromKmNumber, zone.fromMetNumber);
        const to = toPosition(zone.toKmNumber, zone.toMetNumber);
        return (from <= position && to > position) || (from > position && to <= position);
    });
    const parentIds = new Set(matched.map((zone: any) => String(zone.pid ?? '')));
    return matched.filter((zone: any) => !parentIds.has(String(zone.id))).map((zone: any) => String(zone.id));
};

/**
 * Chuyển danh sách sự kiện trực tiếp thành icon đặt theo zone. Sự kiện thiếu lý trình hoặc không rơi vào zone nào
 * thì bỏ qua (như hồ sơ sự cố không có ZoneId). Sắp theo key để thứ tự vẽ ổn định giữa các lần nhận snapshot.
 */
export const toLiveEventIcons = (items: EventSnapshotItemOutput[], zoneList: any[], eventTypeList: any[]): LiveEventIcon[] =>
    [...(items || [])]
        .filter((item) => !!item?.key)
        .sort((a, b) => String(a.key).localeCompare(String(b.key)))
        .flatMap((item) => {
            const listZoneId = resolveLiveEventZoneIds(item, zoneList);
            if (!listZoneId.length) return [];
            const code = String(item.eventTypeCode ?? '').toLowerCase();
            const eventType = code ? (eventTypeList || []).find((type: any) => String(type?.code ?? '').toLowerCase() === code) : null;
            return [
                {
                    incidentId: LIVE_EVENT_ID_PREFIX + item.key,
                    listZoneId,
                    eventGroup: 'event' as const,
                    eventTypeId: String(eventType?.id ?? ''),
                    eventTypeName: item.eventTypeName ?? eventType?.name ?? '',
                    kmNumber: Number(item.kmNumber) || 0,
                    metNumber: Number(item.metNumber) || 0,
                    direction: item.influenceScope ?? '',
                    liveEvent: item,
                },
            ];
        });

/** Dấu vân tay chỉ gồm thứ ảnh hưởng tới việc vẽ icon — số lần phát hiện/giờ cập nhật đổi thì không vẽ lại bản đồ. */
export const liveEventIconFingerprint = (icons: LiveEventIcon[]) =>
    JSON.stringify(icons.map((icon) => [icon.incidentId, icon.listZoneId, icon.eventTypeId, icon.kmNumber, icon.metNumber, icon.liveEvent.icon ?? '']));
