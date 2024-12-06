namespace TwitchChatToSubtitles.Library;

internal class Color
{
    public readonly string RGB; // #RGB
    public readonly string BGR; // HBGR

    public Color(string rgb)
    {
        RGB = rgb;

        string R = rgb.Substring(1, 2);
        string G = rgb.Substring(3, 2);
        string B = rgb.Substring(5, 2);
        BGR = "H" + B + G + R;
    }

    public override string ToString()
    {
        return RGB;
    }
}
