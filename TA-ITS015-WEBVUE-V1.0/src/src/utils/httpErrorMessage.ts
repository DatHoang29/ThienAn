import { i18n } from '/@/i18n';

/** Không đưa object lỗi/request vào props của thông báo giao diện. */
export function httpErrorMessage(value: unknown, fallback = 'Request Error.'): string {
	let message = fallback;
	if (typeof value === 'string' && value.trim()) {
		message = value.trim();
	} else if (value && typeof value === 'object') {
		for (const key of ['message', 'detail', 'title'] as const) {
			const text = (value as Record<string, unknown>)[key];
			if (typeof text === 'string' && text.trim()) {
				message = text.trim();
				break;
			}
		}
	}

	if (message && typeof message === 'string' && message.startsWith('lz.')) {
		try {
			if (i18n.global.te(message)) {
				return i18n.global.t(message);
			}
		} catch {
			// ignore fallback
		}
	}
	return message;
}

