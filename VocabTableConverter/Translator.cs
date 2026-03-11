using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace VocabTableConverter;

internal static class Translator
{
    private static readonly ChatMessage s_Persona = new()
    {
        Role = "system",
        Content = @"
### Role
당신은 일본어 단어 데이터를 정밀 분석하여 구조화된 JSON 형식으로 변환하는 'Linguistic Data Transformer'입니다.

### Task
입력된 JSON 배열 내 각 단어의 [kanji], [kana], [en] 정보를 분석하여, 한국어 의미([kr])를 도출하고 한자와 독음을 1:1로 매칭([mapping])하십시오.

### Rules (Must Follow)
1. 결과는 반드시 유효한 JSON 배열 형식으로만 응답하십시오. (서론, 결론, 설명 금지)
2. [kr]: [en] 정의를 참고하여 가장 적합하고 표준적인 한국어 단어를 선정하십시오.
3. [mapping] 규칙:
   - 각 한자와 대응 독음을 ""한자|독음"" 문자열로 만들어 배열에 담으십시오.
   - 한자 1글자당 대응되는 독음 덩어리를 매핑하십시오. (예: 像|ぞう)
   - 읽기 단위를 임의로 글자 단위로 쪼개지 마십시오. (예: 像|ぞ, 像|う(X) -> 像|ぞう(O))
   - 오쿠리가나나 복합어 등 대응이 모호한 경우, 전체 단위를 하나의 배열 원소로 처리하십시오. (예: 恋|こい, する)
   - 원문이 한자가 아닌 경우 강제로 한자로 바꾸지 말고 그대로 사용하십시오.
4. 모든 응답에는 입력받은 [id] 값을 그대로 유지하십시오.
5. 오쿠리가나 처리 특수 규칙:
   - 일본어 단어 끝에 히라가나가 붙어 있는 경우(예: 分かる의 る), 이를 매핑 배열의 마지막 요소에 반드시 포함하십시오.
   - 단어 전체 글자 수가 mapping 배열의 원소 개수와 일치하는지 반드시 검증하십시오.
   - 예: ""分かる|わかる"" -> [""分|わか"", ""る""] 또는 [""分|わ"", ""か|る""]가 아닌, [""分|わか"", ""る""] 형태로 전체 단어를 온전히 담아야 합니다. 
   - 매핑된 결과를 합쳤을 때 원본 단어의 [kana]가 한 글자도 빠짐없이 복원되어야 합니다.

### Critical Constraints
1. 입력된 [kanji]와 [kana] 문자열을 절대 변경하지 마십시오.
2. 매핑 배열의 요소들을 순서대로 결합했을 때, [kanji] 및 [kana]와 100% 일치해야 합니다.
3. 만약 논리적 매핑이 불가능하다고 판단되면, 무리하게 추론하지 말고 한자 전체를 하나의 매핑 단위로 묶으십시오.
4. ""現|ひ""와 같이 원본 [kana] 발음과 어긋나는 추론은 절대 금지합니다. 원본 데이터를 최우선 순위로 신뢰하십시오.
5. 결과가 원본과 일치하지 않는다면 스스로 재검토하여 올바른 값을 출력하십시오.

### Output Schema
[
  {
    ""id"": 1,
    ""kr"": ""원칙"",
    ""mapping"": [""原|げん"", ""則|そく""]
  }
]
"
    };

    private record ChatMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; set; }

        [JsonPropertyName("content")]
        public required string Content { get; set; }
    }

    private record ChatResponse
    {
        [JsonPropertyName("message")]
        public required ChatMessage Message { get; set; }
    }

    public record InputElement
    {
        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonIgnore]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("kanji")]
        public required string Original { get; set; }

        [JsonPropertyName("kana")]
        public required string Furigana { get; set; }

        [JsonPropertyName("en")]
        public required string English { get; set; }
    }

    public record OutputElement
    {
        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonPropertyName("kr")]
        public required string Korean { get; set; }

        [JsonPropertyName("mapping")]
        public required string[] Mapping { get; set; }
    }

    private static readonly List<ChatMessage> s_Messages = [];

    public static void ClearHistory()
    {
        s_Messages.Clear();
    }

    public static async Task<OutputElement[]> TranslateAsync(IEnumerable<InputElement> inputElements, string prompt, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages = [s_Persona, .. s_Messages.TakeLast(12)];
        messages.Add(new ChatMessage
        {
            Role = "user",
            Content = string.Format(prompt, JsonSerializer.Serialize(inputElements, JsonHelper.GeneralOptions))
        });

        using var http = new HttpClient();
        var json = JsonSerializer.Serialize(new
        {
            model = "gemma2:27b",
            messages,
            stream = true,
            options = new
            {
                temperature = 0.1,
                top_p = 0.9,
                repeat_penalty = 1.1
            }
        }, JsonHelper.GeneralOptions);

        var inputContent = new StringContent(json, Encoding.UTF8, "application/json");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/chat")
        {
            Content = inputContent
        };

        var response = await http.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        string role = "", content = "";
        string compose = "";
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
            {
                break;
            }

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var chunk = JsonSerializer.Deserialize<ChatResponse>(line, JsonHelper.GeneralOptions);
            if (chunk == null)
            {
                throw new FormatException("Invalid response format");
            }

            role = chunk.Message.Role;
            content += chunk.Message.Content;
            compose += chunk.Message.Content;
            while (compose.Contains('\n'))
            {
                int indexOf = compose.IndexOf('\n');
                AnsiConsole.WriteLine(compose[..indexOf]);
                compose = compose[(indexOf + 1)..];
            }
        }

        if (!string.IsNullOrEmpty(compose))
        {
            AnsiConsole.WriteLine(compose);
        }

        AnsiConsole.WriteLine("Generating done.");

        s_Messages.Add(messages.Last());
        s_Messages.Add(new ChatMessage
        {
            Role = role,
            Content = content
        });

        content = content.Trim('`');
        if (content.StartsWith("json"))
        {
            content = content[4..];
        }
        content = content.Trim();
        return JsonSerializer.Deserialize<OutputElement[]>(content, JsonHelper.GeneralOptions) ?? throw new InvalidOperationException("Failed to deserialize output");
    }

    public static void Validate(IReadOnlyList<OutputElement> results, Dictionary<int, InputElement> vocabs, IList<OutputElement> errors)
    {
        foreach (var item in results)
        {
            string reconstructedKanji = "";
            string reconstructedKana = "";

            foreach (var mapping in item.Mapping)
            {
                var ss = mapping.Split('|');
                if (ss.Length == 2)
                {
                    reconstructedKanji += ss[0];
                    reconstructedKana += ss[1];
                }
                else
                {
                    reconstructedKanji += ss[0];
                    reconstructedKana += ss[0];
                }
            }

            var original = vocabs[item.Id];
            if (reconstructedKanji != original.Original || reconstructedKana != original.Furigana)
            {
                errors.Add(item);
            }
        }
    }
}
