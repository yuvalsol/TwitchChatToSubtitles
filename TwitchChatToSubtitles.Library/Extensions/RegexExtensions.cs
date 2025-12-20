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

    public delegate bool IsMatchEvaluator(Match match);

    public static string ReplaceGroup(this Regex regex, string input, string groupName, string replacement, IsMatchEvaluator evaluator = null)
    {
        return regex.Replace(
            input,
            match =>
            {
                if (evaluator != null && evaluator(match) == false)
                    return match.Value;

                Group group = match.Groups[groupName];
                if (group.Success)
                {
                    var sb = new StringBuilder();

                    int previousCaptureEnd = 0;
                    foreach (Capture capture in group.Captures)
                    {
                        int currentCaptureEnd = capture.Index + capture.Length - match.Index;
                        int currentCaptureLength = capture.Index - match.Index - previousCaptureEnd;
                        sb.Append(match.Value.AsSpan(previousCaptureEnd, currentCaptureLength));
                        sb.Append(replacement);
                        previousCaptureEnd = currentCaptureEnd;
                    }

                    sb.Append(match.Value.AsSpan(previousCaptureEnd));

                    return sb.ToString();
                }
                else
                {
                    return match.Value;
                }
            }
        );
    }

    public static string ReplaceGroup(this Regex regex, string input, int groupNum, string replacement, IsMatchEvaluator evaluator = null)
    {
        return regex.Replace(
            input,
            match =>
            {
                if (evaluator != null && evaluator(match) == false)
                    return match.Value;

                Group group = match.Groups[groupNum];
                if (group.Success)
                {
                    var sb = new StringBuilder();

                    int previousCaptureEnd = 0;
                    foreach (Capture capture in group.Captures)
                    {
                        int currentCaptureEnd = capture.Index + capture.Length - match.Index;
                        int currentCaptureLength = capture.Index - match.Index - previousCaptureEnd;
                        sb.Append(match.Value.AsSpan(previousCaptureEnd, currentCaptureLength));
                        sb.Append(replacement);
                        previousCaptureEnd = currentCaptureEnd;
                    }

                    sb.Append(match.Value.AsSpan(previousCaptureEnd));

                    return sb.ToString();
                }
                else
                {
                    return match.Value;
                }
            }
        );
    }
}
