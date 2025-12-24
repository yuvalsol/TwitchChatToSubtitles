namespace TwitchChatToSubtitles.Library;

internal class ASSAColor(string rgb)
{
    public static readonly ASSAColor White = new("#FFFFFF");

    private readonly string bgr = string.Concat("&H00", rgb[5..7], rgb[3..5], rgb[1..3], "&");

    public override string ToString()
    {
        return bgr;
    }
}
