namespace TwitchChatToSubtitles.Library;

public sealed class FinishEventArgs(string srtFile) : EventArgs
{
    public string SrtFile { get; private set; } = srtFile;

    public static new readonly FinishEventArgs Empty = new(default);
}
