namespace TwitchChatToSubtitles.Library;

public class TwitchSubtitlesSettings
{
    public SubtitlesType SubtitlesType { get; set; }

    public bool RegularSubtitles { get { return SubtitlesType == SubtitlesType.RegularSubtitles; } }
    public bool RollingChatSubtitles { get { return SubtitlesType == SubtitlesType.RollingChatSubtitles; } }
    public bool StaticChatSubtitles { get { return SubtitlesType == SubtitlesType.StaticChatSubtitles; } }
    public bool ChatTextFile { get { return SubtitlesType == SubtitlesType.ChatTextFile; } }

    public bool ColorUserNames { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ShowTimestamps { get; set; }
    public int SubtitleShowDuration { get; set; } = 5;
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public SubtitlesLocation SubtitlesLocation { get; set; }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public int TimeOffset { get; set; }

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
                    SubtitlesFontSize != SubtitlesFontSize.None
                );
        }
    }
}
