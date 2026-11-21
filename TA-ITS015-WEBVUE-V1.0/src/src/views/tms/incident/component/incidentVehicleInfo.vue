<template>
    <el-descriptions v-if="rows.length" :title="$t('lz.label.tms.vehicleInfo')" :column="2" border class="incident-vehicle-info">
        <el-descriptions-item v-for="row in rows" :key="row.key" :label="$t(row.label)">{{ row.value }}</el-descriptions-item>
    </el-descriptions>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();
import { incidentMetadataRows } from '/@/utils/incidentMetadata';
const props = defineProps<{ metadata?: string | null }>();
const rows = computed(() => incidentMetadataRows(props.metadata));
</script>

<style scoped>
.incident-vehicle-info { margin: 16px 0; }
</style>
