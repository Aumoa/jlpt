using System.Resources;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JLPTWordbook.Shared;

public class Wordbook(Word[] words)
{
    public readonly IReadOnlyList<Word> Words = Array.AsReadOnly(words);

    public static async Task<Wordbook> ParseAsync(HttpClient client, string url, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(url);
        var query = uri.Query;
        var fileName = Path.GetFileNameWithoutExtension(query);
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new FileNotFoundException(query);
        }

        var localizationResourceManager = new ResourceManager($"JLPTWordbook.Localizational.{fileName.ToUpper()}", typeof(Wordbook).Assembly);
        if (localizationResourceManager == null)
        {
            throw new FileNotFoundException(query);
        }

        var textContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonNode = JsonNode.Parse(textContent);
        if (jsonNode is not JsonArray jArray)
        {
            throw new FormatException();
        }

        List<WordComponent> wordComponentList = [];
        List<Word> wordList = [];
        int wordIndex = 0;
        foreach (var item in jArray)
        {
            if (item is not JsonArray word)
            {
                throw new FormatException();
            }

            foreach (var element in word)
            {
                if (element is not JsonObject component)
                {
                    throw new FormatException();
                }

                if (component["word"] is not JsonValue wordValue || wordValue.GetValueKind() != JsonValueKind.String)
                {
                    throw new FormatException();
                }

                if (component["reading"] is not JsonValue readingValue || readingValue.GetValueKind() != JsonValueKind.String)
                {
                    throw new FormatException();
                }

                wordComponentList.Add(new WordComponent(wordValue.GetValue<string>(), readingValue.GetValue<string>()));
            }

            string resourceName = $"_{wordIndex++}";
            wordList.Add(new Word([.. wordComponentList], () => localizationResourceManager.GetString(resourceName)));
            wordComponentList.Clear();
        }

        return new Wordbook([.. wordList]);
    }
}
