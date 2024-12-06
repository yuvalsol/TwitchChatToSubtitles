namespace TwitchChatToSubtitles.Library;

internal class UserColor
{
    public readonly string User;
    public readonly Color Color;
    public readonly UserColorSearchAndReplace Search1;
    public readonly UserColorSearchAndReplace Search2;

    public UserColor(string user, Color color)
    {
        User = user;
        Color = color;

        Search1 = new UserColorSearchAndReplace(
            "@" + user,
            $"{{\\c&{Color.BGR}&}}@{user}{{\\c}}"
        );

        Search2 = new UserColorSearchAndReplace(
            @"(?<![@/])" + user,
            $"{{\\c&{Color.BGR}&}}{user}{{\\c}}"
        );
    }
}
