namespace System.Text.RegularExpressions;

public static partial class RegexExtensions
{
    public static void Remove(this Regex regex, StringBuilder input)
    {
        regex.Remove(input, input.ToString());
    }

    public static void Remove(this Regex regex, StringBuilder input, string inputText)
    {
        foreach (var match in regex.Matches(inputText).OrderByDescending(m => m.Index))
            input.Remove(match.Index, match.Length);
    }

    public static void Replace(this Regex regex, StringBuilder input, string replacement)
    {
        regex.Replace(input, input.ToString(), replacement);
    }

    public static void Replace(this Regex regex, StringBuilder input, string inputText, string replacement)
    {
        foreach (var match in regex.Matches(inputText).OrderByDescending(m => m.Index))
        {
            input.Remove(match.Index, match.Length);
            input.Insert(match.Index, replacement);
        }
    }
}
