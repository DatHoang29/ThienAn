import type { App } from 'vue';
import { useUserInfo } from '/@/stores/userInfo';

export function setupConstFilter(app: App) {
	// Global filter  Called in vue files  $filters.codeToName(code,type)
	app.config.globalProperties.$filters = {
		codeToName(code: any, type: any) {
			return codeToName(code, type);
		},
	};
}

// Constant value conversion
export function codeToName(code: any, type: any) {
	const userStore = useUserInfo();
	try {
		const name = userStore.constList.find((x: any) => x.code === type).data.result.find((x: any) => x.code === code)?.name;
		return name;
	} catch (error) {
		return code;
	}
}

export function getConstType(type: any) {
	const userStore = useUserInfo();
	const constType = userStore.constList.filter((x: any) => x.code === type)[0].data.result;
	return constType;
} 