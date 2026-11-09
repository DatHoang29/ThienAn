#!/usr/bin/env node

// Nạp sẵn đầu phiên cho công cụ không tự mở dòng `@` trong AGENTS.md (Codex, Antigravity), giống Claude Code:
// đọc các dòng `@` của AGENTS.md (nạp lồng tối đa 5 cấp, bỏ front matter YAML và comment HTML) rồi trả ngữ cảnh theo
// giao thức hook SessionStart của từng công cụ. Danh sách tệp chỉ nằm ở AGENTS.md.
// Tệp công cụ tự nạp thì không bơm lại: Codex tự liệt kê `.agents/skills/`; Antigravity tự nạp `.agents/rules/`
// (rule trigger: always_on) và `.agents/skills/`. Đọc tệp lỗi thì không bơm gì, không chặn phiên.

import fs from 'node:fs';
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

const agent = process.argv[2];
let text = '';
try {
  text = OUTPUT[agent] ? sessionContext(agent) : '';
} catch (error) {
  console.error(`ThienAn session-context: ${error.message}`);
}
if (text) process.stdout.write(`${JSON.stringify(OUTPUT[agent](text))}\n`);
else if (agent === 'antigravity') process.stdout.write('{}\n');
