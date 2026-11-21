import type { App } from 'vue';
import { useUserInfo } from '/@/stores/userInfo';
import { judgementSameArr } from '/@/utils/arrayOperation';

/**
 * User permission directive
 * @directive Single permission verification (v-auth="xxx")
 * @directive Multiple permission verification, display if one is satisfied (v-auths="[xxx,xxx]")
 * @directive Multiple permission verification, display if all are satisfied (v-auth-all="[xxx,xxx]")
 */
export function authDirective(app: App) {
	// Single permission verification (v-auth="xxx")
	app.directive('auth', {
		mounted(el, binding) {
			const stores = useUserInfo();
			if (stores.userInfos.accountType == '111') return; // only use when DEBUG Super administrator, no need to verify permissions
			if (!stores.userInfos.authBtnList.some((v: string) => v === binding.value)) el.parentNode.removeChild(el);
		},
	});
	// Multiple permission verification, display if one is satisfied (v-auths="[xxx,xxx]")
	app.directive('auths', {
		mounted(el, binding) {
			let flag = false;
			const stores = useUserInfo();
			stores.userInfos.authBtnList.map((val: string) => {
				binding.value.map((v: string) => {
					if (val === v) flag = true;
				});
			});
			if (!flag) el.parentNode.removeChild(el);
		},
	});
	// Multiple permission verification, display if all are satisfied (v-auth-all="[xxx,xxx]")
	app.directive('auth-all', {
		mounted(el, binding) {
			const stores = useUserInfo();
			const flag = judgementSameArr(binding.value, stores.userInfos.authBtnList);
			if (!flag) el.parentNode.removeChild(el);
		},
	});
}
