namespace TwitchChatToSubtitles.Library;

internal class Subtitle(TimeSpan showTime, TimeSpan hideTime) : IMessage
{
    internal const string FONT_NAME = "Calibri";
    internal const string FONT_RESET = @"\b0\bord0\shad0";
    internal const string BOLD_FONT_RESET = @"\b1\bord0\shad0";
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

    public bool HasBrailleArtMessage
    {
        get
        {
            return Messages.HasAny(message => message?.IsBrailleArt ?? false);
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
        return ToString(new TwitchSubtitlesSettings(), 1);
    }

    public string ToString(TwitchSubtitlesSettings settings, int messageIndex)
    {
        var sb = new StringBuilder();

        if (settings.ASS)
        {
            if (settings.RollingChatSubtitles)
                sb.Append($@"Dialogue: {messageIndex % 2},{(ShowTime.Days * 24) + ShowTime.Hours:0}{ShowTime:\:mm\:ss\.ff},{(HideTime.Days * 24) + HideTime.Hours:0}{HideTime:\:mm\:ss\.ff},{(settings.SubtitlesLocation.IsRight() && HasBrailleArtMessage ? "BrailleV" : "TextV")}{PosY},,0,0,0,,");
            else if (settings.StaticChatSubtitles)
                sb.Append($@"Dialogue: 0,{(ShowTime.Days * 24) + ShowTime.Hours:0}{ShowTime:\:mm\:ss\.ff},{(HideTime.Days * 24) + HideTime.Hours:0}{HideTime:\:mm\:ss\.ff},{(settings.SubtitlesLocation.IsRight() && HasBrailleArtMessage ? "Braille" : "Default")},,0,0,0,,");
            else
                sb.Append($@"Dialogue: 0,{(ShowTime.Days * 24) + ShowTime.Hours:0}{ShowTime:\:mm\:ss\.ff},{(HideTime.Days * 24) + HideTime.Hours:0}{HideTime:\:mm\:ss\.ff},Default,,0,0,0,,");

            sb.AppendJoin(@"\N", Messages.Select(message => message.ToString(settings, messageIndex)));
        }
        else
        {
            sb.AppendLine($@"{(ShowTime.Days * 24) + ShowTime.Hours:00}{ShowTime:\:mm\:ss\,fff} --> {(HideTime.Days * 24) + HideTime.Hours:00}{HideTime:\:mm\:ss\,fff}");

            if (HasPosY || settings.SubtitlesFontSize != SubtitlesFontSize.None)
            {
                string fontSizeStr = null;
                if (settings.SubtitlesFontSize != SubtitlesFontSize.None)
                    fontSizeStr = $@"\fs{(int)settings.SubtitlesFontSize}";

                if (HasPosY)
                    sb.Append($@"{{\a5\an7\pos({(settings.SubtitlesLocation.IsRight() ? (HasBrailleArtMessage ? settings.BraillePosXLocationRight : settings.TextPosXLocationRight) : POS_X_LOCATION_LEFT)},{PosY})\fn{FONT_NAME}{fontSizeStr}{(settings.BoldText ? BOLD_FONT_RESET : FONT_RESET)}}}");
                else if (settings.SubtitlesFontSize != SubtitlesFontSize.None)
                    sb.Append($@"{{\fn{FONT_NAME}{fontSizeStr}{(settings.BoldText ? BOLD_FONT_RESET : FONT_RESET)}}}");
            }
            else if (settings.IsUsingAssaTags)
            {
                sb.Append($@"{{{(settings.BoldText ? BOLD_FONT_RESET : FONT_RESET)}}}");
            }

            foreach (var message in Messages)
                sb.AppendLine(message.ToString(settings, messageIndex));
        }

        return sb.ToString();
    }
}
