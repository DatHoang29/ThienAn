# Script: setup-symlinks.ps1
# Thiết lập NTFS Junctions tự động từ .claude, .clinerules, .kiro về .agents (Single Source of Truth)

param (
    [string]$WorkspaceRoot = "c:\ThienAn"
)

$WorkspaceRoot = (Resolve-Path $WorkspaceRoot).Path
$AgentsRoot = Join-Path $WorkspaceRoot ".agents"

if (!(Test-Path $AgentsRoot)) {
    Write-Error "Không tìm thấy thư mục .agents tại $AgentsRoot"
    exit 1
}

function Create-Junction ($linkPath, $targetPath) {
    if (Test-Path $linkPath) {
        $item = Get-Item $linkPath -Force
        # Check if it's already a junction/reparse point
        if ($item.Attributes -match "ReparsePoint") {
            Write-Host "  [Đã tồn tại] Junction: $linkPath"
            return
        }
        # Remove regular directory to replace with junction
        Write-Host "  [Thay thế] Xóa thư mục cũ: $linkPath"
        Remove-Item -Path $linkPath -Recurse -Force
    }
    
    $parent = Split-Path $linkPath -Parent
    if (!(Test-Path $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    New-Item -ItemType Junction -Path $linkPath -Target $targetPath | Out-Null
    Write-Host "  [Liên kết thành công] $linkPath -> $targetPath"
}

Write-Host "=== Bắt đầu thiết lập NTFS Junctions (Single Source of Truth: .agents) ==="

# 1. .claude junctions
Write-Host "`n--- Cấu hình .claude ---"
Create-Junction (Join-Path $WorkspaceRoot ".claude\rules") (Join-Path $AgentsRoot "rules")
Create-Junction (Join-Path $WorkspaceRoot ".claude\agents") (Join-Path $AgentsRoot "agent")
Create-Junction (Join-Path $WorkspaceRoot ".claude\skills") (Join-Path $AgentsRoot "skills")
Create-Junction (Join-Path $WorkspaceRoot ".claude\commands") (Join-Path $AgentsRoot "workflows")

# 2. .clinerules junctions
Write-Host "`n--- Cấu hình .clinerules ---"
Create-Junction (Join-Path $WorkspaceRoot ".clinerules\rules") (Join-Path $AgentsRoot "rules")
Create-Junction (Join-Path $WorkspaceRoot ".clinerules\agent") (Join-Path $AgentsRoot "agent")
Create-Junction (Join-Path $WorkspaceRoot ".clinerules\skills") (Join-Path $AgentsRoot "skills")
Create-Junction (Join-Path $WorkspaceRoot ".clinerules\workflows") (Join-Path $AgentsRoot "workflows")
Create-Junction (Join-Path $WorkspaceRoot ".clinerules\hooks") (Join-Path $AgentsRoot "hooks")

# 3. .kiro junctions
Write-Host "`n--- Cấu hình .kiro ---"
Create-Junction (Join-Path $WorkspaceRoot ".kiro\steering") (Join-Path $AgentsRoot "rules")
Create-Junction (Join-Path $WorkspaceRoot ".kiro\agents") (Join-Path $AgentsRoot "agent")
Create-Junction (Join-Path $WorkspaceRoot ".kiro\skills") (Join-Path $AgentsRoot "skills")
Create-Junction (Join-Path $WorkspaceRoot ".kiro\workflows") (Join-Path $AgentsRoot "workflows")

Write-Host "`n=== Hoàn tất thiết lập tất cả các Junctions! ==="
