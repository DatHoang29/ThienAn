#!/usr/bin/env node

// Bộ chặn của dự án cho Codex và Antigravity, đủ như Claude Code: guardrails, validate-tool-call, gortex-enforce;
// lệnh shell hợp lệ mới chuyển cho `rtk hook <codex|antigravity>` để lọc output.
// - Codex: chặn bằng exit 2; rtk trả "allow" kèm lệnh đã viết lại (Codex chỉ nhận lệnh viết lại khi "allow").
// - Antigravity: đọc quyết định JSON trên stdout ("deny" chặn, "ask" để Antigravity xử lý quyền như bình thường);
//   rtk trả "allow" cho mọi lệnh (tự chạy, không hỏi quyền) nên chỉ lấy lệnh đã viết lại, quyết định vẫn là "ask".
// Tool không phải shell không chuyển cho rtk; máy chưa cài rtk thì cho qua, không viết lại lệnh.

import {spawnSync} from 'node:child_process';
import path from 'node:path';
import process from 'node:process';

import {evaluate as evaluateRead} from './gortex-enforce.mjs';
import {evaluateGuardrail} from './guardrails.mjs';
import {evaluateCommand, extractCommand} from './validate-tool-call.mjs';

const SHELL_TOOLS = new Set(['Bash', 'run_command']);
// Antigravity: tool ghi tệp và chạy lệnh đi qua guardrails; tool đọc, tìm chỉ xét gortex-enforce.
const ANTIGRAVITY_GUARDED = new Set(['run_command', 'write_to_file', 'replace_file_content', 'multi_replace_file_content', 'edit_file']);
const GORTEX_HINT = 'Dùng Gortex MCP thay thế: explore (tìm vị trí, nhiệm vụ), search (symbol, chuỗi), read (symbol, cả file), relations (ai gọi, dùng symbol). Gortex hỏng mới được báo lỗi tích hợp, không tự dùng shell (.agents/rules/gortex-tool-map.md).';
const ANTIGRAVITY_PASS = {decision: 'ask'};

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

// Antigravity gửi {toolCall: {name, args}}: đổi về dạng {tool_name, tool_args} mà guardrails, validate-tool-call đọc.
function toolCallOf(agent, payload) {
  if (agent !== 'antigravity') return payload;
  return {tool_name: payload.toolCall?.name ?? '', tool_args: payload.toolCall?.args ?? {}};
}

// gortex-enforce xét theo tool Read, Grep, Glob, Bash của Claude Code: đổi tool đọc, tìm của Antigravity sang dạng đó.
function readCallOf(call) {
  const args = call.tool_args ?? {};
  const list = value => [value].flat().filter(Boolean);
  switch (call.tool_name) {
    case 'run_command':
      return {tool_name: 'Bash', tool_input: {command: args.CommandLine}};
    case 'read_file':
    case 'view_file':
    case 'view_file_outline':
      return {tool_name: 'Read', tool_input: {file_path: args.Path ?? args.AbsolutePath}};
    case 'view_code_item':
      return {tool_name: 'Read', tool_input: {file_path: args.File}};
    case 'grep_search': {
      // Tìm trong một tệp mà không lọc: lấy đường dẫn tệp làm bộ lọc để tài liệu được cho qua như Grep có glob.
      const includes = list(args.Includes).join(' ');
      const file = path.extname(args.SearchPath ?? '') ? args.SearchPath : '';
      return {tool_name: 'Grep', tool_input: {path: args.SearchPath, glob: includes || file}};
    }
    case 'find_by_name': {
      const pattern = [args.Pattern, ...list(args.Extensions).map(extension => `*.${extension}`)].filter(Boolean).join(' ');
      return {tool_name: 'Glob', tool_input: {path: args.SearchDirectory, pattern}};
    }
    case 'codebase_search':
      return {tool_name: 'Grep', tool_input: {path: list(args.TargetDirectories)[0]}};
    default:
      return call;
  }
}

function blockReason(agent, call) {
  if (agent !== 'antigravity' || ANTIGRAVITY_GUARDED.has(call.tool_name)) {
    const guard = evaluateGuardrail(call);
    if (!guard.allowed) return `⛔ BLOCKED by ThienAn Guardrails (${guard.rule}):\n${guard.reason}`;
  }
  if (SHELL_TOOLS.has(call.tool_name)) {
    const check = evaluateCommand(extractCommand(call));
    if (!check.allowed) return `BLOCKED by ThienAn (${check.rule}): ${check.reason}.`;
  }
  const read = process.env.GORTEX_ENFORCE === 'off' ? null : evaluateRead(readCallOf(call));
  return read ? `BLOCKED by ThienAn (gortex-enforce): ${read}\n${GORTEX_HINT}` : null;
}

function antigravityDecision(call, raw) {
  if (!SHELL_TOOLS.has(call.tool_name)) return ANTIGRAVITY_PASS;
  const rtk = spawnSync('rtk', ['hook', 'antigravity'], {input: raw, encoding: 'utf8'});
  if (rtk.error || rtk.status !== 0) return ANTIGRAVITY_PASS;
  try {
    const {overwrite, reason} = JSON.parse(rtk.stdout);
    return overwrite ? {decision: 'ask', reason, overwrite} : ANTIGRAVITY_PASS;
  } catch {
    return ANTIGRAVITY_PASS;
  }
}

async function main() {
  const agent = process.argv[2];
  const raw = await readStdin();

  let payload;
  try {
    payload = JSON.parse(raw || '{}');
  } catch {
    if (agent === 'antigravity') process.stdout.write(`${JSON.stringify(ANTIGRAVITY_PASS)}\n`);
    return 0;
  }

  const call = toolCallOf(agent, payload);
  const reason = blockReason(agent, call);
  if (agent === 'antigravity') {
    const decision = reason ? {decision: 'deny', reason} : antigravityDecision(call, raw);
    process.stdout.write(`${JSON.stringify(decision)}\n`);
    return 0;
  }
  if (reason) {
    console.error(`\n${reason}\n`);
    return 2;
  }
  if (!SHELL_TOOLS.has(call.tool_name)) return 0;

  // Chưa cài rtk hoặc rtk lỗi: cho qua, không viết lại lệnh.
  const rtk = spawnSync('rtk', ['hook', agent], {input: raw, encoding: 'utf8'});
  if (rtk.error || rtk.status !== 0) return 0;
  process.stdout.write(rtk.stdout);
  return 0;
}

process.exitCode = await main();
