<template>
	<div class="login-scene">
	<!-- MẪU 1 - Ưu tiên 1: Emerald Glassmorphism -->
	<div
		class="login-v1"
		:class="{ 'login-v1--revealed': isLoginRevealed }"
		:aria-hidden="showIntro"
		:inert="showIntro"
	>
		<!-- Carousel ảnh nền tràn màn hình (tự đổi ảnh liên tục) -->
		<el-carousel class="bg-carousel" height="100vh" :interval="5000" arrow="never" indicator-position="none" :pause-on-hover="false">
			<el-carousel-item v-for="(img, i) in bgImages" :key="i">
				<img :src="img" class="bg-slide" />
			</el-carousel-item>
		</el-carousel>
		<!-- Lớp phủ gradient navy -> emerald (điều chỉnh độ sáng qua biến --bg-overlay-opacity) -->
		<div class="bg-overlay"></div>

		<!-- Logo góc trái trên - lấy động theo cấu hình hệ thống (giống Aside), fallback logo tĩnh -->
		<div class="brand-logo">
			<img :src="getThemeConfig.logoUrl || defaultLogo" alt="Thien An Logo" />
		</div>

		<!-- Card kính mờ chứa form -->
		<div class="glass-card">
			<!-- Viền sáng chạy quanh card (thuần trang trí) -->
			<span class="glass-card__glow" aria-hidden="true"></span>
			<div class="corp-name">{{ getThemeConfig.globalViceTitle }}</div>
			<div class="corp-desc">{{ getThemeConfig.globalViceTitleMsg }}</div>

			<h1 class="system-title">{{ $t('lz.login.label.systemName') }}</h1>

			<el-tabs v-model="state.tabsActiveName" class="login-tabs">
				<el-tab-pane :label="$t('lz.login.label.signInByAccount')" name="account">
					<Account />
				</el-tab-pane>
			</el-tabs>

			<!-- <div class="forgot-row">
				<a class="forgot-link" @click="handleForgot">{{ $t('lz.login.label.forgotPassword') }}</a>
			</div> -->
		</div>

		<div v-if="copyrightText" class="copyright">{{ copyrightText }}</div>
	</div>

		<Transition name="login-intro">
			<div
				v-if="showIntro"
				class="login-intro"
				role="status"
				aria-label="Đang phát video giới thiệu"
			>
				<video
					ref="introVideoRef"
					class="login-intro__video"
					:class="{ 'login-intro__video--ready': isVideoReady }"
					src="/video.mp4"
					autoplay
					muted
					playsinline
					preload="auto"
					@loadeddata="onVideoReady"
					@playing="onVideoPlaying"
					@waiting="scheduleVideoFallback"
					@stalled="scheduleVideoFallback"
					@ended="finishIntro"
					@error="finishIntro"
				></video>

				<button
					type="button"
					class="login-intro__skip"
					aria-label="Bỏ qua video giới thiệu"
					title="Bỏ qua video"
					@click="finishIntro"
				>
					<svg viewBox="0 0 24 24" aria-hidden="true">
						<path d="M6 6l12 12M18 6 6 18" />
					</svg>
				</button>

				<div v-if="!isVideoReady" class="login-intro__loader" aria-hidden="true">
					<span></span>
				</div>
			</div>
		</Transition>
	</div>
</template>

<script setup lang="ts" name="loginV1">
import { reactive, ref, onMounted, onBeforeUnmount, computed, defineAsyncComponent } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import { storeToRefs } from 'pinia';
import { NextLoading } from '/@/utils/loading';
import { useThemeConfig } from '/@/stores/themeConfig';
import { Session } from '/@/utils/storage';
import { useI18n } from 'vue-i18n';

const Account = defineAsyncComponent(() => import('/@/views/login/component/account.vue'));

// Ảnh cho carousel nền - thay/thêm ảnh chất lượng cao tuỳ ý (vd: login.svg, login2.svg...)
import bgImg1 from '/@/assets/login-icon-two.png';
import bgImg2 from '/@/assets/login-icon-two1.png';
import bgImg3 from '/@/assets/login-icon-two2.png';
// Logo mặc định (fallback) khi cấu hình hệ thống chưa trả về logoUrl
import defaultLogo from '/@/assets/logo.png';
const bgImages = [bgImg1, bgImg2, bgImg3];

const { t: $t, te } = useI18n();
const route = useRoute();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

const VIDEO_FALLBACK_TIMEOUT_MS = 8_000;
const hasActiveSession = Boolean(Session.get('token'));
const hasExternalLoginToken = Boolean(route.query.token);
const shouldPlayIntro = !hasActiveSession && !hasExternalLoginToken;

const introVideoRef = ref<HTMLVideoElement | null>(null);
const showIntro = ref(shouldPlayIntro);
const isLoginRevealed = ref(!shouldPlayIntro);
const isVideoReady = ref(false);
let videoFallbackTimer: number | undefined;

const state = reactive({
	tabsActiveName: 'account',
});

const getThemeConfig = computed(() => themeConfig.value);

// Bản quyền: lấy từ cấu hình hệ thống (sysCopyright, có thể là key i18n giống footer layout);
// cấu hình trống ⇒ tự dựng "Copyright © <năm hiện tại> <tên đơn vị>".
const copyrightText = computed(() => {
	const raw = (getThemeConfig.value.copyright ?? '').trim();
	if (raw) return te(raw) ? $t(raw) : raw;
	const owner = (getThemeConfig.value.globalViceTitle ?? '').trim();
	return owner ? `Copyright © ${new Date().getFullYear()} ${owner}` : '';
});

const handleForgot = () => {
	ElMessage.info('Chức năng đang phát triển');
};

const clearVideoFallback = () => {
	if (videoFallbackTimer === undefined) return;
	window.clearTimeout(videoFallbackTimer);
	videoFallbackTimer = undefined;
};

const finishIntro = () => {
	if (!showIntro.value) return;

	clearVideoFallback();
	document.removeEventListener('keydown', handleIntroKeydown);
	isLoginRevealed.value = true;
	showIntro.value = false;
};

const handleIntroKeydown = (event: KeyboardEvent) => {
	if (!showIntro.value || (event.key !== 'Escape' && event.code !== 'Space')) return;

	event.preventDefault();
	finishIntro();
};

const scheduleVideoFallback = () => {
	if (!showIntro.value) return;

	clearVideoFallback();
	videoFallbackTimer = window.setTimeout(finishIntro, VIDEO_FALLBACK_TIMEOUT_MS);
};

const onVideoReady = async () => {
	isVideoReady.value = true;

	try {
		await introVideoRef.value?.play();
	} catch {
		// Autoplay bị chặn: không giữ người dùng ở màn hình intro.
		finishIntro();
	}
};

const onVideoPlaying = () => {
	isVideoReady.value = true;
	clearVideoFallback();
};

onMounted(() => {
	NextLoading.done();
	if (showIntro.value) {
		document.addEventListener('keydown', handleIntroKeydown);
		scheduleVideoFallback();
	}
});

onBeforeUnmount(() => {
	clearVideoFallback();
	document.removeEventListener('keydown', handleIntroKeydown);
	introVideoRef.value?.pause();
});
</script>

<style scoped lang="scss">
.login-scene {
	position: relative;
	width: 100vw;
	height: 100vh;
	overflow: hidden;
	background: #020617;
}

.login-intro {
	position: fixed;
	inset: 0;
	z-index: 1000;
	display: grid;
	place-items: center;
	overflow: hidden;
	background: #020617;
	will-change: opacity, transform, filter;

	&__video {
		position: absolute;
		inset: 0;
		width: 100%;
		height: 100%;
		object-fit: cover;
		opacity: 0;
		transform: scale(1.012);
		transition: opacity 420ms ease;

		&--ready {
			opacity: 1;
		}
	}

	&__skip {
		position: absolute;
		top: max(20px, env(safe-area-inset-top));
		right: max(20px, env(safe-area-inset-right));
		z-index: 2;
		display: grid;
		place-items: center;
		width: 44px;
		height: 44px;
		padding: 0;
		color: #fff;
		cursor: pointer;
		border: 1px solid rgba(255, 255, 255, 0.28);
		border-radius: 50%;
		background: rgba(2, 6, 23, 0.36);
		box-shadow: 0 8px 24px rgba(2, 6, 23, 0.24);
		backdrop-filter: blur(10px);
		-webkit-backdrop-filter: blur(10px);
		transition:
			transform 180ms ease,
			background-color 180ms ease,
			border-color 180ms ease,
			box-shadow 180ms ease;

		svg {
			width: 20px;
			height: 20px;
			fill: none;
			stroke: currentColor;
			stroke-width: 1.8;
			stroke-linecap: round;
		}

		&:hover {
			transform: scale(1.06);
			border-color: rgba(255, 255, 255, 0.5);
			background: rgba(2, 6, 23, 0.58);
			box-shadow: 0 10px 28px rgba(2, 6, 23, 0.32);
		}

		&:active {
			transform: scale(0.96);
		}

		&:focus-visible {
			outline: 2px solid #fff;
			outline-offset: 3px;
		}
	}

	&__loader {
		position: relative;
		z-index: 1;
		width: 42px;
		height: 42px;
		border-radius: 50%;
		border: 2px solid rgba(255, 255, 255, 0.14);
		border-top-color: rgba(255, 255, 255, 0.82);
		animation: login-intro-spin 800ms linear infinite;
	}
}

.login-intro-leave-active {
	transition:
		opacity 900ms cubic-bezier(0.22, 1, 0.36, 1),
		transform 1100ms cubic-bezier(0.22, 1, 0.36, 1),
		filter 900ms ease;
}

.login-intro-leave-to {
	opacity: 0;
	transform: scale(1.035);
	filter: blur(10px);
}

@keyframes login-intro-spin {
	to {
		transform: rotate(360deg);
	}
}
/* ===== Bảng màu: Navy Glass + Cobalt (đồng bộ tông xanh của hệ thống) ===== */
.login-v1 {
	/* Màu nhấn chính: cobalt → sky */
	--el-color-primary: #3b82f6;
	--lg-accent: #60a5fa;
	--lg-accent-strong: #2563eb;
	--lg-text: #f1f5f9;
	--lg-text-muted: #94a3b8;
	--lg-line: rgba(255, 255, 255, 0.14);

	/* ===== TINH CHỈNH ĐỘ SÁNG NỀN =====
	   --bg-overlay-opacity: độ đậm lớp phủ tối. GIẢM (vd 0.35) => nền SÁNG hơn, TĂNG (vd 0.85) => tối hơn.
	   --bg-image-brightness: độ sáng ảnh. TĂNG (>1, vd 1.2) => ảnh sáng hơn, GIẢM (<1) => tối hơn. */
	--bg-overlay-opacity: 0.2;
	--bg-image-brightness: 1.3;

	position: relative;
	width: 100vw;
	height: 100vh;
	overflow: hidden;
	font-family: 'Inter', 'Helvetica Neue', Arial, sans-serif;
	opacity: 0;
	transform: scale(1.018);
	filter: blur(5px);
	transition:
		opacity 900ms cubic-bezier(0.22, 1, 0.36, 1),
		transform 1100ms cubic-bezier(0.22, 1, 0.36, 1),
		filter 900ms ease;

	&--revealed {
		opacity: 1;
		transform: scale(1);
		filter: blur(0);
	}
}

/* Carousel ảnh nền cao tốc */
.bg-carousel {
	position: absolute;
	inset: 0;
	z-index: 0;
	:deep(.el-carousel__container) {
		height: 100% !important;
	}
	.bg-slide {
		width: 100%;
		height: 100%;
		object-fit: cover;
		/* Điều chỉnh độ sáng ảnh qua biến ở trên */
		filter: brightness(var(--bg-image-brightness));
	}
}

/* Lớp phủ gradient: navy -> emerald. Độ đậm điều khiển bằng opacity để dễ chỉnh sáng/tối */
.bg-overlay {
	position: absolute;
	inset: 0;
	z-index: 1;
	pointer-events: none;
	opacity: var(--bg-overlay-opacity);
	background: linear-gradient(120deg, #0f172a 0%, #0f172a 45%, #00ad5b 100%);
}

/* Logo góc trái: đặt trên "thẻ kính sáng" để logo nhiều màu (đỏ/xanh/chữ đen) luôn rõ trên mọi ảnh nền */
.brand-logo {
	position: absolute;
	top: 28px;
	left: 32px;
	z-index: 3;
	display: inline-flex;
	align-items: center;
	padding: 10px 18px;
	border-radius: 14px;
	background: rgba(255, 255, 255, 0.92);
	backdrop-filter: blur(12px);
	-webkit-backdrop-filter: blur(12px);
	border: 1px solid rgba(255, 255, 255, 0.9);
	box-shadow:
		0 10px 30px -10px rgba(2, 6, 23, 0.45),
		inset 0 1px 0 rgba(255, 255, 255, 1);
	transition: transform 0.3s ease, box-shadow 0.3s ease;

	img {
		display: block;
		height: 72px;
		width: auto;
		object-fit: contain;
	}

	&:hover {
		transform: translateY(-2px);
		box-shadow:
			0 14px 34px -10px rgba(2, 6, 23, 0.5),
			inset 0 1px 0 rgba(255, 255, 255, 1);
	}
}

/* Card kính tối, căn giữa màn hình */
.glass-card {
	position: absolute;
	top: 50%;
	left: 50%;
	transform: translate(-50%, -50%);
	z-index: 3;
	width: 420px;
	max-width: 90%;
	padding: 40px 40px 32px;
	border-radius: 18px;
	color: var(--lg-text);
	background: linear-gradient(160deg, rgba(15, 23, 42, 0.78) 0%, rgba(15, 23, 42, 0.62) 100%);
	backdrop-filter: blur(18px) saturate(140%);
	-webkit-backdrop-filter: blur(18px) saturate(140%);
	border: 1px solid var(--lg-line);
	box-shadow:
		0 30px 60px -12px rgba(2, 6, 23, 0.6),
		inset 0 1px 0 rgba(255, 255, 255, 0.08);

	transition: box-shadow 0.4s ease;

	/* Ánh sáng xanh mờ ở góc trên trái bên trong card: tạo chiều sâu */
	&::before {
		content: '';
		position: absolute;
		inset: 0;
		border-radius: inherit;
		background:
			radial-gradient(120% 55% at 0% 0%, rgba(59, 130, 246, 0.22) 0%, transparent 60%),
			radial-gradient(90% 45% at 100% 100%, rgba(14, 165, 233, 0.12) 0%, transparent 65%);
		pointer-events: none;
	}

	/* Nội dung nằm trên lớp ánh sáng */
	> :not(.glass-card__glow) {
		position: relative;
		z-index: 1;
	}

	/* ===== Viền sáng: 2 "sao chổi" (đầu trắng, đuôi xanh) đuổi nhau quanh card =====
	   Lớp ngoài (span) mang filter drop-shadow ⇒ quầng sáng bám theo đúng vệt sao chổi.
	   Lớp ::before là vòng viền 2px (mask chừa đúng phần viền), gradient xoay bằng @property. */
	.glass-card__glow {
		position: absolute;
		inset: -1px;
		z-index: 2;
		border-radius: inherit;
		pointer-events: none;
		filter: drop-shadow(0 0 2px rgba(224, 242, 254, 1)) drop-shadow(0 0 8px rgba(56, 189, 248, 0.9)) drop-shadow(0 0 20px rgba(37, 99, 235, 0.8));
		transition: filter 0.4s ease;

		&::before {
			content: '';
			position: absolute;
			inset: 0;
			border-radius: inherit;
			padding: 2.5px;
			--lg-border-angle: 0deg;
			background: repeating-conic-gradient(
				from var(--lg-border-angle),
				transparent 0deg 40deg,
				rgba(37, 99, 235, 0.35) 90deg,
				#3b82f6 140deg,
				#38bdf8 165deg,
				#e0f2fe 175deg,
				#ffffff 178deg,
				transparent 180deg
			);
			-webkit-mask:
				linear-gradient(#000 0 0) content-box,
				linear-gradient(#000 0 0);
			-webkit-mask-composite: xor;
			mask-composite: exclude;
			animation: lg-border-spin 6s linear infinite;
		}
	}

	/* Đang nhập liệu: sao chổi chạy nhanh hơn, quầng sáng mạnh hơn, card phát sáng nhẹ */
	&:focus-within {
		box-shadow:
			0 30px 60px -12px rgba(2, 6, 23, 0.6),
			0 0 48px -10px rgba(59, 130, 246, 0.6),
			inset 0 1px 0 rgba(255, 255, 255, 0.08);

		.glass-card__glow {
			filter: drop-shadow(0 0 3px rgba(240, 249, 255, 1)) drop-shadow(0 0 12px rgba(56, 189, 248, 1)) drop-shadow(0 0 28px rgba(37, 99, 235, 0.9));
		}
		.glass-card__glow::before {
			animation-duration: 3s;
		}
	}

	.corp-name {
		font-size: 24px;
		font-weight: 700;
		color: #ffffff;
		line-height: 1.2;
		letter-spacing: 0.2px;
	}
	.corp-desc {
		margin-top: 6px;
		font-size: 13px;
		color: var(--lg-text-muted);
	}
	.system-title {
		display: flex;
		align-items: center;
		gap: 12px;
		margin: 28px 0 12px;
		font-size: 16px;
		font-weight: 600;
		color: #e2e8f0;
		letter-spacing: 1.2px;
		white-space: nowrap;

		/* 2 vạch mảnh 2 bên tiêu đề */
		&::before,
		&::after {
			content: '';
			flex: 1;
			height: 1px;
			background: linear-gradient(90deg, transparent, rgba(148, 163, 184, 0.5));
		}
		&::after {
			background: linear-gradient(90deg, rgba(148, 163, 184, 0.5), transparent);
		}
	}
}

/* Tabs trên nền tối */
.login-tabs {
	:deep(.el-tabs__nav-wrap::after) {
		background-color: var(--lg-line);
	}
	:deep(.el-tabs__item) {
		font-weight: 500;
		color: var(--lg-text-muted);
	}
	:deep(.el-tabs__item.is-active) {
		color: #ffffff;
	}
	:deep(.el-tabs__active-bar) {
		height: 2px;
		background: linear-gradient(90deg, var(--lg-accent), var(--lg-accent-strong));
	}
}

/* ===== Ô nhập liệu (Account.vue): nền kính tối, viền mảnh, focus sáng viền xanh ===== */
:deep(.input-group .el-input__wrapper) {
	height: 46px;
	padding: 0 14px;
	border-radius: 10px;
	background: rgba(255, 255, 255, 0.06);
	box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.16);
	transition: box-shadow 0.2s ease, background-color 0.2s ease;
}
:deep(.input-group .el-input__wrapper:hover) {
	box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.3);
}
:deep(.input-group .el-input__wrapper.is-focus) {
	background: rgba(255, 255, 255, 0.09);
	box-shadow: inset 0 0 0 1px var(--lg-accent), 0 0 0 4px rgba(96, 165, 250, 0.18);
}
/* Biến màu của Element Plus cho ô nhập: chữ sáng, placeholder xám xanh (tránh bị theme chung ép chữ đen) */
:deep(.input-group) {
	--el-input-text-color: var(--lg-text);
	--el-input-placeholder-color: #8a99b0;
	--el-input-icon-color: var(--lg-text-muted);
	--el-input-clear-hover-color: #ffffff;
}
:deep(.input-group .el-input__inner) {
	/* Khi trình duyệt xem trước gợi ý mật khẩu đã lưu, nó ép chữ = màu hệ thống FieldText (!important, CSS trang
	   không ghi đè được). FieldText đi theo color-scheme ⇒ khai báo dark để chữ xem trước là màu sáng. */
	color-scheme: dark;
	height: 44px;
	font-size: 15px;
	font-weight: 500;
	letter-spacing: 0.2px;
	color: var(--lg-text) !important;
	-webkit-text-fill-color: var(--lg-text);
	caret-color: var(--lg-accent);
}
:deep(.input-group .el-input__inner::placeholder) {
	font-size: 14px;
	font-weight: 400;
	color: #8a99b0;
	-webkit-text-fill-color: #8a99b0;
}
/* Chrome autofill (kể cả lúc rê chuột/focus): giữ nền tối, chữ sáng */
:deep(.input-group .el-input__inner:-webkit-autofill),
:deep(.input-group .el-input__inner:-webkit-autofill:hover),
:deep(.input-group .el-input__inner:-webkit-autofill:focus),
:deep(.input-group .el-input__inner:autofill) {
	-webkit-text-fill-color: var(--lg-text) !important;
	caret-color: var(--lg-accent);
	-webkit-box-shadow: 0 0 0 1000px transparent inset;
	transition: background-color 9999s ease-out 0s;
}
:deep(.input-group .el-input__prefix .el-icon),
:deep(.input-group .el-input__suffix .el-icon),
:deep(.input-group .login-content-password) {
	font-size: 17px;
}
:deep(.input-group .el-input__prefix),
:deep(.input-group .el-input__suffix) {
	color: var(--lg-text-muted);
	transition: color 0.2s ease;
}
:deep(.input-group .el-input__wrapper.is-focus .el-input__prefix) {
	color: var(--lg-accent);
}
:deep(.login-content-password:hover) {
	color: #ffffff !important;
}
:deep(.el-form-item__error) {
	color: #fca5a5;
	padding-top: 3px;
}

/* ===== Nút đăng nhập: gradient cobalt → sky + hiệu ứng =====
   - Vệt sáng quét ngang định kỳ (shine)
   - Hover: nổi lên, phát sáng, gradient trôi, mũi tên trượt vào
   - Nhấn: lún nhẹ; v-waves tạo gợn sóng tại điểm nhấn
   - Đang xử lý: gradient chạy liên tục */
:deep(.login-content-submit.el-button) {
	position: relative;
	overflow: hidden;
	width: 100%;
	height: 48px;
	margin-top: 12px;
	border: none;
	border-radius: 12px;
	font-size: 15px;
	font-weight: 600;
	letter-spacing: 1.5px;
	color: #ffffff;
	/* Dải màu đối xứng, mặc định hiện nửa phải (đậm → sáng từ trái sang phải).
	   Hover/loading dịch nền sang PHẢI để luồng màu cùng chiều vệt sáng quét và gợn sóng. */
	background: linear-gradient(110deg, #0ea5e9 0%, #2563eb 28%, #1e56a0 50%, #2563eb 75%, #0ea5e9 100%);
	background-size: 220% 100%;
	background-position: 100% 50%;
	box-shadow: 0 12px 28px -10px rgba(37, 99, 235, 0.75), inset 0 1px 0 rgba(255, 255, 255, 0.25);
	transition:
		transform 0.25s cubic-bezier(0.22, 1, 0.36, 1),
		box-shadow 0.25s ease,
		background-position 0.6s ease;
}
:deep(.login-content-submit.el-button > span) {
	position: relative;
	z-index: 1;
	display: inline-flex;
	align-items: center;
	gap: 8px;
}
/* Vệt sáng quét ngang */
:deep(.login-content-submit.el-button::before) {
	content: '';
	position: absolute;
	top: 0;
	left: -60%;
	width: 45%;
	height: 100%;
	background: linear-gradient(100deg, transparent 0%, rgba(255, 255, 255, 0.35) 50%, transparent 100%);
	transform: skewX(-20deg);
	animation: login-btn-shine 3.6s ease-in-out 1.2s infinite;
	pointer-events: none;
}
:deep(.login-content-submit .login-submit__arrow) {
	width: 0;
	opacity: 0;
	transform: translateX(-6px);
	transition: width 0.25s ease, opacity 0.25s ease, transform 0.25s ease;
}
:deep(.login-content-submit.el-button:hover),
:deep(.login-content-submit.el-button:focus-visible) {
	transform: translateY(-2px);
	background-position: 0% 50%;
	box-shadow: 0 18px 36px -10px rgba(14, 165, 233, 0.75), 0 0 0 1px rgba(255, 255, 255, 0.12), inset 0 1px 0 rgba(255, 255, 255, 0.3);
}
:deep(.login-content-submit.el-button:hover .login-submit__arrow),
:deep(.login-content-submit.el-button:focus-visible .login-submit__arrow) {
	width: 16px;
	opacity: 1;
	transform: translateX(0);
}
:deep(.login-content-submit.el-button:focus-visible) {
	outline: 2px solid #bfdbfe;
	outline-offset: 3px;
}
:deep(.login-content-submit.el-button:active) {
	transform: translateY(0) scale(0.98);
	box-shadow: 0 6px 16px -8px rgba(37, 99, 235, 0.7), inset 0 2px 4px rgba(2, 6, 23, 0.2);
}
/* Đang đăng nhập: giữ nền cố định (đậm trái → sáng phải), vệt sáng quét nhanh, liên tục trái → phải */
:deep(.login-content-submit.el-button.is-loading) {
	transform: none;
	background-position: 100% 50%;
	cursor: progress;
}
:deep(.login-content-submit.el-button.is-loading::before) {
	width: 35%;
	background: linear-gradient(100deg, transparent 0%, rgba(255, 255, 255, 0.45) 50%, transparent 100%);
	animation: login-btn-loading-sweep 1.1s cubic-bezier(0.4, 0, 0.2, 1) infinite;
}

/* Góc xoay của viền sáng card (cần @property để animate được góc trong gradient) */
@property --lg-border-angle {
	syntax: '<angle>';
	initial-value: 0deg;
	inherits: false;
}

@keyframes lg-border-spin {
	to {
		--lg-border-angle: 360deg;
	}
}

@keyframes login-btn-shine {
	0% {
		left: -60%;
	}
	45%,
	100% {
		left: 130%;
	}
}

/* Vệt sáng lúc chờ đăng nhập: quét liên tục từ trái sang phải */
@keyframes login-btn-loading-sweep {
	from {
		left: -40%;
	}
	to {
		left: 110%;
	}
}

.forgot-row {
	text-align: center;
	margin-top: 5px;
	.forgot-link {
		font-size: 13px;
		font-weight: 500;
		color: var(--lg-accent);
		cursor: pointer;
		&:hover {
			text-decoration: underline;
		}
	}
}

.copyright {
	position: absolute;
	bottom: 24px;
	left: 50%;
	transform: translateX(-50%);
	max-width: 90%;
	padding: 6px 16px;
	border-radius: 999px;
	text-align: center;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	z-index: 3;
	font-size: 12px;
	letter-spacing: 0.3px;
	color: rgba(241, 245, 249, 0.92);
	/* Viên kính tối nhỏ: chữ luôn đọc được trên nền đường/trời sáng */
	background: rgba(15, 23, 42, 0.45);
	backdrop-filter: blur(8px);
	-webkit-backdrop-filter: blur(8px);
	border: 1px solid rgba(255, 255, 255, 0.12);
}

@media (max-width: 640px) {
	.brand-logo {
		top: 16px;
		left: 16px;
		padding: 8px 12px;
		img {
			height: 40px;
		}
	}
}

@media (prefers-reduced-motion: reduce) {
	.login-v1,
	.login-intro-leave-active,
	.login-intro__video {
		transition: none;
	}
	.glass-card__glow::before,
	:deep(.login-content-submit.el-button),
	:deep(.login-content-submit.el-button::before) {
		animation: none !important;
		transition: none;
	}
}
</style>
