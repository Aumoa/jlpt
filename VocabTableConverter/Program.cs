using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using VocabTableConverter;

const string kSource = "jlpt_vocab.csv";

CancellationTokenSource cts = new();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    Dictionary<int, Unresolved> unresolved;

    if (File.Exists("unresolved.json"))
    {
        var json = await File.ReadAllTextAsync("unresolved.json");
        var unresolvedArray = JsonSerializer.Deserialize<Unresolved[]>(json, JsonHelper.GeneralOptions);
        unresolved = unresolvedArray?.ToDictionary(u => u.Id, u => u) ?? [];
    }
    else
    {
        unresolved = [];
    }

    var books = await Wordbook.LoadBooksAsync(cts.Token);
    var bookDict = books.ToDictionary(b => b.Level, b => b.ConfigureBook());
    var content = await File.ReadAllTextAsync(kSource, cts.Token);
    var vocabDict = AnsiConsole.Status()
        .Start("Parsing CSV...", ctx =>
        {
            var r = Parser.ParseCSV(content);
            List<Translator.InputElement> inputs = new(r.Length);
            for (int i = 0; i < r.Length; ++i)
            {
                var item = r[i];
                int id = i + 1;
                if (!bookDict[item.JLPTLevel].Contains(id) && !unresolved.ContainsKey(id))
                {
                    inputs.Add(new Translator.InputElement
                    {
                        Id = id,
                        Level = item.JLPTLevel,
                        Original = item.Original,
                        Furigana = item.Furigana,
                        English = item.English
                    });
                }
            }

            inputs.Reverse();
            return inputs.ToDictionary(i => i.Id, i => i);
        });

    const int kChunk = 30;
    const int kRegenerateLimits = 1;

    int completed = 0, max = vocabDict.Count;
    while (vocabDict.Count > 0)
    {
        var timer = Stopwatch.StartNew();

        Translator.ClearHistory();
        List<Translator.InputElement> currentInputs = [];
        HashSet<Wordbook.Book> changedBooks = [];
        var currentVocabs = vocabDict.Values.Take(kChunk).ToArray();
        currentInputs.AddRange(currentVocabs);
        string prompt = "{0}";
        List<Translator.OutputElement> errors = [];

        for (int j = 0; j < kRegenerateLimits; ++j)
        {
            var translatedItems = await AnsiConsole.Status()
                .StartAsync($"Generating responses for {completed}/{max} (trying #{j + 1})", async ctx =>
                {
                    return await Translator.TranslateAsync(currentInputs, prompt, cts.Token);
                });
            errors.Clear();
            Translator.Validate(translatedItems, vocabDict, errors);
            currentInputs.Clear();
            string validationErrors = "";

            foreach (var error in errors)
            {
                var original = vocabDict[error.Id];
                var errorMessage = $"Validation error for {original.Original}|{original.Furigana}: {string.Join(", ", error.Mapping)}";
                Console.WriteLine(errorMessage);
                validationErrors += errorMessage + '\n';
                currentInputs.Add(original);
            }

            var errorsIdSet = errors.Select(e => e.Id).ToHashSet();
            var validItems = translatedItems.Where(t => !errorsIdSet.Contains(t.Id));
            foreach (var validItem in validItems)
            {
                var vocab = vocabDict[validItem.Id];
                List<Wordbook.Element> elements = [];
                foreach (var mapping in validItem.Mapping)
                {
                    var ss = mapping.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    elements.Add(new Wordbook.Element
                    {
                        Word = ss.Length > 0 ? ss[0] : "",
                        Reading = ss.Length > 1 ? ss[1] : ""
                    });
                }

                var book = bookDict[vocab.Level];
                book.Add(new Wordbook.Word
                {
                    Id = validItem.Id,
                    Elements = [.. elements],
                    English = vocab.English,
                    Korean = validItem.Korean
                });
                changedBooks.Add(book);
            }

            if (errors.Count == 0)
            {
                break;
            }

            if (j == kRegenerateLimits - 1)
            {
                AnsiConsole.MarkupLine("[red]Maximum regeneration attempts reached.[/]");
            }
            else
            {
                prompt = $"이런 오류가 발생했어.\n{validationErrors}\n아래 단어들을 기존 규칙을 잘 준수해서 다시 번역해서 JSON으로 뽑아줘.\n각 단어별로 이전 응답과 동일한 응답을 생성하지 마.\n\n{{0}}";
            }
        }

        foreach (var vocabToRemove in currentVocabs)
        {
            vocabDict.Remove(vocabToRemove.Id);
        }

        foreach (var error in errors)
        {
            unresolved.Add(error.Id, new Unresolved { Id = error.Id, Mapping = error.Mapping });
        }

        AnsiConsole.WriteLine("Saving changes...");
        await Wordbook.SaveBooksAsync(changedBooks, cts.Token);
        await File.WriteAllTextAsync("unresolved.json", JsonSerializer.Serialize(unresolved.Values.ToArray(), JsonHelper.GeneralOptions), cts.Token);

        completed += kChunk;

        timer.Stop();
        var time = timer.Elapsed.TotalSeconds;
        var perWord = time / currentVocabs.Length;
        AnsiConsole.MarkupLine("Complete to process {0} items in {1} seconds. ({2} seconds per word.)", currentVocabs.Length, time, perWord);
        AnsiConsole.MarkupLine("Estimated time remaining is {0}.", TimeSpan.FromSeconds(perWord * vocabDict.Count));
    }

    return 0;
}
catch (FormatException e)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

internal record Unresolved
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("mapping")]
    public required string[] Mapping{ get; set; }
}