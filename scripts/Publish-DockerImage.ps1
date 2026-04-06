[CmdletBinding()]
param(
    [Parameter()]
    [string]$Tag = 'latest'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Checked {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter()]
        [string[]]$Arguments = @(),

        [Parameter()]
        [string]$WorkingDirectory = (Get-Location).Path
    )

    Write-Host ("> $FilePath $($Arguments -join ' ')") -ForegroundColor DarkGray
    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$FilePath failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
}

function Sync-DirectoryContents {
    param(
        [Parameter(Mandatory)]
        [string]$SourceDirectory,

        [Parameter(Mandatory)]
        [string]$DestinationDirectory
    )

    if (-not (Test-Path $DestinationDirectory)) {
        New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    }

    Get-ChildItem -Path $DestinationDirectory -Force | Remove-Item -Recurse -Force
    Get-ChildItem -Path $SourceDirectory -Force | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $DestinationDirectory -Recurse -Force
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$frontendRoot = Join-Path $repoRoot 'src\ClientApp\WebApp'
$frontendOutput = Join-Path $frontendRoot 'dist\browser'
$apiWwwroot = Join-Path $repoRoot 'src\Services\ApiService\wwwroot'
$artifactsRoot = Join-Path $repoRoot '.artifacts\docker'
$apiOutput = Join-Path $artifactsRoot 'api'
$dockerfilePath = Join-Path $repoRoot 'Dockerfile'
$pnpm = Get-Command pnpm -ErrorAction SilentlyContinue
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$docker = Get-Command docker -ErrorAction SilentlyContinue

if (-not $pnpm) { throw '未找到 pnpm，请先安装 pnpm 或启用 corepack。' }
if (-not $dotnet) { throw '未找到 dotnet CLI，请先安装 .NET SDK。' }
if (-not $docker) { throw '未找到 docker CLI，请先安装 Docker。' }
if (-not (Test-Path $dockerfilePath)) { throw "未找到 Dockerfile: $dockerfilePath" }

Write-Host '开始构建前端静态资源...' -ForegroundColor Cyan
Invoke-Checked -FilePath $pnpm.Source -Arguments @('build') -WorkingDirectory $frontendRoot
if (-not (Test-Path (Join-Path $frontendOutput 'index.html'))) {
    throw "前端构建未生成 index.html: $frontendOutput"
}
Write-Host '同步前端资源到 ApiService/wwwroot ...' -ForegroundColor Cyan
Sync-DirectoryContents -SourceDirectory $frontendOutput -DestinationDirectory $apiWwwroot

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
if (Test-Path $apiOutput) { Remove-Item $apiOutput -Recurse -Force }

Write-Host '开始发布 ApiService ...' -ForegroundColor Cyan
Invoke-Checked -FilePath $dotnet.Source -Arguments @(
    'publish',
    (Join-Path $repoRoot 'src\Services\ApiService\ApiService.csproj'),
    '-c', 'Release',
    '-o', $apiOutput,
    '/p:UseAppHost=false'
) -WorkingDirectory $repoRoot

$imageName = "niltor/iam:$Tag"
Invoke-Checked -FilePath $docker.Source -Arguments @('build', '--tag', $imageName, '.') -WorkingDirectory $repoRoot
Invoke-Checked -FilePath $docker.Source -Arguments @('push', $imageName) -WorkingDirectory $repoRoot

Write-Host ''
Write-Host '完成。' -ForegroundColor Green
Write-Host "镜像地址: docker.io/$imageName" -ForegroundColor Green
Write-Host '前端已构建并同步到 ApiService/wwwroot。' -ForegroundColor Green
Write-Host 'ApiService 镜像已准备好。' -ForegroundColor Green
