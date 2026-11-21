<template>

  <el-col>
    <el-row justify="center">
      <slot name="prefix"></slot>
      <slot name="default">
      <el-button type="primary" icon="ele-Search" @click="search" v-auth="authSearch" :loading="loading" v-show="showSearch">
        {{ titleSearch ? titleSearch : $t('lz.button.base.search') }} </el-button>
      <slot name="suffix"></slot>
      <slot name="reset">
        <el-button icon="ele-Refresh" @click="reset" v-show="showReset"> {{ $t('lz.button.base.reset') }} </el-button>
      </slot>
      </slot>
      <slot name="postfix"></slot>
    </el-row>
  </el-col>
</template>

<script setup lang="ts">
import {useI18n} from 'vue-i18n';
const { t: $t } = useI18n();
defineOptions({
  name: 'SearchButtonOperation'
});

interface Props {
  authSearch?: string;
  loading?: boolean;
  showReset?: boolean;
  showSearch?: boolean;
  titleSearch?: string;
  titleReset?: string;
}

var props = withDefaults(defineProps<Props>(), {
  showReset: true,
  showSearch: true,
});
interface Emits {
  (e: 'search'): void;
  (e: 'reset'): void;
}

const emit = defineEmits<Emits>();

function search() {
  emit('search');
}

function reset() {
   emit('reset');
}

</script>



<style scoped></style>
