using CommandLine;
using CommandLine.Text;
using TwitchChatToSubtitles.Library;

namespace TwitchChatToSubtitles;

#region Option Interfaces

internal interface IAllSubtitleTypesOptions
{
    [Option("JsonFile", Required = false, HelpText = "Path to Twitch chat JSON file.")]
    string JsonFile { get; set; }

    [Option("ass", Required = false, HelpText = "Whether to write Advanced Sub Station Alpha (.ass) file.")]
    bool ASS { get; set; }

    [Option("BoldText", Required = false, HelpText = "Whether the font of the subtitles text is bold.")]
    bool BoldText { get; set; }

    [Option("ColorUserNames", Required = false, HelpText = "Whether to color user names.")]
    bool ColorUserNames { get; set; }

    [Option("RemoveEmoticonNames", Required = false, HelpText = "Remove emoticon and badge names.")]
    bool RemoveEmoticonNames { get; set; }

    [Option("ShowTimestamps", Required = false, HelpText = "Whether to show chat message timestamps.")]
    bool ShowTimestamps { get; set; }

    [Option("SubtitlesFontSize", Required = false, HelpText = "The font size of the subtitles. Valid values: Regular, Medium, Large, XL, 2XL, 3XL, 4XL, 5XL.")]
    SubtitlesFontSize SubtitlesFontSize { get; set; }

    [Option("TextColor", Required = false, HelpText = "The color of the subtitles text.")]
    string TextColor { get; set; }

    [Option("TimeOffset", Required = false, HelpText = "Time offset for all subtitles, in seconds.")]
    int TimeOffset { get; set; }
}

internal interface IRegularSubtitlesOptions
{
    [Option("RegularSubtitles", Required = false, HelpText = "Convert Twitch chat to regular subtitles.")]
    bool RegularSubtitles { get; set; }

    [Option("SubtitleShowDuration", Default = 5, Required = false, HelpText = "For how long a subtitle is visible on the screen, in seconds.")]
    int SubtitleShowDuration { get; set; }
}

internal interface IRollingChatSubtitlesOptions
{
    [Option("RollingChatSubtitles", Required = false, HelpText = "Convert Twitch chat to rolling chat subtitles.")]
    bool RollingChatSubtitles { get; set; }

    [Option("SubtitlesSpeed", Required = false, HelpText = "How fast the subtitles roll. Valid values: Speed1, Speed2, Speed3, Speed4, Speed5, Speed6, Speed7. Speed1 is 1 second. Speed2 is 0.5 second. Speed6 is 100 milliseconds. Speed7 is 50 milliseconds.")]
    SubtitlesSpeed SubtitlesSpeed { get; set; }
}

internal interface IStaticChatSubtitlesOptions
{
    [Option("StaticChatSubtitles", Required = false, HelpText = "Convert Twitch chat to static chat subtitles.")]
    bool StaticChatSubtitles { get; set; }
}

internal interface IChatSubtitlesOptions
{
    [Option("SubtitlesLocation", Required = false, HelpText = "The location of the subtitles on the screen. Valid values: Left, LeftTopHalf, LeftBottomHalf, LeftTopTwoThirds, LeftBottomTwoThirds, Right, RightTopHalf, RightBottomHalf, RightTopTwoThirds, RightBottomTwoThirds.")]
    SubtitlesLocation SubtitlesLocation { get; set; }

    [Option("SubtitlesRollingDirection", Required = false, HelpText = "The direction that the subtitles roll. Valid values: BottomToTop, TopToBottom.")]
    SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }
}

internal interface IChatTextFileOptions
{
    [Option("ChatTextFile", Required = false, HelpText = "Save Twitch chat to a text file.")]
    bool ChatTextFile { get; set; }
}

#endregion

#region Option Classes

internal class RegularSubtitlesOptions : IRegularSubtitlesOptions
{
    public bool RegularSubtitles { get; set; }
    public int SubtitleShowDuration { get; set; }

#if WINDOWS_BUILD
    [Usage(ApplicationAlias = "TwitchChatToSubtitles.exe")]
#elif LINUX_BUILD
    [Usage(ApplicationAlias = "./TwitchChatToSubtitles")]
#else
    [Usage(ApplicationAlias = "TwitchChatToSubtitles")]
#endif
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example(
                "1. Default usage",
                new TwitchSubtitlesOptions
                {
                    RegularSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json"
                }
            );

            yield return new Example(
                "2. Color user names, remove emoticon names, font size `Medium`, subtitles will use ASSA tags",
                new TwitchSubtitlesOptions
                {
                    RegularSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    ColorUserNames = true,
                    RemoveEmoticonNames = true,
                    SubtitlesFontSize = SubtitlesFontSize.Medium
                }
            );

            yield return new Example(
                "3. The following subset of options create a subtitles file without any ASSA tags",
                new TwitchSubtitlesOptions
                {
                    RegularSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    RemoveEmoticonNames = true,
                    SubtitleShowDuration = 7,
                    TimeOffset = 5
                }
            );
        }
    }
}

internal class RollingChatSubtitlesOptions : IRollingChatSubtitlesOptions, IChatSubtitlesOptions
{
    public bool RollingChatSubtitles { get; set; }
    public SubtitlesLocation SubtitlesLocation { get; set; }
    public SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }

#if WINDOWS_BUILD
    [Usage(ApplicationAlias = "TwitchChatToSubtitles.exe")]
#elif LINUX_BUILD
    [Usage(ApplicationAlias = "./TwitchChatToSubtitles")]
#else
    [Usage(ApplicationAlias = "TwitchChatToSubtitles")]
#endif
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example(
                "1. Default usage",
                new TwitchSubtitlesOptions
                {
                    RollingChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json"
                }
            );

            yield return new Example(
                "2. Color user names, remove emoticon names, font size `Medium`, subtitles location will default to whole `Left` side of the screen",
                new TwitchSubtitlesOptions
                {
                    RollingChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    ColorUserNames = true,
                    RemoveEmoticonNames = true,
                    SubtitlesFontSize = SubtitlesFontSize.Medium
                }
            );

            yield return new Example(
                "3. Subtitles will roll faster than regular speed and will appear on the right side and top half of the screen",
                new TwitchSubtitlesOptions
                {
                    RollingChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    SubtitlesLocation = SubtitlesLocation.RightTopHalf,
                    SubtitlesSpeed = SubtitlesSpeed.Speed2
                }
            );
        }
    }
}

internal class StaticChatSubtitlesOptions : IStaticChatSubtitlesOptions, IChatSubtitlesOptions
{
    public bool StaticChatSubtitles { get; set; }
    public SubtitlesLocation SubtitlesLocation { get; set; }
    public SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }

#if WINDOWS_BUILD
    [Usage(ApplicationAlias = "TwitchChatToSubtitles.exe")]
#elif LINUX_BUILD
    [Usage(ApplicationAlias = "./TwitchChatToSubtitles")]
#else
    [Usage(ApplicationAlias = "TwitchChatToSubtitles")]
#endif
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example(
                "1. Default usage",
                new TwitchSubtitlesOptions
                {
                    StaticChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json"
                }
            );

            yield return new Example(
                "2. Color user names, remove emoticon names, font size `Medium`, subtitles location will default to whole `Left` side of the screen",
                new TwitchSubtitlesOptions
                {
                    StaticChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    ColorUserNames = true,
                    RemoveEmoticonNames = true,
                    SubtitlesFontSize = SubtitlesFontSize.Medium
                }
            );

            yield return new Example(
                "3. Subtitles will appear on the left side and top two-thirds of the screen",
                new TwitchSubtitlesOptions
                {
                    StaticChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    SubtitlesLocation = SubtitlesLocation.LeftTopTwoThirds
                }
            );
        }
    }
}

internal class ChatTextFileOptions : IChatTextFileOptions
{
    public bool ChatTextFile { get; set; }

#if WINDOWS_BUILD
    [Usage(ApplicationAlias = "TwitchChatToSubtitles.exe")]
#elif LINUX_BUILD
    [Usage(ApplicationAlias = "./TwitchChatToSubtitles")]
#else
    [Usage(ApplicationAlias = "TwitchChatToSubtitles")]
#endif
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example(
                "1. Default usage",
                new TwitchSubtitlesOptions
                {
                    ChatTextFile = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json"
                }
            );

            yield return new Example(
                "2. Remove emoticon names, show chat message timestamps",
                new TwitchSubtitlesOptions
                {
                    ChatTextFile = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    RemoveEmoticonNames = true,
                    ShowTimestamps = true
                }
            );
        }
    }
}

internal class AllSubtitleTypesOptions : IAllSubtitleTypesOptions
{
    public string JsonFile { get; set; }
    public bool ASS { get; set; }
    public bool BoldText { get; set; }
    public bool ColorUserNames { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ShowTimestamps { get; set; }
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public string TextColor { get; set; }
    public int TimeOffset { get; set; }
}

#endregion

#region Twitch Subtitles Options

internal partial class TwitchSubtitlesOptions
    : IAllSubtitleTypesOptions
    , IRegularSubtitlesOptions
    , IRollingChatSubtitlesOptions
    , IStaticChatSubtitlesOptions
    , IChatSubtitlesOptions
    , IChatTextFileOptions
{
    public string JsonFile { get; set; }
    public bool ASS { get; set; }

    private bool regularSubtitles;
    public bool RegularSubtitles
    {
        get => regularSubtitles;
        set => SetSubtitlesType(ref regularSubtitles, value);
    }

    private bool rollingChatSubtitles;
    public bool RollingChatSubtitles
    {
        get => rollingChatSubtitles;
        set => SetSubtitlesType(ref rollingChatSubtitles, value);
    }

    private bool staticChatSubtitles;
    public bool StaticChatSubtitles
    {
        get => staticChatSubtitles;
        set => SetSubtitlesType(ref staticChatSubtitles, value);
    }

    private bool chatTextFile;
    public bool ChatTextFile
    {
        get => chatTextFile;
        set => SetSubtitlesType(ref chatTextFile, value);
    }

    private void SetSubtitlesType(ref bool currentValue, bool value)
    {
        if (currentValue == value)
            return;

        if (value)
        {
            regularSubtitles = false;
            rollingChatSubtitles = false;
            staticChatSubtitles = false;
            chatTextFile = false;
        }

        currentValue = value;
    }

    public bool BoldText { get; set; }
    public bool ColorUserNames { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ShowTimestamps { get; set; }
    public int SubtitleShowDuration { get; set; }
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public SubtitlesLocation SubtitlesLocation { get; set; }
    public SubtitlesRollingDirection SubtitlesRollingDirection { get; set; }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public string TextColor { get; set; }
    public int TimeOffset { get; set; }

    public TwitchSubtitlesOptions()
    { }

    public TwitchSubtitlesOptions(TwitchSubtitlesSettings settings)
    {
        ASS = settings.ASS;
        RegularSubtitles = settings.RegularSubtitles;
        RollingChatSubtitles = settings.RollingChatSubtitles;
        StaticChatSubtitles = settings.StaticChatSubtitles;
        ChatTextFile = settings.ChatTextFile;
        BoldText = settings.BoldText;
        ColorUserNames = settings.ColorUserNames;
        RemoveEmoticonNames = settings.RemoveEmoticonNames;
        ShowTimestamps = settings.ShowTimestamps;
        SubtitleShowDuration = settings.SubtitleShowDuration;
        SubtitlesFontSize = settings.SubtitlesFontSize;
        SubtitlesLocation = settings.SubtitlesLocation;
        SubtitlesRollingDirection = settings.SubtitlesRollingDirection;
        SubtitlesSpeed = settings.SubtitlesSpeed;
        if (settings.TextColor != null)
            TextColor = $"#{settings.TextColor.Value.R:X2}{settings.TextColor.Value.G:X2}{settings.TextColor.Value.B:X2}";
        TimeOffset = settings.TimeOffset;
    }

    public TwitchSubtitlesSettings ToSettings()
    {
        return new TwitchSubtitlesSettings
        {
            ASS = ASS,
            SubtitlesType =
                (RegularSubtitles ? SubtitlesType.RegularSubtitles :
                (RollingChatSubtitles ? SubtitlesType.RollingChatSubtitles :
                (StaticChatSubtitles ? SubtitlesType.StaticChatSubtitles :
                (ChatTextFile ? SubtitlesType.ChatTextFile : 0)))),
            BoldText = BoldText,
            ColorUserNames = ColorUserNames,
            RemoveEmoticonNames = RemoveEmoticonNames,
            ShowTimestamps = ShowTimestamps,
            SubtitleShowDuration = SubtitleShowDuration,
            SubtitlesFontSize = SubtitlesFontSize,
            SubtitlesLocation = SubtitlesLocation,
            SubtitlesRollingDirection = SubtitlesRollingDirection,
            SubtitlesSpeed = SubtitlesSpeed,
            TextColor = StringToColor(TextColor),
            TimeOffset = TimeOffset
        };
    }

    [GeneratedRegex(@"^(?:[a-fA-F0-9]{6}|[a-fA-F0-9]{8})$")]
    private static partial Regex RegexColorInHex();

    private static Color? StringToColor(string colorStr)
    {
        if (string.IsNullOrEmpty(colorStr))
            return null;

        if (RegexColorInHex().IsMatch(colorStr))
            colorStr = $"#{colorStr}";

        try
        {
            var tc = TypeDescriptor.GetConverter(typeof(Color));
            return tc.ConvertFromString(colorStr) as Color?;
        }
        catch
        {
            return null;
        }
    }
}

#endregion
