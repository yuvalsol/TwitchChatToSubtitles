namespace TwitchChatToSubtitles.Library;

public sealed class StartWritingPreparationsEventArgs(bool removeEmoticonNames, bool colorUserNames) : EventArgs
{
    public bool RemoveEmoticonNames { get; private set; } = removeEmoticonNames;
    public bool ColorUserNames { get; private set; } = colorUserNames;

    public static new readonly StartWritingPreparationsEventArgs Empty = new(default, default);
}
