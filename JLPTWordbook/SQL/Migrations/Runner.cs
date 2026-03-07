using SQLMigration;

namespace JLPTWordbook.SQL.Migrations;

public static class Runner
{
    public static async ValueTask RunAsync(IConfiguration conf, CancellationToken cancellationToken = default)
    {
        var connectionString = conf.GetConnectionString("JLPTWordbookDatabase");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        await Executor.RunAsync(
            connectionString,
            "JLPTWordbook", [
                new V1__Init(),
                new V2__AddMemorizedWords()
            ],
            Console.Out,
            cancellationToken
            );
    }
}
