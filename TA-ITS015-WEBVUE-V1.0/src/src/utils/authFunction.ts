import { useUserInfo } from '/@/stores/userInfo';
import { judgementSameArr } from '/@/utils/arrayOperation';
import { resolveDirective, withDirectives } from 'vue';

/**
 * Single permission verification
 * @param value Permission value
 * @returns Returns `true` if authorized, otherwise returns `false`
 */
export function auth(value: string): boolean {
	const stores = useUserInfo();
	// Match v-auth: the super administrator is not restricted by the button list.
	if (String(stores.userInfos.accountType) === '111') return true;
	return stores.userInfos.authBtnList?.some((v: string) => v === value) ?? false;
}

/**
 * Multiple permission verification, returns true if at least one is satisfied
 * @param value Permission values
 * @returns Returns `true` if authorized, otherwise returns `false`
 */
export function auths(value: Array<string>): boolean {
	let flag = false;
	const stores = useUserInfo();
	stores.userInfos.authBtnList.map((val: string) => {
		value.map((v: string) => {
			if (val === v) flag = true;
		});
	});
	return flag;
}

/**
 * Multiple permission verification, returns true only if all are satisfied
 * @param value Permission values
 * @returns Returns `true` if authorized, otherwise returns `false`
 */
export function authAll(value: Array<string>): boolean {
	const stores = useUserInfo();
	return judgementSameArr(value, stores.userInfos.authBtnList);
}
/**
 * Single permission verification, returns VNode if satisfied
 * @param VNode Element
 * @param value Permission value
 * @returns VNode
 */
export function hAuth<T extends VNode>(el: T, value: string): T {
	return withDirectives(el, [[resolveDirective('auth'), value]]);
}
/**
 * Multiple permission verification, checks if at least one is satisfied, returns VNode
 * @param VNode Element
 * @param value Permission values
 * @returns VNode
 */
export function hAuths<T extends VNode>(el: T, value: Array<string>): T {
	return withDirectives(el, [[resolveDirective('auths'), value]]);
}
/**
 * Multiple permission verification, checks if all are satisfied, returns VNode
 * @param VNode Element
 * @param value Permission values
 * @returns VNode
 */
export function hAuthAll<T extends VNode>(el: T, value: Array<string>): T {
	return withDirectives(el, [[resolveDirective('auth-all'), value]]);
}
