import { defineConfig } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = typeof import.meta.dirname !== 'undefined'
    ? import.meta.dirname
    : path.dirname(fileURLToPath(import.meta.url));

const VIEWS = path.resolve(__dirname, '../../TA-ITS015-WEBVUE-V1.0/src/src/views');

// Tên thư mục phải GIỐNG HỆT trên đĩa — 'videoWall' có chữ W HOA.
const MODULES = [
    { dir: 'sharedata', name: 'ShareData' },
    { dir: 'videoWall', name: 'VideoWall' },
    { dir: 'tms', name: 'TMS' },
] as const;

const absent = MODULES.filter((m) => !fs.existsSync(path.join(VIEWS, m.dir)));

// Tương đương Target ReportOptionalTestModules của test.csproj: nói rõ cái gì bị loại và vì sao.
if (absent.length)
    console.log(`[E2E MODULES] Loại khỏi chạy (không có thư mục views/): ${absent.map((m) => m.name).join(', ')}`);

export default defineConfig({
    testDir: './specs',
    testIgnore: absent.map((m) => `**/${m.dir}/**`),
    timeout: 60_000,
    fullyParallel: false,
    retries: 0,
    reporter: [['list']],
    use: {
        baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8888',
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
    },
});
