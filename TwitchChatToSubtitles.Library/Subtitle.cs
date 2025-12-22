namespace TwitchChatToSubtitles.Library;

internal class Subtitle(TimeSpan showTime, TimeSpan hideTime) : IMessage
{
    public readonly TimeSpan ShowTime = showTime;
    public TimeSpan HideTime { get; private set; } = hideTime;
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
        PosY = posY;
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, int posY, ChatMessage message)
        : this(showTime, hideTime, message)
    {
        PosY = posY;
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, int posY, IEnumerable<ChatMessage> messages)
        : this(showTime, hideTime, messages)
    {
        PosY = posY;
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, int posY, Subtitle subtitle)
        : this(showTime, hideTime, subtitle)
    {
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

    internal const string FONT_NAME = "Calibri";

    private static int PosX(SubtitlesLocation subtitlesLocation)
    {
        if (subtitlesLocation.IsRight())
        {
            /*
            measurement is for Calibri font and BIGGER_LINE_LENGTH = 45.
            REGULAR_LINE_LENGTH = 50 has more chars and BIGGEST_LINE_LENGTH = 40 has less chars,
            so, although they both are positioned at X = 255,
            they don't overflow out of the right side of the screen

            1
            00:00:00,000 --> 9:59:59,999
            {\a5\an7\pos(255,65)\fnCalibri\fs8\bord0\shad0}
            12345678901234567890123456789012345678901234567890

            2
            00:00:00,000 --> 9:59:59,999
            {\a5\an7\pos(255,71)\fnCalibri\fs9\bord0\shad0}
            123456789012345678901234567890123456789012340

            3
            00:00:00,000 --> 9:59:59,999
            {\a5\an7\pos(255,77)\fnCalibri\fs10\bord0\shad0}
            1234567890123456789012345678901234567890
            */

            return 255; // = 384 - 129
        }
        else
        {
            return 5;
        }
    }

    public override string ToString()
    {
        return ToString(false, SubtitlesLocation.Left, SubtitlesFontSize.None, null, false);
    }

    public string ToString(TwitchSubtitlesSettings settings)
    {
        return ToString(settings.ShowTimestamps, settings.SubtitlesLocation, settings.SubtitlesFontSize, settings.InternalTextColor, settings.IsUsingAssaTags);
    }

    public string ToString(bool showTimestamps, SubtitlesLocation subtitlesLocation, SubtitlesFontSize subtitlesFontSize, Color textColor, bool isUsingAssaTags)
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"{(ShowTime.Days * 24) + ShowTime.Hours:00}{ShowTime:\:mm\:ss\,fff} --> {(HideTime.Days * 24) + HideTime.Hours:00}{HideTime:\:mm\:ss\,fff}");

        if (PosY > 0 || subtitlesFontSize != SubtitlesFontSize.None)
        {
            string fontSizeStr = string.Empty;
            if (subtitlesFontSize != SubtitlesFontSize.None)
                fontSizeStr = $@"\fs{(int)subtitlesFontSize}";

            if (PosY > 0)
                sb.AppendLine($@"{{\a5\an7\pos({PosX(subtitlesLocation)},{PosY})\fn{FONT_NAME}{fontSizeStr}\bord0\shad0}}");
            else if (subtitlesFontSize != SubtitlesFontSize.None)
                sb.AppendLine($@"{{\fn{FONT_NAME}{fontSizeStr}\bord0\shad0}}");
        }
        else if (isUsingAssaTags)
        {
            sb.AppendLine(@"{\bord0\shad0}");
        }

        foreach (var message in Messages)
            sb.AppendLine(message.ToString(showTimestamps, subtitlesFontSize, textColor, isUsingAssaTags));

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
