[CmdletBinding()]
param(
    [Parameter()]
    [string]$DockerHubNamespace,

    [Parameter()]
    [string]$Tag = 'latest',

    [Parameter()]
    [string]$ImageName = 'Ater.IAM',

    [Parameter()]
    [string]$Registry = 'docker.io',

    [Parameter()]
    [string[]]$Platforms = @('linux/amd64'),

    [Parameter()]
    [switch]$SkipPush,

    [Parameter()]
    [switch]$SkipFrontendBuild,

    [Parameter()]
    [switch]$Login,

    [Parameter()]
    [switch]$NoCache,

    [Parameter()]
    [switch]$PullBaseImages
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:ContainerCliPath = $null

function Resolve-ContainerCliPath {
    if ($script:ContainerCliPath) {
        return $script:ContainerCliPath
    }

    $candidates = @(
        (Get-Command docker -ErrorAction SilentlyContinue)?.Source,
        (Get-Command docker -ErrorAction SilentlyContinue)?.ResolvedCommandName,
        (Get-Command docker.exe -ErrorAction SilentlyContinue)?.Source,
        (Get-Command podman -ErrorAction SilentlyContinue)?.Source,
        (Get-Command podman.exe -ErrorAction SilentlyContinue)?.Source,
        (Join-Path ${env:ProgramFiles} 'Docker\Docker\resources\bin\docker.exe'),
        (Join-Path ${env:ProgramW6432} 'Docker\Docker\resources\bin\docker.exe'),
        (Join-Path ${env:LOCALAPPDATA} 'Programs\Podman\podman.exe')
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-Path $candidate) {
            $script:ContainerCliPath = $candidate
            return $script:ContainerCliPath
        }
    }

    return $null
}

function Invoke-Docker {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    Write-Host ('> docker ' + ($Arguments -join ' ')) -ForegroundColor DarkGray
    $containerCli = Resolve-ContainerCliPath
    if (-not $containerCli) {
        throw 'Docker / Podman CLI 未安装或不可用。'
    }

    & $containerCli @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker command failed with exit code $LASTEXITCODE"
    }
}

function Resolve-CommandPath {
    param(
        [Parameter(Mandatory)]
        [string[]]$CommandNames,

        [Parameter()]
        [string[]]$CandidatePaths = @()
    )

    $resolvedCandidates = foreach ($commandName in $CommandNames) {
        (Get-Command $commandName -ErrorAction SilentlyContinue)?.Source
    }

    $resolvedCandidates += $CandidatePaths

    foreach ($candidate in ($resolvedCandidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

function Invoke-Process {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter()]
        [string[]]$Arguments = @(),

        [Parameter()]
        [string]$WorkingDirectory = (Get-Location).Path
    )

    Write-Host ('> ' + $FilePath + ' ' + ($Arguments -join ' ')) -ForegroundColor DarkGray
    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "command failed with exit code $LASTEXITCODE"
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

    if (-not (Test-Path $SourceDirectory)) {
        throw "源目录不存在: $SourceDirectory"
    }

    if (-not (Test-Path $DestinationDirectory)) {
        New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    }

    Get-ChildItem -Path $DestinationDirectory -Force | Remove-Item -Recurse -Force

    Get-ChildItem -Path $SourceDirectory -Force | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination $DestinationDirectory -Recurse -Force
    }
}

function Build-FrontendAssets {
    param(
        [Parameter(Mandatory)]
        [string]$FrontendRoot,

        [Parameter(Mandatory)]
        [string]$FrontendOutput,

        [Parameter(Mandatory)]
        [string]$ApiWwwroot
    )

    $pnpmCli = Resolve-CommandPath -CommandNames @('pnpm.cmd', 'pnpm') -CandidatePaths @(
        (Join-Path ${env:LOCALAPPDATA} 'pnpm\pnpm.cmd')
    )

    if (-not $pnpmCli) {
        throw '未找到 pnpm，请先安装 pnpm 或启用 corepack。'
    }

    Write-Host '开始构建前端静态资源...' -ForegroundColor Cyan
    Invoke-Process -FilePath $pnpmCli -Arguments @('build') -WorkingDirectory $FrontendRoot

    if (-not (Test-Path (Join-Path $FrontendOutput 'index.html'))) {
        throw "前端构建未生成 index.html: $FrontendOutput"
    }

    Write-Host '同步前端资源到 ApiService/wwwroot ...' -ForegroundColor Cyan
    Sync-DirectoryContents -SourceDirectory $FrontendOutput -DestinationDirectory $ApiWwwroot
}

function Publish-DotnetArtifacts {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,

        [Parameter(Mandatory)]
        [string]$ArtifactsRoot
    )

    $dotnetCli = Resolve-CommandPath -CommandNames @('dotnet.exe', 'dotnet') -CandidatePaths @(
        (Join-Path ${env:ProgramFiles} 'dotnet\dotnet.exe')
    )

    if (-not $dotnetCli) {
        throw '未找到 dotnet CLI，请先安装 .NET SDK。'
    }

    $apiOutput = Join-Path $ArtifactsRoot 'api'
    $migrationOutput = Join-Path $ArtifactsRoot 'migration'

    New-Item -ItemType Directory -Path $ArtifactsRoot -Force | Out-Null

    if (Test-Path $apiOutput) {
        Remove-Item -Path $apiOutput -Recurse -Force
    }

    if (Test-Path $migrationOutput) {
        Remove-Item -Path $migrationOutput -Recurse -Force
    }

    Write-Host '开始发布 ApiService ...' -ForegroundColor Cyan
    Invoke-Process -FilePath $dotnetCli -Arguments @(
        'publish',
        'src/Services/ApiService/ApiService.csproj',
        '-c', 'Release',
        '-o', $apiOutput,
        '/p:UseAppHost=false'
    ) -WorkingDirectory $RepoRoot

    Write-Host '开始发布 MigrationService ...' -ForegroundColor Cyan
    Invoke-Process -FilePath $dotnetCli -Arguments @(
        'publish',
        'src/Services/MigrationService/MigrationService.csproj',
        '-c', 'Release',
        '-o', $migrationOutput,
        '/p:UseAppHost=false'
    ) -WorkingDirectory $RepoRoot
}

function Test-DockerAvailable {
    $containerCli = Resolve-ContainerCliPath
    if (-not $containerCli) {
        throw 'Docker / Podman CLI 未安装或不可用。'
    }

    & $containerCli version --format '{{.Server.Version}}' *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker daemon 当前不可用，请先启动 Docker Desktop / dockerd。'
    }
}

function Get-NormalizedRepositoryName {
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryName
    )

    $normalized = $RepositoryName.ToLowerInvariant()
    if ($normalized -notmatch '^[a-z0-9]+([._-][a-z0-9]+)*$') {
        throw "镜像名称 '$RepositoryName' 转换后为 '$normalized'，仍不符合 Docker 仓库命名规则。"
    }

    if ($normalized -cne $RepositoryName) {
        Write-Warning "Docker Hub 仓库名必须为小写，已将 '$RepositoryName' 规范化为 '$normalized'。"
    }

    return $normalized
}

Test-DockerAvailable

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dockerfilePath = Join-Path $repoRoot 'Dockerfile'
$frontendRoot = Join-Path $repoRoot 'src\ClientApp\WebApp'
$frontendOutput = Join-Path $frontendRoot 'dist\browser'
$apiWwwroot = Join-Path $repoRoot 'src\Services\ApiService\wwwroot'
$dockerArtifactsRoot = Join-Path $repoRoot '.artifacts\docker'
if (-not (Test-Path $dockerfilePath)) {
    throw "未找到 Dockerfile: $dockerfilePath"
}

if (-not $SkipPush -and [string]::IsNullOrWhiteSpace($DockerHubNamespace)) {
    throw '发布到 Docker Hub 时必须提供 -DockerHubNamespace。'
}

$repositoryName = Get-NormalizedRepositoryName -RepositoryName $ImageName
$platformValue = ($Platforms | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique) -join ','
if ([string]::IsNullOrWhiteSpace($platformValue)) {
    throw '至少需要一个有效的 -Platforms 值。'
}

$fullImageName = if ([string]::IsNullOrWhiteSpace($DockerHubNamespace)) {
    "${repositoryName}:$Tag"
}
else {
    "${DockerHubNamespace}/${repositoryName}:$Tag"
}

Write-Host "Repository root : $repoRoot" -ForegroundColor Cyan
Write-Host "Dockerfile      : $dockerfilePath" -ForegroundColor Cyan
Write-Host "Image           : $fullImageName" -ForegroundColor Cyan
Write-Host "Platforms       : $platformValue" -ForegroundColor Cyan

if (-not $SkipFrontendBuild) {
    Build-FrontendAssets -FrontendRoot $frontendRoot -FrontendOutput $frontendOutput -ApiWwwroot $apiWwwroot
}
else {
    Write-Host '已跳过前端构建，直接使用现有的 wwwroot 内容。' -ForegroundColor Yellow
}

Publish-DotnetArtifacts -RepoRoot $repoRoot -ArtifactsRoot $dockerArtifactsRoot

if ($Login) {
    Invoke-Docker -Arguments @('login', $Registry)
}

$buildArguments = @(
    'build',
    '--platform', $platformValue,
    '--file', $dockerfilePath,
    '--tag', $fullImageName
)

if ($PullBaseImages) {
    $buildArguments += '--pull'
}

if ($NoCache) {
    $buildArguments += '--no-cache'
}

$buildArguments += $repoRoot
Invoke-Docker -Arguments $buildArguments

if (-not $SkipPush) {
    Invoke-Docker -Arguments @('push', $fullImageName)
}
else {
    Write-Host '已跳过 push，只完成本地镜像构建。' -ForegroundColor Yellow
}

Write-Host ''
Write-Host '完成。' -ForegroundColor Green
Write-Host "镜像地址: $fullImageName" -ForegroundColor Green
Write-Host '前端资源已在本地构建并同步到 ApiService/wwwroot。' -ForegroundColor Green
Write-Host '后端发布产物已生成到 .artifacts/docker，并由运行时镜像直接打包。' -ForegroundColor Green
Write-Host '容器默认会先执行 MigrationService，再启动 ApiService。' -ForegroundColor Green
