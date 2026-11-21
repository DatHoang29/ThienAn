<template>
  <el-row wrap justify="end" class="lt-sm:w-200px workspace-actions">
    <slot name="prefix"></slot>
    <slot name="default">
      <el-button type="primary" icon="ele-Plus" @click="add" v-auth="authAdd" v-show="showAdd">     
        {{ $t('lz.button.base.add') }}
      </el-button>
      <el-button type="danger" v-show="showDelete" icon="ele-Delete" @click="batchDelete" v-auth="authDelete">        
        {{ $t('lz.button.base.batchDelete') }}
      </el-button>
    </slot>
    <slot name="suffix"></slot>
    <slot name="config">
      <el-tooltip raw-content effect="dark" :content="$t('lz.tooltip.base.refreshKeepFilter')" placement="top">
        <el-button class="workspace-config-button" size="small" icon="ele-Refresh" @click="refresh" :loading="loading" v-show="showRefresh">
          {{ $t('lz.button.base.refresh') }}
        </el-button>
      </el-tooltip>
      <el-button class="workspace-config-button" size="small" @click="customColumn" icon="ele-Setting" v-show="showCustomColumn">
        {{ $t('lz.button.base.columnSetting') }}
      </el-button>
    </slot>
    <slot name="postfix"></slot>
  </el-row>
</template>


<script setup lang="ts">
import {useI18n} from 'vue-i18n';
const { t: $t } = useI18n();
defineOptions({
  name: 'TableHeaderOperation'
});

interface Props {
  showDelete?: boolean;
  showAdd?: boolean;
  showRefresh?: boolean;
  showCustomColumn?: boolean;
  authAdd?: string;
  authDelete?: string;
  loading?: boolean;
}

// defineProps<Props>();

//Set default value to props
const props = withDefaults(defineProps<Props>(), {
  showDelete: false,
  showAdd: true,
  showRefresh: true,
  showCustomColumn: true,
});

interface Emits {
  (e: 'add'): void;
  (e: 'delete'): void;
  (e: 'refresh'): void;
  (e: 'customColumn'): void;
}

const emit = defineEmits<Emits>();

function add() {
  emit('add');
}

function batchDelete() {
   emit('delete');
}

function refresh() {
  emit('refresh');
}
function customColumn() {
  emit('customColumn');
}
</script>


<style scoped></style>
