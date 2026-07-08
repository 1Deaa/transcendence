namespace HrmSystem.Application.Common.Interfaces.Data;

public interface IUnitOfWork
{
    /*
        //> This will be taking any changes that have been made to the entities && they become   pending changes and will be
        //> saving them to the database.
        //> <returns>The number of entities saved</returns>
        //! This Method [SaveChangesAsync(...)] is already implemented by EF [DbContext] Class!
     */
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
