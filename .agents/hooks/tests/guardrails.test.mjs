import assert from 'node:assert/strict';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '../../..');

test('Guardrails: Chặn tạo file test ad-hoc ngoài tests/FE và tests/BE (Antigravity payload format)', () => {
  const blocked = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'write_to_file',
      tool_args: { TargetFile: '/Users/hoangquydat/ThienAn/TA-ITS015-WEBVUE-V1.0/tests/temp.test.mjs' }
    }),
    encoding: 'utf8'
  });
  assert.ok(blocked.status !== 0);
  assert.match(blocked.stderr, /BLOCKED by ThienAn Guardrails/);

  const allowedFE = spawnSync(process.execPath, [path.join(root, '.agents/hooks/thienan-guardrails.mjs')], {
    input: JSON.stringify({
      tool_name: 'write_to_file',
      tool_args: { TargetFile: '/Users/hoangquydat/ThienAn/tests/FE/views/sharedata/mapping.test.ts' }
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

test('Prompt Submit Hook: Định hướng SSOT chuẩn JSON không chứa rule tĩnh', () => {
  const hookOutput = spawnSync(process.execPath, [path.join(root, '.agents/hooks/prompt-submit.mjs')], {
    encoding: 'utf8'
  });
  assert.equal(hookOutput.status, 0);
  const parsed = JSON.parse(hookOutput.stdout.trim());
  assert.equal(parsed.hookSpecificOutput?.hookEventName, 'UserPromptSubmit');
  assert.match(parsed.hookSpecificOutput?.additionalContext, /TUÂN THỦ SSOT TẠI \.agents/);
});

