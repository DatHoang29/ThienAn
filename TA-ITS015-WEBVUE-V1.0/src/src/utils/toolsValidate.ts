/**
 * 2020.11.29 lyt Organized
 * Utility class collection, suitable for daily development
 * Added multi-line comment information, hover over the method name to view
 */

/**
 * Validate percentage (no decimals allowed)
 * @param val Current value string
 * @returns Returns the processed string
 */
export function verifyNumberPercentage(val: string): string {
	// Match spaces
	let v = val.replace(/(^\s*)|(\s*$)/g, '');
	// Only numbers and decimal points are allowed, no other input
	v = v.replace(/[^\d]/g, '');
	// Cannot start with 0
	v = v.replace(/^0/g, '');
	// If the number exceeds 100, set it to the maximum value of 100
	v = v.replace(/^[1-9]\d\d{1,3}$/, '100');
	// Return result
	return v;
}

/**
 * Validate percentage (decimals allowed)
 * @param val Current value string
 * @returns Returns the processed string
 */
export function verifyNumberPercentageFloat(val: string): string {
	let v = verifyNumberIntegerAndFloat(val);
	// If the number exceeds 100, set it to the maximum value of 100
	v = v.replace(/^[1-9]\d\d{1,3}$/, '100');
	// No further input allowed after exceeding 100
	v = v.replace(/^100\.$/, '100');
	// Return result
	return v;
}

/**
 * Decimal or integer (no negative numbers allowed)
 * @param val Current value string
 * @returns Returns the processed string
 */
export function verifyNumberIntegerAndFloat(val: string) {
	// Match spaces
	let v = val.replace(/(^\s*)|(\s*$)/g, '');
	// Only numbers and decimal points are allowed, no other input
	v = v.replace(/[^\d.]/g, '');
	// Only one zero is allowed at the start
	v = v.replace(/^0{2}$/g, '0');
	// Ensure the first character is a number, not a dot
	v = v.replace(/^\./g, '');
	// Only one decimal point is allowed
	v = v.replace('.', '$#$').replace(/\./g, '').replace('$#$', '.');
	// Keep 2 digits after the decimal point
	v = v.replace(/^(\-)*(\d+)\.(\d\d).*$/, '$1$2.$3');
	// Return result
	return v;
}

/**
 * Validate positive integers
 * @param val Current value string
 * @returns Returns the processed string
 */
export function verifiyNumberInteger(val: string) {
	// Match spaces
	let v = val.replace(/(^\s*)|(\s*$)/g, '');
	// Remove '.', to prevent issues when pasting, e.g., 0.1.12.12
	v = v.replace(/[\.]*/g, '');
	// Remove numbers starting with 0, to prevent issues when pasting, e.g., 00121323
	v = v.replace(/(^0[\d]*)$/g, '0');
	// The first digit can only be 0 once
	v = v.replace(/^0\d$/g, '0');
	// Only match numbers
	v = v.replace(/[^\d]/g, '');
	// Return result
	return v;
}

/**
 * Remove Chinese characters and spaces
 * @param val Current value string
 * @returns Returns the processed string
 */
export function verifyCnAndSpace(val: string) {
	// Match Chinese characters and spaces
	let v = val.replace(/[\u4e00-\u9fa5\s]+/g, '');
	// Match spaces
	v = v.replace(/(^\s*)|(\s*$)/g, '');
	// Return result
	return v;
}

/**
 * Remove English characters and spaces
 * @param val Current value string
 * @returns Returns the processed string
 */
export function verifyEnAndSpace(val: string) {
	// Match English characters and spaces
	let v = val.replace(/[a-zA-Z]+/g, '');
	// Match spaces
	v = v.replace(/(^\s*)|(\s*$)/g, '');
	// Return result
	return v;
}

/**
 * Prohibit input of spaces
 * @param val Current value string
 * @returns Returns the processed string
 */
export function verifyAndSpace(val: string) {
	// Match spaces
	let v = val.replace(/(^\s*)|(\s*$)/g, '');
	// Return result
	return v;
}

/**
 * Amount separated by `,`
 * @param val Current value string
 * @returns Returns the processed string
 */
export function verifyNumberComma(val: string) {
	// Call the decimal or integer (no negative numbers) method
	let v: any = verifyNumberIntegerAndFloat(val);
	// Convert string to array
	v = v.toString().split('.');
	// \B matches non-word boundaries, both sides are word characters or both sides are non-word characters
	v[0] = v[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
	// Convert array back to string
	v = v.join('.');
	// Return result
	return v;
}

/**
 * Match text color change (during search)
 * @param val Current value string
 * @param text The string value to be processed
 * @param color Highlight color when found
 * @returns Returns the processed string
 */
export function verifyTextColor(val: string, text = '', color = 'red') {
	// Return content, add color
	let v = text.replace(new RegExp(val, 'gi'), `<span style='color: ${color}'>${val}</span>`);
	// Return result
	return v;
}

/**
 * Convert numbers to uppercase Chinese
 * @param val Current value string
 * @param unit Default: 仟佰拾亿仟佰拾万仟佰拾元角分
 * @returns Returns the processed string
 */
export function verifyNumberCnUppercase(val: any, unit = '仟佰拾亿仟佰拾万仟佰拾元角分', v = '') {
	// Add 2 zeros to the current content string, why??
	val += '00';
	// Return the position of the first occurrence of a specified string value in the string, if not found, this method returns -1
	let lookup = val.indexOf('.');
	// substring: does not include the end index content, substr: includes the end index content
	if (lookup >= 0) val = val.substring(0, lookup) + val.substr(lookup + 1, 2);
	// Based on the length of val, return the corresponding uppercase
	unit = unit.substr(unit.length - val.length);
	// Loop to concatenate uppercase
	for (let i = 0; i < val.length; i++) {
		v += '零壹贰叁肆伍陆柒捌玖'.substr(val.substr(i, 1), 1) + unit.substr(i, 1);
	}
	// Regular processing
	v = v
		.replace(/零角零分$/, '整')
		.replace(/零[仟佰拾]/g, '零')
		.replace(/零{2,}/g, '零')
		.replace(/零([亿|万])/g, '$1')
		.replace(/零+元/, '元')
		.replace(/亿零{0,3}万/, '亿')
		.replace(/^元/, '零元');
	// Return result
	return v;
}

/**
 * Mobile phone number
 * @param val Current value string
 * @returns Returns true: mobile phone number is correct
 */
export function verifyPhone(val: string) {
	// false: mobile phone number is incorrect
	if (!/^((12[0-9])|(13[0-9])|(14[5|7])|(15([0-3]|[5-9]))|(18[0|1,5-9]))\d{8}$/.test(val)) return false;
	// true: mobile phone number is correct
	else return true;
}

/**
 * Domestic telephone number
 * @param val Current value string
 * @returns Returns true: domestic telephone number is correct
 */
export function verifyTelPhone(val: string) {
	// false: domestic telephone number is incorrect
	if (!/\d{3}-\d{8}|\d{4}-\d{7}/.test(val)) return false;
	// true: domestic telephone number is correct
	else return true;
}

/**
 * Login account (starts with a letter, allows 5-16 bytes, allows letters, numbers, and underscores)
 * @param val Current value string
 * @returns Returns true: login account is correct
 */
export function verifyAccount(val: string) {
	// false: login account is incorrect
	if (!/^[a-zA-Z][a-zA-Z0-9_]{4,15}$/.test(val)) return false;
	// true: login account is correct
	else return true;
}

/**
 * Password (starts with a letter, length between 6-16, can only contain letters, numbers, and underscores)
 * @param val Current value string
 * @returns Returns true: password is correct
 */
export function verifyPassword(val: string) {
	// false: password is incorrect
	if (!/^[a-zA-Z]\w{5,15}$/.test(val)) return false;
	// true: password is correct
	else return true;
}

/**
 * Strong password (letters + numbers + special characters, length between 6-16)
 * @param val Current value string
 * @returns Returns true: strong password is correct
 */
export function verifyPasswordPowerful(val: string) {
	// false: strong password is incorrect
	if (!/^(?![a-zA-z]+$)(?!\d+$)(?![!@#$%^&\.*]+$)(?![a-zA-z\d]+$)(?![a-zA-z!@#$%^&\.*]+$)(?![\d!@#$%^&\.*]+$)[a-zA-Z\d!@#$%^&\.*]{6,16}$/.test(val)) return false;
	// true: strong password is correct
	else return true;
}

/**
 * Password strength
 * @param val Current value string
 * @description Weak: pure numbers, pure letters, pure special characters
 * @description Medium: letters + numbers, letters + special characters, numbers + special characters
 * @description Strong: letters + numbers + special characters
 * @returns Returns the processed string: weak, medium, strong
 */
export function verifyPasswordStrength(val: string) {
	let v = '';
	// Weak: pure numbers, pure letters, pure special characters
	if (/^(?:\d+|[a-zA-Z]+|[!@#$%^&\.*]+){6,16}$/.test(val)) v = 'Weak';
	// Medium: letters + numbers, letters + special characters, numbers + special characters
	if (/^(?![a-zA-z]+$)(?!\d+$)(?![!@#$%^&\.*]+$)[a-zA-Z\d!@#$%^&\.*]{6,16}$/.test(val)) v = 'Medium';
	// Strong: letters + numbers + special characters
	if (/^(?![a-zA-z]+$)(?!\d+$)(?![!@#$%^&\.*]+$)(?![a-zA-z\d]+$)(?![a-zA-z!@#$%^&\.*]+$)(?![\d!@#$%^&\.*]+$)[a-zA-Z\d!@#$%^&\.*]{6,16}$/.test(val)) v = 'Strong';
	// Return result
	return v;
}

/**
 * IP address
 * @param val Current value string
 * @returns Returns true: IP address is correct
 */
export function verifyIPAddress(val: string) {
	// false: IP address is incorrect
	if (!/^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$/.test(val)) return false;
	// true: IP address is correct
	else return true;
}

/**
 * Email
 * @param val Current value string
 * @returns Returns true: email is correct
 */
export function verifyEmail(val: string) {
	// false: email is incorrect
	if (!/^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/.test(val)) return false;
	// true: email is correct
	else return true;
}

/**
 * ID card
 * @param val Current value string
 * @returns Returns true: ID card is correct
 */
export function verifyIdCard(val: string) {
	// false: ID card is incorrect
	if (!/^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$/.test(val)) return false;
	// true: ID card is correct
	else return true;
}

/**
 * Full name
 * @param val Current value string
 * @returns Returns true: full name is correct
 */
export function verifyFullName(val: string) {
	// false: full name is incorrect
	if (!/^[\u4e00-\u9fa5]{1,6}(·[\u4e00-\u9fa5]{1,6}){0,2}$/.test(val)) return false;
	// true: full name is correct
	else return true;
}

/**
 * Postal code
 * @param val Current value string
 * @returns Returns true: postal code is correct
 */
export function verifyPostalCode(val: string) {
	// false: postal code is incorrect
	if (!/^[1-9][0-9]{5}$/.test(val)) return false;
	// true: postal code is correct
	else return true;
}

/**
 * URL processing
 * @param val Current value string
 * @returns Returns true: URL is correct
 */
export function verifyUrl(val: string) {
	// false: URL is incorrect
	if (
		!/^(?:(?:(?:https?|ftp):)?\/\/)(?:\S+(?::\S*)?@)?(?:(?!(?:10|127)(?:\.\d{1,3}){3})(?!(?:169\.254|192\.168)(?:\.\d{1,3}){2})(?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})(?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])(?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}(?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))|(?:(?:[a-z\u00a1-\uffff0-9]-*)*[a-z\u00a1-\uffff0-9]+)(?:\.(?:[a-z\u00a1-\uffff0-9]-*)*[a-z\u00a1-\uffff0-9]+)*(?:\.(?:[a-z\u00a1-\uffff]{2,})).?)(?::\d{2,5})?(?:[/?#]\S*)?$/i.test(
			val
		)
	)
		return false;
	// true: URL is correct
	else return true;
}

/**
 * License plate number
 * @param val Current value string
 * @returns Returns true: license plate number is correct
 */
export function verifyCarNum(val: string) {
	// false: license plate number is incorrect
	if (
		!/^(([京津沪渝冀豫云辽黑湘皖鲁新苏浙赣鄂桂甘晋蒙陕吉闽贵粤青藏川宁琼使领][A-Z](([0-9]{5}[DF])|([DF]([A-HJ-NP-Z0-9])[0-9]{4})))|([京津沪渝冀豫云辽黑湘皖鲁新苏浙赣鄂桂甘晋蒙陕吉闽贵粤青藏川宁琼使领][A-Z][A-HJ-NP-Z0-9]{4}[A-HJ-NP-Z0-9挂学警港澳使领]))$/.test(
			val
		)
	)
		return false;
	// true: license plate number is correct
	else return true;
}

/**
 * Parse ID card
 */
export function judgementIdCard(idCard: string) {
	let entity = {} as any;
	let currentDate = new Date();
	let yearNow = currentDate.getFullYear();
	let birthDateCode = idCard.substring(6, 14);
	let genderCode = parseInt(idCard.substring(16, 17), 10);

	entity.sex = genderCode % 2 === 0 ? 2 : 1;
	entity.age = yearNow - parseInt(birthDateCode.substring(0, 4));
	entity.birthday = `${birthDateCode.substring(0, 4)}-${birthDateCode.substring(4, 6)}-${birthDateCode.substring(6, 8)}`;
	return entity;
}
