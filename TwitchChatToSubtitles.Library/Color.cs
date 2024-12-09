namespace TwitchChatToSubtitles.Library;

internal class Color
{
    public readonly string RGB; // #RGB
    public readonly string BGR; // HBGR

    public Color(string rgb)
    {
        RGB = rgb;

        var R = rgb.AsSpan(1, 2);
        var G = rgb.AsSpan(3, 2);
        var B = rgb.AsSpan(5, 2);
        BGR = string.Concat("H", B, G, R);
    }

    public override string ToString()
    {
        return RGB;
    }
}
