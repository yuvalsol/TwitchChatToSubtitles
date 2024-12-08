using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwitchChatToSubtitles.Library;

public partial class TwitchSubtitles(TwitchSubtitlesSettings settings)
{
    private readonly TwitchSubtitlesSettings settings = settings ?? new();

    public EventHandler Start;
    public EventHandler StartLoadingJsonFile;
    public EventHandler<FinishLoadingJsonFileEventArgs> FinishLoadingJsonFile;
    public EventHandler StartWritingPreparations;
    public EventHandler FinishWritingPreparations;
    public EventHandler StartWritingSubtitles;
    public EventHandler<ProgressEventArgs> ProgressAsync;
    public EventHandler<ProgressEventArgs> FinishWritingSubtitles;
    public EventHandler<FinishEventArgs> Finish;

    private const int FLUSH_SUBTITLES_LIMIT = 1000;

    public void WriteTwitchSubtitles(string jsonFile)
    {
        if (string.IsNullOrEmpty(jsonFile))
            throw new ArgumentException("JSON file not specified.");

        if (string.Compare(Path.GetExtension(jsonFile), ".json", true) != 0)
            throw new ArgumentException("Not a JSON file '" + jsonFile + "'.");

        if (File.Exists(jsonFile) == false)
            throw new FileNotFoundException("Could not find file '" + jsonFile + "'.");

        if ((settings.RegularSubtitles || settings.RollingChatSubtitles || settings.StaticChatSubtitles) == false)
            throw new ArgumentException("Subtitles type (RegularSubtitles, RollingChatSubtitles, StaticChatSubtitles) is not selected.");

        Start.Raise(this, () => EventArgs.Empty);

        string srtFile = Path.Combine(
            Path.GetDirectoryName(jsonFile),
            Path.GetFileNameWithoutExtension(jsonFile) + ".srt"
        );

        StartLoadingJsonFile.Raise(this, () => EventArgs.Empty);
        JToken root = LoadJsonFile(jsonFile);
        FinishLoadingJsonFile.Raise(this, () => new FinishLoadingJsonFileEventArgs(jsonFile));

        Regex[] regexEmbeddedEmoticons = null;
        Dictionary<string, UserColor> userColors = null;
        if (settings.RemoveEmoticonNames || settings.ColorUserNames)
        {
            StartWritingPreparations.Raise(this, () => EventArgs.Empty);
            regexEmbeddedEmoticons = GetEmbeddedEmoticons(root);
            userColors = GetUserColors(root);
            FinishWritingPreparations.Raise(this, () => EventArgs.Empty);
        }

        StartWritingSubtitles.Raise(this, () => EventArgs.Empty);

        {
            using var srtStream = File.Open(srtFile, FileMode.Create);
            using var writer = new StreamWriter(srtStream, Encoding.UTF8);

            if (settings.RegularSubtitles)
                WriteRegularSubtitles(root, regexEmbeddedEmoticons, userColors, writer);
            else if (settings.RollingChatSubtitles)
                WriteRollingChatSubtitles(root, regexEmbeddedEmoticons, userColors, writer);
            else if (settings.StaticChatSubtitles)
                WriteStaticChatSubtitles(root, regexEmbeddedEmoticons, userColors, writer);
        }

        Finish.Raise(this, () => new FinishEventArgs(srtFile));
    }

    #region Json

    private static JToken LoadJsonFile(string jsonFile)
    {
        using var jsonStream = new StreamReader(jsonFile);
        using var reader = new JsonTextReader(jsonStream);

        try
        {
            return JToken.Load(reader);
        }
        catch (Exception ex)
        {
            throw new Exception("Could not load JSON file '" + jsonFile + "'.", ex);
        }
    }

    private void ReadJsonComment(
        JToken comment,
        Regex[] regexEmbeddedEmoticons,
        Dictionary<string, UserColor> userColors,
        out TimeSpan timestamp,
        out string user,
        out string body,
        out bool isBrailleArt,
        out Color color)
    {
        int content_offset_seconds = comment.SelectToken("content_offset_seconds").Value<int>();
        timestamp = TimeSpan.FromSeconds(content_offset_seconds);

        JToken commenter = comment.SelectToken("commenter");
        user = commenter.SelectToken("display_name").Value<string>();

        JToken message = comment.SelectToken("message");
        body = GetMessageBody(
            message,
            user,
            regexEmbeddedEmoticons,
            userColors,
            timestamp,
            out isBrailleArt
        );

        color = null;
        if (settings.ColorUserNames)
        {
            if (userColors.TryGetValue(user, out var userColor))
                color = userColor.Color;
        }
    }

    #endregion

    #region Regular Subtitles

    private void WriteRegularSubtitles(JToken root, Regex[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, StreamWriter writer)
    {
        TimeSpan subtitleShowDuration = TimeSpan.FromSeconds(settings.SubtitleShowDuration);
        TimeSpan timeOffset = TimeSpan.FromSeconds(settings.TimeOffset);

        int subsCounter = 1;
        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        JToken comments = root.SelectToken("comments");
        if (comments.TryGetNonEnumeratedCount(out int totalMessages) == false)
            totalMessages = comments.Count();

        bool isWriteSubtitles = false;
        foreach (JToken comment in comments)
        {
            ReadJsonComment(
                comment,
                regexEmbeddedEmoticons,
                userColors,
                out TimeSpan timestamp,
                out string user,
                out string body,
                out bool isBrailleArt,
                out Color color
            );

            if (string.IsNullOrWhiteSpace(body))
            {
                discardedMessagesCount++;
                continue;
            }

            messagesCount++;

            TimeSpan showTime = timestamp + timeOffset;
            TimeSpan hideTime = showTime + subtitleShowDuration;

            var message = new ChatMessage(timestamp, user, color, body, isBrailleArt);

            var sub = Subtitles.FirstOrDefault(s => s.ShowTime == showTime);
            if (sub != null)
            {
                sub.AddMessage(message);
            }
            else
            {
                Subtitles.Add(new Subtitle(showTime, hideTime, message));
                subtitlesCount++;
            }

            if (Subtitles.Count >= FLUSH_SUBTITLES_LIMIT)
            {
                if (isWriteSubtitles)
                {
                    if (sub == null)
                    {
                        var lastSub = Subtitles[^1];
                        bool isLastSubOverlap = Subtitles.Where(s => s != lastSub).Any(s => s.IsOverlapWith(lastSub));
                        if (isLastSubOverlap == false)
                        {
                            Subtitles.RemoveAt(Subtitles.Count - 1);

                            SortRegularSubtitles(Subtitles);
                            OverlapRegularSubtitles(Subtitles, ref subtitlesCount);

                            foreach (var subtitle in Subtitles)
                            {
                                writer.WriteLine(subsCounter++);
                                writer.WriteLine(subtitle.ToString(settings));
                            }
                            writer.Flush();

                            Subtitles.Clear();
                            Subtitles.Add(lastSub);

                            isWriteSubtitles = false;
                        }
                    }
                }
                else
                {
                    isWriteSubtitles = true;
                }
            }

            ProgressAsync.RaiseAsync(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }

        if (Subtitles.Count > 0)
        {
            SortRegularSubtitles(Subtitles);
            OverlapRegularSubtitles(Subtitles, ref subtitlesCount);

            foreach (var subtitle in Subtitles)
            {
                writer.WriteLine(subsCounter++);
                writer.WriteLine(subtitle.ToString(settings));
            }
            writer.Flush();
        }

        FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
    }

    private static void OverlapRegularSubtitles(List<Subtitle> Subtitles, ref int subtitlesCount)
    {
        for (int i = 0; i < Subtitles.Count; i++)
        {
            int fromIndex = i;
            int toIndex = i;

            var subtitle1 = Subtitles[i];

            for (int j = i + 1; j < Subtitles.Count; j++)
            {
                var subtitle2 = Subtitles[j];

                bool isOverlap = Enumerable.Range(i, j - i /* [i, j-1] */).Any(n => Subtitles[n].IsOverlapWith(subtitle2));
                if (isOverlap)
                    toIndex = j;
                else
                    break;
            }

            if (fromIndex == toIndex)
                continue;

            var oldSubs = Enumerable.Range(fromIndex, toIndex - fromIndex + 1 /* [fromIndex, toIndex] */)
                .Select(n => Subtitles[n])
                .ToArray();

            var times = oldSubs.Select(s => s.ShowTime)
                .Union(oldSubs.Select(s => s.HideTime))
                .OrderBy(t => t)
                .Select((t, tIndex) => (t, tIndex))
                .ToArray();

            Subtitle[] newSubs = times.Join(
                times.Skip(1),
                t1 => t1.tIndex + 1,
                t2 => t2.tIndex,
                (t1, t2) => new Subtitle(t1.t, t2.t)
            ).ToArray();

            foreach (var oldSub in oldSubs)
            {
                foreach (var newSub in newSubs)
                {
                    if (newSub.IsOverlapWith(oldSub))
                        newSub.AddMessages(oldSub);
                }
            }

            Subtitles.RemoveRange(fromIndex, toIndex - fromIndex + 1);
            Subtitles.InsertRange(fromIndex, newSubs);
            subtitlesCount += newSubs.Length - (toIndex - fromIndex + 1);

            i = toIndex;
        }
    }

    private static void SortRegularSubtitles(List<Subtitle> Subtitles)
    {
        Subtitles.Sort((x, y) => x.ShowTime.CompareTo(y.ShowTime));
    }

    #endregion

    #region Calculate Chat PosYs

    private const int TOP_POS_Y = 10;
    private const int BOTTOM_POS_Y = 270;

    private static void CalculateChatPosYs(int fontSize, SubtitlesLocation subtitlesLocation, out int topPosY, out int bottomPosY, out int posYCount)
    {
        bottomPosY = BOTTOM_POS_Y;

        topPosY = bottomPosY;
        while (topPosY > TOP_POS_Y)
            topPosY -= fontSize;

        posYCount = (bottomPosY - topPosY + fontSize) / fontSize;

        if (subtitlesLocation.IsHalf())
        {
            posYCount = ((posYCount - (posYCount % 2)) / 2) + (posYCount % 2);

            if (subtitlesLocation.IsTopHalf())
                bottomPosY = topPosY + ((posYCount - 1) * fontSize);
            else if (subtitlesLocation.IsBottomHalf())
                topPosY = bottomPosY - ((posYCount - 1) * fontSize);
        }
        else if (subtitlesLocation.IsTwoThirds())
        {
            posYCount = ((posYCount - (posYCount % 3)) * 2 / 3) + (posYCount % 3);

            if (subtitlesLocation.IsTopTwoThirds())
                bottomPosY = topPosY + ((posYCount - 1) * fontSize);
            else if (subtitlesLocation.IsBottomTwoThirds())
                topPosY = bottomPosY - ((posYCount - 1) * fontSize);
        }
    }

    #endregion

    #region Rolling Chat Subtitles

    private void WriteRollingChatSubtitles(JToken root, Regex[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, StreamWriter writer)
    {
        TimeSpan timeOffset = TimeSpan.FromSeconds(settings.TimeOffset);

        if (settings.SubtitlesSpeed == SubtitlesSpeed.None)
            settings.SubtitlesSpeed = SubtitlesSpeed.Regular;
        TimeSpan timeStep = TimeSpan.FromSeconds(1.0 / (int)settings.SubtitlesSpeed);

        if (settings.SubtitlesFontSize == SubtitlesFontSize.None)
            settings.SubtitlesFontSize = SubtitlesFontSize.Regular;
        int fontSize = (int)settings.SubtitlesFontSize;

        if (settings.SubtitlesLocation == SubtitlesLocation.None)
            settings.SubtitlesLocation = SubtitlesLocation.Left;

        CalculateChatPosYs(fontSize, settings.SubtitlesLocation, out int topPosY, out int bottomPosY, out int posYCount);

        int subsCounter = 1;
        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        JToken comments = root.SelectToken("comments");
        if (comments.TryGetNonEnumeratedCount(out int totalMessages) == false)
            totalMessages = comments.Count();

        TimeSpan nextTimeSlot = TimeSpan.MinValue;
        bool isWriteSubtitles = false;
        TimeSpan maxShowTime = TimeSpan.MinValue;
        foreach (JToken comment in comments)
        {
            ReadJsonComment(
                comment,
                regexEmbeddedEmoticons,
                userColors,
                out TimeSpan timestamp,
                out string user,
                out string body,
                out bool isBrailleArt,
                out Color color
            );

            if (string.IsNullOrWhiteSpace(body))
            {
                discardedMessagesCount++;
                continue;
            }

            messagesCount++;

            var message = new ChatMessage(timestamp, user, color, body, isBrailleArt);
            int linesCount = message.LinesCount;

            TimeSpan showTime = timestamp + timeOffset;
            if (showTime < nextTimeSlot)
                showTime = nextTimeSlot;
            nextTimeSlot = showTime + TimeSpan.FromSeconds(linesCount);

            if (isWriteSubtitles)
            {
                if (maxShowTime < showTime)
                {
                    TimeSpan projectedLastShowTime = showTime;
                    for (int i = 0; i < posYCount - 1 + linesCount - 1; i++)
                        projectedLastShowTime += timeStep;

                    if (maxShowTime < projectedLastShowTime)
                    {
                        SortRollingChatSubtitles(Subtitles);

                        int count = 0;
                        var sub = Subtitles.FirstOrDefault(s => s.ShowTime > maxShowTime);
                        if (sub != null)
                            count = Subtitles.IndexOf(sub);
                        else
                            count = Subtitles.Count;

                        foreach (var subtitle in Subtitles.Take(count))
                        {
                            writer.WriteLine(subsCounter++);
                            writer.WriteLine(subtitle.ToString(settings));
                        }
                        writer.Flush();

                        Subtitles.RemoveRange(0, count);

                        isWriteSubtitles = false;
                        maxShowTime = TimeSpan.MinValue;
                    }
                }
            }

            for (int posY = bottomPosY; posY >= topPosY; posY -= fontSize)
            {
                TimeSpan hideTime = showTime + timeStep;
                var subtitle = new Subtitle(showTime, hideTime, posY, message);
                Subtitles.Add(subtitle);
                subtitlesCount++;

                if (posY == topPosY && linesCount > 1)
                {
                    int countBefore = Subtitles.Count;
                    Subtitles.AddRange(ShaveLineFromTheTop(subtitle, timeStep));
                    subtitlesCount += Subtitles.Count - countBefore;
                }

                showTime = hideTime;
            }

            if (Subtitles.Count >= FLUSH_SUBTITLES_LIMIT)
            {
                if (isWriteSubtitles == false)
                {
                    maxShowTime = Subtitles.Max(subtitle => subtitle.ShowTime);
                    isWriteSubtitles = true;
                }
            }

            ProgressAsync.RaiseAsync(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }

        if (Subtitles.Count > 0)
        {
            SortRollingChatSubtitles(Subtitles);

            foreach (var subtitle in Subtitles)
            {
                writer.WriteLine(subsCounter++);
                writer.WriteLine(subtitle.ToString(settings));
            }
            writer.Flush();
        }

        FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
    }

    private static IEnumerable<Subtitle> ShaveLineFromTheTop(Subtitle subtitle, TimeSpan timeStep)
    {
        var currentSubtitle = subtitle;
        while (currentSubtitle != null)
        {
            TimeSpan showTime = currentSubtitle.ShowTime + timeStep;
            TimeSpan hideTime = showTime + timeStep;
            currentSubtitle = currentSubtitle.ShaveLineFromTheTop(showTime, hideTime);
            if (currentSubtitle != null)
                yield return currentSubtitle;
        }
    }

    private static void SortRollingChatSubtitles(List<Subtitle> Subtitles)
    {
        Subtitles.Sort((x, y) =>
        {
            int val = x.ShowTime.CompareTo(y.ShowTime);
            if (val != 0)
                return val;
            return y.PosY.CompareTo(x.PosY);
        });
    }

    #endregion

    #region Static Chat Subtitles

    private void WriteStaticChatSubtitles(JToken root, Regex[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, StreamWriter writer)
    {
        TimeSpan timeOffset = TimeSpan.FromSeconds(settings.TimeOffset);

        if (settings.SubtitlesFontSize == SubtitlesFontSize.None)
            settings.SubtitlesFontSize = SubtitlesFontSize.Regular;
        int fontSize = (int)settings.SubtitlesFontSize;

        if (settings.SubtitlesLocation == SubtitlesLocation.None)
            settings.SubtitlesLocation = SubtitlesLocation.Left;

        CalculateChatPosYs(fontSize, settings.SubtitlesLocation, out int topPosY, out _, out int posYCount);

        // 99:59:59,999
        TimeSpan hideTimeMaxValue = TimeSpan.FromMilliseconds(
            (99 * 60 * 60 * 1000) +
            (59 * 60 * 1000) +
            (59 * 1000) +
            999
        );

        int subsCounter = 1;
        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        JToken comments = root.SelectToken("comments");
        if (comments.TryGetNonEnumeratedCount(out int totalMessages) == false)
            totalMessages = comments.Count();

        Subtitle prevSubtitle = null;
        foreach (JToken comment in comments)
        {
            ReadJsonComment(
                comment,
                regexEmbeddedEmoticons,
                userColors,
                out TimeSpan timestamp,
                out string user,
                out string body,
                out bool isBrailleArt,
                out Color color
            );

            if (string.IsNullOrWhiteSpace(body))
            {
                discardedMessagesCount++;
                continue;
            }

            messagesCount++;

            var message = new ChatMessage(timestamp, user, color, body, isBrailleArt);

            if (prevSubtitle == null)
            {
                TimeSpan showTime = timestamp + timeOffset;
                var subtitle = new Subtitle(showTime, hideTimeMaxValue, topPosY, message);

                Subtitles.Add(subtitle);
                subtitlesCount++;

                prevSubtitle = subtitle;
            }
            else
            {
                TimeSpan showTime = timestamp + timeOffset;
                var subtitle = new Subtitle(showTime, hideTimeMaxValue, topPosY);
                subtitle.AddMessages(prevSubtitle);
                subtitle.AddMessage(message);

                int linesCount = subtitle.LinesCount;
                if (linesCount > posYCount)
                {
                    int shaveCount = linesCount - posYCount;
                    subtitle = subtitle.ShaveLinesFromTheTop(shaveCount);
                }

                subtitle.SetSubtitlesConsecutively(prevSubtitle);

                Subtitles.Add(subtitle);
                subtitlesCount++;

                prevSubtitle = subtitle;
            }

            if (Subtitles.Count >= FLUSH_SUBTITLES_LIMIT)
            {
                int lastIndex = Subtitles.Count - 1;
                var lastSub = Subtitles[lastIndex];
                Subtitles.RemoveAt(lastIndex);

                foreach (var subtitle in Subtitles)
                {
                    writer.WriteLine(subsCounter++);
                    writer.WriteLine(subtitle.ToString(settings));
                }
                writer.Flush();

                Subtitles.Clear();
                Subtitles.Add(lastSub);
            }

            ProgressAsync.RaiseAsync(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }

        if (Subtitles.Count > 0)
        {
            foreach (var subtitle in Subtitles)
            {
                writer.WriteLine(subsCounter++);
                writer.WriteLine(subtitle.ToString(settings));
            }
            writer.Flush();
        }

        FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
    }

    #endregion

    #region Embedded Emoticons

    private Regex[] GetEmbeddedEmoticons(JToken root)
    {
        if (settings.RemoveEmoticonNames == false)
            return null;

        IEnumerable<string> thirdPartyEmoticons = GetThirdPartyEmoticons(root);
        IEnumerable<string> firstPartyEmoticons = GetFirstPartyEmoticons(root);

        IEnumerable<string> embeddedEmoticons = [];

        if (thirdPartyEmoticons != null && firstPartyEmoticons != null)
        {
            embeddedEmoticons = thirdPartyEmoticons.Concat(firstPartyEmoticons)
                .Distinct()
                .OrderBy(name => name);
        }
        else if (thirdPartyEmoticons != null && firstPartyEmoticons == null)
        {
            embeddedEmoticons = thirdPartyEmoticons;
        }
        else if (thirdPartyEmoticons == null && firstPartyEmoticons != null)
        {
            embeddedEmoticons = firstPartyEmoticons;
        }

        return embeddedEmoticons
            .Select(emoticon => new Regex(@"\b" + emoticon + @"\b", RegexOptions.Compiled))
            .ToArray();
    }

    private static IEnumerable<string> GetThirdPartyEmoticons(JToken root)
    {
        try
        {
            return root.SelectTokens("embeddedData.thirdParty[*].name")
                .Cast<JValue>()
                .Select(x => x.Value<string>())
                .Where(name => string.IsNullOrEmpty(name) == false)
                .Distinct()
                .OrderBy(name => name);
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> GetFirstPartyEmoticons(JToken root)
    {
        try
        {
            return root.SelectTokens("embeddedData.thirdParty[*].name")
                .Cast<JValue>()
                .Select(x => x.Value<string>())
                .Where(name => string.IsNullOrEmpty(name) == false)
                .Distinct()
                .OrderBy(name => name);
        }
        catch
        {
            return [];
        }
    }

    #endregion

    #region User Colors

    private const string USER_DEFAULT_COLOR = "#9146FF";

    private Dictionary<string, UserColor> GetUserColors(JToken root)
    {
        if (settings.ColorUserNames == false)
            return null;

        var userColors = new Dictionary<string, UserColor>();

        foreach (JToken comment in root.SelectToken("comments"))
        {
            JToken commenter = comment.SelectToken("commenter");
            string user = commenter.SelectToken("display_name").Value<string>();

            JToken message = comment.SelectToken("message");
            string userColor = message.SelectToken("user_color").Value<string>();

            if (string.IsNullOrEmpty(userColor))
                userColor = USER_DEFAULT_COLOR;

            if (userColors.ContainsKey(user) == false)
                userColors.Add(user, new UserColor(user, new Color(userColor)));
        }

        // force the regexes to compile
        // this will account to the writing preparations time
        foreach (var item in userColors)
        {
            item.Value.Search1.Search.Replace("Lorem ipsum dolor sit amet, consectetur adipiscing elit.", item.Value.Search1.Replace);
            item.Value.Search2.Search.Replace("Lorem ipsum dolor sit amet, consectetur adipiscing elit.", item.Value.Search2.Replace);
        }

        return userColors;
    }

    #endregion

    #region Message Body

    // https://en.wikipedia.org/wiki/Braille_ASCII
    [GeneratedRegex(@"[⠁⠂⠃⠄⠅⠆⠇⠈⠉⠊⠋⠌⠍⠎⠏⠐⠑⠒⠓⠔⠕⠖⠗⠘⠙⠚⠛⠜⠝⠞⠟⠠⠡⠢⠣⠤⠥⠦⠧⠨⠩⠪⠫⠬⠭⠮⠯⠰⠱⠲⠳⠴⠵⠶⠷⠸⠹⠺⠻⠼⠽⠾⠿]")]
    private static partial Regex RegexIsBrailleArt();

    private const char BRAILLE_SPACE = '⠀'; // \u2800

    [GeneratedRegex(@"(?<Prefix>\s*)[A-Za-z0-9]+(?<Suffix>\s*)")]
    private static partial Regex RegexEmoticonName();

    [GeneratedRegex(@"\s{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex RegexDoubleSpaces();

    private string GetMessageBody(
        JToken message,
        string user,
        Regex[] regexEmbeddedEmoticons,
        Dictionary<string, UserColor> userColors,
        TimeSpan timestamp,
        out bool isBrailleArt)
    {
        var body = new StringBuilder();

        if (settings.RemoveEmoticonNames)
        {
            foreach (var fragment in message.SelectToken("fragments"))
            {
                var emoticon = fragment.SelectToken("emoticon");
                if (emoticon.HasValues == false)
                {
                    var text = fragment.SelectToken("text");
                    body.Append(text);
                }
            }
        }
        else
        {
            body.Append(message.SelectToken("body").Value<string>());
        }

        isBrailleArt = RegexIsBrailleArt().IsMatch(body.ToString());

        if (isBrailleArt)
        {
            if (settings.RemoveEmoticonNames)
            {
                body.Clear();
                body.Append(message.SelectToken("body").Value<string>());

                foreach (var match in RegexEmoticonName().Matches(body.ToString()).Cast<Match>().OrderByDescending(m => m.Index))
                {
                    int length =
                        match.Groups["Prefix"].Length +
                        1 + // emoticon name
                        match.Groups["Suffix"].Length;

                    char leftChar = default;
                    if (0 <= match.Index - 1 && match.Index - 1 < body.Length)
                        leftChar = body[match.Index - 1];

                    char rightChar = default;
                    if (0 <= match.Index + match.Length && match.Index + match.Length < body.Length)
                        rightChar = body[match.Index + match.Length];

                    char charReplacement = BRAILLE_SPACE;
                    if (leftChar == rightChar && leftChar != default)
                        charReplacement = leftChar;

                    body.Remove(match.Index, match.Length);
                    var replacement = new string(charReplacement, length);
                    body.Insert(match.Index, replacement);
                }
            }
        }
        else
        {
            if (settings.RemoveEmoticonNames)
            {
                foreach (var regex in regexEmbeddedEmoticons)
                    regex.Replace(body, " ");
            }

            RegexDoubleSpaces().Replace(body, " ");
        }

        if (isBrailleArt)
        {
            SplitMessageBodyForBrailleArt(body);
        }
        else
        {
            if (settings.RollingChatSubtitles || settings.StaticChatSubtitles)
                SplitMessageBody(body, user, timestamp);
        }

        if (settings.ColorUserNames)
        {
            foreach (var item in userColors)
            {
                item.Value.Search1.Search.Replace(body, item.Value.Search1.Replace);
                item.Value.Search2.Search.Replace(body, item.Value.Search2.Replace);
            }
        }

        return body.ToString();
    }

    private const int SPLIT_ON_N_CHARS = 45;

    private void SplitMessageBody(StringBuilder body, string user, TimeSpan timestamp)
    {
        int startIndex = 0;
        while (startIndex < body.Length)
        {
            int endIndex = startIndex + SPLIT_ON_N_CHARS - 1;

            if (startIndex == 0)
            {
                if (settings.ShowTimestamps)
                {
                    // timestamp user: body
                    endIndex -= ChatMessage.ToTimestamp(timestamp).Length + 1;
                }

                if (string.IsNullOrEmpty(user) == false)
                {
                    // user: body
                    endIndex -= user.Length + 2;
                }
            }

            if (endIndex >= body.Length)
                break;

            bool found = false;
            for (int i = endIndex; i >= startIndex; i--)
            {
                if (body[i] == ' ')
                {
                    body[i] = '\n';
                    startIndex = i + 1;
                    found = true;
                    break;
                }
            }

            if (found)
                continue;

            found = false;
            for (int i = endIndex; i >= startIndex; i--)
            {
                // break long url
                if (body[i] == '/')
                {
                    body.Insert(i + 1, '\n');
                    startIndex = i + 2;
                    found = true;
                    break;
                }
            }

            if (found)
                continue;

            if (endIndex + 1 < body.Length)
            {
                body.Insert(endIndex + 1, '\n');
                startIndex = endIndex + 2;
            }
        }

        if (body.Length > 0 && body[^1] == '\n')
            body.Remove(body.Length - 1, 1);

        body.Replace("\n", "\\N");
    }

    private static void SplitMessageBodyForBrailleArt(StringBuilder body)
    {
        int charsPerLine;
        int splitCount;

        int indexFirstSpace = body.ToString().IndexOf(' ');
        if (indexFirstSpace < 10)
            indexFirstSpace = -1;

        if (indexFirstSpace != -1)
        {
            charsPerLine = indexFirstSpace + 1;
            splitCount = body.Length / charsPerLine;
        }
        else
        {
            var bestMeasurement = GetBestFitBrailleMeasurement(body.Length);

            if (bestMeasurement.charsMissingInLastLine > 0)
            {
                for (int i = 0; i < bestMeasurement.charsMissingInLastLine; i++)
                    body.Append(' ');

                bestMeasurement = GetBestFitBrailleMeasurement(body.Length);
            }

            charsPerLine = bestMeasurement.charsPerLine;
            splitCount = bestMeasurement.splitCount;
        }

        var indexes = Enumerable.Range(1, splitCount).Select(n => (n * charsPerLine) + (2 * (n - 1)));
        foreach (var index in indexes)
            body.Insert(index, "\\N");
    }

    private const int CHARS_PER_BRAILLE_LINE = 30;

    private static (int charsPerLine, int splitCount, int charsInLastLine, int charsMissingInLastLine) GetBestFitBrailleMeasurement(int bodyLength)
    {
        var measurements = new List<(int charsPerLine, int splitCount, int charsInLastLine, int charsMissingInLastLine)>();

        for (int charsPerLine = CHARS_PER_BRAILLE_LINE - 5; charsPerLine <= CHARS_PER_BRAILLE_LINE + 5; charsPerLine++)
        {
            int splitCount = Math.DivRem(bodyLength, charsPerLine, out int charsInLastLine);
            int charsMissingInLastLine = (charsPerLine - charsInLastLine) % charsPerLine;
            measurements.Add((charsPerLine, splitCount, charsInLastLine, charsMissingInLastLine));
        }

        measurements.Sort((x, y) =>
        {
            // charsPerLine = 30
            // charsInLastLine =  0 -> charsMissingInLastLine = (30 -  0) % 30 = 30 % 30 =  0
            // charsInLastLine =  1 -> charsMissingInLastLine = (30 -  1) % 30 = 29 % 30 = 29
            // charsInLastLine = 29 -> charsMissingInLastLine = (30 - 29) % 30 =  1 % 30 =  1
            int val = x.charsMissingInLastLine.CompareTo(y.charsMissingInLastLine);
            if (val != 0)
                return val;

            return x.charsPerLine.CompareTo(y.charsPerLine);
        });

        return measurements[0];
    }

    #endregion
}
