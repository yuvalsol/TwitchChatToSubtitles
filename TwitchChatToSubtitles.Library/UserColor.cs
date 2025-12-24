namespace TwitchChatToSubtitles.Library;

internal class UserColor
{
    public readonly string User;
    public readonly ASSAColor Color;

    private readonly Regex Search1;
    private readonly string Replacement1;

    private readonly Regex Search2;
    private readonly string Replacement2;

    public UserColor(string user, ASSAColor color)
    {
        User = user;
        Color = color;

        // (?<=^|\b|\s|\\N)
        // @
        // user
        // (?![/])
        // (?!\.com)
        // (?=$|\b|\s|\\N)
        Search1 = new Regex($@"(?<=^|\b|\s|\\N)@{Regex.Escape(user)}(?![/])(?!\.com)(?=$|\b|\s|\\N)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Replacement1 = $@"{{\c{Color}}}@{user}{{\c}}";

        // (?<=^|\b|\s|\\N)
        // (?<![@/])
        // user
        // (?![@/])
        // (?!\.com)
        // (?=$|\b|\s|\\N)
        Search2 = new Regex($@"(?<=^|\b|\s|\\N)(?<![@/]){Regex.Escape(user)}(?![@/])(?!\.com)(?=$|\b|\s|\\N)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Replacement2 = $@"{{\c{Color}}}{user}{{\c}}";
    }

    public void SearchAndReplace(StringBuilder body)
    {
        Search1.Replace(body, Replacement1);
        Search2.Replace(body, Replacement2);
    }

    public void SearchAndReplace(StringBuilder body, string textColorStr)
    {
        Search1.Replace(body, Replacement1 + textColorStr);
        Search2.Replace(body, Replacement2 + textColorStr);
    }
}
