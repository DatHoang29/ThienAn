<template>
  <div class="year-grid-view">
    <div class="months-grid">
      <div
        v-for="month in 12"
        :key="month"
        class="month-card"
      >
        <div class="month-header" @click="handleMonthClick(month)">
          <span class="month-name">{{ getMonthName(month) }}</span>
        </div>
        <div class="month-calendar">
          <div class="weekdays">
            <span v-for="day in weekDays" :key="day" class="weekday">{{ day }}</span>
          </div>
          <div class="days-grid">
            <div
              v-for="day in getDaysInMonth(month)"
              :key="`${month}-${day.date}`"
              :class="['day', day.class]"
            >
              <div class="day-content">
                <span class="day-number">{{ day.date }}</span>
                <div v-if="day.dayEvents && day.dayEvents.length > 0" class="day-events">
                  <div
                    v-for="event in day.dayEvents.slice(0, 1)"
                    :key="event.id"
                    :class="['event-item', getEventClass(event)]"
                    :style="{ backgroundColor: getEventColor(event) }"
                    @click.stop="handleEventClick(event)"
                    :title="event.name || $t('lz.label.tmsDutySchedule.noName')"
                  >
                    <span class="event-title">{{ truncateText(event.name, 10) }}</span>
                  </div>
                  <div
                    v-if="day.dayEvents.length > 1"
                    class="event-more"
                    @click.stop="handleMoreClick(day.dayEvents, year, month, day.date)"
                  >
                    +{{ day.dayEvents.length - 1 }} {{ $t('lz.label.tmsDutySchedule.more') }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Events List Dialog -->
    <el-dialog
      v-model="state.dialogVisible"
      :title="state.dialogTitle"
      width="700px"
      draggable
      :close-on-click-modal="false"
    >
      <template #header>
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 8px;">
            <ele-Calendar />
          </el-icon>
          <span>{{ state.dialogTitle }}</span>
        </div>
      </template>

      <div class="events-list">
        <div
          v-for="(event, index) in state.dialogEvents"
          :key="event.id"
          class="event-list-item"
          @click="handleEventClick(event)"
        >
          <div class="event-number">{{ index + 1 }}</div>
          <div class="event-indicator" :style="{ backgroundColor: getEventColor(event) }"></div>
          <div class="event-info">
            <div class="event-header">
              <div class="event-name">
                <el-icon class="name-icon"><ele-Document /></el-icon>
                {{ event.name || $t('lz.label.tmsDutySchedule.noName') }}
              </div>
              <el-tag :type="getStatusTagType(event.status)" size="small" effect="dark">
                {{ getStatusLabel(event.status) }}
              </el-tag>
            </div>
            <div class="event-details">
              <div class="event-time">
                <el-icon class="time-icon"><ele-Clock /></el-icon>
                <span>{{ formatEventTime(event.startTime) }} - {{ formatEventTime(event.endTime) }}</span>
              </div>
              <div v-if="event.creatorName" class="event-creator">
                <el-icon class="creator-icon"><ele-User /></el-icon>
                <span>{{ event.creatorName }}</span>
              </div>
            </div>
          </div>
          <el-icon class="view-icon"><ele-View /></el-icon>
        </div>
        <div v-if="state.dialogEvents.length === 0" class="empty-state">
          <el-icon size="48" color="#909399"><ele-FolderOpened /></el-icon>
          <p>{{ $t('lz.label.tmsDutySchedule.noEvents') }}</p>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup name="yearGridView">
import { computed, watch, reactive } from 'vue';
import { useI18n } from 'vue-i18n';

const { t: $t } = useI18n();

const props = defineProps<{
  year: number;
  events: any[];
}>();

const emit = defineEmits(['month-click', 'day-click', 'event-click']);

const state = reactive({
  dialogVisible: false,
  dialogTitle: '',
  dialogEvents: [] as any[],
});

// Watch events prop for debugging
watch(() => props.events, (newEvents) => {
  console.log('YearGridView - events changed, count:', newEvents?.length || 0);
  if (newEvents && newEvents.length > 0) {
    console.log('YearGridView - first event sample:', JSON.stringify(newEvents[0], null, 2));
  }
}, { immediate: true, deep: true });

const currentYear = computed(() => props.year);
const weekDays = computed(() => [
  $t('lz.label.tmsDutySchedule.weekDayMon'),
  $t('lz.label.tmsDutySchedule.weekDayTue'),
  $t('lz.label.tmsDutySchedule.weekDayWed'),
  $t('lz.label.tmsDutySchedule.weekDayThu'),
  $t('lz.label.tmsDutySchedule.weekDayFri'),
  $t('lz.label.tmsDutySchedule.weekDaySat'),
  $t('lz.label.tmsDutySchedule.weekDaySun')
]);

const monthNames = computed(() => [
  $t('lz.label.tmsDutySchedule.month1'), $t('lz.label.tmsDutySchedule.month2'),
  $t('lz.label.tmsDutySchedule.month3'), $t('lz.label.tmsDutySchedule.month4'),
  $t('lz.label.tmsDutySchedule.month5'), $t('lz.label.tmsDutySchedule.month6'),
  $t('lz.label.tmsDutySchedule.month7'), $t('lz.label.tmsDutySchedule.month8'),
  $t('lz.label.tmsDutySchedule.month9'), $t('lz.label.tmsDutySchedule.month10'),
  $t('lz.label.tmsDutySchedule.month11'), $t('lz.label.tmsDutySchedule.month12')
]);

const getMonthName = (month: number) => {
  return monthNames.value[month - 1];
};

// Get events for a specific day
const getEventsForDay = (year: number, month: number, day: number) => {
  if (!props.events || props.events.length === 0) {
    return [];
  }
  
  const checkDateStart = new Date(year, month - 1, day, 0, 0, 0);
  const checkDateEnd = new Date(year, month - 1, day, 23, 59, 59);
  
  return props.events.filter(event => {
    if (!event.startTime && !event.start) return false;
    
    // Support both startTime/endTime (from API) and start/end (from calendar events)
    const eventStart = new Date(event.startTime || event.start);
    const eventEnd = new Date(event.endTime || event.end);
    
    // Check if event overlaps with this day
    return eventStart <= checkDateEnd && eventEnd >= checkDateStart;
  });
};

const getDaysInMonth = (month: number) => {
  const year = props.year;
  const firstDay = new Date(year, month - 1, 1);
  const lastDay = new Date(year, month, 0);
  const daysInMonth = lastDay.getDate();
  
  // Get day of week (0 = Sunday, 1 = Monday, etc.)
  let firstDayOfWeek = firstDay.getDay();
  // Convert to Monday = 0, Sunday = 6
  firstDayOfWeek = firstDayOfWeek === 0 ? 6 : firstDayOfWeek - 1;
  
  const days: any[] = [];
  
  // Add empty cells for days before month starts
  for (let i = 0; i < firstDayOfWeek; i++) {
    days.push({ date: '', class: 'empty', hasEvents: false, dayEvents: [] });
  }
  
  // Add days of month
  const today = new Date();
  const isCurrentYear = year === today.getFullYear();
  const isCurrentMonth = month === today.getMonth() + 1;
  const currentDate = today.getDate();
  
  for (let day = 1; day <= daysInMonth; day++) {
    const isToday = isCurrentYear && isCurrentMonth && day === currentDate;
    const dayEvents = getEventsForDay(year, month, day);
    const hasEvents = dayEvents.length > 0;
    
    days.push({
      date: day,
      class: isToday ? 'today' : (hasEvents ? 'has-events' : ''),
      hasEvents: hasEvents,
      dayEvents: dayEvents
    });
  }
  
  return days;
};

// Get event color based on status - matching main calendar colors
const getEventColor = (event: any) => {
  const status: any = event.status;
  
  // Map status to colors (matching BaseConfig TmsDutyState)
  // register -> info (grey), confirm -> primary (blue), process -> warning (orange)
  // finish -> success (green), cancel -> danger (red)
  const statusColorMap: Record<string, string> = {
    'register': 'var(--el-color-info)',
    'confirm': 'var(--el-color-primary)',
    'process': 'var(--el-color-warning)',
    'finish': 'var(--el-color-success)',
    'cancel': 'var(--el-color-danger)'
  };
  
  return statusColorMap[status?.toLowerCase()] || 'var(--el-color-info)';
};

// Get event class based on status
const getEventClass = (event: any) => {
  const status: any = event.status;
  if (status === 'Registered') return 'status-registered';
  if (status === 'Confirmed') return 'status-confirmed';
  if (status === 'InProgress') return 'status-inprogress';
  if (status === 'Completed') return 'status-completed';
  if (status === 'Cancelled') return 'status-cancelled';
  return 'status-registered'; // default
};

// Truncate text
const truncateText = (text: string, maxLength: number) => {
  if (!text) return '';
  return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
};

const handleMonthClick = (month: number) => {
  emit('month-click', { year: props.year, month });
};

const handleDayClick = (year: number, month: number, day: number) => {
  const events = getEventsForDay(year, month, day);
  emit('day-click', { year, month, day, events });
};

const handleEventClick = (event: any) => {
  console.log('YearGridView - event clicked:', event);
  state.dialogVisible = false;
  emit('event-click', event);
};

const handleMoreClick = (events: any[], year: number, month: number, day: number) => {
  state.dialogEvents = events;
  state.dialogTitle = `${$t('lz.label.tmsDutySchedule.scheduleOnDate')} ${day}/${month}/${year} (${events.length} ${$t('lz.label.tmsDutySchedule.events')})`;
  state.dialogVisible = true;
};

const formatEventTime = (time: any) => {
  if (!time) return '';
  const d = new Date(time);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
};

const getStatusLabel = (status: any) => {
  if (status === 'Registered') return $t('lz.label.tmsDutySchedule.statusRegistered');
  if (status === 'Confirmed') return $t('lz.label.tmsDutySchedule.statusConfirmed');
  if (status === 'InProgress') return $t('lz.label.tmsDutySchedule.statusInProgress');
  if (status === 'Completed') return $t('lz.label.tmsDutySchedule.statusCompleted');
  if (status === 'Cancelled') return $t('lz.label.tmsDutySchedule.statusCancelled');
  return status || $t('lz.label.base.unknown');
};

const getStatusTagType = (status: any) => {
  if (status === 'Registered') return 'info';
  if (status === 'Confirmed') return 'primary';
  if (status === 'InProgress') return 'warning';
  if (status === 'Completed') return 'success';
  if (status === 'Cancelled') return 'danger';
  return 'info';
};
</script>

<style lang="scss" scoped>
.year-grid-view {
  width: 100%;
  height: 100%;
  overflow: auto;
  padding: 20px;
}

.year-header {
  text-align: center;
  margin-bottom: 30px;
  
  h2 {
    font-size: 32px;
    font-weight: 600;
    color: var(--el-text-color-primary);
    margin: 0;
  }
}

.months-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 20px;
  max-width: 1600px;
  margin: 0 auto;
}

.month-card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color);
  border-radius: 8px;
  padding: 15px;
  transition: all 0.3s ease;
  
  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    transform: translateY(-2px);
    border-color: var(--el-color-primary);
  }
}

.month-header {
  text-align: center;
  margin-bottom: 12px;
  padding-bottom: 10px;
  border-bottom: 2px solid var(--el-color-primary);
  cursor: pointer;
  
  &:hover {
    .month-name {
      color: var(--el-color-primary-dark-2);
    }
  }
  
  .month-name {
    font-size: 16px;
    font-weight: 600;
    color: var(--el-color-primary);
    transition: color 0.2s ease;
  }
}

.month-calendar {
  .weekdays {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 2px;
    margin-bottom: 4px;
    
    .weekday {
      text-align: center;
      font-size: 11px;
      font-weight: 600;
      color: var(--el-text-color-secondary);
      padding: 4px 0;
    }
  }
  
  .days-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 2px;
    
    .day {
      min-height: 60px;
      display: flex;
      flex-direction: column;
      border-radius: 4px;
      position: relative;
      transition: all 0.2s ease;
      background: var(--el-fill-color-blank);
      border: 1px solid var(--el-border-color-lighter);
      overflow: hidden;
      
      &.empty {
        visibility: hidden;
      }
      
      &:hover:not(.empty) {
        border-color: var(--el-color-primary);
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        z-index: 1;
      }
      
      &.today {
        border: 2px solid var(--el-color-primary);
        
        .day-number {
          background: var(--el-color-primary);
          color: white;
          font-weight: 600;
        }
      }
      
      &.has-events:not(.today) {
        background: var(--el-color-primary-light-9);
      }
      
      .day-content {
        display: flex;
        flex-direction: column;
        height: 100%;
        width: 100%;
      }
      
      .day-number {
        font-size: 11px;
        padding: 2px 4px;
        text-align: center;
        line-height: 1.2;
        flex-shrink: 0;
        color: var(--el-text-color-primary);
      }
      
      .day-events {
        display: flex;
        flex-direction: column;
        gap: 1px;
        padding: 2px;
        flex: 1;
        overflow: hidden;
        
        .event-item {
          font-size: 9px;
          padding: 1px 3px;
          border-radius: 2px;
          cursor: pointer;
          transition: all 0.2s ease;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
          color: white;
          line-height: 1.4;
          
          &:hover {
            transform: scale(1.05);
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
            z-index: 10;
            overflow: visible;
            white-space: normal;
          }
          
          .event-title {
            font-weight: 500;
          }
          
          &.status-registered {
            background-color: var(--el-color-info); // info - grey
          }
          
          &.status-confirmed {
            background-color: var(--el-color-primary); // primary - blue
          }
          
          &.status-inprogress {
            background-color: var(--el-color-warning); // warning - orange
          }
          
          &.status-completed {
            background-color: var(--el-color-success); // success - green
          }
          
          &.status-cancelled {
            background-color: var(--el-color-danger); // danger - red
          }
        }
        
        .event-more {
          font-size: 9px;
          padding: 1px 3px;
          color: var(--el-color-primary);
          cursor: pointer;
          text-align: center;
          font-weight: 600;
          border-radius: 2px;
          background: var(--el-color-primary-light-9);
          
          &:hover {
            background: var(--el-color-primary-light-8);
            color: var(--el-color-primary-dark-2);
          }
        }
      }
    }
  }
}

.events-list {
  max-height: 500px;
  overflow-y: auto;
  padding: 4px;
}

.event-list-item {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  margin-bottom: 12px;
  cursor: pointer;
  transition: all 0.3s ease;
  background: var(--el-fill-color-blank);
  position: relative;
  overflow: hidden;

  &::before {
    content: '';
    position: absolute;
    left: 0;
    top: 0;
    width: 4px;
    height: 100%;
    background: linear-gradient(180deg, var(--el-color-primary), var(--el-color-primary-light-3));
    opacity: 0;
    transition: opacity 0.3s ease;
  }

  &:hover {
    background: linear-gradient(90deg, var(--el-fill-color-light), var(--el-fill-color-blank));
    border-color: var(--el-color-primary);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    transform: translateY(-2px);

    &::before {
      opacity: 1;
    }

    .view-icon {
      opacity: 1;
      transform: translateX(0);
    }
  }

  .event-number {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--el-color-primary-light-5), var(--el-color-primary-light-7));
    color: var(--el-color-primary);
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 600;
    font-size: 14px;
    flex-shrink: 0;
  }

  .event-indicator {
    width: 5px;
    height: 55px;
    border-radius: 3px;
    flex-shrink: 0;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  }

  .event-info {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 8px;

    .event-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;

      .event-name {
        font-weight: 600;
        font-size: 15px;
        color: var(--el-text-color-primary);
        display: flex;
        align-items: center;
        gap: 8px;
        flex: 1;
        min-width: 0;

        .name-icon {
          color: var(--el-color-primary);
          flex-shrink: 0;
        }
      }
    }

    .event-details {
      display: flex;
      flex-direction: column;
      gap: 6px;

      .event-time,
      .event-creator {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 13px;
        color: var(--el-text-color-secondary);

        .time-icon,
        .creator-icon {
          font-size: 14px;
          color: var(--el-text-color-placeholder);
        }
      }
    }
  }

  .view-icon {
    font-size: 20px;
    color: var(--el-color-primary);
    opacity: 0;
    transform: translateX(-8px);
    transition: all 0.3s ease;
    flex-shrink: 0;
  }
}

.empty-state {
  text-align: center;
  padding: 60px 20px;
  color: var(--el-text-color-secondary);

  p {
    margin-top: 16px;
    font-size: 14px;
  }
}

@media (max-width: 1200px) {
  .months-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

@media (max-width: 768px) {
  .months-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  
  .year-grid-view {
    padding: 10px;
  }
}

@media (max-width: 480px) {
  .months-grid {
    grid-template-columns: 1fr;
  }
}
</style>