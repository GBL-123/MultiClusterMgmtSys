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

Write-Host "== evaluate merged line coverage =="
$assemblies = @('MultiClusterMgmtSys.Domain', 'MultiClusterMgmtSys.Application', 'MultiClusterMgmtSys.Infrastructure', 'MultiClusterMgmtSys.Web')
[xml]$coverage = Get-Content $coverageFile -Raw
$coveredLines = 0
$coverableLines = 0
$foundAssemblies = @()
foreach ($package in $coverage.coverage.packages.package) {
    if ($assemblies -notcontains [string]$package.name) { continue }
    $foundAssemblies += [string]$package.name
    foreach ($class in $package.classes.class) {
        foreach ($line in $class.lines.line) {
            $coverableLines++
            if ([int]$line.hits -gt 0) { $coveredLines++ }
        }
    }
}
$missing = @($assemblies | Where-Object { $foundAssemblies -notcontains $_ })
if ($missing.Count -gt 0) { throw ("assembly packages not found in cobertura report: " + ($missing -join ', ')) }
if ($coverableLines -le 0) { throw "coverable lines not found in cobertura report" }
$percent = [Math]::Round(100.0 * $coveredLines / $coverableLines, 1)

Write-Host ("== merged line coverage [Domain+Application+Infrastructure+Web]: {0} % ({1}/{2}), threshold {3} % ==" -f $percent, $coveredLines, $coverableLines, $Threshold)

if ($percent -lt $Threshold) {
    Write-Host ("FAIL: coverage below threshold. Report: {0}" -f (Join-Path $outDir "report\index.html"))
    exit 1
}

Write-Host ("PASS. Report: {0}" -f (Join-Path $outDir "report\index.html"))
exit 0
