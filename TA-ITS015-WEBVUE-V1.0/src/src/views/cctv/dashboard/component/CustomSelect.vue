<template>
    <div class="custom-select" :class="[rootClass, { 'is-floating': shouldFloat, 'is-disabled': props.disabled }]"
        :style="rootStyle">
        <el-select v-model="modelValue" v-bind="selectAttrs" :placeholder="selectPlaceholder" :disabled="props.disabled"
            size="small" @focus="isFocused = true" @blur="isFocused = false">
            <el-option v-for="option in props.options" :key="option.value" :label="option.label" :value="option.value"
                :disabled="option.disabled">
                <slot name="option" :option="option">
                    <span class="custom-option-label">{{ option.label }}</span>
                </slot>
            </el-option>
        </el-select>
        <label v-if="props.label" class="custom-label">{{ props.label }}</label>
    </div>
</template>

<script setup lang="ts">
defineOptions({
    inheritAttrs: false,
});

import { computed, ref, useAttrs } from 'vue';

interface CustomSelectOption {
    label?: string;
    value: string | number;
    disabled?: boolean;
    data?: unknown;
}
const modelValue = defineModel<string | number>({
    type: [String, Number],
    default: '',
});

const props = defineProps<{
    options: CustomSelectOption[];
    label?: string;
    placeholder?: string;
    disabled?: boolean;
}>();

const attrs = useAttrs();
const isFocused = ref(false);
const hasValue = computed(() => modelValue.value !== '' && modelValue.value !== null && modelValue.value !== undefined);
const hasPlaceholder = computed(() => Boolean(props.placeholder && props.placeholder.trim()));
const shouldFloat = computed(() => hasPlaceholder.value || isFocused.value || hasValue.value);
const selectPlaceholder = computed(() => props.placeholder ?? '');
const rootClass = computed(() => attrs.class);
const rootStyle = computed(() => attrs.style);
const selectAttrs = computed(() => Object.fromEntries(Object.entries(attrs).filter(([key]) => key !== 'class' && key !== 'style')));



</script>
<style scoped lang="scss">
.custom-select {
    position: relative;

    :deep(.el-select) {
        width: 100%;
    }
}

.custom-label {
    position: absolute;
    left: 12px;
    top: 50%;
    transform: translateY(-50%);
    transform-origin: left center;
    padding: 0 6px;
    color: var(--el-text-color-placeholder);
    background: var(--el-bg-color);
    font-size: 14px;
    line-height: 1;
    pointer-events: none;
    transition: all 0.2s ease;
}

.custom-option-label {
    display: block;
    width: 100%;
}

.custom-select.is-floating .custom-label {
    top: 0;
    transform: translateY(-50%) scale(0.86);
    color: var(--el-color-primary);
}

.custom-select.is-disabled .custom-label {
    color: var(--el-disabled-text-color);
}

.el-select-dropdown__item {
    height: 100%;
}
</style>