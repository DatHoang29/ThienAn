#!/usr/bin/env node

// Lọc output lệnh shell qua rtk cho Codex, Gemini CLI mà không để rtk vượt qua bộ chặn của dự án:
// chạy guardrails và validate-tool-call trước, lệnh bị cấm thì chặn luôn (exit 2); chỉ lệnh shell hợp lệ mới chuyển
// cho `rtk hook <codex|gemini>` (rtk trả quyết định "allow"/"ask_user" kèm lệnh đã viết lại).
// Tool không phải shell không chuyển cho rtk; máy chưa cài rtk thì cho qua, không in gì.

import {spawnSync} from 'node:child_process';
import process from 'node:process';

import {evaluateGuardrail} from './guardrails.mjs';
import {evaluateCommand, extractCommand} from './validate-tool-call.mjs';

const SHELL_TOOLS = new Set(['Bash', 'run_shell_command']);

function readStdin() {
  return new Promise((resolve, reject) => {
    let input = '';
    process.stdin.setEncoding('utf8');
    process.stdin.on('data', chunk => {
      input += chunk;
    });
    process.stdin.on('end', () => resolve(input));
    process.stdin.on('error', reject);
  });
}

async function main() {
  const agent = process.argv[2];
  const raw = await readStdin();

  let payload;
  try {
    payload = JSON.parse(raw || '{}');
  } catch {
    return 0;
  }

  const guard = evaluateGuardrail(payload);
  if (!guard.allowed) {
    console.error(`\n⛔ BLOCKED by ThienAn Guardrails (${guard.rule}):\n${guard.reason}\n`);
    return 2;
  }
  if (!SHELL_TOOLS.has(payload.tool_name)) return 0;

  const check = evaluateCommand(extractCommand(payload));
  if (!check.allowed) {
    console.error(`BLOCKED by ThienAn (${check.rule}): ${check.reason}.`);
    return 2;
  }

  // Chưa cài rtk hoặc rtk lỗi: cho qua, không viết lại lệnh.
  const rtk = spawnSync('rtk', ['hook', agent], {input: raw, encoding: 'utf8'});
  if (rtk.error || rtk.status !== 0) return 0;
  process.stdout.write(rtk.stdout);
  return 0;
}

process.exitCode = await main();
