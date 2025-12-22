namespace TwitchChatToSubtitles.Library;

public class TwitchSubtitlesSettings
{
    public SubtitlesType SubtitlesType { get; set; }

    public bool RegularSubtitles { get { return SubtitlesType == SubtitlesType.RegularSubtitles; } }
    public bool RollingChatSubtitles { get { return SubtitlesType == SubtitlesType.RollingChatSubtitles; } }
    public bool StaticChatSubtitles { get { return SubtitlesType == SubtitlesType.StaticChatSubtitles; } }
    public bool ChatTextFile { get { return SubtitlesType == SubtitlesType.ChatTextFile; } }

    internal bool IsAnySubtitlesTypeSelected
    {
        get
        {
            return
                RegularSubtitles ||
                RollingChatSubtitles ||
                StaticChatSubtitles ||
                ChatTextFile;
        }
    }

    public bool ColorUserNames { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ShowTimestamps { get; set; }
    public int SubtitleShowDuration { get; set; } = 5;

    private SubtitlesFontSize subtitlesFontSize;
    public SubtitlesFontSize SubtitlesFontSize
    {
        get => subtitlesFontSize;

        set
        {
            if (subtitlesFontSize != value)
            {
                subtitlesFontSize = value;

                FieldInfo fi = typeof(SubtitlesFontSize).GetField(subtitlesFontSize.ToString());
                var measurements = (FontSizeMeasurementsAttribute)fi.GetCustomAttribute(typeof(FontSizeMeasurementsAttribute));
                BodyLineLength = measurements.BodyLineLength;
                PosXLocationRight = measurements.PosXLocationRight;
            }
        }
    }

    internal int BodyLineLength { get; private set; }
    internal int PosXLocationRight { get; private set; }

    public SubtitlesLocation SubtitlesLocation { get; set; }
    public SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public System.Drawing.Color? TextColor { get; set; }
    public int TimeOffset { get; set; }

    private Color internalTextColor;
    internal Color InternalTextColor
    {
        get
        {
            if (TextColor == null)
                return null;

            internalTextColor ??= new Color(TextColor.Value);

            return internalTextColor;
        }
    }

    internal bool IsUsingAssaTags
    {
        get
        {
            return
                (ChatTextFile == false) &&
                (
                    RollingChatSubtitles ||
                    StaticChatSubtitles ||
                    ColorUserNames ||
                    ShowTimestamps ||
                    SubtitlesFontSize != SubtitlesFontSize.None ||
                    TextColor != null
                );
        }
    }
}
