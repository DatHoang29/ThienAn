import { createI18n } from 'vue-i18n';
import pinia from '/@/stores/index';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';

// Define language internationalization content

/**
 * Note:
 * You need to create a new folder under 'pages' (it's recommended that the `interface directory to be internationalized` 
 * and the `i18n directory` are the same for easy lookup).
 * Be careful not to use the same field names as existing definitions when defining internationalization fields.
 * 1. The ts files under /src/i18n/lang are for the framework's internationalization content
 * 2. The ts files under /src/i18n/pages are for the internationalization content of each interface
 */

// Element Plus built-in internationalization
import enLocale from 'element-plus/es/locale/lang/en';
import viLocale from 'element-plus/es/locale/lang/vi';
import json from '/@/utils/locale';

// Tùy chỉnh ngôn ngữ tiếng Việt cho Element Plus
if (viLocale.el?.datepicker) {
	viLocale.el.datepicker.year = '';
	viLocale.el.datepicker.confirm = 'Xác nhận';
}
if (viLocale.el?.messagebox) {
	viLocale.el.messagebox.confirm = 'Xác nhận';
}
if (viLocale.el?.colorpicker) {
	viLocale.el.colorpicker.confirm = 'Xác nhận';
}

// Define variable content
const element = { 'en-US': enLocale, 'vi-VN': viLocale };
// const itemize = { 'en-US': [], 'vi-VN': [] };

const messages = Object.assign(json);
// Add element['en'].el to messages['en']
for (const key in element) {
	if (messages[key]) {
		messages[key].el = element[key].el;
		messages[key].name = key;
	}
}
// Read the default language from pinia
const stores = useThemeConfig(pinia);
const { themeConfig } = storeToRefs(stores);
// Export language internationalization
// https://vue-i18n.intlify.dev/guide/essentials/fallback.html#explicit-fallback-with-one-locale
export const i18n = createI18n({
	legacy: false,
	silentTranslationWarn: true,
	missingWarn: false,
	silentFallbackWarn: true,
	fallbackWarn: false,
	locale: themeConfig.value.globalI18n,
	fallbackLocale: 'vi-VN',
	messages,
});
