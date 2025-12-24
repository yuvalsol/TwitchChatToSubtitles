using CommandLine;
using CommandLine.Text;
using TwitchChatToSubtitles;
using TwitchChatToSubtitles.Library;

Console.OutputEncoding = Encoding.UTF8;
Console.Clear();

int returnCode = Run(args);

#if DEBUG
//int returnCode = WriteFontSizeTestSubtitles(@"");
//int returnCode = TestMultipleOptions(@"");

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
                            WriteErrorLine(tokenError.Tag + " " + tokenError.Token);
                        else if (error is NamedError namedError)
                            WriteErrorLine(namedError.Tag + " " + namedError.NameInfo.NameText);
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

    var settings = options.ToSettings();
    var twitchSubtitles = new TwitchSubtitles(settings);

    twitchSubtitles.Start += (sender, e) =>
    {
        Console.WriteLine(GetVersion());

        if (settings.RegularSubtitles)
            Console.WriteLine("Regular Subtitles.");
        else if (settings.RollingChatSubtitles)
            Console.WriteLine("Rolling Chat Subtitles.");
        else if (settings.StaticChatSubtitles)
            Console.WriteLine("Static Chat Subtitles.");
        else if (settings.ChatTextFile)
            Console.WriteLine("Chat Text File.");
    };

    twitchSubtitles.StartLoadingJsonFile += (sender, e) =>
    {
        Console.WriteLine("Loading JSON file...");
    };

    twitchSubtitles.FinishLoadingJsonFile += (sender, e) =>
    {
        if (e.Error == null)
        {
            Console.WriteLine("JSON file loaded successfully.");
            Console.WriteLine("JSON file: " + e.JsonFile);
        }
        else
        {
            WriteErrorLine("Could not load JSON file.");
            WriteErrorLine("JSON file: " + e.JsonFile);
        }
    };

    twitchSubtitles.StartWritingPreparations += (sender, e) =>
    {
        string preparations =
            (e.RemoveEmoticonNames ? "emoticons" : string.Empty) +
            (e.RemoveEmoticonNames && e.ColorUserNames ? ", " : string.Empty) +
            (e.ColorUserNames ? "user colors" : string.Empty);

        Console.WriteLine($"Begin writing preparations ({preparations})...");
    };

    twitchSubtitles.FinishWritingPreparations += (sender, e) =>
    {
        if (e.Error == null)
            Console.WriteLine("Writing preparations finished successfully.");
        else
            WriteErrorLine("Failed to finish writing preparations.");
    };

    int leftMessages = 0;
    int topMessages = 0;

    int leftSubtitles = 0;
    int topSubtitles = 0;

    int leftFinish = 0;
    int topFinish = 0;

    twitchSubtitles.StartWritingSubtitles += (sender, e) =>
    {
        Console.Write("Chat Messages: ");
        leftMessages = Console.CursorLeft;
        topMessages = Console.CursorTop;
        Console.WriteLine("0 / 0");

        if (settings.ChatTextFile == false)
        {
            Console.Write("Subtitles: ");
            leftSubtitles = Console.CursorLeft;
            topSubtitles = Console.CursorTop;
            Console.WriteLine("0");
        }

        leftFinish = Console.CursorLeft;
        topFinish = Console.CursorTop;

        Console.CursorVisible = false;
    };

    var lineLength = 45;
    var lockObj = new object();

    void PrintProgress(object sender, ProgressEventArgs e)
    {
        var strMessages = $"{e.MessagesCount:N0} / {e.TotalMessages:N0}";
        if (e.DiscardedMessagesCount > 0)
            strMessages += $" (discarded messages {e.DiscardedMessagesCount:N0})";
        strMessages += new string(' ', lineLength - strMessages.Length);

        string strSubtitles = null;
        if (settings.ChatTextFile == false)
        {
            strSubtitles = $"{e.SubtitlesCount:N0}";
            strSubtitles += new string(' ', lineLength - strSubtitles.Length);
        }

        lock (lockObj)
        {
            Console.SetCursorPosition(leftMessages, topMessages);
            Console.Write(strMessages);

            if (settings.ChatTextFile == false)
            {
                Console.SetCursorPosition(leftSubtitles, topSubtitles);
                Console.Write(strSubtitles);
            }
        }
    }

    twitchSubtitles.ProgressAsync += PrintProgress;
    twitchSubtitles.FinishWritingSubtitles += PrintProgress;

    twitchSubtitles.Finish += (sender, e) =>
    {
        Console.CursorVisible = true;

        if (leftFinish != 0 || topFinish != 0)
            Console.SetCursorPosition(leftFinish, topFinish);

        if (e.Error == null)
        {
            Console.WriteLine("Finished successfully.");

            if (settings.ChatTextFile)
                Console.WriteLine("Chat text file: " + e.SrtFile);
            else
                Console.WriteLine("Subtitles file: " + e.SrtFile);

            string processTime = e.ProcessTime.ToString(e.ProcessTime.Days > 0 ? "d':'hh':'mm':'ss'.'fff" : e.ProcessTime.Hours > 0 ? "h':'mm':'ss'.'fff" : "m':'ss'.'fff");
            Console.WriteLine("Process Time: " + processTime);
        }
        else
        {
            DeleteFile(e.SrtFile);

#if RELEASE
            if (settings.ChatTextFile)
                WriteErrorLine("Failed to write chat text file.");
            else
                WriteErrorLine("Failed to write subtitles.");
            WriteErrorLine("Error: " + e.Error.Message);

            Exception ex = e.Error.InnerException;
            while (ex != null)
            {
                WriteErrorLine("Error: " + ex.Message);
                ex = ex.InnerException;
            }
#elif DEBUG
            if (settings.ChatTextFile)
                WriteErrorLine(e.Error.GetExceptionErrorMessage("Failed to write chat text file."));
            else
                WriteErrorLine(e.Error.GetExceptionErrorMessage("Failed to write subtitles."));
#endif
        }
    };

    twitchSubtitles.Tracepoint += (sender, e) =>
    {
        ConsoleColor foregroundColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(e.Message);
        Console.ForegroundColor = foregroundColor;
    };

    twitchSubtitles.WriteTwitchSubtitles(options.JsonFile);
}

#if DEBUG
#pragma warning disable CS8321 // Local function is declared but never used

static void Debug(TwitchSubtitlesOptions options)
{
    //options.JsonFile = @"";
    //options.ASS = false;

    //options.ChatTextFile = true;
    //options.RegularSubtitles = true;
    //options.RollingChatSubtitles = true;
    //options.StaticChatSubtitles = true;

    //options.ColorUserNames = true;
    //options.RemoveEmoticonNames = true;
    //options.ShowTimestamps = true;

    //options.TextColor = "#FFFF80";
    //options.TextColor = null;

    //options.TimeOffset = 5;
    //options.TimeOffset = 0;

    //options.SubtitlesFontSize = SubtitlesFontSize.X5L;
    //options.SubtitlesFontSize = SubtitlesFontSize.X4L;
    //options.SubtitlesFontSize = SubtitlesFontSize.X3L;
    //options.SubtitlesFontSize = SubtitlesFontSize.X2L;
    //options.SubtitlesFontSize = SubtitlesFontSize.XL;
    //options.SubtitlesFontSize = SubtitlesFontSize.Large;
    //options.SubtitlesFontSize = SubtitlesFontSize.Medium;
    //options.SubtitlesFontSize = SubtitlesFontSize.Regular;

    //options.SubtitlesLocation = SubtitlesLocation.Right;
    //options.SubtitlesLocation = SubtitlesLocation.Left;

    //options.SubtitlesRollingDirection = SubtitlesRollingDirection.TopToBottom;
    //options.SubtitlesRollingDirection = SubtitlesRollingDirection.BottomToTop;

    //options.SubtitlesSpeed = SubtitlesSpeed.Fastest;
    //options.SubtitlesSpeed = SubtitlesSpeed.Faster;
    //options.SubtitlesSpeed = SubtitlesSpeed.Regular;
}

static int WriteFontSizeTestSubtitles(string jsonFile)
{
    if (string.IsNullOrEmpty(jsonFile))
        throw new ArgumentException("JSON file not specified.");

    if (string.Compare(Path.GetExtension(jsonFile), ".json", true) != 0)
        throw new ArgumentException("Not a JSON file '" + jsonFile + "'.");

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
        Console.WriteLine("Subtitles file: " + srtFile);
    }
    catch (Exception ex)
    {
        returnCode = -1;

        DeleteFile(srtFile);

        WriteErrorLine("Failed to write subtitles.");
        WriteErrorLine("Error: " + ex.Message);

        Exception exInner = ex.InnerException;
        while (exInner != null)
        {
            WriteErrorLine("Error: " + exInner.Message);
            exInner = exInner.InnerException;
        }
    }

    return returnCode;
}

static int TestMultipleOptions(string jsonFile)
{
    long startTime = Stopwatch.GetTimestamp();

    var multipleOptions = GetMultipleOptions(jsonFile);
    var totalCount = multipleOptions.Count();

    int count = 1;
    List<(TwitchSubtitlesOptions options, Exception ex)> failedOptions = [];

    foreach (var options in multipleOptions)
    {
        Console.Clear();
        Console.WriteLine($"{count} / {totalCount}");

        try
        {
            WriteTwitchSubtitles(options);
        }
        catch (Exception ex)
        {
            failedOptions.Add(new(options, ex));
        }
        finally
        {
            count++;
        }
    }

    TimeSpan processTime = Stopwatch.GetElapsedTime(startTime);

    Console.Clear();

    int returnCode = 0;

    if (failedOptions.IsNullOrEmpty())
    {
        Console.WriteLine($"All {totalCount} options succeeded");
    }
    else
    {
        returnCode = -1;

        Console.WriteLine($"{failedOptions.Count} option{(failedOptions.Count == 1 ? string.Empty : "s")} failed");
        Console.WriteLine();

        var parser = GetParser();

        foreach (var (options, ex) in failedOptions)
        {
            Console.WriteLine(parser.FormatCommandLine(options));
            Console.WriteLine();
            WriteErrorLine(UnhandledException(ex));
            Console.WriteLine();
            Console.WriteLine(new string('-', 30));
            Console.WriteLine();
        }
    }

    Console.WriteLine("Process Time: " + processTime.ToString(processTime.Days > 0 ? "d':'hh':'mm':'ss'.'fff" : processTime.Hours > 0 ? "h':'mm':'ss'.'fff" : "m':'ss'.'fff"));

    return returnCode;
}

static IEnumerable<TwitchSubtitlesOptions> GetMultipleOptions(string JsonFile)
{
    var boolValues = new bool[] { false, true };
    var subtitlesFontSizeValues = Enum.GetValues<SubtitlesFontSize>().Where(x => x != SubtitlesFontSize.None);
    var subtitlesLocationValues = Enum.GetValues<SubtitlesLocation>().Where(x => x != SubtitlesLocation.None && x.ToString().Contains("Right") == false);
    var subtitlesRollingDirectionValues = Enum.GetValues<SubtitlesRollingDirection>().Where(x => x != SubtitlesRollingDirection.None);
    var subtitlesSpeedValues = Enum.GetValues<SubtitlesSpeed>().Where(x => x != SubtitlesSpeed.None);

    var regularSubtitlesOptions =
        from ASS in boolValues
        from RemoveEmoticonNames in boolValues
        from ShowTimestamps in boolValues
        from SubtitlesFontSize in subtitlesFontSizeValues
        select new TwitchSubtitlesOptions()
        {
            JsonFile = JsonFile,
            ASS = ASS,
            RegularSubtitles = true,
            RemoveEmoticonNames = RemoveEmoticonNames,
            ShowTimestamps = ShowTimestamps,
            SubtitlesFontSize = SubtitlesFontSize,
        };

    var rollingChatSubtitlesOptions =
        from ASS in boolValues
        from RemoveEmoticonNames in boolValues
        from ShowTimestamps in boolValues
        from SubtitlesFontSize in subtitlesFontSizeValues
        from SubtitlesLocation in subtitlesLocationValues
        from SubtitlesRollingDirection in subtitlesRollingDirectionValues
        from SubtitlesSpeed in subtitlesSpeedValues
        select new TwitchSubtitlesOptions()
        {
            JsonFile = JsonFile,
            ASS = ASS,
            RollingChatSubtitles = true,
            RemoveEmoticonNames = RemoveEmoticonNames,
            ShowTimestamps = ShowTimestamps,
            SubtitlesFontSize = SubtitlesFontSize,
            SubtitlesLocation = SubtitlesLocation,
            SubtitlesRollingDirection = SubtitlesRollingDirection,
            SubtitlesSpeed = SubtitlesSpeed,
        };

    var staticChatSubtitlesOptions =
        from ASS in boolValues
        from RemoveEmoticonNames in boolValues
        from ShowTimestamps in boolValues
        from SubtitlesFontSize in subtitlesFontSizeValues
        from SubtitlesLocation in subtitlesLocationValues
        from SubtitlesRollingDirection in subtitlesRollingDirectionValues
        select new TwitchSubtitlesOptions()
        {
            JsonFile = JsonFile,
            ASS = ASS,
            StaticChatSubtitles = true,
            RemoveEmoticonNames = RemoveEmoticonNames,
            ShowTimestamps = ShowTimestamps,
            SubtitlesFontSize = SubtitlesFontSize,
            SubtitlesLocation = SubtitlesLocation,
            SubtitlesRollingDirection = SubtitlesRollingDirection,
        };

    var chatTextFileOptions =
        from RemoveEmoticonNames in boolValues
        select new TwitchSubtitlesOptions()
        {
            JsonFile = JsonFile,
            ChatTextFile = true,
            RemoveEmoticonNames = RemoveEmoticonNames,
            ShowTimestamps = true
        };

    return
        regularSubtitlesOptions
        .Concat(rollingChatSubtitlesOptions)
        .Concat(staticChatSubtitlesOptions)
        .Concat(chatTextFileOptions);
}

#pragma warning restore CS8321 // Local function is declared but never used
#endif

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
    return
        "TwitchChatToSubtitles, Version" + " " +
        Assembly.GetExecutingAssembly().GetName().Version.ToString(2);
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
}