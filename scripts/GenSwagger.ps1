[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ServiceName,

    [Parameter()]
    [string]$DocumentName = "v1"
)

$location = Get-Location
$configuration = "Debug"

function Get-TargetFramework {
    param([Parameter(Mandatory = $true)][string]$CsprojPath)

    [xml]$csproj = Get-Content -Raw -Path $CsprojPath
    $groups = @($csproj.Project.PropertyGroup)

    foreach ($group in $groups) {
        if ($group.TargetFramework) {
            return $group.TargetFramework.Trim()
        }
    }

    foreach ($group in $groups) {
        if ($group.TargetFrameworks) {
            return $group.TargetFrameworks.Split(';')[0].Trim()
        }
    }
    throw "无法从项目文件读取 TargetFramework/TargetFrameworks: $CsprojPath"
}

function Get-ServiceDisplayName {
    param([Parameter(Mandatory = $true)][string]$Name)

    if ($Name -match 'Service$') {
        return $Name.Substring(0, $Name.Length - 'Service'.Length)
    }

    return $Name
}

function Update-SwaggerTitle {
    param(
        [Parameter(Mandatory = $true)][string]$SwaggerPath,
        [Parameter(Mandatory = $true)][string]$Title
    )

    $swaggerDocument = Get-Content -Raw -Path $SwaggerPath | ConvertFrom-Json
    if (-not $swaggerDocument.info) {
        throw "Swagger 文档缺少 info 节点: $SwaggerPath"
    }

    $swaggerDocument.info.title = $Title
    $swaggerDocument | ConvertTo-Json -Depth 100 | Set-Content -Path $SwaggerPath -Encoding UTF8
}

try {
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $serviceDisplayName = Get-ServiceDisplayName -Name $ServiceName

    $projectDir = Join-Path $repoRoot "src/Services/$ServiceName"
    $csprojPath = Join-Path $projectDir "$ServiceName.csproj"
    if (-not (Test-Path $csprojPath -PathType Leaf)) {
        throw "未找到项目文件: $csprojPath"
    }

    $targetFramework = Get-TargetFramework -CsprojPath $csprojPath
    $assemblyPath = Join-Path $projectDir "bin/$configuration/$targetFramework/$ServiceName.dll"
    $swaggerOutputPath = Join-Path $projectDir "swagger.json"
    $clientOutputPath = Join-Path $repoRoot "src/ClientApp/WebApp/src/app"

    Set-Location $repoRoot

    dotnet tool restore
    if (-not (Test-Path $assemblyPath -PathType Leaf)) {
        throw "未找到程序集: $assemblyPath"
    }
    if (-not (Test-Path $clientOutputPath -PathType Container)) {
        throw "未找到前端输出目录: $clientOutputPath"
    }

    Set-Location $projectDir
    dotnet tool run swagger -- tofile --output $swaggerOutputPath $assemblyPath $DocumentName

    Update-SwaggerTitle -SwaggerPath $swaggerOutputPath -Title $serviceDisplayName

    Set-Location $repoRoot
    perigon generate request $swaggerOutputPath $clientOutputPath -t angular
}
catch {
    Write-Error $_
    exit 1
}
finally {
    Set-Location $location
}