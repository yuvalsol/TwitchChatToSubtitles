using TwitchChatToSubtitles.Library;

namespace TwitchChatToSubtitlesUI;

[Serializable]
internal class UISettings
{
    public string _SubtitlesType { get { return $"// {SubtitlesType}"; } }
    public SubtitlesType SubtitlesType { get; set; }
    public bool ColorUserNames { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ShowTimestamps { get; set; }
    public string _SubtitlesLocation { get { return $"// {SubtitlesLocation}"; } }
    public SubtitlesLocation SubtitlesLocation { get; set; }
    public string _SubtitlesFontSize { get { return $"// {SubtitlesFontSize}"; } }
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public string _SubtitlesRollingDirection { get { return $"// {SubtitlesRollingDirection}"; } }
    public SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }
    public string _SubtitlesSpeed { get { return $"// {SubtitlesSpeed}"; } }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public decimal TimeOffset { get; set; }
    public decimal SubtitleShowDuration { get; set; }
    public string TextColor { get; set; }
    public bool CloseWhenFinishedSuccessfully { get; set; }
    public string JsonDirectory { get; set; }
}
