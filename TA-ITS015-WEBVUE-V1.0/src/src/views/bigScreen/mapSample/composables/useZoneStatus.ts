import { expireZoneStatus, zoneUpdateTime } from '/@/utils/zone-status-expiry';
import { ref } from 'vue';
import type L from 'leaflet';
import { getZoneStatusSnapshot } from '/@/services/tms/traffic-status';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useServerTime } from '/@/stores/serverTimeStore';
import { BaseConfigTypeEnum, TmsZoneConditionEnum } from '/@/types/enums';

// Mã trạng thái zone hợp lệ — dùng để không nhầm mã (vd 'slow' là chữ cái thuần) thành tên màu CSS.
const ZONE_CONDITION_CODES = new Set<string>(Object.values(TmsZoneConditionEnum));

/**
 * Composable quản lý trạng thái giao thông (zone status) hiển thị trên bản đồ.
 *
 * - Tải danh sách trạng thái zone realtime từ API.
 * - Tô màu polygon zone theo trạng thái (ưu tiên dữ liệu realtime, sau đó tới
 *   itemDetail, cuối cùng fallback 'normal').
 *
 * Tách khỏi LeafletMap.vue để component gọn hơn. State `listZoneStatus` được
 * sở hữu nội bộ trong composable này (trước đây nằm trong state của component).
 */
export function useZoneStatus() {
    const serverTime = useServerTime();
    // #region State

    // Cấu hình tham số màu sắc cho zone status (đọc 1 lần khi khởi tạo)
    const listZoneStatusColor = useBaseConfig().getConfigDataByCode(BaseConfigTypeEnum.TmsZoneStatusColor);

    // Danh sách trạng thái zone realtime
    const listZoneStatus = ref<any[]>([]) as any;

    // Giữ các bản tin NATS nhận trong lúc API snapshot đang tải để dữ liệu mới không bị ghi đè.
    const latestNatsZoneStatuses = new Map<string, { zoneId: string; condition: string; updateTime: string }>();

    // #endregion

    // #region Data Loading

    /**
     * Load danh sách trạng thái zone.
     * Author: SonTH - Date: 2025/02/27
     */
    let zoneRequestVersion = 0;
    let staleAfterSeconds = 30;
    const applyZoneSnapshot = (snapshot: { staleAfterSeconds?: number; zones?: any[] }) => {
        if (!Array.isArray(snapshot?.zones)) return;
        // A stream snapshot supersedes an older HTTP request and includes expired/deleted rows.
        ++zoneRequestVersion;
        latestNatsZoneStatuses.clear();
        staleAfterSeconds = snapshot.staleAfterSeconds ?? 30;
        listZoneStatus.value = snapshot.zones.map(row => ({ ...row }));
        expireZoneStatuses();
    };
    const loadZoneStatus = async () => {
        const version = ++zoneRequestVersion;
        const response = await getZoneStatusSnapshot();
        if (version !== zoneRequestVersion) return;
        staleAfterSeconds = response.data.result?.staleAfterSeconds ?? 30;
        const snapshot = response.data.result?.zones ?? [];

        listZoneStatus.value = snapshot.map((item: any) => {
            const latestStatus = latestNatsZoneStatuses.get(String(item?.zoneId));
            return latestStatus && zoneUpdateTime(latestStatus.updateTime) > zoneUpdateTime(item.updateTime)
                ? { ...item, ...latestStatus, stale: false } : item;
        });

        latestNatsZoneStatuses.forEach((latestStatus, zoneId) => {
            if (!listZoneStatus.value.some((item: any) => String(item?.zoneId) === zoneId)) {
                listZoneStatus.value.push({ ...latestStatus, stale:
                    serverTime.serverNowMs() - zoneUpdateTime(latestStatus.updateTime) >= staleAfterSeconds * 1000 });
            }
        });
        expireZoneStatuses();
    };

    // #endregion

    const expireZoneStatuses = () => {
        const changed: string[] = [];
        listZoneStatus.value = listZoneStatus.value.map((row: any) => {
            const next = expireZoneStatus(row, serverTime.serverNowMs(), staleAfterSeconds);
            if (next.condition !== row.condition || next.stale !== row.stale) changed.push(String(row.zoneId));
            return next;
        });
        return changed;
    };

    // #region Zone Styling

    /**
     * Mức độ nghiêm trọng của 1 statusKey, dùng để suy ra TỐC ĐỘ dòng chảy vẽ dọc
     * centerline (flow-line): thông thoáng chảy nhanh, ùn tắc chảy chậm/gần như đứng.
     * Số càng lớn = tắc nặng hơn = chảy chậm hơn.
     */
    const STATUS_SEVERITY: Record<string, number> = {
        [TmsZoneConditionEnum.Normal]: 0, // Thông thoáng — chảy nhanh
        [TmsZoneConditionEnum.Slow]: 1, // Chậm — chảy vừa
        [TmsZoneConditionEnum.Congested]: 2, // Tắc nghẽn — chảy chậm
    };

    /**
     * Suy ra tốc độ chạy của flow-line (giây cho 1 vòng dash-offset) theo statusKey.
     * Trả về undefined nếu muốn TẮT flow-line (ví dụ tắc cứng hoàn toàn, đứng yên).
     */
    const getFlowDurationByStatus = (statusKey: string): number => {
        const severity = STATUS_SEVERITY[statusKey] ?? 0;
        // 0 -> 2.4s (nhanh), 1 -> 4.2s (vừa), 2 -> 7s (chậm, cảm giác nhích từng chút)
        return 2.4 + severity * 1.9;
    };

    /**
     * Tô "Route Ribbon" cho polygon zone theo trạng thái giao thông — thiết kế cho CHẾ ĐỘ
     * XEM (traffic info): fill LUÔN đủ đậm để thấy rõ (không có vùng gần-vô-hình), viền
     * chỉ là 1 đường mảnh trong-suốt-nhẹ cùng màu để phân định ranh giới đoạn mà KHÔNG
     * làm gãy cảm giác liên tục. Vì mọi zone liền kề trên cùng tuyến dùng đúng thiết kế
     * này, khi nối lại tổng thể hiện ra như MỘT DẢI ĐƯỜNG liên tục đổi màu theo tình
     * trạng giao thông (giống kiểu bản đồ traffic của Google/Waze).
     *
     * Trả về { statusColor, statusKey, flowDuration } để caller (useMapRender) vẽ thêm
     * flow-line dọc centerline cùng màu/tốc độ — tạo hiệu ứng "dòng xe chảy" trực quan.
     * Author: SonTH - Date: 2025/02/27 (redesigned)
     * @param polygon    L.Polygon của zone
     * @param itemDetail dữ liệu chi tiết zone (chứa additionalInfo.zoneStatusCondition)
     * @param _map       Không còn dùng (trước đây để tiêm gradient vào SVG defs) — giữ để không đổi chữ ký hàm
     */
    const applyZoneStatusStyle = async (polygon: any, itemDetail: any, _map?: L.Map | null): Promise<{ statusColor: string; statusKey: string; flowDuration: number }> => {
        // Tìm trạng thái zone realtime theo zoneId của polygon hiện tại
        // Nguồn dữ liệu này ưu tiên hơn itemDetail vì đây là dữ liệu status runtime
        const zoneStatus = listZoneStatus.value.find((item: any) => item.zoneId == polygon.options.id);

        // Style nền: viền RẤT mảnh, trong suốt nhẹ (chỉ đủ để mắt phân biệt 2 đoạn liền kề
        // khi zoom gần) — không dùng weight/opacity 0 để tránh trường hợp CSS/gradient lỗi
        // khiến polygon "biến mất" hoàn toàn như thiết kế trước.
        const polygonBaseStyle = {
            smoothFactor: 1.2,
            // Viền dày hơn + rõ hơn để mép dải đường sắc nét, nổi bật như đường được tô
            // sáng trong ảnh mẫu (trước đây viền quá mảnh/mờ nên dải nhìn nhạt).
            weight: 2,
            opacity: 0.9,
            // Tô MỘT MÀU đồng đều theo trạng thái (không gradient dọc theo bbox): gradient dọc
            // làm 2 zone song song lệch tông (zone trên lộ mép tối, zone dưới lộ mép sáng).
            fillOpacity: 0.95,
            lineJoin: 'round',
            lineCap: 'round',
        };

        // Áp dụng style cơ bản cho polygon trước
        polygon.setStyle(polygonBaseStyle);

        // Chuyển list cấu hình màu trạng thái thành object map: { <condition>: '<color>' }.
        // CẤU TRÚC config item: { code: <condition: 'normal'|'slow'|'conges' — TmsZoneConditionEnum>, name: <nhãn
        // hiển thị>, value: <mã màu hex> }. Vì vậy PHẢI khóa theo `code` (đúng với giá trị
        // zoneStatus.condition — xem trafficInfo/index.vue dùng getConfigItemName(type, condition)),
        // và lấy MÀU từ `value`. Trước đây map bị đảo (khóa theo name, màu lấy từ code) nên
        // condition không khớp key -> mọi zone rơi về defaultColor -> chỉ hiện 1 màu.
        const isColorLike = (s: string): boolean => /^#|^rgb|^hsl/i.test(s) || /^[a-zA-Z]+$/.test(s);
        const pickColor = (item: any): string => {
            // Màu chuẩn nằm ở 'value'. Dự phòng: 'code' hoặc 'name' nếu chúng là mã màu.
            const v = String(item?.value ?? '').trim();
            if (v) return v;
            const c = String(item?.code ?? '').trim();
            if (c && isColorLike(c) && !ZONE_CONDITION_CODES.has(c.toLowerCase())) return c;
            const n = String(item?.name ?? '').trim();
            if (n && /^#|^rgb|^hsl/i.test(n)) return n;
            return '';
        };
        const colorMap = Object.fromEntries(
            (listZoneStatusColor || [])
                .map((item: any) => [String(item.code || '').trim(), pickColor(item)])
                .filter(([key, color]: any) => key && color)
        );

        // Màu mặc định: ưu tiên màu 'normal' trong cấu hình, nếu không có thì fallback màu cứng
        const defaultColor = colorMap.normal ?? '#22C55E';

        // Xác định status hiện tại theo thứ tự ưu tiên:
        // 1. zoneStatus.condition (realtime) 2. itemDetail.additionalInfo.zoneStatusCondition 3. 'normal'
        // Chỉ tin snapshot (đã xét quá hạn). itemDetail.additionalInfo.zoneStatusCondition là giá trị thô, không xét
        // quá hạn — dùng làm dự phòng thì dòng cũ nhiều tháng vẫn tô chậm/nghẽn trước khi snapshot về.
        const statusKey = zoneStatus && !zoneStatus.stale
            ? String(zoneStatus.condition ?? TmsZoneConditionEnum.Normal).trim()
            : TmsZoneConditionEnum.Normal;

        // Lấy màu tương ứng; không tìm thấy thì dùng màu mặc định
        const statusColor = statusKey === 'unknown' ? '#94a3b8' : colorMap[statusKey] ?? defaultColor;

        // Fill một màu đồng đều theo trạng thái (viền đen là lớp riêng zone-outline trong useMapRender)
        polygon.setStyle({
            ...polygonBaseStyle,
            fillColor: statusColor,
            color: statusColor,
        });

        // Cấp biến CSS --zone-color để quầng sáng (.zone-polygon-status trong LeafletMap.vue)
        // đúng màu trạng thái. getElement() chỉ có sau khi polygon được add vào layer → thử
        // ngay và lùi 1 tick.
        const applyZoneColorVar = () => {
            const el = polygon.getElement && polygon.getElement();
            if (el) el.style.setProperty('--zone-color', statusColor);
            // Viền (lớp riêng, gắn ở useMapRender) cũng đổi tông theo trạng thái mới
            const outlineEl = polygon._zoneOutline?.getElement?.();
            if (outlineEl) outlineEl.style.setProperty('--zone-color', statusColor);
        };
        applyZoneColorVar();
        setTimeout(applyZoneColorVar, 0);

        return { statusColor, statusKey, flowDuration: getFlowDurationByStatus(statusKey) };
    };

    // #endregion

    /**
     * Gộp thay đổi condition nhận từ NATS vào danh sách realtime hiện có.
     * Trả về zoneId thật sự đổi condition để caller chỉ cập nhật phần bản đồ cần thiết.
     */
    const updateZoneStatusFromNats = (updates: Array<{ zoneId: string; condition: string; updateTime: string }>): string[] => {
        const changedZoneIds = new Set<string>();

        updates.forEach((update) => {
            if (!update?.zoneId || !update.condition) return;
            if (!Number.isFinite(zoneUpdateTime(update.updateTime)) || zoneUpdateTime(update.updateTime) > serverTime.serverNowMs()) return;

            const previous = listZoneStatus.value.find((item: any) => String(item.zoneId) === String(update.zoneId));
            if (zoneUpdateTime(update.updateTime) <= zoneUpdateTime(previous?.updateTime)) return;
            latestNatsZoneStatuses.set(String(update.zoneId), { ...update });
            const index = listZoneStatus.value.findIndex((item: any) => String(item?.zoneId) === String(update.zoneId));
            if (index === -1) {
                listZoneStatus.value.push({
                    zoneId: update.zoneId,
                    condition: update.condition,
                    stale: false,
                    updateTime: update.updateTime,
                });
                changedZoneIds.add(String(update.zoneId));
                return;
            }

            const conditionChanged = listZoneStatus.value[index]?.stale || String(listZoneStatus.value[index]?.condition) !== String(update.condition);
            listZoneStatus.value[index] = {
                ...listZoneStatus.value[index],
                condition: update.condition,
                sourceCondition: update.condition,
                stale: false,
                updateTime: update.updateTime,
            };

            if (conditionChanged) changedZoneIds.add(String(update.zoneId));
        });

        expireZoneStatuses().forEach(id => changedZoneIds.add(id));
        return [...changedZoneIds];
    };

    // #region Public API

    return {
        listZoneStatus,
        loadZoneStatus,
        applyZoneSnapshot,
        applyZoneStatusStyle,
        expireZoneStatuses,
        updateZoneStatusFromNats,
    };

    // #endregion
}
