<template>
	<template v-for="val in chils">
		<el-sub-menu :index="val.path" :key="val.path" v-if="val.children && val.children.length > 0">
		<template #title>
			<SvgIcon :name="val.meta.icon" />
			<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" :isShow="true" />
			<!-- <div class="auto-scroll-horizontal">
				<span class="scroll-content-horizontal">{{ $t(`lz.router.${val.name}`, val.meta.title) }}</span>
			</div> -->
			<!-- <span class="menu-title">{{ $t(`lz.router.${val.name}`, val.meta.title) }}</span> -->
			</template>
			<sub-item :chil="val.children" />
		</el-sub-menu>
		<template v-else>
			<el-menu-item :index="val.path" :key="val.path">
				<!-- check val.meta.isLink string is has value -->
				<template v-if="!isHasValue(val.meta.isLink) || (isHasValue(val.meta.isLink) && (val.meta.isIframe))" >
					<SvgIcon :name="val.meta.icon" :class="val.type === 'menu' ? 'menu-icon' : ''"/>
					<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" :isShow="true" />
					<!-- <div class="auto-scroll-horizontal">
						<span class="scroll-content-horizontal">{{ $t(`lz.router.${val.name}`, val.meta.title) }}</span>
					</div> -->
					<!-- <span class="menu-title">{{ $t(`lz.router.${val.name}`, val.meta.title) }}</span> -->
				</template>
				<template v-else>
					<a class="w100" @click.prevent="onALinkClick(val)" style="display:contents">
						<SvgIcon :name="val.meta.icon" :class="val.type === 'menu' ? 'menu-icon' : 'menu-icon-sub'"/>
						<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" :isShow="true" />
						<!-- <div class="auto-scroll-horizontal">
							<span class="scroll-content-horizontal">{{ $t(`lz.router.${val.name}`, val.meta.title) }}</span>
						</div> -->
						<!-- <span class="menu-title">{{ $t(`lz.router.${val.name}`, val.meta.title) }}</span> -->
					</a>
				</template>
			</el-menu-item>
		</template>
	</template>
</template>

<script setup lang="ts" name="navMenuSubItem">
import { computed, defineAsyncComponent } from 'vue';
import { RouteRecordRaw } from 'vue-router';
import other from '/@/utils/other';
// import AutoScrollText from '/@/components/advanced/auto-scroll-text.vue';
const AutoScrollText = defineAsyncComponent(() => import('/@/components/advanced/auto-scroll-text.vue'));
// Define props passed from the parent component
const props = defineProps({
  // Menu list
  chil: {
    type: Array<RouteRecordRaw>,
    default: () => [],
  },
});

// Get child menu data from props
const chils = computed(() => {
  return <RouteItems>props.chil;
});

const isHasValue = (val: string) => {
  return val && val.length > 0;
};

// Handle opening external links
const onALinkClick = (val: RouteItem) => {
	  other.handleOpenLink(val);
};
</script>

<style scoped>

.menu-icon {
  border-radius: 50%;
  background: var(--el-color-primary);
  color: var(--el-color-white)!important;
  font-size: 12px!important;
  width: 25px!important;
  height: 25px!important;
  padding: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  margin-right: 3px;
}




.fa {
	padding-top: 6px;
	font-size: 12px!important;
}

.menu-title {
	overflow: auto;
	display: inline-block;
	width: 80%;
}

.auto-scroll-horizontal {
	width: 80%;
	overflow: hidden;
	white-space: nowrap;
	position: relative;
}

.scroll-content-horizontal {
	display: inline-block;
	padding-left: 10%;
	animation: scroll-left 15s linear infinite;
}

@keyframes scroll-left {
	0% { transform: translateX(0); }
	100% { transform: translateX(-100%); }
}
</style>
