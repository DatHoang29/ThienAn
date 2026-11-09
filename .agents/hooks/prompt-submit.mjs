#!/usr/bin/env node

/**
 * Universal Prompt Submit Hook — ThienAn AI Harness
 * Định hướng ngắn gọn về SSOT (.agents/) cho mọi AI runtime hỗ trợ prompt hooks.
 * Không chứa nội dung quy tắc tĩnh nhằm tuân thủ triệt để nguyên tắc DRY & SSOT.
 * Mặc định in JSON cho Claude Code, Codex; `--event BeforeAgent` cho Gemini CLI; `--text` in chữ thường cho Kiro (Kiro đưa stdout vào ngữ cảnh).
 */

const args = process.argv.slice(2);
const additionalContext = 'TUÂN THỦ SSOT TẠI .agents/: Tra cứu .agents/rules/thienan_rules.md (Mục 19: P0 Safeguards) & .agents/rules/request-routing.md trước khi xử lý.';
const eventIndex = args.indexOf('--event');

const output = {
  hookSpecificOutput: {
    hookEventName: eventIndex >= 0 ? args[eventIndex + 1] : 'UserPromptSubmit',
    additionalContext
  }
};

process.stdout.write(args.includes('--text') ? `${additionalContext}\n` : JSON.stringify(output) + '\n');
