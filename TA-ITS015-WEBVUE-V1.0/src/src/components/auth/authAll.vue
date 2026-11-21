<template>
	<slot v-if="getUserAuthBtnList" />
</template>

<script setup lang="ts" name="authAll">
import { computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useUserInfo } from '/@/stores/userInfo';
import { judgementSameArr } from '/@/utils/arrayOperation';

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
	return judgementSameArr(props.value, userInfos.value.authBtnList);
});
</script>
