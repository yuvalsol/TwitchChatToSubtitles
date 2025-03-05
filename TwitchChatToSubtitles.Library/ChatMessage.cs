namespace TwitchChatToSubtitles.Library;

internal partial class ChatMessage : IMessage
{
    public readonly TimeSpan Timestamp;
    public readonly string User;
    public readonly bool IsModerator;
    public readonly Color UserColor;
    public readonly bool IsBrailleArt;

    public ChatMessage(string body)
    {
        Body = body;
    }

    public ChatMessage(TimeSpan timestamp, string user, bool isModerator, Color userColor, string body, bool isBrailleArt)
        : this(body)
    {
        Timestamp = timestamp;
        User = user;
        IsModerator = isModerator;
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
                if (string.IsNullOrEmpty(Body))
                {
                    if (string.IsNullOrEmpty(User))
                        linesCount = 0;
                    else
                        linesCount = (IsBrailleArt ? 1 : 0);
                }
                else
                {
                    if (string.IsNullOrEmpty(User))
                        linesCount = RegexHardNewLine().Matches(Body).Count + 1;
                    else
                        linesCount = RegexHardNewLine().Matches(Body).Count + 1 + (IsBrailleArt ? 1 : 0);
                }
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
            return new ChatMessage(Timestamp, User, IsModerator, UserColor, Body, IsBrailleArt);

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

    public ChatMessage ShaveLineFromTheBottom()
    {
        return ShaveLinesFromTheBottom(1);
    }

    public ChatMessage ShaveLinesFromTheBottom(int shaveCount)
    {
        if (shaveCount <= 0)
            return new ChatMessage(Timestamp, User, IsModerator, UserColor, Body, IsBrailleArt);

        if (shaveCount >= LinesCount)
            return null;

        int lastIndex = Body.LastIndexOf("\\N");
        for (int i = 0; i < shaveCount - 1 && lastIndex != -1; i++)
            lastIndex = Body.LastIndexOf("\\N", lastIndex);

        if (lastIndex == -1)
        {
            if (IsBrailleArt)
                return new ChatMessage(Timestamp, User, IsModerator, UserColor, string.Empty, IsBrailleArt);
            else
                return null;
        }

        string shavedBody = Body[..lastIndex];
        return new ChatMessage(Timestamp, User, IsModerator, UserColor, shavedBody, IsBrailleArt);
    }

    public static string ToTimestamp(TimeSpan span)
    {
        return span.ToString(span.Days > 0 ? "d':'hh':'mm':'ss" : span.Hours > 0 ? "h':'mm':'ss" : "m':'ss");
    }

    public static string ToChatLogTimestamp(TimeSpan span)
    {
        return span.ToString(span.Days > 0 ? "d':'hh':'mm':'ss" : "hh':'mm':'ss");
    }

    public override string ToString()
    {
        return ToString(false, SubtitlesFontSize.None, null, false);
    }

    public string ToString(TwitchSubtitlesSettings settings)
    {
        return ToString(settings.ShowTimestamps, settings.SubtitlesFontSize, settings.InternalTextColor, settings.IsUsingAssaTags);
    }

    public string ToString(bool showTimestamps, SubtitlesFontSize subtitlesFontSize, Color textColor, bool isUsingAssaTags)
    {
        if (string.IsNullOrEmpty(User))
        {
            if (textColor != null)
                return $@"{{\c&{textColor.BGR}&}}" + Body;
            else
                return Body;
        }
        else
        {
            string timestampStr = string.Empty;
            if (showTimestamps)
            {
                string fontSizeStr = string.Empty;
                if (subtitlesFontSize != SubtitlesFontSize.None)
                    fontSizeStr = $@"\fs{(int)subtitlesFontSize}";

                timestampStr = $@"{{\fs6{(textColor != null ? $@"\c&{textColor.BGR}&" : string.Empty)}}}{ToTimestamp(Timestamp)}{{{(textColor != null ? @"\c" : string.Empty)}\rfs{fontSizeStr}\bord0\shad0}} ";
            }

            return $"{timestampStr}{(IsModerator && isUsingAssaTags ? @"{\u1}" : string.Empty)}{(UserColor != null ? $@"{{\c&{UserColor.BGR}&}}" : (textColor != null ? $@"{{\c&{textColor.BGR}&}}" : string.Empty))}{User}{(UserColor != null || textColor != null ? @"{\c}" : string.Empty)}{(IsModerator && isUsingAssaTags ? @"{\u0}" : string.Empty)}{(textColor != null ? $@"{{\c&{textColor.BGR}&}}" : string.Empty)}:{(IsBrailleArt ? (isUsingAssaTags ? @"\N" : Environment.NewLine) : " ")}{Body}";
        }
    }

    public string ToChatLogString(TwitchSubtitlesSettings settings)
    {
        return ToChatLogString(settings.ShowTimestamps);
    }

    public string ToChatLogString(bool showTimestamps)
    {
        if (string.IsNullOrEmpty(User))
            return (IsBrailleArt ? Body.Replace(@"\N", Environment.NewLine) : Body);
        else
            return $"{(showTimestamps ? $"{ToChatLogTimestamp(Timestamp)} " : string.Empty)}{(IsModerator ? "[M] " : string.Empty)}{User}:{(IsBrailleArt ? Environment.NewLine : " ")}{(IsBrailleArt ? Body.Replace(@"\N", Environment.NewLine) : Body)}";
    }
}
