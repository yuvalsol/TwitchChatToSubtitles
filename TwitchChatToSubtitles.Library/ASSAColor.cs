namespace TwitchChatToSubtitles.Library;

internal class ASSAColor(string rgb)
{
    public static readonly ASSAColor Black = new("#000000");
    public static readonly ASSAColor White = new("#FFFFFF");
    public static readonly ASSAColor SemiTransparentDarkBackground = new("#80000000");
    public static readonly ASSAColor SemiTransparentLightBackground = new("#80FFFFFF");

    private readonly string abgr =
        // #aarrggbb
        (rgb.Length == 9 ? string.Concat("&H", $"{255 - Convert.ToInt32(rgb[1..3], 16):X2}", rgb[7..9], rgb[5..7], rgb[3..5], "&") :
        // #rrggbb
        (rgb.Length == 7 ? string.Concat("&H00", rgb[5..7], rgb[3..5], rgb[1..3], "&") :
        // #rgb
        (rgb.Length == 4 ? string.Concat("&H00", rgb[3..4], rgb[3..4], rgb[2..3], rgb[2..3], rgb[1..2], rgb[1..2], "&") : null)))
        .ToUpperInvariant();

    public override string ToString()
    {
        return abgr;
    }
}
