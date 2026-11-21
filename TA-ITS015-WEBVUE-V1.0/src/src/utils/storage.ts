import Cookies from 'js-cookie';

/**
 * window.localStorage browser permanent cache
 * @method set set permanent cache
 * @method get get permanent cache
 * @method remove remove permanent cache
 * @method clear remove all permanent cache
 */
export const Local = {
	// View v2.4.3 version update log
	setKey(key: string) {
		// @ts-ignore
		return `${__NEXT_NAME__}:${key}`;
	},
	// Set permanent cache
	set<T>(key: string, val: T) {
		window.localStorage.setItem(Local.setKey(key), JSON.stringify(val));
	},
	// Get permanent cache
	get(key: string) {
		let json = <string>window.localStorage.getItem(Local.setKey(key));
		return JSON.parse(json);
	},
	// Remove permanent cache
	remove(key: string) {
		window.localStorage.removeItem(Local.setKey(key));
	},
	// Remove all permanent cache
	clear() {
		window.localStorage.clear();
	},
};

/**
 * window.sessionStorage browser temporary cache
 * @method set set temporary cache
 * @method get get temporary cache
 * @method remove remove temporary cache
 * @method clear remove all temporary cache
 */
export const Session = {
	// Set temporary cache
	set<T>(key: string, val: T) {
		if (key === 'token') return Cookies.set(key, val);
		window.sessionStorage.setItem(Local.setKey(key), JSON.stringify(val));
	},
	// Get temporary cache
	get(key: string) {
		if (key === 'token') return Cookies.get(key);
		let json = <string>window.sessionStorage.getItem(Local.setKey(key));
		return JSON.parse(json);
	},
	// Remove temporary cache
	remove(key: string) {
		if (key === 'token') return Cookies.remove(key);
		window.sessionStorage.removeItem(Local.setKey(key));
	},
	// Remove all temporary cache
	clear() {
		Cookies.remove('token');
		Cookies.remove('userInfo');
		Cookies.remove('constList');
		window.sessionStorage.clear();
	},
};
