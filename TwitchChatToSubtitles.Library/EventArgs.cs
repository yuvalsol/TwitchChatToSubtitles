namespace TwitchChatToSubtitles.Library;

public sealed class StartLoadingJsonFileEventArgs(string jsonFile) : EventArgs
{
    public string JsonFile { get; private set; } = jsonFile;

    public static new readonly StartLoadingJsonFileEventArgs Empty = new(default);
}

public sealed class FinishLoadingJsonFileEventArgs(string jsonFile, Exception error) : EventArgs
{
    public string JsonFile { get; private set; } = jsonFile;
    public Exception Error { get; private set; } = error;

    public static new readonly FinishLoadingJsonFileEventArgs Empty = new(default, default);
}

public sealed class StartWritingPreparationsEventArgs(bool removeEmoticonNames, bool colorUserNames) : EventArgs
{
    public bool RemoveEmoticonNames { get; private set; } = removeEmoticonNames;
    public bool ColorUserNames { get; private set; } = colorUserNames;

    public static new readonly StartWritingPreparationsEventArgs Empty = new(default, default);
}

public sealed class FinishWritingPreparationsEventArgs(bool removeEmoticonNames, bool colorUserNames, Exception error) : EventArgs
{
    public bool RemoveEmoticonNames { get; private set; } = removeEmoticonNames;
    public bool ColorUserNames { get; private set; } = colorUserNames;
    public Exception Error { get; private set; } = error;

    public static new readonly FinishWritingPreparationsEventArgs Empty = new(default, default, default);
}

public sealed class StartWritingSubtitlesEventArgs(string srtFile) : EventArgs
{
    public string SrtFile { get; private set; } = srtFile;

    public static new readonly StartWritingSubtitlesEventArgs Empty = new(default);
}

public sealed class ProgressEventArgs(int messagesCount, int discardedMessagesCount, int totalMessages, int subtitlesCount) : EventArgs
{
    public int MessagesCount { get; private set; } = messagesCount;
    public int DiscardedMessagesCount { get; private set; } = discardedMessagesCount;
    public int TotalMessages { get; private set; } = totalMessages;
    public int SubtitlesCount { get; private set; } = subtitlesCount;

    public static new readonly ProgressEventArgs Empty = new(default, default, default, default);
}

public sealed class FinishEventArgs(string srtFile, Exception error) : EventArgs
{
    public string SrtFile { get; private set; } = srtFile;
    public Exception Error { get; private set; } = error;

    public static new readonly FinishEventArgs Empty = new(default, default);
}
