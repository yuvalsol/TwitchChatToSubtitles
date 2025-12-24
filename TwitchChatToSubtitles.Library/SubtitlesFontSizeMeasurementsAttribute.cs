namespace TwitchChatToSubtitles.Library;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class SubtitlesFontSizeMeasurementsAttribute : Attribute
{
    /// <summary>
    /// The maximum number of characters that comprise a line of text.
    /// </summary>
    public int LineLength { get; set; }

    /// <summary>
    /// The position from the left of a text line, when the subtitles location is set to the right side of the screen.
    /// </summary>
    public int TextPosXLocationRight { get; set; }

    /// <summary>
    /// The position from the left of a braille line, when the subtitles location is set to the right side of the screen.
    /// </summary>
    public int BraillePosXLocationRight { get; set; }

    /// <summary>
    /// The maximum possible position at the bottom of the screen, so the text is visible and not cropped out of the screen.
    /// </summary>
    public int MaxBottomPosY { get; set; }

    /// <summary>
    /// The size of the font of the timestamp.
    /// </summary>
    public int TimestampFontSize { get; set; }
}
