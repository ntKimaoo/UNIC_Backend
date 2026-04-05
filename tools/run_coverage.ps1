#Requires -Version 5.1
<#
.SYNOPSIS
    Chạy unit test + Coverlet, sau đó tạo báo cáo HTML (ReportGenerator) vào thư mục report/.

.DESCRIPTION
    Chỉ chạy test trong UNIC.ServiceTest và UNIC.ControllerTest (không chạy toàn bộ solution).
    1) dotnet test từng project với --collect:"Xplat Code Coverage" (Cobertura).
    2) reportgenerator gộp Cobertura -> report/index.html
       Mặc định chỉ hiển thị 2 assembly: UNIC.BusinessLogic + UNIC.Presentation (ẩn DataAccess, v.v.),
       và ẩn thư mục Presentation\Authorization + BusinessLogic\DTOs (dễ tập trung Service/Controller).

    Lần đầu cần cài ReportGenerator (một lần trên máy):
        dotnet tool install -g dotnet-reportgenerator-globaltool

.PARAMETER Clean
    Xóa TestResults và report trong repo trước khi chạy (tránh gộp nhầm file coverage cũ).

.PARAMETER Open
    Mở report\index.html bằng trình duyệt mặc định sau khi tạo xong.

.PARAMETER NoTest
    Chỉ chạy ReportGenerator (dùng file Cobertura đã có trong TestResults). Hữu ích khi đã chạy test trước đó.

.PARAMETER IncludeAllAssemblies
    Không lọc assembly và không ẩn Authorization/DTOs: báo cáo đầy đủ như Cobertura gốc.

.EXAMPLE
    .\tools\run_coverage.ps1

.EXAMPLE
    .\tools\run_coverage.ps1 -Clean -Open
#>
[CmdletBinding()]
param(
    [switch] $Clean,
    [switch] $Open,
    [switch] $NoTest,
    [switch] $IncludeAllAssemblies
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ReportDir = Join-Path $RepoRoot "report"
$TestResultsDir = Join-Path $RepoRoot "TestResults"

function Test-ReportGenerator {
    $cmd = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $dotnetTools = Join-Path $env:USERPROFILE ".dotnet\tools\reportgenerator.exe"
    if (Test-Path -LiteralPath $dotnetTools) { return $dotnetTools }
    return $null
}

if ($Clean) {
    foreach ($p in @($TestResultsDir, $ReportDir)) {
        if (Test-Path -LiteralPath $p) {
            Write-Host "Removing $p"
            Remove-Item -LiteralPath $p -Recurse -Force
        }
    }
}

Push-Location $RepoRoot
try {
    if (-not $NoTest) {
        $serviceTest = Join-Path $RepoRoot "UNIC.ServiceTest\UNIC.ServiceTest.csproj"
        $controllerTest = Join-Path $RepoRoot "UNIC.ControllerTest\UNIC.ControllerTest.csproj"
        foreach ($p in @($serviceTest, $controllerTest)) {
            if (-not (Test-Path -LiteralPath $p)) {
                throw "Không thấy project test: $p"
            }
        }

        $commonArgs = @(
            '--collect:"Xplat Code Coverage"',
            "--results-directory", $TestResultsDir,
            "--verbosity", "minimal"
        )

        Write-Host "==> dotnet test UNIC.ServiceTest (Cobertura)..."
        dotnet test $serviceTest @commonArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test UNIC.ServiceTest thất bại (exit $LASTEXITCODE)."
        }

        Write-Host "==> dotnet test UNIC.ControllerTest (Cobertura)..."
        dotnet test $controllerTest @commonArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test UNIC.ControllerTest thất bại (exit $LASTEXITCODE)."
        }
    }

    $rg = Test-ReportGenerator
    if (-not $rg) {
        Write-Host ""
        Write-Host "Chưa có ReportGenerator. Cài một lần:" -ForegroundColor Yellow
        Write-Host "  dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
        Write-Host "Sau đó thêm vào PATH: $env:USERPROFILE\.dotnet\tools" -ForegroundColor Yellow
        throw "Không tìm thấy reportgenerator."
    }

    $hits = Get-ChildItem -Path $TestResultsDir -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
    if (-not $hits) {
        throw "Không thấy coverage.cobertura.xml trong TestResults. Chạy lại không dùng -NoTest."
    }

    if (Test-Path -LiteralPath $ReportDir) {
        Write-Host "==> Xóa báo cáo HTML cũ trong report\ ..."
        Get-ChildItem -LiteralPath $ReportDir -Force | Remove-Item -Recurse -Force
    }
    else {
        New-Item -ItemType Directory -Path $ReportDir | Out-Null
    }

    # Sau --results-directory, file nằm dưới TestResults\<guid>\coverage.cobertura.xml
    $reportsPattern = "TestResults/**/coverage.cobertura.xml"

    Write-Host "==> ReportGenerator -> $ReportDir"
    $rgArgs = @(
        "-reports:$reportsPattern",
        "-targetdir:$ReportDir",
        "-reporttypes:Html"
    )
    if (-not $IncludeAllAssemblies) {
        # Chỉ 2 assembly + bỏ Authorization và DTO (file path trong Cobertura dùng backslash)
        $rgArgs += '-assemblyfilters:+UNIC.BusinessLogic;+UNIC.Presentation'
        $rgArgs += '-filefilters:-*\Presentation\Authorization\*;-*\BusinessLogic\DTOs\*'
        Write-Host "    (assembly: BusinessLogic, Presentation | ẩn: Authorization\, DTOs\)"
    }

    & $rg @rgArgs

    if ($LASTEXITCODE -ne 0) {
        throw "reportgenerator thất bại (exit $LASTEXITCODE)."
    }

    $index = Join-Path $ReportDir "index.html"
    Write-Host ""
    Write-Host "Xong. Mở file:" -ForegroundColor Green
    Write-Host "  $index" -ForegroundColor Green

    if ($Open) {
        Start-Process $index
    }
}
finally {
    Pop-Location
}
