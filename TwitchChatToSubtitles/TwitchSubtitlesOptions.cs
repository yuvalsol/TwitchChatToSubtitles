using CommandLine;
using CommandLine.Text;
using TwitchChatToSubtitles.Library;

namespace TwitchChatToSubtitles;

#region Option Interfaces

internal interface IAllSubtitleTypesOptions
{
    [Option("JsonFile", Required = false, HelpText = "Path to Twitch chat JSON file.")]
    string JsonFile { get; set; }

    [Option("SubtitlesFontSize", Required = false, HelpText = "The font size of the subtitles. Valid values: Regular, Bigger, Biggest.")]
    SubtitlesFontSize SubtitlesFontSize { get; set; }

    [Option("ShowTimestamps", Required = false, HelpText = "Whether to show chat message timestamps.")]
    bool ShowTimestamps { get; set; }

    [Option("TimeOffset", Required = false, HelpText = "Time offset for all subtitles, in seconds.")]
    int TimeOffset { get; set; }

    [Option("RemoveEmoticonNames", Required = false, HelpText = "Remove emoticon and badge names.")]
    bool RemoveEmoticonNames { get; set; }

    [Option("ColorUserNames", Required = false, HelpText = "Whether to color user names.")]
    bool ColorUserNames { get; set; }
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

    [Option("SubtitlesSpeed", Required = false, HelpText = "How fast the subtitles roll. Valid values: Regular, Faster, Fastest.")]
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
}

#endregion

#region Option Classes

internal class RegularSubtitlesOptions : IRegularSubtitlesOptions
{
    public bool RegularSubtitles { get; set; }
    public int SubtitleShowDuration { get; set; }

    [Usage(ApplicationAlias = "TwitchChatToSubtitles.exe")]
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
                "2. Color user names, remove emoticon names, font size `Bigger`, subtitles will use ASSA tags",
                new TwitchSubtitlesOptions
                {
                    RegularSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    ColorUserNames = true,
                    RemoveEmoticonNames = true,
                    SubtitlesFontSize = SubtitlesFontSize.Bigger
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
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public SubtitlesLocation SubtitlesLocation { get; set; }

    [Usage(ApplicationAlias = "TwitchChatToSubtitles.exe")]
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
                "2. Color user names, remove emoticon names, font size `Bigger`, subtitles location will default to whole `Left` side of the screen",
                new TwitchSubtitlesOptions
                {
                    RollingChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    ColorUserNames = true,
                    RemoveEmoticonNames = true,
                    SubtitlesFontSize = SubtitlesFontSize.Bigger
                }
            );

            yield return new Example(
                "3. Subtitles will roll faster than regular speed and will appear on the right side and top half of the screen",
                new TwitchSubtitlesOptions
                {
                    RollingChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    SubtitlesLocation = SubtitlesLocation.RightTopHalf,
                    SubtitlesSpeed = SubtitlesSpeed.Faster
                }
            );
        }
    }
}

internal class StaticChatSubtitlesOptions : IStaticChatSubtitlesOptions, IChatSubtitlesOptions
{
    public bool StaticChatSubtitles { get; set; }
    public SubtitlesLocation SubtitlesLocation { get; set; }

    [Usage(ApplicationAlias = "TwitchChatToSubtitles.exe")]
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
                "2. Color user names, remove emoticon names, font size `Bigger`, subtitles location will default to whole `Left` side of the screen",
                new TwitchSubtitlesOptions
                {
                    StaticChatSubtitles = true,
                    JsonFile = @"C:\Path\To\Twitch Chat.json",
                    ColorUserNames = true,
                    RemoveEmoticonNames = true,
                    SubtitlesFontSize = SubtitlesFontSize.Bigger
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

internal class AllSubtitleTypesOptions : IAllSubtitleTypesOptions
{
    public string JsonFile { get; set; }
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public bool ShowTimestamps { get; set; }
    public int TimeOffset { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ColorUserNames { get; set; }
}

#endregion

#region Twitch Subtitles Options

internal class TwitchSubtitlesOptions
    : IAllSubtitleTypesOptions
    , IRegularSubtitlesOptions
    , IRollingChatSubtitlesOptions
    , IStaticChatSubtitlesOptions
    , IChatSubtitlesOptions
{
    public string JsonFile { get; set; }
    public bool RegularSubtitles { get; set; }
    public int SubtitleShowDuration { get; set; }
    public bool RollingChatSubtitles { get; set; }
    public SubtitlesSpeed SubtitlesSpeed { get; set; }
    public bool StaticChatSubtitles { get; set; }
    public SubtitlesLocation SubtitlesLocation { get; set; }
    public SubtitlesFontSize SubtitlesFontSize { get; set; }
    public bool ShowTimestamps { get; set; }
    public int TimeOffset { get; set; }
    public bool RemoveEmoticonNames { get; set; }
    public bool ColorUserNames { get; set; }

    public TwitchSubtitlesSettings ToSettings()
    {
        return new TwitchSubtitlesSettings
        {
            SubtitlesType =
                (RegularSubtitles ? SubtitlesType.RegularSubtitles :
                (RollingChatSubtitles ? SubtitlesType.RollingChatSubtitles :
                (StaticChatSubtitles ? SubtitlesType.StaticChatSubtitles : 0))),
            SubtitleShowDuration = SubtitleShowDuration,
            SubtitlesSpeed = SubtitlesSpeed,
            SubtitlesLocation = SubtitlesLocation,
            SubtitlesFontSize = SubtitlesFontSize,
            ShowTimestamps = ShowTimestamps,
            TimeOffset = TimeOffset,
            RemoveEmoticonNames = RemoveEmoticonNames,
            ColorUserNames = ColorUserNames
        };
    }
}

#endregion
