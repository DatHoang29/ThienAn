<template>
	<!-- MẪU 2 - Ưu tiên 2: dùng khung login.svg phủ toàn màn, ảnh lộ qua vùng trong suốt bên trái -->
	<div class="login-v2">
		<!-- Lớp 0: carousel ảnh nền (nằm DƯỚI, chỉ lộ qua vùng trong suốt của login.svg) -->
		<el-carousel class="photo-carousel" height="100vh" :interval="5000" arrow="never" indicator-position="none" :pause-on-hover="false">
			<el-carousel-item v-for="(img, i) in bgImages" :key="i">
				<img :src="img" class="photo-slide" />
			</el-carousel-item>
		</el-carousel>

		<!-- Lớp 1: chevron login4.svg phủ mờ lên ảnh (đã tách riêng, bỏ panel trắng) -->
		<img src="/@/assets/login/login4.svg" class="frame-svg" alt="" />

		<!-- Lớp 2: logo góc trái trên -->
		<div class="brand-logo">
			<img src="/@/assets/logo.png" alt="Thien An Logo" />
		</div>

		<!-- Lớp 2: form nằm trên panel trắng bên phải -->	
		<div class="right-panel">
			<div class="form-wrap">
				<div class="corp-name">{{ getThemeConfig.globalViceTitle }}</div>
				<div class="corp-desc">{{ getThemeConfig.globalViceTitleMsg }}</div>

				<h1 class="system-title">{{ $t('lz.login.label.systemName') }}</h1>

				<el-tabs v-model="state.tabsActiveName" class="login-tabs">
					<el-tab-pane :label="$t('lz.login.label.signInByAccount')" name="account">
						<Account />
					</el-tab-pane>
				</el-tabs>

				<div class="forgot-row">
					<a class="forgot-link" @click="handleForgot">{{ $t('lz.login.label.forgotPassword') }}</a>
				</div>
			</div>
			<div class="copyright">{{ getThemeConfig.copyright }}</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="loginV2">
import { reactive, onMounted, computed, defineAsyncComponent } from 'vue';
import { ElMessage } from 'element-plus';
import { storeToRefs } from 'pinia';
import { NextLoading } from '/@/utils/loading';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useI18n } from 'vue-i18n';

const Account = defineAsyncComponent(() => import('/@/views/login/component/account.vue'));

// Ảnh cho carousel (lộ qua vùng trong suốt bên trái của login.svg) - thay ảnh chất lượng cao tuỳ ý
import bgImg1 from '/@/assets/login-icon-two.png';
import bgImg2 from '/@/assets/login-icon-two1.png';
import bgImg3 from '/@/assets/login-icon-two2.png';
const bgImages = [bgImg1, bgImg2, bgImg3];

const { t: $t } = useI18n();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

const state = reactive({
	tabsActiveName: 'account',
});

const getThemeConfig = computed(() => themeConfig.value);

const handleForgot = () => {
	ElMessage.info('Chức năng đang phát triển');
};

onMounted(() => {
	NextLoading.done();
});
</script>

<style scoped lang="scss">
/* Font tối ưu tiếng Việt (tải từ Google Fonts). Có thể thay bằng self-host để dùng offline. */
@import url('https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap');

/* ===== Bảng màu Ưu tiên 2 ===== */
.login-v2 {
	/* Emerald Tech sâu hơn cho bối cảnh giao thông bền vững */
	--el-color-primary: #059669;
	/* Viền input mỏng, nhạt (Slate 200) */
	--el-border-color: #e2e8f0;

	/* ===== TINH CHỈNH ĐỘ SÁNG ẢNH NỀN =====
	   TĂNG (>1, vd 1.2) => ảnh SÁNG hơn, GIẢM (<1, vd 0.8) => tối hơn. */
	--photo-brightness: 1.05;
	/* Độ mờ của lớp chevron phủ trên ảnh (0 = trong suốt, 1 = đặc) */
	--chevron-opacity: 0.65;

	position: relative;
	width: 100vw;
	height: 100vh;
	overflow: hidden;
	/* Nền trắng dự phòng cho panel phải (login.svg đã có panel trắng đục) */
	background-color: #ffffff;
	font-family: 'Be Vietnam Pro', 'Inter', 'Helvetica Neue', Arial, sans-serif;
}

/* Lớp 0: carousel ảnh - nằm dưới khung svg, chiếm phần bên trái */
.photo-carousel {
	position: absolute;
	top: 0;
	left: 0;
	width: 70vw;
	height: 100vh;
	z-index: 0;
	:deep(.el-carousel__container) {
		height: 100% !important;
	}
	.photo-slide {
		width: 100%;
		height: 100%;
		object-fit: cover;
		filter: brightness(var(--photo-brightness));
	}
}

/* Lớp 1: chevron login4.svg phủ mờ lên ảnh */
.frame-svg {
	position: absolute;
	inset: 0;
	z-index: 1;
	width: 100vw;
	height: 100vh;
	object-fit: cover;
	opacity: var(--chevron-opacity);
	pointer-events: none;
}

/* Lớp 2: logo */
.brand-logo {
	position: absolute;
	top: 36px;
	left: 44px;
	z-index: 2;
	img {
		height: 90px;
	}
}

/* Lớp 2: panel phải chứa form - nền đặc Slate 50 (login4.svg không còn panel trắng) */
.right-panel {
	position: absolute;
	top: 0;
	right: 0;
	z-index: 2;
	width: 42%;
	height: 100vh;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	background-color: #f8fafc;
	box-shadow: -8px 0 30px rgba(15, 23, 42, 0.12);
}

.form-wrap {
	width: 380px;
	max-width: 78%;

	.corp-name {
		font-size: 26px;
		font-weight: 700;
		color: #0f172a;
		line-height: 1.2;
		text-align: center;
	}
	.corp-desc {
		margin-top: 6px;
		font-size: 14px;
		font-weight: 400;
		color: #475569;
		text-align: center;
	}
	.system-title {
		margin: 32px 0 8px;
		font-size: 19px;
		font-weight: 600;
		text-align: center;
		color: #1e293b;
	}
}

.login-tabs {
	:deep(.el-tabs__item) {
		font-weight: 500;
		color: #475569;
	}
	:deep(.el-tabs__item.is-active) {
		color: var(--el-color-primary);
	}
}

/* ===== Override style form dùng chung trong Account.vue ===== */
/* Bo góc sắc gọn 8px cho ô nhập liệu */
:deep(.input-group .el-input__wrapper) {
	border-radius: 8px;
	padding: 4px 12px;
}
/* Nút đăng nhập: tông Emerald Tech sâu (gradient), bo góc mềm,
   bóng mờ rất nhẹ tạo chiều sâu 3D + sheen kính (glassmorphism) */
:deep(.login-content-submit.el-button) {
	border: none;
	border-radius: 26px;
	height: 48px;
	font-weight: 600;
	color: #fff;
	background: linear-gradient(135deg, #10b981 0%, #059669 52%, #047857 100%);
	box-shadow: 0 10px 24px rgba(5, 150, 105, 0.28), 0 2px 6px rgba(15, 23, 42, 0.08), inset 0 1px 0 rgba(255, 255, 255, 0.28);
	transition: all 0.28s ease;
}
:deep(.login-content-submit.el-button:hover) {
	transform: translateY(-1px);
	background: linear-gradient(135deg, #14c88a 0%, #059669 52%, #036c4a 100%);
	box-shadow: 0 14px 30px rgba(5, 150, 105, 0.34), 0 3px 8px rgba(15, 23, 42, 0.1), inset 0 1px 0 rgba(255, 255, 255, 0.32);
}
:deep(.login-content-submit.el-button:active) {
	transform: translateY(0);
	box-shadow: 0 6px 16px rgba(5, 150, 105, 0.26), inset 0 1px 0 rgba(255, 255, 255, 0.22);
}

.forgot-row {
	text-align: center;
	margin-top: 5px;
	.forgot-link {
		font-size: 13px;
		font-weight: 500;
		color: var(--el-color-primary);
		cursor: pointer;
		&:hover {
			text-decoration: underline;
		}
	}
}

.copyright {
	position: absolute;
	bottom: 28px;
	left: 0;
	width: 100%;
	text-align: center;
	font-size: 12px;
	color: #94a3b8;
}

/* Màn nhỏ: ẩn ảnh, form full để không vỡ layout */
@media screen and (max-width: 900px) {
	.photo-carousel,
	.frame-svg {
		display: none;
	}
	.right-panel {
		width: 100%;
	}
	.login-v2 {
		background-color: #f8fafc;
	}
}
</style>
