namespace TwitchChatToSubtitles.Library;

internal class Subtitle
{
    public readonly TimeSpan ShowTime;
    public TimeSpan HideTime { get; private set; }
    public readonly int PosY;
    private readonly List<ChatMessage> Messages = [];

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, params ChatMessage[] messages)
    {
        ShowTime = showTime;
        HideTime = hideTime;
        AddMessages(messages);
    }

    public Subtitle(TimeSpan showTime, TimeSpan hideTime, int posY, params ChatMessage[] messages)
        : this(showTime, hideTime, messages)
    {
        PosY = posY;
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

    public Subtitle ShaveLineFromTheTop()
    {
        return ShaveLinesFromTheTop(ShowTime, HideTime, 1);
    }

    public Subtitle ShaveLineFromTheTop(TimeSpan showTime, TimeSpan hideTime)
    {
        return ShaveLinesFromTheTop(showTime, hideTime, 1);
    }

    public Subtitle ShaveLinesFromTheTop(int shaveCount)
    {
        return ShaveLinesFromTheTop(ShowTime, HideTime, shaveCount);
    }

    public Subtitle ShaveLinesFromTheTop(TimeSpan showTime, TimeSpan hideTime, int shaveCount)
    {
        if (Messages.IsNullOrEmpty())
            return null;

        var subtitle = new Subtitle(showTime, hideTime, PosY);

        int messageIndex = 0;
        while (shaveCount > 0)
        {
            if (messageIndex >= Messages.Count)
                return null;

            var message = Messages[messageIndex];
            int linesCount = message.LinesCount;

            if (shaveCount <= linesCount)
            {
                if (shaveCount < linesCount)
                {
                    var shavedMessage = message.ShaveLinesFromTheTop(shaveCount);
                    if (shavedMessage != null)
                        subtitle.AddMessage(shavedMessage);
                }

                var nextMessages = Messages.Skip(messageIndex + 1);
                if (nextMessages.HasAny())
                    subtitle.AddMessages(nextMessages);
                shaveCount = 0;
            }
            else if (shaveCount > linesCount)
            {
                messageIndex++;
                shaveCount -= linesCount;
            }
        }

        // shaveCount == 1 && Messages.Count == 1 && Messages[0].LinesCount == 1
        if (subtitle.Messages.IsNullOrEmpty())
            return null;

        return subtitle;
    }

    private static int PosX(SubtitlesLocation subtitlesLocation, SubtitlesFontSize subtitlesFontSize)
    {
        if (subtitlesLocation.IsRight())
        {
            // all measurements are for font Calibri and SPLIT_ON_N_CHARS = 45
            if (subtitlesFontSize == SubtitlesFontSize.Regular)
                return 384 - 115;
            else if (subtitlesFontSize == SubtitlesFontSize.Bigger)
                return 384 - 129;
            else // if (subtitlesFontSize == SubtitlesFontSize.Biggest)
                return 384 - 143;
        }
        else
        {
            return 5;
        }
    }

    public override string ToString()
    {
        return ToString(false, SubtitlesLocation.Left, SubtitlesFontSize.None);
    }

    public string ToString(TwitchSubtitlesSettings settings)
    {
        return ToString(settings.ShowTimestamps, settings.SubtitlesLocation, settings.SubtitlesFontSize);
    }

    public string ToString(bool showTimestamps, SubtitlesLocation subtitlesLocation, SubtitlesFontSize subtitlesFontSize)
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"{(ShowTime.Days * 24) + ShowTime.Hours:00}{ShowTime:\:mm\:ss\,fff} --> {(HideTime.Days * 24) + HideTime.Hours:00}{HideTime:\:mm\:ss\,fff}");

        if (PosY > 0 || subtitlesFontSize != SubtitlesFontSize.None)
        {
            string fontSizeStr = string.Empty;
            if (subtitlesFontSize != SubtitlesFontSize.None)
                fontSizeStr = $@"\fs{(int)subtitlesFontSize}";

            if (PosY > 0)
                sb.AppendLine($@"{{\a5\an7\pos({PosX(subtitlesLocation, subtitlesFontSize)},{PosY}){fontSizeStr}}}");
            else if (subtitlesFontSize != SubtitlesFontSize.None)
                sb.AppendLine($"{{{fontSizeStr}}}");
        }

        foreach (var message in Messages)
            sb.AppendLine(message.ToString(showTimestamps, subtitlesFontSize));

        return sb.ToString();
    }
}
