namespace TwitchChatToSubtitles.Library;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class FontSizeMeasurementsAttribute : Attribute
{
    public int BodyLineLength { get; set; }
    public int PosXLocationRight { get; set; }
}
