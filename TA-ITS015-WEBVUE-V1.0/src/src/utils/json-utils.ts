/**
 * Attempt to convert a string to an object
 * @param value The string to be converted
 * @returns {Object|String}
 */
export const StringToObj = (value: any): any => {
	if (value && typeof value == 'string') {
		try {
			const obj = JSON.parse(value);
			if (typeof obj == 'object') {
				return obj;
			} else return value;
		} catch (e) {
			return value;
		}
	} else return value;
};
