<template>
	<div class="layout-navbars-breadcrumb-user pr15" :style="{ flex: layoutUserFlexNum }">
		<ServerTime/>
		<NotiBell />

		<UserInfo />
		<Search ref="searchRef" />
		<!-- <OnlineUser ref="onlineUserRef" /> -->
	</div>
</template>

<script setup lang="ts" name="layoutBreadcrumbUser">
import { defineAsyncComponent, ref, computed, reactive, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElNotification } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useUserInfo } from '/@/stores/userInfo';
import { useThemeConfig } from '/@/stores/themeConfig';
import other from '/@/utils/other';
import { Local, Session } from '/@/utils/storage';
import Push from 'push.js';
import { VxeUI } from 'vxe-table';
import { loadTerminology } from '/@/utils/termHelper';
// Import components
const UserNews = defineAsyncComponent(() => import('/@/layout/navBars/topBar/userNews.vue'));
const Search = defineAsyncComponent(() => import('/@/layout/navBars/topBar/search.vue'));
const ServerTime = defineAsyncComponent(() => import('/@/layout/navBars/topBar/serverTime.vue'));
const UserInfo = defineAsyncComponent(() => import('/@/layout/navBars/topBar/userInfo.vue'));
const NotiBell = defineAsyncComponent(() => import('/@/views/notification/component/notiBell.vue'));

// const OnlineUser = defineAsyncComponent(() => import('/@/views/system/onlineUser/index.vue'));

// Define variable content
const { locale, t } = useI18n();
const router = useRouter();
const stores = useUserInfo();
const storesThemeConfig = useThemeConfig();
const { userInfos } = storeToRefs(stores);
const { themeConfig } = storeToRefs(storesThemeConfig);
const searchRef = ref();
// const onlineUserRef = ref();
const state = reactive({
	isScreenfull: false,
	disabledI18n: 'vi-VN',
	disabledSize: 'large',
	noticeList: [] as any, // Internal message list
});
// Set split style
const layoutUserFlexNum = computed(() => {
	let num: string | number = '';
	const { layout, isClassicSplitMenu } = themeConfig.value;
	const layoutArr: string[] = ['defaults', 'columns'];
	if (layoutArr.includes(layout) || (layout === 'classic' && !isClassicSplitMenu)) num = '1';
	else num = '';
	return num;
});
// Check for unread messages
const hasUnreadNotice = computed(() => {
	return state.noticeList.some((r: any) => r.readStatus == undefined || r.readStatus == 0);
});
// Fullscreen click event
// const onScreenfullClick = () => {
// 	if (!screenfull.isEnabled) {
// 		ElMessage.warning('Fullscreen not supported');
// 		return false;
// 	}
// 	screenfull.toggle();
// 	screenfull.on('change', () => {
// 		if (screenfull.isFullscreen) state.isScreenfull = true;
// 		else state.isScreenfull = false;
// 	});
// };
// // Layout configuration icon click event
// const onLayoutSetingClick = () => {
// 	mittBus.emit('openSettingsDrawer');
// };
// Dropdown menu click event
// const onHandleCommandClick = (path: string) => {

// 	if (path === 'clearCache') {
// 		Local.clear();
// 		Session.clear();
// 		window.location.reload();
// 	} else if (path === 'logOut') {
// 		console.log('Logout');

// 		ElMessageBox({
// 			closeOnClickModal: false,
// 			closeOnPressEscape: false,
// 			title: t('lz.ui.logOut.logOutTitle'),
// 			message: t('lz.ui.logOut.logOutMessage'),
// 			type: 'warning',
// 			showCancelButton: true,
// 			confirmButtonText: t('lz.button.base.confirm'),
// 			cancelButtonText: t('lz.button.base.cancel'),
// 			buttonSize: 'default',
// 			beforeClose: async (action, instance, done) => {
// 				if (action === 'confirm') {
// 					instance.confirmButtonLoading = true;
// 					instance.confirmButtonText = t('lz.ui.logOut.logOutExit');
// 					instance.confirmButtonLoading = false;
// 					await getAPI(SysAuthApi).apiSystemSysauthLogoutPost();
// 					done();
// 				} else {
// 					done();
// 				}
// 			},
// 		})
// 			.then(async () => {
// 				clearAccessTokens();
// 			})
// 			.catch(() => { });
// 	} else {
// 		router.push(path);
// 	}
// };
// // Menu search click event
// const onSearchClick = () => {
// 	searchRef.value.openSearch();
// };
// Online user list
// const onOnlineUserClick = () => {
// 	onlineUserRef.value.openDrawer();
// };
// Component size change
const onComponentSizeChange = (size: string) => {
	Local.remove('themeConfig');
	themeConfig.value.globalComponentSize = size;
	Local.set('themeConfig', themeConfig.value);
	initI18nOrSize('globalComponentSize', 'disabledSize');
	window.location.reload();
};
// Language switch
const onLanguageChange = async (lang: string) => {
	Local.remove('themeConfig');
	themeConfig.value.globalI18n = lang;
	Local.set('themeConfig', themeConfig.value);
	locale.value = lang;
	VxeUI.setLanguage(lang);
    
	other.useTitle();
	initI18nOrSize('globalI18n', 'disabledI18n');

	// SonTH: Refresh language server and client
	await loadTerminology(lang);
	window.location.reload();
};

// SonTH: Refresh language server and client
const refreshTerm = async (lang: string) => {
	await loadTerminology(lang);
	window.location.reload();
};

// Initialize component size/i18n
const initI18nOrSize = (value: string, attr: string) => {
	(<any>state)[attr] = Local.get('themeConfig')[value];
};
// Page load event
onMounted(async () => {
	if (Local.get('themeConfig')) {
		initI18nOrSize('globalComponentSize', 'disabledSize');
		initI18nOrSize('globalI18n', 'disabledI18n');
	}
	// Manually request user desktop notification permission
	if (Push.Permission.GRANTED) {
		// Check if permission is granted, if not request manually
		Push.Permission.request(null, null);
	}
	// Listen for browser visibility change
	document.addEventListener('visibilitychange', () => {
		if (!document.hidden) {
			// Clear close message notifications
			Push.clear();
		}
	});
	// Load unread internal messages TODO notification
	// var res = await getAPI(SysNoticeApi).apiSysNoticeUnReadListGet();
	// state.noticeList = res.data.result ?? [];

	// Receive internal messages
	// signalR.on('PublicNotice', receiveNotice);

	// // Handle message read status
	// mittBus.on('noticeRead', (id) => {
	// 	const notice = state.noticeList.find((r: any) => r.id == id);
	// 	if (notice == undefined) return;

	// 	// Set as read
	// 	notice.readStatus = 1;
	// });
});
// // Page unload event
// onUnmounted(() => {
// 	mittBus.off('noticeRead', () => {});
// });

const receiveNotice = (msg: any) => {
	state.noticeList.unshift(msg);

	ElNotification({
		title: 'Notification',
		message: 'You have a new message...',
		type: 'info',
		position: 'bottom-right',
	});
	Push.create('Notification', {
		body: 'You have a new message',
		icon: 'logo.png',
		timeout: 4500,
	});
};
</script>

<style scoped lang="scss">
.layout-navbars-breadcrumb-user {
	display: flex;
	align-items: center;
	justify-content: flex-end;

	&-link {
		height: 100%;
		display: flex;
		align-items: center;
		white-space: nowrap;

		&-photo {
			width: 25px;
			height: 25px;
			border-radius: 100%;
		}
	}

	&-icon {
		padding: 0 10px;
		cursor: pointer;
		color: var(--next-bg-topBarColor);
		height: 50px;
		line-height: 50px;
		display: flex;
		align-items: center;

		&:hover {
			background: var(--next-color-user-hover);

			i {
				display: inline-block;
				animation: logoAnimation 0.3s ease-in-out;
			}
		}
	}

	:deep(.el-dropdown) {
		color: var(--next-bg-topBarColor);
	}

	:deep(.el-badge) {
		height: 40px;
		line-height: 40px;
		display: flex;
		align-items: center;
	}

	:deep(.el-badge__content.is-fixed) {
		top: 12px;
	}
}
</style>
