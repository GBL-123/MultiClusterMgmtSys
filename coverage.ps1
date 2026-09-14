param(
    [double]$Threshold = 75
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "== build =="
dotnet build (Join-Path $root "MultiClusterMgmtSys.slnx") --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "build failed" }

$testsExe = Join-Path $root "MultiClusterMgmtSys.Tests\bin\Debug\net10.0\MultiClusterMgmtSys.Tests.exe"
if (-not (Test-Path $testsExe)) { throw "test exe not found: $testsExe" }

$outDir = Join-Path $root "coverage"
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir | Out-Null

$coverageFile = Join-Path $outDir "coverage.cobertura.xml"

Write-Host "== run tests with coverage =="
& $testsExe --coverage --coverage-output-format cobertura --coverage-output $coverageFile
if ($LASTEXITCODE -ne 0) { throw "tests failed" }

if (-not (Test-Path $coverageFile)) {
    $candidate = Get-ChildItem (Join-Path $root "MultiClusterMgmtSys.Tests\bin\Debug\net10.0\TestResults") -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $candidate) { throw "coverage output not found" }
    Copy-Item $candidate.FullName $coverageFile -Force
}

$rgDll = Join-Path $env:USERPROFILE ".nuget\packages\reportgenerator\5.5.11\tools\net10.0\ReportGenerator.dll"
if (-not (Test-Path $rgDll)) { throw "ReportGenerator.dll not found: $rgDll (csproj pins 5.5.11, update script path on upgrade)" }

Write-Host "== generate report =="
& dotnet $rgDll "-reports:$coverageFile" "-targetdir:$(Join-Path $outDir 'report')" "-reporttypes:Html;TextSummary"
if ($LASTEXITCODE -ne 0) { throw "report generation failed" }

$summary = Join-Path $outDir "report\Summary.txt"
$summaryText = Get-Content $summary -Raw
$productLine = ($summaryText -split "`r?`n") | Where-Object { $_ -match "^\s*MultiClusterMgmtSys\s+[\d.,]+%" } | Select-Object -First 1
if (-not $productLine) { throw "MultiClusterMgmtSys assembly row not found in Summary.txt" }
$percentText = [regex]::Match($productLine, "(\d+[\.,]?\d*)%").Groups[1].Value
$percent = [double]($percentText -replace ",", ".")

Write-Host ("== MultiClusterMgmtSys line coverage: {0} % (threshold {1} %) ==" -f $percent, $Threshold)

if ($percent -lt $Threshold) {
    Write-Host ("FAIL: coverage below threshold. Report: {0}" -f (Join-Path $outDir "report\index.html"))
    exit 1
}

Write-Host ("PASS. Report: {0}" -f (Join-Path $outDir "report\index.html"))
exit 0
