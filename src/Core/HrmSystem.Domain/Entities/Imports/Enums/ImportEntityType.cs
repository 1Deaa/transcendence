namespace HrmSystem.Domain.Entities.Imports.Enums;

public enum ImportEntityType
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    //? Phase 1 supports employee imports; more entity types slot in here later.
    Employees = 1,
}
