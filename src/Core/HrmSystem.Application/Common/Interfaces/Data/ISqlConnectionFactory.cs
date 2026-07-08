using System.Data;

namespace HrmSystem.Application.Common.Interfaces.Data;

// For using Dapper, we need to have a way to create a connection to the database. This interface will be used to create a connection to the database and it will be implemented by the infrastructure layer.
public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
