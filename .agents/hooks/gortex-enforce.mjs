#!/usr/bin/env node

/**
 * Gortex Enforcement Hook — ThienAn AI Harness
 * - `pre-tool-use`: chặn (exit 2) Read/Grep/Glob/Bash đọc mã nguồn đã index, chỉ tới tool Gortex tương ứng.
 * Codex, Antigravity dùng `evaluate` qua guarded-rtk.mjs (tool đọc, tìm của Antigravity đổi sang Read/Grep/Glob/Bash).
 * Tắt tạm thời: đặt biến môi trường GORTEX_ENFORCE=off (vd khi daemon Gortex không chạy).
 */

import process from 'node:process';

const CODE_EXT = /\.(?:cs|vue|tsx?|jsx?|mjs|cjs)$/i;
const CODE_EXT_IN_TEXT = /\.(?:cs|vue|tsx?|jsx?|mjs|cjs)(?=$|[\s"'`:;)|>])/i;
const ALLOWED_PATH = /(?:^|\/)(?:\.claude\/plans|\.agents\/hooks|node_modules|bin|obj|dist|\.git)\//i;
const SCRATCH_PATH = /^(?:\/private)?\/tmp\/|\/scratchpad\//i;
const READ_FIRST = /^(?:[A-Za-z_]+=\S+\s+)*(?:cat|head|tail|less|more|grep|egrep|rg|ag|sed|awk|find|nl|bat)\s/;
const READ_CMD =/(?:^|[\s;&|(])(?:cat|head|tail|less|more|grep|egrep|rg|ag|sed|awk|find|nl|bat)\s/;

const TOOL_HINT = [
  'Dùng Gortex thay thế (nạp schema trước bằng ToolSearch nếu chưa có):',
  '  • tìm vị trí/nhiệm vụ  → mcp__gortex__explore',
  '  • tìm symbol, chuỗi     → mcp__gortex__search (operation: symbols | text)',
  '  • đọc symbol, cả file   → mcp__gortex__read (target: symbol | file)',
  '  • ai gọi/dùng symbol    → mcp__gortex__relations (operation: usages | callers)',
  'Gortex hỏng mới được báo lỗi tích hợp, không tự dùng shell (.agents/rules/gortex-tool-map.md).'
].join('\n');

function readStdin() {
  return new Promise((resolve, reject) => {
    let input = '';
    process.stdin.setEncoding('utf8');
    process.stdin.on('data', chunk => {
      input += chunk;
      if (input.length > 1024 * 1024) reject(new Error('hook payload exceeds 1 MiB'));
    });
    process.stdin.on('end', () => resolve(input));
    process.stdin.on('error', reject);
  });
}

function normalize(p) {
  return String(p ?? '').replace(/\\/g, '/');
}

function isExemptPath(p) {
  const n = normalize(p);
  return ALLOWED_PATH.test(n) || SCRATCH_PATH.test(n);
}

export function evaluate(payload) {
  const tool = payload?.tool_name ?? '';
  const input = payload?.tool_input ?? {};

  if (tool === 'Read') {
    const file = normalize(input.file_path);
    if (CODE_EXT.test(file) && !isExemptPath(file)) return `Read(${file}) bị chặn: file mã nguồn đã được Gortex index.`;
    return null;
  }

  if (tool === 'Grep' || tool === 'Glob') {
    const target = normalize(input.path);
    if (target && isExemptPath(target)) return null;
    const filter = normalize(input.glob ?? input.pattern ?? '');
    const type = String(input.type ?? '');
    const codeFilter = CODE_EXT_IN_TEXT.test(filter) || /^(?:cs|ts|js|vue|tsx|jsx)$/i.test(type);
    const docFilter = /\.(?:md|json|sql|ya?ml|csproj|sln|txt|config)$/i.test(filter);
    if (docFilter && !codeFilter) return null;
    if (tool === 'Grep' && !filter && !type) return `Grep bị chặn: tìm kiếm không giới hạn trên cây mã đã index.`;
    if (codeFilter || tool === 'Grep') return `${tool} bị chặn: tìm trong mã nguồn đã index.`;
    return null;
  }

  if (tool === 'Bash') {
    const cmd = String(input.command ?? '');
    // Xét từng đoạn của pipeline: chỉ chặn khi chính lệnh đọc nhận file mã làm đối số
    // (vd `tail -15` trong `node --test x.mjs | tail -15` không bị tính).
    for (const segment of cmd.split(/[;&|]+/)) {
      if (READ_CMD.test(` ${segment.trim()} `) && READ_FIRST.test(segment.trim())
        && CODE_EXT_IN_TEXT.test(segment) && !isExemptPath(segment)) {
        return `Bash đọc mã nguồn bị chặn: ${cmd.slice(0, 120)}`;
      }
    }
    return null;
  }

  return null;
}

async function main() {
  if (process.env.GORTEX_ENFORCE === 'off') return;

  let payload;
  try {
    payload = JSON.parse(await readStdin());
  } catch {
    return; // payload lỗi → không chặn nhầm
  }

  const reason = evaluate(payload);
  if (reason) {
    process.stderr.write(`${reason}\n${TOOL_HINT}\n`);
    process.exit(2);
  }
}

if (import.meta.url === `file://${process.argv[1]}`) {
  main().catch(() => process.exit(0));
}
