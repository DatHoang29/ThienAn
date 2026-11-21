/**
 * Date and time setting shortcut options
 * @returns
 */
export const useDateTimeShortCust = () => {
	return [
		{ text: 'lz.ui.label.today', value: new Date() },
		{
			text: 'lz.ui.label.yesterday',
			value: () => {
				const date = new Date();
				date.setTime(date.getTime() - 3600 * 1000 * 24);
				return date;
			},
		},
		{
			text: 'lz.ui.label.lastWeek',
			value: () => {
				const date = new Date();
				date.setTime(date.getTime() - 3600 * 1000 * 24 * 7);
				return date;
			},
		},
	];
};
