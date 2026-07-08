#! Note: Applies pending migrations for both contexts against the database.
#!       .\Scripts\EF.2.UpdateDatabase.ps1
#!       .\Scripts\EF.2.UpdateDatabase.ps1 -Context Identity
#!       Default (-Context Both) updates BOTH contexts.

param(
    [ValidateSet("App", "Identity", "Both")]
    [string]$Context = "Both"
)

$project = "src/Infrastructure/HrmSystem.Infrastructure"
$startup = "src/Web/HrmSystem.Web"

#! Identity runs first — ApplicationDbContext creates Users with a FK to Identity.IdentityUsers.
#! If Application migrates first, SQL Server rejects the FK (referenced table doesn't exist yet).

if ($Context -eq "Identity" -or $Context -eq "Both") {
    Write-Host "`n--- ApplicationIdentityDbContext ---" -ForegroundColor Cyan
    dotnet ef database update `
        --project $project `
        --startup-project $startup `
        --context ApplicationIdentityDbContext
}

if ($Context -eq "App" -or $Context -eq "Both") {
    Write-Host "`n--- ApplicationDbContext ---" -ForegroundColor Cyan
    dotnet ef database update `
        --project $project `
        --startup-project $startup `
        --context ApplicationDbContext
}
