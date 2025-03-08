namespace TwitchChatToSubtitles.Library;

internal abstract class RollingSubtitleInfo
{
    public int N;
    public TimeSpan ShowTime;
    public TimeSpan HideTime;
    public int PosY;

    public override string ToString()
    {
        return $"{nameof(N)}={N}, {nameof(ShowTime)}={ChatMessage.ToChatLogTimestamp(ShowTime)}, {nameof(PosY)}={PosY}";
    }
}

internal class RollingSubtitleInfoTopToBottom : RollingSubtitleInfo
{
    public int KeepCount_Bottom_RollIn;
    public int ShaveCount_Bottom_RollOut;
    public int KeepCount_Bottom_RollOut;

    public override string ToString()
    {
        return $"{base.ToString()}{Environment.NewLine}{nameof(KeepCount_Bottom_RollIn)}={KeepCount_Bottom_RollIn}, {nameof(ShaveCount_Bottom_RollOut)}={ShaveCount_Bottom_RollOut}, {nameof(KeepCount_Bottom_RollOut)}={KeepCount_Bottom_RollOut}";
    }
}

internal class RollingSubtitleInfoBottomToTop : RollingSubtitleInfo
{
    public int KeepCount_Top_RollIn;
    public int ShaveCount_Top_RollOut;
    public int KeepCount_Top_RollOut;

    public override string ToString()
    {
        return $"{base.ToString()}{Environment.NewLine}{nameof(KeepCount_Top_RollIn)}={KeepCount_Top_RollIn}, {nameof(ShaveCount_Top_RollOut)}={ShaveCount_Top_RollOut}, {nameof(KeepCount_Top_RollOut)}={KeepCount_Top_RollOut}";
    }
}
