// Danh mục dùng chung cho các trang báo cáo phân hệ Thu phí (toll).
//
// Trước đây các trang báo cáo lấy danh sách trạm từ SysConfigData (op_config
// `toll_station_list`), tức phải khai báo tay và dễ lệch với dữ liệu thật. Composable này
// đọc trực tiếp từ API nghiệp vụ `GET /api/toll/tram/list`.
//
// Vì sao vẫn trả về `{ code, name }`: toàn bộ template báo cáo đang bind
// `:key="item.code" :label="item.name" :value="item.code"`. Giữ nguyên shape thì không
// phải sửa template, và nếu sau này đổi nguồn dữ liệu lần nữa cũng chỉ sửa ở đây.

import { getAPI } from '/@/utils/axios-utils';
import { TramApi, CaLamViecApi } from '/@/api-services/toll';
import type { TramOutput, CaLamViecOutput } from '/@/api-services/toll';

/** Phần tử danh mục, khớp shape mà các el-select báo cáo đang dùng. */
export interface TollCatalogItem {
	/** Giá trị gửi lên API báo cáo. */
	code: string;
	/** Nhãn hiển thị. */
	name: string;
	/** Bản ghi gốc, khi cần đọc thêm field (stationType, startTime...). */
	raw?: TramOutput | CaLamViecOutput;
}

/** Chỉ lấy bản ghi đang kích hoạt. Khớp cột TollStation.Active. */
const ACTIVE = 1;

/**
 * Cache ở phạm vi module: 6 trang báo cáo dùng cùng danh sách trạm, không cần
 * mỗi trang gọi API một lần. Giữ cả Promise để hai trang mở gần nhau không gọi trùng.
 */
let stationCache: TollCatalogItem[] | null = null;
let stationInflight: Promise<TollCatalogItem[]> | null = null;

/**
 * Description: Chuyển TramOutput sang phần tử danh mục.
 *
 * QUAN TRỌNG — `code` gửi lên báo cáo: các stored procedure báo cáo lưu lượng nhận tham số
 * `@station`. Ở đây dùng `stationId` (cột TollStation.StationId = mã trạm nghiệp vụ).
 * Nếu SP thực ra so theo `StationBE` (mã trạm theo hệ ETC) thì ĐỔI DUY NHẤT dòng dưới,
 * không phải sửa 6 trang.
 */
const toCatalogItem = (x: TramOutput): TollCatalogItem => ({
	code: x.stationId || '',
	name: x.name ?? x.stationId ?? '',
	raw: x,
});

/**
 * Description: Lấy danh sách trạm thu phí đang kích hoạt, sắp theo tên.
 *              Kết quả được cache cho cả phiên làm việc.
 *
 * @example
 * const { loadTollStations } = useTollCatalog();
 * state.listData.tramList = await loadTollStations();
 */
async function loadTollStations(): Promise<TollCatalogItem[]> {
	if (stationCache) return stationCache;
	if (stationInflight) return stationInflight;

	stationInflight = (async () => {
		try {
			// Thứ tự tham số: (keyword, stationType, active)
			const res = await getAPI(TramApi).apiTollTramListGet(undefined, undefined, ACTIVE);
			const list = (res.data.result ?? [])
				.map(toCatalogItem)
				// Trạm chưa có mã thì không gửi lên báo cáo được, bỏ ra khỏi dropdown.
				.filter((x) => !!x.code)
				.sort((a, b) => a.name.localeCompare(b.name, 'vi'));

			stationCache = list;
			return list;
		} catch (error) {
			// Không cache khi lỗi, để lần mở trang sau thử lại.
			console.error('[Toll] Không tải được danh sách trạm thu phí:', error);
			return [];
		} finally {
			stationInflight = null;
		}
	})();

	return stationInflight;
}

// ─── Ca làm việc ─────────────────────────────────────────────────────────────

let shiftCache: TollCatalogItem[] | null = null;
let shiftInflight: Promise<TollCatalogItem[]> | null = null;

/**
 * Description: Chuyển CaLamViecOutput sang phần tử danh mục.
 *
 * QUAN TRỌNG — `code` gửi lên báo cáo: stored procedure báo cáo theo ca nhận tham số
 * `@shift`. Ở đây dùng `shiftId` (cột TollShift.ShiftId = mã ca nghiệp vụ). Nếu SP thực ra
 * so theo `ShiftBE` (mã ca theo hệ ETC) thì ĐỔI DUY NHẤT dòng dưới.
 */
const toShiftItem = (x: CaLamViecOutput): TollCatalogItem => ({
	code: x.shiftId ?? '',
	name: x.name ?? x.shiftId ?? '',
	raw: x,
});

/**
 * Description: Lấy danh sách ca làm việc, sắp theo giờ bắt đầu.
 *              Kết quả được cache cho cả phiên làm việc.
 *
 * @example
 * const { loadTollShifts } = useTollCatalog();
 * state.listData.caList = await loadTollShifts();
 */
async function loadTollShifts(): Promise<TollCatalogItem[]> {
	if (shiftCache) return shiftCache;
	if (shiftInflight) return shiftInflight;

	shiftInflight = (async () => {
		try {
			const res = await getAPI(CaLamViecApi).apiTollCalamviecListGet();
			const raw = res.data.result ?? [];

			const list = raw
				// Sắp theo giờ bắt đầu để dropdown đúng trình tự ca trong ngày; ca thiếu
				// StartTime thì đẩy xuống cuối thay vì làm sai thứ tự cả danh sách.
				.slice()
				.sort((a, b) => {
					const ta = a.startTime ? new Date(a.startTime).getTime() : Number.MAX_SAFE_INTEGER;
					const tb = b.startTime ? new Date(b.startTime).getTime() : Number.MAX_SAFE_INTEGER;
					return ta - tb;
				})
				.map(toShiftItem)
				// Ca chưa có mã thì không gửi lên báo cáo được, bỏ ra khỏi dropdown.
				.filter((x) => !!x.code);

			shiftCache = list;
			return list;
		} catch (error) {
			// Không cache khi lỗi, để lần mở trang sau thử lại.
			console.error('[Toll] Không tải được danh sách ca làm việc:', error);
			return [];
		} finally {
			shiftInflight = null;
		}
	})();

	return shiftInflight;
}

/**
 * Description: Xoá cache danh mục — gọi sau khi thêm/sửa/xoá trạm hoặc ca ở trang quản lý
 *              để các trang báo cáo thấy dữ liệu mới mà không cần tải lại app.
 */
function clearTollCatalogCache(): void {
	stationCache = null;
	stationInflight = null;
	shiftCache = null;
	shiftInflight = null;
}

export function useTollCatalog() {
	return { loadTollStations, loadTollShifts, clearTollCatalogCache };
}
