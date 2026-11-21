<template>
	<el-config-provider :size="getGlobalComponentSize" :locale="getGlobalI18n">
			<router-view v-show="setLockScreen" />
		<LockScreen v-if="themeConfig.isLockScreen" />
		<Settings ref="settingsRef" v-show="setLockScreen" />
		<CloseFull v-if="!themeConfig.isLockScreen" />

	</el-config-provider>
	<!-- Incident Handle Dialog -->
	<incidentPopup ref="incidentPopupRef" />
	<!-- SonTH - 24/07/2026: Banner cảnh báo sự cố sắp được xử lý tự động (hiển thị trên mọi màn hình) -->
	<incidentAutomationBanner />
	<!-- Cảnh báo phương tiện thuộc danh sách lọc: chỉ lượt MỞ CHUYẾN (notify) mới hiện toast -->
	<vehicleFilterAlert />
</template>

<script setup lang="ts" name="app">
import { defineAsyncComponent, computed, ref, onBeforeMount, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import { useThemeConfig } from '/@/stores/themeConfig';
import { setSemanticTheme, applyDesignPreset } from '/@/utils/theme';
import other from '/@/utils/other';
import { Local, Session } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';
import setIntroduction from '/@/utils/setIconfont';
import { loadTerminologyClient } from '/@/utils/termHelper';
// import Watermark from '/@/utils/watermark';
import { SysConfigApi } from '/@/api-services';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { getAPI } from '/@/utils/axios-utils';
import TransporterSocketCluster from '/@/transporter/transporterSocket';
import TANatsClient from '/@/transporter/transporterNats';
import { useBaseConfig } from '/@/stores/baseConfig';


// Import components
const LockScreen = defineAsyncComponent(() => import('/@/layout/lockScreen/index.vue'));
const Settings = defineAsyncComponent(() => import('/@/layout/navBars/topBar/settings.vue'));
const CloseFull = defineAsyncComponent(() => import('/@/layout/navBars/topBar/closeFull.vue'));
// const Upgrade = defineAsyncComponent(() => import('/@/layout/upgrade/index.vue'));
// const Sponsors = defineAsyncComponent(() => import('/@/layout/sponsors/index.vue'));

// Define variable content
const { messages, locale } = useI18n();
const settingsRef = ref();
const route = useRoute();
const stores = useTagsViewRoutes();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const needUpdate = ref(false);

//SonTH: load defaultlanguage to indexedDB
loadTerminologyClient(Local.get('lang') ?? 'vi-VN', false);

//SonTH: load config data first time if not exist
useBaseConfig().checkConfigDataList();
useBaseConfig().checkOperationConfigList();	

// COMMENT BY NGỌC THẮNG - 08/11/2024 - DON'T COMMIT
const socketTransporter = ref<TransporterSocketCluster>();

// khai báo sử dụng Nats transporter dùng thay thế/bổ sung SocketCluster - ChuongLP - 10/05/2026
const transporterNats = ref<TANatsClient>();

// Set component visibility when locked
const setLockScreen = computed(() => {
	// Prevent unrelated interface from appearing after locking
	// https://gitee.com/lyt-top/vue-next-admin/issues/I6AF8P
	return themeConfig.value.isLockScreen ? themeConfig.value.lockScreenTime > 1 : themeConfig.value.lockScreenTime >= 0;
});

// // Get version number
// const getVersion = computed(() => {
// 	let isVersion = false;
// 	if (route.path !== '/login') {
// 		// @ts-ignore
// 		if ((Local.get('version') && Local.get('version') !== __NEXT_VERSION__) || !Local.get('version')) isVersion = true;
// 	}
// 	return isVersion;
// });

// checkUpdate(() => {
// 	needUpdate.value = true;
// }, 60000);

// Get global component size
const getGlobalComponentSize = computed(() => {
	return other.globalComponentSize();
});
// Get global i18n
const getGlobalI18n = computed(() => {
	return messages.value[locale.value];
});
// Set initialization to prevent restoring defaults on refresh
onBeforeMount(() => {
	// Set batch third-party icon
	setIntroduction.cssCdn();
	// Set batch third-party js
	setIntroduction.jsCdn();
});

// Xử lý lấy thông tin NATS creds từ environment variables hoặc URL, hỗ trợ cả inline creds và creds từ URL - ChuongLP - 10/05/2026
const getEnv = (key: string) => {
	return window.__env__?.[key] ?? import.meta.env[key];
};

// Xử lý tải NATS creds, ưu tiên inline creds nếu có, nếu không thì tải từ URL được cấu hình - ChuongLP - 10/05/2026
const loadNatsCreds = async (): Promise<string | undefined> => {
	const inlineCreds = getEnv('VITE_NATS_CREDS');

	if (inlineCreds && String(inlineCreds).trim().length > 0) {
		return String(inlineCreds);
	}

	const credsUrl = getEnv('VITE_NATS_CREDS_URL');

	if (!credsUrl) {
		return undefined;
	}

	const response = await fetch(credsUrl);

	if (!response.ok) {
		throw new Error(`Không tải được NATS creds từ ${credsUrl}`);
	}

	return await response.text();
};

// Khởi tạo NATS transporter nếu được bật và cấu hình đúng, hỗ trợ cả token và creds - ChuongLP - 10/05/2026
const initNatsTransporter = async () => {
	const natsUrl = getEnv('VITE_NATS_WS_URL');
	const natsEnabled = getEnv('VITE_NATS_ENABLED');

	if (natsEnabled === false || natsEnabled === 'false') {
		console.warn('[App.vue] NATS transporter disabled by env.');
		return;
	}

	if (!natsUrl) {
		console.warn('[App.vue] VITE_NATS_WS_URL chưa được cấu hình.');
		return;
	}

	const creds = await loadNatsCreds();

	transporterNats.value = new TANatsClient({
		servers: natsUrl,
		name: getEnv('VITE_NATS_CLIENT_NAME') || 'TA_WEB_DASHBOARD',
		token: getEnv('VITE_NATS_TOKEN') || undefined,
		creds,
		autoSubscribe: true,
	});

	console.log('[App.vue] NATS transporter initialized:', natsUrl);
};

// On page load
onMounted(async () => {

	// socketTransporter.value = new TransporterSocketCluster({
	// 	hostname: window.__env__.VITE_SOCKET_HOST,
	// 	port: parseInt(window.__env__.VITE_SOCKET_PORT),
	// 	secure: false,
	// });

	// ------------------------------------------------------------------
	// KHÔI PHỤC THEME NGAY (ĐỒNG BỘ, TRƯỚC MỌI `await` BÊN DƯỚI).
	// LÝ DO (bug F5 mất màu tự chỉnh): loadSysInfo() được gọi ở setup và là bất đồng bộ.
	// Nếu việc khôi phục theme nằm SAU `await initNatsTransporter()` (hàm này await fetch
	// NATS creds nên tốn thời gian) thì API sysinfo có thể trả về NGAY TRONG lúc await,
	// khiến loadSysInfo lưu `themeConfig` (đang là màu mặc định do cache chưa kịp khôi phục)
	// ĐÈ lên localStorage -> lần khôi phục sau đọc phải màu mặc định -> mất màu người dùng.
	// Khôi phục đồng bộ tại đây đảm bảo store đã đúng màu TRƯỚC khi bất kỳ callback mạng
	// (.then của loadSysInfo) có cơ hội chạy.
	// ------------------------------------------------------------------
	if (Local.get('themeConfig')) {
		const cachedThemeConfig = Local.get('themeConfig');
		// Migration: nâng cấp màu chủ đạo cũ (#1578a3 - bị chê nhạt) lên tông sống động mặc định mới.
		if ((cachedThemeConfig.primary || '').toLowerCase() === '#1578a3') {
			cachedThemeConfig.primary = themeConfig.value.primary;
		}
		storesThemeConfig.setThemeConfig({ themeConfig: cachedThemeConfig });
		// Guard: chỉ khôi phục khi có snapshot hợp lệ. Nếu là null (khoá không tồn tại),
		// gán cssText = null sẽ xoá sạch biến CSS inline trên <html> -> mất màu sidebar/primary.
		const cachedThemeConfigStyle = Local.get('themeConfigStyle');
		if (cachedThemeConfigStyle) document.documentElement.style.cssText = cachedThemeConfigStyle;
	}
	// Áp DESIGN PRESET đang bật (theme/presets/designPreset.ts): gắn class vào <html>,
	// ghi đè màu/font theo preset rồi sinh thang màu primary + biến bố cục.
	// Preset 'original' = giữ nguyên bản, không ghi đè gì.
	applyDesignPreset(themeConfig.value);
	// Áp bộ màu ngữ nghĩa rực rỡ (success/warning/danger) cho toàn app.
	setSemanticTheme();

	// Khai báo Transporter NATS mới
	await initNatsTransporter();

	nextTick(() => {
		// Listen for layout configuration drawer open click
		mittBus.on('openSettingsDrawer', () => {
			settingsRef.value.openDrawer();
		});
		// Get fullscreen configuration from cache
		if (Session.get('isTagsViewCurrenFull')) {
			stores.setCurrenFullscreen(Session.get('isTagsViewCurrenFull'));
		}
	});

	// Begin - Add by Ngọc Thắng
	// Delay emit event to ensure incidentPopup component is mounted and ready
	// Using nextTick and setTimeout to guarantee listener is setup
	await nextTick();
	setTimeout(() => {
		console.log('[App.vue] onMounted - emitting loadIncidentsOnPageLoad event');
		(mittBus as any).emit('loadIncidentsOnPageLoad', { autoOpen: true });
		isInitialPageLoad.value = false;
		console.log('[App.vue] onMounted completed');
	}, 200);
	// End - Add by Ngọc Thắng
});
// On page destroy, close layout configuration/i18n listener
onUnmounted(async () => {
	mittBus.off('openSettingsDrawer', () => { });
	(mittBus as any).off('loadIncidentsOnPageLoad');
	(mittBus as any).off('loadIncidentsOnRouteChange');

	// Ngắt NATS khi app destroy
	await transporterNats.value?.disconnect(); // Nhớ thêm "await" để đảm bảo ngắt kết nối hoàn tất trước khi component bị hủy
});

// Listen for route changes to set website title
watch(
	() => route.path,
	() => {
		other.useTitle();
	},
	{
		deep: true,
	}
);

// Load system information
const loadSysInfo = () => {
	getAPI(SysConfigApi)
		.apiSystemSysconfigSysinfoGet()
		.then((res) => {

			if (res.data.succeeded != true) return;

			const data = res.data.result;
			// System logo
			themeConfig.value.logoUrl = data.sysLogo;
			// Main title
			themeConfig.value.globalTitle = data.sysTitle;
			// Subtitle
			themeConfig.value.globalViceTitle = data.sysViceTitle;
			// System description
			themeConfig.value.globalViceTitleMsg = data.sysViceDesc;
			// Icp filing information
			themeConfig.value.icp = data.sysIcp;
			themeConfig.value.icpUrl = data.sysIcpUrl;
			// Watermark
			themeConfig.value.isWatermark = data.sysWatermark != null;
			themeConfig.value.watermarkText = data.sysWatermark;
			// Copyright statement
			themeConfig.value.copyright = data.sysCopyright;
			// Update favicon
			updateFavicon(data.sysLogo);

			// Save configuration
			Local.remove('themeConfig');
			Local.set('themeConfig', storesThemeConfig.themeConfig);
		})
		.catch(() => {
			// Clear logo address
			themeConfig.value.logoUrl = '';
			// Save configuration
			Local.remove('themeConfig');
			Local.set('themeConfig', storesThemeConfig.themeConfig);
			return;
		});
};

// Update favicon
const updateFavicon = (url: string): void => {
	const favicon = document.getElementById('favicon') as HTMLAnchorElement;
	favicon!.href = url ? url : 'data:;base64,=';
};

// Load system information
loadSysInfo();

// Prevent Firefox from opening a new window when dragging
document.body.ondrop = function (event) {
	event.preventDefault();
	event.stopPropagation();
};



// ============== BEGIN - MONITOR INCIDENT - AUTHOR: NGỌC THẮNG
// IMPORTANCE: THÔNG BÁO CẦN THIẾT TRONG HAI TRƯỜNG HỢP (CHỦ ĐỘNG WATCH-SOCKET VÀ BỊ ĐỘNG WATCH-ROUTE)
// TRƯỜNG HỢP 1: TRIGGER NHẮC NHỞ NGAY KHI CÓ SỰ THAY ĐỔI TRONG HỆ INCIDENT => WEB-SOCKET TRÊN MỌI MÀN HÌNH (MỘT LẦN DUY NHẤT)
// TRƯỜNG HỢP 2: NHẮC NHỞ KHI INCIDENT VẪN CHƯA ĐƯỢC XỬ LÝ BỞI USER HIỆN TẠI => KHI TRUY CẬP CÁC MÀN HÌNH (MỘT LẦN DUY NHẤT)
import { SysOrgApi } from './api-services/system';
import { SysOpConfigApi } from './api-services/cfgSystem';

import { useCommonVarsAndFuncs } from './stores/its/commonVarsAndFuncs';
const commonVarsAndFuncs = useCommonVarsAndFuncs();

import incidentPopup from '/@/views/modal/incident/incidentPopup.vue';
const incidentPopupRef = ref<InstanceType<typeof incidentPopup>>();

// SonTH - 24/07/2026: Banner cảnh báo tự động xử lý sự cố, tự quản lý vòng đời countdown/nạp dữ liệu
const incidentAutomationBanner = defineAsyncComponent(() => import('/@/views/modal/incidentAutomation/incidentAutomationBanner.vue'));

// Cảnh báo phương tiện theo danh sách TmsVehicleFilter, hiển thị toàn cục bên trái màn hình — chỉ lượt mở chuyến.
const vehicleFilterAlert = defineAsyncComponent(() => import('/@/views/modal/vehicleFilter/vehicleFilterAlert.vue'));

// KienLT - 23/10/2026: MỌI lượt xe khớp danh sách lọc (NATS ta.its.data.vds.filter) gom theo chuyến vào "Hoạt động
// gần đây" (bigscreen) qua store activityFeed — toast chỉ hiện lượt mở chuyến, dự án ~2 km một camera thì báo mọi
// lượt sẽ bật liên tục. Nghe ở đây (toàn cục) để mở bigscreen là đã có các lượt nhận trước đó.
import { useTransporterVehicleAlert } from '/@/transporter/hooks/useTransporterVehicleFilter';
import { useActivityFeed } from '/@/stores/its/activityFeed';
const activityFeed = useActivityFeed();
useTransporterVehicleAlert((data) => activityFeed.pushVehicleFilterPayload(data));

// Track if this is initial page load
const isInitialPageLoad = ref(true);

const showIncidentsByAccount = async() => {
	console.log('[App.vue] showIncidentsByAccount called, isUserLoggedIn:', isUserLoggedIn());
	
	if (!isUserLoggedIn())
		return;

	await incidentPopupRef.value?.loadIncidentsByAccount(true); // Pass true to auto-open on first load
	console.log('[App.vue] showIncidentsByAccount completed');
};

const setCommonVariables = async () => {
	if (!isUserLoggedIn())
		return;

	if((commonVarsAndFuncs.serverHost?.length ?? 0) == 0){
		// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
		commonVarsAndFuncs.serverHost = await useItsMediaServerStore().ensureLoaded();
	}

	if((commonVarsAndFuncs.operationConfigList?.length ?? 0) == 0){
		const response = await getAPI(SysOpConfigApi).apiCfgsystemSysopconfigListGet();
		commonVarsAndFuncs.operationConfigList = response.data.result;
	}

	if((commonVarsAndFuncs.userOrgList?.length ?? 0) == 0){
		const response = await getAPI(SysOrgApi).apiSystemSysorgListGet(0);
		retrieveAllOrganization(response.data.result);
	}
};

// Retrieve all organization 
const retrieveAllOrganization = (orgList: Array | null, depth: Number = 0) => {
	// Recursively up to a depth of 10
	if(depth > 10 || (orgList?.length ?? 0) == 0)
		return;

	orgList?.forEach(orgObj => {
		commonVarsAndFuncs.userOrgList.push({orgId: orgObj.id, orgName: orgObj.name});

		if (orgObj?.children && orgObj?.children.length > 0) { 
			retrieveAllOrganization(orgObj.children, depth + 1); 
		} 
	});
};

// Check if the user has logged in
const isUserLoggedIn = () => {
	if ((route.path?.replaceAll('/', '').length ?? 0) == 0 || route.path?.includes('login'))
		return false;

	return Local.get('access-token')?.length > 0 || Session.get('token')?.length > 0;
}

// Monitoring changes in the incident system => WATCH-ROUTE
watch(() => route.path, async() => {
	console.log('[App.vue] Route changed, isInitialPageLoad:', isInitialPageLoad.value, 'path:', route.path);
	
	// Skip initial page load - it's handled in onMounted
	if (isInitialPageLoad.value) {
		console.log('[App.vue] Skipping route watcher - initial page load');
		return;
	}
	
	await setCommonVariables(); // Must use the keyword "await" here
	
	// When route changes: emit event to show FAB and blink, don't auto-open dialog
	if (isUserLoggedIn()) {
		console.log('[App.vue] Route changed - emitting loadIncidentsOnRouteChange event');
		(mittBus as any).emit('loadIncidentsOnRouteChange', { autoOpen: false });
	}
});
// ============== END - MONITOR INCIDENT - AUTHOR: NGỌC THẮNG

</script>

<style lang="scss">
.el-form--inline {
	.el-form-item {
		.el-select {
			width: 171px !important;
		}

		.el-select__wrapper {
			line-height: 22px !important;
		}

		.el-date-editor {
			--el-date-editor-width: 171px !important;
		}

		.el-input {
			width: 171px !important;
		}
	}
}
</style>
