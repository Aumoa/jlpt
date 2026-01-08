using System.Resources;
using System.Text.Json.Nodes;
using JLPTWordbook.Shared;

namespace JLPTWordbook.Services;

public class Worksheet
{
    public enum Class
    {
        N5,
        N4,
        N3,
        N2
    }

    private readonly Dictionary<Class, Wordbook> m_Wordbooks = [];

    public Wordbook this[Class @class] => m_Wordbooks[@class];

    public class BackgroundService(IWebHostEnvironment hostEnv, Worksheet worksheet, ILogger<BackgroundService> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            List<Task<(Class, Wordbook)>> tasks = [];
            foreach (var @class in Enum.GetValues<Class>())
            {
                tasks.Add(CreateWordbookAsync(logger, @class, cancellationToken));
            }

            var books = await Task.WhenAll(tasks);
            foreach (var (@class, book) in books)
            {
                worksheet.m_Wordbooks.Add(@class, book);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task<(Class, Wordbook)> CreateWordbookAsync(ILogger logger, Class @class, CancellationToken cancellationToken)
        {
            try
            {
                var className = @class.ToString();
                string jsonFileName = Path.Combine(hostEnv.WebRootPath, "words", $"{className.ToLower()}.json");
                var jsonNode = await JsonNode.ParseAsync(File.OpenRead(jsonFileName), cancellationToken: cancellationToken);
                if (jsonNode is not JsonArray jArray)
                {
                    logger.LogError("Failed to parse wordbook JSON for class {Class}", @class);
                    throw new InvalidDataException($"Invalid JSON format in {jsonFileName}");
                }

                var localizationResourceManager = new ResourceManager($"JLPTWordbook.Localizational.{className.ToUpper()}", typeof(Wordbook).Assembly);
                var wordbook = Wordbook.Parse(jArray, localizationResourceManager);

                return (@class, wordbook);
            }
            catch (FileNotFoundException e)
            {
                logger.LogWarning("Wordbook file not found for class {Class}: {Message}", @class, e.Message);
                return (@class, Wordbook.Empty());
            }
        }
    }
}
