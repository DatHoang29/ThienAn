import assert from 'node:assert/strict';
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

test('Harness: validate-tool-call trả mã lỗi khác 0 khi phát hiện lệnh nguy hiểm', () => {
  const result = spawnSync(process.execPath, [path.join(root, '.agents/hooks/validate-tool-call.mjs')], {
    input: JSON.stringify({tool_args: {CommandLine: 'rm -rf /'}}),
    encoding: 'utf8'
  });
  assert.equal(result.status, 1);
  assert.match(result.stderr, /BLOCKED by AG Kit/);
});

