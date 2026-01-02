namespace TwitchChatToSubtitles.Library;

internal class ProcessedComment
{
    public TimeSpan Timestamp;
    public string User;
    public bool IsModerator;
    public string Body;
    public bool IsBrailleArt;
    public ASSAColor Color;
}
