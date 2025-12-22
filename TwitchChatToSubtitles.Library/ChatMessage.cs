namespace TwitchChatToSubtitles.Library;

internal partial class ChatMessage(string body) : IMessage
{
    public readonly TimeSpan Timestamp;
    public readonly string User;
    public readonly bool IsModerator;
    public readonly Color UserColor;
    public readonly bool IsBrailleArt;
    public readonly string Body = body;

    public ChatMessage(TimeSpan timestamp, string user, bool isModerator, Color userColor, string body, bool isBrailleArt)
        : this(body)
    {
        Timestamp = timestamp;
        User = user;
        IsModerator = isModerator;
        UserColor = userColor;
        IsBrailleArt = isBrailleArt;
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

    public ChatMessage ShaveLinesFromTheTop(int shaveCount)
    {
        if (shaveCount <= 0)
            return this;

        if (shaveCount >= LinesCount)
            return null;

        if (IsBrailleArt && string.IsNullOrEmpty(User) == false)
        {
            shaveCount--;
            if (shaveCount == 0)
                return new ChatMessage(Body);
        }

        var startIndex =
            RegexHardNewLine().Matches(Body)
            .Select(m => m.Index)
            .Skip(shaveCount - 1)
            .FirstOrDefault(-1);

        if (startIndex == -1)
            return null;

        startIndex += 2;

        if (startIndex > Body.Length)
            return null;

        if (startIndex == Body.Length)
            return new ChatMessage(string.Empty);

        string shavedBody = Body[startIndex..];
        return new ChatMessage(shavedBody);
    }

    public ChatMessage ShaveLinesFromTheBottom(int shaveCount)
    {
        if (shaveCount <= 0)
            return this;

        if (shaveCount >= LinesCount)
            return null;

        var lastIndex =
            RegexHardNewLine().Matches(Body)
            .Select(m => m.Index)
            .SkipLast(shaveCount - 1)
            .LastOrDefault(-1);

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

                timestampStr = $@"{{\fs6{(textColor != null ? $@"\c&{textColor.BGR}&" : string.Empty)}}}{ToTimestamp(Timestamp)}{{{(textColor != null ? @"\c" : string.Empty)}\rfs\fn{Subtitle.FONT_NAME}{fontSizeStr}\bord0\shad0}} ";
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
            return ChatLogBody();
        else
            return $"{ChatLogTimestampAndUser(showTimestamps)}:{(IsBrailleArt ? Environment.NewLine : " ")}{ChatLogBody()}";
    }

    public string ChatLogTimestampAndUser(bool showTimestamps)
    {
        return $"{(showTimestamps ? $"{ToChatLogTimestamp(Timestamp)} " : string.Empty)}{(IsModerator ? "[M] " : string.Empty)}{User}";
    }

    public string ChatLogBody()
    {
        return (IsBrailleArt ? Body.Replace(@"\N", Environment.NewLine) : Body);
    }
}
