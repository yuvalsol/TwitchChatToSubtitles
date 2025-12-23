namespace TwitchChatToSubtitles.Library;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class SubtitlesFontSizeMeasurementsAttribute : Attribute
{
    public int LineLength { get; set; }
    public int PosXLocationRight { get; set; }
    public int MaxBottomPosY { get; set; }
    public int TimestampFontSize { get; set; }
}
