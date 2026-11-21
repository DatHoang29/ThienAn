import { defineStore } from 'pinia';
import { getAPI } from '/@/utils/axios-utils';
import { TmsIncidentApi, TmsIncidentAutomationRunApi } from '/@/api-services/tms';
import { TmsIncidentStateEnum } from '/@/types/enums/tmsEnum';

/**
 * Trạng thái của một lần chạy tự động (khớp IncidentAutomationConst.RunStatus phía backend).
 */
export const AutomationRunStatus = {
	WarningPending: 'WarningPending',
	Starting: 'Starting',
	Running: 'Running',
	Active: 'Active',
	FinishPending: 'FinishPending',
	Canceling: 'Canceling',
	ResettingDefault: 'ResettingDefault',
	Completed: 'Completed',
	CompletedWithWarnings: 'CompletedWithWarnings',
	Canceled: 'Canceled',
	Failed: 'Failed',
} as const;

/**
 * Key chứa các sự cố đang chờ tự động thực thi trong response của ListByAccount.
 * Phải khớp IncidentAccessService.AutomationPendingKey phía backend.
 */
export const AUTOMATION_PENDING_KEY = 'automationPendingList';

/**
 * Tài khoản ghi nhận cho các bước do luồng tự động tự thực hiện.
 * Phải khớp IncidentAutomationConst.SystemActor phía backend.
 */
export const AUTOMATION_SYSTEM_ACTOR = 'SYSTEM_AUTOMATION';

/**
 * Author: SonTH
 * Description: Store theo dõi các sự cố đang chờ tự động xử lý, phục vụ banner cảnh báo toàn cục.
 *
 *              KHÔNG tự gọi API danh sách: luồng xử lý sự cố THỦ CÔNG là trung tâm, nó nhận broadcast
 *              NATS rồi gọi ListByAccount, sau đó đẩy nhánh automation vào store này qua syncFromIncidentList.
 *              Nhờ vậy chỉ có một nguồn sự thật duy nhất và không phát sinh thêm lời gọi API hay poll.
 *
 *              Countdown tính ở client dựa trên executeDueAt + độ lệch giờ server (serverTime).
 * Created date: 24/07/2026
 */
export const useIncidentAutomation = defineStore('incidentAutomation', {
	state: () => ({
		/** Các sự cố đang đếm ngược chờ tự động thực thi (lấy từ key automationPendingList) */
		pendingList: [] as any[],
		/** Độ lệch (ms) giữa giờ server và giờ client: serverTime - clientNow */
		serverOffsetMs: 0,
		/** Mốc thời gian "hiện tại" dùng để render countdown, được tick mỗi giây */
		nowTick: Date.now(),
		/** Sự cố đang được xem chi tiết trong modal */
		selectedItem: null as any,
		isShowDetailDialog: false,
	}),
	getters: {
		/**
		 * Các sự cố cần hiện banner.
		 * Sắp xếp theo executeDueAt tăng dần: sự cố sắp thực thi nhất nằm trên cùng, vì đó là cái
		 * người vận hành cần quyết định trước.
		 */
		pendingRuns(state): any[] {
			return state.pendingList
				.filter((item) => item?.automation?.status === AutomationRunStatus.WarningPending)
				.sort((a, b) => new Date(a.automation?.executeDueAt ?? 0).getTime() - new Date(b.automation?.executeDueAt ?? 0).getTime());
		},
		/** Có banner cần hiển thị hay không */
		hasPendingRuns(): boolean {
			return (this as any).pendingRuns.length > 0;
		},
	},
	actions: {
		/**
		 * Author: SonTH
		 * Description: Nhận nguyên response của ListByAccount và tách nhánh tự động hóa ra.
		 *              Gọi từ incidentPopup sau mỗi lần nạp danh sách sự cố.
		 * Created date: 24/07/2026
		 */
		syncFromIncidentList(incidentList: any) {
			const list = (incidentList?.[AUTOMATION_PENDING_KEY] ?? []) as any[];

			// Hiệu chỉnh lệch giờ client/server để countdown chính xác trên mọi máy trạm
			const serverTime = list.find((item) => item?.automation?.serverTime)?.automation?.serverTime;
			if (serverTime) this.serverOffsetMs = new Date(serverTime).getTime() - Date.now();

			this.pendingList = list;

			// Modal đang mở nhưng run đã bị xử lý xong => đóng lại để không thao tác trên dữ liệu cũ
			const activeRunIds = list.map((item) => item?.automation?.runId);
			if (this.isShowDetailDialog && !activeRunIds.includes(this.selectedItem?.automation?.runId)) this.closeDetail();
		},

		/** Xóa toàn bộ dữ liệu (đăng xuất, hoặc tài khoản không còn quyền xem) */
		clear() {
			this.pendingList = [];
			this.closeDetail();
		},

		/** Số giây còn lại trước khi tự động thực thi (0 nếu đã tới hạn) */
		getRemainingSeconds(item: any): number {
			const dueAt = item?.automation?.executeDueAt;
			if (!dueAt) return 0;
			const nowServerMs = this.nowTick + this.serverOffsetMs;
			return Math.max(0, Math.ceil((new Date(dueAt).getTime() - nowServerMs) / 1000));
		},

		/** Định dạng countdown mm:ss */
		formatRemaining(item: any): string {
			const total = this.getRemainingSeconds(item);
			const minutes = Math.floor(total / 60);
			const seconds = total % 60;
			return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
		},

		/**
		 * Author: SonTH
		 * Description: Phần trăm thời gian còn lại (100 -> 0) để vẽ thanh tiến trình đếm ngược.
		 *              Thiếu warningStartedAt thì trả 100 để thanh không nhảy loạn.
		 * Created date: 24/07/2026
		 */
		getRemainingPercent(item: any): number {
			const startedAt = item?.automation?.warningStartedAt;
			const dueAt = item?.automation?.executeDueAt;
			if (!startedAt || !dueAt) return 100;

			const totalMs = new Date(dueAt).getTime() - new Date(startedAt).getTime();
			if (totalMs <= 0) return 0;

			const remainingMs = this.getRemainingSeconds(item) * 1000;
			return Math.min(100, Math.max(0, (remainingMs / totalMs) * 100));
		},

		/**
		 * Author: SonTH
		 * Description: Mức độ gấp, dùng để đổi màu và hiệu ứng của banner.
		 *              critical (<=10s) nhấp nháy, high (<=60s) đỏ, normal còn lại màu cảnh báo.
		 * Created date: 24/07/2026
		 */
		getUrgency(item: any): 'normal' | 'high' | 'critical' {
			const remaining = this.getRemainingSeconds(item);
			if (remaining <= 10) return 'critical';
			if (remaining <= 60) return 'high';
			return 'normal';
		},

		/** Cập nhật mốc thời gian để countdown re-render */
		tick() {
			this.nowTick = Date.now();
		},

		/** Mở modal chi tiết. Chỉ có tác dụng khi tài khoản được phép thao tác. */
		openDetail(item: any) {
			if (!item?.automation?.canOperate) return;
			this.selectedItem = item;
			this.isShowDetailDialog = true;
		},

		/** Đóng modal chi tiết */
		closeDetail() {
			this.isShowDetailDialog = false;
			this.selectedItem = null;
		},

		/**
		 * Xác nhận thực thi ngay, bỏ qua thời gian đếm ngược còn lại.
		 * Không tự nạp lại danh sách: backend sẽ phát broadcast, incidentPopup nhận và nạp lại
		 * ListByAccount rồi đồng bộ về store này.
		 */
		async executeNow(runId: string) {
			const res = await getAPI(TmsIncidentAutomationRunApi).apiTmsTmsincidentautomationrunExecutenowPost({ runId });
			return res.data.result;
		},

		/** Hủy lần chạy tự động đang chờ và đưa sự cố về luồng xử lý thủ công. */
		async convertToManual(runId: string) {
			const res = await getAPI(TmsIncidentAutomationRunApi).apiTmsTmsincidentautomationrunConverttomanualPost({ runId });
			return res.data.result;
		},

		/**
		 * Author: SonTH
		 * Description: Đánh dấu sự cố là BỎ QUA - không xử lý. Dùng đúng endpoint của luồng thủ công
		 *              (UpdateState -> Skipped) vì sự cố ở giai đoạn này chưa được xác thực, giống
		 *              hệt nút "Bỏ qua" ở tab Registration của popup xử lý sự cố.
		 *
		 *              Không cần gọi thêm API hủy lần chạy tự động: backend tự hủy khi sự cố chuyển
		 *              sang Skipped bởi một actor không phải hệ thống
		 *              (xem IncidentWorkflowCommandHandler -> SafeCancelActiveRunsAsync).
		 *
		 *              Đây là thao tác THẬT trên dữ liệu, ảnh hưởng mọi máy trạm.
		 * Created date: 24/07/2026
		 */
		async skipIncident(incidentId: string) {
			const res = await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({
				id: incidentId,
				state: TmsIncidentStateEnum.Skipped,
			});
			return res.data.result;
		},
	},
});
