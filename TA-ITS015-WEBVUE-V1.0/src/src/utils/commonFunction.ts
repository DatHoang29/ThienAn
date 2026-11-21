// Common functions
import useClipboard from 'vue-clipboard3';
import { ElMessage } from 'element-plus';
import { formatDate } from '/@/utils/formatTime';
import { useI18n } from 'vue-i18n';

export default function () {
	const { t } = useI18n();
	const { toClipboard } = useClipboard();

	// Percentage formatting
	const percentFormat = (row: EmptyArrayType, column: number, cellValue: string) => {
		return cellValue ? `${cellValue}%` : '-';
	};
	// List date format (YMD)
	const dateFormatYMD = (row: EmptyArrayType, column: number, cellValue: string) => {
		if (!cellValue) return '-';
		return formatDate(new Date(cellValue), 'YYYY-mm-dd');
	};
	// List date format (YMDHMS)
	const dateFormatYMDHMS = (row: EmptyArrayType, column: number, cellValue: string) => {
		if (!cellValue) return '-';
		return formatDate(new Date(cellValue), 'YYYY-mm-dd HH:MM:SS');
	};
	// List date format (HMS)
	const dateFormatHMS = (row: EmptyArrayType, column: number, cellValue: string) => {
		if (!cellValue) return '-';
		let time = 0;
		if (typeof row === 'number') time = row;
		if (typeof cellValue === 'number') time = cellValue;
		return formatDate(new Date(time * 1000), 'HH:MM:SS');
	};
	// Decimal formatting
	const scaleFormat = (value: string = '0', scale: number = 4) => {
		return Number.parseFloat(value).toFixed(scale);
	};
	// Decimal formatting (2 decimal places)
	const scale2Format = (value: string = '0') => {
		return Number.parseFloat(value).toFixed(2);
	};
	// Thousands separator, default to two decimal places
	const groupSeparator = (value: number, minimumFractionDigits: number = 2) => {
		return value.toLocaleString('en-US', {
			minimumFractionDigits: minimumFractionDigits,
			maximumFractionDigits: 2,
		});
	};
	// Click to copy text
	const copyText = (text: string) => {
		return new Promise((resolve, reject) => {
			try {
				// Copy
				toClipboard(text);
				// You can set up a success message for copying here
				ElMessage.success(t('lz.ui.setting.copyTextSuccess'));
				resolve(text);
			} catch (e) {
				// Copy failed
				ElMessage.error(t('lz.ui.setting.copyTextError'));
				reject(e);
			}
		});
	};
	// Remove HTML tags (take the first 5 characters)
	const removeHtmlSub = (value: string) => {
		var str = value.replace(/<[^>]+>/g, '');
		if (str.length > 50) return str.substring(0, 50) + '......';
		else return str;
	};
	// Remove HTML tags
	const removeHtml = (value: string) => {
		return value.replace(/<[^>]+>/g, '');
	};
	// Get enum description
	const getEnumDesc = (key: any, lstEnum: any) => {
		return lstEnum.find((x: any) => x.value == key)?.describe;
	};
	return {
		percentFormat,
		dateFormatYMD,
		dateFormatYMDHMS,
		dateFormatHMS,
		scaleFormat,
		scale2Format,
		groupSeparator,
		copyText,
		removeHtmlSub,
		removeHtml,
		getEnumDesc,
	};
}
