import enUS from '../i18n/lang/en-us.json';
import viVN from '../i18n/lang/vi-vn.json';
import enUSTable from '../i18n/lang/vxetable/en-us.json';
import viVNTable from '../i18n/lang/vxetable/vi-vn.json';
import enUSLogin from '../i18n/lang/login/en-us.json';
import viVNLogin from '../i18n/lang/login/vi-vn.json';
import enUSUI from '../i18n/lang/ui/en-us.json';
import viVNUI from '../i18n/lang/ui/vi-vn.json';
import { terminologyiDb } from '../indexedDb/cfgSystem/terminologyiDb';

// Từ điển ITS: its/<locale>.json (dùng chung) + its/<phân hệ>/<locale>.json. Thêm phân hệ mới chỉ cần tạo thư mục, không cần sửa file này.
const enUSIts = import.meta.glob<Record<string, string>>('../i18n/lang/its/**/en-us.json', { eager: true, import: 'default' });
const viVNIts = import.meta.glob<Record<string, string>>('../i18n/lang/its/**/vi-vn.json', { eager: true, import: 'default' });
const mergeIts = (files: Record<string, Record<string, string>>) => Object.assign({}, ...Object.values(files));

// Default locale
const locales = {
  'en-US': {
    ...enUS,
    ...mergeIts(enUSIts),
    // ...enUSTable,
    ...enUSLogin,
    ...enUSUI
  },
  'vi-VN': {
    ...viVN,
    ...mergeIts(viVNIts),
    // ...viVNTable,
    ...viVNLogin,
    ...viVNUI
  }
};

// SonTH: Get terminology list from indexedDB
async function getTerminologyList(lang: string) {
  if (lang == undefined) {
    lang = 'vi-VN';
  }
  //call terminology from indexedDB
  var term = await terminologyiDb.terminology
    .where('lang')
    .equalsIgnoreCase(lang)
    .first();
  var result = JSON.parse(term?.value || '{}');
  switch (lang) {
    case 'en-US':
      return {
        'en-US': {
          ...result,
          ...Object.fromEntries(Object.entries(locales['en-US']).filter(([key]) => !result[key])),
          ...Object.fromEntries(Object.entries(enUSLogin).filter(([key]) => !result[key])),
          ...Object.fromEntries(Object.entries(enUSUI).filter(([key]) => !result[key])),
        }
      };
    case 'vi-VN':
      return {
        'vi-VN': {
          ...result,
          ...Object.fromEntries(Object.entries(locales['vi-VN']).filter(([key]) => !result[key])),
          ...Object.fromEntries(Object.entries(viVNLogin).filter(([key]) => !result[key])),
          ...Object.fromEntries(Object.entries(viVNUI).filter(([key]) => !result[key])),
        }
      };
  }
  return result;
}

// Combine viVN and viVNTable
export default locales;


export { getTerminologyList };