# 生成迁移脚本
# 参数
param (
    [Parameter()]
    [string]
    $Name = $null
)

dotnet tool restore
$location = Get-Location
$migrationServicePath = Join-Path $PSScriptRoot "..\src\Services\MigrationService"

Set-Location $migrationServicePath
if ([string]::IsNullOrWhiteSpace($Name)) {
    $Name = [DateTime]::Now.ToString("yyyyMMdd-HHmmss")
}
dotnet build
if ($Name -eq "Remove") {
    dotnet ef migrations remove -c DefaultDbContext --no-build --project ../../Definition/EntityFramework
}
else {
    dotnet ef migrations add $Name -c DefaultDbContext --no-build --project ../../Definition/EntityFramework 
}

Set-Location $location
