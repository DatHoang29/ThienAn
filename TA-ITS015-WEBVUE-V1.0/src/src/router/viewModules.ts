import type { Component } from 'vue';

// Shared by tab rendering and menu configuration validation. Vite loads views lazily.
export const viewModules = import.meta.glob<{ default: Component }>(['../views/**/*.{vue,tsx}', '!../views/system/old/**', '!../views/cfgSystem/old/**', '!../views/tms/old/**', '!../views/report/tms/old/**', '!../views/vms/old/**', '!../views/report/lcs/old/**', '!../views/report/vms/old/**', '!../views/report/fms/old/**', '!../views/sharedata/old/**']);
