/** Metadata của đúng tháng theo giờ địa phương; không truy vấn phần thời gian trong tương lai. */
export const getRecordingPeriodRange = (day: Date, now = new Date()) => {
	const selectedDay = new Date(day.getFullYear(), day.getMonth(), day.getDate());
	const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
	if (!Number.isFinite(selectedDay.getTime()) || selectedDay > today) throw new RangeError('Ngày xem lại không hợp lệ');
	const fromDate = new Date(selectedDay.getFullYear(), selectedDay.getMonth(), 1);
	const rangeEnd = new Date(selectedDay.getFullYear(), selectedDay.getMonth() + 1, 1);
	return { fromDate, toDate: new Date(Math.min(rangeEnd.getTime(), now.getTime())) };
};
