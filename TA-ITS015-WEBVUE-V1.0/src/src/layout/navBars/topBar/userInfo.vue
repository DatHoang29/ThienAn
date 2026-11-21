<template>
    <el-dropdown :show-timeout="70" :hide-timeout="800" size="large" @command="onHandleCommandClick" trigger="click">
			<span class="layout-navbars-breadcrumb-user-link">
				<el-tooltip effect="dark" placement="bottom" :auto-close="1500">
					<template #content>
						{{ $t('lz.entity.base.account') }} : {{ userInfos.account }}<br />
						{{ $t('lz.entity.base.realName') }}：{{ userInfos.realName }}<br />
						<!-- {{ $t('lz.entity.base.phone') }}: {{ userInfos.phone }}<br />
						{{ $t('lz.entity.base.email') }}：{{ userInfos.email }}<br /> -->
						{{ $t('lz.entity.system.org') }}：{{ userInfos.orgName }}<br />
						{{ $t('lz.entity.system.pos') }}：{{ userInfos.posName }}<br />
					</template>
					<el-avatar :src="userInfos.avatar" size="small" class="layout-navbars-breadcrumb-user-link-photo mr5" style="border: 2px solid white;font-size: 16px;width: 28px;height: 28px;"> {{ userInfos.username?.slice(0, 1) ?? userInfos.realName?.slice(0, 1) }} </el-avatar>
				</el-tooltip>
				{{ userInfos.realName == '' ? userInfos.account : userInfos.realName }}
				<el-icon class="el-icon--right">
					<ele-ArrowDown />
				</el-icon>
			</span>
			<template #dropdown>
		<el-dropdown-menu class="setting-dropdown">
			<el-dropdown-item :icon="Avatar" command="/system/userCenter">{{ $t('lz.ui.user.userCenter') }}</el-dropdown-item>
			
			<!-- Split layout setting into two parts -->
			<div class="layout-setting-item">
				<el-dropdown-item class="layout-left" @click="onLayoutSetingClick">
					<i class="icon-skin iconfont"></i>{{ $t('lz.ui.header.layout') }}
				</el-dropdown-item>
				<div class="layout-divider"></div>
				<el-dropdown-item class="layout-right" @click="onScreenfullClick">
					<i class="iconfont" :title="state.isScreenfull ? $t('lz.ui.header.fullScreenOff') : $t('lz.ui.header.fullScreenOn')"
					:class="!state.isScreenfull ? 'icon-fullscreen' : 'icon-tuichuquanping'"></i>
				</el-dropdown-item>
			</div>

			<!-- Language dropdown -->
			<el-dropdown-item>
				<el-dropdown 
					placement="left-start" 
					@command="onLanguageChange"
					trigger="hover"
					class="nested-dropdown"
					:teleported="false"
				>
					<span class="el-dropdown-link">
						<span>{{ $t('lz.ui.language.title') }}</span> 
						<el-icon class="el-icon--right"><ele-ArrowRight /></el-icon>
						<el-tooltip :content="$t('lz.button.base.refresh')" placement="top">
							<el-button link class="ml5" @click="refreshTerm(state.disabledI18n)" icon="ele-Refresh"></el-button>
						</el-tooltip>
					</span>
					<template #dropdown>
						<el-dropdown-menu class="language-dropdown">
							<el-dropdown-item command="vi-VN" :disabled="state.disabledI18n === 'vi-VN'">
								{{ $t('lz.ui.language.vietnamese') }}
							</el-dropdown-item>
							<el-dropdown-item command="en-US" :disabled="state.disabledI18n === 'en-US'">
								{{ $t('lz.ui.language.english') }}
							</el-dropdown-item>
						</el-dropdown-menu>
					</template>
				</el-dropdown>
			</el-dropdown-item>

			<el-dropdown-item :icon="Loading" divided command="clearCache">{{ $t('lz.ui.user.clearCache') }}</el-dropdown-item>
			<el-dropdown-item :icon="Guide" command="logOut">{{ $t('lz.ui.user.logOut') }}</el-dropdown-item>
    	</el-dropdown-menu>
			
			</template>
		</el-dropdown>
</template>

<script setup lang="ts" name="layoutBreadcrumbUser">
import {  ref, computed, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessageBox, ElMessage, ElNotification, ElAvatar } from 'element-plus';
import screenfull from 'screenfull';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useUserInfo } from '/@/stores/userInfo';
import { useThemeConfig } from '/@/stores/themeConfig';
import other from '/@/utils/other';
import mittBus from '/@/utils/mitt';
import { Local, Session } from '/@/utils/storage';
import Push from 'push.js';
import { Avatar, Guide, Loading } from '@element-plus/icons-vue';
import { clearAccessTokens, getAPI } from '/@/utils/axios-utils';
import { SysAuthApi } from '/@/api-services/system';
import { VxeUI } from 'vxe-table';
import { loadTerminology } from '/@/utils/termHelper';
// Import components
// const UserNews = defineAsyncComponent(() => import('/@/layout/navBars/topBar/userNews.vue'));
// const Search = defineAsyncComponent(() => import('/@/layout/navBars/topBar/search.vue'));
// const ServerTime = defineAsyncComponent(() => import('/@/layout/navBars/topBar/serverTime.vue'));

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
const onScreenfullClick = () => {
	if (!screenfull.isEnabled) {
		ElMessage.warning('Fullscreen not supported');
		return false;
	}
	screenfull.toggle();
	screenfull.on('change', () => {
		if (screenfull.isFullscreen) state.isScreenfull = true;
		else state.isScreenfull = false;
	});
};
// Layout configuration icon click event
const onLayoutSetingClick = () => {
	mittBus.emit('openSettingsDrawer');
};


// Dropdown menu click event
const onHandleCommandClick = (path: string) => {

	if (path === 'clearCache') {
		Local.clear();
		Session.clear();
		window.location.reload();
	} else if (path === 'logOut') {
		console.log('Logout');

		ElMessageBox({
			closeOnClickModal: false,
			closeOnPressEscape: false,
			title: t('lz.ui.logOut.logOutTitle'),
			message: t('lz.ui.logOut.logOutMessage'),
			type: 'warning',
			showCancelButton: true,
			confirmButtonText: t('lz.button.base.confirm'),
			cancelButtonText: t('lz.button.base.cancel'),
			buttonSize: 'default',
			beforeClose: async (action, instance, done) => {
				if (action === 'confirm') {
					instance.confirmButtonLoading = true;
					instance.confirmButtonText = t('lz.ui.logOut.logOutExit');
					instance.confirmButtonLoading = false;
					await getAPI(SysAuthApi).apiSystemSysauthLogoutPost();
					done();
				} else {
					done();
				}
			},
		})
			.then(async () => {
				clearAccessTokens();
			})
			.catch(() => { });
	} else {
		router.push(path);
	}
};
// Menu search click event
const onSearchClick = () => {
	searchRef.value.openSearch();
};

const refreshTerm = async (lang: string) => {
	await loadTerminology(lang);
	window.location.reload();
};

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
const onLanguageChange = (lang: string) => {
	Local.remove('themeConfig');
	themeConfig.value.globalI18n = lang;
	Local.set('themeConfig', themeConfig.value);
	locale.value = lang;
	VxeUI.setLanguage(lang);
    
	other.useTitle();
	initI18nOrSize('globalI18n', 'disabledI18n');
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
		title: t('lz.ui.notification.notice'),
		message: t('lz.ui.notification.newMessage'),
		type: 'info',
		position: 'bottom-right',
	});
	Push.create(t('lz.ui.notification.notice'), {
		body: t('lz.ui.notification.newMessage'),
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
        z-index: 10000;
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

.layout-setting-item {
    display: flex;
    align-items: center;
    
    .layout-left {
        flex: 1;
        border-right: 1px solid var(--el-border-color-light);
    }
    
    .layout-right {
        padding: 0 12px;
    }
}

:deep(.el-dropdown-menu__item) {
    &.layout-left,
    &.layout-right {
        display: inline-block;
        margin: 0;
        &:hover {
            background: var(--el-dropdown-menuItem-hover-fill);
        }
    }
}

:deep(.el-sub-menu) {
    &.el-dropdown-menu__item {
        padding: 0;
    }
    .el-sub-menu__title {
        padding: 5px 20px;
        &:hover {
            background: var(--el-dropdown-menuItem-hover-fill);
        }
    }
    .el-menu-item {
        padding: 5px 20px;
        min-width: 150px;
    }
}

.el-dropdown-link {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    cursor: pointer;
	
}

.language-dropdown {
    min-width: 120px !important;
    
    &[aria-hidden="false"] {
        display: block !important;
        opacity: 1 !important;
        visibility: visible !important;
    }
    
    .el-dropdown-menu__item {
        &:hover {
            background-color: var(--el-dropdown-menuItem-hover-fill);
        }
    }
}

:deep(.el-dropdown-menu) {
    &.setting-dropdown {
        // Force dropdown to stay visible during hover
        display: block !important;
    }
}

.setting-dropdown {
    .nested-dropdown {
        width: 100%;
        
        .el-dropdown-menu {
            // Force child dropdown to stay visible when active
            &[aria-hidden="false"] {
                display: block !important;
                opacity: 1 !important;
                visibility: visible !important;
                transform: scaleY(1) !important;

                & ~ .el-popper.el-dropdown__popper {
                    // Force parent to stay visible when child is active
                    display: block !important;
                    opacity: 1 !important;
                    visibility: visible !important;
                    transform: scaleY(1) !important;
                }
            }
        }
    }
}

// Force parent dropdown to stay visible
:deep(.el-popper.el-dropdown__popper) {
    &[data-popper-placement^='bottom'] {
        &.setting-dropdown {
            display: block !important;
            opacity: 1 !important;
            visibility: visible !important;
            transform: scaleY(1) !important;
        }
    }
}

// Ensure all poppers stay visible when active
:deep(.el-popper) {
    &[role="tooltip"][aria-hidden="false"] {
        display: block !important;
        opacity: 1 !important;
        visibility: visible !important;
        z-index: 2100 !important;
    }
}
</style>
