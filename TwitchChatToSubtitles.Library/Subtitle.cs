namespace TwitchChatToSubtitles.Library;

internal class Subtitle(TimeSpan showTime, TimeSpan hideTime) : IMessage
{
    internal const string FONT_NAME = "Calibri";
    internal const int POS_X_LOCATION_LEFT = 3;

    public readonly TimeSpan ShowTime = showTime;
    public TimeSpan HideTime { get; private set; } = hideTime;
    public readonly bool HasPosY;
    public readonly int PosY;
    private readonly List<ChatMessage> Messages = [];

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, ChatMessage message)
        : this(showTime, hideTime)
    {
        Messages.Add(message);
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, IEnumerable<ChatMessage> messages)
        : this(showTime, hideTime)
    {
        Messages.AddRange(messages);
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, Subtitle subtitle)
        : this(showTime, hideTime, subtitle.Messages)
    { }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, int posY)
        : this(showTime, hideTime)
    {
        HasPosY = true;
        PosY = posY;
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, int posY, ChatMessage message)
        : this(showTime, hideTime, message)
    {
        HasPosY = true;
        PosY = posY;
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, int posY, IEnumerable<ChatMessage> messages)
        : this(showTime, hideTime, messages)
    {
        HasPosY = true;
        PosY = posY;
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, int posY, Subtitle subtitle)
        : this(showTime, hideTime, subtitle)
    {
        HasPosY = true;
        PosY = posY;
    }

    public bool IsEmpty
    {
        get
        {
            return Messages.IsNullOrEmpty();
        }
    }

    public bool HasEmptyMessage
    {
        get
        {
            return Messages.HasAny(message => message == null);
        }
    }

    public void InsertFirstMessage(ChatMessage message)
    {
        Messages.Insert(0, message);
        linesCount = 0;
    }

    public void AddMessage(ChatMessage message)
    {
        Messages.Add(message);
        linesCount = 0;
    }

    public void AddMessages(IEnumerable<ChatMessage> messages)
    {
        Messages.AddRange(messages);
        linesCount = 0;
    }

    public void AddMessages(Subtitle subtitle)
    {
        AddMessages(subtitle.Messages);
    }

    public void SetSubtitlesConsecutively(Subtitle prevSubtitle)
    {
        prevSubtitle.HideTime = ShowTime;
    }

    private int linesCount;
    public int LinesCount
    {
        get
        {
            if (linesCount == 0)
                linesCount = Messages.Sum(m => m.LinesCount);
            return linesCount;
        }
    }

    public bool IsOverlapWith(Subtitle subtitle)
    {
        // show1 --- hide1
        //           show2 --- hide2

        if (subtitle.ShowTime >= this.HideTime)
            return false;

        //           show1 --- hide1
        // show2 --- hide2

        if (this.ShowTime >= subtitle.HideTime)
            return false;

        return true;
    }

    public Subtitle ShaveLinesFromTheTop(int shaveCount)
    {
        if (shaveCount <= 0)
            return this;

        if (IsEmpty)
            return this;

        while (shaveCount > 0)
        {
            var message = Messages[0];
            int messageLinesCount = message.LinesCount;

            if (shaveCount < messageLinesCount)
            {
                Messages[0] = message.ShaveLinesFromTheTop(shaveCount);
                linesCount = 0;
                return this;
            }
            else if (shaveCount == messageLinesCount)
            {
                Messages.RemoveAt(0);
                linesCount = 0;
                return this;
            }
            else if (shaveCount > messageLinesCount)
            {
                Messages.RemoveAt(0);
                linesCount = 0;
                if (IsEmpty)
                    return this;
                shaveCount -= messageLinesCount;
            }
        }

        return this;
    }

    public Subtitle ShaveLinesFromTheBottom(int shaveCount)
    {
        if (shaveCount <= 0)
            return this;

        if (IsEmpty)
            return this;

        while (shaveCount > 0)
        {
            var message = Messages[^1];
            int messageLinesCount = message.LinesCount;

            if (shaveCount < messageLinesCount)
            {
                Messages[^1] = message.ShaveLinesFromTheBottom(shaveCount);
                linesCount = 0;
                return this;
            }
            else if (shaveCount == messageLinesCount)
            {
                Messages.RemoveAt(Messages.Count - 1);
                linesCount = 0;
                return this;
            }
            else if (shaveCount > messageLinesCount)
            {
                Messages.RemoveAt(Messages.Count - 1);
                linesCount = 0;
                if (IsEmpty)
                    return this;
                shaveCount -= messageLinesCount;
            }
        }

        return this;
    }

    public Subtitle KeepLinesFromTheTop(int keepCount)
    {
        if (keepCount < 0)
            return this;

        return ShaveLinesFromTheBottom(LinesCount - keepCount);
    }

    public Subtitle KeepLinesFromTheBottom(int keepCount)
    {
        if (keepCount < 0)
            return this;

        return ShaveLinesFromTheTop(LinesCount - keepCount);
    }

    public override string ToString()
    {
        return ToString(false, SubtitlesLocation.Left, SubtitlesFontSize.None, 0, 0, null, false);
    }

    public string ToString(TwitchSubtitlesSettings settings)
    {
        return ToString(settings.ShowTimestamps, settings.SubtitlesLocation, settings.SubtitlesFontSize, settings.PosXLocationRight, settings.TimestampFontSize, settings.InternalTextColor, settings.IsUsingAssaTags);
    }

    public string ToString(bool showTimestamps, SubtitlesLocation subtitlesLocation, SubtitlesFontSize subtitlesFontSize, int posXLocationRight, int timestampFontSize, Color textColor, bool isUsingAssaTags)
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"{(ShowTime.Days * 24) + ShowTime.Hours:00}{ShowTime:\:mm\:ss\,fff} --> {(HideTime.Days * 24) + HideTime.Hours:00}{HideTime:\:mm\:ss\,fff}");

        if (HasPosY || subtitlesFontSize != SubtitlesFontSize.None)
        {
            string fontSizeStr = string.Empty;
            if (subtitlesFontSize != SubtitlesFontSize.None)
                fontSizeStr = $@"\fs{(int)subtitlesFontSize}";

            if (HasPosY)
                sb.Append($@"{{\a5\an7\pos({(subtitlesLocation.IsRight() ? posXLocationRight : POS_X_LOCATION_LEFT)},{PosY})\fn{FONT_NAME}{fontSizeStr}\bord0\shad0}}");
            else if (subtitlesFontSize != SubtitlesFontSize.None)
                sb.Append($@"{{\fn{FONT_NAME}{fontSizeStr}\bord0\shad0}}");
        }
        else if (isUsingAssaTags)
        {
            sb.Append(@"{\bord0\shad0}");
        }

        foreach (var message in Messages)
            sb.AppendLine(message.ToString(showTimestamps, subtitlesFontSize, timestampFontSize, textColor, isUsingAssaTags));

        return sb.ToString();
    }

    public string ToChatLogString(TwitchSubtitlesSettings settings)
    {
        return ToChatLogString(settings.ShowTimestamps);
    }

    public string ToChatLogString(bool showTimestamps)
    {
        var sb = new StringBuilder();
        foreach (var message in Messages)
            sb.AppendLine(message.ToChatLogString(showTimestamps));
        return sb.ToString();
    }
}
