using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace VocabTableConverter;

internal static class Wordbook
{
    public record Element
    {
        [JsonPropertyName("word")]
        public required string Word { get; set; }

        [JsonPropertyName("reading")]
        public required string Reading { get; set; }
    }

    public record Word
    {
        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonPropertyName("elements")]
        public required Element[] Elements { get; set; }

        [JsonPropertyName("en")]
        public required string English { get; set; }

        [JsonPropertyName("ko")]
        public required string Korean { get; set; }
    }

    public record Book
    {
        [JsonPropertyName("level")]
        public required string Level { get; set; }

        [JsonPropertyName("words")]
        public required List<Word> Words { get; set; }

        public HashSet<int> ExistingWords = [];

        public Book ConfigureBook()
        {
            ExistingWords = Words.Select(w => w.Id).ToHashSet();
            return this;
        }

        public bool Contains(int id)
        {
            return ExistingWords.Contains(id);
        }

        public void Add(Word word)
        {
            if (!Contains(word.Id))
            {
                Words.Add(word);
                ExistingWords.Add(word.Id);
            }
        }
    }

    public static Task<Book[]> LoadBooksAsync(CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(
            LoadBookAsync("n5.json", cancellationToken),
            LoadBookAsync("n4.json", cancellationToken),
            LoadBookAsync("n3.json", cancellationToken),
            LoadBookAsync("n2.json", cancellationToken),
            LoadBookAsync("n1.json", cancellationToken)
        );
    }

    public static Task SaveBooksAsync(IEnumerable<Book> books, CancellationToken cancellationToken = default)
    {
        List<Task> tasks = [];
        foreach (var book in books)
        {
            tasks.Add(File.WriteAllTextAsync(book.Level.ToLower() + ".json", JsonSerializer.Serialize(book, JsonHelper.GeneralOptions), cancellationToken));
        }
        return Task.WhenAll(tasks);
    }

    public static async Task<Book> LoadBookAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(fileName))
            {
                var json = await File.ReadAllTextAsync(fileName, cancellationToken);
                var book = JsonSerializer.Deserialize<Book>(json, JsonHelper.GeneralOptions);
                return book ?? throw new InvalidOperationException("Failed to deserialize book.");
            }
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[red]Error[/] loading book from file: {0}", e.Message);
        }

        return new Book
        {
            Level = Path.GetFileNameWithoutExtension(fileName).ToUpper(),
            Words = []
        };
    }
}
