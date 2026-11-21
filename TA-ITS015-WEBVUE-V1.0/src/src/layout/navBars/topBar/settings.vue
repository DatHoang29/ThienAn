<template>
	<div class="layout-breadcrumb-seting">
		<el-drawer :title="$t('lz.ui.setting.configTitle')" v-model="getThemeConfig.isDrawer" direction="rtl" destroy-on-close size="280px" @close="onDrawerClose">
			<el-scrollbar class="layout-breadcrumb-seting-bar">
				<!-- Global Theme -->
				<el-divider content-position="center" class="horizontal-divider">{{ $t('lz.ui.setting.oneTitle') }}</el-divider>
				<div class="layout-breadcrumb-seting-bar-flex">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.primary') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-color-picker v-model="getThemeConfig.primary" size="default" @change="onUserPrimaryChange"> </el-color-picker>
					</div>
				</div>
				<div style="margin: 10px 0">
					<el-button style="width: 100%" size="default" class="copy-config-btn" type="primary" ref="onColorRef" @click="state.onColorSet = true"> {{ $t('lz.ui.setting.changeColor') }} </el-button>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsDark') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isIsDark" size="small" @change="onAddDarkChange"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fontFamily') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-select v-model="getThemeConfig.globalFont" size="small" style="width: 120px; margin-right: 1px" @change="onGlobalFontChange">
							<el-option v-for="item in fontOptions" :key="item.value" :label="item.label" :value="item.value"></el-option>
						</el-select>
					</div>
				</div>

				<!-- Top Bar Settings -->
				<el-divider content-position="center" class="horizontal-divider">{{ $t('lz.ui.setting.twoTopTitle') }}</el-divider>
				<div class="layout-breadcrumb-seting-bar-flex">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoTopBar') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-color-picker v-model="getThemeConfig.topBar" size="default" @change="onUserBgColorChange('topBar')" :disabled="getThemeConfig.isTopBarSameThemeColor"> </el-color-picker>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt10">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoIsTopBarColorGradual') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isTopBarSameThemeColor" size="small" @change="onUserTopBarSameThemeColor" ></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoTopBarColor') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-color-picker v-model="getThemeConfig.topBarColor" size="default" @change="onUserBgColorChange('topBarColor')"> </el-color-picker>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt10">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoIsTopBarColorGradual') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isTopBarColorGradual" size="small" @change="onTopBarGradualChange"></el-switch>
					</div>
				</div>

				<!-- Menu Settings -->
				<el-divider content-position="center" class="horizontal-divider">{{ $t('lz.ui.setting.twoMenuTitle') }}</el-divider>
				<div class="layout-breadcrumb-seting-bar-flex">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoMenuBar') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-color-picker v-model="getThemeConfig.menuBar" size="default" @change="onUserBgColorChange('menuBar')"> </el-color-picker>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoMenuBarColor') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-color-picker v-model="getThemeConfig.menuBarColor" size="default" @change="onUserBgColorChange('menuBarColor')"> </el-color-picker>
					</div>
				</div>
				<!-- <div class="layout-breadcrumb-seting-bar-flex">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoMenuBarActiveColor') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-color-picker v-model="getThemeConfig.menuBarActiveColor" size="default" show-alpha @change="onBgColorPickerChange('menuBarActiveColor')" />
					</div>
				</div> -->
				<div class="layout-breadcrumb-seting-bar-flex mt14">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoIsMenuBarColorGradual') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isMenuBarColorGradual" size="small" @change="onMenuBarGradualChange"></el-switch>
					</div>
				</div>

				<!-- Column Settings -->
				<el-divider content-position="center" :style="{ opacity: getThemeConfig.layout !== 'columns' ? 0.5 : 1 }">{{ $t('lz.ui.setting.twoColumnsTitle') }}</el-divider>
				<div class="layout-breadcrumb-seting-bar-flex" :style="{ opacity: getThemeConfig.layout !== 'columns' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoColumnsMenuBar') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-color-picker v-model="getThemeConfig.columnsMenuBar" size="default" @change="onUserBgColorChange('columnsMenuBar')" :disabled="getThemeConfig.layout !== 'columns'">
						</el-color-picker>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex" :style="{ opacity: getThemeConfig.layout !== 'columns' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoColumnsMenuBarColor') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-color-picker v-model="getThemeConfig.columnsMenuBarColor" size="default" @change="onUserBgColorChange('columnsMenuBarColor')" :disabled="getThemeConfig.layout !== 'columns'">
						</el-color-picker>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt14" :style="{ opacity: getThemeConfig.layout !== 'columns' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoIsColumnsMenuBarColorGradual') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isColumnsMenuBarColorGradual" size="small" @change="onColumnsMenuBarGradualChange" :disabled="getThemeConfig.layout !== 'columns'"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt14" :style="{ opacity: getThemeConfig.layout !== 'columns' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoIsColumnsMenuHoverPreload') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isColumnsMenuHoverPreload" size="small" @change="onColumnsMenuHoverPreloadChange" :disabled="getThemeConfig.layout !== 'columns'"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt11">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoColumnsLogoHeight') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-input-number
							v-model="getThemeConfig.columnsLogoHeight"
							controls-position="right"
							:min="1"
							:max="9999"
							@change="onColumnsLogoHeightChange"
							size="small"
							style="width: 90px; margin-right: 1px"
						>
						</el-input-number>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt11">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoColumnsMenuWidth') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-input-number
							v-model="getThemeConfig.columnsMenuWidth"
							controls-position="right"
							:min="1"
							:max="9999"
							@change="onColumnsMenuWidthChange"
							size="small"
							style="width: 90px; margin-right: 1px"
						>
						</el-input-number>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt11">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.twoColumnsMenuHeight') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-input-number
							v-model="getThemeConfig.columnsMenuHeight"
							controls-position="right"
							:min="1"
							:max="9999"
							@change="onColumnsMenuHeightChange"
							size="small"
							style="width: 90px; margin-right: 1px"
						>
						</el-input-number>
					</div>
				</div>

				<!-- Interface Settings -->
				<el-divider content-position="center" class="horizontal-divider">{{ $t('lz.ui.setting.threeTitle') }}</el-divider>
				<div class="layout-breadcrumb-seting-bar-flex" :style="{ opacity: getThemeConfig.layout === 'transverse' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.threeIsCollapse') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isCollapse" :disabled="getThemeConfig.layout === 'transverse'" size="small" @change="onThemeConfigChange"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15" :style="{ opacity: getThemeConfig.layout === 'transverse' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.threeIsUniqueOpened') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isUniqueOpened" :disabled="getThemeConfig.layout === 'transverse'" size="small" @change="setLocalThemeConfig"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.threeIsFixedHeader') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isFixedHeader" size="small" @change="onIsFixedHeaderChange"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15" :style="{ opacity: getThemeConfig.layout !== 'classic' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.threeIsClassicSplitMenu') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isClassicSplitMenu" :disabled="getThemeConfig.layout !== 'classic'" size="small" @change="onClassicSplitMenuChange"> </el-switch>
					</div>
				</div>
				<!-- <div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.threeIsLockScreen') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isLockScreen" size="small" @change="setLocalThemeConfig"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt11">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.threeLockScreenTime') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-input-number v-model="getThemeConfig.lockScreenTime" controls-position="right" :min="1" :max="9999" @change="setLocalThemeConfig" size="small" style="width: 90px; margin-right: 1px">
						</el-input-number>
					</div>
				</div> -->

				<!-- Interface Display -->
				<el-divider content-position="center">{{ $t('lz.ui.setting.fourTitle') }}</el-divider>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsShowLogo') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isShowLogo" size="small" @change="onIsShowLogoChange"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15" :style="{ opacity: getThemeConfig.layout === 'classic' || getThemeConfig.layout === 'transverse' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsBreadcrumb') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch
							v-model="getThemeConfig.isBreadcrumb"
							:disabled="getThemeConfig.layout === 'classic' || getThemeConfig.layout === 'transverse'"
							size="small"
							@change="onIsBreadcrumbChange"
						></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsBreadcrumbIcon') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isBreadcrumbIcon" size="small" @change="setLocalThemeConfig"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsTagsview') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isTagsview" size="small" @change="setLocalThemeConfig"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsTagsviewIcon') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isTagsviewIcon" size="small" @change="setLocalThemeConfig"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsCacheTagsView') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isCacheTagsView" size="small" @change="setLocalThemeConfig"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15" :style="{ opacity: state.isMobile ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsSortableTagsView') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isSortableTagsView" :disabled="state.isMobile ? true : false" size="small" @change="onSortableTagsViewChange"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsShareTagsView') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isShareTagsView" size="small" @change="onShareTagsViewChange"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsFooter') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isFooter" size="small" @change="setLocalThemeConfig"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsGrayscale') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isGrayscale" size="small" @change="onAddFilterChange('grayscale')"></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsInvert') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isInvert" size="small" @change="onAddFilterChange('invert')"></el-switch>
					</div>
				</div>
				<!-- <div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourIsWatermark') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-switch v-model="getThemeConfig.isWatermark" size="small" @change="onWatermarkChange" disabled></el-switch>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt14">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fourWatermarkText') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-input v-model="getThemeConfig.watermarkText" size="small" style="width: 90px; margin-right: 1px" @input="onWatermarkTextInput" disabled></el-input>
					</div>
				</div> -->

				<!-- Other Settings -->
				<el-divider content-position="center" class="horizontal-divider">{{ $t('lz.ui.setting.fiveTitle') }}</el-divider>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fiveTagsStyle') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-select v-model="getThemeConfig.tagsStyle" placeholder="Please select" size="small" style="width: 90px; margin-right: 1px" @change="setLocalThemeConfig">
							<el-option :label="`${$t('lz.ui.setting.tagStyle')} 1`" value="tags-style-one"></el-option>
							<el-option :label="`${$t('lz.ui.setting.tagStyle')} 2`" value="tags-style-two"></el-option>

							<el-option :label="`${$t('lz.ui.setting.tagStyle')} 4`" value="tags-style-four"></el-option>
							<el-option :label="`${$t('lz.ui.setting.tagStyle')} 5`" value="tags-style-five"></el-option>
						</el-select>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fiveAnimation') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-select v-model="getThemeConfig.animation" placeholder="Please select" size="small" style="width: 90px; margin-right: 1px" @change="setLocalThemeConfig">
							<el-option label="slide-right" value="slide-right"></el-option>
							<el-option label="slide-left" value="slide-left"></el-option>
							<el-option label="opacitys" value="opacitys"></el-option>
							<el-option label="fade" value="fade"></el-option>
							<el-option label="fadeUp" value="fadeUp"></el-option>
							<el-option label="fadeDown" value="fadeDown"></el-option>
							<el-option label="fadeLeft" value="fadeLeft"></el-option>
							<el-option label="fadeRight" value="fadeRight"></el-option>
							<el-option label="lightSpeedLeft" value="lightSpeedLeft"></el-option>
							<el-option label="lightSpeedRight" value="lightSpeedRight"></el-option>
							<el-option label="zoom" value="zoom"></el-option>
							<el-option label="zoomUp" value="zoomUp"></el-option>
							<el-option label="zoomDown" value="zoomDown"></el-option>
							<el-option label="zoomLeft" value="zoomLeft"></el-option>
							<el-option label="zoomRight" value="zoomRight"></el-option>
							<el-option label="flip" value="flip"></el-option>
							<el-option label="flipX" value="flipX"></el-option>
							<el-option label="flipY" value="flipY"></el-option>
							<el-option label="backUp" value="backUp"></el-option>
							<el-option label="backDown" value="backDown"></el-option>
							<el-option label="backLeft" value="backLeft"></el-option>
							<el-option label="backRight" value="backRight"></el-option>
							<el-option label="bounce" value="bounce"></el-option>
							<el-option label="bounceUp" value="bounceUp"></el-option>
							<el-option label="bounceDown" value="bounceDown"></el-option>
							<el-option label="bounceLeft" value="bounceLeft"></el-option>
							<el-option label="bounceRight" value="bounceRight"></el-option>
						</el-select>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15" :style="{ opacity: getThemeConfig.layout !== 'columns' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fiveColumnsAsideStyle') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-select
							v-model="getThemeConfig.columnsAsideStyle"
							placeholder="Please select"
							size="small"
							style="width: 90px; margin-right: 1px"
							:disabled="getThemeConfig.layout !== 'columns' ? true : false"
							@change="setLocalThemeConfig"
						>
							<el-option label="Rounded" value="columns-round"></el-option>
							<el-option label="Card" value="columns-card"></el-option>
						</el-select>
					</div>
				</div>
				<div class="layout-breadcrumb-seting-bar-flex mt15 mb27" :style="{ opacity: getThemeConfig.layout !== 'columns' ? 0.5 : 1 }">
					<div class="layout-breadcrumb-seting-bar-flex-label">{{ $t('lz.ui.setting.fiveColumnsAsideLayout') }}</div>
					<div class="layout-breadcrumb-seting-bar-flex-value">
						<el-select
							v-model="getThemeConfig.columnsAsideLayout"
							placeholder="Please select"
							size="small"
							style="width: 90px; margin-right: 1px"
							:disabled="getThemeConfig.layout !== 'columns' ? true : false"
							@change="setLocalThemeConfig"
						>
							<el-option label="Horizontal" value="columns-horizontal"></el-option>
							<el-option label="Vertical" value="columns-vertical"></el-option>
						</el-select>
					</div>
				</div>

				<!-- Layout Switch -->
				<el-divider content-position="center">{{ $t('lz.ui.setting.sixTitle') }}</el-divider>
				<div class="layout-drawer-content-flex">
					<!-- defaults layout -->
					<div class="layout-drawer-content-item" @click="onSetLayout('defaults')">
						<section class="el-container el-circular" :class="{ 'drawer-layout-active': getThemeConfig.layout === 'defaults' }">
							<aside class="el-aside" style="width: 20px"></aside>
							<section class="el-container is-vertical">
								<header class="el-header" style="height: 10px"></header>
								<main class="el-main"></main>
							</section>
						</section>
						<div class="layout-tips-warp" :class="{ 'layout-tips-warp-active': getThemeConfig.layout === 'defaults' }">
							<div class="layout-tips-box">
								<p class="layout-tips-txt">{{ $t('lz.ui.setting.sixDefaults') }}</p>
							</div>
						</div>
					</div>
					<!-- classic layout -->
					<div class="layout-drawer-content-item" @click="onSetLayout('classic')">
						<section class="el-container is-vertical el-circular" :class="{ 'drawer-layout-active': getThemeConfig.layout === 'classic' }">
							<header class="el-header" style="height: 10px"></header>
							<section class="el-container">
								<aside class="el-aside" style="width: 20px"></aside>
								<section class="el-container is-vertical">
									<main class="el-main"></main>
								</section>
							</section>
						</section>
						<div class="layout-tips-warp" :class="{ 'layout-tips-warp-active': getThemeConfig.layout === 'classic' }">
							<div class="layout-tips-box">
								<p class="layout-tips-txt">{{ $t('lz.ui.setting.sixClassic') }}</p>
							</div>
						</div>
					</div>
					<!-- transverse layout -->
					<div class="layout-drawer-content-item" @click="onSetLayout('transverse')">
						<section class="el-container is-vertical el-circular" :class="{ 'drawer-layout-active': getThemeConfig.layout === 'transverse' }">
							<header class="el-header" style="height: 10px"></header>
							<section class="el-container">
								<section class="el-container is-vertical">
									<main class="el-main"></main>
								</section>
							</section>
						</section>
						<div class="layout-tips-warp" :class="{ 'layout-tips-warp-active': getThemeConfig.layout === 'transverse' }">
							<div class="layout-tips-box">
								<p class="layout-tips-txt">{{ $t('lz.ui.setting.sixTransverse') }}</p>
							</div>
						</div>
					</div>
					<!-- columns layout -->
					<div class="layout-drawer-content-item" @click="onSetLayout('columns')">
						<section class="el-container el-circular" :class="{ 'drawer-layout-active': getThemeConfig.layout === 'columns' }">
							<aside class="el-aside-dark" style="width: 10px"></aside>
							<aside class="el-aside" style="width: 20px"></aside>
							<section class="el-container is-vertical">
								<header class="el-header" style="height: 10px"></header>
								<main class="el-main"></main>
							</section>
						</section>
						<div class="layout-tips-warp" :class="{ 'layout-tips-warp-active': getThemeConfig.layout === 'columns' }">
							<div class="layout-tips-box">
								<p class="layout-tips-txt">{{ $t('lz.ui.setting.sixColumns') }}</p>
							</div>
						</div>
					</div>
				</div>
				<div class="copy-config">
					<!-- <el-alert :title="$t('lz.ui.setting.tipText')" type="warning" :closable="false"> </el-alert> -->
					<!-- <el-button size="default" class="copy-config-btn" type="primary" ref="copyConfigBtnRef" @click="onCopyConfigClick">
						<el-icon class="mr5">
							<ele-CopyDocument />
						</el-icon>
						{{ $t('lz.ui.setting.copyText') }}
					</el-button> -->
					<el-button size="default" class="copy-config-btn-reset" type="info" @click="onResetConfigClick">
						<el-icon class="mr5">
							<ele-RefreshRight />
						</el-icon>
						{{ $t('lz.ui.setting.resetText') }}
					</el-button>
				</div>
			</el-scrollbar>
		</el-drawer>

		<el-dialog v-model="state.onColorSet">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-MagicStick /> </el-icon>
					<span> {{ $t('lz.ui.setting.themeColors') }} </span>
				</div>
			</template>
			<div style="height: 70vh; overflow-y: scroll; overflow-x: hidden">
				<el-row :gutter="20">
					<el-col :xs="6" :sm="4" :md="4" :lg="4" :xl="3" class="mb15" v-for="i in chineseColorArr" :key="i" @click="cliChineseColor(i.hex)">
						<div class="traditionalColors" :style="'background:' + i.hex">
							<el-icon class="traditionalColors-circleCheck" v-if="getThemeConfig.primary == i.hex" size="20" color="#fff"><ele-CircleCheck /></el-icon>
						</div>
						<div class="traditionalColors-chines mt10" style="cursor: grab">{{ i.name }}</div>
						<div class="traditionalColors-chines" style="cursor: grab; color: gray">{{ i.hex }}</div>
					</el-col>
				</el-row>
			</div>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="layoutBreadcrumbSeting">
import { nextTick, onUnmounted, onMounted, computed, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useChangeColor, setPrimaryTheme } from '/@/utils/theme';
import { verifyAndSpace } from '/@/utils/toolsValidate';
import { Local, Session } from '/@/utils/storage';
import Watermark from '/@/utils/watermark';
import commonFunction from '/@/utils/commonFunction';
import other from '/@/utils/other';
import mittBus from '/@/utils/mitt';
import chineseColorArr from './colors.json';

// Define variable content
const { locale } = useI18n();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const { copyText } = commonFunction();
const { getLightColor } = useChangeColor();
const state = reactive({
	isMobile: false,
	onColorSet: false,
});

// Danh sách font khả dụng (khai báo @font-face tương ứng trong theme/app.scss)
const fontStackMap: Record<string, string> = {
	Inter: `'Inter', 'Helvetica Neue', Helvetica, Arial, 'PingFang SC', 'Microsoft YaHei', sans-serif`,
	system: `'Helvetica Neue', Helvetica, Arial, 'PingFang SC', 'Microsoft YaHei', SimSun, sans-serif`,
};
const fontOptions = [
	{ label: 'Inter', value: 'Inter' },
	{ label: 'System', value: 'system' },
];
// Áp dụng font vào biến CSS toàn cục (Element Plus, VxeTable... đều dùng --el-font-family)
const applyGlobalFont = (font: string) => {
	const stack = fontStackMap[font] || fontStackMap.Inter;
	document.documentElement.style.setProperty('--el-font-family', stack);
	document.documentElement.style.setProperty('--vxe-ui-font-family', stack);
};

// Đánh dấu người dùng đã TỰ chỉnh màu trong drawer -> khi deploy đổi màu preset sẽ KHÔNG
// ghi đè màu người dùng (xem utils/theme.ts applyDesignPreset). Chỉ gọi từ hành động thật của
// người dùng, TUYỆT ĐỐI không gọi trong các hàm áp màu tự động (initLayoutChangeFun, onMounted).
const markUserThemeCustomized = () => Local.set('themeUserCustomized', true);

// Change theme color
const cliChineseColor = (e: any) => {
	// navigator.clipboard.writeText(e);
	getThemeConfig.value.primary = e;
	markUserThemeCustomized();
	onColorPickerChange();

	// ElMessage.info(`Theme color copied: 【${e}】`);
};

// Get layout configuration information
const getThemeConfig = computed(() => {
	if (!themeConfig.value.columnsMenuWidth) {
		themeConfig.value.columnsMenuWidth = 70;
	}
	if (!themeConfig.value.columnsMenuHeight) {
		themeConfig.value.columnsMenuHeight = 50;
	}
	if (!themeConfig.value.columnsLogoHeight) {
		themeConfig.value.columnsLogoHeight = 50;
	}
	return themeConfig.value;
});
// 1. Global theme
const onColorPickerChange = () => {
	if (!getThemeConfig.value.primary) return ElMessage.warning('Global theme primary color value cannot be empty');
	// Áp thang màu chủ đạo qua helper dùng chung (đồng bộ với lúc khởi động app)
	setPrimaryTheme(getThemeConfig.value.primary);
	setDispatchThemeConfig();
	if (getThemeConfig.value.isTopBarSameThemeColor) {	
		onTopBarSameThemeColor();
	}
};

const onTopBarSameThemeColor = () => {
	if (!getThemeConfig.value.primary) return ElMessage.warning('Global theme primary color value cannot be empty');
	getThemeConfig.value.topBar = getThemeConfig.value.primary;
	onBgColorPickerChange('topBar');
};

// 2. Menu / Top bar
const onBgColorPickerChange = (bg: string) => {
	document.documentElement.style.setProperty(`--next-bg-${bg}`, themeConfig.value[bg]);
	if (bg === 'menuBar') {
		document.documentElement.style.setProperty(`--next-bg-menuBar-light-1`, getLightColor(getThemeConfig.value.menuBar, 0.05));
	}
	onTopBarGradualChange();
	onMenuBarGradualChange();
	onColumnsMenuBarGradualChange();
	setDispatchThemeConfig();
};

// --- Wrapper cho SỰ KIỆN NGƯỜI DÙNG đổi màu: đánh dấu tự chỉnh rồi mới áp màu.
// Template bind vào các wrapper này thay vì gọi thẳng hàm áp màu (vốn cũng bị gọi tự động).
const onUserPrimaryChange = () => {
	markUserThemeCustomized();
	onColorPickerChange();
};
const onUserBgColorChange = (bg: string) => {
	markUserThemeCustomized();
	onBgColorPickerChange(bg);
};
const onUserTopBarSameThemeColor = () => {
	markUserThemeCustomized();
	onTopBarSameThemeColor();
};
// 2. Menu / Top bar --> Top bar background gradient
const onTopBarGradualChange = () => {
	setGraduaFun('.layout-navbars-breadcrumb-index', getThemeConfig.value.isTopBarColorGradual, getThemeConfig.value.topBar);
};
// 2. Menu / Top bar --> Menu background gradient
const onMenuBarGradualChange = () => {
	setGraduaFun('.layout-container .el-aside', getThemeConfig.value.isMenuBarColorGradual, getThemeConfig.value.menuBar);
};
// 2. Menu / Top bar --> Column menu background gradient
const onColumnsMenuBarGradualChange = () => {
	setGraduaFun('.layout-container .layout-columns-aside', getThemeConfig.value.isColumnsMenuBarColorGradual, getThemeConfig.value.columnsMenuBar);
};
// 2. Menu / Top bar --> Background gradient function
const setGraduaFun = (el: string, bool: boolean, color: string) => {
	nextTick(() => {
		setTimeout(() => {
			let els = document.querySelector(el);
			if (!els) return false;
			document.documentElement.style.setProperty('--el-menu-bg-color', document.documentElement.style.getPropertyValue('--next-bg-menuBar'));
			if (bool) els.setAttribute('style', `background:linear-gradient(0.75turn , ${color} , ${getLightColor(color, 0.1)}, ${getLightColor(color, 0.3)})`);
			else els.setAttribute('style', ``);
			setLocalThemeConfig();
		}, 300);
	});
};
// 2. Column settings -> Column menu mouse hover preload
const onColumnsMenuHoverPreloadChange = () => {
	setLocalThemeConfig();
};
// 2. Column settings -> Column logo height
const onColumnsLogoHeightChange = () => {
	document.documentElement.style.setProperty('--el-columnsLogoHeight', `${themeConfig.value.columnsLogoHeight}px`);
	setLocalThemeConfig();
};
// 2. Column settings -> Column menu width
const onColumnsMenuWidthChange = () => {
	document.documentElement.style.setProperty('--el-columnsMenuWidth', `${themeConfig.value.columnsMenuWidth}px`);
	document.documentElement.style.setProperty('--el-columnsMenuRoundWidth', `${themeConfig.value.columnsMenuWidth - 5}px`);
	setLocalThemeConfig();
};
// 2. Column settings -> Column menu height
const onColumnsMenuHeightChange = () => {
	document.documentElement.style.setProperty('--el-columnsMenuHeight', `${themeConfig.value.columnsMenuHeight}px`);
	document.documentElement.style.setProperty('--el-columnsMenuRoundHeight', `${themeConfig.value.columnsMenuHeight - 5}px`);
	setLocalThemeConfig();
};
// 3. Interface settings --> Menu horizontal collapse
const onThemeConfigChange = () => {
	setDispatchThemeConfig();
};
// 3. Interface settings --> Fixed Header
const onIsFixedHeaderChange = () => {
	getThemeConfig.value.isFixedHeaderChange = getThemeConfig.value.isFixedHeader ? false : true;
	setLocalThemeConfig();
};
// 3. Interface settings --> Classic layout split menu
const onClassicSplitMenuChange = () => {
	getThemeConfig.value.isBreadcrumb = false;
	setLocalThemeConfig();
	mittBus.emit('getBreadcrumbIndexSetFilterRoutes');
};
// 4. Interface display --> Sidebar Logo
const onIsShowLogoChange = () => {
	getThemeConfig.value.isShowLogoChange = getThemeConfig.value.isShowLogo ? false : true;
	setLocalThemeConfig();
};
// 4. Interface display --> Breadcrumb
const onIsBreadcrumbChange = () => {
	if (getThemeConfig.value.layout === 'classic') {
		getThemeConfig.value.isClassicSplitMenu = false;
	}
	setLocalThemeConfig();
};
// 4. Interface display --> Enable TagsView drag and drop
const onSortableTagsViewChange = () => {
	mittBus.emit('openOrCloseSortable');
	setLocalThemeConfig();
};
// 4. Interface display --> Enable shared TagsView
const onShareTagsViewChange = () => {
	mittBus.emit('openShareTagsView');
	setLocalThemeConfig();
};
// 4. Interface display --> Grayscale mode / color weakness mode
const onAddFilterChange = (attr: string) => {
	if (attr === 'grayscale') {
		if (getThemeConfig.value.isGrayscale) getThemeConfig.value.isInvert = false;
	} else {
		if (getThemeConfig.value.isInvert) getThemeConfig.value.isGrayscale = false;
	}
	const cssAttr = attr === 'grayscale' ? `grayscale(${getThemeConfig.value.isGrayscale ? 1 : 0})` : `invert(${getThemeConfig.value.isInvert ? '80%' : '0%'})`;
	const appEle = document.body;
	appEle.setAttribute('style', `filter: ${cssAttr}`);
	setLocalThemeConfig();
};
// 1. Global theme --> Font family
const onGlobalFontChange = () => {
	markUserThemeCustomized();
	applyGlobalFont(getThemeConfig.value.globalFont);
	setDispatchThemeConfig();
};
// 4. Interface display --> Dark mode
const onAddDarkChange = () => {
	const body = document.documentElement as HTMLElement;
	if (getThemeConfig.value.isIsDark) {
		body.setAttribute('data-theme', 'dark');
		body.setAttribute('data-vxe-ui-theme', 'dark');
	} else {
		body.setAttribute('data-theme', '');
		body.setAttribute('data-vxe-ui-theme', 'light');
	}
};
// 4. Interface display --> Enable watermark
const onWatermarkChange = () => {
	getThemeConfig.value.isWatermark ? Watermark.set(getThemeConfig.value.watermarkText) : Watermark.del();
	setLocalThemeConfig();
};
// 4. Interface display --> Watermark text
const onWatermarkTextInput = (val: string) => {
	getThemeConfig.value.watermarkText = verifyAndSpace(val);
	if (getThemeConfig.value.watermarkText === '') return false;
	if (getThemeConfig.value.isWatermark) Watermark.set(getThemeConfig.value.watermarkText);
	setLocalThemeConfig();
};
// 5. Layout switch
const onSetLayout = (layout: string) => {
	Local.set('oldLayout', layout);
	if (getThemeConfig.value.layout === layout) return false;
	if (layout === 'transverse') getThemeConfig.value.isCollapse = false;
	getThemeConfig.value.layout = layout;
	getThemeConfig.value.isDrawer = false;
	initLayoutChangeFun();
};
// Set layout switch function
const initLayoutChangeFun = () => {
	onBgColorPickerChange('menuBar');
	onBgColorPickerChange('menuBarColor');
	onBgColorPickerChange('menuBarActiveColor');
	onBgColorPickerChange('topBar');
	onBgColorPickerChange('topBarColor');
	onBgColorPickerChange('columnsMenuBar');
	onBgColorPickerChange('columnsMenuBarColor');
};
// When closing the dialog, initialize variables. Variables are used to handle layoutScrollbarRef.value.update() to update scrollbar height
const onDrawerClose = () => {
	getThemeConfig.value.isFixedHeaderChange = false;
	getThemeConfig.value.isShowLogoChange = false;
	getThemeConfig.value.isDrawer = false;
	setLocalThemeConfig();
};
// Open layout configuration dialog
const openDrawer = () => {
	getThemeConfig.value.isDrawer = true;
};
// Trigger store layout configuration update
const setDispatchThemeConfig = () => {
	setLocalThemeConfig();
	setLocalThemeConfigStyle();
};
// Store layout configuration
const setLocalThemeConfig = () => {
	Local.remove('themeConfig');
	Local.set('themeConfig', getThemeConfig.value);
};
// Store layout configuration global theme style (html root tag)
const setLocalThemeConfigStyle = () => {
	Local.set('themeConfigStyle', document.documentElement.style.cssText);
};
// One-click copy configuration
const onCopyConfigClick = () => {
	let copyThemeConfig = Local.get('themeConfig');
	copyThemeConfig.isDrawer = false;
	copyText(JSON.stringify(copyThemeConfig)).then(() => {
		getThemeConfig.value.isDrawer = false;
	});
};
// One-click restore default
const onResetConfigClick = () => {
	Local.clear();
	Session.clear();
	window.location.reload();
	// @ts-ignore
	Local.set('version', __NEXT_VERSION__);
};
// Initialize menu styles, etc.
const initSetStyle = () => {
	// 2. Menu / Top bar --> Top bar background gradient
	onTopBarGradualChange();
	// 2. Menu / Top bar --> Menu background gradient
	onMenuBarGradualChange();
	// 2. Menu / Top bar --> Column menu background gradient
	onColumnsMenuBarGradualChange();
};
onMounted(() => {
	nextTick(() => {
		// LUÔN dựng lại biến màu bố cục (--next-bg-*) mỗi lần load. Trước đây bị chặn bởi cờ
		// 'frequency' nên Ctrl+F5 (còn cache) không set lại -> sidebar dễ mất màu; trong khi
		// xoá cache lại chạy -> hai trường hợp cho kết quả khác nhau. Bỏ chặn để nhất quán.
		initLayoutChangeFun();
		Local.set('frequency', 1);
		// Listen for window size changes, non-default layout, set to default layout (adapt to mobile)
		mittBus.on('layoutMobileResize', (res: LayoutMobileResize) => {
			getThemeConfig.value.layout = res.layout;
			getThemeConfig.value.isDrawer = false;
			initLayoutChangeFun();
			state.isMobile = other.isMobile();
		});
		setTimeout(() => {
			// Default styles
			onColorPickerChange();
			// Grayscale mode
			if (getThemeConfig.value.isGrayscale) onAddFilterChange('grayscale');
			// Color weakness mode
			if (getThemeConfig.value.isInvert) onAddFilterChange('invert');
			// Dark mode
			if (getThemeConfig.value.isIsDark) onAddDarkChange();
			// Enable watermark
			onWatermarkChange();
			// Set column logo height
			onColumnsLogoHeightChange();
			// Set column menu width
			onColumnsMenuWidthChange();
			// Set column menu height
			onColumnsMenuHeightChange();
			// Language internationalization
			if (Local.get('themeConfig')) locale.value = Local.get('themeConfig').globalI18n;
			// Global font
			applyGlobalFont(getThemeConfig.value.globalFont);
			// Initialize menu styles, etc.
			initSetStyle();
		}, 100);
	});
});
onUnmounted(() => {
	mittBus.off('layoutMobileResize', () => {});
});

// Expose variables
defineExpose({
	openDrawer,
});
</script>

<style scoped lang="scss">
.layout-breadcrumb-seting-bar {
	height: calc(100vh - 50px);
	padding: 0 15px;
	:deep(.el-scrollbar__view) {
		overflow-x: hidden !important;
	}
	
	.layout-breadcrumb-seting-bar-flex {
		display: flex;
		align-items: center;
		margin-bottom: 5px;
		&-label {
			flex: 1;
			color: var(--el-text-color-primary);
		}
	}
	.layout-drawer-content-flex {
		overflow: hidden;
		display: flex;
		flex-wrap: wrap;
		align-content: flex-start;
		margin: 0 -5px;
		.layout-drawer-content-item {
			width: 50%;
			height: 70px;
			cursor: pointer;
			border: 1px solid transparent;
			position: relative;
			padding: 5px;
			.el-container {
				height: 100%;
				.el-aside-dark {
					background-color: var(--next-color-seting-header);
				}
				.el-aside {
					background-color: var(--next-color-seting-aside);
				}
				.el-header {
					background-color: var(--next-color-seting-header);
				}
				.el-main {
					background-color: var(--next-color-seting-main);
				}
			}
			.el-circular {
				border-radius: 2px;
				overflow: hidden;
				border: 1px solid transparent;
				transition: all 0.3s ease-in-out;
			}
			.drawer-layout-active {
				border: 1px solid;
				border-color: var(--el-color-primary);
			}
			.layout-tips-warp,
			.layout-tips-warp-active {
				transition: all 0.3s ease-in-out;
				position: absolute;
				left: 50%;
				top: 50%;
				transform: translate(-50%, -50%);
				border: 1px solid;
				border-color: var(--el-color-primary-light-5);
				border-radius: 100%;
				padding: 4px;
				.layout-tips-box {
					transition: inherit;
					width: 30px;
					height: 30px;
					z-index: 9;
					border: 1px solid;
					border-color: var(--el-color-primary-light-5);
					border-radius: 100%;
					.layout-tips-txt {
						transition: inherit;
						position: relative;
						top: 5px;
						font-size: 12px;
						line-height: 1;
						letter-spacing: 2px;
						white-space: nowrap;
						color: var(--el-color-primary-light-5);
						text-align: center;
						transform: rotate(30deg);
						left: -1px;
						background-color: var(--next-color-seting-main);
						width: 32px;
						height: 17px;
						line-height: 17px;
					}
				}
			}
			.layout-tips-warp-active {
				border: 1px solid;
				border-color: var(--el-color-primary);
				.layout-tips-box {
					border: 1px solid;
					border-color: var(--el-color-primary);
					.layout-tips-txt {
						color: var(--el-color-primary) !important;
						background-color: var(--next-color-seting-main) !important;
					}
				}
			}
			&:hover {
				.el-circular {
					transition: all 0.3s ease-in-out;
					border: 1px solid;
					border-color: var(--el-color-primary);
				}
				.layout-tips-warp {
					transition: all 0.3s ease-in-out;
					border-color: var(--el-color-primary);
					.layout-tips-box {
						transition: inherit;
						border-color: var(--el-color-primary);
						.layout-tips-txt {
							transition: inherit;
							color: var(--el-color-primary) !important;
							background-color: var(--next-color-seting-main) !important;
						}
					}
				}
			}
		}
	}
	.copy-config {
		margin: 10px 0;
		.copy-config-btn {
			width: 100%;
			margin-top: 15px;
		}
		.copy-config-btn-reset {
			width: 100%;
			margin: 10px 0 0;
		}
	}
}

.traditionalColors {
	height: 50px;
	position: relative;
}
.traditionalColors-chines {
	text-align: center;
}
.traditionalColors-circleCheck {
	position: absolute;
	left: calc(50% - 10px);
	top: calc(50% - 10px);
}
.horizontal-divider {
	.el-divider__text {
		width: 220px !important;
	}
}

:deep(#color-box) {
	.el-dialog__body {
		padding: 0 !important;
	}
}
</style>
