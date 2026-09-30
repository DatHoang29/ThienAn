<#
    Description: Phuc hoi cac tep .cs bi nhan doi dong trong (moi dong bi chen them 1 dong trong).
                 Nhan biet theo VUNG: chi thu gon nhung doan xen ke code/blank du dai; vung code
                 binh thuong (co 2 dong code lien tiep) duoc giu nguyen tuyet doi.
                 Bat bien: tap hop cac dong KHONG rong phai y nguyen truoc/sau khi sua.
    Created date: 30/09/2026
#>
param(
    [string]$Root = 'C:\ThienAn\tests',
    [int]$MinCodeLines = 6,
    [switch]$Apply
)

function Convert-Content {
    param([string[]]$lines, [int]$minCode)

    $out = New-Object System.Collections.Generic.List[string]
    $kept = New-Object System.Collections.Generic.List[string]
    $n = $lines.Count
    $i = 0

    while ($i -lt $n) {
        if ($lines[$i].Trim().Length -eq 0) { $out.Add($lines[$i]); $i++; continue }

        # Do do dai doan xen ke bat dau tai $i: code, blank-run(le), code, blank-run(le), ...
        $j = $i
        $codeCount = 0
        while ($j -lt $n -and $lines[$j].Trim().Length -gt 0) {
            $codeCount++
            $k = $j + 1
            $blank = 0
            while ($k -lt $n -and $lines[$k].Trim().Length -eq 0) { $blank++; $k++ }
            if ($blank -eq 0) { break }          # 2 dong code ke nhau -> het doan xen ke
            if ($blank % 2 -eq 0) { break }      # blank-run chan -> khong phai dau vet nhan doi
            if ($k -ge $n) { $j = $k; break }
            $j = $k
        }
        $end = $j

        if ($codeCount -ge $minCode) {
            # Vung nhan doi: giu dong code, blank-run do dai k -> floor(k/2)
            $p = $i
            while ($p -lt $end -and $p -lt $n) {
                if ($lines[$p].Trim().Length -gt 0) { $out.Add($lines[$p]); $p++ }
                else {
                    $b = 0
                    while ($p -lt $n -and $lines[$p].Trim().Length -eq 0) { $b++; $p++ }
                    for ($q = 0; $q -lt [math]::Floor($b / 2); $q++) { $out.Add('') }
                }
            }
            $i = $p
        }
        else {
            # Vung code binh thuong: sao chep nguyen van
            $p = $i
            $stop = [math]::Max($end, $i + 1)
            while ($p -lt $stop -and $p -lt $n) { $out.Add($lines[$p]); $kept.Add("L$($p+1)"); $p++ }
            $i = $p
        }
    }

    return ,@($out.ToArray(), $kept.Count)
}

$files = Get-ChildItem -Path $Root -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$report = @()

foreach ($f in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $text = [System.IO.File]::ReadAllText($f.FullName)
    $lines = $text -split "`r`n", 0

    $res = Convert-Content -lines $lines -minCode $MinCodeLines
    $newLines = $res[0]
    $keptLines = $res[1]

    if ($newLines.Count -eq $lines.Count) { continue }

    # --- Bat bien: moi dong KHONG rong phai y nguyen, dung thu tu ---
    $beforeCode = @($lines  | Where-Object { $_.Trim().Length -gt 0 })
    $afterCode  = @($newLines | Where-Object { $_.Trim().Length -gt 0 })
    $invariantOk = ($beforeCode.Count -eq $afterCode.Count)
    if ($invariantOk) {
        for ($x = 0; $x -lt $beforeCode.Count; $x++) {
            if ($beforeCode[$x] -ne $afterCode[$x]) { $invariantOk = $false; break }
        }
    }

    $rel = $f.FullName.Replace('C:\ThienAn\', '')
    $report += [pscustomobject]@{
        File      = $rel
        Before    = $lines.Count
        After     = $newLines.Count
        KeptAsIs  = $keptLines
        Invariant = if ($invariantOk) { 'OK' } else { 'VI PHAM' }
    }

    if ($Apply -and $invariantOk) {
        $enc = New-Object System.Text.UTF8Encoding($hasBom)
        [System.IO.File]::WriteAllText($f.FullName, ($newLines -join "`r`n"), $enc)
    }
}

$report | Sort-Object File | Format-Table -AutoSize | Out-String -Width 150
$bad = @($report | Where-Object { $_.Invariant -ne 'OK' })
Write-Output "Tong: $($report.Count) tep se doi. Vi pham bat bien: $($bad.Count)."
if ($Apply) { Write-Output 'DA GHI (chi nhung tep bat bien OK).' } else { Write-Output 'CHAY THU - chua ghi gi. Them -Apply de ghi.' }
