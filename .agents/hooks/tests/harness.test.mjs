import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import test from 'node:test';

import {evaluateCommand, extractCommand} from '../validate-tool-call.mjs';

const root = path.resolve(import.meta.dirname, '../../..');

test('Harness: Trích xuất lệnh CommandLine từ tool arguments đa nền tảng', () => {
  assert.equal(extractCommand({tool_args: {CommandLine: 'npm test'}}), 'npm test');
  assert.equal(extractCommand({tool_input: {command: 'npm test'}}), 'npm test');
  assert.equal(extractCommand({arguments: {cmd: 'npm test'}}), 'npm test');
});

test('Harness: Cho phép thao tác dọn dẹp thư mục thông thường', () => {
  assert.equal(evaluateCommand('rm -rf ./dist').allowed, true);
  assert.equal(evaluateCommand('rm -rf node_modules').allowed, true);
  assert.equal(evaluateCommand('git clean -fd').allowed, true);
});

test('Harness: Chặn đứng các lệnh phá hoại ổ đĩa và thư mục gốc hệ thống', () => {
  assert.equal(evaluateCommand('sudo rm -rf /').allowed, false);
  assert.equal(evaluateCommand('mkfs.ext4 /dev/sda1').allowed, false);
  assert.equal(evaluateCommand('dd if=/dev/zero of=/dev/sda').allowed, false);
  assert.equal(evaluateCommand('format C:').allowed, false);
});

test('Harness: validate-tool-call chặn lệnh nguy hiểm bằng mã 2, cho qua thì không in gì ra stdout', () => {
  const result = spawnSync(process.execPath, [path.join(root, '.agents/hooks/validate-tool-call.mjs')], {
    input: JSON.stringify({tool_args: {CommandLine: 'rm -rf /'}}),
    encoding: 'utf8'
  });
  assert.equal(result.status, 2);
  assert.match(result.stderr, /BLOCKED by ThienAn/);

  // Gemini CLI coi stdout không phải JSON là thông báo hệ thống: lệnh được cho qua thì hook im lặng.
  for (const payload of [{tool_name: 'run_shell_command', tool_input: {command: 'git status'}}, {}]) {
    const allowed = spawnSync(process.execPath, [path.join(root, '.agents/hooks/validate-tool-call.mjs')], {
      input: JSON.stringify(payload),
      encoding: 'utf8'
    });
    assert.equal(allowed.status, 0);
    assert.equal(allowed.stdout, '');
  }
});

// Rule luôn bật, mục lục skill, mục lục agent, memory: công cụ nào cũng phải nạp sẵn đủ bộ này từ .agents/.
const alwaysLoaded = [
  '.agents/rules/core-protocol.md',
  '.agents/rules/request-routing.md',
  '.agents/rules/thienan_rules.md',
  '.agents/rules/gortex-tool-map.md',
  '.agents/skills/INDEX.md',
  '.agents/agent/INDEX.md',
  '.agents/memory/MEMORY.md'
];

function toRepoPath(base, relative) {
  return path.relative(root, path.resolve(root, base, relative)).replaceAll('\\', '/');
}

// Claude Code, Gemini CLI: dòng `@đường-dẫn` tính theo thư mục của tệp đang import, nạp lồng tối đa 5 cấp.
function followImports(file, loaded, depth = 0) {
  loaded.add(file);
  if (depth === 5) return loaded;
  for (const [, target] of fs.readFileSync(path.join(root, file), 'utf8').matchAll(/^@(\S+)$/gm)) {
    const relative = toRepoPath(path.dirname(file), target);
    assert.ok(fs.existsSync(path.join(root, relative)), `${file} nạp tệp không tồn tại: ${relative}`);
    if (!loaded.has(relative)) followImports(relative, loaded, depth + 1);
  }
  return loaded;
}

// Kiro: tự nạp AGENTS.md và steering; `#[[file:đường-dẫn]]` tính theo gốc workspace.
function kiroLoaded() {
  const steering = fs.readdirSync(path.join(root, '.kiro/steering')).map(file => `.kiro/steering/${file}`);
  const loaded = new Set(['AGENTS.md', ...steering]);
  for (const file of steering) {
    for (const [, target] of fs.readFileSync(path.join(root, file), 'utf8').matchAll(/^#\[\[file:(.+)\]\]$/gm)) {
      assert.ok(fs.existsSync(path.join(root, target)), `${file} nạp tệp không tồn tại: ${target}`);
      loaded.add(target);
    }
  }
  return loaded;
}

test('Harness: Claude Code, Gemini CLI, Kiro nạp sẵn AGENTS.md, rule luôn bật, mục lục skill, mục lục agent, memory; mọi tệp được nạp đều tồn tại', () => {
  const geminiFiles = [readJsonFile('.gemini/settings.json').context?.fileName ?? 'GEMINI.md'].flat();
  const loaded = {
    'Claude Code': followImports('.claude/CLAUDE.md', new Set()),
    'Gemini CLI': geminiFiles.reduce((files, file) => followImports(file, files), new Set()),
    Kiro: kiroLoaded()
  };
  for (const [tool, files] of Object.entries(loaded)) {
    for (const relative of ['AGENTS.md', ...alwaysLoaded]) {
      assert.ok(files.has(relative), `${tool} chưa nạp ${relative}`);
    }
  }
});

test('Harness: Phần nạp sẵn mỗi phiên (AGENTS.md và các dòng @) không vượt 20.000 ký tự; mỗi dòng mục lục memory ≤ 150 ký tự', () => {
  // Đo như Claude Code nạp: bỏ front matter YAML và comment HTML.
  const loadedText = file => fs.readFileSync(path.join(root, file), 'utf8').replace(/^---\n[\s\S]*?\n---\n/, '').replace(/<!--[\s\S]*?-->/g, '');
  const sizes = [...followImports('AGENTS.md', new Set())].map(file => [file, [...loadedText(file)].length]);
  const total = sizes.reduce((sum, [, size]) => sum + size, 0);
  assert.ok(total <= 20000, `Phần nạp sẵn ${total} ký tự, vượt ngân sách 20.000: ${sizes.map(([file, size]) => `${file} ${size}`).join(', ')}`);

  for (const line of fs.readFileSync(path.join(root, '.agents/memory/MEMORY.md'), 'utf8').split('\n')) {
    assert.ok([...line].length <= 150, `Dòng mục lục memory dài hơn 150 ký tự: ${line.slice(0, 60)}…`);
  }
});

test('Harness: AGENTS.md (Codex, Cline đọc trực tiếp) trỏ tới rule luôn bật, mục lục skill, mục lục agent, memory; mọi liên kết đều tồn tại', () => {
  const text = fs.readFileSync(path.join(root, 'AGENTS.md'), 'utf8');
  const links = [...text.matchAll(/\]\(([^)#\s]+)\)|^@(\S+)$/gm)].map(match => toRepoPath('.', match[1] ?? match[2]));
  for (const link of links) {
    assert.ok(fs.existsSync(path.join(root, link)), `AGENTS.md có liên kết hỏng: ${link}`);
  }
  for (const relative of alwaysLoaded) {
    assert.ok(links.includes(relative), `AGENTS.md chưa trỏ tới ${relative}`);
  }
});

test('Harness: Skill - INDEX.md liệt kê đúng các thư mục skill, SKILL.md có name trùng thư mục và description, skill agent khai báo đều có', () => {
  const skillsDir = path.join(root, '.agents/skills');
  const skills = fs.readdirSync(skillsDir, {withFileTypes: true}).filter(entry => entry.isDirectory()).map(entry => entry.name).sort();
  for (const skill of skills) {
    const text = fs.readFileSync(path.join(skillsDir, skill, 'SKILL.md'), 'utf8');
    // Kiro, Cline bỏ qua skill có name khác tên thư mục; Codex, Kiro, Cline giới hạn description 1024 ký tự.
    assert.equal(text.match(/^name:\s*(.+)$/m)?.[1].trim(), skill, `${skill}/SKILL.md: name phải trùng tên thư mục`);
    const description = text.match(/^description:\s*(.+)$/m)?.[1].trim() ?? '';
    assert.ok(description.length > 0 && description.length <= 1024, `${skill}/SKILL.md: description rỗng hoặc quá 1024 ký tự`);
  }

  const index = fs.readFileSync(path.join(skillsDir, 'INDEX.md'), 'utf8');
  const listed = [...index.matchAll(/^\| `([^`]+)` \|/gm)].map(match => match[1]).sort();
  assert.deepEqual(listed, skills, 'INDEX.md phải liệt kê đúng các thư mục skill trong .agents/skills/');

  const agentDir = path.join(root, '.agents/agent');
  for (const file of fs.readdirSync(agentDir)) {
    const declared = fs.readFileSync(path.join(agentDir, file), 'utf8').match(/^skills:\s*(.*)$/m)?.[1] ?? '';
    for (const skill of declared.split(',').map(name => name.trim()).filter(Boolean)) {
      assert.ok(skills.includes(skill), `.agents/agent/${file} khai báo skill không tồn tại: ${skill}`);
    }
  }
});

test('Harness: Agent - INDEX.md liệt kê đúng các vai, tệp vai có name trùng tên tệp và description; không công cụ nào giữ tệp agent riêng', () => {
  const agentDir = path.join(root, '.agents/agent');
  const agents = fs.readdirSync(agentDir).filter(file => file.endsWith('.md') && file !== 'INDEX.md').map(file => path.basename(file, '.md')).sort();
  for (const agent of agents) {
    const text = fs.readFileSync(path.join(agentDir, `${agent}.md`), 'utf8');
    assert.equal(text.match(/^name:\s*(.+)$/m)?.[1].trim(), agent, `.agents/agent/${agent}.md: name phải trùng tên tệp`);
    assert.ok(text.match(/^description:\s*(.+)$/m)?.[1].trim(), `.agents/agent/${agent}.md: thiếu description`);
  }

  const index = fs.readFileSync(path.join(agentDir, 'INDEX.md'), 'utf8');
  const listed = [...index.matchAll(/^\| `([^`]+)` \|/gm)].map(match => match[1]).sort();
  assert.deepEqual(listed, agents, 'INDEX.md phải liệt kê đúng các tệp vai trong .agents/agent/');

  // Một nguồn: công cụ nạp mục lục, không giữ bản chép hay tệp trỏ của từng vai trong thư mục riêng.
  for (const dir of ['.claude/agents', '.gemini/agents', '.codex/agents', '.kiro/agents']) {
    assert.equal(fs.existsSync(path.join(root, dir)), false, `${dir} không được tồn tại: vai chỉ nằm ở .agents/agent/`);
  }
});

function readJsonFile(relative) {
  return JSON.parse(fs.readFileSync(path.join(root, relative), 'utf8'));
}

function jsonHookGroups(hooks) {
  return Object.entries(hooks ?? {}).flatMap(([event, groups]) =>
    groups.map(group => ({event, matcher: group.matcher ?? '', commands: (group.hooks ?? []).map(hook => hook.command)})));
}

// Đọc nhóm hook trong .codex/config.toml: bảng [[hooks.<Sự kiện>]] giữ matcher, [[hooks.<Sự kiện>.hooks]] giữ command.
function codexHookGroups() {
  const groups = [];
  let current = null;
  for (const line of fs.readFileSync(path.join(root, '.codex/config.toml'), 'utf8').split(/\r?\n/)) {
    const header = line.match(/^\[\[hooks\.(\w+)(\.hooks)?\]\]$/);
    if (header) {
      if (!header[2]) groups.push(current = {event: header[1], matcher: '', commands: []});
      continue;
    }
    if (line.startsWith('[')) {
      current = null;
      continue;
    }
    const pair = line.match(/^(matcher|command) = (".*")$/);
    if (current && pair) {
      if (pair[1] === 'matcher') current.matcher = JSON.parse(pair[2]);
      else current.commands.push(JSON.parse(pair[2]));
    }
  }
  return groups;
}

const adhocTest = 'TA-ITS015-WEBVUE-V1.0/src/adhoc.test.ts';
const subDir = path.join(root, 'tests/BE');

// Mỗi công cụ: cách đọc cấu hình hook, cách khớp matcher, payload gửi cho hook và môi trường chạy lệnh hook như chính công cụ đó.
const hookTools = [
  {
    name: 'Claude Code',
    groups: () => jsonHookGroups(readJsonFile('.claude/settings.json').hooks),
    preTool: 'PreToolUse',
    prompt: null,
    matches: (matcher, tool) => matcher === '' || matcher === '*' || new RegExp(`^(?:${matcher})$`).test(tool),
    shell: command => ({tool_name: 'Bash', tool_input: {command}}),
    write: {tool_name: 'Write', tool_input: {file_path: path.join(root, adhocTest)}},
    cwd: subDir,
    env: {CLAUDE_PROJECT_DIR: root}
  },
  {
    name: 'Codex',
    groups: codexHookGroups,
    preTool: 'PreToolUse',
    prompt: 'UserPromptSubmit',
    matches: (matcher, tool) => new RegExp(matcher).test(tool),
    shell: command => ({tool_name: 'Bash', tool_input: {command}}),
    write: {tool_name: 'apply_patch', tool_input: {command: `*** Begin Patch\n*** Add File: ${adhocTest}\n+x\n*** End Patch\n`}},
    cwd: subDir,
    env: {}
  },
  {
    name: 'Gemini CLI',
    groups: () => jsonHookGroups(readJsonFile('.gemini/settings.json').hooks),
    preTool: 'BeforeTool',
    prompt: null,
    matches: (matcher, tool) => new RegExp(matcher).test(tool),
    shell: command => ({tool_name: 'run_shell_command', tool_input: {command}}),
    write: {tool_name: 'write_file', tool_input: {file_path: path.join(root, adhocTest), content: ''}},
    cwd: subDir,
    env: {GEMINI_PROJECT_DIR: root}
  },
  {
    name: 'Antigravity',
    groups: () => Object.entries(readJsonFile('.agents/hooks.json')).filter(([, list]) => Array.isArray(list)).flatMap(([event, list]) =>
      list.map(item => ({event, matcher: item.matcher ?? '', commands: [item.command]}))),
    preTool: 'PreToolUse',
    prompt: null,
    matches: (matcher, tool) => new RegExp(`^(?:${matcher})$`).test(tool),
    shell: command => ({tool_name: 'run_command', tool_args: {CommandLine: command}}),
    write: {tool_name: 'write_to_file', tool_args: {TargetFile: path.join(root, adhocTest)}},
    cwd: root,
    env: {}
  }
];

// Chạy các lệnh hook gọi script trong .agents/hooks/ (bỏ qua hook Gortex) đúng như công cụ chạy: qua shell, với payload trên stdin.
function runHooks(tool, event, payload) {
  return tool.groups()
    .filter(group => group.event === event && (payload === null || tool.matches(group.matcher, payload.tool_name)))
    .flatMap(group => group.commands)
    .filter(command => command?.includes('.agents/hooks/'))
    .map(command => ({
      command,
      ...spawnSync(command, {shell: '/bin/sh', cwd: tool.cwd, env: {...process.env, ...tool.env}, input: JSON.stringify(payload ?? {}), encoding: 'utf8'})
    }));
}

test('Harness: Hook rtk (Claude Code) - máy chưa cài rtk thì im lặng; có rtk thì chỉ viết lại lệnh, không tự duyệt quyền', {skip: process.platform === 'win32' && 'lệnh hook dùng cú pháp shell POSIX'}, () => {
  const commands = jsonHookGroups(readJsonFile('.claude/settings.json').hooks)
    .filter(group => group.event === 'PreToolUse')
    .flatMap(group => group.commands)
    .filter(command => command.includes('rtk hook claude'));
  assert.equal(commands.length, 1, 'Claude Code: chưa nối hook rtk');
  const payload = JSON.stringify({tool_name: 'Bash', tool_input: {command: 'git status'}});

  // Máy chưa cài rtk (PATH không có rtk): hook không được chặn hay in gì.
  const withoutRtk = spawnSync(commands[0], {shell: '/bin/sh', env: {...process.env, PATH: '/usr/bin:/bin'}, input: payload, encoding: 'utf8'});
  assert.equal(withoutRtk.status, 0);
  assert.equal(withoutRtk.stdout, '');

  // Máy có rtk: chỉ viết lại lệnh; trả permissionDecision "allow" là lách hệ thống quyền của Claude Code.
  if (spawnSync('/bin/sh', ['-c', 'command -v rtk']).status !== 0) return;
  const withRtk = spawnSync(commands[0], {shell: '/bin/sh', input: payload, encoding: 'utf8'});
  const output = JSON.parse(withRtk.stdout).hookSpecificOutput;
  assert.match(output.updatedInput.command, /^rtk git status/);
  assert.notEqual(output.permissionDecision, 'allow');
});

test('Harness: Hook - cấu hình Claude Code, Codex, Gemini CLI, Antigravity gọi đúng .agents/hooks: chặn vi phạm P0, chặn lệnh phá ổ đĩa, cho qua thì im lặng, nhắc SSOT', {skip: process.platform === 'win32' && 'lệnh hook dùng cú pháp shell POSIX'}, () => {
  for (const tool of hookTools) {
    const blocked = payload => runHooks(tool, tool.preTool, payload).some(result => result.status === 2);
    assert.ok(blocked(tool.shell('git add .')), `${tool.name}: chưa nối guardrails.mjs cho lệnh shell`);
    assert.ok(blocked(tool.shell('rm -rf /')), `${tool.name}: chưa nối validate-tool-call.mjs cho lệnh shell`);
    assert.ok(blocked(tool.write), `${tool.name}: chưa nối guardrails.mjs cho thao tác ghi tệp`);

    for (const result of runHooks(tool, tool.preTool, tool.shell('git status'))) {
      assert.equal(result.status, 0, `${tool.name} ${result.command}: ${result.stderr}`);
      if (/guardrails|validate-tool-call/.test(result.command)) {
        assert.equal(result.stdout, '', `${tool.name} ${result.command}: cho qua thì không được in ra stdout`);
      }
    }

    // Chỉ công cụ không tự nạp rule (Codex) mới cần lời nhắc SSOT mỗi prompt.
    if (!tool.prompt) continue;
    const prompts = runHooks(tool, tool.prompt, null).filter(result => result.command.includes('prompt-submit.mjs'));
    assert.equal(prompts.length, 1, `${tool.name}: chưa nối prompt-submit.mjs`);
    assert.equal(prompts[0].status, 0, prompts[0].stderr);
    assert.equal(JSON.parse(prompts[0].stdout).hookSpecificOutput.hookEventName, tool.prompt);
  }
});

