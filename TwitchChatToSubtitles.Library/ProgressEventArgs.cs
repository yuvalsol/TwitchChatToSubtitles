namespace TwitchChatToSubtitles.Library;

public sealed class ProgressEventArgs(int messagesCount, int discardedMessagesCount, int totalMessages, int subtitlesCount) : EventArgs
{
    public int MessagesCount { get; private set; } = messagesCount;
    public int DiscardedMessagesCount { get; private set; } = discardedMessagesCount;
    public int TotalMessages { get; private set; } = totalMessages;
    public int SubtitlesCount { get; private set; } = subtitlesCount;

    public static new readonly ProgressEventArgs Empty = new(default, default, default, default);
}
