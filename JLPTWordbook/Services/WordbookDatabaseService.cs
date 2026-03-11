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

    public async ValueTask<IReadOnlySet<int>> GetMemorizedWordIndicesAsync(string userId, string className, CancellationToken cancellationToken = default)
    {
        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        const string QUERY = "SELECT `word_index` FROM `memorized_word` WHERE `user_id` = @userId AND `class` = @className";
        var command = new CommandDefinition(QUERY, new { userId, className }, cancellationToken: cancellationToken);
        var indices = await connection.QueryAsync<int>(command);

        return new HashSet<int>(indices);
    }

    public async ValueTask MarkWordAsMemorizedAsync(string userId, string className, int wordIndex, CancellationToken cancellationToken = default)
    {
        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        const string QUERY = "INSERT IGNORE INTO `memorized_word` (`user_id`, `class`, `word_index`) VALUES(@userId, @className, @wordIndex)";
        var command = new CommandDefinition(QUERY, new { userId, className, wordIndex }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async ValueTask UnmarkWordAsMemorizedAsync(string userId, string className, int wordIndex, CancellationToken cancellationToken = default)
    {
        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        const string QUERY = "DELETE FROM `memorized_word` WHERE `user_id` = @userId AND `class` = @className AND `word_index` = @wordIndex";
        var command = new CommandDefinition(QUERY, new { userId, className, wordIndex }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async ValueTask ResetMemorizedWordsAsync(string userId, string className, CancellationToken cancellationToken = default)
    {
        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        const string QUERY = "DELETE FROM `memorized_word` WHERE `user_id` = @userId AND `class` = @className";
        var command = new CommandDefinition(QUERY, new { userId, className }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    private MySqlConnection GetConnection()
    {
        return new MySqlConnection(conf.GetConnectionString("JLPTWordbookDatabase"));
    }
}
