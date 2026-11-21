import { onMounted, onUnmounted } from 'vue';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';

/**
 * Description: Lắng nghe thông báo (notification) mới qua mittBus (NATS transporter,
 *              subject ta.its.noti). Component chứa hook phải được mount toàn thời gian
 *              phiên đăng nhập (ví dụ NotiBell trên topBar) để không bỏ sót thông báo.
 * @param handler Hàm xử lý khi nhận noti mới. Tham số `data` là payload từ NATS,
 *                khớp NotiListOutput (id, messageId, title, content, category,
 *                severity, sourceKey, sendTime) — BE có thể gửi PascalCase.
 * @param options.immediate Gọi handler ngay khi mounted (tương đương watch { immediate: true }).
 */
export function useTransporterNoti(handler: (data?: any) => void, options: { immediate?: boolean } = {}) {
	const listener = (data: any) => handler(data);

	onMounted(() => {
		mittBus.on(TransporterEvent.Noti as any, listener);
		if (options.immediate) handler();
	});

	onUnmounted(() => {
		mittBus.off(TransporterEvent.Noti as any, listener);
	});
}
