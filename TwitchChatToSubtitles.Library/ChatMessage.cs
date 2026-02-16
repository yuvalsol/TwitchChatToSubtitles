namespace TwitchChatToSubtitles.Library;

internal partial class ChatMessage(string body) : IMessage
{
    public readonly TimeSpan Timestamp;
    public readonly string User;
    public readonly bool IsModerator;
    public readonly ASSAColor UserColor;
    public readonly bool IsBrailleArt;
    public readonly string Body = body;

    private ChatMessage(string body, bool isBrailleArt)
        : this(body)
    {
        IsBrailleArt = isBrailleArt;
    }

    public ChatMessage(TimeSpan timestamp, string user, bool isModerator, ASSAColor userColor, string body, bool isBrailleArt)
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
                        return 0;
                    else
                        return linesCount = (IsBrailleArt ? 1 : 0);
                }
                else
                {
                    if (string.IsNullOrEmpty(User))
                        return linesCount = RegexHardNewLine().Matches(Body).Count + 1;
                    else
                        return linesCount = RegexHardNewLine().Matches(Body).Count + 1 + (IsBrailleArt ? 1 : 0);
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
                return new ChatMessage(Body, IsBrailleArt);
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
        return new ChatMessage(shavedBody, IsBrailleArt);
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
        return ToString(new TwitchSubtitlesSettings(), 1);
    }

    public string ToString(TwitchSubtitlesSettings settings, int messageIndex)
    {
        if (settings.ChatTextFile)
        {
            string chatLogBody = (IsBrailleArt ? Body.Replace(@"\N", Environment.NewLine) : Body);

            if (string.IsNullOrEmpty(User))
                return chatLogBody;
            else
                return $"[{messageIndex}] {ChatLogTimestampAndUser(settings.ShowTimestamps)}:{(IsBrailleArt ? Environment.NewLine : " ")}{chatLogBody}";
        }

        if (string.IsNullOrEmpty(User))
        {
            if (settings.ASS == false && settings.TextASSAColor != null)
                return $@"{{\c{settings.TextASSAColor}}}{Body}";
            else
                return Body;
        }

        if (settings.ASS)
        {
            string timestampStr = null;
            if (settings.ShowTimestamps)
                timestampStr = $@"{{\fs{settings.TimestampFontSize}}}{ToTimestamp(Timestamp)}{{\rfs}} ";

            if (settings.RegularSubtitles)
                return $"{timestampStr}{(IsModerator && settings.IsUsingAssaTags ? @"{\u1}" : string.Empty)}{(UserColor != null ? $@"{{{(settings.BoldText ? null : @"\b1")}\c{UserColor}}}" : string.Empty)}{User}{(UserColor != null ? $@"{{\c{(settings.BoldText ? null : @"\b0")}}}" : string.Empty)}{(IsModerator && settings.IsUsingAssaTags ? @"{\u0}" : string.Empty)}:{(IsBrailleArt ? @"\N" : " ")}{Body}";
            else
                return $"{timestampStr}{(IsModerator ? @"{\u1}" : string.Empty)}{(UserColor != null ? $@"{{{(settings.BoldText ? null : @"\b1")}\c{UserColor}}}" : string.Empty)}{User}{(UserColor != null ? $@"{{\c{(settings.BoldText ? null : @"\b0")}}}" : string.Empty)}{(IsModerator ? @"{\u0}" : string.Empty)}:{(IsBrailleArt ? @"\N" : " ")}{Body}";
        }
        else
        {
            string timestampStr = null;
            if (settings.ShowTimestamps)
            {
                string fontSizeStr = null;
                if (settings.SubtitlesFontSize != SubtitlesFontSize.None)
                    fontSizeStr = $@"\fs{(int)settings.SubtitlesFontSize}";

                timestampStr = $@"{{\fs{settings.TimestampFontSize}{(settings.TextASSAColor != null ? $@"\c{settings.TextASSAColor}" : string.Empty)}}}{ToTimestamp(Timestamp)}{{{(settings.TextASSAColor != null ? @"\c" : string.Empty)}\rfs\fn{Subtitle.FONT_NAME}{fontSizeStr}{(settings.BoldText ? Subtitle.BOLD_FONT_RESET : Subtitle.FONT_RESET)}}} ";
            }

            return $"{timestampStr}{(IsModerator && settings.IsUsingAssaTags ? @"{\u1}" : string.Empty)}{(UserColor != null ? $@"{{{(settings.BoldText ? null : @"\b1")}\c{UserColor}}}" : (settings.TextASSAColor != null ? $@"{{{(settings.BoldText ? null : @"\b1")}\c{settings.TextASSAColor}}}" : string.Empty))}{User}{(UserColor != null || settings.TextASSAColor != null ? $@"{{\c{(settings.BoldText ? null : @"\b0")}}}" : string.Empty)}{(IsModerator && settings.IsUsingAssaTags ? @"{\u0}" : string.Empty)}{(settings.TextASSAColor != null ? $@"{{\c{settings.TextASSAColor}}}" : string.Empty)}:{(IsBrailleArt ? (settings.IsUsingAssaTags ? @"\N" : Environment.NewLine) : " ")}{Body}";
        }
    }

    internal string ChatLogTimestampAndUser(bool showTimestamps)
    {
        return $"{(showTimestamps ? $"{ToChatLogTimestamp(Timestamp)} " : string.Empty)}{(IsModerator ? "[M] " : string.Empty)}{User}";
    }
}
