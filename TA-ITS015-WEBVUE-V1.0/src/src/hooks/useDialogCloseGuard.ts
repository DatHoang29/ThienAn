import { ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';

/**
 * Author: DatHQ
 * Created date: 20/10/2026
 * Description: Hook bảo vệ đóng hộp thoại khi có thay đổi chưa lưu (Rule 20.26)
 */
export function useDialogCloseGuard() {
	const { t } = useI18n();
	let initialSnapshot: string | null = null;

	const snapshot = (form: any) => {
		try {
			initialSnapshot = form != null ? JSON.stringify(form) : null;
		} catch {
			initialSnapshot = null;
		}
	};

	const isDirty = (form: any): boolean => {
		if (initialSnapshot == null) return false;
		try {
			return JSON.stringify(form) !== initialSnapshot;
		} catch {
			return false;
		}
	};

	const beforeClose = async (form: any, isProcessing: boolean | undefined, done: () => void) => {
		if (isProcessing) return;
		if (!isDirty(form)) {
			done();
			return;
		}
		try {
			await ElMessageBox.confirm(
				t('lz.message.base.confirmDiscardChanges'),
				t('lz.label.base.warning'),
				{
					type: 'warning',
					confirmButtonText: t('lz.button.base.discardChanges'),
					cancelButtonText: t('lz.button.base.continueEditing'),
				}
			);
			done();
		} catch {
			// Người dùng bấm Tiếp tục sửa / out-focus / Esc ở hộp xác nhận -> giữ hộp thoại mở
		}
	};

	return {
		snapshot,
		isDirty,
		beforeClose,
	};
}
