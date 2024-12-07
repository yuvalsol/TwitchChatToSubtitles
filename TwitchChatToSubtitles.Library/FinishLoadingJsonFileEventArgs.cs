namespace TwitchChatToSubtitles.Library;

public sealed class FinishLoadingJsonFileEventArgs(string jsonFile) : EventArgs
{
    public string JsonFile { get; private set; } = jsonFile;

    public static new readonly FinishLoadingJsonFileEventArgs Empty = new(default);
}
