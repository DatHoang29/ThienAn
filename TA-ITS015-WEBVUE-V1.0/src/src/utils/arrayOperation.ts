/**
 * Determine whether two arrays of strings are the same (used for button permission validation). 
 * Duplicate entries in the array strings will be automatically removed (button permission identifiers will not repeat).
 * @param news New data
 * @param old Source data
 * @returns Returns `true` if the two arrays are the same, otherwise returns `false`
 */
export function judgementSameArr(newArr: unknown[] | string[], oldArr: string[]): boolean {
	const news = removeDuplicate(newArr);
	const olds = removeDuplicate(oldArr);
	let count = 0;
	const leng = news.length;
	for (let i in olds) {
		for (let j in news) {
			if (olds[i] === news[j]) count++;
		}
	}
	return count === leng ? true : false;
}

/**
 * Determine whether two objects are the same
 * @param a The first object to compare
 * @param b The second object to compare
 * @returns Returns true if they are the same, otherwise returns false
 */
export function isObjectValueEqual<T>(a: T, b: T): boolean {
	if (!a || !b) return false;
	let aProps = Object.getOwnPropertyNames(a);
	let bProps = Object.getOwnPropertyNames(b);
	if (aProps.length != bProps.length) return false;
	for (let i = 0; i < aProps.length; i++) {
		let propName = aProps[i];
		let propA = a[propName];
		let propB = b[propName];
		if (!b.hasOwnProperty(propName)) return false;
		if (propA instanceof Object) {
			if (!isObjectValueEqual(propA, propB)) return false;
		} else if (propA !== propB) {
			return false;
		}
	}
	return true;
}

/**
 * Remove duplicates from an array or array of objects
 * @param arr Array content
 * @param attr The key value to remove duplicates (for array of objects)
 * @returns
 */
export function removeDuplicate(arr: EmptyArrayType, attr?: string) {
	if (!Object.keys(arr).length) {
		return arr;
	} else {
		if (attr) {
			const obj: EmptyObjectType = {};
			return arr.reduce((cur: EmptyArrayType[], item: EmptyArrayType) => {
				obj[item[attr]] ? '' : (obj[item[attr]] = true && item[attr] && cur.push(item));
				return cur;
			}, []);
		} else {
			return [...new Set(arr)];
		}
	}
}

/* Deep copy of arrays and objects
 * @param value The content to be copied
 * @returns
 */
export const clone = <T>(value: T): T => {
	if (!value) return value;

	// Array
	if (Array.isArray(value)) return value.map((item) => clone(item)) as unknown as T;

	// Regular object
	if (typeof value === 'object') {
		return Object.fromEntries(
			Object.entries(value).map(([k, v]: [string, any]) => {
				return [k, clone(v)];
			})
		) as unknown as T;
	}
	// Primitive type
	return value;
};
