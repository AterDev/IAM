[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9]*$')]
    [string]$ServiceName,

    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$DocumentName = 'v1',

    [uri]$BaseUri = 'https://localhost:9900'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectDirectory = Join-Path $repoRoot "src/Services/$ServiceName"
if (-not (Test-Path -LiteralPath (Join-Path $projectDirectory "$ServiceName.csproj") -PathType Leaf)) {
    throw "找不到服务项目 $ServiceName"
}

$uri = [uri]::new($BaseUri, "/openapi/$DocumentName.json")
$document = Invoke-RestMethod -Uri $uri -TimeoutSec 30
if (-not $document.openapi -or -not $document.info -or -not $document.paths) {
    throw "$uri 未返回有效的 OpenAPI 文档"
}

$document.info.title = $ServiceName -replace 'Service$', ''
$document | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $projectDirectory 'swagger.json') -Encoding utf8
