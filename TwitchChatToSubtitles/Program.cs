using CommandLine;
using CommandLine.Text;
using TwitchChatToSubtitles;
using TwitchChatToSubtitles.Library;

Console.OutputEncoding = Encoding.UTF8;
Console.Clear();

int returnCode = Run(args);
//int returnCode = WriteFontSizeTestSubtitles(@"");
//int returnCode = TestMultipleSettings(@"");

#if DEBUG
if (returnCode != 0)
{
    Console.WriteLine("Press any key to continue . . .");
    Console.ReadKey(true);
}
#endif

return returnCode;

static int Run(string[] args)
{
    int returnCode = 0;

    try
    {
        FixSubtitlesXLFontSize(args);

        var parser = GetParser();

        var parserResult = parser.ParseArguments<TwitchSubtitlesOptions>(args);

        parserResult
            .WithParsed(WriteTwitchSubtitles)
            .WithNotParsed(errors =>
            {
                if (errors.IsVersion())
                {
                    Console.WriteLine(GetVersion());
                    Console.WriteLine();
                }
                else if (errors.IsHelp())
                {
                    Console.WriteLine(GetHelp());
                }
                else if (errors.HasAny())
                {
                    returnCode = -1;

                    WriteErrorLine(GetVersion());
                    WriteErrorLine();

                    WriteErrorLine("TwitchChatToSubtitles Errors:");
                    foreach (Error error in errors)
                    {
                        if (error is TokenError tokenError)
                            WriteErrorLine($"{tokenError.Tag} {tokenError.Token}");
                        else if (error is NamedError namedError)
                            WriteErrorLine($"{namedError.Tag} {namedError.NameInfo.NameText}");
                        else
                            WriteErrorLine(error.Tag.ToString());
                    }
                    WriteErrorLine();
                }
            });
    }
    catch (ArgumentException ex)
    {
        returnCode = -1;
        WriteErrorLine(HandledArgumentException(ex));
    }
    catch (FileNotFoundException ex)
    {
        returnCode = -1;
        WriteErrorLine(HandledArgumentException(ex));
    }
    catch (Exception ex)
    {
        returnCode = -1;
        WriteErrorLine(UnhandledException(ex));
    }

    return returnCode;
}

static void FixSubtitlesXLFontSize(string[] args)
{
    if (args.IsNullOrEmpty())
        return;

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];

        var match = RegexSubtitlesXLFontSize().Match(arg);
        if (match.Success)
        {
            args[i] = $"X{match.Groups["Size"]}L";
            return;
        }
    }
}

static void WriteTwitchSubtitles(TwitchSubtitlesOptions options)
{
#if DEBUG
    Debug(options);
#endif

    var twitchSubtitles = new TwitchSubtitles(options.ToSettings());
    SubscribeToEvents(twitchSubtitles);
    twitchSubtitles.WriteTwitchSubtitles(options.JsonFile);
}

static void SubscribeToEvents(TwitchSubtitles twitchSubtitles)
{
    twitchSubtitles.Start += OnStart;
    twitchSubtitles.StartLoadingJsonFile += OnStartLoadingJsonFile;
    twitchSubtitles.FinishLoadingJsonFile += OnFinishLoadingJsonFile;
    twitchSubtitles.StartWritingPreparations += OnStartWritingPreparations;
    twitchSubtitles.FinishWritingPreparations += OnFinishWritingPreparations;
    twitchSubtitles.StartWritingSubtitles += OnStartWritingSubtitles;
    twitchSubtitles.ProgressAsync += OnPrintProgress;
    twitchSubtitles.FinishWritingSubtitles += OnPrintProgress;
    twitchSubtitles.Finish += OnFinish;
    twitchSubtitles.Tracepoint += OnTracepoint;
}

static void OnStart(object sender, EventArgs e)
{
    Console.WriteLine(GetVersion());

    var twitchSubtitles = (TwitchSubtitles)sender;
    if (twitchSubtitles.RegularSubtitles)
        Console.WriteLine("Regular Subtitles.");
    else if (twitchSubtitles.RollingChatSubtitles)
        Console.WriteLine("Rolling Chat Subtitles.");
    else if (twitchSubtitles.StaticChatSubtitles)
        Console.WriteLine("Static Chat Subtitles.");
    else if (twitchSubtitles.ChatTextFile)
        Console.WriteLine("Chat Text File.");
}

static void OnStartLoadingJsonFile(object sender, StartLoadingJsonFileEventArgs e)
{
    Console.WriteLine("Loading JSON file...");
}

static void OnFinishLoadingJsonFile(object sender, FinishLoadingJsonFileEventArgs e)
{
    if (e.Error == null)
    {
        Console.WriteLine("JSON file loaded successfully.");
        Console.WriteLine($"JSON file: {e.JsonFile}");
    }
    else
    {
        WriteErrorLine("Could not load JSON file.");
        WriteErrorLine($"JSON file: {e.JsonFile}");
    }
}

static void OnStartWritingPreparations(object sender, StartWritingPreparationsEventArgs e)
{
    string preparations =
        (e.RemoveEmoticonNames ? "emoticons" : string.Empty) +
        (e.RemoveEmoticonNames && e.ColorUserNames ? ", " : string.Empty) +
        (e.ColorUserNames ? "user colors" : string.Empty);

    Console.WriteLine($"Begin writing preparations ({preparations})...");
}

static void OnFinishWritingPreparations(object sender, FinishWritingPreparationsEventArgs e)
{
    if (e.Error == null)
        Console.WriteLine("Writing preparations finished successfully.");
    else
        WriteErrorLine("Failed to finish writing preparations.");
}

static void OnStartWritingSubtitles(object sender, StartWritingSubtitlesEventArgs e)
{
    leftMessages = 0;
    topMessages = 0;
    leftSubtitles = 0;
    topSubtitles = 0;
    leftFinish = 0;
    topFinish = 0;

    Console.Write("Chat Messages: ");
    leftMessages = Console.CursorLeft;
    topMessages = Console.CursorTop;
    Console.WriteLine("0 / 0");

    var twitchSubtitles = (TwitchSubtitles)sender;
    if (twitchSubtitles.ChatTextFile == false)
    {
        Console.Write("Subtitles: ");
        leftSubtitles = Console.CursorLeft;
        topSubtitles = Console.CursorTop;
        Console.WriteLine("0");
    }

    leftFinish = Console.CursorLeft;
    topFinish = Console.CursorTop;

    Console.CursorVisible = false;
}

static void OnPrintProgress(object sender, ProgressEventArgs e)
{
    int lineLength = 45;

    var strMessages = $"{e.MessagesCount:N0} / {e.TotalMessages:N0}";
    if (e.DiscardedMessagesCount > 0)
        strMessages += $" (discarded messages {e.DiscardedMessagesCount:N0})";
    strMessages += new string(' ', lineLength - strMessages.Length);

    string strSubtitles = null;
    var twitchSubtitles = (TwitchSubtitles)sender;
    if (twitchSubtitles.ChatTextFile == false)
    {
        strSubtitles = $"{e.SubtitlesCount:N0}";
        strSubtitles += new string(' ', lineLength - strSubtitles.Length);
    }

    lock (lockObj)
    {
        Console.SetCursorPosition(leftMessages, topMessages);
        Console.Write(strMessages);

        if (twitchSubtitles.ChatTextFile == false)
        {
            Console.SetCursorPosition(leftSubtitles, topSubtitles);
            Console.Write(strSubtitles);
        }
    }
}

static void OnFinish(object sender, FinishEventArgs e)
{
    Console.CursorVisible = true;

    if (leftFinish != 0 || topFinish != 0)
        Console.SetCursorPosition(leftFinish, topFinish);

    var twitchSubtitles = (TwitchSubtitles)sender;

    if (e.Error == null)
    {
        Console.WriteLine("Finished successfully.");

        if (string.IsNullOrEmpty(e.SrtFile) == false)
        {
            if (twitchSubtitles.ChatTextFile)
                Console.WriteLine($"Chat text file: {e.SrtFile}");
            else
                Console.WriteLine($"Subtitles file: {e.SrtFile}");
        }

        string processTime = e.ProcessTime.ToString(e.ProcessTime.Days > 0 ? "d':'hh':'mm':'ss'.'fff" : e.ProcessTime.Hours > 0 ? "hh':'mm':'ss'.'fff" : "mm':'ss'.'fff");
        Console.WriteLine($"Process Time: {processTime}");
    }
    else
    {
        DeleteFile(e.SrtFile);

#if RELEASE
        if (twitchSubtitles.ChatTextFile)
            WriteErrorLine("Failed to write chat text file.");
        else
            WriteErrorLine("Failed to write subtitles.");
        WriteErrorLine($"Error: {e.Error.Message}");

        Exception ex = e.Error.InnerException;
        while (ex != null)
        {
            WriteErrorLine($"Error: {ex.Message}");
            ex = ex.InnerException;
        }
#elif DEBUG
        if (twitchSubtitles.ChatTextFile)
            WriteErrorLine(e.Error.GetExceptionErrorMessage("Failed to write chat text file."));
        else
            WriteErrorLine(e.Error.GetExceptionErrorMessage("Failed to write subtitles."));
#endif
    }
}

static void OnTracepoint(object sender, TracepointEventArgs e)
{
    ConsoleColor foregroundColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(e.Message);
    Console.ForegroundColor = foregroundColor;
}

#pragma warning disable CS8321 // Local function is declared but never used

#if DEBUG
static void Debug(TwitchSubtitlesOptions options)
{
    //options.JsonFile = @"";
    //options.ASS = true;

    //options.RegularSubtitles = true;
    //options.RollingChatSubtitles = true;
    //options.StaticChatSubtitles = true;
    //options.ChatTextFile = true;

    //options.ColorUserNames = true;
    //options.RemoveEmoticonNames = true;
    //options.ShowTimestamps = true;

    //options.BoldText = true;
    //options.SemiTransparentDarkBackground = true;
    //options.SemiTransparentLightBackground = true;
    //options.TextColor = "#FFFF80";

    //options.TimeOffset = 5;

    //options.SubtitlesFontSize = SubtitlesFontSize.Regular;
    //options.SubtitlesFontSize = SubtitlesFontSize.Medium;
    //options.SubtitlesFontSize = SubtitlesFontSize.Large;
    //options.SubtitlesFontSize = SubtitlesFontSize.XL;
    //options.SubtitlesFontSize = SubtitlesFontSize.X2L;
    //options.SubtitlesFontSize = SubtitlesFontSize.X3L;
    //options.SubtitlesFontSize = SubtitlesFontSize.X4L;
    //options.SubtitlesFontSize = SubtitlesFontSize.X5L;

    //options.SubtitlesLocation = SubtitlesLocation.Left;
    //options.SubtitlesLocation = SubtitlesLocation.Right;

    //options.SubtitlesRollingDirection = SubtitlesRollingDirection.BottomToTop;
    //options.SubtitlesRollingDirection = SubtitlesRollingDirection.TopToBottom;

    //options.SubtitlesSpeed = SubtitlesSpeed.Speed1;
    //options.SubtitlesSpeed = SubtitlesSpeed.Speed2;
    //options.SubtitlesSpeed = SubtitlesSpeed.Speed3;
}
#endif

static int WriteFontSizeTestSubtitles(string jsonFile)
{
    if (string.IsNullOrEmpty(jsonFile))
        throw new ArgumentException("JSON file not specified.");

    if (string.Compare(Path.GetExtension(jsonFile), ".json", true) != 0)
        throw new ArgumentException($"Not a JSON file '{jsonFile}'.");

    string srtFile = Path.Combine(
        Path.GetDirectoryName(jsonFile),
        Path.GetFileNameWithoutExtension(jsonFile) + ".srt"
    );

    Console.WriteLine("Font Size Test Subtitles.");

    int returnCode = 0;

    try
    {
        TwitchSubtitles.WriteFontSizeTestSubtitles(srtFile);
        Console.WriteLine("Finished successfully.");
        Console.WriteLine($"Subtitles file: {srtFile}");
    }
    catch (Exception ex)
    {
        returnCode = -1;

        DeleteFile(srtFile);

        WriteErrorLine("Failed to write subtitles.");
        WriteErrorLine($"Error: {ex.Message}");

        Exception exInner = ex.InnerException;
        while (exInner != null)
        {
            WriteErrorLine($"Error: {exInner.Message}");
            exInner = exInner.InnerException;
        }
    }

    return returnCode;
}

static int TestMultipleSettings(
    string jsonFile,
    bool toLogFile = true,
    bool includeRegularSubtitles = true,
    bool includeRollingChatSubtitles = true,
    bool includeStaticChatSubtitles = true,
    bool includeChatTextFile = true)
{
    var multipleSettings = GetMultipleSettings(includeRegularSubtitles, includeRollingChatSubtitles, includeStaticChatSubtitles, includeChatTextFile).ToArray();

    int counter = 0;
    int totalCount = multipleSettings.Length;
    string directory = Path.GetDirectoryName(jsonFile);
    string fileName = Path.GetFileNameWithoutExtension(jsonFile);
    string logFile = Path.Combine(directory, $"{fileName} Test Results.txt");
    var parser = GetParser();
    List<(TwitchSubtitlesSettings settings, Exception error)> failedSettings = [];

    var twitchSubtitles = new TwitchSubtitles();
    SubscribeToEvents(twitchSubtitles);

    twitchSubtitles.Start += (sender, e) =>
    {
        File.WriteAllText(logFile, null, Encoding.UTF8);
    };

    twitchSubtitles.StartTestingSettings += (sender, e) =>
    {
        Console.Clear();
        Console.WriteLine($"{++counter} / {totalCount}");
        Console.WriteLine(parser.FormatCommandLine(new TwitchSubtitlesOptions(e.Settings)));
    };

    twitchSubtitles.FinishTestingSettings += (sender, e) =>
    {
        if (e.Error != null)
            failedSettings.Add((e.Settings, e.Error));

        if (toLogFile)
        {
            string processTime = e.ProcessTime.ToString(e.ProcessTime.Days > 0 ? "d':'hh':'mm':'ss'.'fff" : e.ProcessTime.Hours > 0 ? "hh':'mm':'ss'.'fff" : "mm':'ss'.'fff");
            File.AppendAllText(logFile, $"[{counter}] {(e.Error == null ? "Pass" : "Fail")} {processTime} {parser.FormatCommandLine(new TwitchSubtitlesOptions(e.Settings))}{Environment.NewLine}{(e.Error != null ? $"{e.Error.GetExceptionErrorMessage()}{Environment.NewLine}" : null)}", Encoding.UTF8);
        }
    };

    twitchSubtitles.Finish += (sender, e) =>
    {
        Console.Clear();

        string processTime = e.ProcessTime.ToString(e.ProcessTime.Days > 0 ? "d':'hh':'mm':'ss'.'fff" : e.ProcessTime.Hours > 0 ? "hh':'mm':'ss'.'fff" : "mm':'ss'.'fff");

        if (failedSettings.IsNullOrEmpty())
        {
            Console.WriteLine($"All {totalCount} settings succeeded.");
            if (toLogFile)
                Console.WriteLine($"Log File: {logFile}");
            Console.WriteLine($"Process Time: {processTime}");
        }
        else
        {
            int failedCount = failedSettings.Count;
            Console.WriteLine($"{totalCount - failedCount} settings succeeded.");
            WriteErrorLine($"{failedCount} settings failed.");
            if (toLogFile)
                Console.WriteLine($"Log File: {logFile}");
            Console.WriteLine($"Process Time: {processTime}");
            Console.WriteLine();

            foreach (var (settings, error) in failedSettings)
            {
                Console.WriteLine(parser.FormatCommandLine(new TwitchSubtitlesOptions(settings)));
                WriteErrorLine(error.GetExceptionErrorMessage());
                Console.WriteLine();
            }
        }

        DeleteFile(Path.Combine(directory, $"{fileName}.srt"));
        DeleteFile(Path.Combine(directory, $"{fileName}.ass"));
        DeleteFile(Path.Combine(directory, $"{fileName}.txt"));
    };

    twitchSubtitles.TestMultipleSettings(jsonFile, multipleSettings);

    Console.WriteLine("Press any key to continue . . .");
    Console.ReadKey(true);

    return (failedSettings.IsNullOrEmpty() ? 0 : -1);
}

static IEnumerable<TwitchSubtitlesSettings> GetMultipleSettings(
    bool includeRegularSubtitles = true,
    bool includeRollingChatSubtitles = true,
    bool includeStaticChatSubtitles = true,
    bool includeChatTextFile = true)
{
    var boolValues = new bool[] { false, true };
    var subtitlesFontSizeValues = Enum.GetValues<SubtitlesFontSize>().Where(x => x != SubtitlesFontSize.None);
    var subtitlesLocationValues = Enum.GetValues<SubtitlesLocation>().Where(x => x != SubtitlesLocation.None && x.ToString().Contains("Right") == false);
    var subtitlesRollingDirectionValues = Enum.GetValues<SubtitlesRollingDirection>().Where(x => x != SubtitlesRollingDirection.None);
    var subtitlesSpeedValues = Enum.GetValues<SubtitlesSpeed>().Where(x => x != SubtitlesSpeed.None);

    var multipleOptions = Enumerable.Empty<TwitchSubtitlesOptions>();

    if (includeRegularSubtitles)
    {
        multipleOptions = multipleOptions.Concat(
            from ASS in boolValues
            from RemoveEmoticonNames in boolValues
            from ShowTimestamps in boolValues
            from SubtitlesFontSize in subtitlesFontSizeValues
            select new TwitchSubtitlesOptions()
            {
                ASS = ASS,
                RegularSubtitles = true,
                RemoveEmoticonNames = RemoveEmoticonNames,
                ShowTimestamps = ShowTimestamps,
                SubtitlesFontSize = SubtitlesFontSize,
            });
    }

    if (includeRollingChatSubtitles)
    {
        multipleOptions = multipleOptions.Concat(
            from ASS in boolValues
            from RemoveEmoticonNames in boolValues
            from ShowTimestamps in boolValues
            from SubtitlesFontSize in subtitlesFontSizeValues
            from SubtitlesLocation in subtitlesLocationValues
            from SubtitlesRollingDirection in subtitlesRollingDirectionValues
            from SubtitlesSpeed in subtitlesSpeedValues
            select new TwitchSubtitlesOptions()
            {
                ASS = ASS,
                RollingChatSubtitles = true,
                RemoveEmoticonNames = RemoveEmoticonNames,
                ShowTimestamps = ShowTimestamps,
                SubtitlesFontSize = SubtitlesFontSize,
                SubtitlesLocation = SubtitlesLocation,
                SubtitlesRollingDirection = SubtitlesRollingDirection,
                SubtitlesSpeed = SubtitlesSpeed,
            });
    }

    if (includeStaticChatSubtitles)
    {
        multipleOptions = multipleOptions.Concat(
            from ASS in boolValues
            from RemoveEmoticonNames in boolValues
            from ShowTimestamps in boolValues
            from SubtitlesFontSize in subtitlesFontSizeValues
            from SubtitlesLocation in subtitlesLocationValues
            from SubtitlesRollingDirection in subtitlesRollingDirectionValues
            select new TwitchSubtitlesOptions()
            {
                ASS = ASS,
                StaticChatSubtitles = true,
                RemoveEmoticonNames = RemoveEmoticonNames,
                ShowTimestamps = ShowTimestamps,
                SubtitlesFontSize = SubtitlesFontSize,
                SubtitlesLocation = SubtitlesLocation,
                SubtitlesRollingDirection = SubtitlesRollingDirection,
            });
    }

    if (includeChatTextFile)
    {
        multipleOptions = multipleOptions.Concat(
            from RemoveEmoticonNames in boolValues
            select new TwitchSubtitlesOptions()
            {
                ChatTextFile = true,
                RemoveEmoticonNames = RemoveEmoticonNames,
                ShowTimestamps = true
            });
    }

    return multipleOptions.Select(option => option.ToSettings());
}

#pragma warning restore CS8321 // Local function is declared but never used

static void WriteErrorLine(string line = null)
{
    ConsoleColor foregroundColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(line);
    Console.ForegroundColor = foregroundColor;
}

static void DeleteFile(string path)
{
    if (string.IsNullOrEmpty(path))
        return;

    try
    {
        if (File.Exists(path))
        {
#if WINDOWS_BUILD
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,
                    Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException
                );

                return;
            }
            catch { }
#endif

            File.Delete(path);
        }
    }
    catch { }
}

static Parser GetParser()
{
    return new Parser(with =>
    {
        with.CaseSensitive = false;
        with.IgnoreUnknownArguments = true;
        with.HelpWriter = null;
    });
}

static string GetHelp()
{
    return GetHelpMessage(
        GetParser(),
        ["--help"]
    );
}

static string GetHelpMessage(Parser parser, string[] args)
{
    var sb = new StringBuilder();
    sb.AppendLine(GetHelpText<AllSubtitleTypesOptions>(parser, args, GetVersion()));
    sb.AppendLine(GetHelpText<RegularSubtitlesOptions>(parser, args, "Regular Subtitles", "Chat messages will appear at the center-bottom of the screen."));
    sb.AppendLine(GetHelpText<RollingChatSubtitlesOptions>(parser, args, "Rolling Chat Subtitles", "Chat messages will roll vertically across the screen and then disappear.", "Past chat messages won't clutter the screen."));
    sb.AppendLine(GetHelpText<StaticChatSubtitlesOptions>(parser, args, "Static Chat Subtitles", "Chat messages are added to the previous chat messages and remain there. Similar to what Twitch chat does."));
    sb.AppendLine(GetHelpText<ChatTextFileOptions>(parser, args, "Chat Text File", "Save Twitch chat to a text file."));
    return sb.ToString();
}

static string GetHelpText<T>(Parser parser, string[] args, string heading, params string[] descriptionLines)
{
    return HelpText.AutoBuild(
        parser.ParseArguments<T>(args),
        h =>
        {
            bool addUnderline = false;

            if (descriptionLines.HasAny())
            {
                h.AddPreOptionsLines(descriptionLines);
                addUnderline = true;
            }

            if (string.IsNullOrEmpty(heading))
                h.Heading = string.Empty;
            else
                h.Heading = heading + (addUnderline ? Environment.NewLine + new string('-', heading.Length) : null);

            h.Copyright = string.Empty;
            h.AdditionalNewLineAfterOption = false;
            h.MaximumDisplayWidth = 120;
            h.AddNewLineBetweenHelpSections = true;
            h.AddDashesToOption = true;
            h.AutoHelp = false;
            h.AutoVersion = false;
            h.OptionComparison = CompareOptions;
            return h;
        },
        e => e
    );
}

static int CompareOptions(ComparableOption attr1, ComparableOption attr2)
{
    int OrderOptions(string longName)
    {
        if (attr1.LongName == longName)
            return -1;
        if (attr2.LongName == longName)
            return 1;
        return 0;
    }

    int value = 0;

    if ((value = OrderOptions("RegularSubtitles")) != 0)
        return value;

    if ((value = OrderOptions("RollingChatSubtitles")) != 0)
        return value;

    if ((value = OrderOptions("StaticChatSubtitles")) != 0)
        return value;

    if ((value = OrderOptions("ChatTextFile")) != 0)
        return value;

    if ((value = OrderOptions("JsonFile")) != 0)
        return value;

    return attr1.LongName.CompareTo(attr2.LongName);
}

static string GetVersion()
{
    return $"TwitchChatToSubtitles, Version {Assembly.GetExecutingAssembly().GetName().Version.ToString(2)}";
}

static string HandledArgumentException(Exception ex)
{
    var errorMessage = new StringBuilder();

    var assemblyName = Assembly.GetExecutingAssembly().GetName();
    errorMessage.AppendLine($"Error - {assemblyName.Name} {assemblyName.Version.ToString(2)}");
    errorMessage.AppendLine();
    errorMessage.AppendLine(ex.Message);

    return errorMessage.ToString();
}

static string UnhandledException(Exception ex)
{
    var errorMessage = new StringBuilder();

    var assemblyName = Assembly.GetExecutingAssembly().GetName();
    errorMessage.AppendLine($"Unhandled Error - {assemblyName.Name} {assemblyName.Version.ToString(2)}");

    try
    {
        errorMessage.AppendLine(ex.GetUnhandledExceptionErrorMessage());
    }
    catch
    {
        while (ex != null)
        {
            errorMessage.AppendLine();
            errorMessage.AppendLine($"ERROR TYPE: {ex.GetType()}");
            errorMessage.AppendLine($"ERROR: {ex.Message}");

            ex = ex.InnerException;
        }
    }

    return errorMessage.ToString();
}

partial class Program
{
    [GeneratedRegex(@"^(?<Size>[0-9])XL$")]
    private static partial Regex RegexSubtitlesXLFontSize();

    private static int leftMessages = 0;
    private static int topMessages = 0;
    private static int leftSubtitles = 0;
    private static int topSubtitles = 0;
    private static int leftFinish = 0;
    private static int topFinish = 0;

    private static readonly object lockObj = new();
}