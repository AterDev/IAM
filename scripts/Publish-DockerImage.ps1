[CmdletBinding()]
param(
    [Parameter()]
    [string]$Tag = 'latest',
    [Parameter()]
    [bool]$proxy = $false
)


if ($proxy) {
    $env:HTTP_PROXY = "http://127.0.0.1:7890"
    $env:HTTPS_PROXY = "http://127.0.0.1:7890"
}


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

function Get-CommandExecutable {
    param(
        [Parameter(Mandatory)]
        $CommandInfo
    )

    foreach ($value in @($CommandInfo.Path, $CommandInfo.Definition, $CommandInfo.Source, $CommandInfo.Name)) {
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }

    throw "无法解析命令可执行文件: $($CommandInfo.Name)"
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
$pnpm = Get-Command pnpm -ErrorAction SilentlyContinue
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue

if (-not $pnpm) { throw 'pnpm not found. Please install pnpm or enable corepack.' }
if (-not $dotnet) { throw 'dotnet CLI not found. Please install the .NET SDK.' }

Write-Host 'Building frontend assets...' -ForegroundColor Cyan
Invoke-Checked -FilePath (Get-CommandExecutable $pnpm) -Arguments @('build') -WorkingDirectory $frontendRoot
if (-not (Test-Path (Join-Path $frontendOutput 'index.html'))) {
    throw "Frontend build did not produce index.html: $frontendOutput"
}
Write-Host 'Syncing frontend files to ApiService/wwwroot ...' -ForegroundColor Cyan
Sync-DirectoryContents -SourceDirectory $frontendOutput -DestinationDirectory $apiWwwroot

Write-Host 'Publishing and pushing ApiService container image...' -ForegroundColor Cyan
Invoke-Checked -FilePath (Get-CommandExecutable $dotnet) -Arguments @(
    'publish',
    (Join-Path $repoRoot 'src\Services\ApiService\ApiService.csproj'),
    '-c', 'Release',
    '--os', 'linux',
    '--arch', 'x64',
    '/t:PublishContainer',
    '/p:UseAppHost=false',
    '/p:InvariantGlobalization=false',
    '/p:ContainerFamily=alpine-extra',
    '/p:ContainerEnvironmentVariable=DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false',
    '/p:ContainerRegistry=docker.io',
    '/p:ContainerRepository=niltor/iam',
    "/p:ContainerImageTag=$Tag",
    '/p:ContainerPort=8080'
) -WorkingDirectory $repoRoot

Write-Host ''
Write-Host 'Done.' -ForegroundColor Green
Write-Host "Image: docker.io/niltor/iam:$Tag" -ForegroundColor Green
