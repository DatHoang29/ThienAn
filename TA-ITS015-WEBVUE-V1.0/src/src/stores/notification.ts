import { defineStore } from 'pinia';
import { ElNotification } from 'element-plus';
import Push from 'push.js';
import { getAPI } from '/@/utils/axios-utils';
import { NotiApi } from '/@/api-services/noti';
import type { NotiListOutput } from '/@/api-services/noti';

/**
 * Store thông báo (notification) dùng chung toàn ứng dụng.
 * - Nạp số chưa đọc + danh sách mới nhất cho chuông thông báo trên topBar.
 * - Nhận noti realtime từ NATS (subject ta.its.noti) qua useTransporterNoti.
 * - Đánh dấu đã đọc (1 dòng / tất cả) và cập nhật lại badge.
 */

/** Số dòng tải cho popover chuông thông báo — chỉ chứa thông báo CHƯA ĐỌC (xem lại cái cũ qua modal "Xem tất cả"). */
export const NOTI_BELL_PAGE_SIZE = 10;

/** Đọc field bất chấp PascalCase (BE publish) / camelCase. */
const pick = (obj: any, ...keys: string[]): any => {
	if (!obj) return undefined;
	for (const k of keys) {
		if (obj[k] !== undefined && obj[k] !== null) return obj[k];
	}
	return undefined;
};

/** Chuẩn hoá payload NATS / dòng API về NotiListOutput dùng được ở FE. */
const normalizeNoti = (raw: any): NotiListOutput => ({
	id: String(pick(raw, 'Id', 'id') ?? ''),
	messageId: pick(raw, 'MessageId', 'messageId') ?? null,
	title: pick(raw, 'Title', 'title') ?? null,
	content: pick(raw, 'Content', 'content') ?? null,
	category: pick(raw, 'Category', 'category') ?? null,
	severity: pick(raw, 'Severity', 'severity') ?? null,
	sourceKey: pick(raw, 'SourceKey', 'sourceKey') ?? null,
	sendTime: pick(raw, 'SendTime', 'sendTime') ? new Date(pick(raw, 'SendTime', 'sendTime')) : new Date(),
	readTime: pick(raw, 'ReadTime', 'readTime') ? new Date(pick(raw, 'ReadTime', 'readTime')) : null,
});

export const useNotification = defineStore('notification', {
	state: (): {
		unreadCount: number;
		bellList: NotiListOutput[];
		bellLoading: boolean;
	} => ({
		unreadCount: 0,
		bellList: [],
		bellLoading: false,
	}),
	getters: {
		hasUnread: (state) => state.unreadCount > 0,
	},
	actions: {
		/** Nạp số chưa đọc + danh sách mới nhất (gọi khi đăng nhập / mở popover). */
		async init() {
			await Promise.all([this.fetchUnreadCount(), this.fetchBellList()]);
		},

		/** Số thông báo chưa đọc của người dùng hiện tại. */
		async fetchUnreadCount() {
			try {
				const { data } = await getAPI(NotiApi).apiNotifyNotiUnreadcountPost({});
				this.unreadCount = data?.result?.count ?? 0;
			} catch (error) {
				console.error('[Noti] fetchUnreadCount error:', error);
			}
		},

		/** Danh sách CHƯA ĐỌC mới nhất hiển thị trong popover chuông (đã đọc xem lại ở modal/trang quản lý). */
		async fetchBellList() {
			this.bellLoading = true;
			try {
				const { data } = await getAPI(NotiApi).apiNotifyNotiPagePost({
					page: 1,
					pageSize: NOTI_BELL_PAGE_SIZE,
					field: 'sendTime',
					order: 'desc',
					descStr: 'desc',
					isRead: false,
				});
				this.bellList = (data?.result?.records ?? []).map(normalizeNoti);
			} catch (error) {
				console.error('[Noti] fetchBellList error:', error);
			} finally {
				this.bellLoading = false;
			}
		},

		/** Nhận noti mới từ NATS: đẩy lên đầu danh sách, tăng badge và bắn thông báo desktop. */
		receive(raw: any) {
			// BE có thể bọc payload trong 1 lớp (result/noti/data) hoặc gửi mảng — bóc trước khi chuẩn hoá
			const unwrapped = Array.isArray(raw) ? raw : pick(raw, 'result', 'noti', 'Noti', 'data', 'Data') ?? raw;
			const list = Array.isArray(unwrapped) ? unwrapped : [unwrapped];

			for (const raw of list) {
				const noti = normalizeNoti(raw);
				if (!noti.id) {
					// Không được drop im lặng — payload lạ phải thấy ngay trên console để chẩn đoán
					console.warn('[Noti] receive: payload không đọc được id, bỏ qua:', raw);
					continue;
				}
				if (this.bellList.some((r) => r.id === noti.id)) continue;

				this.bellList.unshift(noti);
				if (this.bellList.length > NOTI_BELL_PAGE_SIZE) this.bellList.pop();
				this.unreadCount += 1;

				ElNotification({
					title: noti.title ?? '',
					message: noti.content ?? '',
					type: 'info',
					position: 'bottom-right',
					duration: 4500,
				});
				try {
					Push.create(noti.title ?? 'Notification', {
						body: noti.content ?? '',
						icon: 'logo.png',
						timeout: 4500,
					});
				} catch (error) {
					// Chưa cấp quyền desktop notification thì bỏ qua, không làm đứt luồng nhận noti
					console.warn('[Noti] Push.create error:', error);
				}
			}
		},

		/** Đánh dấu đã đọc 1/nhiều dòng; popover chỉ chứa chưa đọc nên loại các dòng vừa đọc ra khỏi danh sách. */
		async markRead(ids: string[]) {
			const validIds = ids.filter(Boolean);
			if (!validIds.length) return;
			await getAPI(NotiApi).apiNotifyNotiMarkreadPost({ ids: validIds, all: false });
			const idSet = new Set(validIds);
			this.bellList = this.bellList.filter((r) => !idSet.has(r.id!));
			await this.fetchUnreadCount();
		},

		/** Đánh dấu TẤT CẢ đã đọc — popover chưa đọc trở nên trống. */
		async markAllRead() {
			await getAPI(NotiApi).apiNotifyNotiMarkreadPost({ ids: null, all: true });
			this.bellList = [];
			this.unreadCount = 0;
		},
	},
});
