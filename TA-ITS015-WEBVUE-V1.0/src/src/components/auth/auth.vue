<template>
	<slot v-if="getUserAuthBtnList" />
</template>

<script setup lang="ts" name="auth">
import { computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useUserInfo } from '/@/stores/userInfo';

// Define the value passed from the parent component
const props = defineProps({
	value: {
		type: String,
		default: () => '',
	},
});

// Define variable content
const stores = useUserInfo();
const { userInfos } = storeToRefs(stores);

// Get user permissions from pinia
const getUserAuthBtnList = computed(() => {
	return userInfos.value.authBtnList.some((v: string) => v === props.value);
});
</script>
