/**
 * Logic thuần (pure logic) dùng chung cho các camera control UI.
 *
 * Module này KHÔNG phụ thuộc DOM/Vue để dễ kiểm thử (property-based testing)
 * và tái sử dụng giữa CameraTile, FullscreenFocusLayout và PlaybackBar.
 */

import type { VideoMonitoringItem } from '/@/views/cctv/dashboard/types';

/** Chu kỳ tốc độ playback hợp lệ (R2.6). */
export const PLAYBACK_SPEED_CYCLE = [1, 2, 4, 8, 16, 32] as const;

/** Tốc độ playback hợp lệ, thuộc {@link PLAYBACK_SPEED_CYCLE}. */
export type PlaybackSpeed = (typeof PLAYBACK_SPEED_CYCLE)[number];

/**
 * Trả về tốc độ kế tiếp theo chuỗi tuần hoàn 1 → 2 → 4 → 8 → 16 → 32 → 1 (R2.6).
 *
 * Nếu `current` không nằm trong chuỗi, trả về phần tử đầu tiên (1).
 *
 * @param current - Tốc độ playback hiện tại.
 * @returns Tốc độ kế tiếp trong chu kỳ.
 */
export function getNextPlaybackSpeed(current: number): PlaybackSpeed {
  const index = PLAYBACK_SPEED_CYCLE.indexOf(current as PlaybackSpeed);
  if (index === -1) {
    return PLAYBACK_SPEED_CYCLE[0];
  }
  const nextIndex = (index + 1) % PLAYBACK_SPEED_CYCLE.length;
  return PLAYBACK_SPEED_CYCLE[nextIndex];
}

/**
 * Định dạng tốc độ thành số nguyên kèm hậu tố "x", ví dụ `1` → `"1x"`, `32` → `"32x"` (R2.5).
 *
 * Không chứa dấu thập phân.
 *
 * @param speed - Giá trị tốc độ playback.
 * @returns Chuỗi biểu diễn tốc độ, ví dụ `"4x"`.
 */
export function formatPlaybackSpeed(speed: number): string {
  return `${Math.trunc(speed)}x`;
}
/**
 * Xác định Play_Button và Speed_Button có bị vô hiệu hóa (disabled) hay không (R2.7).
 *
 * Trả về `true` khi và chỉ khi `deviceId` rỗng HOẶC `actualPlaybackDate` không tồn tại:
 * - `deviceId` được coi là rỗng nếu là `null`, `undefined`, chuỗi rỗng hoặc chỉ gồm khoảng trắng.
 * - `actualPlaybackDate` được coi là không tồn tại nếu là giá trị falsy (`null`, `undefined`,
 *   chuỗi rỗng, `0`, `NaN`, `false`).
 *
 * @param deviceId - Định danh thiết bị đang xem.
 * @param actualPlaybackDate - Đoạn playback thực tế đang phát.
 * @returns `true` nếu controls bị vô hiệu hóa, ngược lại `false`.
 */
export function isPlaybackControlsDisabled(deviceId: string | null | undefined, actualPlaybackDate: unknown): boolean {
  const hasDeviceId = typeof deviceId === 'string' && deviceId.trim().length > 0;
  const hasPlaybackDate = Boolean(actualPlaybackDate);
  return !hasDeviceId || !hasPlaybackDate;
}

/** Kiểm tra chuỗi có nội dung (không null/undefined/rỗng/chỉ khoảng trắng). */
const hasText = (value: unknown): boolean => typeof value === 'string' && value.trim().length > 0;

/**
 * Lý do điều khiển live (PTZ/Snapshot) bị vô hiệu hóa.
 * - `''`           : khả dụng.
 * - `'no-device-id'`: thiếu Device ID (không định danh được với VMS).
 * - `'offline'`     : thiết bị đang ngoại tuyến (không thao tác thời gian thực được).
 * - `'no-stream'`   : thiết bị không có URL luồng video.
 */
export type LiveControlDisabledReason = '' | 'no-device-id' | 'offline' | 'no-stream';

/**
 * Xác định lý do vô hiệu hóa điều khiển live (PTZ, Snapshot) cho một camera.
 *
 * Thứ tự ưu tiên: thiếu Device ID → ngoại tuyến → thiếu URL luồng. Trả về `''` nếu
 * camera online, có Device ID và có URL (đủ điều kiện thao tác thời gian thực).
 *
 * @param camera - Camera cần kiểm tra (hoặc undefined).
 * @returns Mã lý do vô hiệu hóa, `''` nếu khả dụng.
 */
export function getLiveControlDisabledReason(camera: VideoMonitoringItem | null | undefined): LiveControlDisabledReason {
  if (!camera) return 'no-device-id';
  if (!hasText(camera.deviceId)) return 'no-device-id';
  if (camera.status !== 'online') return 'offline';
  if (!hasText(camera.videoUrl)) return 'no-stream';
  return '';
}

/**
 * Camera có đủ điều kiện dùng điều khiển live (PTZ, Snapshot) hay không.
 * Yêu cầu: online + có Device ID + có URL luồng video.
 *
 * @param camera - Camera cần kiểm tra.
 * @returns `true` nếu được phép PTZ/Snapshot.
 */
export function canUseLiveCameraControls(camera: VideoMonitoringItem | null | undefined): boolean {
  return getLiveControlDisabledReason(camera) === '';
}

/**
 * Camera có đủ điều kiện dùng Playback (xem lại) hay không.
 *
 * Chỉ yêu cầu có **Device ID** để truy vấn footage và lấy URL playback:
 * - KHÔNG yêu cầu online — camera ngoại tuyến vẫn còn footage đã ghi.
 * - KHÔNG yêu cầu URL live — playback tự resolve URL theo Device ID + thời điểm.
 * (Trường hợp không có footage trong khoảng thời gian do PlaybackBar xử lý thông báo.)
 *
 * @param camera - Camera cần kiểm tra.
 * @returns `true` nếu được phép mở Playback.
 */
export function canUseCameraPlayback(camera: VideoMonitoringItem | null | undefined): boolean {
  return Boolean(camera) && hasText(camera?.deviceId);
}

/**
 * Trả về nội dung tooltip theo trạng thái nút động (R4.4).
 *
 * Với một nút có chức năng đổi theo trạng thái (ví dụ mở/đóng panel, bật/tắt),
 * hàm trả về nhãn ứng với trạng thái mở/active khi nút đang mở/active, và nhãn
 * ứng với trạng thái đóng/không-active khi nút đang đóng/không-active.
 *
 * @param args.isOpenOrActive - `true` nếu nút đang ở trạng thái mở/active.
 * @param args.openLabel - Nhãn hiển thị khi nút đang mở/active.
 * @param args.closeLabel - Nhãn hiển thị khi nút đang đóng/không-active.
 * @returns `openLabel` khi `isOpenOrActive` là `true`, ngược lại `closeLabel`.
 */
export function resolveToggleTooltip(args: { isOpenOrActive: boolean; openLabel: string; closeLabel: string }): string {
  const { isOpenOrActive, openLabel, closeLabel } = args;
  return isOpenOrActive ? openLabel : closeLabel;
}

/**
 * Lọc danh sách camera khả dụng để chọn, loại bỏ các ô placeholder (R5.5, R5.6).
 *
 * Chỉ giữ lại các camera thật (mục có `isPlaceholder !== true`) và bảo toàn thứ tự
 * tương đối của chúng. Trả về danh sách rỗng khi `cameras` là `undefined` hoặc rỗng.
 *
 * @param cameras - Danh sách camera đầu vào (có thể chứa placeholder), hoặc `undefined`.
 * @returns Danh sách camera không phải placeholder, giữ nguyên thứ tự tương đối.
 */
export function getSelectableCameras(cameras: VideoMonitoringItem[] | undefined): VideoMonitoringItem[] {
  if (!cameras || cameras.length === 0) {
    return [];
  }
  return cameras.filter((camera) => camera.isPlaceholder !== true);
}

/**
 * Xác định một camera có phải là camera đang xem (selected) hay không (R5.10).
 *
 * Trả về `true` khi và chỉ khi `item.id` trùng với `currentCameraId`.
 *
 * @param item - Camera cần kiểm tra.
 * @param currentCameraId - Định danh camera đang xem.
 * @returns `true` nếu `item` là camera đang xem, ngược lại `false`.
 */
export function isCameraSelected(item: VideoMonitoringItem, currentCameraId: string): boolean {
  return item.id === currentCameraId;
}

/**
 * Xác định một thao tác chọn camera có dẫn tới yêu cầu thay thế hay không (R5.7, R5.9).
 *
 * Trả về `true` khi và chỉ khi `selectedId` khác `currentCameraId`; do đó chọn lại
 * đúng camera đang xem không phát yêu cầu thay thế.
 *
 * @param selectedId - Định danh camera vừa được chọn.
 * @param currentCameraId - Định danh camera đang xem.
 * @returns `true` nếu cần phát yêu cầu thay thế, ngược lại `false`.
 */
export function shouldEmitCameraReplace(selectedId: string, currentCameraId: string): boolean {
  return selectedId !== currentCameraId;
}
/**
 * Áp dụng quy ước fallback "--" cho một giá trị thuộc tính camera (R6.3).
 *
 * Trả về `"--"` khi và chỉ khi giá trị là `null`, `undefined`, chuỗi rỗng
 * hoặc chuỗi chỉ gồm khoảng trắng. Với mọi giá trị khác (bao gồm số, boolean,
 * object...), hàm trả về biểu diễn chuỗi của giá trị gốc qua `String(value)`
 * — KHÔNG thay bằng "--".
 *
 * @param value - Giá trị thuộc tính camera cần định dạng.
 * @returns `"--"` nếu trống, ngược lại chuỗi biểu diễn của giá trị gốc.
 */
export function formatCameraInfoValue(value: unknown): string {
  if (value === null || value === undefined) {
    return '--';
  }
  if (typeof value === 'string' && value.trim().length === 0) {
    return '--';
  }
  return String(value);
}

/**
 * Ánh xạ trạng thái camera sang nhãn hiển thị tiếng Việt (R6.2).
 *
 * Trả về `"trực tuyến"` khi và chỉ khi `status === "online"`; mọi giá trị khác
 * (`"offline"`, `"warning"`, `null`, `undefined`, ...) trả về `"ngoại tuyến"`.
 *
 * @param status - Trạng thái thô của camera.
 * @returns `"trực tuyến"` nếu online, ngược lại `"ngoại tuyến"`.
 */
export function formatCameraStatusLabel(status: unknown): string {
  return status === 'online' ? 'trực tuyến' : 'ngoại tuyến';
}

/**
 * Dữ liệu hiển thị đã chuẩn hóa cho Info_Dialog (R6.2, R6.3).
 *
 * Các trường định danh (`name`, `ip`, `deviceId`, `location`) đã áp dụng fallback
 * "--"; `status` luôn là `"trực tuyến"` hoặc `"ngoại tuyến"`.
 */
export interface CameraInfoView {
  name: string;
  ip: string;
  deviceId: string;
  location: string;
  status: string; // "trực tuyến" | "ngoại tuyến"
}

/**
 * Trích xuất thông tin hiển thị cho Info_Dialog với fallback "--" và nhãn trạng thái (R6.2, R6.3).
 *
 * Trả về `null` khi không có dữ liệu cốt lõi để hiển thị, hỗ trợ hành vi "không mở
 * dialog khi thông tin không khả dụng" (R6.1/R6.6). "Dữ liệu cốt lõi vắng mặt" được
 * diễn giải là: không có đối tượng camera (`undefined`/`null`), HOẶC mọi trường định
 * danh khả dụng (`name`, `ip`, `deviceId`, `location`, `id`) đều trống/khuyết — khi đó
 * không còn gì có ý nghĩa để hiển thị ngoài nhãn trạng thái.
 *
 * Khi có ít nhất một trường định danh dùng được, hàm dựng view qua
 * {@link formatCameraInfoValue} (fallback "--") và {@link formatCameraStatusLabel}.
 *
 * @param camera - Camera đang xem, hoặc `undefined`.
 * @returns {@link CameraInfoView} đã chuẩn hóa, hoặc `null` nếu thiếu dữ liệu cốt lõi.
 */
export function buildCameraInfoView(camera: VideoMonitoringItem | undefined): CameraInfoView | null {
  if (!camera) {
    return null;
  }

  // Coi là "thiếu dữ liệu cốt lõi" nếu mọi trường định danh đều trống/khuyết.
  const identifyingValues = [camera.name, camera.ip, camera.deviceId, camera.location, camera.id];
  const hasUsableData = identifyingValues.some((value) => formatCameraInfoValue(value) !== '--');
  if (!hasUsableData) {
    return null;
  }

  return {
    name: formatCameraInfoValue(camera.name),
    ip: formatCameraInfoValue(camera.ip),
    deviceId: formatCameraInfoValue(camera.deviceId),
    location: formatCameraInfoValue(camera.location),
    status: formatCameraStatusLabel(camera.status),
  };
}
