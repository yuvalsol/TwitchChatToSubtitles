namespace TwitchChatToSubtitles.Library;

public sealed class FinishEventArgs(string srtFile, Exception error) : EventArgs
{
    public string SrtFile { get; private set; } = srtFile;
    public Exception Error { get; private set; } = error;

    public static new readonly FinishEventArgs Empty = new(default, default);
}
