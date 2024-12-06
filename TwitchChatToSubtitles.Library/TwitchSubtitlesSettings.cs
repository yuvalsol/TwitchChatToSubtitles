namespace TwitchChatToSubtitles.Library;

public class TwitchSubtitlesSettings
{
    // RegularSubtitles
    public bool RegularSubtitles { get; set; }
    public int SubtitleShowDuration { get; set; } = 5;

    // RollingChatSubtitles
    private bool rollingChatSubtitles;
    public bool RollingChatSubtitles
    {
        get
        {
            return rollingChatSubtitles;
        }

        set
        {
            rollingChatSubtitles = value;

            if (rollingChatSubtitles)
            {
                if (SubtitlesSpeed == SubtitlesSpeed.None)
                    SubtitlesSpeed = SubtitlesSpeed.Regular;

                if (SubtitlesFontSize == SubtitlesFontSize.None)
                    SubtitlesFontSize = SubtitlesFontSize.Regular;

                if (SubtitlesLocation == SubtitlesLocation.None)
                    SubtitlesLocation = SubtitlesLocation.Left;
            }
        }
    }

    public SubtitlesSpeed SubtitlesSpeed { get; set; }

    // StaticChatSubtitles
    private bool staticChatSubtitles;
    public bool StaticChatSubtitles
    {
        get
        {
            return staticChatSubtitles;
        }

        set
        {
            staticChatSubtitles = value;

            if (staticChatSubtitles)
            {
                if (SubtitlesFontSize == SubtitlesFontSize.None)
                    SubtitlesFontSize = SubtitlesFontSize.Regular;

                if (SubtitlesLocation == SubtitlesLocation.None)
                    SubtitlesLocation = SubtitlesLocation.Left;
            }
        }
    }

    // RollingChatSubtitles
    // StaticChatSubtitles
    public SubtitlesLocation SubtitlesLocation { get; set; }

    // all
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public bool ShowTimestamps { get; set; }
    public int TimeOffset { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ColorUserNames { get; set; }
}
