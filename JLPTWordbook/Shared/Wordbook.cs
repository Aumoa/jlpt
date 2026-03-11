using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JLPTWordbook.Shared;

public class Wordbook(Word[] words)
{
    public readonly IReadOnlyList<Word> Words = Array.AsReadOnly(words);

    public static Wordbook Parse(JsonArray jArray)
    {
        List<WordComponent> wordComponentList = [];
        List<Word> wordList = [];
        foreach (var item in jArray)
        {
            if (item is not JsonObject wordObj)
            {
                throw new FormatException();
            }

            if (wordObj["elements"] is not JsonArray elementsArray)
            {
                throw new FormatException();
            }

            foreach (var element in elementsArray)
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

            string en = wordObj["en"]?.GetValue<string>() ?? string.Empty;
            string ko = wordObj["ko"]?.GetValue<string>() ?? string.Empty;
            wordList.Add(new Word([.. wordComponentList], () =>
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ko" ? ko : en));
            wordComponentList.Clear();
        }

        return new Wordbook([.. wordList]);
    }

    public static Wordbook Empty()
    {
        return new Wordbook([]);
    }
}
