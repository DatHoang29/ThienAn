#!/usr/bin/env bash
# Script: setup-symlinks.sh
# Thiết lập Symbolic Links tự động trên macOS / Linux (Single Source of Truth: .agents)

set -e

WORKSPACE_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
AGENTS_ROOT="${WORKSPACE_ROOT}/.agents"

if [ ! -d "${AGENTS_ROOT}" ]; then
  echo "❌ Không tìm thấy thư mục .agents tại ${AGENTS_ROOT}"
  exit 1
fi

create_symlink() {
  local link_path="$1"
  local target_path="$2"
  local parent_dir
  parent_dir="$(dirname "${link_path}")"

  mkdir -p "${parent_dir}"

  if [ -e "${link_path}" ] || [ -L "${link_path}" ]; then
    rm -rf "${link_path}"
  fi

  ln -s "${target_path}" "${link_path}"
  echo "  ✅ [Linked] ${link_path} -> ${target_path}"
}

echo "=== Bắt đầu thiết lập Symbolic Links trên macOS / Linux ==="

# 1. .claude
echo ""
echo "--- Cấu hình .claude ---"
create_symlink "${WORKSPACE_ROOT}/.claude/rules" "${AGENTS_ROOT}/rules"
create_symlink "${WORKSPACE_ROOT}/.claude/agents" "${AGENTS_ROOT}/agent"
create_symlink "${WORKSPACE_ROOT}/.claude/skills" "${AGENTS_ROOT}/skills"
create_symlink "${WORKSPACE_ROOT}/.claude/commands" "${AGENTS_ROOT}/workflows"

# 2. .clinerules
echo ""
echo "--- Cấu hình .clinerules ---"
create_symlink "${WORKSPACE_ROOT}/.clinerules/rules" "${AGENTS_ROOT}/rules"
create_symlink "${WORKSPACE_ROOT}/.clinerules/agent" "${AGENTS_ROOT}/agent"
create_symlink "${WORKSPACE_ROOT}/.clinerules/skills" "${AGENTS_ROOT}/skills"
create_symlink "${WORKSPACE_ROOT}/.clinerules/workflows" "${AGENTS_ROOT}/workflows"
create_symlink "${WORKSPACE_ROOT}/.clinerules/hooks" "${AGENTS_ROOT}/hooks"

# 3. .kiro
echo ""
echo "--- Cấu hình .kiro ---"
create_symlink "${WORKSPACE_ROOT}/.kiro/steering" "${AGENTS_ROOT}/rules"
create_symlink "${WORKSPACE_ROOT}/.kiro/agents" "${AGENTS_ROOT}/agent"
create_symlink "${WORKSPACE_ROOT}/.kiro/skills" "${AGENTS_ROOT}/skills"
create_symlink "${WORKSPACE_ROOT}/.kiro/workflows" "${AGENTS_ROOT}/workflows"

if [ -f "${WORKSPACE_ROOT}/.mcp.json" ]; then
  mkdir -p "${WORKSPACE_ROOT}/.kiro/settings"
  cp -f "${WORKSPACE_ROOT}/.mcp.json" "${WORKSPACE_ROOT}/.kiro/settings/mcp.json"
fi

echo ""
echo "=== Hoàn tất thiết lập tất cả Symbolic Links trên macOS / Linux! ==="
