namespace HrmSystem.Infrastructure.Persistence.Seeding;

/*
    //! - [DatabaseInitialiser] discovers all seeders via IEnumerable<ISeeder> from DI.
    //! - Adding a new seeder = register one line in DI. Nothing else changes.
    //! - [[int Order]] enforces the FK dependency graph without any manual coordination.
*/
public interface ISeeder
{
    //! Higher number = runs later.
    //> Use multiples of 10 so you can insert new seeders in-between without renumbering (10, 20, 30...).
    int Order { get; }

    /*
        //?     Default false — test data (users, habits) must never reach Production.
        //?     Override to [true] for essential configuration seeders (roles, permissions)
        //?     that must run in every environment including Production.
    */
    bool RunInProduction => false;

    Task SeedAsync(CancellationToken cancellationToken = default);
}
