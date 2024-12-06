using System.Reflection;
using System.Text;
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

                Console.Error.WriteLine(GetVersion());
                Console.Error.WriteLine();

                Console.Error.WriteLine("TwitchChatToSubtitles Errors:");
                foreach (Error error in errors)
                {
                    if (error is TokenError tokenError)
                        Console.Error.WriteLine(tokenError.Tag + " " + tokenError.Token);
                    else if (error is NamedError namedError)
                        Console.Error.WriteLine(namedError.Tag + " " + namedError.NameInfo.NameText);
                    else
                        Console.Error.WriteLine(error.Tag);
                }
                Console.Error.WriteLine();
            }
        });
}
catch (ArgumentException ex)
{
    returnCode = -1;
    Console.Error.WriteLine(HandledArgumentException(ex));
}
catch (FileNotFoundException ex)
{
    returnCode = -1;
    Console.Error.WriteLine(HandledArgumentException(ex));
}
catch (Exception ex)
{
    returnCode = -1;
    Console.Error.WriteLine(UnhandledException(ex));
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
        Console.WriteLine("Writing subtitles...");
    };

    twitchSubtitles.Finish += (object sender, EventArgs e) =>
    {
        Console.WriteLine("Finished successfully.");
        Console.WriteLine("Subtitles file " + TwitchSubtitles.GetSubtitlesFileName(options.JsonFile));
    };

    twitchSubtitles.WriteTwitchSubtitles(options.JsonFile);
}

#if DEBUG
// set options here instead of filling Debug tab in project properties
static void Debug(TwitchSubtitlesOptions options)
{
    //options.JsonFile = @"";
    //options.RegularSubtitles = false;
    //options.RollingChatSubtitles = false;
    //options.StaticChatSubtitles = false;
}
#endif

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
    sb.AppendLine(GetHelpText<RollingChatSubtitlesOptions>(parser, args, "Rolling Chat Subtitles", "Chat messages will roll from the bottom to top of the screen and then disappear.", "Past chat messages won't clutter the screen."));
    sb.AppendLine(GetHelpText<StaticChatSubtitlesOptions>(parser, args, "Static Chat Subtitles", "Chat messages are added to the bottom of all the previous chat messages and remain there. Similar to what Twitch chat does."));
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

    if ((value = OrderOptions("JsonFile")) != 0)
        return value;

    if ((value = OrderOptions("RegularSubtitles")) != 0)
        return value;

    if ((value = OrderOptions("RollingChatSubtitles")) != 0)
        return value;

    if ((value = OrderOptions("StaticChatSubtitles")) != 0)
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
