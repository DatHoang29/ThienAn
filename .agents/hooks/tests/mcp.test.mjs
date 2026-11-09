import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '../../..');
const syncScript = path.join(root, '.agents/mcp/sync.mjs');
const dabScript = path.join(root, '.agents/mcp/dab.mjs');

// Tệp cấu hình MCP của từng công cụ, đều sinh từ .agents/mcp_config.json.
const workspaceTargets = [
  '.mcp.json',
  '.kiro/settings/mcp.json',
  '.vscode/mcp.json',
  '.gemini/settings.json',
  '.codex/config.toml'
];

const source = {
  mcpServers: {
    gortex: {command: 'gortex', args: ['mcp'], env: {GORTEX_INDEX_WORKERS: '8'}},
    mssql_dev: {command: 'node', args: ['.agents/mcp/dab.mjs', 'dev']}
  }
};

const codexPrefix = 'model = "gpt"\n\n[[hooks.Stop]]\n[[hooks.Stop.hooks]]\ncommand = "gortex hook"\n\n';
const geminiHooks = {SessionStart: [{hooks: [{command: 'gortex hook --agent gemini', type: 'command'}]}]};

function writeFile(base, relative, content) {
  const file = path.join(base, relative);
  fs.mkdirSync(path.dirname(file), {recursive: true});
  fs.writeFileSync(file, typeof content === 'string' ? content : `${JSON.stringify(content, null, 2)}\n`);
}

function readJson(base, relative) {
  return JSON.parse(fs.readFileSync(path.join(base, relative), 'utf8'));
}

function makeWorkspace() {
  const base = fs.mkdtempSync(path.join(os.tmpdir(), 'mcp-test-mcp-'));
  writeFile(base, '.agents/mcp_config.json', source);
  writeFile(base, '.gemini/settings.json', {hooks: geminiHooks, mcpServers: {old: {command: 'old'}}});
  writeFile(base, '.codex/config.toml', `${codexPrefix}[mcp_servers]\n[mcp_servers.old]\ncommand = "old"\n`);
  return base;
}

function runSync(base, args, env = process.env) {
  return spawnSync(process.execPath, [syncScript, '--root', base, ...args], {encoding: 'utf8', env});
}

test('MCP: repo không chứa mật khẩu trong nguồn và các tệp cấu hình MCP sinh ra', () => {
  for (const relative of ['.agents/mcp_config.json', ...workspaceTargets]) {
    const file = path.join(root, relative);
    if (!fs.existsSync(file)) continue;
    assert.doesNotMatch(fs.readFileSync(file, 'utf8'), /(password|pwd)\s*=/i, `${relative} chứa mật khẩu`);
  }
});

test('MCP: cấu hình MCP các công cụ trong repo khớp nguồn .agents/mcp_config.json', () => {
  const result = spawnSync(process.execPath, [syncScript, '--check'], {encoding: 'utf8'});
  assert.equal(result.status, 0, result.stdout + result.stderr);
});

test('MCP: --apply sinh đủ tệp cho từng công cụ, giữ phần không thuộc MCP, --check bắt được tệp sửa tay', () => {
  const base = makeWorkspace();

  const apply = runSync(base, ['--apply']);
  assert.equal(apply.status, 0, apply.stderr);

  assert.deepEqual(readJson(base, '.mcp.json'), source);
  assert.deepEqual(readJson(base, '.vscode/mcp.json'), source);

  const kiro = readJson(base, '.kiro/settings/mcp.json');
  assert.ok(kiro.mcpServers.gortex.autoApprove.includes('explore'));
  assert.deepEqual(kiro.mcpServers.mssql_dev, source.mcpServers.mssql_dev);

  const gemini = readJson(base, '.gemini/settings.json');
  assert.deepEqual(gemini.hooks, geminiHooks);
  assert.deepEqual(gemini.mcpServers, source.mcpServers);

  const codex = fs.readFileSync(path.join(base, '.codex/config.toml'), 'utf8');
  assert.ok(codex.startsWith(codexPrefix), 'phần ngoài vùng MCP của Codex phải giữ nguyên');
  assert.match(codex, /\[mcp_servers\.gortex\]\ncommand = "gortex"\nargs = \["mcp", "--tools", "facade-v1"\]\nstartup_timeout_sec = 90/);
  assert.match(codex, /\[mcp_servers\.mssql_dev\]\ncommand = "node"\nargs = \["\.agents\/mcp\/dab\.mjs", "dev"\]\nstartup_timeout_sec = 60/);
  assert.doesNotMatch(codex, /mcp_servers\.old/);

  const snapshot = workspaceTargets.map(relative => fs.readFileSync(path.join(base, relative), 'utf8'));
  assert.equal(runSync(base, ['--apply']).status, 0);
  assert.deepEqual(workspaceTargets.map(relative => fs.readFileSync(path.join(base, relative), 'utf8')), snapshot);
  assert.equal(runSync(base, ['--check']).status, 0);

  writeFile(base, '.mcp.json', {mcpServers: {}});
  const drift = runSync(base, ['--check']);
  assert.equal(drift.status, 1);
  assert.match(drift.stdout, /\.mcp\.json/);
});

test('MCP: --target antigravity chỉ thay server dùng tệp trong repo, đổi sang đường dẫn tuyệt đối và sao lưu tệp cũ', () => {
  const base = makeWorkspace();
  const home = fs.mkdtempSync(path.join(os.tmpdir(), 'mcp-test-home-'));
  const gortex = {command: '/opt/gortex', args: ['mcp', '--index', '/repo']};
  const stitch = {serverUrl: 'https://stitch.example'};
  writeFile(home, '.gemini/antigravity/mcp_config.json', {
    mcpServers: {gortex, stitch, mssql_dev: {command: 'dab', env: {DAB_DEV_CONNECTION_STRING: 'x'}}}
  });

  const result = runSync(base, ['--apply', '--target', 'antigravity'], {...process.env, HOME: home, USERPROFILE: home});
  assert.equal(result.status, 0, result.stderr);

  const config = readJson(home, '.gemini/antigravity/mcp_config.json');
  assert.deepEqual(config.mcpServers.gortex, gortex);
  assert.deepEqual(config.mcpServers.stitch, stitch);
  assert.deepEqual(config.mcpServers.mssql_dev, {
    command: 'node',
    args: [path.join(base, '.agents/mcp/dab.mjs'), 'dev']
  });
  assert.ok(fs.readdirSync(path.join(home, '.gemini/antigravity')).some(name => name.startsWith('mcp_config.json.mcp-backup-')));
});

test('MCP: --target cline / cline-cli ghi vào tệp MCP global của Cline (extension VS Code, CLI), chỉ thay server dùng tệp trong repo', () => {
  const base = makeWorkspace();
  const home = fs.mkdtempSync(path.join(os.tmpdir(), 'mcp-test-home-'));
  const appData = path.join(home, 'AppData', 'Roaming');
  const codeDir = {win32: path.join(appData, 'Code'), darwin: path.join(home, 'Library', 'Application Support', 'Code')}[process.platform] ?? path.join(home, '.config', 'Code');
  const extensionFile = path.join(codeDir, 'User', 'globalStorage', 'saoudrizwan.claude-dev', 'settings', 'cline_mcp_settings.json');
  const cliFile = path.join(home, '.cline', 'data', 'settings', 'cline_mcp_settings.json');
  const gortex = {command: 'gortex', args: ['mcp'], disabled: false, autoApprove: []};
  fs.mkdirSync(path.dirname(extensionFile), {recursive: true});
  fs.writeFileSync(extensionFile, `${JSON.stringify({mcpServers: {gortex}}, null, 2)}\n`);

  for (const target of ['cline', 'cline-cli']) {
    const result = runSync(base, ['--apply', '--target', target], {...process.env, HOME: home, USERPROFILE: home, APPDATA: appData});
    assert.equal(result.status, 0, result.stderr);
  }

  const mssqlDev = {command: 'node', args: [path.join(base, '.agents/mcp/dab.mjs'), 'dev']};
  assert.deepEqual(JSON.parse(fs.readFileSync(extensionFile, 'utf8')).mcpServers, {gortex, mssql_dev: mssqlDev});
  assert.deepEqual(JSON.parse(fs.readFileSync(cliFile, 'utf8')).mcpServers, {mssql_dev: mssqlDev});
  assert.equal(fs.existsSync(path.join(base, '.cline/mcp.json')), false);
});

test('MCP: launcher DAB thiếu chuỗi kết nối thì báo tên biến ra stderr, không ghi gì ra stdout', () => {
  const emptyEnvFile = path.join(fs.mkdtempSync(path.join(os.tmpdir(), 'mcp-test-env-')), '.env');
  fs.writeFileSync(emptyEnvFile, '');
  const env = Object.fromEntries(Object.entries(process.env).filter(([key]) => !key.startsWith('DAB_')));

  const result = spawnSync(process.execPath, [dabScript, 'dev', '--env-file', emptyEnvFile], {encoding: 'utf8', env});
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /DAB_DEV_CONNECTION_STRING/);
  assert.equal(result.stdout, '');
});
