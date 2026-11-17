#!/usr/bin/env node

/**
 * Gortex Hook Launcher — ThienAn AI Harness
 * Gọi `gortex hook` trên macOS, Linux và Windows từ cùng một dòng lệnh trong settings.
 * Bỏ qua khi settings cấp người dùng (~/.claude/settings.json) đã đăng ký `gortex hook`
 * cho cùng sự kiện, để hook không chạy 2 lần. Đổi đường dẫn binary bằng biến GORTEX_BIN.
 */

import { spawn } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

const GORTEX_HOOK_COMMAND = /gortex(\.exe)?["']?\s+hook\b/i;

function readStdin() {
  return new Promise((resolve) => {
    const chunks = [];
    process.stdin.on('data', (chunk) => chunks.push(chunk));
    process.stdin.on('end', () => resolve(Buffer.concat(chunks)));
    process.stdin.on('error', () => resolve(Buffer.concat(chunks)));
  });
}

function readEventName(input) {
  try {
    return JSON.parse(input.toString('utf8')).hook_event_name ?? '';
  } catch {
    return '';
  }
}

function isHandledByUserSettings(eventName) {
  if (!eventName) return false;
  const configDir = process.env.CLAUDE_CONFIG_DIR || join(homedir(), '.claude');
  try {
    const settings = JSON.parse(readFileSync(join(configDir, 'settings.json'), 'utf8'));
    return (settings.hooks?.[eventName] ?? []).some((group) =>
      (group.hooks ?? []).some((hook) => GORTEX_HOOK_COMMAND.test(hook.command ?? '')));
  } catch {
    return false;
  }
}

function resolveGortexBinary() {
  if (process.env.GORTEX_BIN) return process.env.GORTEX_BIN;
  const isWindows = process.platform === 'win32';
  const candidates = isWindows
    ? [process.env.LOCALAPPDATA && join(process.env.LOCALAPPDATA, 'Programs', 'gortex', 'gortex.exe')]
    : ['/opt/homebrew/bin/gortex', '/usr/local/bin/gortex', join(homedir(), '.local', 'bin', 'gortex')];
  return candidates.find((path) => path && existsSync(path)) ?? (isWindows ? 'gortex.exe' : 'gortex');
}

const input = await readStdin();

if (isHandledByUserSettings(readEventName(input))) {
  process.exit(0);
}

const binary = resolveGortexBinary();
const child = spawn(binary, ['hook', ...process.argv.slice(2)], {
  stdio: ['pipe', 'inherit', 'inherit'],
  windowsHide: true
});

child.on('error', (error) => {
  process.stderr.write(`[gortex-hook] Không chạy được "${binary}": ${error.message}. Đặt biến GORTEX_BIN trỏ tới gortex.\n`);
  process.exit(1);
});
child.on('exit', (code) => process.exit(code ?? 1));
child.stdin.on('error', () => {});
child.stdin.end(input);
