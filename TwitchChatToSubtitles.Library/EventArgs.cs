namespace TwitchChatToSubtitles.Library;

public sealed class StartLoadingJsonFileEventArgs(string jsonFile) : EventArgs
{
    public string JsonFile { get; private set; } = jsonFile;
}

public sealed class FinishLoadingJsonFileEventArgs(string jsonFile, Exception error) : EventArgs
{
    public string JsonFile { get; private set; } = jsonFile;
    public Exception Error { get; private set; } = error;
}

public sealed class StartWritingPreparationsEventArgs(bool removeEmoticonNames, bool colorUserNames) : EventArgs
{
    public bool RemoveEmoticonNames { get; private set; } = removeEmoticonNames;
    public bool ColorUserNames { get; private set; } = colorUserNames;
}

public sealed class FinishWritingPreparationsEventArgs(bool removeEmoticonNames, bool colorUserNames, Exception error) : EventArgs
{
    public bool RemoveEmoticonNames { get; private set; } = removeEmoticonNames;
    public bool ColorUserNames { get; private set; } = colorUserNames;
    public Exception Error { get; private set; } = error;
}

public sealed class StartTestingSettingsEventArgs(TwitchSubtitlesSettings settings) : EventArgs
{
    public TwitchSubtitlesSettings Settings { get; private set; } = settings;
}

public sealed class StartWritingSubtitlesEventArgs(string srtFile) : EventArgs
{
    public string SrtFile { get; private set; } = srtFile;
}

public sealed class ProgressEventArgs(int messagesCount, int discardedMessagesCount, int totalMessages, int subtitlesCount) : EventArgs
{
    public int MessagesCount { get; private set; } = messagesCount;
    public int DiscardedMessagesCount { get; private set; } = discardedMessagesCount;
    public int TotalMessages { get; private set; } = totalMessages;
    public int SubtitlesCount { get; private set; } = subtitlesCount;
}

public sealed class FinishTestingSettingsEventArgs(TwitchSubtitlesSettings settings, TimeSpan processTime, Exception error) : EventArgs
{
    public TwitchSubtitlesSettings Settings { get; private set; } = settings;
    public TimeSpan ProcessTime { get; private set; } = processTime;
    public Exception Error { get; private set; } = error;
}

public sealed class FinishEventArgs(string srtFile, TimeSpan processTime, Exception error) : EventArgs
{
    public string SrtFile { get; private set; } = srtFile;
    public TimeSpan ProcessTime { get; private set; } = processTime;
    public Exception Error { get; private set; } = error;
}

public sealed class TracepointEventArgs(string message) : EventArgs
{
    public string Message { get; private set; } = message;
}
