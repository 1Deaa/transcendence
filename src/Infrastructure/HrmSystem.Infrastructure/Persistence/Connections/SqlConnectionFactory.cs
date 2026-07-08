using System.Data;
using HrmSystem.Application.Common.Interfaces.Data;
using Microsoft.Data.SqlClient;

namespace HrmSystem.Infrastructure.Persistence.Connections;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        /*
            //! For PostgreSQL:
            //! - NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
            //? For connecting to a Microsoft SQL Server database
            //? - SqlConnection connection = new SqlConnection(_connectionString);
            //?     - But Requires: dotnet add package Microsoft.Data.SqlClient
         */
        var connection = new SqlConnection(_connectionString);

        connection.Open();

        return connection;
    }
}
