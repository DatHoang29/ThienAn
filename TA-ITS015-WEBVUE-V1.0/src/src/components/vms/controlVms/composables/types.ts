import type { Ref } from 'vue';
import type { useMapStore } from '/@/stores/its/mapStore';

/**
 * Deps dùng chung truyền vào các composable của controlVms.
 * state là nguồn dữ liệu duy nhất, khai báo tại index.vue và truyền xuống.
 */
export interface VmsControlDeps {
	state: any; // reactive state (khai báo ở index.vue)
	props: { mode: 'manual' | 'incident' };
	$t: (key: string, ...args: any[]) => string;
	locale: any; // useI18n().locale
	mapStore: ReturnType<typeof useMapStore>;
	getConfigDataByCode: (code: any) => any;
	// Cấu hình vận hành (SysOpConfig): dùng helper ./useOpConfig thay vì truyền qua deps,
	// vì cần await ensureOpConfigLoaded() trước khi đọc.
	selectedTemplate: Ref<any>; // ref template đang chọn (dùng chung content + display)
}
