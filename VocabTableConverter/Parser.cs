using System.Text;

namespace VocabTableConverter;

internal static class Parser
{
    public record VocabEntry(string Original, string Furigana, string English, string JLPTLevel);

    public static VocabEntry[] ParseCSV(string csv)
    {
        var lines = csv.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var columns = Columns(lines[0]);
        if (columns.Length != 4)
        {
            throw new FormatException("Unexpected number of columns in the CSV file.");
        }

        var vocabList = new List<VocabEntry>();
        for (int i = 1; i < lines.Length; ++i)
        {
            var cols = Parser.Columns(lines[i]);
            if (cols.Length != 4)
            {
                throw new FormatException($"Unexpected number of columns in line {i + 1}.");
            }
            vocabList.Add(new VocabEntry(cols[0], cols[1], cols[2], cols[3]));
        }

        return [.. vocabList];
    }

    public static string[] Columns(string s)
    {
        List<string> values = [];
        int seekpos = 0;
        for (int i = 0; i < s.Length; ++i)
        {
            if (s[i] == ',')
            {
                values.Add(s[seekpos..i]);
                seekpos = i + 1;
                continue;
            }

            if (seekpos == i && s[i] == '"')
            {
                values.Add(ReadText(ref s, ref seekpos, ref i));
            }
        }

        if (seekpos < s.Length)
        {
            values.Add(s[seekpos..]);
        }

        return [.. values];
    }

    private static string ReadText(ref string s, ref int seekpos, ref int i)
    {
        ++i;
        seekpos = i;
        StringBuilder sb = new();
        for (; i < s.Length; ++i)
        {
            if (s[i] == '"')
            {
                if (s.Length > i + 1)
                {
                    if (s[i + 1] == '"')
                    {
                        sb.Append('"');
                        ++i;
                    }
                    else if (s[i + 1] == ',')
                    {
                        sb.Append(s[seekpos..i]);
                        seekpos = i + 2;
                        i = seekpos;
                        return sb.ToString();
                    }
                    else
                    {
                        throw new FormatException("Unexpected character after closing quote in quoted text.");
                    }
                }
                else
                {
                    sb.Append(s[seekpos..i]);
                    seekpos = i + 1;
                    i = seekpos;
                    return sb.ToString();
                }
            }
        }

        throw new FormatException("Unexpected end of string while parsing quoted text.");
    }
}
