import { defineStore } from 'pinia';
import { Local } from '/@/utils/storage';

/**
 * Một dịch vụ được khai báo để giám sát qua heartbeat NATS.
 * Người dùng khai báo trên trang "Giám sát dịch vụ" (views/tms/serviceMonitor).
 */
export interface ServiceMonitorItem {
	/** Khoá cục bộ (uuid) — dùng làm keyField cho lưới. */
	id: string;
	/** Tên hiển thị, vd "Dịch vụ thu phí (Toll)". */
	name: string;
	/** Phân hệ, vd "toll", "fms" (chỉ để nhóm/hiển thị). */
	module: string;
	/** Subject heartbeat cần theo dõi, vd "ta.its.heartbeat.toll". */
	subject: string;
	/** Chu kỳ heartbeat kỳ vọng (giây). Quá ngưỡng này không nhận gói => Offline. Mặc định 30. */
	intervalSeconds: number;
	/** 1 = đang theo dõi, 0 = tạm dừng theo dõi. */
	status: number;
	/** Thứ tự hiển thị. */
	orderNo: number;
	/** Ghi chú. */
	remark?: string;
}

const STORAGE_KEY = 'tmsServiceMonitorList';

/** Danh sách mặc định gợi ý (vms, toll, vds, fms) khi người dùng chưa cấu hình gì. */
const defaultList = (): ServiceMonitorItem[] => [
	{ id: 'svc-vms', name: 'Dịch vụ biển báo (VMS)', module: 'vms', subject: 'ta.its.heartbeat.vms', intervalSeconds: 30, status: 1, orderNo: 10, remark: '' },
	{ id: 'svc-toll', name: 'Dịch vụ thu phí (Toll)', module: 'toll', subject: 'ta.its.heartbeat.toll', intervalSeconds: 30, status: 1, orderNo: 20, remark: '' },
	{ id: 'svc-vds', name: 'Dịch vụ đếm xe (VDS)', module: 'vds', subject: 'ta.its.heartbeat.vds', intervalSeconds: 30, status: 1, orderNo: 30, remark: '' },
	{ id: 'svc-fms', name: 'Dịch vụ giám sát thiết bị (FMS)', module: 'fms', subject: 'ta.its.heartbeat.fms', intervalSeconds: 30, status: 1, orderNo: 40, remark: '' },
	{ id: 'svc-wpsync', name: 'Dịch vụ đồng bộ WP (WP)', module: 'wp', subject: 'ta.its.heartbeat.wpsync', intervalSeconds: 30, status: 1, orderNo: 50, remark: '' },
	{ id: 'svc-sharedata', name: 'Dịch vụ chia sẻ dữ liệu (ShareData)', module: 'sharedata', subject: 'ta.its.heartbeat.sharedata', intervalSeconds: 30, status: 1, orderNo: 60, remark: '' },
];

const genId = (): string => {
	try {
		if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') return crypto.randomUUID();
	} catch {
		/* ignore */
	}
	return `svc-${Date.now()}-${Math.floor(Math.random() * 100000)}`;
};

const loadFromStorage = (): ServiceMonitorItem[] => {
	try {
		const raw = Local.get(STORAGE_KEY);
		if (Array.isArray(raw) && raw.length) return raw as ServiceMonitorItem[];
	} catch {
		/* ignore */
	}
	return defaultList();
};

/**
 * Store quản lý cấu hình danh sách dịch vụ cần giám sát.
 * Lưu tại localStorage (chưa cần backend) — mỗi trình duyệt/máy trạm tự cấu hình danh sách theo dõi.
 */
export const useServiceMonitor = defineStore('tmsServiceMonitor', {
	state: () => ({
		list: loadFromStorage() as ServiceMonitorItem[],
	}),
	getters: {
		/** Danh sách đã sắp xếp theo orderNo rồi tên. */
		sortedList(state): ServiceMonitorItem[] {
			return [...state.list].sort((a, b) => (a.orderNo ?? 0) - (b.orderNo ?? 0) || (a.name || '').localeCompare(b.name || ''));
		},
		/** Các subject đang bật theo dõi. */
		activeSubjects(state): string[] {
			return state.list.filter((x) => x.status === 1 && !!x.subject).map((x) => x.subject);
		},
	},
	actions: {
		persist() {
			Local.set(STORAGE_KEY, this.list);
		},
		/** Ghi đè toàn bộ danh sách. */
		setList(list: ServiceMonitorItem[]) {
			this.list = list ?? [];
			this.persist();
		},
		/** Thêm mới. Tự sinh id nếu thiếu. */
		add(item: ServiceMonitorItem) {
			const record = { ...item, id: item.id || genId() };
			this.list.push(record);
			this.persist();
		},
		/** Cập nhật theo id. */
		update(item: ServiceMonitorItem) {
			const idx = this.list.findIndex((x) => x.id === item.id);
			if (idx >= 0) this.list[idx] = { ...item };
			else this.list.push({ ...item, id: item.id || genId() });
			this.persist();
		},
		/** Xoá theo id. */
		remove(id: string) {
			this.list = this.list.filter((x) => x.id !== id);
			this.persist();
		},
		/** Xoá nhiều theo danh sách id. */
		removeMany(ids: string[]) {
			const set = new Set(ids);
			this.list = this.list.filter((x) => !set.has(x.id));
			this.persist();
		},
		/** Khôi phục danh sách mặc định (toll + fms). */
		resetDefault() {
			this.list = defaultList();
			this.persist();
		},
	},
});

export { genId as genServiceMonitorId };
