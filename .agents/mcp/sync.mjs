#!/usr/bin/env node

// Sinh cấu hình MCP của từng công cụ AI từ nguồn duy nhất .agents/mcp_config.json.
// Mặc định chỉ kiểm (exit 1 khi có tệp lệch); --apply mới ghi. Tệp ngoài repo chỉ ghi khi chọn --target rõ ràng.

import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import process from 'node:process';

const SECRET_PATTERN = /(password|pwd)\s*=/i;
const CODEX_BEGIN = '# >>> .agents/mcp/sync.mjs (sinh từ .agents/mcp_config.json — không sửa tay)';
const CODEX_END = '# <<< .agents/mcp/sync.mjs';

// Tùy chọn riêng của từng công cụ, không đưa vào nguồn chung.
// mssql_staging bật khi mở phiên như dev/test (chỉ đọc); Claude Code bật bằng cài đặt của công cụ.
const CODEX_DEFAULTS = {startup_timeout_sec: 60};
const CODEX_EXTRAS = {
  gortex: {
    startup_timeout_sec: 90,
    default_tools_approval_mode: 'approve',
    tools: {tool_profile: {approval_mode: 'approve'}}
  }
};
// Antigravity tắt server bằng "disabled": true (định dạng `agy mcp disable` ghi ra).
const ANTIGRAVITY_EXTRAS = {};

const TARGETS = {
  claude: {file: root => path.join(root, '.mcp.json'), render: servers => json({mcpServers: servers})},
  vscode: {file: root => path.join(root, '.vscode', 'mcp.json'), render: servers => json({mcpServers: servers})},
  codex: {file: root => path.join(root, '.codex', 'config.toml'), render: renderCodex},
  antigravity: {home: true, file: () => path.join(os.homedir(), '.gemini', 'antigravity', 'mcp_config.json'), render: renderGlobal}
};

function json(value) {
  return `${JSON.stringify(value, null, 2)}\n`;
}

function isPlainObject(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function withExtras(servers, extras, defaults = {}) {
  return Object.fromEntries(Object.entries(servers).map(([name, server]) => [name, {...server, ...defaults, ...extras[name]}]));
}

function isRepoPath(arg) {
  return typeof arg === 'string' && arg.startsWith('.agents/');
}

// Tệp global dùng chung mọi workspace: chỉ ghi server chạy tệp trong repo (đổi sang đường dẫn tuyệt đối),
// server khác (gortex, stitch...) giữ nguyên như người dùng đã cấu hình.
function renderGlobal(servers, current, root) {
  const config = current ? JSON.parse(current) : {};
  config.mcpServers ??= {};
  for (const [name, server] of Object.entries(servers)) {
    if (!(server.args ?? []).some(isRepoPath)) continue;
    config.mcpServers[name] = {...server, args: server.args.map(arg => (isRepoPath(arg) ? path.join(root, arg) : arg)), ...ANTIGRAVITY_EXTRAS[name]};
  }
  return json(config);
}

function tomlKey(key) {
  return /^[A-Za-z0-9_-]+$/.test(key) ? key : JSON.stringify(key);
}

function tomlValue(value) {
  if (Array.isArray(value)) return `[${value.map(tomlValue).join(', ')}]`;
  if (typeof value === 'string') return JSON.stringify(value);
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  throw new Error(`Không ghi được giá trị TOML kiểu ${typeof value}`);
}

function tomlTable(name, table) {
  const entries = Object.entries(table);
  const scalars = entries.filter(([, value]) => !isPlainObject(value));
  const blocks = [];
  if (scalars.length || entries.length === 0) {
    blocks.push([`[${name}]`, ...scalars.map(([key, value]) => `${tomlKey(key)} = ${tomlValue(value)}`)].join('\n'));
  }
  for (const [key, value] of entries.filter(([, value]) => isPlainObject(value))) {
    blocks.push(tomlTable(`${name}.${tomlKey(key)}`, value));
  }
  return blocks.join('\n\n');
}

// Chỉ thay vùng giữa hai dấu đánh dấu; lần đầu vùng là từ dòng [mcp_servers] tới hết tệp.
function renderCodex(servers, current) {
  const text = current ?? '';
  const tables = Object.entries(withExtras(servers, CODEX_EXTRAS, CODEX_DEFAULTS))
    .map(([name, server]) => tomlTable(`mcp_servers.${tomlKey(name)}`, server));
  const region = `${[CODEX_BEGIN, ...tables, CODEX_END].join('\n\n')}\n`;

  const begin = text.indexOf(CODEX_BEGIN);
  if (begin >= 0) {
    const end = text.indexOf(CODEX_END, begin);
    if (end < 0) throw new Error(`.codex/config.toml có "${CODEX_BEGIN}" nhưng thiếu "${CODEX_END}".`);
    const lineEnd = text.indexOf('\n', end);
    return text.slice(0, begin) + region + (lineEnd < 0 ? '' : text.slice(lineEnd + 1));
  }

  const legacy = text.search(/^\[mcp_servers\]\s*$/m);
  if (legacy >= 0) {
    const foreign = text.slice(legacy).match(/^\[\[?(?!mcp_servers[.\]])[^\]\n]+\]/m);
    if (foreign) throw new Error(`.codex/config.toml có bảng ${foreign[0]} sau [mcp_servers]; thêm dấu "${CODEX_BEGIN}" / "${CODEX_END}" quanh vùng MCP rồi chạy lại.`);
    return text.slice(0, legacy) + region;
  }
  if (/^\[mcp_servers\./m.test(text)) {
    throw new Error(`.codex/config.toml có bảng mcp_servers.* không xác định được vùng; thêm dấu "${CODEX_BEGIN}" / "${CODEX_END}" rồi chạy lại.`);
  }
  return text ? `${text.replace(/\n*$/, '\n')}\n${region}` : region;
}

function parseArgs(argv) {
  const options = {root: path.resolve(import.meta.dirname, '../..'), apply: false, target: null};
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === '--root') options.root = path.resolve(argv[++i] ?? '');
    else if (arg === '--apply') options.apply = true;
    else if (arg === '--target') options.target = argv[++i];
    else if (arg !== '--check') throw new Error(`Tham số không hợp lệ: ${arg}`);
  }
  if (options.target && !Object.hasOwn(TARGETS, options.target)) {
    throw new Error(`--target phải là một trong: ${Object.keys(TARGETS).join(', ')}`);
  }
  return options;
}

function loadServers(root) {
  const text = fs.readFileSync(path.join(root, '.agents', 'mcp_config.json'), 'utf8');
  if (SECRET_PATTERN.test(text)) throw new Error('.agents/mcp_config.json chứa mật khẩu; chuyển chuỗi kết nối sang .agents/mcp/.env.');
  const servers = JSON.parse(text).mcpServers;
  if (!isPlainObject(servers)) throw new Error('.agents/mcp_config.json: mcpServers phải là object.');
  return servers;
}

function main() {
  const options = parseArgs(process.argv.slice(2));
  const servers = loadServers(options.root);
  const names = options.target ? [options.target] : Object.keys(TARGETS).filter(name => !TARGETS[name].home);
  let drift = false;

  for (const name of names) {
    const target = TARGETS[name];
    const file = target.file(options.root);
    const label = target.home ? file.replace(os.homedir(), '~') : path.relative(options.root, file);
    const current = fs.existsSync(file) ? fs.readFileSync(file, 'utf8') : null;
    const next = target.render(servers, current, options.root);

    if (current === next) {
      console.log(`khớp     ${label}`);
    } else if (!options.apply) {
      drift = true;
      console.log(`lệch     ${label}`);
    } else {
      fs.mkdirSync(path.dirname(file), {recursive: true});
      if (target.home && current !== null) {
        const backupFile = `${file}.mcp-backup-${new Date().toISOString().replace(/[:.]/g, '-')}`;
        fs.copyFileSync(file, backupFile);
        console.log(`sao lưu  ${backupFile.replace(os.homedir(), '~')}`);
      }
      fs.writeFileSync(file, next, 'utf8');
      console.log(`đã ghi   ${label}`);
    }
  }

  if (drift) {
    console.log('Có tệp lệch nguồn .agents/mcp_config.json — chạy lại với --apply để sinh lại.');
    process.exitCode = 1;
  }
}

try {
  main();
} catch (error) {
  console.error(`mcp/sync: ${error.message}`);
  process.exitCode = 1;
}
