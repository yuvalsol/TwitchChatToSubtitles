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

        string srtFile = Path.Combine(
            Path.GetDirectoryName(jsonFile),
            Path.GetFileNameWithoutExtension(jsonFile) + (settings.ChatTextFile ? ".txt" : ".srt")
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

            WritingPreparations(root, ref regexEmbeddedEmoticons, ref userColors, ref error);

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
                WriteRegularSubtitles(root, regexEmbeddedEmoticons, userColors, writer, ref error);
            else if (settings.RollingChatSubtitles)
                WriteRollingChatSubtitles(root, regexEmbeddedEmoticons, userColors, writer, ref error);
            else if (settings.StaticChatSubtitles)
                WriteStaticChatSubtitles(root, regexEmbeddedEmoticons, userColors, writer, ref error);
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

    private void WritingPreparations(
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
            if (settings.RemoveEmoticonNames)
                taskRemoveEmoticonNames = Task.Run(() => GetEmbeddedEmoticons(root, ct), ct);

            Task<Dictionary<string, UserColor>> taskColorUserNames = null;
            if (settings.ColorUserNames)
                taskColorUserNames = Task.Run(() => GetUserColors(root, ct), ct);

            if (settings.RemoveEmoticonNames && settings.ColorUserNames)
                Task.WaitAll([taskRemoveEmoticonNames, taskColorUserNames], ct);
            else if (settings.RemoveEmoticonNames)
                taskRemoveEmoticonNames.Wait(ct);
            else if (settings.ColorUserNames)
                taskColorUserNames.Wait(ct);

            if (settings.RemoveEmoticonNames)
                regexEmbeddedEmoticons = taskRemoveEmoticonNames.Result;

            if (settings.ColorUserNames)
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

            return
                Enumerable.Empty<string>()
                .Concat(emoticonsInMessages)
                .Concat(emoticonNames)
                .Distinct()
                .OrderBy(name => name)
                // keep text emoticons in messages
                .Where(name => RegexTextEmoticon().IsMatch(name) == false)
                .Select(emoticon => (emoticon, new Regex($@"\b{Regex.Escape(emoticon)}\b", RegexOptions.Compiled)))
                .ToArray();
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
                    userColors.Add(user, new UserColor(user, new Color(userColor)));
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
                        userColors.Add(streamer, new UserColor(streamer, new Color(DEFAULT_USER_COLOR)));
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
                            userColors.Add(streamer, new UserColor(streamer, new Color(DEFAULT_USER_COLOR)));
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

    #region Debug Code

#if DEBUG
    private readonly List<int> Debug_MessageIndexes = [];

    private bool Debug_KeepMessage(int messageIndex)
    {
        return
            Debug_MessageIndexes.IsNullOrEmpty() ||
            Debug_MessageIndexes.BinarySearch(messageIndex) >= 0;
    }
#endif

    #endregion

    #region Regular Subtitles

    private void WriteRegularSubtitles(JToken root, (string emoticon, Regex regex)[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, StreamWriter writer, ref Exception error)
    {
        TimeSpan subtitleShowDuration = TimeSpan.FromSeconds(settings.SubtitleShowDuration);

        Task<int> lastWritingTask = null;
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        GetComments(root, settings, out JToken comments, out int totalMessages, out TimeSpan timeOffset);

        bool isWriteSubtitles = false;

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
            if (Debug_KeepMessage(messagesCount) == false)
                return;
#endif

            TimeSpan showTime = pccm.processedComment.Timestamp + timeOffset;

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
                            WriteSubtitles(ref lastWritingTask, copySubtitles, settings, writer, ct);

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

                WriteSubtitles(ref lastWritingTask, Subtitles, settings, writer, ct);
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

    #region Rolling Chat Subtitles

    private void WriteRollingChatSubtitles(JToken root, (string emoticon, Regex regex)[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, StreamWriter writer, ref Exception error)
    {
        if (settings.SubtitlesRollingDirection == SubtitlesRollingDirection.None)
            settings.SubtitlesRollingDirection = SubtitlesRollingDirection.BottomToTop;

        if (settings.SubtitlesSpeed == SubtitlesSpeed.None)
            settings.SubtitlesSpeed = SubtitlesSpeed.Regular;
        TimeSpan timeStep = TimeSpan.FromSeconds(1.0 / (int)settings.SubtitlesSpeed);

        if (settings.SubtitlesFontSize == SubtitlesFontSize.None)
            settings.SubtitlesFontSize = SubtitlesFontSize.Regular;
        int fontSize = (int)settings.SubtitlesFontSize;

        if (settings.SubtitlesLocation == SubtitlesLocation.None)
            settings.SubtitlesLocation = SubtitlesLocation.Left;

        CalculateChatPosYs(fontSize, settings.SubtitlesLocation, out int topPosY, out int bottomPosY, out int posYCount);

        Task<int> lastWritingTask = null;
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        GetComments(root, settings, out JToken comments, out int totalMessages, out TimeSpan timeOffset);

        TimeSpan nextAvailableTimeSlot = TimeSpan.MinValue;
        bool isWriteSubtitles = false;
        TimeSpan maxShowTime = TimeSpan.MinValue;

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
            if (Debug_KeepMessage(messagesCount) == false)
                return;
#endif

            int linesCount = pccm.message.LinesCount;

            TimeSpan showTime = pccm.processedComment.Timestamp + timeOffset;
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
                        WriteSubtitles(ref lastWritingTask, copySubtitles, settings, writer, ct);

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

                WriteSubtitles(ref lastWritingTask, Subtitles, settings, writer, ct);
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

    private void WriteStaticChatSubtitles(JToken root, (string emoticon, Regex regex)[] regexEmbeddedEmoticons, Dictionary<string, UserColor> userColors, StreamWriter writer, ref Exception error)
    {
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

        Task<int> lastWritingTask = null;
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        List<Subtitle> Subtitles = [];

        int messagesCount = 0;
        int discardedMessagesCount = 0;
        int subtitlesCount = 0;

        GetComments(root, settings, out JToken comments, out int totalMessages, out TimeSpan timeOffset);

        Subtitle prevSubtitle = null;

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
            if (Debug_KeepMessage(messagesCount) == false)
                return;
#endif

            if (prevSubtitle == null)
            {
                TimeSpan showTime = pccm.processedComment.Timestamp + timeOffset;
                var subtitle = new Subtitle(showTime, hideTimeMaxValue, topPosY, pccm.message);

                Subtitles.Add(subtitle);
                subtitlesCount++;

                prevSubtitle = subtitle;
            }
            else
            {
                TimeSpan showTime = pccm.processedComment.Timestamp + timeOffset;
                var subtitle = new Subtitle(showTime, hideTimeMaxValue, topPosY, prevSubtitle);
                subtitle.AddMessage(pccm.message);

                if (subtitle.LinesCount > posYCount)
                    subtitle.KeepLinesFromTheBottom(posYCount);

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
                WriteSubtitles(ref lastWritingTask, copySubtitles, settings, writer, ct);

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
                WriteSubtitles(ref lastWritingTask, Subtitles, settings, writer, ct);

            lastWritingTask?.Wait(ct);

            FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }
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

        GetComments(root, settings, out JToken comments, out int totalMessages, out TimeSpan timeOffset);

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
            if (Debug_KeepMessage(messagesCount) == false)
                return;
#endif

            ChatMessages.Add(pccm.message);
            subtitlesCount++;

            if (ChatMessages.Count >= FLUSH_SUBTITLES_COUNT)
            {
                var copyChatMessages = ChatMessages.ToArray();
                WriteChatMessages(ref lastWritingTask, copyChatMessages, settings, writer, ct);
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
                WriteChatMessages(ref lastWritingTask, ChatMessages, settings, writer, ct);

            lastWritingTask?.Wait(ct);

            FinishWritingSubtitles.Raise(this, () => new ProgressEventArgs(messagesCount, discardedMessagesCount, totalMessages, subtitlesCount));
        }
    }

    #endregion

    #region Get Comments

    private static void GetComments(JToken root, TwitchSubtitlesSettings settings, out JToken comments, out int totalMessages, out TimeSpan timeOffset)
    {
        comments = root.SelectToken("comments");
        if (comments.TryGetNonEnumeratedCount(out totalMessages) == false)
            totalMessages = comments.Count();

        timeOffset = TimeSpan.FromSeconds(settings.TimeOffset);

        if (settings.TimeOffset < 0)
        {
            JToken firstComment = comments.FirstOrDefault();
            if (firstComment != null)
            {
                int seconds = firstComment.SelectToken("content_offset_seconds").Value<int>();
                if (seconds + settings.TimeOffset < 0)
                    timeOffset = TimeSpan.FromSeconds(-seconds);
            }
        }
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

            return comments.Select(comment => GetProcessComment(comment, regexEmbeddedEmoticons, userColors, settings, ct)).ToArray();
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
            Timestamp = TimeSpan.FromSeconds(comment.SelectToken("content_offset_seconds").Value<int>()),
            User = comment.SelectToken("commenter").SelectToken("display_name").Value<string>(),
            IsModerator = comment.SelectToken("message").SelectToken("user_badges").Any(ub =>
                ub.SelectToken("_id").Value<string>() == "broadcaster" ||
                ub.SelectToken("_id").Value<string>() == "moderator"
            )
        };

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

            SplitMessageBodyForBrailleArt(body);

            if (settings.IsUsingAssaTags == false)
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
                if (settings.InternalTextColor != null)
                    userColor.SearchAndReplace(body, $@"{{\c&{settings.InternalTextColor.BGR}&}}");
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

            return emoticons.Where(x => bodyString.Contains(x.emoticon, StringComparison.OrdinalIgnoreCase)).ToArray();
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

            return userColors.Where(x => bodyString.Contains(x.Value.User, StringComparison.OrdinalIgnoreCase)).ToArray();
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

    private const int REGULAR_LINE_LENGTH = 50;
    private const int BIGGER_LINE_LENGTH = 45;
    private const int BIGGEST_LINE_LENGTH = 40;

    private static int GetBodyLineLength(SubtitlesFontSize subtitlesFontSize)
    {
        if (subtitlesFontSize == SubtitlesFontSize.Regular)
            return REGULAR_LINE_LENGTH;
        else if (subtitlesFontSize == SubtitlesFontSize.Bigger)
            return BIGGER_LINE_LENGTH;
        else /*if (subtitlesFontSize == SubtitlesFontSize.Biggest)*/
            return BIGGEST_LINE_LENGTH;
    }

    private static void SplitMessageBody(StringBuilder body, string user, TimeSpan timestamp, TwitchSubtitlesSettings settings)
    {
        int bodyLineLength = GetBodyLineLength(settings.SubtitlesFontSize);

        int startIndex = 0;
        while (startIndex < body.Length)
        {
            int endIndex = startIndex + bodyLineLength - 1;

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

    private const int BRAILLE_LINE_LENGTH = 30;

    private static (int charsPerLine, int splitCount, int charsInLastLine, int charsMissingInLastLine) GetBestFitBrailleMeasurement(int bodyLength)
    {
        var measurements = new List<(int charsPerLine, int splitCount, int charsInLastLine, int charsMissingInLastLine)>();

        for (int charsPerLine = BRAILLE_LINE_LENGTH - 5; charsPerLine <= BRAILLE_LINE_LENGTH + 5; charsPerLine++)
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

    #region Write Subtitles & Chat Messages

    private static void WriteSubtitles(
        ref Task<int> lastWritingTask,
        IEnumerable<Subtitle> subtitles,
        TwitchSubtitlesSettings settings,
        StreamWriter writer,
        CancellationToken ct)
    {
        WriteMessages(ref lastWritingTask, subtitles, settings, writer, false, ct);
    }

    private static void WriteChatMessages(
        ref Task<int> lastWritingTask,
        IEnumerable<ChatMessage> chatMessages,
        TwitchSubtitlesSettings settings,
        StreamWriter writer,
        CancellationToken ct)
    {
        WriteMessages(ref lastWritingTask, chatMessages, settings, writer, true, ct);
    }

    private static void WriteMessages<TMessage>(
        ref Task<int> lastWritingTask,
        IEnumerable<TMessage> messages,
        TwitchSubtitlesSettings settings,
        StreamWriter writer,
        bool toChatLogString,
        CancellationToken ct)
        where TMessage : IMessage
    {
        if (lastWritingTask != null && lastWritingTask.Exception != null)
            throw lastWritingTask.Exception;

        if (lastWritingTask == null)
            lastWritingTask = Task.Run(() => WriteMessagesAsync(messages, 1, settings, writer, toChatLogString, ct), ct);
        else
            lastWritingTask = lastWritingTask.ContinueWith((previousTask) => WriteMessagesAsync(messages, previousTask.Result, settings, writer, toChatLogString, ct), ct);
    }

    private static int WriteMessagesAsync<TMessage>(
        IEnumerable<TMessage> messages,
        int subsCounter,
        TwitchSubtitlesSettings settings,
        StreamWriter writer,
        bool toChatLogString,
        CancellationToken ct)
        where TMessage : IMessage
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            foreach (var message in messages)
            {
                if (toChatLogString)
                {
                    subsCounter++;
                    writer.WriteLine(message.ToChatLogString(settings));
                }
                else
                {
                    writer.WriteLine(subsCounter++);
                    writer.WriteLine(message.ToString(settings));
                }
            }

            writer.Flush();

            ct.ThrowIfCancellationRequested();

            return subsCounter;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct)
        {
            // swallow the exception
            // and finish the task gracefully
            return subsCounter;
        }
        catch
        {
            throw;
        }
    }

    #endregion
}
