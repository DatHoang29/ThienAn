param(
    [string]$BaseUrl = 'http://localhost:5005',
    [string]$Groups = 'TMS,VDS',
    [string]$SchemaDirectory,
    [string]$CodegenJar = $env:SWAGGER_CODEGEN_JAR,
    [string]$JavaPath = 'java',
    [string]$Models
)
$ErrorActionPreference = 'Stop'
if (!$CodegenJar) { $CodegenJar = Join-Path $PSScriptRoot 'swagger-codegen-cli.jar' }
if (!(Test-Path -LiteralPath $CodegenJar)) { throw 'Swagger Codegen JAR is required.' }
$destination = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../src/api-services'))
$work = Join-Path ([IO.Path]::GetTempPath()) ('tac-api-generate-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $work
$selected = if ($Models) { @($Models.Split(',', [StringSplitOptions]::RemoveEmptyEntries)) } else { @() }
$copies = @()
foreach ($group in $Groups.Split(',', [StringSplitOptions]::RemoveEmptyEntries)) {
    if ($group -notmatch '^[A-Za-z0-9]+$') { throw 'Invalid API group.' }
    $name = $group.ToLowerInvariant()
    $schema = if ($SchemaDirectory) { Join-Path $SchemaDirectory "$name.swagger.json" } else { "$BaseUrl/swagger/$group/swagger.json" }
    $output = Join-Path $work $name
    & $JavaPath -jar $CodegenJar generate -i $schema -l typescript-axios -o $output --type-mappings DateTime=Date
    if ($LASTEXITCODE -ne 0) { throw "Generation failed for $group. Existing clients were preserved." }
    foreach ($file in Get-ChildItem -LiteralPath $output -Recurse -File -Filter '*.ts') {
        $relative = [IO.Path]::GetRelativePath($output, $file.FullName)
        if ($selected.Count -gt 0 -and $file.BaseName -notin $selected) { continue }
        $copies += @{ Source = $file.FullName; Target = Join-Path (Join-Path $destination $name) $relative }
    }
}
if ($selected.Count -gt 0) {
    foreach ($model in $selected) {
        if (!($copies | Where-Object { [IO.Path]::GetFileNameWithoutExtension($_.Source) -eq $model })) {
            throw "Model $model was not generated. Existing clients were preserved."
        }
    }
}
# Copy only after every requested group has generated successfully. Do not delete client trees.
foreach ($copy in $copies) {
    $null = New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($copy.Target))
    Copy-Item -LiteralPath $copy.Source -Destination $copy.Target
    if ((Get-FileHash -LiteralPath $copy.Source).Hash -ne (Get-FileHash -LiteralPath $copy.Target).Hash) {
        throw 'Generated client copy verification failed.'
    }
}
Write-Output "Generated and copied $($copies.Count) TypeScript files. Generator output: $work"
