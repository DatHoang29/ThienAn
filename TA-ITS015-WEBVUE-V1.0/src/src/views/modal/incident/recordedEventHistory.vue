<template>
  <el-button v-if="getToken()" class="event-history-button" @click="openHistory">Lịch sử sự kiện</el-button>
  <el-dialog v-model="visible" title="Lịch sử sự kiện" width="min(1000px, 95vw)">
    <el-date-picker v-model="range" type="datetimerange" start-placeholder="Từ" end-placeholder="Đến" />
    <el-button :loading="loading" @click="page = 1; load()">Tìm kiếm</el-button>
    <el-alert v-if="error" :title="error" type="error" :closable="false" />
    <el-table :data="rows" v-loading="loading">
      <el-table-column prop="name" label="Sự kiện" min-width="260" />
      <el-table-column prop="startDate" label="Thời gian" min-width="180" />
      <el-table-column prop="location" label="Vị trí" />
      <el-table-column label="Trạng thái" min-width="120"><template #default="{ row }">{{ row.state === '5' ? 'Đã hủy' : row.state === 'recorded' ? 'Dữ liệu cũ, chờ chuyển đổi' : row.state }}</template></el-table-column>
      <el-table-column prop="remark" label="Ghi chú / lý do" min-width="260" />
      <el-table-column prop="description" label="Chi tiết" min-width="180" />
    </el-table>
    <el-pagination v-model:current-page="page" :page-size="50" :total="total" layout="prev, pager, next, total" @current-change="load" />
  </el-dialog>
</template>
<script setup lang="ts" name="tmsRecordedEventHistory">
import { ref, onUnmounted } from 'vue';
import { getAPI, getToken } from '/@/utils/axios-utils';
import { TmsRecordedEventApi } from '/@/api-services/tms';
import type { TmsIncident } from '/@/api-services/tms';
import { useTransporterIncident } from '/@/transporter/hooks/useTransporterIncident';

const visible = ref(false), loading = ref(false), error = ref('');
const rows = ref<TmsIncident[]>([]), page = ref(1), total = ref(0);
const range = ref<[Date, Date]>([new Date(new Date().setHours(0, 0, 0, 0)), new Date()]);
let requestId = 0;
// TmsIncident.StartDate uses server local time; send wall-clock strings without UTC conversion.
const localTime = (d: Date) => `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}T${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}:${String(d.getSeconds()).padStart(2,'0')}`;
async function load() {
  if (!getToken() || !range.value?.[0] || !range.value?.[1]) return;
  const id = ++requestId;
  loading.value = true;
  try {
    // from/to khai là Date trong client sinh ra; truyền chuỗi giờ máy chủ để JSON giữ nguyên, không đổi sang UTC.
    const response = await getAPI(TmsRecordedEventApi).apiTmsTmsrecordedeventPagePost({
      from: localTime(range.value[0]) as unknown as Date, to: localTime(range.value[1]) as unknown as Date, page: page.value, pageSize: 50,
    });
    if (id !== requestId) return;
    rows.value = response.data.result?.records ?? [];
    total.value = response.data.result?.total ?? 0;
    error.value = '';
  } catch {
    if (id === requestId) error.value = 'Không tải được lịch sử sự kiện.';
  } finally { if (id === requestId) loading.value = false; }
}
function openHistory() { visible.value = true; range.value[1] = new Date(); void load(); }
useTransporterIncident(data => {
  if (visible.value && data?.eventGroup === 'event') { range.value[1] = new Date(); void load(); }
});
onUnmounted(() => { ++requestId; });
</script>
<style scoped>
.event-history-button { position: fixed; bottom: 18px; left: 18px; z-index: 1900; }
</style>
