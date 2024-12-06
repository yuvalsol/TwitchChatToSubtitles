namespace TwitchChatToSubtitles.Library;

public enum SubtitlesLocation
{
    None = 0,
    Left,
    LeftTopHalf,
    LeftBottomHalf,
    LeftTopTwoThirds,
    LeftBottomTwoThirds,
    Right,
    RightTopHalf,
    RightBottomHalf,
    RightTopTwoThirds,
    RightBottomTwoThirds
}

internal static partial class SubtitlesLocationExtensions
{
    public static bool IsLeft(this SubtitlesLocation subtitlesLocation)
    {
        return
            subtitlesLocation == SubtitlesLocation.Left ||
            subtitlesLocation == SubtitlesLocation.LeftTopHalf ||
            subtitlesLocation == SubtitlesLocation.LeftBottomHalf ||
            subtitlesLocation == SubtitlesLocation.LeftTopTwoThirds ||
            subtitlesLocation == SubtitlesLocation.LeftBottomTwoThirds;
    }

    public static bool IsRight(this SubtitlesLocation subtitlesLocation)
    {
        return
            subtitlesLocation == SubtitlesLocation.Right ||
            subtitlesLocation == SubtitlesLocation.RightTopHalf ||
            subtitlesLocation == SubtitlesLocation.RightBottomHalf ||
            subtitlesLocation == SubtitlesLocation.RightTopTwoThirds ||
            subtitlesLocation == SubtitlesLocation.RightBottomTwoThirds;
    }

    public static bool IsHalf(this SubtitlesLocation subtitlesLocation)
    {
        return
            subtitlesLocation.IsTopHalf() ||
            subtitlesLocation.IsBottomHalf();
    }

    public static bool IsTopHalf(this SubtitlesLocation subtitlesLocation)
    {
        return
            subtitlesLocation == SubtitlesLocation.LeftTopHalf ||
            subtitlesLocation == SubtitlesLocation.RightTopHalf;
    }

    public static bool IsBottomHalf(this SubtitlesLocation subtitlesLocation)
    {
        return
            subtitlesLocation == SubtitlesLocation.LeftBottomHalf ||
            subtitlesLocation == SubtitlesLocation.RightBottomHalf;
    }

    public static bool IsTwoThirds(this SubtitlesLocation subtitlesLocation)
    {
        return
            subtitlesLocation.IsTopTwoThirds() ||
            subtitlesLocation.IsBottomTwoThirds();
    }

    public static bool IsTopTwoThirds(this SubtitlesLocation subtitlesLocation)
    {
        return
            subtitlesLocation == SubtitlesLocation.LeftTopTwoThirds ||
            subtitlesLocation == SubtitlesLocation.RightTopTwoThirds;
    }

    public static bool IsBottomTwoThirds(this SubtitlesLocation subtitlesLocation)
    {
        return
            subtitlesLocation == SubtitlesLocation.LeftBottomTwoThirds ||
            subtitlesLocation == SubtitlesLocation.RightBottomTwoThirds;
    }
}
