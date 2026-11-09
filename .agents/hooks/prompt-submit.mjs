#!/usr/bin/env node

/**
 * Universal Prompt Submit Hook — ThienAn AI Harness
 * Định hướng ngắn gọn về SSOT (.agents/) cho công cụ không tự nạp rule (Codex chỉ đọc AGENTS.md, không nạp lồng dòng @).
 * Không chứa nội dung quy tắc tĩnh nhằm tuân thủ triệt để nguyên tắc DRY & SSOT.
 */

const output = {
  hookSpecificOutput: {
    hookEventName: 'UserPromptSubmit',
    additionalContext: 'TUÂN THỦ SSOT TẠI .agents/: Tra cứu .agents/rules/thienan_rules.md (Mục 19: P0 Safeguards) & .agents/rules/request-routing.md trước khi xử lý.'
  }
};

process.stdout.write(JSON.stringify(output) + '\n');
