namespace System.Text.RegularExpressions;

public static partial class RegexExtensions
{
    public static void Replace(this Regex regex, StringBuilder input, string replacement)
    {
        foreach (var match in regex.Matches(input.ToString()).Cast<Match>().OrderByDescending(m => m.Index))
        {
            input.Remove(match.Index, match.Length);
            input.Insert(match.Index, replacement);
        }
    }
}
