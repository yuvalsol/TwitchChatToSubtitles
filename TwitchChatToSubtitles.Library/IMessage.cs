namespace TwitchChatToSubtitles.Library;

internal interface IMessage
{
    string ToString(TwitchSubtitlesSettings settings);
    string ToChatLogString(TwitchSubtitlesSettings settings);
}
