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
  // Hỗ trợ các chuẩn payload:
  // - Antigravity: { tool_name, tool_args: { TargetFile, Path, CommandLine, ... } }
  // - Claude Code: { tool_name, tool_input: { file_path, command, path, ... } }
  const toolName = payload?.tool_name ?? payload?.toolName ?? payload?.tool ?? '';
  const args = payload?.tool_args ?? payload?.toolArgs ?? payload?.tool_input ?? payload?.toolInput ?? payload?.arguments ?? {};
  const targetFile = args.TargetFile ?? args.targetFile ?? args.file_path ?? args.filePath ?? args.path ?? args.Path ?? args.AbsolutePath ?? '';
  const command = args.CommandLine ?? args.commandLine ?? args.command ?? args.cmd ?? '';

  // 1. Guard Rule 19.63: Cấm tạo file test ad-hoc rải rác ngoài tests/FE, tests/BE và .agents/hooks/tests/
  if (targetFile) {
    const normalized = targetFile.replace(/\\/g, '/');
    const isTestFile = /(?:^|\/)[^/]*(?:test|spec)[^/]*\.(?:[cm]?js|ts|py|cs)$/i.test(normalized);
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

    // 3. Guard Rule 19.100: Cấm stage, commit hoặc push các tệp cấu hình local và bí mật
    const secretFilesPattern = /(?:Database\.json|TAKey[A-Za-z0-9_-]*\.key|Shared\.Core\.Security\.dll|\.env\.development|\.env\.local)/i;
    const isGitSecretOp = /\bgit\s+(?:add|commit|stage)\b/i.test(command) && secretFilesPattern.test(command);
    if (isGitSecretOp) {
      return {
        allowed: false,
        rule: 'rule-19.100-no-staging-secrets',
        reason: `⛔ CẤM stage/commit tệp cấu hình local hoặc bí mật (Database.json, TAKey*.key, Shared.Core.Security.dll, .env.development, .env.local) theo Rule 19.100!`
      };
    }

    // 4. Guard Rule 19.100 & 19.58: Cấm chạy 'git add .' / 'git add -A' / 'git commit -a' vô tội vạ làm lộ bí mật
    const isIndiscriminateGitStage = /(?:^|[;&|]\s*)git\s+add\s+(?:-A|\.|\*)(?:\s|$|[;&|])|(?:^|[;&|]\s*)git\s+commit\s+[^\n]*-[A-Za-z]*a/i.test(command);
    if (isIndiscriminateGitStage) {
      return {
        allowed: false,
        rule: 'rule-19.100-no-indiscriminate-staging',
        reason: `⛔ CẤM chạy 'git add .', 'git add -A' hoặc 'git commit -a' vì nguy cơ stage nhầm file cấu hình local hoặc bí mật! Bắt buộc chỉ stage đích danh từng file cần commit theo Rule 19.58 & Rule 19.100.`
      };
    }

    // 5. Guard Rule 19.13: Cấm Auto-Build Frontend sau khi sửa code
    //    Cho phép `build-api` (sinh client API từ Swagger, không phải build Frontend) — 27/10/2026
    const isFrontendBuild = /\b(?:npm|pnpm|yarn)\s+(?:run\s+)?build(?![\w-])/i.test(command) && !/antigravity-plugin/i.test(command);
    if (isFrontendBuild) {
      return {
        allowed: false,
        rule: 'rule-19.13-no-auto-build-frontend',
        reason: `Frontend đã có Vite dev server HMR tự nạp thay đổi. ⛔ CẤM chạy 'npm run build' / 'pnpm build' sau khi sửa FE theo Rule 19.13!`
      };
    }

    // 6. Guard Rule 19.5: Cấm cờ --no-build khi chạy dotnet test
    const isTestNoBuild = /(?:^|[;&|]\s*)dotnet\s+test\b[^\n]*(?:^|\s)--no-build(?:\s|$|[;&|])/i.test(command);
    if (isTestNoBuild) {
      return {
        allowed: false,
        rule: 'rule-19.5-no-test-no-build',
        reason: `⛔ CẤM dùng cờ '--no-build' khi chạy 'dotnet test' để đảm bảo test runner luôn biên dịch đồng bộ mã nguồn mới nhất theo Rule 19.5!`
      };
    }

    // 7. Guard Rule 19.58: Cấm lệnh git hủy hoại mã nguồn (git reset --hard, git clean -fd)
    const isDestructiveGit = /\bgit\s+reset\s+--hard\b|\bgit\s+clean\s+-[a-zA-Z]*f/i.test(command);
    if (isDestructiveGit) {
      return {
        allowed: false,
        rule: 'rule-19.58-no-destructive-git',
        reason: `⛔ CẤM tự ý chạy 'git reset --hard' hoặc 'git clean -fd' vì có thể xóa mất code đang phát triển hoặc cấu hình local của dev theo Rule 19.58!`
      };
    }

    // 8. Guard Rule 19.91: Cấm force-push lên main hoặc dev
    const isForcePushProtected =
      /\bgit\s+push\b[^\n]*(?:--force|-f(?:\s|$|[;&|]))[^\n]*(?:origin\s+)?(?:main|dev)(?:\s|$|[;&|])/i.test(command) ||
      /\bgit\s+push\b[^\n]*(?:origin\s+)?(?:main|dev)[^\n]*(?:--force|-f(?:\s|$|[;&|]))/i.test(command);
    if (isForcePushProtected) {
      return {
        allowed: false,
        rule: 'rule-19.91-no-force-push-protected',
        reason: `⛔ Tuyệt đối CẤM force-push lên nhánh chính 'dev' hoặc 'main' theo Rule 19.91!`
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
