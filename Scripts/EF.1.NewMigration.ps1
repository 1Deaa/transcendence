#! Note: Pass the migration name and optionally the context when calling this script.
#!       .\Scripts\EF.1.NewMigration.ps1 -MigrationName MyNewMigrationName
#!       .\Scripts\EF.1.NewMigration.ps1 -MigrationName MyNewMigrationName -Context Identity
#!       .\Scripts\EF.1.NewMigration.ps1 -MigrationName MyNewMigrationName -Context App
#!       Default (-Context Both) runs the migration for BOTH contexts.
#!
#!  Output directories (relative to the Infrastructure project root):
#!       App      → Persistence/Contexts/Application/
#!       Identity → Persistence/Contexts/Identity/

param(
    [Parameter(Mandatory)]
    [string]$MigrationName,

    [ValidateSet("App", "Identity", "Both")]
    [string]$Context = "Both"
)

$project = "src/Infrastructure/HrmSystem.Infrastructure"
$startup = "src/Web/HrmSystem.Web"

if ($Context -eq "App" -or $Context -eq "Both") {
    Write-Host "`n--- ApplicationDbContext ---" -ForegroundColor Cyan
    dotnet ef migrations add $MigrationName `
        --project $project `
        --startup-project $startup `
        --context ApplicationDbContext `
        --output-dir Persistence/Contexts/Migrations/Application
}

if ($Context -eq "Identity" -or $Context -eq "Both") {
    Write-Host "`n--- ApplicationIdentityDbContext ---" -ForegroundColor Cyan
    dotnet ef migrations add $MigrationName `
        --project $project `
        --startup-project $startup `
        --context ApplicationIdentityDbContext `
        --output-dir Persistence/Contexts/Migrations/Identity
}



#! This is working; but using Powershell Syntax; which I don't like; I Prefer using EF Core and .NET CLI directly in the terminal; but I will keep this for reference and for those who prefer using Powershell.
#Push-Location src\DevHabits.Api
#dotnet ef migrations add AddingIndexOnIsActiveProperty `
#	--output-dir Database\Migrations
#Pop-Location
