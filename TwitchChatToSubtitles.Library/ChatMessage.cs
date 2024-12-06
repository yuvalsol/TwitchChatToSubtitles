namespace TwitchChatToSubtitles.Library;

internal partial class ChatMessage
{
    public readonly TimeSpan Timestamp;
    public readonly string User;
    public readonly Color UserColor;
    public readonly bool IsBrailleArt;

    public ChatMessage(string body)
    {
        Body = body;
    }

    public ChatMessage(TimeSpan timestamp, string user, Color userColor, string body, bool isBrailleArt)
        : this(body)
    {
        Timestamp = timestamp;
        User = user;
        UserColor = userColor;
        IsBrailleArt = isBrailleArt;
    }

    private string body;
    public string Body
    {
        get
        {
            return body;
        }

        set
        {
            body = value;
            linesCount = 0;
        }
    }

    [GeneratedRegex(@"\\N")]
    private static partial Regex RegexHardNewLine();

    private int linesCount;
    public int LinesCount
    {
        get
        {
            if (linesCount == 0)
            {
                if (string.IsNullOrEmpty(User))
                    linesCount = RegexHardNewLine().Matches(Body).Count + 1;
                else
                    linesCount = RegexHardNewLine().Matches(Body).Count + 1 + (IsBrailleArt ? 1 : 0);
            }

            return linesCount;
        }
    }

    public ChatMessage ShaveLineFromTheTop()
    {
        return ShaveLinesFromTheTop(1);
    }

    public ChatMessage ShaveLinesFromTheTop(int shaveCount)
    {
        if (shaveCount <= 0)
            return new ChatMessage(Timestamp, User, UserColor, Body, IsBrailleArt);

        if (shaveCount >= LinesCount)
            return null;

        if (IsBrailleArt && string.IsNullOrEmpty(User) == false)
        {
            shaveCount--;
            if (shaveCount == 0)
                return new ChatMessage(Body);
        }

        int startIndex = 0;
        for (int i = 0; i < shaveCount; i++)
        {
            int index = Body.IndexOf("\\N", startIndex);
            if (index == -1)
                return null;
            startIndex = index + 2;
        }

        if (startIndex >= Body.Length)
            return null;

        string shavedBody = Body[startIndex..];

        if (string.IsNullOrEmpty(shavedBody))
            return null;

        return new ChatMessage(shavedBody);
    }

    public static string ToTimestamp(TimeSpan span)
    {
        return span.ToString(span.Days > 0 ? "d':'hh':'mm':'ss" : span.Hours > 0 ? "h':'mm':'ss" : "m':'ss");
    }

    public override string ToString()
    {
        return ToString(false, SubtitlesFontSize.None);
    }

    public string ToString(TwitchSubtitlesSettings settings)
    {
        return ToString(settings.ShowTimestamps, settings.SubtitlesFontSize);
    }

    public string ToString(bool showTimestamps, SubtitlesFontSize subtitlesFontSize)
    {
        if (string.IsNullOrEmpty(User))
        {
            return Body;
        }
        else
        {
            string timestampStr = string.Empty;
            if (showTimestamps)
            {
                string fontSizeStr = string.Empty;
                if (subtitlesFontSize != SubtitlesFontSize.None)
                    fontSizeStr = $@"{{\fs{(int)subtitlesFontSize}}} ";

                timestampStr = $@"{{\fs6}}{ToTimestamp(Timestamp)}{{\rfs}}{fontSizeStr}";
            }

            return $"{timestampStr}{(UserColor != null ? $@"{{\c&{UserColor.BGR}&}}" : string.Empty)}{User}{(UserColor != null ? @"{\c}" : string.Empty)}:{(IsBrailleArt ? @"\N" : " ")}{Body}";
        }
    }
}
