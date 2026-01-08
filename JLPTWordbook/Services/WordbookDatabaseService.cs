using Dapper;
using MySql.Data.MySqlClient;

namespace JLPTWordbook.Services;

public class WordbookDatabaseService(IConfiguration conf)
{
    public async ValueTask LoginAsync(string id, string? nameHint, CancellationToken cancellationToken = default)
    {
        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        const string QUERY1 = @"
INSERT INTO `user` (`id`, `name_hint`) VALUES(@id, @nameHint)
  ON DUPLICATE KEY UPDATE `name_hint` = @nameHint, `last_logged_at` = NOW()
";

        var command = new CommandDefinition(QUERY1, new { id, nameHint }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    private MySqlConnection GetConnection()
    {
        return new MySqlConnection(conf.GetConnectionString("JLPTWordbookDatabase"));
    }
}
