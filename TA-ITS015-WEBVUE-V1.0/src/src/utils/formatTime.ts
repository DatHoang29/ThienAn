/**
 * Convert date and time
 * @param date Current time in the format of new Date()
 * @param format Format string for the desired time format
 * @description format string can be any format, such as `YYYY-mm`, `YYYY-mm-dd`
 * @description format for quarter: "YYYY-mm-dd HH:MM:SS QQQQ"
 * @description format for weekday: "YYYY-mm-dd HH:MM:SS WWW"
 * @description format for week: "YYYY-mm-dd HH:MM:SS ZZZ"
 * @description format for quarter + weekday + week: "YYYY-mm-dd HH:MM:SS WWW QQQQ ZZZ"
 * @returns Returns the formatted time string
 */
export function formatDate(date: Date, format: string): string {
	let we = date.getDay(); // weekday
	let z = getWeek(date); // week
	let qut = Math.floor((date.getMonth() + 3) / 3).toString(); // quarter
	const opt: { [key: string]: string } = {
		'Y+': date.getFullYear().toString(), // year
		'm+': (date.getMonth() + 1).toString(), // month (months start from 0, so add 1)
		'd+': date.getDate().toString(), // day
		'H+': date.getHours().toString(), // hour
		'M+': date.getMinutes().toString(), // minute
		'S+': date.getSeconds().toString(), // second
		'q+': qut, // quarter
	};
	// Chinese numbers (weekday)
	const week: { [key: string]: string } = {
		'0': 'Sun',
		'1': 'Mon',
		'2': 'Tue',
		'3': 'Wed',
		'4': 'Thu',
		'5': 'Fri',
		'6': 'Sat',
	};
	// Chinese numbers (quarter)
	const quarter: { [key: string]: string } = {
		'1': '1st',
		'2': '2nd',
		'3': '3rd',
		'4': '4th',
	};
	if (/(W+)/.test(format)) format = format.replace(RegExp.$1, RegExp.$1.length > 1 ? (RegExp.$1.length > 2 ? 'Week ' + week[we] : 'W' + week[we]) : week[we]);
	if (/(Q+)/.test(format)) format = format.replace(RegExp.$1, RegExp.$1.length == 4 ? quarter[qut] + ' quarter' : quarter[qut]);
	if (/(Z+)/.test(format)) format = format.replace(RegExp.$1, RegExp.$1.length == 3 ? z + 'th week' : z + '');
	for (let k in opt) {
		let r = new RegExp('(' + k + ')').exec(format);
		// If the length is not 1, add leading zeros
		if (r) format = format.replace(r[1], RegExp.$1.length == 1 ? opt[k] : opt[k].padStart(RegExp.$1.length, '0'));
	}
	return format;
}

/**
 * Get the week number of the current date
 * @param dateTime Current date value
 * @returns Returns the week number
 */
export function getWeek(dateTime: Date): number {
	let temptTime = new Date(dateTime.getTime());
	// Weekday
	let weekday = temptTime.getDay() || 7;
	// Monday + 5 days = Saturday
	temptTime.setDate(temptTime.getDate() - weekday + 1 + 5);
	let firstDay = new Date(temptTime.getFullYear(), 0, 1);
	let dayOfWeek = firstDay.getDay();
	let spendDay = 1;
	if (dayOfWeek != 0) spendDay = 7 - dayOfWeek + 1;
	firstDay = new Date(temptTime.getFullYear(), 0, 1 + spendDay);
	let d = Math.ceil((temptTime.valueOf() - firstDay.valueOf()) / 86400000);
	let result = Math.ceil(d / 7);
	return result;
}

/**
 * Convert time to "seconds ago", "minutes ago", "hours ago", "days ago"
 * @param param Current time in the format of new Date() or string format
 * @param format Format string for the desired time format
 * @description param for 10 seconds:  10 * 1000
 * @description param for 1 minute:   60 * 1000
 * @description param for 1 hour: 60 * 60 * 1000
 * @description param for 24 hours: 60 * 60 * 24 * 1000
 * @description param for 3 days:   60 * 60* 24 * 1000 * 3
 * @returns Returns the formatted time string
 */
export function formatPast(param: string | Date, format: string = 'YYYY-mm-dd'): string {
	// Format handling and storage of converted values
	let t: any, s: number;
	// Get the JavaScript timestamp
	let time: number = new Date().getTime();
	// Check if the input is an object
	typeof param === 'string' || 'object' ? (t = new Date(param).getTime()) : (t = param);
	// Current timestamp - input timestamp
	time = Number.parseInt(`${time - t}`);
	if (time < 10000) {
		// Within 10 seconds
		return 'Just now';
	} else if (time < 60000 && time >= 10000) {
		// More than 10 seconds and less than 1 minute
		s = Math.floor(time / 1000);
		return `${s} seconds ago`;
	} else if (time < 3600000 && time >= 60000) {
		// More than 1 minute and less than 1 hour
		s = Math.floor(time / 60000);
		return `${s} minutes ago`;
	} else if (time < 86400000 && time >= 3600000) {
		// More than 1 hour and less than 24 hours
		s = Math.floor(time / 3600000);
		return `${s} hours ago`;
	} else if (time < 259200000 && time >= 86400000) {
		// More than 1 day and less than 3 days
		s = Math.floor(time / 86400000);
		return `${s} days ago`;
	} else {
		// More than 3 days
		let date = typeof param === 'string' || 'object' ? new Date(param) : param;
		return formatDate(date, format);
	}
}

/**
 * Time greeting
 * @param param Current time in the format of new Date()
 * @description param calling `formatAxis(new Date())` will output "Good morning"
 * @returns Returns the formatted time string
 */
export function formatAxis(param: Date): string {
	let hour: number = new Date(param).getHours();
	if (hour < 6) return 'lz.ui.signIn.goodDawn';
	else if (hour < 9) return 'lz.ui.signIn.goodMorning';
	else if (hour < 12) return 'lz.ui.signIn.goodMorning';
	else if (hour < 14) return 'lz.ui.signIn.goodNoon';
	else if (hour < 17) return 'lz.ui.signIn.goodAfternoon';
	else if (hour < 19) return 'lz.ui.signIn.goodEvening';
	else if (hour < 22) return 'lz.ui.signIn.goodNight';
	else return 'lz.ui.signIn.goodNight';
}
