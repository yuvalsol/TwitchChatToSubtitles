using TwitchChatToSubtitles.Library;

namespace TwitchChatToSubtitlesUI;

[Serializable]
internal class UISettings
{
    public string SubtitlesTypeName { get { return TwitchChatToSubtitlesForm.GetEnumName(SubtitlesType); } }
    public SubtitlesType SubtitlesType { get; set; }
    public bool BoldText { get; set; }
    public bool ColorUserNames { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ShowTimestamps { get; set; }
    public string SubtitlesLocationName { get { return TwitchChatToSubtitlesForm.GetEnumName(SubtitlesLocation); } }
    public SubtitlesLocation SubtitlesLocation { get; set; }
    public string SubtitlesFontSizeName { get { return TwitchChatToSubtitlesForm.GetEnumName(SubtitlesFontSize); } }
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public string SubtitlesRollingDirectionName { get { return TwitchChatToSubtitlesForm.GetEnumName(SubtitlesRollingDirection); } }
    public SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }
    public string SubtitlesSpeedName { get { return TwitchChatToSubtitlesForm.GetEnumName(SubtitlesSpeed); } }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public decimal TimeOffset { get; set; }
    public decimal SubtitleShowDuration { get; set; }
    public string TextColor { get; set; }
    public bool ASS { get; set; }
    public bool CloseWhenFinishedSuccessfully { get; set; }
    public string JsonDirectory { get; set; }
}
