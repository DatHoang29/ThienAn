export const ZONE_STALE_SECONDS = 30;
// SQL zone UpdateTime without an offset follows the existing server-local time contract.
export function zoneUpdateTime(value: unknown): number {
  const time = typeof value === 'string' ? Date.parse(value) : NaN;
  return Number.isFinite(time) ? time : -Infinity;
}
export function expireZoneStatus<T extends { updateTime?: unknown; condition?: string; stale?: boolean; sourceCondition?: string }>(row: T, now = Date.now(), staleAfterSeconds = ZONE_STALE_SECONDS): T {
  const time = zoneUpdateTime(row.updateTime);
  const stale = !Number.isFinite(time) || time > now || now - time >= staleAfterSeconds * 1000;
  // Giữ mã gốc để quá hạn TẠM (giờ client lệch nhẹ làm dòng vừa ghi trông như ở tương lai) hết thì khôi phục được,
  // thay vì kẹt ở 'normal' tới lần ghi sau. Ai ghi condition mới thì phải bỏ/ghi lại sourceCondition.
  const sourceCondition = row.sourceCondition ?? row.condition;
  return { ...row, stale, sourceCondition, condition: stale ? 'normal' : sourceCondition };
}
