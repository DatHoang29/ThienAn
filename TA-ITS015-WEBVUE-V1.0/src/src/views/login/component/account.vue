<template>
	<el-tooltip :visible="state.capsLockVisible" effect="light" :content="$t('lz.login.label.capLockOn')" placement="top">
		<el-form ref="ruleFormRef" :model="state.ruleForm" size="large" :rules="state.rules" class="login-content-form">
			<el-form-item class="login-animation1" prop="account" style="margin-bottom: 20px;">
				<el-input ref="accountRef" text :placeholder="$t('lz.login.label.pleaseInput', {input: $t('lz.login.label.account')})" v-model="state.ruleForm.username" clearable autocomplete="off" @keyup.enter.native="handleSignIn" class="input-group">
					<template #prefix>
						<el-icon>
							<ele-User />
						</el-icon>
					</template>
				</el-input>
			</el-form-item>
			<el-form-item class="login-animation2" prop="password" style="margin-bottom: 20px;">
				<el-input ref="passwordRef" :type="state.isShowPassword ? 'text' : 'password'" :placeholder="$t('lz.login.label.pleaseInput', {input: $t('lz.login.label.password')})" v-model="state.ruleForm.password" autocomplete="off" @keyup.enter.native="handleSignIn" class="input-group">
					<template #prefix>
						<el-icon>
							<ele-Unlock />
						</el-icon>
					</template>
					<template #suffix>
						<i class="iconfont el-input__icon login-content-password " :class="state.isShowPassword ? 'icon-yincangmima' : 'icon-xianshimima'" @click="state.isShowPassword = !state.isShowPassword">
						</i>
					</template>
				</el-input>
			</el-form-item>
			<el-form-item v-if="state.showOtpField" class="login-animation3" prop="otp" style="margin-bottom: 20px;">
				<el-input ref="otpRef" text :placeholder="$t('lz.login.label.pleaseInput', {input: $t('lz.login.label.otp')})" v-model="state.ruleForm.otp" clearable autocomplete="off" @keyup.enter.native="handleSignIn" class="input-group">
					<template #prefix>
						<el-icon>
							<ele-Key />
						</el-icon>
					</template>
				</el-input>
			</el-form-item>
			<el-form-item class="login-animation4">
				<el-button type="primary" class="login-content-submit" round v-waves="'light'" @click="handleSignIn" :loading="state.loading.signIn">
					<span>{{ $t('lz.login.button.signIn') }}</span>
					<el-icon v-if="!state.loading.signIn" class="login-submit__arrow"><ele-Right /></el-icon>
				</el-button>
			</el-form-item>
		</el-form>
	</el-tooltip>
</template>

<script lang="ts" setup name="loginAccount">
import { reactive, computed, ref, onMounted, onUnmounted, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, InputInstance } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { initBackEndControlRoutes } from '/@/router/backEnd';
import { Local, Session } from '/@/utils/storage';
import { formatAxis } from '/@/utils/formatTime';
import { NextLoading } from '/@/utils/loading';
import { useThemeConfig } from '/@/stores/themeConfig';
import { storeToRefs } from 'pinia';
import { accessTokenKey, clearTokens, feature, getAPI } from '/@/utils/axios-utils';
import { SysAuthApi } from '/@/api-services/system';
import { useUserInfo } from '/@/stores/userInfo';

import { useRequestOldRoutes,searchRoutes } from '/@/stores/requestOldRoutes';


const storesRequestOldRoutes = useRequestOldRoutes();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const userInfo = useUserInfo();
const {t:$t} = useI18n();

const route = useRoute();
const router = useRouter();

const ruleFormRef = ref();
const accountRef = ref<InputInstance>();
const passwordRef = ref<InputInstance>();
const otpRef = ref<InputInstance>();

const state = reactive({
	isShowPassword: false,
	ruleForm: {
		username: '',
		password: '',
		otp: ''
	},
	rules: {
		username: [{ required: true, message: $t('lz.login.validation.required',{PropertyName: $t('lz.login.label.account')}), trigger: 'blur' }],
		password: [{ required: true, message: $t('lz.login.validation.required',{PropertyName: $t('lz.login.label.password')}), trigger: 'blur' }],
		otp: [{ required: true, message: $t('lz.login.validation.required',{PropertyName: $t('lz.login.label.otp')}), trigger: 'blur' }],
	},
	loading: {
		signIn: false,
	},
	isPassRotate: false,
	capsLockVisible: false,
	showOtpField: false,
	tempToken: '',
	requireOtp: false,
});

onMounted(async () => {
	// If the URL has a Token parameter (third-party login)
	var accessToken = route.query.token;

	if (accessToken != null && accessToken != undefined) {
		await saveTokenAndInitRoutes(accessToken);
	}

	// Detect Caps Lock key
	document.addEventListener('keyup', handleKeyPress);
});

onUnmounted(() => {
	document.removeEventListener('keyup', handleKeyPress);
});

const handleKeyPress = (e: any) => {
	const isCapsLockOn = e.getModifierState('CapsLock');
	state.capsLockVisible = isCapsLockOn;
};

// Get current time
const currentTime = computed(() => {
	return $t(formatAxis(new Date()));
});

// Sign in
const onSignIn = async () => {
	console.log('login')
	// Validate required fields based on current state
	const fieldsToValidate = state.showOtpField ? ['username', 'password'] : ['username', 'password'];
	
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return false;
		try {
			state.loading.signIn = true;
			
			// Prepare login request
			const loginData: any = {
				username: state.ruleForm.username,
				password: state.ruleForm.password
			};
			
			// Add tempToken and otp if OTP is required
			if (state.requireOtp && state.tempToken) {
				loginData.username = "";
				loginData.password = "";
				loginData.tempToken = state.tempToken;
				loginData.otpCode = state.ruleForm.otp;
				console.log(loginData);
			}
			
			const [err, res] = await feature(getAPI(SysAuthApi).apiSystemSysauthLoginPost(loginData));
			
			if (res?.data?.succeeded != true) {
				state.loading.signIn = false;
				if (res?.data?.message) {
					ElMessage.error(res?.data?.message);
				}
				if(err && err.message && err.message.length == 0) {
					ElMessage.error($t('lz.login.exception.errorOccurred'));
				}
				return;
			}
			
			const result = res?.data?.result;
			
			// Check if OTP is required
			if (result?.requireOtp) {
				state.showOtpField = true;
				state.requireOtp = true;
				state.tempToken = result.tempToken || '';
				state.loading.signIn = false;
				
				// Focus on OTP input
				nextTick(() => {
					otpRef.value?.focus();
				});
				
				ElMessage.info(result.message || 'Vui lòng nhập mã OTP');
				return;
			}
			
			// Login successful, save token and initialize routes
			if (result?.token) {
				await saveTokenAndInitRoutes(result.token);
			}
		} finally {
			state.loading.signIn = false;
		}
	});
};

// Save token and initialize routes
const saveTokenAndInitRoutes = async (accessToken: string | any) => {
	// Cache token
	Local.set(accessTokenKey, accessToken);
	// Local.set(refreshAccessTokenKey, refreshAccessToken);
	Session.set('token', accessToken);

	// After adding dynamic routes, navigate to router, otherwise it may report an error No match found for location with path "/"
	const isNoPower = await initBackEndControlRoutes();
	signInSuccess(isNoPower); // Execute signInSuccess again
};

// Redirect after successful login
const signInSuccess = (isNoPower: boolean | undefined) => {
	if (isNoPower) {
		ElMessage.warning($t('lz.login.exception.noPermission'));
		clearTokens(); // Clear token cache
	} else {
		// Initialize greeting message for successful login time
		let currentTimeInfo = currentTime.value;
		//SONTH: Redirect to defaultMenu if it exists
		// Lấy defaultMenu từ userInfo
		var defaultMenu = userInfo.userInfos.defaultMenu;
		// Kiểm tra defaultMenu có tồn tại không
		if (defaultMenu) {
			// Nếu có defaultMenu thì chuyển hướng đến defaultMenu
			router.push(defaultMenu);
		} else
		// After successful login, redirect to home page. If the path is copied and pasted, and is not the home/login page, redirect to the corresponding path after successful login
		if (route.query?.redirect) {
			router.push({
				path: <string>route.query?.redirect,
				query: Object.keys(<string>route.query?.params).length > 0 ? JSON.parse(<string>route.query?.params) : '',
			});
		} else {
			router.push('/');
		}

		// Successful login message
		const signInText = $t('lz.login.label.signInText');
		ElMessage.success(`${currentTimeInfo} ${state.ruleForm.username}，${signInText}`);
		// Add loading to prevent a brief blank screen when first entering the interface
		NextLoading.start();
	}
};

// Open rotation verification
// const openRotateVerify = () => {
// 	state.isPassRotate = false;
// };

// // Pass rotation verification
// const passRotateVerify = () => {
// 	state.isPassRotate = true;
// 	onSignIn();
// };

// Handle login
const handleSignIn = () => {
	if (!state.ruleForm.username) {
		accountRef.value?.focus();
	} else if (!state.ruleForm.password) {
		passwordRef.value?.focus();
	// } else if (state.captchaEnabled && !state.ruleForm.code) {
	// 	codeRef.value?.focus();
	} else {
		onSignIn();
	}
};

// // WeChat login
// const weixinSignIn = () => {
// 	window.open('http://localhost:5005/api/sysoauth/signin?provider=Gitee&redirectUrl=http://localhost:8888');
// };

// Export object
defineExpose({ saveTokenAndInitRoutes });
</script>

<style lang="scss" scoped>
.dialog-header {
	:deep(.el-dialog) {
		width: unset !important;

		.el-dialog__header {
			display: none;
		}

		.el-dialog__wrapper {
			position: absolute !important;
		}

		.v-modal {
			position: absolute !important;
		}
	}
}

.login-content-form {
	margin-top: 20px;

	@for $i from 0 through 4 {
		.login-animation#{$i} {
			opacity: 0;
			animation-name: error-num;
			animation-duration: 0.5s;
			animation-fill-mode: forwards;
			animation-delay: calc($i/10) + s;
		}
	}

	.login-content-password {
		display: inline-block;
		width: 20px;
		cursor: pointer;

		&:hover {
			color: #909399;
		}
	}

	.login-content-code {
		display: flex;
		align-items: center;
		justify-content: space-around;

		.login-content-code-img {
			width: 100%;
			height: 40px;
			line-height: 40px;
			background-color: #ffffff;
			border: 1px solid rgb(220, 223, 230);
			cursor: pointer;
			transition: all ease 0.2s;
			border-radius: 4px;
			user-select: none;

			&:hover {
				border-color: #c0c4cc;
				transition: all ease 0.2s;
			}
		}
	}

	.login-content-submit {
		width: 100%;
		letter-spacing: 2px;
		margin-top: 15px;
	}

	.login-msg {
		color: var(--el-text-color-placeholder);
	}
}
.el-form-item{
	margin: 0
}
</style>
