<template>
	<slot v-if="getUserAuthBtnList" />
</template>

<script setup lang="ts" name="auths">
import { computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useUserInfo } from '/@/stores/userInfo';

// Define the value passed from the parent component
const props = defineProps({
	value: {
		type: Array,
		default: () => [],
	},
});

// Define variable content
const stores = useUserInfo();
const { userInfos } = storeToRefs(stores);

// Get user permissions from pinia
const getUserAuthBtnList = computed(() => {
	let flag = false;
	userInfos.value.authBtnList.map((val: string) => {
		props.value.map((v) => {
			if (val === v) flag = true;
		});
	});
	return flag;
});
</script>
