<template>
    <el-tag v-if="configItem" :type="configItem.tagType" :style="configItem.styleSetting" :class="configItem.className">
        {{ configItem.name }}
    </el-tag>
</template>

<script setup lang="ts">

interface Props {
    configItem: any;
}

defineProps<Props>();

</script>
  
  
