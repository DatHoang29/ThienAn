#!/usr/bin/env node

import path from 'node:path';
import process from 'node:process';

function readStdin() {
  return new Promise((resolve, reject) => {
    let input = '';
    process.stdin.setEncoding('utf8');
    process.stdin.on('data', chunk => {
      input += chunk;
      if (input.length > 1024 * 1024) {
        reject(new Error('hook payload exceeds 1 MiB'));
      }
    });
    process.stdin.on('end', () => resolve(input));
    process.stdin.on('error', reject);
  });
}

export function evaluateGuardrail(payload) {
  // Hỗ trợ cả 2 chuẩn payload:
  // - Antigravity: { tool_name, tool_args: { TargetFile, CommandLine, ... } }
  // - Claude Code: { tool_name, tool_input: { file_path, command, path, ... } }
  const toolName = payload?.tool_name ?? payload?.toolName ?? payload?.tool ?? '';
  const args = payload?.tool_args ?? payload?.toolArgs ?? payload?.tool_input ?? payload?.toolInput ?? payload?.arguments ?? {};
  const targetFile = args.TargetFile ?? args.targetFile ?? args.file_path ?? args.filePath ?? args.path ?? args.AbsolutePath ?? '';
  const command = args.CommandLine ?? args.commandLine ?? args.command ?? args.cmd ?? '';

  // 1. Guard Rule 19.63: Cấm tạo file test ad-hoc rải rác ngoài tests/FE và tests/BE
  if (targetFile) {
    const normalized = targetFile.replace(/\\/g, '/');
    const isTestFile = /(?:^|\/)[^/]*(?:test|spec)[^/]*\.(?:[cm]?js|ts|py)$/i.test(normalized);
    if (isTestFile) {
      const isAllowedDir =
        normalized.includes('/tests/FE/') ||
        normalized.includes('/tests/BE/') ||
        normalized.includes('/.agents/hooks/tests/');

      if (!isAllowedDir) {
        return {
          allowed: false,
          rule: 'rule-19.63-no-adhoc-tests',
          reason: `Dự án cấm tạo file test ad-hoc rải rác (${path.basename(normalized)})! Mọi test Frontend BẮT BUỘC viết bằng Playwright tại 'tests/FE/', test Backend tại 'tests/BE/'.`
        };
      }
    }
  }

  // 2. Guard Rule 19.63: Cấm chạy runner test ad-hoc ngoài runner chuẩn
  if (command) {
    const isAdhocNodeTest = /\bnode\s+--test\b/i.test(command) && !command.includes('.agents/hooks/tests');
    if (isAdhocNodeTest) {
      return {
        allowed: false,
        rule: 'rule-19.63-unified-test-runner',
        reason: `Dự án cấm chạy test bằng lệnh 'node --test' ad-hoc! Test Frontend BẮT BUỘC chạy bằng 'pnpm test:e2e' tại 'tests/FE/', Backend bằng 'dotnet test tests/BE/test.csproj'.`
      };
    }
  }

  return { allowed: true, rule: null, reason: 'guardrails passed' };
}

async function main() {
  let raw;
  try {
    raw = await readStdin();
  } catch (error) {
    console.error(`ThienAn hook warning: ${error.message}`);
    return 0;
  }

  let payload;
  try {
    payload = JSON.parse(raw || '{}');
  } catch {
    return 0;
  }

  const result = evaluateGuardrail(payload);
  if (!result.allowed) {
    console.error(`\n⛔ BLOCKED by ThienAn Guardrails (${result.rule}):\n${result.reason}\n`);
    // Exit code 2 tương thích cả Antigravity và Claude Code (Claude Code nhận exit 2 để block tool và feed stderr cho LLM)
    return 2;
  }

  return 0;
}

if (import.meta.url === `file://${process.argv[1]}`) {
  process.exitCode = await main();
}
