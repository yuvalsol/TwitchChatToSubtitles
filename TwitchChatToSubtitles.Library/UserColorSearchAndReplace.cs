namespace TwitchChatToSubtitles.Library;

internal class UserColorSearchAndReplace(string pattern, string replace)
{
    public readonly Regex Search = new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public readonly string Replace = replace;
}
