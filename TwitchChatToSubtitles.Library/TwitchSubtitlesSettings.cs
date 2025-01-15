namespace TwitchChatToSubtitles.Library;

public class TwitchSubtitlesSettings
{
    public SubtitlesType SubtitlesType { get; set; }

    // RegularSubtitles
    public bool RegularSubtitles { get { return SubtitlesType == SubtitlesType.RegularSubtitles; } }
    public int SubtitleShowDuration { get; set; } = 5;

    // RollingChatSubtitles
    public bool RollingChatSubtitles { get { return SubtitlesType == SubtitlesType.RollingChatSubtitles; } }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }

    // StaticChatSubtitles
    public bool StaticChatSubtitles { get { return SubtitlesType == SubtitlesType.StaticChatSubtitles; } }

    // RollingChatSubtitles
    // StaticChatSubtitles
    public SubtitlesLocation SubtitlesLocation { get; set; }

    // all
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public bool ShowTimestamps { get; set; }
    public int TimeOffset { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ColorUserNames { get; set; }

    internal bool IsUsingAssaTags
    {
        get
        {
            return
                RollingChatSubtitles ||
                StaticChatSubtitles ||
                ColorUserNames ||
                ShowTimestamps ||
                SubtitlesFontSize != SubtitlesFontSize.None;
        }
    }
}
