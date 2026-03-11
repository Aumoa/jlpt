using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace VocabTableConverter;

internal static class JsonHelper
{

    public static readonly JsonSerializerOptions GeneralOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };
}
