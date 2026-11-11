import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import test from 'node:test';

import {
  evaluateIdleGuard,
  inspectTranscript,
  DEFAULT_IDLE_MINUTES,
  DEFAULT_TOKEN_THRESHOLD
} from '../idle-guard.mjs';

const root = path.resolve(import.meta.dirname, '../../..');
const scriptPath = path.join(root, '.agents/hooks/idle-guard.mjs');

function createTempTranscript(lines) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'idle-guard-test-'));
  const filePath = path.join(dir, 'session.jsonl');
  fs.writeFileSync(filePath, lines.map(l => JSON.stringify(l)).join('\n') + '\n');
  return { filePath, dir };
}

test('Idle Guard: Cho qua khi có lệnh /compact hoặc /clear hoặc cờ --force', () => {
  const resultCompact = evaluateIdleGuard({ prompt: '/compact', transcript_path: '/non/existent' });
  assert.equal(resultCompact.allowed, true);

  const resultClear = evaluateIdleGuard({ prompt: '/clear', transcript_path: '/non/existent' });
  assert.equal(resultClear.allowed, true);

  const resultForce = evaluateIdleGuard({ prompt: 'lỗi ui --force', transcript_path: '/non/existent' });
  assert.equal(resultForce.allowed, true);

  const resultExclamation = evaluateIdleGuard({ prompt: 'lỗi ui !force', transcript_path: '/non/existent' });
  assert.equal(resultExclamation.allowed, true);

  const resultSkip = evaluateIdleGuard({ prompt: 'lỗi ui', transcript_path: '/non/existent' }, { skip: true });
  assert.equal(resultSkip.allowed, true);
});

test('Idle Guard: Cho qua khi không có tệp transcript hoặc tệp rỗng', () => {
  const resNoFile = evaluateIdleGuard({ prompt: 'hello', transcript_path: '' });
  assert.equal(resNoFile.allowed, true);

  const resMissing = evaluateIdleGuard({ prompt: 'hello', transcript_path: '/tmp/missing-file.jsonl' });
  assert.equal(resMissing.allowed, true);
});

test('Idle Guard: Cho qua khi phiên mới hoặc hoạt động gần đây (< 45 phút)', () => {
  const now = Date.now();
  const tenMinutesAgo = new Date(now - 10 * 60 * 1000).toISOString();
  const { filePath, dir } = createTempTranscript([
    { timestamp: tenMinutesAgo, type: 'assistant', message: { usage: { cache_read_input_tokens: 150_000 } } }
  ]);

  try {
    const result = evaluateIdleGuard({ prompt: 'sửa tiếp', transcript_path: filePath }, { now });
    assert.equal(result.allowed, true);
    assert.equal(result.idleMinutes, 10);
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

test('Idle Guard: Cho qua khi phiên nhẹ (< 100k tokens) kể cả khi idle lâu', () => {
  const now = Date.now();
  const twoHoursAgo = new Date(now - 120 * 60 * 1000).toISOString();
  const { filePath, dir } = createTempTranscript([
    { timestamp: twoHoursAgo, type: 'assistant', message: { usage: { cache_read_input_tokens: 5_000, input_tokens: 500 } } }
  ]);

  try {
    const result = evaluateIdleGuard({ prompt: 'sửa tiếp', transcript_path: filePath }, { now });
    assert.equal(result.allowed, true);
    assert.equal(result.idleMinutes, 120);
    assert.equal(result.tokens, 5500);
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

test('Idle Guard: Chặn (allowed: false) khi Idle >= 45 phút VÀ Context >= 100k tokens', () => {
  const now = Date.now();
  const fiftyMinutesAgo = new Date(now - 50 * 60 * 1000).toISOString();
  const { filePath, dir } = createTempTranscript([
    { timestamp: fiftyMinutesAgo, type: 'assistant', message: { usage: { cache_read_input_tokens: 300_000, input_tokens: 2_000 } } }
  ]);

  try {
    const result = evaluateIdleGuard({ prompt: 'lỗi giao diện', transcript_path: filePath }, { now });
    assert.equal(result.allowed, false);
    assert.equal(result.idleMinutes, 50);
    assert.equal(result.tokens, 302_000);
    assert.match(result.message, /CHẶN BỞI THIENAN IDLE GUARD/);
    assert.match(result.message, /50 phút/);
    assert.match(result.message, /~302k tokens/);
    assert.match(result.message, /\/compact/);
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

test('Idle Guard: Thực thi script qua stdin - Exit code 2 khi bị chặn, Exit code 0 khi cho qua', () => {
  const now = Date.now();
  const twoHoursAgo = new Date(now - 120 * 60 * 1000).toISOString();
  const { filePath, dir } = createTempTranscript([
    { timestamp: twoHoursAgo, type: 'assistant', message: { usage: { cache_read_input_tokens: 500_000 } } }
  ]);

  try {
    // Trường hợp bị chặn: exit code 2
    const blocked = spawnSync(process.execPath, [scriptPath], {
      input: JSON.stringify({
        prompt: 'lỗi ui',
        transcript_path: filePath
      }),
      encoding: 'utf8'
    });
    assert.equal(blocked.status, 2);
    assert.match(blocked.stderr, /CHẶN BỞI THIENAN IDLE GUARD/);
    assert.match(blocked.stderr, /\/compact/);

    // Trường hợp thêm cờ --force: exit code 0
    const allowedForce = spawnSync(process.execPath, [scriptPath], {
      input: JSON.stringify({
        prompt: 'lỗi ui --force',
        transcript_path: filePath
      }),
      encoding: 'utf8'
    });
    assert.equal(allowedForce.status, 0);

    // Trường hợp gõ /compact: exit code 0
    const allowedCompact = spawnSync(process.execPath, [scriptPath], {
      input: JSON.stringify({
        prompt: '/compact',
        transcript_path: filePath
      }),
      encoding: 'utf8'
    });
    assert.equal(allowedCompact.status, 0);
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

test('Idle Guard: Kiểm tra thực tế với phiên fork 851k tokens lịch sử', () => {
  const historicalSession = '/Users/hoangquydat/.claude/projects/-Users-hoangquydat-ThienAn/f1a2575f-c5c7-439a-a974-7c93e694cf54.jsonl';
  if (!fs.existsSync(historicalSession)) return;

  // Thời điểm 6:58 PM (11:58:22 UTC) khi user gửi prompt 'lỗi ui' sau 1h59m idle
  const submissionTime = new Date('2026-11-11T11:58:22.000Z').getTime();

  const inspection = inspectTranscript(historicalSession, submissionTime);
  assert.ok(inspection.idleMinutes >= 115, `Idle phút phải ~119 phút, thực tế: ${inspection.idleMinutes}`);
  assert.ok(inspection.tokens >= 800_000, `Tokens phải >= 800k, thực tế: ${inspection.tokens}`);

  const evalBlocked = evaluateIdleGuard({
    prompt: 'lỗi ui',
    transcript_path: historicalSession
  }, { now: submissionTime });

  assert.equal(evalBlocked.allowed, false);
  assert.match(evalBlocked.message, /CHẶN BỞI THIENAN IDLE GUARD/);
  assert.match(evalBlocked.message, /~855k tokens/);
});
