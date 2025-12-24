namespace TwitchChatToSubtitles.Library;

internal interface IMessage
{
    string ToString(TwitchSubtitlesSettings settings, int messageIndex);
}
