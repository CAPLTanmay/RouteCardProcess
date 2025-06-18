using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RouteCardProcess.Interfaces;

public class UserMessageService : IUserMessageService
{
    public Dictionary<int, string> Messages { get; private set; }

    public UserMessageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        using var connection = new SqlConnection(connectionString);
        var result = connection.Query<(int UserMessageCode, string UserMessageText)>("usp_GetAllUserMessages",commandType: CommandType.StoredProcedure);

        Messages = result.ToDictionary(x => x.UserMessageCode, x => x.UserMessageText);
    }
}

