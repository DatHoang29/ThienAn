import test from 'node:test';
import assert from 'node:assert/strict';
import { evaluate } from '../gortex-enforce.mjs';

const call = (tool_name, tool_input) => evaluate({ tool_name, tool_input });

test('chặn Read file mã nguồn đã index', () => {
  assert.ok(call('Read', { file_path: '/repo/src/Foo.cs' }));
  assert.ok(call('Read', { file_path: '/repo/src/views/a.vue' }));
});

test('cho qua tài liệu, cấu hình, bin/obj, plan, scratchpad', () => {
  assert.equal(call('Read', { file_path: '/repo/README.md' }), null);
  assert.equal(call('Read', { file_path: '/repo/appsettings.json' }), null);
  assert.equal(call('Read', { file_path: '/repo/tests/BE/bin/Foo.cs' }), null);
  assert.equal(call('Read', { file_path: '/Users/x/.claude/plans/p.md' }), null);
  assert.equal(call('Read', { file_path: '/private/tmp/claude-501/scratchpad/a.ts' }), null);
});

test('chặn Grep/Glob lên mã, cho qua khi lọc tài liệu', () => {
  assert.ok(call('Grep', { pattern: 'foo', path: '/repo' }));
  assert.ok(call('Glob', { pattern: '**/*.cs' }));
  assert.equal(call('Grep', { pattern: 'foo', glob: '*.md' }), null);
  assert.equal(call('Glob', { pattern: '**/*.md' }), null);
});

test('Bash: chặn lệnh đọc file mã, cho qua lệnh khác', () => {
  assert.ok(call('Bash', { command: 'grep -n foo src/A.cs' }));
  assert.ok(call('Bash', { command: 'cat src/a.ts' }));
  assert.equal(call('Bash', { command: 'ls tests/BE' }), null);
  assert.equal(call('Bash', { command: 'git status' }), null);
});
