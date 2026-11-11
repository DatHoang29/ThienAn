#!/usr/bin/env node

// Hook bảo vệ hạn ngạch phiên làm việc (Idle Guard):
// Chặn người dùng gửi prompt khi phiên đã Idle quá lâu (Prompt Cache Anthropic hết hạn sau 1 giờ)
// VÀ ngữ cảnh phiên đang rất nặng (>= 100k tokens), tránh việc bị phạt nạp lại toàn bộ ngữ cảnh
// gây cạn 10% - 25% hạn ngạch 5-giờ trong 1 tin nhắn duy nhất.

import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

export const DEFAULT_IDLE_MINUTES = 45; // Ngưỡng an toàn (cache server Anthropic hết sau 60 phút)
export const DEFAULT_TOKEN_THRESHOLD = 100_000; // Ngưỡng ngữ cảnh nặng (100k tokens)
export const DEFAULT_FILE_SIZE_THRESHOLD = 500_000; // 500 KB transcript nếu không có usage

function readTail(filePath, maxBytes = 1024 * 1024) {
  try {
    const stat = fs.statSync(filePath);
    const size = stat.size;
    const bytesToRead = Math.min(size, maxBytes);
    const buffer = Buffer.alloc(bytesToRead);
    const fd = fs.openSync(filePath, 'r');
    fs.readSync(fd, buffer, 0, bytesToRead, Math.max(0, size - bytesToRead));
    fs.closeSync(fd);
    return { text: buffer.toString('utf8'), size };
  } catch {
    return { text: '', size: 0 };
  }
}

export function inspectTranscript(filePath, now = Date.now()) {
  const { text, size } = readTail(filePath);
  if (!text) return { idleMinutes: 0, tokens: 0, size };

  const lines = text.trim().split(/\r?\n/);
  let tokens = 0;
  let lastActiveMs = null;

  for (let i = lines.length - 1; i >= 0; i--) {
    const line = lines[i].trim();
    if (!line) continue;
    try {
      const obj = JSON.parse(line);
      // Tìm số tokens gần nhất từ trường usage
      if (!tokens) {
        const u = obj.message?.usage || obj.usage;
        if (u) {
          const sum = (u.cache_read_input_tokens || 0) + (u.cache_creation_input_tokens || 0) + (u.input_tokens || 0);
          if (sum > 0) tokens = sum;
        }
      }
      // Tìm timestamp của lượt assistant gần nhất (thời điểm server Anthropic hoàn tất phản hồi lượt trước)
      const isAssistant = obj.type === 'assistant' || obj.message?.role === 'assistant';
      if (lastActiveMs === null && isAssistant && obj.timestamp) {
        const time = new Date(obj.timestamp).getTime();
        if (!Number.isNaN(time) && time <= now) {
          lastActiveMs = time;
        }
      }
    } catch {
      // bỏ qua dòng JSON lỗi
    }
  }

  // Fallback nếu không có tin nhắn assistant (ví dụ lượt system trước đó)
  if (lastActiveMs === null) {
    for (let i = lines.length - 1; i >= 0; i--) {
      try {
        const obj = JSON.parse(lines[i].trim());
        if (obj.timestamp) {
          const time = new Date(obj.timestamp).getTime();
          if (!Number.isNaN(time) && time <= now && now - time > 15_000) {
            lastActiveMs = time;
            break;
          }
        }
      } catch {}
    }
  }

  const idleMs = lastActiveMs !== null ? Math.max(0, now - lastActiveMs) : 0;
  const idleMinutes = Math.floor(idleMs / 60_000);

  return { idleMinutes, tokens, size };
}

export function evaluateIdleGuard(payload, options = {}) {
  const prompt = String(payload?.prompt ?? '').trim();
  const transcriptPath = payload?.transcript_path ?? payload?.transcriptPath ?? '';
  const now = options.now ?? Date.now();
  const idleThreshold = options.idleThresholdMinutes ?? DEFAULT_IDLE_MINUTES;
  const tokenThreshold = options.tokenThreshold ?? DEFAULT_TOKEN_THRESHOLD;
  const sizeThreshold = options.sizeThreshold ?? DEFAULT_FILE_SIZE_THRESHOLD;

  // 1. Cho qua khi có cờ bỏ qua hoặc lệnh compact / clear
  if (process.env.SKIP_IDLE_GUARD === '1' || options.skip === true) {
    return { allowed: true, reason: 'skipped by env flag' };
  }
  if (prompt.startsWith('/compact') || prompt.startsWith('/clear')) {
    return { allowed: true, reason: 'compact/clear command' };
  }
  if (prompt.includes('--force') || prompt.includes('!force')) {
    return { allowed: true, reason: 'force override' };
  }

  // 2. Không có transcript -> cho qua
  if (!transcriptPath || !fs.existsSync(transcriptPath)) {
    return { allowed: true, reason: 'no transcript available' };
  }

  // 3. Kiểm tra độ trễ (idle) và kích thước phiên
  const { idleMinutes, tokens, size } = inspectTranscript(transcriptPath, now);

  const isIdle = idleMinutes >= idleThreshold;
  const isHeavy = tokens >= tokenThreshold || (tokens === 0 && size >= sizeThreshold);

  if (isIdle && isHeavy) {
    const hours = Math.floor(idleMinutes / 60);
    const mins = idleMinutes % 60;
    const idleDisplay = hours > 0 ? `${hours} giờ ${mins} phút` : `${mins} phút`;
    const tokenDisplay = tokens > 0 ? `~${Math.round(tokens / 1000).toLocaleString('vi-VN')}k tokens` : `transcript ${(size / 1024 / 1024).toFixed(1)} MB`;
    const sizeDisplay = size > 1024 * 1024 ? `${(size / 1024 / 1024).toFixed(1)} MB` : `${Math.round(size / 1024)} KB`;

    const message = [
      `⛔ CHẶN BỞI THIENAN IDLE GUARD (Bảo vệ hạn ngạch 5-giờ):`,
      `Phiên làm việc đã nghỉ (Idle): ${idleDisplay} (vượt quá ${idleThreshold} phút — Prompt Cache máy chủ đã hết hạn!).`,
      `Ngữ cảnh phiên hiện tại rất nặng: ${tokenDisplay} (${sizeDisplay}).`,
      ``,
      `Nếu gửi prompt lúc này, hệ thống sẽ nạp lại toàn bộ ${tokenDisplay} từ đầu,`,
      `gây tốn từ 10% đến 25% hạn ngạch 5-giờ của bạn chỉ cho 1 tin nhắn duy nhất!`,
      ``,
      `👉 HÀNH ĐỘNG CẦN LÀM:`,
      `1. Gõ '/compact' để tóm tắt và thu gọn phiên làm việc trước khi hỏi tiếp.`,
      `2. Hoặc mở phiên mới bằng '/clear'.`,
      `(Nếu bạn thực sự muốn tiếp tục gửi mà không compact, hãy thêm '--force' vào cuối câu prompt).`
    ].join('\n');

    return {
      allowed: false,
      idleMinutes,
      tokens,
      size,
      message
    };
  }

  return { allowed: true, idleMinutes, tokens, size, reason: 'within safe limits' };
}

function readStdin() {
  return new Promise((resolve, reject) => {
    let input = '';
    process.stdin.setEncoding('utf8');
    process.stdin.on('data', chunk => {
      input += chunk;
      if (input.length > 2 * 1024 * 1024) {
        reject(new Error('hook payload exceeds 2 MiB'));
      }
    });
    process.stdin.on('end', () => resolve(input));
    process.stdin.on('error', reject);
  });
}

async function main() {
  let raw = '';
  try {
    raw = await readStdin();
  } catch (error) {
    return 0;
  }

  let payload = {};
  try {
    payload = JSON.parse(raw || '{}');
  } catch {
    return 0;
  }

  const result = evaluateIdleGuard(payload);
  if (!result.allowed) {
    console.error(`\n${result.message}\n`);
    // Exit code 2: Claude Code hủy prompt và in stderr ra màn hình
    return 2;
  }

  return 0;
}

if (import.meta.url === `file://${process.argv[1]}`) {
  process.exitCode = await main();
}
