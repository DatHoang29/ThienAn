#!/usr/bin/env node

// Nạp sẵn đầu phiên cho công cụ không tự mở dòng `@` trong AGENTS.md (Codex, Antigravity), giống Claude Code:
// đọc các dòng `@` của AGENTS.md (nạp lồng tối đa 5 cấp, bỏ front matter YAML và comment HTML) rồi trả ngữ cảnh theo
// giao thức hook SessionStart của từng công cụ. Danh sách tệp chỉ nằm ở AGENTS.md.
// Tệp công cụ tự nạp thì không bơm lại: Codex tự liệt kê `.agents/skills/`; Antigravity tự nạp `.agents/rules/`
// (rule trigger: always_on) và `.agents/skills/`. Đọc tệp lỗi thì không bơm gì, không chặn phiên.

import {spawnSync} from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import process from 'node:process';

const root = path.resolve(import.meta.dirname, '../..');
const NATIVE = {codex: /^\.agents\/skills\//, antigravity: /^\.agents\/(?:rules|skills)\//};
const OUTPUT = {
  codex: text => ({hookSpecificOutput: {hookEventName: 'SessionStart', additionalContext: text}}),
  antigravity: text => ({injectSteps: [{systemMessage: {systemMessage: text}}]})
};

function importedFiles(file, loaded, depth = 0) {
  loaded.add(file);
  if (depth === 5) return loaded;
  for (const [, target] of fs.readFileSync(path.join(root, file), 'utf8').matchAll(/^@(\S+)$/gm)) {
    const relative = path.relative(root, path.resolve(root, path.dirname(file), target)).replaceAll('\\', '/');
    if (!loaded.has(relative)) importedFiles(relative, loaded, depth + 1);
  }
  return loaded;
}

function loadedText(file) {
  return fs.readFileSync(path.join(root, file), 'utf8').replace(/^---\n[\s\S]*?\n---\n/, '').replace(/<!--[\s\S]*?-->/g, '').trim();
}

function sessionContext(agent) {
  return [...importedFiles('AGENTS.md', new Set())]
    .filter(file => file !== 'AGENTS.md' && !NATIVE[agent]?.test(file))
    .map(file => `Contents of ${file}:\n\n${loadedText(file)}`)
    .join('\n\n');
}

function resolveGortexBinary() {
  if (process.env.GORTEX_BIN) return process.env.GORTEX_BIN;
  const isWindows = process.platform === 'win32';
  const candidates = isWindows
    ? [process.env.LOCALAPPDATA && path.join(process.env.LOCALAPPDATA, 'Programs', 'gortex', 'gortex.exe')]
    : ['/opt/homebrew/bin/gortex', '/usr/local/bin/gortex', path.join(os.homedir(), '.local', 'bin', 'gortex')];
  return candidates.find(candidate => candidate && fs.existsSync(candidate)) ?? (isWindows ? 'gortex.exe' : 'gortex');
}

function gortexContext() {
  let payload = {};
  try {
    payload = JSON.parse(fs.readFileSync(0, 'utf8') || '{}');
  } catch {}
  const res = spawnSync(resolveGortexBinary(), ['hook', '--agent', 'antigravity'], {
    input: JSON.stringify({hook_event_name: 'SessionStart', ...payload}),
    encoding: 'utf8',
    windowsHide: true
  });
  if (res.error || res.status !== 0 || !res.stdout) return '';
  try {
    return JSON.parse(res.stdout).hookSpecificOutput?.additionalContext?.trim() ?? '';
  } catch {
    return '';
  }
}

const agent = process.argv[2];
let text = '';
try {
  text = OUTPUT[agent]
    ? [sessionContext(agent), agent === 'antigravity' ? gortexContext() : ''].filter(Boolean).join('\n\n')
    : '';
} catch (error) {
  console.error(`ThienAn session-context: ${error.message}`);
}
if (text) process.stdout.write(`${JSON.stringify(OUTPUT[agent](text))}\n`);
else if (agent === 'antigravity') process.stdout.write('{}\n');
