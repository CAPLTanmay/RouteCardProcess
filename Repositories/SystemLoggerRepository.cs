using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Repositories;
using Dapper;
using System.Data;

public class SystemLoggerRepository : ISystemLoggerRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SystemLoggerRepository(SqlConnectionFactory connectionFactory )
    {
        _connectionFactory = connectionFactory;
    }

    public async Task LogAsync(string moduleName, string functionName, string errorMessage)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@ModuleName", moduleName);
        parameters.Add("@FunctionName", functionName);
        parameters.Add("@ErrorMsg", errorMessage);

        await connection.ExecuteAsync("usp_InsertLog", parameters, commandType: CommandType.StoredProcedure);
    }

}
