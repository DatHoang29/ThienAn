<template>
    <div class="metric-card">
        <span>{{ label }}</span>
        <strong>{{ value }}</strong>
    </div>
</template>

<script setup lang="ts">
defineProps<{
    label: string;
    value: string | number;
}>();
</script>

<style scoped lang="scss">
.metric-card {
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    background: #ffffff;
    padding: 18px;
    display: grid;
    gap: 8px;

    span {
        color: #667085;
        font-size: 13px;
    }

    strong {
        color: #101828;
        font-size: 30px;
        line-height: 34px;
    }
}
</style>
