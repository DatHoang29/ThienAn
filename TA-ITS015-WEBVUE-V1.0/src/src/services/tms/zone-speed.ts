// Author: KienLT
// Description: API + luật thuần cho trang cấu hình tốc độ min/max theo zone.
// Created date: 08/10/2026
//
// Gọi API qua client sinh (TmsZoneApi). Kiểu dựa trên model sinh, chỉ thu hẹp `id` thành bắt buộc vì cây, nháp
// và tập chọn đều khoá theo id (bản ghi thiếu id bị bỏ khi nạp).
//
// `effectiveSpeed` PHẢI khớp `Services.Vds/Catalog/SpeedRangeResolver` (ForZone/MinSpeedOfZone):
//   - mỗi giá trị: zone khai thì dùng zone, thiếu thì lấy mặc định của hướng;
//   - hướng mặc định của zone = LaneId; LaneId rỗng hoặc "0" (= mọi hướng) thì lấy hướng camera VDS
//     (ưu tiên camera nhận zone này làm vùng chính);
//   - ghép xong mà thiếu một vế hoặc min > max ⇒ service bỏ qua (không đẩy xuống AID).

import { getAPI } from '/@/utils/axios-utils';
import { TmsZoneApi } from '/@/api-services/tms';
import type { ZoneDirectionDefaultOutput, ZoneSpeedConfigOutput, ZoneSpeedUpdateItemInput, ZoneVdsCameraOutput } from '/@/api-services/tms';

/** Trần ngưỡng — khớp `VdsConfigRules.MaxSpeedKmh` phía BE (AID chỉ nhận số nguyên). */
export const ZONE_SPEED_MAX_KMH = 300;

/** Camera VDS gắn với zone; `isPrimary` = zone này là VÙNG CHÍNH của camera ⇒ ngưỡng zone được đẩy xuống AID. */
export type ZoneSpeedCamera = ZoneVdsCameraOutput & { id: string };

export type ZoneSpeedRow = Omit<ZoneSpeedConfigOutput, 'vdsCameras'> & { id: string; vdsCameras?: ZoneSpeedCamera[] };

export type DirectionSpeedDefault = Required<Pick<ZoneDirectionDefaultOutput, 'minSpeed' | 'maxSpeed'>> & { direction: string };

export interface ZoneSpeedConfigList {
	directionDefaults: DirectionSpeedDefault[];
	zones: ZoneSpeedRow[];
}

const hasId = <T extends { id?: string | null }>(item: T): item is T & { id: string } => !!item.id;

export const getZoneSpeedConfigList = async (): Promise<ZoneSpeedConfigList> => {
	const response = await getAPI(TmsZoneApi).apiTmsTmszoneSpeedconfiglistGet();
	const result = response.data.result;
	return {
		directionDefaults: (result?.directionDefaults ?? [])
			.filter(d => !!d.direction && d.minSpeed != null && d.maxSpeed != null)
			.map(d => ({ direction: d.direction!, minSpeed: d.minSpeed!, maxSpeed: d.maxSpeed! })),
		zones: (result?.zones ?? []).filter(hasId).map(z => ({ ...z, vdsCameras: (z.vdsCameras ?? []).filter(hasId) })),
	};
};

/** Lưu một lô; trả số zone BE đã cập nhật. */
export const batchUpdateZoneSpeed = async (items: ZoneSpeedUpdateItemInput[]): Promise<number> => {
	const response = await getAPI(TmsZoneApi).apiTmsTmszoneBatchupdatespeedPost({ items });
	return response.data.result?.updated ?? 0;
};

// ═══════════════════════ Luật thuần ═══════════════════════

export type SpeedSource = 'zone' | 'mixed' | 'direction' | 'none';

/** Khoá i18n nhãn nguồn ngưỡng. */
export const SPEED_SOURCE_I18N: Record<SpeedSource, string> = {
	zone: 'lz.label.tmsZone.sourceZone',
	mixed: 'lz.label.tmsZone.sourceMixed',
	direction: 'lz.label.tmsZone.sourceDirection',
	none: 'lz.label.tmsZone.sourceNone',
};

export interface EffectiveSpeed {
	min: number | null;
	max: number | null;
	/** Có đủ cặp hợp lệ (min ≤ max) ⇒ service dùng được để đẩy xuống AID. */
	valid: boolean;
	source: SpeedSource;
	/** Hướng đã dùng để lấy mặc định; null = không xác định được hướng. */
	direction: string | null;
}

/** Số nguyên dương hợp lệ, hoặc null (= chưa khai). Khớp cách service làm tròn cột số thực của zone. */
export const toKmh = (value: number | null | undefined): number | null => {
	if (value == null || !Number.isFinite(value) || value <= 0) return null;
	const rounded = Math.round(value);
	return rounded > 0 ? rounded : null;
};

/** Hướng mặc định của zone: LaneId (trừ rỗng/"0"), không có thì hướng camera VDS (ưu tiên vùng chính). */
export const directionOfZone = (zone: ZoneSpeedRow): string | null => {
	const lane = zone.laneId?.toString().trim();
	if (lane && lane !== '0') return lane;
	const cameras = zone.vdsCameras ?? [];
	const camera = cameras.find(c => c.isPrimary && c.directionId) ?? cameras.find(c => c.directionId);
	return camera?.directionId?.toString().trim() || null;
};

/**
 * Ngưỡng của ZONE theo hướng của zone — khớp `SpeedRangeResolver.ForZone`/`MinSpeedOfZone`: dùng cho đánh giá
 * trạng thái vùng (V_min tính chậm). KHÔNG phải ngưỡng đẩy xuống AID — cái đó theo hướng CAMERA, xem `aidSpeeds`.
 * LaneId rỗng/"0": service lấy hướng của camera vừa gửi bản tin; ở đây lấy camera vùng chính làm đại diện.
 */
export const effectiveSpeed = (
	zone: ZoneSpeedRow,
	minSpeed: number | null | undefined,
	maxSpeed: number | null | undefined,
	defaults: Map<string, DirectionSpeedDefault>
): EffectiveSpeed => combineSpeed(minSpeed, maxSpeed, directionOfZone(zone), defaults);

/** V_min dùng cho đánh giá chậm theo tốc độ: KHÔNG đòi đủ cặp (khớp `MinSpeedOfZone`). */
export const slowMinOfZone = (zone: ZoneSpeedRow, minSpeed: number | null | undefined, defaults: Map<string, DirectionSpeedDefault>) => {
	const direction = directionOfZone(zone);
	return toKmh(minSpeed) ?? (direction ? defaults.get(direction)?.minSpeed ?? null : null);
};

export interface AidSpeed extends EffectiveSpeed {
	/** Camera (mã) nhận zone này làm vùng chính, cùng hướng. */
	cameras: string[];
}

/**
 * Ngưỡng ĐẨY XUỐNG LÀN AID — khớp `SpeedRangeResolver.ForCamera`: chỉ camera nhận zone này làm VÙNG CHÍNH, mỗi
 * camera ghép giá trị zone với mặc định theo hướng CỦA CAMERA (không theo LaneId zone). Gộp theo hướng camera.
 * Rỗng ⇒ zone không là vùng chính của camera nào ⇒ ngưỡng zone không đi xuống AID.
 */
export const aidSpeeds = (
	zone: ZoneSpeedRow,
	minSpeed: number | null | undefined,
	maxSpeed: number | null | undefined,
	defaults: Map<string, DirectionSpeedDefault>
): AidSpeed[] => {
	const byDirection = new Map<string, AidSpeed>();
	for (const camera of zone.vdsCameras ?? []) {
		if (!camera.isPrimary) continue;
		const direction = camera.directionId?.toString().trim() || null;
		const key = direction ?? '—';
		if (!byDirection.has(key)) byDirection.set(key, { ...combineSpeed(minSpeed, maxSpeed, direction, defaults), cameras: [] });
		byDirection.get(key)!.cameras.push(camera.code || camera.name || camera.id);
	}
	return [...byDirection.values()];
};

/** Ghép giá trị zone với mặc định của MỘT hướng, theo từng giá trị (khớp `SpeedRangeResolver.Combine`). */
export const combineSpeed = (
	minSpeed: number | null | undefined,
	maxSpeed: number | null | undefined,
	direction: string | null,
	defaults: Map<string, DirectionSpeedDefault>
): EffectiveSpeed => {
	const fallback = direction ? defaults.get(direction) : undefined;
	const zoneMin = toKmh(minSpeed);
	const zoneMax = toKmh(maxSpeed);
	const min = zoneMin ?? fallback?.minSpeed ?? null;
	const max = zoneMax ?? fallback?.maxSpeed ?? null;
	const source: SpeedSource =
		zoneMin != null && zoneMax != null ? 'zone'
		: zoneMin != null || zoneMax != null ? 'mixed'
		: fallback ? 'direction' : 'none';
	return { min, max, valid: min != null && max != null && min <= max, source, direction };
};

/** Vị trí theo mét; khoảng của zone có thể khai NGƯỢC (Km từ > Km đến) nên luôn trả [đầu, cuối] tăng dần. */
export const zoneSpan = (zone: ZoneSpeedRow): [number, number] | null => {
	if (zone.fromKmNumber == null && zone.toKmNumber == null) return null;
	const from = (zone.fromKmNumber ?? 0) * 1000 + (zone.fromMetNumber ?? 0);
	const to = (zone.toKmNumber ?? 0) * 1000 + (zone.toMetNumber ?? 0);
	return from <= to ? [from, to] : [to, from];
};

export const formatKm = (meters: number) => `Km${Math.floor(meters / 1000)}+${String(Math.round(meters % 1000)).padStart(3, '0')}`;

// ═══════════════════════ Màu theo Max ═══════════════════════

/**
 * 5 bậc theo Max hiệu lực (ordinal), một sắc primary nhạt → đậm — dùng biến Element Plus để theo theme sáng/tối.
 * Dùng chung cho dải Km và huy hiệu trên cây để hai nơi đọc cùng một thang.
 */
export const SPEED_BINS = [
	{ upTo: 40, label: '≤ 40', color: 'var(--el-color-primary-light-7)', ink: 'var(--el-color-primary-dark-2)' },
	{ upTo: 60, label: '41–60', color: 'var(--el-color-primary-light-5)', ink: 'var(--el-color-primary-dark-2)' },
	{ upTo: 80, label: '61–80', color: 'var(--el-color-primary-light-3)', ink: 'var(--el-color-primary-dark-2)' },
	{ upTo: 100, label: '81–100', color: 'var(--el-color-primary)', ink: 'var(--el-color-white)' },
	{ upTo: Infinity, label: '> 100', color: 'var(--el-color-primary-dark-2)', ink: 'var(--el-color-white)' },
] as const;

export const speedBinOf = (eff: EffectiveSpeed) =>
	(eff.valid && eff.max != null ? SPEED_BINS.find(b => eff.max! <= b.upTo) : undefined);
