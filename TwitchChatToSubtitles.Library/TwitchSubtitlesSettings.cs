namespace TwitchChatToSubtitles.Library;

public class TwitchSubtitlesSettings
{
    public bool ASS { get; set; }

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
                var measurements = (SubtitlesFontSizeMeasurementsAttribute)fi.GetCustomAttribute(typeof(SubtitlesFontSizeMeasurementsAttribute));
                LineLength = measurements.LineLength;
                TextPosXLocationRight = measurements.TextPosXLocationRight;
                BraillePosXLocationRight = measurements.BraillePosXLocationRight;
                MaxBottomPosY = measurements.MaxBottomPosY;
                TimestampFontSize = measurements.TimestampFontSize;
            }
        }
    }

    internal int LineLength { get; private set; }
    internal int TextPosXLocationRight { get; private set; }
    internal int BraillePosXLocationRight { get; private set; }
    internal int MaxBottomPosY { get; private set; }
    internal int TimestampFontSize { get; private set; }

    public SubtitlesLocation SubtitlesLocation { get; set; }
    public SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public Color? TextColor { get; set; }
    public int TimeOffset { get; set; }

    private ASSAColor textASSAColor;
    internal ASSAColor TextASSAColor
    {
        get
        {
            if (TextColor == null)
                return null;

            return textASSAColor ??= new ASSAColor($"#{TextColor.Value.R:X2}{TextColor.Value.G:X2}{TextColor.Value.B:X2}");
        }
    }

    internal bool IsUsingAssaTags
    {
        get
        {
            return
                (ChatTextFile == false) &&
                (
                    ASS ||
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
