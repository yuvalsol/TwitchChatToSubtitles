using TwitchChatToSubtitles.Library;

namespace TwitchChatToSubtitlesUI;

[Serializable]
internal class UISettings
{
    public SubtitlesType SubtitlesType { get; set; }
    public bool ColorUserNames { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ShowTimestamps { get; set; }
    public SubtitlesLocation SubtitlesLocation { get; set; }
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public decimal TimeOffset { get; set; }
    public decimal SubtitleShowDuration { get; set; }
    public string TextColor { get; set; }
    public bool CloseWhenFinishedSuccessfully { get; set; }
    public string JsonDirectory { get; set; }
}
