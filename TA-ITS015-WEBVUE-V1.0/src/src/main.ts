import { createApp } from 'vue';
import pinia from '/@/stores/index';
import App from '/@/App.vue';
import router from '/@/router';
import { directive } from '/@/directive/index';
import { i18n } from '/@/i18n/index';
import other from '/@/utils/other';
import ElementPlus from 'element-plus';
import '/@/theme/index.scss';
import VueGridLayout from 'vue-grid-layout';
// Animation library
import 'animate.css';
// Electronic signature
import VueSignaturePad from 'vue-signature-pad';
// Organizational chart
import vue3TreeOrg from 'vue3-tree-org';
import 'vue3-tree-org/lib/vue3-tree-org.css';
import PrimeVue from 'primevue/config';

// Vxe-Table
import { setupVXETable } from '/@/hooks/setupVXETableHook';
import Aura from '@primevue/themes/aura';
import vi from '/@/i18n/lang/primeVue/vi';
import en from '/@/i18n/lang/primeVue/en';
import { useThemeConfig } from './stores/themeConfig';
const storesThemeConfig = useThemeConfig();
const themeConfig = storesThemeConfig.themeConfig;
const app = createApp(App);
directive(app);
other.elSvg(app);
// Gán biến primevueConfig để dùng sau
const primevueConfig = {
    theme: {
        preset: Aura,
        options: {
            darkModeSelector: false,
        }
    },
    locale: themeConfig.globalI18n == 'vi-VN' ? vi : en
};
globalThis.$primevueConfig = primevueConfig;

app.use(pinia).use(router).use(ElementPlus).use(PrimeVue, primevueConfig).use(setupVXETable).use(i18n).use(VueGridLayout).use(VueSignaturePad).use(vue3TreeOrg).mount('#app');

// Gán vào globalThis (hoặc inject sau cũng được)
