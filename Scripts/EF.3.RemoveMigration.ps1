#! Note: Removes the last unapplied migration for the specified context.
#!       .\Scripts\EF.3.RemoveMigration.ps1 -Context App
#!       .\Scripts\EF.3.RemoveMigration.ps1 -Context Identity
#!       -Context is required — removing from both at once is intentionally not supported
#!       to avoid accidentally removing the wrong migration from the wrong context.

param(
    [Parameter(Mandatory)]
    [ValidateSet("App", "Identity")]
    [string]$Context
)

$project = "src/Infrastructure/HrmSystem.Infrastructure"
$startup = "src/Web/HrmSystem.Web"

$contextClass = if ($Context -eq "App") { "ApplicationDbContext" } else { "ApplicationIdentityDbContext" }

Write-Host "`n--- $contextClass ---" -ForegroundColor Yellow
dotnet ef migrations remove `
    --project $project `
    --startup-project $startup `
    --context $contextClass
