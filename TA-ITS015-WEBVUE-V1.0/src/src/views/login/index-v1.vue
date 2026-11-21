<template>
  <div class="login-container">
    <img src="/@/assets/login/login.svg" alt="Background" class="fullscreen-background" />
    
    <!-- Top left logo -->
    <div class="top-left-logo">
      <img src="/@/assets/logo.png" alt="Thien An Logo" class="logo" />
    </div>
    <el-carousel height="500px" class="login-carousel">
				<el-carousel-item>
					<img :src="loginIconTwo" class="login-icon-group-icon" />
				</el-carousel-item>
				<el-carousel-item>
					<img :src="loginIconTwo1" class="login-icon-group-icon" />
				</el-carousel-item>
				<el-carousel-item>
					<img :src="loginIconTwo2" class="login-icon-group-icon" />
				</el-carousel-item>
			</el-carousel>
    <!-- Top right info -->
    <div class="top-right-info">
      <div class="corporation-name">{{ getThemeConfig.globalViceTitle }}</div>
      <div class="preview-text">{{ getThemeConfig.globalViceTitleMsg }}</div>
    </div>
    
    <!-- Login form container -->
    <div class="login-form-container">
      <!-- <h1 class="system-title">HỆ THỐNG GIÁM SÁT GIAO THÔNG</h1> -->
      <!-- <SigmaChart /> -->
      <div class="login-right">
			<div class="login-right-warp flex-margin">
				<span class="login-right-warp-one"></span>
				<span class="login-right-warp-two"></span>
				<div class="login-right-warp-main">
					<div class="login-right-warp-main-title">{{ getThemeConfig.globalTitle?.toUpperCase() }}</div>
					<div class="login-right-warp-main-form">
						<div v-if="!state.isScan">
							<el-tabs v-model="state.tabsActiveName">
								<el-tab-pane :label="$t('lz.login.label.signInByAccount')" name="account">
									<Account />
								</el-tab-pane>
							</el-tabs>
						</div>
					</div>
				</div>
			</div>
		</div>
      
      <!-- <div class="remember-me">
        <span>Remember Me</span>
        <el-switch v-model="loginForm.rememberMe" class="green-switch" />
      </div> -->
      
      <!-- <el-button type="primary" class="login-button" @click="handleLogin">SIGN IN</el-button> -->
    </div>
    
	<div class="copyright" :class="[getThemeConfig.icp ? 'mb25' : 'mt5']">{{ getThemeConfig.copyright }}</div>

  </div>
</template>

<script setup lang="ts" name="loginIndex">
import { reactive, onMounted, onBeforeUnmount, defineAsyncComponent, computed } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { NextLoading } from '/@/utils/loading';
import loginIconTwo from '/@/assets/login-icon-two.png';
import loginIconTwo1 from '/@/assets/login-icon-two1.png';
import loginIconTwo2 from '/@/assets/login-icon-two2.png';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
// import SigmaChart from '/@/components/graph/sigmaChart.vue';

// Account Page
const Account = defineAsyncComponent(() => import('/@/views/login/component/account.vue'));

const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const state = reactive({
	tabsActiveName: 'account',
	isScan: false,
});
// Load theme config
const getThemeConfig = computed(() => {
	return themeConfig.value;
});
// Mounted
onMounted(() => {
	NextLoading.done();
});
// Account Page

const router = useRouter();

const loginForm = reactive({
  username: '',
  password: '',
  rememberMe: false
});

const handleLogin = () => {
  if (!loginForm.username || !loginForm.password) {
    ElMessage.error('Please enter username and password');
    return;
  }
  
  // Here you would typically call your authentication API
  // For demo purposes, we'll just redirect to the dashboard
  router.push('/');
};

// Prevent zoom
const preventZoom = (e) => {
  if (e.ctrlKey) {
    if (e.wheelDelta > 0 || e.detail < 0) {
      e.preventDefault();
    } else if (e.wheelDelta < 0 || e.detail > 0) {
      e.preventDefault();
    }
  }
};

// Add meta tag to prevent zoom
const addViewportMeta = () => {
  let meta = document.createElement('meta');
  meta.name = 'viewport';
  meta.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no';
  meta.id = 'viewport-meta';
  document.head.appendChild(meta);
};

// Event listeners for wheel and touchmove events
const addEventListeners = () => {
  // Modern browsers
  window.addEventListener('wheel', preventZoom, { passive: false });
  
  // Older browsers
  window.addEventListener('mousewheel', preventZoom, { passive: false });
  window.addEventListener('DOMMouseScroll', preventZoom, { passive: false });
  
  // Touch events
  document.addEventListener('touchmove', (e) => {
    if (e.touches.length > 1) {
      e.preventDefault();
    }
  }, { passive: false });
  
  // Keyboard events
  document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && (e.key === '+' || e.key === '-' || e.key === '0')) {
      e.preventDefault();
    }
  });
};

// Remove event listeners when component is unmounted
const removeEventListeners = () => {
  window.removeEventListener('wheel', preventZoom);
  window.removeEventListener('mousewheel', preventZoom);
  window.removeEventListener('DOMMouseScroll', preventZoom);
};

onMounted(() => {
  NextLoading.done();
  addViewportMeta();
  addEventListeners();
});

onBeforeUnmount(() => {
  removeEventListeners();
  // Remove meta tag when component is unmounted
  const meta = document.getElementById('viewport-meta');
  if (meta) {
    document.head.removeChild(meta);
  }
});
</script>

<style scoped lang="scss">
.login-container {
  position: relative;
  width: 100vw;
  height: 100vh;
  margin: 0;
  padding: 0;
  overflow: hidden;
  background-color: white;
}

.fullscreen-background {
    position: fixed;
    object-fit: cover;
    width: 100vw;
    height: 100vh;
    z-index: 10;
    /* transform-origin: center center; */
}
/* Top left logo styles */
.top-left-logo {
  position: absolute;
  top: 40px;
  left: 40px;
  display: flex;
  align-items: center;
  z-index: 10;
  
  .logo {
    height: 150px;
  }
  
  .logo-text {
    margin-left: 10px;
    
    .company-name {
      font-size: 28px;
      font-weight: 600;
      color: #e63946;
      line-height: 1;
    }
    
    .technology-text {
      font-size: 14px;
      color: #333;
      letter-spacing: 3px;
    }
  }
}

/* Top right info styles */
.top-right-info {
	position: absolute;
	top: 40px;
	right: 10%;
	text-align: center;
	z-index: 10;
	width: 25%;
	font-size: 50px;
  
  .corporation-name {
    font-size: 30px;
    font-weight: 600;
    color: var(--el-color-primary);
    margin-bottom: 5px;
  }
  
  .preview-text {
    font-size: 20px;
    color: var(--el-color-primary);
  }
}

/* Login form container styles */
.login-form-container {
  position: absolute;
  top: 50%;
  right: 10%;
  transform: translateY(-50%);
  width: 25%;
  z-index: 10;
  
  .system-title {
    color: #00703c;
    font-size: 24px;
    font-weight: 600;
    margin-bottom: 40px;
  }
  
  .login-label {
    color: #00703c;
    font-size: 16px;
    margin-bottom: 20px;
    border-bottom: 1px solid #00703c;
    padding-bottom: 5px;
  }
  
  .input-group {
    margin-bottom: 20px;
    
    :deep(.el-input__wrapper) {
      border-radius: 25px;
      box-shadow: 0 0 0 1px #dcdfe6 inset;
      border: 1px solid #00703c;
    }
    
    :deep(.el-input__prefix) {
      color: #00703c;
    }
  }
  
  .remember-me {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    color: #606266;
    
    .green-switch {
      :deep(.el-switch__core) {
        background-color: #dcdfe6;
        border-color: #dcdfe6;
      }
      
      :deep(.el-switch__core .el-switch__action) {
        background-color: white;
      }
      
      :deep(.is-checked .el-switch__core) {
        background-color: #00703c !important;
        border-color: #00703c !important;
      }
    }
  }
  
  .login-button {
    width: 100%;
    height: 35px;
    border-radius: 22px;
    font-weight: 600;
    background-color: #00703c;
    border: none;
    
    &:hover {
      background-color: #005a30;
    }
  }
}

.copyright {
	width: 25%;
    position: absolute;
    bottom: 10%;
    right: 10%;
    text-align: center;
    font-size: 12px;
    color: #666;
	z-index: 10;
}

/* Make sure HTML and body take up full space */
:deep(html), :deep(body) {
  width: 100%;
  height: 100%;
  margin: 0;
  padding: 0;
  overflow: hidden;
}

/* Responsive adjustments */
@media screen and (max-width: 768px) {
  .login-form-container {
    width: 90%;
    right: 5%;
  }
  
  .top-left-logo, .top-right-info {
    position: relative;
    margin: 20px;
  }
}

.login-carousel {
  position: absolute;
  top: 0;
  left: 0;
  width: 70vw;
  height: 100vh;
  z-index: 0;
}

.login-icon-group-icon {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* Adjust element-plus carousel styling */
:deep(.el-carousel__container) {
  height: 100% !important;
}

:deep(.el-carousel__item) {
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
}

/* Additional styles to prevent scaling */
html, body {
  touch-action: pan-x pan-y; /* Allow scrolling but not zooming */
  -ms-touch-action: pan-x pan-y;
  overflow: hidden;
  position: fixed;
  width: 100%;
  height: 100%;
}

.login-right {
		// width: 50%;
		// float: right;
		background: var(--el-color-white);
		background-image: url('../../assets/bg.svg');
		background-size: 100% auto;
		background-position: 50% calc(50% - 15px);
		background-attachment: fixed;
		background-repeat: no-repeat;
		.login-right-warp {
			// border: 1px solid var(--el-color-primary-light-3);
			// border-radius: 3px;
			// height: 550px;
			position: relative;
			overflow: hidden;
			background-color: var(--el-color-white);
			.login-right-warp-one,
			.login-right-warp-two {
				position: absolute;
				display: block;
				width: inherit;
				height: inherit;
				&::before,
				&::after {
					content: '';
					position: absolute;
					z-index: 1;
				}
			}
			.login-right-warp-one {
				&::before {
					filter: hue-rotate(0deg);
					top: 0px;
					left: 0;
					width: 100%;
					height: 1px;
					background: linear-gradient(90deg, transparent, var(--el-color-primary));
					animation: loginLeft 3s linear infinite;
				}
				&::after {
					filter: hue-rotate(0deg);
					top: -100%;
					right: 2px;
					width: 1px;
					height: 100%;
					background: linear-gradient(180deg, transparent, var(--el-color-primary));
					animation: loginTop 3s linear infinite;
					animation-delay: 0.7s;
				}
			}
			.login-right-warp-two {
				&::before {
					filter: hue-rotate(0deg);
					bottom: 2px;
					right: -100%;
					width: 100%;
					height: 1px;
					background: linear-gradient(270deg, transparent, var(--el-color-primary));
					animation: loginRight 3s linear infinite;
					animation-delay: 1.4s;
				}
				&::after {
					filter: hue-rotate(0deg);
					bottom: -100%;
					left: 0px;
					width: 1px;
					height: 100%;
					background: linear-gradient(360deg, transparent, var(--el-color-primary));
					animation: loginBottom 3s linear infinite;
					animation-delay: 2.1s;
				}
			}
			.login-right-warp-main {
				display: flex;
				flex-direction: column;
				height: 100%;
				.login-right-warp-main-title {
					// height: 130px;
					// line-height: 130px;
					margin-top: 25px;
					margin-bottom: 40px;
					font-size: 20px;
					font-weight: bold;
					text-align: center;
					font-family: 'Arial', sans-serif;
					//letter-spacing: 3px;
					animation: logoAnimation 0.3s ease;
					animation-delay: 0.3s;
					color: var(--el-color-primary);
				}
				.login-right-warp-main-form {
					flex: 1;
					padding: 0 50px 50px;
					.login-content-main-scan {
						position: absolute;
						top: 0;
						right: 0;
						width: 50px;
						height: 50px;
						overflow: hidden;
						cursor: pointer;
						transition: all ease 0.3s;
						color: var(--el-color-primary);
						&-delta {
							position: absolute;
							width: 35px;
							height: 70px;
							z-index: 2;
							top: 2px;
							right: 21px;
							background: var(--el-color-white);
							transform: rotate(-45deg);
						}
						&:hover {
							opacity: 1;
							transition: all ease 0.3s;
							color: var(--el-color-primary) !important;
						}
						i {
							width: 47px;
							height: 50px;
							display: inline-block;
							font-size: 48px;
							position: absolute;
							right: 1px;
							top: 0px;
						}
					}
				}
			}
		}
		/* You can add a pseudo-element here to overlay the original content for a frosted effect */
		.login-right-warp::before {
			content: '';
			position: absolute;
			top: 0;
			left: 0;
			width: 100%;
			height: 100%;
			background-color: rgba(255, 255, 255, 1);
			filter: blur(4px); /* Adjust the blur radius to change the intensity of the frosted effect */
			z-index: 1;
		}

		/* Keep the original content visible, placed below the pseudo-element */
		.login-right-warp > * {
			position: relative;
			z-index: 2;
		}
	}
</style>
