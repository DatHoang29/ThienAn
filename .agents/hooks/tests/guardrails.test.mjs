import assert from 'node:assert/strict';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '../../..');

test('Guardrails: Chặn tạo file test ad-hoc ngoài tests/FE và tests/BE (Antigravity payload format)', () => {
  const blocked = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'write_file',
      tool_args: { Path: '/Users/hoangquydat/ThienAn/TA-ITS015-WEBVUE-V1.0/tests/temp.test.mjs' }
    }),
    encoding: 'utf8'
  });
  assert.ok(blocked.status !== 0);
  assert.match(blocked.stderr, /BLOCKED by ThienAn Guardrails/);

  const allowedFE = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'write_file',
      tool_args: { Path: '/Users/hoangquydat/ThienAn/tests/FE/views/sharedata/mapping.test.ts' }
    }),
    encoding: 'utf8'
  });
  assert.equal(allowedFE.status, 0);
});

test('Guardrails: Chặn tạo file test ad-hoc (Claude Code / Codex payload format)', () => {
  const blocked = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'Write',
      tool_input: { file_path: 'TA-ITS015-WEBVUE-V1.0/tests/adhoc.spec.ts' }
    }),
    encoding: 'utf8'
  });
  assert.ok(blocked.status !== 0);
  assert.match(blocked.stderr, /BLOCKED by ThienAn Guardrails/);

  const allowedBE = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'Write',
      tool_input: { file_path: 'tests/BE/test.csproj' }
    }),
    encoding: 'utf8'
  });
  assert.equal(allowedBE.status, 0);
});

test('Guardrails: Chặn lệnh chạy node --test ad-hoc ngoài tests chuẩn', () => {
  const blocked = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'run_command',
      tool_args: { CommandLine: 'node --test my-test.js' }
    }),
    encoding: 'utf8'
  });
  assert.ok(blocked.status !== 0);
  assert.match(blocked.stderr, /BLOCKED by ThienAn Guardrails/);
});

test('Guardrails: Rule 19.100 - Chặn stage/commit file bí mật và local config', () => {
  const secrets = ['Database.json', 'TAKey_Demo.key', 'Shared.Core.Security.dll', '.env.development', '.env.local'];
  for (const secret of secrets) {
    const res = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
      input: JSON.stringify({
        tool_name: 'run_command',
        tool_args: { CommandLine: `git add src/${secret}` }
      }),
      encoding: 'utf8'
    });
    assert.ok(res.status !== 0, `Expected 'git add src/${secret}' to be blocked`);
    assert.match(res.stderr, /rule-19.100-no-staging-secrets/);
  }

  // Cho phép git add file thường
  const allowed = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'run_command',
      tool_args: { CommandLine: 'git add src/MyHandler.cs' }
    }),
    encoding: 'utf8'
  });
  assert.equal(allowed.status, 0);
});

test('Guardrails: Rule 19.100 & 19.58 - Chặn git add . / git add -A / git commit -a', () => {
  const blockedCommands = ['git add .', 'git add -A', 'git add *', 'git commit -am "test"'];
  for (const cmd of blockedCommands) {
    const res = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
      input: JSON.stringify({
        tool_name: 'run_command',
        tool_args: { CommandLine: cmd }
      }),
      encoding: 'utf8'
    });
    assert.ok(res.status !== 0, `Expected '${cmd}' to be blocked`);
    assert.match(res.stderr, /rule-19.100-no-indiscriminate-staging/);
  }
});

test('Guardrails: Rule 19.13 - Chặn npm/pnpm/yarn run build cho Frontend', () => {
  const blockedBuilds = ['npm run build', 'pnpm build', 'yarn build', 'cd TA-ITS015-WEBVUE-V1.0 && pnpm run build'];
  for (const cmd of blockedBuilds) {
    const res = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
      input: JSON.stringify({
        tool_name: 'run_command',
        tool_args: { CommandLine: cmd }
      }),
      encoding: 'utf8'
    });
    assert.ok(res.status !== 0, `Expected '${cmd}' to be blocked`);
    assert.match(res.stderr, /rule-19.13-no-auto-build-frontend/);
  }

  // Cho phép build plugin antigravity
  const allowed = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'run_command',
      tool_args: { CommandLine: 'npm run build:antigravity-plugin' }
    }),
    encoding: 'utf8'
  });
  assert.equal(allowed.status, 0);
});

test('Guardrails: Rule 19.5 - Chặn cờ --no-build khi chạy dotnet test', () => {
  const blocked = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'run_command',
      tool_args: { CommandLine: 'dotnet test tests/BE/test.csproj --no-build' }
    }),
    encoding: 'utf8'
  });
  assert.ok(blocked.status !== 0);
  assert.match(blocked.stderr, /rule-19.5-no-test-no-build/);

  // Cho phép chạy dotnet test bình thường
  const allowed = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'run_command',
      tool_args: { CommandLine: 'dotnet test tests/BE/test.csproj --filter FullyQualifiedName~MyTest' }
    }),
    encoding: 'utf8'
  });
  assert.equal(allowed.status, 0);
});

test('Guardrails: Rule 19.58 & 19.91 - Chặn git reset --hard, git clean -fd và force push lên dev/main', () => {
  const destructive = ['git reset --hard HEAD~1', 'git clean -fd', 'git push origin main --force', 'git push -f origin dev'];
  for (const cmd of destructive) {
    const res = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
      input: JSON.stringify({
        tool_name: 'run_command',
        tool_args: { CommandLine: cmd }
      }),
      encoding: 'utf8'
    });
    assert.ok(res.status !== 0, `Expected '${cmd}' to be blocked`);
  }

  // Cho phép force-with-lease trên nhánh cá nhân
  const allowedPush = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'run_command',
      tool_args: { CommandLine: 'git push origin feat/my-feature --force-with-lease' }
    }),
    encoding: 'utf8'
  });
  assert.equal(allowedPush.status, 0);
});

test('Prompt Submit Hook: Định hướng SSOT chuẩn JSON không chứa rule tĩnh', () => {
  const hookOutput = spawnSync(process.execPath, [path.join(root, '.agents/hooks/prompt-submit.mjs')], {
    encoding: 'utf8'
  });
  assert.equal(hookOutput.status, 0);
  const parsed = JSON.parse(hookOutput.stdout.trim());
  assert.equal(parsed.hookSpecificOutput?.hookEventName, 'UserPromptSubmit');
  assert.match(parsed.hookSpecificOutput?.additionalContext, /TUÂN THỦ SSOT TẠI \.agents/);
});

test('Hooks: Hoạt động chính xác khi cwd là thư mục con với CLAUDE_PROJECT_DIR', () => {
  const subCwd = path.join(root, 'tests/BE');
  const env = { ...process.env, CLAUDE_PROJECT_DIR: root };

  // 1. prompt-submit
  const promptRes = spawnSync(process.execPath, [path.join(root, '.agents/hooks/prompt-submit.mjs')], {
    cwd: subCwd,
    env,
    encoding: 'utf8'
  });
  assert.equal(promptRes.status, 0);
  assert.equal(promptRes.stderr, '');
  assert.match(promptRes.stdout, /UserPromptSubmit/);

  // 2. thienan-guardrails
  const guardRes = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    cwd: subCwd,
    env,
    input: JSON.stringify({ tool_name: 'Bash', tool_input: { command: 'git status' } }),
    encoding: 'utf8'
  });
  assert.equal(guardRes.status, 0);
  assert.equal(guardRes.stderr, '');

  // 3. gortex-enforce session-start
  const sessionRes = spawnSync(process.execPath, [path.join(root, '.agents/hooks/gortex-enforce.mjs'), 'session-start'], {
    cwd: subCwd,
    env,
    encoding: 'utf8'
  });
  assert.equal(sessionRes.status, 0);
  assert.equal(sessionRes.stderr, '');
  assert.match(sessionRes.stdout, /SessionStart/);

  // 4. gortex-enforce pre-tool-use (cho qua)
  const enforcePass = spawnSync(process.execPath, [path.join(root, '.agents/hooks/gortex-enforce.mjs'), 'pre-tool-use'], {
    cwd: subCwd,
    env,
    input: JSON.stringify({ tool_name: 'Read', tool_input: { file_path: 'README.md' } }),
    encoding: 'utf8'
  });
  assert.equal(enforcePass.status, 0);
  assert.equal(enforcePass.stderr, '');

  // 5. gortex-enforce pre-tool-use (chặn)
  const enforceBlock = spawnSync(process.execPath, [path.join(root, '.agents/hooks/gortex-enforce.mjs'), 'pre-tool-use'], {
    cwd: subCwd,
    env,
    input: JSON.stringify({ tool_name: 'Read', tool_input: { file_path: 'src/App.cs' } }),
    encoding: 'utf8'
  });
  assert.equal(enforceBlock.status, 2);
  assert.match(enforceBlock.stderr, /bị chặn/);
});
