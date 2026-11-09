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

  // Lệnh được cho qua thì hook im lặng.
  for (const payload of [{tool_name: 'Bash', tool_input: {command: 'git status'}}, {}]) {
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

// Claude Code: dòng `@đường-dẫn` tính theo thư mục của tệp đang import, nạp lồng tối đa 5 cấp.
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

// Nội dung một tệp như Claude Code nạp: bỏ front matter YAML và comment HTML.
function loadedText(file) {
  return fs.readFileSync(path.join(root, file), 'utf8').replace(/^---\n[\s\S]*?\n---\n/, '').replace(/<!--[\s\S]*?-->/g, '');
}

test('Harness: Claude Code nạp sẵn AGENTS.md, rule luôn bật, mục lục skill, mục lục agent, memory; mọi tệp được nạp đều tồn tại', () => {
  const loaded = {
    'Claude Code': followImports('.claude/CLAUDE.md', new Set())
  };
  for (const [tool, files] of Object.entries(loaded)) {
    for (const relative of ['AGENTS.md', ...alwaysLoaded]) {
      assert.ok(files.has(relative), `${tool} chưa nạp ${relative}`);
    }
  }
});

test('Harness: Phần nạp sẵn mỗi phiên (AGENTS.md và các dòng @) không vượt 20.000 ký tự; mỗi dòng mục lục memory ≤ 150 ký tự', () => {
  const sizes = [...followImports('AGENTS.md', new Set())].map(file => [file, [...loadedText(file)].length]);
  const total = sizes.reduce((sum, [, size]) => sum + size, 0);
  assert.ok(total <= 20000, `Phần nạp sẵn ${total} ký tự, vượt ngân sách 20.000: ${sizes.map(([file, size]) => `${file} ${size}`).join(', ')}`);

  for (const line of fs.readFileSync(path.join(root, '.agents/memory/MEMORY.md'), 'utf8').split('\n')) {
    assert.ok([...line].length <= 150, `Dòng mục lục memory dài hơn 150 ký tự: ${line.slice(0, 60)}…`);
  }
});

test('Harness: AGENTS.md (Codex đọc trực tiếp) trỏ tới rule luôn bật, mục lục skill, mục lục agent, memory; mọi liên kết đều tồn tại', () => {
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
    // Quy ước Agent Skills: name trùng tên thư mục, description không quá 1024 ký tự.
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
  for (const dir of ['.claude/agents', '.codex/agents']) {
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
    const pair = line.match(/^(matcher|command|additionalContextLimit) = (".*"|\d+)$/);
    if (current && pair) {
      if (pair[1] === 'matcher') current.matcher = JSON.parse(pair[2]);
      else if (pair[1] === 'command') current.commands.push(JSON.parse(pair[2]));
      else current.additionalContextLimit = Number(pair[2]);
    }
  }
  return groups;
}

const adhocTest = 'TA-ITS015-WEBVUE-V1.0/src/adhoc.test.ts';
const codeFile = path.join(root, 'TA-ITS015-WEBAPI-V1.0/src/TAC_WebAPI/Program.cs');
const subDir = path.join(root, 'tests/BE');

// Antigravity (agy): mỗi khóa cấp trên của .agents/hooks.json là một hook có tên; PreToolUse lồng matcher + hooks,
// SessionStart là danh sách lệnh phẳng.
function antigravityHookGroups() {
  return Object.values(readJsonFile('.agents/hooks.json')).flatMap(spec => Object.entries(spec)
    .filter(([, list]) => Array.isArray(list))
    .flatMap(([event, list]) => list.map(item => ({event, matcher: item.matcher ?? '', commands: item.hooks ? item.hooks.map(hook => hook.command) : [item.command]}))));
}

// Payload agy gửi cho hook PreToolUse: {toolCall: {name, args}} kèm các trường chung.
const antigravityCall = (name, args) => ({toolCall: {name, args}, stepIdx: 1, conversationId: 'c', workspacePaths: [root]});
const decisionOf = result => JSON.parse(result.stdout || '{}').decision;

// Mỗi công cụ: cách đọc cấu hình hook, cách khớp matcher, payload gửi cho hook, môi trường chạy lệnh hook và cách hook báo chặn, cho qua như chính công cụ đó.
const hookTools = [
  {
    name: 'Claude Code',
    groups: () => jsonHookGroups(readJsonFile('.claude/settings.json').hooks),
    matches: (matcher, tool) => matcher === '' || matcher === '*' || new RegExp(`^(?:${matcher})$`).test(tool),
    shell: command => ({tool_name: 'Bash', tool_input: {command}}),
    write: {tool_name: 'Write', tool_input: {file_path: path.join(root, adhocTest)}},
    read: file => ({tool_name: 'Read', tool_input: {file_path: file}}),
    cwd: subDir,
    env: {CLAUDE_PROJECT_DIR: root},
    blocked: result => result.status === 2,
    passed: result => result.status === 0 && result.stdout === ''
  },
  {
    name: 'Codex',
    groups: codexHookGroups,
    matches: (matcher, tool) => new RegExp(matcher).test(tool),
    shell: command => ({tool_name: 'Bash', tool_input: {command}}),
    write: {tool_name: 'apply_patch', tool_input: {command: `*** Begin Patch\n*** Add File: ${adhocTest}\n+x\n*** End Patch\n`}},
    // Codex đọc tệp bằng lệnh shell.
    read: file => ({tool_name: 'Bash', tool_input: {command: `sed -n 1,40p ${file}`}}),
    cwd: subDir,
    env: {},
    blocked: result => result.status === 2,
    // Cho qua thì không chặn; stdout chỉ có thể là lệnh rtk viết lại.
    passed: result => result.status === 0
  },
  {
    name: 'Antigravity',
    groups: antigravityHookGroups,
    matches: (matcher, tool) => new RegExp(matcher).test(tool),
    shell: command => antigravityCall('run_command', {CommandLine: command, Cwd: root}),
    write: antigravityCall('write_to_file', {TargetFile: path.join(root, adhocTest), CodeContent: 'x'}),
    read: file => antigravityCall('read_file', {Path: file}),
    // agy chạy lệnh hook tại thư mục chứa hooks.json.
    cwd: path.join(root, '.agents'),
    env: {},
    // agy đọc quyết định JSON trên stdout: "deny" chặn, "ask" để Antigravity xử lý quyền như bình thường.
    blocked: result => result.status === 0 && decisionOf(result) === 'deny',
    passed: result => result.status === 0 && decisionOf(result) === 'ask'
  }
];

// Chạy các lệnh hook gọi script trong .agents/hooks/ (bỏ qua hook Gortex) đúng như công cụ chạy: qua shell, với payload trên stdin.
function runHooks(tool, event, payload) {
  const toolName = payload?.tool_name ?? payload?.toolCall?.name;
  return tool.groups()
    .filter(group => group.event === event && (payload === null || tool.matches(group.matcher, toolName)))
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

test('Harness: Hook rtk (Codex) qua guarded-rtk.mjs - lệnh bị cấm chặn trước rtk; chỉ lệnh shell mới chuyển cho rtk; máy chưa cài rtk thì im lặng', () => {
  const script = path.join(root, '.agents/hooks/guarded-rtk.mjs');
  const run = (agent, payload, env = process.env) => spawnSync(process.execPath, [script, agent], {input: JSON.stringify(payload), env, encoding: 'utf8'});
  // Payload đúng dạng Codex gửi (rtk hook codex cần đủ các trường này).
  const codex = (tool_name, tool_input) => ({session_id: 's', transcript_path: null, cwd: root, hook_event_name: 'PreToolUse', model: 'gpt', permission_mode: 'default', turn_id: 't', tool_name, tool_use_id: 'u', tool_input});
  const addFile = file => codex('apply_patch', {command: `*** Begin Patch\n*** Add File: ${file}\n+x\n*** End Patch\n`});

  // Lệnh bị cấm: chặn bằng mã 2, rtk không kịp trả quyết định "allow".
  for (const [agent, payload] of [
    ['codex', codex('Bash', {command: 'git add .'})],
    ['codex', codex('Bash', {command: 'rm -rf /'})],
    ['codex', addFile('TA-ITS015-WEBVUE-V1.0/src/a.test.ts')]
  ]) {
    const blocked = run(agent, payload);
    assert.equal(blocked.status, 2, `${agent} ${JSON.stringify(payload.tool_input)}`);
    assert.equal(blocked.stdout, '');
  }

  // Tool không phải shell (apply_patch): không chuyển cho rtk, vì rtk tự trả "allow" là lách hệ thống quyền.
  const write = run('codex', addFile('README.md'));
  assert.equal(write.status, 0);
  assert.equal(write.stdout, '');

  // Máy chưa cài rtk: lệnh hợp lệ cho qua, không in gì.
  const withoutRtk = run('codex', codex('Bash', {command: 'git status'}), {...process.env, PATH: '/usr/bin:/bin'});
  assert.equal(withoutRtk.status, 0);
  assert.equal(withoutRtk.stdout, '');

  // Máy có rtk: lệnh shell hợp lệ được viết lại.
  if (spawnSync('/bin/sh', ['-c', 'command -v rtk']).status !== 0) return;
  assert.match(JSON.parse(run('codex', codex('Bash', {command: 'git status'})).stdout).hookSpecificOutput.updatedInput.command, /^rtk git status/);
});

test('Harness: Hook rtk (Antigravity) qua guarded-rtk.mjs - lệnh bị cấm trả "deny" trước rtk; lệnh shell được rtk viết lại nhưng vẫn để Antigravity hỏi quyền ("ask"); máy chưa cài rtk thì "ask"', () => {
  const script = path.join(root, '.agents/hooks/guarded-rtk.mjs');
  const run = (payload, env = process.env) => JSON.parse(spawnSync(process.execPath, [script, 'antigravity'], {input: JSON.stringify(payload), env, encoding: 'utf8'}).stdout);

  for (const payload of [
    antigravityCall('run_command', {CommandLine: 'git add .'}),
    antigravityCall('run_command', {CommandLine: 'rm -rf /'}),
    antigravityCall('write_to_file', {TargetFile: path.join(root, adhocTest), CodeContent: 'x'}),
    antigravityCall('edit_file', {Path: path.join(root, adhocTest), Target: 'a', Replacement: 'b'})
  ]) {
    const output = run(payload);
    assert.equal(output.decision, 'deny', JSON.stringify(payload.toolCall));
    assert.match(output.reason, /BLOCKED by ThienAn/);
  }

  // Tool không phải shell: không chuyển cho rtk.
  assert.deepEqual(run(antigravityCall('write_to_file', {TargetFile: path.join(root, 'README.md'), CodeContent: 'x'})), {decision: 'ask'});

  // Máy chưa cài rtk: cho qua, Antigravity hỏi quyền như bình thường.
  assert.deepEqual(run(antigravityCall('run_command', {CommandLine: 'git status'}), {...process.env, PATH: '/usr/bin:/bin'}), {decision: 'ask'});

  // Máy có rtk: lấy lệnh rtk viết lại; không nhận "allow" của rtk (tự chạy, không hỏi quyền), kể cả lệnh rtk không viết lại.
  if (spawnSync('/bin/sh', ['-c', 'command -v rtk']).status !== 0) return;
  const rewritten = run(antigravityCall('run_command', {CommandLine: 'git status'}));
  assert.equal(rewritten.decision, 'ask');
  assert.match(rewritten.overwrite.CommandLine, /^rtk git status/);
  assert.deepEqual(run(antigravityCall('run_command', {CommandLine: 'npm install left-pad'})), {decision: 'ask'});
});

test('Harness: Hook - cấu hình Claude Code, Codex, Antigravity gọi đúng .agents/hooks: chặn vi phạm P0, chặn lệnh phá ổ đĩa, lệnh hợp lệ cho qua', {skip: process.platform === 'win32' && 'lệnh hook dùng cú pháp shell POSIX'}, () => {
  for (const tool of hookTools) {
    for (const command of tool.groups().flatMap(group => group.commands).filter(command => command?.includes('.agents/hooks/'))) {
      const script = command.match(/\.agents\/hooks\/[\w.-]+\.mjs/)?.[0];
      assert.ok(script && fs.existsSync(path.join(root, script)), `${tool.name}: hook trỏ tới script không tồn tại: ${command}`);
    }

    const blocked = payload => runHooks(tool, 'PreToolUse', payload).some(tool.blocked);
    assert.ok(blocked(tool.shell('git add .')), `${tool.name}: chưa nối guardrails.mjs cho lệnh shell`);
    assert.ok(blocked(tool.shell('rm -rf /')), `${tool.name}: chưa nối validate-tool-call.mjs cho lệnh shell`);
    assert.ok(blocked(tool.write), `${tool.name}: chưa nối guardrails.mjs cho thao tác ghi tệp`);

    const results = runHooks(tool, 'PreToolUse', tool.shell('git status'));
    assert.ok(results.length > 0, `${tool.name}: lệnh shell không đi qua hook nào`);
    for (const result of results) {
      assert.ok(tool.passed(result), `${tool.name} ${result.command}: lệnh hợp lệ bị chặn hoặc hook lỗi: ${result.stdout}${result.stderr}`);
    }
  }
});

test('Harness: Hook - Claude Code, Codex, Antigravity cùng chặn đọc, tìm mã nguồn đã index bằng tool có sẵn (gortex-enforce); tài liệu cho qua', {skip: process.platform === 'win32' && 'lệnh hook dùng cú pháp shell POSIX'}, () => {
  for (const tool of hookTools) {
    assert.ok(runHooks(tool, 'PreToolUse', tool.read(codeFile)).some(tool.blocked), `${tool.name}: chưa nối gortex-enforce cho thao tác đọc mã nguồn`);
    assert.ok(runHooks(tool, 'PreToolUse', tool.read(path.join(root, 'AGENTS.md'))).every(tool.passed), `${tool.name}: đọc tài liệu không được bị chặn`);
  }

  // Antigravity còn tìm bằng grep_search, find_by_name: xét như Grep, Glob của Claude Code.
  const antigravity = hookTools.find(tool => tool.name === 'Antigravity');
  const search = (name, args) => runHooks(antigravity, 'PreToolUse', antigravityCall(name, args));
  assert.ok(search('grep_search', {SearchPath: path.join(root, 'TA-ITS015-WEBAPI-V1.0/src'), Query: 'Program', Includes: ['*.cs']}).some(antigravity.blocked), 'Antigravity: grep_search trong mã nguồn phải bị chặn');
  assert.ok(search('find_by_name', {SearchDirectory: root, Pattern: '*.vue'}).some(antigravity.blocked), 'Antigravity: find_by_name tìm tệp mã phải bị chặn');
  assert.ok(search('grep_search', {SearchPath: path.join(root, 'DocBusinessThienAn'), Query: 'ShareData', Includes: ['*.md']}).every(antigravity.passed), 'Antigravity: tìm trong tài liệu không được bị chặn');
});

test('Harness: Antigravity - .agents/hooks.json đúng định dạng agy: mỗi khóa cấp trên là một hook có tên, sự kiện hợp lệ, PreToolUse lồng matcher + hooks', () => {
  const grouped = new Set(['PreToolUse', 'PostToolUse']);
  const flat = new Set(['SessionStart', 'PreInvocation', 'PostInvocation', 'Stop']);
  for (const [name, spec] of Object.entries(readJsonFile('.agents/hooks.json'))) {
    // agy bỏ qua cả tệp khi một khóa cấp trên không phải hook ("cannot unmarshal ... into JSONHookSpec").
    assert.ok(spec && typeof spec === 'object' && !Array.isArray(spec), `.agents/hooks.json: "${name}" phải là một hook có tên (object)`);
    for (const [event, list] of Object.entries(spec)) {
      if (event === 'enabled') continue;
      assert.ok(grouped.has(event) || flat.has(event), `hook "${name}": sự kiện không hợp lệ ${event}`);
      for (const item of list) {
        if (grouped.has(event)) assert.ok(typeof item.matcher === 'string' && Array.isArray(item.hooks), `hook "${name}" ${event}: cần matcher và hooks`);
        else assert.equal(typeof item.command, 'string', `hook "${name}" ${event}: mỗi phần tử là một lệnh`);
      }
    }
  }
});

test('Harness: Codex, Antigravity nhận phần nạp sẵn qua hook SessionStart như Claude Code - đủ tệp dòng @ của AGENTS.md; Antigravity bỏ rule, skill vì tự nạp', {skip: process.platform === 'win32' && 'lệnh hook dùng cú pháp shell POSIX'}, () => {
  const imported = [...followImports('AGENTS.md', new Set())].filter(file => file !== 'AGENTS.md');
  const sessionOutput = name => {
    const tool = hookTools.find(item => item.name === name);
    const results = runHooks(tool, 'SessionStart', null).filter(result => result.command.includes('session-context.mjs'));
    assert.equal(results.length, 1, `${name}: chưa nối session-context.mjs cho SessionStart`);
    assert.equal(results[0].status, 0, results[0].stderr);
    return JSON.parse(results[0].stdout);
  };

  // Codex chỉ tự đọc AGENTS.md: nhận đủ các tệp dòng @, nội dung như Claude Code nạp.
  const codexText = sessionOutput('Codex').hookSpecificOutput.additionalContext;
  for (const file of imported) {
    assert.ok(codexText.includes(`Contents of ${file}:\n\n${loadedText(file).trim()}`), `Codex chưa nạp ${file}`);
  }
  // Codex mặc định cắt additionalContext ở 2.500 token: giới hạn phải đủ cho phần nạp sẵn (ước lượng ≤ 1 token / 2 ký tự).
  const session = codexHookGroups().find(group => group.event === 'SessionStart' && group.commands.some(command => command.includes('session-context.mjs')));
  assert.ok(session.additionalContextLimit >= Math.ceil([...codexText].length / 2), `Codex: additionalContextLimit ${session.additionalContextLimit} quá nhỏ`);
  for (const source of ['startup', 'clear', 'compact']) {
    assert.ok(new RegExp(session.matcher).test(source), `Codex: SessionStart chưa nạp lại khi ${source}`);
  }

  // Antigravity tự nạp .agents/rules/ (rule trigger: always_on) và .agents/skills/: chỉ bơm các tệp còn lại.
  const antigravityText = sessionOutput('Antigravity').injectSteps[0].systemMessage.systemMessage;
  for (const file of imported) {
    const native = /^\.agents\/(?:rules|skills)\//.test(file);
    assert.equal(antigravityText.includes(`Contents of ${file}:`), !native, `Antigravity: ${file} ${native ? 'đã tự nạp, không bơm lại' : 'chưa nạp'}`);
    if (file.startsWith('.agents/rules/')) {
      assert.match(fs.readFileSync(path.join(root, file), 'utf8'), /^---\n(?:.*\n)*?trigger: always_on\n(?:.*\n)*?---\n/, `${file}: Antigravity chỉ tự nạp rule trigger: always_on`);
    }
  }
});

