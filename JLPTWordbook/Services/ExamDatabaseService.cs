using Dapper;
using MySql.Data.MySqlClient;

namespace JLPTWordbook.Services;

public record ExamSessionRecord(long Id, string UserId, string Class, DateTime TakenAt, int TotalCount, int CorrectCount);

public record ExamSessionItemRecord(long Id, long SessionId, int WordIndex, string? UserAnswer, bool IsCorrect);

public class ExamDatabaseService(IConfiguration conf)
{
    public async ValueTask<long> SaveExamSessionAsync(
        string userId,
        string className,
        int totalCount,
        int correctCount,
        IEnumerable<(int WordIndex, string? UserAnswer, bool IsCorrect)> items,
        CancellationToken cancellationToken = default)
    {
        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string INSERT_SESSION = @"
INSERT INTO `exam_session` (`user_id`, `class`, `total_count`, `correct_count`)
VALUES (@userId, @className, @totalCount, @correctCount);
SELECT LAST_INSERT_ID();";

            var sessionCommand = new CommandDefinition(INSERT_SESSION, new { userId, className, totalCount, correctCount }, transaction, cancellationToken: cancellationToken);
            var sessionId = await connection.ExecuteScalarAsync<long>(sessionCommand);

            const string INSERT_ITEM = @"
INSERT INTO `exam_session_item` (`session_id`, `word_index`, `user_answer`, `is_correct`)
VALUES (@sessionId, @wordIndex, @userAnswer, @isCorrect)";

            foreach (var (wordIndex, userAnswer, isCorrect) in items)
            {
                var itemCommand = new CommandDefinition(INSERT_ITEM, new { sessionId, wordIndex, userAnswer, isCorrect }, transaction, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(itemCommand);
            }

            await transaction.CommitAsync(cancellationToken);
            return sessionId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async ValueTask<IReadOnlyList<ExamSessionRecord>> GetExamHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        const string QUERY = @"
SELECT `id`, `user_id` AS UserId, `class` AS Class, `taken_at` AS TakenAt, `total_count` AS TotalCount, `correct_count` AS CorrectCount
FROM `exam_session`
WHERE `user_id` = @userId
ORDER BY `taken_at` DESC";

        var command = new CommandDefinition(QUERY, new { userId }, cancellationToken: cancellationToken);
        var sessions = await connection.QueryAsync<ExamSessionRecord>(command);
        return sessions.ToList();
    }

    public async ValueTask<IReadOnlyList<ExamSessionItemRecord>> GetExamSessionItemsAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);

        const string QUERY = @"
SELECT `id`, `session_id` AS SessionId, `word_index` AS WordIndex, `user_answer` AS UserAnswer, `is_correct` AS IsCorrect
FROM `exam_session_item`
WHERE `session_id` = @sessionId";

        var command = new CommandDefinition(QUERY, new { sessionId }, cancellationToken: cancellationToken);
        var items = await connection.QueryAsync<ExamSessionItemRecord>(command);
        return items.ToList();
    }

    private MySqlConnection GetConnection()
    {
        return new MySqlConnection(conf.GetConnectionString("JLPTWordbookDatabase"));
    }
}
