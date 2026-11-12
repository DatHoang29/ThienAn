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

test('Idle Guard: Cho qua khi phiên ĐÃ COMPACT kể cả khi để qua đêm (> 8 tiếng)', () => {
  const now = Date.now();
  const tenHoursAgo = new Date(now - 10 * 60 * 60 * 1000).toISOString();
  const elevenHoursAgo = new Date(now - 11 * 60 * 60 * 1000).toISOString();

  // Mô phỏng: phiên cũ nặng 800k tokens, sau đó được compact thành công
  const { filePath, dir } = createTempTranscript([
    { timestamp: elevenHoursAgo, type: 'assistant', message: { usage: { cache_read_input_tokens: 800_000 } } },
    { timestamp: tenHoursAgo, type: 'system', subtype: 'compact_boundary', content: 'Conversation compacted' },
    { timestamp: tenHoursAgo, type: 'user', message: { role: 'user', content: 'This session is being continued from a previous conversation...' } }
  ]);

  try {
    const result = evaluateIdleGuard({ prompt: 'câu hỏi buổi sáng', transcript_path: filePath }, { now });
    assert.equal(result.allowed, true, 'Phiên đã compact phải được cho qua kể cả khi để qua đêm');
    assert.equal(result.isCompacted, true);
    assert.ok(result.tokens < DEFAULT_TOKEN_THRESHOLD, 'Số token sau compact phải nhỏ hơn ngưỡng nặng');
  } finally {
    fs.rmSync(dir, { recursive: true, force: true });
  }
});

test('Idle Guard: Chặn (allowed: false) khi Idle >= 45 phút VÀ Context >= 100k tokens CHƯA COMPACT', () => {
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

test('Idle Guard: Kiểm tra thực tế với phiên fork 851k tokens lịch sử - Đã Compact sáng nay thì cho qua', () => {
  const historicalSession = '/Users/hoangquydat/.claude/projects/-Users-hoangquydat-ThienAn/f1a2575f-c5c7-439a-a974-7c93e694cf54.jsonl';
  if (!fs.existsSync(historicalSession)) return;

  // Thời điểm 8:05 AM ngày 12/11 khi người dùng gửi prompt sau khi đã compact tối hôm trước
  const morningTime = new Date('2026-11-12T01:05:54.000Z').getTime();

  const inspection = inspectTranscript(historicalSession, morningTime);
  assert.equal(inspection.isCompacted, true, 'Phiên phải được nhận diện là đã compact');
  assert.ok(inspection.tokens < DEFAULT_TOKEN_THRESHOLD, 'Tokens sau compact phải nhỏ hơn 100k');

  const evalAllowed = evaluateIdleGuard({
    prompt: 'các test case của mình đã phủ hết DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/kiem-tai chưa',
    transcript_path: historicalSession
  }, { now: morningTime });

  assert.equal(evalAllowed.allowed, true, 'Phiên đã compact thì không được chặn');
});
