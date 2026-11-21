/**
 * Determine if the current webpage has updates by listening to the JS SRC of the current page
 * Usage: main.js import checkUpdate from "/@/utils/auto-update";
 */

let lastSrcs: any[] | null; // Last JS address collection
// const scriptReg = /(?<=<script.*src=["']).*?(?=["'])/gm; // iOS does not support assertion matching
const scriptReg = /<script.*?src=['"](.*?)['"]/gm;

/**
 * Get the latest JS collection
 * @returns
 */
async function extractNewScripts() {
	const html = await fetch('/?_t=' + Date.now()).then((res) => res.text());
	scriptReg.lastIndex = 0;
	const result = html.match(scriptReg);
	return result;
}
/**
 * Determine if there are updates
 * @returns
 */
async function checkUpdate() {
	const newScripts = await extractNewScripts();
	if (!lastSrcs) {
		lastSrcs = newScripts;
		return false;
	}
	if (newScripts == null) return false;
	let result = false;
	if (lastSrcs.length !== newScripts.length) {
		result = true;
	}
	for (let i = 0; i < lastSrcs.length; i++) {
		if (lastSrcs[i] !== newScripts[i]) {
			result = true;
			break;
		}
	}
	lastSrcs = newScripts;
	return result;
}

/**
 * Timer to regularly check for updates, execute callback function if there are updates
 * @param callbackFn
 * @param interval
 */
function checkUpdateInterval(callbackFn: any, interval: number = 60000) {
	setTimeout(async () => {
		const willUpdate = await checkUpdate();
		if (willUpdate) {
			callbackFn();
		}
		checkUpdateInterval(callbackFn, interval);
	}, interval);
}

export default checkUpdateInterval;
