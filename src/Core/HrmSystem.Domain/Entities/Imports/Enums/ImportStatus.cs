namespace HrmSystem.Domain.Entities.Imports.Enums;

public enum ImportStatus
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    //? Uploaded, waiting for the background job to pick it up.
    Pending = 1,

    Processing = 2,
    Completed = 3,

    //? Fatal failure (unreadable file, job crash) — row-level errors do NOT fail the job.
    Failed = 4,
}
