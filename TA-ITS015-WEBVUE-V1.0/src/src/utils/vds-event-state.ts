/** Trạng thái sự kiện VDS từ snapshot: hết hạn theo giờ server, nhớ key vắng mặt thêm 30s để chống nhấp nháy khi kết nối lại. */
import type { EventSnapshotItemOutput, EventSnapshotOutput } from '/@/api-services/vds';

// Client sinh ra khai các mốc giờ là Date nhưng JSON thực tế là chuỗi; new Date(...) nhận được cả hai.
const toMs = (value?: Date | string | null) => (value == null ? NaN : new Date(value).getTime());
const ignoredCodes = ['unknown', 'normal'];

export class VdsEventState {
	items: EventSnapshotItemOutput[] = [];
	private seen = new Map<string, number | null>();
	private initialized = false;
	private serverAt = 0;
	private receivedAt = 0;
	now(monotonic: number) {
		return this.serverAt + monotonic - this.receivedAt;
	}
	private prune(monotonic: number) {
		for (const [key, absentAt] of this.seen) if (absentAt !== null && monotonic - absentAt > 30000) this.seen.delete(key);
	}
	accept(snapshot: EventSnapshotOutput, monotonic: number): EventSnapshotItemOutput[] {
		const server = toMs(snapshot.computedAt);
		if (!Number.isFinite(server) || !Array.isArray(snapshot.items)) return [];
		this.expire(monotonic);
		this.serverAt = server;
		this.receivedAt = monotonic;
		this.items = snapshot.items.filter((item) => item.key && !ignoredCodes.includes((item.eventTypeCode ?? '').toLowerCase()) && toMs(item.displayExpiresAt) > server);
		const present = new Set(this.items.map((item) => item.key!));
		for (const [key, absentAt] of this.seen) if (!present.has(key) && absentAt === null) this.seen.set(key, monotonic);
		this.prune(monotonic);
		const fresh = this.items.filter((item) => this.initialized && !this.seen.has(item.key!) && toMs(item.lastDetectTime) >= server - 30000);
		this.items.forEach((item) => this.seen.set(item.key!, null));
		this.initialized = true;
		return fresh;
	}
	expire(monotonic: number) {
		this.items = this.items.filter((item) => {
			const expiry = toMs(item.displayExpiresAt);
			if (expiry > this.now(monotonic)) return true;
			if (this.seen.get(item.key!) === null) this.seen.set(item.key!, this.receivedAt + expiry - this.serverAt);
			return false;
		});
		this.prune(monotonic);
		return this.items;
	}
}
// Chỉ định danh tài khoản/tenant; exp/iat/token mới không thay đổi baseline.
export function vdsEventPrincipal(headers: Record<string, string>): string {
	const token = (headers.Authorization ?? '').replace(/^Bearer\s+/i, '');
	if (!token) return 'anonymous';
	try {
		const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
		const claims = JSON.parse(decodeURIComponent(Array.from(atob(payload), (c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0')).join('')));
		const user = claims.UserId ?? claims.userId ?? claims.sub ?? claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
		const tenant = claims.TenantId ?? claims.tenantId;
		if (user != null) return JSON.stringify([String(user), tenant == null ? null : String(tenant)]);
	} catch {
		/* Token không hợp lệ: không gộp hai danh tính chưa xác định. */
	}
	return 'unidentified:' + token;
}
