/**
 * Đưa bản đồ tới card block và nhấp nháy để người dùng thấy ngay (dùng ở tab Vị trí và tab Thiết lập block).
 * Card do BlockMapLayer vẽ, mang data-block-id / data-block-device-id và style `.is-located`.
 */

interface LocatableMap {
  once: (event: string, handler: () => void) => unknown;
  panTo: (latlng: [number, number]) => unknown;
}

interface LocatableBlock {
  id: string;
  displayPosition: { lat: number; lng: number };
}

const LOCATED_CLASS = 'is-located';
const LOCATED_DURATION_MS = 1600;

/** Nhấp nháy thiết bị trong card; không thấy thiết bị (đang bị filter ẩn) thì nhấp nháy cả card. */
export const flashBlockOnMap = (blockId: string, deviceId?: string) => {
  const blockSelector = `.map-block[data-block-id="${CSS.escape(blockId)}"]`;
  const deviceEl = deviceId ? document.querySelector(`${blockSelector} [data-block-device-id="${CSS.escape(deviceId)}"]`) : null;
  const target = (deviceEl || document.querySelector(blockSelector)) as HTMLElement | null;
  if (!target) return;
  target.classList.remove(LOCATED_CLASS);
  void target.offsetWidth; // khởi động lại animation khi bấm liên tiếp
  target.classList.add(LOCATED_CLASS);
  window.setTimeout(() => target.classList.remove(LOCATED_CLASS), LOCATED_DURATION_MS);
};

/** panTo luôn phát 'moveend' (kể cả khi không cần di chuyển) → nhấp nháy sau khi bản đồ dừng. */
export const locateBlockOnMap = (map: LocatableMap | null | undefined, block: LocatableBlock | null | undefined, deviceId?: string) => {
  if (!map || !block) return;
  map.once('moveend', () => flashBlockOnMap(block.id, deviceId));
  map.panTo([block.displayPosition.lat, block.displayPosition.lng]);
};
