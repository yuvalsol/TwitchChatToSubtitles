using CommandLine;
using CommandLine.Text;
using TwitchChatToSubtitles;
using TwitchChatToSubtitles.Library;

int returnCode = 0;

Console.OutputEncoding = Encoding.UTF8;

try
{
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
    Console.WriteLine("Press any key to continue . . .");
    Console.ReadKey(true);
}
catch (FileNotFoundException ex)
{
    returnCode = -1;
    WriteErrorLine(HandledArgumentException(ex));
    Console.WriteLine("Press any key to continue . . .");
    Console.ReadKey(true);
}
catch (Exception ex)
{
    returnCode = -1;
    WriteErrorLine(UnhandledException(ex));
    Console.WriteLine("Press any key to continue . . .");
    Console.ReadKey(true);
}
finally
{
    if (System.Diagnostics.Debugger.IsAttached)
    {
        Console.WriteLine("Press any key to continue . . .");
        Console.ReadKey(true);
    }
}

return returnCode;

static void WriteTwitchSubtitles(TwitchSubtitlesOptions options)
{
#if DEBUG
    Debug(options);
#endif

    var settings = options.ToSettings();
    var twitchSubtitles = new TwitchSubtitles(settings);

    twitchSubtitles.Start += (object sender, EventArgs e) =>
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

    twitchSubtitles.StartLoadingJsonFile += (object sender, StartLoadingJsonFileEventArgs e) =>
    {
        Console.WriteLine("Loading JSON file...");
    };

    twitchSubtitles.FinishLoadingJsonFile += (object sender, FinishLoadingJsonFileEventArgs e) =>
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

    twitchSubtitles.StartWritingPreparations += (object sender, StartWritingPreparationsEventArgs e) =>
    {
        string preparations =
            (e.RemoveEmoticonNames ? "emoticons" : string.Empty) +
            (e.RemoveEmoticonNames && e.ColorUserNames ? ", " : string.Empty) +
            (e.ColorUserNames ? "user colors" : string.Empty);

        Console.WriteLine($"Begin writing preparations ({preparations})...");
    };

    twitchSubtitles.FinishWritingPreparations += (object sender, FinishWritingPreparationsEventArgs e) =>
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

    twitchSubtitles.StartWritingSubtitles += (object sender, StartWritingSubtitlesEventArgs e) =>
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

    twitchSubtitles.Finish += (object sender, FinishEventArgs e) =>
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

#if DEBUG
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
#endif
        }
        else
        {
            try
            {
                if (File.Exists(e.SrtFile))
                    File.Delete(e.SrtFile);
            }
            catch { }

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

            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
        }
    };

    twitchSubtitles.Tracepoint += (object sender, TracepointEventArgs e) =>
    {
        ConsoleColor foregroundColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(e.Message);
        Console.ForegroundColor = foregroundColor;
    };

    twitchSubtitles.WriteTwitchSubtitles(options.JsonFile);
}

#if DEBUG
// set options here instead of filling Debug tab in project properties
static void Debug(TwitchSubtitlesOptions options)
{
    //options.JsonFile = @"";
    //options.ChatTextFile = false;
    //options.StaticChatSubtitles = false;
    //options.RollingChatSubtitles = true;
    //options.RegularSubtitles = false;
    //options.ColorUserNames = true;
    //options.RemoveEmoticonNames = true;
    //options.ShowTimestamps = true;
    //options.SubtitlesFontSize = SubtitlesFontSize.Bigger;
    //options.SubtitlesRollingDirection = SubtitlesRollingDirection.BottomToTop;
    //options.SubtitlesSpeed = SubtitlesSpeed.Regular;
    //options.SubtitlesLocation = SubtitlesLocation.Left;
}
#endif

static void WriteErrorLine(string line = null)
{
    ConsoleColor foregroundColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(line);
    Console.ForegroundColor = foregroundColor;
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
