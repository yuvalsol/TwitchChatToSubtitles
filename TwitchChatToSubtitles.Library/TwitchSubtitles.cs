using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwitchChatToSubtitles.Library;

public partial class TwitchSubtitles(TwitchSubtitlesSettings settings)
{
    private readonly TwitchSubtitlesSettings settings = settings ?? new();

    public EventHandler Start;
    public EventHandler<StartLoadingJsonFileEventArgs> StartLoadingJsonFile;
    public EventHandler<FinishLoadingJsonFileEventArgs> FinishLoadingJsonFile;
    public EventHandler<StartWritingPreparationsEventArgs> StartWritingPreparations;
    public EventHandler<FinishWritingPreparationsEventArgs> FinishWritingPreparations;
    public EventHandler<StartWritingSubtitlesEventArgs> StartWritingSubtitles;
    public EventHandler<ProgressEventArgs> ProgressAsync;
    public EventHandler<ProgressEventArgs> FinishWritingSubtitles;
    public EventHandler<FinishEventArgs> Finish;
    public EventHandler<TracepointEventArgs> Tracepoint;

    private const int COMMENTS_CHUNK_SIZE = 100;
    private const int EMBEDDED_EMOTICONS_CHUNK_SIZE = 500;
    private const int USER_COLORS_CHUNK_SIZE = 500;
    private const int FLUSH_SUBTITLES_COUNT = 1000;

    public void WriteTwitchSubtitles(string jsonFile)
    {
#if DEBUG
        SortMessageIndexesToKeep();
#endif

        long startTime = Stopwatch.GetTimestamp();

        if (string.IsNullOrEmpty(jsonFile))
            throw new ArgumentException("JSON file not specified.");

        if (string.Compare(Path.GetExtension(jsonFile), ".json", true) != 0)
            throw new ArgumentException("Not a JSON file '" + jsonFile + "'.");

        if (File.Exists(jsonFile) == false)
            throw new FileNotFoundException("Could not find file '" + jsonFile + "'.");

        if (settings.IsAnySubtitlesTypeSelected == false)
            throw new ArgumentException("Subtitles type was not selected.");

        Exception error = null;

        Start.Raise(this, () => EventArgs.Empty);

        string fileName = Path.GetFileNameWithoutExtension(jsonFile);

        string srtFile = Path.Combine(
            Path.GetDirectoryName(jsonFile),
             fileName + (settings.ChatTextFile ? ".txt" : (settings.ASS ? ".ass" : ".srt"))
        );

        StartLoadingJsonFile.Raise(this, () => new StartLoadingJsonFileEventArgs(jsonFile));
        JToken root = LoadJsonFile(jsonFile, ref error);
        FinishLoadingJsonFile.Raise(this, () => new FinishLoadingJsonFileEventArgs(jsonFile, error));

        if (error != null)
        {
            Finish.Raise(this, () => new FinishEventArgs(srtFile, TimeSpan.Zero, error));
            return;
        }

        if (settings.ChatTextFile)
            settings.ColorUserNames = false;

        (string emoticon, Regex regex)[] regexEmbeddedEmoticons = null;
        Dictionary<string, UserColor> userColors = null;

        if (settings.RemoveEmoticonNames || settings.ColorUserNames)
        {
            StartWritingPreparations.Raise(this, () => new StartWritingPreparationsEventArgs(settings.RemoveEmoticonNames, settings.ColorUserNames));

            WritingPreparations(settings.RemoveEmoticonNames, settings.ColorUserNames, root, ref regexEmbeddedEmoticons, ref userColors, ref error);

            FinishWritingPreparations.Raise(this, () => new FinishWritingPreparationsEventArgs(settings.RemoveEmoticonNames, settings.ColorUserNames, error));

            if (error != null)
            {
                Finish.Raise(this, () => new FinishEventArgs(srtFile, TimeSpan.Zero, error));
                return;
            }
        }

        StartWritingSubtitles.Raise(this, () => new StartWritingSubtitlesEventArgs(srtFile));

        {
            using var srtStream = File.Open(srtFile, FileMode.Create);
            using var writer = new StreamWriter(srtStream, Encoding.UTF8);

            if (settings.RegularSubtitles)
                WriteRegularSubtitles(root, regexEmbeddedEmoticons, userColors, fileName, writer, ref error);
            else if (settings.RollingChatSubtitles)
                WriteRollingChatSubtitles(root, regexEmbeddedEmoticons, userColors, fileName, writer, ref error);
            else if (settings.StaticChatSubtitles)
                WriteStaticChatSubtitles(root, regexEmbeddedEmoticons, userColors, fileName, writer, ref error);
            else if (settings.ChatTextFile)
                WriteChatTextFile(root, regexEmbeddedEmoticons, userColors, writer, ref error);
        }

        TimeSpan processTime = Stopwatch.GetElapsedTime(startTime);

        Finish.Raise(this, () => new FinishEventArgs(srtFile, processTime, error));
    }

    #region Load Json File

    private static JToken LoadJsonFile(string jsonFile, ref Exception error)
    {
        try
        {
            using var jsonStream = new StreamReader(jsonFile);
            using var reader = new JsonTextReader(jsonStream);

            JToken root = JToken.Load(reader);

            if (IsTwitchChatJsonFile(root))
            {
                return root;
            }
            else
            {
                error = new Exception("Malformed or not a Twitch chat JSON file.");
                return null;
            }
        }
        catch (Exception ex)
        {
            error = ex;
            return null;
        }
    }

    private static bool IsTwitchChatJsonFile(JToken root)
    {
        return root.SelectToken("comments") != null;
    }

    #endregion

    #region Writing Preparations

    private static void WritingPreparations(
        bool removeEmoticonNames,
        bool colorUserNames,
        JToken root,
        ref (string emoticon, Regex regex)[] regexEmbeddedEmoticons,
        ref Dictionary<string, UserColor> userColors,
        ref Exception error)
    {
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        try
        {
            Task<(string emoticon, Regex regex)[]> taskRemoveEmoticonNames = null;
            if (removeEmoticonNames)
                taskRemoveEmoticonNames = Task.Run(() => GetEmbeddedEmoticons(root, ct), ct);

            Task<Dictionary<string, UserColor>> taskColorUserNames = null;
            if (colorUserNames)
                taskColorUserNames = Task.Run(() => GetUserColors(root, ct), ct);

            if (removeEmoticonNames && colorUserNames)
                Task.WaitAll([taskRemoveEmoticonNames, taskColorUserNames], ct);
            else if (removeEmoticonNames)
                taskRemoveEmoticonNames.Wait(ct);
            else if (colorUserNames)
                taskColorUserNames.Wait(ct);

            if (removeEmoticonNames)
                regexEmbeddedEmoticons = taskRemoveEmoticonNames.Result;

            if (colorUserNames)
                userColors = taskColorUserNames.Result;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish gracefully
        }
        catch (AggregateException aex)
        {
            error = aex.InnerException;
        }
        catch (Exception ex)
        {
            cts.Cancel();
            error = ex;
        }
    }

    #endregion

    #region Embedded Emoticons

    // https://en.wikipedia.org/wiki/List_of_emoticons
    [GeneratedRegex(@"^([A-Z])?[-—–―‒_`~!@#$%^&*()=+[\]{};:'""\\|,.<>/?‘’“”0-9]+([A-Z])?$|^\\o/$|^\(\.Y\.\)$|^\(o\)\(o\)$|^DX$|^XD$|^XP$", RegexOptions.IgnoreCase)]
    private static partial Regex RegexTextEmoticon();

    private static (string emoticon, Regex regex)[] GetEmbeddedEmoticons(JToken root, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            var emoticonIds =
                Enumerable.Empty<JToken>()
                .Concat(root.SelectTokens("embeddedData.thirdParty[*].id"))
                .Concat(root.SelectTokens("embeddedData.firstParty[*].id"))
                .Cast<JValue>()
                .Select(node => node.Value<string>())
                .Where(id => string.IsNullOrEmpty(id) == false)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

            var emoticonsInMessages =
                root.SelectTokens("comments[*].message.fragments[*].emoticon.emoticon_id")
                .Cast<JValue>()
                .Select(node => new { node, id = node.Value<string>() })
                .Where(item => string.IsNullOrEmpty(item.id) == false)
                .Where(item => emoticonIds.Contains(item.id))
                .Select(item => item.node)
                .Select(node => node.Parent/*emoticon_id container*/.Parent/*emoticon*/.Parent/*emoticon container*/.Parent/*fragment*/.SelectToken("text").Value<string>())
                .Where(name => string.IsNullOrEmpty(name) == false);

            var emoticonNames =
                Enumerable.Empty<JToken>()
                .Concat(root.SelectTokens("embeddedData.thirdParty[*].name"))
                .Concat(root.SelectTokens("embeddedData.firstParty[*].name"))
                .Cast<JValue>()
                .Select(node => node.Value<string>())
                .Where(name => string.IsNullOrEmpty(name) == false);

            ct.ThrowIfCancellationRequested();

            return [..
                Enumerable.Empty<string>()
                .Concat(emoticonsInMessages)
                .Concat(emoticonNames)
                .Distinct()
                .OrderBy(name => name)
                // keep text emoticons in messages
                .Where(name => RegexTextEmoticon().IsMatch(name) == false)
                .Select(emoticon => (emoticon, new Regex($@"\b{Regex.Escape(emoticon)}\b", RegexOptions.Compiled)))];
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish the task gracefully
            return null;
        }
        catch
        {
            throw;
        }
    }

    #endregion

    #region User Colors

    private const string DEFAULT_USER_COLOR = "#9146FF";

    // (?<=^|\b|\s)
    // @
    // [A-Za-z0-9_]+
    // (?=$|\b|\s)
    [GeneratedRegex(@"(?<=^|\b|\s)@[A-Za-z0-9_]+(?=$|\b|\s)", RegexOptions.IgnoreCase)]
    private static partial Regex RegexUserName();

    private static Dictionary<string, UserColor> GetUserColors(JToken root, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            var userColors = new Dictionary<string, UserColor>(StringComparer.OrdinalIgnoreCase);

            foreach (JToken comment in root.SelectToken("comments"))
            {
                string user = comment.SelectToken("commenter").SelectToken("display_name").Value<string>();

                string userColor = comment.SelectToken("message").SelectToken("user_color").Value<string>();
                if (string.IsNullOrEmpty(userColor))
                    userColor = DEFAULT_USER_COLOR;

                if (userColors.ContainsKey(user) == false)
                    userColors.Add(user, new UserColor(user, new ASSAColor(userColor)));
            }

            var streamerToken = root.SelectToken("streamer");
            if (streamerToken != null)
            {
                string streamer = streamerToken.SelectToken("name").Value<string>();
                if (string.IsNullOrEmpty(streamer) == false)
                {
                    if (streamer.StartsWith('@'))
                        streamer = streamer[1..];

                    if (userColors.ContainsKey(streamer) == false)
                        userColors.Add(streamer, new UserColor(streamer, new ASSAColor(DEFAULT_USER_COLOR)));
                }
            }

            var videoToken = root.SelectToken("video");
            if (videoToken != null)
            {
                var videoTitle = videoToken.SelectToken("title").Value<string>();
                if (string.IsNullOrEmpty(videoTitle) == false)
                {
                    foreach (Match match in RegexUserName().Matches(videoTitle))
                    {
                        string streamer = match.Value[1..];
                        if (userColors.ContainsKey(streamer) == false)
                            userColors.Add(streamer, new UserColor(streamer, new ASSAColor(DEFAULT_USER_COLOR)));
                    }
                }
            }

            ct.ThrowIfCancellationRequested();

            return userColors;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish the task gracefully
            return null;
        }
        catch
        {
            throw;
        }
    }

    #endregion

    #region Get Streamer Name

    public static string GetStreamerName(string jsonFile)
    {
        using var fs = new FileStream(jsonFile, FileMode.Open, FileAccess.Read);
        using var document = JsonDocument.Parse(fs);

        JsonElement root = document.RootElement;

        JsonProperty streamerJsonProperty = root.EnumerateObject().FirstOrDefault(property => property.Name == "streamer");
        if (streamerJsonProperty.Name != "streamer")
            return null;

        JsonProperty nameJsonProperty = streamerJsonProperty.Value.EnumerateObject().FirstOrDefault(property => property.Name == "name");
        if (nameJsonProperty.Name != "name")
            return null;

        string streamer = nameJsonProperty.Value.ToString();

        if (streamer.StartsWith('@'))
            streamer = streamer[1..];

        return streamer;
    }

    #endregion

    #region Regular Subtitles

    private void WriteRegularSubtitles(JToken root, (string emoticon, Regex regex)[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, string fileName, StreamWriter writer, ref Exception error)
    {
        TimeSpan subtitleShowDuration = TimeSpan.FromSeconds(settings.SubtitleShowDuration);

        Task<int> lastWritingTask = null;
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        GetComments(root, out JToken comments, out int totalMessages);

        bool isWriteSubtitles = false;

        if (settings.ASS)
            WriteASSRegularSubtitles(fileName, writer);

        ProcessCommentsInChunks(
            comments,
            regexEmbeddedEmoticons,
            userColors,
            settings,
            ProcessComment,
            PostProcessCommentsChunk,
            PostProcessComments,
            ref error,
            cts,
            ct
        );

        void ProcessComment((ProcessedComment processedComment, ChatMessage message) pccm)
        {
            if (pccm.processedComment.Timestamp < TimeSpan.Zero ||
                string.IsNullOrEmpty(pccm.processedComment.Body))
            {
                discardedMessagesCount++;
                return;
            }

            messagesCount++;

#if DEBUG
            if (IsKeepMessage(messagesCount) == false)
            {
                discardedMessagesCount++;
                return;
            }
#endif

            TimeSpan showTime = pccm.processedComment.Timestamp;

            var sub = Subtitles.FirstOrDefault(s => s.ShowTime == showTime);
            if (sub != null)
            {
                sub.AddMessage(pccm.message);
            }
            else
            {
                TimeSpan hideTime = showTime + subtitleShowDuration;
                Subtitles.Add(new Subtitle(showTime, hideTime, pccm.message));
                subtitlesCount++;
            }

            if (Subtitles.Count >= FLUSH_SUBTITLES_COUNT)
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

                            var copySubtitles = Subtitles.ToArray();
                            WriteMessages(ref lastWritingTask, copySubtitles, settings, writer, ct);

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
        }

        void PostProcessCommentsChunk()
        {
            ProgressAsync.RaiseAsync(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }

        void PostProcessComments()
        {
            if (Subtitles.Count > 0)
            {
                SortRegularSubtitles(Subtitles);
                OverlapRegularSubtitles(Subtitles, ref subtitlesCount);

                WriteMessages(ref lastWritingTask, Subtitles, settings, writer, ct);
            }

            lastWritingTask?.Wait(ct);

            FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }
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

            var newSubs = times.Join(
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

    #region Rolling Chat Subtitles

    private void WriteRollingChatSubtitles(JToken root, (string emoticon, Regex regex)[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, string fileName, StreamWriter writer, ref Exception error)
    {
        if (settings.SubtitlesRollingDirection == SubtitlesRollingDirection.None)
            settings.SubtitlesRollingDirection = SubtitlesRollingDirection.BottomToTop;

        if (settings.SubtitlesSpeed == SubtitlesSpeed.None)
            settings.SubtitlesSpeed = SubtitlesSpeed.Speed1;
        TimeSpan timeStep = TimeSpan.FromMilliseconds((int)settings.SubtitlesSpeed);

        if (settings.SubtitlesFontSize == SubtitlesFontSize.None)
            settings.SubtitlesFontSize = SubtitlesFontSize.Regular;
        int fontSize = (int)settings.SubtitlesFontSize;

        if (settings.SubtitlesLocation == SubtitlesLocation.None)
            settings.SubtitlesLocation = SubtitlesLocation.Left;

        CalculateChatPosYs(fontSize, settings.MaxBottomPosY, settings.SubtitlesLocation, settings.SubtitlesRollingDirection, out int topPosY, out int bottomPosY, out int posYCount);

        Task<int> lastWritingTask = null;
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        GetComments(root, out JToken comments, out int totalMessages);

        TimeSpan nextAvailableTimeSlot = TimeSpan.MinValue;
        bool isWriteSubtitles = false;
        TimeSpan maxShowTime = TimeSpan.MinValue;

        if (settings.ASS)
            WriteASSRollingChatSubtitles(fileName, writer, topPosY, bottomPosY);

        ProcessCommentsInChunks(
            comments,
            regexEmbeddedEmoticons,
            userColors,
            settings,
            ProcessComment,
            PostProcessCommentsChunk,
            PostProcessComments,
            ref error,
            cts,
            ct
        );

        void ProcessComment((ProcessedComment processedComment, ChatMessage message) pccm)
        {
            if (pccm.processedComment.Timestamp < TimeSpan.Zero ||
                string.IsNullOrEmpty(pccm.processedComment.Body))
            {
                discardedMessagesCount++;
                return;
            }

            messagesCount++;

#if DEBUG
            if (IsKeepMessage(messagesCount) == false)
            {
                discardedMessagesCount++;
                return;
            }
#endif

            TimeSpan showTime = pccm.processedComment.Timestamp;
            int linesCount = pccm.message.LinesCount;

            if (showTime < nextAvailableTimeSlot)
                showTime = nextAvailableTimeSlot;
            nextAvailableTimeSlot = showTime + (linesCount * timeStep);

            IEnumerable<Subtitle> subtitles = null;

            if (settings.SubtitlesRollingDirection == SubtitlesRollingDirection.TopToBottom)
            {
                var timeAndPos =
                    Enumerable.Range(1, posYCount + (linesCount - 1))
                    .Select(n => new
                    {
                        n,
                        showTime = showTime + ((n - 1) * timeStep),
                        hideTime = showTime + (n * timeStep),
                        posY = (n <= linesCount ? topPosY : topPosY + ((n - linesCount) * fontSize))
                    });

                IEnumerable<RollingSubtitleInfoTopToBottom> subsInfo = null;

                if (linesCount <= posYCount)
                {
                    subsInfo = timeAndPos.Select(x => new RollingSubtitleInfoTopToBottom
                    {
                        N = x.n,
                        ShowTime = x.showTime,
                        HideTime = x.hideTime,
                        PosY = x.posY,

                        // subtitle roll in from the top
                        KeepCount_Bottom_RollIn = (x.n < linesCount ? x.n : -1),

                        // subtitle roll out from the bottom
                        ShaveCount_Bottom_RollOut = (x.n > posYCount ? x.n - posYCount : -1),
                        KeepCount_Bottom_RollOut = -1
                    });
                }
                else
                {
                    subsInfo = timeAndPos.Select(x => new RollingSubtitleInfoTopToBottom
                    {
                        N = x.n,
                        ShowTime = x.showTime,
                        HideTime = x.hideTime,
                        PosY = x.posY,

                        // subtitle roll in from the top
                        KeepCount_Bottom_RollIn = (x.n < posYCount ? x.n : -1),

                        // subtitle roll out from the bottom
                        ShaveCount_Bottom_RollOut = (x.n > posYCount ? x.n - posYCount : -1),
                        KeepCount_Bottom_RollOut = (x.n >= posYCount && x.n < linesCount ? posYCount : -1)
                    });
                }

                subtitles = subsInfo.Select(info =>
                {
                    var sub =
                        new Subtitle(info.ShowTime, info.HideTime, info.PosY, pccm.message)
                        .KeepLinesFromTheBottom(info.KeepCount_Bottom_RollIn)
                        .ShaveLinesFromTheBottom(info.ShaveCount_Bottom_RollOut)
                        .KeepLinesFromTheBottom(info.KeepCount_Bottom_RollOut);

                    if (sub.IsEmpty)
                        throw GetEmptyRollingSubtitleException(pccm.message, settings.SubtitlesRollingDirection, linesCount, posYCount, info);

                    if (sub.HasEmptyMessage)
                        throw GetEmptyMessageRollingSubtitleException(pccm.message, settings.SubtitlesRollingDirection, linesCount, posYCount, info);

                    return sub;
                });
            }
            else
            {
                var timeAndPos =
                    Enumerable.Range(1, posYCount + (linesCount - 1))
                    .Select(n => new
                    {
                        n,
                        showTime = showTime + ((n - 1) * timeStep),
                        hideTime = showTime + (n * timeStep),
                        posY = (n >= posYCount ? topPosY : bottomPosY - ((n - 1) * fontSize))
                    });

                IEnumerable<RollingSubtitleInfoBottomToTop> subsInfo = null;

                if (linesCount <= posYCount)
                {
                    subsInfo = timeAndPos.Select(x => new RollingSubtitleInfoBottomToTop
                    {
                        N = x.n,
                        ShowTime = x.showTime,
                        HideTime = x.hideTime,
                        PosY = x.posY,

                        // subtitle roll in from the bottom
                        KeepCount_Top_RollIn = (x.n < linesCount ? x.n : -1),

                        // subtitle roll out from the top
                        ShaveCount_Top_RollOut = (x.n > posYCount ? x.n - posYCount : -1),
                        KeepCount_Top_RollOut = -1
                    });
                }
                else
                {
                    subsInfo = timeAndPos.Select(x => new RollingSubtitleInfoBottomToTop
                    {
                        N = x.n,
                        ShowTime = x.showTime,
                        HideTime = x.hideTime,
                        PosY = x.posY,

                        // subtitle roll in from the bottom
                        KeepCount_Top_RollIn = (x.n < posYCount ? x.n : -1),

                        // subtitle roll out from the top
                        ShaveCount_Top_RollOut = (x.n > posYCount ? x.n - posYCount : -1),
                        KeepCount_Top_RollOut = (x.n >= posYCount && x.n < linesCount ? posYCount : -1)
                    });
                }

                subtitles = subsInfo.Select(info =>
                {
                    var sub =
                        new Subtitle(info.ShowTime, info.HideTime, info.PosY, pccm.message)
                        .KeepLinesFromTheTop(info.KeepCount_Top_RollIn)
                        .ShaveLinesFromTheTop(info.ShaveCount_Top_RollOut)
                        .KeepLinesFromTheTop(info.KeepCount_Top_RollOut);

                    if (sub.IsEmpty)
                        throw GetEmptyRollingSubtitleException(pccm.message, settings.SubtitlesRollingDirection, linesCount, posYCount, info);

                    if (sub.HasEmptyMessage)
                        throw GetEmptyMessageRollingSubtitleException(pccm.message, settings.SubtitlesRollingDirection, linesCount, posYCount, info);

                    return sub;
                });
            }

            if (isWriteSubtitles)
            {
                if (maxShowTime < showTime)
                {
                    TimeSpan projectedLastShowTime = subtitles.Last().ShowTime;

                    if (maxShowTime < projectedLastShowTime)
                    {
                        SortRollingChatSubtitles(Subtitles, settings.SubtitlesRollingDirection);

                        int count = 0;
                        var sub = Subtitles.FirstOrDefault(s => s.ShowTime > maxShowTime);
                        if (sub != null)
                            count = Subtitles.IndexOf(sub);
                        else
                            count = Subtitles.Count;

                        var copySubtitles = Subtitles.Take(count).ToArray();
                        WriteMessages(ref lastWritingTask, copySubtitles, settings, writer, ct);

                        Subtitles.RemoveRange(0, count);

                        isWriteSubtitles = false;
                        maxShowTime = TimeSpan.MinValue;
                    }
                }
            }

            int countBefore = Subtitles.Count;
            Subtitles.AddRange(subtitles);
            subtitlesCount += Subtitles.Count - countBefore;

            if (Subtitles.Count >= FLUSH_SUBTITLES_COUNT)
            {
                if (isWriteSubtitles == false)
                {
                    maxShowTime = Subtitles.Max(subtitle => subtitle.ShowTime);
                    isWriteSubtitles = true;
                }
            }
        }

        void PostProcessCommentsChunk()
        {
            ProgressAsync.RaiseAsync(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }

        void PostProcessComments()
        {
            if (Subtitles.Count > 0)
            {
                SortRollingChatSubtitles(Subtitles, settings.SubtitlesRollingDirection);

                WriteMessages(ref lastWritingTask, Subtitles, settings, writer, ct);
            }

            lastWritingTask?.Wait(ct);

            FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }
    }

    private static void SortRollingChatSubtitles(List<Subtitle> Subtitles, SubtitlesRollingDirection subtitlesRollingDirection)
    {
        if (subtitlesRollingDirection == SubtitlesRollingDirection.TopToBottom)
            SortRollingChatSubtitles_TopToBottom(Subtitles);
        else
            SortRollingChatSubtitles_BottomToTop(Subtitles);
    }

    private static void SortRollingChatSubtitles_TopToBottom(List<Subtitle> Subtitles)
    {
        Subtitles.Sort((x, y) =>
        {
            int val = x.ShowTime.CompareTo(y.ShowTime);
            if (val != 0)
                return val;

            return y.PosY.CompareTo(x.PosY);
        });
    }

    private static void SortRollingChatSubtitles_BottomToTop(List<Subtitle> Subtitles)
    {
        Subtitles.Sort((x, y) =>
        {
            int val = x.ShowTime.CompareTo(y.ShowTime);
            if (val != 0)
                return val;

            return x.PosY.CompareTo(y.PosY);
        });
    }

    private static Exception GetEmptyRollingSubtitleException(
        ChatMessage message,
        SubtitlesRollingDirection subtitlesRollingDirection,
        int linesCount,
        int posYCount,
        RollingSubtitleInfo info)
    {
        return GetRollingSubtitleException(
            "Rolling subtitle is empty.",
            message,
            subtitlesRollingDirection,
            linesCount,
            posYCount,
            info
        );
    }

    private static Exception GetEmptyMessageRollingSubtitleException(
        ChatMessage message,
        SubtitlesRollingDirection subtitlesRollingDirection,
        int linesCount,
        int posYCount,
        RollingSubtitleInfo info)
    {
        return GetRollingSubtitleException(
            "Rolling subtitle has an empty message.",
            message,
            subtitlesRollingDirection,
            linesCount,
            posYCount,
            info
        );
    }

    private static Exception GetRollingSubtitleException(
        string title,
        ChatMessage message,
        SubtitlesRollingDirection subtitlesRollingDirection,
        int linesCount,
        int posYCount,
        RollingSubtitleInfo info)
    {
        return new Exception(string.Join(Environment.NewLine,
            title,
            message.ChatLogTimestampAndUser(true)
#if DEBUG
            , $"{subtitlesRollingDirection}, {nameof(linesCount)}={linesCount} {(linesCount <= posYCount ? "<=" : ">")} {nameof(posYCount)}={posYCount}",
            info.ToString()
#endif
        ));
    }

    #endregion

    #region Static Chat Subtitles

    private void WriteStaticChatSubtitles(JToken root, (string emoticon, Regex regex)[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, string fileName, StreamWriter writer, ref Exception error)
    {
        if (settings.SubtitlesRollingDirection == SubtitlesRollingDirection.None)
            settings.SubtitlesRollingDirection = SubtitlesRollingDirection.BottomToTop;

        if (settings.SubtitlesFontSize == SubtitlesFontSize.None)
            settings.SubtitlesFontSize = SubtitlesFontSize.Regular;
        int fontSize = (int)settings.SubtitlesFontSize;

        if (settings.SubtitlesLocation == SubtitlesLocation.None)
            settings.SubtitlesLocation = SubtitlesLocation.Left;

        CalculateChatPosYs(fontSize, settings.MaxBottomPosY, settings.SubtitlesLocation, settings.SubtitlesRollingDirection, out int topPosY, out _, out int posYCount);

        // 99:59:59,999
        TimeSpan hideTimeMaxValue = TimeSpan.FromMilliseconds(
            (99 * 60 * 60 * 1000) +
            (59 * 60 * 1000) +
            (59 * 1000) +
            999
        );

        Task<int> lastWritingTask = null;
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        GetComments(root, out JToken comments, out int totalMessages);

        Subtitle prevSubtitle = null;

        if (settings.ASS)
            WriteASSStaticChatSubtitles(fileName, writer, topPosY);

        ProcessCommentsInChunks(
            comments,
            regexEmbeddedEmoticons,
            userColors,
            settings,
            ProcessComment,
            PostProcessCommentsChunk,
            PostProcessComments,
            ref error,
            cts,
            ct
        );

        void ProcessComment((ProcessedComment processedComment, ChatMessage message) pccm)
        {
            if (pccm.processedComment.Timestamp < TimeSpan.Zero ||
                string.IsNullOrEmpty(pccm.processedComment.Body))
            {
                discardedMessagesCount++;
                return;
            }

            messagesCount++;

#if DEBUG
            if (IsKeepMessage(messagesCount) == false)
            {
                discardedMessagesCount++;
                return;
            }
#endif

            TimeSpan showTime = pccm.processedComment.Timestamp;

            if (prevSubtitle == null)
            {
                var subtitle = new Subtitle(showTime, hideTimeMaxValue, topPosY, pccm.message);

                Subtitles.Add(subtitle);
                subtitlesCount++;

                prevSubtitle = subtitle;
            }
            else
            {
                var subtitle = new Subtitle(showTime, hideTimeMaxValue, topPosY, prevSubtitle);

                if (settings.SubtitlesRollingDirection == SubtitlesRollingDirection.TopToBottom)
                {
                    subtitle.InsertFirstMessage(pccm.message);

                    if (subtitle.LinesCount > posYCount)
                        subtitle.KeepLinesFromTheTop(posYCount);
                }
                else
                {
                    subtitle.AddMessage(pccm.message);

                    if (subtitle.LinesCount > posYCount)
                        subtitle.KeepLinesFromTheBottom(posYCount);
                }

                subtitle.SetSubtitlesConsecutively(prevSubtitle);

                Subtitles.Add(subtitle);
                subtitlesCount++;

                prevSubtitle = subtitle;
            }

            if (Subtitles.Count >= FLUSH_SUBTITLES_COUNT)
            {
                int lastIndex = Subtitles.Count - 1;
                var lastSub = Subtitles[lastIndex];
                Subtitles.RemoveAt(lastIndex);

                var copySubtitles = Subtitles.ToArray();
                WriteMessages(ref lastWritingTask, copySubtitles, settings, writer, ct);

                Subtitles.Clear();
                Subtitles.Add(lastSub);
            }
        }

        void PostProcessCommentsChunk()
        {
            ProgressAsync.RaiseAsync(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }

        void PostProcessComments()
        {
            if (Subtitles.Count > 0)
                WriteMessages(ref lastWritingTask, Subtitles, settings, writer, ct);

            lastWritingTask?.Wait(ct);

            FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }
    }

    #endregion

    #region Calculate Chat PosYs

    private static void CalculateChatPosYs(int fontSize, int maxBottomPosY, SubtitlesLocation subtitlesLocation, SubtitlesRollingDirection subtitlesRollingDirection, out int topPosY, out int bottomPosY, out int posYCount)
    {
        topPosY = 0;
        bottomPosY = 0;
        while (bottomPosY <= maxBottomPosY)
            bottomPosY += fontSize;
        bottomPosY -= fontSize;

        if (maxBottomPosY > bottomPosY)
        {
            int diff = maxBottomPosY - bottomPosY;

            if (subtitlesRollingDirection == SubtitlesRollingDirection.BottomToTop)
            {
                topPosY += diff;
                bottomPosY = maxBottomPosY;
            }
            else if (subtitlesRollingDirection == SubtitlesRollingDirection.TopToBottom)
            {
                for (int n = 3; n >= 1; n--)
                {
                    if (diff >= n)
                    {
                        topPosY = n;
                        bottomPosY += n;
                        break;
                    }
                }
            }
        }

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

    #region Write ASS

    private void WriteASSRegularSubtitles(string fileName, StreamWriter writer)
    {
        WriteASSScriptInfo(fileName, writer);

        int fontSize = (settings.SubtitlesFontSize == SubtitlesFontSize.None ? 20 : (int)settings.SubtitlesFontSize);
        var color = (settings.TextASSAColor ?? ASSAColor.White).ToString()[..^1];
        writer.WriteLine($"Style: Default,{Subtitle.FONT_NAME},{fontSize},{color},{color},&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,2,10,10,10,1");

        WriteASSEvents(writer);
        writer.Flush();
    }

    private void WriteASSRollingChatSubtitles(string fileName, StreamWriter writer, int topPosY, int bottomPosY)
    {
        WriteASSScriptInfo(fileName, writer);

        int fontSize = (int)settings.SubtitlesFontSize;
        var color = (settings.TextASSAColor ?? ASSAColor.White).ToString()[..^1];

        if (settings.SubtitlesLocation.IsRight())
        {
            int posY = topPosY;
            while (posY <= bottomPosY)
            {
                writer.WriteLine($@"Style: TextV{posY},{Subtitle.FONT_NAME},{fontSize},{color},{color},&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,{settings.TextPosXLocationRight},0,{posY},1");
                posY += fontSize;
            }

            posY = topPosY;
            while (posY <= bottomPosY)
            {
                writer.WriteLine($@"Style: BrailleV{posY},{Subtitle.FONT_NAME},{fontSize},{color},{color},&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,{settings.BraillePosXLocationRight},0,{posY},1");
                posY += fontSize;
            }
        }
        else
        {
            int posY = topPosY;
            while (posY <= bottomPosY)
            {
                writer.WriteLine($@"Style: TextV{posY},{Subtitle.FONT_NAME},{fontSize},{color},{color},&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,{Subtitle.POS_X_LOCATION_LEFT},0,{posY},1");
                posY += fontSize;
            }
        }

        WriteASSEvents(writer);
        writer.Flush();
    }

    private void WriteASSStaticChatSubtitles(string fileName, StreamWriter writer, int topPosY)
    {
        WriteASSScriptInfo(fileName, writer);

        int fontSize = (int)settings.SubtitlesFontSize;
        var color = (settings.TextASSAColor ?? ASSAColor.White).ToString()[..^1];

        if (settings.SubtitlesLocation.IsRight())
        {
            writer.WriteLine($@"Style: Default,{Subtitle.FONT_NAME},{fontSize},{color},{color},&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,{settings.TextPosXLocationRight},0,{topPosY},1");
            writer.WriteLine($@"Style: Braille,{Subtitle.FONT_NAME},{fontSize},{color},{color},&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,{settings.BraillePosXLocationRight},0,{topPosY},1");
        }
        else
        {
            writer.WriteLine($@"Style: Default,{Subtitle.FONT_NAME},{fontSize},{color},{color},&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,{Subtitle.POS_X_LOCATION_LEFT},0,{topPosY},1");
        }

        WriteASSEvents(writer);
        writer.Flush();
    }

    private static void WriteASSScriptInfo(string fileName, StreamWriter writer)
    {
        writer.WriteLine($@"[Script Info]
; This is an Advanced Sub Station Alpha v4+ script.
Title: {fileName}
ScriptType: v4.00+
PlayResX: 384
PlayResY: 288
PlayDepth: 0
ScaledBorderAndShadow: Yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
    }

    private static void WriteASSEvents(StreamWriter writer)
    {
        writer.WriteLine($@"
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");
    }

    #endregion

    #region Chat Text File

    private void WriteChatTextFile(JToken root, (string emoticon, Regex regex)[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, StreamWriter writer, ref Exception error)
    {
        Task<int> lastWritingTask = null;
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        List<ChatMessage> ChatMessages = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        GetComments(root, out JToken comments, out int totalMessages);

        ProcessCommentsInChunks(
            comments,
            regexEmbeddedEmoticons,
            userColors,
            settings,
            ProcessComment,
            PostProcessCommentsChunk,
            PostProcessComments,
            ref error,
            cts,
            ct
        );

        void ProcessComment((ProcessedComment processedComment, ChatMessage message) pccm)
        {
            if (string.IsNullOrEmpty(pccm.processedComment.Body))
            {
                discardedMessagesCount++;
                return;
            }

            messagesCount++;

#if DEBUG
            if (IsKeepMessage(messagesCount) == false)
            {
                discardedMessagesCount++;
                return;
            }
#endif

            ChatMessages.Add(pccm.message);
            subtitlesCount++;

            if (ChatMessages.Count >= FLUSH_SUBTITLES_COUNT)
            {
                var copyChatMessages = ChatMessages.ToArray();
                WriteMessages(ref lastWritingTask, copyChatMessages, settings, writer, ct);
                ChatMessages.Clear();
            }
        }

        void PostProcessCommentsChunk()
        {
            ProgressAsync.RaiseAsync(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }

        void PostProcessComments()
        {
            if (ChatMessages.Count > 0)
                WriteMessages(ref lastWritingTask, ChatMessages, settings, writer, ct);

            lastWritingTask?.Wait(ct);

            FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }
    }

    #endregion

    #region Get Comments

    private static void GetComments(JToken root, out JToken comments, out int totalMessages)
    {
        comments = root.SelectToken("comments");
        if (comments.TryGetNonEnumeratedCount(out totalMessages) == false)
            totalMessages = comments.Count();
    }

    #endregion

    #region Process Comments

    private static void ProcessCommentsInChunks(
        JToken comments,
        (string emoticon, Regex regex)[] regexEmbeddedEmoticons,
        Dictionary<string, UserColor> userColors,
        TwitchSubtitlesSettings settings,
        Action<(ProcessedComment, ChatMessage)> ProcessComment,
        Action PostProcessCommentsChunk,
        Action PostProcessComments,
        ref Exception error,
        CancellationTokenSource cts,
        CancellationToken ct)
    {
        try
        {
            Task<(ProcessedComment, ChatMessage)[]> processCommentsTask = null;

            JToken[] firstChunk = null;
            bool hasReachedFirstChunk = false;
            bool hasReachedSecondChunk = false;

            foreach (JToken[] chunk in comments.Chunk(COMMENTS_CHUNK_SIZE))
            {
                if (hasReachedFirstChunk == false)
                {
                    firstChunk = chunk;
                    hasReachedFirstChunk = true;
                    continue;
                }

                if (hasReachedSecondChunk == false)
                {
                    processCommentsTask = Task.Run(() => ProcessCommentsAsync(chunk /* second chunk */, regexEmbeddedEmoticons, userColors, settings, ct), ct);

                    foreach (JToken comment in firstChunk)
                        ProcessComment(GetProcessComment(comment, regexEmbeddedEmoticons, userColors, settings, ct));
                    PostProcessCommentsChunk();

                    firstChunk = null;
                    hasReachedSecondChunk = true;
                    continue;
                }

                processCommentsTask.Wait(ct);
                (ProcessedComment, ChatMessage)[] currentComments = processCommentsTask.Result;

                processCommentsTask = Task.Run(() => ProcessCommentsAsync(chunk, regexEmbeddedEmoticons, userColors, settings, ct), ct);

                foreach ((ProcessedComment, ChatMessage) processedComment in currentComments)
                    ProcessComment(processedComment);
                PostProcessCommentsChunk();
            }

            if (hasReachedFirstChunk)
            {
                if (hasReachedSecondChunk)
                {
                    processCommentsTask.Wait(ct);
                    (ProcessedComment, ChatMessage)[] currentComments = processCommentsTask.Result;

                    foreach ((ProcessedComment, ChatMessage) processedComment in currentComments)
                        ProcessComment(processedComment);
                    PostProcessCommentsChunk();
                }
                else
                {
                    foreach (JToken comment in firstChunk)
                        ProcessComment(GetProcessComment(comment, regexEmbeddedEmoticons, userColors, settings, ct));
                    PostProcessCommentsChunk();
                }
            }

            PostProcessComments();
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish the task gracefully
        }
        catch (AggregateException aex)
        {
            error = aex.InnerException;
        }
        catch (Exception ex)
        {
            cts.Cancel();
            error = ex;
        }
    }

    private static (ProcessedComment, ChatMessage)[] ProcessCommentsAsync(
        JToken[] comments,
        (string emoticon, Regex regex)[] regexEmbeddedEmoticons,
        Dictionary<string, UserColor> userColors,
        TwitchSubtitlesSettings settings,
        CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            return [.. comments.Select(comment => GetProcessComment(comment, regexEmbeddedEmoticons, userColors, settings, ct))];
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish the task gracefully
            return null;
        }
        catch
        {
            throw;
        }
    }

    private static (ProcessedComment, ChatMessage) GetProcessComment(
        JToken comment,
        (string emoticon, Regex regex)[] regexEmbeddedEmoticons,
        Dictionary<string, UserColor> userColors,
        TwitchSubtitlesSettings settings,
        CancellationToken ct)
    {
        var processedComment = new ProcessedComment
        {
            Timestamp =
                TimeSpan.FromSeconds(comment.SelectToken("content_offset_seconds").Value<int>()) +
                TimeSpan.FromSeconds(settings.ChatTextFile ? 0 : settings.TimeOffset),
            User = comment.SelectToken("commenter").SelectToken("display_name").Value<string>(),
            IsModerator = comment.SelectToken("message").SelectToken("user_badges").Any(ub =>
                ub.SelectToken("_id").Value<string>() == "broadcaster" ||
                ub.SelectToken("_id").Value<string>() == "moderator"
            )
        };

        if (processedComment.Timestamp < TimeSpan.Zero)
            return (processedComment, null);

        processedComment.Body = GetMessageBody(
            comment.SelectToken("message"),
            processedComment.User,
            regexEmbeddedEmoticons,
            userColors,
            processedComment.Timestamp,
            settings,
            out processedComment.IsBrailleArt,
            ct
        );

        if (string.IsNullOrEmpty(processedComment.Body))
            return (processedComment, null);

        if (settings.ColorUserNames)
        {
            if (userColors.TryGetValue(processedComment.User, out var userColor))
                processedComment.Color = userColor.Color;
        }

        var message = new ChatMessage(processedComment.Timestamp, processedComment.User, processedComment.IsModerator, processedComment.Color, processedComment.Body, processedComment.IsBrailleArt);
        return (processedComment, message);
    }

    #endregion

    #region Message Body

    // https://en.wikipedia.org/wiki/Braille_ASCII
    [GeneratedRegex(@"[⠁⠂⠃⠄⠅⠆⠇⠈⠉⠊⠋⠌⠍⠎⠏⠐⠑⠒⠓⠔⠕⠖⠗⠘⠙⠚⠛⠜⠝⠞⠟⠠⠡⠢⠣⠤⠥⠦⠧⠨⠩⠪⠫⠬⠭⠮⠯⠰⠱⠲⠳⠴⠵⠶⠷⠸⠹⠺⠻⠼⠽⠾⠿]")]
    private static partial Regex RegexIsBrailleArt();

    private const char BRAILLE_SPACE = '⠀'; // \u2800

    [GeneratedRegex(@"(?<Prefix>\s*)[A-Za-z0-9]+(?<Suffix>\s*)")]
    private static partial Regex RegexEmoticonName();

    [GeneratedRegex(@"\s*⭕\s*")]
    private static partial Regex RegexNonBrailleWithSpaces();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex RegexDoubleSpaces();

    [GeneratedRegex(@"^\s+|\s+$")]
    private static partial Regex RegexBodyTrim();

    // starts with https:// or www.
    // (?:https://|http://|ftp://|www\.)
    // [A-Za-z0-9-\\@:%_\+~#=,?&./]+

    // starts with xxx.xxx.xxx/
    // (?:[A-Za-z0-9-\\@:%_\+~#=,?&]+\.)+
    // [A-Za-z0-9-\\@:%_\+~#=,?&]+/
    // [A-Za-z0-9-\\@:%_\+~#=,?&./]+

    // ends with com,gov,net,org,tv
    // (?:[A-Za-z0-9-\\@:%_\+~#=,?&]+\.)+
    // (?:com|gov|net|org|tv)

    [GeneratedRegex(@"(?<Link>(?:https://|http://|ftp://|www\.)[A-Za-z0-9-\\@:%_\+~#=,?&./]+|(?:[A-Za-z0-9-\\@:%_\+~#=,?&]+\.)+[A-Za-z0-9-\\@:%_\+~#=,?&]+/[A-Za-z0-9-\\@:%_\+~#=,?&./]+|(?:[A-Za-z0-9-\\@:%_\+~#=,?&]+\.)+(?:com|gov|net|org|tv))", RegexOptions.IgnoreCase)]
    private static partial Regex RegexLink();

    private static string GetMessageBody(
        JToken message,
        string user,
        (string emoticon, Regex regex)[] regexEmbeddedEmoticons,
        Dictionary<string, UserColor> userColors,
        TimeSpan timestamp,
        TwitchSubtitlesSettings settings,
        out bool isBrailleArt,
        CancellationToken ct)
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

        if (isBrailleArt = RegexIsBrailleArt().IsMatch(body.ToString()))
        {
            if (settings.RemoveEmoticonNames)
            {
                body.Clear();
                body.Append(message.SelectToken("body").Value<string>());

                foreach (var match in RegexEmoticonName().Matches(body.ToString()).Cast<Match>().OrderByDescending(m => m.Index))
                {
                    int length =
                        match.Groups["Prefix"].Length +
                        2 + // emoticon name
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

            if (RegexNonBrailleWithSpaces().IsMatch(body.ToString()))
            {
                foreach (var match in RegexNonBrailleWithSpaces().Matches(body.ToString()).Cast<Match>())
                {
                    body.Replace(' ', BRAILLE_SPACE, match.Index, match.Length);
                }
            }

            body.Replace(" ", "\\N");

            if (settings.ASS == false && settings.IsUsingAssaTags == false)
                body.Replace(@"\N", Environment.NewLine);

            return body.ToString();
        }

        if (settings.RemoveEmoticonNames)
        {
            string bodyString = body.ToString();

            if (regexEmbeddedEmoticons.Length <= EMBEDDED_EMOTICONS_CHUNK_SIZE)
            {
                foreach (var (emoticon, regex) in regexEmbeddedEmoticons)
                {
                    if (bodyString.Contains(emoticon, StringComparison.OrdinalIgnoreCase))
                    {
                        regex.Replace(body, " ");
                        bodyString = body.ToString();
                    }
                }
            }
            else
            {
                var tasks = regexEmbeddedEmoticons
                    .Chunk(EMBEDDED_EMOTICONS_CHUNK_SIZE)
                    .Select(items => Task.Run(() => IsBodyHasEmbeddedEmoticons(bodyString, items, ct), ct))
                    .ToArray();

                Task.WaitAll(tasks, ct);

                foreach (var task in tasks.Where(x => x.Result.HasAny()))
                {
                    foreach (var (emoticon, regex) in task.Result)
                        regex.Replace(body, " ");
                }
            }
        }

        body.Replace("\uDB40\uDC00", " ").Replace('\uFFFC', ' ');
        RegexDoubleSpaces().Replace(body, " ");
        RegexBodyTrim().Replace(body, string.Empty);

        if (body.Length == 0)
            return null;

        if (settings.ChatTextFile)
            return body.ToString();

        if (settings.RollingChatSubtitles || settings.StaticChatSubtitles)
            SplitMessageBody(body, user, timestamp, settings);

        if (settings.IsUsingAssaTags)
        {
            string bodyString = body.ToString();
            if (RegexLink().IsMatch(bodyString))
            {
                foreach (var match in RegexLink().Matches(bodyString).Cast<Match>().OrderByDescending(m => m.Index))
                {
                    body.Insert(match.Index + match.Length, @"{\u0}");
                    body.Insert(match.Index, @"{\u1}");
                }

                body.Replace(@"{\u1}\N", @"\N{\u1}").Replace(@"\N{\u0}", @"{\u0}\N");
            }
        }

        if (settings.ColorUserNames)
        {
            string bodyString = body.ToString();

            if (userColors.Count <= USER_COLORS_CHUNK_SIZE)
            {
                foreach (var item in userColors)
                {
                    if (bodyString.Contains(item.Value.User, StringComparison.OrdinalIgnoreCase))
                    {
                        ColorUserNames(item.Value);
                        bodyString = body.ToString();
                    }
                }
            }
            else
            {
                var tasks = userColors
                    .Chunk(USER_COLORS_CHUNK_SIZE)
                    .Select(items => Task.Run(() => IsBodyHasUserNames(bodyString, items, ct), ct))
                    .ToArray();

                Task.WaitAll(tasks, ct);

                foreach (var task in tasks.Where(x => x.Result.HasAny()))
                {
                    foreach (var item in task.Result)
                        ColorUserNames(item.Value);
                }
            }

            void ColorUserNames(UserColor userColor)
            {
                if (settings.ASS == false && settings.TextASSAColor != null)
                    userColor.SearchAndReplace(body, $@"{{\c{settings.TextASSAColor}}}");
                else
                    userColor.SearchAndReplace(body);
            }
        }

        return body.ToString();
    }

    private static (string emoticon, Regex regex)[] IsBodyHasEmbeddedEmoticons(string bodyString, (string emoticon, Regex regex)[] emoticons, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            return [.. emoticons.Where(x => bodyString.Contains(x.emoticon, StringComparison.OrdinalIgnoreCase))];
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish the task gracefully
            return null;
        }
        catch
        {
            throw;
        }
    }

    private static KeyValuePair<string, UserColor>[] IsBodyHasUserNames(string bodyString, KeyValuePair<string, UserColor>[] userColors, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            return [.. userColors.Where(x => bodyString.Contains(x.Value.User, StringComparison.OrdinalIgnoreCase))];
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish the task gracefully
            return null;
        }
        catch
        {
            throw;
        }
    }

    private static void SplitMessageBody(StringBuilder body, string user, TimeSpan timestamp, TwitchSubtitlesSettings settings)
    {
        try
        {
            int startIndex = 0;
            while (startIndex < body.Length)
            {
                int endIndex = startIndex + settings.LineLength - 1;

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

                    if (endIndex <= 0)
                    {
                        body.Insert(0, '\n');
                        startIndex = 1;
                        continue;
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
                    if (body[i] == '/' || body[i] == '?' || body[i] == '&')
                    {
                        body.Insert(i + 1, '\n');
                        startIndex = i + 2;
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;

                if (startIndex == 0)
                {
                    body.Insert(0, '\n');
                    startIndex = 1;
                    continue;
                }

                if (endIndex + 1 <= body.Length)
                {
                    body.Insert(endIndex + 1, '\n');
                    startIndex = endIndex + 2;
                }
            }

            if (body.Length > 0 && body[^1] == '\n')
                body.Remove(body.Length - 1, 1);

            body.Replace("\n", "\\N");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to split message body.{Environment.NewLine}{ChatMessage.ToTimestamp(timestamp)} {user}: {body}", ex);
        }
    }

    #endregion

    #region Write Messages

    private static void WriteMessages(
        ref Task<int> lastWritingTask,
        IEnumerable<IMessage> messages,
        TwitchSubtitlesSettings settings,
        StreamWriter writer,
        CancellationToken ct)
    {
        if (lastWritingTask != null && lastWritingTask.Exception != null)
            throw lastWritingTask.Exception;

        if (lastWritingTask == null)
            lastWritingTask = Task.Run(() => WriteMessagesAsync(messages, 1, settings, writer, ct), ct);
        else
            lastWritingTask = lastWritingTask.ContinueWith((previousTask) => WriteMessagesAsync(messages, previousTask.Result, settings, writer, ct), ct);
    }

    private static int WriteMessagesAsync(
        IEnumerable<IMessage> messages,
        int messagesCounter,
        TwitchSubtitlesSettings settings,
        StreamWriter writer,
        CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            if (settings.ChatTextFile || settings.ASS)
            {
                foreach (var message in messages)
                    writer.WriteLine(message.ToString(settings, messagesCounter++));
            }
            else
            {
                foreach (var message in messages)
                {
                    writer.WriteLine(messagesCounter);
                    writer.WriteLine(message.ToString(settings, messagesCounter++));
                }
            }

            writer.Flush();

            ct.ThrowIfCancellationRequested();

            return messagesCounter;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish the task gracefully
            return messagesCounter;
        }
        catch
        {
            throw;
        }
    }

    #endregion

    #region Messages To Keep

#if DEBUG
    private readonly List<int> MessageIndexesToKeep = [];

    private void SortMessageIndexesToKeep()
    {
        MessageIndexesToKeep.Sort();
    }

    private bool IsKeepMessage(int messageIndex)
    {
        return
            MessageIndexesToKeep.Count == 0 ||
            MessageIndexesToKeep.BinarySearch(messageIndex) >= 0;
    }
#endif

    #endregion

    #region Write Font Size Test Subtitles

    public static void WriteFontSizeTestSubtitles(string srtFile)
    {
        using var srtStream = File.Open(srtFile, FileMode.Create);
        using var writer = new StreamWriter(srtStream, Encoding.UTF8);

        int subtitleNumber = 1;

        bool isBrailleArt = false;
        for (int i = 0; i < 2; i++)
        {
            bool isRight = false;
            for (int k = 0; k < 2; k++)
            {
                int posY = (isBrailleArt ? 150 : 50);

                foreach (SubtitlesFontSize subtitlesFontSize in Enum.GetValues<SubtitlesFontSize>())
                {
                    if (subtitlesFontSize == SubtitlesFontSize.None)
                        continue;

                    FieldInfo fi = typeof(SubtitlesFontSize).GetField(subtitlesFontSize.ToString());
                    var measurements = (SubtitlesFontSizeMeasurementsAttribute)fi.GetCustomAttribute(typeof(SubtitlesFontSizeMeasurementsAttribute));

                    string line = null;
                    if (isBrailleArt)
                    {
                        int brailleLineLength = 30;

                        if (isRight)
                        {
                            line = $"⡇{new string('⣿', brailleLineLength - 2)}⢸";
                        }
                        else
                        {
                            int spaceOffset = 0;

                            if (subtitlesFontSize == SubtitlesFontSize.Regular)
                                spaceOffset = 117;
                            else if (subtitlesFontSize == SubtitlesFontSize.Medium)
                                spaceOffset = 96;
                            else if (subtitlesFontSize == SubtitlesFontSize.Large)
                                spaceOffset = 87;
                            else if (subtitlesFontSize == SubtitlesFontSize.XL)
                                spaceOffset = 80;
                            else if (subtitlesFontSize == SubtitlesFontSize.X2L)
                                spaceOffset = 68;
                            else if (subtitlesFontSize == SubtitlesFontSize.X3L)
                                spaceOffset = 59;
                            else if (subtitlesFontSize == SubtitlesFontSize.X4L)
                                spaceOffset = 51;
                            else if (subtitlesFontSize == SubtitlesFontSize.X5L)
                                spaceOffset = 45;

                            line = $"⡇{new string(' ', spaceOffset)}{subtitlesFontSize}, {brailleLineLength} chars, PosX {measurements.BraillePosXLocationRight}";
                        }
                    }
                    else
                    {
                        var nums = Enumerable.Range(1, measurements.LineLength);
                        if (isRight)
                            nums = nums.Reverse();
                        line = string.Join(string.Empty, nums.Select(n => n % 10));

                        if (isRight == false)
                        {
                            int spaceOffset = 0;

                            if (subtitlesFontSize == SubtitlesFontSize.Regular)
                                spaceOffset = 49;
                            else if (subtitlesFontSize == SubtitlesFontSize.Medium)
                                spaceOffset = 39;
                            else if (subtitlesFontSize == SubtitlesFontSize.Large)
                                spaceOffset = 37;
                            else if (subtitlesFontSize == SubtitlesFontSize.XL)
                                spaceOffset = 36;
                            else if (subtitlesFontSize == SubtitlesFontSize.X2L)
                                spaceOffset = 30;
                            else if (subtitlesFontSize == SubtitlesFontSize.X3L)
                                spaceOffset = 21;
                            else if (subtitlesFontSize == SubtitlesFontSize.X4L)
                                spaceOffset = 13;
                            else if (subtitlesFontSize == SubtitlesFontSize.X5L)
                                spaceOffset = 6;

                            line += $"{new string(' ', spaceOffset)}{subtitlesFontSize}, {measurements.LineLength} chars";
                        }
                    }

                    if (subtitlesFontSize <= SubtitlesFontSize.Large)
                        posY += 8;
                    else if (subtitlesFontSize <= SubtitlesFontSize.X3L)
                        posY += 10;
                    else
                        posY += 11;

                    writer.WriteLine(subtitleNumber++);
                    writer.WriteLine("00:00:00,000 --> 10:00:00,000");
                    writer.WriteLine($@"{{\a5\an7\pos({(isRight ? (isBrailleArt ? measurements.BraillePosXLocationRight : measurements.TextPosXLocationRight) : Subtitle.POS_X_LOCATION_LEFT)},{posY})\fn{Subtitle.FONT_NAME}\fs{(int)subtitlesFontSize}{Subtitle.FONT_RESET}}}");
                    writer.WriteLine(line);
                    writer.WriteLine(string.Empty);
                }

                isRight = true;
            }

            isBrailleArt = true;
        }
    }

    #endregion
}
